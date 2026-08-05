using SubTerra.Gameplay.Drone;
using SubTerra.Shared;
using NUnit.Framework;
using UnityEngine;

namespace SubTerra.Gameplay.Tests.Drone
{
    public sealed class DroneSensorUpgradeTests
    {
        [Test]
        public void DroneScanUpgrade_ChangesActualSensorRadius()
        {
            var host = new GameObject("DroneSensorUpgradeTests");
            try
            {
                var sensor = host.AddComponent<DroneSensor>();
                sensor.SetUpgradeEffects(new FixedEffects(7f));
                Assert.That(sensor.EffectiveMineralScanRadius, Is.EqualTo(7));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private sealed class FixedEffects : IUpgradeEffectProvider
        {
            private readonly float scanRadius;
            public FixedEffects(float scanRadius) => this.scanRadius = scanRadius;
            public int GetDrillLevel() => 0;
            public float GetDrillSpeedMultiplier() => 1f;
            public float GetEnergyEfficiencyMultiplier() => 1f;
            public int GetMaximumEnergy(int baseMaximum) => baseMaximum;
            public float GetMaximumCargoWeight(float baseMaximum) => baseMaximum;
            public float GetDroneScanRadius(float baseRadius) => scanRadius;
            public float GetDroneRescuePreservation(float basePreservation) => basePreservation;
            public float GetGasResistance() => 0f;
        }
    }
}
