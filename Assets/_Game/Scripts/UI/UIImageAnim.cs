using UnityEngine;
using UnityEngine.UI;

namespace MiniPlatformer
{
    /// <summary>Plays a sprite strip on a UI Image (character previews, fruit icon). Unscaled time.</summary>
    [RequireComponent(typeof(Image))]
    public class UIImageAnim : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 20f;
        public bool bob;
        Image m_Img;
        float m_T;
        Vector2 m_Base;
        RectTransform m_Rt;

        void Awake()
        {
            m_Img = GetComponent<Image>();
            m_Rt = (RectTransform)transform;
            m_Base = m_Rt.anchoredPosition;
        }

        public void SetFrames(Sprite[] f)
        {
            frames = f;
            m_T = 0f;
            if (m_Img != null && f != null && f.Length > 0) m_Img.sprite = f[0];
        }

        void Update()
        {
            if (frames == null || frames.Length == 0) return;
            m_T += Time.unscaledDeltaTime;
            m_Img.sprite = frames[(int)(m_T * fps) % frames.Length];
            if (bob) m_Rt.anchoredPosition = m_Base + Vector2.up * Mathf.Sin(m_T * 3f) * 6f;
        }
    }
}
