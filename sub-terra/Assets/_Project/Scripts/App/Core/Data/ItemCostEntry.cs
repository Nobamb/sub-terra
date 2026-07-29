using System;
using UnityEngine;

namespace SubTerra.App.Core.Data
{
    /// <summary>
    /// App 로컬(ScriptableObject 직렬화) 비용 항목.
    /// 런타임 지갑 계약은 Shared ItemCostDto를 쓰며, ItemCostMapping으로 변환한다.
    /// 플레이어 보유량이 아닌 정적 정의(필요 ID·수량)만 담는다.
    /// </summary>
    [Serializable]
    public struct ItemCostEntry
    {
        [SerializeField] private string itemId;
        [SerializeField] private int quantity;

        public string ItemId => itemId;
        public int Quantity => quantity;

        public ItemCostEntry(string itemId, int quantity)
        {
            this.itemId = itemId;
            this.quantity = quantity;
        }
    }
}
