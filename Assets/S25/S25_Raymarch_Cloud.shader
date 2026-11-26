// ============================================================================
// S25_RaymarchCloud
// 쿼드를 창문으로 삼아, 그 너머에 있는 구름 덩어리를 레이마칭으로 그리는 셰이더
//
// 구(S25_RaymarchWindow)와 다른 점
//   - 구는 겉면이 있어서 "표면에 닿으면 멈춤"
//   - 구름은 겉면이 없어서, 덩어리 안을 작은 걸음으로 걸으며 걸음마다 짙기를 쌓아 감
//   - 쌓인 짙기가 이 픽셀의 불투명도(α)가 되고, S24의 알파 블렌딩으로 뒤의 장면과 섞임
// ============================================================================
Shader "CG/S25_RaymarchCloud"
{
    Properties
    {
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)      // 구름의 색 (구름 전체가 이 한 가지 색, 짙기에 따라 투명도만 달라짐)
        _Radius ("Radius", Range(0.05, 0.5)) = 0.4             // 구름 덩어리(구)의 반지름. 쿼드 기준 좌표의 길이
        _Density ("Density", Range(0, 20)) = 6                 // 구름이 얼마나 짙은지. 클수록 빨리 불투명해짐
        _NoiseScale ("Noise Scale", Range(1, 20)) = 6          // 짙고 옅은 얼룩의 크기. 클수록 얼룩이 잘아짐
    }
    SubShader
    {
        // RenderPipeline: URP용 셰이더라는 표시 (S20과 같음)
        // Queue = Transparent: 불투명한 물체를 모두 그린 뒤, 투명한 물체 순서로 그림 (S24)
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }          // URP가 이 Pass를 일반 그리기에 씀 (S20과 같음)

            // 알파 블렌딩 (S24): 결과 = 구름 색 × α + 이미 그려진 배경 × (1 − α)
            Blend SrcAlpha OneMinusSrcAlpha
            // 깊이 값을 쓰지 않음 (S24): 반투명한 구름이 뒤의 투명 물체를 깊이 테스트로 가리지 않게 함
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert                                 // 정점마다 실행할 함수 이름
            #pragma fragment frag                               // 픽셀마다 실행할 함수 이름
            // URP 셰이더 라이브러리: TransformObjectToHClip, TransformWorldToObject, GetCameraPositionWS, _Time 등이 여기에 있음
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // 조명 계산을 하지 않으므로 Lighting.hlsl은 넣지 않음

            // Properties의 값을 셰이더 안에서 쓰기 위한 변수 (이름이 Properties와 같아야 연결됨)
            float4 _CloudColor;
            float _Radius;
            float _Density;
            float _NoiseScale;

            // 메시에서 정점마다 받아 오는 값
            struct Attributes
            {
                float3 positionOS : POSITION;     // 쿼드 정점의 위치 (쿼드 기준 좌표)
            };

            // vert가 계산해 frag로 넘기는 값 (정점 사이의 픽셀에서는 자동으로 보간되어 들어옴)
            struct Varyings
            {
                float4 positionCS : SV_POSITION;  // 화면에 그릴 위치 (GPU가 픽셀을 정하는 데 씀)
                float3 positionOS : TEXCOORD0;    // 이 픽셀에 그려지는 쿼드 위의 점 (쿼드 기준 좌표) = 창의 칸
            };

            // 정점마다 한 번씩 실행
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);  // 쿼드 기준 좌표 → 화면에 그릴 위치
                output.positionOS = input.positionOS;                         // 쿼드 기준 좌표를 그대로 frag에 넘김 (광선 방향을 정할 때 씀)
                return output;
            }

            // ------------------------------------------------------------------
            // 구름 덩어리의 겉모양: 구의 SDF
            // 점 p에서 구 표면까지의 가장 짧은 거리 (밖이면 양수, 안이면 음수)
            // 구름에는 닿아서 멈출 겉면이 없지만, 덩어리가 어디 있는지(안인지 밖인지, 얼마나 먼지)는 알아야 하므로 씀
            // ------------------------------------------------------------------
            float SDF(float3 p)
            {
                return length(p) - _Radius;       // 쿼드의 기준점(0, 0, 0)에서 p까지의 거리 − 반지름
            }

            // ------------------------------------------------------------------
            // Hash: 위치를 넣으면 0~1 사이의 "무작위처럼 보이는" 값을 돌려줌
            // 같은 위치를 넣으면 늘 같은 값이 나옴 (프레임마다 바뀌지 않음)
            // 안의 숫자들(0.3183099, 0.1, 17)은 특별한 뜻이 없고, 결과가 고르게 흩어지도록 고른 값
            // ------------------------------------------------------------------
            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);    // 위치를 뒤섞어 소수 부분만 남김 (frac: S24의 Fraction과 같음)
                p = p * 17;                       // 값을 키워 다음 곱셈에서 더 크게 뒤섞이게 함
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));   // 세 성분을 곱하고 더해 섞은 뒤 소수 부분만 → 0~1
            }

            // ------------------------------------------------------------------
            // Noise: 공간 전체에 부드럽게 이어지는 얼룩 (값 0~1)
            // 방법: 공간을 크기 1짜리 정육면체 격자로 나누고, 격자의 꼭짓점마다 Hash로 무작위 값을 정함
            //       그 사이의 점은 둘러싼 꼭짓점 8개의 값을 거리에 따라 섞어서 정함
            // Hash만 쓰면 점마다 값이 제멋대로 튀어 지지직거리는 잡음이 되지만,
            // 꼭짓점 사이를 섞으면 값이 부드럽게 바뀌어 구름 같은 얼룩이 됨
            // ------------------------------------------------------------------
            float Noise(float3 x)
            {
                float3 i = floor(x);              // 이 점이 들어 있는 격자 칸의 한쪽 꼭짓점 (각 성분을 내림한 정수)
                float3 f = frac(x);               // 칸 안에서의 위치 (각 성분 0~1)

                // 칸 안의 위치를 S24의 Smoothstep과 같은 곡선으로 바꿈
                // 그냥 f를 쓰면 칸의 경계에서 값의 변화가 꺾여 격자 무늬가 보이고, 이 곡선을 쓰면 경계에서 부드럽게 이어짐
                f = f * f * (3 - 2 * f);

                // 꼭짓점 8개의 값을 세 번에 걸쳐 섞음 (lerp: S22의 선형 보간)
                //   1) x 방향으로 섞어 4개로
                //   2) y 방향으로 섞어 2개로
                //   3) z 방향으로 섞어 1개로
                return lerp(lerp(lerp(Hash(i + float3(0, 0, 0)), Hash(i + float3(1, 0, 0)), f.x),   // 아래 앞 모서리: x로 섞음
                                 lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x),   // 위 앞 모서리: x로 섞음
                                 f.y),                                                              // 앞면: y로 섞음
                            lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x),   // 아래 뒤 모서리: x로 섞음
                                 lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x),   // 위 뒤 모서리: x로 섞음
                                 f.y),                                                              // 뒷면: y로 섞음
                            f.z);                                                                   // 앞면과 뒷면: z로 섞음
            }

            // ------------------------------------------------------------------
            // CloudDensity: 위치 p에서 구름이 얼마나 짙은지 (0 = 없음)
            //   덩어리 가운데일수록 짙고(shape), 노이즈로 얼룩지게 하고(Noise), 시간에 따라 무늬가 위로 흘러감
            // ------------------------------------------------------------------
            float CloudDensity(float3 p)
            {
                // shape: 구 표면에서 0, 가운데로 갈수록 커져 중심에서 1
                // -SDF(p)는 안쪽으로 얼마나 들어왔는지(안쪽이면 양수), 반지름으로 나눠 0~1로 맞춤
                // saturate: 0보다 작으면 0, 1보다 크면 1로 자름 (덩어리 밖은 0)
                float shape = saturate(-SDF(p) / _Radius);

                // 노이즈를 읽을 위치: 크기를 _NoiseScale배로 늘려 얼룩을 잘게 하고, 시간에 따라 y를 빼서 무늬가 위로 흐르게 함
                // (S24 실습에서 Offset에 음수를 써야 위로 흘렀던 것과 같은 원리)
                float3 q = p * _NoiseScale + float3(0, -_Time.y * 0.3, 0);

                // 두 값을 곱함: 덩어리 밖(shape 0)이나 노이즈가 옅은 곳(Noise 0 근처)은 옅어짐
                return shape * Noise(q);
            }

            // ------------------------------------------------------------------
            // frag() 한 번 = 픽셀 하나의 색과 불투명도를 정하는 일
            // GPU가 화면에서 쿼드가 차지하는 픽셀마다 frag()를 한 번씩 실행함
            // ------------------------------------------------------------------
            float4 frag(Varyings input) : SV_Target
            {
                // [1] 광선 정하기: 카메라에서 출발해 이 픽셀(창의 칸)을 지나는 반직선
                // 카메라 위치(월드 좌표)를 쿼드 기준 좌표로 바꿈 → 구름 덩어리(쿼드 기준 좌표)와 같은 기준으로 맞춤
                float3 cameraOS = TransformWorldToObject(GetCameraPositionWS());
                // 광선 방향: 카메라에서 이 픽셀(창의 칸)을 향하는 쪽, 길이 1
                float3 rayDir = normalize(input.positionOS - cameraOS);

                float3 p = cameraOS;                     // 현재 위치: 카메라에서 시작
                float traveled = 0;                      // 지금까지 광선을 따라 옮긴 거리
                float alpha = 0;                         // 지금까지 쌓인 불투명도 (0 = 완전 투명, 1 = 완전 불투명)

                float stepSize = 0.02;                   // 덩어리 안에서 걷는 걸음 (늘 같음). 작을수록 곱지만 걸음이 많이 필요함

                // [2] 광선을 따라 걷기 (최대 128번)
                for (int i = 0; i < 128; i++)
                {
                    float dist = SDF(p);                 // 현재 위치에서 구름 덩어리 표면까지의 거리

                    if (dist > 0)
                    {
                        // [덩어리 밖] 구의 레이마칭과 같음: SDF만큼 옮겨 덩어리에 다가감
                        // 다만 표면 바로 앞에서 걸음이 0에 가까워져 멈춘 듯 머무는 일이 없도록, 최소 stepSize만큼은 옮김
                        float move = max(dist, stepSize);
                        p = p + move * rayDir;
                        traveled = traveled + move;
                    }
                    else
                    {
                        // [덩어리 안] 겉면이 없으므로 멈추지 않음
                        // 이 자리의 짙기로 이번 걸음에서 생기는 불투명도 a를 구함
                        //   짙을수록, _Density가 클수록, 걸음이 길수록 커짐
                        float a = CloudDensity(p) * _Density * stepSize;

                        // 쌓기: 이번 a를 아직 투명하게 남은 몫(1 − alpha)에만 더함
                        //   앞(카메라 쪽)에 이미 쌓인 구름이 뒤쪽 구름을 그만큼 가림
                        //   S24 공식(전경 × α + 배경 × (1 − α))과 같은 원리를 걸음마다 되풀이하는 것
                        alpha = alpha + (1 - alpha) * a;

                        // 작은 걸음으로 한 칸 더 들어감
                        p = p + stepSize * rayDir;
                        traveled = traveled + stepSize;
                    }

                    // [3] 멈추는 때
                    //   거의 불투명해졌으면 더 뒤는 어차피 가려져 안 보이므로 멈춤
                    //   카메라에서 너무 멀리 왔으면 멈춤
                    if (alpha > 0.99 || traveled > 100)
                    {
                        break;
                    }
                }

                // [4] 구름을 전혀 지나지 않았으면(불투명도가 거의 0) 이 픽셀은 그리지 않음 → 뒤의 장면이 그대로 보임
                if (alpha < 0.001)
                {
                    discard;
                }

                // [5] 구름 색과 쌓인 불투명도를 내보냄
                // 이 α로 S24의 알파 블렌딩이 일어나 뒤의 장면과 섞임 (짙은 곳은 하얗게, 옅은 곳은 뒤가 비침)
                return float4(_CloudColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}