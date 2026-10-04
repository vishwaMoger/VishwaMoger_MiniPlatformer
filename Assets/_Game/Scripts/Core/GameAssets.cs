using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Central registry of every sprite sheet, sound and material the game uses.
    /// Filled automatically by the editor setup (Tools > Fruit Quest > Run Full Setup) and loaded from Resources.
    /// Keys are paths relative to Assets/_Game/Art (or Audio) without the "(WxH)" size suffix,
    /// e.g. "PA1/Main Characters/Ninja Frog/Run" or "Music/stage1".
    /// </summary>
    [CreateAssetMenu(menuName = "Fruit Quest/Game Assets")]
    public class GameAssets : ScriptableObject
    {
        [Serializable] public class SpriteSet { public string key; public Sprite[] frames; }
        [Serializable] public class ClipEntry { public string key; public AudioClip clip; }
        [Serializable] public class TextureEntry { public string key; public Texture2D texture; }

        public List<SpriteSet> sprites = new List<SpriteSet>();
        public List<ClipEntry> clips = new List<ClipEntry>();
        public List<TextureEntry> textures = new List<TextureEntry>();
        public List<TextAsset> levels = new List<TextAsset>();

        [Header("Materials")]
        public Material spriteLit;
        public Material spriteGlow;
        public Material parallax;
        public Material additive;
        public Material water;
        public Material irisWipe;
        public Material trekPath;
        // saved text materials (outline / drop shadow on): runtime-only materials lose these shader features in builds
        public Material textTitle, textHeading, textBody, textWorld;

        [Header("Branding")]
        public Sprite logo;

        [Header("Fonts")]
        public TMP_FontAsset font;

        static GameAssets s_Instance;
        Dictionary<string, Sprite[]> m_Sprites;
        Dictionary<string, AudioClip> m_Clips;
        Dictionary<string, Texture2D> m_Textures;

        public static GameAssets I
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = Resources.Load<GameAssets>("GameAssets");
                    if (s_Instance == null) Debug.LogError("GameAssets not found. Run Tools > Fruit Quest > Run Full Setup.");
                }
                return s_Instance;
            }
        }

        void BuildCache()
        {
            m_Sprites = new Dictionary<string, Sprite[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in sprites) if (s != null && !string.IsNullOrEmpty(s.key)) m_Sprites[s.key] = s.frames;
            m_Clips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in clips) if (c != null && c.clip != null) m_Clips[c.key] = c.clip;
            m_Textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in textures) if (t != null && t.texture != null) m_Textures[t.key] = t.texture;
        }

        public Sprite[] Frames(string key)
        {
            if (m_Sprites == null) BuildCache();
            if (m_Sprites.TryGetValue(key, out var f)) return f;
            Debug.LogWarning($"[GameAssets] Missing sprite set '{key}'");
            return Array.Empty<Sprite>();
        }

        public Sprite Sprite(string key, int index = 0)
        {
            var f = Frames(key);
            return f.Length == 0 ? null : f[Mathf.Clamp(index, 0, f.Length - 1)];
        }

        public AudioClip Clip(string key)
        {
            if (m_Clips == null) BuildCache();
            return m_Clips.TryGetValue(key, out var c) ? c : null;
        }

        public Texture2D Texture(string key)
        {
            if (m_Textures == null) BuildCache();
            if (m_Textures.TryGetValue(key, out var t)) return t;
            Debug.LogWarning($"[GameAssets] Missing texture '{key}'");
            return null;
        }

        void OnEnable() { m_Sprites = null; m_Clips = null; m_Textures = null; }
    }
}
