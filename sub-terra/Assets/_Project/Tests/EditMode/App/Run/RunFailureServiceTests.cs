using NUnit.Framework;
using SubTerra.App.Inventory;
using SubTerra.App.Run;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;

namespace SubTerra.App.Tests.Run
{
    public sealed class RunFailureServiceTests
    {
        [Test]
        public void L_S01_FailureCauses_AreExplicit()
        {
            Assert.That((int)RunFailureCause.PowerDepleted, Is.Not.EqualTo(0));
            Assert.That((int)RunFailureCause.StructuralCollapse, Is.Not.EqualTo(0));
            Assert.That((int)RunFailureCause.GasExposure, Is.Not.EqualTo(0));
            Assert.That((int)RunFailureCause.Fall, Is.Not.EqualTo(0));
        }

        [Test]
        public void PromptB55_HealthRegenerationAndFallDamage_AreDeterministic()
        {
            var state = new PlayerSurvivalState(100, 1f);
            Assert.That(state.TryApplyDamage(
                RunFailureCause.Fall, 20, 1f, 0f, false, out _), Is.True);
            Assert.That(state.AdvanceRegeneration(5f), Is.True);
            Assert.That(state.Health, Is.EqualTo(85f).Within(0.0001f));

            Assert.That(PlayerFallDamageRules.CalculateDamage(9.99f, false), Is.Zero);
            Assert.That(PlayerFallDamageRules.CalculateDamage(10f, false), Is.EqualTo(10));
            Assert.That(PlayerFallDamageRules.CalculateDamage(14.9f, false), Is.EqualTo(14));
            Assert.That(PlayerFallDamageRules.CalculateDamage(30f, true), Is.Zero);

            Assert.That(state.ApplyUpgradeEffects(130, 0.3f), Is.True);
            Assert.That(state.MaximumHealth, Is.EqualTo(130));
            Assert.That(state.Health, Is.EqualTo(115f).Within(0.0001f));
        }

        [Test]
        public void PromptB68_FallDamage_ScalesWithCargoImpactMultiplier()
        {
            Assert.That(
                CargoLoadEffectPolicy.EvaluateJumpMultiplier(10f, 50f),
                Is.EqualTo(0.95f).Within(0.0001f));
            Assert.That(
                CargoLoadEffectPolicy.EvaluateFallImpactMultiplier(10f, 50f),
                Is.EqualTo(1.1f).Within(0.0001f));
            Assert.That(
                CargoLoadEffectPolicy.EvaluateJumpMultiplier(50f, 50f),
                Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(
                CargoLoadEffectPolicy.EvaluateFallImpactMultiplier(50f, 50f),
                Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(PlayerFallDamageRules.ScaleDamage(10, 1f), Is.EqualTo(10));
            Assert.That(PlayerFallDamageRules.ScaleDamage(10, 1.1f), Is.EqualTo(11));
            Assert.That(PlayerFallDamageRules.ScaleDamage(10, 1.5f), Is.EqualTo(15));
        }

        [Test]
        public void L_S02_PlayerSurvival_UsesHealthActionAndInvulnerability()
        {
            var state = new PlayerSurvivalState(100);

            Assert.That(state.TryApplyDamage(
                RunFailureCause.StructuralCollapse, 25, 1f, 0.75f, false, out _), Is.True);
            Assert.That(state.Health, Is.EqualTo(75));
            Assert.That(state.TryApplyDamage(
                RunFailureCause.StructuralCollapse, 25, 1.5f, 0.75f, false, out _), Is.False);
            Assert.That(state.Health, Is.EqualTo(75));

            Assert.That(state.TryApplyDamage(
                RunFailureCause.GasExposure, 0, 2f, 0.75f, true, out var incapacitated), Is.True);
            Assert.That(incapacitated, Is.True);
            Assert.That(state.Health, Is.Zero);
            Assert.That(state.CanAct, Is.False);
        }

        [Test]
        public void PromptB55_1_DamageShakeScalesWithAppliedHealthDamage()
        {
            float lightHit = PlayerSurvivalController.ResolveDamageShakeAmplitude(10f, 100);
            float heavyHit = PlayerSurvivalController.ResolveDamageShakeAmplitude(50f, 100);

            Assert.That(heavyHit, Is.GreaterThan(lightHit));
        }

        [TestCase(RunFailureCause.PowerDepleted)]
        [TestCase(RunFailureCause.StructuralCollapse)]
        [TestCase(RunFailureCause.GasExposure)]
        public void L_F01_AllCauses_EnterOneFailureService(RunFailureCause cause)
        {
            var fixture = CreateFixture();
            var input = Failure("cause:" + cause, cause);

            Assert.That(fixture.Service.TryBegin(input, null, out var result), Is.True);
            Assert.That(result.Input.cause, Is.EqualTo(cause));
            Assert.That(fixture.State.Run.LifecyclePhase, Is.EqualTo(RunLifecyclePhase.Returning));
        }

        [Test]
        public void L_F02_CargoLoss_IsDeterministicAndPreservesGold()
        {
            var left = CreateFixture();
            var right = CreateFixture();
            left.State.SetGold(321);
            right.State.SetGold(321);

            Assert.That(left.Service.TryBegin(Failure("left", RunFailureCause.GasExposure), null, out var a), Is.True);
            Assert.That(right.Service.TryBegin(Failure("right", RunFailureCause.GasExposure), null, out var b), Is.True);

            Assert.That(a.CargoLoss.LostValue, Is.EqualTo(b.CargoLoss.LostValue));
            Assert.That(a.CargoLoss.Entries.Count, Is.EqualTo(b.CargoLoss.Entries.Count));
            Assert.That(left.Inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(6));
            Assert.That(left.Inventory.State.GetQuantity("mineral.iron"), Is.EqualTo(6));
            Assert.That(left.State.Player.Gold, Is.EqualTo(321));
            Assert.That(a.CargoLoss.PreservationRatio, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void L_F03_DroneUpgrade_PreservesMoreCargoThroughSharedProvider()
        {
            var baseFixture = CreateFixture(new FixedUpgradeEffects(0f));
            var upgradedFixture = CreateFixture(new FixedUpgradeEffects(0.3f));

            baseFixture.Service.TryBegin(Failure("base", RunFailureCause.StructuralCollapse), null, out var baseline);
            upgradedFixture.Service.TryBegin(Failure("upgrade", RunFailureCause.StructuralCollapse), null, out var upgraded);

            Assert.That(upgraded.CargoLoss.LostValue, Is.LessThan(baseline.CargoLoss.LostValue));
            Assert.That(upgraded.CargoLoss.PreservationRatio, Is.EqualTo(0.9f).Within(0.0001f));
            Assert.That(upgradedFixture.Inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(9));
            Assert.That(upgradedFixture.Inventory.State.GetQuantity("mineral.iron"), Is.EqualTo(9));
        }

        [Test]
        public void L_F04_DuplicateFailureToken_CannotLoseCargoTwice()
        {
            var fixture = CreateFixture();
            var input = Failure("same-frame", RunFailureCause.PowerDepleted);

            Assert.That(fixture.Service.TryBegin(input, null, out _), Is.True);
            Assert.That(fixture.Service.Complete(input.failureToken, false), Is.True);
            var afterFirst = fixture.Inventory.State.CaptureFingerprint();

            Assert.That(fixture.Service.TryBegin(input, null, out _), Is.False);
            Assert.That(fixture.Inventory.State.CaptureFingerprint(), Is.EqualTo(afterFirst));
        }

        [Test]
        public void L_F05_ActiveCheckpointWins_OtherwiseSurfaceFallback()
        {
            var checkpointFixture = CreateFixture();
            var checkpoint = new OutpostStatusDto
            {
                isActive = true,
                checkpointId = "checkpoint.deep.01",
                checkpointX = 4,
                checkpointY = -20
            };
            checkpointFixture.Service.TryBegin(
                Failure("checkpoint", RunFailureCause.StructuralCollapse),
                checkpoint,
                out var atCheckpoint);

            Assert.That(atCheckpoint.ReturnTarget.Kind, Is.EqualTo(RunReturnTargetKind.OutpostCheckpoint));
            Assert.That(atCheckpoint.Rescue.usedCheckpoint, Is.True);
            Assert.That(atCheckpoint.ReturnTarget.X, Is.EqualTo(4));
            Assert.That(atCheckpoint.ReturnTarget.Y, Is.EqualTo(-20));

            var fallbackFixture = CreateFixture();
            fallbackFixture.Service.TryBegin(
                Failure("fallback", RunFailureCause.StructuralCollapse),
                new OutpostStatusDto { isActive = false, checkpointId = "inactive" },
                out var fallback);
            Assert.That(fallback.ReturnTarget.Kind, Is.EqualTo(RunReturnTargetKind.SurfaceFallback));
            Assert.That(fallback.Rescue.usedCheckpoint, Is.False);
        }

        private static Fixture CreateFixture(IUpgradeEffectProvider effects = null)
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1f, 10, "Copper");
            catalog.Register("mineral.iron", 2f, 20, "Iron");
            var state = GameState.CreateNew();
            state.BeginRun();
            var inventory = new InventoryService(catalog, 100f, state);
            inventory.TryAddMineral("mineral.copper", 10);
            inventory.TryAddMineral("mineral.iron", 10);
            return new Fixture(
                state,
                inventory,
                new RunFailureService(state, inventory, effects, 0.4f));
        }

        private static RunFailureInputDto Failure(string token, RunFailureCause cause)
        {
            return new RunFailureInputDto
            {
                failureToken = token,
                cause = cause,
                sourceId = "test",
                remainingHealth = 0
            };
        }

        private sealed class Fixture
        {
            public GameState State { get; }
            public InventoryService Inventory { get; }
            public RunFailureService Service { get; }

            public Fixture(GameState state, InventoryService inventory, RunFailureService service)
            {
                State = state;
                Inventory = inventory;
                Service = service;
            }
        }

        private sealed class FixedUpgradeEffects : IUpgradeEffectProvider
        {
            private readonly float rescueBonus;

            public FixedUpgradeEffects(float bonus)
            {
                rescueBonus = bonus;
            }

            public int GetDrillLevel() => 0;
            public float GetDrillSpeedMultiplier() => 1f;
            public float GetEnergyEfficiencyMultiplier() => 1f;
            public int GetMaximumEnergy(int baseMaximum) => baseMaximum;
            public float GetMaximumCargoWeight(float baseMaximum) => baseMaximum;
            public float GetDroneScanRadius(float baseRadius) => baseRadius;
            public float GetDroneRescuePreservation(float basePreservation) =>
                basePreservation + rescueBonus;
            public float GetGasResistance() => 0f;
        }
    }
}
