// S13_MyShader.shader — 씬: S13_Landscape / 오브젝트: Cube (Mat_MyShader)
// 작동하는 가장 작은 URP 셰이더. 앞으로 이 파일을 복사해 조금씩 늘려 감

// 셰이더의 이름. 재질 Inspector의 Shader 목록에 "S13 > MyShader"로 나타남
Shader "CG/S13_MyShader"
{
    // SubShader: 이 셰이더를 그리는 방법 한 묶음
    SubShader
    {
        // Pass: 오브젝트를 한 번 그리는 단위
        Pass
        {
            // HLSLPROGRAM ~ ENDHLSL: GPU에서 실행되는 코드 (HLSL 언어)
            HLSLPROGRAM

            // 정점 단계에서 실행할 함수 = vert (파이프라인 지도 ②)
            #pragma vertex vert
            // 픽셀 단계에서 실행할 함수 = frag (파이프라인 지도 ④)
            #pragma fragment frag

            // URP가 준비해 둔 행렬(M, V, P)과 함수(TransformObjectToHClip)를 불러옴
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // vert: 정점 하나마다 한 번 실행
            //   입력 positionOS : POSITION → 메시의 정점 위치(오브젝트 공간, x y z)를 받음
            //   반환값 : SV_POSITION      → 이 값이 클립 공간 위치임을 GPU에 알림
            float4 vert(float3 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS);   // P × V × M × (x, y, z, 1)
            }

            // frag: 삼각형이 덮는 픽셀 하나마다 한 번 실행
            //   반환값 : SV_Target → 이 값을 화면에 쓸 색으로 씀
            //   half4 = 실수 4개 (빨강, 초록, 파랑, 알파), 각각 0 ~ 1
            half4 frag() : SV_Target
            {
                return half4(1, 0, 0, 1);   // ← 오늘 바꿀 곳: 불투명한 빨강
            }

            ENDHLSL
        }
    }
}