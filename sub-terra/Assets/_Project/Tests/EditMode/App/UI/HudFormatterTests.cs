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
        public void FormatDefaults_ZeroNullAndNone()
        {
            Assert.That(HudFormatter.FormatGold(0), Is.EqualTo("0"));
            Assert.That(HudFormatter.FormatGold(-3), Is.EqualTo("0"));
            Assert.That(HudFormatter.FormatDepth(0), Is.EqualTo("0"));
            Assert.That(HudFormatter.FormatCargo(0f), Is.EqualTo("0"));
            Assert.That(HudFormatter.FormatUnsettledValue(0f), Is.EqualTo("0"));
            Assert.That(HudFormatter.FormatStructuralRisk(StructuralRiskLevel.Safe), Is.EqualTo(HudFormatter.LabelSafe));
            Assert.That(HudFormatter.FormatGasRisk(GasRiskLevel.Safe), Is.EqualTo(HudFormatter.LabelSafe));
            Assert.That(HudFormatter.FormatBuildingSelection(null, null), Is.EqualTo(HudFormatter.DefaultBuildingNone));
            Assert.That(HudFormatter.FormatBuildingSelection(string.Empty, string.Empty),
                Is.EqualTo(HudFormatter.DefaultBuildingNone));
            Assert.That(HudFormatter.FormatInteractionPrompt(null), Is.EqualTo(HudFormatter.DefaultInteractionEmpty));
            Assert.That(HudFormatter.FormatInteractionPrompt(string.Empty),
                Is.EqualTo(HudFormatter.DefaultInteractionEmpty));
        }

        [Test]
        public void FormatRiskAndSelection_UsesDefinedLabels()
        {
            Assert.That(HudFormatter.FormatStructuralRisk(StructuralRiskLevel.Caution),
                Is.EqualTo(HudFormatter.LabelCaution));
            Assert.That(HudFormatter.FormatStructuralRisk(StructuralRiskLevel.Critical),
                Is.EqualTo(HudFormatter.LabelCritical));
            Assert.That(HudFormatter.FormatGasRisk(GasRiskLevel.Elevated),
                Is.EqualTo(HudFormatter.LabelGasElevated));
            Assert.That(HudFormatter.FormatGasRisk(GasRiskLevel.Hazard),
                Is.EqualTo(HudFormatter.LabelGasHazard));
            Assert.That(HudFormatter.FormatBuildingSelection("building.support.basic", "기본 버팀목"),
                Is.EqualTo("기본 버팀목"));
            Assert.That(HudFormatter.FormatBuildingSelection("building.support.basic", null),
                Is.EqualTo("building.support.basic"));
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
