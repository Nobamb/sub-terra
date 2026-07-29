using SubTerra.App.Economy;
using UnityEngine;

namespace SubTerra.App.UI.Economy
{
    /// <summary>
    /// Scene/Prefab에서 Economy Presenter와 View를 연결한다.
    /// 수명은 MonoBehaviour, 거래 로직은 Presenter/Service에 둔다.
    /// </summary>
    public sealed class EconomyPanelBinder : MonoBehaviour
    {
        [SerializeField] private EconomyPanelView view;

        private EconomyPanelPresenter presenter;

        public EconomyPanelPresenter Presenter => presenter;
        public bool IsBound => presenter != null && presenter.IsBound;

        private void Awake()
        {
            if (view == null)
            {
                view = GetComponent<EconomyPanelView>();
            }

            presenter = new EconomyPanelPresenter(view);
        }

        private void OnDestroy()
        {
            // 파괴된 UI가 이벤트에 남지 않도록 대칭 Unbind.
            presenter?.Unbind();
            presenter = null;
        }

        public void BindTo(EconomyService economy, CraftingService crafting = null)
        {
            if (presenter == null)
            {
                presenter = new EconomyPanelPresenter(view);
            }

            presenter.Bind(economy, crafting);
        }

        public void Unbind()
        {
            presenter?.Unbind();
        }
    }
}
