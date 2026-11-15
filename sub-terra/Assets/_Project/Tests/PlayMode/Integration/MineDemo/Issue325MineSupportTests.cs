using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.Building;
using SubTerra.Gameplay.Building;
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
    public sealed class Issue325MineSupportTests
    {
        [UnityTest]
        public IEnumerator Mine_PlacedSupportClearsExactlyTheBrightPreviewCells()
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
            var structure = Object.FindAnyObjectByType<StructuralIntegritySystem>();
            var overlay = Object.FindAnyObjectByType<StructuralCrackOverlay>();
            var placement = Object.FindAnyObjectByType<BuildingPlacementSystem>();
            var map = mining.ForegroundTilemap;
            var resolver = Object.FindAnyObjectByType<MiningTileResolver>();
            Assert.That(resolver.TryFindTileById("tile.rock.normal", out var rock), Is.True);
            var floor = map.WorldToCell(player.Position) + new Vector3Int(25, -10, 0);
            for (int x = -7; x <= 7; x++)
            for (int y = 0; y <= 4; y++)
                map.SetTile(floor + new Vector3Int(x, y, 0), y == 0 || y == 3 ? rock : null);
            var origin = floor + Vector3Int.up;
            var roof = floor + Vector3Int.up * 3;
            var position = map.GetCellCenterWorld(origin + Vector3Int.left * 2);
            var body = player.GetComponent<Rigidbody2D>();
            body.position = position;
            player.transform.position = position;
            var collider = map.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
            if (collider != null && collider.hasTilemapChanges) collider.ProcessTilemapChanges();
            Physics2D.SyncTransforms();

            Assert.That(runtime.InventoryService.TryAddMineralExact(DataIds.Minerals.Copper, 2).DidChange, Is.True);
            var binder = Object.FindAnyObjectByType<BuildingMenuBinder>(FindObjectsInactive.Include);
            Assert.That(binder.SelectBuilding(DataIds.Buildings.SupportBasic), Is.True);
            Assert.That(placement.CanPlaceAt(origin, out var failure), Is.True, failure.ToString());
            var support = placement.Selection.RuntimePrefab.GetComponent<StructuralSupport>();
            for (int x = -6; x <= 6; x++)
                structure.EvaluateStatusAt(roof + Vector3Int.right * x + Vector3Int.down);

            var preview = Object.FindAnyObjectByType<BuildingPlacementPreview>(FindObjectsInactive.Include);
            Assert.That(preview, Is.Not.Null);
            preview.SetSupportRange(map, placement.GetSelectedPlacementCenter(origin), support, structure);
            var bright = new System.Collections.Generic.HashSet<Vector3Int>();
            foreach (var marker in preview.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (marker.gameObject.activeSelf && marker.name.StartsWith("SupportRange_", StringComparison.Ordinal)
                    && marker.color.a > .5f)
                    bright.Add(map.WorldToCell(marker.transform.position));
            }

            Assert.That(bright, Has.Count.EqualTo(7));
            var previouslyCracked = new System.Collections.Generic.HashSet<Vector3Int>();
            for (int x = -6; x <= 6; x++)
            {
                var cell = roof + Vector3Int.right * x;
                if (overlay.TryGetCellRisk(cell, out _)) previouslyCracked.Add(cell);
            }

            Assert.That(placement.TryPlaceAt(origin).IsSuccess, Is.True);
            for (int x = -6; x <= 6; x++)
            {
                var cell = roof + Vector3Int.right * x;
                Assert.That(overlay.TryGetCellRisk(cell, out _),
                    Is.EqualTo(previouslyCracked.Contains(cell) && !bright.Contains(cell)), cell.ToString());
            }

            Assert.That(runtime.InventoryService.State.GetQuantity(DataIds.Minerals.Copper), Is.Zero);
        }
    }
}
