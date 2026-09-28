using NUnit.Framework;
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
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
            Assert.That(UiLayerPriority.QuestPopup, Is.GreaterThan(SubTerra.App.UI.Drone.DroneDialogueSocket.OverlaySortingOrder));
            Assert.That(UiLayerPriority.QuestPopup, Is.LessThan(UiLayerPriority.EmergencyRescueModal));
        }

        [Test]
        public void DraggablePopups_StayAboveFixedUiAndReuseClosedTopOrder()
        {
            var root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var first = CreatePopup(root.transform, "First");
                var second = CreatePopup(root.transform, "Second");
                var third = CreatePopup(root.transform, "Third");

                first.SetActive(true);
                second.SetActive(true);
                third.SetActive(true);

                var firstOrder = first.GetComponent<Canvas>().sortingOrder;
                Assert.That(firstOrder, Is.GreaterThan(32_000));
                Assert.That(second.GetComponent<Canvas>().sortingOrder, Is.EqualTo(firstOrder + 1));
                Assert.That(third.GetComponent<Canvas>().sortingOrder, Is.EqualTo(firstOrder + 2));
                Assert.That(first.GetComponent<GraphicRaycaster>(), Is.Not.Null);

                third.SetActive(false);
                var fourth = CreatePopup(root.transform, "Fourth");
                fourth.SetActive(true);

                Assert.That(fourth.GetComponent<Canvas>().sortingOrder, Is.EqualTo(firstOrder + 2));
                Assert.That(second.GetComponent<Canvas>().sortingOrder, Is.EqualTo(firstOrder + 1));

                second.SetActive(false);
                Assert.That(fourth.GetComponent<Canvas>().sortingOrder, Is.EqualTo(firstOrder + 1));
                second.SetActive(true);
                Assert.That(second.GetComponent<Canvas>().sortingOrder, Is.EqualTo(firstOrder + 2));

                var runtimePopup = new GameObject("RuntimePopup", typeof(RectTransform), typeof(Canvas));
                runtimePopup.transform.SetParent(root.transform, false);
                var runtimeCanvas = runtimePopup.GetComponent<Canvas>();
                PopupWindowSorting.BringToFront(runtimeCanvas);
                Assert.That(runtimeCanvas.sortingOrder, Is.EqualTo(firstOrder + 3));

                PopupWindowSorting.Remove(runtimeCanvas);
                runtimePopup.SetActive(false);
                Assert.That(second.GetComponent<Canvas>().sortingOrder, Is.EqualTo(firstOrder + 2));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DragWindow_DoesNotSwallowClicksThroughHiddenParentOrButtonPress()
        {
            var root = new GameObject(
                "Root",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup));
            try
            {
                var group = root.GetComponent<CanvasGroup>();
                group.alpha = 0f;
                group.blocksRaycasts = false;

                var windowObject = new GameObject("Window", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                windowObject.SetActive(false);
                windowObject.transform.SetParent(root.transform, false);
                var drag = windowObject.AddComponent<PopupWindowDrag>();
                windowObject.SetActive(true);

                var gate = windowObject.GetComponent<PopupWindowRaycastGate>();
                Assert.That(gate, Is.Not.Null);
                Assert.That(gate.IsRaycastLocationValid(Vector2.zero, null), Is.False);

                group.blocksRaycasts = true;
                Assert.That(gate.IsRaycastLocationValid(Vector2.zero, null), Is.True);
                Assert.That(drag is IDragHandler, Is.False);
                Assert.That(drag is IPointerDownHandler, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreatePopup(Transform parent, string name)
        {
            var popup = new GameObject(name, typeof(RectTransform));
            popup.SetActive(false);
            popup.transform.SetParent(parent, false);
            popup.AddComponent<PopupWindowDrag>();
            return popup;
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
