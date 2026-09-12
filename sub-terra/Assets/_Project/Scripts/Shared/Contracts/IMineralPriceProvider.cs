namespace SubTerra.Shared
{
    public interface IMineralPriceProvider
    {
        bool TryGetMineralUnitPrice(string mineralId, out int unitPrice);
    }
}
