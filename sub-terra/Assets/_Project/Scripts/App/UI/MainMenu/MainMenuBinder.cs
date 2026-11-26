using SubTerra.App.Audio;
using SubTerra.App.Save;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Intro;
using SubTerra.App.UI.QuitConfirm;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using SubTerra.Shared.Localization;
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

        // B-180 ★: 슬롯 삭제 소리. PM이 번호만 바꿔 끼운다.
        public const SfxId DeleteTrashSfx = SfxId.UiClick;
        public const SfxId DeleteConfirmOpenSfx = SfxId.UiPopupOpen;
        public const SfxId DeleteConfirmSfx = SfxId.RunFailBlackout;
        public const SfxId DeleteCancelSfx = SfxId.UiPopupClose;
        // B-183 ★ 슬롯 선택 연타 간격.
        public const float SlotSelectMinGap = 0.12f;

        private MainMenuPresenter presenter;
        private bool startingNewGame;
        private bool continuing;
        private MinerSelectPanel boundMinerSelect;

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
            view.RefreshMenuLabels();

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
            view.SlotSelected += OnSlotSelectedSfx;
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
                view.SlotSelected -= OnSlotSelectedSfx;
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

            BindMinerSelect(null);

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
            if (UiPauseGate.IsDebugTerminalOpen) return;
            if (presenter == null || view == null || Keyboard.current == null) return;
            // 종료 확인 창이 ESC를 먼저 받는다.
            if (QuitConfirmFlow.IsBlockingInput) return;
            // B-209: 광부 선택 화면이 Esc(돌아가기)·키보드를 직접 받는다.
            if (view.IsMinerSelectOpen) return;
            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

            if (presenter.Settings.IsOpen)
            {
                if (!view.TryCloseControlSchemePanel()) OnSettingsCancel();
                return;
            }

            var topWindow = PopupWindowSorting.Top;
            if (topWindow != null)
            {
                var clock = topWindow.GetComponentInParent<MineResetClockOverlay>();
                if (clock != null && clock.TryClosePopup(topWindow)) return;
                if (view.IsTopOverwriteWindow(topWindow)) OnOverwriteCancel();
                else if (view.IsTopDeleteWindow(topWindow)) OnDeleteCancel();
                return;
            }

            if (view.IsOverwriteConfirmVisible) OnOverwriteCancel();
            else if (view.IsDeleteConfirmVisible) OnDeleteCancel();
            else if (CanOpenSettingsFromEscape()) OnSettingsClicked();
        }

        public bool HasRequiredReferences()
        {
            return view != null && view.HasRequiredReferences();
        }

        private void OnSlotSelectedSfx(int slotId)
        {
            if (view != null) view.PlaySlotSelectedNoise(slotId);
            var player = SfxPlayer.Instance;
            if (player != null && player.SecondsSinceLastPlayed(SfxId.UiSlotSelect) < SlotSelectMinGap) return;
            SfxPlayer.Play(SfxId.UiSlotSelect);
        }

        private void OnNewGameClicked()
        {
            if (startingNewGame || continuing || (view != null && view.IsMinerSelectOpen)) return;
            SfxPlayer.Play(SfxId.UiConfirm);
            presenter?.RequestNewGame();
        }

        private void OnContinueClicked()
        {
            if (startingNewGame || continuing || (view != null && view.IsMinerSelectOpen)) return;
            SfxPlayer.Play(SfxId.UiConfirm);
            presenter?.RequestContinue();
        }

        private void OnDeleteSlotClicked(int slotId)
        {
            if (startingNewGame || continuing) return;
            if (presenter == null) return;
            SfxPlayer.Play(DeleteTrashSfx);
            if (presenter.RequestDeleteSlot(slotId))
            {
                SfxPlayer.Play(DeleteConfirmOpenSfx);
            }
        }

        private void OnDeleteConfirm()
        {
            if (presenter == null) return;
            if (presenter.ConfirmDeleteSlot() > 0)
            {
                SfxPlayer.Play(DeleteConfirmSfx);
            }
        }

        private void OnDeleteCancel()
        {
            if (presenter == null) return;
            // 확인 창이 떠 있었을 때만 닫힘음(취소·X 버튼·ESC 공통).
            var wasOpen = presenter.IsDeleteConfirmOpen;
            presenter.CancelDeleteSlot();
            if (wasOpen)
            {
                SfxPlayer.Play(DeleteCancelSfx);
            }
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
            SfxPlayer.Play(SfxId.UiConfirm);
            presenter?.ConfirmOverwriteNewGame();
        }

        private void OnOverwriteCancel()
        {
            // 취소 경로: 파일·State 보존. Presenter 게이트만 닫힌다.
            SfxPlayer.Play(SfxId.UiClick);
            presenter?.CancelOverwriteNewGame();
        }

        private void OnSettingsApply()
        {
            if (presenter == null || view == null)
            {
                return;
            }

            var draft = view.ReadSettingsDraft(presenter.Settings.Draft);
            view.PlaySettingsApplySound();
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
                    SfxPlayer.Play(SfxId.UiCancelFail);
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
                view.SetMessage(string.Format(LocalizationService.Get("main_menu.continue_failed"), result.Status));
                SfxPlayer.Play(SfxId.UiCancelFail);
            }
        }

        private void OnStartNewGame(int slotId)
        {
            // 덮어쓰기 확인을 통과한 경로만 여기에 온다. B-209: 바로 시작하지 않고 광부 선택 화면을 연다.
            // 저장·덮어쓰기는 선택 화면에서 탐사 시작을 누른 뒤(MinerSelectSession.TryStart)에만 일어난다.
            if (startingNewGame) return;
            var runtime = SaveRuntimeController.Instance;
            if (runtime == null)
            {
                SfxPlayer.Play(SfxId.UiCancelFail);
                return;
            }
            // 같은 클릭에서 이미 확인음이 났으면 연타 제한이 한 번으로 줄인다.
            SfxPlayer.Play(SfxId.UiConfirm);
            if (slotId < SavePathPolicy.MinimumSlot || slotId > SavePathPolicy.MaximumSlot)
            {
                SfxPlayer.Play(SfxId.UiCancelFail);
                return;
            }

            var session = new MinerSelectSession(slotId, StartNewGameEntry);
            var panel = view != null ? view.ShowMinerSelect(session) : null;
            if (panel == null)
            {
                // 화면을 만들지 못하면 예전처럼 기본 광부(남자)로 바로 시작한다.
                Debug.LogWarning("[SubTerra] Miner select screen unavailable; starting with the default miner.");
                StartWithoutMinerSelect(slotId);
                return;
            }

            BindMinerSelect(panel);
        }

        private static bool StartNewGameEntry(int slotId, bool confirmOverwrite, PlayerGender gender)
        {
            var runtime = SaveRuntimeController.Instance;
            return runtime != null && runtime.StartNewGame(slotId, confirmOverwrite, gender);
        }

        private void BindMinerSelect(MinerSelectPanel panel)
        {
            if (boundMinerSelect == panel) return;
            if (boundMinerSelect != null)
            {
                boundMinerSelect.BackRequested -= OnMinerSelectBack;
                boundMinerSelect.LaunchReady -= OnMinerSelectLaunch;
            }

            boundMinerSelect = panel;
            if (boundMinerSelect != null)
            {
                boundMinerSelect.BackRequested += OnMinerSelectBack;
                boundMinerSelect.LaunchReady += OnMinerSelectLaunch;
            }
        }

        private void OnMinerSelectBack()
        {
            // 슬롯·저장은 그대로다. 덮어쓰기 허락도 버린다(다시 새 게임을 누르면 다시 묻는다).
            if (view != null) view.HideMinerSelect();
            if (presenter != null)
            {
                presenter.OverwriteGate.Clear();
                presenter.SelectSlot(presenter.SelectedSlot);
            }
        }

        /// <summary>선택 화면이 검게 닫혔다. 인트로 영상 → 새 게임 시작(씬 로드) → 켜짐 연출 순서로 넘긴다.</summary>
        private void OnMinerSelectLaunch(MinerSelectSession session)
        {
            if (startingNewGame || session == null) return;
            startingNewGame = true;
            try
            {
                // B-212: 고른 광부 성별로 영상을 정한다(여자 파일이 없으면 남자로 폴백). 모션 감소면 파일을 고르지 않는다.
                var reduceMotion = AccessibilityPreferences.ReduceMotion;
                var videoPath = reduceMotion ? null : NewGameIntroPlayer.ResolveVideoPath(session.Flow.Selected);
                var playVideo = NewGameIntroPolicy.ShouldPlayVideo(true, reduceMotion, videoPath != null);
                var player = NewGameIntroPlayer.Create();
                player.StartCoroutine(player.RunNewGameLaunch(
                    playVideo,
                    session.TryStart,
                    IsNewGameReady,
                    OnMinerSelectLaunchFailed,
                    videoPath));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[SubTerra] New game intro failed; starting without it. " + ex);
                if (!session.HasStarted && !session.TryStart())
                {
                    OnMinerSelectLaunchFailed();
                }
            }
        }

        private static bool IsNewGameReady()
        {
            var runtime = SaveRuntimeController.Instance;
            return runtime == null || runtime.IsUiReady;
        }

        private void OnMinerSelectLaunchFailed()
        {
            if (this == null) return;
            startingNewGame = false;
            if (view != null && view.MinerSelect != null) view.MinerSelect.RecoverAfterFailedStart();
            SfxPlayer.Play(SfxId.UiCancelFail);
        }

        private void StartWithoutMinerSelect(int slotId)
        {
            startingNewGame = true;
            view.PlayExitTransition(() =>
            {
                var runtime = SaveRuntimeController.Instance;
                if (runtime == null || !runtime.StartNewGame(slotId, confirmOverwrite: true))
                {
                    startingNewGame = false;
                    if (view != null) view.CancelExitTransition();
                    SfxPlayer.Play(SfxId.UiCancelFail);
                }
            });
        }

        private void OnQuit()
        {
            // B-170: 바로 끄지 않고 종료 확인 창을 띄운다. 저장 중 판단은 presenter가 먼저 했다.
            var intro = view != null ? view.Intro : null;
            QuitConfirmFlow.Request(false, intro, presenter != null && presenter.Settings.IsOpen);
        }

        private void OnSettingsApplied(SettingsValues values)
        {
            var previousLanguage = LocalizationService.Current;
            SettingsRuntimeApplier.Apply(values, applyResolution: true);
            if (LocalizationService.Current != previousLanguage)
            {
                view.RefreshMenuLabels();
                presenter.Refresh();
            }
        }
    }
}
