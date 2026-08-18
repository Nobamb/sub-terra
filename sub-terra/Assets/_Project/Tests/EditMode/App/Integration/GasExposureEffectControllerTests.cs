using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.State;
using SubTerra.Gameplay.Hazards;
using SubTerra.Shared;
using UnityEngine;
using GameplayGasRiskLevel = SubTerra.Gameplay.Hazards.GasRiskLevel;

namespace SubTerra.App.Tests.Integration
{
    public sealed class GasExposureEffectControllerTests
    {
        [Test]
        public void H_F01_ControllerDrainsGameStateWithoutGoingBelowZero()
        {
            var gameObject = new GameObject("GasEffectController");
            try
            {
                var controller = gameObject.AddComponent<GasExposureEffectController>();
                var state = GameState.CreateNew();
                controller.Bind(state, new FixedUpgradeEffects(0f));
                controller.ApplyExposure(CriticalExposure());

                controller.Advance(100f);

                Assert.That(state.Player.Energy, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PromptB55_GasProducesTenHealthDamagePerSecond()
        {
            var gameObject = new GameObject("GasEffectController");
            try
            {
                var controller = gameObject.AddComponent<GasExposureEffectController>();
                controller.Bind(GameState.CreateNew(), new FixedUpgradeEffects(0f));
                controller.ApplyExposure(CriticalExposure(1f));
                GasExposureFailureInputDto captured = null;
                var count = 0;
                controller.FailureInputRaised += input =>
                {
                    captured = input;
                    count++;
                };

                controller.Advance(10f);
                controller.Advance(5f);

                Assert.That(count, Is.EqualTo(2));
                Assert.That(captured.severity, Is.EqualTo(GasExposureFailureSeverity.Damage));
                Assert.That(captured.damage, Is.EqualTo(50));
                Assert.That(captured.gasZoneId, Is.EqualTo("gas-controller"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void H_F04_OutpostRangeImmediatelySheltersPlayer()
        {
            var gameObject = new GameObject("GasEffectController");
            try
            {
                var controller = gameObject.AddComponent<GasExposureEffectController>();
                controller.Bind(GameState.CreateNew(), new FixedUpgradeEffects(0f));
                controller.ApplyExposure(CriticalExposure());

                controller.ApplyOutpostStatus(new OutpostStatusDto
                {
                    isActive = true,
                    isInInteractionRange = true
                });

                Assert.That(controller.CurrentState.IsSheltered, Is.True);
                Assert.That(controller.CurrentState.Risk, Is.EqualTo(GameplayGasRiskLevel.Safe));
                Assert.That(controller.Advance(5f).EnergyDrain, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void H_F04_InactiveOutpostDoesNotCancelAnotherOutpostShelter()
        {
            var gameObject = new GameObject("GasEffectController");
            try
            {
                var controller = gameObject.AddComponent<GasExposureEffectController>();
                controller.Bind(GameState.CreateNew(), new FixedUpgradeEffects(0f));
                controller.ApplyExposure(CriticalExposure());
                controller.ApplyOutpostStatus(new OutpostStatusDto
                {
                    outpostInstanceId = "outpost-a",
                    isActive = true,
                    isInInteractionRange = true
                });

                controller.ApplyOutpostStatus(new OutpostStatusDto
                {
                    outpostInstanceId = "outpost-b",
                    isActive = false,
                    isInInteractionRange = false
                });

                Assert.That(controller.CurrentState.IsSheltered, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static GasExposureState CriticalExposure(float intensity = 0.8f)
        {
            return new GasExposureState(
                true,
                GameplayGasRiskLevel.Critical,
                GasType.Toxic,
                "gas-controller",
                30f,
                intensity);
        }

        private sealed class FixedUpgradeEffects : IUpgradeEffectProvider
        {
            private readonly float resistance;

            public FixedUpgradeEffects(float gasResistance)
            {
                resistance = gasResistance;
            }

            public int GetDrillLevel() => 0;
            public float GetDrillSpeedMultiplier() => 1f;
            public float GetEnergyEfficiencyMultiplier() => 1f;
            public int GetMaximumEnergy(int baseMaximum) => baseMaximum;
            public float GetMaximumCargoWeight(float baseMaximum) => baseMaximum;
            public float GetDroneScanRadius(float baseRadius) => baseRadius;
            public float GetDroneRescuePreservation(float basePreservation) => basePreservation;
            public float GetGasResistance() => resistance;
        }
    }
}
