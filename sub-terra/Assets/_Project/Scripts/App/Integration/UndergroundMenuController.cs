using SubTerra.App.Audio;
using SubTerra.App.Save;
using SubTerra.App.UI;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.App.UI.EmergencyRescue;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Outpost;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.QuitConfirm;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.App.UI.Tutorial;
using SubTerra.Gameplay.Player;
using SubTerra.Shared.Audio;
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
            for (var i = 0; i < shortcuts.Length; i++)
            {
                // B-170: 종료 확인 창 등장음이 클릭음을 대신한다.
                if (shortcuts[i].name == "QuitShortcut") UiSfx.Attach(shortcuts[i], SfxId.None, hover: true);
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
            // B-170: 종료 확인 창이 ESC·X를 먼저 받고(-400), 떠 있는 동안 다른 단축키를 막는다.
            if (QuitConfirmFlow.IsBlockingInput) return;
            // 시작 브리핑처럼 진행을 멈춘 동안에는 설정·종료 단축키를 막고 X(닫기)만 허용한다.
            var paused = UiPauseGate.IsHeld;
            if (!paused && keyboard.escapeKey.wasPressedThisFrame) HandleEscape();
            var selected = UnityEngine.EventSystems.EventSystem.current;
            if (selected != null && selected.currentSelectedGameObject != null
                && selected.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null) return;
            if (keyboard.xKey.wasPressedThisFrame) HandleCloseTopPopup();
            if (!paused && keyboard.oKey.wasPressedThisFrame) RequestQuit();
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

            // 시작 브리핑은 가장 위에 뜨는 모달이므로 다른 팝업보다 먼저 닫는다.
            var briefing = FindFirstObjectByType<DemoObjectiveView>();
            if (briefing != null && briefing.TryCloseGuidance()) return;

            var topWindow = PopupWindowSorting.Top;
            if (topWindow != null)
            {
                if (briefing != null && briefing.TryCloseClaimPopup(topWindow)) return;
                if (briefing != null && briefing.TryCloseDetailsPopup(topWindow)) return;
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
            // B-166: ESC와 설정 버튼이 모두 이 경로를 지난다. 엘리베이터 탑승 중에는 소리 없이 무시한다.
            if (IsElevatorRiding()) return;
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

        /// <summary>B-166: 탑승 중이면 설정창을 열지 않는다. 판정은 ElevatorController.IsRiding 한 곳에서 한다.</summary>
        public static bool IsElevatorRiding()
        {
            var elevators = FindObjectsByType<ElevatorController>(FindObjectsInactive.Exclude);
            for (var i = 0; i < elevators.Length; i++)
            {
                if (elevators[i] != null && elevators[i].IsRiding) return true;
            }

            return false;
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

        /// <summary>메뉴 "게임 종료(O)" 버튼과 O키. B-170: 바로 끄지 않고 종료 확인 창을 띄운다.</summary>
        public void RequestQuit()
        {
            if (SaveRuntimeController.Instance == null) return;
            // 설정 창이 열려 있으면 무시. 탑승 중·일시정지 중 판단은 QuitConfirmFlow가 한다.
            QuitConfirmFlow.Request(true, null, IsSettingsOpen);
        }

        // B-170: App은 Gameplay를 모르므로 엘리베이터 탑승 판정을 여기서 등록한다(지상·광산 모두).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterQuitRideCheck()
        {
            QuitConfirmFlow.RideCheck = IsElevatorRiding;
        }
    }
}
