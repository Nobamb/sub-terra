using System;
using SubTerra.App.Save;

namespace SubTerra.App.UI.MainMenu
{
    public interface IMainMenuView
    {
        void SetSlotDisplay(int slotId, string label, bool canContinue, string statusText);
        void SetSelectedSlot(int slotId, bool canContinue, string message);
        void SetOverwriteConfirmVisible(bool visible, int slotId);
        void SetSettingsVisible(bool visible);
        void SetSettingsDraft(SettingsValues values);
        void SetVersionLabel(string version);
        void SetMessage(string message);
        void SetQuitBlockedMessage(string message);
    }

    /// <summary>
    /// Main Menu 순수 Presenter.
    /// 슬롯 메타데이터·덮어쓰기 확인·설정 초안·종료 판정만 담당하고,
    /// 실제 Save/Scene/플랫폼 종료는 이벤트로 위임한다.
    /// </summary>
    public sealed class MainMenuPresenter : IDisposable
    {
        private readonly IMainMenuView view;
        private readonly LoadService loader;
        private readonly NewGameOverwriteGate overwriteGate = new NewGameOverwriteGate();
        private readonly SettingsSession settings;
        private readonly string gameVersion;
        private int selectedSlot = SavePathPolicy.MinimumSlot;

        public event Action<int> ContinueRequested;
        public event Action<int> StartNewGameConfirmed;
        public event Action QuitConfirmed;
        public event Action<SettingsValues> SettingsApplied;

        public int SelectedSlot => selectedSlot;
        public NewGameOverwriteGate OverwriteGate => overwriteGate;
        public SettingsSession Settings => settings;
        public bool IsOverwriteConfirmOpen => overwriteGate.IsAwaitingConfirm;

        public MainMenuPresenter(
            IMainMenuView menuView,
            LoadService loadService,
            string version,
            SettingsValues initialSettings = null)
        {
            view = menuView ?? throw new ArgumentNullException(nameof(menuView));
            loader = loadService ?? throw new ArgumentNullException(nameof(loadService));
            gameVersion = string.IsNullOrEmpty(version) ? "0.0.0" : version;
            settings = new SettingsSession(initialSettings);
        }

        public void Refresh()
        {
            view.SetVersionLabel(gameVersion);
            for (var slot = SavePathPolicy.MinimumSlot;
                slot <= SavePathPolicy.MaximumSlot;
                slot++)
            {
                var metadata = loader.GetSlotMetadata(slot);
                var eligibility = SlotContinuePolicy.FromMetadata(metadata);
                var canContinue = SlotContinuePolicy.CanContinue(eligibility);
                var label = BuildSlotLabel(metadata, eligibility);
                view.SetSlotDisplay(
                    slot,
                    label,
                    canContinue,
                    SlotContinuePolicy.Describe(eligibility));
            }

            SelectSlot(selectedSlot);
        }

        public void SelectSlot(int slotId)
        {
            if (slotId < SavePathPolicy.MinimumSlot
                || slotId > SavePathPolicy.MaximumSlot)
            {
                return;
            }

            selectedSlot = slotId;
            var metadata = loader.GetSlotMetadata(slotId);
            var eligibility = SlotContinuePolicy.FromMetadata(metadata);
            var canContinue = SlotContinuePolicy.CanContinue(eligibility);
            var message = "선택 슬롯 " + slotId + " — " + SlotContinuePolicy.Describe(eligibility);
            if (eligibility == SlotContinueEligibility.Unrecoverable)
            {
                message += " (이어하기 불가, 새 게임 시 확인 필요)";
            }

            view.SetSelectedSlot(slotId, canContinue, message);
        }

        public void RequestContinue()
        {
            var metadata = loader.GetSlotMetadata(selectedSlot);
            var eligibility = SlotContinuePolicy.FromMetadata(metadata);
            if (!SlotContinuePolicy.CanContinue(eligibility))
            {
                view.SetMessage(SlotContinuePolicy.Describe(eligibility));
                return;
            }

            // 로드 유효성은 Phase K LoadService가 다시 판정한다.
            ContinueRequested?.Invoke(selectedSlot);
        }

        public NewGameRequestStatus RequestNewGame()
        {
            var metadata = loader.GetSlotMetadata(selectedSlot);
            var eligibility = SlotContinuePolicy.FromMetadata(metadata);
            var status = overwriteGate.Request(selectedSlot, eligibility);
            if (status == NewGameRequestStatus.AwaitingOverwriteConfirm)
            {
                // 기존 슬롯 덮어쓰기는 명시 확인 없이 수행하지 않는다.
                view.SetOverwriteConfirmVisible(true, selectedSlot);
                view.SetMessage("슬롯 " + selectedSlot + "에 세이브가 있습니다. 덮어쓸까요?");
                return status;
            }

            if (status == NewGameRequestStatus.ReadyToStart)
            {
                view.SetOverwriteConfirmVisible(false, selectedSlot);
                StartNewGameConfirmed?.Invoke(selectedSlot);
            }
            else
            {
                view.SetMessage("새 게임을 시작할 수 없습니다.");
            }

            return status;
        }

        public NewGameRequestStatus ConfirmOverwriteNewGame()
        {
            var status = overwriteGate.ConfirmOverwrite();
            view.SetOverwriteConfirmVisible(false, selectedSlot);
            if (status == NewGameRequestStatus.ReadyToStart)
            {
                var slot = overwriteGate.PendingSlotId > 0
                    ? overwriteGate.PendingSlotId
                    : selectedSlot;
                StartNewGameConfirmed?.Invoke(slot);
            }

            return status;
        }

        public NewGameRequestStatus CancelOverwriteNewGame()
        {
            // 취소: 파일·GameState 불변. 게이트만 닫는다.
            var status = overwriteGate.CancelOverwrite();
            view.SetOverwriteConfirmVisible(false, selectedSlot);
            view.SetMessage("새 게임이 취소되었습니다.");
            return status;
        }

        public void OpenSettings()
        {
            settings.Open();
            view.SetSettingsDraft(settings.Draft);
            view.SetSettingsVisible(true);
        }

        public void ApplySettings(SettingsValues edited)
        {
            if (edited != null)
            {
                settings.Draft.CopyFrom(edited);
            }

            settings.Apply();
            view.SetSettingsVisible(false);
            SettingsApplied?.Invoke(settings.Applied);
        }

        public void CancelSettings()
        {
            settings.Cancel();
            view.SetSettingsVisible(false);
        }

        public void ResetSettingsDefaults()
        {
            settings.ResetDefaults();
            view.SetSettingsDraft(settings.Draft);
        }

        public QuitDecision RequestQuit(bool isDirty, bool saveInProgress)
        {
            var decision = QuitPolicy.Decide(isDirty, saveInProgress);
            if (decision == QuitDecision.DeferWhileSaving)
            {
                view.SetQuitBlockedMessage("저장 중입니다. 잠시 후 다시 시도하세요.");
                return decision;
            }

            QuitConfirmed?.Invoke();
            return decision;
        }

        public void Dispose()
        {
            ContinueRequested = null;
            StartNewGameConfirmed = null;
            QuitConfirmed = null;
            SettingsApplied = null;
        }

        private static string BuildSlotLabel(
            SaveSlotMetadata metadata,
            SlotContinueEligibility eligibility)
        {
            if (metadata == null)
            {
                return "Slot ?";
            }

            if (eligibility == SlotContinueEligibility.Empty)
            {
                return "Slot " + metadata.SlotId + "  Empty";
            }

            if (eligibility == SlotContinueEligibility.Unrecoverable)
            {
                return "Slot " + metadata.SlotId + "  [Damaged]";
            }

            return "Slot " + metadata.SlotId
                + "  Gold " + metadata.Gold
                + "  Depth " + metadata.Depth
                + (metadata.IsRecoverableFromBackup ? "  [Backup]" : string.Empty);
        }
    }
}
