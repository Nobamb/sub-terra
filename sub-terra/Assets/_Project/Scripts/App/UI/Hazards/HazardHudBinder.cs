using UnityEngine;

namespace SubTerra.App.UI.Hazards
{
    /// <summary>Hazard HUD Prefab과 Gameplay 상태 Bridge의 구독 수명을 맞춘다.</summary>
    public sealed class HazardHudBinder : MonoBehaviour
    {
        [SerializeField] private HazardHudView view;
        [SerializeField] private MonoBehaviour statusSourceBehaviour;

        private HazardHudPresenter presenter;

        public HazardHudPresenter Presenter => presenter;
        public bool IsBound => presenter != null && presenter.IsBound;

        private void Awake()
        {
            if (view == null)
            {
                view = GetComponent<HazardHudView>();
            }

            presenter = new HazardHudPresenter(view);
        }

        private void OnEnable()
        {
            presenter?.Bind(statusSourceBehaviour as IHazardStatusSource);
        }

        private void OnDisable()
        {
            presenter?.Unbind();
        }

        public void BindTo(IHazardStatusSource source)
        {
            presenter?.Bind(source);
        }

        public bool HasRequiredReferences()
        {
            // Gameplay Bridge는 Scene 객체이므로 Prefab 외부에서 BindTo로 주입한다.
            return view != null;
        }
    }
}
