using System.Collections.Generic;

namespace SubTerra.Shared
{
    /// <summary>
    /// 시설·제작 비용의 지불 가능 여부와 실제 차감 경계.
    /// Gameplay(A)는 구체 Inventory/Economy 클래스가 아니라 이 계약만 참조한다.
    /// CanAfford는 상태를 바꾸지 않으며, TrySpend는 전량 검증 후 한 번에만 차감한다.
    /// </summary>
    public interface IResourceWallet
    {
        /// <summary>
        /// 비용 목록을 지불할 수 있는지 읽기 전용으로 검사한다.
        /// 자원 예약·차감·이벤트를 발생시키지 않는다.
        /// </summary>
        bool CanAfford(IReadOnlyList<ItemCostDto> costs);

        /// <summary>
        /// 비용을 다시 검증한 뒤, 전부 보유할 때만 한 번에 차감한다.
        /// 부분 차감 경로가 없으며 실패 시 상태는 이전과 동일하다.
        /// </summary>
        bool TrySpend(IReadOnlyList<ItemCostDto> costs);
    }
}
