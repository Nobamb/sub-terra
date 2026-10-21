using System;

namespace SubTerra.App.Tutorial
{
    /// <summary>
    /// 화물 공간 부족 팝업(B-152) 표시값. 지급 판정은 InventoryService.CanFitExact가 하고, 이 값은 보여 주기만 한다.
    /// 부족한 공간 = 현재 무게 + 보상 무게 − 최대 적재량 (0 미만은 0). 보상 무게에 골드는 들어가지 않는다.
    /// </summary>
    public readonly struct CargoShortfallModel
    {
        public float CurrentWeight { get; }
        public float MaxCapacity { get; }
        public float RewardWeight { get; }
        public float Shortfall { get; }

        public bool IsShort => Shortfall > 0f;

        private CargoShortfallModel(float current, float max, float rewardWeight)
        {
            CurrentWeight = Clean(current);
            MaxCapacity = Clean(max);
            RewardWeight = Clean(rewardWeight);
            Shortfall = Clean(CurrentWeight + RewardWeight - MaxCapacity);
        }

        public static CargoShortfallModel From(float currentWeight, float maxCapacity, float rewardWeight)
        {
            return new CargoShortfallModel(currentWeight, maxCapacity, rewardWeight);
        }

        /// <summary>
        /// 고정 칸 게이지. 채운 칸은 현재 무게 비율, 초과 칸은 ceil(부족 / 칸당 무게)를 1~maxOverflowCells로 자른다.
        /// 가득 차지 않았으면 마지막 칸은 비워 둬 '꽉 참'과 구분한다.
        /// </summary>
        public CargoGaugeLayout Layout(int cellCount, int maxOverflowCells)
        {
            var cells = Math.Max(1, Math.Min(cellCount, CargoGaugeLayout.MaxCells));
            var overflowCap = Math.Max(1, Math.Min(maxOverflowCells, CargoGaugeLayout.MaxOverflowCells));
            var perCell = MaxCapacity > 0f ? MaxCapacity / cells : 1f;

            int filled;
            if (MaxCapacity <= 0f)
            {
                filled = CurrentWeight > 0f ? cells : 0;
            }
            else if (CurrentWeight >= MaxCapacity)
            {
                filled = cells;
            }
            else
            {
                filled = (int)Math.Floor(CurrentWeight / perCell + 0.5f);
                if (CurrentWeight > 0f && filled == 0)
                {
                    filled = 1;
                }

                filled = Math.Min(filled, cells - 1);
            }

            var overflow = 0;
            if (IsShort)
            {
                overflow = (int)Math.Ceiling(Shortfall / perCell - 0.0001f);
                overflow = Math.Max(1, Math.Min(overflow, overflowCap));
            }

            return new CargoGaugeLayout(cells, filled, overflow, perCell);
        }

        // 무게는 소수점 둘째 자리까지만 의미가 있다. 19 + 1.5 − 20 같은 계산의 부동소수 오차를 지운다.
        private static float Clean(float value)
        {
            if (float.IsNaN(value) || value <= 0f)
            {
                return 0f;
            }

            return (float)Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }

    public readonly struct CargoGaugeLayout
    {
        public const int MaxCells = 40;
        public const int MaxOverflowCells = 8;

        public int CellCount { get; }
        public int FilledCells { get; }
        public int OverflowCells { get; }
        public float WeightPerCell { get; }

        public CargoGaugeLayout(int cellCount, int filledCells, int overflowCells, float weightPerCell)
        {
            CellCount = cellCount;
            FilledCells = filledCells;
            OverflowCells = overflowCells;
            WeightPerCell = weightPerCell;
        }
    }
}
