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
    ///   level 3 | menu title|levels|heroes | hold right 1.5 | press jump | tp 10 5 | god on | unlock | state | wait 0.5
    /// </summary>
    public class DevConsole : MonoBehaviour
    {
        const string CmdPath = "Temp/devcmd.txt";
        const string StatePath = "Temp/devstate.txt";
        bool m_God;
        bool m_Running;
        static string m_Measure = "";

        void Update()
        {
            if (m_God && PlayerController.I != null && LevelSession.Current != null) LevelSession.Current.hearts = 3;
            if (m_Running || !File.Exists(CmdPath)) return;
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
                switch (a[0])
                {
                    case "level": Game.I.PlayLevel((int)F(1)); break;
                    case "menu":
                        var s = a.Length > 1 && a[1] == "levels" ? MenuScreen.LevelSelect : a.Length > 1 && a[1] == "heroes" ? MenuScreen.Characters : MenuScreen.Title;
                        if (Game.I.InLevel) Game.I.GoToMenu(s); else UIRoot.I.ShowMenu(s, (int)F(2, SaveSystem.Data.lastWorld));
                        break;
                    case "back": UIRoot.I.MenuBack(); break;
                    case "settings": UIRoot.I.OpenSettings(false); break;
                    case "pause": Game.I.TogglePause(); break;
                    case "hold":
                        Set(a[1], true);
                        yield return new WaitForSecondsRealtime(F(2, 0.5f));
                        Set(a[1], false);
                        break;
                    case "press":
                        Set(a[1], true);
                        yield return null;
                        yield return null;
                        Set(a[1], false);
                        break;
                    case "on": Set(a[1], true); break;
                    case "off": Set(a[1], false); break;
                    case "shot":
                        ScreenCapture.CaptureScreenshot("Temp/" + (a.Length > 1 ? a[1] : "shot") + ".png");
                        yield return null;
                        break;
                    case "wait": yield return new WaitForSecondsRealtime(F(1, 0.5f)); break;
                    case "jumpdist":
                    {
                        var pl = PlayerController.I;
                        float x0 = pl.transform.position.x, y0 = pl.FeetY, maxY = y0, t0 = Time.time;
                        Set("jump", true);
                        while (pl.IsGrounded && Time.time - t0 < 0.5f) yield return null;
                        if (a.Length > 1 && a[1] == "double") { yield return new WaitForSeconds(0.42f); Set("jump", false); yield return null; yield return null; Set("jump", true); }
                        while (!pl.IsGrounded && Time.time - t0 < 4f) { maxY = Mathf.Max(maxY, pl.FeetY); yield return null; }
                        Set("jump", false);
                        m_Measure = $"jumpdist: dx {pl.transform.position.x - x0:0.00} apex +{maxY - y0:0.00} air {Time.time - t0:0.00}s";
                        break;
                    }
                    case "measure":
                    {
                        var pl = PlayerController.I;
                        float y0 = pl.FeetY, x0 = pl.transform.position.x, maxY = y0, t0 = Time.time;
                        float dur = F(1, 1.5f);
                        while (Time.time - t0 < dur)
                        {
                            maxY = Mathf.Max(maxY, pl.FeetY);
                            yield return null;
                        }
                        m_Measure = $"measure: apex +{maxY - y0:0.00}  dx {pl.transform.position.x - x0:0.00}  endY {pl.FeetY - y0:0.00}";
                        break;
                    }
                    case "tp":
                        var p = PlayerController.I;
                        if (p != null) { p.Body.position = new Vector2(F(1), F(2) + 0.75f); p.Body.linearVelocity = Vector2.zero; }
                        break;
                    case "tpobj":
                    {
                        // tpobj <name> [index]: stand on top of a level object
                        var pl = PlayerController.I;
                        int idx = (int)F(2, 0f), n = 0;
                        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
                        {
                            if (t.name != a[1] || t.parent == null || t.parent.name != "Entities") continue;
                            if (n++ != idx) continue;
                            var col = t.GetComponent<Collider2D>();
                            float top = col != null ? col.bounds.max.y : t.position.y + 0.5f;
                            pl.Body.position = new Vector2(t.position.x, top + PlayerController.ColliderHeight * 0.5f + 0.02f);
                            pl.Body.linearVelocity = Vector2.zero;
                            break;
                        }
                        break;
                    }
                    case "god": m_God = a.Length < 2 || a[1] != "off"; break;
                    case "unlock":
                        SaveSystem.Data.unlocked = LevelCatalog.LevelCount;
                        SaveSystem.Save();
                        break;
                    case "stars":
                        for (int i = 0; i < SaveSystem.Data.stars.Length; i++) SaveSystem.Data.stars[i] = 7;
                        SaveSystem.Save();
                        break;
                    case "reset": SaveSystem.ResetProgress(); break;
                    case "zoom": CameraRig.I.SetZoom(F(1, 1f)); CameraRig.I.ZoomPunch(F(1, 1f)); break;
                    case "cam":
                        CameraRig.I.SetTarget(null, false);
                        CameraRig.I.clampToBounds = false;
                        CameraRig.I.SnapTo(new Vector2(F(1), F(2)));
                        break;
                    case "follow":
                        if (PlayerController.I != null) { CameraRig.I.clampToBounds = true; CameraRig.I.SetTarget(PlayerController.I.transform, true); }
                        break;
                    case "timescale": Time.timeScale = F(1, 1f); break;
                    case "state": WriteState(); break;
                    case "win": LevelSession.Current?.Complete(); break;
                    case "cover": yield return DevCapture.Covers((int)F(1)); break;
                    case "clip": yield return DevCapture.Clip(a[1], (int)F(2), F(3, 5f), (int)F(4, 1920), (int)F(5, 1080), F(6, 15f)); break;
                    case "scene": yield return DevCapture.Scene(a[1], a[2], (int)F(3, 1920), (int)F(4, 1080), F(5, 11f)); break;
                    case "sheet": m_Measure = DevCapture.ContactSheet(a[1], (int)F(2, 15)); break;
                    case "trailer":
                    {
                        // trailer <out.mp4> <w> <h> <cover.png> <clip:start:count>...
                        string res;
                        try { res = DevCapture.Trailer(a[1], (int)F(2), (int)F(3), a[4], a[5..]); }
                        catch (System.Exception e) { res = "trailer failed: " + e.Message; }
                        m_Measure = res;
                        break;
                    }
                    case "die":
                    {
                        var ls = LevelSession.Current;
                        if (ls != null && ls.player != null) { ls.hearts = 1; ls.LoseHeart(); ls.player.Die(); }
                        break;
                    }
                    case "sessiontime": if (LevelSession.Current != null) LevelSession.Current.time = F(1); break;
                    case "click":
                    {
                        // click <label or object name>: presses the first active, interactable button that matches
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

        static void Set(string what, bool held)
        {
            switch (what)
            {
                case "left": GameInput.TouchLeft = held; break;
                case "right": GameInput.TouchRight = held; break;
                case "jump": GameInput.TouchJump = held; break;
                case "dash": GameInput.TouchDash = held; break;
                case "down": GameInput.TouchDown = held; break;
            }
        }

        static void WriteState()
        {
            var sb = new StringBuilder();
            var p = PlayerController.I;
            var s = LevelSession.Current;
            sb.AppendLine($"time {Time.time:0.00} inLevel {Game.I.InLevel} paused {Game.I.IsPaused} timeScale {Time.timeScale} sdk {Platform.Ready} adPlaying {Platform.AdPlaying} level {Game.I.CurrentLevel} unlocked {SaveSystem.Data.unlocked}");
            var popups = GameObject.Find("Popups");
            if (popups != null) { sb.Append("popups:"); foreach (Transform c in popups.transform) sb.Append(" " + c.name); sb.AppendLine(); }
            if (p != null)
                sb.AppendLine($"player feet ({p.transform.position.x:0.00},{p.FeetY:0.00}) vel {p.Velocity} grounded {p.IsGrounded} wall {p.IsWallSliding} dead {p.IsDead} ctrl {p.Controllable}");
            if (s != null)
                sb.AppendLine($"hearts {s.hearts} fruits {s.fruitsCollected}/{s.fruitsTotal} time {s.time:0.0} finished {s.finished} checkpoint {s.checkpoint}");
            var icon = GameObject.Find("FruitIcon");
            if (icon != null) sb.AppendLine($"fruitIcon scale {icon.transform.localScale.x:0.000}");
            sb.AppendLine(m_Measure);
            File.WriteAllText(StatePath, sb.ToString());
        }
    }
}
#endif
