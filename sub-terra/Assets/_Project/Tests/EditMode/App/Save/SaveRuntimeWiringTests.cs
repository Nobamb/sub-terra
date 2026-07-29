using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Save;
using SubTerra.App.UI.Save;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.Save
{
    public sealed class SaveRuntimeWiringTests
    {
        private const string BootstrapPath =
            "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        private const string MainMenuPath =
            "Assets/_Project/Scenes/App/MainMenu.unity";
        private const string IntegrationPath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [Test]
        public void K_S01_RuntimeScenes_HaveSaveRootMenuAndLoadableIntegration()
        {
            var bootstrapScene = EditorSceneManager.OpenScene(
                BootstrapPath,
                OpenSceneMode.Additive);
            try
            {
                Assert.That(
                    FindInScene<GameBootstrapper>(bootstrapScene),
                    Is.Not.Null);
                Assert.That(
                    FindInScene<SaveRuntimeController>(bootstrapScene),
                    Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(bootstrapScene, true);
            }

            var mainMenuScene = EditorSceneManager.OpenScene(
                MainMenuPath,
                OpenSceneMode.Additive);
            try
            {
                var binder = FindInScene<SaveSlotPanelBinder>(mainMenuScene);
                Assert.That(binder, Is.Not.Null);
                Assert.That(binder.HasRequiredReferences(), Is.True);
                Assert.That(FindInScene<EventSystem>(mainMenuScene), Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(mainMenuScene, true);
            }

            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(IntegrationPath),
                Is.Not.Null);
            Assert.That(
                EditorBuildSettings.scenes.Any(
                    scene => scene.enabled && scene.path == IntegrationPath),
                Is.True);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
