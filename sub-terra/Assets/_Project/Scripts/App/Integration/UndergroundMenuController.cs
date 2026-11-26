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
        private bool holdsSettingsPause;

        public bool IsSettingsOpen => settings != null && settings.IsOpen;

        private void OnEnable()
        {
            var shortcuts = GetComponentsInChildren<UnityEngine.UI.Button>(true);
            for (var i = 0; i < shortcuts.Length; i++)
            {
                if (shortcuts[i].name != "SettingsShortcut") continue;
                UiSfx.Attach(shortcuts[i], SfxId.None, hover: true);
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
            ReleaseSettingsPause();
            if (settingsView == null) return;
            settingsView.SettingsApplyClicked -= ApplySettings;
            settingsView.SettingsCancelClicked -= CloseSettings;
            settingsView.SettingsDefaultsClicked -= ResetDefaults;
            settingsView.MasterVolumePreviewChanged -= PreviewVolume;
            settings = null;
        }

        private void Update()
        {
            if (holdsSettingsPause && (settingsRoot == null || !settingsRoot.activeInHierarchy))
            {
                if (IsSettingsOpen) settings.Cancel();
                ReleaseSettingsPause();
            }

            if (UiPauseGate.IsDebugTerminalOpen) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            // 종료 확인 창이 ESC를 먼저 받고(-400), 같은 프레임의 다른 단축키를 막는다.
            if (QuitConfirmFlow.IsBlockingInput) return;
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                HandleEscape();
                return;
            }
            var selected = UnityEngine.EventSystems.EventSystem.current;
            if (selected != null && selected.currentSelectedGameObject != null
                && selected.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null) return;
            if (!UiPauseGate.IsInputBlocked && keyboard.oKey.wasPressedThisFrame) RequestQuit();
        }

        public void HandleEscape()
        {
            if (UiPauseGate.IsDebugTerminalOpen || QuitConfirmFlow.IsBlockingInput) return;
            if (HandleCloseTopPopup()) return;
            if (chrome != null && chrome.IsInventoryPanelOpen) UiKeyboardSubmitGuard.ClearSelection();
            OpenSettings();
        }

        public bool HandleCloseTopPopup()
        {
            if (IsSettingsOpen)
            {
                if (settingsView == null || !settingsView.TryCloseControlSchemePanel()) CloseSettings();
                return true;
            }

            if (holdsSettingsPause) return true;

            // 시작 브리핑은 최상위 고정 모달이다. 보관함과 인벤토리는 ESC 후보에서 뺀다.
            var briefing = FindFirstObjectByType<DemoObjectiveView>();
            if (briefing != null && briefing.TryCloseGuidance()) return true;

            var topWindow = PopupWindowSorting.FindTop(canvas =>
                (chrome == null || !chrome.ContainsInventoryWindow(canvas))
                && (outpost == null || !outpost.ContainsStorageWindow(canvas)));
            // 긴급 탈출 창은 일반 팝업 목록 대신 고정된 Canvas 순서를 사용한다.
            if (escape != null && (topWindow == null || EmergencyEscapePanelView.SortingOrder > topWindow.sortingOrder)
                && escape.TryCloseFromEscape()) return true;
            if (topWindow != null)
            {
                if (briefing != null && briefing.TryCloseClaimPopup(topWindow)) return true;
                if (briefing != null && briefing.TryCloseDetailsPopup(topWindow)) return true;
                if (briefing != null && briefing.TryCloseCargoPopup(topWindow)) return true;
                var deepZone = topWindow.GetComponent<DeepZoneUnlockPopupEscClose>();
                if (deepZone != null) { deepZone.Close(); return true; }
                var rescueView = topWindow.GetComponent<EmergencyRescuePanelView>();
                if (rescueView != null)
                {
                    var rescueController = FindFirstObjectByType<EmergencyRescueRuntimeController>();
                    if (rescueController != null) rescueController.ClosePanel();
                    return true;
                }
                var clock = topWindow.GetComponentInParent<MineResetClockOverlay>();
                if (clock != null && clock.TryClosePopup(topWindow)) return true;
                if (outpost != null && outpost.IsTopWindow(topWindow))
                { outpost.ClosePanel(); return true; }
                if (escape != null && escape.IsTopWindow(topWindow) && escape.TryCloseFromEscape()) return true;
                if (chrome != null && chrome.CloseTopPanel(topWindow)) return true;
                if (panels != null && panels.IsVisible(RuntimePanelId.Upgrade)
                    && topWindow.GetComponentInChildren<ProgressionPanelView>(true) != null)
                { panels.CloseUpgrade(); }
                // 최상위 모달이 닫기 연출 중이어도 아래 창이나 설정으로 ESC를 전달하지 않는다.
                return true;
            }

            if (TryHideOpenDeepZoneUnlockPopup()) return true;
            if (briefing != null && briefing.TryCloseTopPopup()) return true;

            var rescue = FindFirstObjectByType<EmergencyRescueRuntimeController>();
            if (rescue != null && rescue.IsPanelOpen) { rescue.ClosePanel(); return true; }
            if (outpost != null && !outpost.IsStorageOpen && outpost.Presenter != null
                && outpost.Presenter.IsInteractionPanelOpen)
            { outpost.ClosePanel(); return true; }
            if (chrome != null && chrome.CloseTopPanel()) return true;
            if (panels != null && panels.IsVisible(RuntimePanelId.Upgrade))
            {
                panels.CloseUpgrade();
                return true;
            }

            var placement = FindFirstObjectByType<GameplayBuildingPlacementBridge>();
            if (placement == null || !placement.HasActivePreview) return false;
            placement.CancelPreview(true);
            return true;
        }

        public void OpenSettings()
        {
            if (settings == null || settingsView == null || settingsRoot == null
                || IsSettingsOpen || !UiPauseGate.CanOpenMineSettings) return;
            // B-166: ESC와 설정 버튼이 모두 이 경로를 지난다. 엘리베이터 탑승 중에는 소리 없이 무시한다.
            if (IsElevatorRiding()) return;
            holdsSettingsPause = UiPauseGate.Acquire(UiPauseGate.MineSettings);
            settings.Open();
            settingsView.SetSettingsDraft(settings.Draft);
            settingsView.SetSettingsVisible(true);
            // 지하 드론 말풍선과 다른 모달 위에서도 설정 조작이 가능해야 한다.
            if (settingsRoot != null)
                settingsView.RaiseSettingsAbovePopups();
            UiKeyboardSubmitGuard.ClearSelection();
        }

        public void CloseSettings()
        {
            if (!IsSettingsOpen || settingsView == null) return;
            settings.Cancel();
            settingsView.SetSettingsVisible(false, ReleaseSettingsPause);
            SettingsRuntimeApplier.RestoreAppliedVolume(settings.Applied);
            UiKeyboardSubmitGuard.ClearSelection();
        }

        private void ReleaseSettingsPause()
        {
            if (!holdsSettingsPause) return;
            holdsSettingsPause = false;
            UiPauseGate.Release(UiPauseGate.MineSettings);
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
            settingsView.PlaySettingsApplySound();
            settings.Apply();
            SettingsRuntimeApplier.Apply(settings.Applied, applyResolution: true);
            settingsView.SetSettingsVisible(false, ReleaseSettingsPause);
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
