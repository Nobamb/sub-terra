using System;
using SubTerra.App.RuntimeInfo;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>Main Menu 표시 전용. 골드/인벤토리/레벨을 직접 변경하지 않는다.</summary>
    public sealed class MainMenuView : MonoBehaviour, IMainMenuView
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button[] slotButtons = new Button[3];
        [SerializeField] private TMP_Text[] slotTexts = new TMP_Text[3];
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text versionText;

        [Header("Overwrite Confirm")]
        [SerializeField] private GameObject overwriteConfirmRoot;
        [SerializeField] private TMP_Text overwriteMessageText;
        [SerializeField] private Button overwriteConfirmButton;
        [SerializeField] private Button overwriteCancelButton;

        [Header("Settings")]
        [SerializeField] private GameObject settingsRoot;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeLabel;
        [SerializeField] private Toggle reduceMotionToggle;
        [SerializeField] private TMP_Text reduceMotionLabel;
        [SerializeField] private TMP_Text resolutionLabel;
        [SerializeField] private Button resolutionPrevButton;
        [SerializeField] private Button resolutionNextButton;
        [SerializeField] private TMP_Text languageLabel;
        [SerializeField] private Button languageCycleButton;
        [SerializeField] private TMP_Text bgmHintLabel;
        [SerializeField] private Button settingsApplyButton;
        [SerializeField] private Button settingsCancelButton;
        [SerializeField] private Button settingsDefaultsButton;

        private int draftResolutionWidth = 1920;
        private int draftResolutionHeight = 1080;
        private string draftLanguageCode = GameLanguageCodes.Korean;

        public event Action<int> SlotSelected;
        public event Action ContinueClicked;
        public event Action NewGameClicked;
        public event Action SettingsClicked;
        public event Action QuitClicked;
        public event Action OverwriteConfirmClicked;
        public event Action OverwriteCancelClicked;
        public event Action SettingsApplyClicked;
        public event Action SettingsCancelClicked;
        public event Action SettingsDefaultsClicked;
        /// <summary>슬라이더 조작 시 즉시 음량 미리듣기용.</summary>
        public event Action<float> MasterVolumePreviewChanged;

        private void OnEnable()
        {
            WireSlot(0, SelectSlot1);
            WireSlot(1, SelectSlot2);
            WireSlot(2, SelectSlot3);
            continueButton?.onClick.AddListener(OnContinue);
            newGameButton?.onClick.AddListener(OnNewGame);
            settingsButton?.onClick.AddListener(OnSettings);
            quitButton?.onClick.AddListener(OnQuit);
            overwriteConfirmButton?.onClick.AddListener(OnOverwriteConfirm);
            overwriteCancelButton?.onClick.AddListener(OnOverwriteCancel);
            settingsApplyButton?.onClick.AddListener(OnSettingsApply);
            settingsCancelButton?.onClick.AddListener(OnSettingsCancel);
            settingsDefaultsButton?.onClick.AddListener(OnSettingsDefaults);
            resolutionPrevButton?.onClick.AddListener(OnResolutionPrev);
            resolutionNextButton?.onClick.AddListener(OnResolutionNext);
            languageCycleButton?.onClick.AddListener(OnLanguageCycle);
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
        }

        private void OnDisable()
        {
            UnwireSlot(0, SelectSlot1);
            UnwireSlot(1, SelectSlot2);
            UnwireSlot(2, SelectSlot3);
            continueButton?.onClick.RemoveListener(OnContinue);
            newGameButton?.onClick.RemoveListener(OnNewGame);
            settingsButton?.onClick.RemoveListener(OnSettings);
            quitButton?.onClick.RemoveListener(OnQuit);
            overwriteConfirmButton?.onClick.RemoveListener(OnOverwriteConfirm);
            overwriteCancelButton?.onClick.RemoveListener(OnOverwriteCancel);
            settingsApplyButton?.onClick.RemoveListener(OnSettingsApply);
            settingsCancelButton?.onClick.RemoveListener(OnSettingsCancel);
            settingsDefaultsButton?.onClick.RemoveListener(OnSettingsDefaults);
            resolutionPrevButton?.onClick.RemoveListener(OnResolutionPrev);
            resolutionNextButton?.onClick.RemoveListener(OnResolutionNext);
            languageCycleButton?.onClick.RemoveListener(OnLanguageCycle);
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            }
        }

        public void SetSlotDisplay(int slotId, string label, bool canContinue, string statusText)
        {
            if (slotId < 1 || slotId > 3)
            {
                return;
            }

            var index = slotId - 1;
            if (slotTexts != null && index < slotTexts.Length && slotTexts[index] != null)
            {
                slotTexts[index].text = label ?? string.Empty;
            }
        }

        public void SetSelectedSlot(int slotId, bool canContinue, string message)
        {
            if (continueButton != null)
            {
                continueButton.interactable = canContinue;
            }

            if (messageText != null && !string.IsNullOrEmpty(message))
            {
                messageText.text = message;
            }
        }

        public void SetOverwriteConfirmVisible(bool visible, int slotId)
        {
            if (overwriteConfirmRoot != null)
            {
                overwriteConfirmRoot.SetActive(visible);
            }

            if (visible && overwriteMessageText != null)
            {
                overwriteMessageText.text =
                    "슬롯 " + slotId + " 세이브를 덮어쓰시겠습니까?";
            }
        }

        public void SetSettingsVisible(bool visible)
        {
            if (settingsRoot != null)
            {
                settingsRoot.SetActive(visible);
            }
        }

        public void SetSettingsDraft(SettingsValues values)
        {
            if (values == null)
            {
                return;
            }

            draftResolutionWidth = values.ResolutionWidth > 0 ? values.ResolutionWidth : 1920;
            draftResolutionHeight = values.ResolutionHeight > 0 ? values.ResolutionHeight : 1080;
            draftLanguageCode = string.IsNullOrEmpty(values.LanguageCode)
                ? GameLanguageCodes.Korean
                : values.LanguageCode;

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(values.MasterVolume);
            }

            if (reduceMotionToggle != null)
            {
                reduceMotionToggle.SetIsOnWithoutNotify(values.ReduceMotion);
            }

            RefreshSettingsLabels(values.MasterVolume);
        }

        public void SetVersionLabel(string version)
        {
            if (versionText != null)
            {
                versionText.text = BuildVersionInfo.Format(version);
            }
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
            }
        }

        public void SetQuitBlockedMessage(string message)
        {
            SetMessage(message);
        }

        public SettingsValues ReadSettingsDraft(SettingsValues fallback)
        {
            var result = fallback != null ? fallback.Clone() : SettingsValues.CreateDefaults();
            if (masterVolumeSlider != null)
            {
                result.MasterVolume = masterVolumeSlider.value;
            }

            if (reduceMotionToggle != null)
            {
                result.ReduceMotion = reduceMotionToggle.isOn;
            }

            result.ResolutionWidth = draftResolutionWidth;
            result.ResolutionHeight = draftResolutionHeight;
            result.LanguageCode = draftLanguageCode;
            return result;
        }

        public bool HasRequiredReferences()
        {
            return continueButton != null
                && newGameButton != null
                && settingsButton != null
                && quitButton != null
                && versionText != null
                && messageText != null
                && slotButtons != null
                && slotButtons.Length == 3
                && slotTexts != null
                && slotTexts.Length == 3
                && overwriteConfirmRoot != null
                && overwriteConfirmButton != null
                && overwriteCancelButton != null
                && settingsRoot != null
                && settingsApplyButton != null
                && settingsCancelButton != null
                && settingsDefaultsButton != null;
        }

        private void RefreshSettingsLabels(float volume)
        {
            if (masterVolumeLabel != null)
            {
                masterVolumeLabel.text = LocalizationService.FormatMasterVolume(volume);
            }

            if (resolutionLabel != null)
            {
                resolutionLabel.text = LocalizationService.FormatResolution(
                    draftResolutionWidth,
                    draftResolutionHeight);
            }

            if (reduceMotionLabel != null)
            {
                reduceMotionLabel.text = LocalizationService.Get(
                    "settings.reduce_motion",
                    "화면 진동 억제");
            }

            if (languageLabel != null)
            {
                var language = GameLanguageCodes.FromCode(draftLanguageCode);
                languageLabel.text = LocalizationService.Get("settings.language", "언어")
                    + ": "
                    + LocalizationService.FormatLanguage(language);
            }

            if (bgmHintLabel != null)
            {
                bgmHintLabel.text = LocalizationService.Get(
                    "settings.bgm_hint",
                    "BGM 4종(타이틀/기지/탐사/위험)은 마스터 음량으로 조절됩니다.");
            }
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (masterVolumeLabel != null)
            {
                masterVolumeLabel.text = LocalizationService.FormatMasterVolume(value);
            }

            MasterVolumePreviewChanged?.Invoke(value);
        }

        private void OnResolutionPrev()
        {
            var next = ResolutionPresets.Cycle(draftResolutionWidth, draftResolutionHeight, -1);
            draftResolutionWidth = next.width;
            draftResolutionHeight = next.height;
            if (resolutionLabel != null)
            {
                resolutionLabel.text = LocalizationService.FormatResolution(
                    draftResolutionWidth,
                    draftResolutionHeight);
            }
        }

        private void OnResolutionNext()
        {
            var next = ResolutionPresets.Cycle(draftResolutionWidth, draftResolutionHeight, 1);
            draftResolutionWidth = next.width;
            draftResolutionHeight = next.height;
            if (resolutionLabel != null)
            {
                resolutionLabel.text = LocalizationService.FormatResolution(
                    draftResolutionWidth,
                    draftResolutionHeight);
            }
        }

        private void OnLanguageCycle()
        {
            draftLanguageCode = draftLanguageCode == GameLanguageCodes.English
                ? GameLanguageCodes.Korean
                : GameLanguageCodes.English;
            // 라벨 미리보기는 선택 언어 기준으로 갱신(적용 전 미리보기).
            var previous = LocalizationService.Current;
            LocalizationService.SetLanguageCode(draftLanguageCode);
            float volume = masterVolumeSlider != null ? masterVolumeSlider.value : 1f;
            RefreshSettingsLabels(volume);
            LocalizationService.SetLanguage(previous);
        }

        private void WireSlot(int index, UnityEngine.Events.UnityAction action)
        {
            if (slotButtons != null && index < slotButtons.Length)
            {
                slotButtons[index]?.onClick.AddListener(action);
            }
        }

        private void UnwireSlot(int index, UnityEngine.Events.UnityAction action)
        {
            if (slotButtons != null && index < slotButtons.Length)
            {
                slotButtons[index]?.onClick.RemoveListener(action);
            }
        }

        private void SelectSlot1() => SlotSelected?.Invoke(1);
        private void SelectSlot2() => SlotSelected?.Invoke(2);
        private void SelectSlot3() => SlotSelected?.Invoke(3);
        private void OnContinue() => ContinueClicked?.Invoke();
        private void OnNewGame() => NewGameClicked?.Invoke();
        private void OnSettings() => SettingsClicked?.Invoke();
        private void OnQuit() => QuitClicked?.Invoke();
        private void OnOverwriteConfirm() => OverwriteConfirmClicked?.Invoke();
        private void OnOverwriteCancel() => OverwriteCancelClicked?.Invoke();
        private void OnSettingsApply() => SettingsApplyClicked?.Invoke();
        private void OnSettingsCancel() => SettingsCancelClicked?.Invoke();
        private void OnSettingsDefaults() => SettingsDefaultsClicked?.Invoke();
    }
}
