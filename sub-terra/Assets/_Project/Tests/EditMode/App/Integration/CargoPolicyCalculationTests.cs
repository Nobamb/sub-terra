using NUnit.Framework;
using SubTerra.Gameplay.Player;

namespace SubTerra.Gameplay.Player.Tests
{
    public sealed class CargoPolicyCalculationTests
    {
        [TestCase(49f, 100f, CargoSpeedPolicy.LightLoadMultiplier)]
        [TestCase(50f, 100f, CargoSpeedPolicy.MediumLoadMultiplier)]
        [TestCase(79.9f, 100f, CargoSpeedPolicy.MediumLoadMultiplier)]
        [TestCase(80f, 100f, CargoSpeedPolicy.HeavyLoadMultiplier)]
        [TestCase(100f, 100f, CargoSpeedPolicy.HeavyLoadMultiplier)]
        public void E_F04_CargoWeight_UsesThreePrdSpeedSteps(
            float current,
            float maximum,
            float expected)
        {
            Assert.AreEqual(expected, CargoSpeedPolicy.Evaluate(current, maximum), 0.0001f);
        }

        [TestCase(0f, 50f, 1f, 1f)]
        [TestCase(10f, 50f, 0.95f, 1.1f)]
        [TestCase(50f, 50f, 0.75f, 1.5f)]
        [TestCase(75f, 50f, 0.75f, 1.5f)]
        public void PromptB68_CargoLoadEffects_ScaleLinearlyAndClamp(
            float current,
            float maximum,
            float expectedJump,
            float expectedFallImpact)
        {
            Assert.That(
                CargoLoadEffectPolicy.EvaluateJumpMultiplier(current, maximum),
                Is.EqualTo(expectedJump).Within(0.0001f));
            Assert.That(
                CargoLoadEffectPolicy.EvaluateFallImpactMultiplier(current, maximum),
                Is.EqualTo(expectedFallImpact).Within(0.0001f));
        }

    }
}
