using System;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using UnityEngine;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// Scene/Prefab 수명과 순수 Presenter를 연결한다.
    /// 업그레이드 패널이 시작 시 비활성이면 BindTo가 Awake보다 먼저 호출될 수 있다.
    /// 그 경우 Awake에서 Presenter를 새로 만들면 Service 바인딩이 풀리므로 보존한다.
    /// </summary>
    public sealed class ProgressionPanelBinder : MonoBehaviour
    {
        [SerializeField] private ProgressionPanelView view;
        [SerializeField] private bool tracePurchases;

        private ProgressionPanelPresenter presenter;
        private ProgressionService service;

        public ProgressionPanelPresenter Presenter => presenter;
        public bool IsBound => presenter != null && presenter.IsBound;

        private void Awake()
        {
            EnsureView();
            // BindTo가 이미 서비스에 연결한 Presenter가 있으면 덮어쓰지 않는다.
            if (presenter == null)
            {
                presenter = new ProgressionPanelPresenter(view);
            }
            else if (view is ProgressionPanelView panelView)
            {
                panelView.BindPresenter(presenter);
            }
        }

        private void OnEnable()
        {
            // 패널을 다시 열었을 때, 채굴 등으로 바뀐 보유 자원을 즉시 다시 반영한다.
            if (presenter != null && presenter.IsBound)
            {
                presenter.Refresh();
            }

            // 열릴 때 다른 HUD에 묻히지 않도록 앞으로.
            if (view != null)
            {
                view.BringToFront();
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>한 번도 활성화되지 않은 창도 씬 소유자가 명시적으로 구독을 해제한다.</summary>
        public void Unbind()
        {
            if (service != null)
            {
                service.DeepZoneAccessChanged -= OnDeepZoneAccessChanged;
            }

            presenter?.Unbind();
            presenter = null;
            service = null;
        }

        public void BindTo(ProgressionService service)
        {
            BindTo(service, null);
        }

        public void BindTo(ProgressionService service, Func<int> completedObjectivesProvider)
        {
            EnsureView();
            if (this.service != null)
            {
                this.service.DeepZoneAccessChanged -= OnDeepZoneAccessChanged;
            }

            this.service = service;
            if (this.service != null)
            {
                this.service.DeepZoneAccessChanged += OnDeepZoneAccessChanged;
            }

            if (presenter == null)
            {
                presenter = new ProgressionPanelPresenter(view);
            }

            presenter.Bind(service, completedObjectivesProvider);
        }

        private void OnDeepZoneAccessChanged(ZoneAccessResult result)
        {
            if (result.IsUnlocked && result.DidUnlockNow)
            {
                view?.ShowDeepZoneUnlockPopup();
            }
        }

        public bool SelectUpgrade(string upgradeId)
        {
            if (presenter == null)
            {
                return false;
            }

            // 아직 Bind 전이면 선택 실패 — 호출 측에서 알 수 있게 false.
            if (!presenter.IsBound)
            {
                return false;
            }

            return presenter.SelectUpgrade(upgradeId);
        }

        public void PurchaseSelected()
        {
            var upgradeId = presenter?.SelectedUpgradeId ?? string.Empty;
            TracePurchase("before", upgradeId, string.Empty);
            try
            {
                if (presenter == null)
                {
                    TracePurchase("after", upgradeId, "PresenterMissing");
                    return;
                }

                var result = presenter.RequestPurchase();
                TracePurchase("after", upgradeId, result.Status.ToString());
            }
            catch (Exception exception)
            {
                // 예외를 숨기지 않고 원래 스택을 유지한다. 진단은 발생 단계를 덧붙인다.
                TracePurchase("exception", upgradeId, exception.GetType().Name);
                throw;
            }
        }

        private void TracePurchase(string phase, string upgradeId, string outcome)
        {
            // 일반 플레이에서는 기록하지 않는다. exe도 -trace-upgrades로 같은 진단을 켤 수 있다.
            if (!tracePurchases && Array.IndexOf(Environment.GetCommandLineArgs(), "-trace-upgrades") < 0)
                return;

            var runtime = SaveRuntimeController.Instance;
            var current = runtime != null ? runtime.Progression : null;
            var boundLevel = service != null && service.TryGetSnapshot(upgradeId, out var bound)
                ? bound.CurrentLevel : -1;
            var currentLevel = current != null && current.TryGetSnapshot(upgradeId, out var live)
                ? live.CurrentLevel : -1;
            Debug.Log($"[UpgradePurchaseTrace] phase={phase} frame={Time.frameCount} "
                + $"scene={gameObject.scene.name} binder={GetEntityId()} upgrade={upgradeId} "
                + $"bound={IsBound} currentService={current != null && ReferenceEquals(service, current)} "
                + $"boundLevel={boundLevel} currentLevel={currentLevel} outcome={outcome}", this);
        }

        private void EnsureView()
        {
            if (view == null)
            {
                view = GetComponent<ProgressionPanelView>();
            }
        }
    }
}
