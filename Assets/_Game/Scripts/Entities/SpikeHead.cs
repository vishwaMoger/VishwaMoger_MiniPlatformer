using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Thwomp-style crusher: slams down when the player passes underneath, then slowly rises.</summary>
    public class SpikeHead : MonoBehaviour
    {
        enum State { Idle, Falling, Landed, Rising }
        State m_State;
        Vector2 m_Home;
        float m_Vel, m_Timer, m_Blink;
        SpriteAnim m_Anim;
        Rigidbody2D m_Rb;
        const float Half = 1.25f;

        public static SpikeHead Create(Transform parent, Vector2 pos)
        {
            var go = EntityKit.Root("SpikeHead", parent, pos, Layers.Hazard);
            var s = go.AddComponent<SpikeHead>();
            s.m_Home = pos;
            s.m_Anim = EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Traps/Spike Head/Idle"), Sort.Traps + 1);
            EntityKit.Box(go, new Vector2(2.3f, 2.3f), Vector2.zero, true);
            go.AddComponent<Hazard>();
            s.m_Rb = EntityKit.Kinematic(go);
            s.m_Blink = Random.Range(1f, 3f);
            return s;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var a = GameAssets.I;
            Vector2 pos = m_Rb.position;
            switch (m_State)
            {
                case State.Idle:
                    m_Blink -= dt;
                    if (m_Blink <= 0f)
                    {
                        m_Blink = Random.Range(2f, 4f);
                        m_Anim.Play(a.Frames("PA1/Traps/Spike Head/Blink"), 20f, false, () => m_Anim.Play(a.Frames("PA1/Traps/Spike Head/Idle")));
                    }
                    var p = PlayerController.I;
                    if (p != null && !p.IsDead && Mathf.Abs(p.transform.position.x - pos.x) < 1.9f && p.transform.position.y < pos.y &&
                        pos.y - p.transform.position.y < 14f && !Physics2D.Linecast(pos + Vector2.down * Half, p.transform.position, Layers.SolidMask))
                    {
                        m_State = State.Falling;
                        m_Vel = 0f;
                    }
                    break;
                case State.Falling:
                    m_Vel = Mathf.Min(m_Vel + 60f * dt, 26f);
                    float step = m_Vel * dt;
                    var hit = Physics2D.BoxCast(pos, new Vector2(2.2f, 2.2f), 0f, Vector2.down, step + 0.05f, Layers.GroundMask);
                    if (hit.collider != null)
                    {
                        pos.y -= Mathf.Max(0f, hit.distance - 0.05f);
                        m_Rb.MovePosition(pos);
                        m_State = State.Landed;
                        m_Timer = 0.9f;
                        m_Anim.Play(a.Frames("PA1/Traps/Spike Head/Bottom Hit"), 20f, false, () => m_Anim.Play(a.Frames("PA1/Traps/Spike Head/Idle")));
                        CameraRig.I?.Shake(DistanceShake(0.4f));
                        FxSystem.Dust(pos + Vector2.down * Half, 10, 3f, 0.6f);
                        AudioManager.I?.Play("slam", Mathf.Clamp01(DistanceShake(1f) * 2f), 0.05f);
                    }
                    else m_Rb.MovePosition(pos + Vector2.down * step);
                    if (pos.y < -20f) { m_State = State.Rising; }
                    break;
                case State.Landed:
                    m_Timer -= dt;
                    if (m_Timer <= 0f) m_State = State.Rising;
                    break;
                case State.Rising:
                    pos = Vector2.MoveTowards(pos, m_Home, 3.5f * dt);
                    m_Rb.MovePosition(pos);
                    if ((pos - m_Home).sqrMagnitude < 0.0001f) { m_State = State.Idle; m_Timer = 0.4f; }
                    break;
            }
        }

        float DistanceShake(float amount)
        {
            var cam = CameraRig.I;
            if (cam == null) return 0f;
            float d = Vector2.Distance(cam.Position, m_Rb.position);
            return amount * Mathf.Clamp01(1f - d / 16f);
        }
    }
}
