using NUnit.Framework;

namespace SubTerra.Gameplay.Structural.Tests
{
    public sealed class StructuralFeedbackTests
    {
        [Test]
        public void CrackDensity_IncreasesAcrossWarningLevels()
        {
            int caution = StructuralCrackOverlay.GetVisibleCount(9, StructuralRiskLevel.Caution);
            int danger = StructuralCrackOverlay.GetVisibleCount(9, StructuralRiskLevel.Danger);
            int imminent = StructuralCrackOverlay.GetVisibleCount(9, StructuralRiskLevel.CollapseImminent);

            Assert.That(caution, Is.LessThan(danger));
            Assert.That(danger, Is.LessThan(imminent));
        }

        [Test]
        public void WarningPitch_DistinguishesAllWarningLevels()
        {
            float caution = StructuralRiskFeedback.GetPitch(StructuralRiskLevel.Caution);
            float danger = StructuralRiskFeedback.GetPitch(StructuralRiskLevel.Danger);
            float imminent = StructuralRiskFeedback.GetPitch(StructuralRiskLevel.CollapseImminent);

            Assert.That(caution, Is.LessThan(danger));
            Assert.That(danger, Is.LessThan(imminent));
        }

        [Test]
        public void ReducedMotion_DisablesCameraShakeRequest()
        {
            Assert.That(
                StructuralRiskFeedback.ShouldRequestCameraShake(
                    StructuralRiskLevel.CollapseImminent,
                    true),
                Is.False);
            Assert.That(
                StructuralRiskFeedback.ShouldRequestCameraShake(
                    StructuralRiskLevel.Danger,
                    false),
                Is.True);
        }

        [Test]
        public void ShakeAmplitude_ScalesWithRiskLevel()
        {
            Assert.That(
                StructuralRiskFeedback.ResolveShakeAmplitude(StructuralRiskLevel.CollapseImminent),
                Is.GreaterThan(
                    StructuralRiskFeedback.ResolveShakeAmplitude(StructuralRiskLevel.Danger)));
            Assert.That(
                StructuralRiskFeedback.ResolveShakeDuration(StructuralRiskLevel.Danger),
                Is.GreaterThan(0f));
        }
    }
}
