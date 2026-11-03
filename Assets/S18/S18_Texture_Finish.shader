Shader "CG/S18_Texture_Finish"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0, 0, 0, 1)
        [HDR] _EmissionColor ("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionMap ("Emission Map", 2D) = "white" {}            // S17처럼 텍스처 칸
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _BaseColor;
            float4 _EmissionColor;
            sampler2D _EmissionMap;                                      // S17처럼 텍스처 받기

            void vert(float3 positionOS : POSITION,                  // S17처럼 UV 좌표 넘기기
                      float2 uv : TEXCOORD0,
                      out float2 uvOut : TEXCOORD0,
                      out float4 positionCS : SV_POSITION)
            {
                uvOut = uv;
                positionCS = TransformObjectToHClip(positionOS);
            }

            float4 frag(float2 uv : TEXCOORD0) : SV_Target
            {
                float3 crack = tex2D(_EmissionMap, uv).rgb;                    // S17처럼 텍스처 읽기
                return float4(_BaseColor.rgb + _EmissionColor.rgb * crack, 1); // crack을 곱함
            }

            ENDHLSL
        }
    }
}