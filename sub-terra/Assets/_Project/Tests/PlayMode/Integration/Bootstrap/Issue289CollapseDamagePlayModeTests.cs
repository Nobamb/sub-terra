using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Integration;
using SubTerra.App.Save;
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode
{
    /// <summary>
    /// B-193: Bootstrap에서 실제 광산 씬으로 들어가, 테스트용 람다가 아니라 씬에 배선된
    /// 낙석 배율 콜백과 착지 가스 구독이 동작하는지 본다.
    /// </summary>
    public sealed class Issue289CollapseDamagePlayModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string TracePath = "Temp/b193-trace.log";

        private UiTestEnvironment environment;
        private readonly StringBuilder trace = new StringBuilder();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (trace.Length > 0) File.WriteAllText(TracePath, trace.ToString());
            trace.Clear();
            if (environment != null) environment.Dispose();
            environment = null;
            var clean = SceneManager.CreateScene("B193Cleanup-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(clean);
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != clean && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator LiveMine_FallingRocksHitByKindAndGold()
        {
            yield return BootToMine();
            var world = Live();
            var overlay = Overlay();
            Log("receiver bound=" + (Field(overlay, "collapseDamageReceiver") != null)
                + " multiplier bound=" + (Field(overlay, "resolveCollapseDamageMultiplier") != null));

            var cases = new (string id, float expected)[]
            {
                ("tile.rock.normal", 25f), ("tile.copper", 37.5f), ("tile.iron", 50f),
                ("tile.lithium", 20f), ("tile.rock.normal.gold", 32.5f), ("tile.iron.gold", 65f)
            };
            foreach (var (id, expected) in cases)
            {
                yield return WaitInvulnerabilityEnd(world.Survival);
                world.Survival.SetHealthAbsolute(100);
                var before = world.Survival.State.Health;
                Assert.That(world.Resolver.TryFindTileById(id, out var tile), Is.True, id);
                var cell = DropAbovePlayer(world, tile);
                yield return new WaitForSecondsRealtime(0.9f);
                var damage = before - world.Survival.State.Health;
                Log(id + " damage=" + damage);
                Assert.That(damage, Is.EqualTo(expected).Within(0.01f), id);
            }
        }

        [UnityTest]
        public IEnumerator LiveMine_NaturalCollapseAndContinue_KeepDamageAndLandingGas()
        {
            yield return BootToMine();
            var runtime = SaveRuntimeController.Instance;
            Assert.That(runtime.SaveCurrent().IsSuccess, Is.True);
            ContinueResult continued = null;
            runtime.BeginContinue(1, result => continued = result);
            yield return UiTestWait.Until(() => continued != null, "continue complete", 30f);
            Assert.That(continued.IsSuccess, Is.True, continued.Status.ToString());
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.Integration
                && runtime.IsUiReady, "continued mine ready", 30f);
            yield return UiTestEnvironment.DismissBriefing();
            yield return new WaitForSecondsRealtime(1f);

            var world = Live();
            Log("continued receiver bound=" + (Field(world.Overlay, "collapseDamageReceiver") != null)
                + " multiplier bound=" + (Field(world.Overlay, "resolveCollapseDamageMultiplier") != null));
            var structural = Object.FindAnyObjectByType<StructuralIntegritySystem>();
            var collapse = typeof(StructuralIntegritySystem).GetMethod("CollapseUnsupportedCeiling", Private);

            var expectations = new Dictionary<string, (float damage, int zones)>
            {
                { "tile.iron", (50f, 0) },
                { "tile.gas-pocket", (25f, 1) },
                { "tile.lithium.gold", (26f, 1) }
            };
            foreach (var id in new[] { "tile.iron", "tile.gas-pocket", "tile.lithium.gold" })
            {
                yield return WaitInvulnerabilityEnd(world.Survival);
                world.Survival.SetHealthAbsolute(100);
                var before = world.Survival.State.Health;
                var zones = world.Gas.ActiveZones.Count;
                Assert.That(world.Resolver.TryFindTileById(id, out var tile), Is.True, id);
                var playerCell = world.Foreground.WorldToCell(world.Player.transform.position);
                var cell = playerCell + new Vector3Int(0, 3, 0);
                world.Foreground.SetTile(cell, tile);
                collapse.Invoke(structural, new object[] { new List<Vector3Int> { cell } });
                Log(id + " tile still present after collapse=" + world.Foreground.HasTile(cell));
                yield return new WaitForSecondsRealtime(1.2f);
                var damage = before - world.Survival.State.Health;
                var added = world.Gas.ActiveZones.Count - zones;
                Log(id + " damage=" + damage + " zonesAdded=" + added);
                Assert.That(damage, Is.EqualTo(expectations[id].damage).Within(0.01f), id);
                Assert.That(added, Is.EqualTo(expectations[id].zones), id);
            }
        }

        [UnityTest]
        public IEnumerator LiveMine_DebugTerminalCommands_UseTheRealPaths()
        {
            yield return BootToMine();
            var world = Live();
            var runtime = SaveRuntimeController.Instance;
            var session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            var context = new DeveloperDebugCommandContext(
                GameBootstrapper.Instance.State, runtime.InventoryService, () => world.Survival, null);

            // hit: 낙석 피해만. 종류별 배율, 낙석 오브젝트나 가스는 없다.
            var hits = new (string line, float expected)[]
            {
                ("hit rock", 25f), ("hit copper", 37.5f), ("hit iron", 50f), ("hit lithium", 20f),
                ("hit rock golden", 32.5f), ("hit iron golden", 65f)
            };
            var zonesBefore = world.Gas.ActiveZones.Count;
            foreach (var (line, expected) in hits)
            {
                yield return WaitInvulnerabilityEnd(world.Survival);
                world.Survival.SetHealthAbsolute(100);
                var message = session.Execute(line, context);
                Log(line + " -> " + message);
                Assert.That(100f - world.Survival.State.Health, Is.EqualTo(expected).Within(0.01f), line);
            }

            Assert.That(world.Gas.ActiveZones.Count, Is.EqualTo(zonesBefore), "hit은 착지 가스를 만들지 않는다");
            yield return WaitInvulnerabilityEnd(world.Survival);
            world.Survival.SetHealthAbsolute(100);
            Assert.That(session.Execute("hit copper", context), Does.StartWith("hit tile.copper"));
            Assert.That(session.Execute("hit copper", context), Does.StartWith("피해 없음"), "무적 시간 동안은 겹치지 않는다");
            Assert.That(world.Survival.State.Health, Is.EqualTo(62.5f).Within(0.01f));
            yield return WaitInvulnerabilityEnd(world.Survival);
            Assert.That(session.Execute("hit copper", context), Does.StartWith("hit tile.copper"));
            Assert.That(world.Survival.State.Health, Is.EqualTo(25f).Within(0.01f), "무적이 끝나면 다시 들어간다");

            // fall: 머리 위에서 실제로 떨어져 피해와 (가스류) 착지 가스가 함께 나온다.
            var falls = new (string line, float damage, int zones)[]
            {
                ("fall gas", 25f, 1), ("fall lithium", 20f, 1), ("fall lithium golden", 26f, 1),
                ("fall copper", 37.5f, 0), ("fall rock golden", 32.5f, 0)
            };
            foreach (var (line, damage, zones) in falls)
            {
                yield return WaitInvulnerabilityEnd(world.Survival);
                // 앞선 낙석이 남긴 가스가 도트 피해로 무적을 먼저 소모하지 않게 비운다.
                world.Gas.ClearRestoredZones();
                world.Survival.SetHealthAbsolute(100);
                var zoneCount = world.Gas.ActiveZones.Count;
                var message = session.Execute(line, context);
                Assert.That(message, Does.StartWith("fall tile."), message);
                Assert.That(world.Gas.ActiveZones.Count, Is.EqualTo(zoneCount), "낙하 도중에는 가스가 없다");
                yield return new WaitForSecondsRealtime(1.2f);
                Log(line + " -> health=" + world.Survival.State.Health + " zonesAdded=" + (world.Gas.ActiveZones.Count - zoneCount));
                Assert.That(100f - world.Survival.State.Health, Is.EqualTo(damage).Within(0.01f), line);
                Assert.That(world.Gas.ActiveZones.Count - zoneCount, Is.EqualTo(zones), line);
            }

            // hitgas: 첫 틱은 바로, 이후 1초 간격으로 10씩.
            yield return WaitInvulnerabilityEnd(world.Survival);
            world.Survival.SetHealthAbsolute(100);
            Assert.That(session.Execute("hitgas", context), Does.StartWith("hitgas 1회"));
            Assert.That(world.Survival.State.Health, Is.EqualTo(90f).Within(0.01f));
            yield return new WaitForSeconds(1.2f);
            Assert.That(world.Survival.State.Health, Is.EqualTo(90f).Within(0.01f), "hitgas는 한 번만");

            world.Survival.SetHealthAbsolute(100);
            yield return WaitInvulnerabilityEnd(world.Survival);
            Assert.That(session.Execute("hitgas 3", context), Does.StartWith("hitgas 3회"));
            Assert.That(world.Survival.State.Health, Is.EqualTo(90f).Within(0.01f), "첫 틱은 바로");
            yield return new WaitForSeconds(0.5f);
            Assert.That(world.Survival.State.Health, Is.EqualTo(90f).Within(0.01f), "틱 간격 전에는 그대로");
            yield return new WaitForSeconds(0.8f);
            Assert.That(world.Survival.State.Health, Is.EqualTo(80f).Within(0.01f), "1초 뒤 두 번째");
            yield return new WaitForSeconds(1.0f);
            Assert.That(world.Survival.State.Health, Is.EqualTo(70f).Within(0.01f), "2초 뒤 세 번째");
            yield return new WaitForSeconds(1.2f);
            Assert.That(world.Survival.State.Health, Is.EqualTo(70f).Within(0.01f), "세 번에서 멈춘다");

            // falling: 10 / 30 / 40m, 10m 미만은 10m와 같다.
            foreach (var (line, damage) in new[]
                { ("falling", 10f), ("falling 5m", 10f), ("falling 30m", 30f), ("falling 40m", 40f) })
            {
                yield return WaitInvulnerabilityEnd(world.Survival);
                world.Survival.SetHealthAbsolute(100);
                var message = session.Execute(line, context);
                Log(line + " -> " + message);
                Assert.That(100f - world.Survival.State.Health, Is.EqualTo(damage).Within(0.01f), line);
            }

            // spawn: 바라보는 직선의 빈 칸을 먼저 쓰고, 막혀 있으면 그 블록을 바꾼다.
            var live = DeveloperDebugSpawnCommands.FindLive();
            var map = live.Foreground;
            var origin = live.Player.Position;
            var facing = live.Player.FacingDirection;
            var sideCell = map.WorldToCell(origin) + new Vector3Int(facing > 0f ? 1 : -1, 0, 0);
            Assert.That(world.Resolver.TryFindTileById("tile.rock.normal", out var rockTile), Is.True);
            map.SetTile(sideCell + Vector3Int.down, null);
            map.SetTile(sideCell, null);
            map.SetTile(sideCell + Vector3Int.up, rockTile);
            Assert.That(world.Resolver.TryFindTileById("tile.iron", out var ironTile), Is.True);
            var spawned = session.Execute("spawn iron", context);
            Log("spawn(empty) -> " + spawned);
            Assert.That(spawned, Does.StartWith("spawn tile.iron ("));
            Assert.That(map.GetTile(sideCell), Is.SameAs(ironTile), "직선 앞 빈 칸에 놓는다");
            Assert.That(map.GetTile(sideCell + Vector3Int.up), Is.SameAs(rockTile), "이웃 블록은 건드리지 않는다");

            spawned = session.Execute("spawn iron golden", context);
            Log("spawn(blocked) -> " + spawned);
            Assert.That(spawned, Does.StartWith("spawn tile.iron.gold ("));
            Assert.That(world.Resolver.TryFindTileById("tile.iron.gold", out var goldIron), Is.True);
            Assert.That(map.GetTile(sideCell), Is.SameAs(goldIron), "막혀 있으면 그 블록을 바꾼다");
        }

        private Vector3Int DropAbovePlayer(LiveWorld world, TileBase tile)
        {
            var playerCell = world.Foreground.WorldToCell(world.Player.transform.position);
            var cell = playerCell + new Vector3Int(0, 3, 0);
            world.Foreground.SetTile(cell, tile);
            world.Overlay.PlayCollapse(cell, world.Foreground, 0.4f);
            world.Foreground.SetTile(cell, null);
            return cell;
        }

        private static IEnumerator WaitInvulnerabilityEnd(PlayerSurvivalController survival)
        {
            yield return UiTestWait.Until(
                () => !survival.State.IsInvulnerable(Time.unscaledTime), "invulnerability end", 3f);
        }

        private sealed class LiveWorld
        {
            public PlayerSurvivalController Survival;
            public PlayerMovement Player;
            public Tilemap Foreground;
            public MiningTileResolver Resolver;
            public StructuralCrackOverlay Overlay;
            public GasHazardSystem Gas;
        }

        private LiveWorld Live()
        {
            var failure = Object.FindAnyObjectByType<RunFailureRuntimeController>();
            Assert.That(failure, Is.Not.Null);
            var debugWorld = DeveloperDebugSpawnCommands.FindLive();
            return new LiveWorld
            {
                Survival = failure.SurvivalController,
                Player = Object.FindAnyObjectByType<PlayerMovement>(),
                Foreground = debugWorld.Foreground,
                Resolver = debugWorld.Resolver,
                Overlay = Overlay(),
                Gas = Object.FindAnyObjectByType<GasHazardSystem>()
            };
        }

        private static StructuralCrackOverlay Overlay()
        {
            var structural = Object.FindAnyObjectByType<StructuralIntegritySystem>();
            Assert.That(structural, Is.Not.Null);
            return (StructuralCrackOverlay)typeof(StructuralIntegritySystem)
                .GetField("crackOverlay", Private).GetValue(structural);
        }

        private static object Field(object target, string name)
        {
            return target.GetType().GetField(name, Private).GetValue(target);
        }

        private void Log(string line)
        {
            trace.AppendLine(line);
        }

        private IEnumerator BootToMine()
        {
            environment = new UiTestEnvironment();
            GameBootstrapper.ResetInstanceForTests();
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap);
            yield return UiTestWait.Until(() => SaveRuntimeController.Instance != null
                && SaveRuntimeController.Instance.InventoryService != null
                && SceneManager.GetActiveScene().name == SceneNames.MainMenu, "Bootstrap runtime and MainMenu", 20f);
            var runtime = SaveRuntimeController.Instance;
            var fs = new MemoryFileSystem();
            var paths = new SavePathPolicy(Path.Combine(Path.GetTempPath(), "subterra-b193-memory"));
            var mapper = new SaveDataMapper(new SystemSaveClock());
            var json = new SaveJsonCodec(new SaveMigrationService());
            typeof(SaveRuntimeController).GetField("saveService", Private)
                .SetValue(runtime, new SaveService(fs, paths, mapper, json));
            typeof(SaveRuntimeController).GetField("loadService", Private)
                .SetValue(runtime, new LoadService(fs, paths, mapper, json));
            Assert.That(runtime.StartNewGame(1), Is.True);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.SurfaceBase
                && runtime.IsUiReady, "memory-slot SurfaceBase", 20f);
            var state = GameBootstrapper.Instance.State;
            state.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
            state.SetQuestRewardSettlement(string.Empty, 1);
            Assert.That(runtime.TryStartExploration(out var reason), Is.True, reason);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.Integration
                && runtime.IsUiReady, "mine ready", 20f);
            yield return UiTestEnvironment.DismissBriefing();
            yield return UiTestWait.Until(() => !ControlPreferences.IsSettingsOpen && !UiPauseGate.IsHeld,
                "mine input ready", 5f, "animation");
        }

        private sealed class MemoryFileSystem : ISaveFileSystem
        {
            private readonly Dictionary<string, string> files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public bool FileExists(string path) => files.ContainsKey(path);
            public void CreateDirectory(string path) { }
            public void WriteAllText(string path, string contents) { files[path] = contents; }
            public string ReadAllText(string path) => files[path];
            public void DeleteFile(string path) { files.Remove(path); }
            public void MoveFile(string sourcePath, string destinationPath)
            {
                files[destinationPath] = files[sourcePath];
                files.Remove(sourcePath);
            }
        }
    }
}
