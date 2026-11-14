// Stylized flowing water: scrolling value-noise streaks with foam, used for the falling water
// (UV.y runs from top to bottom) and the pool (UV.x = angle, UV.y = radius, ripples flow outward).
Shader "MagicLand/Water"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.55, 0.8, 0.95, 1)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.8
        _Glow ("Glow", Float) = 1.05
        _ScaleU ("Streak Count Across (integer)", Float) = 6
        _ScaleV ("Streak Length", Float) = 3
        _Speed ("Flow Speed", Float) = 1.5
        _FoamStart ("Foam Start (along V)", Range(0, 1)) = 0.75
        _SideFade ("Side Fade (along U, 0 = off)", Range(0, 0.5)) = 0.15
        _EndFade ("End Fade (along V)", Range(0.001, 0.5)) = 0.15
        _CenterFoam ("Center Foam (V, 0 = off)", Range(0, 0.5)) = 0
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
                half4 _FoamColor;
                float _Opacity;
                float _Glow;
                float _ScaleU;
                float _ScaleV;
                float _Speed;
                float _FoamStart;
                float _SideFade;
                float _EndFade;
                float _CenterFoam;
                half4 _FogColor;
                float _FogStart;
                float _FogEnd;
                float _FogMax;
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
                float fog : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Value noise that wraps every 'period' cells along X, so the pool has no seam.
            float Noise(float2 p, float period)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float x0 = fmod(i.x, period);
                float x1 = fmod(i.x + 1.0, period);
                float a = Hash(float2(x0, i.y));
                float b = Hash(float2(x1, i.y));
                float c = Hash(float2(x0, i.y + 1.0));
                float d = Hash(float2(x1, i.y + 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(posWS);
                o.uv = v.uv;
                float d = distance(posWS, GetCameraPositionWS());
                o.fog = saturate((d - _FogStart) / max(_FogEnd - _FogStart, 0.01)) * _FogMax;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _Speed;
                float2 p = float2(i.uv.x * _ScaleU, i.uv.y * _ScaleV - t);
                float n = Noise(p, _ScaleU) * 0.6 + Noise(p * 2.0 + float2(0.0, 3.7), _ScaleU * 2.0) * 0.4;
                float streak = smoothstep(0.5, 0.8, n);
                float foam = saturate((i.uv.y - _FoamStart) / max(1.0 - _FoamStart, 0.001));
                // Optional white splash around uv.y = 0 (where the pool ripples start).
                foam = max(foam, _CenterFoam > 0.0 ? 1.0 - smoothstep(0.0, _CenterFoam, i.uv.y) : 0.0);

                half3 col = lerp(_Color.rgb, _FoamColor.rgb, saturate(streak * 0.8 + foam * 0.7)) * _Glow;
                col = lerp(col, _FogColor.rgb, i.fog);

                float side = _SideFade > 0.0 ? saturate(min(i.uv.x, 1.0 - i.uv.x) / _SideFade) : 1.0;
                float end = saturate((1.0 - i.uv.y) / _EndFade);
                float alpha = _Opacity * (0.75 + 0.25 * streak) * side * end;
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
