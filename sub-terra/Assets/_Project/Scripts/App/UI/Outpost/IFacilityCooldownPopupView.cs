using System;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 재사용 대기 안내 팝업 계약. 남은 시간은 저장된 쿨타임만 읽으며 팝업이 독립 타이머를 갖지 않는다.
    /// IOutpostPanelView를 넓히지 않도록 선택 인터페이스로 둔다. 구현하지 않은 View는 기존 문구 안내를 쓴다.
    /// </summary>
    public interface IFacilityCooldownPopupView
    {
        /// <summary>
        /// 대기 팝업을 연다. remainingProvider는 인스턴스의 남은 초를 돌려주며, 항목이 없거나 만료되면 0 이하를 준다.
        /// 표시를 시작했으면 true, 만들 수 없으면 false(호출자가 기존 문구로 대신 안내한다).
        /// </summary>
        bool ShowFacilityCooldown(string buildingId, string instanceId, Func<string, double> remainingProvider);

        /// <summary>대기 팝업을 퇴장시킨다. 이미 퇴장 중이거나 숨김이면 아무 일도 하지 않는다.</summary>
        void HideFacilityCooldown();
    }
}
