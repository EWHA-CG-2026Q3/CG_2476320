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

        // ── 지붕 영역 마스크 (M6) ─────────────────────────────────────
        _CoverMin       ("Cover Min (XZ)", Vector) = (0,0,0,0)
        _CoverMax       ("Cover Max (XZ)", Vector) = (0,0,0,0)
        _CoverSoftness  ("Cover Softness (m)", Float) = 0.3
        _CoverInset     ("Cover Inset (m)", Float) = 0.2
        _UseCoverMask   ("Use Cover Mask", Float) = 0

        // ── 가짜 실내광 (C2) ─────────────────────────────────────────
        _InteriorGlowColor   ("실내광 밝은 색",  Color) = (0.627, 0.353, 0.208, 1) // #A05A35 WarmMid
        _InteriorShadeColor  ("실내광 그늘 색",  Color) = (0.431, 0.184, 0.180, 1) // #6E2F2E WarmShade
        _InteriorGlowStrength("실내광 세기", Range(0,2)) = 1.0
        _InteriorCenter      ("실내 중심 (World)", Vector) = (0,2,1.68,0)
        _InteriorFloorY      ("실내 바닥 Y", Float) = 0.3
        _InteriorCeilY       ("실내 천장 Y", Float) = 4.09
        _UseInteriorGlow     ("실내광 사용", Float) = 0                // 0=끔, 1=켬
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP                   // Forward+ 타일 클러스터 조명
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS    // C4: 창살 무늬 그림자
            #pragma multi_compile_fog                                     // 규칙 8

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float4 _BaseColor, _SnowColor;
            float  _SnowLevel, _SnowBlend, _SnowAmount;
            float  _Smoothness, _SnowSmoothness;
            float4 _CoverMin, _CoverMax;
            float  _CoverSoftness, _CoverInset, _UseCoverMask;
            float4 _InteriorGlowColor, _InteriorShadeColor, _InteriorCenter;
            float  _InteriorGlowStrength, _InteriorFloorY, _InteriorCeilY, _UseInteriorGlow;

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

                // 지붕 영역 마스크: 월드 xz가 지붕 사각형 안이면 눈 제거 (좌표계 수업 내용)
                float2 posXZ    = i.positionWS.xz;
                float2 rMin     = _CoverMin.xy + _CoverInset;
                float2 rMax     = _CoverMax.xy - _CoverInset;
                float2 insideXZ = smoothstep(rMin - _CoverSoftness, rMin, posXZ)
                                * (1 - smoothstep(rMax, rMax + _CoverSoftness, posXZ));
                float  inside   = insideXZ.x * insideXZ.y;
                snowMask *= lerp(1, 1 - inside, _UseCoverMask);

                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                float3 baseCol = baseTex.rgb * _BaseColor.rgb;

                // 주 조명 + 그림자
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light  mainLight   = GetMainLight(shadowCoord);
                float  ndl    = saturate(dot(N, mainLight.direction)) * mainLight.shadowAttenuation;
                float3 ambient     = SampleSH(N);
                // Forward+: 화면을 타일로 나눠 타일별 조명 목록을 쓰므로 전용 루프 매크로 필요
                // (일반 for문 + GetAdditionalLightsCount()는 Forward+에서 0을 반환해 작동 안 함)
                InputData inputData; // LIGHT_LOOP_BEGIN 매크로가 내부적으로 참조
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                float3 addLight = 0;
                uint   addCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(addCount)
                    // C4: shadowMask 오버로드 사용 → shadowAttenuation으로 창살 무늬 그림자
                    Light l = GetAdditionalLight(lightIndex, i.positionWS, half4(1,1,1,1));
                    addLight += l.color * saturate(dot(N, l.direction))
                              * l.distanceAttenuation * l.shadowAttenuation;
                LIGHT_LOOP_END
                float3 lighting = ambient + mainLight.color * ndl + addLight;

                // 베이스: 조명 적용 / 눈: 야간에도 식별되도록 최소 밝기 보장
                float3 litBase = baseCol * lighting;
                float3 litSnow = _SnowColor.rgb * max(lighting, float3(0.25, 0.28, 0.35));
                float3 lit     = lerp(litBase, litSnow, snowMask);

                // C2: 가짜 실내광 — 안쪽을 향한 면만 따뜻하게
                // 바깥 면(창살·기둥 외면)은 파란 실루엣 그대로 → "창 너머로 빛" 느낌
                // 수업 근거: 벡터 내적(facing), 월드 좌표 비교(inside, h), uniform
                if (_UseInteriorGlow > 0.5)
                {
                    float3 toCenter  = normalize(_InteriorCenter.xyz - i.positionWS);
                    float  intFacing = saturate(dot(N, toCenter));        // 안쪽 면만 양수
                    float  h         = saturate((i.positionWS.y - _InteriorFloorY)
                                     / (_InteriorCeilY - _InteriorFloorY));
                    // 천장 쪽(초롱 가까이)이 더 밝음
                    float3 warm = lerp(_InteriorShadeColor.rgb, _InteriorGlowColor.rgb,
                                       lerp(0.4, 1.0, h) * intFacing);
                    lit += baseTex.rgb * warm * inside * _InteriorGlowStrength;
                }

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
