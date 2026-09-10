using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Economy;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Drone;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// Integration Scene에서 데모 목표 Director를 Service·이벤트에 연결한다.
    /// Gameplay 계산을 복제하지 않고 성공/상태 이벤트만 전달한다.
    /// </summary>
    public sealed class TutorialDirectorBinder : MonoBehaviour, IGameplayEventSink
    {
        [SerializeField] private DemoObjectiveView objectiveView;
        [SerializeField] private DroneUiBinder droneUiBinder;

        private DemoObjectiveDirector director;
        private DemoObjectivePresenter presenter;
        private SaveRuntimeController runtime;
        private GameBootstrapper bootstrap;
        private InventoryService inventory;
        private EconomyService economy;
        private ProgressionService progression;
        private OutpostService outpost;
        private GameState boundState;
        private bool bound;

        public DemoObjectiveDirector Director => director;
        public DemoObjectivePresenter Presenter => presenter;
        public bool IsBound => bound;

        private void Awake()
        {
            if (objectiveView == null)
            {
                objectiveView = GetComponentInChildren<DemoObjectiveView>(true);
            }

            director = new DemoObjectiveDirector();
            presenter = new DemoObjectivePresenter(objectiveView);
            if (objectiveView != null)
            {
                objectiveView.DismissRequested += OnDismissRequested;
                objectiveView.DetailsRequested += OnDetailsRequested;
                objectiveView.DetailsDismissRequested += OnDetailsDismissRequested;
            }
        }

        private void OnDestroy()
        {
            if (objectiveView != null)
            {
                objectiveView.DismissRequested -= OnDismissRequested;
                objectiveView.DetailsRequested -= OnDetailsRequested;
                objectiveView.DetailsDismissRequested -= OnDetailsDismissRequested;
            }

            Unbind();
        }

        /// <summary>IntegrationRuntimeBinder가 서비스 준비 후 호출한다.</summary>
        public void BindTo(
            GameState gameState,
            InventoryService inventoryService,
            EconomyService economyService,
            ProgressionService progressionService,
            OutpostService outpostService)
        {
            Unbind();
            boundState = gameState;
            inventory = inventoryService;
            economy = economyService;
            progression = progressionService;
            outpost = outpostService;
            runtime = SaveRuntimeController.Instance;
            bootstrap = GameBootstrapper.Instance;

            director ??= new DemoObjectiveDirector();
            presenter ??= new DemoObjectivePresenter(objectiveView);
            director.BindGameState(boundState);

            if (boundState?.Progress != null
                && (!string.IsNullOrEmpty(boundState.Progress.CurrentObjectiveId)
                    || boundState.Progress.CompletedObjectives > 0
                    || boundState.Progress.IsDemoComplete))
            {
                director.RestoreFromProgress(boundState.Progress);
            }
            else
            {
                director.ResetNewGame();
            }

            presenter.Bind(director);

            if (progression != null)
            {
                progression.PurchaseCompleted += OnPurchaseCompleted;
                progression.DeepZoneAccessChanged += OnDeepZoneAccessChanged;
            }

            if (outpost != null)
            {
                outpost.OperationCompleted += OnOutpostOperation;
                if (outpost.State != null && outpost.State.InstalledOutpostIds.Count > 0)
                {
                    director.NotifyOutpostAlreadyInstalled();
                }
            }

            if (boundState != null)
            {
                boundState.StructuralRiskChanged += OnStructuralRiskChanged;
                boundState.GasExposureChanged += OnGasExposureChanged;
                boundState.DemoProgressChanged += OnDemoProgressChanged;
                OnStructuralRiskChanged(boundState.Run.StructuralRisk);
                OnGasExposureChanged(boundState.Run.GasExposure);
            }

            bound = true;
            // 이어하기 시 이미 업그레이드 조건이 맞으면 잠금·목표를 평가한다.
            EvaluateDeepZoneProgress();
        }

        public void Unbind()
        {
            if (progression != null)
            {
                progression.PurchaseCompleted -= OnPurchaseCompleted;
                progression.DeepZoneAccessChanged -= OnDeepZoneAccessChanged;
            }

            if (outpost != null)
            {
                outpost.OperationCompleted -= OnOutpostOperation;
            }

            if (boundState != null)
            {
                boundState.StructuralRiskChanged -= OnStructuralRiskChanged;
                boundState.GasExposureChanged -= OnGasExposureChanged;
                boundState.DemoProgressChanged -= OnDemoProgressChanged;
            }

            presenter?.Unbind();
            inventory = null;
            economy = null;
            progression = null;
            outpost = null;
            boundState = null;
            bound = false;
        }

        public void Publish(GameplayEventDto gameplayEvent)
        {
            director?.OnGameplayEvent(gameplayEvent);
        }

#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
        /// <summary>Development 전용 강제 진행. QA/Release 빌드에서는 컴파일되지 않는다.</summary>
        public void DebugForceAdvanceObjective()
        {
            director?.DebugForceAdvance();
        }
#endif

        private void OnDismissRequested()
        {
            presenter?.DismissGuidance();
        }

        private void OnDetailsRequested()
        {
            presenter?.OpenDetails();
        }

        private void OnDetailsDismissRequested()
        {
            presenter?.CloseDetails();
        }

        private void OnPurchaseCompleted(ProgressionPurchaseResult result)
        {
            director?.OnProgressionPurchaseCompleted(result);
            if (result.IsSuccess)
            {
                // 생산 경로: 구매 성공 후 ProgressionService.TryUnlockDeepZone을 호출한다.
                EvaluateDeepZoneProgress();
            }
        }

        private void OnDeepZoneAccessChanged(ZoneAccessResult result)
        {
            director?.OnDeepZoneAccessChanged(result);
        }

        private void OnDemoProgressChanged()
        {
            // Scene 전환·포탈처럼 State를 직접 전진시킨 경로는 현재 Director에도 동기화한다.
            if (boundState?.Progress != null
                && director != null
                && (director.CurrentObjectiveId != boundState.Progress.CurrentObjectiveId
                    || director.CompletedCount != boundState.Progress.CompletedObjectives
                    || director.IsDemoComplete != boundState.Progress.IsDemoComplete))
            {
                director.RestoreFromProgress(boundState.Progress);
            }

            EvaluateDeepZoneProgress();
        }

        /// <summary>
        /// GetDeepZoneAccess로 조건 충족을 알리고, TryUnlockDeepZone으로 실제 잠금을 커밋한다.
        /// Director는 가짜 이벤트가 아니라 이 Service 결과만으로 deep 목표를 전진한다.
        /// </summary>
        private void EvaluateDeepZoneProgress()
        {
            if (progression == null || boundState?.Progress == null || director == null)
            {
                return;
            }

            if (director.CurrentObjectiveId != DemoObjectiveIds.UnlockDeepZone)
            {
                return;
            }

            var completed = boundState.Progress.CompletedObjectives;
            // 퀘스트 13에 도달한 뒤에만 실제 잠금을 커밋해 선행 행동의 자동 완료를 막는다.
            var unlock = progression.TryUnlockDeepZone(completed);
            if (unlock.IsUnlocked
                && !unlock.DidUnlockNow
                && progression.State != null
                && progression.State.IsZoneUnlocked(DataIds.Zones.Deep))
            {
                // 세이브에 이미 해제된 경우 이벤트가 없으므로 명시 전진.
                director.NotifyDeepZoneAlreadyUnlocked();
            }
        }

        private void OnOutpostOperation(OutpostOperationResult result)
        {
            director?.OnOutpostOperationCompleted(result);
        }

        private void OnStructuralRiskChanged(StructuralRiskLevel level)
        {
            director?.OnStructuralRiskChanged(level);
            var active = level == StructuralRiskLevel.Caution
                || level == StructuralRiskLevel.Critical
                || level == StructuralRiskLevel.Imminent;
            UpdateHazardYield(active);
        }

        private void OnGasExposureChanged(GasRiskLevel level)
        {
            director?.OnGasExposureChanged(level);
            var active = level == GasRiskLevel.Elevated || level == GasRiskLevel.Hazard;
            UpdateHazardYield(active);
        }

        private void UpdateHazardYield(bool hazardFromLatest)
        {
            if (presenter == null || boundState == null)
            {
                return;
            }

            var structural = boundState.Run.StructuralRisk == StructuralRiskLevel.Caution
                || boundState.Run.StructuralRisk == StructuralRiskLevel.Critical
                || boundState.Run.StructuralRisk == StructuralRiskLevel.Imminent;
            var gas = boundState.Run.GasExposure == GasRiskLevel.Elevated
                || boundState.Run.GasExposure == GasRiskLevel.Hazard;
            presenter.SetHazardActive(structural || gas || hazardFromLatest);
        }

    }
}
