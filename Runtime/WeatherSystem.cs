using System.Collections.Generic;
using UnityEngine;

namespace AuraLiteWorldGenerator.Runtime
{
    /// <summary>
    /// Runtime weather system that controls rain, snow, wind, and fog.
    /// Integrates with the generated world's lighting and particle systems.
    /// </summary>
    [AddComponentMenu("AuraLite/World Generator/Weather System")]
    public class WeatherSystem : MonoBehaviour
    {
        public enum WeatherState
        {
            Clear,
            Cloudy,
            Rain,
            HeavyRain,
            Snow,
            Foggy,
            Storm
        }

        [Header("Current State")]
        public WeatherState currentWeather = WeatherState.Clear;
        [Range(0f, 1f)] public float intensity = 0f;
        [Range(0f, 1f)] public float windStrength = 0.3f;
        public Vector3 windDirection = new Vector3(1f, 0f, 0.5f);

        [Header("Transitions")]
        public float transitionSpeed = 0.5f;
        [Range(60f, 600f)] public float minWeatherDuration = 120f;
        [Range(60f, 600f)] public float maxWeatherDuration = 360f;

        [Header("Rain")]
        public ParticleSystem rainParticles;
        public int rainEmissionRate = 3000;
        public int heavyRainEmissionRate = 8000;

        [Header("Snow")]
        public ParticleSystem snowParticles;
        public int snowEmissionRate = 2000;
        public int heavySnowEmissionRate = 5000;

        [Header("Lighting")]
        public Light sunLight;
        public Color clearSunColor = new Color(1f, 0.96f, 0.91f);
        public Color rainSunColor = new Color(0.6f, 0.65f, 0.7f);
        public Color stormSunColor = new Color(0.35f, 0.38f, 0.42f);
        public float clearSunIntensity = 1.05f;
        public float rainSunIntensity = 0.55f;
        public float stormSunIntensity = 0.25f;

        [Header("Fog")]
        public Color clearFogColor = new Color(0.68f, 0.75f, 0.82f);
        public Color rainFogColor = new Color(0.5f, 0.55f, 0.6f);
        public float clearFogDensity = 0.001f;
        public float rainFogDensity = 0.004f;
        public float stormFogDensity = 0.008f;

        [Header("Ambient")]
        public Color clearAmbientSky = new Color(0.72f, 0.80f, 0.90f);
        public Color rainAmbientSky = new Color(0.40f, 0.45f, 0.52f);

        private float _targetIntensity;
        private float _currentIntensity;
        private float _nextChangeTime;
        private float _cachedRainEmission;
        private float _cachedSnowEmission;

        private void Start()
        {
            if (sunLight == null)
                sunLight = RenderSettings.sun;

            CreateParticleSystemsIfNeeded();
            SetWeather(WeatherState.Clear, 0f);
            ScheduleNextChange();
        }

        private void Update()
        {
            // Auto weather cycling
            if (Time.time > _nextChangeTime)
            {
                WeatherState next = PickRandomWeather();
                float target = GetTargetIntensity(next);
                SetWeather(next, target);
                ScheduleNextChange();
            }

            // Smooth intensity transitions
            _currentIntensity = Mathf.Lerp(_currentIntensity, _targetIntensity, Time.deltaTime * transitionSpeed);
            intensity = _currentIntensity;

            // Update visual systems
            UpdateParticles();
            UpdateLighting();
            UpdateFog();
            UpdateWind();
        }

        public void SetWeather(WeatherState state, float targetIntensity)
        {
            currentWeather = state;
            _targetIntensity = targetIntensity;
        }

        public void ForceWeather(WeatherState state, float targetIntensity)
        {
            currentWeather = state;
            _targetIntensity = targetIntensity;
            _currentIntensity = targetIntensity;
            intensity = targetIntensity;
        }

        private WeatherState PickRandomWeather()
        {
            float roll = Random.value;
            if (roll < 0.40f) return WeatherState.Clear;
            if (roll < 0.60f) return WeatherState.Cloudy;
            if (roll < 0.75f) return WeatherState.Rain;
            if (roll < 0.82f) return WeatherState.HeavyRain;
            if (roll < 0.88f) return WeatherState.Foggy;
            if (roll < 0.95f) return WeatherState.Snow;
            return WeatherState.Storm;
        }

        private float GetTargetIntensity(WeatherState state)
        {
            switch (state)
            {
                case WeatherState.Clear: return 0f;
                case WeatherState.Cloudy: return 0.3f;
                case WeatherState.Rain: return 0.6f;
                case WeatherState.HeavyRain: return 0.9f;
                case WeatherState.Snow: return 0.7f;
                case WeatherState.Foggy: return 0.5f;
                case WeatherState.Storm: return 1f;
                default: return 0f;
            }
        }

        private void ScheduleNextChange()
        {
            _nextChangeTime = Time.time + Random.Range(minWeatherDuration, maxWeatherDuration);
        }

        private void CreateParticleSystemsIfNeeded()
        {
            if (rainParticles == null)
            {
                var rainGO = new GameObject("RainParticles");
                rainGO.transform.SetParent(transform);
                rainParticles = rainGO.AddComponent<ParticleSystem>();

                var rainMain = rainParticles.main;
                rainMain.loop = true;
                rainMain.startSpeed = new ParticleSystem.MinMaxCurve(15f, 25f);
                rainMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
                rainMain.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
                rainMain.startColor = new Color(0.7f, 0.8f, 1f, 0.4f);
                rainMain.simulationSpace = ParticleSystemSimulationSpace.World;
                rainMain.maxParticles = 10000;

                var rainShape = rainParticles.shape;
                rainShape.shapeType = ParticleSystemShapeType.Box;
                rainShape.scale = new Vector3(80f, 1f, 80f);

                var rainEmission = rainParticles.emission;
                rainEmission.rateOverTime = 0;

                // Position above camera
                if (Camera.main != null)
                {
                    rainGO.transform.position = Camera.main.transform.position + Vector3.up * 25f;
                }
            }

            if (snowParticles == null)
            {
                var snowGO = new GameObject("SnowParticles");
                snowGO.transform.SetParent(transform);
                snowParticles = snowGO.AddComponent<ParticleSystem>();

                var snowMain = snowParticles.main;
                snowMain.loop = true;
                snowMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
                snowMain.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
                snowMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
                snowMain.startColor = new Color(1f, 1f, 1f, 0.7f);
                snowMain.simulationSpace = ParticleSystemSimulationSpace.World;
                snowMain.maxParticles = 8000;
                snowMain.gravityModifier = 0.1f;

                var snowShape = snowParticles.shape;
                snowShape.shapeType = ParticleSystemShapeType.Box;
                snowShape.scale = new Vector3(100f, 1f, 100f);

                var snowEmission = snowParticles.emission;
                snowEmission.rateOverTime = 0;

                if (Camera.main != null)
                {
                    snowGO.transform.position = Camera.main.transform.position + Vector3.up * 30f;
                }
            }
        }

        private void UpdateParticles()
        {
            if (rainParticles != null)
            {
                var emission = rainParticles.emission;
                float targetRain = 0f;

                if (currentWeather == WeatherState.Rain)
                    targetRain = Mathf.Lerp(0, rainEmissionRate, _currentIntensity);
                else if (currentWeather == WeatherState.HeavyRain || currentWeather == WeatherState.Storm)
                    targetRain = Mathf.Lerp(rainEmissionRate, heavyRainEmissionRate, _currentIntensity);

                _cachedRainEmission = Mathf.Lerp(_cachedRainEmission, targetRain, Time.deltaTime * 2f);
                emission.rateOverTime = _cachedRainEmission;

                // Follow camera
                if (Camera.main != null)
                {
                    var pos = Camera.main.transform.position + Vector3.up * 25f;
                    rainParticles.transform.position = pos;
                }
            }

            if (snowParticles != null)
            {
                var emission = snowParticles.emission;
                float targetSnow = 0f;

                if (currentWeather == WeatherState.Snow)
                    targetSnow = Mathf.Lerp(0, snowEmissionRate, _currentIntensity);

                _cachedSnowEmission = Mathf.Lerp(_cachedSnowEmission, targetSnow, Time.deltaTime * 2f);
                emission.rateOverTime = _cachedSnowEmission;

                if (Camera.main != null)
                {
                    var pos = Camera.main.transform.position + Vector3.up * 30f;
                    snowParticles.transform.position = pos;
                }
            }
        }

        private void UpdateLighting()
        {
            if (sunLight == null) return;

            Color targetColor;
            float targetIntensity;

            switch (currentWeather)
            {
                case WeatherState.Clear:
                case WeatherState.Cloudy:
                    targetColor = Color.Lerp(clearSunColor, rainSunColor, _currentIntensity);
                    targetIntensity = Mathf.Lerp(clearSunIntensity, rainSunIntensity, _currentIntensity);
                    break;
                case WeatherState.Rain:
                case WeatherState.HeavyRain:
                    targetColor = Color.Lerp(rainSunColor, stormSunColor, _currentIntensity);
                    targetIntensity = Mathf.Lerp(rainSunIntensity, stormSunIntensity, _currentIntensity);
                    break;
                case WeatherState.Storm:
                    targetColor = stormSunColor;
                    targetIntensity = stormSunIntensity;
                    break;
                case WeatherState.Snow:
                    targetColor = Color.Lerp(clearSunColor, new Color(0.8f, 0.82f, 0.88f), _currentIntensity);
                    targetIntensity = Mathf.Lerp(clearSunIntensity, 0.6f, _currentIntensity);
                    break;
                case WeatherState.Foggy:
                    targetColor = Color.Lerp(clearSunColor, rainSunColor, _currentIntensity);
                    targetIntensity = Mathf.Lerp(clearSunIntensity, rainSunIntensity, _currentIntensity);
                    break;
                default:
                    targetColor = clearSunColor;
                    targetIntensity = clearSunIntensity;
                    break;
            }

            sunLight.color = Color.Lerp(sunLight.color, targetColor, Time.deltaTime * transitionSpeed);
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, targetIntensity, Time.deltaTime * transitionSpeed);

            // Ambient
            RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor,
                Color.Lerp(clearAmbientSky, rainAmbientSky, _currentIntensity), Time.deltaTime * transitionSpeed);
        }

        private void UpdateFog()
        {
            float targetDensity;
            Color targetColor;

            switch (currentWeather)
            {
                case WeatherState.Foggy:
                    targetDensity = Mathf.Lerp(clearFogDensity, rainFogDensity * 2f, _currentIntensity);
                    targetColor = Color.Lerp(clearFogColor, rainFogColor, _currentIntensity);
                    break;
                case WeatherState.Rain:
                case WeatherState.HeavyRain:
                    targetDensity = Mathf.Lerp(clearFogDensity, rainFogDensity, _currentIntensity);
                    targetColor = Color.Lerp(clearFogColor, rainFogColor, _currentIntensity);
                    break;
                case WeatherState.Storm:
                    targetDensity = Mathf.Lerp(clearFogDensity, stormFogDensity, _currentIntensity);
                    targetColor = Color.Lerp(clearFogColor, stormSunColor, _currentIntensity);
                    break;
                default:
                    targetDensity = clearFogDensity;
                    targetColor = clearFogColor;
                    break;
            }

            if (RenderSettings.fogMode == FogMode.Exponential || RenderSettings.fogMode == FogMode.ExponentialSquared)
            {
                RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetDensity, Time.deltaTime * transitionSpeed);
            }

            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetColor, Time.deltaTime * transitionSpeed);
        }

        private void UpdateWind()
        {
            // Gently rotate wind direction over time
            float windAngle = Mathf.Sin(Time.time * 0.02f) * 15f;
            var rotated = Quaternion.Euler(0f, windAngle, 0f) * windDirection.normalized;
            windDirection = rotated;

            // Adjust particle wind
            if (rainParticles != null)
            {
                var noise = rainParticles.noise;
                noise.strength = windStrength * _currentIntensity * 3f;
                noise.frequency = 0.5f;
            }

            if (snowParticles != null)
            {
                var noise = snowParticles.noise;
                noise.strength = windStrength * _currentIntensity * 5f;
                noise.frequency = 0.3f;

                var velocity = snowParticles.velocityOverLifetime;
                velocity.x = windDirection.x * windStrength * 2f;
                velocity.z = windDirection.z * windStrength * 2f;
            }
        }
    }
}
