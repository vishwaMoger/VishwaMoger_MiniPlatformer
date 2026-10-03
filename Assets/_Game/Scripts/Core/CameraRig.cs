using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Smooth platformer camera: horizontal look-ahead, vertical "platform snapping" dead-zone,
    /// level-bounds clamping, trauma-based screen shake and zoom punches.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig I { get; private set; }
        public Camera Cam { get; private set; }

        public Transform target;
        public Rect bounds = new Rect(0, 0, 200, 40);
        public bool clampToBounds = true;
        /// <summary>Extra upward framing (world units), e.g. to keep a tall fruit tower in view.</summary>
        public float lookUp;
        float m_LookUp;

        const float LookAhead = 2.6f;
        const float SmoothX = 0.16f;
        const float SmoothY = 0.22f;

        Vector2 m_Pos, m_Vel;
        float m_Look, m_LookVel;
        float m_TargetY;
        float m_Trauma;
        float m_Zoom = 1f, m_ZoomTarget = 1f;
        float m_Seed;
        Rigidbody2D m_Body;

        public Vector2 Position => m_Pos;
        public float HalfHeight => Cam.orthographicSize;
        public float HalfWidth => Cam.orthographicSize * Cam.aspect;

        void Awake()
        {
            I = this;
            Cam = GetComponent<Camera>();
            m_Seed = Random.value * 100f;
        }

        public void SetTarget(Transform t, bool snap)
        {
            target = t;
            m_Body = t ? t.GetComponent<Rigidbody2D>() : null;
            if (snap && t != null)
            {
                m_Pos = (Vector2)t.position + Vector2.up * 1.5f;
                m_TargetY = m_Pos.y;
                m_Vel = Vector2.zero;
                m_Look = 0f;
                Clamp(ref m_Pos);
                Apply(Vector2.zero);
            }
        }

        public void SnapTo(Vector2 p)
        {
            m_Pos = p;
            m_TargetY = p.y;
            m_Vel = Vector2.zero;
            Apply(Vector2.zero);
        }

        public void Shake(float amount)
        {
            if (!SaveSystem.Data.screenShake) amount *= 0.25f;
            m_Trauma = Mathf.Clamp01(m_Trauma + amount);
        }

        public void ZoomPunch(float zoom) { m_Zoom = zoom; }
        public void SetZoom(float zoom) { m_ZoomTarget = zoom; }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (target != null)
            {
                Vector2 tp = target.position;
                float facing = 0f, vx = 0f;
                bool grounded = true;
                if (m_Body != null)
                {
                    vx = m_Body.linearVelocity.x;
                    facing = Mathf.Sign(vx);
                    grounded = Mathf.Abs(m_Body.linearVelocity.y) < 0.5f;
                }
                float lookGoal = Mathf.Abs(vx) > 1f ? Mathf.Sign(vx) * LookAhead : facing * LookAhead * 0.5f;
                m_Look = Mathf.SmoothDamp(m_Look, lookGoal, ref m_LookVel, 0.6f, Mathf.Infinity, dt);

                // only re-center vertically when landing or when the player leaves a band around the view center
                m_LookUp = Mathf.MoveTowards(m_LookUp, lookUp, dt * 3f);
                float anchorY = tp.y + 1.8f + m_LookUp;
                if (grounded) m_TargetY = anchorY;
                else if (anchorY < m_TargetY - 2.5f) m_TargetY = anchorY - 1f; // falling: look below the player
                else if (anchorY > m_TargetY + 3.2f) m_TargetY = anchorY - 3.2f;

                var goal = new Vector2(tp.x + m_Look, m_TargetY);
                if (clampToBounds) Clamp(ref goal);
                float smoothY = (m_Body != null && m_Body.linearVelocity.y < -12f) ? 0.08f : SmoothY;
                m_Pos.x = Mathf.SmoothDamp(m_Pos.x, goal.x, ref m_Vel.x, SmoothX, Mathf.Infinity, dt);
                m_Pos.y = Mathf.SmoothDamp(m_Pos.y, goal.y, ref m_Vel.y, smoothY, Mathf.Infinity, dt);
                if (clampToBounds) Clamp(ref m_Pos);
            }

            m_Zoom = Mathf.Lerp(m_Zoom, m_ZoomTarget, 1f - Mathf.Exp(-dt * 6f));
            Cam.orthographicSize = K.CameraSize / m_Zoom;

            // shake
            m_Trauma = Mathf.Max(0f, m_Trauma - Time.unscaledDeltaTime * 1.6f);
            Vector2 shake = Vector2.zero;
            if (m_Trauma > 0f)
            {
                float s = m_Trauma * m_Trauma * 0.55f;
                float t = Time.unscaledTime * 28f;
                shake = new Vector2(Mathf.PerlinNoise(m_Seed, t) - 0.5f, Mathf.PerlinNoise(m_Seed + 7f, t) - 0.5f) * 2f * s;
            }
            Apply(shake);
        }

        void Apply(Vector2 shake)
        {
            transform.position = new Vector3(m_Pos.x + shake.x, m_Pos.y + shake.y, -10f);
        }

        void Clamp(ref Vector2 p)
        {
            float hh = K.CameraSize / m_ZoomTarget;
            float hw = hh * (Cam != null ? Cam.aspect : 16f / 9f);
            float minX = bounds.xMin + hw, maxX = bounds.xMax - hw;
            float minY = bounds.yMin + hh, maxY = bounds.yMax - hh;
            p.x = minX > maxX ? bounds.center.x : Mathf.Clamp(p.x, minX, maxX);
            p.y = minY > maxY ? bounds.yMin + hh : Mathf.Clamp(p.y, minY, maxY);
        }
    }
}
