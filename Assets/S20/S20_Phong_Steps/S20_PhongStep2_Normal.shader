Shader "CG/S20_PhongStep2_Normal"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.8, 0.8, 0.8, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _BaseColor;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
            
                // float brightness = 1;                                  // 고칠 줄: 그 픽셀의 밝기. 0.1, 0.5, 1.0, 1.5, 2.0 등등 (평면으로 보임)
                float brightness = N.y;                                // 위가 밝고, 옆과 아래는 검음
                // float brightness = -N.y;                               // 위가 밝고, 옆과 아래는 검음
                // float brightness = N.x;                                // 오른쪽이 밝음
                // float brightness = -N.x;                               // 왼른쪽이 밝음
                // float brightness = dot(N, normalize(float3(1, 1, 0))); // 오른쪽 위가 밝음
                // float brightness = dot(N, normalize(float3(1, 1, 0))) + 0.2f; // 오른쪽 위가 밝음 + 약간의 주변광 (미흡)
                // float brightness = max(0, dot(N, normalize(float3(1, 1, 0)))) + 0.2f; // 오른쪽 위가 밝음 + 약간의 주변광 (개선)
            
                float3 color = _BaseColor.rgb * brightness;
                return float4(color, 1);

            }
            ENDHLSL
        }
    }
}