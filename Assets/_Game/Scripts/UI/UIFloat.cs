using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Small reusable UI motion: spin (light rays), twinkle (sparkles pop in and out), pulse (breathing buttons)
    /// and bob (floating fruit). Unscaled time so it keeps moving while the game is paused.
    /// </summary>
    public class UIFloat : MonoBehaviour
    {
        public enum Mode { Spin, Twinkle, Pulse, Bob }
        public Mode mode;
        public float speed = 1f, amount = 1f, phase;

        RectTransform m_Rt;
        Vector2 m_Base;
        Vector3 m_Scale;

        public static UIFloat Add(GameObject go, Mode mode, float speed, float amount, float phase = 0f)
        {
            var f = go.AddComponent<UIFloat>();
            f.mode = mode; f.speed = speed; f.amount = amount; f.phase = phase;
            return f;
        }

        void Start()
        {
            m_Rt = (RectTransform)transform;
            m_Base = m_Rt.anchoredPosition;
            m_Scale = m_Rt.localScale;
        }

        void Update()
        {
            if (m_Rt == null) return;
            float t = Time.unscaledTime * speed + phase;
            switch (mode)
            {
                case Mode.Spin:
                    m_Rt.localRotation = Quaternion.Euler(0f, 0f, -t * 8f * amount);
                    break;
                case Mode.Twinkle:
                {
                    // quick pop, long rest
                    float k = Mathf.Repeat(t, 1f);
                    float s = k < 0.35f ? Mathf.Sin(k / 0.35f * Mathf.PI) : 0f;
                    m_Rt.localScale = m_Scale * (s * amount);
                    m_Rt.localRotation = Quaternion.Euler(0f, 0f, k * 90f);
                    break;
                }
                case Mode.Pulse:
                    m_Rt.localScale = m_Scale * (1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.035f * amount);
                    break;
                case Mode.Bob:
                    m_Rt.anchoredPosition = m_Base + new Vector2(Mathf.Sin(t * 0.7f) * 6f, Mathf.Sin(t) * 14f) * amount;
                    m_Rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.8f) * 8f * amount);
                    break;
            }
        }
    }
}
