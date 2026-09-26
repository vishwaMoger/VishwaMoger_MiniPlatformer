using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Restores one heart. Stays in the level if the player is already at full health.</summary>
    public class HeartPickup : MonoBehaviour
    {
        Transform m_Visual;
        bool m_Taken;

        public static HeartPickup Create(Transform parent, Vector2 pos)
        {
            var go = EntityKit.Root("Heart", parent, pos, Layers.Pickup);
            var h = go.AddComponent<HeartPickup>();
            var sr = new GameObject("Sprite").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(go.transform, false);
            sr.sprite = PixelArt.Heart;
            sr.sharedMaterial = GameAssets.I.spriteGlow;
            sr.sortingOrder = Sort.Items;
            h.m_Visual = sr.transform;
            FxSystem.Halo(h.m_Visual, new Color(1f, 0.3f, 0.35f, 0.6f), 1.1f);
            FxSystem.PointLight(go.transform, new Color(1f, 0.4f, 0.45f), 0.6f, 2.5f);
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = 0.5f;
            c.isTrigger = true;
            return h;
        }

        void Update()
        {
            if (m_Taken) return;
            float t = Time.time;
            m_Visual.localPosition = new Vector3(0f, Mathf.Sin(t * 2.5f) * 0.12f, 0f);
            float beat = 1f + Mathf.Max(0f, Mathf.Sin(t * 6f)) * 0.08f;
            m_Visual.localScale = Vector3.one * beat;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (m_Taken) return;
            var p = EntityKit.PlayerFrom(other);
            if (p == null || p.IsDead) return;
            var s = LevelSession.Current;
            if (s == null || !s.AddHeart()) return;
            m_Taken = true;
            AudioManager.I?.Play("heart", 0.8f, 0f);
            FxSystem.Sparkle(transform.position, new Color(1f, 0.4f, 0.5f), 14, 4f, 0.7f, 0.6f);
            Tween.To(this, 0.25f, k => { if (m_Visual) m_Visual.localScale = Vector3.one * (1f + k); }, null, false, () => Destroy(gameObject));
        }
    }
}
