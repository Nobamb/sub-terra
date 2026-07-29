namespace SubTerra.Shared
{
    /// <summary>
    /// Gameplay(A)이 App(B)의 진행도 구현을 알지 않고 현재 업그레이드 효과를 읽는 경계.
    /// 기본 수치는 Gameplay 소유자가 넘기고 Provider는 데이터의 현재 레벨 보너스만 적용한다.
    /// </summary>
    public interface IUpgradeEffectProvider
    {
        float GetDrillSpeedMultiplier();
        float GetEnergyEfficiencyMultiplier();
        int GetMaximumEnergy(int baseMaximum);
        float GetMaximumCargoWeight(float baseMaximum);
        float GetDroneScanRadius(float baseRadius);
        float GetDroneRescuePreservation(float basePreservation);
        float GetGasResistance();
    }
}
