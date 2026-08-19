using SubTerra.App.Core;
using SubTerra.App.Inventory;
using SubTerra.App.Save;
using SubTerra.App.UI.Economy;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Progression;
using SubTerra.Shared;
using SubTerra.Shared.Localization;
using UnityEngine;

namespace SubTerra.App.UI.SurfaceBase
{
    /// <summary>
    /// Surface Base Scene 조립.
    /// 판매·제작·업그레이드는 기존 Economy/Progression 바인더에 연결하고
    /// 목표·잠금·탐사 시작만 Surface Presenter가 담당한다.
    /// 탐사 단일 비행 가드는 SaveRuntimeController.TryStartExploration 한 경로만 사용한다.
    /// prompt-B 31-1/31-3: 설정·종료는 Main Menu와 동일 정책을 재사용한다.
    /// </summary>
    public sealed class SurfaceBaseBinder : MonoBehaviour
    {
        [SerializeField] private SurfaceBaseView view;
        [SerializeField] private EconomyPanelBinder economyBinder;
        [SerializeField] private ProgressionPanelBinder progressionBinder;

        private SurfaceBasePresenter presenter;
        private SettingsSession settings;
        private InventoryService boundInventory;
        private bool mineResetBusy;

        public SurfaceBasePresenter Presenter => presenter;
        public bool IsBound => presenter != null;

        private void OnEnable()
        {
            if (view == null)
            {
                view = GetComponent<SurfaceBaseView>();
            }

            var runtime = SaveRuntimeController.Instance;
            var bootstrap = GameBootstrapper.Instance;
            if (view == null || runtime == null || bootstrap == null)
            {
                return;
            }

            // 런타임이 Economy/Progression을 소유. Surface는 복제 트랜잭션을 만들지 않는다.
            runtime.EnsureGameplayServices();

            // Surface Base 진입 시 판매 게이트 허용. Binder 대입은 ISellGate setter만 사용.
            if (runtime.SellGate != null)
            {
                runtime.SellGate.IsSellAllowed = true;
            }

            if (economyBinder != null && runtime.Economy != null)
            {
                var state = bootstrap.State;
                var catalog = bootstrap.AssignedCatalog as SubTerra.App.Core.Data.GameDataCatalog;
                // inventory + GameState + optional catalog(아이콘)로 판매 목록/크레딧 배선.
                economyBinder.BindTo(
                    runtime.Economy,
                    runtime.Crafting,
                    runtime.InventoryService,
                    state,
                    catalog);
            }

            if (progressionBinder != null && runtime.Progression != null)
            {
                // 구매 성공 후 TryUnlockDeepZone에 넘길 완료 목표 수(GameState).
                var state = bootstrap.State;
                progressionBinder.BindTo(
                    runtime.Progression,
                    () => state != null && state.Progress != null
                        ? state.Progress.CompletedObjectives
                        : 0);
            }

            // 채굴·판매 후 보유량이 바뀌면 업그레이드 구매 가능 여부를 즉시 갱신한다.
            boundInventory = runtime.InventoryService;
            if (boundInventory != null)
            {
                boundInventory.InventoryChanged += OnInventoryChangedForProgression;
            }

            presenter = new SurfaceBasePresenter(view);
            presenter.Bind(
                bootstrap.State,
                runtime.Progression,
                SaveRuntimeController.MineElevatorEnergyCost);
            var initialSettings = SettingsRuntimeApplier.LoadOrDefaults();
            SettingsRuntimeApplier.Apply(initialSettings, applyResolution: false);
            settings = new SettingsSession(initialSettings);
            view.SetSettingsVisible(false);
            view.SetMineResetConfirmVisible(false);
            view.SetMineResetBusy(false);

            view.ExploreClicked += OnExploreClicked;
            view.SettingsClicked += OnSettingsClicked;
            view.QuitClicked += OnQuitClicked;
            view.SettingsApplyClicked += OnSettingsApply;
            view.SettingsCancelClicked += OnSettingsCancel;
            view.SettingsDefaultsClicked += OnSettingsDefaults;
            view.MasterVolumePreviewChanged += OnMasterVolumePreview;
            view.ResetMineClicked += OnResetMineClicked;
            view.ResetMineConfirmed += OnResetMineConfirmed;
            view.ResetMineCancelled += OnResetMineCancelled;

            presenter.RefreshReadModel();
            if (runtime.ElevatorState == ElevatorTravelState.Arrived)
            {
                view.SetMessage("Arrived · Surface Base 도착");
            }
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.ExploreClicked -= OnExploreClicked;
                view.SettingsClicked -= OnSettingsClicked;
                view.QuitClicked -= OnQuitClicked;
                view.SettingsApplyClicked -= OnSettingsApply;
                view.SettingsCancelClicked -= OnSettingsCancel;
                view.SettingsDefaultsClicked -= OnSettingsDefaults;
                view.MasterVolumePreviewChanged -= OnMasterVolumePreview;
                view.ResetMineClicked -= OnResetMineClicked;
                view.ResetMineConfirmed -= OnResetMineConfirmed;
                view.ResetMineCancelled -= OnResetMineCancelled;
                view.SetSettingsVisible(false);
                view.SetMineResetConfirmVisible(false);
                view.SetMineResetBusy(false);
            }

            if (presenter != null)
            {
                presenter.Dispose();
                presenter = null;
            }

            settings = null;
            mineResetBusy = false;
            if (boundInventory != null)
            {
                boundInventory.InventoryChanged -= OnInventoryChangedForProgression;
                boundInventory = null;
            }

            // Surface 이탈 시 판매 게이트 차단.
            var runtimeOnDisable = SaveRuntimeController.Instance;
            if (runtimeOnDisable?.SellGate != null)
            {
                runtimeOnDisable.SellGate.IsSellAllowed = false;
            }

            economyBinder?.Unbind();
            progressionBinder?.Presenter?.Unbind();
        }

        private void OnInventoryChangedForProgression(InventorySnapshot _)
        {
            progressionBinder?.Presenter?.Refresh();
            // 판매 후 귀환 요약(미정산 가치 등)도 같이 갱신.
            presenter?.RefreshReadModel();
        }

        public bool HasRequiredReferences()
        {
            return view != null && view.HasRequiredReferences();
        }

        private void OnExploreClicked()
        {
            if (presenter == null)
            {
                return;
            }

            // 단일 진입점: 런타임 가드+Scene 로드 결과만 Presenter에 전달한다.
            // Presenter가 자체 가드로 선점 성공 처리하지 않는다.
            presenter.RequestExplorationStart(TryStartExplorationViaRuntime);
        }

        private void OnSettingsClicked()
        {
            if (settings == null || view == null)
            {
                return;
            }

            settings.Open();
            view.SetSettingsDraft(settings.Draft);
            view.SetSettingsVisible(true);
        }

        private void OnSettingsApply()
        {
            if (settings == null || view == null)
            {
                return;
            }

            var draft = view.ReadSettingsDraft(settings.Draft);
            settings.Draft.CopyFrom(draft);
            settings.Apply();
            view.SetSettingsVisible(false);
            SettingsRuntimeApplier.Apply(settings.Applied, applyResolution: true);
            view.SetMineResetConfirmVisible(false);
        }

        private void OnSettingsCancel()
        {
            if (settings == null || view == null)
            {
                return;
            }

            settings.Cancel();
            view.SetSettingsVisible(false);
            SettingsRuntimeApplier.RestoreAppliedVolume(settings.Applied);
        }

        private void OnSettingsDefaults()
        {
            if (settings == null || view == null)
            {
                return;
            }

            settings.ResetDefaults();
            view.SetSettingsDraft(settings.Draft);
        }

        private void OnMasterVolumePreview(float volume)
        {
            SettingsRuntimeApplier.PreviewMasterVolume(volume);
        }

        private void OnResetMineClicked()
        {
            if (mineResetBusy || presenter == null || view == null)
            {
                return;
            }

            var runtime = SaveRuntimeController.Instance;
            if (runtime == null
                || runtime.ActiveSlot == 0
                || runtime.IsSaveInProgress
                || runtime.ExplorationGuard.IsInFlight
                || runtime.ElevatorState == ElevatorTravelState.Calling
                || runtime.ElevatorState == ElevatorTravelState.Moving
                || (settings != null && settings.IsOpen)
                || (economyBinder != null && economyBinder.IsModalVisible))
            {
                view.SetMessage(LocalizationService.Get("mine_reset.fail.busy"));
                return;
            }

            if (!presenter.TryGetMineResetQuote(out var currentGold, out _))
            {
                view.SetMessage(string.Format(
                    LocalizationService.Get("mine_reset.fail.gold"),
                    currentGold));
                return;
            }

            view.SetMessage(string.Empty);
            view.SetMineResetConfirmVisible(true, currentGold);
        }

        private void OnResetMineConfirmed()
        {
            if (mineResetBusy || view == null)
            {
                return;
            }

            var runtime = SaveRuntimeController.Instance;
            var reason = string.Empty;
            mineResetBusy = true;
            view.SetMineResetBusy(true);
            try
            {
                if (runtime == null || !runtime.TryResetMine(out reason))
                {
                    var key = string.IsNullOrEmpty(reason)
                        ? "mine_reset.fail.busy"
                        : reason;
                    var message = LocalizationService.Get(key);
                    if (key == "mine_reset.fail.gold")
                    {
                        var gold = GameBootstrapper.Instance?.State?.Player?.Gold ?? 0;
                        message = string.Format(message, gold);
                    }

                    view.SetMessage(message);
                    return;
                }

                view.SetMessage(LocalizationService.Get("mine_reset.success"));
            }
            finally
            {
                view.SetMineResetConfirmVisible(false);
                view.SetMineResetBusy(false);
                mineResetBusy = false;
            }
        }

        private void OnResetMineCancelled()
        {
            if (mineResetBusy || view == null)
            {
                return;
            }

            view.SetMineResetConfirmVisible(false);
        }

        private void OnQuitClicked()
        {
            var runtime = SaveRuntimeController.Instance;
            if (runtime == null)
            {
                return;
            }

            var decision = QuitPolicy.Decide(runtime.IsDirty, runtime.IsSaveInProgress);
            if (decision == QuitDecision.DeferWhileSaving)
            {
                view?.SetMessage("저장 중입니다. 잠시 후 다시 시도하세요.");
                return;
            }

            runtime.RequestQuit();
        }

        /// <summary>
        /// 런타임 TryStartExploration만 호출. 실패 시 reason을 그대로 돌려 실패 UI를 연다.
        /// </summary>
        private (bool success, string reason) TryStartExplorationViaRuntime()
        {
            var runtime = SaveRuntimeController.Instance;
            if (runtime == null)
            {
                return (false, "저장 런타임 없음");
            }

            // 유효 슬롯·State 없거나 로드 실패 시 광산 Scene에 들어가지 않는다.
            if (!runtime.TryStartExploration(out var reason))
            {
                return (false, reason);
            }

            return (true, string.Empty);
        }
    }
}
