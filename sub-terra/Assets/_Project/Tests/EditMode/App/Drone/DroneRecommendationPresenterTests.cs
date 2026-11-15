using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.UI.Drone;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.Drone
{
    public sealed class DroneRecommendationPresenterTests
    {
        [Test]
        public void K_F04_WorldAndOverlayViews_ReceiveTheSameDialogueResult()
        {
            var settings = ScriptableObject.CreateInstance<DroneAnalysisSettings>();
            var template = ScriptableObject.CreateInstance<DialogueTemplateData>();
            try
            {
                settings.EditorSetDefaults();
                template.EditorSet(
                    DataIds.Dialogue.DroneExplore,
                    "탐사",
                    "explore",
                    0,
                    "심도 {depth}");
                var overlay = new RecordingDialogueView();
                var world = new RecordingDialogueView();
                var reason = new RecordingReasonView();
                var presenter = new DroneRecommendationPresenter(overlay, world, reason);
                presenter.Bind(
                    new FixedProvider(),
                    new DroneAnalysisService(settings),
                    new TemplateDialogueGenerator(
                        new[] { template },
                        new FixedClock(),
                        settings));

                Assert.That(overlay.LastDialogue, Is.Not.Null);
                Assert.That(world.LastDialogue, Is.SameAs(overlay.LastDialogue));
                Assert.That(reason.LastAnalysis.RecommendedAction, Is.EqualTo(DroneAction.ContinueDescending));

                presenter.Refresh(isScanPulseActive: true);
                Assert.That(overlay.LastOperationalState, Is.EqualTo(DroneOperationalState.Scanning));
                Assert.That(world.LastOperationalState, Is.EqualTo(DroneOperationalState.Scanning));
            }
            finally
            {
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Issue324_StructuralDialogueUpdatesOnlyWhenRowStageOrDirectionChanges()
        {
            var settings = ScriptableObject.CreateInstance<DroneAnalysisSettings>();
            var template = ScriptableObject.CreateInstance<DialogueTemplateData>();
            var explore = ScriptableObject.CreateInstance<DialogueTemplateData>();
            try
            {
                settings.EditorSetDefaults();
                template.EditorSet(DataIds.Dialogue.DroneStructuralWarning, "경고", "warning", 0, "{safetyGuidance}");
                explore.EditorSet(DataIds.Dialogue.DroneExplore, "탐사", "explore", 0, "심도 {depth}");
                var provider = new FixedProvider { Forecast = new DroneStructuralForecast(DroneStructuralWarning.Collapse, -8, -1) };
                var view = new RecordingDialogueView();
                var presenter = new DroneRecommendationPresenter(view, null);
                presenter.Bind(provider, new DroneAnalysisService(settings),
                    new TemplateDialogueGenerator(new[] { template, explore }, new FixedClock(), settings));

                Assert.That(view.LastDialogue.Text, Does.Contain("왼쪽"));
                Assert.That(view.DialogueCount, Is.EqualTo(1));
                presenter.Refresh();
                Assert.That(view.DialogueCount, Is.EqualTo(1));
                provider.Forecast = new DroneStructuralForecast(DroneStructuralWarning.Collapse, -8, 1);
                presenter.Refresh();
                Assert.That(view.LastDialogue.Text, Does.Contain("오른쪽"));
                provider.Forecast = new DroneStructuralForecast(DroneStructuralWarning.Overhead, -8, 0);
                presenter.Refresh();
                Assert.That(view.LastDialogue.Text, Does.Contain("머리 위"));
                provider.Forecast = new DroneStructuralForecast(DroneStructuralWarning.Overhead, -9, 0);
                presenter.Refresh();
                Assert.That(view.DialogueCount, Is.EqualTo(4));
                provider.Forecast = new DroneStructuralForecast(DroneStructuralWarning.Safe, -9, 0);
                presenter.Refresh();
                Assert.That(view.LastDialogue.TemplateId, Is.EqualTo(DataIds.Dialogue.DroneExplore));
            }
            finally
            {
                Object.DestroyImmediate(explore);
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(settings);
            }
        }

        private sealed class FixedProvider : IDroneStructuralForecastProvider
        {
            public DroneStructuralForecast? Forecast { get; set; }
            public DroneStructuralForecast? ReadStructuralForecast() => Forecast;

            public DroneContextDto CreateContext()
            {
                return new DroneContextDto
                {
                    depth = 12,
                    currentEnergy = 100,
                    returnEnergyEstimate = 20,
                    structuralIntegrity = 1f,
                    gasRisk = 0f,
                    unsettledCargoValue = 0,
                    cargoWeight = 0f,
                    maxCargoWeight = 50f,
                    nearestBaseDistance = 10f,
                    nearbyMineralIds = new List<string>(),
                    returnPathAvailable = true
                };
            }
        }

        private sealed class FixedClock : IDroneClock
        {
            public double Now => 0d;
        }

        private sealed class RecordingDialogueView : IDroneDialogueView, IDroneOperationalStateView
        {
            public DroneDialogueResult LastDialogue { get; private set; }
            public DroneOperationalState LastOperationalState { get; private set; }
            public int DialogueCount { get; private set; }
            public void SetDialogue(DroneDialogueResult dialogue)
            {
                LastDialogue = dialogue;
                DialogueCount++;
            }
            public void SetOperationalState(DroneOperationalState state) => LastOperationalState = state;
            public void SetVisible(bool visible) { }
        }

        private sealed class RecordingReasonView : IDroneReasonView
        {
            public DroneAnalysisResult LastAnalysis { get; private set; }
            public void SetAnalysis(DroneAnalysisResult analysis) => LastAnalysis = analysis;
            public void SetVisible(bool visible) { }
        }
    }
}
