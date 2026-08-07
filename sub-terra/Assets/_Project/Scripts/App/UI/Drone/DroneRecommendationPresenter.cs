using System.Threading;
using System.Threading.Tasks;
using SubTerra.App.AI;
using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;
using SubTerra.Shared;

namespace SubTerra.App.UI.Drone
{
    /// <summary>Shared Context를 분석하고 결과를 두 View에 전달한다. UI는 점수를 계산하지 않는다.</summary>
    public sealed class DroneRecommendationPresenter
    {
        private readonly IDroneDialogueView dialogueView;
        private readonly IDroneDialogueView worldDialogueView;
        private readonly IDroneReasonView reasonView;

        private IDroneContextProvider contextProvider;
        private DroneAnalysisService analysisService;
        private TemplateDialogueGenerator dialogueGenerator;
        private IDialogueGenerator cloudDialogueGenerator;
        private CancellationTokenSource bindingCancellation;
        private int bindingVersion;
        private string lastShownTemplateId = string.Empty;
        private DroneAction lastShownAction;
        private bool hasShownDialogue;

        public bool IsBound =>
            contextProvider != null && analysisService != null && dialogueGenerator != null;

        public DroneRecommendationPresenter(
            IDroneDialogueView droneDialogueView,
            IDroneReasonView droneReasonView)
            : this(droneDialogueView, null, droneReasonView)
        {
        }

        public DroneRecommendationPresenter(
            IDroneDialogueView droneDialogueView,
            IDroneDialogueView droneWorldDialogueView,
            IDroneReasonView droneReasonView)
        {
            dialogueView = droneDialogueView;
            worldDialogueView = droneWorldDialogueView;
            reasonView = droneReasonView;
        }

        public void Bind(
            IDroneContextProvider provider,
            DroneAnalysisService analysis,
            TemplateDialogueGenerator generator)
        {
            Bind(provider, analysis, generator, null);
        }

        public void Bind(
            IDroneContextProvider provider,
            DroneAnalysisService analysis,
            TemplateDialogueGenerator generator,
            IDialogueGenerator cloudGenerator)
        {
            Unbind();
            contextProvider = provider;
            analysisService = analysis;
            dialogueGenerator = generator;
            cloudDialogueGenerator = cloudGenerator;
            bindingCancellation = new CancellationTokenSource();

            // digger-bot 하단 창 가시성은 HudPanelChromeController(Tab/클릭/X)가 소유한다.
            // Presenter는 머리 위 말풍선(World)과 창 텍스트 갱신을 담당한다.
            worldDialogueView?.SetVisible(IsBound);
            lastShownTemplateId = string.Empty;
            lastShownAction = default;
            hasShownDialogue = false;
            if (IsBound)
            {
                Refresh();
            }
        }

        public void Unbind()
        {
            bindingVersion++;
            bindingCancellation?.Cancel();
            bindingCancellation?.Dispose();
            bindingCancellation = null;
            contextProvider = null;
            analysisService = null;
            dialogueGenerator = null;
            cloudDialogueGenerator = null;
            lastShownTemplateId = string.Empty;
            lastShownAction = default;
            hasShownDialogue = false;
            worldDialogueView?.SetVisible(false);
        }

        public DroneAnalysisResult Refresh()
        {
            if (!IsBound)
            {
                return null;
            }

            var analysis = AnalyzeCurrentContext();
            reasonView?.SetAnalysis(analysis);

            // 상황(추천 행동·템플릿)이 바뀌면 쿨다운을 무시하고 말풍선/창 대사를 즉시 갱신한다.
            var situationChanged = analysis?.Dialogue != null
                && (!hasShownDialogue
                    || !string.Equals(
                        analysis.Dialogue.TemplateId,
                        lastShownTemplateId,
                        System.StringComparison.Ordinal)
                    || analysis.RecommendedAction != lastShownAction);

            var dialogue = dialogueGenerator.Generate(analysis, situationChanged);
            if (!dialogue.IsSuppressed)
            {
                lastShownTemplateId = dialogue.TemplateId ?? string.Empty;
                lastShownAction = analysis != null
                    ? analysis.RecommendedAction
                    : default;
                hasShownDialogue = true;
                dialogueView?.SetDialogue(dialogue);
                worldDialogueView?.SetDialogue(dialogue);
            }

            return analysis;
        }

        /// <summary>허용된 이벤트 또는 사용자 직접 요청에서만 클라우드 표현 경로를 연다.</summary>
        public async Task<DialogueGenerationResult> RequestCloudDialogueAsync(
            CloudDialogueEvent eventType,
            CancellationToken cancellationToken = default)
        {
            if (!IsBound)
            {
                return null;
            }

            var version = bindingVersion;
            var analysis = AnalyzeCurrentContext();
            reasonView?.SetAnalysis(analysis);

            if (cloudDialogueGenerator == null)
            {
                var templateResult = new DialogueGenerationResult(
                    analysis,
                    dialogueGenerator.Generate(analysis, true),
                    false);
                ShowIfCurrent(templateResult, version);
                return templateResult;
            }

            using (var requestCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    bindingCancellation.Token,
                    cancellationToken))
            {
                DialogueGenerationResult generated;
                try
                {
                    generated = await cloudDialogueGenerator.GenerateAsync(
                        analysis,
                        eventType,
                        requestCancellation.Token);
                }
                catch
                {
                    generated = new DialogueGenerationResult(
                        analysis,
                        dialogueGenerator.Generate(analysis, true),
                        false);
                }

                ShowIfCurrent(generated, version);
                return generated;
            }
        }

        private DroneAnalysisResult AnalyzeCurrentContext()
        {
            DroneContextDto context;
            try
            {
                context = contextProvider.CreateContext();
            }
            catch
            {
                context = null;
            }

            return analysisService.Analyze(context);
        }

        private void ShowIfCurrent(DialogueGenerationResult generated, int version)
        {
            if (generated == null
                || generated.WasCancelled
                || generated.Dialogue == null
                || generated.Dialogue.IsSuppressed
                || version != bindingVersion
                || !IsBound)
            {
                return;
            }

            dialogueView?.SetDialogue(generated.Dialogue);
            worldDialogueView?.SetDialogue(generated.Dialogue);
        }
    }
}
