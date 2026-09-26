using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Charges at full speed when it spots the player on its level; stunned when it slams into a wall.</summary>
    public class Rino : Enemy
    {
        enum State { Idle, Charge, Stunned }
        State m_State;
        float m_Timer;

        public static Rino Create(Transform parent, Vector2 feet)
        {
            var go = EntityKit.Root("Rino", parent, feet);
            var r = go.AddComponent<Rino>();
            r.SetupVisual("Rino", 17f / 16f - 1f / 16f, new Vector2(2.4f, 1.5f), new Vector2(0f, 0.75f), "Idle");
            r.SetupWalkerBody(new Vector2(2.4f, 1.6f));
            r.Face(-1f);
            return r;
        }

        void FixedUpdate()
        {
            if (Dead) return;
            float dt = Time.fixedDeltaTime;
            var v = rb.linearVelocity;
            var p = Player;
            Vector2 pos = transform.position;
            switch (m_State)
            {
                case State.Idle:
                    v.x = 0f;
                    anim.Play(F("Idle"));
                    m_Timer -= dt;
                    if (p != null && m_Timer <= 0f)
                    {
                        float dx = p.transform.position.x - pos.x, dy = p.transform.position.y - pos.y;
                        if (Mathf.Abs(dx) < 10f && dy > -0.8f && dy < 2.2f &&
                            !Physics2D.Linecast(pos + Vector2.up * 0.8f, (Vector2)p.transform.position, Layers.SolidMask))
                        {
                            Face(dx);
                            m_State = State.Charge;
                            FxSystem.Dust(pos, 4, 1.5f, 0.5f);
                        }
                    }
                    break;
                case State.Charge:
                    anim.Play(F("Run"));
                    v.x = Mathf.MoveTowards(v.x, facing * 10f, 30f * dt);
                    bool wall = Physics2D.Raycast(pos + Vector2.up * 0.7f, Vector2.right * facing, 1.35f, Layers.SolidMask);
                    bool ledge = !Physics2D.Raycast(pos + new Vector2(facing * 1.3f, 0.2f), Vector2.down, 0.8f, Layers.GroundMask);
                    if (Random.value < 0.3f) FxSystem.Dust(pos - Vector2.right * facing, 1, 0.5f, 0.3f);
                    if (wall)
                    {
                        m_State = State.Stunned;
                        m_Timer = 1.2f;
                        v.x = -facing * 3f;
                        v.y = 4f;
                        anim.Play(F("Hit Wall"), 20f, false);
                        CameraRig.I?.Shake(0.2f);
                        AudioManager.I?.Play("slam", 0.5f);
                        FxSystem.Dust(pos + new Vector2(facing * 1.2f, 0.7f), 8, 2f, 1f);
                    }
                    else if (ledge) { m_State = State.Idle; m_Timer = 0.8f; v.x = 0f; Face(-facing); }
                    break;
                case State.Stunned:
                    m_Timer -= dt;
                    v.x = Mathf.MoveTowards(v.x, 0f, 10f * dt);
                    if (m_Timer <= 0f) { m_State = State.Idle; m_Timer = 0.5f; Face(-facing); }
                    break;
            }
            rb.linearVelocity = v;
        }
    }
}
