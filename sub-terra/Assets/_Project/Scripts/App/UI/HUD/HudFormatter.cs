using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// HUD 표시 문자열 포맷터. State 수치 → 표시 텍스트만 담당하며 상태를 변경하지 않는다.
    /// null/0/미선택 시 명시적 기본값을 사용한다.
    /// </summary>
    public static class HudFormatter
    {
        public const string DefaultBuildingNone = "선택 없음";
        public const string DefaultInteractionEmpty = "";
        public const string LabelSafe = "안전";
        public const string LabelCaution = "주의";
        public const string LabelCritical = "위험";
        public const string LabelGasElevated = "노출";
        public const string LabelGasHazard = "위험";

        public static string FormatEnergy(int current, int max)
        {
            if (current < 0)
            {
                current = 0;
            }

            if (max < 0)
            {
                max = 0;
            }

            return "전력 " + current + " / " + max;
        }

        public static string FormatEnergy(EnergyReadModel model)
        {
            return FormatEnergy(model.Current, model.Max);
        }

        public static string FormatHealth(float current, int maximum)
        {
            var safeMaximum = maximum < 1 ? 1 : maximum;
            var safeCurrent = float.IsNaN(current) || current < 0f ? 0f : current;
            safeCurrent = safeCurrent > safeMaximum ? safeMaximum : safeCurrent;
            return "체력 " + (int)System.Math.Ceiling(safeCurrent) + " / " + safeMaximum;
        }

        public static string FormatHealth(PlayerHealthReadModel model)
        {
            return FormatHealth(model.Current, model.Maximum);
        }

        public static string FormatGold(int gold)
        {
            var safe = gold < 0 ? 0 : gold;
            return "골드 " + safe;
        }

        public static string FormatDepth(int depth)
        {
            var safe = depth < 0 ? 0 : depth;
            return "깊이 " + safe + "m";
        }

        public static string FormatCargoAmount(float cargoWeight)
        {
            if (cargoWeight < 0f)
            {
                cargoWeight = 0f;
            }

            return cargoWeight.ToString("0.#");
        }

        public static string FormatCargo(float cargoWeight)
        {
            return "화물 " + FormatCargoAmount(cargoWeight);
        }

        /// <summary>
        /// 인벤토리 패널 상단 요약. prompt-B 36-1: 표시 라벨은 "인벤토리".
        /// HUD 화물 한 줄 표시(FormatCargo)와 구분한다.
        /// </summary>
        public static string FormatCargoSummary(float currentWeight, float maxCapacity)
        {
            return "인벤토리 " + FormatCargoAmount(currentWeight)
                + " / " + FormatCargoAmount(maxCapacity);
        }

        public static string FormatUnsettledValue(float value)
        {
            if (value < 0f)
            {
                value = 0f;
            }

            return "미정산 " + value.ToString("0");
        }

        public static string FormatStructuralRisk(StructuralRiskLevel level)
        {
            switch (level)
            {
                case StructuralRiskLevel.Caution:
                    return "구조 " + LabelCaution;
                case StructuralRiskLevel.Critical:
                    return "구조 " + LabelCritical;
                case StructuralRiskLevel.Imminent:
                    return "구조 붕괴 임박";
                default:
                    return "구조 " + LabelSafe;
            }
        }

        public static string FormatGasRisk(GasRiskLevel level)
        {
            switch (level)
            {
                case GasRiskLevel.Elevated:
                    return "가스 " + LabelGasElevated;
                case GasRiskLevel.Hazard:
                    return "가스 " + LabelGasHazard;
                default:
                    return "가스 " + LabelSafe;
            }
        }

        public static string FormatBuildingSelection(BuildingSelectionReadModel selection)
        {
            if (!selection.HasSelection)
            {
                return "시설 " + DefaultBuildingNone;
            }

            if (!string.IsNullOrEmpty(selection.DisplayName))
            {
                return "시설 " + selection.DisplayName;
            }

            return "시설 " + selection.BuildingId;
        }

        public static string FormatBuildingSelection(string buildingId, string displayName)
        {
            return FormatBuildingSelection(new BuildingSelectionReadModel(buildingId, displayName));
        }

        public static string FormatInteractionPrompt(string prompt)
        {
            return string.IsNullOrEmpty(prompt) ? DefaultInteractionEmpty : prompt;
        }

        /// <summary>가스 경고 패널 활성 여부. Safe가 아니면 표시.</summary>
        public static bool ShouldShowGasWarning(GasRiskLevel level)
        {
            return level != GasRiskLevel.Safe;
        }
    }
}
