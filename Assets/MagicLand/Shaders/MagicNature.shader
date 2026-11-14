// Low-poly nature shader: vertex colors, soft directional light, distance fog
// and wind sway driven by per-vertex weights (UV1.x = weight in meters, UV1.y = phase).
// Global light, wind and up directions are set by NatureEnvironment so they follow the recentered content.
Shader "MagicLand/Nature"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.93, 0.68, 0.62, 1)
        _FogStart ("Fog Start", Float) = 12
        _FogEnd ("Fog End", Float) = 58
        _FogMax ("Fog Max", Range(0, 1)) = 0.92
        _Ambient ("Ambient", Color) = (0.42, 0.42, 0.6, 1)
        _LightColor ("Light Color", Color) = (1, 0.86, 0.7, 1)
        _WindAmount ("Wind Amount", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull [_Cull]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float3 _MagicLightDir;
            float3 _MagicWindDir;
            float3 _MagicUp;
            float _MagicWindStrength;

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                float _FogStart;
                float _FogEnd;
                float _FogMax;
                half4 _Ambient;
                half4 _LightColor;
                float _WindAmount;
                float _Cull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 wind : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 color : COLOR;
                float3 normalWS : TEXCOORD0;
                float fog : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 SafeDir(float3 d, float3 fallback)
            {
                return dot(d, d) > 0.01 ? normalize(d) : fallback;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float w = v.wind.x * _WindAmount * _MagicWindStrength;
                if (w > 0.0001)
                {
                    float t = _Time.y;
                    float phase = v.wind.y;
                    float3 windDir = SafeDir(_MagicWindDir, float3(1, 0, 0));
                    float3 up = SafeDir(_MagicUp, float3(0, 1, 0));
                    float3 side = normalize(cross(up, windDir));
                    float sway = sin(t * 0.9 + phase) * 0.7 + sin(t * 1.73 + phase * 1.3) * 0.3 + 0.35;
                    float across = sin(t * 0.67 + phase * 0.7) * 0.4;
                    float flutter = sin(t * 5.0 + dot(posWS, float3(2.1, 3.3, 1.7))) * 0.1;
                    posWS += (windDir * (sway + flutter) + side * across + up * flutter * 0.5) * w;
                }

                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color.rgb;
                float d = distance(posWS, GetCameraPositionWS());
                o.fog = saturate((d - _FogStart) / max(_FogEnd - _FogStart, 0.01)) * _FogMax;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 l = SafeDir(_MagicLightDir, normalize(float3(0.35, 0.75, -0.45)));
                float3 up = SafeDir(_MagicUp, float3(0, 1, 0));
                float ndl = dot(n, l) * 0.5 + 0.5;
                ndl *= ndl;
                half3 light = _Ambient.rgb * (0.8 + 0.2 * dot(n, up)) + _LightColor.rgb * ndl;
                half3 col = i.color * light;
                col = lerp(col, _FogColor.rgb, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
