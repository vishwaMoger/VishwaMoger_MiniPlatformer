using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Stationary plant that turns toward the player and spits seeds.</summary>
    public class Plant : Enemy
    {
        float m_Cd = 1f;
        bool m_Attacking;

        public static Plant Create(Transform parent, Vector2 feet)
        {
            var go = EntityKit.Root("Plant", parent, feet);
            var p = go.AddComponent<Plant>();
            p.SetupVisual("Plant", 21f / 16f, new Vector2(1.3f, 1.8f), new Vector2(0f, 0.9f), "Idle");
            p.Face(-1f);
            return p;
        }

        protected override void Update()
        {
            base.Update();
            if (Dead || m_Attacking) return;
            var p = Player;
            m_Cd -= Time.deltaTime;
            if (p == null) return;
            float dx = p.transform.position.x - transform.position.x;
            float dy = p.transform.position.y - transform.position.y;
            if (Mathf.Abs(dx) < 11f && Mathf.Abs(dy) < 2.5f)
            {
                Face(dx);
                if (m_Cd <= 0f)
                {
                    m_Cd = 1.9f;
                    m_Attacking = true;
                    anim.Play(F("Attack"), 20f, false, () => { m_Attacking = false; anim.Play(F("Idle")); }, true, frame =>
                    {
                        if (frame == 4 && !Dead)
                            Projectile.Fire(transform.parent, (Vector2)transform.position + new Vector2(facing * 1f, 1.05f), new Vector2(facing * 7.5f, 0f), folder);
                    });
                }
            }
        }
    }
}
