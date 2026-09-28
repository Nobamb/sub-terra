using SubTerra.App.Save;
using SubTerra.App.UI;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.App.UI.EmergencyRescue;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Outpost;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.App.UI.Tutorial;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SubTerra.App.Integration
{
    // 기존 패널의 Update보다 먼저 설정/팝업 단축키를 처리한다.
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
            var shortcuts = GetComponentsInChildren<UnityEngine.UI.Button>(true);
            for (var i = 0; i < shortcuts.Length; i++)
            {
                if (shortcuts[i].name != "SettingsShortcut") continue;
                var label = shortcuts[i].GetComponentInChildren<TMPro.TMP_Text>(true);
                if (label != null) label.text = "설정(esc)";
                break;
            }
            if (settingsView == null) return;
            var initial = SettingsRuntimeApplier.LoadOrDefaults();
            SettingsRuntimeApplier.ApplyPersistedControlScheme();
            settings = new SettingsSession(initial);
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
            var selected = UnityEngine.EventSystems.EventSystem.current;
            if (selected != null && selected.currentSelectedGameObject != null
                && selected.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null) return;
            if (keyboard.xKey.wasPressedThisFrame) HandleCloseTopPopup();
            if (keyboard.oKey.wasPressedThisFrame) RequestQuit();
        }

        public void HandleEscape()
        {
            if (IsSettingsOpen)
            {
                CloseSettings();
                return;
            }

            OpenSettings();
        }

        public void HandleCloseTopPopup()
        {
            if (IsSettingsOpen)
            {
                if (settingsView != null && settingsView.TryCloseControlSchemePanel()) return;
                CloseSettings();
                return;
            }

            var topWindow = PopupWindowSorting.Top;
            if (topWindow != null)
            {
                var deepZone = topWindow.GetComponent<DeepZoneUnlockPopupEscClose>();
                if (deepZone != null) { deepZone.Close(); return; }
                var rescueView = topWindow.GetComponent<EmergencyRescuePanelView>();
                if (rescueView != null)
                {
                    var rescueController = FindFirstObjectByType<EmergencyRescueRuntimeController>();
                    if (rescueController != null) rescueController.ClosePanel();
                    return;
                }
                var clock = topWindow.GetComponentInParent<MineResetClockOverlay>();
                if (clock != null && clock.TryClosePopup(topWindow)) return;
                if (outpost != null && outpost.IsTopWindow(topWindow))
                { outpost.ClosePanel(); return; }
                if (escape != null && escape.IsTopWindow(topWindow))
                { escape.Close(); return; }
                if (chrome != null && chrome.CloseTopPanel(topWindow)) return;
                if (panels != null && panels.IsVisible(RuntimePanelId.Upgrade)
                    && topWindow.GetComponentInChildren<ProgressionPanelView>(true) != null)
                { panels.CloseUpgrade(); return; }
                return;
            }

            if (TryHideOpenDeepZoneUnlockPopup()) return;

            var objectives = FindFirstObjectByType<DemoObjectiveView>();
            if (objectives != null && objectives.TryCloseTopPopup()) return;

            var rescue = FindFirstObjectByType<EmergencyRescueRuntimeController>();
            if (rescue != null && rescue.IsPanelOpen) { rescue.ClosePanel(); return; }
            if (outpost != null && outpost.Presenter != null && outpost.Presenter.IsInteractionPanelOpen)
            { outpost.ClosePanel(); return; }
            if (escape != null && escape.IsOpen) { escape.Close(); return; }
            if (chrome != null && chrome.CloseTopPanel()) return;
            if (panels != null && panels.IsVisible(RuntimePanelId.Upgrade))
            {
                panels.CloseUpgrade();
                return;
            }
            var placement = FindFirstObjectByType<GameplayBuildingPlacementBridge>();
            if (placement != null) placement.CancelPreview();
        }

        public void OpenSettings()
        {
            if (settings == null || settingsView == null || IsSettingsOpen) return;
            settings.Open();
            settingsView.SetSettingsDraft(settings.Draft);
            settingsView.SetSettingsVisible(true);
            // 지하 드론 말풍선과 다른 모달 위에서도 설정 조작이 가능해야 한다.
            if (settingsRoot != null)
                settingsRoot.GetComponent<Canvas>().sortingOrder = PopupWindowSorting.SettingsSortOrder;
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
            for (var i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view != null && view.TryHideDeepZoneUnlockPopup())
                    return true;
            }

            return false;
        }

        private void ApplySettings()
        {
            if (!IsSettingsOpen || settingsView == null) return;
            settings.Draft.CopyFrom(settingsView.ReadSettingsDraft(settings.Draft));
            settings.Apply();
            SettingsRuntimeApplier.Apply(settings.Applied, applyResolution: true);
            settingsView.SetSettingsVisible(false);
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
