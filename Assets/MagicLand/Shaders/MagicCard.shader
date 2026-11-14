// World selection card: picture with rounded corners and a soft glowing border that brightens on hover.
Shader "MagicLand/Card"
{
    Properties
    {
        _MainTex ("Picture", 2D) = "white" {}
        _BorderColor ("Border Color", Color) = (1, 0.85, 0.55, 1)
        _Aspect ("Width / Height", Float) = 1.333
        _Radius ("Corner Radius", Float) = 0.08
        _Border ("Border Width", Float) = 0.035
        _Highlight ("Highlight", Range(0, 1)) = 0
        _Dim ("Dim", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
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
                half4 _BorderColor;
                float _Aspect;
                float _Radius;
                float _Border;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Highlight)
                UNITY_DEFINE_INSTANCED_PROP(float, _Dim)
            UNITY_INSTANCING_BUFFER_END(Props)

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float highlight = UNITY_ACCESS_INSTANCED_PROP(Props, _Highlight);
                float dim = UNITY_ACCESS_INSTANCED_PROP(Props, _Dim);

                // Rounded rectangle distance, in card height units.
                float2 p = (i.uv - 0.5) * float2(_Aspect, 1.0);
                float2 halfSize = float2(_Aspect, 1.0) * 0.5 - _Radius;
                float2 q = abs(p) - halfSize;
                float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - _Radius;
                clip(-d);

                float border = _Border * (1.0 + 0.6 * highlight);
                float2 inner = (i.uv - 0.5) * (1.0 + border * 2.0 * float2(1.0 / _Aspect, 1.0)) + 0.5;
                half3 picture = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, inner).rgb;
                half grey = dot(picture, half3(0.3, 0.59, 0.11));
                picture = lerp(picture, grey * half3(0.75, 0.7, 0.85), dim * 0.6) * (1.0 - dim * 0.35);
                picture *= 1.0 + 0.15 * highlight;

                half edge = smoothstep(-border - 0.004, -border, d);
                half3 rim = _BorderColor.rgb * (0.7 + 0.8 * highlight) * (1.0 - dim * 0.5);
                return half4(lerp(picture, rim, edge), 1);
            }
            ENDHLSL
        }
    }
}
