using System;
using SubTerra.App.State;

namespace SubTerra.App.Run
{
    public enum EmergencyEscapePaymentFailure
    {
        None = 0,
        InvalidState = 1,
        InsufficientGold = 2,
        InsufficientEnergy = 3
    }

    public readonly struct EmergencyEscapeCost
    {
        public int Gold { get; }
        public int Energy { get; }

        public EmergencyEscapeCost(int gold, int energy)
        {
            Gold = gold;
            Energy = energy;
        }
    }

    /// <summary>긴급 탈출 비용을 전량 검증한 뒤 GameState에 한 번만 반영한다.</summary>
    public sealed class EmergencyEscapeService
    {
        public const int GoldCost = 100;
        public const double MaximumEnergyCostRatio = 0.1d;

        private readonly GameState gameState;

        public EmergencyEscapeService(GameState state)
        {
            gameState = state;
        }

        public EmergencyEscapeCost CurrentCost => new(
            GoldCost,
            CalculateEnergyCost(gameState?.Player?.MaxEnergy ?? 0));

        public bool TrySpend(
            out EmergencyEscapeCost cost,
            out EmergencyEscapePaymentFailure failure)
        {
            cost = CurrentCost;
            if (!GameState.IsComplete(gameState))
            {
                failure = EmergencyEscapePaymentFailure.InvalidState;
                return false;
            }

            if (gameState.Player.Gold < cost.Gold)
            {
                failure = EmergencyEscapePaymentFailure.InsufficientGold;
                return false;
            }

            if (gameState.Player.Energy < cost.Energy)
            {
                failure = EmergencyEscapePaymentFailure.InsufficientEnergy;
                return false;
            }

            gameState.SetGold(gameState.Player.Gold - cost.Gold);
            gameState.SetCurrentEnergy(gameState.Player.Energy - cost.Energy);
            failure = EmergencyEscapePaymentFailure.None;
            return true;
        }

        public static int CalculateEnergyCost(int maximumEnergy)
        {
            return maximumEnergy <= 0
                ? 0
                : (int)Math.Ceiling(maximumEnergy * MaximumEnergyCostRatio);
        }
    }
}
