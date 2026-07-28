using System;
using System.Collections.Generic;

namespace SubTerra.App.Inventory
{
    /// <summary>
    /// 인벤토리 런타임 상태. 광물별 수량(영구 ID → 0 이상 정수)과 합산 캐시를 소유한다.
    /// 수량 0 항목은 저장하지 않는다. mutable Dictionary는 외부에 노출하지 않는다.
    /// </summary>
    public sealed class InventoryState
    {
        /// <summary>MVP 기본 최대 화물 중량. 업그레이드로 늘리는 것은 후속 단계.</summary>
        public const float DefaultMaxCapacity = 50f;

        private readonly Dictionary<string, int> quantities = new Dictionary<string, int>();

        public float MaxCapacity { get; private set; }
        public float CurrentWeight { get; private set; }
        public float UnsettledValue { get; private set; }

        public InventoryState(float maxCapacity = DefaultMaxCapacity)
        {
            MaxCapacity = maxCapacity < 0f ? 0f : maxCapacity;
            CurrentWeight = 0f;
            UnsettledValue = 0f;
        }

        public int GetQuantity(string mineralId)
        {
            if (string.IsNullOrEmpty(mineralId))
            {
                return 0;
            }

            return quantities.TryGetValue(mineralId, out var qty) ? qty : 0;
        }

        /// <summary>내부 스택 읽기 전용 뷰. 호출자가 Dictionary를 수정하면 안 된다.</summary>
        internal IReadOnlyDictionary<string, int> Quantities => quantities;

        public void SetMaxCapacity(float maxCapacity)
        {
            MaxCapacity = maxCapacity < 0f ? 0f : maxCapacity;
        }

        /// <summary>검증을 통과한 뒤에만 호출. 수량 0이면 항목을 제거한다.</summary>
        internal void SetQuantity(string mineralId, int quantity)
        {
            if (string.IsNullOrEmpty(mineralId))
            {
                return;
            }

            if (quantity <= 0)
            {
                quantities.Remove(mineralId);
                return;
            }

            quantities[mineralId] = quantity;
        }

        internal void ApplyAggregates(float weight, float unsettledValue)
        {
            CurrentWeight = weight < 0f ? 0f : weight;
            UnsettledValue = unsettledValue < 0f ? 0f : unsettledValue;
        }

        /// <summary>UI·테스트용 불변 스냅샷. 스택 배열 복사본을 담는다.</summary>
        public InventorySnapshot CreateSnapshot(IMineralCatalogLookup catalog)
        {
            var entries = new List<InventoryStackEntry>(quantities.Count);
            foreach (var pair in quantities)
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                var display = pair.Key;
                var unitWeight = 0f;
                var unitPrice = 0;
                if (catalog != null && catalog.TryGetMineral(pair.Key, out var info))
                {
                    display = string.IsNullOrEmpty(info.DisplayName) ? pair.Key : info.DisplayName;
                    unitWeight = info.UnitWeight;
                    unitPrice = info.UnitPrice;
                }

                entries.Add(new InventoryStackEntry(pair.Key, display, pair.Value, unitWeight, unitPrice));
            }

            // 표시 안정성을 위해 ID 순 정렬
            entries.Sort((a, b) => string.CompareOrdinal(a.MineralId, b.MineralId));
            return new InventorySnapshot(
                CurrentWeight,
                MaxCapacity,
                UnsettledValue,
                entries.ToArray());
        }

        /// <summary>실패 원자성 검증용 동일 상태 비교.</summary>
        public InventoryFingerprint CaptureFingerprint()
        {
            return new InventoryFingerprint(quantities, CurrentWeight, UnsettledValue, MaxCapacity);
        }
    }

    /// <summary>스냅샷 동등 비교용 지문. 테스트에서 실패 전후 State 불변을 확인한다.</summary>
    public readonly struct InventoryFingerprint : IEquatable<InventoryFingerprint>
    {
        private readonly string encoded;
        public float CurrentWeight { get; }
        public float UnsettledValue { get; }
        public float MaxCapacity { get; }

        public InventoryFingerprint(
            IReadOnlyDictionary<string, int> quantities,
            float currentWeight,
            float unsettledValue,
            float maxCapacity)
        {
            CurrentWeight = currentWeight;
            UnsettledValue = unsettledValue;
            MaxCapacity = maxCapacity;

            if (quantities == null || quantities.Count == 0)
            {
                encoded = string.Empty;
                return;
            }

            var keys = new List<string>(quantities.Keys);
            keys.Sort(StringComparer.Ordinal);
            var parts = new List<string>(keys.Count);
            for (var i = 0; i < keys.Count; i++)
            {
                parts.Add(keys[i] + "=" + quantities[keys[i]]);
            }

            encoded = string.Join(";", parts);
        }

        public bool Equals(InventoryFingerprint other)
        {
            return encoded == other.encoded
                && Math.Abs(CurrentWeight - other.CurrentWeight) < 0.0001f
                && Math.Abs(UnsettledValue - other.UnsettledValue) < 0.0001f
                && Math.Abs(MaxCapacity - other.MaxCapacity) < 0.0001f;
        }

        public override bool Equals(object obj)
        {
            return obj is InventoryFingerprint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (encoded != null ? encoded.GetHashCode() : 0)
                ^ CurrentWeight.GetHashCode()
                ^ UnsettledValue.GetHashCode();
        }
    }
}
