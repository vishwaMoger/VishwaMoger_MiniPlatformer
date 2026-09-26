using System.Collections;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Floating arrow that pops the player upward and refreshes their air jump + dash.</summary>
    public class ArrowBoost : MonoBehaviour
    {
        SpriteAnim m_Anim;
        Collider2D m_Col;
        SpriteRenderer m_Halo;
        Vector3 m_Base;

        public static ArrowBoost Create(Transform parent, Vector2 pos)
        {
            var go = EntityKit.Root("Arrow", parent, pos, Layers.Pickup);
            var a = go.AddComponent<ArrowBoost>();
            a.m_Anim = EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Traps/Arrow/Idle"), Sort.Items, default, true);
            a.m_Halo = FxSystem.Halo(a.m_Anim.transform, new Color(0.5f, 1f, 0.6f, 0.4f), 1.2f);
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = 0.6f;
            c.isTrigger = true;
            a.m_Col = c;
            return a;
        }

        void Update()
        {
            m_Anim.transform.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 3f) * 0.12f, 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var p = EntityKit.PlayerFrom(other);
            if (p == null || p.IsDead) return;
            p.Launch(new Vector2(0f, 17f));
            AudioManager.I?.Play("arrow", 0.8f, 0.05f);
            FxSystem.Sparkle(transform.position, new Color(0.5f, 1f, 0.6f), 10, 4f, 0.5f, 0.5f);
            StartCoroutine(Respawn());
        }

        IEnumerator Respawn()
        {
            m_Col.enabled = false;
            var a = GameAssets.I;
            m_Anim.Play(a.Frames("PA1/Traps/Arrow/Hit"), 20f, false);
            m_Halo.enabled = false;
            yield return new WaitForSeconds(0.25f);
            m_Anim.Renderer.enabled = false;
            yield return new WaitForSeconds(2f);
            m_Anim.Renderer.enabled = true;
            m_Halo.enabled = true;
            m_Anim.Play(a.Frames("PA1/Traps/Arrow/Idle"));
            m_Anim.transform.localScale = Vector3.zero;
            Tween.To(this, 0.3f, k => m_Anim.transform.localScale = Vector3.one * k, Ease.OutBack, false);
            m_Col.enabled = true;
        }
    }
}
