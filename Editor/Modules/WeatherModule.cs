using System.Threading;
using System.Threading.Tasks;
using AuraLiteWorldGenerator.Editor.Core;
using AuraLiteWorldGenerator.Runtime;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Modules
{
    /// <summary>
    /// Sets up the runtime weather system in the generated world.
    /// </summary>
    public class WeatherModule : IWorldGeneratorModule
    {
        public string Id => "Weather";
        public int Order => 115;

        public Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct)
        {
            progress.Report("Setting up weather system", 0.5f);

            // Create WeatherSystem GameObject
            var weatherGO = new GameObject("WeatherSystem");
            weatherGO.transform.SetParent(ctx.Hierarchy.Root.transform);
            var weather = weatherGO.AddComponent<WeatherSystem>();

            // Configure based on settings
            weather.minWeatherDuration = 120f;
            weather.maxWeatherDuration = 360f;
            weather.windDirection = new Vector3(1f, 0f, 0.5f).normalized;
            weather.transitionSpeed = 0.5f;

            // Start with clear weather
            weather.ForceWeather(WeatherSystem.WeatherState.Clear, 0f);

            progress.Report("Weather system ready", 1.0f);
            return Task.CompletedTask;
        }
    }
}
