using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Outpost;
using SubTerra.App.UI.Sell;
using UnityEngine;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 정산 콘솔 규칙을 공통 판매 창에 넘기는 어댑터.
    /// 플레이어 화물만 대상, 희귀 품목은 정산 콘솔에서 판매 불가(행은 보이되 조절 불가),
    /// 골드 보너스는 기존 화물 전체 정산처럼 합계에 한 번 적용. 거래는 OutpostPanelPresenter → OutpostService.
    /// </summary>
    public sealed class SettlementSellBackend : IResourceSellBackend
    {
        public const string HintText = "정산 콘솔 · 화물의 자원을 선택하고 수량을 정해 골드로 판매하세요.";
        public const string BulkNotice = "희귀 품목은 정산 콘솔에서 판매할 수 없습니다 · 지상 판매창 이용";
        public const string RareNote = "정산 불가 · 지상에서 판매";

        private OutpostPanelPresenter presenter;
        private OutpostService service;
        private Func<string, Sprite> iconResolver;
        private OutpostSnapshot latest;

        public SettlementSellBackend(
            OutpostPanelPresenter presenter,
            OutpostService service,
            Func<string, Sprite> iconResolver)
        {
            this.presenter = presenter;
            this.service = service;
            this.iconResolver = iconResolver;
            if (service != null)
            {
                service.SnapshotChanged += OnSnapshotChanged;
            }
        }

        public event Action Changed;

        public string Hint => HintText;
        public string BulkRuleNotice => BulkNotice;

        public void SetIconResolver(Func<string, Sprite> resolver)
        {
            iconResolver = resolver;
        }

        public ResourceSellSnapshot Read(bool fresh)
        {
            if (service == null)
            {
                return ResourceSellSnapshot.Empty;
            }

            var snapshot = fresh || latest == null ? service.GetSnapshot() : latest;
            latest = snapshot;
            var cargo = snapshot != null ? snapshot.PlayerCargo : null;
            var lines = new List<ResourceSellLine>();
            var stacks = cargo != null ? cargo.Stacks : null;
            for (var i = 0; stacks != null && i < stacks.Count; i++)
            {
                var stack = stacks[i];
                if (stack.Quantity <= 0)
                {
                    continue;
                }

                var rare = DataIds.RareItems.IsRare(stack.MineralId);
                lines.Add(new ResourceSellLine(
                    stack.MineralId,
                    ItemDisplayNames.PreferDisplay(stack.MineralId, stack.DisplayName),
                    stack.Quantity,
                    stack.UnitPrice,
                    stack.UnitWeight,
                    iconResolver != null ? iconResolver(stack.MineralId) : null,
                    canSell: !rare,
                    inBulk: !rare,
                    note: rare ? RareNote : string.Empty));
            }

            return new ResourceSellSnapshot(
                lines,
                service.PlayerGold,
                cargo != null ? cargo.CurrentWeight : 0f,
                cargo != null ? cargo.MaxCapacity : 0f,
                service.GoldGainBonusPercent,
                ResourceSellBonusMode.OnTotal);
        }

        public ResourceSellCommitResult Commit(IReadOnlyList<KeyValuePair<string, int>> items)
        {
            if (presenter == null)
            {
                return new ResourceSellCommitResult(false, 0, "전진기지 서비스가 연결되지 않았습니다.");
            }

            var result = presenter.RequestSellBatch(items);
            return new ResourceSellCommitResult(result.IsSuccess, result.GoldDelta, result.Message);
        }

        public void Dispose()
        {
            if (service != null)
            {
                service.SnapshotChanged -= OnSnapshotChanged;
            }

            presenter = null;
            service = null;
            iconResolver = null;
            latest = null;
            Changed = null;
        }

        private void OnSnapshotChanged(OutpostSnapshot snapshot)
        {
            latest = snapshot;
            Changed?.Invoke();
        }
    }
}
