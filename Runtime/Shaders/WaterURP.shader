Shader "AuraLite/WaterURP"
{
    Properties
    {
        _BaseColor("Shallow Color", Color) = (0.1, 0.5, 0.7, 0.75)
        _DeepColor("Deep Color", Color) = (0.04, 0.18, 0.38, 1.0)
        _FoamColor("Foam Color", Color) = (0.95, 0.98, 1.0, 1.0)
        _ScatterColor("Subsurface Scatter", Color) = (0.0, 0.6, 0.4, 1.0)
        
        _Metallic("Metallic", Range(0,1)) = 0.1
        _Smoothness("Smoothness", Range(0,1)) = 0.92
        
        _WaveSpeed("Wave Speed", Float) = 1.0
        _WaveScale("Wave Scale", Float) = 0.4
        _WaveHeight("Wave Height", Float) = 0.15
        
        _DepthMax("Depth Max", Float) = 8.0
        _DepthFalloff("Depth Falloff", Float) = 2.0
        
        _FoamWidth("Foam Width", Float) = 0.6
        _FoamCutoff("Foam Cutoff", Range(0,1)) = 0.3
        _FoamDistortion("Foam Distortion", Float) = 2.0
        
        _FresnelPower("Fresnel Power", Float) = 5.0
        _FresnelBias("Fresnel Bias", Float) = 0.04
        _FresnelScale("Fresnel Scale", Float) = 0.96
        
        _CausticsStrength("Caustics Strength", Float) = 0.3
        _CausticsScale("Caustics Scale", Float) = 8.0
        _CausticsSpeed("Caustics Speed", Float) = 0.5
        
        _FlowSpeed("Flow Speed", Float) = 0.3
        _FlowDirection("Flow Direction", Vector) = (1, 0, 0.5, 0)
        
        _RefractionStrength("Refraction", Range(0,0.1)) = 0.02
        
        _NormalStrength("Normal Strength", Float) = 0.8
        _NormalScale("Normal Scale", Float) = 30.0
        
        [Toggle(_GERSTNER_WAVES)] _GerstnerWaves("Gerstner Waves", Float) = 1.0
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
            #pragma multi_compile_local _ _GERSTNER_WAVES
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
                float2 flowUV : TEXCOORD6;
            };

            // Properties
            float4 _BaseColor;
            float4 _DeepColor;
            float4 _FoamColor;
            float4 _ScatterColor;
            float _Metallic;
            float _Smoothness;
            float _WaveSpeed;
            float _WaveScale;
            float _WaveHeight;
            float _DepthMax;
            float _DepthFalloff;
            float _FoamWidth;
            float _FoamCutoff;
            float _FoamDistortion;
            float _FresnelPower;
            float _FresnelBias;
            float _FresnelScale;
            float _CausticsStrength;
            float _CausticsScale;
            float _CausticsSpeed;
            float _FlowSpeed;
            float4 _FlowDirection;
            float _RefractionStrength;
            float _NormalStrength;
            float _NormalScale;

            // ====== Noise Helpers ======
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p, int octaves)
            {
                float f = 0.0;
                float w = 0.5;
                for (int i = 0; i < 4; i++) // Unrolled for compatibility
                {
                    f += w * noise(p);
                    p *= 2.0;
                    w *= 0.5;
                    if (i >= octaves - 1) break;
                }
                return f;
            }

            // ====== Gerstner Wave ======
            float3 gerstnerWave(float2 pos, float2 dir, float steepness, float wavelength, float time)
            {
                float k = 2.0 * 3.14159265 / wavelength;
                float c = sqrt(9.81 / k);
                float2 d = normalize(dir);
                float f = k * (dot(d, pos) - c * time);
                float a = steepness / k;
                
                return float3(
                    d.x * a * cos(f),
                    a * sin(f),
                    d.y * a * cos(f)
                );
            }

            float3 getWaveDisplacement(float3 pos, float time)
            {
                float3 disp = float3(0, 0, 0);
                
                // 4 wave components for natural look
                disp += gerstnerWave(pos.xz, float2(1.0, 0.3), 0.25, 12.0, time * _WaveSpeed);
                disp += gerstnerWave(pos.xz, float2(0.5, 1.0), 0.18, 8.0, time * _WaveSpeed * 1.1);
                disp += gerstnerWave(pos.xz, float2(-0.3, 0.7), 0.12, 5.0, time * _WaveSpeed * 1.3);
                disp += gerstnerWave(pos.xz, float2(0.8, -0.4), 0.08, 3.5, time * _WaveSpeed * 0.9);
                
                return disp * _WaveHeight;
            }

            // ====== Water Normal from Noise ======
            float3 getWaterNormal(float2 uv, float time)
            {
                float scale = _NormalScale;
                float2 p1 = uv * scale + time * _WaveSpeed * 0.5;
                float2 p2 = uv * scale * 1.4 + time * _WaveSpeed * 0.3 + float2(5.2, 1.3);
                
                float n1 = noise(p1);
                float n2 = noise(p2);
                
                // Compute normal from gradient
                float2 eps = float2(0.01, 0.0);
                float h = (noise(p1) + noise(p2)) * 0.5;
                float hx = (noise(p1 + eps.xy) + noise(p2 + eps.xy)) * 0.5;
                float hz = (noise(p1 + eps.yx) + noise(p2 + eps.yx)) * 0.5;
                
                float3 n = normalize(float3(
                    (h - hx) / eps.x * _NormalStrength,
                    1.0,
                    (h - hz) / eps.x * _NormalStrength
                ));
                
                return n;
            }

            // ====== Caustics ======
            float caustics(float2 uv, float time)
            {
                float2 p = uv * _CausticsScale;
                float t = time * _CausticsSpeed;
                
                float v1 = 1.0 - abs(sin((p.x + t) * 1.5 + sin((p.y + t * 0.7) * 2.1)));
                float v2 = 1.0 - abs(sin((p.y - t * 0.6) * 1.8 + cos((p.x + t * 0.4) * 1.6)));
                
                // Second layer with offset
                float2 p2 = uv * _CausticsScale * 1.5 + float2(3.7, 1.2);
                float v3 = 1.0 - abs(sin((p2.x + t * 0.8) * 1.3 + sin((p2.y - t * 0.5) * 1.7)));
                float v4 = 1.0 - abs(sin((p2.y + t * 0.3) * 1.5 + cos((p2.x - t * 0.7) * 1.2)));
                
                return (v1 * v2 * v3 * v4) * _CausticsStrength;
            }

            // ====== Foam ======
            float computeFoam(float waterDepth, float3 normalWS, float2 uv, float time)
            {
                // Shore foam based on depth
                float shoreFoam = 1.0 - smoothstep(0.0, _FoamWidth, waterDepth);
                
                // Wave crest foam
                float waveFoam = smoothstep(0.5, 0.8, normalWS.y) * (1.0 - smoothstep(0.8, 0.5, normalWS.y));
                
                // Noise-distorted foam for natural look
                float2 foamUV = uv * _FoamDistortion + float2(time * 0.1, time * 0.07);
                float foamNoise = fbm(foamUV, 3);
                
                float foam = max(shoreFoam, waveFoam * 0.3);
                foam *= smoothstep(_FoamCutoff, _FoamCutoff + 0.2, foamNoise + foam * 0.5);
                
                return saturate(foam);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float time = _Time.y;
                
                #if _GERSTNER_WAVES
                // Apply Gerstner wave displacement
                float3 displacement = getWaveDisplacement(worldPos, time);
                worldPos += displacement;
                #else
                // Fallback: simple sine waves
                float wave1 = sin(worldPos.x * _WaveScale + time * _WaveSpeed);
                float wave2 = cos(worldPos.z * _WaveScale * 0.8 + time * _WaveSpeed * 1.2);
                float wave3 = sin((worldPos.x + worldPos.z) * _WaveScale * 0.6 + time * _WaveSpeed * 0.7);
                worldPos.y += (wave1 * 0.5 + wave2 * 0.3 + wave3 * 0.2) * _WaveHeight;
                #endif
                
                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = input.uv;
                output.positionWS = worldPos;
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                // Compute normal from wave displacement
                float3 normal = getWaterNormal(worldPos.xz, time);
                
                #if _GERSTNER_WAVES
                // Add Gerstner-derived normal contribution
                float eps = 0.1;
                float3 dispX = getWaveDisplacement(worldPos + float3(eps, 0, 0), time);
                float3 dispZ = getWaveDisplacement(worldPos + float3(0, 0, eps), time);
                float3 tangent = normalize(float3(eps, displacement.y - dispX.y, 0));
                float3 bitangent = normalize(float3(0, displacement.y - dispZ.y, eps));
                float3 gerstnerNormal = normalize(cross(bitangent, tangent));
                normal = normalize(lerp(normal, gerstnerNormal, 0.6));
                #endif
                
                output.normalWS = normal;
                
                // Flow UV for river current
                float2 flowDir = normalize(_FlowDirection.xz);
                output.flowUV = worldPos.xz * 0.1 + flowDir * time * _FlowSpeed;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // ====== Depth ======
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float waterDepth = max(0, sceneDepth - input.screenPos.w);
                float depthFactor = saturate(waterDepth / _DepthMax);
                float depthFalloff = exp(-waterDepth * _DepthFalloff / _DepthMax);
                
                // ====== Water Normal ======
                float3 waterNormal = normalize(input.normalWS);
                
                // Perturb UVs with refraction
                float2 refractedUV = screenUV + waterNormal.xz * _RefractionStrength * depthFalloff;
                
                // ====== Base Color with Depth ======
                half4 shallowCol = _BaseColor;
                half4 deepCol = _DeepColor;
                
                // Subsurface scattering approximation
                float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                float sss = pow(saturate(dot(viewDir, -_MainLightPosition.xyz)), 3.0) * depthFalloff;
                half3 scatterContrib = _ScatterColor.rgb * sss * 0.3;
                
                half4 waterColor;
                waterColor.rgb = lerp(shallowCol.rgb, deepCol.rgb, depthFactor);
                waterColor.rgb += scatterContrib;
                
                // ====== Caustics ======
                float caustic = caustics(input.positionWS.xz * 0.1, time);
                waterColor.rgb += caustic * depthFalloff * _ScatterColor.rgb;
                
                // ====== Foam ======
                float foam = computeFoam(waterDepth, waterNormal, input.positionWS.xz * 0.05, time);
                waterColor.rgb = lerp(waterColor.rgb, _FoamColor.rgb, foam * _FoamColor.a);
                
                // ====== Flow / River Current ======
                float flowNoise1 = noise(input.flowUV * 3.0);
                float flowNoise2 = noise(input.flowUV * 5.0 + float2(1.7, 3.2));
                float flowPattern = (flowNoise1 * 0.7 + flowNoise2 * 0.3);
                // Bright streaks on the water surface
                waterColor.rgb += flowPattern * 0.06 * depthFalloff;
                
                // ====== Fresnel ======
                float fresnel = _FresnelScale * pow(1.0 - saturate(dot(waterNormal, viewDir)), _FresnelPower) + _FresnelBias;
                fresnel = saturate(fresnel);
                
                // Specular highlight
                Light mainLight = GetMainLight();
                float3 halfVec = normalize(viewDir + mainLight.direction);
                float spec = pow(saturate(dot(waterNormal, halfVec)), 256.0 * _Smoothness);
                float3 specColor = mainLight.color * spec * 2.0;
                
                // ====== Final Combine ======
                // Mix water color with sky reflection via Fresnel
                half3 skyReflection = half3(0.5, 0.6, 0.8) * mainLight.color; // Simple sky proxy
                waterColor.rgb = lerp(waterColor.rgb, skyReflection, fresnel * 0.5);
                waterColor.rgb += specColor;
                
                // Alpha: deeper = more opaque, foam is fully opaque
                waterColor.a = saturate(_BaseColor.a + depthFactor * 0.2 + foam * 0.3);
                waterColor.a = lerp(waterColor.a, 1.0, fresnel * 0.3);
                
                return waterColor;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
