using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.State;

namespace SubTerra.App.Tutorial
{
    /// <summary>
    /// 퀘스트 클리어 보상을 InventoryService와 GameState.AddGold 한 경로로만 지급한다.
    /// UI는 이 서비스 결과만 표시하며 인벤/골드를 직접 바꾸지 않는다.
    /// </summary>
    public sealed class QuestRewardService
    {
        private InventoryService inventory;
        private GameState gameState;

        public string PendingObjectiveId { get; private set; } = string.Empty;
        public int SettledCount { get; private set; }
        public QuestReward PendingReward { get; private set; }

        public bool HasPending => !string.IsNullOrEmpty(PendingObjectiveId);

        public event Action<QuestRewardGrantResult> GrantResolved;
        public event Action<QuestRewardGrantResult> CapacityBlocked;
        public event Action<QuestRewardGrantResult> ClaimOffered;

        public void Bind(InventoryService inventoryService, GameState state)
        {
            inventory = inventoryService;
            gameState = state;
            PullFromProgress();
        }

        public void Unbind()
        {
            inventory = null;
            gameState = null;
            PendingObjectiveId = string.Empty;
            PendingReward = QuestReward.None;
            SettledCount = 0;
        }

        /// <summary>
        /// 세이브 복원·목표 전진 후 미정산 보상을 팝업 확인 대기로 올린다.
        /// 실제 지급은 팝업을 닫을 때 ClaimPending에서만 수행한다.
        /// </summary>
        public QuestRewardGrantResult SyncFromProgress()
        {
            PullFromProgress();
            if (gameState?.Progress == null)
            {
                return new QuestRewardGrantResult(
                    QuestRewardGrantStatus.NothingPending,
                    string.Empty,
                    QuestReward.None,
                    "state-missing");
            }

            var completed = gameState.Progress.CompletedObjectives;
            if (SettledCount > completed)
            {
                SettledCount = completed;
                PushToProgress();
            }

            if (HasPending)
            {
                return OfferClaim(PendingObjectiveId, PendingReward, "awaiting-claim");
            }

            return OfferNextIfNeeded();
        }

        /// <summary>클리어 팝업을 닫은 뒤에만 보상을 지급하거나 화물 부족 선택을 연다.</summary>
        public QuestRewardGrantResult ClaimPending()
        {
            if (!HasPending)
            {
                return SyncFromProgress();
            }

            return TryGrant(PendingObjectiveId);
        }

        public bool CanFitPending()
        {
            if (!HasPending || inventory == null)
            {
                return true;
            }

            return inventory.CanFitExact(PendingReward.MineralEntries());
        }

        public QuestRewardGrantResult RetryPending()
        {
            if (!HasPending)
            {
                return SyncFromProgress();
            }

            return TryGrant(PendingObjectiveId);
        }

        public QuestRewardGrantResult ForfeitPending()
        {
            if (!HasPending)
            {
                return new QuestRewardGrantResult(
                    QuestRewardGrantStatus.NothingPending,
                    string.Empty,
                    QuestReward.None,
                    "no-pending");
            }

            var forfeitedId = PendingObjectiveId;
            var reward = PendingReward;
            ClearPending();
            SettledCount++;
            PushToProgress();
            var result = new QuestRewardGrantResult(
                QuestRewardGrantStatus.Forfeited,
                forfeitedId,
                reward,
                "forfeited");
            GrantResolved?.Invoke(result);

            var followUp = OfferNextIfNeeded();
            return followUp.IsAwaitingClaim || followUp.NeedsPlayerChoice ? followUp : result;
        }

        public InventoryMutationResult Dump(string mineralId, int quantity)
        {
            if (inventory == null)
            {
                return InventoryMutationResult.Invalid(
                    InventoryMutationStatus.CatalogMissing,
                    mineralId,
                    quantity,
                    "inventory-missing");
            }

            return inventory.TryReduceMineral(mineralId, quantity);
        }

        public IReadOnlyList<QuestDumpRow> GetDumpRows()
        {
            if (inventory == null)
            {
                return Array.Empty<QuestDumpRow>();
            }

            var snapshot = inventory.GetSnapshot();
            var rows = new List<QuestDumpRow>(snapshot.Stacks.Count);
            for (var i = 0; i < snapshot.Stacks.Count; i++)
            {
                var stack = snapshot.Stacks[i];
                rows.Add(new QuestDumpRow(
                    stack.MineralId,
                    stack.DisplayName,
                    stack.Quantity,
                    stack.UnitWeight));
            }

            return rows;
        }

        public string FormatDumpSummary()
        {
            if (inventory == null)
            {
                return string.Empty;
            }

            var needed = PendingReward.ComputeMineralWeight(inventory.CatalogLookup);
            return "현재 화물 "
                + inventory.CurrentWeight.ToString("0.#")
                + " / "
                + inventory.MaxCapacity.ToString("0.#")
                + "  ·  보상 중량 "
                + needed.ToString("0.#");
        }

        public static string DisplayNameOf(string mineralId)
        {
            if (mineralId == DataIds.Minerals.Copper)
            {
                return "구리";
            }

            if (mineralId == DataIds.Minerals.Iron)
            {
                return "철";
            }

            if (mineralId == DataIds.Minerals.Lithium)
            {
                return "리튬";
            }

            return mineralId ?? string.Empty;
        }

        private QuestRewardGrantResult TryGrant(string objectiveId)
        {
            if (!DemoObjectiveCatalog.TryGet(objectiveId, out var definition))
            {
                SettledCount++;
                ClearPending();
                PushToProgress();
                var skipped = new QuestRewardGrantResult(
                    QuestRewardGrantStatus.AlreadySettled,
                    objectiveId,
                    QuestReward.None,
                    "unknown-objective");
                var followUp = OfferNextIfNeeded();
                return followUp.IsAwaitingClaim ? followUp : skipped;
            }

            var reward = definition.Reward;
            if (reward.IsEmpty)
            {
                return Settle(objectiveId, reward, "empty-reward");
            }

            if (reward.HasMinerals)
            {
                if (inventory == null)
                {
                    return Block(objectiveId, reward, "inventory-missing");
                }

                var entries = reward.MineralEntries();
                if (!inventory.CanFitExact(entries))
                {
                    return Block(objectiveId, reward, "capacity-full");
                }

                var added = inventory.TryAddManyExact(entries);
                if (added.Status != InventoryMutationStatus.Success)
                {
                    return Block(objectiveId, reward, added.Diagnostic);
                }
            }

            if (reward.HasGold && gameState != null)
            {
                gameState.AddGold(reward.Gold);
            }

            return Settle(objectiveId, reward, "granted");
        }

        private QuestRewardGrantResult Settle(string objectiveId, QuestReward reward, string message)
        {
            ClearPending();
            SettledCount++;
            PushToProgress();
            var result = new QuestRewardGrantResult(
                QuestRewardGrantStatus.Granted,
                objectiveId,
                reward,
                message);
            GrantResolved?.Invoke(result);
            var followUp = OfferNextIfNeeded();
            return followUp.IsAwaitingClaim ? followUp : result;
        }

        private QuestRewardGrantResult OfferNextIfNeeded()
        {
            if (gameState?.Progress == null)
            {
                return new QuestRewardGrantResult(
                    QuestRewardGrantStatus.NothingPending,
                    string.Empty,
                    QuestReward.None,
                    "state-missing");
            }

            var completed = gameState.Progress.CompletedObjectives;
            while (SettledCount < completed)
            {
                if (SettledCount < 0 || SettledCount >= DemoObjectiveIds.Ordered.Length)
                {
                    break;
                }

                var objectiveId = DemoObjectiveIds.Ordered[SettledCount];
                if (!DemoObjectiveCatalog.TryGet(objectiveId, out var definition))
                {
                    SettledCount++;
                    ClearPending();
                    PushToProgress();
                    continue;
                }

                PendingObjectiveId = objectiveId;
                PendingReward = definition.Reward;
                PushToProgress();
                return OfferClaim(objectiveId, definition.Reward, "awaiting-claim");
            }

            if (HasPending)
            {
                ClearPending();
                PushToProgress();
            }

            return new QuestRewardGrantResult(
                QuestRewardGrantStatus.NothingPending,
                string.Empty,
                QuestReward.None,
                "caught-up");
        }

        private QuestRewardGrantResult OfferClaim(string objectiveId, QuestReward reward, string message)
        {
            var result = new QuestRewardGrantResult(
                QuestRewardGrantStatus.AwaitingClaim,
                objectiveId,
                reward,
                message);
            ClaimOffered?.Invoke(result);
            return result;
        }

        private QuestRewardGrantResult Block(string objectiveId, QuestReward reward, string message)
        {
            PendingObjectiveId = objectiveId;
            PendingReward = reward;
            PushToProgress();
            var result = new QuestRewardGrantResult(
                QuestRewardGrantStatus.NeedsCapacity,
                objectiveId,
                reward,
                message);
            CapacityBlocked?.Invoke(result);
            return result;
        }

        private void ClearPending()
        {
            PendingObjectiveId = string.Empty;
            PendingReward = QuestReward.None;
        }

        private void PullFromProgress()
        {
            var progress = gameState?.Progress;
            if (progress == null)
            {
                return;
            }

            SettledCount = progress.QuestRewardSettledCount;
            PendingObjectiveId = progress.PendingQuestRewardId ?? string.Empty;
            PendingReward = DemoObjectiveCatalog.TryGet(PendingObjectiveId, out var definition)
                ? definition.Reward
                : QuestReward.None;
        }

        private void PushToProgress()
        {
            gameState?.SetQuestRewardSettlement(PendingObjectiveId, SettledCount);
        }
    }
}
