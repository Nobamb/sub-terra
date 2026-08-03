using NUnit.Framework;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Hazards.Tests
{
    public sealed class GasExposureEffectModelTests
    {
        private static GasExposureState CriticalExposure(float intensity = 0.8f)
        {
            return new GasExposureState(
                true,
                GasRiskLevel.Critical,
                GasType.Toxic,
                "gas-test",
                30f,
                intensity);
        }

        [Test]
        public void H_F01_EnergyAndExposureAdvanceOnlyAtFixedTicks()
        {
            var model = new GasExposureEffectModel();
            model.SetExposure(CriticalExposure(), 0f, false);

            var beforeTick = model.Advance(0.99f);
            var firstTick = model.Advance(0.01f);
            var secondTick = model.Advance(1f);

            Assert.That(beforeTick.EnergyDrain, Is.Zero);
            Assert.That(beforeTick.State.CumulativeExposure, Is.Zero);
            Assert.That(firstTick.EnergyDrain, Is.EqualTo(1));
            Assert.That(firstTick.State.CumulativeExposure, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(secondTick.EnergyDrain, Is.EqualTo(2));
        }

        [Test]
        public void H_F02_ExitImmediatelyRestoresMovementAndVisionThenRecoversExposure()
        {
            var model = new GasExposureEffectModel();
            model.SetExposure(CriticalExposure(), 0f, false);
            var exposed = model.Advance(2f).State;

            var exited = model.SetExposure(default, 0f, false);
            var recovered = model.Advance(1f).State;

            Assert.That(exposed.SpeedMultiplier, Is.LessThan(1f));
            Assert.That(exposed.VisionObscuration, Is.GreaterThan(0f));
            Assert.That(exited.SpeedMultiplier, Is.EqualTo(1f));
            Assert.That(exited.VisionObscuration, Is.Zero);
            Assert.That(recovered.CumulativeExposure, Is.LessThan(exited.CumulativeExposure));
        }

        [Test]
        public void H_F04_ResistanceReducesEveryEffectFromCatalogValue()
        {
            var unprotected = new GasExposureEffectModel();
            var protectedModel = new GasExposureEffectModel();
            unprotected.SetExposure(CriticalExposure(), 0f, false);
            protectedModel.SetExposure(CriticalExposure(), 0.3f, false);

            var raw = unprotected.Advance(5f);
            var protectedResult = protectedModel.Advance(5f);

            Assert.That(protectedResult.EnergyDrain, Is.LessThan(raw.EnergyDrain));
            Assert.That(protectedResult.State.CumulativeExposure,
                Is.LessThan(raw.State.CumulativeExposure));
            Assert.That(protectedResult.State.SpeedMultiplier,
                Is.GreaterThan(raw.State.SpeedMultiplier));
            Assert.That(protectedResult.State.VisionObscuration,
                Is.LessThan(raw.State.VisionObscuration));
        }

        [Test]
        public void H_F04_ActiveOutpostShelterNeutralizesEffects()
        {
            var model = new GasExposureEffectModel();
            var state = model.SetExposure(CriticalExposure(), 0f, true);
            var tick = model.Advance(5f);

            Assert.That(state.IsSheltered, Is.True);
            Assert.That(state.Risk, Is.EqualTo(GasRiskLevel.Safe));
            Assert.That(state.SpeedMultiplier, Is.EqualTo(1f));
            Assert.That(state.VisionObscuration, Is.Zero);
            Assert.That(tick.EnergyDrain, Is.Zero);
            Assert.That(tick.State.CumulativeExposure, Is.Zero);
        }

        [Test]
        public void H_F03_OverlappingZonesUseHighestIntensityOnce()
        {
            var root = new GameObject("GasSystem");
            var player = new GameObject("Player");
            try
            {
                var system = root.AddComponent<GasHazardSystem>();
                system.RestoreGasZone(new GasSnapshotDto
                {
                    gasZoneId = "low",
                    gasTypeId = GasType.Toxic.ToString(),
                    concentrationLevel = 0.3f,
                    isActive = true
                });
                system.RestoreGasZone(new GasSnapshotDto
                {
                    gasZoneId = "high",
                    gasTypeId = GasType.Toxic.ToString(),
                    concentrationLevel = 0.9f,
                    isActive = true
                });

                system.SetPlayerTransform(player.transform);

                Assert.That(system.CurrentExposure.GasZoneId, Is.EqualTo("high"));
                Assert.That(system.CurrentExposure.Intensity, Is.EqualTo(0.9f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(player);
            }
        }
    }
}
