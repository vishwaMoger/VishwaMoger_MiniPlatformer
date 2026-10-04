using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MiniPlatformer
{
    public enum TextStyle { Title, Heading, Body, Dark }

    /// <summary>Helpers that build the smooth uGUI from code (procedural UIArt skins + Lilita One).</summary>
    public static class UIKit
    {
        static Material s_Title, s_Heading, s_Body, s_Dark;

        public static readonly Color Cream = new Color32(255, 255, 255, 255);
        public static readonly Color Ink = new Color32(74, 46, 72, 255);
        public static readonly Color Gold = new Color32(255, 214, 74, 255);
        public static readonly Color Muted = new Color32(150, 140, 170, 255);
        static readonly Color Outline = new Color32(58, 30, 70, 255);

        static TMP_FontAsset Font => GameAssets.I.font;

        static Material Mat(TextStyle s)
        {
            if (s_Title == null)
            {
                // prefer the saved materials (GameAssets): only they keep the outline / underlay variants in a build
                var ga = GameAssets.I;
                s_Title = ga.textTitle != null ? ga.textTitle : MakeMat(TitleSpec);
                s_Heading = ga.textHeading != null ? ga.textHeading : MakeMat(HeadingSpec);
                s_Body = ga.textBody != null ? ga.textBody : MakeMat(BodySpec);
                s_Dark = new Material(Font.material);
            }
            return s switch { TextStyle.Title => s_Title, TextStyle.Heading => s_Heading, TextStyle.Body => s_Body, _ => s_Dark };
        }

        // outline width, underlay (drop shadow) on, shadow offset
        public static readonly (float outline, bool underlay, float offset) TitleSpec = (0.32f, true, 0.9f), HeadingSpec = (0.24f, true, 0.6f),
            BodySpec = (0.18f, true, 0.45f), WorldSpec = (0.45f, true, 0.7f); // world labels sit on dark brick: thick outline + shadow

        static Material MakeMat((float outline, bool underlay, float offset) spec)
        {
            var m = new Material(Font.material);
            ConfigureText(m, spec);
            return m;
        }

        /// <summary>Text material for world-space labels (GOAL / START): thick outline and a drop shadow.</summary>
        public static Material WorldTextMaterial => s_World != null ? s_World : (s_World = GameAssets.I.textWorld != null ? GameAssets.I.textWorld : MakeMat(WorldSpec));
        static Material s_World;

        /// <summary>Sets up an SDF text material (used at runtime and by the editor to save the build materials).</summary>
        public static void ConfigureText(Material m, (float outline, bool underlay, float offset) spec)
        {
            float outline = spec.outline; bool underlay = spec.underlay; float offset = spec.offset;
            Color outlineColor = Outline;
            m.EnableKeyword(ShaderUtilities.Keyword_Outline);
            m.SetFloat(ShaderUtilities.ID_OutlineWidth, outline);
            m.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
            m.SetFloat(ShaderUtilities.ID_FaceDilate, outline * 0.6f);
            m.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0.02f);
            if (underlay)
            {
                m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.08f, 0.02f, 0.12f, 0.45f));
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -offset);
                m.SetFloat(ShaderUtilities.ID_UnderlayDilate, outline * 1.2f);
                m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.35f);
            }
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name, float inset = 0f)
        {
            var rt = Rect(parent, name, Vector2.zero, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image Image(Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size, string name = "Image")
        {
            var rt = Rect(parent, name, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>9-sliced smooth skin (see UIArt.Skin for keys).</summary>
        public static Image Panel(Transform parent, string key, Vector2 anchor, Vector2 pos, Vector2 size, string name = "Panel")
        {
            var img = Image(parent, UIArt.Skin(key), anchor, pos, size, name);
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            return img;
        }

        public static Image Dim(Transform parent, float alpha = 0.6f)
        {
            var rt = Stretch(parent, "Dim");
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = null;
            img.color = new Color(0.1f, 0.05f, 0.18f, alpha);
            img.raycastTarget = true;
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string text, float size, Vector2 anchor, Vector2 pos, Vector2 box,
            TextStyle style = TextStyle.Body, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = Rect(parent, "Text", anchor, pos, box);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font;
            t.fontSharedMaterial = Mat(style);
            t.text = text;
            t.fontSize = size * 1.3f;
            t.color = color ?? (style == TextStyle.Dark ? Ink : Cream);
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.lineSpacing = -4f;
            return t;
        }

        public static Button Button(Transform parent, string label, string color, Vector2 anchor, Vector2 pos, Vector2 size,
            UnityAction onClick, Sprite icon = null, float fontSize = 34f, string sound = "click")
        {
            var img = Panel(parent, color, anchor, pos, size, "Button_" + label);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.SpriteSwap;
            var pressed = UIArt.Skin(color + "_pressed");
            btn.spriteState = new SpriteState { pressedSprite = pressed, selectedSprite = null, highlightedSprite = null };
            if (onClick != null) btn.onClick.AddListener(onClick);
            var fx = img.gameObject.AddComponent<UIButtonFX>();
            fx.sound = sound;

            var content = Rect(img.transform, "Content", new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), size);
            fx.content = content;
            bool hasLabel = !string.IsNullOrEmpty(label);
            if (icon != null)
            {
                float s = hasLabel ? Mathf.Min(size.y - 30f, 64f) : Mathf.Min(size.x, size.y) * 0.62f;
                var iconImg = Image(content, icon, new Vector2(0.5f, 0.5f), new Vector2(hasLabel ? -size.x * 0.5f + s * 0.5f + 30f : 0f, 0f), new Vector2(s, s), "Icon");
                iconImg.preserveAspect = true;
            }
            if (hasLabel)
            {
                var t = Text(content, label, fontSize, new Vector2(0.5f, 0.5f), new Vector2(icon != null ? 24f : 0f, 0f), size, TextStyle.Body);
                t.textWrappingMode = TextWrappingModes.NoWrap;
            }
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.Automatic;
            btn.navigation = nav;
            return btn;
        }

        public static Button IconButton(Transform parent, Sprite icon, string color, Vector2 anchor, Vector2 pos, float size, UnityAction onClick, string sound = "click")
            => Button(parent, null, color, anchor, pos, new Vector2(size, size), onClick, icon, 0f, sound);

        public static Slider Slider(Transform parent, Vector2 anchor, Vector2 pos, float width, float value, Action<float> onChange)
        {
            var root = Rect(parent, "Slider", anchor, pos, new Vector2(width, 48f));
            var track = Panel(root, "track", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 40f), "Track");
            var fillArea = Rect(root, "FillArea", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width - 12f, 30f));
            var fill = Panel(fillArea, "fill", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 30f), "Fill");
            fill.color = new Color32(120, 214, 84, 255);
            fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            fill.rectTransform.anchorMax = new Vector2(1f, 1f);
            fill.rectTransform.sizeDelta = Vector2.zero;
            var handleArea = Rect(root, "HandleArea", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width - 40f, 48f));
            var handle = Image(handleArea, UIArt.Skin("circle"), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(64f, 64f), "Handle");
            handle.raycastTarget = true;
            var s = root.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.minValue = 0f;
            s.maxValue = 1f;
            s.value = value;
            s.onValueChanged.AddListener(v => onChange(v));
            track.raycastTarget = true;
            return s;
        }

        public static CanvasGroup Group(GameObject go)
        {
            var g = go.GetComponent<CanvasGroup>();
            return g != null ? g : go.AddComponent<CanvasGroup>();
        }

        /// <summary>Pop-in animation for panels (scale + fade), unscaled time so it works while paused.</summary>
        public static void PopIn(RectTransform rt, float delay = 0f, float duration = 0.35f)
        {
            var g = Group(rt.gameObject);
            g.alpha = 0f;
            rt.localScale = Vector3.one * 0.85f;
            Tween.To(rt, duration, k =>
            {
                g.alpha = Mathf.Clamp01(k * 2f);
                rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, Ease.OutBack(k));
            }, x => x, true, null, delay);
        }

        public static void SlideIn(RectTransform rt, Vector2 from, float delay = 0f, float duration = 0.45f)
        {
            var to = rt.anchoredPosition;
            rt.anchoredPosition = to + from;
            var g = Group(rt.gameObject);
            g.alpha = 0f;
            Tween.To(rt, duration, k =>
            {
                g.alpha = Mathf.Clamp01(k * 3f);
                rt.anchoredPosition = Vector2.LerpUnclamped(to + from, to, Ease.OutBack(k));
            }, x => x, true, null, delay);
        }

        public static string FormatTime(float t)
        {
            if (t <= 0f) return "--:--.-";
            int m = (int)(t / 60f);
            float s = t - m * 60f;
            return $"{m:00}:{s:00.0}";
        }
    }
}
