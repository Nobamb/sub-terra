using SubTerra.App.Progression;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 업그레이드 목록·상세·구매 UI Presenter.
    /// 구매 버튼은 ProgressionService만 호출하며 중복 제출을 한 건으로 제한한다.
    /// </summary>
    public sealed class ProgressionPanelPresenter
    {
        private readonly IProgressionPanelView view;
        private ProgressionService service;
        private string selectedUpgradeId = string.Empty;
        private bool busy;

        public bool IsBound => service != null;
        public bool IsBusy => busy;
        public string SelectedUpgradeId => selectedUpgradeId;

        public ProgressionPanelPresenter(IProgressionPanelView view)
        {
            this.view = view;
        }

        public void Bind(ProgressionService progression)
        {
            Unbind();
            service = progression;
            if (service != null)
            {
                service.PurchaseCompleted += OnPurchaseCompleted;
                service.UpgradeChanged += OnUpgradeChanged;
                service.DeepZoneAccessChanged += OnDeepZoneAccessChanged;
            }

            view?.SetBusy(false);
            view?.SetPurchaseResult(string.Empty, string.Empty);
            Refresh();
        }

        public void Unbind()
        {
            if (service != null)
            {
                service.PurchaseCompleted -= OnPurchaseCompleted;
                service.UpgradeChanged -= OnUpgradeChanged;
                service.DeepZoneAccessChanged -= OnDeepZoneAccessChanged;
                service = null;
            }

            busy = false;
            view?.SetBusy(false);
        }

        public bool SelectUpgrade(string upgradeId)
        {
            if (service == null
                || string.IsNullOrEmpty(upgradeId)
                || !service.TryGetSnapshot(upgradeId, out var snapshot))
            {
                return false;
            }

            selectedUpgradeId = upgradeId;
            view?.SetSelectedUpgrade(snapshot);
            return true;
        }

        public ProgressionPurchaseResult RequestPurchase()
        {
            if (busy)
            {
                var result = ProgressionPurchaseResult.Fail(
                    ProgressionPurchaseStatus.Busy,
                    selectedUpgradeId,
                    0,
                    "처리 중입니다.",
                    "Presenter re-entry blocked.");
                ApplyResult(result);
                return result;
            }

            if (service == null)
            {
                var result = ProgressionPurchaseResult.Fail(
                    ProgressionPurchaseStatus.DependencyMissing,
                    selectedUpgradeId,
                    0,
                    "진행도 서비스가 없습니다.",
                    "ProgressionService not bound.");
                ApplyResult(result);
                return result;
            }

            busy = true;
            view?.SetBusy(true);
            try
            {
                return service.TryPurchase(selectedUpgradeId);
            }
            finally
            {
                busy = false;
                view?.SetBusy(false);
            }
        }

        public ZoneAccessResult RefreshDeepZoneAccess(
            int completedObjectives,
            bool persistUnlock = true)
        {
            if (service == null)
            {
                var missing = new ZoneAccessResult(false, false, "진행도 서비스가 없습니다.");
                view?.SetDeepZoneAccess(missing);
                return missing;
            }

            var result = persistUnlock
                ? service.TryUnlockDeepZone(completedObjectives)
                : service.GetDeepZoneAccess(completedObjectives);
            view?.SetDeepZoneAccess(result);
            return result;
        }

        public void Refresh()
        {
            if (service == null)
            {
                view?.SetUpgradeList(System.Array.Empty<UpgradeSnapshot>());
                return;
            }

            view?.SetUpgradeList(service.GetSnapshots());
            if (!string.IsNullOrEmpty(selectedUpgradeId)
                && service.TryGetSnapshot(selectedUpgradeId, out var selected))
            {
                view?.SetSelectedUpgrade(selected);
            }
        }

        private void OnPurchaseCompleted(ProgressionPurchaseResult result)
        {
            ApplyResult(result);
        }

        private void OnUpgradeChanged(UpgradeSnapshot snapshot)
        {
            Refresh();
            if (snapshot.UpgradeId == selectedUpgradeId)
            {
                view?.SetSelectedUpgrade(snapshot);
            }
        }

        private void OnDeepZoneAccessChanged(ZoneAccessResult result)
        {
            view?.SetDeepZoneAccess(result);
        }

        private void ApplyResult(ProgressionPurchaseResult result)
        {
            view?.SetPurchaseResult(
                result.UserMessage,
                result.IsSuccess ? string.Empty : result.Diagnostic);
        }
    }
}
