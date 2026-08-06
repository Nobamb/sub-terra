using System;
using SubTerra.App.Progression;
using UnityEngine;

namespace SubTerra.App.UI.Progression
{
    /// <summary>Scene/Prefab 수명과 순수 Presenter를 연결한다.</summary>
    public sealed class ProgressionPanelBinder : MonoBehaviour
    {
        [SerializeField] private ProgressionPanelView view;

        private ProgressionPanelPresenter presenter;

        public ProgressionPanelPresenter Presenter => presenter;
        public bool IsBound => presenter != null && presenter.IsBound;

        private void Awake()
        {
            if (view == null)
            {
                view = GetComponent<ProgressionPanelView>();
            }

            presenter = new ProgressionPanelPresenter(view);
        }

        private void OnEnable()
        {
            // 패널을 다시 열었을 때, 채굴 등으로 바뀐 보유 자원을 즉시 다시 반영한다.
            presenter?.Refresh();
        }

        private void OnDestroy()
        {
            presenter?.Unbind();
            presenter = null;
        }

        public void BindTo(ProgressionService service)
        {
            BindTo(service, null);
        }

        public void BindTo(ProgressionService service, Func<int> completedObjectivesProvider)
        {
            if (presenter == null)
            {
                presenter = new ProgressionPanelPresenter(view);
            }

            presenter.Bind(service, completedObjectivesProvider);
        }

        public bool SelectUpgrade(string upgradeId)
        {
            return presenter != null && presenter.SelectUpgrade(upgradeId);
        }

        public void PurchaseSelected()
        {
            presenter?.RequestPurchase();
        }
    }
}
