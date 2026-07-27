namespace SubTerra.Gameplay.Building
{
    /// <summary>Adapter point for App-owned inventory and cost handling.</summary>
    public interface IBuildingResourceWallet
    {
        bool CanAfford(string buildingId);
        bool TrySpend(string buildingId);
    }
}
