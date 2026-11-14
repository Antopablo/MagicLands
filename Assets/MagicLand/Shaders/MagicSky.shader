// Inside-out sky sphere: soft vertical gradient, aurora band and twinkling procedural stars.
Shader "MagicLand/Sky"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.05, 0.04, 0.20, 1)
        _MidColor ("Middle Color", Color) = (0.30, 0.12, 0.45, 1)
        _BottomColor ("Bottom Color", Color) = (0.02, 0.10, 0.18, 1)
        _AuroraColor ("Aurora Color", Color) = (0.20, 0.90, 0.75, 1)
        _StarDensity ("Star Density", Float) = 120
        _StarBrightness ("Star Brightness", Float) = 1.6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" "RenderPipeline"="UniversalPipeline" }
        Cull Front
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _MidColor;
                half4 _BottomColor;
                half4 _AuroraColor;
                float _StarDensity;
                float _StarBrightness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dir : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                // Object-space direction keeps the sky aligned with the recentered content root.
                o.dir = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float t = _Time.y;

                // Gradient follows content "up" (Y), so it stays coherent when the user lies down.
                half3 col = d.y > 0
                    ? lerp(_MidColor.rgb, _TopColor.rgb, smoothstep(0.0, 0.8, d.y))
                    : lerp(_MidColor.rgb, _BottomColor.rgb, smoothstep(0.0, 0.6, -d.y));

                // Slow waving aurora band around the forward direction.
                float wave = sin(d.x * 4.0 + t * 0.15) * 0.08 + sin(d.x * 9.0 - t * 0.1) * 0.03;
                float band = exp(-pow((d.y - 0.35 - wave) * 7.0, 2.0));
                col += _AuroraColor.rgb * band * (0.35 + 0.15 * sin(t * 0.3 + d.x * 3.0));

                // Procedural stars on a 3D grid, twinkling individually.
                float3 cell = floor(d * _StarDensity);
                float3 local = frac(d * _StarDensity) - 0.5;
                float rnd = hash31(cell);
                float star = step(0.985, rnd) * smoothstep(0.35, 0.0, length(local));
                float twinkle = 0.6 + 0.4 * sin(t * (1.0 + rnd * 3.0) + rnd * 60.0);
                col += star * twinkle * _StarBrightness * saturate(d.y + 0.6);

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
