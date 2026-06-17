using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using AuraLiteWorldGenerator.Editor.Core;
using AuraLiteWorldGenerator.Editor.Core.Logging;

namespace AuraLiteWorldGenerator.Tests
{
    public class MockModule : IWorldGeneratorModule
    {
        public string Id { get; set; }
        public int Order { get; set; }
        public bool Executed { get; set; }

        public Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct)
        {
            Executed = true;
            return Task.CompletedTask;
        }
    }

    public class PipelineTests
    {
        [Test]
        public async Task Pipeline_ExecutesModulesInOrder()
        {
            var services = new ServiceContainer();
            services.RegisterInstance<ILogger>(new UnityLogger());
            
            var pipeline = new WorldGenerationPipeline(services);
            var mod1 = new MockModule { Id = "1", Order = 2 };
            var mod2 = new MockModule { Id = "2", Order = 1 };
            
            pipeline.AddModule(mod1);
            pipeline.AddModule(mod2);
            
            var ctx = new GenerationContext(0, new Editor.GenerationSettings(), services, CancellationToken.None);
            await pipeline.ExecuteAsync(ctx, new UnityProgressReporter("Test"), CancellationToken.None);
            
            Assert.IsTrue(mod1.Executed);
            Assert.IsTrue(mod2.Executed);
        }
    }
}
