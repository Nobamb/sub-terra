using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Tests.Debug
{
    /// <summary>B-193: hit / fall / hitgas / falling 명령과 spawn의 Enter 목표 칸 규칙.</summary>
    public sealed class PromptB193DebugCommandTests
    {
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";
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

        [TestCase("hit rock", 25f)]
        [TestCase("hit copper", 37.5f)]
        [TestCase("hit iron", 50f)]
        [TestCase("hit lithium", 20f)]
        [TestCase("hit rock golden", 32.5f)]
        [TestCase("hit iron golden", 65f)]
        [TestCase("hit gas", 25f)]
        [TestCase("hit 구리", 37.5f)]
        [TestCase("hit normal gold", 32.5f)]
        public void Hit_AppliesCollapseDamageByKindAndGold(string line, float expected)
        {
            var rig = CreateRig(SceneNames.Integration);
            var survival = CreateSurvival();
            var session = Session();

            var message = session.Execute(line, Context(rig, survival));

            Assert.That(message, Does.StartWith("hit tile."), message);
            Assert.That(100f - survival.State.Health, Is.EqualTo(expected).Within(0.01f), line);
        }

        [Test]
        public void Hit_SecondHitInsideInvulnerabilityIsRejected()
        {
            var rig = CreateRig(SceneNames.Integration);
            var survival = CreateSurvival();
            var session = Session();
            var context = Context(rig, survival);

            Assert.That(session.Execute("hit copper", context), Does.StartWith("hit tile.copper"));
            var afterFirst = survival.State.Health;
            Assert.That(session.Execute("hit copper", context), Does.StartWith("피해 없음"));
            Assert.That(survival.State.Health, Is.EqualTo(afterFirst));
        }

        [Test]
        public void Hit_DoesNotShakeTheCamera_ButRealCollapseImpactStillDoes()
        {
            var rig = CreateRig(SceneNames.Integration);
            var cameraObject = new GameObject("PromptB193Camera");
            created.Add(cameraObject);
            var follow = cameraObject.AddComponent<PlayerCameraFollow>();
            var shakeField = typeof(PlayerCameraFollow).GetField(
                "shakeTimeRemaining", BindingFlags.Instance | BindingFlags.NonPublic);
            var previousReduceMotion = AccessibilityPreferences.ReduceMotion;
            AccessibilityPreferences.ReduceMotion = false;
            try
            {
                var survival = CreateSurvival();
                SetField(survival, "cameraFollow", follow);

                Assert.That(Session().Execute("hit copper", Context(rig, survival)), Does.StartWith("hit tile.copper"));
                Assert.That((float)shakeField.GetValue(follow), Is.Zero, "hit 명령은 카메라를 흔들지 않는다");

                var real = CreateSurvival();
                SetField(real, "cameraFollow", follow);
                Assert.That(real.ApplyCollapseImpact(1f), Is.True);
                Assert.That((float)shakeField.GetValue(follow), Is.GreaterThan(0f), "실제 낙석 피해는 그대로 흔든다");

                shakeField.SetValue(follow, 0f);
                var faller = CreateSurvival();
                SetField(faller, "cameraFollow", follow);
                Assert.That(Session().Execute("falling 30m", Context(rig, faller)), Does.StartWith("falling 30m"));
                Assert.That((float)shakeField.GetValue(follow), Is.Zero, "falling 명령은 카메라를 흔들지 않는다");
                Assert.That(faller.State.Health, Is.LessThan(100f));

                // 디버그 구간이 끝나면 다시 흔든다.
                var after = CreateSurvival();
                SetField(after, "cameraFollow", follow);
                Assert.That(after.ApplyCollapseImpact(1f), Is.True);
                Assert.That((float)shakeField.GetValue(follow), Is.GreaterThan(0f));
            }
            finally
            {
                AccessibilityPreferences.ReduceMotion = previousReduceMotion;
            }
        }

        [Test]
        public void Hit_DoesNotDropRocksOrMakeGas()
        {
            var rig = CreateRig(SceneNames.Integration);
            var survival = CreateSurvival();

            Assert.That(Session().Execute("hit gas", Context(rig, survival)), Does.StartWith("hit tile.gas-pocket"));

            Assert.That(rig.Overlay.transform.Find("StructuralFallingRock"), Is.Null);
        }

        [Test]
        public void HitAndFall_RejectBadArgumentsFuelGoldAndOtherScenes()
        {
            var rig = CreateRig(SceneNames.Integration);
            var survival = CreateSurvival();
            var session = Session();
            var context = Context(rig, survival);

            Assert.That(session.Execute("hit", context), Is.EqualTo("hit 인자 오류. 예: hit copper"));
            Assert.That(session.Execute("hit nope", context), Is.EqualTo("hit 인자 오류. 예: hit copper"));
            Assert.That(session.Execute("fall", context), Is.EqualTo("fall 인자 오류. 예: fall gas"));
            Assert.That(session.Execute("hit engine_fuel golden", context), Is.EqualTo("엔진연료는 골드 변형이 없습니다"));
            Assert.That(session.Execute("fall engine_fuel golden", context), Is.EqualTo("엔진연료는 골드 변형이 없습니다"));
            Assert.That(survival.State.Health, Is.EqualTo(100f));

            rig.SceneName = SceneNames.SurfaceBase;
            Assert.That(session.Execute("hit copper", context), Is.EqualTo("광산 씬에서만 사용할 수 있습니다"));
            Assert.That(session.Execute("fall copper", context), Is.EqualTo("광산 씬에서만 사용할 수 있습니다"));
            Assert.That(session.Execute("hitgas", context), Is.EqualTo("광산 씬에서만 사용할 수 있습니다"));
            Assert.That(session.Execute("falling", context), Is.EqualTo("광산 씬에서만 사용할 수 있습니다"));
            Assert.That(survival.State.Health, Is.EqualTo(100f));
        }

        [Test]
        public void Fall_StartsRealFallingRockAboveThePlayerHead()
        {
            var rig = CreateRig(SceneNames.Integration);
            var survival = CreateSurvival();
            var floor = rig.Register("tile.rock.normal", string.Empty, false);
            rig.Tilemap.SetTile(new Vector3Int(1, -2, 0), floor);

            var message = Session().Execute("fall iron golden", Context(rig, survival));

            Assert.That(message, Does.StartWith("fall tile.iron.gold ("), message);
            var rock = rig.Overlay.transform.Find("StructuralFallingRock");
            Assert.That(rock, Is.Not.Null, "실제 낙하 경로(StructuralCrackOverlay 낙석)를 타야 한다");
            var playerCell = rig.Tilemap.WorldToCell(rig.Player.Position);
            Assert.That(rig.Tilemap.WorldToCell(rock.position).x, Is.EqualTo(playerCell.x));
            Assert.That(rock.position.y, Is.GreaterThan(rig.Player.Position.y));
            Assert.That(survival.State.Health, Is.EqualTo(100f), "피해는 낙석이 닿을 때 들어간다");
        }

        [TestCase("hitgas", 1)]
        [TestCase("hitgas 1", 1)]
        [TestCase("hitgas 3", 3)]
        public void HitGas_RaisesTheGasDamageInputThroughTheEffectController(string line, int expectedMessageCount)
        {
            var rig = CreateRig(SceneNames.Integration);
            var survival = CreateSurvival();
            var inputs = new List<GasExposureFailureInputDto>();
            rig.GasEffect.FailureInputRaised += inputs.Add;

            var message = Session().Execute(line, Context(rig, survival));

            Assert.That(message, Does.StartWith("hitgas " + expectedMessageCount + "회"), message);
            Assert.That(message, Does.Contain("틱당 10"));
            // 첫 틱은 바로, 나머지는 틱 간격(1초)으로 이어진다. 간격 자체는 PlayMode에서 시간으로 본다.
            Assert.That(inputs, Has.Count.EqualTo(1));
            Assert.That(inputs[0].damage, Is.EqualTo(10));
            Assert.That(inputs[0].severity, Is.EqualTo(GasExposureFailureSeverity.Damage));
        }

        [TestCase("hitgas 0")]
        [TestCase("hitgas -1")]
        [TestCase("hitgas abc")]
        [TestCase("hitgas 1 2")]
        [TestCase("hitgas 31")]
        public void HitGas_RejectsBadCounts(string line)
        {
            var rig = CreateRig(SceneNames.Integration);
            var survival = CreateSurvival();
            var inputs = new List<GasExposureFailureInputDto>();
            rig.GasEffect.FailureInputRaised += inputs.Add;

            Assert.That(Session().Execute(line, Context(rig, survival)), Is.EqualTo("hitgas 인자 오류. 예: hitgas 3"));
            Assert.That(inputs, Is.Empty);
        }

        [TestCase("falling", 10f)]
        [TestCase("falling 5m", 10f)]
        [TestCase("falling 9.9m", 10f)]
        [TestCase("falling 10m", 10f)]
        [TestCase("falling 30m", 30f)]
        [TestCase("falling 40m", 40f)]
        [TestCase("falling 40", 40f)]
        [TestCase("falling 0", 10f)]
        [TestCase("falling -5m", 10f)]
        public void Falling_AppliesTheSameDamageAsAnActualFall(string line, float expectedDamage)
        {
            var rig = CreateRig(SceneNames.Integration);
            var survival = CreateSurvival();
            var requested = 0f;
            var tokens = line.Split(' ');
            if (tokens.Length == 2)
            {
                requested = float.Parse(tokens[1].TrimEnd('m'), System.Globalization.CultureInfo.InvariantCulture);
            }

            var message = Session().Execute(line, Context(rig, survival));

            Assert.That(message, Does.StartWith("falling "), message);
            Assert.That(100f - survival.State.Health, Is.EqualTo(expectedDamage).Within(0.01f));
            // 별도 공식이 아니라 ApplyFall이 계산한 값과 같아야 한다.
            var reference = CreateSurvival();
            reference.ApplyFall(Mathf.Max(requested, reference.MinimumFallHeight));
            Assert.That(100f - reference.State.Health, Is.EqualTo(100f - survival.State.Health).Within(0.01f));
        }

        [Test]
        public void Falling_BelowMinimumHeightEqualsMinimumHeight()
        {
            var rig = CreateRig(SceneNames.Integration);
            var low = CreateSurvival();
            var minimum = CreateSurvival();
            Session().Execute("falling 3m", Context(rig, low));
            Session().Execute("falling 10m", Context(rig, minimum));
            Assert.That(low.State.Health, Is.EqualTo(minimum.State.Health));
            Assert.That(low.State.Health, Is.LessThan(100f));
        }

        [TestCase("falling abc")]
        [TestCase("falling NaN")]
        [TestCase("falling 5m 6m")]
        public void Falling_RejectsBadHeights(string line)
        {
            var rig = CreateRig(SceneNames.Integration);
            var survival = CreateSurvival();

            Assert.That(
                Session().Execute(line, Context(rig, survival)),
                Is.EqualTo("falling 인자 오류. 예: falling 30m"));
            Assert.That(survival.State.Health, Is.EqualTo(100f));
        }

        [Test]
        public void Spawn_TargetsTheSameCellAsEnterMining()
        {
            var rig = CreateRig(SceneNames.Integration);
            var rock = rig.Register("tile.rock.normal", string.Empty, false);
            var mining = rig.Host.AddComponent<MiningSystem>();
            typeof(MiningSystem).GetField("foregroundTilemap", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(mining, rig.Tilemap);
            var method = typeof(MiningSystem).GetMethod(
                "TryGetDirectionalCell", BindingFlags.Instance | BindingFlags.NonPublic);

            var layouts = new[]
            {
                new[] { new Vector3Int(1, 0, 0) },
                new[] { new Vector3Int(1, 1, 0) },
                new[] { new Vector3Int(1, -1, 0) },
                new[] { new Vector3Int(1, 1, 0), new Vector3Int(1, -1, 0) },
                new[] { new Vector3Int(1, 0, 0), new Vector3Int(1, 1, 0) },
                new[] { new Vector3Int(5, 5, 0) },
                new Vector3Int[0]
            };
            foreach (var facing in new[] { 1f, -1f })
            {
                foreach (var layout in layouts)
                {
                    rig.Tilemap.ClearAllTiles();
                    foreach (var cell in layout)
                    {
                        rig.Tilemap.SetTile(new Vector3Int(cell.x * (int)facing, cell.y, 0), rock);
                    }

                    var args = new object[] { new Vector2(0.5f, 0.5f), facing, DeveloperDebugSpawnWorld.DefaultMiningReach, null };
                    var enterFound = (bool)method.Invoke(mining, args);
                    var spawnFound = DeveloperDebugSpawnCommands.TryResolveFacingCell(
                        rig.Tilemap,
                        new Vector2(0.5f, 0.5f),
                        facing,
                        DeveloperDebugSpawnWorld.DefaultMiningReach,
                        out var spawnCell);

                    Assert.That(spawnFound, Is.EqualTo(enterFound));
                    if (enterFound)
                    {
                        Assert.That(spawnCell, Is.EqualTo((Vector3Int)args[3]));
                    }
                }
            }
        }

        [Test]
        public void Spawn_PlacesOnEmptyStraightCellBeforeReplacingNeighbors()
        {
            var rig = CreateRig(SceneNames.Integration);
            var rock = rig.Register("tile.rock.normal", string.Empty, false);
            var iron = rig.Register("tile.iron", "mineral.iron", false);
            var session = Session();
            var context = Context(rig, CreateSurvival());

            // 위·아래에 블록이 있어도 직선 앞 빈 칸이 먼저고, 이웃 블록은 건드리지 않는다.
            rig.Tilemap.SetTile(new Vector3Int(1, 1, 0), rock);
            rig.Tilemap.SetTile(new Vector3Int(1, -1, 0), rock);
            Assert.That(session.Execute("spawn iron", context), Is.EqualTo("spawn tile.iron (1, 0)"));
            Assert.That(rig.Tilemap.GetTile(new Vector3Int(1, 0, 0)), Is.SameAs(iron));
            Assert.That(rig.Tilemap.GetTile(new Vector3Int(1, 1, 0)), Is.SameAs(rock));
            Assert.That(rig.Tilemap.GetTile(new Vector3Int(1, -1, 0)), Is.SameAs(rock));

            // 직선 앞이 막혀 있으면 그 블록을 바꾼다.
            Assert.That(session.Execute("spawn rock", context), Is.EqualTo("spawn tile.rock.normal (1, 0)"));
            Assert.That(rig.Tilemap.GetTile(new Vector3Int(1, 0, 0)), Is.SameAs(rock));
        }

        [Test]
        public void Spawn_AcceptsRockAndGoldenAliases()
        {
            var rig = CreateRig(SceneNames.Integration);
            var rock = rig.Register("tile.rock.normal", string.Empty, false);
            var goldRock = rig.Register("tile.rock.normal.gold", string.Empty, false);
            var copperGold = rig.Register("tile.copper.gold", "mineral.copper", false);
            rig.Tilemap.SetTile(new Vector3Int(1, 0, 0), rock);
            var session = Session();
            var context = Context(rig, CreateSurvival());

            Assert.That(session.Execute("spawn rock", context), Is.EqualTo("spawn tile.rock.normal (1, 0)"));
            Assert.That(session.Execute("spawn rock golden", context), Is.EqualTo("spawn tile.rock.normal.gold (1, 0)"));
            Assert.That(rig.Tilemap.GetTile(new Vector3Int(1, 0, 0)), Is.SameAs(goldRock));
            Assert.That(session.Execute("spawn normal gold", context), Is.EqualTo("spawn tile.rock.normal.gold (1, 0)"));
            Assert.That(session.Execute("spawn copper golden", context), Is.EqualTo("spawn tile.copper.gold (1, 0)"));
            Assert.That(rig.Tilemap.GetTile(new Vector3Int(1, 0, 0)), Is.SameAs(copperGold));
            Assert.That(session.Execute("spawn engine_fuel golden", context), Is.EqualTo("엔진연료는 골드 변형이 없습니다"));
        }

        [Test]
        public void Help_ListsTheNewCommands_AndSourceStaysGated()
        {
            var help = Session().Execute("help", null);
            Assert.That(help, Does.Contain("hit <rock|copper|iron|lithium|gas|engine_fuel> [golden]"));
            Assert.That(help, Does.Contain("fall <rock|copper|iron|lithium|gas|engine_fuel> [golden]"));
            Assert.That(help, Does.Contain("hitgas [횟수]"));
            Assert.That(help, Does.Contain("falling [높이m]"));
            Assert.That(help, Does.Contain("spawn <rock|copper|iron|lithium|gas|engine_fuel> [golden]"));

            var source = System.IO.File.ReadAllText(System.IO.Path.Combine(
                Application.dataPath,
                "_Project/Scripts/App/Integration/Debug/DeveloperDebugCollapseCommands.cs"));
            Assert.That(source, Does.StartWith("#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT"));
            Assert.That(source, Does.Contain("ApplyCollapseImpact"));
            Assert.That(source, Does.Contain("SimulateFall"));
            Assert.That(source, Does.Contain("TryDropTile"));
            Assert.That(source, Does.Contain("DebugRaiseDamageTicks"));
            Assert.That(source, Does.Not.Contain("SfxId"));
        }

        private static DeveloperDebugCommandSession Session()
        {
            return new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
        }

        private DeveloperDebugCommandContext Context(Rig rig, PlayerSurvivalController survival)
        {
            return new DeveloperDebugCommandContext(
                null,
                null,
                () => survival,
                null,
                null,
                null,
                null,
                () => rig.ToWorld());
        }

        private PlayerSurvivalController CreateSurvival()
        {
            var host = new GameObject("PromptB193Survival");
            created.Add(host);
            var controller = host.AddComponent<PlayerSurvivalController>();
            controller.SetHealthAbsolute(100);
            var settings = typeof(PlayerSurvivalController)
                .GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(controller) as PlayerSurvivalSettings;
            if (settings != null)
            {
                created.Add(settings);
            }

            return controller;
        }

        private Rig CreateRig(string sceneName)
        {
            var host = new GameObject("PromptB193Rig");
            created.Add(host);
            var gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(host.transform, false);
            gridObject.AddComponent<Grid>();
            var mapObject = new GameObject(DepthDarknessBlockVisual.ForegroundTilemapName);
            mapObject.transform.SetParent(gridObject.transform, false);
            var tilemap = mapObject.AddComponent<Tilemap>();
            mapObject.AddComponent<TilemapRenderer>();
            mapObject.AddComponent<TilemapCollider2D>();
            var resolver = host.AddComponent<MiningTileResolver>();
            var snapshot = host.AddComponent<SubTerra.Gameplay.Snapshot.WorldSnapshotSystem>();
            SetField(snapshot, "foregroundTilemap", tilemap);
            SetField(snapshot, "tileResolver", resolver);

            var overlay = host.AddComponent<StructuralCrackOverlay>();
            var structural = host.AddComponent<StructuralIntegritySystem>();
            var settings = ScriptableObject.CreateInstance<StructuralRiskSettings>();
            created.Add(settings);
            SetField(structural, "foregroundTilemap", tilemap);
            SetField(structural, "crackOverlay", overlay);
            SetField(structural, "riskSettings", settings);
            var gasEffect = host.AddComponent<GasExposureEffectController>();

            var playerObject = new GameObject("PromptB193Player");
            created.Add(playerObject);
            playerObject.transform.SetParent(host.transform, false);
            playerObject.transform.position = new Vector3(0.5f, 0.5f, 0f);
            playerObject.AddComponent<Rigidbody2D>();
            var movement = playerObject.AddComponent<PlayerMovement>();
            SetField(movement, "body", null);
            movement.SetMoveInput(1f);

            return new Rig
            {
                Host = host,
                Tilemap = tilemap,
                Resolver = resolver,
                Snapshot = snapshot,
                Overlay = overlay,
                Structural = structural,
                GasEffect = gasEffect,
                Player = movement,
                SceneName = sceneName,
                Catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath),
                Created = created
            }.WithAllTiles();
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }

        private sealed class Rig
        {
            public GameObject Host;
            public Tilemap Tilemap;
            public MiningTileResolver Resolver;
            public SubTerra.Gameplay.Snapshot.WorldSnapshotSystem Snapshot;
            public StructuralCrackOverlay Overlay;
            public StructuralIntegritySystem Structural;
            public GasExposureEffectController GasEffect;
            public PlayerMovement Player;
            public string SceneName;
            public GameDataCatalog Catalog;
            public List<UnityEngine.Object> Created;
            private readonly List<TileBase> tiles = new List<TileBase>();
            private readonly List<MiningTileDto> definitions = new List<MiningTileDto>();

            public Rig WithAllTiles()
            {
                Register("tile.rock.normal", string.Empty, false);
                Register("tile.rock.normal.gold", string.Empty, false);
                Register("tile.copper", "mineral.copper", false);
                Register("tile.copper.gold", "mineral.copper", false);
                Register("tile.iron", "mineral.iron", false);
                Register("tile.iron.gold", "mineral.iron", false);
                Register("tile.lithium", "mineral.lithium", true);
                Register("tile.lithium.gold", "mineral.lithium", true);
                Register("tile.gas-pocket", string.Empty, true);
                Register("tile.gas-pocket.gold", string.Empty, true);
                Register("tile.locked.signal", "item.rare.engine_fuel", false);
                return this;
            }

            public Tile Register(string tileId, string mineralId, bool containsGas)
            {
                foreach (var existing in tiles)
                {
                    if (existing != null && existing.name == tileId)
                    {
                        return (Tile)existing;
                    }
                }

                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = tileId;
                Created.Add(tile);
                tiles.Add(tile);
                definitions.Add(new MiningTileDto(
                    tileId, mineralId, 0, true, 2f, 1f, containsGas ? 0.5f : 0f, containsGas));
                Resolver.EditorSetEntries(tiles.ToArray(), definitions.ToArray());
                return tile;
            }

            public DeveloperDebugSpawnWorld ToWorld()
            {
                return new DeveloperDebugSpawnWorld(
                    SceneName, Player, Tilemap, Resolver, Snapshot)
                {
                    Structural = Structural,
                    GasEffect = GasEffect,
                    Catalog = Catalog
                };
            }
        }
    }
}
