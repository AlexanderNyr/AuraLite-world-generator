Shader "AuraLite/WaterURP"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.1, 0.4, 0.6, 0.8)
        _DeepColor("Deep Color", Color) = (0.05, 0.2, 0.4, 1.0)
        _FoamColor("Foam Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Metallic("Metallic", Range(0,1)) = 0.5
        _Smoothness("Smoothness", Range(0,1)) = 0.9
        _WaveSpeed("Wave Speed", Float) = 1.0
        _WaveScale("Wave Scale", Float) = 0.5
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
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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
                float4 screenPos : TEXCOORD4;
                float3 normalWS : TEXCOORD5;
            };

            float4 _BaseColor;
            float4 _DeepColor;
            float4 _FoamColor;
            float _Metallic;
            float _Smoothness;
            float _WaveSpeed;
            float _WaveScale;

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                
                float time = _Time.y * _WaveSpeed;
                float wave1 = sin(worldPos.x * _WaveScale + time);
                float wave2 = cos(worldPos.z * _WaveScale * 0.8 + time * 1.2);
                
                worldPos.y += wave1 * 0.1;
                worldPos.y += wave2 * 0.08;
                
                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = input.uv;
                output.positionWS = worldPos;
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                float dx = cos(worldPos.x * _WaveScale + time) * _WaveScale * 0.1;
                float dz = -sin(worldPos.z * _WaveScale * 0.8 + time * 1.2) * _WaveScale * 0.8 * 0.08;
                output.normalWS = normalize(float3(-dx, 1.0, -dz));
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float waterDepth = sceneDepth - input.screenPos.w;
                
                float depthFade = saturate(waterDepth * 0.2);
                half4 col = lerp(_BaseColor, _DeepColor, depthFade);
                
                float foamFade = saturate(1.0 - (waterDepth * 2.0));
                col.rgb = lerp(col.rgb, _FoamColor.rgb, foamFade * _FoamColor.a);
                
                float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                float fresnel = pow(1.0 - saturate(dot(input.normalWS, viewDir)), 5.0);
                col.rgb += fresnel * 0.5;
                
                col.a = saturate(col.a + depthFade);
                return col;
            }
            ENDHLSL
        }
    }
}
