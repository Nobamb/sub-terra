using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Inventory;
using SubTerra.App.Run;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.RunFailure;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;

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
            var input = Failure("same-frame", RunFailureCause.StructuralCollapse);

            Assert.That(fixture.Service.TryBegin(input, null, out _), Is.True);
            Assert.That(fixture.Service.Complete(input.failureToken, false), Is.True);
            var afterFirst = fixture.Inventory.State.CaptureFingerprint();

            Assert.That(fixture.Service.TryBegin(input, null, out _), Is.False);
            Assert.That(fixture.Inventory.State.CaptureFingerprint(), Is.EqualTo(afterFirst));
        }

        [Test]
        public void PromptB81_PowerDepleted_CannotEnterRunFailureService()
        {
            var fixture = CreateFixture();
            var before = fixture.Inventory.State.CaptureFingerprint();

            Assert.That(
                fixture.Service.TryBegin(
                    Failure("power-zero", RunFailureCause.PowerDepleted),
                    null,
                    out _),
                Is.False);
            Assert.That(fixture.State.Run.LifecyclePhase, Is.EqualTo(RunLifecyclePhase.Active));
            Assert.That(fixture.Inventory.State.CaptureFingerprint(), Is.EqualTo(before));
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

        [Test]
        public void PromptB148_RevealTimeline_SequentialStartAndEaseOutCountUp()
        {
            var lost = new List<CargoLossEntryDto>
            {
                new CargoLossEntryDto { mineralId = "mineral.copper", quantity = 10 },
                new CargoLossEntryDto { mineralId = "mineral.iron", quantity = 24 },
                new CargoLossEntryDto { mineralId = "mineral.gold_ore", quantity = 8 }
            };
            var timeline = new RunFailureRevealTimeline(
                RunFailureCause.StructuralCollapse,
                lost,
                0.6f);

            var initial = timeline.Evaluate(0f);
            Assert.That(initial.IsWindowGlitching, Is.True);
            Assert.That(initial.Items.Count, Is.EqualTo(3));
            Assert.That(initial.Items[0].StartTime, Is.LessThan(initial.Items[1].StartTime));
            Assert.That(initial.Items[1].StartTime, Is.LessThan(initial.Items[2].StartTime));

            // 각 항목의 수량 카운트업은 0에서 시작해 0.3~0.5초 안에 목표 수량에 도달한다.
            for (var i = 0; i < initial.Items.Count; i++)
            {
                var item = initial.Items[i];
                Assert.That(item.CountUpDuration, Is.InRange(0.30f, 0.50f));

                var atStart = timeline.Evaluate(item.StartTime);
                Assert.That(atStart.Items[i].DisplayQuantity, Is.EqualTo(0));
                Assert.That(atStart.Items[i].IsVisible, Is.True);

                var mid = timeline.Evaluate(item.StartTime + (item.CountUpDuration * 0.5f));
                Assert.That(mid.Items[i].DisplayQuantity, Is.GreaterThan(0));
                Assert.That(mid.Items[i].DisplayQuantity, Is.LessThanOrEqualTo(item.TargetQuantity));

                var atEnd = timeline.Evaluate(item.StartTime + item.CountUpDuration);
                Assert.That(atEnd.Items[i].DisplayQuantity, Is.EqualTo(item.TargetQuantity));
                Assert.That(atEnd.Items[i].IsCompleted, Is.True);
            }
        }

        [Test]
        public void PromptB148_RevealTimeline_SkipReachesFinalStateAndSecondConfirmAdvances()
        {
            var lost = new List<CargoLossEntryDto>
            {
                new CargoLossEntryDto { mineralId = "mineral.copper", quantity = 12 },
                new CargoLossEntryDto { mineralId = "mineral.iron", quantity = 7 }
            };
            var timeline = new RunFailureRevealTimeline(
                RunFailureCause.Fall,
                lost,
                0.6f);

            timeline.Advance(0.10f);
            Assert.That(timeline.IsComplete, Is.False);

            // 첫 확인/스킵 입력은 연출만 즉시 최종 상태로 건너뛰고 다음 단계로 넘어가지 않는다.
            Assert.That(timeline.TryConfirmOrSkip(), Is.False);
            Assert.That(timeline.IsSkipped, Is.True);
            Assert.That(timeline.IsComplete, Is.True);
            Assert.That(timeline.IsConfirmed, Is.False);

            var skippedFrame = timeline.Evaluate(0.10f);
            Assert.That(skippedFrame.IsWindowGlitching, Is.False);
            Assert.That(skippedFrame.Items[0].DisplayQuantity, Is.EqualTo(12));
            Assert.That(skippedFrame.Items[1].DisplayQuantity, Is.EqualTo(7));
            Assert.That(skippedFrame.Items[0].IsCompleted, Is.True);
            Assert.That(skippedFrame.Items[1].IsCompleted, Is.True);

            // 최종 상태에서 한 번 더 확인을 눌렀을 때만 다음 전환으로 넘어간다.
            Assert.That(timeline.TryConfirmOrSkip(), Is.True);
            Assert.That(timeline.IsConfirmed, Is.True);
        }

        [Test]
        public void PromptB148_RevealTimeline_EmptyLostCargo_ReturnsEmptyMessage()
        {
            var timeline = new RunFailureRevealTimeline(
                RunFailureCause.StructuralCollapse,
                new List<CargoLossEntryDto>(),
                0.75f);

            var frame = timeline.Evaluate(0f);
            Assert.That(frame.HasLostCargo, Is.False);
            Assert.That(frame.Items.Count, Is.Zero);
            Assert.That(frame.EmptyCargoText, Is.EqualTo("잃은 자원 없음"));
            Assert.That(frame.PreservationText, Is.EqualTo("드론 구조: 화물 75% 보존"));
        }

        [Test]
        public void PromptB148_RevealTimeline_SelectsGreenForGasAndRedForOthers()
        {
            var gasTimeline = new RunFailureRevealTimeline(
                RunFailureCause.GasExposure,
                null,
                0.6f);
            var collapseTimeline = new RunFailureRevealTimeline(
                RunFailureCause.StructuralCollapse,
                null,
                0.6f);
            var fallTimeline = new RunFailureRevealTimeline(
                RunFailureCause.Fall,
                null,
                0.6f);

            Assert.That(gasTimeline.GlitchColor.g, Is.GreaterThan(gasTimeline.GlitchColor.r));
            Assert.That(gasTimeline.GlitchColor.g, Is.GreaterThan(gasTimeline.GlitchColor.b));
            Assert.That(gasTimeline.CauseTitle, Is.EqualTo("가스 중독"));

            Assert.That(collapseTimeline.GlitchColor.r, Is.GreaterThan(collapseTimeline.GlitchColor.g));
            Assert.That(collapseTimeline.GlitchColor.r, Is.GreaterThan(collapseTimeline.GlitchColor.b));
            Assert.That(collapseTimeline.CauseTitle, Is.EqualTo("체력 소진"));
            Assert.That(fallTimeline.GlitchColor, Is.EqualTo(collapseTimeline.GlitchColor));
        }

        [Test]
        public void PromptB148_TransitionTimeline_WaitsForConfirmAndRunsDarkenGlitchTvUnderTwoSeconds()
        {
            var timeline = new RunFailureTransitionTimeline(
                RunFailureCause.GasExposure,
                null,
                false);

            Assert.That(timeline.TotalDuration, Is.LessThanOrEqualTo(2.0f));
            Assert.That(timeline.HeavyGlitchDuration, Is.InRange(0.50f, 0.70f));

            // 확인 전에는 다음 단계로 진행하지 않는다.
            var beforeConfirm = timeline.Advance(0.50f);
            Assert.That(beforeConfirm.Phase, Is.EqualTo(RunFailureTransitionPhase.NotStarted));
            Assert.That(timeline.ElapsedSeconds, Is.Zero);
            Assert.That(timeline.PlayerMoveCount, Is.Zero);

            timeline.BeginConfirm();
            var visitedPhases = new List<RunFailureTransitionPhase>();
            var moveTriggers = 0;

            while (!timeline.IsComplete)
            {
                var frame = timeline.Advance(0.05f);
                if (visitedPhases.Count == 0 || visitedPhases[visitedPhases.Count - 1] != frame.Phase)
                {
                    visitedPhases.Add(frame.Phase);
                }

                if (frame.ShouldMovePlayer)
                {
                    moveTriggers++;
                    Assert.That(frame.Phase, Is.EqualTo(RunFailureTransitionPhase.HeavyGlitch));
                    Assert.That(frame.BlackoutAlpha, Is.EqualTo(1f).Within(0.0001f));
                }
            }

            Assert.That(moveTriggers, Is.EqualTo(1));
            Assert.That(timeline.PlayerMoveCount, Is.EqualTo(1));
            Assert.That(
                visitedPhases,
                Is.EqualTo(new[]
                {
                    RunFailureTransitionPhase.Darken,
                    RunFailureTransitionPhase.HeavyGlitch,
                    RunFailureTransitionPhase.TvTurnOn,
                    RunFailureTransitionPhase.Completed
                }));
        }

        [Test]
        public void PromptB148_PanelView_RendersEmptyOrScrollableCargoAndHandlesSkipThenConfirm()
        {
            var canvasObj = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
            var panelObj = new GameObject("RunFailurePanel", typeof(RectTransform));
            panelObj.transform.SetParent(canvasObj.transform, false);
            var view = panelObj.AddComponent<RunFailurePanelView>();
            view.BuildOrUpdateHierarchy();

            try
            {
                Assert.That(view.HasRequiredReferences(), Is.True);
                var visualRoot = panelObj.transform.Find("VisualRoot") as RectTransform;
                Assert.That(visualRoot, Is.Not.Null);
                Assert.That(visualRoot.sizeDelta, Is.EqualTo(RunFailurePanelView.CardSize));
                Assert.That(visualRoot.Find("GlitchBands/Band_0"), Is.Not.Null);
                Assert.That(visualRoot.Find("WindowGlitchBars/WindowGlitchBar_0"), Is.Not.Null);
                Assert.That(visualRoot.Find("Content/TitleDivider"), Is.Not.Null);
                Assert.That(visualRoot.Find("Content/LostCargoSection"), Is.Not.Null);
                Assert.That(visualRoot.Find("Content/PreservationBadge"), Is.Not.Null);

                var emptyRescue = new PlayerRescueResultDto
                {
                    failureToken = "empty",
                    cause = RunFailureCause.GasExposure,
                    preservationRatio = 0.6f,
                    lostCargo = new List<CargoLossEntryDto>()
                };
                view.Show(emptyRescue);
                Assert.That(view.DisplayedCauseTitle, Is.EqualTo("가스 중독"));
                Assert.That(view.DisplayedEmptyCargoText, Is.EqualTo("잃은 자원 없음"));
                Assert.That(view.DisplayedPreservationText, Is.EqualTo("드론 구조: 화물 60% 보존"));
                Assert.That(view.ActiveCargoRowCount, Is.Zero);

                var multiRescue = new PlayerRescueResultDto
                {
                    failureToken = "multi",
                    cause = RunFailureCause.StructuralCollapse,
                    preservationRatio = 0.7f,
                    lostCargo = new List<CargoLossEntryDto>
                    {
                        new CargoLossEntryDto { mineralId = "mineral.copper", quantity = 10 },
                        new CargoLossEntryDto { mineralId = "mineral.iron", quantity = 15 },
                        new CargoLossEntryDto { mineralId = "mineral.silver", quantity = 6 },
                        new CargoLossEntryDto { mineralId = "mineral.gold_ore", quantity = 4 },
                        new CargoLossEntryDto { mineralId = "mineral.titanium", quantity = 3 },
                        new CargoLossEntryDto { mineralId = "mineral.cobalt", quantity = 5 }
                    }
                };
                view.Show(multiRescue);
                Assert.That(view.DisplayedCauseTitle, Is.EqualTo("체력 소진"));
                Assert.That(view.ActiveCargoRowCount, Is.EqualTo(6));
                Assert.That(view.IsRevealComplete, Is.False);
                Assert.That(view.IsConfirmed, Is.False);

                // 1차 입력: 스킵만 수행
                Assert.That(view.ConfirmOrSkip(), Is.False);
                Assert.That(view.IsRevealComplete, Is.True);
                Assert.That(view.IsConfirmed, Is.False);

                // 2차 입력: 확인 확정
                Assert.That(view.ConfirmOrSkip(), Is.True);
                Assert.That(view.IsConfirmed, Is.True);
                Assert.That(view.ActiveTransitionTimeline, Is.Not.Null);
                var darkenFrame = view.ActiveTransitionTimeline.Evaluate(0.05f);
                Assert.That(darkenFrame.Phase, Is.EqualTo(RunFailureTransitionPhase.Darken));
                Assert.That(darkenFrame.WindowGlitchIntensity, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(canvasObj);
            }
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
            public int GetGoldGainBonusPercent() => 0;
            public int GetMiningYieldBonus(string mineralId) => 0;
        }
    }
}
