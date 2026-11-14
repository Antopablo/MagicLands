// Additive soft glow. Works for particles (uses vertex color and UV radial falloff)
// and for sphere meshes (uses a view-facing fresnel core).
Shader "MagicLand/Glow"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.8, 0.5, 1)
        _Intensity ("Intensity", Float) = 1.5
        [Toggle(_SPHERE_MODE)] _SphereMode ("Sphere Mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local _SPHERE_MODE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float3 normalWS : TEXCOORD1;
                float3 viewWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(posWS);
                o.uv = v.uv;
                o.color = v.color;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = GetWorldSpaceViewDir(posWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
            #if defined(_SPHERE_MODE)
                float facing = saturate(dot(normalize(i.normalWS), normalize(i.viewWS)));
                float glow = pow(facing, 3.0);
            #else
                float r = length(i.uv - 0.5) * 2.0;
                float glow = saturate(1.0 - r);
                glow = glow * glow;
            #endif
                half3 col = _Color.rgb * i.color.rgb * i.color.a * glow * _Intensity;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
