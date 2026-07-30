using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.Shared;

namespace SubTerra.App.Tests.Tutorial
{
    /// <summary>N-F04 목표 ID 세이브 왕복·중복 보상 방지.</summary>
    public sealed class DemoObjectiveSaveRoundTripTests
    {
        [Test]
        public void N_F04_SaveMapper_RoundTripsCurrentObjectiveId()
        {
            var game = GameState.FromParts(
                new PlayerState(80, 100, 12, 1f, 5f, 0f),
                new ProgressState(7, true, DemoObjectiveIds.OutpostInstall, false),
                new RunState(10, true));
            Assert.That(game, Is.Not.Null);

            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1f, 5);
            var inventory = new InventoryService(catalog, 50f, game);
            var upgrades = new UpgradeState();
            Assert.That(upgrades.TryRestore(new List<UpgradeLevelState>()), Is.True);
            Assert.That(upgrades.TryRestoreUnlockedZones(new List<string>()), Is.True);

            var mapper = new SaveDataMapper(new FixedClock(1000));
            // (game, inventory, upgrades, dialogue, world, targetScene, gameVersion)
            var data = mapper.Capture(new SaveCaptureContext(
                game,
                inventory.State,
                upgrades,
                null,
                null,
                "Mine_Demo_Integration",
                "1.0.0"));

            Assert.That(data, Is.Not.Null);
            Assert.That(data.progress.currentObjectiveId, Is.EqualTo(DemoObjectiveIds.OutpostInstall));
            Assert.That(data.progress.completedObjectives, Is.EqualTo(7));
            Assert.That(data.progress.isDemoComplete, Is.False);

            Assert.That(mapper.TryRestore(data, out var restored), Is.True);
            Assert.That(
                restored.GameState.Progress.CurrentObjectiveId,
                Is.EqualTo(DemoObjectiveIds.OutpostInstall));
            Assert.That(restored.GameState.Progress.CompletedObjectives, Is.EqualTo(7));
            Assert.That(restored.GameState.Progress.IsDemoComplete, Is.False);

            var director = new DemoObjectiveDirector();
            director.BindGameState(restored.GameState);
            director.RestoreFromProgress(restored.GameState.Progress);
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.OutpostInstall));
            Assert.That(director.CompletedCount, Is.EqualTo(7));
        }

        [Test]
        public void N_F04_LegacySaveWithoutObjectiveId_ResolvesByCompletedCount()
        {
            var resolved = DemoObjectiveCatalog.ResolveObjectiveId(string.Empty, 3);
            Assert.That(resolved, Is.EqualTo(DemoObjectiveIds.Ordered[3]));
            Assert.That(resolved, Is.EqualTo(DemoObjectiveIds.MineLithium));

            var unknown = DemoObjectiveCatalog.ResolveObjectiveId("demo.unknown.legacy", 0);
            Assert.That(unknown, Is.EqualTo(DemoObjectiveIds.ExploreStart));
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
