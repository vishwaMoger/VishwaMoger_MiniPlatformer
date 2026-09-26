// Infinite horizontally-scrolling background layer with tint and atmospheric fog.
// _Scroll.x = horizontal UV offset, _Scroll.y = horizontal tiling (repeats across the quad).
Shader "MiniPlatformer/ParallaxLayer"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Tint("Tint", Color) = (1,1,1,1)
        _FogColor("Fog Color (a = amount)", Color) = (1,1,1,0)
        _HeightFog("Height Fog (bottom)", Range(0,1)) = 0
        _Scroll("Scroll (x offset, y tiling)", Vector) = (0,1,0,0)
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Tint;
                half4 _FogColor;
                half _HeightFog;
                float4 _Scroll;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float h : TEXCOORD1; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = float2(v.uv.x * _Scroll.y + _Scroll.x, v.uv.y);
                o.h = v.uv.y;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Tint;
                half fog = saturate(_FogColor.a + _HeightFog * (1.0 - i.h));
                c.rgb = lerp(c.rgb, _FogColor.rgb, fog);
                return c;
            }
            ENDHLSL
        }
    }
}
