// Soft petal for falling particles: alpha-blended ellipse tinted by the particle color, double sided, with distance fog.
Shader "MagicLand/Petal"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _FogColor ("Fog Color", Color) = (0.93, 0.68, 0.62, 1)
        _FogStart ("Fog Start", Float) = 12
        _FogEnd ("Fog End", Float) = 58
        _FogMax ("Fog Max", Range(0, 1)) = 0.9
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
                half4 _Color;
                half4 _FogColor;
                float _FogStart;
                float _FogEnd;
                float _FogMax;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float fog : TEXCOORD1;
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
                o.color = v.color * _Color;
                float d = distance(posWS, GetCameraPositionWS());
                o.fog = saturate((d - _FogStart) / max(_FogEnd - _FogStart, 0.01)) * _FogMax;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Petal shape: an ellipse slightly pointed at one end, lighter in the middle.
                float2 p = (i.uv - 0.5) * 2.0;
                p.x *= 1.0 + p.y * 0.35;
                float r = length(float2(p.x * 1.6, p.y));
                float alpha = saturate((1.0 - r) * 6.0);
                half3 col = i.color.rgb * (0.85 + 0.25 * (1.0 - r));
                col = lerp(col, _FogColor.rgb, i.fog);
                return half4(col, alpha * i.color.a);
            }
            ENDHLSL
        }
    }
}
