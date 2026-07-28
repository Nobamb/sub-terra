namespace SubTerra.App.Inventory
{
    /// <summary>
    /// 인벤토리 계산에 필요한 광물 단위 정의 스냅샷.
    /// ScriptableObject 참조 없이 단위 중량·가격만 담는다.
    /// </summary>
    public readonly struct MineralUnitInfo
    {
        public string Id { get; }
        public string DisplayName { get; }
        public float UnitWeight { get; }
        public int UnitPrice { get; }

        public MineralUnitInfo(string id, string displayName, float unitWeight, int unitPrice)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            UnitWeight = unitWeight;
            UnitPrice = unitPrice;
        }
    }
}
