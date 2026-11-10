Shader "CG/S20_PhongStep10_BlinnPhong"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.8, 0.8, 0.8, 1)
        _Ambient ("Ambient", Range(0, 1)) = 0.2
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess ("Shininess", Range(1, 1024)) = 128       // Phong(32)과 비슷한 크기가 되도록 4배
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
            float4 _SpecularColor;
            float _Shininess;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;                  // 빛이 있는 쪽
                float3 V = normalize(GetCameraPositionWS() - input.positionWS);  // 카메라가 있는 쪽
                float3 H = normalize(L + V);                     // 새로: 빛이 있는 쪽과 카메라가 있는 쪽의 한가운데 (R 대신)

                // dot(N, L): normal과 빛이 있는 쪽 사이 각도의 cos. 정면이면 1, 옆이면 0, 등지면 음수
                // max(0, …): 빛을 등진 쪽의 음수를 0으로 막음
                float diffuse = max(0, dot(N, L));               // 확산광의 밝기

                // dot(N, H): normal이 한가운데 방향을 정면으로 향하면 1. 그 자리에서 빛이 눈으로 곧장 튕겨 옴
                // pow(…, _Shininess): Shininess번 곱해, 1에 아주 가까운 자리만 밝게 남김
                float specular = pow(max(0, dot(N, H)), _Shininess);   // 반사광의 밝기 (Phong의 dot(R, V) 대신)

                float3 color = _BaseColor.rgb * (_Ambient + diffuse * mainLight.color)
                             + _SpecularColor.rgb * specular * mainLight.color;
                return float4(color, 1);
            }
            ENDHLSL
        }
    }
}