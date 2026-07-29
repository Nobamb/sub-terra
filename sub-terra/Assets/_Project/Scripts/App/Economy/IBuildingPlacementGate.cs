namespace SubTerra.App.Economy
{
    /// <summary>
    /// A측 Runtime Prefab/배치 성공 여부를 추상화한 게이트.
    /// 담당자 B는 Prefab 내부를 수정하지 않고, 성공/실패 bool만 수신한다.
    /// 통합 Scene 실배선 전 테스트 대역으로도 사용한다.
    /// </summary>
    public interface IBuildingPlacementGate
    {
        /// <summary>
        /// 위치 검증과 Runtime Prefab 생성을 시도한다.
        /// true일 때만 호출측이 TrySpend를 진행해야 한다.
        /// </summary>
        bool TryPlace(string buildingId);
    }
}
