using System.Collections.Generic;
using System.Text;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;

namespace SubTerra.App.Tutorial
{
    /// <summary>데모 퀘스트 1개의 클리어 보상. 카탈로그 고정값이며 런타임 State가 아니다.</summary>
    public readonly struct QuestReward
    {
        public int Copper { get; }
        public int Iron { get; }
        public int Lithium { get; }
        public int Gold { get; }

        public QuestReward(int copper, int iron, int lithium, int gold)
        {
            Copper = copper < 0 ? 0 : copper;
            Iron = iron < 0 ? 0 : iron;
            Lithium = lithium < 0 ? 0 : lithium;
            Gold = gold < 0 ? 0 : gold;
        }

        public static QuestReward None => default;

        public bool HasMinerals => Copper > 0 || Iron > 0 || Lithium > 0;
        public bool HasGold => Gold > 0;
        public bool IsEmpty => !HasMinerals && !HasGold;

        public IReadOnlyList<KeyValuePair<string, int>> MineralEntries()
        {
            var entries = new List<KeyValuePair<string, int>>(3);
            if (Copper > 0)
            {
                entries.Add(new KeyValuePair<string, int>(DataIds.Minerals.Copper, Copper));
            }

            if (Iron > 0)
            {
                entries.Add(new KeyValuePair<string, int>(DataIds.Minerals.Iron, Iron));
            }

            if (Lithium > 0)
            {
                entries.Add(new KeyValuePair<string, int>(DataIds.Minerals.Lithium, Lithium));
            }

            return entries;
        }

        public float ComputeMineralWeight(IMineralCatalogLookup catalog)
        {
            if (!HasMinerals || catalog == null)
            {
                return 0f;
            }

            var quantities = new Dictionary<string, int>(3);
            if (Copper > 0)
            {
                quantities[DataIds.Minerals.Copper] = Copper;
            }

            if (Iron > 0)
            {
                quantities[DataIds.Minerals.Iron] = Iron;
            }

            if (Lithium > 0)
            {
                quantities[DataIds.Minerals.Lithium] = Lithium;
            }

            return InventoryCalculator.ComputeTotalWeight(quantities, catalog);
        }

        public string FormatKorean()
        {
            if (IsEmpty)
            {
                return "없음";
            }

            var builder = new StringBuilder();
            Append(builder, "구리", Copper);
            Append(builder, "철", Iron);
            Append(builder, "리튬", Lithium);
            Append(builder, "골드", Gold);
            return builder.ToString();
        }

        private static void Append(StringBuilder builder, string label, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(label).Append(' ').Append(amount);
        }
    }

    public enum QuestRewardGrantStatus
    {
        Granted = 0,
        NeedsCapacity = 1,
        Forfeited = 2,
        AlreadySettled = 3,
        NothingPending = 4
    }

    public readonly struct QuestRewardGrantResult
    {
        public QuestRewardGrantStatus Status { get; }
        public string ObjectiveId { get; }
        public QuestReward Reward { get; }
        public string Message { get; }

        public QuestRewardGrantResult(
            QuestRewardGrantStatus status,
            string objectiveId,
            QuestReward reward,
            string message)
        {
            Status = status;
            ObjectiveId = objectiveId ?? string.Empty;
            Reward = reward;
            Message = message ?? string.Empty;
        }

        public bool DidGrant => Status == QuestRewardGrantStatus.Granted;
        public bool NeedsPlayerChoice => Status == QuestRewardGrantStatus.NeedsCapacity;
    }

    public readonly struct QuestDumpRow
    {
        public string MineralId { get; }
        public string DisplayName { get; }
        public int Quantity { get; }
        public float UnitWeight { get; }

        public QuestDumpRow(string mineralId, string displayName, int quantity, float unitWeight)
        {
            MineralId = mineralId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Quantity = quantity < 0 ? 0 : quantity;
            UnitWeight = unitWeight < 0f ? 0f : unitWeight;
        }
    }
}
