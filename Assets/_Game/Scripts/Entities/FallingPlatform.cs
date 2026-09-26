using System.Collections;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Propeller platform that shakes, drops after being stood on, and respawns.</summary>
    public class FallingPlatform : MonoBehaviour, IStandable
    {
        Vector2 m_Home;
        Rigidbody2D m_Rb;
        Collider2D m_Col;
        SpriteAnim m_Anim;
        bool m_Triggered;

        public static FallingPlatform Create(Transform parent, Vector2 pos)
        {
            var go = EntityKit.Root("FallingPlatform", parent, pos, Layers.OneWay);
            var f = go.AddComponent<FallingPlatform>();
            f.m_Home = pos;
            f.m_Anim = EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Traps/Falling Platforms/On"), Sort.OneWay + 2, new Vector2(0f, 0.2f));
            f.m_Col = EntityKit.Box(go, new Vector2(2f, 0.35f), new Vector2(0f, 0.34f), false);
            f.m_Col.usedByEffector = true;
            var eff = go.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 160f;
            f.m_Rb = EntityKit.Kinematic(go);
            return f;
        }

        public void OnStood(PlayerController p)
        {
            if (!m_Triggered) StartCoroutine(Fall());
        }

        IEnumerator Fall()
        {
            m_Triggered = true;
            var visual = m_Anim.transform;
            var basePos = visual.localPosition;
            for (float t = 0; t < 0.45f; t += Time.deltaTime)
            {
                visual.localPosition = basePos + (Vector3)(Random.insideUnitCircle * 0.06f);
                yield return null;
            }
            visual.localPosition = basePos;
            m_Anim.Play(GameAssets.I.Frames("PA1/Traps/Falling Platforms/Off"));
            FxSystem.Dust((Vector2)transform.position, 4, 1f, 0.2f);
            m_Rb.bodyType = RigidbodyType2D.Dynamic;
            m_Rb.gravityScale = 2.5f;
            m_Rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            gameObject.layer = Layers.OneWay;
            yield return new WaitForSeconds(0.6f);
            m_Col.enabled = false;
            var sr = m_Anim.Renderer;
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                sr.color = new Color(1, 1, 1, 1f - t / 0.5f);
                yield return null;
            }
            m_Rb.bodyType = RigidbodyType2D.Kinematic;
            m_Rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(1.8f);
            m_Rb.position = m_Home;
            transform.position = m_Home;
            m_Col.enabled = true;
            m_Anim.Play(GameAssets.I.Frames("PA1/Traps/Falling Platforms/On"));
            for (float t = 0; t < 0.35f; t += Time.deltaTime)
            {
                sr.color = new Color(1, 1, 1, t / 0.35f);
                yield return null;
            }
            sr.color = Color.white;
            m_Triggered = false;
        }
    }
}
