// Double-sided, softly glowing butterfly wing.
// Texture channels: R = tint mask (dark border to bright center), G = spot mask, A = wing shape.
Shader "MagicLand/Butterfly"
{
    Properties
    {
        _MainTex ("Wing Mask", 2D) = "white" {}
        _Color ("Color", Color) = (0.3, 0.6, 1, 1)
        _SpotColor ("Spot Color", Color) = (1, 1, 1, 1)
        _Glow ("Glow", Float) = 1.25
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _SpotColor;
                float _Glow;
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
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 m = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half3 col = lerp(_Color.rgb * 0.12, _Color.rgb, m.r) + m.g * _SpotColor.rgb;
                return half4(col * _Glow, m.a);
            }
            ENDHLSL
        }
    }
}
