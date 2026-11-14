// Soft pastel rainbow band. Mesh UV.x runs along the arc (0..1), UV.y across the band (0 inner, 1 outer).
// _Fade is driven at runtime by RainbowReveal.
Shader "MagicLand/Rainbow"
{
    Properties
    {
        _Opacity ("Opacity", Range(0, 1)) = 0.5
        _Fade ("Fade", Range(0, 1)) = 1
        _Pastel ("Pastel", Range(0, 1)) = 0.2
        [Toggle] _Reverse ("Reverse Colors (secondary bow)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Opacity;
                float _Fade;
                float _Pastel;
                float _Reverse;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float v = lerp(i.uv.y, 1.0 - i.uv.y, _Reverse);
                // Red on the outside, violet on the inside.
                float hue = (1.0 - v) * 0.78;
                float3 rgb = saturate(abs(frac(hue + float3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 1.0);
                rgb = lerp(rgb, 1.0, _Pastel);

                float band = pow(sin(PI * saturate(i.uv.y)), 1.3);
                float ends = smoothstep(0.0, 0.15, i.uv.x) * smoothstep(0.0, 0.15, 1.0 - i.uv.x);
                return half4(rgb, band * ends * _Opacity * _Fade);
            }
            ENDHLSL
        }
    }
}
