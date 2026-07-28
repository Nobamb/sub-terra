using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;
using SubTerra.Shared;

namespace SubTerra.App.UI.Drone
{
    /// <summary>Shared Context를 분석하고 결과를 두 View에 전달한다. UI는 점수를 계산하지 않는다.</summary>
    public sealed class DroneRecommendationPresenter
    {
        private readonly IDroneDialogueView dialogueView;
        private readonly IDroneReasonView reasonView;

        private IDroneContextProvider contextProvider;
        private DroneAnalysisService analysisService;
        private TemplateDialogueGenerator dialogueGenerator;

        public bool IsBound =>
            contextProvider != null && analysisService != null && dialogueGenerator != null;

        public DroneRecommendationPresenter(
            IDroneDialogueView droneDialogueView,
            IDroneReasonView droneReasonView)
        {
            dialogueView = droneDialogueView;
            reasonView = droneReasonView;
        }

        public void Bind(
            IDroneContextProvider provider,
            DroneAnalysisService analysis,
            TemplateDialogueGenerator generator)
        {
            Unbind();
            contextProvider = provider;
            analysisService = analysis;
            dialogueGenerator = generator;

            var visible = IsBound;
            dialogueView?.SetVisible(visible);
            reasonView?.SetVisible(visible);
            if (visible)
            {
                Refresh();
            }
        }

        public void Unbind()
        {
            contextProvider = null;
            analysisService = null;
            dialogueGenerator = null;
            dialogueView?.SetVisible(false);
            reasonView?.SetVisible(false);
        }

        public DroneAnalysisResult Refresh()
        {
            if (!IsBound)
            {
                return null;
            }

            DroneContextDto context;
            try
            {
                context = contextProvider.CreateContext();
            }
            catch
            {
                context = null;
            }

            var analysis = analysisService.Analyze(context);
            reasonView?.SetAnalysis(analysis);

            var dialogue = dialogueGenerator.Generate(analysis);
            if (!dialogue.IsSuppressed)
            {
                dialogueView?.SetDialogue(dialogue);
            }

            return analysis;
        }
    }
}
