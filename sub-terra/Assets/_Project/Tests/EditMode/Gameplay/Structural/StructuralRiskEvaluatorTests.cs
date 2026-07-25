using NUnit.Framework;

namespace SubTerra.Gameplay.Structural.Tests
{
    public sealed class StructuralRiskEvaluatorTests
    {
        [Test]
        public void Evaluate_ReturnsStable_WhenSupportOffsetsMiningImpact()
        {
            StructuralRiskLevel risk = StructuralRiskEvaluator.Evaluate(40, 1, 35);

            Assert.That(risk, Is.EqualTo(StructuralRiskLevel.Stable));
        }

        [Test]
        public void Evaluate_ReturnsCaution_ForModerateUnsupportedCeiling()
        {
            StructuralRiskLevel risk = StructuralRiskEvaluator.Evaluate(20, 1, 0);

            Assert.That(risk, Is.EqualTo(StructuralRiskLevel.Caution));
        }

        [Test]
        public void Evaluate_ReturnsCritical_ForHighImpactAndUnsupportedTiles()
        {
            StructuralRiskLevel risk = StructuralRiskEvaluator.Evaluate(40, 2, 0);

            Assert.That(risk, Is.EqualTo(StructuralRiskLevel.Critical));
        }
    }
}
