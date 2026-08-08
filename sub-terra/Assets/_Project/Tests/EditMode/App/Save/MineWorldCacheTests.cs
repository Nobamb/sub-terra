using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.Save
{
    /// <summary>
    /// 엘리베이터 왕복 Mine world 유지 — PR1 저장 안전망 단위 테스트.
    /// T1: Provider null + 캐시 있음 → world가 비지 않음
    /// T2: Clear 후 저장 → 이전 miningChanges 미혼입
    /// T3: Capture→캐시→Mapper miningChanges 왕복
    /// </summary>
    public sealed class MineWorldCacheTests
    {
        private string testRoot;
        private SavePathPolicy paths;
        private PhysicalSaveFileSystem physical;
        private SaveDataMapper mapper;
        private SaveJsonCodec json;

        [SetUp]
        public void SetUp()
        {
            testRoot = Path.Combine(
                Path.GetTempPath(),
                "subterra-mine-cache-" + System.Guid.NewGuid().ToString("N"));
            paths = new SavePathPolicy(testRoot);
            physical = new PhysicalSaveFileSystem();
            mapper = new SaveDataMapper(new FixedClock());
            json = new SaveJsonCodec(new SaveMigrationService());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, true);
            }
        }

        [Test]
        public void T1_NullProvider_WithCache_PreservesMiningChangesInSave()
        {
            var cache = new MineWorldCache();
            cache.ReplaceFromProvider(CreateMinedSnapshot(seed: 42, x: 3, y: 7));

            var context = CreateCaptureContext(
                worldProvider: null,
                mineWorldFallback: cache.Peek());
            var save = new SaveService(physical, paths, mapper, json);
            var load = new LoadService(physical, paths, mapper, json);

            Assert.That(save.Save(1, context).IsSuccess, Is.True);
            var loaded = load.Load(1);

            Assert.That(loaded.IsSuccess, Is.True);
            Assert.That(loaded.State.World.worldSeed, Is.EqualTo(42));
            Assert.That(loaded.State.World.miningChanges, Is.Not.Null);
            Assert.That(loaded.State.World.miningChanges.Count, Is.EqualTo(1));
            Assert.That(loaded.State.World.miningChanges[0].x, Is.EqualTo(3));
            Assert.That(loaded.State.World.miningChanges[0].y, Is.EqualTo(7));
            Assert.That(loaded.State.World.miningChanges[0].isDestroyed, Is.True);
        }

        [Test]
        public void T2_ClearCache_ThenSave_DoesNotLeakPreviousMiningChanges()
        {
            var cache = new MineWorldCache();
            cache.ReplaceFromProvider(CreateMinedSnapshot(seed: 99, x: 1, y: 1));
            cache.Clear();

            var context = CreateCaptureContext(
                worldProvider: null,
                mineWorldFallback: cache.Peek());
            var save = new SaveService(physical, paths, mapper, json);
            var load = new LoadService(physical, paths, mapper, json);

            Assert.That(save.Save(1, context).IsSuccess, Is.True);
            var loaded = load.Load(1);

            Assert.That(loaded.IsSuccess, Is.True);
            // 캐시 없음 → 빈 world. 이전 슬롯 채굴이 혼입되면 안 된다.
            Assert.That(
                loaded.State.World.miningChanges == null
                || loaded.State.World.miningChanges.Count == 0,
                Is.True);
        }

        [Test]
        public void T3_ProviderCapture_UpdatesCache_AndMapperRoundTrips()
        {
            var cache = new MineWorldCache();
            var provider = new FixedProvider(CreateMinedSnapshot(seed: 777, x: 5, y: 9));

            // CaptureContext와 동일: Provider 캡처 → 캐시 반영 → 폴백 포함 컨텍스트.
            var captured = provider.CaptureSnapshot();
            cache.ReplaceFromProvider(captured);

            var context = CreateCaptureContext(
                worldProvider: provider,
                mineWorldFallback: cache.Peek());
            var save = new SaveService(physical, paths, mapper, json);
            var load = new LoadService(physical, paths, mapper, json);

            Assert.That(save.Save(2, context).IsSuccess, Is.True);
            var loaded = load.Load(2);

            Assert.That(loaded.IsSuccess, Is.True);
            Assert.That(loaded.State.World.worldSeed, Is.EqualTo(777));
            Assert.That(loaded.State.World.miningChanges.Count, Is.EqualTo(1));
            Assert.That(loaded.State.World.miningChanges[0].x, Is.EqualTo(5));
            Assert.That(loaded.State.World.miningChanges[0].y, Is.EqualTo(9));

            // 캐시 Peek도 동일 변경점을 유지.
            var peeked = cache.Peek();
            Assert.That(peeked.miningChanges[0].x, Is.EqualTo(5));
            Assert.That(MineWorldCache.HasMeaningfulContent(peeked), Is.True);
        }

        [Test]
        public void SeedFromSave_IgnoresEmptyWorld_DoesNotClearExistingCache()
        {
            var cache = new MineWorldCache();
            cache.ReplaceFromProvider(CreateMinedSnapshot(seed: 11, x: 2, y: 4));
            cache.SeedFromSave(new WorldSnapshotDto());

            var peeked = cache.Peek();
            Assert.That(peeked, Is.Not.Null);
            Assert.That(peeked.miningChanges[0].x, Is.EqualTo(2));
        }

        [Test]
        public void ReplaceFromProvider_Null_KeepsPreviousCache()
        {
            var cache = new MineWorldCache();
            cache.ReplaceFromProvider(CreateMinedSnapshot(seed: 1, x: 8, y: 8));
            cache.ReplaceFromProvider(null);

            Assert.That(cache.HasSnapshot, Is.True);
            Assert.That(cache.Peek().miningChanges[0].x, Is.EqualTo(8));
        }

        [Test]
        public void HasMeaningfulContent_DetectsMiningAndBuildings()
        {
            Assert.That(MineWorldCache.HasMeaningfulContent(null), Is.False);
            Assert.That(MineWorldCache.HasMeaningfulContent(new WorldSnapshotDto()), Is.False);
            Assert.That(
                MineWorldCache.HasMeaningfulContent(
                    new WorldSnapshotDto { worldSeed = 1 }),
                Is.True);
            Assert.That(
                MineWorldCache.HasMeaningfulContent(
                    new WorldSnapshotDto
                    {
                        miningChanges = new List<MiningSnapshotDto>
                        {
                            new MiningSnapshotDto { x = 0, y = 0, isDestroyed = true }
                        }
                    }),
                Is.True);
            Assert.That(
                MineWorldCache.HasMeaningfulContent(
                    new WorldSnapshotDto
                    {
                        buildings = new List<BuildingSnapshotDto>
                        {
                            new BuildingSnapshotDto { instanceId = "b1" }
                        }
                    }),
                Is.True);
        }

        private static WorldSnapshotDto CreateMinedSnapshot(long seed, int x, int y)
        {
            return new WorldSnapshotDto
            {
                worldSeed = seed,
                generatorVersion = 1,
                miningChanges = new List<MiningSnapshotDto>
                {
                    new MiningSnapshotDto
                    {
                        x = x,
                        y = y,
                        isDestroyed = true
                    }
                }
            };
        }

        private SaveCaptureContext CreateCaptureContext(
            IWorldSnapshotProvider worldProvider,
            WorldSnapshotDto mineWorldFallback)
        {
            var outpost = new OutpostState();
            Assert.That(outpost.TryRestore(
                System.Array.Empty<OutpostStorageEntryState>(),
                System.Array.Empty<string>(),
                string.Empty,
                0,
                0), Is.True);

            var game = GameState.FromParts(
                new PlayerState(100, 100, 0, 0f, 0f, 0f),
                new ProgressState(0, false),
                new RunState(0, 0, true, StructuralRiskLevel.Safe, GasRiskLevel.Safe, RunLifecyclePhase.Ready),
                outpost);

            var inventory = new InventoryState(100f);
            var upgrades = new UpgradeState();

            return new SaveCaptureContext(
                game,
                inventory,
                upgrades,
                dialogueGenerator: null,
                worldProvider,
                SceneNames.SurfaceBase,
                "0.1-mine-cache-test",
                mineWorldFallback);
        }

        private sealed class FixedClock : ISaveClock
        {
            public long UtcNowSeconds => 1_700_000_000;
        }

        private sealed class FixedProvider : IWorldSnapshotProvider
        {
            private readonly WorldSnapshotDto snapshot;

            public FixedProvider(WorldSnapshotDto world)
            {
                snapshot = world;
            }

            public WorldSnapshotDto CaptureSnapshot()
            {
                return MineWorldCache.Clone(snapshot);
            }

            public bool RestoreSnapshot(WorldSnapshotDto dto) => true;
        }
    }
}
