using SubTerra.App.Core.Data;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Inventory
{
    /// <summary>
    /// 월드 채굴 커밋 결과. 기본분은 전량, 수확 보너스는 남은 자리만큼만 들어간다.
    /// </summary>
    public readonly struct MiningYieldCommitResult
    {
        public MiningCommitStatus Status { get; }
        public int AcceptedBaseQuantity { get; }
        public int AcceptedBonusQuantity { get; }
        public int AcceptedGold { get; }
        public bool Succeeded => Status == MiningCommitStatus.Success;

        public MiningYieldCommitResult(
            MiningCommitStatus status,
            int acceptedBaseQuantity,
            int acceptedBonusQuantity,
            int acceptedGold = 0)
        {
            Status = status;
            AcceptedBaseQuantity = acceptedBaseQuantity < 0 ? 0 : acceptedBaseQuantity;
            AcceptedBonusQuantity = acceptedBonusQuantity < 0 ? 0 : acceptedBonusQuantity;
            AcceptedGold = acceptedGold;
        }

        public MiningCommitResult ToShared()
        {
            return new MiningCommitResult(Status);
        }

        public static MiningYieldCommitResult Fail(MiningCommitStatus status)
        {
            return new MiningYieldCommitResult(status, 0, 0);
        }

        public static MiningYieldCommitResult Success(int acceptedBase, int acceptedBonus, int acceptedGold = 0)
        {
            return new MiningYieldCommitResult(MiningCommitStatus.Success, acceptedBase, acceptedBonus, acceptedGold);
        }
    }

    /// <summary>
    /// 채굴 타일 기본분은 exact, 수확 보너스는 partial.
    /// 퀘스트·판매·보관함 이동은 이 경로를 쓰지 않는다.
    /// </summary>
    public static class MiningYieldCommit
    {
        public static MiningYieldCommitResult TryCommit(
            InventoryService inventory,
            GameState state,
            IUpgradeEffectProvider effects,
            string mineralId,
            int quantity,
            int energyCost,
            int goldGrant = 0)
        {
            if (inventory == null || state == null)
            {
                return MiningYieldCommitResult.Fail(MiningCommitStatus.DependencyMissing);
            }

            var cost = energyCost < 0 ? 0 : energyCost;
            if (state.Player.Energy < cost)
            {
                return MiningYieldCommitResult.Fail(MiningCommitStatus.InsufficientEnergy);
            }

            if (quantity < 0)
            {
                return MiningYieldCommitResult.Fail(MiningCommitStatus.InvalidReward);
            }

            var hasId = !string.IsNullOrEmpty(mineralId);
            if (quantity > 0 && !hasId)
            {
                return MiningYieldCommitResult.Fail(MiningCommitStatus.InvalidReward);
            }

            var acceptedBase = 0;
            var acceptedBonus = 0;
            if (hasId && quantity > 0)
            {
                var reward = inventory.TryAddMineralExact(mineralId, quantity);
                if (reward.Status != InventoryMutationStatus.Success)
                {
                    return MiningYieldCommitResult.Fail(
                        reward.Status == InventoryMutationStatus.CapacityFull
                            ? MiningCommitStatus.InventoryFull
                            : MiningCommitStatus.InvalidReward);
                }

                acceptedBase = reward.AcceptedQuantity;
                var bonus = effects != null ? effects.GetMiningYieldBonus(mineralId) : 0;
                if (bonus > 0)
                {
                    var bonusResult = inventory.TryAddMineral(mineralId, bonus);
                    acceptedBonus = bonusResult.AcceptedQuantity;
                }
            }

            int acceptedGold = System.Math.Min(System.Math.Max(0, goldGrant), int.MaxValue - state.Player.Gold);
            if (acceptedGold > 0) state.AddGold(acceptedGold);
            state.SetCurrentEnergy(state.Player.Energy - cost);
            return MiningYieldCommitResult.Success(acceptedBase, acceptedBonus, acceptedGold);
        }

        public static string FormatHudFeedback(
            string mineralId,
            int acceptedBase,
            int acceptedBonus,
            int acceptedGold = 0)
        {
            string goldText = acceptedGold > 0 ? "골드 +" + acceptedGold + "G" : string.Empty;
            if (string.IsNullOrEmpty(mineralId) || acceptedBase + acceptedBonus <= 0)
            {
                return goldText;
            }

            string suffix = acceptedGold > 0 ? "  " + goldText : string.Empty;
            var name = ItemDisplayNames.Mineral(mineralId);
            if (acceptedBonus > 0)
            {
                return name + " +" + acceptedBase + " (+" + acceptedBonus + " 수확)" + suffix;
            }

            return name + " +" + acceptedBase + suffix;
        }
    }
}
