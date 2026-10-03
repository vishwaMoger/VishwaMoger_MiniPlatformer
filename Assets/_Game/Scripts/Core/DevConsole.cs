#if UNITY_EDITOR
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Editor-only automation hook used for playtesting from the command line.
    /// Write commands (one per line) to Temp/devcmd.txt; results are written to Temp/devstate.txt.
    ///   level 3 | daily | menu title|levels|heroes | slide left|right|up|down|cell N | solve | hint | freeze
    ///   win | die | wait 0.5 | state | popup daily|spin|missions|shop | coins 500 | newday | click LABEL
    ///   cover | clip | sheet | trailer   store assets, see DevCapture
    /// </summary>
    public class DevConsole : MonoBehaviour
    {
        const string CmdPath = "Temp/devcmd.txt";
        const string StatePath = "Temp/devstate.txt";
        bool m_Running;
        static string m_Measure = "";

        void Update()
        {
            if (m_Running || Game.I == null || UIRoot.I == null || !File.Exists(CmdPath)) return;
            string[] lines;
            try { lines = File.ReadAllLines(CmdPath); File.Delete(CmdPath); }
            catch { return; }
            StartCoroutine(Run(lines));
        }

        IEnumerator Run(string[] lines)
        {
            m_Running = true;
            foreach (var raw in lines)
            {
                var a = raw.Trim().Split(' ');
                if (a.Length == 0 || a[0].Length == 0) continue;
                float F(int i, float def = 0f) => a.Length > i && float.TryParse(a[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;
                var board = Game.I.Board;
                switch (a[0])
                {
                    case "level": Game.I.PlayLevel((int)F(1, 1)); break;
                    case "daily": Game.I.PlayDaily(); break;
                    case "menu":
                        var s = a.Length > 1 && a[1] == "levels" ? MenuScreen.LevelSelect : a.Length > 1 && a[1] == "heroes" ? MenuScreen.Characters : MenuScreen.Title;
                        if (Game.I.InLevel) Game.I.GoToMenu(s); else UIRoot.I.ShowMenu(s);
                        break;
                    case "back": UIRoot.I.MenuBack(); break;
                    case "pause": Game.I.TogglePause(); break;
                    case "slide":
                        if (board == null) break;
                        {
                            var lv = board.Level;
                            int hole = System.Array.IndexOf(board.CurrentLayout(), -1);
                            int cell = a.Length > 1 && a[1] == "cell" ? (int)F(2) :
                                a[1] == "left" ? lv.Step(hole, RoomDef.R) : a[1] == "right" ? lv.Step(hole, RoomDef.L) :
                                a[1] == "up" ? lv.Step(hole, RoomDef.D) : lv.Step(hole, RoomDef.U);
                            m_Measure = "slide " + board.TrySlideFrom(cell);
                            yield return new WaitForSeconds(0.3f);
                        }
                        break;
                    case "solve":
                        // play the hint solver's moves until the route is connected (or give up)
                        if (board == null) break;
                        for (int i = 0; i < 40 && board != null && !board.Finished; i++)
                        {
                            if (board.Level.Connected(board.CurrentLayout())) { m_Measure = "solved in " + board.Slides; break; }
                            int c = board.Level.HintMove(board.CurrentLayout());
                            if (c < 0) { m_Measure = "no hint"; break; }
                            if (!board.TrySlideFrom(c)) { yield return new WaitForSeconds(0.25f); continue; }
                            yield return new WaitForSeconds(0.3f);
                        }
                        break;
                    case "autotest":
                    {
                        // autotest <from> <to>: freeze the hero, solve with the hint solver, release, and check the hero
                        // walks the route to the trophy. Reports levels where the walk fails.
                        var sb = new StringBuilder();
                        int ok = 0, total = 0;
                        for (int n = (int)F(1, 1); n <= (int)F(2, 10); n++)
                        {
                            Game.I.PlayLevel(n);
                            yield return new WaitForSecondsRealtime(1.4f);
                            var bd = Game.I.Board;
                            if (bd == null || bd.Level.number != n) { sb.Append($"L{n}:noload "); continue; }
                            bd.Freeze(120f);
                            for (int i = 0; i < 60 && !bd.Level.Connected(bd.CurrentLayout()); i++)
                            {
                                int c = bd.Level.HintMove(bd.CurrentLayout());
                                if (c < 0) break;
                                if (!bd.TrySlideFrom(c)) { yield return new WaitForSeconds(0.2f); continue; }
                                yield return new WaitForSeconds(0.25f);
                            }
                            bool connected = bd.Level.Connected(bd.CurrentLayout());
                            while (!bd.Started) yield return null;
                            bd.Freeze(0.01f);
                            float t0 = Time.time;
                            while (!bd.Finished && Time.time - t0 < 45f) yield return null;
                            total++;
                            bool won = bd.Hero != null && bd.Hero.Won;
                            if (won) ok++;
                            else sb.Append($"L{n}:{(connected ? "" : "unsolved,")}{(bd.Hero != null && bd.Hero.Dead ? "died" : "stuck")}@({bd.Hero?.transform.position.x:0.0},{bd.Hero?.FeetY:0.0}) ");
                            yield return new WaitForSecondsRealtime(1.6f);
                        }
                        m_Measure = $"autotest {ok}/{total} won. " + sb;
                        break;
                    }
                    case "hint": if (board != null) m_Measure = "hint " + board.ShowHint(); break;
                    case "path":
                        if (board != null)
                        {
                            var lr = board.GetComponentInChildren<LineRenderer>();
                            var pts = new Vector3[lr.positionCount];
                            lr.GetPositions(pts);
                            var psb = new StringBuilder("path:");
                            foreach (var pt in pts) psb.Append($" ({pt.x:0.0},{pt.y:0.0})");
                            m_Measure = psb.ToString() + " complete " + board.PathComplete + " progress " + board.Progress.ToString("0.00");
                        }
                        break;
                    case "freeze": board?.Freeze(F(1, 6f)); break;
                    case "win":
                        if (board != null && board.Hero != null)
                        {
                            var goal = board.GetComponentInChildren<TrekGoal>();
                            if (goal != null) { board.Hero.Body.position = (Vector2)goal.transform.position + Vector2.up * 0.8f; board.Hero.Moving = true; }
                        }
                        break;
                    case "die": board?.Hero?.Die(false); break;
                    case "shot":
                        ScreenCapture.CaptureScreenshot("Temp/" + (a.Length > 1 ? a[1] : "shot") + ".png");
                        yield return null;
                        break;
                    case "wait": yield return new WaitForSecondsRealtime(F(1, 0.5f)); break;
                    case "popup":
                        switch (a.Length > 1 ? a[1] : "")
                        {
                            case "daily": UIRoot.I.ShowDaily(); break;
                            case "spin": UIRoot.I.ShowSpin(); break;
                            case "missions": UIRoot.I.ShowMissions(); break;
                            case "shop": UIRoot.I.ShowShop(); break;
                        }
                        break;
                    case "coins": Economy.AddCoins((int)F(1, 500)); break;
                    case "newday":
                        SaveSystem.Data.dailyDay = Economy.Today - 1; SaveSystem.Data.spinDay = -1; SaveSystem.Data.missionDay = -1;
                        SaveSystem.Save();
                        break;
                    case "unlock": SaveSystem.Data.level = Mathf.Max(SaveSystem.Data.level, (int)F(1, 30)); SaveSystem.Save(); break;
                    case "reset": SaveSystem.ResetProgress(); break;
                    case "timescale": Time.timeScale = F(1, 1f); break;
                    case "state": WriteState(); break;
                    case "gen":
                    {
                        // gen <from> <to>: generate levels and report sizes / par / route length (generator sanity)
                        var sb = new StringBuilder();
                        for (int n = (int)F(1, 1); n <= (int)F(2, 20); n++)
                        {
                            var lv = Game.BuildLevel(n, false);
                            sb.Append($"L{n}:{lv.w}x{lv.h} path {lv.pathLength} par {lv.par} t {lv.targetTime:0} hint {(lv.HintMove(lv.layout) >= 0 ? "y" : "N")} | ");
                        }
                        m_Measure = sb.ToString();
                        break;
                    }
                    case "cover":
                        yield return DevCapture.Covers((int)F(1, 9), (int)F(2, 9));
                        m_Measure = DevCapture.LastResult;
                        break;
                    case "clip": // clip <name> <level> <portrait 0|1> <w> <h> <camera> [maxSec] [slideGap] [hold 0|1]
                        yield return DevCapture.Clip(a[1], (int)F(2, 1), F(3) > 0f, (int)F(4, 1920), (int)F(5, 1080), a.Length > 6 ? a[6] : "fit", F(7, 14f), F(8, 0.5f), F(9) > 0f);
                        m_Measure = DevCapture.LastResult;
                        break;
                    case "bitrate":
                        DevCapture.Bitrate = a.Length > 1 && a[1] == "low" ? UnityEditor.VideoBitrateMode.Low : a.Length > 1 && a[1] == "medium" ? UnityEditor.VideoBitrateMode.Medium : UnityEditor.VideoBitrateMode.High;
                        break;
                    case "sheet": m_Measure = DevCapture.ContactSheet(a[1], (int)F(2, 6)); break;
                    case "trailer": // trailer <out.mp4> <w> <h> <cover.png> <clip:start:len>...
                        m_Measure = DevCapture.Trailer(a[1], (int)F(2), (int)F(3), a[4], a[5..]);
                        break;
                    case "click":
                    {
                        string label = string.Join(" ", a, 1, a.Length - 1);
                        foreach (var b in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                        {
                            if (!b.isActiveAndEnabled || !b.interactable) continue;
                            var t = b.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                            if ((t != null && string.Equals(t.text, label, System.StringComparison.OrdinalIgnoreCase)) || b.name == label)
                            {
                                b.onClick.Invoke();
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            WriteState();
            m_Running = false;
        }

        static void WriteState()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"time {Time.time:0.00} inLevel {Game.I.InLevel} paused {Game.I.IsPaused} timeScale {Time.timeScale} sdk {Platform.Ready} adPlaying {Platform.AdPlaying} level {Game.I.CurrentLevel} next {SaveSystem.Data.level}");
            var popups = GameObject.Find("Popups");
            if (popups != null) { sb.Append("popups:"); foreach (Transform c in popups.transform) sb.Append(" " + c.name); sb.AppendLine(); }
            var b = Game.I.Board;
            if (b != null)
            {
                var lv = b.Level;
                sb.AppendLine($"board {lv.w}x{lv.h} slides {b.Slides}/{lv.par} time {b.Elapsed:0.0}/{lv.targetTime:0} coins {b.CoinsTaken}/{b.CoinsTotal} started {b.Started} finished {b.Finished} connected {lv.Connected(b.CurrentLayout())}");
                sb.AppendLine("layout " + string.Join(",", b.CurrentLayout()));
                if (b.Hero != null) sb.AppendLine($"hero ({b.Hero.transform.position.x:0.00},{b.Hero.FeetY:0.00}) facing {b.Hero.Facing} moving {b.Hero.Moving} dead {b.Hero.Dead} won {b.Hero.Won}");
            }
            sb.AppendLine($"coins {Economy.Coins} daily {Economy.DailyReady} freeSpin {Economy.FreeSpinReady} rewarded {Platform.RewardedAvailable} stars {SaveSystem.TotalStars()}");
            sb.AppendLine(m_Measure);
            File.WriteAllText(StatePath, sb.ToString());
        }
    }
}
#endif
