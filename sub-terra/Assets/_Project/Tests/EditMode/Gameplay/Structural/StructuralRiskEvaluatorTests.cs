using NUnit.Framework;
using UnityEngine;

namespace SubTerra.Gameplay.Structural.Tests
{
    public sealed class StructuralRiskEvaluatorTests
    {
        private StructuralRiskSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<StructuralRiskSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
        }

        [TestCase(0f, StructuralRiskLevel.Stable)]
        [TestCase(30f, StructuralRiskLevel.Caution)]
        [TestCase(60f, StructuralRiskLevel.Danger)]
        [TestCase(90f, StructuralRiskLevel.CollapseImminent)]
        public void Evaluate_TransitionsThroughFourOrderedLevels(
            float miningImpact,
            StructuralRiskLevel expected)
        {
            StructuralRiskLevel risk = StructuralRiskEvaluator.Evaluate(
                miningImpact,
                0,
                0,
                settings);

            Assert.That(risk, Is.EqualTo(expected));
        }

        [Test]
        public void Evaluate_ReturnsStable_WhenSupportOffsetsImpactAndCeiling()
        {
            StructuralRiskLevel risk = StructuralRiskEvaluator.Evaluate(
                10f,
                1,
                35,
                settings);

            Assert.That(risk, Is.EqualTo(StructuralRiskLevel.Stable));
        }
    }
}
