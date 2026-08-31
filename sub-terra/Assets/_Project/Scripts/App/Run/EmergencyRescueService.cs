using System;
using System.Collections.Generic;
using SubTerra.App.Inventory;
using SubTerra.App.State;

namespace SubTerra.App.Run
{
    public enum EmergencyRescueFailure
    {
        None = 0,
        InvalidState = 1,
        EnergyAvailable = 2,
        InventoryChanged = 3
    }

    public readonly struct EmergencyRescueMineralCost
    {
        public string MineralId { get; }
        public string DisplayName { get; }
        public int Before { get; }
        public int Charged { get; }
        public int After => Math.Max(0, Before - Charged);

        public EmergencyRescueMineralCost(
            string mineralId,
            string displayName,
            int before,
            int charged)
        {
            MineralId = mineralId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Before = Math.Max(0, before);
            Charged = Math.Max(0, charged);
        }
    }

    public sealed class EmergencyRescueCost
    {
        private readonly EmergencyRescueMineralCost[] minerals;

        public int GoldBefore { get; }
        public int GoldCharged { get; }
        public int GoldAfter => Math.Max(0, GoldBefore - GoldCharged);
        public IReadOnlyList<EmergencyRescueMineralCost> Minerals => minerals;
        public bool IsFree => GoldCharged == 0 && minerals.Length == 0;

        public EmergencyRescueCost(
            int goldBefore,
            int goldCharged,
            EmergencyRescueMineralCost[] mineralCosts)
        {
            GoldBefore = Math.Max(0, goldBefore);
            GoldCharged = Math.Max(0, goldCharged);
            minerals = mineralCosts ?? Array.Empty<EmergencyRescueMineralCost>();
        }
    }

    /// <summary>
    /// 전력 고갈 전용 유료 구출 비용을 계산하고 플레이어 미정산 화물만 한 번에 차감한다.
    /// 전진기지 보관함은 InventoryService와 별도 상태이므로 이 경로에서 접근하지 않는다.
    /// </summary>
    public sealed class EmergencyRescueService
    {
        public const int MaximumGoldCost = 250;
        public const int MineralLossPercent = 80;

        private readonly GameState gameState;
        private readonly InventoryService inventory;

        public EmergencyRescueService(GameState state, InventoryService inventoryService)
        {
            gameState = state;
            inventory = inventoryService;
        }

        public bool IsAvailable => GameState.IsComplete(gameState)
            && inventory != null
            && gameState.Player.Energy <= 0
            && gameState.Run.LifecyclePhase == RunLifecyclePhase.Active;

        public EmergencyRescueCost GetCurrentCost()
        {
            int goldBefore = GameState.IsComplete(gameState) ? gameState.Player.Gold : 0;
            int goldCost = Math.Min(MaximumGoldCost, Math.Max(0, goldBefore));
            InventorySnapshot snapshot = inventory != null ? inventory.GetSnapshot() : null;
            if (snapshot == null || snapshot.Stacks.Count == 0)
            {
                return new EmergencyRescueCost(
                    goldBefore,
                    goldCost,
                    Array.Empty<EmergencyRescueMineralCost>());
            }

            var costs = new List<EmergencyRescueMineralCost>(snapshot.Stacks.Count);
            for (var i = 0; i < snapshot.Stacks.Count; i++)
            {
                InventoryStackEntry stack = snapshot.Stacks[i];
                int charged = CalculateMineralLoss(stack.Quantity);
                if (charged <= 0)
                {
                    continue;
                }

                costs.Add(new EmergencyRescueMineralCost(
                    stack.MineralId,
                    stack.DisplayName,
                    stack.Quantity,
                    charged));
            }

            return new EmergencyRescueCost(goldBefore, goldCost, costs.ToArray());
        }

        public bool TryRescue(out EmergencyRescueCost chargedCost, out EmergencyRescueFailure failure)
        {
            chargedCost = GetCurrentCost();
            if (!GameState.IsComplete(gameState) || inventory == null)
            {
                failure = EmergencyRescueFailure.InvalidState;
                return false;
            }

            if (gameState.Player.Energy > 0
                || gameState.Run.LifecyclePhase != RunLifecyclePhase.Active)
            {
                failure = EmergencyRescueFailure.EnergyAvailable;
                return false;
            }

            var reductions = new List<KeyValuePair<string, int>>(chargedCost.Minerals.Count);
            for (var i = 0; i < chargedCost.Minerals.Count; i++)
            {
                EmergencyRescueMineralCost mineral = chargedCost.Minerals[i];
                reductions.Add(new KeyValuePair<string, int>(mineral.MineralId, mineral.Charged));
            }

            // 인벤토리를 전량 사전 검증·차감한 뒤 실패할 수 없는 골드 절대값 변경을 적용한다.
            InventoryMutationResult mutation = inventory.TryReduceMany(reductions);
            if (mutation.Status != InventoryMutationStatus.Success)
            {
                failure = EmergencyRescueFailure.InventoryChanged;
                return false;
            }

            gameState.SetGold(chargedCost.GoldAfter);
            failure = EmergencyRescueFailure.None;
            return true;
        }

        public static int CalculateMineralLoss(int quantity)
        {
            if (quantity <= 0)
            {
                return 0;
            }

            // 80%를 정수로 계산하고 정확히 0.5인 경우 올림한다.
            long scaled = (long)quantity * MineralLossPercent;
            long rounded = (scaled + 50L) / 100L;
            return rounded > int.MaxValue ? int.MaxValue : (int)rounded;
        }
    }
}
