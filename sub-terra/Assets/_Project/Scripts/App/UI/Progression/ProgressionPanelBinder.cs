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

        private void OnDestroy()
        {
            presenter?.Unbind();
            presenter = null;
        }

        public void BindTo(ProgressionService service)
        {
            if (presenter == null)
            {
                presenter = new ProgressionPanelPresenter(view);
            }

            presenter.Bind(service);
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
