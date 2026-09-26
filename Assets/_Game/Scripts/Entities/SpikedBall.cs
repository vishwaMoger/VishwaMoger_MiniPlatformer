using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Pendulum: a spiked ball swinging on a chain from a pivot.</summary>
    public class SpikedBall : MonoBehaviour
    {
        float m_Amplitude = 70f, m_Period = 3.2f, m_Phase;

        public static GameObject Create(Transform parent, Vector2 pivot, float length, float phase)
        {
            var go = EntityKit.Root("SpikedBall", parent, pivot);
            var s = go.AddComponent<SpikedBall>();
            s.m_Phase = phase;
            var a = GameAssets.I;
            var link = a.Sprite("PA1/Traps/Spiked Ball/Chain");
            int links = Mathf.Max(2, Mathf.RoundToInt(length / 0.5f));
            for (int i = 0; i < links; i++)
            {
                var sr = new GameObject("Link").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(go.transform, false);
                sr.transform.localPosition = new Vector3(0f, -i * (length / links), 0f);
                sr.sprite = link;
                sr.sharedMaterial = a.spriteLit;
                sr.sortingOrder = Sort.Chains;
            }
            var ball = new GameObject("Ball") { layer = Layers.Hazard };
            ball.transform.SetParent(go.transform, false);
            ball.transform.localPosition = new Vector3(0f, -length, 0f);
            var bsr = ball.AddComponent<SpriteRenderer>();
            bsr.sprite = a.Sprite("PA1/Traps/Spiked Ball/Spiked Ball");
            bsr.sharedMaterial = a.spriteLit;
            bsr.sortingOrder = Sort.Traps;
            var c = ball.AddComponent<CircleCollider2D>();
            c.radius = 0.75f;
            c.isTrigger = true;
            EntityKit.Kinematic(ball).interpolation = RigidbodyInterpolation2D.None;
            ball.AddComponent<Hazard>();
            return go;
        }

        void Update()
        {
            float ang = m_Amplitude * Mathf.Sin((Time.time / m_Period + m_Phase) * Mathf.PI * 2f);
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }
    }
}
