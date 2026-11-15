using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Save;
using SubTerra.App.Tutorial;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Structural;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode.MineDemo
{
    public sealed class Issue317MineCollapseTests
    {
        [UnityTest]
        public IEnumerator Mine_OnlyMiningStartsWarning_OffscreenPauses_AndRockDamagesPlayer()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-subterra-save-root");
            Assert.That(index, Is.InRange(0, args.Length - 2));
            Assert.That(Path.GetFullPath(args[index + 1]).StartsWith(
                Path.GetFullPath("Temp") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True);
            GameBootstrapper.ResetInstanceForTests();
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap/Bootstrap.unity");
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.MainMenu, "main menu", 60f);
            var runtime = SaveRuntimeController.Instance;
            Assert.That(runtime.StartNewGame(2, true), Is.True);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.SurfaceBase && runtime.IsUiReady,
                "surface ready", 60f);
            GameBootstrapper.Instance.State.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
            GameBootstrapper.Instance.State.SetQuestRewardSettlement(string.Empty, 1);
            Assert.That(runtime.TryStartExploration(out var reason), Is.True, reason);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.Integration && runtime.IsUiReady,
                "mine ready", 60f);
            yield return UiTestEnvironment.DismissBriefing();

            var mining = Object.FindAnyObjectByType<MiningSystem>();
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            var survival = Object.FindAnyObjectByType<PlayerSurvivalController>();
            var structure = Object.FindAnyObjectByType<StructuralIntegritySystem>();
            var overlay = Object.FindAnyObjectByType<StructuralCrackOverlay>();
            var map = mining.ForegroundTilemap;
            var resolver = Object.FindAnyObjectByType<MiningTileResolver>();
            Assert.That(resolver.TryFindTileById("tile.rock.normal", out var rock), Is.True);
            var floor = map.WorldToCell(player.Position) + new Vector3Int(25, 6, 0);
            for (int x = -5; x <= 5; x++)
            for (int y = 0; y <= 4; y++)
                map.SetTile(floor + new Vector3Int(x, y, 0), y == 0 || y == 3 && Math.Abs(x) <= 4
                    || y <= 2 && Math.Abs(x) == 4 ? rock : null);
            var target = floor + Vector3Int.up * 2;
            var roof = target + Vector3Int.up;
            map.SetTile(target, rock);
            structure.RegisterProtectedCell(floor + Vector3Int.right * 4);
            var collider = map.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
            if (collider != null && collider.hasTilemapChanges) collider.ProcessTilemapChanges();
            var position = map.GetCellCenterWorld(floor + Vector3Int.up);
            var body = player.GetComponent<Rigidbody2D>();
            body.position = position;
            player.transform.position = position;
            Physics2D.SyncTransforms();
            var camera = Camera.main;
            var follow = camera.GetComponent<PlayerCameraFollow>();
            if (follow != null) follow.enabled = false;
            Vector3 visible = new Vector3(position.x, position.y, camera.transform.position.z);
            camera.transform.position = visible;
            structure.ConfigureWorldSeed(1);
            structure.RebuildRiskFromMinedCells(new[] { target + Vector3Int.right });
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(overlay.IsTelegraphing(roof), Is.False);
            Assert.That(map.HasTile(roof), Is.True);

            Assert.That(mining.TryMineInstant(target), Is.True, mining.LastFailure.ToString());
            Assert.That(overlay.IsTelegraphing(roof), Is.True, "seed 1 selects the distance-four roof");
            var offscreen = visible + Vector3.right * 100f;
            float until = Time.realtimeSinceStartup + 1.2f;
            while (Time.realtimeSinceStartup < until)
            {
                Camera.main.transform.position = offscreen;
                yield return null;
                Assert.That(map.HasTile(roof), Is.True, "offscreen roof must remain in the tilemap");
            }
            Assert.That(overlay.IsTelegraphing(roof), Is.True, "probability warning remains pending offscreen");
            Assert.That(map.HasTile(roof), Is.True);
            float health = survival.State.Health;
            camera.transform.position = visible;
            yield return UiTestWait.Until(() => !map.HasTile(roof), "visible warning elapsed", 4f);
            yield return UiTestWait.Until(() => survival.State.Health < health, "falling rock contact", 4f);
            Assert.That(health - survival.State.Health, Is.EqualTo(12.5f).Within(.01f));
        }
    }
}
