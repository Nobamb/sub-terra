using NUnit.Framework;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
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

        [Test]
        public void SurfaceBaseSettings_StaysAboveLevelSummaryPanel()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab");
            Assert.That(prefab, Is.Not.Null);

            var instance = Object.Instantiate(prefab);
            try
            {
                var view = instance.GetComponent<SurfaceBaseView>();
                Assert.That(view, Is.Not.Null);

                var progression = instance.GetComponentInChildren<ProgressionPanelView>(true);
                Assert.That(progression, Is.Not.Null);
                Assert.That(progression.LevelsOnlySummary, Is.True);

                // 레벨 요약이 모달 sorting을 올리지 않는지 확인.
                progression.BringToFront();
                var levelCanvas = progression.GetComponent<Canvas>();
                Assert.That(
                    levelCanvas == null || !levelCanvas.overrideSorting
                    || levelCanvas.sortingOrder < UiLayerPriority.SettingsModal,
                    Is.True);

                view.SetSettingsVisible(true);
                var settings = FindChild(instance.transform, "SettingsPanel");
                Assert.That(settings, Is.Not.Null);
                Assert.That(settings.GetSiblingIndex(), Is.GreaterThan(
                    FindChild(instance.transform, "SurfaceBaseContent").GetSiblingIndex()));

                var settingsCanvas = settings.GetComponent<Canvas>();
                Assert.That(settingsCanvas, Is.Not.Null);
                Assert.That(settingsCanvas.overrideSorting, Is.True);
                Assert.That(settingsCanvas.sortingOrder, Is.EqualTo(UiLayerPriority.SettingsModal));

                // 레벨 요약은 SurfaceBaseContent 하위에 묶여 있어야 한다.
                var content = FindChild(instance.transform, "SurfaceBaseContent");
                Assert.That(progression.transform.IsChildOf(content), Is.True);
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
