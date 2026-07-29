using System.Threading;
using System.Threading.Tasks;
using SubTerra.App.Drone;

namespace SubTerra.App.AI
{
    public interface IDialogueGenerator
    {
        Task<DialogueGenerationResult> GenerateAsync(
            DroneAnalysisResult analysis,
            CloudDialogueEvent eventType,
            CancellationToken cancellationToken);
    }
}
