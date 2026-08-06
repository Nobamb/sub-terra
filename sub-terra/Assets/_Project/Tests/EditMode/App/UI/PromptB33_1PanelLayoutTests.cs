using System.Linq;
using NUnit.Framework;
using SubTerra.App.UI;
using SubTerra.App.UI.Inventory;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>
    /// Verifies the current Phase Q panel layout without rebuilding or mutating
    /// the integration scene during the EditMode test run.
    /// </summary>
    public sealed class PromptB33_1PanelLayoutTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [Test]
        public void IntegrationScene_HasSingleHiddenInventoryPanel()
        {
            var scene = OpenIntegration();
            var layout = FindTransform(scene, "PanelLayout");
            Assert.That(layout, Is.Not.Null);

            var panels = layout.GetComponentsInChildren<InventoryPanelView>(true);
            Assert.That(panels.Length, Is.EqualTo(1));
            Assert.That(panels[0].gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ShortcutBar_UsesInventoryLabelAndPanelToggle()
        {
            var scene = OpenIntegration();
            var bar = FindTransform(scene, "PanelShortcutBar");
            var controller = FindTransform(scene, "PanelLayout")
                ?.GetComponent<PanelToggleController>();
            Assert.That(bar, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);

            var inventoryButton = bar.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button =>
                {
                    var label = button.GetComponentInChildren<TMP_Text>(true);
                    return label != null && label.text != null && label.text.Contains("[I]");
                });
            Assert.That(inventoryButton, Is.Not.Null);

            var wired = Enumerable.Range(0, inventoryButton.onClick.GetPersistentEventCount())
                .Any(index => inventoryButton.onClick.GetPersistentTarget(index) == controller
                    && inventoryButton.onClick.GetPersistentMethodName(index)
                        == nameof(PanelToggleController.ToggleInventory));
            Assert.That(wired, Is.True);
        }

        [Test]
        public void IntegrationScene_UsesSinglePanelToggleController()
        {
            var scene = OpenIntegration();
            var layout = FindTransform(scene, "PanelLayout");
            Assert.That(layout, Is.Not.Null);
            Assert.That(layout.GetComponents<PanelToggleController>().Length, Is.EqualTo(1));
        }

        private static Scene OpenIntegration()
        {
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static Transform FindTransform(Scene scene, string objectName)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(transform => transform.name == objectName);
        }
    }
}
