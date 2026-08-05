using SubTerra.App.Save;
using SubTerra.Shared;
using UnityEngine;

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

            presenter = new MainMenuPresenter(
                view,
                runtime.Loader,
                Application.version);
            presenter.ContinueRequested += OnContinue;
            presenter.StartNewGameConfirmed += OnStartNewGame;
            presenter.QuitConfirmed += OnQuit;
            presenter.SettingsApplied += OnSettingsApplied;

            view.SlotSelected += presenter.SelectSlot;
            view.ContinueClicked += presenter.RequestContinue;
            view.NewGameClicked += OnNewGameClicked;
            view.SettingsClicked += presenter.OpenSettings;
            view.QuitClicked += OnQuitClicked;
            view.OverwriteConfirmClicked += OnOverwriteConfirm;
            view.OverwriteCancelClicked += OnOverwriteCancel;
            view.SettingsApplyClicked += OnSettingsApply;
            view.SettingsCancelClicked += presenter.CancelSettings;
            view.SettingsDefaultsClicked += presenter.ResetSettingsDefaults;

            presenter.Refresh();
        }

        private void OnDisable()
        {
            if (view != null && presenter != null)
            {
                view.SlotSelected -= presenter.SelectSlot;
                view.ContinueClicked -= presenter.RequestContinue;
                view.NewGameClicked -= OnNewGameClicked;
                view.SettingsClicked -= presenter.OpenSettings;
                view.QuitClicked -= OnQuitClicked;
                view.OverwriteConfirmClicked -= OnOverwriteConfirm;
                view.OverwriteCancelClicked -= OnOverwriteCancel;
                view.SettingsApplyClicked -= OnSettingsApply;
                view.SettingsCancelClicked -= presenter.CancelSettings;
                view.SettingsDefaultsClicked -= presenter.ResetSettingsDefaults;
            }

            if (presenter != null)
            {
                presenter.ContinueRequested -= OnContinue;
                presenter.StartNewGameConfirmed -= OnStartNewGame;
                presenter.QuitConfirmed -= OnQuit;
                presenter.SettingsApplied -= OnSettingsApplied;
                presenter.Dispose();
                presenter = null;
            }
        }

        public bool HasRequiredReferences()
        {
            return view != null && view.HasRequiredReferences();
        }

        private void OnNewGameClicked()
        {
            presenter?.RequestNewGame();
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

        private void OnQuitClicked()
        {
            var runtime = SaveRuntimeController.Instance;
            var dirty = runtime != null && runtime.IsDirty;
            var saving = runtime != null && runtime.IsSaveInProgress;
            presenter?.RequestQuit(dirty, saving);
        }

        private void OnContinue(int slotId)
        {
            SaveRuntimeController.Instance?.BeginContinue(slotId, OnContinueCompleted);
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
            SaveRuntimeController.Instance?.StartNewGame(slotId, confirmOverwrite: true);
        }

        private void OnQuit()
        {
            SaveRuntimeController.Instance?.RequestQuit();
        }

        private void OnSettingsApplied(SettingsValues values)
        {
            if (values == null)
            {
                return;
            }

            AudioListener.volume = Mathf.Clamp01(values.MasterVolume);
            AccessibilityPreferences.ReduceMotion = values.ReduceMotion;
            // 해상도는 플레이어 빌드에서만 적용. Editor는 미리보기만.
            if (!Application.isEditor
                && values.ResolutionWidth > 0
                && values.ResolutionHeight > 0)
            {
                Screen.SetResolution(
                    values.ResolutionWidth,
                    values.ResolutionHeight,
                    FullScreenMode.Windowed);
            }
        }
    }
}
