using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Floor fan that pushes the player upward inside its air column (visualised with wind streaks).</summary>
    public class Fan : MonoBehaviour
    {
        public const float LiftVelocity = 11f;

        public static Fan Create(Transform parent, Vector2 cellBottom, float height)
        {
            var go = EntityKit.Root("Fan", parent, cellBottom, Layers.Pickup);
            var f = go.AddComponent<Fan>();
            var anim = EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Traps/Fan/On"), Sort.Traps, new Vector2(0f, 0.25f));
            anim.Speed = 2f;
            EntityKit.Box(go, new Vector2(1.3f, height), new Vector2(0f, height * 0.5f + 0.3f), true);

            // wind streak particles
            var psGo = new GameObject("Wind");
            psGo.transform.SetParent(go.transform, false);
            psGo.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            var ps = psGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = height / 7f;
            main.startSpeed = 7f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
            main.startColor = new Color(1f, 1f, 1f, 0.45f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 16f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.1f, 0.1f, 0.1f);
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = psGo.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 3f;
            r.sharedMaterial = new Material(GameAssets.I.additive) { mainTexture = PixelArt.Pixel.texture };
            r.sortingOrder = Sort.FX - 1;
            ps.Play();
            return f;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            var p = EntityKit.PlayerFrom(other);
            if (p != null && !p.IsDead) p.ApplyWind(LiftVelocity);
        }
    }
}
