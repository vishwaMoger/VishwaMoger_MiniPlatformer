// Full-screen pixelated iris transition for uGUI (drawn on a RawImage that covers the screen).
// _Radius 0 = fully closed (black), >= 1.2 = fully open.
Shader "MiniPlatformer/UI/IrisWipe"
{
    Properties
    {
        [PerRendererData] _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (0.08,0.06,0.12,1)
        _Radius("Radius", Float) = 1.5
        _Center("Center (uv)", Vector) = (0.5,0.5,0,0)
        _Aspect("Aspect", Float) = 1.777
        _Pixel("Pixel Size (uv)", Float) = 0.006
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Radius;
            float4 _Center;
            float _Aspect;
            float _Pixel;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = floor(i.uv / float2(_Pixel / _Aspect, _Pixel)) * float2(_Pixel / _Aspect, _Pixel);
                float2 d = (uv - _Center.xy) * float2(_Aspect, 1);
                float dist = length(d);
                float a = step(_Radius, dist);
                // a thin bright ring on the edge
                float ring = step(_Radius - 0.012, dist) * (1 - a) * step(0.01, _Radius);
                fixed4 c = _Color;
                c.a = a;
                c.rgb = lerp(c.rgb, fixed3(1, 0.85, 0.55), ring);
                c.a = max(c.a, ring);
                return c;
            }
            ENDCG
        }
    }
}
