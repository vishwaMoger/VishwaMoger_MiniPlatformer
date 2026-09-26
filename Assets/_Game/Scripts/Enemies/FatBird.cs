using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Hovers high up and belly-flops onto the player, then flutters back up. Safe to stomp while hovering.</summary>
    public class FatBird : Enemy
    {
        enum State { Hover, Fall, Ground, Rise }
        State m_State;
        Vector2 m_Home;
        float m_Vel, m_Timer;

        public static FatBird Create(Transform parent, Vector2 center)
        {
            var go = EntityKit.Root("FatBird", parent, center);
            var f = go.AddComponent<FatBird>();
            f.m_Home = center;
            f.SetupVisual("FatBird", 0f, new Vector2(1.8f, 1.8f), new Vector2(0f, -0.2f), "Idle");
            f.SetupFlyerBody();
            return f;
        }

        protected override bool CanBeStomped => m_State == State.Hover || m_State == State.Rise || m_State == State.Ground;

        void FixedUpdate()
        {
            if (Dead) return;
            float dt = Time.fixedDeltaTime;
            Vector2 pos = rb.position;
            switch (m_State)
            {
                case State.Hover:
                    anim.Play(F("Idle"));
                    rb.MovePosition(m_Home + Vector2.up * Mathf.Sin(Time.time * 2.5f) * 0.2f);
                    m_Timer -= dt;
                    var p = Player;
                    if (p != null && m_Timer <= 0f && Mathf.Abs(p.transform.position.x - pos.x) < 1.6f && p.transform.position.y < pos.y - 0.5f &&
                        pos.y - p.transform.position.y < 12f)
                    {
                        m_State = State.Fall;
                        m_Vel = 0f;
                        anim.Play(F("Fall"));
                    }
                    break;
                case State.Fall:
                    m_Vel = Mathf.Min(m_Vel + 55f * dt, 24f);
                    var hit = Physics2D.BoxCast(pos, new Vector2(1.6f, 1.6f), 0f, Vector2.down, m_Vel * dt + 0.05f, Layers.GroundMask);
                    if (hit.collider != null)
                    {
                        rb.MovePosition(pos + Vector2.down * Mathf.Max(0f, hit.distance - 0.02f));
                        m_State = State.Ground;
                        m_Timer = 1f;
                        anim.Play(F("Ground"), 20f, false);
                        CameraRig.I?.Shake(0.22f);
                        FxSystem.Dust(pos + Vector2.down * 1f, 8, 2.5f, 0.5f);
                        AudioManager.I?.Play("slam", 0.6f);
                    }
                    else rb.MovePosition(pos + Vector2.down * m_Vel * dt);
                    break;
                case State.Ground:
                    m_Timer -= dt;
                    if (m_Timer <= 0f) { m_State = State.Rise; anim.Play(F("Idle")); }
                    break;
                case State.Rise:
                    rb.MovePosition(Vector2.MoveTowards(pos, m_Home, 3f * dt));
                    if ((pos - m_Home).sqrMagnitude < 0.01f) { m_State = State.Hover; m_Timer = 0.8f; }
                    break;
            }
        }
    }
}
