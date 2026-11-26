Shader "CG/S25_ScreenSpaceReflection"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2, 0.2, 0.2, 1)  // 바닥 색
        _Reflectivity ("Reflectivity", Range(0, 1)) = 0.6      // 반사가 섞이는 정도
        _StepSize ("Step Size", Range(0.01, 0.5)) = 0.1         // 광선이 한 번에 걷는 거리 (일정함)
    }
    SubShader
    {
        // 불투명 물체가 모두 그려진 뒤에 그려야 "이미 그려진 장면"을 읽을 수 있음 → Transparent 순서로 그림
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"   // 화면에 그려진 장면의 깊이
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"  // 화면에 그려진 장면의 색

            float4 _BaseColor;
            float _Reflectivity;
            float _StepSize;

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
                float3 V = normalize(GetCameraPositionWS() - input.positionWS);   // 카메라가 있는 쪽

                // [1] 광선 정하기: 바닥의 이 점에서 출발해, 시선이 바닥에 튕겨 나가는 방향으로 뻗는 반직선
                // reflect(-V, N): 카메라에서 들어온 방향을 normal 기준으로 거울처럼 꺾은 방향 (S19의 R과 같음)
                float3 rayDir = reflect(-V, N);
                float3 p = input.positionWS;                     // 현재 위치는 바닥의 이 점에서 시작 (월드 좌표)

                float3 reflectColor = float3(0, 0, 0);
                bool hit = false;
                float2 lastValidUV = float2(0.5, 0.5);

                for (int i = 0; i < 64; i++)
                {
                    p = p + _StepSize * rayDir;
                    float2 uv = ComputeNormalizedDeviceCoordinates(p, UNITY_MATRIX_VP);

                    // 화면 밖으로 나간 경우 → 직전 유효 UV로 하늘색 샘플링
                    if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                    {
                        reflectColor = SampleSceneColor(lastValidUV);
                        hit = true;
                        break;
                    }

                    lastValidUV = uv;

                    float sceneDepth = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                    float rayDepth = -TransformWorldToView(p).z;

                    // 하늘 픽셀: 깊이가 far clip에 근접 → 하늘색 샘플링
                    if (sceneDepth > _ProjectionParams.z * 0.95)
                    {
                        reflectColor = SampleSceneColor(uv);
                        hit = true;
                        break;
                    }

                    if (rayDepth > sceneDepth && rayDepth - sceneDepth < 0.3)
                    {
                        reflectColor = SampleSceneColor(uv);
                        hit = true;
                        break;
                    }
                }

                // 미스 시에도 lastValidUV(하늘/수평선)로 fallback
                if (!hit)
                {
                    reflectColor = SampleSceneColor(lastValidUV);
                }

                float3 color = lerp(_BaseColor.rgb, reflectColor, _Reflectivity);
                return float4(color, 1);
            }
            ENDHLSL
        }
    }
}