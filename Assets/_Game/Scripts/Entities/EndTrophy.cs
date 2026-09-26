using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MiniPlatformer
{
    /// <summary>Level goal. In boss levels it stays hidden until the boss is defeated.</summary>
    public class EndTrophy : MonoBehaviour
    {
        SpriteAnim m_Anim;
        Collider2D m_Trigger;
        Light2D m_Light;
        SpriteRenderer m_Halo;
        bool m_Done;

        public static EndTrophy Create(Transform parent, Vector2 feetPos, bool hidden)
        {
            var go = EntityKit.Root("EndTrophy", parent, feetPos, Layers.Pickup);
            var e = go.AddComponent<EndTrophy>();
            e.m_Anim = EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Items/Checkpoints/End/End (Idle)"), Sort.Items - 2, new Vector2(0f, 2f), true);
            e.m_Trigger = EntityKit.Box(go, new Vector2(2f, 2.4f), new Vector2(0f, 1.2f), true);
            e.m_Light = FxSystem.PointLight(go.transform, new Color(1f, 0.85f, 0.45f), 1f, 6f, 0.5f, new Vector2(0f, 1.6f));
            e.m_Halo = FxSystem.Halo(go.transform, new Color(1f, 0.8f, 0.4f, 0.35f), 3.2f);
            e.m_Halo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            if (hidden) go.SetActive(false);
            return e;
        }

        void Update()
        {
            if (m_Halo != null) m_Halo.transform.localScale = Vector3.one * (3.2f + Mathf.Sin(Time.time * 2f) * 0.25f);
        }

        public void Reveal()
        {
            gameObject.SetActive(true);
            var t = m_Anim.transform;
            t.localScale = Vector3.zero;
            Tween.To(this, 0.7f, k => t.localScale = Vector3.one * k, Ease.OutBack, false);
            FxSystem.Sparkle((Vector2)transform.position + Vector2.up * 1.5f, new Color(1f, 0.9f, 0.5f), 30, 6f, 0.8f, 0.9f);
            AudioManager.I?.Play("appear", 0.9f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (m_Done) return;
            var p = EntityKit.PlayerFrom(other);
            if (p == null || p.IsDead) return;
            m_Done = true;
            m_Anim.Play(GameAssets.I.Frames("PA1/Items/Checkpoints/End/End (Pressed)"), 20f, false);
            p.Freeze();
            FxSystem.Confetti((Vector2)transform.position + Vector2.up * 1.5f, 80, 12f);
            FxSystem.HitStop(0.12f);
            CameraRig.I?.Shake(0.25f);
            CameraRig.I?.ZoomPunch(1.12f);
            PostFX.I?.GlowPulse();
            AudioManager.I?.Play("win", 0.9f, 0f);
            m_Light.intensity = 2f;
            Tween.Delay(0.5f, () => { if (p != null) p.PlayDisappear(); });
            Tween.Delay(0.9f, () => LevelSession.Current?.Complete());
        }
    }
}
