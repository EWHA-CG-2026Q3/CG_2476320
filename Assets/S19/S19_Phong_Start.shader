Shader "CG/S19_Phong_Start"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.8, 0.8, 0.8, 1)
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess ("Shininess", Range(1, 256)) = 32
        _Ambient ("Ambient", Range(0, 1)) = 0.1
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
            float4 _SpecularColor;
            float _Shininess;
            float _Ambient;

            struct Attributes                       // 메시에서 vert로 들어오는 값
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings                         // vert에서 frag로 넘기는 값
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
                float3 N = normalize(input.normalWS);                            // N: normal
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;                                  // L: 빛이 있는 쪽
                float3 V = normalize(GetCameraPositionWS() - input.positionWS);  // V: 카메라가 있는 쪽
                float3 R = reflect(-L, N);                                       // R: 빛이 튕겨 나가는 방향

                float diffuse  = max(0, dot(N, L));                              // 확산광
                float specular = pow(max(0, dot(R, V)), _Shininess);             // 반사광 (아직 쓰지 않음)

                float3 color = _BaseColor.rgb * (_Ambient + diffuse * mainLight.color);   // 고칠 줄
                return float4(color, 1);
            }
            ENDHLSL
        }
    }
}