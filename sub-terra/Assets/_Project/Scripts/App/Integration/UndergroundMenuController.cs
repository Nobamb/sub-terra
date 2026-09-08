using SubTerra.App.Save;
using SubTerra.App.UI;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Outpost;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.App.Integration
{
    // 기존 패널의 Update보다 먼저 Esc를 처리해 닫기와 설정 열기가 겹치지 않게 한다.
    [DefaultExecutionOrder(-300)]
    public sealed class UndergroundMenuController : MonoBehaviour
    {
        [SerializeField] private SurfaceBaseView settingsView;
        [SerializeField] private GameObject settingsRoot;
        [SerializeField] private HudPanelChromeController chrome;
        [SerializeField] private PanelToggleController panels;
        [SerializeField] private OutpostPanelBinder outpost;
        [SerializeField] private EmergencyEscapePanelBinder escape;
        private SettingsSession settings;

        public bool IsSettingsOpen => settings != null && settings.IsOpen;

        private void OnEnable()
        {
            if (settingsView == null) return;
            settings = new SettingsSession(SettingsRuntimeApplier.LoadOrDefaults());
            settingsView.SetSettingsVisible(false);
            settingsView.SettingsApplyClicked += ApplySettings;
            settingsView.SettingsCancelClicked += CloseSettings;
            settingsView.SettingsDefaultsClicked += ResetDefaults;
            settingsView.MasterVolumePreviewChanged += PreviewVolume;
        }

        private void OnDisable()
        {
            CloseSettings();
            if (settingsView == null) return;
            settingsView.SettingsApplyClicked -= ApplySettings;
            settingsView.SettingsCancelClicked -= CloseSettings;
            settingsView.SettingsDefaultsClicked -= ResetDefaults;
            settingsView.MasterVolumePreviewChanged -= PreviewVolume;
            settings = null;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) HandleEscape();
            // 검색창에 입력하는 O는 종료 단축키로 취급하지 않는다.
            var selected = UnityEngine.EventSystems.EventSystem.current;
            if (selected != null && selected.currentSelectedGameObject != null
                && selected.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null) return;
            if (keyboard.oKey.wasPressedThisFrame) RequestQuit();
        }

        public void HandleEscape()
        {
            if (IsSettingsOpen)
            {
                if (settingsView != null && settingsView.TryCloseControlSchemePanel()) return;
                CloseSettings();
                return;
            }

            // 심층 해금 팝업은 닫기 버튼과 동일 경로로 닫고, 설정 창을 열지 않는다.
            if (TryHideOpenDeepZoneUnlockPopup())
            {
                return;
            }

            bool closed = false;
            var rescue = FindFirstObjectByType<EmergencyRescueRuntimeController>();
            if (rescue != null && rescue.IsPanelOpen) { rescue.ClosePanel(); closed = true; }
            if (outpost != null && outpost.Presenter != null && outpost.Presenter.IsInteractionPanelOpen)
            { outpost.ClosePanel(); closed = true; }
            if (escape != null && escape.IsOpen) { escape.Close(); closed = true; }
            if (chrome != null)
            {
                closed |= chrome.IsBuildingMenuOpen || chrome.IsInventoryPanelOpen
                    || chrome.IsGameGuideOpen || chrome.IsDiggerBotOpen;
                chrome.CloseBuildingMenu();
                chrome.CloseInventoryPanel();
                chrome.CloseGameGuide();
                chrome.CloseDiggerBot();
            }
            if (panels != null && panels.IsVisible(RuntimePanelId.Upgrade))
            { panels.CloseUpgrade(); closed = true; }
            if (!closed) OpenSettings();
        }

        public void OpenSettings()
        {
            if (settings == null || settingsView == null || IsSettingsOpen) return;
            settings.Open();
            settingsView.SetSettingsDraft(settings.Draft);
            settingsView.SetSettingsVisible(true);
            // 지하 드론 말풍선과 다른 모달 위에서도 설정 조작이 가능해야 한다.
            if (settingsRoot != null)
                settingsRoot.GetComponent<Canvas>().sortingOrder = SubTerra.App.Tutorial.UiLayerPriority.EmergencyRescueModal + 1;
            UiKeyboardSubmitGuard.ClearSelection();
        }

        public void CloseSettings()
        {
            if (!IsSettingsOpen || settingsView == null) return;
            settings.Cancel();
            settingsView.SetSettingsVisible(false);
            SettingsRuntimeApplier.RestoreAppliedVolume(settings.Applied);
            UiKeyboardSubmitGuard.ClearSelection();
        }

        private static bool TryHideOpenDeepZoneUnlockPopup()
        {
            var views = FindObjectsByType<ProgressionPanelView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var closed = false;
            for (var i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view != null && view.TryHideDeepZoneUnlockPopup())
                {
                    closed = true;
                }
            }

            return closed;
        }

        private void ApplySettings()
        {
            if (!IsSettingsOpen || settingsView == null) return;
            settings.Draft.CopyFrom(settingsView.ReadSettingsDraft(settings.Draft));
            settings.Apply();
            settingsView.SetSettingsVisible(false);
            SettingsRuntimeApplier.Apply(settings.Applied, applyResolution: true);
        }

        private void ResetDefaults()
        {
            if (!IsSettingsOpen || settingsView == null) return;
            settings.ResetDefaults();
            settingsView.SetSettingsDraft(settings.Draft);
        }

        private void PreviewVolume(float volume) => SettingsRuntimeApplier.PreviewMasterVolume(volume);

        public void RequestQuit()
        {
            var runtime = SaveRuntimeController.Instance;
            if (runtime == null) return;
            if (QuitPolicy.Decide(runtime.IsDirty, runtime.IsSaveInProgress) == QuitDecision.DeferWhileSaving)
                return;
            runtime.RequestQuit();
        }
    }
}
