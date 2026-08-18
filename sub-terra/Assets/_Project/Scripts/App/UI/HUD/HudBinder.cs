using SubTerra.App.Core;
using SubTerra.App.State;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// Scene/Prefab 수명에 Presenter 구독을 연결한다.
    /// OnEnable 구독·풀 렌더, OnDisable 해제로 파괴된 UI가 이벤트에 남지 않게 한다.
    /// UI는 State를 직접 쓰지 않는다.
    /// </summary>
    public sealed class HudBinder : MonoBehaviour
    {
        [SerializeField] private BasicHudView basicHud;
        [SerializeField] private StructuralHudView structuralHud;
        [SerializeField] private GasWarningPanelView gasWarningPanel;

        private HudPresenter presenter;
        private IPlayerHealthSource healthSource;

        public HudPresenter Presenter => presenter;
        public BasicHudView BasicHud => basicHud;
        public StructuralHudView StructuralHud => structuralHud;
        public GasWarningPanelView GasWarningPanel => gasWarningPanel;

        private void OnEnable()
        {
            EnsurePresenter();
            presenter.Bind(ResolveState());
            BindHealthEvents();
        }

        private void OnDisable()
        {
            if (presenter != null)
            {
                presenter.Unbind();
            }

            UnbindHealthEvents();
        }

        /// <summary>테스트·수동 주입용. 활성 중에 호출하면 즉시 재바인드한다.</summary>
        public void BindTo(GameState state)
        {
            EnsurePresenter();
            presenter.Bind(state);
        }

        public void BindHealthSource(IPlayerHealthSource source)
        {
            UnbindHealthEvents();
            healthSource = source;
            BindHealthEvents();
        }

        public bool HasRequiredReferences()
        {
            return basicHud != null
                && structuralHud != null
                && gasWarningPanel != null
                && basicHud.HasRequiredReferences()
                && structuralHud.HasRequiredReferences()
                && gasWarningPanel.HasRequiredReferences();
        }

        private void EnsurePresenter()
        {
            if (presenter != null)
            {
                return;
            }

            var composite = new CompositeHudView(basicHud, structuralHud, gasWarningPanel);
            presenter = new HudPresenter(composite);
        }

        private static GameState ResolveState()
        {
            var root = GameBootstrapper.Instance;
            return root != null ? root.State : null;
        }

        private void BindHealthEvents()
        {
            if (healthSource == null || basicHud == null || !isActiveAndEnabled)
            {
                return;
            }

            healthSource.HealthChanged -= OnHealthChanged;
            healthSource.HealthChanged += OnHealthChanged;
            OnHealthChanged(healthSource.GetHealth());
        }

        private void UnbindHealthEvents()
        {
            if (healthSource != null)
            {
                healthSource.HealthChanged -= OnHealthChanged;
            }
        }

        private void OnHealthChanged(PlayerHealthReadModel health)
        {
            if (basicHud != null)
            {
                basicHud.SetHealth(HudFormatter.FormatHealth(health));
            }
        }
    }
}
