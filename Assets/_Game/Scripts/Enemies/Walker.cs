using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Ground patroller that turns at walls and ledges. Variants: Mushroom, Slime, Snail (plain), Chicken (chases),
    /// AngryPig (2 hits, enraged after the first), Bunny (hops), Trunk (stops to shoot).
    /// </summary>
    public class Walker : Enemy
    {
        public enum Kind { Mushroom, Chicken, Slime, AngryPig, Bunny, Snail, Trunk }

        Kind m_Kind;
        float m_Speed;
        float m_Pause;
        float m_HopTimer, m_ShootCd;
        bool m_Angry, m_Shooting;
        float m_HalfWidth;
        string m_Move = "Run", m_Idle = "Idle";

        public static Walker Create(Transform parent, Vector2 feet, Kind kind)
        {
            var go = EntityKit.Root(kind.ToString(), parent, feet);
            var w = go.AddComponent<Walker>();
            w.m_Kind = kind;
            w.Build();
            return w;
        }

        void Build()
        {
            // sprite center y = frameHeight/2 - bottom padding (px / 16)
            switch (m_Kind)
            {
                case Kind.Mushroom:
                    SetupVisual("Mushroom", 1f, new Vector2(1.2f, 0.95f), new Vector2(0f, 0.5f), "Run");
                    SetupWalkerBody(new Vector2(1.1f, 1f)); m_Speed = 2.2f; break;
                case Kind.Chicken:
                    SetupVisual("Chicken", 17f / 16f - 2f / 16f, new Vector2(1.1f, 1.4f), new Vector2(0f, 0.72f), "Run");
                    SetupWalkerBody(new Vector2(1.1f, 1.5f)); m_Speed = 2.6f; break;
                case Kind.Slime:
                    SetupVisual("Slime", 15f / 16f, new Vector2(1.7f, 1f), new Vector2(0f, 0.55f), "Idle-Run");
                    SetupWalkerBody(new Vector2(1.7f, 1f)); m_Speed = 1.3f; m_Move = "Idle-Run"; m_Idle = "Idle-Run"; break;
                case Kind.AngryPig:
                    SetupVisual("AngryPig", 15f / 16f - 2f / 16f, new Vector2(1.4f, 1.3f), new Vector2(0f, 0.68f), "Walk");
                    SetupWalkerBody(new Vector2(1.3f, 1.4f)); m_Speed = 1.8f; m_Move = "Walk"; hp = 2; break;
                case Kind.Bunny:
                    SetupVisual("Bunny", 22f / 16f - 3f / 16f, new Vector2(1.1f, 1.8f), new Vector2(0f, 0.9f), "Run");
                    SetupWalkerBody(new Vector2(1.1f, 1.9f)); m_Speed = 3f; m_HopTimer = Random.Range(0.8f, 1.6f); break;
                case Kind.Snail:
                    SetupVisual("Snail", 12f / 16f, new Vector2(1.6f, 1f), new Vector2(0f, 0.5f), "Walk");
                    SetupWalkerBody(new Vector2(1.6f, 1.1f)); m_Speed = 1f; m_Move = "Walk"; break;
                case Kind.Trunk:
                    SetupVisual("Trunk", 1f, new Vector2(1.6f, 1.45f), new Vector2(0f, 0.72f), "Run");
                    SetupWalkerBody(new Vector2(1.6f, 1.5f)); m_Speed = 1.6f; m_ShootCd = 1f; break;
            }
            m_HalfWidth = ((BoxCollider2D)body).size.x * 0.5f;
            Face(-1f);
        }

        protected override void Update()
        {
            base.Update();
            if (Dead || m_Shooting) return;
            bool moving = m_Pause <= 0f && Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            if (m_Kind == Kind.Bunny && !Grounded())
                anim.Play(F(rb.linearVelocity.y > 0 ? "Jump" : "Fall"));
            else
                anim.Play(F(moving ? (m_Angry ? "Run" : m_Move) : m_Idle));
        }

        bool Grounded() => Physics2D.Raycast((Vector2)transform.position + Vector2.up * 0.1f, Vector2.down, 0.2f, Layers.GroundMask);

        void FixedUpdate()
        {
            if (Dead || rb == null) return;
            float dt = Time.fixedDeltaTime;
            var v = rb.linearVelocity;
            var p = Player;
            float speed = m_Angry ? m_Speed * 2.6f : m_Speed;

            if (m_Kind == Kind.Chicken && p != null)
            {
                float dx = p.transform.position.x - transform.position.x;
                float dy = p.transform.position.y - transform.position.y;
                if (Mathf.Abs(dx) < 8f && Mathf.Abs(dy) < 2f) { Face(dx); speed = 5.5f; m_Pause = 0f; }
            }

            if (m_Kind == Kind.Trunk && p != null && !m_Shooting)
            {
                m_ShootCd -= dt;
                float dx = p.transform.position.x - transform.position.x;
                float dy = p.transform.position.y - transform.position.y;
                if (m_ShootCd <= 0f && Mathf.Abs(dx) < 10f && Mathf.Abs(dy) < 1.8f && Mathf.Sign(dx) == facing)
                {
                    m_ShootCd = 2.2f;
                    m_Shooting = true;
                    rb.linearVelocity = new Vector2(0f, v.y);
                    anim.Play(F("Attack"), 20f, false, () => { m_Shooting = false; }, true, frame =>
                    {
                        if (frame == 9 && !Dead)
                            Projectile.Fire(transform.parent, (Vector2)transform.position + new Vector2(facing * 1.1f, 0.75f), new Vector2(facing * 7f, 0f), folder);
                    });
                    return;
                }
            }
            if (m_Shooting) { rb.linearVelocity = new Vector2(0f, v.y); return; }

            if (m_Pause > 0f)
            {
                m_Pause -= dt;
                v.x = 0f;
                if (m_Pause <= 0f) Face(-facing);
            }
            else
            {
                Vector2 pos = transform.position;
                bool grounded = Grounded();
                bool wall = Physics2D.Raycast(pos + Vector2.up * 0.5f, Vector2.right * facing, m_HalfWidth + 0.15f, Layers.SolidMask);
                bool ledge = grounded && !Physics2D.Raycast(pos + new Vector2(facing * (m_HalfWidth + 0.1f), 0.2f), Vector2.down, 0.8f, Layers.GroundMask);
                if (wall || ledge)
                {
                    m_Pause = m_Angry || m_Kind == Kind.Chicken ? 0.05f : 0.5f;
                    v.x = 0f;
                }
                else v.x = facing * speed;

                if (m_Kind == Kind.Bunny && grounded)
                {
                    m_HopTimer -= dt;
                    if (m_HopTimer <= 0f) { m_HopTimer = Random.Range(1.1f, 1.8f); v.y = 9.5f; }
                }
            }
            rb.linearVelocity = v;
        }

        protected override bool CanBeStomped => true;

        protected override void OnHurt()
        {
            if (m_Kind == Kind.AngryPig)
            {
                m_Angry = true;
                anim.Play(F("Hit 1"), 20f, false);
                m_Pause = 0.3f;
            }
        }

        protected override string HitAnim => m_Kind == Kind.AngryPig ? "Hit 2" : "Hit";
    }
}
