using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Smooth, anti-aliased UI art generated at startup with signed distance fields:
    /// glossy rounded buttons, soft panels, HUD chips and vector icons. Nothing is stored on disk.
    /// </summary>
    public static class UIArt
    {
        static readonly Dictionary<string, Sprite> s_Cache = new Dictionary<string, Sprite>();

        // ------------------------------------------------------------------ SDF helpers (pixel units, y up)

        public static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

        public static float RoundBox(Vector2 p, Vector2 c, Vector2 half, float r)
        {
            var q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(r, r);
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        public static float Segment(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        public static float Polygon(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i], w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s *= -1f;
            }
            return s * Mathf.Sqrt(d);
        }

        public static Vector2[] StarPoints(Vector2 c, float rOuter, float rInner, int n = 5, float rotDeg = 90f)
        {
            var pts = new Vector2[n * 2];
            for (int i = 0; i < n * 2; i++)
            {
                float a = (rotDeg + i * 180f / n) * Mathf.Deg2Rad;
                float r = i % 2 == 0 ? rOuter : rInner;
                pts[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return pts;
        }

        /// <summary>Heart made of two circles and a rounded triangle.</summary>
        public static float HeartSdf(Vector2 p, Vector2 c, float s)
        {
            float a = Circle(p, c + new Vector2(-0.26f * s, 0.12f * s), 0.3f * s);
            float b = Circle(p, c + new Vector2(0.26f * s, 0.12f * s), 0.3f * s);
            float t = Polygon(p, new[] { c + new Vector2(-0.53f * s, 0.05f * s), c + new Vector2(0f, -0.58f * s), c + new Vector2(0.53f * s, 0.05f * s), c + new Vector2(0f, 0.2f * s) }) - 0.02f * s;
            return Mathf.Min(Mathf.Min(a, b), t);
        }

        // ------------------------------------------------------------------ rasteriser

        public class Layer
        {
            public Func<Vector2, float> sdf;
            public Color top, bottom;      // vertical gradient over the layer's pixel range
            public float yTop, yBottom;
            public float feather = 1f;     // anti-alias width (px); larger = soft shadow
            public Vector2 offset;
            public Layer(Func<Vector2, float> f, Color c) { sdf = f; top = bottom = c; yTop = 1e6f; yBottom = -1e6f; }
            public Layer Grad(Color t, Color b, float y0, float y1) { top = t; bottom = b; yTop = y0; yBottom = y1; return this; }
            public Layer Soft(float f) { feather = f; return this; }
            public Layer Offset(float x, float y) { offset = new Vector2(x, y); return this; }
        }

        public static Texture2D Render(int w, int h, params Layer[] layers)
        {
            var px = new Color[w * h];
            const int ss = 2; // supersample for smoother edges
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color acc = new Color(0, 0, 0, 0);
                    foreach (var L in layers)
                    {
                        float cov = 0f;
                        for (int sy = 0; sy < ss; sy++)
                            for (int sx = 0; sx < ss; sx++)
                            {
                                var p = new Vector2(x + (sx + 0.5f) / ss, y + (sy + 0.5f) / ss) - L.offset;
                                float d = L.sdf(p);
                                cov += Mathf.Clamp01(0.5f - d / L.feather);
                            }
                        cov /= ss * ss;
                        if (cov <= 0f) continue;
                        float k = L.yTop > 1e5f ? 0f : Mathf.InverseLerp(L.yTop, L.yBottom, y + 0.5f);
                        Color c = Color.Lerp(L.top, L.bottom, k);
                        float a = c.a * cov;
                        // "over" compositing
                        float outA = a + acc.a * (1f - a);
                        if (outA > 0f)
                        {
                            acc.r = (c.r * a + acc.r * acc.a * (1f - a)) / outA;
                            acc.g = (c.g * a + acc.g * acc.a * (1f - a)) / outA;
                            acc.b = (c.b * a + acc.b * acc.a * (1f - a)) / outA;
                        }
                        acc.a = outA;
                    }
                    px[y * w + x] = acc;
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 1 };
            tex.SetPixels(px);
            tex.Apply(true, true);
            return tex;
        }

        static Sprite Cached(string key, Func<Sprite> make)
        {
            if (!s_Cache.TryGetValue(key, out var s) || s == null) { s = make(); s.name = key; s_Cache[key] = s; }
            return s;
        }

        static Sprite Sliced(Texture2D t, float border) =>
            Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border), false);

        // no fallback physics shape: it needs readable pixels, and these textures are GPU-only
        static Sprite Plain(Texture2D t) => Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero, false);

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        // ------------------------------------------------------------------ skins (9-sliced)

        struct Palette { public Color top, bottom, lip, edge; public Palette(string t, string b, string l, string e) { top = Hex(t); bottom = Hex(b); lip = Hex(l); edge = Hex(e); } }

        static readonly Dictionary<string, Palette> Buttons = new Dictionary<string, Palette>
        {
            { "green", new Palette("#9BEA5E", "#4DB33A", "#2F7E2A", "#256B22") },
            { "blue", new Palette("#7FD0FF", "#3D8FEA", "#2A62B8", "#20529E") },
            { "yellow", new Palette("#FFE680", "#F7B52C", "#C57E14", "#A86A10") },
            { "red", new Palette("#FF9A8A", "#EC4F5B", "#B32E45", "#99243A") },
            { "purple", new Palette("#C9A2FF", "#8A5CEB", "#5E38B5", "#4E2D9A") },
            { "grey", new Palette("#E3E8F0", "#A5AEC0", "#6F7890", "#5E667C") },
        };

        /// <summary>
        /// Returns a 9-sliced UI skin. Buttons: green, blue, yellow, red, purple, grey (+ "_pressed").
        /// Surfaces: panel (cream popup), card, inset, glass (dark HUD chip), track, dim_card.
        /// </summary>
        public static Sprite Skin(string key) => Cached("skin_" + key, () => MakeSkin(key));

        static Sprite MakeSkin(string key)
        {
            const int S = 96, R = 26;
            var c = new Vector2(S / 2f, S / 2f);
            bool pressed = key.EndsWith("_pressed");
            string baseKey = pressed ? key.Substring(0, key.Length - 8) : key;
            if (Buttons.TryGetValue(baseKey, out var pal))
            {
                float lip = pressed ? 2f : 7f;
                var half = new Vector2(S / 2f - 4f, S / 2f - 4f - 3.5f);
                var bodyC = c + new Vector2(0f, 3.5f - (pressed ? 5f : 0f) + (pressed ? 0f : 0f));
                var lipC = bodyC - new Vector2(0f, lip);
                var tex = Render(S, S,
                    new Layer(p => RoundBox(p, lipC - new Vector2(0, 1.5f), half, R), new Color(0, 0, 0, 0.28f)).Soft(4f),
                    new Layer(p => RoundBox(p, lipC, half, R), pal.edge),
                    new Layer(p => RoundBox(p, lipC, half - new Vector2(2.5f, 2.5f), R - 2.5f), pal.lip),
                    new Layer(p => RoundBox(p, bodyC, half, R), pal.edge),
                    new Layer(p => RoundBox(p, bodyC, half - new Vector2(3f, 3f), R - 3f), pal.top).Grad(pal.top, pal.bottom, bodyC.y + half.y, bodyC.y - half.y),
                    // soft gloss on the upper half
                    new Layer(p => RoundBox(p, bodyC + new Vector2(0f, half.y * 0.45f), new Vector2(half.x - 9f, half.y * 0.38f), R * 0.6f), new Color(1, 1, 1, 0.28f)).Soft(3f));
                return Sliced(tex, 34f);
            }

            switch (baseKey)
            {
                case "panel":
                {
                    var half = new Vector2(S / 2f - 7f, S / 2f - 7f);
                    var tex = Render(S, S,
                        new Layer(p => RoundBox(p, c - new Vector2(0, 3), half, R), new Color(0.1f, 0.05f, 0.15f, 0.35f)).Soft(6f),
                        new Layer(p => RoundBox(p, c, half, R), Hex("#E2B77D")),
                        new Layer(p => RoundBox(p, c, half - new Vector2(4, 4), R - 4), Hex("#FFFBF1")).Grad(Hex("#FFFDF7"), Hex("#FBEBD0"), c.y + half.y, c.y - half.y));
                    return Sliced(tex, 36f);
                }
                case "card":
                {
                    var half = new Vector2(S / 2f - 5f, S / 2f - 5f);
                    var tex = Render(S, S,
                        new Layer(p => RoundBox(p, c - new Vector2(0, 4), half, R), new Color(0.15f, 0.08f, 0.2f, 0.25f)).Soft(4f),
                        new Layer(p => RoundBox(p, c, half, R), Hex("#FFFFFF")).Grad(Hex("#FFFFFF"), Hex("#F2F4FA"), c.y + half.y, c.y - half.y),
                        new Layer(p => -(RoundBox(p, c, half - new Vector2(1, 1), R - 1)) - 0f, new Color(0, 0, 0, 0f)));
                    return Sliced(tex, 34f);
                }
                case "dim_card":
                {
                    var half = new Vector2(S / 2f - 5f, S / 2f - 5f);
                    var tex = Render(S, S,
                        new Layer(p => RoundBox(p, c - new Vector2(0, 4), half, R), new Color(0.1f, 0.05f, 0.15f, 0.25f)).Soft(4f),
                        new Layer(p => RoundBox(p, c, half, R), Hex("#C9CEDC")).Grad(Hex("#D5DAE6"), Hex("#AEB5C7"), c.y + half.y, c.y - half.y));
                    return Sliced(tex, 34f);
                }
                case "inset":
                {
                    var half = new Vector2(S / 2f - 3f, S / 2f - 3f);
                    var tex = Render(S, S,
                        new Layer(p => RoundBox(p, c, half, R - 6), new Color(0.35f, 0.2f, 0.1f, 0.14f)),
                        new Layer(p => RoundBox(p, c - new Vector2(0, 2), half - new Vector2(1, 2), R - 7), new Color(1f, 0.98f, 0.93f, 0.5f)).Soft(3f));
                    return Sliced(tex, 30f);
                }
                case "glass":
                {
                    var half = new Vector2(S / 2f - 3f, S / 2f - 3f);
                    var tex = Render(S, S,
                        new Layer(p => RoundBox(p, c, half, R), new Color(0.05f, 0.03f, 0.12f, 0.55f)),
                        new Layer(p => Mathf.Abs(RoundBox(p, c, half - new Vector2(1.5f, 1.5f), R - 1.5f)) - 1.2f, new Color(1f, 1f, 1f, 0.18f)));
                    return Sliced(tex, 32f);
                }
                case "track":
                {
                    var half = new Vector2(S / 2f - 3f, S / 2f - 3f);
                    var tex = Render(S, S,
                        new Layer(p => RoundBox(p, c, half, R), Hex("#5B4660")),
                        new Layer(p => RoundBox(p, c - new Vector2(0, 2), half - new Vector2(3, 3), R - 3), Hex("#3E2E44")));
                    return Sliced(tex, 32f);
                }
                case "fill":
                {
                    var half = new Vector2(S / 2f - 3f, S / 2f - 3f);
                    var tex = Render(S, S, new Layer(p => RoundBox(p, c, half, R), Color.white).Grad(Color.white, new Color(0.82f, 0.82f, 0.82f), c.y + half.y, c.y - half.y));
                    return Sliced(tex, 32f);
                }
                case "circle":
                {
                    var tex = Render(S, S,
                        new Layer(p => Circle(p, c - new Vector2(0, 3), S / 2f - 6f), new Color(0, 0, 0, 0.3f)).Soft(4f),
                        new Layer(p => Circle(p, c, S / 2f - 6f), Hex("#FFFFFF")).Grad(Hex("#FFFFFF"), Hex("#E6E9F2"), S, 0));
                    return Plain(tex);
                }
            }
            Debug.LogWarning("Unknown UI skin " + key);
            return Skin("panel");
        }

        // ------------------------------------------------------------------ icons

        const int I = 128;
        static readonly Vector2 C = new Vector2(I / 2f, I / 2f);
        static readonly Color Shadow = new Color(0.1f, 0.05f, 0.15f, 0.35f);

        /// <summary>White glyph with a soft drop shadow, for use on colored buttons.</summary>
        static Sprite Glyph(string key, Func<Vector2, float> f) => Cached(key, () => Plain(Render(I, I,
            new Layer(f, Shadow).Offset(0, -5).Soft(3f),
            new Layer(f, Color.white).Grad(Color.white, Hex("#EEF2FF"), I, 0))));

        public static Sprite Play => Glyph("i_play", p => Polygon(p, new[] { new Vector2(40, 22), new Vector2(40, 106), new Vector2(104, 64) }) - 8f);
        /// <summary>Video/ad badge: rounded screen with a play cut-out (marks rewarded-ad buttons).</summary>
        public static Sprite Video => Glyph("i_video", p => Mathf.Max(RoundBox(p, new Vector2(64, 64), new Vector2(48, 36), 12),
            -(Polygon(p, new[] { new Vector2(52, 44), new Vector2(52, 84), new Vector2(86, 64) }) - 2f)));
        public static Sprite Pause => Glyph("i_pause", p => Mathf.Min(RoundBox(p, new Vector2(46, 64), new Vector2(12, 38), 7), RoundBox(p, new Vector2(82, 64), new Vector2(12, 38), 7)));
        public static Sprite Home => Glyph("i_home", p =>
        {
            float roof = Polygon(p, new[] { new Vector2(64, 108), new Vector2(18, 66), new Vector2(110, 66) }) - 5f;
            float body = RoundBox(p, new Vector2(64, 46), new Vector2(34, 28), 6);
            float door = RoundBox(p, new Vector2(64, 34), new Vector2(10, 16), 4);
            return Mathf.Max(Mathf.Min(roof, body), -door);
        });
        public static Sprite Levels => Glyph("i_levels", p =>
        {
            float d = 1e6f;
            for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) d = Mathf.Min(d, RoundBox(p, new Vector2(34 + i * 30, 34 + j * 30), new Vector2(11, 11), 4));
            return d;
        });
        public static Sprite Restart => Glyph("i_restart", p =>
        {
            float ring = Mathf.Abs(Circle(p, C, 36f)) - 9f;
            float ang = Mathf.Atan2(p.y - C.y, p.x - C.x) * Mathf.Rad2Deg;
            if (ang > 25f && ang < 100f) ring = Mathf.Max(ring, 4f);
            Vector2 A(float deg, float r) => C + new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad)) * r;
            float head = Polygon(p, new[] { A(100f, 16f), A(100f, 58f), A(52f, 37f) }) - 3f;
            return Mathf.Min(ring, head);
        });
        public static Sprite Gear => Glyph("i_gear", p =>
        {
            float d = Circle(p, C, 34f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                d = Mathf.Min(d, Segment(p, C + dir * 30f, C + dir * 48f, 9f));
            }
            return Mathf.Max(d, -Circle(p, C, 14f));
        });
        public static Sprite Close => Glyph("i_close", p => Mathf.Min(Segment(p, new Vector2(34, 34), new Vector2(94, 94), 11f), Segment(p, new Vector2(34, 94), new Vector2(94, 34), 11f)));
        public static Sprite Check => Glyph("i_check", p => Mathf.Min(Segment(p, new Vector2(28, 66), new Vector2(54, 40), 11f), Segment(p, new Vector2(54, 40), new Vector2(100, 90), 11f)));
        public static Sprite Arrow(int dir) => Glyph("i_arrow" + dir, p =>
        {
            // dir: 0 right, 1 up, 2 left, 3 down (rotate the sample point)
            Vector2 q = p - C;
            for (int i = 0; i < dir; i++) q = new Vector2(q.y, -q.x);
            q += C;
            return Polygon(q, new[] { new Vector2(40, 22), new Vector2(40, 106), new Vector2(102, 64) }) - 9f;
        });
        public static Sprite Dash => Glyph("i_dash", p => Mathf.Min(
            Mathf.Min(Segment(p, new Vector2(28, 98), new Vector2(60, 64), 10f), Segment(p, new Vector2(60, 64), new Vector2(28, 30), 10f)),
            Mathf.Min(Segment(p, new Vector2(64, 98), new Vector2(96, 64), 10f), Segment(p, new Vector2(96, 64), new Vector2(64, 30), 10f))));
        public static Sprite Clock => Glyph("i_clock", p =>
        {
            float ring = Mathf.Abs(Circle(p, C, 42f)) - 8f;
            float hands = Mathf.Min(Segment(p, C, C + new Vector2(0, 26), 6f), Segment(p, C, C + new Vector2(20, 0), 6f));
            return Mathf.Min(ring, hands);
        });
        public static Sprite Music => Glyph("i_music", p => Mathf.Min(Mathf.Min(Circle(p, new Vector2(46, 36), 16), Circle(p, new Vector2(90, 46), 16)),
            Mathf.Min(Mathf.Min(Segment(p, new Vector2(58, 38), new Vector2(58, 100), 6), Segment(p, new Vector2(102, 48), new Vector2(102, 108), 6)), Segment(p, new Vector2(58, 100), new Vector2(102, 108), 8))));

        /// <summary>Colored icons (hearts, stars, lock, fruit badge) with outline + gloss.</summary>
        static Sprite Badge(string key, Func<Vector2, float> f, Color top, Color bottom, Color outline, float outlineW = 7f) => Cached(key, () => Plain(Render(I, I,
            new Layer(f, Shadow).Offset(0, -6).Soft(4f),
            new Layer(p => f(p) - outlineW, outline),
            new Layer(f, top).Grad(top, bottom, 110, 18),
            new Layer(p => Mathf.Max(f(p) + 8f, -(p.y - 76f)), new Color(1, 1, 1, 0.32f)).Soft(2f))));

        public static Sprite Heart => Badge("b_heart", p => HeartSdf(p, C + new Vector2(0, 2), 96f), Hex("#FF7A85"), Hex("#E0283F"), Hex("#7A1024"));
        public static Sprite HeartEmpty => Badge("b_heart_e", p => HeartSdf(p, C + new Vector2(0, 2), 96f), Hex("#6F6A86"), Hex("#4A4561"), Hex("#2A2638"));
        public static Sprite Star => Badge("b_star", p => Polygon(p, StarPoints(C + new Vector2(0, -2), 56f, 25f)) - 4f, Hex("#FFE66B"), Hex("#FFA91F"), Hex("#9A5A06"));
        public static Sprite StarEmpty => Badge("b_star_e", p => Polygon(p, StarPoints(C + new Vector2(0, -2), 56f, 25f)) - 4f, Hex("#8C8AA3"), Hex("#62607A"), Hex("#39374B"));
        public static Sprite Lock => Badge("b_lock", p => Mathf.Min(RoundBox(p, new Vector2(64, 46), new Vector2(34, 28), 10),
            Mathf.Max(Mathf.Abs(Circle(p, new Vector2(64, 76), 24f)) - 7f, -(p.y - 70f))), Hex("#FFD56A"), Hex("#E7A51C"), Hex("#7D4F09"));
        /// <summary>Gold coin with an engraved ring.</summary>
        public static Sprite Coin => Badge("b_coin", p => Mathf.Max(Circle(p, C, 48f), -(Mathf.Abs(Circle(p, C, 31f)) - 3.5f)),
            Hex("#FFE77A"), Hex("#F5A623"), Hex("#8A5A00"));
        /// <summary>Red notification dot.</summary>
        public static Sprite Dot => Badge("b_dot", p => Circle(p, C, 40f), Hex("#FF7A7A"), Hex("#E5303F"), Hex("#FFFFFF"), 9f);

        // ---- meta icons (white glyphs for colored buttons; tint the Image for use on light cards)
        public static Sprite Gift => Glyph("i_gift", p =>
        {
            float body = RoundBox(p, new Vector2(64, 40), new Vector2(36, 26), 6);
            float lid = RoundBox(p, new Vector2(64, 76), new Vector2(44, 10), 5);
            float box = Mathf.Max(Mathf.Min(body, lid), -RoundBox(p, new Vector2(64, 58), new Vector2(3.5f, 48), 1)); // ribbon slit
            // bow: two loops with holes, knotted in the middle
            float loopL = Mathf.Max(Circle(p, new Vector2(49, 98), 14f), -Circle(p, new Vector2(49, 98), 6f));
            float loopR = Mathf.Max(Circle(p, new Vector2(79, 98), 14f), -Circle(p, new Vector2(79, 98), 6f));
            return Mathf.Min(Mathf.Min(box, Mathf.Min(loopL, loopR)), Circle(p, new Vector2(64, 92), 7f));
        });
        public static Sprite Wheel => Glyph("i_wheel", p =>
        {
            float ring = Mathf.Abs(Circle(p, C, 42f)) - 7f;
            float spokes = 1e6f;
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 4f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 40f;
                spokes = Mathf.Min(spokes, Segment(p, C - dir, C + dir, 4.5f));
            }
            float pointer = Polygon(p, new[] { new Vector2(52, 124), new Vector2(76, 124), new Vector2(64, 100) }) - 2f;
            return Mathf.Min(Mathf.Min(ring, spokes), Mathf.Min(Circle(p, C, 11f), pointer));
        });
        public static Sprite MissionList => Glyph("i_missions", p =>
        {
            float d = 1e6f;
            for (int i = 0; i < 3; i++)
            {
                float y = 34 + i * 30;
                d = Mathf.Min(d, Segment(p, new Vector2(56, y), new Vector2(104, y), 7f));
                d = Mathf.Min(d, RoundBox(p, new Vector2(30, y), new Vector2(9, 9), 3));
            }
            return d;
        });
        public static Sprite Bag => Glyph("i_bag", p =>
        {
            float body = RoundBox(p, new Vector2(64, 50), new Vector2(40, 34), 10);
            float handle = Mathf.Max(Mathf.Abs(Circle(p, new Vector2(64, 84), 20f)) - 6f, -(p.y - 84f));
            return Mathf.Min(body, handle);
        });
        public static Sprite Magnet => Glyph("i_magnet", p =>
        {
            float bend = Mathf.Max(Mathf.Abs(Circle(p, new Vector2(64, 60), 30f)) - 11f, p.y - 60f);
            float legs = Mathf.Min(Segment(p, new Vector2(34, 60), new Vector2(34, 100), 11f), Segment(p, new Vector2(94, 60), new Vector2(94, 100), 11f));
            return Mathf.Min(bend, legs);
        });
        public static Sprite Glue => Glyph("i_glue", p => Mathf.Min(Circle(p, new Vector2(64, 50), 30f),
            Polygon(p, new[] { new Vector2(64, 112), new Vector2(36, 58), new Vector2(92, 58) }) - 2f));
        public static Sprite Shield => Glyph("i_shield", p =>
            Polygon(p, new[] { new Vector2(28, 102), new Vector2(100, 102), new Vector2(100, 66), new Vector2(64, 20), new Vector2(28, 66) }) - 6f);
        public static Sprite Tower => Glyph("i_tower", p => Mathf.Min(Mathf.Min(Circle(p, new Vector2(64, 30), 20f), Circle(p, new Vector2(58, 64), 18f)), Circle(p, new Vector2(68, 96), 16f)));

        public static Sprite BoosterIcon(int b) => b == 0 ? Clock : b == 1 ? Star : Check;

        // ------------------------------------------------------------------ title-screen effects

        /// <summary>Sunburst: soft alternating light wedges that fade out toward the edge.</summary>
        public static Sprite Rays => Cached("fx_rays", () =>
        {
            const int S = 256;
            var px = new Color[S * S];
            var c = new Vector2(S / 2f, S / 2f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    var d = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float r = d.magnitude / (S / 2f);
                    if (r > 1f) continue;
                    float ang = Mathf.Atan2(d.y, d.x);
                    float wedge = Mathf.SmoothStep(0.15f, 0.55f, 0.5f + 0.5f * Mathf.Cos(ang * 12f));
                    float fade = Mathf.Pow(1f - r, 1.6f) * Mathf.SmoothStep(0.05f, 0.25f, r);
                    px[y * S + x] = new Color(1f, 1f, 1f, wedge * fade);
                }
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            tex.SetPixels(px);
            tex.Apply(true, true);
            return Plain(tex);
        });

        /// <summary>Soft round glow (white, alpha falls off smoothly).</summary>
        public static Sprite Glow => Cached("fx_glow", () =>
        {
            const int S = 128;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float r = (new Vector2(x + 0.5f, y + 0.5f) - new Vector2(S / 2f, S / 2f)).magnitude / (S / 2f);
                    float a = Mathf.Clamp01(1f - r);
                    px[y * S + x] = new Color(1f, 1f, 1f, a * a * (3f - 2f * a));
                }
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            tex.SetPixels(px);
            tex.Apply(true, true);
            return Plain(tex);
        });

        /// <summary>Four-point twinkle star.</summary>
        public static Sprite Twinkle => Cached("fx_twinkle", () => Plain(Render(I, I,
            new Layer(StarDist, new Color(1f, 0.95f, 0.72f, 1f)).Soft(1.5f),
            new Layer(p => Circle(p, C, 12f), Color.white).Soft(8f))));

        // concave four-point star (astroid |x|^.5 + |y|^.5 = r^.5), as an approximate distance field
        static float StarDist(Vector2 p)
        {
            float ax = Mathf.Max(Mathf.Abs(p.x - 64f), 0.05f), ay = Mathf.Max(Mathf.Abs(p.y - 64f), 0.05f);
            float f = Mathf.Sqrt(ax) + Mathf.Sqrt(ay) - Mathf.Sqrt(60f);
            float grad = 0.5f * Mathf.Sqrt(1f / ax + 1f / ay);
            return f / grad;
        }

        /// <summary>Diagonal light band used for the shine sweep on buttons.</summary>
        public static Sprite ShineBand => Cached("fx_shine", () =>
        {
            const int W = 64, H = 128;
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float t = Mathf.Abs((x - W / 2f) / (W / 2f));
                    float a = Mathf.Clamp01(1f - t);
                    px[y * W + x] = new Color(1f, 1f, 1f, a * a * 0.55f);
                }
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels(px);
            tex.Apply(false, true);
            return Plain(tex);
        });
    }
}
