using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AuraLiteWorldGenerator.Runtime
{
    /// <summary>
    /// Runtime day/night cycle that rotates the sun, changes lighting, and toggles lamps.
    /// Designed to work with the AuraLite world generator output.
    /// </summary>
    [AddComponentMenu("AuraLite/World Generator/Day Night Cycle")]
    public class DayNightCycle : MonoBehaviour
    {
        [Header("Time Settings")]
        [Range(0f, 24f)] public float timeOfDay = 10f;
        public float dayLengthMinutes = 30f; // Real-time minutes per game day
        public bool autoAdvance = true;
        public bool pauseAtNight;

        [Header("Sun")]
        public Light sun;
        public Transform sunPivot;
        public float sunriseHour = 6f;
        public float sunsetHour = 20f;
        public float maxSunAngle = 80f;
        public Gradient sunColorOverDay;
        public AnimationCurve sunIntensityOverDay;

        [Header("Moon")]
        public Light moon;
        public float moonIntensity = 0.15f;
        public Color moonColor = new Color(0.6f, 0.65f, 0.8f);

        [Header("Sky & Ambient")]
        public Gradient skyColorOverDay;
        public Gradient equatorColorOverDay;
        public Gradient groundColorOverDay;
        public Gradient fogColorOverDay;
        public AnimationCurve fogDensityOverDay;

        [Header("Stars")]
        public ParticleSystem stars;
        public float starFadeStart = 19f;
        public float starFadeEnd = 21f;
        public float starUnfadeStart = 4f;
        public float starUnfadeEnd = 6f;

        [Header("Lamps")]
        public float lampOnHour = 18.5f;
        public float lampOffHour = 6.5f;
        public Color lampColor = new Color(1f, 0.8f, 0.5f);
        public float lampIntensity = 2f;
        public float lampRange = 15f;

        [Header("Volume")]
        public Volume globalVolume;

        private List<Light> _lampLights = new List<Light>();
        private List<Renderer> _lampGlows = new List<Renderer>();
        private float _timeSpeed;
        private bool _lampsOn;
        private ColorAdjustments _colorAdjustments;

        private void Start()
        {
            InitializeGradients();
            FindLamps();
            FindSun();
            FindVolume();

            _timeSpeed = 24f / (dayLengthMinutes * 60f); // hours per second
        }

        private void Update()
        {
            if (autoAdvance)
            {
                if (pauseAtNight && IsNight())
                {
                    // Don't advance time at night
                }
                else
                {
                    timeOfDay += _timeSpeed * Time.deltaTime;
                }

                if (timeOfDay >= 24f) timeOfDay -= 24f;
                if (timeOfDay < 0f) timeOfDay += 24f;
            }

            UpdateSun();
            UpdateMoon();
            UpdateAmbient();
            UpdateFog();
            UpdateStars();
            UpdateLamps();
            UpdateVolume();
        }

        public bool IsNight()
        {
            return timeOfDay < sunriseHour || timeOfDay > sunsetHour;
        }

        public float GetDaylightFactor()
        {
            if (timeOfDay < sunriseHour - 1f) return 0f;
            if (timeOfDay < sunriseHour + 1f) return Mathf.InverseLerp(sunriseHour - 1f, sunriseHour + 1f, timeOfDay);
            if (timeOfDay < sunsetHour - 1f) return 1f;
            if (timeOfDay < sunsetHour + 1f) return 1f - Mathf.InverseLerp(sunsetHour - 1f, sunsetHour + 1f, timeOfDay);
            return 0f;
        }

        private void InitializeGradients()
        {
            if (sunColorOverDay == null || sunColorOverDay.colorKeys.Length == 0)
            {
                sunColorOverDay = new Gradient();
                sunColorOverDay.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0f),    // midnight - warm horizon
                        new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.22f),  // sunrise
                        new GradientColorKey(new Color(1f, 0.9f, 0.7f), 0.28f),  // early morning
                        new GradientColorKey(new Color(1f, 0.96f, 0.91f), 0.4f), // mid-morning
                        new GradientColorKey(new Color(1f, 0.96f, 0.91f), 0.6f), // afternoon
                        new GradientColorKey(new Color(1f, 0.8f, 0.5f), 0.75f),  // late afternoon
                        new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.82f),  // sunset
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0.9f), // dusk
                        new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 1f) // deep night
                    },
                    new[]
                    {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(1f, 0.25f),
                        new GradientAlphaKey(1f, 0.75f),
                        new GradientAlphaKey(0f, 0.85f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }

            if (sunIntensityOverDay == null || sunIntensityOverDay.length == 0)
            {
                sunIntensityOverDay = new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.22f, 0.3f),
                    new Keyframe(0.3f, 1.05f),
                    new Keyframe(0.5f, 1.05f),
                    new Keyframe(0.75f, 0.8f),
                    new Keyframe(0.82f, 0.3f),
                    new Keyframe(0.9f, 0f),
                    new Keyframe(1f, 0f)
                );
            }

            if (fogDensityOverDay == null || fogDensityOverDay.length == 0)
            {
                fogDensityOverDay = new AnimationCurve(
                    new Keyframe(0f, 0.003f),
                    new Keyframe(0.25f, 0.002f),
                    new Keyframe(0.5f, 0.001f),
                    new Keyframe(0.75f, 0.002f),
                    new Keyframe(0.85f, 0.005f),
                    new Keyframe(1f, 0.003f)
                );
            }
        }

        private void FindLamps()
        {
            // Find all lamp glow objects created by VillagePropsGenerator
            _lampGlows.Clear();
            _lampLights.Clear();

            var allRenderers = FindObjectsOfType<MeshRenderer>();
            foreach (var mr in allRenderers)
            {
                if (mr.name.StartsWith("LampGlow"))
                {
                    _lampGlows.Add(mr);
                }
            }
        }

        private void FindSun()
        {
            if (sun == null)
            {
                sun = RenderSettings.sun;
            }

            if (sunPivot == null && sun != null)
            {
                sunPivot = sun.transform.parent;
                if (sunPivot == null)
                {
                    var pivot = new GameObject("SunPivot");
                    pivot.transform.position = Vector3.zero;
                    sun.transform.SetParent(pivot.transform);
                    sunPivot = pivot.transform;
                }
            }
        }

        private void FindVolume()
        {
            if (globalVolume == null)
            {
                globalVolume = FindObjectOfType<Volume>();
            }

            if (globalVolume != null && globalVolume.profile != null)
            {
                globalVolume.profile.TryGet(out _colorAdjustments);
            }
        }

        private void UpdateSun()
        {
            if (sun == null) return;

            float t = timeOfDay / 24f; // 0-1

            // Rotate sun based on time
            float sunAngle = Mathf.Lerp(-maxSunAngle, maxSunAngle, Mathf.InverseLerp(sunriseHour, sunsetHour, timeOfDay));
            float azimuth = Mathf.Lerp(0f, 180f, t);

            if (sunPivot != null)
            {
                sunPivot.rotation = Quaternion.Euler(sunAngle, azimuth, 0f);
            }
            else
            {
                sun.transform.rotation = Quaternion.Euler(sunAngle, azimuth - 180f, 0f);
            }

            // Color and intensity
            sun.color = sunColorOverDay.Evaluate(t);
            sun.intensity = sunIntensityOverDay.Evaluate(t);

            // Toggle shadows at night
            sun.shadows = sun.intensity > 0.05f ? LightShadows.Soft : LightShadows.None;
        }

        private void UpdateMoon()
        {
            if (moon == null) return;

            float daylight = GetDaylightFactor();
            moon.intensity = Mathf.Lerp(moonIntensity, 0f, daylight);

            // Moon is opposite the sun
            if (sun != null)
            {
                moon.transform.rotation = Quaternion.LookRotation(-sun.transform.forward, Vector3.up);
            }

            moon.color = moonColor;
        }

        private void UpdateAmbient()
        {
            float t = timeOfDay / 24f;
            float daylight = GetDaylightFactor();

            RenderSettings.ambientSkyColor = Color.Lerp(
                new Color(0.05f, 0.05f, 0.12f),
                skyColorOverDay != null && skyColorOverDay.colorKeys.Length > 0
                    ? skyColorOverDay.Evaluate(t)
                    : new Color(0.72f, 0.80f, 0.90f),
                daylight
            );

            RenderSettings.ambientEquatorColor = Color.Lerp(
                new Color(0.04f, 0.04f, 0.08f),
                equatorColorOverDay != null && equatorColorOverDay.colorKeys.Length > 0
                    ? equatorColorOverDay.Evaluate(t)
                    : new Color(0.45f, 0.52f, 0.43f),
                daylight
            );

            RenderSettings.ambientGroundColor = Color.Lerp(
                new Color(0.02f, 0.02f, 0.04f),
                groundColorOverDay != null && groundColorOverDay.colorKeys.Length > 0
                    ? groundColorOverDay.Evaluate(t)
                    : new Color(0.12f, 0.14f, 0.10f),
                daylight
            );
        }

        private void UpdateFog()
        {
            float t = timeOfDay / 24f;
            float daylight = GetDaylightFactor();

            Color nightFog = new Color(0.03f, 0.04f, 0.08f);
            Color dayFog = fogColorOverDay != null && fogColorOverDay.colorKeys.Length > 0
                ? fogColorOverDay.Evaluate(t)
                : new Color(0.68f, 0.75f, 0.82f);

            RenderSettings.fogColor = Color.Lerp(nightFog, dayFog, daylight);

            if (fogDensityOverDay != null && fogDensityOverDay.length > 0 &&
                (RenderSettings.fogMode == FogMode.Exponential || RenderSettings.fogMode == FogMode.ExponentialSquared))
            {
                RenderSettings.fogDensity = fogDensityOverDay.Evaluate(t);
            }
        }

        private void UpdateStars()
        {
            if (stars == null) return;

            float starAlpha = 0f;
            if (timeOfDay > starFadeStart && timeOfDay < starFadeEnd)
            {
                starAlpha = Mathf.InverseLerp(starFadeStart, starFadeEnd, timeOfDay);
            }
            else if (timeOfDay > starUnfadeStart && timeOfDay < starUnfadeEnd)
            {
                starAlpha = 1f - Mathf.InverseLerp(starUnfadeStart, starUnfadeEnd, timeOfDay);
            }
            else if (timeOfDay >= starFadeEnd || timeOfDay <= starUnfadeStart)
            {
                starAlpha = 1f;
            }

            var main = stars.main;
            var color = main.startColor.color;
            color.a = starAlpha;
            main.startColor = color;
        }

        private void UpdateLamps()
        {
            bool shouldLampsBeOn = timeOfDay > lampOnHour || timeOfDay < lampOffHour;

            if (shouldLampsBeOn == _lampsOn)
                return;

            _lampsOn = shouldLampsBeOn;

            foreach (var glow in _lampGlows)
            {
                if (glow == null) continue;

                // Find or add a point light
                var light = glow.GetComponent<Light>();
                if (light == null && _lampsOn)
                {
                    light = glow.gameObject.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = lampColor;
                    light.intensity = lampIntensity;
                    light.range = lampRange;
                    light.shadows = LightShadows.Soft;
                    light.renderMode = LightRenderMode.ForcePixel;
                }

                if (light != null)
                {
                    light.enabled = _lampsOn;
                }

                // Toggle emissive material
                var mat = glow.sharedMaterial;
                if (mat != null)
                {
                    if (_lampsOn)
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", lampColor * 2f);
                    }
                    else
                    {
                        mat.DisableKeyword("_EMISSION");
                    }
                }
            }
        }

        private void UpdateVolume()
        {
            if (_colorAdjustments != null)
            {
                float daylight = GetDaylightFactor();
                // Slightly desaturate at night, increase contrast during golden hour
                float saturation = Mathf.Lerp(-10f, 10f, daylight);

                // Golden hour boost
                float goldenHour = 0f;
                if (timeOfDay > 17f && timeOfDay < 20f)
                    goldenHour = 1f - Mathf.Abs(timeOfDay - 18.5f) / 1.5f;
                if (timeOfDay > 5f && timeOfDay < 8f)
                    goldenHour = 1f - Mathf.Abs(timeOfDay - 6.5f) / 1.5f;

                _colorAdjustments.satificationOverrideState = true;
                _colorAdjustments.saturation.value = saturation + goldenHour * 15f;
            }
        }
    }
}
