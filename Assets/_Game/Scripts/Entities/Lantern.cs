using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MiniPlatformer
{
    /// <summary>Decorative hanging lantern with a flickering warm light.</summary>
    public class Lantern : MonoBehaviour
    {
        Light2D m_Light;
        SpriteRenderer m_Halo;
        float m_Seed;

        public static Lantern Create(Transform parent, Vector2 pos)
        {
            var go = EntityKit.Root("Lantern", parent, pos);
            var l = go.AddComponent<Lantern>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArt.Lantern;
            sr.sharedMaterial = GameAssets.I.spriteGlow;
            sr.sortingOrder = Sort.Decor + 1;
            l.m_Light = FxSystem.PointLight(go.transform, new Color(1f, 0.72f, 0.4f), 1.1f, 6f, 0.5f);
            l.m_Halo = FxSystem.Halo(go.transform, new Color(1f, 0.7f, 0.35f, 0.35f), 2.2f, Sort.Decor);
            l.m_Seed = Random.value * 10f;
            return l;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(m_Seed, Time.time * 3f);
            m_Light.intensity = 0.9f + n * 0.45f;
            m_Halo.transform.localScale = Vector3.one * (2f + n * 0.4f);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.3f + m_Seed) * 4f);
        }
    }
}
