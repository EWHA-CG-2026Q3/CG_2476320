Shader "CG/Mid_SnowTexture"
{
    Properties
    {
        _BaseColor      ("Base Color", Color) = (0.88, 0.92, 1.0, 1)   // 약간 푸른 흰색
        _DetailMap      ("Detail Map", 2D) = "white" {}                  // S18처럼 텍스처 받기
        _Tiling         ("Tiling", Float) = 8                            // S18처럼 반복 횟수
        _DetailStrength ("Detail Strength", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog                                    // 규칙 8: 안개 지원
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4    _BaseColor;
            sampler2D _DetailMap;
            float     _Tiling;
            float     _DetailStrength;

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float  fogFactor  : TEXCOORD1;                          // 규칙 8: 안개 계수
            };

            v2f vert(float3 positionOS : POSITION, float2 uv : TEXCOORD0)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(positionOS);
                o.uv         = uv;
                o.fogFactor  = ComputeFogFactor(o.positionCS.z);        // 규칙 8
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // S18처럼 텍스처 읽기 + BaseColor 곱하기
                float detail = tex2D(_DetailMap, i.uv * _Tiling).r;
                float3 col = _BaseColor.rgb * lerp(1, detail, _DetailStrength);
                return float4(MixFog(col, i.fogFactor), 1);             // 규칙 8
            }
            ENDHLSL
        }
    }
}
