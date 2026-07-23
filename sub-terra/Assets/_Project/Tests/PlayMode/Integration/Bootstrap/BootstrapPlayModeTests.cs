using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SubTerra.App.Core;
using SubTerra.App.State;

namespace SubTerra.App.Tests
{
    public sealed class BootstrapPlayModeTests
    {
        [UnityTest]
        public IEnumerator BootstrapScene_CreatesSinglePersistentRoot()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap);
            yield return null;
            Assert.That(GameBootstrapper.Instance, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<GameBootstrapper>(), Has.Length.EqualTo(1));
            Assert.That(GameBootstrapper.Instance.State, Is.Not.Null);
            Assert.That(GameState.IsComplete(GameBootstrapper.Instance.State), Is.True);
            Assert.That(GameBootstrapper.Instance.InitializationFailed, Is.False);
        }

        [UnityTest]
        public IEnumerator BootstrapReload_KeepsSingleRootAndDoesNotOverwriteState()
        {
            // A-F02: Bootstrap 재로드 시 중복 Root는 폐기되고 기존 State가 유지된다.
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap);
            // Start/씬 전환 프레임을 충분히 기다린다.
            yield return null;
            yield return null;

            var root = GameBootstrapper.Instance;
            Assert.That(root, Is.Not.Null);
            Assert.That(root.IsInitialized, Is.True);
            Assert.That(GameState.IsComplete(root.State), Is.True);

            root.State.Player.AddGold(11);
            var ownedState = root.State;

            // Bootstrap 씬을 다시 로드하면 새 GameBootstrapper가 Awake에서 폐기된다.
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap);
            yield return null;
            yield return null;

            Assert.That(GameBootstrapper.Instance, Is.SameAs(root));
            Assert.That(Object.FindObjectsByType<GameBootstrapper>(), Has.Length.EqualTo(1));
            Assert.That(GameBootstrapper.Instance.State, Is.SameAs(ownedState));
            Assert.That(GameBootstrapper.Instance.State.Player.Gold, Is.EqualTo(11));
            Assert.That(GameBootstrapper.Instance.InitializationFailed, Is.False);
        }
    }
}
