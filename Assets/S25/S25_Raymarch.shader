Shader "CG/S25_Raymarch"
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
            // 구의 SDF: 점 p에서 구 표면까지의 가장 짧은 거리
            // 구의 중심은 쿼드의 기준점(0, 0, 0), 반지름은 _Radius
            /////////////////////////////////////////////////////////
            /*
            float SDF(float3 p)
            {
                return length(p) - _Radius;
            }
            */

            /////////////////////////////////////////////////////////
            // 상자의 SDF: 점 p에서 상자 표면까지의 가장 짧은 거리 (밖이면 양수, 안이면 음수)
            // b: 상자의 중심에서 각 면까지의 거리 (상자 크기의 절반)
            /////////////////////////////////////////////////////////
            /*
            float SDF(float3 p)
            {
                float3 b = float3(0.25, 0.25, 0.25);          // 한 변이 0.5인 정육면체

                // abs(p): 상자는 위아래·좌우·앞뒤가 대칭이므로, p를 모두 양수 쪽(+x, +y, +z)으로 옮겨 생각함
                // q: 축마다 p가 면에서 얼마나 벗어났는지
                //    양수면 그 축 방향으로 면 바깥에 있고, 음수면 그 축 방향으로는 면 안쪽에 있음
                float3 q = abs(p) - b;

                // [바깥 부분] p가 상자 밖에 있을 때 표면까지의 거리
                // max(q, 0): 면 바깥으로 벗어난 축의 값만 남기고, 안쪽인 축은 0으로 지움
                //   벗어난 축이 하나면 → 그 면까지의 거리
                //   벗어난 축이 둘이면 → 모서리(변)까지의 거리
                //   벗어난 축이 셋이면 → 꼭짓점까지의 거리
                // length(…): 남은 값들로 만든 화살표의 길이 = 가장 가까운 면·모서리·꼭짓점까지의 직선거리
                // p가 상자 안에 있으면 세 축 모두 음수라 전부 0이 되어, 이 값은 0
                float outside = length(max(q, 0));

                // [안쪽 부분] p가 상자 안에 있을 때 표면까지의 거리 (음수)
                // max(q.x, max(q.y, q.z)): 세 축 가운데 0에 가장 가까운 값 = 가장 가까운 면까지 얼마나 들어와 있는지
                // min(…, 0): p가 상자 밖이면(이 값이 양수면) 0으로 지움 → 바깥에서는 이 값이 결과에 영향을 주지 않음
                float inside = min(max(q.x, max(q.y, q.z)), 0);

                // 밖이면 outside만 남고(inside = 0), 안이면 inside만 남음(outside = 0)
                return outside + inside;
            }
            */

            /////////////////////////////////////////////////////////
            // 다이아몬드의 SDF: 점 p에서 다이아몬드 표면까지의 거리 (밖이면 양수, 안이면 음수)
            // a: 중심에서 가운데 정사각형의 각 변까지의 거리 (정사각형 크기의 절반)
            // h: 중심에서 위(아래) 꼭짓점까지의 높이
            // S08의 다이아몬드는 a = h (정사각형 반폭 0.5, 높이 0.5)
            /////////////////////////////////////////////////////////
            /*
            float SDF(float3 p)
            {
                float a = 0.35;
                float h = 0.35;

                // 다이아몬드는 면 8개로 이루어지고, 면은 모두 평면임
                // 위아래·좌우·앞뒤가 대칭이므로, p를 모두 양수 쪽(+x, +y, +z)으로 옮겨 생각하면
                // 위쪽 면 가운데 +x를 향한 면과 +z를 향한 면, 두 개만 따지면 됨
                float3 q = abs(p);

                // +x를 향한 면의 평면: x / a + y / h = 1
                // +z를 향한 면의 평면: z / a + y / h = 1
                // 두 면 중 p가 더 바깥으로 벗어난 쪽을 고름 → max(x, z)
                // 그러면 다이아몬드 안쪽은 max(x, z) / a + y / h < 1, 표면은 = 1, 바깥은 > 1
                float k = max(q.x, q.z) / a + q.y / h - 1;

                // k는 "평면 식에 넣은 값"이라 아직 거리가 아님
                // 평면 식의 계수 (1/a, 1/h)로 만든 normal 방향의 길이로 나누면 평면까지의 거리가 됨
                // (S08·S19의 내적: 점과 평면 사이 거리 = 평면의 normal 방향으로 잰 길이)
                return k / sqrt(1 / (a * a) + 1 / (h * h));
            }
            */

            /////////////////////////////////////////////////////////
            // 별의 SDF: 점 p에서 입체 오각별 표면까지의 거리 (밖이면 양수, 안이면 음수)
            // 별은 xy 평면에 놓이고(꼭짓점 하나가 +y를 향함), z 방향으로 두께를 가짐
            /////////////////////////////////////////////////////////
            float SDF(float3 p)
            {
                float r  = 0.4;    // 중심에서 별 끝(바깥 꼭짓점)까지의 거리
                float rf = 0.45;   // 안쪽 꼭짓점이 얼마나 들어가는지 (작을수록 뾰족한 별)
                float t  = 0.08;   // 두께의 절반 (중심에서 앞면·뒷면까지)

                // ── 1단계: 평면 위 오각별까지의 거리 (xy만 사용) ──
                float2 q = p.xy;

                // 별은 좌우 대칭이고, 72°마다 같은 모양이 되풀이됨
                // 상자에서 abs로 한 칸에 몰아넣었듯, 접기를 거듭해 q를 별의 "끝 하나의 오른쪽 절반" 조각 안으로 옮김
                float2 k1 = float2(0.809016994, -0.587785252);   // 접는 기준선의 방향 (cos 36°, −sin 36°)
                float2 k2 = float2(-k1.x, k1.y);                 // 반대쪽 기준선
                q.x = abs(q.x);                                  // 좌우 대칭 → 오른쪽으로
                q -= 2 * max(dot(k1, q), 0) * k1;                // 기준선 k1 너머에 있으면 거울처럼 접어 넘김
                q -= 2 * max(dot(k2, q), 0) * k2;                // 기준선 k2 너머에 있으면 거울처럼 접어 넘김
                q.x = abs(q.x);                                  // 다시 좌우 대칭 → 오른쪽으로

                // 이제 q는 위쪽 끝 하나 근처에만 있음 → 그 끝의 변 하나(선분)까지의 거리만 구하면 됨
                q.y -= r;                                                     // 별 끝을 원점으로 옮김
                float2 ba = rf * float2(-k1.y, k1.x) - float2(0, 1);         // 별 끝에서 안쪽 꼭짓점으로 가는 변의 방향
                float s = clamp(dot(q, ba) / dot(ba, ba), 0, r);            // 변 위에서 q에 가장 가까운 지점 (변의 범위 안으로 제한)
                float d2 = length(q - ba * s) * sign(q.y * ba.x - q.x * ba.y);  // 그 지점까지의 거리, 별 안쪽이면 음수

                // ── 2단계: 두께만큼 앞뒤로 늘리기 ──
                // 2D 별까지의 거리(d2)와 앞뒤 면까지의 거리(|z| − t)를 상자 SDF와 같은 방식으로 합침
                float2 w = float2(d2, abs(p.z) - t);
                return min(max(w.x, w.y), 0) + length(max(w, 0));
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