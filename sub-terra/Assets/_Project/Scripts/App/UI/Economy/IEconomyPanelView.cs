using System.Collections.Generic;

namespace SubTerra.App.UI.Economy
{
    /// <summary>
    /// 판매·제작 패널 표시 계약.
    /// State/Inventory를 직접 쓰지 않고 결과 메시지·목록·버튼 활성만 설정한다.
    /// mutation 토큰(Gold/Inventory/Spend)은 메서드명에 쓰지 않는다(E_S05).
    /// 표시용 Credits 접두/접미는 허용한다.
    /// </summary>
    public interface IEconomyPanelView
    {
        void SetStatusMessage(string message);
        void SetStatusDetail(string detail);
        void SetBusy(bool busy);
        void SetVisible(bool visible);

        /// <summary>보유 수량 &gt; 0 광물 행 목록. 표시 전용.</summary>
        void SetSellRows(IReadOnlyList<SellMineralRowReadModel> rows);

        /// <summary>선택 광물 하이라이트·수량·단가 표시.</summary>
        void SetSelectedMineral(string mineralId, int sellQuantity, int owned, int unitPrice);

        /// <summary>수량 +/-/최대 컨트롤 값·범위.</summary>
        void SetSellQuantityControls(int sellQuantity, int min, int max);

        /// <summary>예상 크레딧 미리보기(카탈로그 단가 × 수량).</summary>
        void SetPreviewCredits(int previewCredits, string previewLabel);

        /// <summary>보유 크레딧 라벨. 이름에 Gold 없음 → E_S05 통과.</summary>
        void SetCreditsLabel(int credits);

        /// <summary>선택 판매 / 전체 판매 버튼 활성.</summary>
        void SetSellActionsEnabled(bool sellSelected, bool sellAll);

        /// <summary>판매할 광물이 없을 때 empty 상태.</summary>
        void SetEmptySellState(bool isEmpty, string emptyMessage);
    }
}
