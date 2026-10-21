using System;

namespace SubTerra.App.Tutorial
{
    /// <summary>
    /// 화물 버리기 창(B-153) 표시·요청 수량 계산. 실제 버리기·지급 판정은 QuestRewardService와 InventoryService가 한다.
    /// 무게는 CargoShortfallModel과 같이 소수 둘째 자리까지만 쓴다.
    /// </summary>
    public static class CargoDiscardMath
    {
        /// <summary>9.5 / 2 같은 경계에서 부동소수 오차로 한 개 더 나오지 않게 나눗셈 전에 뺀다.</summary>
        public const float Epsilon = 0.0001f;

        /// <summary>min(요청 수, 보유 수). 0 아래로 내려가지 않는다. int.MaxValue('전부')는 보유 수 그대로다.</summary>
        public static int Clamp(int requested, int owned)
        {
            if (requested <= 0 || owned <= 0)
            {
                return 0;
            }

            return Math.Min(requested, owned);
        }

        public static float Weight(int count, float unitWeight)
        {
            if (count <= 0 || unitWeight <= 0f)
            {
                return 0f;
            }

            return Round2(count * (double)Round2(unitWeight));
        }

        /// <summary>
        /// 부족을 0으로 만드는 가장 적은 정수 개수 = ceil(부족 / 개당 무게)를 보유 수로 자른다.
        /// 보유를 다 버려도 모자라면 Covers가 false이고 Remaining에 남는 부족이 담긴다.
        /// </summary>
        public static CargoDiscardNeed Needed(float shortfall, float unitWeight, int owned)
        {
            var need = Round2(shortfall);
            var unit = Round2(unitWeight);
            if (need <= 0f)
            {
                return new CargoDiscardNeed(0, true, 0f, 0f);
            }

            if (owned <= 0 || unit <= 0f)
            {
                return new CargoDiscardNeed(0, false, need, 0f);
            }

            var exact = Math.Ceiling((need - Epsilon) / unit);
            if (exact <= owned)
            {
                var count = Math.Max(1, (int)exact);
                return new CargoDiscardNeed(count, true, 0f, Weight(count, unit));
            }

            return new CargoDiscardNeed(owned, false, Round2(need - (double)Weight(owned, unit)), Weight(owned, unit));
        }

        public static float Round2(double value)
        {
            if (double.IsNaN(value) || value <= 0d)
            {
                return 0f;
            }

            return (float)Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }

    public readonly struct CargoDiscardNeed
    {
        /// <summary>버릴 개수. 부족이 없거나 보유가 0이면 0.</summary>
        public int Count { get; }

        /// <summary>Count만큼 버리면 부족이 0이 되는지.</summary>
        public bool Covers { get; }

        /// <summary>Count만큼 버린 뒤에도 남는 부족.</summary>
        public float Remaining { get; }

        public float Freed { get; }

        public CargoDiscardNeed(int count, bool covers, float remaining, float freed)
        {
            Count = count;
            Covers = covers;
            Remaining = remaining;
            Freed = freed;
        }
    }
}
