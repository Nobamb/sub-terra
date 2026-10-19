using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Save;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Player;
using SubTerra.Gameplay.Snapshot;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.Save
{
    public sealed class StarterLadderPlayModeTests
    {
        [UnityTest]
        public IEnumerator NewGame_HasNoStarterLadder_ButSavedPlayerLadderSurvivesContinue()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-subterra-save-root");
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            var root = Path.GetFullPath(args[index + 1]);
            var temp = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp"));
            Assert.That(root.StartsWith(temp + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True);
            GameBootstrapper.ResetInstanceForTests();
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap/Bootstrap.unity");
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == "MainMenu");
            var runtime = SaveRuntimeController.Instance;
            try
            {
                Assert.That(runtime.StartNewGame(3, true), Is.True);
                yield return WaitUntil(() => SceneManager.GetActiveScene().name == "SurfaceBase" && runtime.IsUiReady);
                Assert.That(runtime.TryStartExploration(out var reason), Is.True, reason);
                yield return WaitUntil(() => SceneManager.GetActiveScene().name == SceneNames.Integration && runtime.IsUiReady);
                Assert.That(UnityEngine.Object.FindObjectsByType<LadderZone>(FindObjectsInactive.Include), Is.Empty);

                // 기존 저장과 같은 건설 DTO로 플레이어 소유 사다리를 복원한다.
                var world = UnityEngine.Object.FindAnyObjectByType<WorldSnapshotSystem>();
                var snapshot = world.CaptureSnapshot();
                snapshot.buildings.Add(new BuildingSnapshotDto
                {
                    instanceId = "saved-player-ladder-0001",
                    buildingTypeId = "building.ladder.basic",
                    x = -10,
                    y = -7,
                    footprintWidth = 1,
                    footprintHeight = 5
                });
                Assert.That(world.RestoreSnapshot(snapshot), Is.True);
                yield return null;
                AssertSavedLadder();
                Assert.That(runtime.SaveCurrent().IsSuccess, Is.True);
                bool completed = false;
                bool success = false;
                runtime.BeginContinue(3, result => { completed = true; success = result.IsSuccess; });
                yield return WaitUntil(() => completed);
                Assert.That(success, Is.True);
                AssertSavedLadder();
            }
            finally
            {
                if (runtime != null) UnityEngine.Object.Destroy(runtime.gameObject);
                GameBootstrapper.ResetInstanceForTests();
            }
            yield return null;
        }

        private static void AssertSavedLadder()
        {
            var ladders = UnityEngine.Object.FindObjectsByType<LadderZone>(FindObjectsInactive.Include);
            Assert.That(ladders.Length, Is.EqualTo(1));
            Assert.That(ladders[0].GetComponent<BuildingInstance>().InstanceId, Is.EqualTo("saved-player-ladder-0001"));
            Assert.That(ladders[0].GetComponent<BuildingInstance>().BuildingId, Is.EqualTo("building.ladder.basic"));
            Assert.That(UnityEngine.Object.FindAnyObjectByType<WorldSnapshotSystem>().CaptureSnapshot().buildings
                .Any(item => item.instanceId == "saved-player-ladder-0001" && item.x == -10 && item.y == -7), Is.True);
        }

        private static IEnumerator WaitUntil(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 25f;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Runtime transition timed out.");
        }
    }
}
