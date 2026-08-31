using System;
using System.Collections.Generic;
using SubTerra.App.Inventory;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Run
{
    public sealed class RunFailureResult
    {
        public RunFailureInputDto Input { get; }
        public RunReturnTarget ReturnTarget { get; }
        public CargoLossPlan CargoLoss { get; }
        public PlayerRescueResultDto Rescue { get; }

        public RunFailureResult(
            RunFailureInputDto input,
            RunReturnTarget returnTarget,
            CargoLossPlan cargoLoss,
            PlayerRescueResultDto rescue)
        {
            Input = input;
            ReturnTarget = returnTarget;
            CargoLoss = cargoLoss;
            Rescue = rescue;
        }
    }

    /// <summary>
    /// 원인과 무관하게 화물 손실·복귀 결정을 한 번만 커밋하는 Run 실패 Orchestrator.
    /// 월드 시설은 건드리지 않고 Inventory와 Run 전이만 소유한다.
    /// </summary>
    public sealed class RunFailureService
    {
        private readonly GameState gameState;
        private readonly InventoryService inventory;
        private readonly IUpgradeEffectProvider upgradeEffects;
        private readonly float baseLossRatio;
        private string activeToken = string.Empty;
        private string lastCompletedToken = string.Empty;

        public bool IsHandling => !string.IsNullOrEmpty(activeToken);

        public RunFailureService(
            GameState state,
            InventoryService inventoryService,
            IUpgradeEffectProvider effects,
            float requestedBaseLossRatio)
        {
            gameState = state;
            inventory = inventoryService;
            upgradeEffects = effects;
            baseLossRatio = Clamp(requestedBaseLossRatio, 0.3f, 0.5f);
        }

        public bool TryBegin(
            RunFailureInputDto input,
            OutpostStatusDto outpost,
            out RunFailureResult result)
        {
            result = null;
            if (!IsValid(input)
                || !GameState.IsComplete(gameState)
                || inventory == null
                || gameState.Run.LifecyclePhase != RunLifecyclePhase.Active
                || IsHandling
                || string.Equals(lastCompletedToken, input.failureToken, StringComparison.Ordinal))
            {
                return false;
            }

            var basePreservation = 1f - baseLossRatio;
            var preservation = upgradeEffects != null
                ? upgradeEffects.GetDroneRescuePreservation(basePreservation)
                : basePreservation;
            preservation = Clamp(preservation, 0f, 1f);
            var loss = CargoLossCalculator.Calculate(inventory.GetSnapshot(), preservation);
            var mutation = inventory.TryReduceMany(loss.Reductions);
            if (mutation.Status != InventoryMutationStatus.Success)
            {
                return false;
            }

            var target = RescueCoordinator.ResolveReturnTarget(outpost);
            activeToken = input.failureToken;
            gameState.SetRunLifecyclePhase(RunLifecyclePhase.Returning);
            var rescue = BuildRescue(input, target, loss);
            result = new RunFailureResult(input, target, loss, rescue);
            return true;
        }

        public bool Complete(string failureToken, bool remainInMine)
        {
            if (string.IsNullOrWhiteSpace(failureToken)
                || !string.Equals(activeToken, failureToken, StringComparison.Ordinal)
                || !GameState.IsComplete(gameState))
            {
                return false;
            }

            gameState.SetRunLifecyclePhase(
                remainInMine ? RunLifecyclePhase.Active : RunLifecyclePhase.Completed);
            lastCompletedToken = activeToken;
            activeToken = string.Empty;
            return true;
        }

        private static PlayerRescueResultDto BuildRescue(
            RunFailureInputDto input,
            RunReturnTarget target,
            CargoLossPlan loss)
        {
            var result = new PlayerRescueResultDto
            {
                failureToken = input.failureToken,
                cause = input.cause,
                returnTargetId = target.CheckpointId,
                returnX = target.X,
                returnY = target.Y,
                usedCheckpoint = target.Kind == RunReturnTargetKind.OutpostCheckpoint,
                preservationRatio = loss.PreservationRatio,
                lostWeight = loss.LostWeight,
                lostValue = loss.LostValue,
                lostCargo = new List<CargoLossEntryDto>()
            };

            for (var i = 0; i < loss.Entries.Count; i++)
            {
                result.lostCargo.Add(loss.Entries[i]);
            }

            return result;
        }

        private static bool IsValid(RunFailureInputDto input)
        {
            return input != null
                && !string.IsNullOrWhiteSpace(input.failureToken)
                && input.cause != RunFailureCause.Unknown
                // 전력 0은 Prompt-B 81의 선택형 긴급 구출 경로가 소유한다.
                && input.cause != RunFailureCause.PowerDepleted;
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (float.IsNaN(value)) return minimum;
            if (value <= minimum) return minimum;
            return value >= maximum ? maximum : value;
        }
    }
}
