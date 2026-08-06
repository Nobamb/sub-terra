using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.Drone
{
    public sealed class DroneAnalysisServiceTests
    {
        private DroneAnalysisSettings settings;
        private DroneAnalysisService service;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<DroneAnalysisSettings>();
            settings.EditorSetDefaults();
            service = new DroneAnalysisService(settings);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void I_S01_AllSevenCandidates_AreAlwaysPresent()
        {
            var result = service.Analyze(SafeContext());

            Assert.That(
                result.Candidates.Select(candidate => candidate.Action),
                Is.EquivalentTo(new[]
                {
                    DroneAction.ReturnToBase,
                    DroneAction.InstallSupport,
                    DroneAction.LeaveGasZone,
                    DroneAction.MineNearbyMineral,
                    DroneAction.BuildOutpost,
                    DroneAction.ContinueDescending,
                    DroneAction.Recharge
                }));
        }

        [Test]
        public void I_F01_GasRisk_BeatsLowEnergyAndLithium_WithActualFact()
        {
            var context = SafeContext();
            context.currentEnergy = 10;
            context.returnEnergyEstimate = 12;
            context.gasRisk = 0.7f;
            context.nearbyMineralIds.Add(DataIds.Minerals.Lithium);

            var result = service.Analyze(context);

            Assert.That(result.RecommendedAction, Is.EqualTo(DroneAction.LeaveGasZone));
            Assert.That(result.Recommendation.Score, Is.EqualTo(50));
            Assert.That(result.Recommendation.Reasons.Single().ActualValue, Is.EqualTo(0.7d).Within(0.001d));
            Assert.That(result.Dialogue.TemplateId, Is.EqualTo(DataIds.Dialogue.DroneGasWarning));
        }

        [Test]
        public void I_F02_ReturnScore_AggregatesEnergyAndCargoReasons()
        {
            var context = SafeContext();
            context.currentEnergy = 20;
            context.returnEnergyEstimate = 18;
            context.unsettledCargoValue = 250;

            var result = service.Analyze(context);
            var candidate = result.FindCandidate(DroneAction.ReturnToBase);

            Assert.That(candidate.Score, Is.EqualTo(60));
            Assert.That(
                candidate.Reasons.Select(reason => reason.Id),
                Is.EqualTo(new[] { "low_energy", "valuable_cargo" }));
        }

        [Test]
        public void I_F03_TiedScores_UseFixedPriority_AcrossNewServices()
        {
            var context = SafeContext();
            context.unsettledCargoValue = 100;
            context.nearbyMineralIds.Add(DataIds.Minerals.Lithium);

            for (var i = 0; i < 100; i++)
            {
                var next = new DroneAnalysisService(settings).Analyze(context);
                Assert.That(next.RecommendedAction, Is.EqualTo(DroneAction.ReturnToBase));
                Assert.That(next.Recommendation.Score, Is.EqualTo(20));
            }
        }

        [Test]
        public void DialoguePriority_CollapseImminent_BeatsGasAndPower()
        {
            var context = SafeContext();
            context.structuralIntegrity = 0.1f;
            context.gasRisk = 1f;
            context.currentEnergy = 0;
            context.returnEnergyEstimate = 10;

            var result = service.Analyze(context);

            Assert.That(
                result.Dialogue.TemplateId,
                Is.EqualTo(DataIds.Dialogue.DroneStructuralWarning));
            Assert.That(result.Dialogue.IsUrgent, Is.True);
        }

        [Test]
        public void K_F01_InventoryFull_UsesActualCapacityAndReturnTemplate()
        {
            var context = SafeContext();
            context.cargoWeight = 50f;
            context.maxCargoWeight = 50f;

            var result = service.Analyze(context);

            Assert.That(result.RecommendedAction, Is.EqualTo(DroneAction.ReturnToBase));
            Assert.That(result.Dialogue.TemplateId, Is.EqualTo(DataIds.Dialogue.DroneCargoFull));
            Assert.That(result.Dialogue.Tokens["cargoWeight"], Is.EqualTo("50"));
            Assert.That(result.Dialogue.Tokens["maxCargoWeight"], Is.EqualTo("50"));
            Assert.That(
                result.Recommendation.Reasons.Single().ActualValue,
                Is.EqualTo(50d));
        }

        [Test]
        public void K_F02_GasEntry_IsUrgentBeforeRegularExploration()
        {
            var context = SafeContext();
            context.gasRisk = 0.5f;

            var result = service.Analyze(context);

            Assert.That(result.Dialogue.TemplateId, Is.EqualTo(DataIds.Dialogue.DroneGasWarning));
            Assert.That(result.Dialogue.IsUrgent, Is.True);
        }

        [Test]
        public void K_S02_AllRequiredSituationTriggers_MapToTemplates()
        {
            var gas = SafeContext();
            gas.gasRisk = 0.5f;
            var collapse = SafeContext();
            collapse.structuralIntegrity = 0f;
            var cargo = SafeContext();
            cargo.cargoWeight = cargo.maxCargoWeight;
            var power = SafeContext();
            power.currentEnergy = 10;
            power.returnEnergyEstimate = 10;
            var rare = SafeContext();
            rare.nearbyMineralIds.Add(DataIds.Minerals.Lithium);
            var returning = SafeContext();
            returning.unsettledCargoValue = settings.HighCargoValueThreshold;

            Assert.That(service.Analyze(gas).Dialogue.TemplateId, Is.EqualTo(DataIds.Dialogue.DroneGasWarning));
            Assert.That(service.Analyze(collapse).Dialogue.TemplateId, Is.EqualTo(DataIds.Dialogue.DroneStructuralWarning));
            Assert.That(service.Analyze(cargo).Dialogue.TemplateId, Is.EqualTo(DataIds.Dialogue.DroneCargoFull));
            Assert.That(service.Analyze(power).Dialogue.TemplateId, Is.EqualTo(DataIds.Dialogue.LowPowerWarning));
            Assert.That(service.Analyze(rare).Dialogue.TemplateId, Is.EqualTo(DataIds.Dialogue.DroneLithium));
            Assert.That(service.Analyze(returning).Dialogue.TemplateId, Is.EqualTo(DataIds.Dialogue.DroneReturn));
        }

        [Test]
        public void I_F05_NullAndUnknownContext_UseSafeFallbackWithoutInventedValue()
        {
            var missing = service.Analyze(null);
            Assert.That(missing.RecommendedAction, Is.EqualTo(DroneAction.ReturnToBase));
            Assert.That(missing.UsedFallback, Is.True);
            Assert.That(double.IsNaN(missing.Recommendation.Reasons[0].ActualValue), Is.True);

            var partial = new DroneContextDto
            {
                depth = -1,
                currentEnergy = -1,
                returnEnergyEstimate = -1,
                structuralIntegrity = -1f,
                gasRisk = -1f,
                unsettledCargoValue = -1,
                cargoWeight = -1f,
                nearestBaseDistance = -1f,
                nearbyMineralIds = null,
                returnPathAvailable = false
            };

            Assert.DoesNotThrow(() => service.Analyze(partial));
            var partialResult = service.Analyze(partial);
            Assert.That(partialResult.RecommendedAction, Is.EqualTo(DroneAction.ReturnToBase));
            Assert.That(partialResult.UsedFallback, Is.True);
            Assert.That(
                partialResult.Recommendation.Reasons.Any(
                    reason => !double.IsNaN(reason.ActualValue)),
                Is.False);
            Assert.That(
                partialResult.Dialogue.Tokens.Values,
                Has.None.EqualTo("-1"));
        }

        [Test]
        public void RechargeAndOutpostCandidates_UseContextDistanceAndDepth()
        {
            var recharge = SafeContext();
            recharge.currentEnergy = 5;
            recharge.returnEnergyEstimate = 10;
            recharge.nearestBaseDistance = 4f;
            Assert.That(
                service.Analyze(recharge).RecommendedAction,
                Is.EqualTo(DroneAction.Recharge));

            var outpost = SafeContext();
            outpost.depth = 80;
            outpost.nearestBaseDistance = 60f;
            Assert.That(
                service.Analyze(outpost).RecommendedAction,
                Is.EqualTo(DroneAction.BuildOutpost));
        }

        [Test]
        public void StructuralWarning_UsesSupportDialogue_AndUnavailablePathStopsDescent()
        {
            var structural = SafeContext();
            structural.structuralIntegrity = 0.4f;
            var structuralResult = service.Analyze(structural);
            Assert.That(
                structuralResult.RecommendedAction,
                Is.EqualTo(DroneAction.InstallSupport));
            Assert.That(
                structuralResult.Dialogue.TemplateId,
                Is.EqualTo(DataIds.Dialogue.DroneStructuralWarning));

            var noPath = SafeContext();
            noPath.returnPathAvailable = false;
            Assert.That(
                service.Analyze(noPath).RecommendedAction,
                Is.EqualTo(DroneAction.BuildOutpost));
        }

        private static DroneContextDto SafeContext()
        {
            return new DroneContextDto
            {
                depth = 10,
                currentEnergy = 100,
                returnEnergyEstimate = 20,
                structuralIntegrity = 1f,
                gasRisk = 0f,
                unsettledCargoValue = 0,
                cargoWeight = 0f,
                maxCargoWeight = 50f,
                nearestBaseDistance = 20f,
                nearbyMineralIds = new List<string>(),
                returnPathAvailable = true
            };
        }
    }
}
