namespace SubTerra.App.Economy
{
    /// <summary>
    /// 단가 × 수량 골드 계산 공유 규칙.
    /// Service 판매 커밋과 Presenter 미리보기가 동일 오버플로 검사를 쓰도록 한곳에 둔다.
    /// </summary>
    public static class EconomyPricing
    {
        /// <summary>단가 × 수량. 오버플로 시 false.</summary>
        public static bool TryComputeGoldGain(
            int unitPrice,
            int quantity,
            out int goldGain,
            out string diagnostic)
        {
            goldGain = 0;
            diagnostic = string.Empty;

            if (unitPrice == 0 || quantity == 0)
            {
                goldGain = 0;
                return true;
            }

            if (unitPrice < 0 || quantity < 0)
            {
                diagnostic = "Negative unitPrice or quantity.";
                return false;
            }

            // unitPrice * quantity 가 int 범위를 넘는지 검사.
            if (quantity > int.MaxValue / unitPrice)
            {
                diagnostic = "unitPrice*quantity overflow.";
                return false;
            }

            goldGain = unitPrice * quantity;
            return true;
        }
    }
}
