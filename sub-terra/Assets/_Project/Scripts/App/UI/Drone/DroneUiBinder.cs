using System.Threading;
using System.Threading.Tasks;
using SubTerra.App.AI;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.UI.Drone
{
    /// <summary>두 드론 UI Prefab과 Shared Provider의 수명을 연결한다.</summary>
    public sealed class DroneUiBinder : MonoBehaviour
    {
        [SerializeField] private DroneDialoguePanelView dialogueView;
        [SerializeField] private DroneDialogueSocket worldDialogueSocket;
        [SerializeField] private DroneReasonPanelView reasonView;
        [SerializeField] private MonoBehaviour contextProviderBehaviour;
        [SerializeField] private GameDataCatalog catalog;
        [SerializeField] private DroneAnalysisSettings settings;
        [SerializeField] private CloudDialogueConfig cloudDialogueConfig;
        [SerializeField, Min(0.1f)] private float refreshInterval = 0.5f;

        private DroneRecommendationPresenter presenter;
        private float nextRefreshAt;

        public DroneRecommendationPresenter Presenter => presenter;
        public bool IsBound => presenter != null && presenter.IsBound;
        public bool HasWorldDialogueSocket => worldDialogueSocket != null
            && worldDialogueSocket.HasRequiredReferences();

        private void Awake()
        {
            EnsurePresenter();
        }

        private void OnEnable()
        {
            TryBindSerializedProvider();
        }

        private void OnDisable()
        {
            presenter?.Unbind();
        }

        private void Update()
        {
            if (!IsBound || Time.unscaledTime < nextRefreshAt)
            {
                return;
            }

            nextRefreshAt = Time.unscaledTime + refreshInterval;
            presenter.Refresh();
        }

        public void BindTo(
            IDroneContextProvider provider,
            DroneAnalysisSettings analysisSettings,
            GameDataCatalog dataCatalog,
            IDroneClock clock = null,
            IDialogueTransport dialogueTransport = null,
            CloudDialogueConfig cloudConfig = null)
        {
            EnsurePresenter();
            settings = analysisSettings;
            catalog = dataCatalog;
            if (provider == null || settings == null || catalog == null)
            {
                presenter.Unbind();
                return;
            }

            var activeClock = clock ?? new UnityRealtimeDroneClock();
            var analysis = new DroneAnalysisService(settings);
            var generator = new TemplateDialogueGenerator(
                catalog.Dialogues,
                activeClock,
                settings);
            var activeCloudConfig = cloudConfig != null
                ? cloudConfig
                : cloudDialogueConfig;
            IDialogueGenerator cloudGenerator = null;
            if (activeCloudConfig != null)
            {
                var options = activeCloudConfig.CreateOptions();
                cloudGenerator = new CloudDialogueGenerator(
                    generator,
                    dialogueTransport ?? new UnityWebRequestDialogueTransport(),
                    options,
                    new CloudDialoguePolicy(activeClock, options));
            }

            presenter.Bind(provider, analysis, generator, cloudGenerator);
            nextRefreshAt = Time.unscaledTime + refreshInterval;
        }

        public DroneAnalysisResult AnalyzeNow()
        {
            return presenter?.Refresh();
        }

        public Task<DialogueGenerationResult> RequestCloudDialogueAsync(
            CloudDialogueEvent eventType,
            CancellationToken cancellationToken = default)
        {
            return presenter == null
                ? Task.FromResult<DialogueGenerationResult>(null)
                : presenter.RequestCloudDialogueAsync(eventType, cancellationToken);
        }

        /// <summary>Unity UI Button에서 연결할 수 있는 사용자 직접 분석 경로.</summary>
        public async void RequestManualAnalysis()
        {
            await RequestCloudDialogueAsync(CloudDialogueEvent.ManualAnalysis);
        }

        public bool HasRequiredReferences()
        {
            // Provider는 Scene의 A Runtime 또는 Integration 어댑터가 주입한다.
            // 추천 근거는 우측 단독 패널 또는 Digger-Bot 통합 창 중 하나면 충분하다.
            return dialogueView != null
                && ResolveReasonView() != null
                && catalog != null
                && settings != null;
        }

        private void TryBindSerializedProvider()
        {
            BindTo(
                contextProviderBehaviour as IDroneContextProvider,
                settings,
                catalog);
        }

        private void EnsurePresenter()
        {
            if (dialogueView == null)
            {
                dialogueView = GetComponentInChildren<DroneDialoguePanelView>(true);
            }

            if (worldDialogueSocket == null)
            {
                worldDialogueSocket = FindFirstObjectByType<DroneDialogueSocket>();
            }

            if (reasonView == null)
            {
                reasonView = GetComponentInChildren<DroneReasonPanelView>(true);
            }

            // 통합 레이아웃에서는 별도 추천 패널 없이 대화 창이 근거를 함께 표시한다.
            var activeReason = ResolveReasonView();
            // world socket이 늦게 연결되면 Presenter를 다시 만들어 말풍선 경로를 복구한다.
            if (presenter == null)
            {
                presenter = new DroneRecommendationPresenter(
                    dialogueView,
                    worldDialogueSocket,
                    activeReason);
            }
        }

        private IDroneReasonView ResolveReasonView()
        {
            if (dialogueView != null && dialogueView.HasIntegratedReasonTexts())
            {
                return dialogueView;
            }

            if (reasonView != null)
            {
                return reasonView;
            }

            return dialogueView as IDroneReasonView;
        }
    }
}
