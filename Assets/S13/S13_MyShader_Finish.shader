Shader "CG/S13_MyShader_Finish"                                // [변경] 이름
{
    Properties                                              // [추가]
    {                                                       // [추가]
        _BaseColor ("Base Color", Color) = (1, 0, 0, 1)     // [추가]
    }                                                       // [추가]

    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)                 // [추가]
                half4 _BaseColor;                           // [추가]
            CBUFFER_END                                     // [추가]

            float4 vert(float3 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS);
            }

            half4 frag() : SV_Target
            {
                return _BaseColor;                          // [변경]
            }
            ENDHLSL
        }
    }
}