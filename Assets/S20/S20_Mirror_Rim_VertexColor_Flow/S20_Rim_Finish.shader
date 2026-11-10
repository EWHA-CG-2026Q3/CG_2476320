Shader "CG/S20_Rim_Finish"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2, 0.25, 0.5, 1)    // 고리가 잘 보이도록 어두운 남색
        _Ambient ("Ambient", Range(0, 1)) = 0.2
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess ("Shininess", Range(1, 256)) = 32
        [HDR] _RimColor ("Rim Color", Color) = (0.3, 0.8, 1, 1)   // 새로: 윤곽의 색 (HDR: 1을 넘는 밝기도 고를 수 있음)
        _RimPower ("Rim Power", Range(0.5, 8)) = 5                // 새로: 클수록 고리가 얇아짐
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
            float4 _RimColor;
            float _RimPower;

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
                float3 R = reflect(-L, N);                       // 빛이 튕겨 나가는 방향

                float diffuse  = max(0, dot(N, L));                       // 확산광의 밝기
                float specular = pow(max(0, dot(R, V)), _Shininess);      // 반사광의 밝기

                // 새로: dot(N, V)는 카메라를 정면으로 향한 가운데가 1, 윤곽이 0
                //       1에서 빼면 거꾸로 윤곽이 1, 가운데가 0
                float rim = pow(saturate(1 - dot(N, V)), _RimPower);     // 윤곽일수록 1 (saturate: 0–1 밖의 값을 막음)

                float3 color = _BaseColor.rgb * (_Ambient + diffuse * mainLight.color)
                             + _SpecularColor.rgb * specular * mainLight.color
                             + _RimColor.rgb * rim;              // 새로: 윤곽에만 색을 더함 (빛과 상관없음)
                return float4(color, 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"    // 그림자 드리우기
    }
}