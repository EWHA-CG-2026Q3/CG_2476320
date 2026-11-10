Shader "CG/S20_PhongStep5_Ambient"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.8, 0.8, 0.8, 1)
        _Ambient ("Ambient", Range(0, 1)) = 0.2          // 주변광: 계산하지 않고 손으로 정하는 값
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float4 _BaseColor;
            float _Ambient;

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
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;                  // 빛이 있는 쪽

                // dot(N, L): normal과 빛이 있는 쪽 사이 각도의 cos. 정면이면 1, 옆이면 0, 등지면 음수
                // max(0, …): 빛을 등진 쪽의 음수를 0으로 막음
                float diffuse = max(0, dot(N, L));               // 확산광의 밝기

                float3 color = _BaseColor.rgb * (_Ambient + diffuse * mainLight.color);   // 앞서 사용하던 0.2 대신 _Ambient
                return float4(color, 1);
            }
            ENDHLSL
        }
    }
}