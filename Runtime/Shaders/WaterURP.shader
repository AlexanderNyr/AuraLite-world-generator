Shader "AuraLite/WaterURP"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.1, 0.4, 0.6, 0.8)
        _DeepColor("Deep Color", Color) = (0.05, 0.2, 0.4, 1.0)
        _Metallic("Metallic", Range(0,1)) = 0.5
        _Smoothness("Smoothness", Range(0,1)) = 0.9
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            float4 _BaseColor;
            float4 _DeepColor;
            float _Metallic;
            float _Smoothness;

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                // Simple Gerstner-like wave displacement
                worldPos.y += sin(worldPos.x * 0.5 + _Time.y) * 0.1;
                worldPos.y += cos(worldPos.z * 0.4 + _Time.y * 1.2) * 0.08;
                
                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = input.uv;
                output.positionWS = worldPos;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
