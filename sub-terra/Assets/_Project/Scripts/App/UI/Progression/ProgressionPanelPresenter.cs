using System;
using SubTerra.App.Progression;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 업그레이드 목록·상세·구매 UI Presenter.
    /// 구매 버튼은 ProgressionService만 호출하며 중복 제출을 한 건으로 제한한다.
    /// 구매 성공 후에는 완료 목표 수로 TryUnlockDeepZone을 호출해 심층 잠금을 커밋한다.
    /// </summary>
    public sealed class ProgressionPanelPresenter
    {
        private readonly IProgressionPanelView view;
        private ProgressionService service;
        private Func<int> completedObjectivesProvider;
        private string selectedUpgradeId = string.Empty;
        private bool busy;
        private UpgradeCategory activeCategory = UpgradeCategory.Drill;

        public bool IsBound => service != null;
        public bool IsBusy => busy;
        public string SelectedUpgradeId => selectedUpgradeId;
        public UpgradeCategory ActiveCategory => activeCategory;

        public ProgressionPanelPresenter(IProgressionPanelView view)
        {
            this.view = view;
            if (view is ProgressionPanelView panelView)
            {
                panelView.BindPresenter(this);
            }
        }

        public void Bind(ProgressionService progression)
        {
            Bind(progression, null);
        }

        public void Bind(ProgressionService progression, Func<int> completedObjectives)
        {
            Unbind();
            service = progression;
            completedObjectivesProvider = completedObjectives ?? (() => 0);
            if (service != null)
            {
                service.PurchaseCompleted += OnPurchaseCompleted;
                service.UpgradeChanged += OnUpgradeChanged;
                service.DeepZoneAccessChanged += OnDeepZoneAccessChanged;
            }

            if (view is ProgressionPanelView panelView)
            {
                panelView.BindPresenter(this);
                panelView.SetActiveCategory(activeCategory);
            }

            view?.SetBusy(false);
            view?.SetPurchaseResult(string.Empty, string.Empty);
            Refresh();
            // 바인드 시점에도 조건이 이미 맞으면 잠금을 커밋한다.
            if (service != null)
            {
                RefreshDeepZoneAccess(completedObjectivesProvider(), persistUnlock: true);
            }
        }

        /// <summary>탭 전환. 해당 카테고리 첫 업그레이드를 자동 선택한다.</summary>
        public void SelectCategory(UpgradeCategory category)
        {
            activeCategory = category;
            if (view is ProgressionPanelView panelView)
            {
                panelView.SetActiveCategory(category);
            }

            selectedUpgradeId = string.Empty;
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

            completedObjectivesProvider = null;
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

            var snapshots = service.GetSnapshots();
            view?.SetUpgradeList(snapshots);

            // 현재 탭에 속한 항목만 자동 선택 후보로 쓴다.
            if (string.IsNullOrEmpty(selectedUpgradeId)
                || !UpgradeCategoryRules.Matches(selectedUpgradeId, activeCategory))
            {
                for (var i = 0; i < snapshots.Count; i++)
                {
                    if (UpgradeCategoryRules.Matches(snapshots[i].UpgradeId, activeCategory))
                    {
                        SelectUpgrade(snapshots[i].UpgradeId);
                        return;
                    }
                }

                selectedUpgradeId = string.Empty;
                return;
            }

            if (!string.IsNullOrEmpty(selectedUpgradeId)
                && service.TryGetSnapshot(selectedUpgradeId, out var selected))
            {
                view?.SetSelectedUpgrade(selected);
            }
        }

        private void OnPurchaseCompleted(ProgressionPurchaseResult result)
        {
            ApplyResult(result);
            if (result.IsSuccess)
            {
                // 구매 성공 직후 실제 Service 경로로 심층 잠금을 시도한다.
                var completed = completedObjectivesProvider != null
                    ? completedObjectivesProvider()
                    : 0;
                RefreshDeepZoneAccess(completed, persistUnlock: true);
            }
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
