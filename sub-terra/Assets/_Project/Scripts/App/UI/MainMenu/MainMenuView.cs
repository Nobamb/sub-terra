using System;
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
        [SerializeField] private TMP_Text resolutionLabel;
        [SerializeField] private Button settingsApplyButton;
        [SerializeField] private Button settingsCancelButton;
        [SerializeField] private Button settingsDefaultsButton;

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

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(values.MasterVolume);
            }

            if (resolutionLabel != null)
            {
                resolutionLabel.text =
                    values.ResolutionWidth + " x " + values.ResolutionHeight;
            }
        }

        public void SetVersionLabel(string version)
        {
            if (versionText != null)
            {
                versionText.text = "v" + (version ?? string.Empty);
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
