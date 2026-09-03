using System.IO;
using NUnit.Framework;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.EmergencyRescue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB85EmergencyRescueChipTests
    {
        [Test]
        public void PromptB85_1_RescuePopup_RendersAboveDroneDialogue()
        {
            var canvasObject = new GameObject("HudCanvas", typeof(RectTransform), typeof(Canvas));
            EmergencyRescuePanelView view = null;
            try
            {
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                view = EmergencyRescuePanelView.Create(canvasObject.transform, null);

                var popupCanvas = view.GetComponent<Canvas>();
                Assert.That(popupCanvas, Is.Not.Null);
                Assert.That(popupCanvas.overrideSorting, Is.True);
                Assert.That(
                    popupCanvas.sortingOrder,
                    Is.GreaterThan(DroneDialogueSocket.OverlaySortingOrder));
                Assert.That(view.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            }
            finally
            {
                if (view != null && view.gameObject != null)
                {
                    Object.DestroyImmediate(view.gameObject);
                }

                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void PromptB85_ChipFollowsPlayerHeadOnOverlayCanvas()
        {
            string view = Read("Scripts", "App", "UI", "EmergencyRescue", "EmergencyRescuePanelView.cs");
            string follow = Read("Scripts", "App", "UI", "EmergencyRescue", "EmergencyRescueChipFollow.cs");
            string controller = Read("Scripts", "App", "Integration", "EmergencyRescueRuntimeController.cs");

            Assert.That(view, Does.Contain("EmergencyRescueChipFollow"));
            Assert.That(view, Does.Contain("overrideSorting = true"));
            Assert.That(view, Does.Contain("UiLayerPriority.CriticalHazard"));
            Assert.That(view, Does.Not.Contain("energyAnchor"));
            Assert.That(follow, Does.Contain("WorldToScreenPoint"));
            Assert.That(follow, Does.Contain("playerCollider.bounds.max.y"));
            Assert.That(follow, Does.Contain("ScreenOffset = new(0f, 48f)"));
            Assert.That(follow, Does.Contain("Mathf.Clamp"));
            Assert.That(controller, Does.Contain("SetFollowTarget(player)"));
            Assert.That(controller, Does.Contain("머리 위 버튼을 누르거나 R 키를 누르세요"));
            Assert.That(controller, Does.Contain("keyboard.rKey.wasPressedThisFrame"));
            Assert.That(controller, Does.Not.Contain("전력 옆 버튼을 누르세요"));
        }

        [Test]
        public void PromptB85_MiningMouseClick_IgnoresUiPointer()
        {
            string mining = Read("Scripts", "Gameplay", "Mining", "PlayerMiningController.cs");
            Assert.That(mining, Does.Contain("IsPointerOverUi"));
            Assert.That(mining, Does.Contain("IsPointerOverGameObject"));
            Assert.That(mining, Does.Contain("RaycastAll"));
            Assert.That(mining, Does.Contain("enterKey.isPressed"));
        }

        [Test]
        public void PromptB85_CreatedChip_IsClickableHeadMarkerNotEnergyNeighbor()
        {
            var canvasObject = new GameObject("HudCanvas", typeof(RectTransform), typeof(Canvas));
            EmergencyRescuePanelView view = null;
            try
            {
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                view = EmergencyRescuePanelView.Create(canvasObject.transform, null);
                Assert.That(view, Is.Not.Null);

                Transform chip = canvasObject.transform.Find("EmergencyRescueChip");
                Assert.That(chip, Is.Not.Null);
                Assert.That(chip.parent, Is.EqualTo(canvasObject.transform));
                Assert.That(chip.IsChildOf(view.transform), Is.False);

                var image = chip.GetComponent<Image>();
                var button = chip.GetComponent<Button>();
                var nestedCanvas = chip.GetComponent<Canvas>();
                var raycaster = chip.GetComponent<GraphicRaycaster>();
                var follow = chip.GetComponent<EmergencyRescueChipFollow>();
                var label = chip.GetComponentInChildren<TMP_Text>(true);
                var rect = chip.GetComponent<RectTransform>();

                Assert.That(image, Is.Not.Null);
                Assert.That(image.raycastTarget, Is.True);
                Assert.That(button, Is.Not.Null);
                Assert.That(nestedCanvas, Is.Not.Null);
                Assert.That(nestedCanvas.overrideSorting, Is.True);
                Assert.That(nestedCanvas.sortingOrder, Is.EqualTo(UiLayerPriority.CriticalHazard));
                Assert.That(raycaster, Is.Not.Null);
                Assert.That(follow, Is.Not.Null);
                Assert.That(label, Is.Not.Null);
                Assert.That(label.text, Does.Contain("구출"));
                Assert.That(rect.pivot, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(view.IsChipVisible, Is.False);

                view.SetChipVisible(true);
                Assert.That(view.IsChipVisible, Is.True);
                Assert.That(chip.GetSiblingIndex(), Is.EqualTo(canvasObject.transform.childCount - 1));
            }
            finally
            {
                if (view != null && view.gameObject != null)
                {
                    Object.DestroyImmediate(view.gameObject);
                }

                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void PromptB85_ChipFollowTick_PlacesChipAbovePlayerHead()
        {
            GameObject createdCamera = null;
            Camera worldCamera = Camera.main;
            if (worldCamera == null)
            {
                createdCamera = new GameObject("PromptB85Camera");
                worldCamera = createdCamera.AddComponent<Camera>();
                worldCamera.orthographic = true;
                worldCamera.orthographicSize = 8f;
                createdCamera.tag = "MainCamera";
                createdCamera.transform.position = new Vector3(0f, 0f, -10f);
            }

            var canvasObject = new GameObject("HudCanvas", typeof(RectTransform), typeof(Canvas));
            var player = new GameObject("PromptB85Player");
            EmergencyRescuePanelView view = null;
            try
            {
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                player.transform.position = new Vector3(2f, -3f, 0f);
                var collider = player.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(0.7f, 0.7f);

                view = EmergencyRescuePanelView.Create(canvasObject.transform, null);
                view.SetFollowTarget(player.transform);
                view.SetChipVisible(true);

                Transform chip = canvasObject.transform.Find("EmergencyRescueChip");
                var follow = chip.GetComponent<EmergencyRescueChipFollow>();
                follow.Tick();

                Vector3 expected = worldCamera.WorldToScreenPoint(
                    new Vector3(player.transform.position.x, collider.bounds.max.y, 0f));
                expected += (Vector3)EmergencyRescueChipFollow.ScreenOffset;
                Vector3 actual = RectTransformUtility.WorldToScreenPoint(null, chip.position);
                Assert.That(actual.x, Is.EqualTo(expected.x).Within(2f));
                Assert.That(actual.y, Is.EqualTo(expected.y).Within(2f));
            }
            finally
            {
                if (view != null && view.gameObject != null)
                {
                    Object.DestroyImmediate(view.gameObject);
                }

                Object.DestroyImmediate(canvasObject);
                Object.DestroyImmediate(player);
                if (createdCamera != null)
                {
                    Object.DestroyImmediate(createdCamera);
                }
            }
        }

        private static string Read(params string[] parts)
        {
            var path = Path.Combine(Application.dataPath, "_Project");
            for (var index = 0; index < parts.Length; index++)
            {
                path = Path.Combine(path, parts[index]);
            }

            return File.ReadAllText(path);
        }
    }
}
