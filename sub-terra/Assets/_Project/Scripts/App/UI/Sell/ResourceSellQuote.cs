using SubTerra.App.Economy;

namespace SubTerra.App.UI.Sell
{
    /// <summary>
    /// 현재 선택의 거래 예상 결과. 각 화면 서비스와 같은 EconomyPricing 규칙으로 계산해
    /// 판매 버튼 금액이 실제 지급액과 일치하게 한다.
    /// 행의 '받을 골드'는 보너스 전 금액(단가 × 수량)이고, 보너스는 합계에서 따로 보여 준다.
    /// </summary>
    public readonly struct ResourceSellQuote
    {
        public int TotalQuantity { get; }
        public int SelectedKinds { get; }
        public int BaseGold { get; }
        public int BonusGold { get; }
        public int TotalGold { get; }
        public int GoldBefore { get; }
        public int GoldAfter { get; }
        public float CargoBefore { get; }
        public float CargoAfter { get; }
        public float CargoCapacity { get; }
        public bool Overflow { get; }

        public bool CanSell => TotalQuantity > 0 && !Overflow;

        private ResourceSellQuote(
            int totalQuantity,
            int selectedKinds,
            int baseGold,
            int bonusGold,
            int goldBefore,
            float cargoBefore,
            float cargoAfter,
            float cargoCapacity,
            bool overflow)
        {
            TotalQuantity = totalQuantity;
            SelectedKinds = selectedKinds;
            BaseGold = baseGold;
            BonusGold = bonusGold;
            TotalGold = overflow ? 0 : baseGold + bonusGold;
            GoldBefore = goldBefore;
            GoldAfter = overflow ? goldBefore : goldBefore + TotalGold;
            CargoBefore = cargoBefore;
            CargoAfter = cargoAfter < 0f ? 0f : cargoAfter;
            CargoCapacity = cargoCapacity;
            Overflow = overflow;
        }

        /// <summary>행 하나의 보너스 전 금액. 넘치면 0.</summary>
        public static int LineGold(ResourceSellLine line, int quantity)
        {
            return EconomyPricing.TryComputeGoldGain(line.UnitPrice, quantity, out var gold, out _) ? gold : 0;
        }

        public static ResourceSellQuote Compute(ResourceSellSnapshot snapshot, ResourceSellSelection selection)
        {
            if (snapshot == null || selection == null)
            {
                return default;
            }

            long baseGold = 0;
            long bonusGold = 0;
            long quantity = 0;
            var kinds = 0;
            double removedWeight = 0d;
            var overflow = false;
            for (var i = 0; i < snapshot.Lines.Count; i++)
            {
                var line = snapshot.Lines[i];
                var selected = selection.Get(line.ItemId);
                if (selected <= 0 || !line.CanSell)
                {
                    continue;
                }

                if (!EconomyPricing.TryComputeGoldGain(line.UnitPrice, selected, out var lineGold, out _))
                {
                    overflow = true;
                    continue;
                }

                kinds++;
                quantity += selected;
                baseGold += lineGold;
                removedWeight += (double)line.UnitWeight * selected;
                if (snapshot.BonusMode == ResourceSellBonusMode.PerLine)
                {
                    bonusGold += EconomyPricing.ComputeGoldBonus(lineGold, snapshot.BonusPercent);
                }
            }

            if (baseGold > int.MaxValue)
            {
                overflow = true;
            }
            else if (snapshot.BonusMode == ResourceSellBonusMode.OnTotal)
            {
                bonusGold = EconomyPricing.ComputeGoldBonus((int)baseGold, snapshot.BonusPercent);
            }

            if (baseGold + bonusGold > (long)int.MaxValue - snapshot.Gold)
            {
                overflow = true;
            }

            return new ResourceSellQuote(
                quantity > int.MaxValue ? int.MaxValue : (int)quantity,
                kinds,
                overflow ? 0 : (int)baseGold,
                overflow ? 0 : (int)bonusGold,
                snapshot.Gold,
                snapshot.CargoWeight,
                (float)(snapshot.CargoWeight - removedWeight),
                snapshot.CargoCapacity,
                overflow);
        }
    }
}
