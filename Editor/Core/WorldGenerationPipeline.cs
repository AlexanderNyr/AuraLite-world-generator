using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            var sortedModules = _modules.OrderBy(m => m.Order).ToList();
            float total = sortedModules.Count;
            
            for (int i = 0; i < sortedModules.Count; i++)
            {
                var module = sortedModules[i];
                ct.ThrowIfCancellationRequested();
                
                float startProgress = i / total;
                float endProgress = (i + 1) / total;
                
                var moduleProgress = new SubProgressReporter(progress, startProgress, endProgress);
                
                try
                {
                    await module.ExecuteAsync(context, moduleProgress, ct);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Module {module.Id} failed: {ex.Message}\n{ex.StackTrace}");
                    throw;
                }
            }
        }
    }

    internal class SubProgressReporter : IProgressReporter
    {
        private readonly IProgressReporter _parent;
        private readonly float _start;
        private readonly float _end;

        public SubProgressReporter(IProgressReporter parent, float start, float end)
        {
            _parent = parent;
            _start = start;
            _end = end;
        }

        public void Report(string step, float normalized, string detail = null)
        {
            float p = _start + normalized * (_end - _start);
            _parent.Report(step, p, detail);
        }

        public IProgressReporter CreateSubProgress(float weight)
        {
            // Simplified for now
            return this;
        }
    }
}
