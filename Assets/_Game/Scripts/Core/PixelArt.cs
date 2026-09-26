using System.Collections.Generic;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Procedurally drawn pixel art (UI icons, signs, lanterns, glow textures). Everything is generated once
    /// at startup, so the game ships no extra image files for these.
    /// </summary>
    public static class PixelArt
    {
        static readonly Dictionary<string, Sprite> s_Cache = new Dictionary<string, Sprite>();

        static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
        {
            { 'K', new Color32(32, 23, 42, 255) },    // outline
            { 'W', new Color32(255, 255, 255, 255) },
            { 'R', new Color32(228, 59, 68, 255) },
            { 'r', new Color32(162, 38, 51, 255) },
            { 'P', new Color32(255, 170, 170, 255) },
            { 'Y', new Color32(254, 231, 97, 255) },
            { 'y', new Color32(247, 164, 42, 255) },
            { 'g', new Color32(58, 68, 102, 255) },
            { 'G', new Color32(98, 110, 142, 255) },
            { 'B', new Color32(184, 111, 80, 255) },
            { 'b', new Color32(115, 62, 57, 255) },
            { 'L', new Color32(234, 212, 170, 255) },
            { 'O', new Color32(254, 174, 52, 255) },
        };

        class Canvas
        {
            public readonly int w, h;
            public readonly Color32[] px;
            public Canvas(int w, int h) { this.w = w; this.h = h; px = new Color32[w * h]; }
            // (x, y) with y measured from the TOP, like the ascii art
            public void Set(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < w && y < h) px[(h - 1 - y) * w + x] = c; }
            public Color32 Get(int x, int y) => (x >= 0 && y >= 0 && x < w && y < h) ? px[(h - 1 - y) * w + x] : new Color32(0, 0, 0, 0);

            public void Poly(Vector2[] pts, Color32 c)
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        if (Inside(new Vector2(x + 0.5f, y + 0.5f), pts)) Set(x, y, c);
            }

            public void Disc(float cx, float cy, float r, Color32 c, float rInner = -1f, float gapFrom = 999f, float gapTo = 999f)
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d > r || d < rInner) continue;
                        float ang = Mathf.Atan2(-dy, dx) * Mathf.Rad2Deg;
                        if (ang < 0) ang += 360f;
                        if (ang >= gapFrom && ang <= gapTo) continue;
                        Set(x, y, c);
                    }
            }

            public void Rect(int x0, int y0, int rw, int rh, Color32 c)
            {
                for (int y = y0; y < y0 + rh; y++) for (int x = x0; x < x0 + rw; x++) Set(x, y, c);
            }

            static bool Inside(Vector2 p, Vector2[] poly)
            {
                bool inside = false;
                for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                {
                    if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                        (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                        inside = !inside;
                }
                return inside;
            }

            /// <summary>Adds a 1px soft drop shadow under every opaque pixel.</summary>
            public Canvas WithShadow()
            {
                var c = new Canvas(w + 1, h + 1);
                var sh = new Color32(20, 12, 30, 110);
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (Get(x, y).a > 0) c.Set(x + 1, y + 1, sh);
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { var p = Get(x, y); if (p.a > 0) c.Set(x, y, p); }
                return c;
            }

            public Sprite ToSprite(string name, float ppu = 16f, FilterMode filter = FilterMode.Point, Vector2? pivot = null)
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = filter, wrapMode = TextureWrapMode.Clamp, name = name };
                tex.SetPixels32(px);
                tex.Apply(false, true);
                var s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot ?? new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, Vector4.zero, false);
                s.name = name;
                return s;
            }
        }

        static Canvas Ascii(string[] rows, Dictionary<char, Color32> remap = null)
        {
            int h = rows.Length, w = 0;
            foreach (var r in rows) w = Mathf.Max(w, r.Length);
            var c = new Canvas(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    char ch = rows[y][x];
                    if (remap != null && remap.TryGetValue(ch, out var rc)) { c.Set(x, y, rc); continue; }
                    if (Palette.TryGetValue(ch, out var col)) c.Set(x, y, col);
                }
            return c;
        }

        static Sprite Cached(string key, System.Func<Sprite> make)
        {
            if (!s_Cache.TryGetValue(key, out var s) || s == null) { s = make(); s_Cache[key] = s; }
            return s;
        }

        static readonly string[] HeartRows =
        {
            "..KKK...KKK..",
            ".KRRRK.KRRRK.",
            "KRWWRRKRRRRrK",
            "KRWRRRRRRRRrK",
            "KRRRRRRRRRRrK",
            "KRRRRRRRRRrrK",
            ".KRRRRRRRRrK.",
            "..KRRRRRRrK..",
            "...KRRRRrK...",
            "....KRRrK....",
            ".....KrK.....",
            "......K......",
        };

        static readonly string[] StarRows =
        {
            ".......K.......",
            "......KYK......",
            "......KYK......",
            ".....KYYYK.....",
            ".....KYWYK.....",
            "KKKKKYYWYYKKKKK",
            "KYYYYYYYYYYYYyK",
            ".KyYYYYYYYYYyK.",
            "..KyYYYYYYYyK..",
            "...KYYYYYYYK...",
            "...KYYYYYYYK...",
            "..KYYYyKyYYYK..",
            "..KYYyK.KyYYK..",
            ".KYyK.....KyYK.",
            ".KKK.......KKK.",
        };

        static readonly Dictionary<char, Color32> EmptyRemap = new Dictionary<char, Color32>
        {
            { 'R', new Color32(58, 68, 102, 255) }, { 'r', new Color32(40, 46, 72, 255) }, { 'W', new Color32(98, 110, 142, 255) },
            { 'Y', new Color32(58, 68, 102, 255) }, { 'y', new Color32(40, 46, 72, 255) },
        };

        public static Sprite Heart => Cached("heart", () => Ascii(HeartRows).ToSprite("heart"));
        public static Sprite HeartEmpty => Cached("heart_e", () => Ascii(HeartRows, EmptyRemap).ToSprite("heart_e"));
        public static Sprite Star => Cached("star", () => Ascii(StarRows).ToSprite("star"));
        public static Sprite StarEmpty => Cached("star_e", () => Ascii(StarRows, EmptyRemap).ToSprite("star_e"));

        public static Sprite Lock => Cached("lock", () => Ascii(new[]
        {
            "...KKKKKK...",
            "..KGGGGGGK..",
            ".KGKKKKKKGK.",
            ".KGK....KGK.",
            ".KGK....KGK.",
            "KKKKKKKKKKKK",
            "KYWYYYYYYYyK",
            "KYYYYYYYYYyK",
            "KYYYYKKYYYyK",
            "KYYYYKKYYYyK",
            "KYYYYYKYYYyK",
            "KYYYYYYYYYyK",
            "KyyyyyyyyyyK",
            "KKKKKKKKKKKK",
        }).ToSprite("lock"));

        public static Sprite Pause => Cached("pause", () => Ascii(Repeat("WWW..WWW", 10)).WithShadow().ToSprite("pause"));

        public static Sprite Play => Cached("play", () => Ascii(new[]
        {
            "WW.......", "WWWW.....", "WWWWWW...", "WWWWWWWW.", "WWWWWWWWW",
            "WWWWWWWWW", "WWWWWWWW.", "WWWWWW...", "WWWW.....", "WW.......",
        }).WithShadow().ToSprite("play"));

        public static Sprite Home => Cached("home", () => Ascii(new[]
        {
            ".....WW.....", "....WWWW....", "...WWWWWW...", "..WWWWWWWW..", ".WWWWWWWWWW.", "WWWWWWWWWWWW",
            ".WWWWWWWWWW.", ".WWWW..WWWW.", ".WWWW..WWWW.", ".WWWW..WWWW.", ".WWWW..WWWW.",
        }).WithShadow().ToSprite("home"));

        public static Sprite Levels => Cached("levels", () =>
        {
            var rows = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++) rows.Add("WWW.WWW.WWW");
                if (i < 2) rows.Add("...........");
            }
            return Ascii(rows.ToArray()).WithShadow().ToSprite("levels");
        });

        public static Sprite Check => Cached("check", () => Ascii(new[]
        {
            ".........WW", "........WWW", ".......WWW.", "WW....WWW..", "WWW..WWW...", ".WWWWWW....", "..WWWW.....", "...WW......",
        }).WithShadow().ToSprite("check"));

        public static Sprite Restart => Cached("restart", () =>
        {
            var c = new Canvas(13, 13);
            var w = new Color32(255, 255, 255, 255);
            Vector2 P(float deg, float r) => new Vector2(6.5f + r * Mathf.Cos(deg * Mathf.Deg2Rad), 6.5f - r * Mathf.Sin(deg * Mathf.Deg2Rad));
            c.Disc(6.5f, 6.5f, 6.1f, w, 3.5f, 20f, 112f);
            c.Poly(new[] { P(116f, 1.6f), P(116f, 8.0f), P(62f, 4.8f) }, w); // arrow head pointing clockwise
            return c.WithShadow().ToSprite("restart");
        });

        public static Sprite Gear => Cached("gear", () =>
        {
            var c = new Canvas(13, 13);
            var w = new Color32(255, 255, 255, 255);
            c.Disc(6.5f, 6.5f, 4.6f, w);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var perp = new Vector2(-dir.y, dir.x);
                var ctr = new Vector2(6.5f, 6.5f);
                c.Poly(new[] { ctr + dir * 3.5f - perp * 1.2f, ctr + dir * 6.6f - perp * 1.2f, ctr + dir * 6.6f + perp * 1.2f, ctr + dir * 3.5f + perp * 1.2f }, w);
            }
            c.Disc(6.5f, 6.5f, 2.0f, new Color32(0, 0, 0, 0));
            // punch the hole (Disc with transparent writes alpha 0)
            return c.WithShadow().ToSprite("gear");
        });

        public static Sprite Close => Cached("close", () =>
        {
            var c = new Canvas(11, 11);
            var w = new Color32(255, 255, 255, 255);
            c.Poly(new[] { new Vector2(0, 1.5f), new Vector2(1.5f, 0), new Vector2(11, 9.5f), new Vector2(9.5f, 11) }, w);
            c.Poly(new[] { new Vector2(9.5f, 0), new Vector2(11, 1.5f), new Vector2(1.5f, 11), new Vector2(0, 9.5f) }, w);
            return c.WithShadow().ToSprite("close");
        });

        public static Sprite Arrow(int dir) => Cached("arrow" + dir, () =>
        {
            // dir: 0 right, 1 up, 2 left, 3 down
            var c = new Canvas(12, 12);
            var w = new Color32(255, 255, 255, 255);
            Vector2[] tri = { new Vector2(2, 0.5f), new Vector2(11.5f, 6), new Vector2(2, 11.5f) };
            for (int i = 0; i < tri.Length; i++)
            {
                var p = tri[i] - new Vector2(6, 6);
                for (int r = 0; r < dir; r++) p = new Vector2(p.y, -p.x); // rotate 90 (screen y is flipped)
                tri[i] = p + new Vector2(6, 6);
            }
            c.Poly(tri, w);
            return c.WithShadow().ToSprite("arrow" + dir);
        });

        public static Sprite Dash => Cached("dash", () =>
        {
            var c = new Canvas(14, 12);
            var w = new Color32(255, 255, 255, 255);
            c.Poly(new[] { new Vector2(0, 0.5f), new Vector2(3, 0.5f), new Vector2(8, 6), new Vector2(3, 11.5f), new Vector2(0, 11.5f), new Vector2(5, 6) }, w);
            c.Poly(new[] { new Vector2(6, 0.5f), new Vector2(9, 0.5f), new Vector2(14, 6), new Vector2(9, 11.5f), new Vector2(6, 11.5f), new Vector2(11, 6) }, w);
            return c.WithShadow().ToSprite("dash");
        });

        public static Sprite Clock => Cached("clock", () =>
        {
            var c = new Canvas(12, 12);
            var w = new Color32(255, 255, 255, 255);
            c.Disc(6, 6, 6, w, 4.4f);
            c.Rect(5, 2, 2, 5, w);
            c.Rect(5, 5, 4, 2, w);
            return c.WithShadow().ToSprite("clock");
        });

        public static Sprite Sign => Cached("sign", () => Ascii(new[]
        {
            "................",
            ".KKKKKKKKKKKKKK.",
            "KLLLLLLLLLLLLLLK",
            "KLBBBBBBBBBBBBLK",
            "KLBbbbBBbbbbBBLK",
            "KLBBBBBBBBBBBBLK",
            "KLBbbBbbbbBbbBLK",
            "KLBBBBBBBBBBBBLK",
            "KbbbbbbbbbbbbbbK",
            ".KKKKKKbbKKKKKK.",
            "......KBbK......",
            "......KBbK......",
            "......KBbK......",
            "......KBbK......",
            ".....KKBbKK.....",
            "....KKKKKKKK....",
        }).ToSprite("sign"));

        public static Sprite Lantern => Cached("lantern", () => Ascii(new[]
        {
            "....KK....",
            "...K..K...",
            "..KKKKKK..",
            "..KbbbbK..",
            ".KYWYYYyK.",
            ".KYYYYYyK.",
            ".KYYYYYyK.",
            ".KYYYYYyK.",
            "..KbbbbK..",
            "..KKKKKK..",
            "....KK....",
        }).ToSprite("lantern"));

        /// <summary>Soft radial glow used for halos and particles (bilinear filtered).</summary>
        public static Sprite Glow => Cached("glow", () =>
        {
            const int n = 64;
            var c = new Canvas(n, n);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    c.Set(x, y, new Color32(255, 255, 255, (byte)(d * d * 255)));
                }
            return c.ToSprite("glow", n / 2f, FilterMode.Bilinear);
        });

        /// <summary>Solid white pixel block (for pixel particles and UI fills).</summary>
        public static Sprite Pixel => Cached("pixel", () =>
        {
            var c = new Canvas(4, 4);
            c.Rect(0, 0, 4, 4, new Color32(255, 255, 255, 255));
            return c.ToSprite("pixel", 16f);
        });

        public static Sprite Circle => Cached("circle", () =>
        {
            var c = new Canvas(8, 8);
            c.Disc(4, 4, 4, new Color32(255, 255, 255, 255));
            return c.ToSprite("circle", 16f);
        });

        static string[] Repeat(string row, int n)
        {
            var a = new string[n];
            for (int i = 0; i < n; i++) a[i] = row;
            return a;
        }
    }
}
