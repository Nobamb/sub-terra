using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.Save
{
    public sealed class MineResetServiceTests
    {
        [Test]
        public void Gold800_Reset_SpendsFeeAndReplacesWorldWithEmptyNewSeed()
        {
            var state = GameState.CreateNew();
            state.SetGold(800);
            var cache = CreatePopulatedCache(41);

            var success = MineResetService.TryReset(
                state,
                cache,
                new FixedSeedSource(99),
                out var result);

            Assert.That(success, Is.True);
            Assert.That(result.Status, Is.EqualTo(MineResetStatus.Success));
            Assert.That(result.PreviousSeed, Is.EqualTo(41));
            Assert.That(result.NewSeed, Is.EqualTo(99));
            Assert.That(result.RemainingGold, Is.EqualTo(300));
            Assert.That(state.Player.Gold, Is.EqualTo(300));

            var world = cache.Peek();
            Assert.That(world.worldSeed, Is.EqualTo(99));
            Assert.That(world.generatorVersion, Is.EqualTo(7));
            Assert.That(world.miningChanges, Is.Empty);
            Assert.That(world.changedTiles, Is.Empty);
            Assert.That(world.collapseChanges, Is.Empty);
            Assert.That(world.buildings, Is.Empty);
            Assert.That(world.gasChanges, Is.Empty);
            Assert.That(world.discoveredChunkIds, Is.Empty);
            Assert.That(world.powerState.cableConnections, Is.Empty);
        }

        [Test]
        public void GoldBelowFee_ResetFailsWithoutChangingGoldOrWorld()
        {
            var state = GameState.CreateNew();
            state.SetGold(499);
            var cache = CreatePopulatedCache(41);
            var before = cache.Peek();

            var success = MineResetService.TryReset(
                state,
                cache,
                new FixedSeedSource(99),
                out var result);

            Assert.That(success, Is.False);
            Assert.That(result.Status, Is.EqualTo(MineResetStatus.InsufficientGold));
            Assert.That(state.Player.Gold, Is.EqualTo(499));
            Assert.That(cache.Peek().worldSeed, Is.EqualTo(before.worldSeed));
            Assert.That(cache.Peek().miningChanges.Count, Is.EqualTo(before.miningChanges.Count));
            Assert.That(cache.Peek().buildings.Count, Is.EqualTo(before.buildings.Count));
        }

        [Test]
        public void ExactFee_ResetAllowsZeroBalance()
        {
            var state = GameState.CreateNew();
            state.SetGold(MineResetService.FeeGold);

            Assert.That(
                MineResetService.TryReset(
                    state,
                    CreatePopulatedCache(41),
                    new FixedSeedSource(99),
                    out var result),
                Is.True);
            Assert.That(result.RemainingGold, Is.Zero);
            Assert.That(state.Player.Gold, Is.Zero);
        }

        [Test]
        public void Reset_DoesNotMutateInventoryOrUpgrades()
        {
            var state = GameState.CreateNew();
            state.SetGold(800);
            var inventory = new InventoryState();
            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1f, 10, "구리");
            var inventoryService = new InventoryService(catalog, inventory, state);
            Assert.That(
                inventoryService.TryAddMineral("mineral.copper", 12).Status,
                Is.EqualTo(InventoryMutationStatus.Success));
            var upgrades = new UpgradeState();
            Assert.That(upgrades.TryRestore(
                new[] { new UpgradeLevelState("upgrade.drill", 2) }), Is.True);
            Assert.That(upgrades.TryRestoreUnlockedZones(new[] { "zone.deep" }), Is.True);

            Assert.That(
                MineResetService.TryReset(
                    state,
                    CreatePopulatedCache(41),
                    new FixedSeedSource(99),
                    out _),
                Is.True);

            Assert.That(inventory.GetQuantity("mineral.copper"), Is.EqualTo(12));
            Assert.That(upgrades.GetLevel("upgrade.drill"), Is.EqualTo(2));
            Assert.That(upgrades.IsZoneUnlocked("zone.deep"), Is.True);
        }

        [Test]
        public void SameOrZeroSeeds_ExhaustRetries_WithoutChangingState()
        {
            var state = GameState.CreateNew();
            state.SetGold(800);
            var cache = CreatePopulatedCache(41);

            var success = MineResetService.TryReset(
                state,
                cache,
                new FixedSeedSource(0, 41, 0, 41, 0, 41, 0, 41),
                out var result);

            Assert.That(success, Is.False);
            Assert.That(result.Status, Is.EqualTo(MineResetStatus.SeedFailed));
            Assert.That(state.Player.Gold, Is.EqualTo(800));
            Assert.That(cache.Peek().worldSeed, Is.EqualTo(41));
        }

        [Test]
        public void RuntimeReset_OutsideSurface_FailsWithoutChangingGold()
        {
            Assert.That(
                SceneManager.GetActiveScene().name,
                Is.Not.EqualTo(SceneNames.SurfaceBase),
                "EditMode test scene must represent a non-Surface context.");
            GameObject runtimeObject = null;
            try
            {
                typeof(SaveRuntimeController)
                    .GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, null);
                runtimeObject = new GameObject("MineResetRuntimeTest");
                var runtime = runtimeObject.AddComponent<SaveRuntimeController>();
                var state = GameState.CreateNew();
                state.SetGold(800);
                var boundStateField = typeof(SaveRuntimeController)
                    .GetField("boundState", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(boundStateField, Is.Not.Null);
                boundStateField.SetValue(runtime, state);

                Assert.That(runtime.TryResetMine(out var reason), Is.False);
                Assert.That(reason, Is.EqualTo("mine_reset.fail.surface"));
                Assert.That(state.Player.Gold, Is.EqualTo(800));
            }
            finally
            {
                if (runtimeObject != null)
                {
                    Object.DestroyImmediate(runtimeObject);
                }

            }
        }

        private static MineWorldCache CreatePopulatedCache(long seed)
        {
            var cache = new MineWorldCache();
            cache.ReplaceFromProvider(new WorldSnapshotDto
            {
                worldSeed = seed,
                generatorVersion = 7,
                miningChanges = new List<MiningSnapshotDto>
                {
                    new MiningSnapshotDto { x = 1, y = 2, isDestroyed = true }
                },
                changedTiles = new List<ChangedTileSnapshotDto>
                {
                    new ChangedTileSnapshotDto { x = 3, y = 4 }
                },
                buildings = new List<BuildingSnapshotDto>
                {
                    new BuildingSnapshotDto { instanceId = "support-1" }
                },
                discoveredChunkIds = new List<string> { "chunk-1" },
                powerState = new PowerSnapshotDto
                {
                    cableConnections = new List<PowerConnectionSnapshotDto>
                    {
                        new PowerConnectionSnapshotDto()
                    }
                }
            });
            return cache;
        }

        private sealed class FixedSeedSource : IMineResetSeedSource
        {
            private readonly long[] seeds;
            private int index;

            public FixedSeedSource(params long[] values)
            {
                seeds = values;
            }

            public long NextSeed()
            {
                if (seeds == null || seeds.Length == 0)
                {
                    return 0;
                }

                var value = seeds[index < seeds.Length ? index : seeds.Length - 1];
                index++;
                return value;
            }
        }
    }
}
