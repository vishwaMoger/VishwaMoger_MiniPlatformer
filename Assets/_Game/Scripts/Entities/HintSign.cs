using TMPro;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Wooden sign that shows a tutorial speech bubble when the player walks close.</summary>
    public class HintSign : MonoBehaviour
    {
        public static bool HideAll; // editor capture: keep text out of trailer footage
        Transform m_Bubble;
        float m_Show;

        public static HintSign Create(Transform parent, Vector2 cellBottom, string text)
        {
            // "keyboard text|touch text": pick the variant for the current control scheme
            int bar = text.IndexOf('|');
            if (bar >= 0) text = UIRoot.TouchUI ? text.Substring(bar + 1).Trim() : text.Substring(0, bar).Trim();
            var go = EntityKit.Root("Sign", parent, cellBottom + Vector2.up * 0.5f);
            var s = go.AddComponent<HintSign>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArt.Sign;
            sr.sharedMaterial = GameAssets.I.spriteLit;
            sr.sortingOrder = Sort.Decor;

            var bubble = new GameObject("Bubble").transform;
            bubble.SetParent(go.transform, false);
            bubble.localPosition = new Vector3(0f, 2.4f, 0f);
            var bg = bubble.gameObject.AddComponent<SpriteRenderer>();
            bg.sprite = UIArt.Skin("card");
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.sortingOrder = Sort.WorldUI;
            bg.sharedMaterial = GameAssets.I.spriteGlow;

            var tgo = new GameObject("Text");
            tgo.transform.SetParent(bubble, false);
            var tmp = tgo.AddComponent<TextMeshPro>();
            tmp.font = GameAssets.I.font;
            tmp.text = text;
            tmp.fontSize = 3.2f;
            tmp.color = new Color32(62, 39, 49, 255);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.rectTransform.sizeDelta = new Vector2(9f, 2f);
            tmp.sortingOrder = Sort.WorldUI + 1;
            tmp.ForceMeshUpdate();
            var size = tmp.GetRenderedValues(false);
            bg.size = new Vector2(Mathf.Max(3f, size.x + 1.2f), Mathf.Max(1.4f, size.y + 0.9f));
            s.m_Bubble = bubble;
            bubble.localScale = Vector3.zero;
            return s;
        }

        void Update()
        {
            var p = PlayerController.I;
            bool near = !HideAll && p != null && Vector2.Distance(p.transform.position, transform.position) < 3.2f;
            m_Show = Mathf.MoveTowards(m_Show, near ? 1f : 0f, Time.deltaTime * 5f);
            float k = near ? Ease.OutBack(m_Show) : m_Show;
            m_Bubble.localScale = Vector3.one * k;
            m_Bubble.localPosition = new Vector3(0f, 2.4f + Mathf.Sin(Time.time * 2f) * 0.05f, 0f);
        }
    }
}
