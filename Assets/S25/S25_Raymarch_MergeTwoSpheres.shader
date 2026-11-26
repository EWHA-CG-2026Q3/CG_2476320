Shader "CG/S25_Raymarch_MergeTwoSpheres"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0.5, 0.2, 1)
        _Radius ("Radius", Range(0.05, 0.5)) = 0.35            // 구의 반지름
        _Ambient ("Ambient", Range(0, 1)) = 0.2
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess ("Shininess", Range(1, 1024)) = 128
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
            float _Radius;
            float _Ambient;
            float4 _SpecularColor;
            float _Shininess;

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;    // 이 픽셀에 그려지는 쿼드 위의 점 (쿼드 기준 좌표) = 창의 칸
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.positionOS = input.positionOS;
                return output;
            }

            /////////////////////////////////////////////////////////
            // 두 구를 부드럽게 녹여 붙인 SDF
            /////////////////////////////////////////////////////////            
            float SDF(float3 p)
            {
                // 두 구의 중심: 시간에 따라 서로 다가갔다 멀어졌다 함
                float3 c1 = float3( sin(_Time.y) * 0.25, 0, 0);   // 구 1의 중심
                float3 c2 = float3(-sin(_Time.y) * 0.25, 0, 0);   // 구 2의 중심 (반대로 움직임)

                // 각 구까지의 거리 (구 하나의 SDF와 같음. 중심이 원점이 아니어서 p - 중심으로 씀)
                float d1 = length(p - c1) - 0.15;
                float d2 = length(p - c2) - 0.15;

                // [그냥 합치기] 두 구 중 더 가까운 쪽까지의 거리
                // return min(d1, d2);
                // → 두 구가 겹치면 그대로 겹쳐 보이고, 만나는 곳에 각진 이음매가 생김

                // [부드럽게 합치기] 두 거리가 비슷한 곳(두 구 사이)에서만 둘을 섞고, 조금 더 작게 만듦
                float k = 0.1;                                    // 녹아 붙는 범위: 클수록 더 멀리서부터 이어짐
                float h = saturate(0.5 + 0.5 * (d2 - d1) / k);    // 두 거리의 차이로 정한 섞는 비율 (0~1)
                return lerp(d2, d1, h) - k * h * (1 - h);
            }

            // 닿은 점 p의 normal: SDF가 가장 빨리 커지는 방향
            // x, y, z 축으로 각각 아주 조금(0.001)씩 +, − 쪽으로 옮겨 SDF의 차이를 구하고, 세 차이를 묶음
            float3 GetNormal(float3 p)
            {
                float dx = SDF(p + float3(0.001, 0, 0)) - SDF(p - float3(0.001, 0, 0));
                float dy = SDF(p + float3(0, 0.001, 0)) - SDF(p - float3(0, 0.001, 0));
                float dz = SDF(p + float3(0, 0, 0.001)) - SDF(p - float3(0, 0, 0.001));
                return normalize(float3(dx, dy, dz));
            }

            // frag() 한 번 = 픽셀 하나의 색을 정하는 일
            // GPU가 쿼드가 덮는 픽셀마다 frag()를 한 번씩 실행함
            float4 frag(Varyings input) : SV_Target
            {
                // [1] 광선 정하기: 카메라에서 출발해 이 픽셀(창의 칸)을 지나는 반직선
                // 카메라 위치를 쿼드 기준 좌표로 바꿈 (구가 쿼드 기준 좌표로 정해져 있으므로)
                float3 cameraOS = TransformWorldToObject(GetCameraPositionWS());
                // 광선 방향: 카메라에서 이 픽셀(창의 칸)을 향하는 쪽 (길이 1)
                float3 rayDir = normalize(input.positionOS - cameraOS);

                // [1] 현재 위치를 카메라 위치에 둠
                float3 p = cameraOS;
                float traveled = 0;                              // 지금까지 광선을 따라 옮긴 거리
                bool hit = false;                                // 구에 닿았는지

                // [6] 2~5를 최대 64번 되풀이
                for (int i = 0; i < 64; i++)
                {
                    // [2] 현재 위치에서 구 표면까지의 가장 짧은 거리
                    float dist = SDF(p);

                    // [3] 거리가 거의 0이면 현재 위치가 구 표면 위에 있음 → 닿음, 멈춤
                    if (dist < 0.001)
                    {
                        hit = true;
                        break;
                    }

                    // [4] 현재 위치를 광선 방향으로 dist만큼 옮김
                    p = p + dist * rayDir;
                    traveled = traveled + dist;

                    // [5] 카메라에서 100 넘게 왔으면 구에 닿지 않은 것 → 멈춤
                    if (traveled > 100)
                    {
                        break;
                    }
                }

                // 닿지 않았으면 이 픽셀은 그리지 않음 (뒤의 장면이 보임)
                if (!hit)
                {
                    discard;
                }

                // 닿았으면 반복을 멈춘 현재 위치 p가 이 픽셀에 보이는 구 표면의 점
                // 그 점의 normal을 SDF로 구하고, normal과 위치를 월드 좌표로 바꿈 (빛과 카메라가 월드 좌표이므로)
                float3 N = TransformObjectToWorldNormal(GetNormal(p));
                float3 positionWS = TransformObjectToWorld(p);

                // Blinn-Phong: 여기부터는 S20과 같음
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;                  // 빛이 있는 쪽
                float3 V = normalize(GetCameraPositionWS() - positionWS);  // 카메라가 있는 쪽
                float3 H = normalize(L + V);                     // 빛이 있는 쪽과 카메라가 있는 쪽의 한가운데

                float diffuse = max(0, dot(N, L));               // 확산광의 밝기
                float specular = pow(max(0, dot(N, H)), _Shininess);   // 반사광의 밝기

                float3 color = _BaseColor.rgb * (_Ambient + diffuse * mainLight.color)
                             + _SpecularColor.rgb * specular * mainLight.color;
                return float4(color, 1);
            }
            ENDHLSL
        }
    }
}