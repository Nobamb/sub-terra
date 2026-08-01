namespace SubTerra.Shared
{
    /// <summary>채굴 완료 시 전력과 광물 보상을 함께 확정하는 결과.</summary>
    public enum MiningCommitStatus
    {
        Success = 0,
        InsufficientEnergy = 1,
        InventoryFull = 2,
        InvalidReward = 3,
        DependencyMissing = 4
    }

    public readonly struct MiningCommitResult
    {
        public MiningCommitStatus Status { get; }
        public bool Succeeded => Status == MiningCommitStatus.Success;

        public MiningCommitResult(MiningCommitStatus status)
        {
            Status = status;
        }

        public static MiningCommitResult Success()
        {
            return new MiningCommitResult(MiningCommitStatus.Success);
        }
    }

    /// <summary>
    /// Gameplay이 App의 전력·Inventory 구현을 모르고 채굴 비용과 보상을 원자적으로 커밋하는 경계.
    /// 시작/진행 중에는 CanAffordEnergy만 조회하고, 취소 시에는 비용을 차감하지 않는다.
    /// </summary>
    public interface IMiningTransaction
    {
        bool CanAffordEnergy(int energyCost);

        MiningCommitResult TryCommitMining(
            string mineralId,
            int quantity,
            int energyCost);
    }
}
