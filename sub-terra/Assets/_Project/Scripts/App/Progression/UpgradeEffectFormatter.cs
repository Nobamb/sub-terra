using System;
using System.Globalization;
using SubTerra.App.Core.Data;

namespace SubTerra.App.Progression
{
    /// <summary>
    /// 업그레이드 효과 수치를 실제 명칭과 단위로 바꾼다(예: 0.25 → "+25%").
    /// 효과 값의 의미는 UpgradeEffectProvider와 Gameplay 소비 코드를 기준으로 한다.
    /// </summary>
    public static class UpgradeEffectFormatter
    {
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        /// <summary>효과가 무엇을 바꾸는지 나타내는 짧은 명칭.</summary>
        public static string EffectName(string upgradeId)
        {
            switch (upgradeId)
            {
                case DataIds.Upgrades.DrillSpeed:
                    return "채굴 속도";
                case DataIds.Upgrades.DrillEfficiency:
                    return "채굴 전력 소모";
                case DataIds.Upgrades.MaximumEnergy:
                    return "최대 전력";
                case DataIds.Upgrades.MaximumHealth:
                    return "최대 체력";
                case DataIds.Upgrades.HealthRegeneration:
                    return "체력 재생";
                case DataIds.Upgrades.MaximumCargo:
                    return "최대 화물 중량";
                case DataIds.Upgrades.CargoGold:
                    return "골드 획득";
                case DataIds.Upgrades.DroneScan:
                    return "드론 스캔 반경";
                case DataIds.Upgrades.DroneRescue:
                    return "구조 시 화물 보존";
                case DataIds.Upgrades.GasResistance:
                    return "가스 피해";
                default:
                    return ItemDisplayNames.Upgrade(upgradeId);
            }
        }

        /// <summary>효과 값을 단위와 부호가 붙은 문자열로 만든다.</summary>
        public static string FormatValue(string upgradeId, float value)
        {
            // 레벨 0은 "+0%"/"-0%" 대신 읽기 쉬운 문구로 보여 준다.
            if (value <= 0f)
            {
                return upgradeId == DataIds.Upgrades.DrillSpeed ? "기본" : "없음";
            }

            switch (upgradeId)
            {
                case DataIds.Upgrades.DrillSpeed:
                case DataIds.Upgrades.CargoGold:
                    return "+" + Number(PercentOf(upgradeId, value)) + "%";
                case DataIds.Upgrades.DrillEfficiency:
                case DataIds.Upgrades.GasResistance:
                    return "-" + Number(Percent(value)) + "%";
                case DataIds.Upgrades.DroneRescue:
                    return "+" + Number(Percent(value)) + "%";
                case DataIds.Upgrades.MaximumEnergy:
                case DataIds.Upgrades.MaximumHealth:
                    return "+" + Number(value);
                case DataIds.Upgrades.HealthRegeneration:
                    return "+" + Number(value) + "/초";
                case DataIds.Upgrades.MaximumCargo:
                    return "+" + Number(value) + "kg";
                case DataIds.Upgrades.DroneScan:
                    return Number(value) + "칸";
                default:
                    return Number(value);
            }
        }

        /// <summary>"채굴 속도 +25%"처럼 명칭과 값을 함께 쓴다.</summary>
        public static string Describe(string upgradeId, float value)
        {
            return EffectName(upgradeId) + " " + FormatValue(upgradeId, value);
        }

        // 드릴 속도는 데이터에 배수 증가분(0.25)로 저장되어 퍼센트로, 골드 획득은 이미 퍼센트(50)로 저장된다.
        private static float PercentOf(string upgradeId, float value)
        {
            return upgradeId == DataIds.Upgrades.CargoGold ? value : Percent(value);
        }

        private static float Percent(float fraction)
        {
            return (float)Math.Round(fraction * 100f, MidpointRounding.AwayFromZero);
        }

        private static string Number(float value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.##", Culture);
        }
    }
}
