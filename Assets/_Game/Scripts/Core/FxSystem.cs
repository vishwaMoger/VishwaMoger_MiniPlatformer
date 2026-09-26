using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MiniPlatformer
{
    /// <summary>Pooled world-space effects: dust, sparkles, debris, confetti, afterimages, lights and hit-stop.</summary>
    public class FxSystem : MonoBehaviour
    {
        public static FxSystem I { get; private set; }

        ParticleSystem m_Dust, m_Sparkle, m_Debris, m_Confetti;
        readonly Queue<SpriteRenderer> m_Ghosts = new Queue<SpriteRenderer>();
        Coroutine m_HitStop;
        float m_HitStopUntil;

        public static void Create()
        {
            if (I != null) return;
            var go = new GameObject("FX");
            DontDestroyOnLoad(go);
            I = go.AddComponent<FxSystem>();
        }

        void Awake()
        {
            var a = GameAssets.I;
            var dustTex = a.Sprite("PA1/Other/Dust Particle")?.texture;
            m_Dust = MakeSystem("Dust", Mat(a.spriteLit, dustTex), -0.15f, 400);
            m_Sparkle = MakeSystem("Sparkle", Mat(a.additive, PixelArt.Glow.texture), 0f, 400);
            m_Debris = MakeSystem("Debris", Mat(a.spriteLit, PixelArt.Pixel.texture), 2.4f, 400, 0.85f);
            m_Confetti = MakeSystem("Confetti", Mat(a.spriteLit, PixelArt.Pixel.texture), 0.5f, 400, 0.9f, true);
            for (int i = 0; i < 16; i++)
            {
                var g = new GameObject("Ghost").AddComponent<SpriteRenderer>();
                g.transform.SetParent(transform);
                g.sharedMaterial = a.spriteGlow;
                g.sortingOrder = Sort.Player - 1;
                g.gameObject.SetActive(false);
                m_Ghosts.Enqueue(g);
            }
        }

        static Material Mat(Material src, Texture tex)
        {
            var m = new Material(src);
            if (tex != null) m.mainTexture = tex;
            return m;
        }

        ParticleSystem MakeSystem(string name, Material mat, float gravity, int max, float shrinkTo = 0.1f, bool spin = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.startSpeed = 0f;
            main.gravityModifier = gravity;
            main.duration = 1f;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, shrinkTo));
            if (spin)
            {
                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            }
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.sortingOrder = Sort.FX;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
            return ps;
        }

        static void Emit(ParticleSystem ps, Vector2 pos, Vector2 vel, Color c, float size, float life, float rot = 0f)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = new Vector3(pos.x, pos.y, 0f),
                velocity = vel,
                startColor = c,
                startSize = size,
                startLifetime = life,
                rotation = rot,
                applyShapeToPosition = false,
            };
            ps.Emit(p, 1);
        }

        // ----------------------------------------------------------------- public helpers

        public static void Dust(Vector2 pos, int count = 4, float spread = 1.5f, float up = 0.6f)
        {
            if (I == null) return;
            for (int i = 0; i < count; i++)
            {
                var v = new Vector2(Random.Range(-spread, spread), Random.Range(0.1f, up));
                Emit(I.m_Dust, pos + new Vector2(Random.Range(-0.3f, 0.3f), 0.05f), v, new Color(1, 1, 1, 0.85f), Random.Range(0.6f, 1.0f), Random.Range(0.3f, 0.5f));
            }
        }

        public static void DustRing(Vector2 pos, int count = 10, float speed = 3f)
        {
            if (I == null) return;
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f;
                var v = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.45f) * speed;
                Emit(I.m_Dust, pos, v, new Color(1, 1, 1, 0.9f), 0.8f, 0.35f);
            }
        }

        public static void Sparkle(Vector2 pos, Color c, int count = 8, float speed = 3f, float size = 0.6f, float life = 0.5f)
        {
            if (I == null) return;
            for (int i = 0; i < count; i++)
            {
                var v = Random.insideUnitCircle.normalized * Random.Range(speed * 0.4f, speed);
                Emit(I.m_Sparkle, pos, v, c, Random.Range(size * 0.6f, size), Random.Range(life * 0.6f, life));
            }
        }

        public static void Debris(Vector2 pos, Color c, int count = 10, float speed = 5f, float size = 0.25f)
        {
            if (I == null) return;
            for (int i = 0; i < count; i++)
            {
                var v = new Vector2(Random.Range(-1f, 1f), Random.Range(0.3f, 1.2f)) * speed;
                Emit(I.m_Debris, pos + Random.insideUnitCircle * 0.3f, v, c, Random.Range(size * 0.6f, size * 1.3f), Random.Range(0.5f, 0.9f), Random.Range(0, 360));
            }
        }

        static readonly Color[] ConfettiColors =
        {
            new Color32(254, 231, 97, 255), new Color32(228, 59, 68, 255), new Color32(99, 199, 77, 255),
            new Color32(44, 232, 245, 255), new Color32(255, 119, 168, 255), new Color32(254, 174, 52, 255),
        };

        public static void Confetti(Vector2 pos, int count = 60, float speed = 11f)
        {
            if (I == null) return;
            for (int i = 0; i < count; i++)
            {
                var v = new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(0.6f, 1.2f)) * speed;
                var c = ConfettiColors[Random.Range(0, ConfettiColors.Length)];
                Emit(I.m_Confetti, pos, v, c, Random.Range(0.18f, 0.32f), Random.Range(1.4f, 2.4f), Random.Range(0, 360));
            }
        }

        /// <summary>Fading afterimage of a sprite (dash trail).</summary>
        public static void Ghost(SpriteRenderer src, Color tint, float life = 0.25f)
        {
            if (I == null || src == null || I.m_Ghosts.Count == 0) return;
            var g = I.m_Ghosts.Dequeue();
            g.gameObject.SetActive(true);
            g.sprite = src.sprite;
            g.flipX = src.flipX;
            g.transform.SetPositionAndRotation(src.transform.position, src.transform.rotation);
            g.transform.localScale = src.transform.lossyScale;
            g.color = tint;
            Tween.To(g, life, k => g.color = new Color(tint.r, tint.g, tint.b, tint.a * (1f - k)), x => x, false, () =>
            {
                g.gameObject.SetActive(false);
                I.m_Ghosts.Enqueue(g);
            });
        }

        /// <summary>Freezes gameplay for a few frames to sell an impact.</summary>
        public static void HitStop(float seconds)
        {
            if (I == null || Time.timeScale == 0f) return;
            I.m_HitStopUntil = Mathf.Max(I.m_HitStopUntil, Time.unscaledTime + seconds);
            if (I.m_HitStop == null) I.m_HitStop = I.StartCoroutine(I.HitStopRoutine());
        }

        IEnumerator HitStopRoutine()
        {
            Time.timeScale = 0.02f;
            while (Time.unscaledTime < m_HitStopUntil)
            {
                if (Game.I != null && Game.I.IsPaused) break;
                yield return null;
            }
            if (Game.I == null || !Game.I.IsPaused) Time.timeScale = 1f;
            m_HitStop = null;
        }

        public static Light2D PointLight(Transform parent, Color color, float intensity, float radius, float falloff = 0.6f, Vector2 offset = default)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.pointLightOuterRadius = radius;
            l.pointLightInnerRadius = radius * 0.1f;
            l.falloffIntensity = falloff;
            l.shadowsEnabled = false;
            return l;
        }

        /// <summary>Additive glow sprite behind an object (pairs with Bloom).</summary>
        public static SpriteRenderer Halo(Transform parent, Color color, float size, int order = Sort.Items - 1)
        {
            var go = new GameObject("Halo");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArt.Glow;
            sr.sharedMaterial = GameAssets.I.additive;
            sr.color = color;
            sr.sortingOrder = order;
            go.transform.localScale = Vector3.one * size;
            return sr;
        }
    }
}
