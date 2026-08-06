using UnityEngine;

namespace SubTerra.App.UI.Inventory
{
    /// <summary>인벤토리 서비스 스냅샷을 UI 한 행으로 변환한 읽기 전용 값이다.</summary>
    public readonly struct InventoryStackReadModel
    {
        public string MineralId { get; }
        public string DisplayName { get; }
        public Sprite Icon { get; }
        public int Quantity { get; }

        public InventoryStackReadModel(string mineralId, string displayName, Sprite icon, int quantity)
        {
            MineralId = mineralId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Icon = icon;
            Quantity = quantity < 0 ? 0 : quantity;
        }
    }
}
