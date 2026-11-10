Shader "CG/S20_VertexColor_Finish"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)            // 정점 색에 곱하는 색 (흰색이면 정점 색 그대로)
        _Ambient ("Ambient", Range(0, 1)) = 0.2
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess ("Shininess", Range(1, 256)) = 32
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
                float4 color      : COLOR;                   // 새로: 메시의 정점 색 (mesh.colors)
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 color      : TEXCOORD2;               // 새로: 정점 색을 frag로 넘김
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.color      = input.color;             // 새로: 그대로 넘김 (삼각형 안에서는 보간됨)
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;                  // 빛이 있는 쪽
                float3 V = normalize(GetCameraPositionWS() - input.positionWS);  // 카메라가 있는 쪽
                float3 R = reflect(-L, N);                       // 빛이 튕겨 나가는 방향

                float diffuse  = max(0, dot(N, L));                       // 확산광의 밝기
                float specular = pow(max(0, dot(R, V)), _Shininess);      // 반사광의 밝기

                float3 surfaceColor = input.color.rgb * _BaseColor.rgb;   // 새로: Base Color 대신 정점 색

                float3 color = surfaceColor * (_Ambient + diffuse * mainLight.color)
                             + _SpecularColor.rgb * specular * mainLight.color;
                return float4(color, 1);
            }
            ENDHLSL
        }
    }
}