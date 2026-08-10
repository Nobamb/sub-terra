using NUnit.Framework;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.MainMenu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB42UiLayerTests
    {
        [Test]
        public void UiModalLayers_AreAboveSurfaceBaseAndHudLayers()
        {
            Assert.That(UiLayerPriority.SettingsModal, Is.GreaterThan(UiLayerPriority.TutorialGuidance));
            Assert.That(UiLayerPriority.ModalPanel, Is.GreaterThan(UiLayerPriority.SettingsModal));
            Assert.That(UiLayerPriority.ModalPanel, Is.GreaterThan(UiLayerPriority.CriticalHazard));
        }

        [Test]
        public void MainMenuSettingsAndSelectedSlot_UseTheRequiredVisualPriority()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/MainMenuPanel.prefab");
            var instance = Object.Instantiate(prefab);
            try
            {
                var view = instance.GetComponent<MainMenuView>();
                view.SetSettingsVisible(true);

                var settingsCanvas = FindChild(instance.transform, "SettingsPanel").GetComponent<Canvas>();
                Assert.That(settingsCanvas.overrideSorting, Is.True);
                Assert.That(settingsCanvas.sortingOrder, Is.EqualTo(UiLayerPriority.SettingsModal));

                view.SetSelectedSlot(2, false, string.Empty);
                var slot1 = FindChild(instance.transform, "Slot1").GetComponent<Button>();
                var slot2 = FindChild(instance.transform, "Slot2").GetComponent<Button>();
                Assert.That(slot2.colors.normalColor, Is.EqualTo(slot2.colors.pressedColor));
                Assert.That(slot1.colors.normalColor, Is.Not.EqualTo(slot1.colors.pressedColor));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
