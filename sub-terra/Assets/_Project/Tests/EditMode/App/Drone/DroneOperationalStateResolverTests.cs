using NUnit.Framework;
using SubTerra.App.Drone;

namespace SubTerra.App.Tests.Drone
{
    public sealed class DroneOperationalStateResolverTests
    {
        [Test]
        public void ResolvesIdleWhenNoPulseOrUrgentAnalysisExists()
        {
            Assert.That(
                DroneOperationalStateResolver.Resolve(false, false),
                Is.EqualTo(DroneOperationalState.Idle));
        }

        [Test]
        public void ResolvesScanningWhileScanPulseIsVisible()
        {
            Assert.That(
                DroneOperationalStateResolver.Resolve(false, true),
                Is.EqualTo(DroneOperationalState.Scanning));
        }

        [Test]
        public void PriorityOverridesAnActiveScanPulse()
        {
            Assert.That(
                DroneOperationalStateResolver.Resolve(true, true),
                Is.EqualTo(DroneOperationalState.Priority));
        }
    }
}
