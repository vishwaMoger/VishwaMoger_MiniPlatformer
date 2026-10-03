using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace MiniPlatformer.EditorTools
{
    /// <summary>
    /// Renders the game logo, app icon and splash branding to PNGs (Tools > Fruit Quest > Generate Branding).
    /// Lettering is drawn from the Lilita One SDF atlas so it stays crisp with thick outlines at any size.
    /// </summary>
    public static class BrandingGenerator
    {
        const string Dir = "Assets/_Game/Art/Branding";
        const string LogoPath = Dir + "/logo.png";
        const string IconPath = Dir + "/icon.png";
        const string FontPath = "Assets/_Game/Art/Fonts/LilitaOne SDF.asset";
        const string PA = "Assets/_Game/Art/PixelAdventure1/";

        [MenuItem("Tools/Fruit Quest/Generate Branding")]
        public static void Generate()
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllBytes(LogoPath, RenderLogo().EncodeToPNG());
            File.WriteAllBytes(IconPath, RenderIcon().EncodeToPNG());
            AssetDatabase.ImportAsset(LogoPath);
            AssetDatabase.ImportAsset(IconPath);
            ConfigureImporter(LogoPath, true);
            ConfigureImporter(IconPath, false);
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[FruitQuest] Branding generated.");
        }

        static void ConfigureImporter(string path, bool sprite)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = sprite;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Trilinear;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.maxTextureSize = 2048;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.SaveAndReimport();
        }

        static void ApplyPlayerSettings()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.backgroundColor = new Color32(30, 20, 51, 255);
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.UnityLogoBelow;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            PlayerSettings.SplashScreen.overlayOpacity = 0f;
            if (logo != null) PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2.5f, logo) };
        }

        // ------------------------------------------------------------------ canvas

        class Canvas
        {
            public readonly int w, h;
            public readonly Color[] px;
            public Canvas(int w, int h) { this.w = w; this.h = h; px = new Color[w * h]; }

            public void Blend(int x, int y, Color c, float cov)
            {
                if (x < 0 || y < 0 || x >= w || y >= h || cov <= 0f) return;
                ref Color d = ref px[y * w + x];
                float a = c.a * Mathf.Clamp01(cov);
                float outA = a + d.a * (1f - a);
                if (outA <= 0f) return;
                d.r = (c.r * a + d.r * d.a * (1f - a)) / outA;
                d.g = (c.g * a + d.g * d.a * (1f - a)) / outA;
                d.b = (c.b * a + d.b * d.a * (1f - a)) / outA;
                d.a = outA;
            }

            /// <summary>Fills an SDF shape (negative = inside) with a vertical gradient.</summary>
            public void Shape(Func<Vector2, float> sdf, Color top, Color bottom, float yTop, float yBottom, float feather = 1f, Vector2 offset = default)
            {
                for (int y = 0; y < h; y++)
                {
                    Color c = Color.Lerp(top, bottom, Mathf.InverseLerp(yTop, yBottom, y));
                    for (int x = 0; x < w; x++)
                    {
                        float d = sdf(new Vector2(x + 0.5f, y + 0.5f) - offset);
                        if (d > feather) continue;
                        Blend(x, y, c, 0.5f - d / feather);
                    }
                }
            }

            public void Shape(Func<Vector2, float> sdf, Color c, float feather = 1f, Vector2 offset = default) =>
                Shape(sdf, c, c, 0, h, feather, offset);

            /// <summary>Nearest-neighbour stamp of a pixel-art sprite (keeps the pixels crisp).</summary>
            public void Stamp(Color32[] src, int sw, int sh, Rect frame, Vector2 bottomLeft, int scale)
            {
                for (int fy = 0; fy < (int)frame.height; fy++)
                    for (int fx = 0; fx < (int)frame.width; fx++)
                    {
                        var c = (Color)src[((int)frame.y + fy) * sw + (int)frame.x + fx];
                        if (c.a < 0.05f) continue;
                        for (int sy = 0; sy < scale; sy++)
                            for (int sx = 0; sx < scale; sx++)
                                Blend((int)bottomLeft.x + fx * scale + sx, (int)bottomLeft.y + fy * scale + sy, c, 1f);
                    }
            }

            public Texture2D ToTexture()
            {
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
                t.SetPixels(px);
                t.Apply();
                return t;
            }
        }

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        static Color32[] LoadPixels(string path, out int w, out int h)
        {
            var t = new Texture2D(2, 2);
            t.LoadImage(File.ReadAllBytes(path));
            w = t.width; h = t.height;
            var p = t.GetPixels32();
            UnityEngine.Object.DestroyImmediate(t);
            return p;
        }

        static void Sprite(Canvas c, string file, int frameSize, Vector2 center, int scale)
        {
            var p = LoadPixels(PA + file, out int w, out int h);
            var frame = new Rect(0, h - frameSize, frameSize, frameSize);
            c.Stamp(p, w, h, frame, center - new Vector2(frameSize * scale / 2f, frameSize * scale / 2f), scale);
        }

        static float Sparkle(Vector2 p, Vector2 c, float r)
        {
            var q = p - c;
            float d1 = Mathf.Abs(q.x) * 0.25f + Mathf.Abs(q.y) - r;
            float d2 = Mathf.Abs(q.x) + Mathf.Abs(q.y) * 0.25f - r;
            return Mathf.Min(d1, d2);
        }

        // ------------------------------------------------------------------ SDF text

        class SdfText
        {
            readonly TMP_FontAsset m_Font;
            readonly Color32[] m_Atlas;
            readonly int m_AW, m_AH;
            readonly float m_Spread;

            public SdfText(TMP_FontAsset font)
            {
                m_Font = font;
                font.ReadFontAssetDefinition();
                font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz0123456789!-", out _);
                var src = font.atlasTexture;
                var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(src, rt);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
                copy.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                m_Atlas = copy.GetPixels32();
                m_AW = src.width; m_AH = src.height;
                UnityEngine.Object.DestroyImmediate(copy);
                m_Spread = font.atlasPadding;
            }

            float Sample(float u, float v)
            {
                u = Mathf.Clamp(u - 0.5f, 0, m_AW - 1.001f);
                v = Mathf.Clamp(v - 0.5f, 0, m_AH - 1.001f);
                int x = (int)u, y = (int)v;
                float fx = u - x, fy = v - y;
                float a = m_Atlas[y * m_AW + x].a, b = m_Atlas[y * m_AW + x + 1].a;
                float c = m_Atlas[(y + 1) * m_AW + x].a, d = m_Atlas[(y + 1) * m_AW + x + 1].a;
                return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy) / 255f;
            }

            public struct Letter { public Rect atlasRect; public Vector2 origin; public float scale, angle; public Vector2 pivot; public Color color; }

            /// <summary>Lays out a string centred on 'center' (baseline-ish) at 'px' size; each letter gets a playful tilt.</summary>
            public Letter[] Layout(string s, Vector2 center, float px, Color[] colors, float tiltAmount, float spacing = 0.92f)
            {
                float k = px / m_Font.faceInfo.pointSize;
                float total = 0f;
                foreach (char ch in s) total += Advance(ch) * k * spacing;
                float x = center.x - total / 2f;
                var list = new System.Collections.Generic.List<Letter>();
                int i = 0;
                foreach (char ch in s)
                {
                    if (!m_Font.characterLookupTable.TryGetValue(ch, out var chr)) { x += px * 0.3f; continue; }
                    var g = chr.glyph;
                    var r = g.glyphRect;
                    var m = g.metrics;
                    if (ch != ' ')
                    {
                        float left = x + m.horizontalBearingX * k, bottom = center.y + (m.horizontalBearingY - m.height) * k - px * 0.35f;
                        float wobble = Mathf.Sin(i * 1.7f) * tiltAmount;
                        list.Add(new Letter
                        {
                            atlasRect = new Rect(r.x, r.y, r.width, r.height),
                            origin = new Vector2(left, bottom + Mathf.Sin(i * 1.3f + 0.4f) * px * 0.04f),
                            scale = k,
                            angle = wobble,
                            pivot = new Vector2(left + m.width * k / 2f, bottom + m.height * k / 2f),
                            color = colors[i % colors.Length],
                        });
                    }
                    x += Advance(ch) * k * spacing;
                    i++;
                }
                return list.ToArray();
            }

            float Advance(char ch) => m_Font.characterLookupTable.TryGetValue(ch, out var c) ? c.glyph.metrics.horizontalAdvance : m_Font.faceInfo.pointSize * 0.3f;

            /// <summary>Signed distance (px, negative inside) to a letter, and its local height fraction.</summary>
            public float Distance(Letter L, Vector2 p, out float heightFrac)
            {
                // undo the tilt around the letter's centre
                float a = -L.angle * Mathf.Deg2Rad;
                var q = p - L.pivot;
                q = new Vector2(q.x * Mathf.Cos(a) - q.y * Mathf.Sin(a), q.x * Mathf.Sin(a) + q.y * Mathf.Cos(a)) + L.pivot;
                float u = L.atlasRect.x + (q.x - L.origin.x) / L.scale;
                float v = L.atlasRect.y + (q.y - L.origin.y) / L.scale;
                heightFrac = Mathf.InverseLerp(L.origin.y, L.origin.y + L.atlasRect.height * L.scale, q.y);
                float pad = m_Spread;
                if (u < L.atlasRect.xMin - pad || u > L.atlasRect.xMax + pad || v < L.atlasRect.yMin - pad || v > L.atlasRect.yMax + pad) return 1e4f;
                float sdf = Sample(u, v);                // 0.5 = edge, 1 = deep inside
                return -(sdf - 0.5f) * 2f * pad * L.scale; // convert to output pixels
            }

            public void Draw(Canvas c, Letter[] letters, float outline, Color outlineColor, Vector2 shadowOffset, Color shadow, bool gloss)
            {
                for (int y = 0; y < c.h; y++)
                    for (int x = 0; x < c.w; x++)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f);
                        float best = 1e4f, bestShadow = 1e4f, frac = 0f;
                        int bi = -1;
                        for (int i = 0; i < letters.Length; i++)
                        {
                            float d = Distance(letters[i], p, out float f);
                            if (d < best) { best = d; bi = i; frac = f; }
                            float ds = Distance(letters[i], p - shadowOffset, out _);
                            if (ds < bestShadow) bestShadow = ds;
                        }
                        if (bestShadow < outline + 2f) c.Blend(x, y, shadow, 0.5f - (bestShadow - outline) / 2f);
                        if (bi < 0 || best > outline + 1f) continue;
                        c.Blend(x, y, outlineColor, 0.5f - (best - outline));
                        if (best < 1f)
                        {
                            var baseCol = letters[bi].color;
                            var col = Color.Lerp(baseCol * 0.8f, Color.Lerp(baseCol, Color.white, 0.12f), frac);
                            col.a = 1f;
                            if (gloss && frac > 0.6f) col = Color.Lerp(col, Color.white, 0.28f);
                            c.Blend(x, y, col, 0.5f - best);
                        }
                    }
            }
        }

        // ------------------------------------------------------------------ logo

        const string CoverLogoPath = Dir + "/logo_cover.png";

        /// <summary>Title-only logo (no subtitle ribbon) for store covers: CrazyGames allows no text besides the game name.</summary>
        [MenuItem("Tools/Fruit Quest/Generate Cover Logo")]
        public static void GenerateCoverLogo()
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllBytes(CoverLogoPath, RenderLogo(false).EncodeToPNG());
            AssetDatabase.ImportAsset(CoverLogoPath);
            ConfigureImporter(CoverLogoPath, true);
            Debug.Log("[FruitQuest] Cover logo generated.");
        }

        static Texture2D RenderLogo(bool subtitle = true)
        {
            const int W = 2600, H = 720;
            const float cx = W / 2f;
            var c = new Canvas(W, H);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var text = new SdfText(font);
            Color ink = Hex("#3A1E46");

            // one line of big, candy-coloured lettering
            var colors = new[]
            {
                Hex("#FF5A5F"), Hex("#FF9F2E"), Hex("#FFD23F"), Hex("#6BD14B"), Hex("#39B8F0"), Color.white,
                Hex("#FF6FB5"), Hex("#A57CFF"), Hex("#39B8F0"), Hex("#6BD14B"), Hex("#FF9F2E"),
            };
            var letters = text.Layout("TILE TREK", new Vector2(cx, 440), 350f, colors, 5f);
            text.Draw(c, letters, 20f, ink, new Vector2(9, -18), new Color(0.12f, 0.03f, 0.2f, 0.55f), true);

            // ribbon with the subtitle
            if (subtitle)
            {
                Func<Vector2, float> ribbon = p => UIArt.RoundBox(p, new Vector2(cx, 150), new Vector2(470, 58), 24);
                Func<Vector2, float> tails = p => Mathf.Min(
                    UIArt.Polygon(p, new[] { new Vector2(cx - 570, 98), new Vector2(cx - 440, 98), new Vector2(cx - 440, 192), new Vector2(cx - 570, 192), new Vector2(cx - 525, 145) }),
                    UIArt.Polygon(p, new[] { new Vector2(cx + 570, 98), new Vector2(cx + 440, 98), new Vector2(cx + 440, 192), new Vector2(cx + 570, 192), new Vector2(cx + 525, 145) }));
                c.Shape(p => Mathf.Min(ribbon(p), tails(p)), new Color(0.1f, 0.03f, 0.2f, 0.35f), 10f, new Vector2(0, -12));
                c.Shape(p => tails(p) - 8f, ink);
                c.Shape(tails, Hex("#C23A4C"), Hex("#8E2336"), 192, 98);
                c.Shape(p => ribbon(p) - 8f, ink);
                c.Shape(ribbon, Hex("#FF6B6B"), Hex("#D93650"), 208, 92);
                c.Shape(p => Mathf.Max(ribbon(p) + 10f, -(p.y - 166f)), new Color(1, 1, 1, 0.25f), 3f);
                var sub = text.Layout("SLIDE THE ROOMS!", new Vector2(cx, 168), 64f, new[] { Color.white }, 0f, 0.95f);
                text.Draw(c, sub, 6f, ink, new Vector2(0, -5), new Color(0.12f, 0.03f, 0.2f, 0.4f), false);
            }

            // just the lettering (+ ribbon): crop tight to the drawn pixels
            var full = c.ToTexture();
            var cropped = AutoCrop(full, 16);
            UnityEngine.Object.DestroyImmediate(full);
            return cropped;
        }

        /// <summary>Crops a texture to the bounding box of its non-transparent pixels plus a margin.</summary>
        static Texture2D AutoCrop(Texture2D t, int margin)
        {
            var px = t.GetPixels32();
            int minX = t.width, minY = t.height, maxX = -1, maxY = -1;
            for (int y = 0; y < t.height; y++)
                for (int x = 0; x < t.width; x++)
                    if (px[y * t.width + x].a > 4)
                    {
                        if (x < minX) minX = x; if (x > maxX) maxX = x;
                        if (y < minY) minY = y; if (y > maxY) maxY = y;
                    }
            if (maxX < 0) return UnityEngine.Object.Instantiate(t);
            minX = Mathf.Max(0, minX - margin); minY = Mathf.Max(0, minY - margin);
            maxX = Mathf.Min(t.width - 1, maxX + margin); maxY = Mathf.Min(t.height - 1, maxY + margin);
            int w = maxX - minX + 1, h = maxY - minY + 1;
            var o = new Texture2D(w, h, TextureFormat.RGBA32, false);
            o.SetPixels(t.GetPixels(minX, minY, w, h));
            o.Apply();
            return o;
        }

        // ------------------------------------------------------------------ icon

        static Texture2D RenderIcon()
        {
            const int S = 1024;
            var c = new Canvas(S, S);
            var ctr = new Vector2(S / 2f, S / 2f);
            Func<Vector2, float> sq = p => UIArt.RoundBox(p, ctr, new Vector2(500, 500), 200);
            c.Shape(sq, Hex("#9BE7FF"), Hex("#3C8FEA"), S, 0);
            // sun glow
            c.Shape(p => Mathf.Max(UIArt.Circle(p, new Vector2(760, 780), 260), sq(p)), new Color(1f, 1f, 0.85f, 0.35f), 120f);
            // hills
            c.Shape(p => Mathf.Max(UIArt.Circle(p, new Vector2(300, -250), 620), sq(p)), Hex("#6BD14B"), Hex("#3E8948"), 380, 0);
            c.Shape(p => Mathf.Max(UIArt.Circle(p, new Vector2(900, -420), 700), sq(p)), Hex("#8BE35F"), Hex("#4DB33A"), 300, 0);
            Sprite(c, "Main Characters/Ninja Frog/Idle (32x32).png", 32, new Vector2(470, 460), 20);
            Sprite(c, "Items/Fruits/Apple.png", 32, new Vector2(800, 800), 11);
            c.Shape(p => Mathf.Abs(sq(p) + 14f) - 12f, new Color(1f, 1f, 1f, 0.35f), 2f);
            return c.ToTexture();
        }
    }
}
