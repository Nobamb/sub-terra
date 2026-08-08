using UnityEngine;

namespace SubTerra.App.UI.Economy
{
    /// <summary>
    /// 판매 목록 한 행 읽기 모델. Presenter가 스냅샷에서 만들고 View는 표시만 한다.
    /// </summary>
    public readonly struct SellMineralRowReadModel
    {
        public string MineralId { get; }
        public string DisplayName { get; }
        public int OwnedQuantity { get; }
        public int UnitPrice { get; }
        public int LinePreviewCredits { get; }
        public bool IsSelected { get; }
        public Sprite Icon { get; }

        public SellMineralRowReadModel(
            string mineralId,
            string displayName,
            int ownedQuantity,
            int unitPrice,
            int linePreviewCredits,
            bool isSelected,
            Sprite icon)
        {
            MineralId = mineralId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            OwnedQuantity = ownedQuantity < 0 ? 0 : ownedQuantity;
            UnitPrice = unitPrice;
            LinePreviewCredits = linePreviewCredits < 0 ? 0 : linePreviewCredits;
            IsSelected = isSelected;
            Icon = icon;
        }
    }
}
