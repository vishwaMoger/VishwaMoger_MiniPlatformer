// Animated pixel water (or bottom fog when _WaveHeight is 0) drawn across pits.
// UV y = 0 at the bottom of the quad, 1 at its top edge.
Shader "MiniPlatformer/Water"
{
    Properties
    {
        _TopColor("Top Color", Color) = (0.45,0.8,0.95,0.85)
        _DeepColor("Deep Color", Color) = (0.1,0.3,0.55,0.95)
        _FoamColor("Foam Color", Color) = (1,1,1,0.9)
        _WaveHeight("Wave Height (uv)", Range(0,0.2)) = 0.04
        _WaveFreq("Wave Frequency", Float) = 1.5
        _WaveSpeed("Wave Speed", Float) = 1.2
        _PixelsPerUnit("Pixel Snap (ppu)", Float) = 16
        _Size("Quad Size (world)", Vector) = (10,4,0,0)
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
                half4 _TopColor;
                half4 _DeepColor;
                half4 _FoamColor;
                float _WaveHeight;
                float _WaveFreq;
                float _WaveSpeed;
                float _PixelsPerUnit;
                float4 _Size;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 world : TEXCOORD1; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.world = TransformObjectToWorld(v.positionOS.xyz).xy;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // snap to the pixel-art grid so the water matches the sprites
                float2 w = floor(i.world * _PixelsPerUnit) / _PixelsPerUnit;
                float t = _Time.y * _WaveSpeed;
                float wave = sin(w.x * _WaveFreq + t) * 0.6 + sin(w.x * _WaveFreq * 2.3 - t * 1.7) * 0.4;
                float surface = 1.0 - _WaveHeight * (0.5 + 0.5 * wave);
                float y = floor(i.uv.y * _Size.y * _PixelsPerUnit) / (_Size.y * _PixelsPerUnit);
                if (y > surface) discard;

                half depth = saturate((surface - y) * 1.6);
                half4 c = lerp(_TopColor, _DeepColor, depth);

                // foam line + drifting sparkles near the surface
                float foamBand = (_WaveHeight > 0.0001) ? step(surface - y, 1.5 / (_Size.y * _PixelsPerUnit)) : 0;
                float sparkle = step(0.985, frac(sin(dot(floor(w * 2.0) + floor(t * 2.0), float2(12.9898, 78.233))) * 43758.5453)) * (1 - depth) * step(0.0001, _WaveHeight);
                c = lerp(c, _FoamColor, saturate(foamBand + sparkle * 0.6));
                return c;
            }
            ENDHLSL
        }
    }
}
