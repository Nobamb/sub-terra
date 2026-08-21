using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.Tutorial;

namespace SubTerra.App.Tests.Tutorial
{
    public sealed class DemoObjectiveSaveRoundTripTests
    {
        [Test]
        public void PromptB60_SaveMapperRoundTripsCurrentQuest()
        {
            var game = GameState.FromParts(
                new PlayerState(80, 100, 12, 1f, 5f, 0f),
                new ProgressState(10, true, DemoObjectiveIds.InstallOutpostCore, false),
                new RunState(10, true));
            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1f, 5);
            var inventory = new InventoryService(catalog, 50f, game);
            var upgrades = new UpgradeState();
            Assert.That(upgrades.TryRestore(new List<UpgradeLevelState>()), Is.True);
            Assert.That(upgrades.TryRestoreUnlockedZones(new List<string>()), Is.True);

            var mapper = new SaveDataMapper(new FixedClock(1000));
            var data = mapper.Capture(new SaveCaptureContext(
                game,
                inventory.State,
                upgrades,
                null,
                null,
                "Mine_Demo_Integration",
                "1.0.0"));

            Assert.That(data.progress.currentObjectiveId, Is.EqualTo(DemoObjectiveIds.InstallOutpostCore));
            Assert.That(data.progress.completedObjectives, Is.EqualTo(10));
            Assert.That(mapper.TryRestore(data, out var restored), Is.True);

            var director = new DemoObjectiveDirector();
            director.BindGameState(restored.GameState);
            director.RestoreFromProgress(restored.GameState.Progress);
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.InstallOutpostCore));
            Assert.That(director.CompletedCount, Is.EqualTo(10));
        }

        [Test]
        public void PromptB60_LegacyOrMismatchedIdUsesNewSequenceCount()
        {
            Assert.That(
                DemoObjectiveCatalog.ResolveObjectiveId(string.Empty, 3),
                Is.EqualTo(DemoObjectiveIds.TravelToSurface));
            Assert.That(
                DemoObjectiveCatalog.ResolveObjectiveId(DemoObjectiveIds.MineLithium, 3),
                Is.EqualTo(DemoObjectiveIds.TravelToSurface));
            Assert.That(
                DemoObjectiveCatalog.ResolveObjectiveId("demo.unknown.legacy", 0),
                Is.EqualTo(DemoObjectiveIds.MineBlock));

            var engine = new DemoObjectiveTransitionEngine();
            engine.Restore(DemoObjectiveIds.PlaceSupportInDanger, 6);
            Assert.That(engine.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.PlaceSupportInDanger));
            Assert.That(engine.CompletedCount, Is.EqualTo(6));
        }

        private sealed class FixedClock : ISaveClock
        {
            private readonly long seconds;

            public FixedClock(long utcSeconds)
            {
                seconds = utcSeconds;
            }

            public long UtcNowSeconds => seconds;
        }
    }
}
