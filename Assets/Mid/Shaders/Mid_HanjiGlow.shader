// 창호지 발광 셰이더 (C3: REQ_Lantern.md §C3)
// 평평한 발광만 — 조명 계산 없음, 레퍼런스 집 창문처럼 uniformly 빛남
// Cull Off + SV_IsFrontFace: 바깥면(창살 너머)은 밝게, 안쪽면은 약간 어둡게
Shader "CG/Mid_HanjiGlow"
{
    Properties
    {
        [HDR]
        _CoreColor      ("중심 색 (HDR)", Color)       = (1.0, 0.698, 0.341, 1) // #FFB257 WarmLight
        [HDR]
        _EdgeColor      ("가장자리 색 (HDR)", Color)   = (0.627, 0.353, 0.208, 1) // #A05A35 WarmMid
        _GlowIntensity  ("발광 세기", Float)           = 1.3
        _InnerSideFactor("안쪽면 밝기 배율", Range(0,1)) = 0.5
        _HanjiMap       ("한지 결 텍스처", 2D)         = "white" {}
        _HanjiTiling    ("한지 타일링", Float)         = 2.0
        _HanjiStrength  ("한지 결 강도", Range(0,1))   = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off // 양면 렌더링 — 바깥/안쪽 모두 보임

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog  // 규칙 8

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_HanjiMap); SAMPLER(sampler_HanjiMap);
            float4 _CoreColor, _EdgeColor;
            float  _GlowIntensity, _InnerSideFactor;
            float  _HanjiTiling, _HanjiStrength;

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float  fogFactor  : TEXCOORD1; // 규칙 8
            };

            v2f vert(float3 posOS : POSITION, float2 uv : TEXCOORD0)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(posOS);
                o.uv         = uv;
                o.fogFactor  = ComputeFogFactor(o.positionCS.z); // 규칙 8
                return o;
            }

            float4 frag(v2f i, bool front : SV_IsFrontFace) : SV_Target
            {
                // 패널 중심(uv=0.5)이 가장 밝고, 가장자리로 갈수록 _EdgeColor
                // 수업 근거: UV 좌표 계산, lerp로 그라데이션
                float2 center2d    = abs(i.uv - 0.5);            // 0=중심, 0.5=가장자리
                float  centerFactor = 1.0 - 2.0 * max(center2d.x, center2d.y);

                // 한지 결 텍스처 (없으면 white → 영향 없음)
                float  hanji = SAMPLE_TEXTURE2D(_HanjiMap, sampler_HanjiMap, i.uv * _HanjiTiling).r;

                float3 col = lerp(_EdgeColor.rgb, _CoreColor.rgb, saturate(centerFactor));
                col *= lerp(1.0, hanji, _HanjiStrength);
                col *= _GlowIntensity;

                // SV_IsFrontFace: 바깥면(front=true)은 밝게, 안쪽면은 약간 어둡게
                // 수업 근거: 셰이더 시스템 값 (SV_ 시맨틱)
                col *= front ? 1.0 : _InnerSideFactor;

                return float4(MixFog(col, i.fogFactor), 1.0); // 규칙 8
            }
            ENDHLSL
        }

        // ShadowCaster: 패널은 그림자를 드리우지 않음 (C4 InteriorLight가 창살로만 그림자 만들게)
        // 패널 MeshRenderer.shadowCastingMode = Off 로 설정 (Mid_HanjiPanelBuilder에서)
    }
}
