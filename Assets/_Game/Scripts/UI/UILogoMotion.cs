using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Title logo idle motion: a slow float with a gentle "breathing" scale and sway.</summary>
    public class UILogoMotion : MonoBehaviour
    {
        RectTransform m_Rt;
        Vector2 m_Base;
        float m_Start;

        void Start()
        {
            m_Rt = (RectTransform)transform;
            m_Base = m_Rt.anchoredPosition;
            m_Start = Time.unscaledTime;
        }

        void LateUpdate()
        {
            if (m_Rt == null) return;
            float k = Time.unscaledTime - m_Start;
            m_Rt.anchoredPosition = m_Base + Vector2.up * Mathf.Sin(k * 1.6f) * 8f;
            m_Rt.localScale = Vector3.one * (1f + Mathf.Sin(k * 2.1f) * 0.015f);
            m_Rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 0.9f) * 1.2f);
        }
    }
}
