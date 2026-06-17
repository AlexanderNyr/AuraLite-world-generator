using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using AuraLiteWorldGenerator.Runtime;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Sets up lighting, environment, camera, reflection probes, clouds, and global volume.
    /// </summary>
    public static class LightingAndEnvironment
    {
        public static void ConfigureEnvironment(GenerationSettings settings)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.80f, 0.90f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.52f, 0.43f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.14f, 0.10f);
            
            // Use exponential fog for more realistic atmospheric scattering
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0012f;
            RenderSettings.fogColor = new Color(0.68f, 0.75f, 0.82f);
            
            QualitySettings.shadowCascades = settings.qualityBoost >= 10f ? 4 : 2;
            QualitySettings.shadowDistance = 400f + settings.qualityBoost * 50f;
            QualitySettings.lodBias = 2.0f + (settings.qualityBoost * 0.5f);
            QualitySettings.enableLODCrossFade = true;
            QualitySettings.antiAliasing = 8;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.globalTextureMipmapLimit = 0;
            
            // Better shadow quality
            QualitySettings.shadowResolution = settings.qualityBoost >= 5f ? ShadowResolution.VeryHigh : ShadowResolution.High;
            QualitySettings.shadowProjection = ShadowProjection.CloseFit;
            QualitySettings.shadowNearPlaneOffset = 2f;
            QualitySettings.shadowDistance = 400f + settings.qualityBoost * 50f;
            
            // Reflection settings
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionBounces = 3;
            RenderSettings.reflectionIntensity = 1f;
        }

        public static void CreateLightingRig(Transform parent, Vector3 center)
        {
            GameObject lighting = new GameObject("LightingRig");
            lighting.transform.SetParent(parent);

            GameObject sunGO = new GameObject("Sun");
            sunGO.transform.SetParent(lighting.transform);
            sunGO.transform.rotation = Quaternion.Euler(34f, -24f, 0f);
            Light sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.05f; // Reduced intensity
            sun.color = new Color(1f, 0.96f, 0.91f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.92f;
            sun.shadowBias = 0.03f;
            sun.shadowNormalBias = 0.4f;
            
            // Safe way to add light data without breaking compilation on different URP versions
            var lightData = sunGO.AddComponent<UniversalAdditionalLightData>();
            if (lightData != null)
            {
                // Try to set volumetric lighting via reflection if available
                var volProp = lightData.GetType().GetProperty("volumetricLighting");
                if (volProp != null && volProp.PropertyType == typeof(bool))
                    volProp.SetValue(lightData, true);
            }

            RenderSettings.sun = sun;

            GameObject fillGO = new GameObject("SkyFill");
            fillGO.transform.SetParent(lighting.transform);
            fillGO.transform.position = center + new Vector3(0f, 80f, 0f);
            Light fill = fillGO.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.10f;
            fill.color = new Color(0.70f, 0.80f, 1f);
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(342f, 150f, 0f);
        }

        public static void CreateCloudSystem(BuildContext ctx, WorldLayout layout, GenerationSettings settings, Transform parent)
        {
            GameObject root = new GameObject("SkyClouds");
            root.transform.SetParent(parent);
            float baseHeight = Mathf.Max(900f, layout.terrainHeightMeters * 5.5f);
            
            // Scaled cloud layers
            int layers = Mathf.RoundToInt(6f + settings.qualityBoost * 2f);
            layers = Mathf.Min(layers, 120); // Safety cap
            for (int i = 0; i < layers; i++)
            {
                float ring = i % 3;
                float angle = i / (float)Mathf.Max(1, layers) * Mathf.PI * 2f + GeometryHelpers.Hash01(i, 1, layout.seed + 1501) * 0.4f;
                float radius = Mathf.Lerp(layout.villageLengthMeters * 1.4f, layout.worldSizeMeters * 0.34f, ring / 2f);
                Vector3 pos = layout.villageCenter + new Vector3(Mathf.Cos(angle) * radius, baseHeight + ring * 120f + GeometryHelpers.Hash01(i, 2, layout.seed + 1511) * 90f, Mathf.Sin(angle) * radius);
                GameObject cloud = new GameObject("Cloud_" + i, typeof(MeshFilter), typeof(MeshRenderer));
                cloud.transform.SetParent(root.transform);
                cloud.transform.position = pos;
                cloud.transform.localScale = new Vector3(Mathf.Lerp(900f, 1800f, GeometryHelpers.Hash01(i, 3, layout.seed + 1521)), Mathf.Lerp(240f, 420f, GeometryHelpers.Hash01(i, 4, layout.seed + 1531)), 1f);
                cloud.GetComponent<MeshFilter>().sharedMesh = ctx.quadMesh;
                MeshRenderer mr = cloud.GetComponent<MeshRenderer>();
                mr.sharedMaterial = ctx.cloudMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                CameraFacingBillboard bb = cloud.AddComponent<CameraFacingBillboard>();
                bb.yOnly = false;
                bb.yawOffset = 180f;
            }
        }

        public static void CreateMainCamera(WorldLayout layout, GenerationSettings settings)
        {
            GameObject cameraGO = new GameObject("Main Camera");
            Camera cam = cameraGO.AddComponent<Camera>();
            UniversalAdditionalCameraData urp = cameraGO.AddComponent<UniversalAdditionalCameraData>();
            cameraGO.tag = "MainCamera";
            cameraGO.AddComponent<AudioListener>();

            cam.allowHDR = true;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = Mathf.Lerp(52f, 47f, Mathf.InverseLerp(1f, 3f, settings.qualityBoost));
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = Mathf.Max(60000f, settings.fogEndKm * 1000f + 5000f);

            urp.renderPostProcessing = true;
            urp.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            urp.antialiasingQuality = AntialiasingQuality.High;
            urp.stopNaN = true;
            urp.dithering = true;

            cameraGO.transform.position = layout.villageCenter + new Vector3(-layout.villageLengthMeters * 0.45f, 42f, -layout.villageLengthMeters * 0.62f);
            cameraGO.transform.rotation = Quaternion.Euler(15f, 38f, 0f);

            GameObject panorama = new GameObject("Panorama Camera");
            Camera panoCam = panorama.AddComponent<Camera>();
            UniversalAdditionalCameraData panoUrp = panorama.AddComponent<UniversalAdditionalCameraData>();
            panoCam.enabled = false;
            panoCam.allowHDR = true;
            panoCam.fieldOfView = 46f;
            panoCam.farClipPlane = Mathf.Max(60000f, settings.fogEndKm * 1000f + 5000f);
            panoUrp.renderPostProcessing = true;
            panoUrp.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            panoUrp.antialiasingQuality = AntialiasingQuality.High;
            panoUrp.dithering = true;
            panorama.transform.position = layout.villageCenter + new Vector3(-layout.villageLengthMeters * 0.10f, 28f, -layout.villageLengthMeters * 0.12f);
            panorama.transform.rotation = Quaternion.Euler(11f, 22f, 0f);
        }

        public static void CreateReflectionProbe(WorldLayout layout, Transform parent)
        {
            GameObject probeGO = new GameObject("ReflectionProbe");
            probeGO.transform.SetParent(parent);
            probeGO.transform.position = layout.villageCenter + Vector3.up * 18f;
            ReflectionProbe probe = probeGO.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.size = new Vector3(layout.villageLengthMeters * 2.4f, 120f, layout.villageLengthMeters * 2.4f);
            probe.intensity = 0.65f; // Reduced intensity
        }

        public static void CreateGlobalVolume(BuildContext ctx, Transform parent)
        {
            GameObject go = new GameObject("Global Volume");
            go.transform.SetParent(parent);
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = ctx.globalVolumeProfile;
        }
    }
}
