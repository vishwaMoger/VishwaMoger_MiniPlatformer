using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>World-themed weather that follows the camera: spring petals, autumn leaves, rain, fireflies, snow (and pollen/motes).</summary>
    public class AmbientFX : MonoBehaviour
    {
        ParticleSystem m_Ps;
        Transform m_Cam;
        Camera m_Camera;
        float m_Size;

        public static AmbientFX Create(Transform parent, Ambient kind, Transform cam)
        {
            var go = new GameObject("Ambient_" + kind);
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<AmbientFX>();
            fx.m_Cam = cam;
            fx.m_Camera = cam != null ? cam.GetComponent<Camera>() : null;
            fx.Build(kind);
            return fx;
        }

        void Build(Ambient kind)
        {
            var a = GameAssets.I;
            m_Ps = gameObject.AddComponent<ParticleSystem>();
            m_Ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = m_Ps.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 160;
            main.startSpeed = 0f;
            main.prewarm = true;
            var shape = m_Ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            FitArea();
            var em = m_Ps.emission;
            var noise = m_Ps.noise;
            noise.enabled = true;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.2f;
            var col = m_Ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) });
            col.color = grad;
            var vel = m_Ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            var r = GetComponent<ParticleSystemRenderer>();
            r.sortingOrder = Sort.FX - 2;

            Material glow = new Material(a.additive) { mainTexture = PixelArt.Glow.texture };
            Material pixel = new Material(a.spriteLit) { mainTexture = PixelArt.Pixel.texture };

            switch (kind)
            {
                case Ambient.Pollen:
                    r.sharedMaterial = glow;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 0.8f, 0.7f), new Color(1f, 0.95f, 0.6f, 0.4f));
                    em.rateOverTime = 7f;
                    vel.x = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
                    vel.y = new ParticleSystem.MinMaxCurve(0.05f, 0.35f);
                    vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                    noise.strength = 0.4f;
                    break;
                case Ambient.Leaves:
                    r.sharedMaterial = pixel;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.3f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color32(232, 106, 23, 255), new Color32(190, 74, 47, 255));
                    main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                    em.rateOverTime = 6f;
                    vel.x = new ParticleSystem.MinMaxCurve(-1.2f, -0.4f);
                    vel.y = new ParticleSystem.MinMaxCurve(-1.2f, -0.6f);
                    vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                    noise.strength = 0.9f;
                    var rot = m_Ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
                    break;
                case Ambient.Petals:
                    r.sharedMaterial = pixel;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color32(255, 178, 210, 255), new Color32(246, 129, 172, 255));
                    main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                    em.rateOverTime = 8f;
                    vel.x = new ParticleSystem.MinMaxCurve(-1.4f, -0.6f);
                    vel.y = new ParticleSystem.MinMaxCurve(-0.7f, -0.3f);
                    vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                    noise.strength = 0.8f;
                    var rot2 = m_Ps.rotationOverLifetime; rot2.enabled = true; rot2.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
                    break;
                case Ambient.Fireflies:
                    r.sharedMaterial = glow;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 1f, 0.4f, 0.9f), new Color(1f, 0.95f, 0.4f, 0.8f));
                    em.rateOverTime = 9f;
                    vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                    vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.25f);
                    vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                    noise.strength = 1.1f;
                    var sz = m_Ps.sizeOverLifetime; sz.enabled = true;
                    var curve = new AnimationCurve(new Keyframe(0, 0.2f), new Keyframe(0.25f, 1f), new Keyframe(0.5f, 0.35f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0.2f));
                    sz.size = new ParticleSystem.MinMaxCurve(1f, curve);
                    break;
                case Ambient.Snow:
                    r.sharedMaterial = glow;
                    main.maxParticles = 420;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 15f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.34f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.95f), new Color(0.8f, 0.9f, 1f, 0.7f));
                    em.rateOverTime = 34f;
                    vel.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.2f);
                    vel.y = new ParticleSystem.MinMaxCurve(-1.4f, -0.7f);
                    vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                    noise.strength = 0.7f;
                    noise.frequency = 0.35f;
                    break;
                case Ambient.Rain:
                {
                    r.sharedMaterial = pixel;
                    r.renderMode = ParticleSystemRenderMode.Stretch;
                    r.velocityScale = 0.05f;
                    r.lengthScale = 1.2f;
                    main.maxParticles = 450;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.2f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.1f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.85f, 1f, 0.55f), new Color(0.9f, 0.95f, 1f, 0.35f));
                    em.rateOverTime = 160f;
                    vel.x = new ParticleSystem.MinMaxCurve(-5f, -4f);
                    vel.y = new ParticleSystem.MinMaxCurve(-24f, -19f);
                    vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                    noise.enabled = false;
                    var rcol = m_Ps.colorOverLifetime;
                    var rg = new Gradient();
                    rg.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                        new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.1f), new GradientAlphaKey(1, 0.9f), new GradientAlphaKey(0, 1) });
                    rcol.color = rg;
                    break;
                }
                default: // motes
                    r.sharedMaterial = glow;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.24f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.8f, 1f, 0.6f), new Color(0.9f, 0.9f, 1f, 0.35f));
                    em.rateOverTime = 10f;
                    vel.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.1f);
                    vel.y = new ParticleSystem.MinMaxCurve(-0.6f, -0.2f);
                    vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                    noise.strength = 0.5f;
                    break;
            }
            if (m_Cam != null) transform.position = new Vector3(m_Cam.position.x, m_Cam.position.y, 0f);
            m_Ps.Play();
        }

        /// <summary>Spawn area = the whole view plus a margin (cameras zoom out a lot for big puzzle boards).</summary>
        void FitArea()
        {
            float h = m_Camera != null ? m_Camera.orthographicSize : 11f;
            float w = m_Camera != null ? h * m_Camera.aspect : h * 1.78f;
            m_Size = h;
            var shape = m_Ps.shape;
            shape.scale = new Vector3(w * 2f + 8f, h * 2f + 8f, 1f);
        }

        void LateUpdate()
        {
            if (m_Cam != null) transform.position = new Vector3(m_Cam.position.x, m_Cam.position.y, 0f);
            if (m_Camera != null && Mathf.Abs(m_Camera.orthographicSize - m_Size) > 0.5f) FitArea();
        }
    }
}
