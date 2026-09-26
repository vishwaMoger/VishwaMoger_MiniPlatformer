using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Gentle sine bob for UI: either each child with a phase offset (logo letters) or the object itself.</summary>
    public class UIWave : MonoBehaviour
    {
        public bool single;
        public float amplitude = 10f, speed = 3f, phase = 0.45f;
        RectTransform[] m_Items;
        Vector2[] m_Base;
        float m_Seed;

        void Start()
        {
            m_Seed = Random.value * 6f;
            if (single)
            {
                m_Items = new[] { (RectTransform)transform };
                amplitude = 4f;
            }
            else
            {
                m_Items = new RectTransform[transform.childCount];
                for (int i = 0; i < m_Items.Length; i++) m_Items[i] = (RectTransform)transform.GetChild(i);
            }
            m_Base = new Vector2[m_Items.Length];
            for (int i = 0; i < m_Items.Length; i++) m_Base[i] = m_Items[i].anchoredPosition;
        }

        void Update()
        {
            if (m_Items == null) return;
            float t = Time.unscaledTime * speed + m_Seed;
            for (int i = 0; i < m_Items.Length; i++)
            {
                if (m_Items[i] == null) continue;
                m_Items[i].anchoredPosition = m_Base[i] + Vector2.up * Mathf.Sin(t + i * phase) * amplitude;
                if (!single) m_Items[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 0.8f + i * phase) * 4f);
            }
        }
    }
}
