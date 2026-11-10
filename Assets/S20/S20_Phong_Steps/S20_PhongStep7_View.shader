Shader "CG/S20_PhongStep7_View"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.8, 0.8, 0.8, 1)
        _Ambient ("Ambient", Range(0, 1)) = 0.2
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        // _SpecularAmount는 뺌: 반사광의 밝기를 이제 frag에서 계산함
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

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;                   // 새로: 그 픽셀의 월드 위치 (V를 구하려고)
                float3 normalWS   : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);   // 새로: 위치를 월드 좌표로
                output.positionCS = TransformWorldToHClip(output.positionWS);   // 월드 좌표에서 Clip Space로
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;                  // 빛이 있는 쪽
                float3 V = normalize(GetCameraPositionWS() - input.positionWS);  // 새로: 카메라가 있는 쪽

                // dot(N, L): normal과 빛이 있는 쪽 사이 각도의 cos. 정면이면 1, 옆이면 0, 등지면 음수
                // max(0, …): 빛을 등진 쪽의 음수를 0으로 막음
                float diffuse = max(0, dot(N, L));               // 확산광의 밝기

                // dot(N, V): normal과 카메라가 있는 쪽 사이 각도의 cos. 카메라를 정면으로 향하면 1
                float specular = max(0, dot(N, V));              // 반사광의 밝기 (임시): _SpecularAmount 대신 계산

                float3 color = _BaseColor.rgb * (_Ambient + diffuse * mainLight.color)
                             + _SpecularColor.rgb * specular * mainLight.color;
                return float4(color, 1);
            }
            ENDHLSL
        }
    }
}