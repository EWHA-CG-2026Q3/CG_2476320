// 청사초롱 비단(submesh 0·1)용 발광 셰이더 (REQ_Lantern.md §5)
// Mid_SnowCover.shader 구조를 기반으로 발광 계산 추가
// 살·덮개·고리·줄·술은 CG/Mid_SnowCover(_SnowAmount=0) 사용
Shader "CG/Mid_Lantern"
{
    Properties
    {
        _SilkColor      ("홍사 색", Color)            = (0.722, 0.267, 0.165, 1) // #B8442A — 주황쪽으로 조정
        _BandColor      ("청사 띠 색", Color)          = (0.082, 0.275, 0.522, 1) // #154685 팔레트 WoodShade
        _BandTop        ("띠 위 경계 (v)", Range(0,1)) = 0.88
        _BandBottom     ("띠 아래 경계 (v)", Range(0,1)) = 0.12
        _SilkMap        ("비단 결 텍스처", 2D)         = "white" {}
        _SilkTiling     ("비단 타일링", Float)         = 1.0
        _SilkStrength   ("비단 결 강도", Range(0,1))   = 0.3
        [HDR]
        _GlowCoreColor  ("발광 가운데 색 (HDR)", Color) = (0.996, 0.863, 0.749, 1) // #FEDCBF — 불에 가까운 살구색
        [HDR]
        _GlowEdgeColor  ("발광 가장자리 색 (HDR)", Color) = (1.0, 0.478, 0.180, 1) // #FF7A2E — 비단 통과 주황
        _GlowIntensity  ("발광 세기", Float)            = 1.4                     // 1.4: 채널 클리핑 없이 Bloom 활용
        _GlowFalloff    ("세로 발광 분포", Float)       = 1.5
        _InnerBoost     ("안쪽면 발광 배율", Float)     = 1.0                     // SilkInner 머티리얼에서 1.5로
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        // ── ForwardLit ────────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog      // 규칙 8: 안개 지원

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_SilkMap); SAMPLER(sampler_SilkMap);

            float4 _SilkColor, _BandColor, _GlowCoreColor, _GlowEdgeColor;
            float  _BandTop, _BandBottom;
            float  _SilkTiling, _SilkStrength;
            float  _GlowIntensity, _GlowFalloff, _InnerBoost;

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;  // 규칙 8
            };

            v2f vert(float3 posOS : POSITION, float3 normalOS : NORMAL, float2 uv : TEXCOORD0)
            {
                v2f o;
                float3 posWS  = TransformObjectToWorld(posOS);
                o.positionCS  = TransformWorldToHClip(posWS);
                o.positionWS  = posWS;
                o.normalWS    = TransformObjectToWorldNormal(normalOS);
                o.uv          = uv;
                o.fogFactor   = ComputeFogFactor(o.positionCS.z);  // 규칙 8
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // 1. 청사/홍사 구분 (UV v 기준 — 위아래 띠가 청사)
                bool   isBand   = i.uv.y < _BandBottom || i.uv.y > _BandTop;
                float3 baseCol  = isBand ? _BandColor.rgb : _SilkColor.rgb;
                // 비단 결 텍스처 (한지 느낌, 선택 사항 — _SilkMap 없으면 white)
                float  silkTex  = SAMPLE_TEXTURE2D(_SilkMap, sampler_SilkMap, i.uv * _SilkTiling).r;
                baseCol        *= lerp(1.0, silkTex, _SilkStrength);

                // 2. 세로 발광 분포: sin(π*v)^falloff → 가운데 불빛이 가장 밝게
                // (수업 근거: 삼각함수 — sin(π*v)는 0과 1에서 0, 가운데에서 1)
                float  vertical = pow(sin(PI * i.uv.y), _GlowFalloff);
                vertical        = saturate(vertical);

                // 3. N·V 내적 → facing (수업 근거: 벡터 내적, 정면=1 / 가장자리=0)
                float3 N      = normalize(i.normalWS);
                float3 V      = normalize(GetCameraPositionWS() - i.positionWS);
                float  facing = abs(dot(N, V));

                // 4. 발광 2단 색: 가운데(vertical*facing→1)는 살구색, 바깥은 주황
                // 원인: #FEDCBF×2.5 → 흰색, #A83226 섞음 → 분홍. 2단 직접 지정으로 해결
                float3 glowColor = lerp(_GlowEdgeColor.rgb, _GlowCoreColor.rgb,
                                        saturate(vertical * facing));
                // 청사 띠: 발광 색 × BandColor → 어둡고 탁한 남보라 (검게 끊기지 않게)
                glowColor = isBand ? glowColor * _BandColor.rgb * 2.0 : glowColor;
                float3 glow = glowColor * _GlowIntensity * _InnerBoost;

                // 5. 약한 환경광 + 발광 → Bloom이 1보다 큰 값에서 번짐
                float3 finalCol = baseCol * 0.05 + glow;

                return float4(MixFog(finalCol, i.fogFactor), 1.0);  // 규칙 8
            }
            ENDHLSL
        }

        // ── ShadowCaster ──────────────────────────────────────────────
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
