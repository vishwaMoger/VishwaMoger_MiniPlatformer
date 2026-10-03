#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor.Media;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Editor-only store-asset capture for CrazyGames (driven through DevConsole). Only the game camera is rendered, so
    /// the HUD, popups and cheers (screen-space overlay UI) never appear.
    ///   cover &lt;level&gt; &lt;portraitLevel&gt;            the three covers + the two trailer openers, logo composited on top
    ///   clip &lt;name&gt; &lt;level&gt; &lt;portrait 0|1&gt; &lt;w&gt; &lt;h&gt; &lt;camera&gt; [maxSec]   a scripted solve recorded at a fixed 30 fps
    ///   sheet &lt;name&gt; &lt;step&gt;                     contact sheet of a clip (for picking segments)
    ///   trailer &lt;out.mp4&gt; &lt;w&gt; &lt;h&gt; &lt;cover.png&gt; &lt;clip:start:len&gt;...   static cover, then crossfaded clips, H.264, no audio
    /// Camera specs (zoom is a multiple of the whole-board fit):
    ///   fit | push:z0:z1:sec (centred push-in) | pan:z:sec (left-to-right glide) | follow:z (tracks the hero)
    ///   finale:z (whole board until the route is complete, then pushes in on the hero running home)
    /// </summary>
    public static class DevCapture
    {
        public const string OutDir = "Builds/CrazyGames_Submission";
        public const string FrameDir = "Temp/capture";
        const int Fps = 30;
        public static string LastResult = "";
        /// <summary>Encoder quality for `trailer` (DevConsole `bitrate low|medium|high`); medium keeps uploads small.</summary>
        public static UnityEditor.VideoBitrateMode Bitrate = UnityEditor.VideoBitrateMode.High;

        static Camera Cam => Game.I.Cam;

        static IEnumerator Load(int level, bool portrait, RenderTexture rt)
        {
            Game.ForcePortrait = portrait;
            Cam.targetTexture = rt; // the board fits itself to the render texture's aspect
            TrekHero.SpeedMul = 1f;  // CrazyGames: no fast-forwarding
            Game.I.PlayLevel(level);
            yield return new WaitForSecondsRealtime(0.3f);
            for (float t = 0f; t < 15f && (Game.I.Busy || Game.I.Board == null || Game.I.Board.Level.number != level); t += Time.unscaledDeltaTime)
                yield return null;
            yield return null;
        }

        static void Restore()
        {
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            if (Cam != null) Cam.targetTexture = null;
            Game.ForcePortrait = null;
            TrekHero.SpeedMul = SaveSystem.Data.speed > 0 ? SaveSystem.Data.speed : 1f;
        }

        // whole-board framing for the current render target, in CameraRig zoom units
        static float FitZoom(TrekBoard b, float aspect, float margin = 0.7f)
        {
            float bw = b.Level.w * TrekRoom.Size, bh = b.Level.h * TrekRoom.Size;
            float half = Mathf.Max(bh * 0.5f + margin, (bw * 0.5f + margin) / aspect);
            return K.CameraSize / half;
        }

        static Vector2 BoardCentre(TrekBoard b) => new Vector2(b.Level.w * TrekRoom.Size * 0.5f, b.Level.h * TrekRoom.Size * 0.5f);

        static void Frame(Vector2 centre, float zoom)
        {
            CameraRig.I.SetTarget(null, false);
            CameraRig.I.ZoomPunch(zoom);
            CameraRig.I.SetZoom(zoom);
            CameraRig.I.SnapTo(centre);
        }

        // ---------------------------------------------------------------- scripted solve

        /// <summary>
        /// Slides along the hero-aware hint solver at a readable pace until the route is complete. With hold (big
        /// boards), the hero waits at the start (the countdown keeps running) and sets off once the route has melted
        /// into one hall, so there is no risk of a death on camera.
        /// </summary>
        class Solver
        {
            readonly TrekBoard m_Board;
            readonly float m_Gap;
            bool m_Held;
            float m_Next;
            public Solver(TrekBoard board, float firstAt, float gap, bool hold)
            {
                m_Board = board; m_Next = firstAt; m_Gap = gap; m_Held = hold;
                if (hold) board.HoldStart(true);
            }

            public void Tick(float t)
            {
                if (m_Board == null || m_Board.Finished || t < m_Next) return;
                if (m_Held && m_Board.PathComplete)
                {
                    m_Board.HoldStart(false); // the hall has melted: off they go
                    m_Held = false;
                    return;
                }
                if (m_Board.PathComplete) return;
                // the hero is still at the start while held: the start-based solver is the reliable one there
                int c = m_Held ? m_Board.Level.HintMove(m_Board.CurrentLayout()) : m_Board.HeroHint();
                if (c >= 0 && m_Board.TrySlideFrom(c)) m_Next = t + m_Gap;
                else m_Next = t + 0.1f;
                if (m_Held && m_Board.Level.Connected(m_Board.CurrentLayout())) m_Next = t + 0.7f; // let the melt start first
            }
        }

        // ---------------------------------------------------------------- gameplay clips

        public static IEnumerator Clip(string name, int level, bool portrait, int w, int h, string camera, float maxSec, float gap, bool hold)
        {
            string dir = FrameDir + "/" + name;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            LastResult = name + ": failed";
            for (int take = 0; take < 3; take++)
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
                yield return Load(level, portrait, rt);
                var b = Game.I.Board;
                if (b == null) { LastResult = name + ": no board"; break; }

                float aspect = w / (float)h, fit = FitZoom(b, aspect);
                Vector2 centre = BoardCentre(b);
                var cam = camera.Split(':');
                float P(int i, float def) => cam.Length > i && float.TryParse(cam[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : def;
                Vector2 pos = centre;
                float zoom = fit * (cam[0] == "push" ? P(1, 0.85f) : cam[0] == "pan" || cam[0] == "follow" ? P(1, 1.6f) : 1f);
                float completeAt = -1f;

                // 60 simulated fps like real play (contact checks run per frame), every 2nd frame kept for 30 fps video
                var solver = new Solver(b, 0.7f, gap * (take == 1 ? 0.8f : 1f), hold || take == 2);
                Time.captureFramerate = Fps * 2;
                int frames = Mathf.RoundToInt(maxSec * Fps), kept = 0, tail = -1, completeFrame = -1, winFrame = -1;
                for (int f = 0; f < frames * 2 && kept < frames; f++)
                {
                    float t = f / (Fps * 2f);
                    solver.Tick(t);
                    if (b.PathComplete && completeAt < 0f) { completeAt = t; completeFrame = kept; }
                    var hero = b.Hero != null ? (Vector2)b.Hero.transform.position : centre;
                    float halfH = K.CameraSize / zoom, halfW = halfH * aspect;
                    float bw = b.Level.w * TrekRoom.Size, bh = b.Level.h * TrekRoom.Size;
                    switch (cam[0])
                    {
                        case "push":
                        {
                            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / P(3, 6f)));
                            zoom = fit * Mathf.Lerp(P(1, 0.85f), P(2, 1.25f), k);
                            pos = Vector2.Lerp(centre, Vector2.Lerp(centre, hero, 0.35f), k);
                            break;
                        }
                        case "pan":
                        {
                            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / P(2, 6f)));
                            float x0 = Mathf.Min(centre.x, halfW - 0.6f), x1 = Mathf.Max(centre.x, bw - halfW + 0.6f);
                            pos = new Vector2(Mathf.Lerp(x0, x1, k), Mathf.Lerp(pos.y, Mathf.Clamp(hero.y + 1f, halfH - 0.6f, Mathf.Max(halfH - 0.6f, bh - halfH + 0.6f)), 0.05f));
                            break;
                        }
                        case "follow":
                            pos = Vector2.Lerp(pos, hero + Vector2.up * 1.2f, f == 0 ? 1f : 0.06f);
                            break;
                        case "finale":
                            if (completeAt >= 0f)
                            {
                                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - completeAt - 0.35f) / 1.3f));
                                zoom = fit * Mathf.Lerp(1f, P(1, 1.9f), k);
                                pos = Vector2.Lerp(centre, hero + Vector2.up * 1.1f, k);
                            }
                            break;
                    }
                    Frame(pos, zoom);
                    yield return null;
                    if (f % 2 == 1)
                    {
                        Cam.Render();
                        SaveRt(rt, $"{dir}/f_{kept++:0000}.jpg", false);
                    }
                    if (b.Hero != null && b.Hero.Dead) break;
                    if (b.Hero != null && b.Hero.Won && tail < 0) { winFrame = kept; tail = kept + Mathf.RoundToInt(1.6f * Fps); }
                    if (tail >= 0 && kept >= tail) break;
                }
                Time.captureFramerate = 0;
                bool died = b.Hero != null && b.Hero.Dead, won = b.Hero != null && b.Hero.Won;
                LastResult = $"{name}: level {level} {(portrait ? "portrait" : "landscape")} {b.Level.w}x{b.Level.h} take {take + 1} {(won ? "WON" : died ? "DIED" : "timeout")} {kept} frames, route complete at {(completeAt < 0 ? "-" : (completeAt * Fps).ToString("0"))}";
                File.WriteAllText(dir + "/info.txt", $"frames {kept} complete {completeFrame} won {winFrame}");
                if (!died) break;
            }
            File.AppendAllText(FrameDir + "/results.txt", LastResult + System.Environment.NewLine);
            Restore();
            rt.Release();
            Object.Destroy(rt);
        }

        // ---------------------------------------------------------------- covers

        static string LogoPath => "Assets/_Game/Art/Branding/logo_cover.png";

        /// <summary>The covers: a finished route melted into one glowing hall, the hero running home, the title on top.</summary>
        public static IEnumerator Covers(int level, int portraitLevel)
        {
            Directory.CreateDirectory(OutDir);
            Directory.CreateDirectory(FrameDir);
            yield return CoverSet(level, false, new[]
            {
                (OutDir + "/cover_1920x1080.png", 1920, 1080),
                (OutDir + "/cover_800x800.png", 800, 800),
                (FrameDir + "/cover_video_1920x1080.png", 1920, 1080),
            });
            yield return CoverSet(portraitLevel, true, new[]
            {
                (OutDir + "/cover_800x1200.png", 800, 1200),
                (FrameDir + "/cover_video_1080x1620.png", 1080, 1620),
            });
            Restore();
            LastResult = "covers written to " + OutDir;
        }

        static IEnumerator CoverSet(int level, bool portrait, (string path, int w, int h)[] shots)
        {
            var first = new RenderTexture(shots[0].w, shots[0].h, 24);
            yield return Load(level, portrait, first);
            var b = Game.I.Board;
            if (b == null) yield break;
            b.Freeze(60f);
            // solve during the countdown, then let the hero run the finished hall
            for (int i = 0; i < 60 && !b.PathComplete; i++)
            {
                int c = b.HeroHint();
                if (c < 0 || !b.TrySlideFrom(c)) { yield return new WaitForSeconds(0.1f); continue; }
                yield return new WaitForSeconds(0.32f);
            }
            while (!b.Started) yield return null;
            yield return new WaitForSeconds(1.2f); // walls melt, other rooms dim
            b.Freeze(0.01f);
            yield return new WaitForSeconds(0.45f); // first strides, still in the start room so the whole gold route shows
            Time.timeScale = 0f;
            var logo = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            logo.LoadImage(File.ReadAllBytes(LogoPath));
            foreach (var (path, w, h) in shots)
            {
                var rt = new RenderTexture(w, h, 24);
                Cam.targetTexture = rt;
                yield return null; // the board refits itself to a new aspect first; frame the shot after that
                float aspect = w / (float)h;
                float bw = b.Level.w * TrekRoom.Size, bh = b.Level.h * TrekRoom.Size;
                // board in the lower part of the picture, the title above it
                float boardShare = aspect > 1.2f ? 0.6f : aspect > 0.9f ? 0.58f : 0.6f, boardY = aspect > 1.2f ? 0.36f : 0.38f;
                float half = Mathf.Max(bh / (2f * boardShare), bw / (2f * 0.94f * aspect));
                var c = BoardCentre(b);
                Frame(new Vector2(c.x, c.y + half * (1f - 2f * boardY)), K.CameraSize / half);
                yield return null;
                yield return null; // parallax / weather refit to the new framing
                Cam.Render();
                var tex = Grab(rt);
                float logoW = w * (aspect > 1.2f ? 0.56f : 0.86f);
                float logoTop = h * (aspect > 1.2f ? 0.05f : aspect > 0.9f ? 0.07f : 0.09f);
                Composite(tex, logo, logoW, logoTop);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.Destroy(tex);
                Cam.targetTexture = null;
                rt.Release();
                Object.Destroy(rt);
            }
            Object.Destroy(logo);
            Time.timeScale = 1f;
            first.Release();
            Object.Destroy(first);
        }

        static Texture2D Grab(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }

        // alpha-blends the logo (bilinear scaled) centred horizontally, its top edge logoTop pixels below the image top
        static void Composite(Texture2D dst, Texture2D logo, float width, float logoTop)
        {
            int lw = Mathf.RoundToInt(width), lh = Mathf.RoundToInt(width * logo.height / logo.width);
            int x0 = (dst.width - lw) / 2, y0 = dst.height - Mathf.RoundToInt(logoTop) - lh;
            var px = dst.GetPixels32();
            for (int y = 0; y < lh; y++)
            {
                int dy = y0 + y;
                if (dy < 0 || dy >= dst.height) continue;
                for (int x = 0; x < lw; x++)
                {
                    int dx = x0 + x;
                    if (dx < 0 || dx >= dst.width) continue;
                    var s = logo.GetPixelBilinear((x + 0.5f) / lw, (y + 0.5f) / lh);
                    if (s.a <= 0f) continue;
                    int i = dy * dst.width + dx;
                    px[i] = Color32.Lerp(px[i], (Color32)new Color(s.r, s.g, s.b, 1f), s.a);
                }
            }
            dst.SetPixels32(px);
            dst.Apply();
        }

        static void SaveRt(RenderTexture rt, string path, bool png)
        {
            var tex = Grab(rt);
            File.WriteAllBytes(path, png ? tex.EncodeToPNG() : tex.EncodeToJPG(95));
            Object.Destroy(tex);
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
                bitRateMode = Bitrate,
            };
            const int coverHold = 30, fade = 9;
            var frame = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var load = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Color32[] prevTail;
            int total = 0;
            using (var enc = new MediaEncoder(outPath, attr))
            {
                // CrazyGames: the static cover is the opening frame
                var cover = LoadPx(load, coverPath, w, h);
                for (int i = 0; i < coverHold; i++) { frame.SetPixels32(cover); frame.Apply(false); enc.AddFrame(frame); total++; }
                prevTail = cover;

                foreach (var c in clips)
                {
                    var parts = c.Split(':');
                    string dir = FrameDir + "/" + parts[0];
                    int start = int.Parse(parts[1]), count = int.Parse(parts[2]);
                    for (int i = 0; i < count; i++)
                    {
                        var px = LoadPx(load, $"{dir}/f_{start + i:0000}.jpg", w, h);
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

        /// <summary>Grid of every n-th frame of a clip, numbered, for picking segments. Returns the sheet path.</summary>
        public static string ContactSheet(string clip, int step, int cols = 5, int cellW = 384)
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
            return outPath + $" ({files.Length} frames, cell i = frame i*{step})";
        }

        static Color32[] LoadPx(Texture2D tex, string path, int w, int h)
        {
            tex.LoadImage(File.ReadAllBytes(path));
            if (tex.width == w && tex.height == h) return tex.GetPixels32();
            // a cover in another size (the 800x1200 portrait cover opens the 1080x1620 video): scale it to fit
            if (Mathf.Abs(tex.width / (float)tex.height - w / (float)h) > 0.01f) throw new System.Exception($"{path} is {tex.width}x{tex.height}, expected the shape of {w}x{h}");
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(tex, rt);
            var scaled = Grab(rt);
            RenderTexture.ReleaseTemporary(rt);
            var px = scaled.GetPixels32();
            Object.Destroy(scaled);
            return px;
        }
    }
}
#endif
