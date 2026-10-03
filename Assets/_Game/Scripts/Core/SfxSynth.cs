using System.Collections.Generic;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Tiny sfxr-style synthesizer. Generates retro sound effects at startup so they cost no disk space
    /// and match the chiptune soundtrack.
    /// </summary>
    public static class SfxSynth
    {
        public enum Wave { Square, Saw, Sine, Noise, Triangle }

        public struct Params
        {
            public Wave wave;
            public float duration, attack, freqStart, freqEnd, duty, volume, vibratoDepth, vibratoSpeed;
            public float[] arpeggio; // frequency multipliers applied in equal time slices
        }

        const int SampleRate = 22050;

        public static AudioClip Make(string name, Params p)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(p.duration * SampleRate));
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            float phase = 0f, noiseVal = 0f, noisePhase = 0f;
            float duty = p.duty <= 0f ? 0.5f : p.duty;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float freq = p.freqStart * Mathf.Pow(p.freqEnd / Mathf.Max(1f, p.freqStart), t);
                if (p.arpeggio != null && p.arpeggio.Length > 0)
                    freq = p.freqStart * p.arpeggio[Mathf.Min(p.arpeggio.Length - 1, (int)(t * p.arpeggio.Length))];
                if (p.vibratoDepth > 0f)
                    freq *= 1f + p.vibratoDepth * Mathf.Sin(2f * Mathf.PI * p.vibratoSpeed * i / SampleRate);

                phase += freq / SampleRate;
                phase -= Mathf.Floor(phase);
                float s;
                switch (p.wave)
                {
                    case Wave.Square: s = phase < duty ? 0.6f : -0.6f; break;
                    case Wave.Saw: s = (phase * 2f - 1f) * 0.6f; break;
                    case Wave.Sine: s = Mathf.Sin(phase * Mathf.PI * 2f); break;
                    case Wave.Triangle: s = (Mathf.Abs(phase * 4f - 2f) - 1f) * 0.9f; break;
                    default:
                        noisePhase += freq / SampleRate;
                        if (noisePhase >= 1f) { noisePhase -= 1f; noiseVal = (float)(rng.NextDouble() * 2.0 - 1.0); }
                        s = noiseVal * 0.7f;
                        break;
                }

                float env;
                float atk = Mathf.Max(0.0001f, p.attack);
                if (t < atk) env = t / atk;
                else env = Mathf.Pow(1f - (t - atk) / (1f - atk), 1.6f);
                data[i] = s * env * (p.volume <= 0 ? 0.5f : p.volume);
            }

            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static Dictionary<string, AudioClip> BuildLibrary()
        {
            var d = new Dictionary<string, AudioClip>();
            void Add(string k, Params p) => d[k] = Make(k, p);

            Add("stomp", new Params { wave = Wave.Square, duration = 0.16f, freqStart = 520, freqEnd = 90, duty = 0.35f, volume = 0.45f });
            Add("break", new Params { wave = Wave.Noise, duration = 0.28f, freqStart = 5200, freqEnd = 700, volume = 0.5f });
            Add("boxhit", new Params { wave = Wave.Square, duration = 0.09f, freqStart = 300, freqEnd = 180, duty = 0.5f, volume = 0.35f });
            Add("shoot", new Params { wave = Wave.Square, duration = 0.12f, freqStart = 950, freqEnd = 320, duty = 0.25f, volume = 0.3f });
            Add("land", new Params { wave = Wave.Noise, duration = 0.07f, freqStart = 1400, freqEnd = 500, volume = 0.22f });
            Add("hurt", new Params { wave = Wave.Saw, duration = 0.3f, freqStart = 680, freqEnd = 120, volume = 0.45f, vibratoDepth = 0.12f, vibratoSpeed = 28f });
            Add("dash", new Params { wave = Wave.Noise, duration = 0.2f, attack = 0.15f, freqStart = 2600, freqEnd = 9000, volume = 0.3f });
            Add("fire", new Params { wave = Wave.Noise, duration = 0.45f, attack = 0.2f, freqStart = 1800, freqEnd = 600, volume = 0.35f });
            Add("slam", new Params { wave = Wave.Noise, duration = 0.35f, freqStart = 900, freqEnd = 120, volume = 0.6f });
            Add("heart", new Params { wave = Wave.Square, duration = 0.34f, freqStart = 523, duty = 0.5f, volume = 0.35f, arpeggio = new[] { 1f, 1.26f, 1.5f, 2f } });
            Add("checkpoint", new Params { wave = Wave.Triangle, duration = 0.42f, freqStart = 392, volume = 0.6f, arpeggio = new[] { 1f, 1.26f, 1.5f, 2f, 2.52f } });
            Add("star", new Params { wave = Wave.Square, duration = 0.22f, freqStart = 880, duty = 0.25f, volume = 0.3f, arpeggio = new[] { 1f, 1.5f, 2f } });
            Add("bosshit", new Params { wave = Wave.Saw, duration = 0.45f, freqStart = 240, freqEnd = 50, volume = 0.55f, vibratoDepth = 0.2f, vibratoSpeed = 18f });
            Add("bounce", new Params { wave = Wave.Square, duration = 0.14f, freqStart = 260, freqEnd = 780, duty = 0.4f, volume = 0.35f });
            Add("arrow", new Params { wave = Wave.Triangle, duration = 0.25f, freqStart = 440, freqEnd = 1400, volume = 0.55f });
            Add("appear", new Params { wave = Wave.Sine, duration = 0.35f, freqStart = 300, freqEnd = 1200, volume = 0.4f, vibratoDepth = 0.05f, vibratoSpeed = 30f });
            Add("tick", new Params { wave = Wave.Square, duration = 0.04f, freqStart = 1500, freqEnd = 1500, duty = 0.5f, volume = 0.2f });
            // fruit stack
            Add("topple", new Params { wave = Wave.Triangle, duration = 0.55f, freqStart = 700, freqEnd = 110, volume = 0.55f, vibratoDepth = 0.18f, vibratoSpeed = 14f });
            Add("splat", new Params { wave = Wave.Noise, duration = 0.18f, freqStart = 3000, freqEnd = 400, volume = 0.45f });
            Add("creak", new Params { wave = Wave.Saw, duration = 0.12f, freqStart = 180, freqEnd = 150, volume = 0.22f, vibratoDepth = 0.3f, vibratoSpeed = 40f });
            Add("bank", new Params { wave = Wave.Square, duration = 0.5f, freqStart = 523, duty = 0.25f, volume = 0.35f, arpeggio = new[] { 1f, 1.26f, 1.5f, 2f, 2.52f, 3f } });
            Add("coin", new Params { wave = Wave.Square, duration = 0.12f, freqStart = 988, duty = 0.5f, volume = 0.25f, arpeggio = new[] { 1f, 1.335f } });
            Add("spin", new Params { wave = Wave.Square, duration = 0.03f, freqStart = 2200, freqEnd = 1800, duty = 0.5f, volume = 0.18f });
            Add("thunder", new Params { wave = Wave.Noise, duration = 1.6f, attack = 0.03f, freqStart = 520, freqEnd = 50, volume = 0.6f });
            Add("jackpot", new Params { wave = Wave.Square, duration = 0.9f, freqStart = 523, duty = 0.35f, volume = 0.35f, arpeggio = new[] { 1f, 1.26f, 1.5f, 2f, 1.5f, 2f, 2.52f, 3f, 4f } });
            return d;
        }
    }
}
