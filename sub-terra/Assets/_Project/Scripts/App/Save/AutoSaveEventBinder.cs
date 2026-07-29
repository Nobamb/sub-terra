using System;
using SubTerra.App.Economy;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;

namespace SubTerra.App.Save
{
    /// <summary>기존 Service 성공 이벤트를 자동 저장 큐에 연결하고 실패 이벤트는 저장하지 않는다.</summary>
    public sealed class AutoSaveEventBinder : IDisposable
    {
        private readonly AutoSaveCoordinator coordinator;
        private readonly EconomyService economy;
        private readonly ProgressionService progression;
        private readonly OutpostService outpost;

        public AutoSaveEventBinder(
            AutoSaveCoordinator autoSaveCoordinator,
            EconomyService economyService = null,
            ProgressionService progressionService = null,
            OutpostService outpostService = null)
        {
            coordinator = autoSaveCoordinator
                ?? throw new ArgumentNullException(nameof(autoSaveCoordinator));
            economy = economyService;
            progression = progressionService;
            outpost = outpostService;

            if (economy != null)
            {
                economy.AutoSaveRequested += OnEconomySave;
            }

            if (progression != null)
            {
                progression.AutoSaveRequested += OnProgressionSave;
            }

            if (outpost != null)
            {
                outpost.AutoSaveRequested += OnOutpostSave;
            }
        }

        public void Notify(AutoSaveReason reason)
        {
            _ = coordinator.RequestAsync(reason);
        }

        public void Dispose()
        {
            if (economy != null)
            {
                economy.AutoSaveRequested -= OnEconomySave;
            }

            if (progression != null)
            {
                progression.AutoSaveRequested -= OnProgressionSave;
            }

            if (outpost != null)
            {
                outpost.AutoSaveRequested -= OnOutpostSave;
            }
        }

        private void OnEconomySave(EconomyAutoSaveRequest request)
        {
            _ = coordinator.RequestAsync(
                request.Kind == EconomyTransactionKind.Sell
                    ? AutoSaveReason.Settlement
                    : AutoSaveReason.Manual);
        }

        private void OnProgressionSave(ProgressionAutoSaveRequest request)
        {
            _ = coordinator.RequestAsync(AutoSaveReason.UpgradePurchased);
        }

        private void OnOutpostSave(OutpostAutoSaveRequest request)
        {
            _ = coordinator.RequestAsync(
                request.Reason == OutpostAutoSaveReason.Installation
                    ? AutoSaveReason.OutpostInstalled
                    : AutoSaveReason.Settlement);
        }
    }
}
