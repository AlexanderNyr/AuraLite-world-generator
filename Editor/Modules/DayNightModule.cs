using System.Threading;
using System.Threading.Tasks;
using AuraLiteWorldGenerator.Editor.Core;
using AuraLiteWorldGenerator.Runtime;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Modules
{
    /// <summary>
    /// Sets up the day/night cycle system in the generated world.
    /// Finds and connects the sun, creates the cycle controller, and configures lamp integration.
    /// </summary>
    public class DayNightModule : IWorldGeneratorModule
    {
        public string Id => "DayNight";
        public int Order => 116;

        public Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct)
        {
            progress.Report("Setting up day/night cycle", 0.3f);

            var dnGO = new GameObject("DayNightCycle");
            dnGO.transform.SetParent(ctx.Hierarchy.Root.transform);
            var dayNight = dnGO.AddComponent<DayNightCycle>();

            // Find the sun created by LightingModule
            Light sun = null;
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional && l.intensity > 0.5f)
                {
                    sun = l;
                    break;
                }
            }

            if (sun != null)
            {
                dayNight.sun = sun;
                // Create a pivot for the sun so we can rotate it smoothly
                var pivot = new GameObject("SunPivot");
                pivot.transform.SetParent(dnGO.transform);
                pivot.transform.position = Vector3.zero;
                sun.transform.SetParent(pivot.transform);
                dayNight.sunPivot = pivot.transform;
            }

            // Configure timing
            dayNight.dayLengthMinutes = 30f;
            dayNight.autoAdvance = true;
            dayNight.pauseAtNight = false;
            dayNight.timeOfDay = 10f; // Start at 10:00 AM

            // Find global volume for color grading
            var volume = Object.FindAnyObjectByType<UnityEngine.Rendering.Volume>();
            if (volume != null)
            {
                dayNight.globalVolume = volume;
            }

            progress.Report("Day/night cycle ready", 1.0f);
            return Task.CompletedTask;
        }
    }
}
