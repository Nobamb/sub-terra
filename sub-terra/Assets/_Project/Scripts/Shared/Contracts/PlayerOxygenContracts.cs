namespace SubTerra.Shared
{
    /// <summary>산소 관련 진행도 효과만 Gameplay 생존 상태에 제공하는 경계.</summary>
    public interface IPlayerOxygenUpgradeProvider
    {
        /// <summary>기본 최대 산소에 업그레이드 가산을 더한 최대량.</summary>
        float GetMaximumOxygen(float baseMaximum);

        /// <summary>최종 소모율에 한 번 더 곱할 감소 비율. 0 이상 1 미만.</summary>
        float GetOxygenDrainReduction();
    }
}
