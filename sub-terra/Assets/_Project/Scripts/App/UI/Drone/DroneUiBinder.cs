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
        [SerializeField] private DroneReasonPanelView reasonView;
        [SerializeField] private MonoBehaviour contextProviderBehaviour;
        [SerializeField] private GameDataCatalog catalog;
        [SerializeField] private DroneAnalysisSettings settings;
        [SerializeField, Min(0.1f)] private float refreshInterval = 0.5f;

        private DroneRecommendationPresenter presenter;
        private float nextRefreshAt;

        public DroneRecommendationPresenter Presenter => presenter;
        public bool IsBound => presenter != null && presenter.IsBound;

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
            IDroneClock clock = null)
        {
            EnsurePresenter();
            settings = analysisSettings;
            catalog = dataCatalog;
            if (provider == null || settings == null || catalog == null)
            {
                presenter.Unbind();
                return;
            }

            var analysis = new DroneAnalysisService(settings);
            var generator = new TemplateDialogueGenerator(
                catalog.Dialogues,
                clock ?? new UnityRealtimeDroneClock(),
                settings);
            presenter.Bind(provider, analysis, generator);
            nextRefreshAt = Time.unscaledTime + refreshInterval;
        }

        public DroneAnalysisResult AnalyzeNow()
        {
            return presenter?.Refresh();
        }

        public bool HasRequiredReferences()
        {
            // Provider는 Scene의 A Runtime 또는 Integration 어댑터가 주입한다.
            return dialogueView != null
                && reasonView != null
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

            if (reasonView == null)
            {
                reasonView = GetComponentInChildren<DroneReasonPanelView>(true);
            }

            if (presenter == null)
            {
                presenter = new DroneRecommendationPresenter(dialogueView, reasonView);
            }
        }
    }
}
