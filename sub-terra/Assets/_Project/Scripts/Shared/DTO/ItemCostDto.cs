using System;

namespace SubTerra.Shared
{
    /// <summary>
    /// 건설·제작·업그레이드 비용 항목 DTO.
    /// 영구 아이템 ID와 양수 수량만 담으며, Unity Object 참조는 두지 않는다.
    /// App의 ItemCostEntry(ScriptableObject 직렬화)와 동일 의미를 공유 계약으로 노출한다.
    /// </summary>
    [Serializable]
    public readonly struct ItemCostDto
    {
        public string ItemId { get; }
        public int Quantity { get; }

        public ItemCostDto(string itemId, int quantity)
        {
            ItemId = itemId ?? string.Empty;
            Quantity = quantity;
        }
    }
}
