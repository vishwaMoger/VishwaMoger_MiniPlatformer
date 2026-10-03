using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Music crossfading (with a muffled low-pass while paused) and pooled, pitch-varied SFX.</summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        static readonly Dictionary<string, string> s_FileSfx = new Dictionary<string, string>
        {
            { "jump", "SFX/Jump_1" }, { "doublejump", "SFX/jump_2" }, { "collect", "SFX/collect" },
            { "die", "SFX/Dying" }, { "spring", "SFX/trampoline" }, { "win", "SFX/win" },
            { "click", "UI/click_002" }, { "select", "UI/select_002" }, { "back", "UI/back_002" },
            { "confirm", "UI/confirmation_002" }, { "error", "UI/error_004" }, { "switch", "UI/switch_002" },
            { "drop", "UI/drop_002" }, { "unlock", "UI/maximize_006" },
            { "complete", "Jingles/jingles_NES00" }, { "gameover", "Jingles/jingles_NES13" },
            { "jingle_small", "Jingles/jingles_NES09" },
        };

        AudioSource[] m_Music = new AudioSource[2];
        AudioLowPassFilter[] m_Filters = new AudioLowPassFilter[2];
        int m_ActiveMusic;
        string m_CurrentMusic;
        readonly List<AudioSource> m_Pool = new List<AudioSource>();
        Dictionary<string, AudioClip> m_Synth;
        readonly Dictionary<string, AudioClip> m_Cache = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> m_LastPlayed = new Dictionary<string, float>();
        float m_Duck = 1f, m_DuckTarget = 1f;
        float m_Cutoff = 22000f, m_CutoffTarget = 22000f;
        Coroutine m_Fade;

        float m_ComboTime;
        int m_Combo;

        public static void Create()
        {
            if (I != null) return;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            I = go.AddComponent<AudioManager>();
        }

        void Awake()
        {
            for (int i = 0; i < 2; i++)
            {
                var child = new GameObject("Music" + i);
                child.transform.SetParent(transform);
                var src = child.AddComponent<AudioSource>();
                src.loop = true; src.playOnAwake = false; src.volume = 0f; src.priority = 0;
                m_Music[i] = src;
                m_Filters[i] = child.AddComponent<AudioLowPassFilter>();
                m_Filters[i].cutoffFrequency = 22000f;
            }
            for (int i = 0; i < 12; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                m_Pool.Add(src);
            }
            m_Synth = SfxSynth.BuildLibrary();
        }

        // audio filters don't exist on WebGL, so the pause "muffle" is also a volume dip there
        float MusicVolume => Platform.HostMuted ? 0f : SaveSystem.Data.music * 0.55f * (m_CutoffTarget < 10000f ? 0.5f : 1f);
        float SfxVolume => Platform.HostMuted ? 0f : SaveSystem.Data.sfx;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            m_Duck = Mathf.MoveTowards(m_Duck, m_DuckTarget, dt * 2f);
            m_Cutoff = Mathf.Lerp(m_Cutoff, m_CutoffTarget, 1f - Mathf.Exp(-dt * 8f));
            var active = m_Music[m_ActiveMusic];
            if (m_Fade == null && active.clip != null) active.volume = MusicVolume * m_Duck;
            foreach (var f in m_Filters) f.cutoffFrequency = m_Cutoff;
        }

        public void PlayMusic(string key, float fade = 0.8f)
        {
            if (m_CurrentMusic == key) return;
            m_CurrentMusic = key;
            var clip = GameAssets.I.Clip(key);
            if (m_Fade != null) StopCoroutine(m_Fade);
            m_Fade = StartCoroutine(CrossFade(clip, fade));
        }

        IEnumerator CrossFade(AudioClip clip, float time)
        {
            var from = m_Music[m_ActiveMusic];
            m_ActiveMusic = 1 - m_ActiveMusic;
            var to = m_Music[m_ActiveMusic];
            to.clip = clip;
            to.volume = 0f;
            if (clip != null) to.Play();
            float startFrom = from.volume;
            for (float t = 0; t < time; t += Time.unscaledDeltaTime)
            {
                float k = t / time;
                from.volume = startFrom * (1f - k);
                to.volume = MusicVolume * m_Duck * k;
                yield return null;
            }
            from.Stop();
            from.volume = 0f;
            to.volume = MusicVolume * m_Duck;
            m_Fade = null;
        }

        /// <summary>Temporarily lowers the music (e.g. under a jingle).</summary>
        public void Duck(float amount, float seconds)
        {
            StartCoroutine(DuckRoutine(amount, seconds));
        }

        IEnumerator DuckRoutine(float amount, float seconds)
        {
            m_DuckTarget = amount;
            yield return new WaitForSecondsRealtime(seconds);
            m_DuckTarget = 1f;
        }

        public void SetMuffled(bool muffled) => m_CutoffTarget = muffled ? 900f : 22000f;

        AudioClip Resolve(string key)
        {
            if (m_Cache.TryGetValue(key, out var c)) return c;
            if (m_Synth.TryGetValue(key, out c)) { m_Cache[key] = c; return c; }
            if (s_FileSfx.TryGetValue(key, out var path)) c = GameAssets.I.Clip(path);
            if (c == null) c = GameAssets.I.Clip(key);
            m_Cache[key] = c;
            return c;
        }

        public void Play(string key, float volume = 1f, float pitchVariance = 0.05f, float pitch = 1f)
        {
            var clip = Resolve(key);
            if (clip == null) return;
            // avoid machine-gunning the same clip in one frame
            if (m_LastPlayed.TryGetValue(key, out var last) && Time.unscaledTime - last < 0.03f) return;
            m_LastPlayed[key] = Time.unscaledTime;

            AudioSource src = null;
            foreach (var s in m_Pool) if (!s.isPlaying) { src = s; break; }
            if (src == null) src = m_Pool[0];
            src.clip = clip;
            src.volume = volume * SfxVolume;
            src.pitch = pitch * (1f + Random.Range(-pitchVariance, pitchVariance));
            src.Play();
        }

        /// <summary>Fruit pickup: pitch climbs with quick successive pickups.</summary>
        public void PlayCollect()
        {
            if (Time.time - m_ComboTime > 0.9f) m_Combo = 0;
            m_ComboTime = Time.time;
            float pitch = Mathf.Pow(1.059463f, Mathf.Min(m_Combo, 12));
            m_Combo++;
            Play("collect", 0.7f, 0f, pitch);
        }
    }
}
