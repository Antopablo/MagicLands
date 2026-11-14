// Unlit glowing lava: vertex colors drawn at full brightness with slow pulses running along the flow.
// Per-vertex data in UV1: x = pulse amount (0 = steady), y = phase (increasing downstream makes the pulses travel).
Shader "MagicLand/Lava"
{
    Properties
    {
        _Intensity ("Intensity", Float) = 1
        _Speed ("Pulse Speed", Float) = 1.3
        _FogColor ("Fog Color", Color) = (0.95, 0.6, 0.5, 1)
        _FogStart ("Fog Start", Float) = 20
        _FogEnd ("Fog End", Float) = 90
        _FogMax ("Fog Max", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
                float _Speed;
                half4 _FogColor;
                float _FogStart;
                float _FogEnd;
                float _FogMax;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 pulse : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 color : COLOR;
                float2 pulse : TEXCOORD0;
                float fog : TEXCOORD1;
                float3 posWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(posWS);
                o.color = v.color.rgb;
                o.pulse = v.pulse;
                o.posWS = posWS;
                float d = distance(posWS, GetCameraPositionWS());
                o.fog = saturate((d - _FogStart) / max(_FogEnd - _FogStart, 0.01)) * _FogMax;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _Speed;
                float wave = sin(t - i.pulse.y) * 0.6 + sin(t * 0.63 + dot(i.posWS, float3(0.7, 0.3, 0.5))) * 0.4;
                half3 col = i.color * _Intensity * (1.0 + i.pulse.x * 0.22 * wave);
                col = lerp(col, _FogColor.rgb, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
