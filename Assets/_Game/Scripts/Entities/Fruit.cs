using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Collectible fruit with bobbing, a soft glow and a sparkle burst on pickup.</summary>
    public class Fruit : MonoBehaviour
    {
        public static readonly string[] Kinds = { "Apple", "Bananas", "Cherries", "Kiwi", "Melon", "Orange", "Pineapple", "Strawberry" };
        static readonly Color[] KindColors =
        {
            new Color(1f, 0.35f, 0.3f), new Color(1f, 0.9f, 0.3f), new Color(1f, 0.3f, 0.4f), new Color(0.6f, 0.9f, 0.3f),
            new Color(0.5f, 1f, 0.5f), new Color(1f, 0.65f, 0.2f), new Color(1f, 0.85f, 0.3f), new Color(1f, 0.35f, 0.45f),
        };

        SpriteAnim m_Anim;
        Transform m_Visual;
        Vector3 m_Base;
        float m_Phase;
        bool m_Taken;
        int m_Kind;
        float m_Delay;

        public static Fruit Create(Transform parent, Vector2 pos, int kind, bool glowHalo, float pickupDelay = 0f)
        {
            var go = EntityKit.Root("Fruit", parent, pos, Layers.Pickup);
            var f = go.AddComponent<Fruit>();
            f.m_Kind = Mathf.Abs(kind) % Kinds.Length;
            f.m_Anim = EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Items/Fruits/" + Kinds[f.m_Kind]), Sort.Items, default, true);
            f.m_Anim.RandomizeStart();
            f.m_Visual = f.m_Anim.transform;
            if (glowHalo) FxSystem.Halo(f.m_Visual, KindColors[f.m_Kind] * 0.5f, 0.9f);
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = 0.45f;
            c.isTrigger = true;
            f.m_Phase = pos.x * 0.7f;
            f.m_Delay = pickupDelay;
            return f;
        }

        void Update()
        {
            if (m_Taken) return;
            m_Delay -= Time.deltaTime;
            m_Visual.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 3f + m_Phase) * 0.1f, 0f);
        }

        void OnTriggerEnter2D(Collider2D other) => TryTake(other);
        void OnTriggerStay2D(Collider2D other) => TryTake(other);

        void TryTake(Collider2D other)
        {
            if (m_Taken || m_Delay > 0f) return;
            var p = EntityKit.PlayerFrom(other);
            if (p == null || p.IsDead) return;
            m_Taken = true;
            GetComponent<Collider2D>().enabled = false;
            LevelSession.Current?.CollectFruit();
            AudioManager.I?.PlayCollect();
            FxSystem.Sparkle(transform.position, KindColors[m_Kind], 7, 3.2f, 0.5f, 0.45f);
            foreach (Transform child in m_Visual) Destroy(child.gameObject);
            m_Anim.Play(GameAssets.I.Frames("PA1/Items/Fruits/Collected"), 20f, false, () => Destroy(gameObject));
            Tween.Punch(m_Visual, 0.35f, 0.2f, false);
        }
    }
}
