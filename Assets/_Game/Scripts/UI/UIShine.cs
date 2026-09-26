using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>A light band that sweeps across its (masked) parent every few seconds.</summary>
    public class UIShine : MonoBehaviour
    {
        public float width = 460f, interval = 3.2f, duration = 0.6f;
        RectTransform m_Rt;
        float m_Offset;

        void Start()
        {
            m_Rt = (RectTransform)transform;
            m_Offset = Random.value;
        }

        void Update()
        {
            float t = Mathf.Repeat(Time.unscaledTime / interval + m_Offset, 1f) * interval;
            float k = Mathf.Clamp01(t / duration);
            float x = Mathf.Lerp(-width * 0.6f, width * 0.6f, k);
            m_Rt.anchoredPosition = new Vector2(x, 0f);
            m_Rt.localRotation = Quaternion.Euler(0f, 0f, -22f);
        }
    }
}
