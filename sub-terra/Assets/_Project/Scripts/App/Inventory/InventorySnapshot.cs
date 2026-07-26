using System.Collections.Generic;

namespace SubTerra.App.Inventory
{
    /// <summary>광물 한 스택의 읽기 전용 스냅샷. 수량 0 항목은 포함하지 않는다.</summary>
    public readonly struct InventoryStackEntry
    {
        public string MineralId { get; }
        public string DisplayName { get; }
        public int Quantity { get; }
        public float UnitWeight { get; }
        public int UnitPrice { get; }

        public InventoryStackEntry(
            string mineralId,
            string displayName,
            int quantity,
            float unitWeight,
            int unitPrice)
        {
            MineralId = mineralId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Quantity = quantity;
            UnitWeight = unitWeight;
            UnitPrice = unitPrice;
        }
    }

    /// <summary>
    /// 인벤토리 전체 읽기 스냅샷.
    /// 중량·가치는 Inventory 계층에서 이미 계산된 값을 담으며 UI가 재계산하지 않는다.
    /// </summary>
    public sealed class InventorySnapshot
    {
        private readonly InventoryStackEntry[] stacks;

        public float CurrentWeight { get; }
        public float MaxCapacity { get; }
        public float UnsettledValue { get; }
        public IReadOnlyList<InventoryStackEntry> Stacks => stacks;

        public InventorySnapshot(
            float currentWeight,
            float maxCapacity,
            float unsettledValue,
            InventoryStackEntry[] stackEntries)
        {
            CurrentWeight = currentWeight < 0f ? 0f : currentWeight;
            MaxCapacity = maxCapacity < 0f ? 0f : maxCapacity;
            UnsettledValue = unsettledValue < 0f ? 0f : unsettledValue;
            stacks = stackEntries ?? System.Array.Empty<InventoryStackEntry>();
        }

        public int GetQuantity(string mineralId)
        {
            if (string.IsNullOrEmpty(mineralId) || stacks.Length == 0)
            {
                return 0;
            }

            for (var i = 0; i < stacks.Length; i++)
            {
                if (stacks[i].MineralId == mineralId)
                {
                    return stacks[i].Quantity;
                }
            }

            return 0;
        }
    }
}
