using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MiniPlatformer
{
    /// <summary>Global URP volume: bloom, vignette, color grading per world, and reactive pulses (hit, victory).</summary>
    public class PostFX : MonoBehaviour
    {
        public static PostFX I { get; private set; }

        Volume m_Volume;
        Bloom m_Bloom;
        Vignette m_Vignette;
        ColorAdjustments m_Color;
        ChromaticAberration m_Chroma;
        LiftGammaGain m_Lgg;
        float m_BaseVignette = 0.28f, m_BaseBloom = 0.6f;
        float m_Hit, m_Glow;

        public static void Create()
        {
            if (I != null) return;
            var go = new GameObject("PostFX");
            DontDestroyOnLoad(go);
            I = go.AddComponent<PostFX>();
        }

        void Awake()
        {
            m_Volume = gameObject.AddComponent<Volume>();
            m_Volume.isGlobal = true;
            m_Volume.priority = 10;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            m_Volume.sharedProfile = profile;

            m_Bloom = profile.Add<Bloom>(true);
            m_Bloom.threshold.Override(0.85f);
            m_Bloom.intensity.Override(0.6f);
            m_Bloom.scatter.Override(0.65f);
            m_Bloom.highQualityFiltering.Override(false);

            m_Vignette = profile.Add<Vignette>(true);
            m_Vignette.intensity.Override(0.28f);
            m_Vignette.smoothness.Override(0.45f);
            m_Vignette.color.Override(new Color(0.08f, 0.04f, 0.12f));

            m_Color = profile.Add<ColorAdjustments>(true);
            m_Color.saturation.Override(10f);
            m_Color.contrast.Override(8f);
            m_Color.postExposure.Override(0f);
            m_Color.colorFilter.Override(Color.white);

            m_Chroma = profile.Add<ChromaticAberration>(true);
            m_Chroma.intensity.Override(0f);

            m_Lgg = profile.Add<LiftGammaGain>(true);
            m_Lgg.lift.Override(new Vector4(1f, 1f, 1f, 0f));
        }

        public void ApplyWorld(WorldTheme w)
        {
            m_BaseBloom = w.bloom;
            m_BaseVignette = w.vignette;
            m_Color.saturation.Override(w.saturation);
            m_Color.contrast.Override(w.contrast);
            m_Color.postExposure.Override(w.exposure);
            m_Color.colorFilter.Override(w.colorFilter);
            m_Bloom.intensity.Override(w.bloom);
            m_Vignette.intensity.Override(w.vignette);
            // lift the shadows slightly toward the world's sky tone for a cohesive palette
            var s = w.sky;
            m_Lgg.lift.Override(new Vector4(s.r * 0.5f + 0.5f, s.g * 0.5f + 0.5f, s.b * 0.5f + 0.5f, 0.02f));
        }

        public void HitPulse() => m_Hit = 1f;
        public void GlowPulse() => m_Glow = 1f;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            m_Hit = Mathf.MoveTowards(m_Hit, 0f, dt * 2.5f);
            m_Glow = Mathf.MoveTowards(m_Glow, 0f, dt * 1.2f);
            m_Chroma.intensity.value = m_Hit * 0.8f;
            m_Vignette.intensity.value = m_BaseVignette + m_Hit * 0.18f;
            m_Vignette.color.value = Color.Lerp(new Color(0.08f, 0.04f, 0.12f), new Color(0.55f, 0.05f, 0.08f), m_Hit);
            m_Bloom.intensity.value = m_BaseBloom + m_Glow * 1.2f;
        }
    }
}
