#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Editor-only store-asset capture (driven through DevConsole):
    ///   cover &lt;world&gt;                      renders the three CrazyGames covers + the portrait video opener
    ///   clip &lt;name&gt; &lt;level&gt; &lt;sec&gt; &lt;w&gt; &lt;h&gt; &lt;halfW&gt;   records bot-played gameplay frames at a fixed 30 fps
    ///   trailer &lt;out.mp4&gt; &lt;w&gt; &lt;h&gt; &lt;cover.png&gt; &lt;clip:start:len&gt;...   crossfades cover + clips into an H.264 MP4 (no audio)
    /// </summary>
    public static class DevCapture
    {
        public const string OutDir = "Builds/CrazyGames_Submission";
        const string FrameDir = "Temp/capture";
        const int Fps = 30;
        const float GroundY = -5.5f; // MenuStage ground surface

        static Material s_Unlit;
        static Material Unlit => s_Unlit != null ? s_Unlit : (s_Unlit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")));

        // ---------------------------------------------------------------- covers

        struct Item { public Sprite sprite; public float u, v, scale; public bool onGround, flip; public Color color; public int order; }

        public static IEnumerator Covers(int world)
        {
            Directory.CreateDirectory(OutDir);
            if (Game.I.InLevel) { Game.I.GoToMenu(MenuScreen.Title); yield return new WaitForSecondsRealtime(2.5f); }
            Game.I.SetMenuWorld(world);
            yield return null;
            var stage = GameObject.Find("MenuStage");
            var runner = stage != null ? stage.transform.Find("Hero") : null;
            if (runner != null) runner.gameObject.SetActive(false);

            yield return Shot(OutDir + "/cover_1920x1080.png", 1920, 1080, 8.4375f, Landscape());
            yield return Shot(OutDir + "/cover_800x1200.png", 800, 1200, 11.25f, Portrait());
            yield return Shot(OutDir + "/cover_800x800.png", 800, 800, 8.6f, Square());
            yield return Shot(FrameDir + "/cover_video_1080x1620.png", 1080, 1620, 11.25f, Portrait());
            yield return Shot(FrameDir + "/cover_video_1920x1080.png", 1920, 1080, 8.4375f, Landscape());

            if (runner != null) runner.gameObject.SetActive(true);
            CameraRig.I.ZoomPunch(1f);
            CameraRig.I.SetZoom(1f);
            CameraRig.I.SnapTo(Vector2.zero);
        }

        static Sprite Logo => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Branding/logo_cover.png");
        static Sprite Hero(int i, string anim, int frame = 0)
        {
            var f = GameAssets.I.Frames(LevelCatalog.Characters[i].folder + "/" + anim);
            return f != null && f.Length > 0 ? f[Mathf.Min(frame, f.Length - 1)] : null;
        }
        static Sprite Fruit(string name) => GameAssets.I.Frames("PA1/Items/Fruits/" + name)[0];

        // u, v are fractions of the half view size from the view centre; onGround items stand on the menu ground
        static Item Glow(float v, float scale, float alpha) => new Item { sprite = UIArt.Glow, u = 0f, v = v, scale = scale, color = new Color(1f, 0.97f, 0.85f, alpha), order = 1 };
        static Item Rays(float v, float scale) => new Item { sprite = UIArt.Rays, u = 0f, v = v, scale = scale, color = new Color(1f, 0.96f, 0.75f, 0.45f), order = 0 };
        static Item LogoAt(float v, float scale) => new Item { sprite = Logo, u = 0f, v = v, scale = scale, color = Color.white, order = 10 };
        static Item H(int hero, string anim, int frame, float u, float v, float scale, bool flip = false, bool ground = false) =>
            new Item { sprite = Hero(hero, anim, frame), u = u, v = v, scale = scale, flip = flip, onGround = ground, color = Color.white, order = 6 };
        static Item F(string fruit, float u, float v, float scale) => new Item { sprite = Fruit(fruit), u = u, v = v, scale = scale, color = Color.white, order = 4 };

        static List<Item> Landscape() => new List<Item>
        {
            Rays(0.28f, 0.28f), Glow(0.28f, 0.2f, 0.6f), LogoAt(0.3f, 1.2f),
            H(0, "Jump", 0, -0.7f, -0.22f, 3.1f),
            H(1, "Run", 3, 0.64f, 0f, 3.3f, true, true),
            H(3, "Idle", 2, -0.3f, 0f, 2.3f, false, true),
            H(2, "Fall", 0, 0.3f, -0.3f, 2.3f, true),
            F("Apple", -0.86f, 0.62f, 2.2f), F("Cherries", 0.84f, 0.66f, 2.2f), F("Bananas", -0.5f, 0.72f, 1.9f),
            F("Melon", 0.52f, 0.76f, 1.9f), F("Strawberry", -0.08f, -0.42f, 2f), F("Pineapple", 0.1f, -0.36f, 2f), F("Orange", 0.9f, 0.2f, 1.9f),
        };

        static List<Item> Portrait() => new List<Item>
        {
            Rays(0.4f, 0.3f), Glow(0.4f, 0.22f, 0.6f), LogoAt(0.4f, 0.97f),
            H(0, "Jump", 0, -0.42f, -0.18f, 2.9f),
            H(1, "Run", 3, 0.42f, 0f, 2.9f, true, true),
            F("Apple", -0.7f, 0.9f, 2f), F("Cherries", 0.7f, 0.88f, 2f), F("Strawberry", 0.05f, -0.1f, 1.9f),
            F("Pineapple", 0.78f, 0.05f, 1.9f), F("Melon", -0.8f, 0.1f, 1.8f),
        };

        static List<Item> Square() => new List<Item>
        {
            Rays(0.33f, 0.3f), Glow(0.33f, 0.22f, 0.6f), LogoAt(0.33f, 0.84f),
            H(0, "Jump", 0, -0.55f, -0.4f, 2.5f),
            H(1, "Run", 3, 0.55f, 0f, 2.5f, true, true),
            F("Apple", -0.84f, 0.84f, 1.8f), F("Cherries", 0.84f, 0.84f, 1.8f), F("Strawberry", 0.02f, -0.52f, 1.8f),
        };

        /// <summary>Renders the live menu world plus a sprite composition straight from the game camera.</summary>
        static IEnumerator Shot(string path, int w, int h, float halfH, List<Item> items)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var cam = Game.I.Cam;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            cam.targetTexture = rt;
            float zoom = K.CameraSize / halfH;
            CameraRig.I.ZoomPunch(zoom);
            CameraRig.I.SetZoom(zoom);
            float halfW = halfH * w / h;
            float cy = GroundY + halfH * 0.62f; // ground ~19% up from the bottom edge
            CameraRig.I.SnapTo(new Vector2(0f, cy));

            var root = new GameObject("CoverComposition").transform;
            float logoW = 0f;
            foreach (var it in items)
            {
                if (it.sprite == null) continue;
                var go = new GameObject(it.sprite.name);
                go.transform.SetParent(root, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = it.sprite;
                sr.sharedMaterial = Unlit;
                sr.color = it.color;
                sr.flipX = it.flip;
                sr.sortingOrder = Sort.FX + 100 + it.order;
                float s = it.scale;
                if (it.sprite == Logo)
                {
                    // logo fills most of the width on narrow covers, ~55% on the wide one
                    float target = Mathf.Min(halfW * 2f * (w > h ? 0.56f : 0.92f), halfH * 2f * 0.45f * it.sprite.bounds.size.x / it.sprite.bounds.size.y);
                    s = target / it.sprite.bounds.size.x * it.scale;
                    logoW = target;
                }
                else if (it.sprite == UIArt.Rays || it.sprite == UIArt.Glow) s = Mathf.Max(halfW, halfH) * it.scale * 2f; // big soft light
                go.transform.localScale = Vector3.one * s;
                float x = it.u * halfW;
                float y = it.onGround ? GroundY + it.sprite.bounds.size.y * s * 0.5f - 0.1f * s : cy + it.v * halfH;
                go.transform.position = new Vector3(x, y, 0f);
            }

            yield return null;
            yield return null;
            cam.Render();
            SaveRt(rt, path, true);
            cam.targetTexture = null;
            rt.Release();
            Object.Destroy(root.gameObject);
            yield return null;
        }

        static void SaveRt(RenderTexture rt, string path, bool png)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, png ? tex.EncodeToPNG() : tex.EncodeToJPG(95));
            Object.Destroy(tex);
        }

        // ---------------------------------------------------------------- gameplay clips

        public static IEnumerator Clip(string name, int level, float seconds, int w, int h, float halfW)
        {
            string dir = FrameDir + "/" + name;
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);

            HintSign.HideAll = true;
            Game.I.PlayLevel(level);
            yield return new WaitForSecondsRealtime(0.2f);
            // wait for the NEW level (the previous session is still 'running' while the iris closes)
            for (float t = 0; t < 12f && !(LevelSession.Current != null && LevelSession.Current.levelIndex == level && LevelSession.Current.running
                                          && PlayerController.I != null && PlayerController.I.Controllable); t += Time.unscaledDeltaTime)
                yield return null;

            var cam = Game.I.Cam;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            cam.targetTexture = rt;
            float halfH = halfW * h / w;
            float zoom = K.CameraSize / halfH;
            CameraRig.I.ZoomPunch(zoom);
            CameraRig.I.SetZoom(zoom);
            if (PlayerController.I != null) CameraRig.I.SetTarget(PlayerController.I.transform, true);

            var bot = Game.I.gameObject.AddComponent<DemoBot>();
            Time.captureFramerate = Fps;
            for (int i = 0; i < 10; i++) yield return null; // let the camera settle
            int frames = Mathf.RoundToInt(seconds * Fps);
            for (int f = 0; f < frames; f++)
            {
                yield return null;
                cam.Render();
                SaveRt(rt, $"{dir}/f_{f:0000}.jpg", false);
            }
            Time.captureFramerate = 0;
            Object.Destroy(bot);
            GameInput.ClearTouch();
            cam.targetTexture = null;
            rt.Release();
            CameraRig.I.SetZoom(1f);
            HintSign.HideAll = false;
        }

        // ---------------------------------------------------------------- choreographed set pieces

        /// <summary>
        /// Records a hand-made trailer scene (Assets/_Game/Editor/Trailer/&lt;name&gt;.txt): a dry run records the hero's
        /// path, then the real take lays fruit along it and captures frames at a fixed 30 fps.
        /// </summary>
        public static IEnumerator Scene(string name, string outName, int w, int h, float halfW)
        {
            var text = File.ReadAllText($"Assets/_Game/Editor/Trailer/{name}.txt");
            int world = 0; string script = ""; string trail = ""; float seconds = 4f; bool stomp = false;
            foreach (var raw in text.Replace("\r", "").Split('\n'))
            {
                if (raw.Trim() == "---") break;
                int c = raw.IndexOf(':');
                if (c <= 0) continue;
                string k = raw.Substring(0, c).Trim(), v = raw.Substring(c + 1).Trim();
                if (k == "world") int.TryParse(v, out world);
                else if (k == "script") script = v;
                else if (k == "trail") trail = v;
                else if (k == "stomp") stomp = v == "yes";
                else if (k == "seconds") float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds);
            }
            LevelBuilder.DebugLevelText = text;
            LevelBuilder.DebugWorld = world;
            HintSign.HideAll = true;

            // stomp: a dry run finds where the first jump lands, and a (frozen) mushroom is placed exactly there
            Vector2? enemy = null;
            if (stomp)
            {
                var first = new List<Vector2>();
                yield return Take(script, seconds, null, 0, 0, 0f, first, null, null);
                enemy = FirstLanding(first);
            }
            List<Vector2> path = null;
            if (!string.IsNullOrEmpty(trail))
            {
                var dry = new List<Vector2>();
                yield return Take(script, seconds, null, 0, 0, 0f, dry, null, enemy);
                path = dry;
            }
            yield return Take(script, seconds, outName, w, h, halfW, null, TrailPoints(path, trail), enemy);

            LevelBuilder.DebugLevelText = null;
            LevelBuilder.DebugWorld = -1;
            HintSign.HideAll = false;
        }

        /// <summary>
        /// Where to put a stomp target on the first airborne arc: the point where the hero is coming down but still
        /// ~1.4 units up (stomps need the feet above the enemy's middle). Returns the enemy's feet position.
        /// </summary>
        static Vector2? FirstLanding(List<Vector2> path)
        {
            int end = 0;
            while (end + 1 < path.Count && Vector2.Distance(path[end], path[end + 1]) <= 0.8f) end++;
            if (path.Count == 0) return null;
            float groundY = path[end].y - 0.55f;
            for (int i = 1; i <= end; i++)
                if (path[i].y < path[i - 1].y && path[i].y - 0.55f - groundY < 1.4f)
                    return new Vector2(path[i].x + 0.35f, Mathf.Round(groundY));
            return new Vector2(path[end].x, Mathf.Round(groundY));
        }

        static List<Vector2> TrailPoints(List<Vector2> path, string ranges)
        {
            var pts = new List<Vector2>();
            if (path == null || string.IsNullOrEmpty(ranges)) return pts;
            foreach (var r in ranges.Split(' '))
            {
                var ab = r.Split('-');
                if (ab.Length != 2 || !float.TryParse(ab[0], out var x0) || !float.TryParse(ab[1], out var x1)) continue;
                float acc = 1f;
                for (int i = 1; i < path.Count; i++)
                {
                    var p = path[i];
                    if (p.x < x0 || p.x > x1) continue;
                    acc += Vector2.Distance(path[i - 1], p);
                    if (acc < 1.3f) continue;
                    acc = 0f;
                    pts.Add(p);
                }
            }
            return pts;
        }

        /// <summary>One take: builds the scene, runs the choreography, optionally records frames / the hero path.</summary>
        static IEnumerator Take(string script, float seconds, string outName, int w, int h, float halfW, List<Vector2> pathOut, List<Vector2> fruit, Vector2? enemy)
        {
            var before = LevelSession.Current;
            Game.I.PlayLevel(0);
            for (float t = 0; t < 12f && !(LevelSession.Current != null && LevelSession.Current != before && LevelSession.Current.running
                                          && PlayerController.I != null && PlayerController.I.Controllable); t += Time.unscaledDeltaTime)
                yield return null;

            if (enemy.HasValue)
            {
                var ents = GameObject.Find("Entities");
                var walker = Walker.Create(ents != null ? ents.transform : null, enemy.Value, Walker.Kind.Mushroom);
                var f = typeof(Walker).GetField("m_Speed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                f?.SetValue(walker, 0f); // stand still so the stomp is frame-exact
            }
            if (fruit != null && fruit.Count > 0)
            {
                var ents = GameObject.Find("Entities");
                for (int i = 0; i < fruit.Count; i++) MiniPlatformer.Fruit.Create(ents != null ? ents.transform : null, fruit[i], i % 8, false);
            }

            var cam = Game.I.Cam;
            RenderTexture rt = null;
            string dir = null;
            if (outName != null)
            {
                dir = FrameDir + "/" + outName;
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
                rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
                cam.targetTexture = rt;
                float zoom = K.CameraSize / (halfW * h / w);
                CameraRig.I.ZoomPunch(zoom);
                CameraRig.I.SetZoom(zoom);
                CameraRig.I.SetTarget(PlayerController.I.transform, true);
            }

            var run = ScriptedRun.Attach(Game.I.gameObject, script);
            run.pathOut = pathOut;
            // simulate at 60 fps like real play (contact checks run per frame), keep every 2nd frame for a 30 fps video
            Time.captureFramerate = Fps * 2;
            int frames = Mathf.RoundToInt(seconds * Fps);
            for (int f = 0; f < frames * 2; f++)
            {
                yield return null; // manual render: works even when the Editor window isn't repainting
                if (rt != null && (f & 1) == 1) { cam.Render(); SaveRt(rt, $"{dir}/f_{f / 2:0000}.jpg", false); }
            }
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            Object.Destroy(run);
            GameInput.ClearTouch();
            if (rt != null) { cam.targetTexture = null; rt.Release(); }
            CameraRig.I.SetZoom(1f);
            yield return null;
        }

        // ---------------------------------------------------------------- trailer assembly

        /// <param name="clips">"name:startFrame:frameCount" entries</param>
        public static string Trailer(string outPath, int w, int h, string coverPath, string[] clips)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            var attr = new VideoTrackAttributes
            {
                frameRate = new MediaRational(Fps),
                width = (uint)w,
                height = (uint)h,
                includeAlpha = false,
                bitRateMode = VideoBitrateMode.High,
            };
            const int coverHold = 36, fade = 10;
            var frame = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var load = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Color32[] prevTail = null;
            int total = 0;
            using (var enc = new MediaEncoder(outPath, attr))
            {
                // opening: the static cover
                var cover = Load(load, coverPath, w, h);
                for (int i = 0; i < coverHold; i++) { frame.SetPixels32(cover); frame.Apply(false); enc.AddFrame(frame); total++; }
                prevTail = cover;

                foreach (var c in clips)
                {
                    var parts = c.Split(':');
                    string dir = FrameDir + "/" + parts[0];
                    int start = int.Parse(parts[1]), count = int.Parse(parts[2]);
                    for (int i = 0; i < count; i++)
                    {
                        var px = Load(load, $"{dir}/f_{start + i:0000}.jpg", w, h);
                        if (i < fade && prevTail != null)
                        {
                            float k = (i + 1f) / (fade + 1f);
                            var mix = new Color32[px.Length];
                            for (int p = 0; p < px.Length; p++) mix[p] = Color32.Lerp(prevTail[p], px[p], k);
                            px = mix;
                        }
                        frame.SetPixels32(px);
                        frame.Apply(false);
                        enc.AddFrame(frame);
                        total++;
                        if (i == count - 1) prevTail = px;
                    }
                }
            }
            Object.DestroyImmediate(frame);
            Object.DestroyImmediate(load);
            return $"{outPath}: {total} frames, {total / (float)Fps:0.0}s, {new FileInfo(outPath).Length / 1048576f:0.0} MB";
        }

        /// <summary>Grid of every n-th frame of a clip (for picking the best segment). Returns the sheet path.</summary>
        public static string ContactSheet(string clip, int step, int cols = 4, int cellW = 480)
        {
            string dir = FrameDir + "/" + clip;
            var files = Directory.GetFiles(dir, "f_*.jpg");
            System.Array.Sort(files);
            var src = new Texture2D(2, 2);
            src.LoadImage(File.ReadAllBytes(files[0]));
            int cellH = Mathf.RoundToInt(cellW * src.height / (float)src.width);
            int n = (files.Length + step - 1) / step, rows = (n + cols - 1) / cols;
            var sheet = new Texture2D(cols * cellW, rows * cellH, TextureFormat.RGB24, false);
            var rt = RenderTexture.GetTemporary(cellW, cellH);
            for (int i = 0; i < n; i++)
            {
                src.LoadImage(File.ReadAllBytes(files[i * step]));
                Graphics.Blit(src, rt);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                int cx = i % cols, cy = rows - 1 - i / cols;
                sheet.ReadPixels(new Rect(0, 0, cellW, cellH), cx * cellW, cy * cellH);
                RenderTexture.active = prev;
            }
            sheet.Apply();
            RenderTexture.ReleaseTemporary(rt);
            string outPath = FrameDir + "/sheet_" + clip + ".jpg";
            File.WriteAllBytes(outPath, sheet.EncodeToJPG(85));
            Object.Destroy(sheet);
            Object.Destroy(src);
            return outPath;
        }

        static Color32[] Load(Texture2D tex, string path, int w, int h)
        {
            tex.LoadImage(File.ReadAllBytes(path));
            if (tex.width != w || tex.height != h) throw new System.Exception($"{path} is {tex.width}x{tex.height}, expected {w}x{h}");
            return tex.GetPixels32();
        }
    }

    /// <summary>Plays a choreography script (see Tools/levelgen/trailer_scenes.py): commands fire at hero x positions.</summary>
    public class ScriptedRun : MonoBehaviour
    {
        class Cmd { public float x, a, b; public char type; public bool done; }
        readonly List<Cmd> m_Cmds = new List<Cmd>();
        public List<Vector2> pathOut;
        float m_Hold, m_Wait;
        int m_Release, m_Dash, m_SlowFrames;
        float m_SlowScale = 1f;
        DemoBot m_Bot;

        public static ScriptedRun Attach(GameObject go, string script)
        {
            var r = go.AddComponent<ScriptedRun>();
            foreach (var tok in (script ?? "").Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                if (tok.Length < 3 || tok[1] != '@') continue;
                var parts = tok.Substring(2).Split(':');
                float P(int i) => parts.Length > i && float.TryParse(parts[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0f;
                r.m_Cmds.Add(new Cmd { type = tok[0], x = P(0), a = P(1), b = P(2) });
            }
            return r;
        }

        void Update()
        {
            var p = PlayerController.I;
            if (p == null) return;
            if (LevelSession.Current != null) LevelSession.Current.hearts = 3; // never die on camera
            if (pathOut != null && !p.IsGrounded && p.Controllable) pathOut.Add(new Vector2(p.transform.position.x, p.FeetY + 0.55f)); // fruit only along airborne arcs
            if (m_SlowFrames > 0) { Time.timeScale = m_SlowScale; if (--m_SlowFrames == 0) Time.timeScale = 1f; }
            if (m_Bot != null) return;
            if (!p.Controllable) { GameInput.TouchRight = false; GameInput.TouchJump = false; return; }

            float x = p.transform.position.x;
            foreach (var c in m_Cmds)
            {
                if (c.done || x < c.x) continue;
                switch (c.type)
                {
                    case 'J': if (p.IsGrounded || p.IsWallSliding) { m_Hold = c.a > 0f ? c.a : 0.32f; c.done = true; } break;
                    case 'D': m_Release = 2; m_Hold = c.a > 0f ? c.a : 0.3f; c.done = true; break;
                    case 'X': m_Dash = 2; c.done = true; break;
                    case 'W': m_Wait = c.a; c.done = true; break;
                    case 'S': m_SlowScale = c.a; m_SlowFrames = Mathf.RoundToInt(c.b / Mathf.Max(0.001f, Time.captureDeltaTime > 0f ? Time.captureDeltaTime : 1f / 60f)); c.done = true; break;
                    case 'A': m_Bot = gameObject.AddComponent<DemoBot>(); c.done = true; return;
                }
            }

            GameInput.TouchRight = m_Wait <= 0f;
            m_Wait -= Time.deltaTime;
            if (m_Release > 0) { GameInput.TouchJump = false; m_Release--; }
            else { GameInput.TouchJump = m_Hold > 0f; m_Hold -= Time.deltaTime; }
            GameInput.TouchDash = m_Dash > 0;
            if (m_Dash > 0) m_Dash--;
        }

        void OnDestroy()
        {
            if (m_Bot != null) Destroy(m_Bot);
            Time.timeScale = 1f;
        }
    }

    /// <summary>Very small auto-runner used for trailer footage: runs right, jumps walls, gaps, hazards and enemies.</summary>
    public class DemoBot : MonoBehaviour
    {
        float m_Hold, m_Stuck, m_LastX;
        bool m_Double, m_Release;

        void Update()
        {
            var p = PlayerController.I;
            if (p == null || !p.Controllable) { GameInput.TouchRight = false; GameInput.TouchJump = false; return; }
            if (LevelSession.Current != null) LevelSession.Current.hearts = 3; // never die on camera
            GameInput.TouchRight = true;

            var feet = new Vector2(p.transform.position.x, p.FeetY);
            bool wall = Physics2D.Raycast(feet + new Vector2(0.3f, 0.6f), Vector2.right, 1.3f, Layers.SolidMask);
            bool gap = !Physics2D.Raycast(feet + new Vector2(1.5f, 0.3f), Vector2.down, 3.5f, Layers.GroundMask);
            bool danger = Physics2D.OverlapBox(feet + new Vector2(2f, 0.8f), new Vector2(2.4f, 1.6f), 0f,
                (1 << Layers.Hazard) | (1 << Layers.Enemy) | (1 << Layers.EnemyHit));

            if (m_Release) { GameInput.TouchJump = false; m_Release = false; m_Hold = 0.28f; return; }
            if (p.IsGrounded || p.IsWallSliding)
            {
                m_Double = false;
                if ((wall || gap || danger || p.IsWallSliding) && m_Hold <= 0f) m_Hold = 0.34f;
            }
            else if (!m_Double && p.Velocity.y < -0.5f && !Physics2D.Raycast(feet, Vector2.down, 4f, Layers.GroundMask))
            {
                m_Double = true;
                m_Release = true; // need a fresh press for the air jump
                GameInput.TouchJump = false;
                return;
            }

            // stuck against something: dash + hop
            if (Mathf.Abs(p.transform.position.x - m_LastX) < 0.02f) m_Stuck += Time.deltaTime; else m_Stuck = 0f;
            m_LastX = p.transform.position.x;
            GameInput.TouchDash = m_Stuck > 0.6f;
            if (m_Stuck > 0.6f) { m_Stuck = 0f; m_Hold = 0.34f; }

            GameInput.TouchJump = m_Hold > 0f;
            m_Hold -= Time.deltaTime;
        }
    }
}
#endif
