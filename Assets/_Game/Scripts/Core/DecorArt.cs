using System.Collections.Generic;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Pixel-art scenery drawn in Pixel Adventure's palette (dark outline, flat bright fills): grass, flowers, bushes,
    /// mushrooms, rocks, leaves, stumps, crystals, ferns, vines and banners. Generated at startup, recolored per world.
    /// </summary>
    public static class DecorArt
    {
        public enum Kind { Tuft, Flower, Bush, Mushroom, Rock, Leaves, Stump, Crystal, Fern, GlowShroom, Vine, Banner }

        static readonly Dictionary<string, Sprite> s_Cache = new Dictionary<string, Sprite>();

        static readonly string[] Tuft =
        {
            "....K....",
            ".K.KGK.K.",
            "KGKKGKKGK",
            "KGGKGgKGK",
            ".KgGgGgK.",
        };
        static readonly string[] Flower =
        {
            "..KKK..",
            ".KPPPK.",
            "KPPYPPK",
            ".KPPPK.",
            "..KGK..",
            ".KGGK..",
            "..KGK..",
            "..KGK..",
        };
        static readonly string[] Bush =
        {
            ".....KKK..KKK...",
            "...KKGGGKKGGGK..",
            "..KGGGlGGGGlGGK.",
            ".KGGlGGGGGGGGGGK",
            "KGGGGGGGlGGGGGgK",
            "KGgGGGGGGGGGGggK",
            "KgGGGgGGGGgGGggK",
            ".KggggggggggggK.",
        };
        static readonly string[] Mushroom =
        {
            "..KKKKK..",
            ".KRRWRRK.",
            "KRWRRRWRK",
            "KRRRRRRRK",
            ".KKKKKKK.",
            "...KLK...",
            "...KLK...",
            "..KLLLK..",
        };
        static readonly string[] Rock =
        {
            "...KKKK...",
            ".KKsssSKK.",
            "KssSssssSK",
            "KsssssSssK",
            "KSssSsssSK",
        };
        static readonly string[] Leaves =
        {
            "...KK..KK...",
            ".KKOOKKooKK.",
            "KOooOOOooOoK",
        };
        static readonly string[] Stump =
        {
            ".KKKKKKKK.",
            "KBLLLLLLBK",
            "KbBBBBBBbK",
            "KbBbBBbBbK",
            "KbBBbBBBbK",
            "KKbBBBBbKK",
            "KbKKKKKKbK",
        };
        static readonly string[] Crystal =
        {
            "....K....",
            "...KWK...",
            "...KPK.K.",
            "..KPWPKWK",
            "K.KPPPKPK",
            "KWKPPPKPK",
            "KPKPPPKPK",
            "KPPKPKPPK",
        };
        static readonly string[] Fern =
        {
            "....KK.....",
            ".KK.KGK..KK",
            "KGGKKGKKGGK",
            ".KGGKGKGGK.",
            "..KGGGGGK..",
            "KKKGGgGGKKK",
            "KGGGgggGGGK",
        };
        static readonly string[] GlowShroom =
        {
            ".KKKKK.",
            "KTTWTTK",
            "KTTTTTK",
            ".KKKKK.",
            "..KLK..",
            "..KLK..",
            ".KLLLK.",
        };
        static readonly string[] Vine =
        {
            "KGGGK",
            ".KGK.",
            ".KGK.",
            "KGGK.",
            ".KGK.",
            ".KGGK",
            ".KGK.",
            ".KGK.",
            "KGGK.",
            ".KGK.",
            ".KGK.",
            ".KlK.",
            "..K..",
        };
        static readonly string[] Banner =
        {
            "KKKKKKKK",
            "KyyyyyyK",
            "KRRRRRRK",
            "KRRRRRRK",
            "KRYRRYRK",
            "KRRYYRRK",
            "KRRYYRRK",
            "KRYRRYRK",
            "KRRRRRRK",
            "KRRRRRRK",
            "KRRRRRRK",
            "KRRKKRRK",
            "KRK..KRK",
            "KK....KK",
        };

        static Color32 C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        static readonly Dictionary<char, Color32> Base = new Dictionary<char, Color32>
        {
            { 'K', C("#211F30") }, { 'W', C("#FFFFFF") }, { 'Y', C("#FEE761") }, { 'y', C("#FEAE34") },
            { 'L', C("#EAD4AA") }, { 'B', C("#B86F50") }, { 'b', C("#733E39") }, { 's', C("#8B9BB4") }, { 'S', C("#5A6988") },
            { 'O', C("#F77622") }, { 'o', C("#BE4A2F") },
        };

        /// <summary>Per-world recolor of the foliage (G/g/l), petals (P), mushroom caps (R) and glow (T).</summary>
        static Dictionary<char, Color32> WorldColors(int world)
        {
            var d = new Dictionary<char, Color32>(Base);
            switch (world)
            {
                case 0: d['G'] = C("#63C74D"); d['g'] = C("#3E8948"); d['l'] = C("#A5E86B"); d['P'] = C("#F77FBE"); d['R'] = C("#E43B44"); break;
                case 1: d['G'] = C("#F7A43A"); d['g'] = C("#C2602E"); d['l'] = C("#FEE761"); d['P'] = C("#FEE761"); d['R'] = C("#B86F50"); break;
                case 2: d['G'] = C("#F07FC0"); d['g'] = C("#B5418C"); d['l'] = C("#FFC4E6"); d['P'] = C("#C8B6FF"); d['R'] = C("#FF6F9C"); break;
                case 3: d['G'] = C("#2FA37A"); d['g'] = C("#1C6B55"); d['l'] = C("#6EE0A5"); d['P'] = C("#8FF3FF"); d['R'] = C("#2CE8F5"); break;
                default: d['G'] = C("#6C7A8C"); d['g'] = C("#454E5E"); d['l'] = C("#9AA8B8"); d['P'] = C("#C9B8FF"); d['R'] = C("#8A5CEB"); break;
            }
            d['T'] = world == 3 ? C("#2CE8F5") : C("#FF8FD8");
            if (world == 4) { d['R'] = C("#8A2BE2"); }
            return d;
        }

        public static Sprite Get(Kind kind, int world, int variant = 0)
        {
            string key = $"{kind}_{world}_{variant}";
            if (s_Cache.TryGetValue(key, out var s) && s != null) return s;
            string[] rows = kind switch
            {
                Kind.Tuft => Tuft, Kind.Flower => Flower, Kind.Bush => Bush, Kind.Mushroom => Mushroom, Kind.Rock => Rock,
                Kind.Leaves => Leaves, Kind.Stump => Stump, Kind.Crystal => Crystal, Kind.Fern => Fern,
                Kind.GlowShroom => GlowShroom, Kind.Vine => Vine, _ => Banner,
            };
            var pal = WorldColors(world);
            if (kind == Kind.Flower && variant == 1) pal['P'] = C("#FFFFFF");
            if (kind == Kind.Flower && variant == 2) pal['P'] = C("#FEE761");
            if (kind == Kind.Banner && world == 4) pal['R'] = variant == 1 ? C("#5E38B5") : C("#A22633");
            bool hanging = kind == Kind.Vine || kind == Kind.Banner;
            s = Build(rows, pal, hanging ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f));
            s.name = key;
            s_Cache[key] = s;
            return s;
        }

        static Sprite Build(string[] rows, Dictionary<char, Color32> pal, Vector2 pivot)
        {
            int h = rows.Length, w = 0;
            foreach (var r in rows) w = Mathf.Max(w, r.Length);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    if (pal.TryGetValue(rows[y][x], out var c)) px[(h - 1 - y) * w + x] = c;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, K.PPU, 0, SpriteMeshType.FullRect, Vector4.zero, false);
        }

        /// <summary>Weighted scenery choices for ground surfaces in each world.</summary>
        public static (Kind kind, int variant, float weight)[] GroundSet(int world) => world switch
        {
            0 => new[] { (Kind.Tuft, 0, 5f), (Kind.Flower, 0, 2f), (Kind.Flower, 1, 1.5f), (Kind.Flower, 2, 1.5f), (Kind.Bush, 0, 1.5f), (Kind.Rock, 0, 0.8f), (Kind.Mushroom, 0, 0.8f) },
            1 => new[] { (Kind.Tuft, 0, 4f), (Kind.Leaves, 0, 3f), (Kind.Mushroom, 0, 1.5f), (Kind.Stump, 0, 1f), (Kind.Bush, 0, 1.5f), (Kind.Rock, 0, 0.8f) },
            2 => new[] { (Kind.Tuft, 0, 4f), (Kind.Crystal, 0, 1.5f), (Kind.Flower, 1, 2f), (Kind.Flower, 0, 1.5f), (Kind.Bush, 0, 1.2f), (Kind.Rock, 0, 1f) },
            3 => new[] { (Kind.Fern, 0, 3f), (Kind.Tuft, 0, 3f), (Kind.GlowShroom, 0, 2f), (Kind.Rock, 0, 1f), (Kind.Bush, 0, 1f) },
            _ => new[] { (Kind.Tuft, 0, 3f), (Kind.Rock, 0, 2f), (Kind.Mushroom, 0, 0.6f) },
        };

        public static bool Glows(Kind k) => k == Kind.Crystal || k == Kind.GlowShroom;
    }
}
