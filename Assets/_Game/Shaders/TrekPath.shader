// Route trail for the sliding-room puzzles: a breadcrumb of round, outlined beads along a LineRenderer
// (UV.x 0..1 along the line, times _Length = world units; UV.y across). A wave makes the beads swell one after another towards the
// end of the route, so the direction reads at a glance. Normal alpha blending keeps the colours rich (no bloom
// blow-out); a dark rim makes the beads readable on any background. Vertex colour = gradient along the route.
Shader "MiniPlatformer/TrekPath"
{
    Properties
    {
        _Spacing("Bead spacing (units)", Float) = 0.62
        _Radius("Bead radius (units)", Float) = 0.15
        _Speed("Wave speed", Float) = 5
        _Width("Line width (units)", Float) = 0.6
        _RevealLen("Revealed length (world units)", Float) = 999
        _Length("Line length (world units)", Float) = 10
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Spacing;
                float _Radius;
                float _Speed;
                float _Width;
                float _RevealLen;
                float _Length;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float u = i.uv.x * _Length;                               // world units along the route
                float cell = floor(u / _Spacing);
                float centre = (cell + 0.5) * _Spacing;
                if (centre > _RevealLen) return 0;                    // not drawn yet (grows outwards)

                // each bead swells in turn: a wave running towards the end of the route
                float wave = sin(cell * 0.85 - _Time.y * _Speed) * 0.5 + 0.5;
                float r = _Radius * (0.82 + 0.38 * wave * wave);
                float2 p = float2(u - centre, (i.uv.y - 0.5) * _Width);
                float d = length(p);
                float aa = fwidth(d) * 1.2 + 1e-4;

                half fill = 1.0 - smoothstep(r - aa, r + aa, d);              // bead body
                half rim = 1.0 - smoothstep(r + 0.045 - aa, r + 0.045 + aa, d); // dark outline
                half glow = saturate(1.0 - d / (r * 2.4)) * 0.35 * (0.5 + wave);
                // soft highlight on the upper left of each bead
                half spec = 1.0 - smoothstep(r * 0.25, r * 0.55, length(p - float2(-r * 0.3, r * 0.3)));

                half3 body = lerp(i.color.rgb, half3(1, 1, 1), spec * 0.55 + wave * 0.2);
                half3 rimCol = i.color.rgb * 0.25;
                half3 col = lerp(rimCol, body, fill);
                half a = max(rim, glow) * i.color.a;
                col = lerp(i.color.rgb, col, rim);                            // the glow takes the route colour
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
