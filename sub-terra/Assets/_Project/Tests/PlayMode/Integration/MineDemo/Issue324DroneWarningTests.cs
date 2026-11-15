using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone;
using SubTerra.App.Integration;
using SubTerra.App.UI.Drone;
using SubTerra.App.Save;
using SubTerra.App.Tutorial;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Drone;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Structural;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode.MineDemo
{
    public sealed class Issue324DroneWarningTests
    {
        [UnityTest]
        public IEnumerator Mine_KeyboardTargetForecastsCollapseBeforeRemovingSupport()
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
            var structure = Object.FindAnyObjectByType<StructuralIntegritySystem>();
            var overlay = Object.FindAnyObjectByType<StructuralCrackOverlay>();
            var drone = Object.FindAnyObjectByType<DroneSensor>();
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            var map = mining.ForegroundTilemap;
            var resolver = Object.FindAnyObjectByType<MiningTileResolver>();
            Assert.That(resolver.TryFindTileById("tile.rock.normal", out var rock), Is.True);
            var floor = map.WorldToCell(player.Position) + new Vector3Int(25, 6, 0);
            for (int x = -8; x <= 4; x++)
            for (int y = 0; y <= 4; y++)
                map.SetTile(floor + new Vector3Int(x, y, 0),
                    y == 0 || y == 3 || y <= 2 && (x == -8 || x == 4) ? rock : null);
            structure.RegisterProtectedCell(floor + Vector3Int.left * 8);
            var support = floor + new Vector3Int(4, 2, 0);
            var position = map.GetCellCenterWorld(floor + new Vector3Int(3, 2, 0));
            var body = player.GetComponent<Rigidbody2D>();
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            body.position = position;
            player.transform.position = position;
            Physics2D.SyncTransforms();

            var keyboardMining = player.GetComponent<PlayerMiningController>();
            Assert.That(keyboardMining.TryGetPredictedTargetCell(out var predicted), Is.True);
            Assert.That(predicted, Is.EqualTo(support));
            Assert.That(drone.CaptureContext().StructuralRisk, Is.EqualTo(StructuralRiskLevel.Stable));

            var adapter = Object.FindAnyObjectByType<DroneContextProviderAdapter>();
            var hud = Object.FindAnyObjectByType<DroneUiBinder>();
            Assert.That(adapter, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            var frontRoof = floor + new Vector3Int(3, 3, 0);
            bool visibleBefore = overlay.HasVisibleCell(frontRoof);
            int shakesBefore = overlay.ShakingCellCount;
            var failureBefore = mining.LastFailure;
            int structuralEvents = 0;
            structure.StructuralStatusChanged += () => structuralEvents++;

            var forecast = adapter.ReadStructuralForecast();
            Assert.That(forecast.HasValue, Is.True);
            Assert.That(forecast.Value.Warning, Is.EqualTo(DroneStructuralWarning.Collapse));
            Assert.That(forecast.Value.Direction, Is.EqualTo(-1));
            var analysis = hud.AnalyzeNow();
            Assert.That(analysis.Dialogue.TemplateId, Is.EqualTo(DataIds.Dialogue.DroneStructuralWarning));
            Assert.That(analysis.Dialogue.IsUrgent, Is.True);
            Assert.That(overlay.HasVisibleCell(frontRoof), Is.EqualTo(visibleBefore));
            Assert.That(overlay.ShakingCellCount, Is.EqualTo(shakesBefore));
            Assert.That(structuralEvents, Is.Zero);
            Assert.That(mining.LastFailure, Is.EqualTo(failureBefore));
            Assert.That(mining.IsMining, Is.False);
            Assert.That(map.HasTile(support), Is.True);

            using var input = new UiTestEnvironment();
            input.BindInput();
            yield return UiTestWait.FocusGameView();
            input.Keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(input.Keyboard, new KeyboardState(Key.Enter));
            try
            {
                yield return UiTestWait.Until(() => mining.IsMining, "Enter starts mining", 3f);
                Assert.That(mining.ActiveCell, Is.EqualTo(predicted));
                yield return UiTestWait.Until(() => !map.HasTile(support), "Enter mines the predicted target", 6f);
            }
            finally
            {
                InputSystem.QueueStateEvent(input.Keyboard, new KeyboardState());
            }
            Assert.That(adapter.ReadStructuralForecast()?.Warning, Is.EqualTo(DroneStructuralWarning.Telegraphing));
        }
    }
}
