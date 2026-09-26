using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniPlatformer
{
    /// <summary>Juicy button feel: hover/select grow, press squash with content shift, click sound.</summary>
    public class UIButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler, ISubmitHandler, IPointerClickHandler
    {
        public string sound = "click";
        public RectTransform content;
        public float pulse; // > 0 = gentle idle "breathing" to draw the eye
        bool m_Hover, m_Down;
        float m_Scale = 1f;
        Selectable m_Sel;

        void Awake() => m_Sel = GetComponent<Selectable>();

        public void OnPointerEnter(PointerEventData e)
        {
            m_Hover = true;
            if (m_Sel != null && m_Sel.interactable) AudioManager.I?.Play("select", 0.25f, 0.05f);
        }
        public void OnPointerExit(PointerEventData e) { m_Hover = false; m_Down = false; }
        public void OnPointerDown(PointerEventData e) => m_Down = true;
        public void OnPointerUp(PointerEventData e) => m_Down = false;
        public void OnSelect(BaseEventData e) => m_Hover = true;
        public void OnDeselect(BaseEventData e) => m_Hover = false;

        public void OnPointerClick(PointerEventData e) => Click();
        public void OnSubmit(BaseEventData e) => Click();

        void Click()
        {
            if (m_Sel != null && !m_Sel.interactable) { AudioManager.I?.Play("error", 0.5f); return; }
            AudioManager.I?.Play(sound, 0.7f, 0.03f);
            m_Scale = 0.88f;
        }

        void Update()
        {
            bool interactable = m_Sel == null || m_Sel.interactable;
            float target = !interactable ? 1f : m_Down ? 0.92f : m_Hover ? 1.06f : 1f;
            if (pulse > 0f && !m_Down) target *= 1f + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.035f * pulse;
            m_Scale = Mathf.Lerp(m_Scale, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f));
            transform.localScale = Vector3.one * m_Scale;
            if (content != null) content.anchoredPosition = new Vector2(0f, m_Down ? -2f : 4f);
        }
    }
}
