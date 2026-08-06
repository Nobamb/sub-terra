using System.Linq;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.UI.RunFailure;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.Integration
{
    public sealed class PhaseLRunFailureStaticTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [Test]
        public void L_S03_IntegrationScene_WiresSurvivalFailureUiAndAllGameplayInput()
        {
            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                var roots = scene.GetRootGameObjects();
                var controller = Find<RunFailureRuntimeController>(roots);
                var survival = Find<PlayerSurvivalController>(roots);
                var view = Find<RunFailurePanelView>(roots);
                var binder = Find<IntegrationRuntimeBinder>(roots);
                Assert.That(controller, Is.Not.Null);
                Assert.That(survival, Is.Not.Null);
                Assert.That(view, Is.Not.Null);
                Assert.That(view.HasRequiredReferences(), Is.True);

                var controllerObject = new SerializedObject(controller);
                Assert.That(controllerObject.FindProperty("survivalController").objectReferenceValue,
                    Is.SameAs(survival));
                Assert.That(controllerObject.FindProperty("failureView").objectReferenceValue,
                    Is.SameAs(view));
                Assert.That(controllerObject.FindProperty("playerMovement").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(controllerObject.FindProperty("localSurfaceFallback").objectReferenceValue,
                    Is.Not.Null);

                var inputs = controllerObject.FindProperty("gameplayInputBehaviours");
                Assert.That(Contains<PlayerController>(inputs), Is.True, "movement input must lock");
                Assert.That(Contains<PlayerMiningController>(inputs), Is.True, "mining input must lock");
                Assert.That(
                    Contains<BuildingPlacementInput>(inputs)
                    || Contains<GameplayBuildingPlacementBridge>(inputs),
                    Is.True,
                    "building input must lock");

                var blocker = view.GetComponent<UnityEngine.UI.Image>();
                var blockerRect = view.GetComponent<RectTransform>();
                Assert.That(blocker, Is.Not.Null);
                Assert.That(blocker.raycastTarget, Is.True, "failure overlay must block HUD clicks");
                Assert.That(blockerRect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(blockerRect.anchorMax, Is.EqualTo(Vector2.one));

                var binderObject = new SerializedObject(binder);
                Assert.That(binderObject.FindProperty("runFailureController").objectReferenceValue,
                    Is.SameAs(controller));
            }
            finally
            {
                if (!string.IsNullOrEmpty(previous))
                {
                    EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
                }
            }
        }

        private static T Find<T>(GameObject[] roots) where T : Component
        {
            return roots.SelectMany(root => root.GetComponentsInChildren<T>(true)).FirstOrDefault();
        }

        private static bool Contains<T>(SerializedProperty array) where T : Behaviour
        {
            for (var i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue is T)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
