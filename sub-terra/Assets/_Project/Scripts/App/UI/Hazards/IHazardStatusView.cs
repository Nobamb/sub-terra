namespace SubTerra.App.UI.Hazards
{
    public interface IHazardStatusView
    {
        void SetStructuralStatus(HazardStatusReadModel status);
        void SetGasStatus(HazardStatusReadModel status);
        void SetPowerStatus(PowerStatusReadModel status);
        void SetGasPriority(bool isPriority);
    }
}
