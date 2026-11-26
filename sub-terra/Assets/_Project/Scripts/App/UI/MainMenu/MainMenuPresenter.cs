using System;
using SubTerra.App.Save;
using SubTerra.Shared.Localization;

namespace SubTerra.App.UI.MainMenu
{
    public interface IMainMenuView
    {
        void SetSlotDisplay(int slotId, string label, bool canContinue, string statusText);
        void SetSelectedSlot(int slotId, bool canContinue, string message);
        void SetPrimaryAction(string label, bool enabled);
        void SetOverwriteConfirmVisible(bool visible, int slotId);
        void SetSettingsVisible(bool visible);
        void SetSettingsDraft(SettingsValues values);
        void SetVersionLabel(string version);
        void SetMessage(string message);
        void SetQuitBlockedMessage(string message);
        /// <summary>B-150: 빈/정상/손상 카드 표시(노이즈 썸네일, 삭제 버튼 노출).</summary>
        void SetSlotKind(int slotId, SaveSlotCardKind kind);
        void SetDeleteConfirmVisible(bool visible, int slotId);
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
        private readonly SlotDeleteGate deleteGate = new SlotDeleteGate();
        private readonly SettingsSession settings;
        private readonly string gameVersion;
        private int selectedSlot = SavePathPolicy.MinimumSlot;

        public event Action<int> ContinueRequested;
        public event Action<int> StartNewGameConfirmed;
        public event Action QuitConfirmed;
        public event Action<SettingsValues> SettingsApplied;
        /// <summary>삭제 확인을 통과한 슬롯. 실제 파일 삭제는 바인더가 런타임에 위임한다.</summary>
        public event Action<int> DeleteSlotConfirmed;

        public int SelectedSlot => selectedSlot;
        public NewGameOverwriteGate OverwriteGate => overwriteGate;
        public SettingsSession Settings => settings;
        public bool IsOverwriteConfirmOpen => overwriteGate.IsAwaitingConfirm;
        public bool IsDeleteConfirmOpen => deleteGate.IsAwaitingConfirm;

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
                var label = SaveSlotCardFormatter.BuildLabel(metadata, eligibility);
                view.SetSlotDisplay(
                    slot,
                    label,
                    canContinue,
                    SlotContinuePolicy.Describe(eligibility));
                view.SetSlotKind(slot, SaveSlotCardFormatter.KindOf(eligibility));
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
            var message = string.Format(LocalizationService.Get("main_menu.selected_slot"),
                slotId, SlotContinuePolicy.Describe(eligibility));
            if (eligibility == SlotContinueEligibility.Unrecoverable)
            {
                message += LocalizationService.Get("main_menu.selected_slot_corrupted");
            }

            view.SetSelectedSlot(slotId, canContinue, message);
            var empty = eligibility == SlotContinueEligibility.Empty;
            view.SetPrimaryAction(LocalizationService.Get(empty ? "main_menu.start_expedition" : "main_menu.continue"),
                empty || canContinue);
        }

        public void RequestContinue()
        {
            if (deleteGate.IsAwaitingConfirm || overwriteGate.IsAwaitingConfirm) return;
            var metadata = loader.GetSlotMetadata(selectedSlot);
            var eligibility = SlotContinuePolicy.FromMetadata(metadata);
            if (eligibility == SlotContinueEligibility.Empty)
            {
                RequestNewGame();
                return;
            }
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
            if (deleteGate.IsAwaitingConfirm)
            {
                return NewGameRequestStatus.Cancelled;
            }

            var metadata = loader.GetSlotMetadata(selectedSlot);
            var eligibility = SlotContinuePolicy.FromMetadata(metadata);
            var status = overwriteGate.Request(selectedSlot, eligibility);
            if (status == NewGameRequestStatus.AwaitingOverwriteConfirm)
            {
                // 기존 슬롯 덮어쓰기는 명시 확인 없이 수행하지 않는다.
                view.SetOverwriteConfirmVisible(true, selectedSlot);
                view.SetMessage(string.Format(LocalizationService.Get("main_menu.overwrite_status"), selectedSlot));
                return status;
            }

            if (status == NewGameRequestStatus.ReadyToStart)
            {
                view.SetOverwriteConfirmVisible(false, selectedSlot);
                StartNewGameConfirmed?.Invoke(selectedSlot);
            }
            else
            {
                view.SetMessage(LocalizationService.Get("main_menu.start_failed"));
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
            view.SetMessage(LocalizationService.Get("main_menu.start_cancelled"));
            return status;
        }

        /// <summary>B-150: 휴지통 버튼. 기록이 있는 슬롯만 확인 창을 연다(확인 전 삭제 없음).</summary>
        public bool RequestDeleteSlot(int slotId)
        {
            if (overwriteGate.IsAwaitingConfirm
                || slotId < SavePathPolicy.MinimumSlot
                || slotId > SavePathPolicy.MaximumSlot)
            {
                return false;
            }

            var eligibility = SlotContinuePolicy.FromMetadata(loader.GetSlotMetadata(slotId));
            if (!deleteGate.Request(slotId, eligibility))
            {
                return false;
            }

            if (selectedSlot != slotId)
            {
                SelectSlot(slotId);
            }

            view.SetDeleteConfirmVisible(true, slotId);
            return true;
        }

        public int ConfirmDeleteSlot()
        {
            var slot = deleteGate.Confirm();
            view.SetDeleteConfirmVisible(false, slot);
            if (slot > 0)
            {
                DeleteSlotConfirmed?.Invoke(slot);
            }

            return slot;
        }

        public void CancelDeleteSlot()
        {
            // 취소: 파일 불변. 게이트만 닫는다.
            var slot = deleteGate.PendingSlotId;
            deleteGate.Cancel();
            view.SetDeleteConfirmVisible(false, slot);
        }

        /// <summary>바인더가 삭제를 마친 뒤 호출한다. 카드·이어하기 상태를 다시 읽는다.</summary>
        public void CompleteDeleteSlot(int slotId, bool deleted)
        {
            Refresh();
            view.SetMessage(string.Format(
                LocalizationService.Get(deleted ? "main_menu.delete_success" : "main_menu.delete_failed"),
                SaveSlotCardFormatter.Title(slotId)));
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
            SettingsApplied?.Invoke(settings.Applied);
            view.SetSettingsVisible(false);
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
                view.SetQuitBlockedMessage(LocalizationService.Get("main_menu.quit_blocked"));
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
            DeleteSlotConfirmed = null;
        }
    }
}
