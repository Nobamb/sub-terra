using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>보관함·정산 패널의 자원 선택 한 줄.</summary>
    public readonly struct OutpostMineralOption
    {
        public string MineralId { get; }
        public string DisplayName { get; }
        public int OwnedQuantity { get; }
        public int StoredQuantity { get; }

        public string Label =>
            DisplayName + "  (보유 " + OwnedQuantity + " / 보관 " + StoredQuantity + ")";

        public OutpostMineralOption(
            string mineralId,
            string displayName,
            int ownedQuantity,
            int storedQuantity)
        {
            MineralId = mineralId ?? string.Empty;
            DisplayName = string.IsNullOrEmpty(displayName) ? MineralId : displayName;
            OwnedQuantity = ownedQuantity < 0 ? 0 : ownedQuantity;
            StoredQuantity = storedQuantity < 0 ? 0 : storedQuantity;
        }
    }

    /// <summary>
    /// 보유·보관·기본 카탈로그 광물을 합치고, 표시 이름 일부로 검색한다.
    /// </summary>
    public static class OutpostMineralPickerFilter
    {
        private static readonly string[] DefaultMineralIds =
        {
            DataIds.Minerals.Copper,
            DataIds.Minerals.Iron,
            DataIds.Minerals.Lithium
        };

        public static IReadOnlyList<OutpostMineralOption> Build(
            InventorySnapshot playerCargo,
            InventorySnapshot storage)
        {
            var owned = new Dictionary<string, InventoryStackEntry>(StringComparer.Ordinal);
            var stored = new Dictionary<string, InventoryStackEntry>(StringComparer.Ordinal);
            Collect(playerCargo, owned);
            Collect(storage, stored);

            var ids = new List<string>(DefaultMineralIds.Length + owned.Count + stored.Count);
            for (var i = 0; i < DefaultMineralIds.Length; i++)
            {
                AddUnique(ids, DefaultMineralIds[i]);
            }

            AddKeys(ids, owned);
            AddKeys(ids, stored);
            ids.Sort(StringComparer.Ordinal);

            var options = new List<OutpostMineralOption>(ids.Count);
            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                owned.TryGetValue(id, out var ownedStack);
                stored.TryGetValue(id, out var storedStack);
                var displayName = ItemDisplayNames.PreferDisplay(
                    id,
                    !string.IsNullOrEmpty(ownedStack.DisplayName)
                        ? ownedStack.DisplayName
                        : storedStack.DisplayName);
                options.Add(new OutpostMineralOption(
                    id,
                    displayName,
                    ownedStack.Quantity,
                    storedStack.Quantity));
            }

            return options;
        }

        public static IReadOnlyList<OutpostMineralOption> Filter(
            IReadOnlyList<OutpostMineralOption> options,
            string query)
        {
            if (options == null || options.Count == 0)
            {
                return Array.Empty<OutpostMineralOption>();
            }

            var needle = query == null ? string.Empty : query.Trim();
            if (needle.Length == 0)
            {
                return options;
            }

            var matches = new List<OutpostMineralOption>(options.Count);
            for (var i = 0; i < options.Count; i++)
            {
                if (Matches(options[i], needle))
                {
                    matches.Add(options[i]);
                }
            }

            return matches;
        }

        private static bool Matches(OutpostMineralOption option, string needle)
        {
            if (ContainsIgnoreCase(option.DisplayName, needle)
                || ContainsIgnoreCase(option.MineralId, needle)
                || ContainsIgnoreCase(option.Label, needle))
            {
                return true;
            }

            var id = option.MineralId;
            var separator = id.LastIndexOf('.');
            return separator >= 0
                && separator + 1 < id.Length
                && ContainsIgnoreCase(id.Substring(separator + 1), needle);
        }

        private static bool ContainsIgnoreCase(string value, string needle)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void Collect(
            InventorySnapshot snapshot,
            Dictionary<string, InventoryStackEntry> target)
        {
            if (snapshot?.Stacks == null)
            {
                return;
            }

            for (var i = 0; i < snapshot.Stacks.Count; i++)
            {
                var stack = snapshot.Stacks[i];
                if (string.IsNullOrEmpty(stack.MineralId) || stack.Quantity <= 0)
                {
                    continue;
                }

                target[stack.MineralId] = stack;
            }
        }

        private static void AddKeys(
            List<string> ids,
            Dictionary<string, InventoryStackEntry> source)
        {
            foreach (var id in source.Keys)
            {
                AddUnique(ids, id);
            }
        }

        private static void AddUnique(List<string> ids, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                {
                    return;
                }
            }

            ids.Add(id);
        }
    }
}
