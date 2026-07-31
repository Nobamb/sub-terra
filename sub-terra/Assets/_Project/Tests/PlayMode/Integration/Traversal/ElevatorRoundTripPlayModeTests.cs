using System.Collections;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Integration;
using SubTerra.Gameplay.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.Traversal
{
    public sealed class ElevatorRoundTripPlayModeTests
    {
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
