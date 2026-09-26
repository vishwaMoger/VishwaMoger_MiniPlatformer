using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Heavy stone block that dashes between two points and can be ridden like a platform.</summary>
    public class RockHead : MonoBehaviour
    {
        SpriteAnim m_Anim;
        PathMover m_Mover;
        float m_Blink;

        public static RockHead Create(Transform parent, Vector2 pos, Vector2? pathEnd)
        {
            var go = EntityKit.Root("RockHead", parent, pos, Layers.Ground);
            var r = go.AddComponent<RockHead>();
            r.m_Anim = EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Traps/Rock Head/Idle"), Sort.Traps);
            EntityKit.Box(go, new Vector2(2f, 2f), Vector2.zero, false);
            EntityKit.Kinematic(go);
            r.m_Blink = Random.Range(1f, 3f);
            if (pathEnd.HasValue)
            {
                EntityKit.Chain(parent, pos, pathEnd.Value);
                r.m_Mover = go.AddComponent<PathMover>();
                r.m_Mover.Init(pos, pathEnd.Value, 7f, 1.1f, true);
                Vector2 dir = (pathEnd.Value - pos).normalized;
                r.m_Mover.onArrive = end => r.Impact(end == 1 ? dir : -dir);
            }
            return r;
        }

        void Impact(Vector2 dir)
        {
            var a = GameAssets.I;
            string key = Mathf.Abs(dir.x) > Mathf.Abs(dir.y) ? (dir.x > 0 ? "Right Hit" : "Left Hit") : (dir.y > 0 ? "Top Hit" : "Bottom Hit");
            m_Anim.Play(a.Frames("PA1/Traps/Rock Head/" + key), 20f, false, () => m_Anim.Play(a.Frames("PA1/Traps/Rock Head/Idle")));
            FxSystem.Dust((Vector2)transform.position + dir * 1.05f, 6, 2f, 0.8f);
            var cam = CameraRig.I;
            if (cam != null)
            {
                float d = Vector2.Distance(cam.Position, transform.position);
                if (d < 14f)
                {
                    cam.Shake(0.18f * (1f - d / 14f));
                    AudioManager.I?.Play("slam", 0.5f * (1f - d / 14f), 0.1f);
                }
            }
        }

        void Update()
        {
            m_Blink -= Time.deltaTime;
            if (m_Blink <= 0f && m_Anim.Current == GameAssets.I.Frames("PA1/Traps/Rock Head/Idle"))
            {
                m_Blink = Random.Range(2f, 4f);
                var a = GameAssets.I;
                m_Anim.Play(a.Frames("PA1/Traps/Rock Head/Blink"), 20f, false, () => m_Anim.Play(a.Frames("PA1/Traps/Rock Head/Idle")));
            }
        }
    }
}
