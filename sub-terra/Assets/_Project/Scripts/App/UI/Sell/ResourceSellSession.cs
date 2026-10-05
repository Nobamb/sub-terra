using System;
using System.Collections.Generic;

namespace SubTerra.App.UI.Sell
{
    public enum ResourceSellConfirmStatus
    {
        Sold = 0,
        /// <summary>표시 중 보유량·가격이 바뀌어 수량을 다시 맞췄다. 거래하지 않았다.</summary>
        Adjusted = 1,
        Failed = 2,
        Empty = 3,
        Busy = 4
    }

    /// <summary>판매 확정 결과. 성공일 때만 판매한 품목과 골드 변화가 의미 있다.</summary>
    public sealed class ResourceSellConfirmOutcome
    {
        public ResourceSellConfirmStatus Status { get; }
        public string Message { get; }
        public IReadOnlyList<string> SoldItemIds { get; }
        public int GoldBefore { get; }
        public int GoldAfter { get; }

        public bool IsSold => Status == ResourceSellConfirmStatus.Sold;

        public ResourceSellConfirmOutcome(
            ResourceSellConfirmStatus status,
            string message,
            IReadOnlyList<string> soldItemIds = null,
            int goldBefore = 0,
            int goldAfter = 0)
        {
            Status = status;
            Message = message ?? string.Empty;
            SoldItemIds = soldItemIds ?? Array.Empty<string>();
            GoldBefore = goldBefore;
            GoldAfter = goldAfter;
        }
    }

    /// <summary>
    /// 지상 판매창과 정산 콘솔이 같이 쓰는 판매 상태. 화면 규칙은 IResourceSellBackend가 데이터로 넘기고,
    /// 여기서는 수량 선택·예상 결과·확정 재검증·중복 실행 방지만 다룬다. UnityEngine 표시 코드는 없다.
    /// </summary>
    public sealed class ResourceSellSession
    {
        public const string AdjustedMessage = "보유량이 변경되어 판매 수량을 조정했습니다. 확인 후 다시 판매하세요.";

        private readonly ResourceSellSelection selection = new ResourceSellSelection();
        private IResourceSellBackend backend;
        private ResourceSellSnapshot snapshot = ResourceSellSnapshot.Empty;
        private bool committing;

        public ResourceSellSession(IResourceSellBackend backend)
        {
            this.backend = backend;
            if (backend != null)
            {
                backend.Changed += OnBackendChanged;
            }
        }

        /// <summary>표시해야 할 값이 바뀌었다(행 수량·골드·화물·선택).</summary>
        public event Action Changed;

        public ResourceSellSnapshot Snapshot => snapshot;
        public ResourceSellSelection Selection => selection;
        public ResourceSellQuote Quote => ResourceSellQuote.Compute(snapshot, selection);
        public bool IsCommitting => committing;
        public bool IsBound => backend != null;
        public string Hint => backend != null ? backend.Hint : string.Empty;

        /// <summary>제한 행(희귀 품목 등)이 있을 때만 보이는 최대 선택 규칙 안내.</summary>
        public string BulkRuleNotice
        {
            get
            {
                if (backend == null)
                {
                    return string.Empty;
                }

                for (var i = 0; i < snapshot.Lines.Count; i++)
                {
                    if (!snapshot.Lines[i].InBulk)
                    {
                        return backend.BulkRuleNotice ?? string.Empty;
                    }
                }

                return string.Empty;
            }
        }

        public bool HasBulkTargets
        {
            get
            {
                for (var i = 0; i < snapshot.Lines.Count; i++)
                {
                    var line = snapshot.Lines[i];
                    if (line.InBulk && selection.Get(line.ItemId) < line.MaxSelectable)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>창을 열 때마다 최신 보유량·가격을 읽고 수량은 0에서 시작한다.</summary>
        public void Open()
        {
            selection.Reset();
            snapshot = Read(true);
            RaiseChanged();
        }

        public int GetQuantity(string itemId) => selection.Get(itemId);

        public bool Adjust(string itemId, int delta)
        {
            return Mutate(itemId, line => selection.Adjust(line, delta));
        }

        public bool SetMax(string itemId)
        {
            return Mutate(itemId, line => selection.SetMax(line));
        }

        public bool SelectAllBulk()
        {
            if (committing || !selection.SelectBulk(snapshot.Lines))
            {
                return false;
            }

            RaiseChanged();
            return true;
        }

        public bool ResetSelection()
        {
            if (committing || !selection.Reset())
            {
                return false;
            }

            RaiseChanged();
            return true;
        }

        /// <summary>서비스 상태를 다시 읽는다. 값이 그대로면 아무것도 하지 않는다.</summary>
        public void Refresh()
        {
            if (backend == null || committing)
            {
                return;
            }

            var next = Read(false);
            var changed = !next.SameValues(snapshot);
            snapshot = next;
            changed |= selection.Reconcile(snapshot);
            if (changed)
            {
                RaiseChanged();
            }
        }

        /// <summary>
        /// 판매 확정. 서비스에서 다시 읽어 표시 중 보유량·가격이 바뀌었으면 거래 없이 수량을 맞춘다.
        /// 거래 중 재진입은 Busy로 막고, 실패하면 선택을 유지한다.
        /// </summary>
        public ResourceSellConfirmOutcome Confirm()
        {
            if (committing)
            {
                return new ResourceSellConfirmOutcome(ResourceSellConfirmStatus.Busy, "처리 중입니다.");
            }

            if (backend == null)
            {
                return new ResourceSellConfirmOutcome(ResourceSellConfirmStatus.Failed, "판매 서비스가 연결되지 않았습니다.");
            }

            var displayed = snapshot;
            var fresh = Read(true);
            var adjusted = selection.Reconcile(fresh) || PriceChangedForSelection(displayed, fresh);
            snapshot = fresh;
            if (adjusted)
            {
                RaiseChanged();
                return new ResourceSellConfirmOutcome(ResourceSellConfirmStatus.Adjusted, AdjustedMessage);
            }

            var items = selection.ToItems(snapshot);
            if (items.Count == 0)
            {
                return new ResourceSellConfirmOutcome(ResourceSellConfirmStatus.Empty, "판매할 수량을 선택하세요.");
            }

            var quote = ResourceSellQuote.Compute(snapshot, selection);
            if (quote.Overflow)
            {
                return new ResourceSellConfirmOutcome(ResourceSellConfirmStatus.Failed, "골드 한도를 초과합니다.");
            }

            var goldBefore = snapshot.Gold;
            ResourceSellCommitResult result;
            committing = true;
            try
            {
                result = backend.Commit(items);
            }
            finally
            {
                committing = false;
            }

            snapshot = Read(true);
            if (!result.Success)
            {
                selection.Reconcile(snapshot);
                RaiseChanged();
                return new ResourceSellConfirmOutcome(
                    ResourceSellConfirmStatus.Failed,
                    string.IsNullOrEmpty(result.Message) ? "판매에 실패했습니다." : result.Message);
            }

            var sold = new List<string>(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                sold.Add(items[i].Key);
            }

            selection.Reset();
            RaiseChanged();
            return new ResourceSellConfirmOutcome(
                ResourceSellConfirmStatus.Sold,
                result.Message,
                sold,
                goldBefore,
                snapshot.Gold);
        }

        public void Dispose()
        {
            if (backend != null)
            {
                backend.Changed -= OnBackendChanged;
                backend.Dispose();
                backend = null;
            }

            selection.Reset();
            snapshot = ResourceSellSnapshot.Empty;
            Changed = null;
        }

        private bool Mutate(string itemId, Func<ResourceSellLine, bool> change)
        {
            if (committing || !snapshot.TryFind(itemId, out var line) || !change(line))
            {
                return false;
            }

            RaiseChanged();
            return true;
        }

        private bool PriceChangedForSelection(ResourceSellSnapshot displayed, ResourceSellSnapshot fresh)
        {
            for (var i = 0; i < fresh.Lines.Count; i++)
            {
                var line = fresh.Lines[i];
                if (selection.Get(line.ItemId) <= 0)
                {
                    continue;
                }

                if (!displayed.TryFind(line.ItemId, out var shown) || shown.UnitPrice != line.UnitPrice)
                {
                    return true;
                }
            }

            return displayed.BonusPercent != fresh.BonusPercent && !selection.IsEmpty;
        }

        private ResourceSellSnapshot Read(bool fresh)
        {
            var read = backend != null ? backend.Read(fresh) : null;
            return read ?? ResourceSellSnapshot.Empty;
        }

        private void OnBackendChanged()
        {
            // 거래 도중 들어오는 중간 이벤트는 확정 뒤 한 번에 반영한다.
            if (!committing)
            {
                Refresh();
            }
        }

        private void RaiseChanged()
        {
            Changed?.Invoke();
        }
    }
}
