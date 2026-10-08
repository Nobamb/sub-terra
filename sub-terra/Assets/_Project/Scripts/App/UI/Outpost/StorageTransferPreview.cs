using System.Collections.Generic;
using System.Globalization;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>보관함 팝업 목록 한 줄(표시 전용).</summary>
    public readonly struct StorageCargoLine
    {
        public string MineralId { get; }
        public string DisplayName { get; }
        public int Quantity { get; }
        public float Weight { get; }

        public StorageCargoLine(string mineralId, string displayName, int quantity, float weight)
        {
            MineralId = mineralId ?? string.Empty;
            DisplayName = string.IsNullOrEmpty(displayName) ? MineralId : displayName;
            Quantity = quantity < 0 ? 0 : quantity;
            Weight = weight < 0f ? 0f : weight;
        }
    }

    public readonly struct StorageCargoTotals
    {
        public int Kinds { get; }
        public int Quantity { get; }
        public float Weight { get; }

        public StorageCargoTotals(int kinds, int quantity, float weight)
        {
            Kinds = kinds;
            Quantity = quantity;
            Weight = weight;
        }
    }

    /// <summary>
    /// 보관함 팝업의 순수 표시 계산. 실제 이동량 판정은 OutpostService(ClampToAvailable)가 하며,
    /// 여기서는 같은 규칙으로 미리보기만 만든다.
    /// </summary>
    public static class StorageTransferPreview
    {
        public static List<StorageCargoLine> Lines(InventorySnapshot snapshot)
        {
            var lines = new List<StorageCargoLine>();
            if (snapshot?.Stacks == null)
            {
                return lines;
            }

            for (var i = 0; i < snapshot.Stacks.Count; i++)
            {
                var stack = snapshot.Stacks[i];
                if (string.IsNullOrEmpty(stack.MineralId) || stack.Quantity <= 0)
                {
                    continue;
                }

                lines.Add(new StorageCargoLine(
                    stack.MineralId,
                    ItemDisplayNames.PreferDisplay(stack.MineralId, stack.DisplayName),
                    stack.Quantity,
                    stack.UnitWeight * stack.Quantity));
            }

            return lines;
        }

        public static StorageCargoTotals Totals(InventorySnapshot snapshot)
        {
            var lines = Lines(snapshot);
            var quantity = 0;
            var weight = 0f;
            for (var i = 0; i < lines.Count; i++)
            {
                quantity += lines[i].Quantity;
                weight += lines[i].Weight;
            }

            return new StorageCargoTotals(lines.Count, quantity, weight);
        }

        /// <summary>수량 입력 범위: 0 ~ (보관 시 보유량, 꺼내기 시 보관량 중 큰 값).</summary>
        public static int ClampInput(int requested, int owned, int stored)
        {
            var max = owned > stored ? owned : stored;
            if (requested <= 0 || max <= 0)
            {
                return 0;
            }

            return requested < max ? requested : max;
        }

        /// <summary>요청량으로 보관할 때 실제로 옮겨질 수량(요청이 더 많으면 보유 전부).</summary>
        public static int DepositAmount(int quantity, int owned)
        {
            return OutpostTransferQuantity.ClampToAvailable(quantity, owned);
        }

        /// <summary>요청량으로 꺼낼 때 실제로 옮겨질 수량(요청이 더 많으면 보관 전부). 무게 검사는 Service가 한다.</summary>
        public static int WithdrawAmount(int quantity, int stored)
        {
            return OutpostTransferQuantity.ClampToAvailable(quantity, stored);
        }

        /// <summary>등장 연출에 튀어나올 광물 ID(화물 → 보관 순, 중복 없이 최대 max개).</summary>
        public static List<string> PopIds(InventorySnapshot playerCargo, InventorySnapshot storage, int max)
        {
            var ids = new List<string>();
            Collect(playerCargo, ids, max);
            Collect(storage, ids, max);
            return ids;
        }

        public static string Weight(float value)
        {
            return (value < 0f ? 0f : value).ToString("0.#", CultureInfo.InvariantCulture) + "kg";
        }

        public static string TotalsText(StorageCargoTotals totals)
        {
            return totals.Kinds + "종 · " + totals.Quantity + "개 · " + Weight(totals.Weight);
        }

        private static void Collect(InventorySnapshot snapshot, List<string> ids, int max)
        {
            if (snapshot?.Stacks == null)
            {
                return;
            }

            for (var i = 0; i < snapshot.Stacks.Count && ids.Count < max; i++)
            {
                var stack = snapshot.Stacks[i];
                if (stack.Quantity > 0 && !string.IsNullOrEmpty(stack.MineralId) && !ids.Contains(stack.MineralId))
                {
                    ids.Add(stack.MineralId);
                }
            }
        }
    }
}
