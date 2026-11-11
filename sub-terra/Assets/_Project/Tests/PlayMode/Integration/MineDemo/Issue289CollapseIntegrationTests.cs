using System;
using System.Collections;
using System.IO;
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
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode.MineDemo
{
    public sealed class Issue289CollapseIntegrationTests
    {
        [UnityTest]
        public IEnumerator BootstrapMine_FallingLithiumUsesLiveDamageAndLandingGas()
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
            var overlay = Object.FindAnyObjectByType<StructuralCrackOverlay>();
            var gas = Object.FindAnyObjectByType<GasHazardSystem>();
            var resolver = Object.FindAnyObjectByType<MiningTileResolver>();
            var map = mining.ForegroundTilemap;
            Assert.That(resolver.TryFindTileById("tile.lithium", out var lithium), Is.True);
            Assert.That(resolver.TryFindTileById("tile.rock.normal", out var rock), Is.True);
            var playerCell = map.WorldToCell(player.Position);
            var start = playerCell + Vector3Int.up * 2;
            map.SetTile(playerCell + Vector3Int.down, rock);
            map.SetTile(playerCell, null);
            map.SetTile(playerCell + Vector3Int.up, null);
            map.SetTile(start, lithium);
            Assert.That(map.GetSprite(start), Is.Not.Null);
            Assert.That(mining.TryResolveTile(lithium, out var definition), Is.True);
            Assert.That(definition.containsGas, Is.True);

            float health = survival.State.Health;
            int zones = gas.ActiveZones.Count;
            overlay.PlayCollapse(start, map, .35f);
            map.SetTile(start, null);
            yield return new WaitForSecondsRealtime(.6f);

            float actualDamage = health - survival.State.Health;
            int addedZones = gas.ActiveZones.Count - zones;
            Assert.That(actualDamage, Is.EqualTo(20f).Within(.01f), $"actual lithium damage; added gas zones: {addedZones}");
            Assert.That(addedZones, Is.EqualTo(1), "gas after landing");

            gas.ClearRestoredZones();
            var context = new DeveloperDebugCommandContext(null, null, () => survival, null);
            var commands = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.CreateIsolated());
            foreach (var sample in new[] { ("rock", 25f), ("copper", 37.5f), ("iron", 50f),
                ("lithium", 20f), ("rock golden", 32.5f), ("iron golden", 65f) })
            {
                survival.State.RestoreFull();
                health = survival.State.Health;
                Assert.That(commands.Execute("hit " + sample.Item1, context), Does.StartWith("hit "));
                Assert.That(health - survival.State.Health, Is.EqualTo(sample.Item2).Within(.01f), sample.Item1);
                Assert.That(commands.Execute("hit rock", context), Does.Not.StartWith("hit "), "invulnerability");
                Assert.That(gas.ActiveZones, Is.Empty, "hit does not release gas");
            }

            foreach (var sample in new[] { ("falling", 10f), ("falling 5m", 10f), ("falling -5m", 10f),
                ("falling 30m", 30f), ("falling 40m", 40f) })
            {
                survival.State.RestoreFull();
                health = survival.State.Health;
                Assert.That(commands.Execute(sample.Item1, context), Is.EqualTo("낙하 피해 적용"));
                Assert.That(health - survival.State.Health, Is.EqualTo(sample.Item2).Within(.01f), sample.Item1);
            }

            survival.State.RestoreFull();
            health = survival.State.Health;
            Assert.That(commands.Execute("hitgas 3", context), Is.EqualTo("hitgas 3"));
            Assert.That(survival.State.Health, Is.EqualTo(health), "gas damage starts on the first tick");
            for (int tick = 1; tick <= 3; tick++)
            {
                float expected = health - tick * 10f;
                yield return UiTestWait.Until(() => survival.State.Health <= expected, $"gas tick {tick}", 2f);
                Assert.That(survival.State.Health, Is.EqualTo(expected).Within(.01f));
            }

            foreach (var sample in new[] { ("rock", 25f, false), ("copper", 37.5f, false), ("iron", 50f, false),
                ("lithium", 20f, true), ("rock golden", 32.5f, false), ("iron golden", 65f, false), ("gas", 25f, true) })
            {
                gas.ClearRestoredZones();
                survival.State.RestoreFull();
                health = survival.State.Health;
                bool opensFromTerminal = sample.Item1 == "lithium";
                if (opensFromTerminal) UiPauseGate.Acquire(SaveRuntimeController.DebugTerminalPauseOwner);
                try
                {
                    Assert.That(commands.Execute("fall " + sample.Item1, context), Is.EqualTo("fall " + sample.Item1.Split(' ')[0]));
                    if (opensFromTerminal)
                    {
                        yield return null;
                        Assert.That(overlay.transform.Find("StructuralFallingRock"), Is.Null, "terminal is still open");
                    }
                }
                finally
                {
                    if (opensFromTerminal) UiPauseGate.Release(SaveRuntimeController.DebugTerminalPauseOwner);
                }
                yield return UiTestWait.Until(() => overlay.transform.Find("StructuralFallingRock") != null, "debug rock started", 2f);
                yield return UiTestWait.Until(() => overlay.transform.Find("StructuralFallingRock") == null, "debug rock landed", 4f);
                Assert.That(health - survival.State.Health, Is.EqualTo(sample.Item2).Within(.01f), sample.Item1);
                Assert.That(gas.ActiveZones.Count, Is.EqualTo(sample.Item3 ? 1 : 0), sample.Item1);
            }

            gas.ClearRestoredZones();
            survival.State.RestoreFull();
            health = survival.State.Health;
            var support = start + Vector3Int.down;
            map.SetTile(start + Vector3Int.up, null);
            map.SetTile(start, lithium);
            map.SetTile(support, rock);
            Assert.That(mining.TryMineInstant(support), Is.True, mining.LastFailure.ToString());
            yield return UiTestWait.Until(() => !map.HasTile(start) && gas.ActiveZones.Count == 1,
                "mining triggered a lithium collapse and landing gas", 8f);
            Assert.That(health - survival.State.Health, Is.EqualTo(20f).Within(.01f), "mining-triggered falling rock");
        }
    }
}
