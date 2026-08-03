using SubTerra.App.State;

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

        public static string FormatGold(int gold)
        {
            return gold < 0 ? "0" : gold.ToString();
        }

        public static string FormatDepth(int depth)
        {
            return depth < 0 ? "0" : depth.ToString();
        }

        public static string FormatCargo(float cargoWeight)
        {
            if (cargoWeight < 0f)
            {
                cargoWeight = 0f;
            }

            return cargoWeight.ToString("0.#");
        }

        public static string FormatUnsettledValue(float value)
        {
            if (value < 0f)
            {
                value = 0f;
            }

            return value.ToString("0");
        }

        public static string FormatStructuralRisk(StructuralRiskLevel level)
        {
            switch (level)
            {
                case StructuralRiskLevel.Caution:
                    return LabelCaution;
                case StructuralRiskLevel.Critical:
                    return LabelCritical;
                default:
                    return LabelSafe;
            }
        }

        public static string FormatGasRisk(GasRiskLevel level)
        {
            switch (level)
            {
                case GasRiskLevel.Elevated:
                    return LabelGasElevated;
                case GasRiskLevel.Hazard:
                    return LabelGasHazard;
                default:
                    return LabelSafe;
            }
        }

        public static string FormatBuildingSelection(BuildingSelectionReadModel selection)
        {
            if (!selection.HasSelection)
            {
                return DefaultBuildingNone;
            }

            if (!string.IsNullOrEmpty(selection.DisplayName))
            {
                return selection.DisplayName;
            }

            return selection.BuildingId;
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
