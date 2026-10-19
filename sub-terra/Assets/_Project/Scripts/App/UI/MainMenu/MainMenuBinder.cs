using SubTerra.App.Save;
using SubTerra.App.UI.HUD;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// MainMenu Scene 수명과 Presenter를 연결한다.
    /// 저장/Scene/종료 부작용은 SaveRuntimeController에만 위임한다.
    /// </summary>
    public sealed class MainMenuBinder : MonoBehaviour
    {
        [SerializeField] private MainMenuView view;

        private MainMenuPresenter presenter;
        private bool startingNewGame;
        private bool continuing;

        public MainMenuPresenter Presenter => presenter;
        public bool IsBound => presenter != null;

        private void OnEnable()
        {
            if (view == null)
            {
                view = GetComponent<MainMenuView>();
            }

            var runtime = SaveRuntimeController.Instance;
            if (view == null || runtime == null || runtime.Loader == null)
            {
                return;
            }

            var initialSettings = SettingsRuntimeApplier.LoadOrDefaults();
            SettingsRuntimeApplier.Apply(initialSettings, applyResolution: true);

            presenter = new MainMenuPresenter(
                view,
                runtime.Loader,
                Application.version,
                initialSettings);
            presenter.ContinueRequested += OnContinue;
            presenter.StartNewGameConfirmed += OnStartNewGame;
            presenter.QuitConfirmed += OnQuit;
            presenter.SettingsApplied += OnSettingsApplied;
            presenter.DeleteSlotConfirmed += OnDeleteSlotConfirmed;

            view.SlotSelected += presenter.SelectSlot;
            view.ContinueClicked += OnContinueClicked;
            view.NewGameClicked += OnNewGameClicked;
            view.SettingsClicked += OnSettingsClicked;
            view.QuitClicked += OnQuitClicked;
            view.OverwriteConfirmClicked += OnOverwriteConfirm;
            view.OverwriteCancelClicked += OnOverwriteCancel;
            view.DeleteSlotClicked += OnDeleteSlotClicked;
            view.DeleteConfirmClicked += OnDeleteConfirm;
            view.DeleteCancelClicked += OnDeleteCancel;
            view.SettingsApplyClicked += OnSettingsApply;
            view.SettingsCancelClicked += OnSettingsCancel;
            view.SettingsDefaultsClicked += presenter.ResetSettingsDefaults;
            view.MasterVolumePreviewChanged += OnMasterVolumePreview;

            presenter.Refresh();
        }

        private void OnDisable()
        {
            if (view != null && presenter != null)
            {
                view.SlotSelected -= presenter.SelectSlot;
                view.ContinueClicked -= OnContinueClicked;
                view.NewGameClicked -= OnNewGameClicked;
                view.SettingsClicked -= OnSettingsClicked;
                view.QuitClicked -= OnQuitClicked;
                view.OverwriteConfirmClicked -= OnOverwriteConfirm;
                view.OverwriteCancelClicked -= OnOverwriteCancel;
                view.DeleteSlotClicked -= OnDeleteSlotClicked;
                view.DeleteConfirmClicked -= OnDeleteConfirm;
                view.DeleteCancelClicked -= OnDeleteCancel;
                view.SettingsApplyClicked -= OnSettingsApply;
                view.SettingsCancelClicked -= OnSettingsCancel;
                view.SettingsDefaultsClicked -= presenter.ResetSettingsDefaults;
                view.MasterVolumePreviewChanged -= OnMasterVolumePreview;
            }

            if (presenter != null)
            {
                presenter.ContinueRequested -= OnContinue;
                presenter.StartNewGameConfirmed -= OnStartNewGame;
                presenter.QuitConfirmed -= OnQuit;
                presenter.SettingsApplied -= OnSettingsApplied;
                presenter.DeleteSlotConfirmed -= OnDeleteSlotConfirmed;
                presenter.Dispose();
                presenter = null;
            }
        }

        private void Update()
        {
            if (presenter == null || view == null || Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (presenter.Settings.IsOpen) OnSettingsCancel();
                else if (CanOpenSettingsFromEscape()) OnSettingsClicked();
            }
            if (Keyboard.current.xKey.wasPressedThisFrame)
            {
                var selected = UnityEngine.EventSystems.EventSystem.current;
                if (selected != null && selected.currentSelectedGameObject != null
                    && selected.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null) return;
                if (presenter.Settings.IsOpen)
                {
                    if (!view.TryCloseControlSchemePanel()) OnSettingsCancel();
                }
                else if (PopupWindowSorting.Top != null)
                {
                    var topWindow = PopupWindowSorting.Top;
                    var clock = topWindow.GetComponentInParent<MineResetClockOverlay>();
                    if (clock != null && clock.TryClosePopup(topWindow)) return;
                    if (view.IsTopOverwriteWindow(topWindow)) OnOverwriteCancel();
                    else if (view.IsTopDeleteWindow(topWindow)) OnDeleteCancel();
                }
                else if (view.IsOverwriteConfirmVisible) OnOverwriteCancel();
                else if (view.IsDeleteConfirmVisible) OnDeleteCancel();
            }
        }

        public bool HasRequiredReferences()
        {
            return view != null && view.HasRequiredReferences();
        }

        private void OnNewGameClicked()
        {
            if (startingNewGame || continuing) return;
            presenter?.RequestNewGame();
        }

        private void OnContinueClicked()
        {
            if (startingNewGame || continuing) return;
            presenter?.RequestContinue();
        }

        private void OnDeleteSlotClicked(int slotId)
        {
            if (startingNewGame || continuing) return;
            presenter?.RequestDeleteSlot(slotId);
        }

        private void OnDeleteConfirm()
        {
            presenter?.ConfirmDeleteSlot();
        }

        private void OnDeleteCancel()
        {
            presenter?.CancelDeleteSlot();
        }

        private void OnDeleteSlotConfirmed(int slotId)
        {
            // 확인 창을 통과한 경로만 여기에 온다. 실제 삭제는 런타임 세이브 서비스가 한다.
            var runtime = SaveRuntimeController.Instance;
            var deleted = runtime != null && runtime.DeleteSlot(slotId);
            presenter?.CompleteDeleteSlot(slotId, deleted);
        }

        private static bool CanOpenSettingsFromEscape()
        {
#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
            if (SubTerra.App.UI.UiPauseGate.IsHeldBy(
                    SaveRuntimeController.DebugTerminalPauseOwner))
            {
                return false;
            }
#endif
            return true;
        }

        private void OnSettingsClicked()
        {
            if (presenter == null || view == null) return;
            presenter.OpenSettings();
            view.RaiseSettingsAbovePopups();
        }

        private void OnOverwriteConfirm()
        {
            presenter?.ConfirmOverwriteNewGame();
        }

        private void OnOverwriteCancel()
        {
            // 취소 경로: 파일·State 보존. Presenter 게이트만 닫힌다.
            presenter?.CancelOverwriteNewGame();
        }

        private void OnSettingsApply()
        {
            if (presenter == null || view == null)
            {
                return;
            }

            var draft = view.ReadSettingsDraft(presenter.Settings.Draft);
            presenter.ApplySettings(draft);
        }

        private void OnSettingsCancel()
        {
            if (presenter == null)
            {
                return;
            }

            presenter.CancelSettings();
            // 미리듣기 음량을 적용된 값으로 되돌린다.
            SettingsRuntimeApplier.RestoreAppliedVolume(presenter.Settings.Applied);
        }

        private void OnMasterVolumePreview(float volume)
        {
            SettingsRuntimeApplier.PreviewMasterVolume(volume);
        }

        private void OnQuitClicked()
        {
            var runtime = SaveRuntimeController.Instance;
            var dirty = runtime != null && runtime.IsDirty;
            var saving = runtime != null && runtime.IsSaveInProgress;
            presenter?.RequestQuit(dirty, saving);
        }

        private void OnContinue(int slotId)
        {
            if (continuing || startingNewGame) return;
            continuing = true;
            // B-150: 카드 번쩍 → TV 꺼짐 연출 뒤 기존 이어하기 흐름을 그대로 탄다.
            view.PlayExitTransition(() =>
            {
                var runtime = SaveRuntimeController.Instance;
                if (runtime == null)
                {
                    continuing = false;
                    if (view != null) view.CancelExitTransition();
                    return;
                }

                runtime.BeginContinue(slotId, OnContinueCompleted);
            });
        }

        private void OnContinueCompleted(ContinueResult result)
        {
            if (this == null || presenter == null)
            {
                return;
            }

            if (result == null || result.IsSuccess)
            {
                return;
            }

            continuing = false;
            if (view != null)
            {
                view.CancelExitTransition();
            }

            // 로드 실패 후 탐사/지상 Scene에 들어가지 않는다. 오류만 표시.
            presenter.Refresh();
            if (view != null)
            {
                view.SetMessage("이어하기 실패: " + result.Status);
            }
        }

        private void OnStartNewGame(int slotId)
        {
            // 덮어쓰기 확인을 통과한 경로만 여기에 온다.
            if (startingNewGame) return;
            var runtime = SaveRuntimeController.Instance;
            if (runtime == null) return;
            startingNewGame = true;
            view.PlayExitTransition(() =>
            {
                if (runtime == null || !runtime.StartNewGame(slotId, confirmOverwrite: true))
                {
                    startingNewGame = false;
                    if (view != null) view.CancelExitTransition();
                }
            });
        }

        private void OnQuit()
        {
            SaveRuntimeController.Instance?.RequestQuit();
        }

        private void OnSettingsApplied(SettingsValues values)
        {
            SettingsRuntimeApplier.Apply(values, applyResolution: true);
        }
    }
}
