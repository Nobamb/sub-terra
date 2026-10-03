using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Integration;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Snapshot;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Tests.Debug
{
    public sealed class PromptB125SpawnCommandTests
    {
        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(created[i]);
                }
            }

            created.Clear();
        }

        [Test]
        public void FacingCell_StepsOneHorizontalCell()
        {
            var rig = CreateRig(SceneNames.Integration);
            Vector3Int cell;
            Assert.That(
                DeveloperDebugSpawnCommands.TryResolveFacingCell(
                    rig.Tilemap,
                    new Vector2(0.5f, 0.5f),
                    1f,
                    out cell),
                Is.True);
            Assert.That(cell, Is.EqualTo(new Vector3Int(1, 0, 0)));

            Assert.That(
                DeveloperDebugSpawnCommands.TryResolveFacingCell(
                    rig.Tilemap,
                    new Vector2(0.5f, 0.5f),
                    -1f,
                    out cell),
                Is.True);
            Assert.That(cell, Is.EqualTo(new Vector3Int(-1, 0, 0)));

            Assert.That(
                DeveloperDebugSpawnCommands.TryResolveFacingCell(
                    rig.Tilemap,
                    new Vector2(0.2f, 0.5f),
                    1f,
                    out cell),
                Is.True);
            Assert.That(cell, Is.EqualTo(new Vector3Int(0, 0, 0)));
            Assert.That(
                DeveloperDebugSpawnCommands.TryResolveFacingCell(
                    rig.Tilemap,
                    new Vector2(0.5f, 0.5f),
                    0f,
                    out cell),
                Is.False);
        }

        [Test]
        public void Spawn_AirCell_SurvivesRegen_EvenIfPreviouslyMined()
        {
            var rig = CreateRig(SceneNames.Integration);
            var copper = Register(rig, "tile.copper", 4f, false);
            PlacePlayer(rig, new Vector2(0.5f, 0.5f), 1f);
            Assert.That(
                DeveloperDebugSpawnCommands.TryResolveFacingCell(
                    rig.Tilemap,
                    rig.Player.Position,
                    rig.Player.FacingDirection,
                    out var cell),
                Is.True);
            Assert.That(cell, Is.EqualTo(new Vector3Int(1, 0, 0)));
            Assert.That(rig.Tilemap.HasTile(cell), Is.False);
            rig.Snapshot.RecordMinedCell(cell.x, cell.y, true, 0f);

            var above = Register(rig, "tile.rock.normal", 1f, false);
            rig.Tilemap.SetTile(cell + Vector3Int.up, above);

            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var message = session.Execute("spawn copper", Context(rig));

            Assert.That(message, Is.EqualTo("spawn tile.copper (1, 0)"));
            Assert.That(rig.Tilemap.GetTile(cell), Is.SameAs(copper));
            Assert.That(rig.Tilemap.GetTile(cell + Vector3Int.up), Is.SameAs(above));
            Assert.That(rig.Collider, Is.Not.Null);

            var captured = rig.Snapshot.CaptureSnapshot();
            Assert.That(captured.changedTiles, Has.Count.EqualTo(1));
            Assert.That(captured.changedTiles[0].tileId, Is.EqualTo("tile.copper"));
            Assert.That(captured.changedTiles[0].x, Is.EqualTo(1));
            Assert.That(captured.changedTiles[0].y, Is.EqualTo(0));
            Assert.That(captured.changedTiles[0].remainingDurability, Is.EqualTo(4f));
            Assert.That(captured.miningChanges, Has.Some.Matches<MiningSnapshotDto>(
                mined => mined.x == 1 && mined.y == 0 && mined.isDestroyed));

            Assert.That(rig.Snapshot.RestoreSnapshot(captured), Is.True);
            Assert.That(rig.Generator.Calls, Is.EqualTo(1));
            Assert.That(rig.Tilemap.HasTile(cell), Is.True);
            Assert.That(rig.Tilemap.GetTile(cell), Is.SameAs(copper));
        }

        [Test]
        public void Spawn_ReplacesExistingTile_AndKeepsKindIds()
        {
            var rig = CreateRig(SceneNames.Integration);
            PlacePlayer(rig, new Vector2(0.5f, 0.5f), 1f);
            var tiles = new Dictionary<string, Tile>();
            tiles["tile.rock.normal"] = Register(rig, "tile.rock.normal", 2f, false);
            tiles["tile.rock.normal.gold"] = Register(rig, "tile.rock.normal.gold", 2f, false);
            tiles["tile.copper"] = Register(rig, "tile.copper", 2f, false);
            tiles["tile.copper.gold"] = Register(rig, "tile.copper.gold", 2f, false);
            tiles["tile.iron"] = Register(rig, "tile.iron", 2f, false);
            tiles["tile.iron.gold"] = Register(rig, "tile.iron.gold", 2f, false);
            tiles["tile.lithium"] = Register(rig, "tile.lithium", 2f, false);
            tiles["tile.lithium.gold"] = Register(rig, "tile.lithium.gold", 2f, false);
            tiles["tile.gas-pocket"] = Register(rig, "tile.gas-pocket", 2f, true);
            tiles["tile.gas-pocket.gold"] = Register(rig, "tile.gas-pocket.gold", 2f, true);
            tiles["tile.locked.signal"] = Register(rig, "tile.locked.signal", 3f, false);
            rig.Tilemap.SetTile(new Vector3Int(1, 0, 0), tiles["tile.rock.normal"]);

            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var context = Context(rig);
            var cases = new[]
            {
                ("spawn normal", "tile.rock.normal"),
                ("생성 일반 골드", "tile.rock.normal.gold"),
                ("spawn copper", "tile.copper"),
                ("SPAWN COPPER GOLD", "tile.copper.gold"),
                ("생성 철", "tile.iron"),
                ("spawn iron gold", "tile.iron.gold"),
                ("spawn 리튬", "tile.lithium"),
                ("생성 lithium 골드", "tile.lithium.gold"),
                ("spawn gas", "tile.gas-pocket"),
                ("생성 가스 gold", "tile.gas-pocket.gold"),
                ("spawn engine_fuel", "tile.locked.signal"),
                ("생성 엔진연료", "tile.locked.signal")
            };

            for (var i = 0; i < cases.Length; i++)
            {
                var message = session.Execute(cases[i].Item1, context);
                Assert.That(message, Is.EqualTo("spawn " + cases[i].Item2 + " (1, 0)"), cases[i].Item1);
                Assert.That(rig.Tilemap.GetTile(new Vector3Int(1, 0, 0)), Is.SameAs(tiles[cases[i].Item2]));
            }
        }

        [Test]
        public void Spawn_LeftFacing_UsesTheLeftCell()
        {
            var rig = CreateRig(SceneNames.Integration);
            var iron = Register(rig, "tile.iron", 2f, false);
            PlacePlayer(rig, new Vector2(0.5f, 0.5f), -1f);
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());

            var message = session.Execute("spawn iron", Context(rig));

            Assert.That(message, Is.EqualTo("spawn tile.iron (-1, 0)"));
            Assert.That(rig.Tilemap.GetTile(new Vector3Int(-1, 0, 0)), Is.SameAs(iron));
            Assert.That(rig.Tilemap.HasTile(new Vector3Int(1, 0, 0)), Is.False);
        }

        [Test]
        public void Spawn_Gas_DoesNotActivateZone()
        {
            var rig = CreateRig(SceneNames.Integration);
            Register(rig, "tile.gas-pocket", 2f, true);
            PlacePlayer(rig, new Vector2(0.5f, 0.5f), 1f);
            var gas = rig.Host.AddComponent<GasHazardSystem>();
            SetField(gas, "foregroundTilemap", rig.Tilemap);
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());

            var message = session.Execute("생성 가스", Context(rig));

            Assert.That(message, Is.EqualTo("spawn tile.gas-pocket (1, 0)"));
            Assert.That(gas.ActiveZones.Count, Is.Zero);
        }

        [Test]
        public void Spawn_RejectsFuelGold_MissingVariant_AndWrongScene_WithoutMutation()
        {
            var rig = CreateRig(SceneNames.SurfaceBase);
            var copper = Register(rig, "tile.copper", 4f, false);
            var signal = Register(rig, "tile.locked.signal", 3f, false);
            PlacePlayer(rig, new Vector2(0.5f, 0.5f), 1f);
            var existing = new Vector3Int(4, 4, 0);
            rig.Tilemap.SetTile(existing, copper);
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var context = Context(rig);

            Assert.That(session.Execute("spawn", context), Is.EqualTo("spawn 인자 오류. 예: spawn copper"));
            Assert.That(session.Execute("spawn nope", context), Is.EqualTo("spawn 인자 오류. 예: spawn copper"));
            Assert.That(session.Execute("spawn copper extra", context), Is.EqualTo("spawn 인자 오류. 예: spawn copper"));
            Assert.That(session.Execute("spawn engine_fuel gold", context), Is.EqualTo("엔진연료는 골드 변형이 없습니다"));
            Assert.That(session.Execute("생성 엔진연료 골드", context), Is.EqualTo("엔진연료는 골드 변형이 없습니다"));
            Assert.That(session.Execute("spawn copper", context), Is.EqualTo("광산 씬에서만 사용할 수 있습니다"));
            rig.SceneName = SceneNames.MainMenu;
            Assert.That(session.Execute("생성 구리", context), Is.EqualTo("광산 씬에서만 사용할 수 있습니다"));
            rig.SceneName = SceneNames.Integration;
            Assert.That(session.Execute("spawn copper gold", context), Is.EqualTo("타일 없음: tile.copper.gold"));
            Assert.That(rig.Tilemap.HasTile(new Vector3Int(1, 0, 0)), Is.False);
            Assert.That(rig.Tilemap.GetTile(existing), Is.SameAs(copper));
            Assert.That(rig.Snapshot.CaptureSnapshot().changedTiles, Is.Empty);
            Assert.That(signal, Is.Not.Null);
        }

        [Test]
        public void Spawn_ReportsMissingPlayerTilemapResolverAndSnapshot()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var rig = CreateRig(SceneNames.Integration);
            Register(rig, "tile.copper", 1f, false);
            PlacePlayer(rig, new Vector2(0.5f, 0.5f), 1f);

            rig.Player = null;
            Assert.That(session.Execute("spawn copper", Context(rig)), Is.EqualTo("플레이어 없음"));
            PlacePlayer(rig, new Vector2(0.5f, 0.5f), 1f);
            var tilemap = rig.Tilemap;
            rig.Tilemap = null;
            Assert.That(session.Execute("spawn copper", Context(rig)), Is.EqualTo("광산 타일맵 없음"));
            rig.Tilemap = tilemap;
            var resolver = rig.Resolver;
            rig.Resolver = null;
            Assert.That(session.Execute("spawn copper", Context(rig)), Is.EqualTo("타일 리졸버 없음"));
            rig.Resolver = resolver;
            rig.Snapshot = null;
            Assert.That(session.Execute("spawn copper", Context(rig)), Is.EqualTo("스냅샷 없음"));
            Assert.That(tilemap.HasTile(new Vector3Int(1, 0, 0)), Is.False);
        }

        [Test]
        public void Help_ListsSpawn_AndSourceStaysGated()
        {
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var help = session.Execute("help", null);
            Assert.That(help, Does.Contain("spawn <normal|copper|iron|lithium|gas|engine_fuel> [gold]"));
            Assert.That(help, Does.Contain("생성"));
            Assert.That(help, Does.Contain("예: spawn copper"));

            var source = File.ReadAllText(SourcePath(
                "_Project/Scripts/App/Integration/Debug/DeveloperDebugSpawnCommands.cs"));
            var registry = File.ReadAllText(SourcePath(
                "_Project/Scripts/App/Integration/Debug/DeveloperDebugCommandRegistry.cs"));
            Assert.That(source, Does.Contain("#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT"));
            Assert.That(source, Does.Contain("RecordChangedTile"));
            Assert.That(source, Does.Contain("TryFindTileById"));
            Assert.That(source, Does.Contain("SetTile"));
            Assert.That(source, Does.Contain("ProcessTilemapChanges"));
            Assert.That(source, Does.Contain("tile.locked.signal"));
            Assert.That(source, Does.Contain("tile.gas-pocket"));
            Assert.That(source, Does.Contain("tile.rock.normal"));
            Assert.That(source, Does.Not.Contain("MineLayerTileIds"));
            Assert.That(source, Does.Not.Contain("using SubTerra.Gameplay.DemoWorld"));
            Assert.That(source, Does.Not.Contain("ActivateAt"));
            Assert.That(source, Does.Not.Contain("Debug.isDebugBuild"));
            var stripped = source.Replace("SUBTERRA_BUILD_DEVELOPMENT", string.Empty);
            Assert.That(stripped, Does.Not.Contain("DEVELOPMENT_BUILD"));
            Assert.That(stripped, Does.Not.Contain("SUBTERRA_BUILD_QA"));
            Assert.That(registry, Does.Contain("DeveloperDebugSpawnCommands.Register"));
        }

        private DeveloperDebugCommandContext Context(SpawnRig rig)
        {
            return new DeveloperDebugCommandContext(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                () => rig.ToWorld());
        }

        private SpawnRig CreateRig(string sceneName)
        {
            var host = new GameObject("PromptB125Spawn");
            created.Add(host);
            var gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(host.transform, false);
            gridObject.AddComponent<Grid>();
            var mapObject = new GameObject(DepthDarknessBlockVisual.ForegroundTilemapName);
            mapObject.transform.SetParent(gridObject.transform, false);
            var tilemap = mapObject.AddComponent<Tilemap>();
            mapObject.AddComponent<TilemapRenderer>();
            var collider = mapObject.AddComponent<TilemapCollider2D>();
            var resolver = host.AddComponent<MiningTileResolver>();
            var snapshot = host.AddComponent<WorldSnapshotSystem>();
            var generator = host.AddComponent<ClearingGenerator>();
            generator.Map = tilemap;
            SetField(snapshot, "foregroundTilemap", tilemap);
            SetField(snapshot, "tileResolver", resolver);
            SetField(snapshot, "baseWorldGeneratorBehaviour", generator);
            return new SpawnRig
            {
                Host = host,
                Tilemap = tilemap,
                Collider = collider,
                Resolver = resolver,
                Snapshot = snapshot,
                Generator = generator,
                SceneName = sceneName,
                Tiles = new List<TileBase>(),
                Definitions = new List<MiningTileDto>()
            };
        }

        private Tile Register(SpawnRig rig, string tileId, float durability, bool containsGas)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = tileId;
            created.Add(tile);
            rig.Tiles.Add(tile);
            rig.Definitions.Add(new MiningTileDto(
                tileId,
                string.Empty,
                0,
                true,
                durability,
                1f,
                containsGas ? 0.5f : 0f,
                containsGas));
            rig.Resolver.EditorSetEntries(rig.Tiles.ToArray(), rig.Definitions.ToArray());
            return tile;
        }

        private void PlacePlayer(SpawnRig rig, Vector2 position, float facing)
        {
            var playerObject = new GameObject("PromptB125SpawnPlayer");
            created.Add(playerObject);
            playerObject.transform.SetParent(rig.Host.transform, false);
            playerObject.transform.position = new Vector3(position.x, position.y, 0f);
            playerObject.AddComponent<Rigidbody2D>();
            var movement = playerObject.AddComponent<PlayerMovement>();
            // EditMode에서는 리지드바디 위치가 트랜스폼과 어긋날 수 있다.
            // Position은 body가 없으면 트랜스폼을 쓴다.
            SetField(movement, "body", null);
            if (facing < 0f)
            {
                movement.SetMoveInput(-1f);
            }
            else if (facing > 0f)
            {
                movement.SetMoveInput(1f);
            }

            rig.Player = movement;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static string SourcePath(string assetRelativePath)
        {
            return Path.Combine(Application.dataPath, assetRelativePath);
        }

        private sealed class SpawnRig
        {
            public GameObject Host;
            public Tilemap Tilemap;
            public TilemapCollider2D Collider;
            public MiningTileResolver Resolver;
            public WorldSnapshotSystem Snapshot;
            public PlayerMovement Player;
            public ClearingGenerator Generator;
            public string SceneName;
            public List<TileBase> Tiles;
            public List<MiningTileDto> Definitions;

            public DeveloperDebugSpawnWorld ToWorld()
            {
                return new DeveloperDebugSpawnWorld(
                    SceneName,
                    Player,
                    Tilemap,
                    Resolver,
                    Snapshot);
            }
        }

        private sealed class ClearingGenerator : MonoBehaviour, IWorldBaseGenerator
        {
            public Tilemap Map;
            public int Calls;

            public bool Regenerate(long worldSeed, int generatorVersion)
            {
                Calls++;
                if (Map != null)
                {
                    Map.ClearAllTiles();
                }

                return true;
            }
        }
    }
}
