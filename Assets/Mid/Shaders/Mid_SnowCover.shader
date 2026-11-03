Shader "CG/Mid_SnowCover"
{
    Properties
    {
        _BaseMap        ("Base Map", 2D) = "white" {}
        _BaseColor      ("Base Color", Color) = (1,1,1,1)
        _SnowColor      ("Snow Color", Color) = (0.88, 0.92, 1.0, 1)   // M1 눈 바닥 색과 맞춤
        _SnowLevel      ("Snow Level", Range(0,1)) = 0.6                 // 노멀 Y 임계값
        _SnowBlend      ("Snow Blend", Range(0.01,0.5)) = 0.15           // 경계 부드러움
        _SnowAmount     ("Snow Amount", Range(0,1)) = 1.0                // 전체 세기 (0→1 영상 소재)
        _Smoothness     ("Smoothness", Range(0,1)) = 0.5
        _SnowSmoothness ("Snow Smoothness", Range(0,1)) = 0.7           // 눈은 약간 반짝
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        // ── ForwardLit: 조명·그림자 수신 ──────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog                                     // 규칙 8

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float4 _BaseColor, _SnowColor;
            float  _SnowLevel, _SnowBlend, _SnowAmount;
            float  _Smoothness, _SnowSmoothness;

            struct v2f
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;                           // 규칙 8
            };

            v2f vert(float3 posOS : POSITION, float3 normalOS : NORMAL, float2 uv : TEXCOORD0)
            {
                v2f o;
                float3 posWS    = TransformObjectToWorld(posOS);
                o.positionCS    = TransformWorldToHClip(posWS);
                o.positionWS    = posWS;
                o.normalWS      = TransformObjectToWorldNormal(normalOS);
                o.uv            = TRANSFORM_TEX(uv, _BaseMap);
                o.fogFactor     = ComputeFogFactor(o.positionCS.z);       // 규칙 8
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // 노멀 기반 눈 마스크 (좌표계·Transform 수업 내용)
                float3 N        = normalize(i.normalWS);
                float  ndotUp   = dot(N, float3(0,1,0));
                float  snowMask = smoothstep(_SnowLevel, _SnowLevel + _SnowBlend, ndotUp) * _SnowAmount;

                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                float3 baseCol = baseTex.rgb * _BaseColor.rgb;

                // 주 조명 + 그림자
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light  mainLight   = GetMainLight(shadowCoord);
                float  ndl    = saturate(dot(N, mainLight.direction)) * mainLight.shadowAttenuation;
                float3 ambient     = SampleSH(N);
                float3 addLight    = 0;
                int    addCount    = GetAdditionalLightsCount();
                for (int li = 0; li < addCount; li++)
                {
                    Light l = GetAdditionalLight(li, i.positionWS);
                    addLight += l.color * saturate(dot(N, l.direction)) * l.distanceAttenuation;
                }
                float3 lighting = ambient + mainLight.color * ndl + addLight;

                // 베이스: 조명 적용 / 눈: 야간에도 식별되도록 최소 밝기 보장
                float3 litBase = baseCol * lighting;
                float3 litSnow = _SnowColor.rgb * max(lighting, float3(0.25, 0.28, 0.35));
                float3 lit     = lerp(litBase, litSnow, snowMask);

                return float4(MixFog(lit, i.fogFactor), 1);              // 규칙 8
            }
            ENDHLSL
        }

        // ── ShadowCaster: 그림자 투영 ─────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            float4 vertShadow(float3 posOS : POSITION, float3 normalOS : NORMAL) : SV_POSITION
            {
                float3 posWS    = TransformObjectToWorld(posOS);
                float3 normalWS = TransformObjectToWorldNormal(normalOS);
                return TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _LightDirection));
            }

            half4 fragShadow() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
