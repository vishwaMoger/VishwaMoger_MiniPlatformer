using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Marks a collider that kills the hero on touch.</summary>
    public class TrekDanger : MonoBehaviour { }

    /// <summary>The trophy in the exit room.</summary>
    public class TrekGoal : MonoBehaviour
    {
        SpriteAnim m_Anim;
        bool m_Done;

        public static TrekGoal Create(Transform parent, Vector2 localFeet)
        {
            var go = EntityKit.Root("Goal", parent, Vector2.zero, Layers.Pickup);
            go.transform.localPosition = localFeet;
            var g = go.AddComponent<TrekGoal>();
            g.m_Anim = EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Items/Checkpoints/End/End (Idle)"), Sort.Items - 2, new Vector2(0f, 1.5f), false);
            g.m_Anim.transform.localScale = Vector3.one * 0.95f;
            EntityKit.Box(go, new Vector2(1.3f, 1.8f), new Vector2(0f, 0.9f), true);
            return g;
        }

        public void Reach()
        {
            if (m_Done) return;
            m_Done = true;
            m_Anim.Play(GameAssets.I.Frames("PA1/Items/Checkpoints/End/End (Pressed)"), 20f, false);
            FxSystem.Confetti((Vector2)transform.position + Vector2.up * 1.5f, 80, 12f);
        }
    }

    /// <summary>Spinning coin: currency, plus it marks the route.</summary>
    public class TrekCoin : MonoBehaviour
    {
        Transform m_Visual;
        float m_Phase;
        bool m_Taken;

        public static TrekCoin Create(Transform parent, Vector2 local, bool glow)
        {
            var go = EntityKit.Root("Coin", parent, Vector2.zero, Layers.Pickup);
            go.transform.localPosition = local;
            var c = go.AddComponent<TrekCoin>();
            var vis = new GameObject("Sprite");
            vis.transform.SetParent(go.transform, false);
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = UIArt.Coin;
            sr.sharedMaterial = glow ? GameAssets.I.spriteGlow : GameAssets.I.spriteLit;
            sr.sortingOrder = Sort.Items;
            c.m_Visual = vis.transform;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;
            col.isTrigger = true;
            c.m_Phase = local.x + local.y;
            return c;
        }

        void Update()
        {
            float t = Time.time * 3.2f + m_Phase;
            m_Visual.localScale = new Vector3(0.46f * Mathf.Max(0.12f, Mathf.Abs(Mathf.Cos(t))), 0.46f, 1f);
            m_Visual.localPosition = new Vector3(0f, Mathf.Sin(t * 0.6f) * 0.08f, 0f);
        }

        public bool Take()
        {
            if (m_Taken) return false;
            m_Taken = true;
            FxSystem.Sparkle(transform.position, new Color(1f, 0.85f, 0.3f), 6, 3f, 0.5f, 0.4f);
            Destroy(gameObject);
            return true;
        }
    }

    /// <summary>Hazards built from the trap art. All live in room-local space so they slide with their room.</summary>
    public static class TrekHazards
    {
        public static void Spike(Transform parent, Vector2 cellCenter)
        {
            var go = EntityKit.Root("Spike", parent, Vector2.zero, Layers.Hazard);
            go.transform.localPosition = cellCenter;
            EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Traps/Spikes/Idle"), Sort.Traps);
            EntityKit.Box(go, new Vector2(0.85f, 0.4f), new Vector2(0f, -0.28f), true);
            go.AddComponent<TrekDanger>();
        }

        public static void Saw(Transform parent, Vector2 a, Vector2 b, float speed)
        {
            var chainSprite = GameAssets.I.Sprite("PA1/Traps/Saw/Chain");
            if (chainSprite != null)
                for (int i = 0; i <= 8; i++)
                {
                    var link = EntityKit.StaticSprite(parent, chainSprite, Sort.Chains, Vector2.zero, new Color(1, 1, 1, 0.75f));
                    link.transform.localPosition = Vector2.Lerp(a, b, i / 8f);
                }
            var go = EntityKit.Root("Saw", parent, Vector2.zero, Layers.Hazard);
            go.transform.localPosition = a;
            var anim = EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Traps/Saw/On"), Sort.Traps, default, true);
            anim.Speed = 1.5f;
            anim.transform.localScale = Vector3.one * 0.8f;
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = 0.7f;
            c.isTrigger = true;
            go.AddComponent<TrekDanger>();
            go.AddComponent<TrekPingPong>().Init(a, b, speed);
        }

        public static void Fire(Transform parent, Vector2 floorCenter, float phase)
        {
            var go = EntityKit.Root("Fire", parent, Vector2.zero, Layers.Hazard);
            go.transform.localPosition = floorCenter;
            var anim = EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Traps/Fire/Off"), Sort.Traps, new Vector2(0f, 0.5f), true);
            var col = EntityKit.Box(go, new Vector2(0.6f, 1.3f), new Vector2(0f, 1f), true);
            go.AddComponent<TrekDanger>();
            go.AddComponent<TrekFire>().Init(anim, col, phase);
        }
    }

    /// <summary>Moves back and forth between two local points.</summary>
    public class TrekPingPong : MonoBehaviour
    {
        Vector2 m_A, m_B;
        float m_Speed, m_T;

        public void Init(Vector2 a, Vector2 b, float speed) { m_A = a; m_B = b; m_Speed = speed; }

        void Update()
        {
            float len = Mathf.Max(0.01f, Vector2.Distance(m_A, m_B));
            m_T += Time.deltaTime * m_Speed / len;
            float k = Mathf.PingPong(m_T, 1f);
            transform.localPosition = Vector2.Lerp(m_A, m_B, k * k * (3f - 2f * k));
        }
    }

    /// <summary>Fire jet that cycles on and off; deadly only while burning.</summary>
    public class TrekFire : MonoBehaviour
    {
        SpriteAnim m_Anim;
        Collider2D m_Col;
        float m_T;
        bool m_On;

        public void Init(SpriteAnim anim, Collider2D col, float phase) { m_Anim = anim; m_Col = col; m_T = phase; Set(false); }

        void Set(bool on)
        {
            m_On = on;
            m_Col.enabled = on;
            m_Anim.Play(GameAssets.I.Frames(on ? "PA1/Traps/Fire/On" : "PA1/Traps/Fire/Off"));
        }

        void Update()
        {
            m_T += Time.deltaTime;
            bool on = Mathf.Repeat(m_T, 2.4f) < 1.2f;
            if (on != m_On) Set(on);
        }
    }

    /// <summary>A Pixel Adventure enemy patrolling a room floor. Touching it is deadly; landing on it squashes it.</summary>
    public class TrekCritter : MonoBehaviour
    {
        static readonly (string folder, string move, float spriteY, float w, float h, float speed)[] Kinds =
        {
            ("Mushroom", "Run", 1f, 1.1f, 0.9f, 1.6f),
            ("Chicken", "Run", 15f / 16f, 1f, 1.3f, 2.2f),
            ("AngryPig", "Walk", 13f / 16f, 1.3f, 1.2f, 1.5f),
            ("Slime", "Idle-Run", 15f / 16f, 1.5f, 0.9f, 1.1f),
            ("Snail", "Walk", 12f / 16f, 1.4f, 0.9f, 0.9f),
        };

        SpriteAnim m_Anim;
        float m_A, m_B, m_Speed, m_Dir = 1f;
        public bool Dead { get; private set; }
        string m_Folder;

        public static TrekCritter Create(Transform parent, int variant, float minX, float maxX)
        {
            var k = Kinds[Mathf.Abs(variant) % Kinds.Length];
            var go = EntityKit.Root(k.folder, parent, Vector2.zero, Layers.EnemyHit);
            go.transform.localPosition = new Vector2((minX + maxX) * 0.5f, 1f);
            var c = go.AddComponent<TrekCritter>();
            c.m_Folder = "PA2/Enemies/" + k.folder + "/";
            c.m_Anim = EntityKit.Sprite(go, GameAssets.I.Frames(c.m_Folder + k.move), Sort.Enemies, new Vector2(0f, k.spriteY));
            c.m_Anim.RandomizeStart();
            EntityKit.Box(go, new Vector2(k.w, k.h), new Vector2(0f, k.h * 0.5f), true);
            c.m_A = minX + k.w * 0.5f;
            c.m_B = maxX - k.w * 0.5f;
            c.m_Speed = k.speed;
            c.m_Dir = variant % 2 == 0 ? 1f : -1f;
            return c;
        }

        void Update()
        {
            if (Dead) return;
            var p = transform.localPosition;
            p.x += m_Dir * m_Speed * Time.deltaTime;
            if (p.x > m_B) { p.x = m_B; m_Dir = -1f; }
            if (p.x < m_A) { p.x = m_A; m_Dir = 1f; }
            transform.localPosition = p;
            m_Anim.Renderer.flipX = m_Dir > 0f; // Pixel Adventure enemies face left
        }

        public void Squash()
        {
            if (Dead) return;
            Dead = true;
            GetComponent<Collider2D>().enabled = false;
            AudioManager.I?.Play("stomp", 0.85f, 0.08f);
            var hit = GameAssets.I.Frames(m_Folder + "Hit");
            if (hit == null || hit.Length == 0) hit = GameAssets.I.Frames(m_Folder + "Hit 1");
            if (hit == null || hit.Length == 0) { Destroy(gameObject); return; }
            m_Anim.Play(hit, 20f, false, () => { if (this != null) Destroy(gameObject); });
        }
    }
}
