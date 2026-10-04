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
                            for (int i = 0; i < 200 && !bd.Level.Connected(bd.CurrentLayout()); i++)
                            {
                                int c = bd.HeroHint(); // the same hint the player gets (search, else undo the scramble)
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
                    case "curate":
                    {
                        // curate <portrait 0|1> [candidates]: for each campaign level, generate many candidates and keep
                        // the one closest to the difficulty curve with a fair start; writes the {seed, par} table.
                        bool portrait = F(1) > 0f;
                        int candidates = (int)F(2, 80);
                        int budget = (int)F(3, 1500000);
                        int fromN = (int)F(4, 1), toN = (int)F(5, TrekLevel.CampaignLength);
                        var table = new StringBuilder();
                        var report = new StringBuilder();
                        for (int n = fromN; n <= toN; n++)
                        {
                            int target = TrekLevel.TargetSlides[n - 1];
                            TrekLevel best = null; int bestOpt = -1; float bestScore = float.MaxValue;
                            for (int k = 0; k < candidates; k++)
                            {
                                var lv = TrekLevel.Generate(n, n * 1009 + 17 + k * 104729, false, portrait);
                                if (lv.number != n) continue; // generator fell back to a tiny board
                                int opt = lv.SolutionLength(lv.layout, budget);
                                if (opt < 1) continue;
                                var walk = lv.WalkFromStart(lv.layout);
                                float dist = 0f;
                                for (int i = 1; i < walk.points.Count; i++) dist += Vector2.Distance(walk.points[i - 1], walk.points[i]);
                                float score = Mathf.Abs(opt - target) * 10f;
                                if (n < 8)
                                {
                                    // learning levels: a fair start, no rush
                                    if (walk.died) score += 25f + Mathf.Max(0f, 16f - dist) * 2f;
                                }
                                else
                                {
                                    // from level 8 the hero heads for danger unless you act: time pressure, but always enough
                                    // walk (hero speed 2.6/s, plus the 3 s countdown) to make the needed slides
                                    float need = 5f + opt * 1.4f;
                                    if (!walk.died) score += n >= 15 ? 12f : 6f;
                                    else if (dist < need) score += (need - dist) * 3f;
                                    else score += Mathf.Max(0f, dist - need - 14f) * 0.5f; // not so far away that it is no pressure at all
                                }
                                int deadly = 0; foreach (var r in lv.rooms) if (r.Deadly) deadly++;
                                if (n >= 6 && deadly < 2) score += (2 - deadly) * 4f; // later levels should have traps to avoid
                                if (score < bestScore) { bestScore = score; best = lv; bestOpt = opt; }
                                yield return null;
                            }
                            if (best == null) { report.Append($"L{n}:NONE "); table.Append("            { 0, 0 }," + System.Environment.NewLine); continue; }
                            int par = bestOpt + (n <= 6 ? 1 : 2);
                            table.Append($"            {{ {best.seed}, {par} }}, // {n}: {best.w}x{best.h}, {bestOpt} slides, route {best.pathLength}" + System.Environment.NewLine);
                            report.Append($"L{n}:{bestOpt}/{target}{(bestScore >= 10f ? "!" : "")} ");
                        }
                        File.WriteAllText($"Temp/campaign_{(portrait ? "portrait" : "landscape")}.txt", table.ToString());
                        m_Measure = $"curate {(portrait ? "portrait" : "landscape")}: " + report;
                        break;
                    }
                    case "hintstress":
                    {
                        // hintstress <from> <to> <trials> [portrait 0|1]: mid-game states (random slides, hero parked where
                        // their walk ends), then the board's hint logic (search, else undo the scramble, else rescue the
                        // hero to the start) is followed until the hero's walk reaches the trophy. Reports failures.
                        var sb = new StringBuilder();
                        int trials = (int)F(3, 30), ok = 0, fail = 0, total = 0, rescues = 0, undos = 0, searched = 0, steps = 0;
                        float worstMs = 0f;
                        var rng = new System.Random(1234);
                        var sw = new System.Diagnostics.Stopwatch();
                        for (int n = (int)F(1, 1); n <= (int)F(2, 20); n++)
                        {
                            var lv = TrekLevel.Campaign(n, F(4) > 0f);
                            for (int t = 0; t < trials; t++)
                            {
                                var lay = (int[])lv.layout.Clone();
                                var path = new System.Collections.Generic.List<int>(lv.scramblePath);
                                void Slide(int c)
                                {
                                    int hole = System.Array.IndexOf(lay, -1);
                                    lay[hole] = lay[c]; lay[c] = -1;
                                    if (path.Count >= 2 && path[path.Count - 2] == c) path.RemoveAt(path.Count - 1); else path.Add(c);
                                }
                                int k = rng.Next(14);
                                for (int i = 0; i < k; i++) { var nb = lv.Neighbours(System.Array.IndexOf(lay, -1)); Slide(nb[rng.Next(nb.Count)]); }
                                var walk = lv.WalkFromStart(lay);
                                if (walk.died || walk.reached) continue;
                                int heroRoom = lay[walk.cells[walk.cells.Count - 1]];
                                float hx = 3.5f, dir = rng.Next(2) == 0 ? 1f : -1f;
                                total++;
                                bool solved = false;
                                for (int step = 0; step < 150 && !solved; step++)
                                {
                                    int hc = System.Array.IndexOf(lay, heroRoom);
                                    if (lv.Walk(lay, hc, hx, dir, false).reached) { solved = true; break; }
                                    sw.Restart();
                                    int mv = lv.HintMove(lay, heroRoom, hx, dir, 60000, 150);
                                    sw.Stop();
                                    worstMs = Mathf.Max(worstMs, (float)sw.Elapsed.TotalMilliseconds);
                                    if (mv < 0 && heroRoom >= lv.pathLength)
                                    {
                                        sw.Restart();
                                        mv = lv.HintMove(lay, heroRoom, hx, dir, 250000, 400);
                                        sw.Stop();
                                        worstMs = Mathf.Max(worstMs, (float)sw.Elapsed.TotalMilliseconds);
                                    }
                                    if (mv >= 0) searched++;
                                    else
                                    {
                                        int undo = path.Count >= 2 ? path[path.Count - 2] : -1;
                                        if (heroRoom >= lv.pathLength || undo < 0)
                                        {
                                            rescues++;
                                            heroRoom = lv.startRoom; lv.StartSpawn(out hx, out dir);
                                            continue;
                                        }
                                        mv = undo; undos++;
                                    }
                                    Slide(mv);
                                    steps++;
                                }
                                if (solved) ok++;
                                else { fail++; if (fail <= 10) sb.Append($"L{n}:unsolved(room {heroRoom}) "); }
                            }
                            yield return null;
                        }
                        m_Measure = $"hintstress {ok}/{total} solved, {fail} failed; moves: {searched} searched, {undos} undo; {rescues} rescues; {steps} slides; worst {worstMs:0}ms. " + sb;
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
