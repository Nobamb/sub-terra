using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.Drone
{
    public sealed class TemplateDialogueGeneratorTests
    {
        private readonly List<Object> created = new List<Object>();
        private DroneAnalysisSettings settings;
        private DroneAnalysisService analysis;

        [SetUp]
        public void SetUp()
        {
            settings = Track(ScriptableObject.CreateInstance<DroneAnalysisSettings>());
            settings.EditorSetDefaults();
            analysis = new DroneAnalysisService(settings);
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(created[i]);
            }
            created.Clear();
        }

        [Test]
        public void I_F04_RegularDialogue_IsSuppressedUntilCooldownExpires()
        {
            var clock = new ManualClock();
            var template = CreateTemplate(
                DataIds.Dialogue.DroneExplore,
                "심도 {depth}, {action}");
            var generator = new TemplateDialogueGenerator(
                new[] { template },
                clock,
                settings);
            var context = SafeContext();

            var first = generator.Generate(analysis.Analyze(context));
            clock.NowValue = 5d;
            var during = generator.Generate(analysis.Analyze(context));
            clock.NowValue = 10d;
            var after = generator.Generate(analysis.Analyze(context));

            Assert.That(first.Text, Is.EqualTo("심도 10, 하강 계속"));
            Assert.That(during.IsSuppressed, Is.True);
            Assert.That(after.IsSuppressed, Is.False);
        }

        [Test]
        public void UrgentDialogue_UsesShortRepeatPolicy()
        {
            var clock = new ManualClock();
            var template = CreateTemplate(
                DataIds.Dialogue.DroneGasWarning,
                "가스 {gasRisk}");
            var generator = new TemplateDialogueGenerator(
                new[] { template },
                clock,
                settings);
            var context = SafeContext();
            context.gasRisk = 1f;

            Assert.That(generator.Generate(analysis.Analyze(context)).IsSuppressed, Is.False);
            clock.NowValue = 2d;
            Assert.That(generator.Generate(analysis.Analyze(context)).IsSuppressed, Is.True);
            clock.NowValue = 3d;
            Assert.That(generator.Generate(analysis.Analyze(context)).Text, Is.EqualTo("가스 1"));
        }

        [Test]
        public void MissingTemplateToken_ReturnsTruthNeutralFallback()
        {
            var template = CreateTemplate(
                DataIds.Dialogue.DroneExplore,
                "확률 {successChance}");
            var generator = new TemplateDialogueGenerator(
                new[] { template },
                new ManualClock(),
                settings);

            var result = generator.Generate(analysis.Analyze(SafeContext()));

            Assert.That(result.UsedFallback, Is.True);
            Assert.That(result.Text, Does.Not.Contain("successChance"));
            Assert.That(result.Text, Does.Not.Contain("%"));
        }

        private DialogueTemplateData CreateTemplate(string id, string text)
        {
            var template = Track(ScriptableObject.CreateInstance<DialogueTemplateData>());
            template.EditorSet(id, id, "test", 0, text);
            return template;
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
                nearestBaseDistance = 20f,
                nearbyMineralIds = new List<string>(),
                returnPathAvailable = true
            };
        }

        private T Track<T>(T value) where T : Object
        {
            created.Add(value);
            return value;
        }

        private sealed class ManualClock : IDroneClock
        {
            public double NowValue;
            public double Now => NowValue;
        }
    }
}
