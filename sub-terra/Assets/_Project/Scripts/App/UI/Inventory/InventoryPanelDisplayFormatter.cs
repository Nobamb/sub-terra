using System.Globalization;
using SubTerra.App.UI.HUD;
using UnityEngine;

namespace SubTerra.App.UI.Inventory
{
    public enum InventoryCargoLevel
    {
        Empty,
        Normal,
        NearFull,
        Full
    }

    /// <summary>
    /// prompt-B 112: 인벤토리 창 표시 문구. 스냅샷 값을 포맷만 하며 중량·가치를 재계산하지 않는다.
    /// </summary>
    public static class InventoryPanelDisplayFormatter
    {
        public const float NearFullRatio = 0.8f;
        private const float FullEpsilon = 0.0001f;

        public static float FillRatio(float current, float max)
        {
            return max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        public static InventoryCargoLevel Level(float current, float max)
        {
            if (max <= 0f || current <= 0f)
            {
                return InventoryCargoLevel.Empty;
            }

            if (current >= max - FullEpsilon)
            {
                return InventoryCargoLevel.Full;
            }

            // 40/50처럼 경계값이 부동소수 오차로 기준 아래로 떨어지지 않게 허용 오차를 둔다.
            return current / max >= NearFullRatio - FullEpsilon ? InventoryCargoLevel.NearFull : InventoryCargoLevel.Normal;
        }

        public static string LoadAmount(float current, float max)
        {
            return "<b>" + HudFormatter.FormatCargoAmount(current) + "</b>"
                + "<size=60%><color=#8FB3C2>  / " + HudFormatter.FormatCargoAmount(max) + "</color></size>";
        }

        public static string Percent(float current, float max)
        {
            if (Level(current, max) == InventoryCargoLevel.Full)
            {
                return "100%";
            }

            // 가득 차기 전에는 100%로 반올림되지 않게 내림한다.
            return Mathf.FloorToInt(FillRatio(current, max) * 100f).ToString(CultureInfo.InvariantCulture) + "%";
        }

        public static string LevelLabel(InventoryCargoLevel level)
        {
            switch (level)
            {
                case InventoryCargoLevel.Full:
                    return "가득 참";
                case InventoryCargoLevel.NearFull:
                    return "거의 가득";
                case InventoryCargoLevel.Normal:
                    return "적재 여유";
                default:
                    return "비어 있음";
            }
        }

        public static string StateLine(float current, float max)
        {
            var level = Level(current, max);
            var label = LevelLabel(level);
            if (level == InventoryCargoLevel.Full)
            {
                return label + "  ·  더 이상 채굴할 수 없습니다";
            }

            var remaining = max > current ? max - current : 0f;
            return label + "  ·  남은 용량 " + HudFormatter.FormatCargoAmount(remaining);
        }

        public static string UnsettledAmount(float value)
        {
            if (value < 0f)
            {
                value = 0f;
            }

            return value.ToString("#,0", CultureInfo.InvariantCulture);
        }

        public static string Quantity(int quantity)
        {
            if (quantity < 0)
            {
                quantity = 0;
            }

            return "<b>" + quantity.ToString("#,0", CultureInfo.InvariantCulture) + "</b><size=60%> 개</size>";
        }

        public static string QuantityState(int quantity)
        {
            return quantity > 0 ? "보유 중" : "보유 없음";
        }

        public static string UnitWeight(float unitWeight)
        {
            return unitWeight > 0f ? "개당 무게 " + HudFormatter.FormatCargoAmount(unitWeight) : string.Empty;
        }

        public static string OwnedKinds(int owned, int total)
        {
            return "보유 " + owned + " / " + total + "종";
        }
    }
}
