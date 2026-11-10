Shader "CG/S20_Mirror_Finish"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.8, 0.8, 0.8, 1)
        _Ambient ("Ambient", Range(0, 1)) = 0.2
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess ("Shininess", Range(1, 256)) = 32
        _EnvCube ("Environment Cubemap", Cube) = "" {}            // 새로: 비칠 주변 (큐브맵 그림)
        _Reflectivity ("Reflectivity", Range(0, 1)) = 1           // 주변이 얼마나 비치는지 (0이면 Phong, 1이면 거울)
        _Roughness ("Roughness", Range(0, 1)) = 0                 // 0이면 또렷한 거울, 클수록 흐릿함
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
            float _Reflectivity;
            float _Roughness;

            TEXTURECUBE(_EnvCube);                               // 새로: 큐브맵 텍스처
            SAMPLER(sampler_EnvCube);                            // 새로: 그 텍스처를 읽는 방법 (필터링, 래핑)

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

                float3 phong = _BaseColor.rgb * (_Ambient + diffuse * mainLight.color)
                             + _SpecularColor.rgb * specular * mainLight.color;

                // 눈에서 온 시선(-V)이 표면에서 튕겨 나가는 방향. 빛의 R과 같은 reflect, 넣는 방향만 다름
                float3 viewReflect = reflect(-V, N);
                // 그 방향에 있는 주변의 색을 큐브맵에서 읽음. Roughness가 클수록 작은(흐린) 밉맵에서 읽음
                float3 env = SAMPLE_TEXTURECUBE_LOD(_EnvCube, sampler_EnvCube, viewReflect, _Roughness * 6).rgb;

                float3 color = lerp(phong, env, _Reflectivity);  // Phong의 색과 주변의 색을 섞음
                return float4(color, 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"    // 그림자 드리우기
    }
}