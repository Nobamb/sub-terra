using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.UI.Sell
{
    /// <summary>
    /// 보너스 반올림 규칙. 화면마다 기존 거래 규칙이 달라 공통 UI에 데이터로 넘긴다.
    /// PerLine: 지상 판매(광물별 TrySellMineral과 같은 규칙), OnTotal: 정산 콘솔(기존 화물 전체 정산과 같은 규칙).
    /// </summary>
    public enum ResourceSellBonusMode
    {
        PerLine = 0,
        OnTotal = 1
    }

    /// <summary>판매 창 한 행의 원천 데이터. 수량·단가·무게는 실제 인벤토리 스냅샷 값이다.</summary>
    public readonly struct ResourceSellLine
    {
        public string ItemId { get; }
        public string DisplayName { get; }
        public int Owned { get; }
        public int UnitPrice { get; }
        public float UnitWeight { get; }
        public Sprite Icon { get; }

        /// <summary>이 화면에서 판매할 수 있는지. false면 행은 보이지만 수량 조절이 막힌다.</summary>
        public bool CanSell { get; }

        /// <summary>'최대 선택' 일괄 대상인지. 희귀 품목처럼 개별 지정만 허용하면 false.</summary>
        public bool InBulk { get; }

        /// <summary>행 이름 아래에 보이는 제한 안내. 없으면 빈 문자열.</summary>
        public string Note { get; }

        public int MaxSelectable => CanSell ? Owned : 0;

        public ResourceSellLine(
            string itemId,
            string displayName,
            int owned,
            int unitPrice,
            float unitWeight,
            Sprite icon,
            bool canSell = true,
            bool inBulk = true,
            string note = null)
        {
            ItemId = itemId ?? string.Empty;
            DisplayName = string.IsNullOrEmpty(displayName) ? ItemId : displayName;
            Owned = owned < 0 ? 0 : owned;
            UnitPrice = unitPrice < 0 ? 0 : unitPrice;
            UnitWeight = unitWeight < 0f ? 0f : unitWeight;
            Icon = icon;
            CanSell = canSell;
            InBulk = canSell && inBulk;
            Note = note ?? string.Empty;
        }
    }

    /// <summary>판매 창이 한 번에 읽는 상태. 화면(지상/정산 콘솔)마다 어댑터가 만든다.</summary>
    public sealed class ResourceSellSnapshot
    {
        public static readonly ResourceSellSnapshot Empty =
            new ResourceSellSnapshot(Array.Empty<ResourceSellLine>(), 0, 0f, 0f, 0, ResourceSellBonusMode.PerLine);

        public IReadOnlyList<ResourceSellLine> Lines { get; }
        public int Gold { get; }
        public float CargoWeight { get; }
        public float CargoCapacity { get; }
        public int BonusPercent { get; }
        public ResourceSellBonusMode BonusMode { get; }

        public ResourceSellSnapshot(
            IReadOnlyList<ResourceSellLine> lines,
            int gold,
            float cargoWeight,
            float cargoCapacity,
            int bonusPercent,
            ResourceSellBonusMode bonusMode)
        {
            Lines = lines ?? Array.Empty<ResourceSellLine>();
            Gold = gold < 0 ? 0 : gold;
            CargoWeight = cargoWeight < 0f ? 0f : cargoWeight;
            CargoCapacity = cargoCapacity < 0f ? 0f : cargoCapacity;
            BonusPercent = bonusPercent;
            BonusMode = bonusMode;
        }

        public bool TryFind(string itemId, out ResourceSellLine line)
        {
            for (var i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].ItemId == itemId)
                {
                    line = Lines[i];
                    return true;
                }
            }

            line = default;
            return false;
        }

        /// <summary>표시가 바뀌는 값만 비교한다(아이콘·이름 제외). 같으면 다시 그릴 필요가 없다.</summary>
        public bool SameValues(ResourceSellSnapshot other)
        {
            if (other == null
                || other.Gold != Gold
                || other.BonusPercent != BonusPercent
                || other.BonusMode != BonusMode
                || Mathf.Abs(other.CargoWeight - CargoWeight) > 0.0001f
                || Mathf.Abs(other.CargoCapacity - CargoCapacity) > 0.0001f
                || other.Lines.Count != Lines.Count)
            {
                return false;
            }

            for (var i = 0; i < Lines.Count; i++)
            {
                var a = Lines[i];
                var b = other.Lines[i];
                if (a.ItemId != b.ItemId
                    || a.Owned != b.Owned
                    || a.UnitPrice != b.UnitPrice
                    || a.CanSell != b.CanSell
                    || a.InBulk != b.InBulk)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>실제 거래 결과. 성공 여부·지급 골드·사용자 문구만 담는다.</summary>
    public readonly struct ResourceSellCommitResult
    {
        public bool Success { get; }
        public int GoldDelta { get; }
        public string Message { get; }

        public ResourceSellCommitResult(bool success, int goldDelta, string message)
        {
            Success = success;
            GoldDelta = goldDelta;
            Message = message ?? string.Empty;
        }
    }

    /// <summary>
    /// 공통 판매 창이 화면별 규칙에 접근하는 경계. 읽기와 거래 위임만 제공한다.
    /// 거래는 각 화면의 기존 서비스(EconomyService / OutpostService)가 수행한다.
    /// </summary>
    public interface IResourceSellBackend
    {
        /// <summary>현재 상태. fresh가 true면 캐시 없이 서비스에서 다시 읽는다.</summary>
        ResourceSellSnapshot Read(bool fresh);

        ResourceSellCommitResult Commit(IReadOnlyList<KeyValuePair<string, int>> items);

        /// <summary>판매 안내(제목 아래 한 줄).</summary>
        string Hint { get; }

        /// <summary>최대 선택 규칙 안내. 해당 행이 있을 때만 보인다.</summary>
        string BulkRuleNotice { get; }

        event Action Changed;

        void Dispose();
    }
}
