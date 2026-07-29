using System;
using SubTerra.App.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Save
{
    /// <summary>세 슬롯과 이어하기/복구 선택지를 표시한다. 파일 I/O와 복구 판정은 수행하지 않는다.</summary>
    public sealed class SaveSlotPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button[] slotButtons = new Button[3];
        [SerializeField] private TMP_Text[] slotTexts = new TMP_Text[3];
        [SerializeField] private Button continueButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private TMP_Text messageText;

        public event Action<int> SlotSelected;
        public event Action ContinueRequested;
        public event Action RetryRequested;
        public event Action StartNewGameRequested;

        private void OnEnable()
        {
            if (slotButtons.Length >= 3)
            {
                slotButtons[0]?.onClick.AddListener(SelectSlot1);
                slotButtons[1]?.onClick.AddListener(SelectSlot2);
                slotButtons[2]?.onClick.AddListener(SelectSlot3);
            }

            continueButton?.onClick.AddListener(RequestContinue);
            retryButton?.onClick.AddListener(RequestRetry);
            newGameButton?.onClick.AddListener(RequestNewGame);
        }

        private void OnDisable()
        {
            if (slotButtons.Length >= 3)
            {
                slotButtons[0]?.onClick.RemoveListener(SelectSlot1);
                slotButtons[1]?.onClick.RemoveListener(SelectSlot2);
                slotButtons[2]?.onClick.RemoveListener(SelectSlot3);
            }

            continueButton?.onClick.RemoveListener(RequestContinue);
            retryButton?.onClick.RemoveListener(RequestRetry);
            newGameButton?.onClick.RemoveListener(RequestNewGame);
        }

        public void SetSlot(SaveSlotMetadata metadata)
        {
            if (metadata == null
                || metadata.SlotId < SavePathPolicy.MinimumSlot
                || metadata.SlotId > SavePathPolicy.MaximumSlot)
            {
                return;
            }

            var index = metadata.SlotId - 1;
            if (slotTexts[index] != null)
            {
                if (metadata.CanContinue)
                {
                    slotTexts[index].text = "Slot " + metadata.SlotId
                        + "  Gold " + metadata.Gold
                        + "  Depth " + metadata.Depth
                        + (metadata.IsRecoverableFromBackup ? "  [Backup]" : string.Empty);
                }
                else if (metadata.LoadStatus == LoadStatus.NotFound
                    || metadata.LoadStatus == LoadStatus.InvalidSlot)
                {
                    slotTexts[index].text = "Slot " + metadata.SlotId + "  Empty";
                }
                else
                {
                    slotTexts[index].text = "Slot " + metadata.SlotId + "  [Damaged]";
                }
            }
        }

        public void SetSelectedSlot(int slotId, bool canContinue)
        {
            if (continueButton != null)
            {
                continueButton.interactable = canContinue;
            }

            if (messageText != null)
            {
                messageText.text = "Selected Slot: " + slotId;
            }
        }

        public void ShowLoadResult(LoadResult result)
        {
            var choices = result?.RecoveryChoices ?? SaveRecoveryChoice.None;
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(
                    (choices & SaveRecoveryChoice.Retry) != 0);
            }

            if (newGameButton != null)
            {
                newGameButton.gameObject.SetActive(
                    (choices & SaveRecoveryChoice.StartNewGame) != 0);
            }

            if (messageText != null && result != null)
            {
                messageText.text = result.Status switch
                {
                    LoadStatus.RecoveredFromBackup => "Recovered from the backup save.",
                    LoadStatus.BothCopiesInvalid => "The save and backup are both invalid.",
                    LoadStatus.FutureVersion => "This save requires a newer game version.",
                    LoadStatus.IoFailure => "The save files are not accessible.",
                    LoadStatus.NotFound => "No save exists in this slot.",
                    _ => string.Empty
                };
            }
        }

        public void SetVisible(bool visible)
        {
            (panelRoot != null ? panelRoot : gameObject).SetActive(visible);
        }

        public bool HasRequiredReferences()
        {
            return panelRoot != null
                && slotButtons != null
                && slotButtons.Length == 3
                && slotTexts != null
                && slotTexts.Length == 3
                && continueButton != null
                && retryButton != null
                && newGameButton != null
                && messageText != null;
        }

        private void SelectSlot1() => SlotSelected?.Invoke(1);
        private void SelectSlot2() => SlotSelected?.Invoke(2);
        private void SelectSlot3() => SlotSelected?.Invoke(3);
        private void RequestContinue() => ContinueRequested?.Invoke();
        private void RequestRetry() => RetryRequested?.Invoke();
        private void RequestNewGame() => StartNewGameRequested?.Invoke();
    }
}
