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
            }
            finally
            {
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(settings);
            }
        }

        private sealed class FixedProvider : IDroneContextProvider
        {
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

        private sealed class RecordingDialogueView : IDroneDialogueView
        {
            public DroneDialogueResult LastDialogue { get; private set; }
            public void SetDialogue(DroneDialogueResult dialogue) => LastDialogue = dialogue;
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
