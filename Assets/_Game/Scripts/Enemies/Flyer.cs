using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Flying enemies: BlueBird (patrols), Bee (hovers, drops stingers when you pass below),
    /// Bat (sleeps on the ceiling, then chases), Ghost (patrols and phases in and out).
    /// </summary>
    public class Flyer : Enemy
    {
        public enum Kind { BlueBird, Bee, Bat, Ghost }
        Kind m_Kind;
        Vector2 m_Home, m_A, m_B;
        float m_T, m_Speed = 2.2f;
        int m_Dir = 1;
        float m_Timer, m_Cd;
        bool m_Awake, m_Visible = true, m_Attacking;

        public static Flyer Create(Transform parent, Vector2 center, Kind kind, Vector2? pathEnd)
        {
            var go = EntityKit.Root(kind.ToString(), parent, center);
            var f = go.AddComponent<Flyer>();
            f.m_Kind = kind;
            f.m_Home = center;
            f.m_A = center;
            f.m_B = pathEnd ?? center + Vector2.right * (kind == Kind.Bee ? 0f : 4f);
            if (kind == Kind.BlueBird || kind == Kind.Ghost) f.m_A = pathEnd.HasValue ? center : center - Vector2.right * 3f;
            switch (kind)
            {
                case Kind.BlueBird: f.SetupVisual("BlueBird", 0f, new Vector2(1.3f, 1.1f), Vector2.zero, "Flying"); f.m_Speed = 3f; break;
                case Kind.Bee: f.SetupVisual("Bee", 0f, new Vector2(1.3f, 1.3f), Vector2.zero, "Idle"); f.m_Speed = 1.5f; break;
                case Kind.Bat: f.SetupVisual("Bat", 0f, new Vector2(1.5f, 1.1f), Vector2.zero, "Idle"); break;
                case Kind.Ghost: f.SetupVisual("Ghost", 0f, new Vector2(1.4f, 1.3f), Vector2.zero, "Idle"); f.m_Speed = 1.8f; f.m_Timer = Random.Range(2f, 3.5f); break;
            }
            f.SetupFlyerBody();
            f.Face(-1f);
            return f;
        }

        protected override bool CanBeStomped => m_Visible;
        protected override bool IsHarmful => m_Visible;

        protected override void Update()
        {
            base.Update();
            if (Dead) return;
            float dt = Time.deltaTime;
            switch (m_Kind)
            {
                case Kind.Ghost: GhostPhase(dt); break;
                case Kind.Bee: BeeAttack(dt); break;
            }
        }

        void FixedUpdate()
        {
            if (Dead) return;
            float dt = Time.fixedDeltaTime;
            Vector2 pos = rb.position;
            Vector2 next = pos;
            switch (m_Kind)
            {
                case Kind.BlueBird:
                case Kind.Ghost:
                {
                    float len = Mathf.Max(0.5f, Vector2.Distance(m_A, m_B));
                    m_T += m_Dir * m_Speed * dt / len;
                    if (m_T > 1f) { m_T = 1f; m_Dir = -1; }
                    if (m_T < 0f) { m_T = 0f; m_Dir = 1; }
                    next = Vector2.Lerp(m_A, m_B, Mathf.SmoothStep(0f, 1f, m_T)) + Vector2.up * Mathf.Sin(Time.time * 3f) * 0.25f;
                    Face(next.x - pos.x);
                    break;
                }
                case Kind.Bee:
                    next = m_Home + new Vector2(Mathf.Sin(Time.time * 1.3f) * 0.6f, Mathf.Sin(Time.time * 2.6f) * 0.3f);
                    break;
                case Kind.Bat:
                {
                    var p = Player;
                    if (!m_Awake)
                    {
                        if (p != null && Vector2.Distance(p.transform.position, m_Home) < 6.5f)
                        {
                            m_Awake = true;
                            m_Timer = 5f;
                            anim.Play(F("Ceiling Out"), 20f, false, () => anim.Play(F("Flying")));
                        }
                    }
                    else
                    {
                        m_Timer -= dt;
                        Vector2 goal = (p != null && m_Timer > 0f) ? (Vector2)p.transform.position + Vector2.up * 0.4f : m_Home;
                        next = Vector2.MoveTowards(pos, goal, (m_Timer > 0f ? 3.2f : 4f) * dt);
                        next += Vector2.up * Mathf.Sin(Time.time * 6f) * 0.02f;
                        Face(goal.x - pos.x);
                        if (m_Timer <= 0f && (next - m_Home).sqrMagnitude < 0.01f)
                        {
                            m_Awake = false;
                            m_Timer = 2f;
                            anim.Play(F("Ceiling In"), 20f, false, () => anim.Play(F("Idle")));
                        }
                    }
                    break;
                }
            }
            rb.MovePosition(next);
        }

        void GhostPhase(float dt)
        {
            m_Timer -= dt;
            if (m_Timer > 0f) return;
            if (m_Visible)
            {
                m_Visible = false;
                m_Timer = 1.6f;
                anim.Play(F("Desappear"), 20f, false, () => sr.enabled = false);
            }
            else
            {
                m_Visible = true;
                m_Timer = Random.Range(2.5f, 3.5f);
                sr.enabled = true;
                anim.Play(F("Appear"), 20f, false, () => anim.Play(F("Idle")));
            }
            sr.color = new Color(1f, 1f, 1f, 0.9f);
        }

        void BeeAttack(float dt)
        {
            m_Cd -= dt;
            var p = Player;
            if (p == null || m_Attacking || m_Cd > 0f) return;
            float dx = p.transform.position.x - transform.position.x;
            float dy = p.transform.position.y - transform.position.y;
            if (Mathf.Abs(dx) < 1.6f && dy < -0.5f && dy > -10f)
            {
                m_Cd = 1.4f;
                m_Attacking = true;
                anim.Play(F("Attack"), 20f, false, () => { m_Attacking = false; anim.Play(F("Idle")); }, true, frame =>
                {
                    if (frame == 4 && !Dead)
                        Projectile.Fire(transform.parent, (Vector2)transform.position + Vector2.down * 0.7f, new Vector2(0f, -8f), folder);
                });
            }
        }
    }
}
