using System;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Ping-pong movement between two points with optional easing and end pauses. Exposes velocity for riders.</summary>
    [DefaultExecutionOrder(-50)]
    public class PathMover : MonoBehaviour, IMovingSurface
    {
        public Vector2 a, b;
        public float speed = 2.5f;
        public float pause = 0.4f;
        public bool eased = true;
        public float startPhase;
        public Action<int> onArrive; // 0 = reached a, 1 = reached b

        public Vector2 Velocity { get; private set; }

        Rigidbody2D m_Rb;
        float m_U;
        int m_Dir = 1;
        float m_Wait;
        float m_Length;

        public void Init(Vector2 from, Vector2 to, float spd, float endPause, bool ease, float phase = 0f)
        {
            a = from; b = to; speed = spd; pause = endPause; eased = ease;
            m_Rb = GetComponent<Rigidbody2D>();
            m_Length = Mathf.Max(0.01f, Vector2.Distance(a, b));
            m_U = Mathf.Clamp01(phase);
            Place(Eval(m_U));
        }

        Vector2 Eval(float u) => Vector2.Lerp(a, b, eased ? Mathf.SmoothStep(0f, 1f, u) : u);

        void Place(Vector2 p)
        {
            if (m_Rb != null) m_Rb.position = p;
            transform.position = p;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 before = Eval(m_U);
            if (m_Wait > 0f)
            {
                m_Wait -= dt;
                Velocity = Vector2.zero;
                return;
            }
            // eased paths move a little faster in the middle to keep the same average speed
            m_U += m_Dir * speed * dt / m_Length;
            if (m_U >= 1f) { m_U = 1f; m_Dir = -1; m_Wait = pause; onArrive?.Invoke(1); }
            else if (m_U <= 0f) { m_U = 0f; m_Dir = 1; m_Wait = pause; onArrive?.Invoke(0); }
            Vector2 after = Eval(m_U);
            Velocity = (after - before) / dt;
            if (m_Rb != null) m_Rb.MovePosition(after);
            else transform.position = after;
        }
    }
}
