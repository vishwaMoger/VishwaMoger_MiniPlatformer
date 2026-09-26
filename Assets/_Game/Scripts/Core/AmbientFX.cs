using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>World-themed atmospheric particles (pollen, leaves, petals, fireflies, motes) that follow the camera.</summary>
    public class AmbientFX : MonoBehaviour
    {
        ParticleSystem m_Ps;
        Transform m_Cam;

        public static AmbientFX Create(Transform parent, Ambient kind, Transform cam)
        {
            var go = new GameObject("Ambient_" + kind);
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<AmbientFX>();
            fx.m_Cam = cam;
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
            shape.scale = new Vector3(40f, 22f, 1f);
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

        void LateUpdate()
        {
            if (m_Cam != null) transform.position = new Vector3(m_Cam.position.x, m_Cam.position.y, 0f);
        }
    }
}
