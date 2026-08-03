using System.Collections;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Integration;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.Gameplay.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.Traversal
{
    public sealed class ElevatorRoundTripPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameBootstrapper.ResetInstanceForTests();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SurfaceArrival_RechargesEnergyToMaximum()
        {
            GameBootstrapper.ResetInstanceForTests();
            var root = new GameObject("SurfaceArrivalRuntime");
            var bootstrap = root.AddComponent<GameBootstrapper>();
            bootstrap.enabled = false;
            var state = GameState.CreateNew();
            Assert.That(bootstrap.TryReplaceState(state), Is.True);
            root.AddComponent<SaveRuntimeController>();

            yield return null;
            yield return null;
            state.SetCurrentEnergy(0);

            SceneManager.LoadScene(SceneNames.SurfaceBase);
            yield return null;

            Assert.That(state.Player.Energy, Is.EqualTo(state.Player.MaxEnergy));
        }

        [UnityTest]
        public IEnumerator SurfaceMineSurface_ScenesAndStationsRemainUsable()
        {
            SceneManager.LoadScene(SceneNames.SurfaceBase);
            yield return null;
            Assert.AreEqual(SceneNames.SurfaceBase, SceneManager.GetActiveScene().name);

            SceneManager.LoadScene(SceneNames.Integration);
            yield return null;
            yield return null;
            Assert.AreEqual(SceneNames.Integration, SceneManager.GetActiveScene().name);
            Assert.NotNull(Object.FindFirstObjectByType<ElevatorController>());
            Assert.NotNull(Object.FindFirstObjectByType<ElevatorTravelBridge>());

            SceneManager.LoadScene(SceneNames.SurfaceBase);
            yield return null;
            Assert.AreEqual(SceneNames.SurfaceBase, SceneManager.GetActiveScene().name);
        }
    }
}
