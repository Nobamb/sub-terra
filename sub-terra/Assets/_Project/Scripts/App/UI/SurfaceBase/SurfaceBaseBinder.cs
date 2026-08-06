using SubTerra.App.Core;
using SubTerra.App.Inventory;
using SubTerra.App.Save;
using SubTerra.App.UI.Economy;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Progression;
using SubTerra.Shared;
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
            if (economyBinder != null && runtime.Economy != null)
            {
                economyBinder.BindTo(runtime.Economy, runtime.Crafting);
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

            view.ExploreClicked += OnExploreClicked;
            view.SettingsClicked += OnSettingsClicked;
            view.QuitClicked += OnQuitClicked;
            view.SettingsApplyClicked += OnSettingsApply;
            view.SettingsCancelClicked += OnSettingsCancel;
            view.SettingsDefaultsClicked += OnSettingsDefaults;
            view.MasterVolumePreviewChanged += OnMasterVolumePreview;

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
                view.SetSettingsVisible(false);
            }

            if (presenter != null)
            {
                presenter.Dispose();
                presenter = null;
            }

            settings = null;
            if (boundInventory != null)
            {
                boundInventory.InventoryChanged -= OnInventoryChangedForProgression;
                boundInventory = null;
            }

            economyBinder?.Unbind();
            progressionBinder?.Presenter?.Unbind();
        }

        private void OnInventoryChangedForProgression(InventorySnapshot _)
        {
            progressionBinder?.Presenter?.Refresh();
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
