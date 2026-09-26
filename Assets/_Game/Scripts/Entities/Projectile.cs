using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Enemy bullet (plant / trunk / bee). Breaks into Pixel Adventure pieces on impact.</summary>
    public class Projectile : MonoBehaviour
    {
        Vector2 m_Vel;
        float m_Life = 5f;
        string m_Folder;
        bool m_Dead;

        public static Projectile Fire(Transform parent, Vector2 pos, Vector2 velocity, string enemyFolder)
        {
            var go = EntityKit.Root("Bullet", parent, pos, Layers.Projectile);
            var p = go.AddComponent<Projectile>();
            p.m_Vel = velocity;
            p.m_Folder = enemyFolder;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameAssets.I.Sprite(enemyFolder + "/Bullet");
            sr.sharedMaterial = GameAssets.I.spriteGlow;
            sr.sortingOrder = Sort.Enemies + 1;
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = 0.25f;
            c.isTrigger = true;
            EntityKit.Kinematic(go);
            AudioManager.I?.Play("shoot", 0.5f, 0.1f);
            return p;
        }

        void FixedUpdate()
        {
            if (m_Dead) return;
            var rb = GetComponent<Rigidbody2D>();
            rb.MovePosition(rb.position + m_Vel * Time.fixedDeltaTime);
            m_Life -= Time.fixedDeltaTime;
            if (m_Life <= 0f) Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (m_Dead) return;
            var p = EntityKit.PlayerFrom(other);
            if (p != null) p.Damage(transform.position);
            else if (other.gameObject.layer != Layers.Ground) return;
            Pop();
        }

        void Pop()
        {
            m_Dead = true;
            var pieces = GameAssets.I.Frames(m_Folder + "/Bullet Pieces");
            for (int i = 0; i < pieces.Length; i++)
            {
                var go = new GameObject("Piece");
                go.transform.position = transform.position;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = pieces[i];
                sr.sharedMaterial = GameAssets.I.spriteLit;
                sr.sortingOrder = Sort.FX;
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 2f;
                go.layer = Layers.Projectile;
                rb.linearVelocity = new Vector2(-m_Vel.normalized.x * 2f + (i == 0 ? -1 : 1), 4f);
                rb.angularVelocity = Random.Range(-500f, 500f);
                Destroy(go, 0.8f);
            }
            Destroy(gameObject);
        }
    }
}
