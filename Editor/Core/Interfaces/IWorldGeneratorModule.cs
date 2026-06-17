using System.Threading;
using System.Threading.Tasks;

namespace AuraLiteWorldGenerator.Editor.Core
{
    /// <summary>
    /// Base interface for all world generation modules.
    /// </summary>
    public interface IWorldGeneratorModule
    {
        string Id { get; }
        int Order { get; }
        Task ExecuteAsync(GenerationContext ctx, IProgressReporter progress, CancellationToken ct);
    }

    /// <summary>
    /// Interface for reporting progress during generation.
    /// </summary>
    public interface IProgressReporter
    {
        void Report(string step, float normalized, string detail = null);
        IProgressReporter CreateSubProgress(float weight);
    }
}
