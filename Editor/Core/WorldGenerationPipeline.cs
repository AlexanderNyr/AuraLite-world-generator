using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuraLiteWorldGenerator.Editor.Core.Logging;

namespace AuraLiteWorldGenerator.Editor.Core
{
    public class WorldGenerationPipeline
    {
        private readonly List<IWorldGeneratorModule> _modules = new List<IWorldGeneratorModule>();
        private readonly IServiceContainer _services;

        public WorldGenerationPipeline(IServiceContainer services)
        {
            _services = services;
        }

        public void AddModule(IWorldGeneratorModule module)
        {
            _modules.Add(module);
        }

        public async Task ExecuteAsync(GenerationContext context, IProgressReporter progress, CancellationToken ct)
        {
            var logger = _services.Resolve<ILogger>();
            var sortedModules = _modules.OrderBy(m => m.Order).ToList();
            float total = sortedModules.Count;
            
            var report = new GenerationReport
            {
                Seed = context.Seed.ToString(),
                Timestamp = DateTime.UtcNow.ToString("O")
            };
            
            var totalTimer = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < sortedModules.Count; i++)
            {
                var module = sortedModules[i];
                ct.ThrowIfCancellationRequested();
                
                float startProgress = i / total;
                float endProgress = (i + 1) / total;
                
                var moduleProgress = new SubProgressReporter(progress, startProgress, endProgress);
                var modReport = new ModuleReport { Id = module.Id, Status = "Started" };
                report.Modules.Add(modReport);
                
                var modTimer = System.Diagnostics.Stopwatch.StartNew();
                
                try
                {
                    logger.Info($"Starting module: {module.Id}");
                    await module.ExecuteAsync(context, moduleProgress, ct);
                    modReport.Status = "Completed";
                }
                catch (Exception ex)
                {
                    modReport.Status = "Failed";
                    modReport.Errors.Add(ex.Message);
                    logger.Error($"Module {module.Id} failed", ex);
                    throw;
                }
                finally
                {
                    modTimer.Stop();
                    modReport.DurationSeconds = (float)modTimer.Elapsed.TotalSeconds;
                    logger.Info($"Module {module.Id} {modReport.Status} in {modReport.DurationSeconds:0.0}s");
                }
            }
            
            totalTimer.Stop();
            report.TotalTimeSeconds = (float)totalTimer.Elapsed.TotalSeconds;
            logger.Info($"Pipeline completed in {report.TotalTimeSeconds:0.0}s");
        }
    }

    internal class SubProgressReporter : IProgressReporter
    {
        private readonly IProgressReporter _parent;
        private readonly float _start;
        private readonly float _end;
        private float _currentLocalProgress;

        public SubProgressReporter(IProgressReporter parent, float start, float end)
        {
            _parent = parent;
            _start = start;
            _end = end;
            _currentLocalProgress = 0f;
        }

        public void Report(string step, float normalized, string detail = null)
        {
            _currentLocalProgress = normalized;
            float p = _start + normalized * (_end - _start);
            _parent.Report(step, p, detail);
        }

        public IProgressReporter CreateSubProgress(float weight)
        {
            float subStart = _start + _currentLocalProgress * (_end - _start);
            float subEnd = Mathf.Clamp01(subStart + weight * (_end - _start));
            return new SubProgressReporter(_parent, subStart, subEnd);
        }
    }
}
