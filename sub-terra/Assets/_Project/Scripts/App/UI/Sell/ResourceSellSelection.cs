using System.Collections.Generic;

namespace SubTerra.App.UI.Sell
{
    /// <summary>
    /// 행별 판매 예정 수량. 항상 0 이상, 행의 판매 가능 수량 이하로 묶는다.
    /// +5·+10은 현재 값에 더하는 누적 조절이고, 최대는 그 행의 판매 가능 수량으로 맞춘다.
    /// </summary>
    public sealed class ResourceSellSelection
    {
        private readonly Dictionary<string, int> quantities = new Dictionary<string, int>();

        public int Get(string itemId)
        {
            return itemId != null && quantities.TryGetValue(itemId, out var value) ? value : 0;
        }

        public bool IsEmpty
        {
            get
            {
                foreach (var pair in quantities)
                {
                    if (pair.Value > 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>delta만큼 더하거나 뺀 뒤 [0, 최대]로 자른다. 값이 바뀌면 true.</summary>
        public bool Adjust(ResourceSellLine line, int delta)
        {
            var current = Get(line.ItemId);
            long next = (long)current + delta;
            return Set(line, next > int.MaxValue ? int.MaxValue : (int)System.Math.Max(next, int.MinValue));
        }

        public bool SetMax(ResourceSellLine line)
        {
            return Set(line, line.MaxSelectable);
        }

        public bool Set(ResourceSellLine line, int quantity)
        {
            var clamped = Clamp(quantity, line.MaxSelectable);
            var current = Get(line.ItemId);
            if (clamped == current)
            {
                return false;
            }

            if (clamped == 0)
            {
                quantities.Remove(line.ItemId);
            }
            else
            {
                quantities[line.ItemId] = clamped;
            }

            return true;
        }

        /// <summary>'최대 선택': 일괄 대상 행만 최대로 채운다. 대상이 아닌 행의 직접 지정 값은 그대로 둔다.</summary>
        public bool SelectBulk(IReadOnlyList<ResourceSellLine> lines)
        {
            var changed = false;
            for (var i = 0; lines != null && i < lines.Count; i++)
            {
                if (lines[i].InBulk)
                {
                    changed |= SetMax(lines[i]);
                }
            }

            return changed;
        }

        public bool Reset()
        {
            var changed = !IsEmpty;
            quantities.Clear();
            return changed;
        }

        /// <summary>
        /// 최신 행 목록에 맞춰 수량을 다시 자른다. 사라진 행·판매 불가가 된 행은 0이 된다.
        /// 하나라도 바뀌면 true(화면과 실제 거래가 어긋나지 않도록 확정 전에 쓴다).
        /// </summary>
        public bool Reconcile(ResourceSellSnapshot snapshot)
        {
            if (quantities.Count == 0)
            {
                return false;
            }

            var changed = false;
            var keys = new List<string>(quantities.Keys);
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var max = snapshot != null && snapshot.TryFind(key, out var line) ? line.MaxSelectable : 0;
                var current = quantities[key];
                var clamped = Clamp(current, max);
                if (clamped == current)
                {
                    continue;
                }

                changed = true;
                if (clamped == 0)
                {
                    quantities.Remove(key);
                }
                else
                {
                    quantities[key] = clamped;
                }
            }

            return changed;
        }

        /// <summary>거래 요청 목록. 표시 순서를 유지하고 판매 가능한 행만 넣는다.</summary>
        public List<KeyValuePair<string, int>> ToItems(ResourceSellSnapshot snapshot)
        {
            var items = new List<KeyValuePair<string, int>>();
            if (snapshot == null)
            {
                return items;
            }

            for (var i = 0; i < snapshot.Lines.Count; i++)
            {
                var line = snapshot.Lines[i];
                var quantity = Get(line.ItemId);
                if (quantity > 0 && line.CanSell)
                {
                    items.Add(new KeyValuePair<string, int>(line.ItemId, quantity));
                }
            }

            return items;
        }

        public static int Clamp(int quantity, int max)
        {
            if (max <= 0 || quantity <= 0)
            {
                return 0;
            }

            return quantity > max ? max : quantity;
        }
    }
}
