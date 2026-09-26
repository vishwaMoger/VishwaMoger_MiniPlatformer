using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Breakable crate: hit it from below or stomp it to smash it open and release fruit.</summary>
    public class Box : MonoBehaviour
    {
        int m_Hits;
        int m_Fruits;
        string m_Folder;
        SpriteAnim m_Anim;
        float m_Cooldown;
        bool m_Broken;

        public static Box Create(Transform parent, Vector2 cellCenter, bool big)
        {
            var go = EntityKit.Root("Box", parent, cellCenter, Layers.Ground);
            var b = go.AddComponent<Box>();
            b.m_Folder = big ? "PA1/Items/Boxes/Box3/" : "PA1/Items/Boxes/Box1/";
            b.m_Hits = big ? 3 : 1;
            b.m_Fruits = big ? 5 : 3;
            b.m_Anim = EntityKit.Sprite(go, GameAssets.I.Frames(b.m_Folder + "Idle"), Sort.Items - 1, new Vector2(0f, 0.1f));
            EntityKit.Box(go, new Vector2(1.2f, 1.2f), new Vector2(0f, 0.05f), false);
            return b;
        }

        public static int FruitCount(bool big) => big ? 5 : 3;

        void OnCollisionEnter2D(Collision2D c)
        {
            if (m_Broken || m_Cooldown > Time.time) return;
            var p = c.collider.GetComponent<PlayerController>();
            if (p == null) return;
            var b = GetComponent<Collider2D>().bounds;
            float pv = p.PrevVelocity.y;
            if (p.Col.bounds.max.y <= b.min.y + 0.2f && pv > 1f) Hit(p, false);          // head bump
            else if (p.FeetY >= b.max.y - 0.2f && pv < -4f) Hit(p, true);                 // stomp
        }

        void Hit(PlayerController p, bool fromAbove)
        {
            m_Cooldown = Time.time + 0.15f;
            m_Hits--;
            if (fromAbove) p.Bounce(11f, 15f);
            CameraRig.I?.Shake(0.1f);
            FxSystem.HitStop(0.04f);
            Tween.Punch(m_Anim.transform, 0.25f, 0.2f, false);
            if (m_Hits > 0)
            {
                var a = GameAssets.I;
                m_Anim.Play(a.Frames(m_Folder + "Hit"), 20f, false, () => m_Anim.Play(a.Frames(m_Folder + "Idle")), true);
                AudioManager.I?.Play("boxhit", 0.8f, 0.1f);
                return;
            }
            Break();
        }

        void Break()
        {
            m_Broken = true;
            AudioManager.I?.Play("break", 0.8f, 0.05f);
            Vector2 pos = transform.position;
            FxSystem.Debris(pos, new Color32(184, 111, 80, 255), 14, 6f, 0.3f);
            FxSystem.Dust(pos, 6, 2f, 1f);
            // Pixel Adventure break pieces fly out
            var pieces = GameAssets.I.Frames(m_Folder + "Break");
            for (int i = 0; i < pieces.Length; i++)
            {
                var go = new GameObject("Piece");
                go.transform.position = pos;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = pieces[i];
                sr.sharedMaterial = GameAssets.I.spriteLit;
                sr.sortingOrder = Sort.FX;
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 3f;
                rb.linearVelocity = new Vector2((i % 2 == 0 ? -1 : 1) * Random.Range(2f, 5f), Random.Range(6f, 10f));
                rb.angularVelocity = Random.Range(-720f, 720f);
                go.layer = Layers.Projectile; // collides with nothing that matters
                Destroy(go, 1.2f);
            }
            for (int i = 0; i < m_Fruits; i++)
            {
                float ang = Mathf.Lerp(40f, 140f, m_Fruits == 1 ? 0.5f : i / (float)(m_Fruits - 1)) * Mathf.Deg2Rad;
                var target = pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 1.6f;
                var f = Fruit.Create(transform.parent, pos, i + (int)pos.x, LevelSession.Current != null && LevelSession.Current.world.playerLight > 0f, 0.35f);
                var t = f.transform;
                Tween.To(f, 0.35f, k => { if (t) t.position = Vector2.Lerp(pos, target, k); }, Ease.OutBack, false);
            }
            Destroy(gameObject);
        }
    }
}
