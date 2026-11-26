using System.Globalization;
using SubTerra.App.UI.HUD;
using UnityEngine;
using static SubTerra.Shared.Localization.LocalizationService;

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
                    return Text("적재량 가득 참", "Cargo full");
                case InventoryCargoLevel.NearFull:
                    return Text("적재 한계 임박", "Nearly full");
                case InventoryCargoLevel.Normal:
                    return Text("적재 여유", "Space available");
                default:
                    return Text("비어 있음", "Empty");
            }
        }

        public static string StateLine(float current, float max)
        {
            var level = Level(current, max);
            var label = LevelLabel(level);
            if (level == InventoryCargoLevel.Full)
            {
                return label;
            }

            // prompt-B 190: 남은 적재량을 먼저 읽고 상태 문구를 뒤에 붙인다.
            var remaining = max > current ? max - current : 0f;
            return Text("남은 적재량 ", "Free capacity: ") + HudFormatter.FormatCargoAmount(remaining) + "  ·  " + label;
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

            return "<b>" + quantity.ToString("#,0", CultureInfo.InvariantCulture) + "</b>" + Text("<size=60%> 개</size>", string.Empty);
        }

        /// <summary>prompt-B 190: 명세표 개당 무게 열. 열 머리가 단위를 설명하므로 숫자만 둔다.</summary>
        public static string UnitWeight(float unitWeight)
        {
            return HudFormatter.FormatCargoAmount(unitWeight);
        }

        /// <summary>prompt-B 190: 이 줄 무게 = 수량 × 개당 무게(표시 전용, 적재 판정에 쓰지 않는다).</summary>
        public static string LineWeight(int quantity, float unitWeight)
        {
            if (quantity <= 0 || unitWeight <= 0f)
            {
                return HudFormatter.FormatCargoAmount(0f);
            }

            return HudFormatter.FormatCargoAmount(quantity * unitWeight);
        }

        public static string LoadedKinds(int kinds)
        {
            kinds = System.Math.Max(0, kinds);
            return kinds.ToString(CultureInfo.InvariantCulture) + Text("종", kinds == 1 ? " type" : " types");
        }
    }
}
