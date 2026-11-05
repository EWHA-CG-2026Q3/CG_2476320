Shader "CG/Mid_SnowTexture"
{
    Properties
    {
        _BaseColor      ("Base Color", Color) = (0.88, 0.92, 1.0, 1)   // 약간 푸른 흰색
        _DetailMap      ("Detail Map", 2D) = "white" {}                  // S18처럼 텍스처 받기
        _Tiling         ("Tiling", Float) = 8                            // S18처럼 반복 횟수
        _DetailStrength ("Detail Strength", Range(0,1)) = 0.3
        _ShadowColor    ("Shadow Color", Color) = (0.21, 0.42, 0.66, 1) // #386CA9 — 원칙 2: 그림자는 남색
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 1.0
        _PointLightGain ("Point Light Gain", Range(0,10)) = 1.0          // 포인트 라이트 세기 배율
        _WarmLightTint  ("Warm Light Tint", Color) = (1,1,1,1)           // 눈 위 빛 색 보정 (기본 흰색=그대로)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP                   // Forward+ 타일 클러스터 조명
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS    // C4: 창살 무늬 그림자
            #pragma multi_compile_fog                                     // 규칙 8: 안개 지원
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float4    _BaseColor;
            sampler2D _DetailMap;
            float     _Tiling;
            float     _DetailStrength;
            float4    _ShadowColor;
            float     _ShadowStrength;
            float     _PointLightGain;
            float4    _WarmLightTint;

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float  fogFactor  : TEXCOORD1;                           // 규칙 8: 안개 계수
                float3 positionWS : TEXCOORD2;                           // 그림자 좌표 계산용
            };

            v2f vert(float3 positionOS : POSITION, float2 uv : TEXCOORD0)
            {
                v2f o;
                float3 posWS  = TransformObjectToWorld(positionOS);
                o.positionCS  = TransformWorldToHClip(posWS);
                o.positionWS  = posWS;
                o.uv          = uv;
                o.fogFactor   = ComputeFogFactor(o.positionCS.z);        // 규칙 8
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // S18처럼 텍스처 읽기 + BaseColor 곱하기
                float  detail = tex2D(_DetailMap, i.uv * _Tiling).r;
                float3 col    = _BaseColor.rgb * lerp(1, detail, _DetailStrength);

                // 그림자 수신 (URP 메인 라이트 그림자 맵 사용 — 발표 시 명시)
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                float  shadow      = MainLightRealtimeShadow(shadowCoord);
                col = lerp(lerp(col, _ShadowColor.rgb, _ShadowStrength), col, shadow);

                // 포인트 라이트(초롱 불빛) → 처마 아래 눈밭에 주황빛 원
                // Forward+: 타일 클러스터 루프 매크로 사용 (수업 근거: Forward+는 화면 타일별 조명 목록)
                InputData inputData;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                float3 ptLight = 0;
                uint   addCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(addCount)
                    // C4: shadowMask 오버로드 → shadowAttenuation으로 창살 무늬 그림자
                    Light l = GetAdditionalLight(lightIndex, i.positionWS, half4(1,1,1,1));
                    ptLight += l.color * l.distanceAttenuation * l.shadowAttenuation
                             * saturate(dot(float3(0,1,0), l.direction));
                LIGHT_LOOP_END
                // 눈 위 따뜻한 빛은 눈 색에 곱하지 않고 그대로 더함 (파란 눈 × 주황 = 회색 방지)
                // 수업 근거: 빛의 선형 합성 (additive lighting)
                col += ptLight * _PointLightGain * _WarmLightTint.rgb;

                return float4(MixFog(col, i.fogFactor), 1);              // 규칙 8
            }
            ENDHLSL
        }

        // 그림자를 받으려면 ShadowCaster 패스도 있어야 다른 오브젝트에 그림자를 드림
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
