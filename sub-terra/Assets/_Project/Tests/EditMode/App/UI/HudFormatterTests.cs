using NUnit.Framework;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;

namespace SubTerra.App.Tests.UI
{
    public sealed class HudFormatterTests
    {
        [Test]
        public void FormatEnergy_UsesCurrentAndMax()
        {
            Assert.That(HudFormatter.FormatEnergy(40, 100), Is.EqualTo("전력 40 / 100"));
            Assert.That(HudFormatter.FormatEnergy(new EnergyReadModel(0, 0)), Is.EqualTo("전력 0 / 0"));
        }

        [Test]
        public void PromptB55_FormatHealth_UsesCurrentAndMaximum()
        {
            Assert.That(HudFormatter.FormatHealth(100f, 100), Is.EqualTo("체력 100 / 100"));
            Assert.That(HudFormatter.FormatHealth(75.2f, 130), Is.EqualTo("체력 76 / 130"));
            Assert.That(HudFormatter.FormatHealth(-1f, 0), Is.EqualTo("체력 0 / 1"));
        }

        [Test]
        public void FormatDefaults_ZeroNullAndNone()
        {
            Assert.That(HudFormatter.FormatGold(0), Is.EqualTo("골드 0"));
            Assert.That(HudFormatter.FormatGold(-3), Is.EqualTo("골드 0"));
            Assert.That(HudFormatter.FormatDepth(0), Is.EqualTo("깊이 0m"));
            Assert.That(HudFormatter.FormatCargo(0f), Is.EqualTo("화물 0"));
            // prompt-B 36-1: 인벤토리 패널 요약 라벨은 "인벤토리".
            Assert.That(HudFormatter.FormatCargoSummary(0f, 50f), Is.EqualTo("인벤토리 0 / 50"));
            Assert.That(HudFormatter.FormatUnsettledValue(0f), Is.EqualTo("미정산 0"));
            Assert.That(HudFormatter.FormatStructuralRisk(StructuralRiskLevel.Safe),
                Is.EqualTo("구조 " + HudFormatter.LabelSafe));
            Assert.That(HudFormatter.FormatGasRisk(GasRiskLevel.Safe),
                Is.EqualTo("가스 " + HudFormatter.LabelSafe));
            Assert.That(HudFormatter.FormatBuildingSelection(null, null),
                Is.EqualTo("시설 " + HudFormatter.DefaultBuildingNone));
            Assert.That(HudFormatter.FormatBuildingSelection(string.Empty, string.Empty),
                Is.EqualTo("시설 " + HudFormatter.DefaultBuildingNone));
            Assert.That(HudFormatter.FormatInteractionPrompt(null), Is.EqualTo(HudFormatter.DefaultInteractionEmpty));
            Assert.That(HudFormatter.FormatInteractionPrompt(string.Empty),
                Is.EqualTo(HudFormatter.DefaultInteractionEmpty));
        }

        [Test]
        public void FormatRiskAndSelection_UsesDefinedLabels()
        {
            Assert.That(HudFormatter.FormatStructuralRisk(StructuralRiskLevel.Caution),
                Is.EqualTo("구조 " + HudFormatter.LabelCaution));
            Assert.That(HudFormatter.FormatStructuralRisk(StructuralRiskLevel.Critical),
                Is.EqualTo("구조 " + HudFormatter.LabelCritical));
            Assert.That(HudFormatter.FormatStructuralRisk(StructuralRiskLevel.Imminent),
                Is.EqualTo("구조 붕괴 임박"));
            Assert.That(HudFormatter.FormatGasRisk(GasRiskLevel.Elevated),
                Is.EqualTo("가스 " + HudFormatter.LabelGasElevated));
            Assert.That(HudFormatter.FormatGasRisk(GasRiskLevel.Hazard),
                Is.EqualTo("가스 " + HudFormatter.LabelGasHazard));
            Assert.That(HudFormatter.FormatBuildingSelection("building.support.basic", "기본 버팀목"),
                Is.EqualTo("시설 기본 버팀목"));
            Assert.That(HudFormatter.FormatBuildingSelection("building.support.basic", null),
                Is.EqualTo("시설 building.support.basic"));
            Assert.That(HudFormatter.FormatInteractionPrompt("E: 충전"), Is.EqualTo("E: 충전"));
        }

        [Test]
        public void ShouldShowGasWarning_OnlyWhenNotSafe()
        {
            Assert.That(HudFormatter.ShouldShowGasWarning(GasRiskLevel.Safe), Is.False);
            Assert.That(HudFormatter.ShouldShowGasWarning(GasRiskLevel.Elevated), Is.True);
            Assert.That(HudFormatter.ShouldShowGasWarning(GasRiskLevel.Hazard), Is.True);
        }
    }
}
