namespace SubTerra.App.UI.Hazards
{
    /// <summary>
    /// A가 발행한 구조·가스·전력 결과를 그대로 HUD로 전달한다.
    /// 가스 Critical은 일반 건설 정보보다 앞에 보이도록 우선순위만 결정한다.
    /// </summary>
    public sealed class HazardHudPresenter
    {
        private readonly IHazardStatusView view;
        private IHazardStatusSource source;

        public bool IsBound => source != null;

        public HazardHudPresenter(IHazardStatusView view)
        {
            this.view = view;
        }

        public void Bind(IHazardStatusSource statusSource)
        {
            Unbind();
            source = statusSource;
            if (source == null)
            {
                return;
            }

            source.StructuralStatusChanged += OnStructuralStatusChanged;
            source.GasStatusChanged += OnGasStatusChanged;
            source.PowerStatusChanged += OnPowerStatusChanged;

            OnStructuralStatusChanged(source.StructuralStatus);
            OnGasStatusChanged(source.GasStatus);
            OnPowerStatusChanged(source.PowerStatus);
        }

        public void Unbind()
        {
            if (source == null)
            {
                return;
            }

            source.StructuralStatusChanged -= OnStructuralStatusChanged;
            source.GasStatusChanged -= OnGasStatusChanged;
            source.PowerStatusChanged -= OnPowerStatusChanged;
            source = null;
        }

        private void OnStructuralStatusChanged(HazardStatusReadModel status)
        {
            view?.SetStructuralStatus(status);
        }

        private void OnGasStatusChanged(HazardStatusReadModel status)
        {
            view?.SetGasStatus(status);
            view?.SetGasPriority(status.Severity == HazardSeverity.Critical);
        }

        private void OnPowerStatusChanged(PowerStatusReadModel status)
        {
            view?.SetPowerStatus(status);
        }
    }
}
