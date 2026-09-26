using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniPlatformer
{
    /// <summary>Multi-touch on-screen control that writes into GameInput while held.</summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum Action { Left, Right, Jump, Dash }
        public Action action;
        Image m_Img;
        int m_Pointers;

        void Awake() => m_Img = GetComponent<Image>();

        void Set(bool held)
        {
            switch (action)
            {
                case Action.Left: GameInput.TouchLeft = held; break;
                case Action.Right: GameInput.TouchRight = held; break;
                case Action.Jump: GameInput.TouchJump = held; break;
                case Action.Dash: GameInput.TouchDash = held; break;
            }
            if (m_Img != null) m_Img.color = new Color(1f, 1f, 1f, held ? 0.85f : 0.45f);
            transform.localScale = Vector3.one * (held ? 0.92f : 1f);
        }

        public void OnPointerDown(PointerEventData e) { m_Pointers++; Set(true); }
        public void OnPointerUp(PointerEventData e) { m_Pointers = Mathf.Max(0, m_Pointers - 1); if (m_Pointers == 0) Set(false); }
        public void OnPointerExit(PointerEventData e) { if (m_Pointers > 0) { m_Pointers = Mathf.Max(0, m_Pointers - 1); if (m_Pointers == 0) Set(false); } }
        void OnDisable() { m_Pointers = 0; Set(false); }
    }
}
