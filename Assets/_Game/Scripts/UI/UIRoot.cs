using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniPlatformer
{
    public enum MenuScreen { Title, LevelSelect, Characters }

    /// <summary>
    /// All UI, built from code: title, level select, characters, settings, HUD, pause, results, transitions.
    /// Split across UIRoot.cs (core), UIRoot.Menus.cs and UIRoot.InGame.cs.
    /// </summary>
    public partial class UIRoot : MonoBehaviour
    {
        public static UIRoot I { get; private set; }

        Canvas m_Canvas, m_TopCanvas;
        RectTransform m_Backdrop, m_MenuLayer, m_HudLayer, m_PopupLayer, m_ToastLayer;
        RawImage m_Iris;
        Material m_IrisMat;
        float m_IrisRadius = 1.6f;

        public static void Create()
        {
            if (I != null) return;
            var go = new GameObject("UI");
            DontDestroyOnLoad(go);
            I = go.AddComponent<UIRoot>();
        }

        void Awake()
        {
            m_Canvas = MakeCanvas("Canvas", 10);
            m_Backdrop = UIKit.Stretch(m_Canvas.transform, "Backdrop");
            m_MenuLayer = UIKit.Stretch(m_Canvas.transform, "Menus");
            m_HudLayer = UIKit.Stretch(m_Canvas.transform, "HUD");
            m_PopupLayer = UIKit.Stretch(m_Canvas.transform, "Popups");
            m_TopCanvas = MakeCanvas("TopCanvas", 100);
            m_ToastLayer = UIKit.Stretch(m_TopCanvas.transform, "Toasts");

            var irisRt = UIKit.Stretch(m_TopCanvas.transform, "Iris");
            m_Iris = irisRt.gameObject.AddComponent<RawImage>();
            m_IrisMat = new Material(GameAssets.I.irisWipe);
            m_Iris.material = m_IrisMat;
            m_Iris.raycastTarget = false;
            SetIris(0f, new Vector2(0.5f, 0.5f)); // start closed; Game opens it

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        Canvas MakeCanvas(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.layer = 5;
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            c.pixelPerfect = false;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; // whole 1920x1080 layout always fits (any browser window shape)
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
        }

        public static bool TouchUI => SaveSystem.Data.touchControls == 1 ||
                                      (SaveSystem.Data.touchControls == 0 && (Platform.IsMobileDevice ||
                                                                               (Application.platform != RuntimePlatform.WebGLPlayer && Input.touchSupported)));

        void Select(Selectable s)
        {
            if (s == null || EventSystem.current == null) return;
            if (TouchUI) { EventSystem.current.SetSelectedGameObject(null); return; }
            EventSystem.current.SetSelectedGameObject(s.gameObject);
        }

        // ---------------------------------------------------------------- iris transition

        void SetIris(float radius, Vector2 center)
        {
            m_IrisRadius = radius;
            m_IrisMat.SetFloat("_Radius", radius);
            m_IrisMat.SetVector("_Center", center);
            m_IrisMat.SetFloat("_Aspect", Screen.height > 0 ? (float)Screen.width / Screen.height : 1.777f);
            m_Iris.enabled = radius < 1.5f;
            m_Iris.raycastTarget = radius < 1.4f;
        }

        public IEnumerator IrisClose(Vector2 center)
        {
            if (m_IrisRadius <= 0.001f) yield break; // already closed (first load)
            float from = Mathf.Min(m_IrisRadius, 1.5f);
            AudioManager.I?.Play("switch", 0.4f, 0f, 0.8f);
            for (float t = 0; t < 0.45f; t += Time.unscaledDeltaTime)
            {
                SetIris(Mathf.Lerp(from, 0f, Ease.InCubic(t / 0.45f)), center);
                yield return null;
            }
            SetIris(0f, center);
            yield return new WaitForSecondsRealtime(0.08f);
        }

        public IEnumerator IrisOpen(Vector2 center)
        {
            for (float t = 0; t < 0.55f; t += Time.unscaledDeltaTime)
            {
                SetIris(Mathf.Lerp(0f, 1.6f, Ease.OutCubic(t / 0.55f)), center);
                yield return null;
            }
            SetIris(1.6f, center);
        }

        // ---------------------------------------------------------------- menu backdrop crossfade

        RawImage m_Snapshot;
        RenderTexture m_SnapshotRt;
        Coroutine m_SnapshotFade;

        /// <summary>Shows a frozen frame of the previous menu world over the new one and fades it out.</summary>
        public void FadeBackdropSnapshot(RenderTexture rt, float seconds)
        {
            ClearSnapshot();
            m_SnapshotRt = rt;
            var img = UIKit.Stretch(m_Backdrop, "Snapshot").gameObject.AddComponent<RawImage>();
            img.texture = rt;
            img.raycastTarget = false;
            m_Snapshot = img;
            m_SnapshotFade = StartCoroutine(SnapshotFade(img, seconds));
        }

        IEnumerator SnapshotFade(RawImage img, float seconds)
        {
            for (float t = 0f; t < seconds && img != null; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                img.color = new Color(1f, 1f, 1f, 1f - k * k * (3f - 2f * k));
                yield return null;
            }
            m_SnapshotFade = null;
            ClearSnapshot();
        }

        void ClearSnapshot()
        {
            if (m_SnapshotFade != null) { StopCoroutine(m_SnapshotFade); m_SnapshotFade = null; }
            if (m_Snapshot != null) Destroy(m_Snapshot.gameObject);
            m_Snapshot = null;
            if (m_SnapshotRt != null) RenderTexture.ReleaseTemporary(m_SnapshotRt);
            m_SnapshotRt = null;
        }

        // ---------------------------------------------------------------- ads

        GameObject m_AdBlocker;

        /// <summary>Full-screen input blocker with a spinner, shown while a video ad is requested/playing.</summary>
        public void SetAdBlocker(bool on)
        {
            if (!on) { if (m_AdBlocker != null) Destroy(m_AdBlocker); m_AdBlocker = null; return; }
            if (m_AdBlocker != null) return;
            var root = UIKit.Stretch(m_TopCanvas.transform, "AdBlocker");
            root.SetAsFirstSibling();
            m_AdBlocker = root.gameObject;
            UIKit.Dim(root, 0.7f);
            var spin = UIKit.Image(root, UIArt.Gear, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 120f), "Spinner");
            spin.color = UIKit.Cream;
            UIFloat.Add(spin.gameObject, UIFloat.Mode.Spin, 30f, 1f);
        }

        /// <summary>Runs an action after a real-time delay (ignores timeScale, survives pauses and ads).</summary>
        public void RunRealtime(float seconds, System.Action action) => StartCoroutine(RunRealtimeRoutine(seconds, action));

        IEnumerator RunRealtimeRoutine(float seconds, System.Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action?.Invoke();
        }

        // ---------------------------------------------------------------- banner & toast

        public void Banner(string title, string subtitle)
        {
            var rt = UIKit.Rect(m_ToastLayer, "Banner", new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1100f, 170f));
            UIKit.Panel(rt, "panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 170f));
            UIKit.Panel(rt, "inset", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060f, 130f));
            UIKit.Text(rt, title, 44f, new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(1040f, 60f), TextStyle.Dark);
            UIKit.Text(rt, subtitle, 22f, new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(1040f, 40f), TextStyle.Dark, new Color32(150, 110, 140, 255));
            var g = UIKit.Group(rt.gameObject);
            g.blocksRaycasts = false;
            StartCoroutine(BannerRoutine(rt, g));
        }

        IEnumerator BannerRoutine(RectTransform rt, CanvasGroup g)
        {
            Vector2 shown = rt.anchoredPosition, hidden = shown + new Vector2(0f, 300f);
            for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                float k = Ease.OutBack(t / 0.5f);
                rt.anchoredPosition = Vector2.LerpUnclamped(hidden, shown, k);
                g.alpha = Mathf.Clamp01(t * 4f);
                yield return null;
            }
            rt.anchoredPosition = shown;
            yield return new WaitForSecondsRealtime(1.9f);
            for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                float k = Ease.InCubic(t / 0.4f);
                rt.anchoredPosition = Vector2.Lerp(shown, hidden, k);
                g.alpha = 1f - k;
                yield return null;
            }
            Destroy(rt.gameObject);
        }

        public void Toast(string msg)
        {
            var rt = UIKit.Rect(m_ToastLayer, "Toast", new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(900f, 90f));
            var t = UIKit.Text(rt, msg, 34f, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 90f), TextStyle.Heading, UIKit.Gold);
            var g = UIKit.Group(rt.gameObject);
            g.blocksRaycasts = false;
            StartCoroutine(ToastRoutine(rt, g));
        }

        IEnumerator ToastRoutine(RectTransform rt, CanvasGroup g)
        {
            Vector2 p = rt.anchoredPosition;
            for (float t = 0; t < 1.6f; t += Time.unscaledDeltaTime)
            {
                float appear = Mathf.Clamp01(t / 0.25f);
                rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, Ease.OutBack(appear));
                rt.anchoredPosition = p + Vector2.up * (t * 30f);
                g.alpha = t < 1.2f ? appear : 1f - (t - 1.2f) / 0.4f;
                yield return null;
            }
            Destroy(rt.gameObject);
        }
    }
}
