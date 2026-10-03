using System.Collections.Generic;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// The route trail: a breadcrumb of round beads (TrekPath shader) showing where the hero will walk through the
    /// rooms you've linked so far. Beads swell in a wave towards the end of the route; corners are rounded; the
    /// colour runs mint to sky while you build, turns gold when the route reaches the trophy (a star marks it) and
    /// ends in a red cross when it leads into danger. It grows outward when the route changes.
    /// </summary>
    public class TrekPath : MonoBehaviour
    {
        static readonly Color BuildA = new Color32(120, 255, 190, 255), BuildB = new Color32(90, 190, 255, 255);
        static readonly Color DoneA = new Color32(255, 236, 120, 255), DoneB = new Color32(255, 170, 40, 255);
        static readonly Color DangerB = new Color32(255, 90, 100, 255);
        static readonly int RevealId = Shader.PropertyToID("_RevealLen");
        static readonly int LengthId = Shader.PropertyToID("_Length");
        const float Corner = 0.9f;

        LineRenderer m_Line;
        MaterialPropertyBlock m_Mpb;
        readonly List<Vector3> m_Points = new List<Vector3>();
        float m_Length, m_Reveal;
        int m_State; // 0 building, 1 complete, 2 danger
        float m_Blend;
        SpriteRenderer m_EndMark;

        public static TrekPath Create(Transform parent)
        {
            var go = new GameObject("RouteTrail");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<TrekPath>();
            p.Build();
            return p;
        }

        void Build()
        {
            m_Line = gameObject.AddComponent<LineRenderer>();
            m_Line.useWorldSpace = true;
            m_Line.sharedMaterial = GameAssets.I.trekPath != null ? GameAssets.I.trekPath : GameAssets.I.additive;
            m_Line.textureMode = LineTextureMode.Stretch;
            m_Line.widthMultiplier = 0.6f;
            m_Line.numCornerVertices = 4;
            m_Line.numCapVertices = 4;
            m_Line.sortingOrder = Sort.Player - 3;
            m_Line.positionCount = 0;
            m_Line.alignment = LineAlignment.View;
            m_Mpb = new MaterialPropertyBlock();

            var end = new GameObject("EndMark");
            end.transform.SetParent(transform, false);
            m_EndMark = end.AddComponent<SpriteRenderer>();
            m_EndMark.sortingOrder = Sort.Player - 2;
        }

        /// <summary>New route. Keeps the already-drawn part when the start of the route is unchanged.</summary>
        public void Set(List<Vector2> pts, bool reached, bool died)
        {
            int state = reached ? 1 : died ? 2 : 0;
            if (state != m_State) { m_State = state; m_Blend = 0f; }
            var smooth = Smooth(pts);
            float keep = 0f;
            for (int i = 1; i < smooth.Count && i < m_Points.Count; i++)
            {
                if ((smooth[i] - m_Points[i]).sqrMagnitude > 0.01f || (smooth[i - 1] - m_Points[i - 1]).sqrMagnitude > 0.01f) break;
                keep += Vector3.Distance(smooth[i - 1], smooth[i]);
            }
            m_Points.Clear();
            m_Points.AddRange(smooth);
            m_Length = 0f;
            for (int i = 1; i < m_Points.Count; i++) m_Length += Vector3.Distance(m_Points[i - 1], m_Points[i]);
            m_Reveal = Mathf.Min(m_Reveal, keep);
            m_Line.positionCount = m_Points.Count;
            m_Line.SetPositions(m_Points.ToArray());
        }

        /// <summary>Removes duplicate points and rounds every corner with a little curve.</summary>
        static List<Vector3> Smooth(List<Vector2> raw)
        {
            var pts = new List<Vector2>();
            foreach (var p in raw) if (pts.Count == 0 || (pts[pts.Count - 1] - p).sqrMagnitude > 0.0025f) pts.Add(p);
            var outPts = new List<Vector3>();
            if (pts.Count == 0) return outPts;
            outPts.Add(pts[0]);
            for (int i = 1; i < pts.Count - 1; i++)
            {
                Vector2 a = pts[i - 1], b = pts[i], c = pts[i + 1];
                float r = Mathf.Min(Corner, Vector2.Distance(a, b) * 0.45f, Vector2.Distance(b, c) * 0.45f);
                if (r < 0.05f || Vector2.Dot((b - a).normalized, (c - b).normalized) > 0.995f) { outPts.Add(b); continue; }
                Vector2 p0 = b + (a - b).normalized * r, p2 = b + (c - b).normalized * r;
                for (int k = 0; k <= 6; k++)
                {
                    float t = k / 6f;
                    outPts.Add(Vector2.Lerp(Vector2.Lerp(p0, b, t), Vector2.Lerp(b, p2, t), t));
                }
            }
            outPts.Add(pts[pts.Count - 1]);
            return outPts;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;
            m_Reveal = Mathf.MoveTowards(m_Reveal, m_Length + 1f, dt * 24f);
            m_Blend = Mathf.MoveTowards(m_Blend, 1f, dt * 2.5f);

            Color a = m_State == 1 ? Color.Lerp(BuildA, DoneA, m_Blend) : BuildA;
            Color b = m_State == 1 ? Color.Lerp(BuildB, DoneB, m_Blend) : m_State == 2 ? Color.Lerp(BuildB, DangerB, m_Blend) : BuildB;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            m_Line.colorGradient = grad;

            if (m_Mpb == null) m_Mpb = new MaterialPropertyBlock();
            m_Line.GetPropertyBlock(m_Mpb);
            m_Mpb.SetFloat(RevealId, m_Reveal);
            m_Mpb.SetFloat(LengthId, Mathf.Max(0.01f, m_Length));
            m_Line.SetPropertyBlock(m_Mpb);

            // end marker: a star on the trophy, a red cross at danger, nothing at a dead end
            bool revealed = m_Points.Count > 1 && m_Reveal >= m_Length;
            m_EndMark.enabled = revealed && m_State != 0;
            if (!m_EndMark.enabled) return;
            m_EndMark.transform.position = m_Points[m_Points.Count - 1] + Vector3.up * 0.15f;
            float pop = Mathf.Clamp01(m_Blend * 2f);
            if (m_State == 1)
            {
                m_EndMark.sprite = UIArt.Star;
                m_EndMark.color = Color.white;
                m_EndMark.transform.localScale = Vector3.one * (0.75f + 0.08f * Mathf.Sin(now * 4f)) * Ease.OutBack(pop);
                m_EndMark.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(now * 2f) * 8f);
            }
            else
            {
                m_EndMark.sprite = UIArt.Close;
                m_EndMark.color = new Color32(255, 80, 90, 255);
                m_EndMark.transform.localScale = Vector3.one * (0.55f + 0.1f * Mathf.Abs(Mathf.Sin(now * 6f))) * Ease.OutBack(pop);
                m_EndMark.transform.localRotation = Quaternion.identity;
            }
        }
    }
}
