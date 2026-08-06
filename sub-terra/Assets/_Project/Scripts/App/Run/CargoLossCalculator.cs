using System;
using System.Collections.Generic;
using SubTerra.App.Inventory;
using SubTerra.Shared;

namespace SubTerra.App.Run
{
    public sealed class CargoLossPlan
    {
        private readonly CargoLossEntryDto[] entries;
        private readonly KeyValuePair<string, int>[] reductions;

        public float PreservationRatio { get; }
        public float LostWeight { get; }
        public float LostValue { get; }
        public IReadOnlyList<CargoLossEntryDto> Entries => entries;
        public IReadOnlyList<KeyValuePair<string, int>> Reductions => reductions;

        public CargoLossPlan(
            float preservationRatio,
            float lostWeight,
            float lostValue,
            CargoLossEntryDto[] lossEntries)
        {
            PreservationRatio = Clamp01(preservationRatio);
            LostWeight = Math.Max(0f, lostWeight);
            LostValue = Math.Max(0f, lostValue);
            entries = lossEntries ?? Array.Empty<CargoLossEntryDto>();
            reductions = new KeyValuePair<string, int>[entries.Length];
            for (var i = 0; i < entries.Length; i++)
            {
                reductions[i] = new KeyValuePair<string, int>(
                    entries[i].mineralId,
                    entries[i].quantity);
            }
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value)) return 0f;
            if (value <= 0f) return 0f;
            return value >= 1f ? 1f : value;
        }
    }

    /// <summary>
    /// 동일 Inventory와 보존율에 항상 같은 ID·수량 손실을 반환한다.
    /// 정수 수량 반올림 잔여는 소수부가 큰 스택, 같은 경우 영구 ID 순으로 배분한다.
    /// </summary>
    public static class CargoLossCalculator
    {
        private sealed class Candidate
        {
            public InventoryStackEntry Stack;
            public int Lost;
            public double Remainder;
        }

        public static CargoLossPlan Calculate(
            InventorySnapshot inventory,
            float preservationRatio)
        {
            var preservation = Clamp01(preservationRatio);
            var lossRatio = 1d - preservation;
            if (inventory == null || inventory.Stacks.Count == 0 || lossRatio <= 0d)
            {
                return new CargoLossPlan(preservation, 0f, 0f, Array.Empty<CargoLossEntryDto>());
            }

            var candidates = new List<Candidate>(inventory.Stacks.Count);
            long totalQuantity = 0;
            long assignedQuantity = 0;
            for (var i = 0; i < inventory.Stacks.Count; i++)
            {
                var stack = inventory.Stacks[i];
                if (string.IsNullOrEmpty(stack.MineralId) || stack.Quantity <= 0)
                {
                    continue;
                }

                var exact = stack.Quantity * lossRatio;
                var floor = (int)Math.Floor(exact);
                candidates.Add(new Candidate
                {
                    Stack = stack,
                    Lost = floor,
                    Remainder = exact - floor
                });
                totalQuantity += stack.Quantity;
                assignedQuantity += floor;
            }

            var desired = (long)Math.Round(
                totalQuantity * lossRatio,
                MidpointRounding.AwayFromZero);
            candidates.Sort((left, right) =>
            {
                var remainder = right.Remainder.CompareTo(left.Remainder);
                return remainder != 0
                    ? remainder
                    : string.CompareOrdinal(left.Stack.MineralId, right.Stack.MineralId);
            });

            for (var i = 0; assignedQuantity < desired && i < candidates.Count; i++)
            {
                if (candidates[i].Lost >= candidates[i].Stack.Quantity)
                {
                    continue;
                }

                candidates[i].Lost++;
                assignedQuantity++;
            }

            candidates.Sort((left, right) =>
                string.CompareOrdinal(left.Stack.MineralId, right.Stack.MineralId));
            var entries = new List<CargoLossEntryDto>(candidates.Count);
            double lostWeight = 0d;
            double lostValue = 0d;
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate.Lost <= 0)
                {
                    continue;
                }

                var weight = Math.Max(0d, candidate.Stack.UnitWeight) * candidate.Lost;
                var value = Math.Max(0, candidate.Stack.UnitPrice) * (double)candidate.Lost;
                lostWeight += weight;
                lostValue += value;
                entries.Add(new CargoLossEntryDto
                {
                    mineralId = candidate.Stack.MineralId,
                    quantity = candidate.Lost,
                    lostWeight = ToFiniteFloat(weight),
                    lostValue = ToFiniteFloat(value)
                });
            }

            return new CargoLossPlan(
                preservation,
                ToFiniteFloat(lostWeight),
                ToFiniteFloat(lostValue),
                entries.ToArray());
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value)) return 0f;
            if (value <= 0f) return 0f;
            return value >= 1f ? 1f : value;
        }

        private static float ToFiniteFloat(double value)
        {
            if (value <= 0d || double.IsNaN(value)) return 0f;
            return value >= float.MaxValue ? float.MaxValue : (float)value;
        }
    }
}
