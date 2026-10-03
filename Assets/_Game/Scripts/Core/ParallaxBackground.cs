using System.Collections.Generic;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Multi-layer parallax backdrop. Each layer is a single camera-sized quad whose texture is scrolled in the
    /// ParallaxLayer shader, so it tiles infinitely at the cost of one draw call per layer.
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        class Layer
        {
            public BgLayer def;
            public Transform tr;
            public MeshRenderer mr;
            public MaterialPropertyBlock mpb;
            public float widthUnits, heightUnits;
        }

        static readonly int ScrollId = Shader.PropertyToID("_Scroll");
        static readonly int TintId = Shader.PropertyToID("_Tint");
        static readonly int FogId = Shader.PropertyToID("_FogColor");
        static readonly int HeightFogId = Shader.PropertyToID("_HeightFog");
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        readonly List<Layer> m_Layers = new List<Layer>();
        Camera m_Cam;
        float m_RefY;
        float m_Time;
        float m_SetHeight; // tallest layer of the set (world units), the height the art was drawn to fill

        /// <summary>Extra horizontal offset (the menu drifts the background with this).</summary>
        public float virtualX;

        static Mesh s_Quad;

        public static ParallaxBackground Create(Transform parent, BgLayer[] layers, Camera cam, float refY)
        {
            var go = new GameObject("Parallax");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<ParallaxBackground>();
            p.m_Cam = cam;
            p.m_RefY = refY;
            p.Build(layers);
            return p;
        }

        static Mesh Quad()
        {
            if (s_Quad != null) return s_Quad;
            s_Quad = new Mesh { name = "ParallaxQuad" };
            s_Quad.vertices = new[] { new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(-0.5f, 1, 0), new Vector3(0.5f, 1, 0) };
            s_Quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            s_Quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            s_Quad.RecalculateBounds();
            s_Quad.bounds = new Bounds(new Vector3(0, 0.5f, 0), new Vector3(1000, 1000, 1));
            return s_Quad;
        }

        void Build(BgLayer[] defs)
        {
            var assets = GameAssets.I;
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                var tex = assets.Texture(d.key);
                if (tex == null) continue;
                var go = new GameObject("BG_" + i);
                go.transform.SetParent(transform, false);
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = Quad();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = assets.parallax;
                mr.sortingLayerName = Sort.BackgroundLayer;
                mr.sortingOrder = -100 + i;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                var l = new Layer
                {
                    def = d, tr = go.transform, mr = mr, mpb = new MaterialPropertyBlock(),
                    widthUnits = tex.width / K.PPU * d.scale,
                    heightUnits = tex.height / K.PPU * d.scale,
                };
                l.mpb.SetTexture(MainTexId, tex);
                l.mpb.SetColor(TintId, d.tint);
                l.mpb.SetColor(FogId, d.fog);
                l.mpb.SetFloat(HeightFogId, d.heightFog);
                m_Layers.Add(l);
                if (d.factor > 0.001f) m_SetHeight = Mathf.Max(m_SetHeight, l.heightUnits + Mathf.Max(0f, d.bottom));
            }
            LateUpdate();
        }

        void LateUpdate()
        {
            if (m_Cam == null) return;
            m_Time += Time.deltaTime;
            Vector3 cp = m_Cam.transform.position;
            float halfH = m_Cam.orthographicSize;
            float halfW = halfH * m_Cam.aspect;
            float camX = cp.x + virtualX;
            // the art is drawn for a 16:9 view; when the view is taller (tall boards, phones) the whole set scales up
            // together so it still fills the screen instead of ending in a hard edge half way up
            float fill = m_SetHeight > 0f ? Mathf.Max(1f, (halfH * 2f + 2.5f) / m_SetHeight) : 1f;
            foreach (var l in m_Layers)
            {
                float width = halfW * 2f + 2f;
                // slight vertical parallax so tall climbs feel deep
                float vy = Mathf.Clamp((cp.y - m_RefY) * l.def.factor * 0.35f, -4f, 4f);
                float bottom = cp.y - halfH + l.def.bottom * fill - vy;
                float h = l.heightUnits * fill;
                // the far sky layer always covers the whole view
                if (l.def.factor <= 0.001f && h < halfH * 2f + 2f) { h = halfH * 2f + 3f; bottom = cp.y - halfH - 1.5f; }
                l.tr.position = new Vector3(cp.x, bottom, 10f);
                l.tr.localScale = new Vector3(width, h, 1f);
                // a stretched layer keeps the texture's aspect ratio (no squashed moons): widen its texture units too
                float wu = l.widthUnits * (h / l.heightUnits);
                float offset = (camX * l.def.factor + m_Time * l.def.autoScroll) / wu;
                float tiling = width / wu;
                offset += 0.5f - tiling * 0.5f; // texture centre sits at the view centre (moons stay centred)
                l.mpb.SetVector(ScrollId, new Vector4(offset, tiling, 0, 0));
                l.mr.SetPropertyBlock(l.mpb);
            }
        }
    }
}
