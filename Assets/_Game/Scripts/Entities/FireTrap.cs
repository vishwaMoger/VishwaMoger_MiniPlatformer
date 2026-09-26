using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MiniPlatformer
{
    /// <summary>Metal block that ignites a column of fire shortly after being stepped on.</summary>
    public class FireTrap : MonoBehaviour, IStandable
    {
        SpriteAnim m_Anim;
        Hazard m_Hazard;
        Collider2D m_FireCol;
        Light2D m_Light;
        bool m_Busy;

        public static FireTrap Create(Transform parent, Vector2 cellCenter)
        {
            var go = EntityKit.Root("FireTrap", parent, cellCenter, Layers.Ground);
            var f = go.AddComponent<FireTrap>();
            // 16x32 sprite: lower half is the block (this cell), upper half the flame (cell above)
            f.m_Anim = EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Traps/Fire/Off"), Sort.Traps, new Vector2(0f, 0.5f), true);
            EntityKit.Box(go, new Vector2(1f, 1f), Vector2.zero, false);
            var fire = new GameObject("Flame") { layer = Layers.Hazard };
            fire.transform.SetParent(go.transform, false);
            f.m_FireCol = EntityKit.Box(fire, new Vector2(0.7f, 0.9f), new Vector2(0f, 1f), true);
            f.m_Hazard = fire.AddComponent<Hazard>();
            f.m_Hazard.active = false;
            f.m_FireCol.enabled = false;
            f.m_Light = FxSystem.PointLight(go.transform, new Color(1f, 0.55f, 0.2f), 0f, 3.5f, 0.6f, new Vector2(0f, 1.2f));
            return f;
        }

        public void OnStood(PlayerController p)
        {
            if (!m_Busy) StartCoroutine(Cycle());
        }

        IEnumerator Cycle()
        {
            m_Busy = true;
            var a = GameAssets.I;
            m_Anim.Play(a.Frames("PA1/Traps/Fire/Hit"), 20f, true);
            yield return new WaitForSeconds(0.45f);
            m_Anim.Play(a.Frames("PA1/Traps/Fire/On"), 20f, true);
            m_Hazard.active = true;
            m_FireCol.enabled = true;
            AudioManager.I?.Play("fire", 0.6f);
            float t = 0f;
            while (t < 1.3f)
            {
                t += Time.deltaTime;
                m_Light.intensity = 1.1f + Mathf.PerlinNoise(Time.time * 12f, 0f) * 0.6f;
                if (Random.value < 0.3f) FxSystem.Sparkle((Vector2)transform.position + new Vector2(Random.Range(-0.3f, 0.3f), 1.4f), new Color(1f, 0.6f, 0.2f), 1, 1.5f, 0.35f, 0.5f);
                yield return null;
            }
            m_Hazard.active = false;
            m_FireCol.enabled = false;
            m_Light.intensity = 0f;
            m_Anim.Play(a.Frames("PA1/Traps/Fire/Off"));
            yield return new WaitForSeconds(0.7f);
            m_Busy = false;
        }
    }
}
