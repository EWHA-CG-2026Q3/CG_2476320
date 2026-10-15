// =====================================================================
// S13_SDFCircle_Finish.shader
// 씬: S13_SDFPreview / 오브젝트: Quad
// 삼각형 없이 원 그리기: 픽셀마다 중심까지의 거리로 원 안팎을 판정
// S13_MyShader(템플릿)에 [추가], [변경]한 부분만 표시
// =====================================================================
Shader "CG/S13_SDFCircle_Finish"
{
    // [추가] 재질 Inspector에 나타날 칸
    Properties
    {
        _Radius ("Radius", Range(0, 0.5)) = 0.4                 // 원의 반지름 (UV 기준, 0 ~ 0.5)
        _CircleColor ("Circle Color", Color) = (1, 0.5, 0, 1)   // 원 안쪽 색
        _BackColor ("Back Color", Color) = (0, 0, 0, 1)         // 원 바깥 색
    }

    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // [추가] 재질마다 다른 값
            CBUFFER_START(UnityPerMaterial)
                float _Radius;
                half4 _CircleColor;
                half4 _BackColor;
            CBUFFER_END

            // [추가] vert가 frag로 넘기는 값을 묶은 자료형
            //   템플릿에서는 화면 위치 하나뿐이라 묶을 필요가 없었음
            //   이제 화면 위치와 UV 두 가지라 하나로 묶어서 넘김
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;   // 화면 위치 (GPU가 래스터화에 씀)
                float2 uv          : TEXCOORD0;     // UV (래스터화 중 픽셀마다 보간됨, S06)
            };

            // [변경] 입력에 UV를 추가하고, 반환을 Varyings로
            //   : TEXCOORD0 → "메시의 첫 번째 UV(mesh.uv)를 여기에 넣어라"라는 표시
            Varyings vert(float3 positionOS : POSITION, float2 uv : TEXCOORD0)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(positionOS);   // P × V × M (템플릿과 같음)
                OUT.uv = uv;                                            // UV는 그대로 넘김
                return OUT;
            }

            // [변경] 입력으로 Varyings를 받아, 픽셀마다 다른 색을 냄
            half4 frag(Varyings IN) : SV_Target
            {
                // 이 픽셀의 UV와 Quad 중심 (0.5, 0.5) 사이의 거리
                float d = distance(IN.uv, float2(0.5, 0.5));

                // step(a, b): b가 a 이상이면 1, 아니면 0
                //   step(d, _Radius) → 반지름이 거리 이상(원 안쪽)이면 1, 바깥이면 0
                float inside = step(d, _Radius);

                // lerp(A, B, t): t가 0이면 A, 1이면 B (실습 6의 Lerp 노드와 같음)
                //   안쪽(1) → 원 색, 바깥(0) → 배경 색
                return lerp(_BackColor, _CircleColor, inside);
            }
            ENDHLSL
        }
    }
}