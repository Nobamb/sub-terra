using NUnit.Framework;

namespace SubTerra.Gameplay.Hazards.Tests
{
    public sealed class GasRiskEvaluatorTests
    {
        [TestCase(0f, GasRiskLevel.Safe)]
        [TestCase(0.25f, GasRiskLevel.Caution)]
        [TestCase(0.7f, GasRiskLevel.Critical)]
        public void Evaluate_ReturnsExpectedRisk(float intensity, GasRiskLevel expected)
        {
            Assert.That(GasRiskEvaluator.Evaluate(intensity), Is.EqualTo(expected));
        }

        [Test]
        public void ClampIntensity_ClampsOutsideRange()
        {
            Assert.That(GasRiskEvaluator.ClampIntensity(2f), Is.EqualTo(1f));
            Assert.That(GasRiskEvaluator.ClampIntensity(-1f), Is.EqualTo(0f));
        }

        [Test]
        public void ExposureState_UsesZoneIdAsPartOfItsIdentity()
        {
            var first = new GasExposureState(true, GasRiskLevel.Caution, GasType.Toxic, "gas-0001", 5f);
            var second = new GasExposureState(true, GasRiskLevel.Caution, GasType.Toxic, "gas-0002", 5f);

            Assert.That(first.Equals(second), Is.False);
        }
    }
}
