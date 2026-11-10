Shader "CG/S20_Water_Finish"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}                    // 새로: 물 무늬 텍스처
        _Flow ("Flow", Vector) = (0.1, 0, 0, 0)                   // 새로: 흐르는 방향과 속도 (x, y만 씀)
        _Ambient ("Ambient", Range(0, 1)) = 0.2
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess ("Shininess", Range(1, 1024)) = 256            // 물은 매끈해서 하이라이트가 작고 또렷하게
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

            sampler2D _BaseMap;
            float4 _BaseMap_ST;                                  // 새로: 재질의 Tiling(xy)과 Offset(zw)
            float4 _Flow;
            float _Ambient;
            float4 _SpecularColor;
            float _Shininess;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;                   // 새로: 메시의 UV0
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;                   // 새로: UV를 frag로 넘김 (0, 1은 이미 씀)
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.uv         = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;   // 새로: Tiling과 Offset 적용
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;                  // 빛이 있는 쪽
                float3 V = normalize(GetCameraPositionWS() - input.positionWS);  // 카메라가 있는 쪽
                float3 H = normalize(L + V);                     // 빛이 있는 쪽과 카메라가 있는 쪽의 한가운데

                float diffuse  = max(0, dot(N, L));                       // 확산광의 밝기
                float specular = pow(max(0, dot(N, H)), _Shininess);      // 반사광의 밝기 (Blinn-Phong)

                // 새로: 시간에 따라 UV를 밀어 줌. UV가 1을 넘으면 Repeat로 처음부터 다시 읽음 (S18)
                float2 p = input.uv + _Time.y * _Flow.xy;
                float3 baseColor = tex2D(_BaseMap, p).rgb;       // 새로: Base Color 대신 흐르는 텍스처의 색

                float3 color = baseColor * (_Ambient + diffuse * mainLight.color)
                             + _SpecularColor.rgb * specular * mainLight.color;
                return float4(color, 1);
            }
            ENDHLSL
        }
    }
}