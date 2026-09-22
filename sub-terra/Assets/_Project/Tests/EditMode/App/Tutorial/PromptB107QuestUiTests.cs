using System.IO;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Tutorial
{
    public sealed class PromptB107QuestUiTests
    {
        [Test]
        public void Crossfade_FadesAcrossHalfASecondAndBack()
        {
            var root = new GameObject("fade", typeof(RectTransform), typeof(Button));
            var normal = ChildImage(root.transform, "NormalImage");
            var hover = ChildImage(root.transform, "HoverImage");
            var fade = root.AddComponent<QuestSpriteCrossfade>();
            var button = root.GetComponent<Button>();
            var serialized = new SerializedObject(fade);
            serialized.FindProperty("normalImage").objectReferenceValue = normal;
            serialized.FindProperty("hoverImage").objectReferenceValue = hover;
            serialized.FindProperty("selectable").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(QuestSpriteCrossfade.DurationSeconds, Is.EqualTo(0.5f));
            fade.SetPointed(true);
            fade.Tick(0.25f);
            Assert.That(hover.color.a, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(normal.color.a, Is.EqualTo(0.5f).Within(0.001f));
            fade.Tick(0.25f);
            Assert.That(hover.color.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(normal.color.a, Is.EqualTo(0f).Within(0.001f));
            fade.SetPointed(false);
            fade.Tick(0.5f);
            Assert.That(hover.color.a, Is.EqualTo(0f).Within(0.001f));

            button.interactable = false;
            fade.SetPointed(true);
            fade.Tick(0.5f);
            Assert.That(hover.color.a, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Scene_UsesQuestFramesWithoutCoveringTheFacilityPanel()
        {
            PromptB107QuestUiBuilder.Build();
            var scene = SceneManager.GetSceneByPath(PromptB107QuestUiBuilder.IntegrationScenePath);
            var closeAfter = !scene.IsValid() || !scene.isLoaded;
            if (closeAfter)
            {
                scene = EditorSceneManager.OpenScene(
                    PromptB107QuestUiBuilder.IntegrationScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                var root = Find(scene, "DemoObjectiveRoot");
                var summary = Find(scene, "QuestSummaryButton");
                var details = Find(scene, "QuestDetailsPanel");
                var view = root.GetComponent<DemoObjectiveView>();
                var summaryRect = summary.GetComponent<RectTransform>();
                var bottom = summaryRect.anchoredPosition.y - summaryRect.sizeDelta.y;

                Assert.That(view.HasDetailsReferences(), Is.True);
                Assert.That(view.HasRewardLogReferences(), Is.True);
                Assert.That(details.activeSelf, Is.False);
                Assert.That(summaryRect.anchoredPosition.y, Is.LessThan(-260f));
                Assert.That(bottom, Is.GreaterThan(-426f));
                Assert.That(summaryRect.sizeDelta.x, Is.EqualTo(442f));
                Assert.That(summaryRect.sizeDelta.y, Is.EqualTo(130f));

                var summaryFade = summary.GetComponent<QuestSpriteCrossfade>();
                Assert.That(summaryFade, Is.Not.Null);
                Assert.That(Image(summary.transform, "NormalImage").sprite.name, Is.EqualTo("quest-active-off"));
                Assert.That(Image(summary.transform, "HoverImage").sprite.name, Is.EqualTo("quest-active-on"));
                Assert.That(Image(summary.transform, "HoverImage").color.a, Is.EqualTo(0f));
                Assert.That(summary.GetComponent<Button>().onClick.GetPersistentMethodName(0),
                    Is.EqualTo(nameof(DemoObjectiveView.OnObjectiveDetailsClicked)));

                var detailsRect = details.GetComponent<RectTransform>();
                Assert.That(detailsRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(detailsRect.sizeDelta.x, Is.GreaterThanOrEqualTo(640f));
                Assert.That(detailsRect.sizeDelta.y, Is.GreaterThanOrEqualTo(320f));
                Assert.That(details.GetComponent<Image>().sprite.name, Is.EqualTo("quest-particular-frame"));
                Assert.That(details.GetComponent<Canvas>().sortingOrder, Is.EqualTo(UiLayerPriority.QuestPopup));

                var prev = Find(scene, "QuestDetailsPrevButton");
                var next = Find(scene, "QuestDetailsNextButton");
                var close = Find(scene, "QuestDetailsCloseButton");
                Assert.That(Image(prev.transform, "NormalImage").sprite, Is.SameAs(Image(next.transform, "NormalImage").sprite));
                Assert.That(Image(prev.transform, "HoverImage").sprite.name, Is.EqualTo("quest-particular-button-active-on"));
                Assert.That(next.transform.localScale.x, Is.EqualTo(-1f));
                Assert.That(prev.transform.localScale.x, Is.EqualTo(1f));
                Assert.That(Image(close.transform, "NormalImage").sprite.name, Is.EqualTo("quest-close-button-active-off"));
                Assert.That(Image(close.transform, "HoverImage").sprite.name, Is.EqualTo("quest-close-button-active-on"));
                Assert.That(prev.GetComponent<Button>().onClick.GetPersistentMethodName(0),
                    Is.EqualTo(nameof(DemoObjectiveView.OnDetailsPrevClicked)));
                Assert.That(next.GetComponent<Button>().onClick.GetPersistentMethodName(0),
                    Is.EqualTo(nameof(DemoObjectiveView.OnDetailsNextClicked)));
                Assert.That(close.GetComponent<Button>().onClick.GetPersistentMethodName(0),
                    Is.EqualTo(nameof(DemoObjectiveView.OnDetailsDismissClicked)));
                Assert.That(Find(scene, "QuestThumbnail").GetComponent<QuestThumbnailView>().EntryCount, Is.EqualTo(18));
                Assert.That(Find(scene, "RewardSlotCopper"), Is.Not.Null);
                Assert.That(Find(scene, "RewardSlotIron"), Is.Not.Null);
                Assert.That(Find(scene, "RewardSlotLithium"), Is.Not.Null);
                Assert.That(Find(scene, "RewardSlotGold"), Is.Not.Null);
                var facility = Find(scene, "BuildingPanel").GetComponent<RectTransform>();
                Assert.That(facility.anchoredPosition.y, Is.EqualTo(-426f));

                var clone = Object.Instantiate(root);
                try
                {
                    var cloneView = clone.GetComponent<DemoObjectiveView>();
                    var current = DemoObjectiveCatalog.GetRequired(DemoObjectiveIds.UpgradeDrillSpeed);
                    cloneView.SetObjective(new DemoObjectiveReadModel(
                        current.Id,
                        current.Title,
                        current.Description,
                        current.NextActionHint,
                        6,
                        18,
                        false,
                        false,
                        false,
                        string.Empty,
                        string.Empty));
                    Assert.That(FindChild(clone.transform, "ProgressCount").GetComponent<TMP_Text>().text,
                        Is.EqualTo("진행도 6 / 18"));
                    Assert.That(FindChild(clone.transform, "QuestStatusIcon").GetComponent<Image>().sprite.name,
                        Is.EqualTo("quest-status-ring"));

                    var escape = DemoObjectiveCatalog.GetRequired(DemoObjectiveIds.EmergencyEscapeReturn);
                    cloneView.SetDetailsText(escape.Title, escape.Description, string.Empty);
                    cloneView.ApplyQuestDetailsVisual(escape.Id, escape.Reward, true, false, 18, 18);
                    Assert.That(FindChild(clone.transform, "RewardSlotLithium").gameObject.activeSelf, Is.True);
                    Assert.That(FindChild(clone.transform, "RewardSlotGold").gameObject.activeSelf, Is.True);
                    Assert.That(FindChild(clone.transform, "RewardSlotCopper").gameObject.activeSelf, Is.False);
                    Assert.That(FindChild(clone.transform, "RewardSlotLithium").Find("Amount").GetComponent<TMP_Text>().text,
                        Is.EqualTo("5"));
                    Assert.That(FindChild(clone.transform, "RewardSlotGold").Find("Amount").GetComponent<TMP_Text>().text,
                        Is.EqualTo("300"));
                    Assert.That(FindChild(clone.transform, "QuestDetailsStatus").GetComponent<TMP_Text>().text,
                        Is.EqualTo("모든 목표 완료"));
                    Assert.That(FindChild(clone.transform, "QuestClearBadge").gameObject.activeSelf, Is.True);
                    Assert.That(FindChild(clone.transform, "QuestThumbnail").Find("Primary").GetComponent<Image>().sprite,
                        Is.Not.Null);
                    Assert.That(FindChild(clone.transform, "QuestThumbnail").Find("Tertiary").gameObject.activeSelf, Is.True);

                    Capture(clone, "quest-basic.png", false, false);
                    Capture(clone, "quest-hover.png", true, false);
                    cloneView.SetDetailsNavInteractable(true, true);
                    Capture(clone, "quest-details.png", false, true);
                    cloneView.SetDetailsNavInteractable(false, true);
                    Assert.That(FindChild(clone.transform, "QuestDetailsPrevButton").gameObject.activeSelf, Is.False);
                    Assert.That(FindChild(clone.transform, "QuestDetailsNextButton").gameObject.activeSelf, Is.True);
                }
                finally
                {
                    Object.DestroyImmediate(clone);
                }
            }
            finally
            {
                if (closeAfter && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void Capture(GameObject root, string fileName, bool hover, bool details)
        {
            var host = new GameObject("QuestCapture", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = host.GetComponent<Canvas>();
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var cameraObject = new GameObject("QuestCaptureCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.05f, 0.07f);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            var texture = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = texture;
            var placed = Object.Instantiate(root, host.transform, false);
            var placedRect = placed.GetComponent<RectTransform>();
            placedRect.anchorMin = Vector2.zero;
            placedRect.anchorMax = Vector2.one;
            placedRect.offsetMin = Vector2.zero;
            placedRect.offsetMax = Vector2.zero;
            foreach (var popupName in new[] { "GuidancePanel", "DemoCompletePanel", "QuestRewardCapacityPanel", "QuestRewardDumpPanel", "QuestClearRewardPanel" })
            {
                var popup = FindChild(placed.transform, popupName);
                if (popup != null)
                {
                    popup.gameObject.SetActive(false);
                }
            }

            var summary = FindChild(placed.transform, "QuestSummaryButton").GetComponent<QuestSpriteCrossfade>();
            summary.SetPointed(hover);
            summary.Tick(hover ? 0.5f : 0f);
            FindChild(placed.transform, "QuestDetailsPanel").gameObject.SetActive(details);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = texture;
                var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0);
                image.Apply();
                var directory = Path.Combine(
                    Application.dataPath,
                    "..",
                    "..",
                    "work_process",
                    "MVP2",
                    "UI-fix-markdown-document",
                    "evidence",
                    "prompt-b107");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, fileName), image.EncodeToPNG());
                Object.DestroyImmediate(image);
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(host);
            }
        }

        private static Image ChildImage(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);
            return child.GetComponent<Image>();
        }

        private static Image Image(Transform parent, string name)
        {
            return parent.Find(name).GetComponent<Image>();
        }

        private static GameObject Find(Scene scene, string name)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (var j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == name)
                    {
                        return transforms[j].gameObject;
                    }
                }
            }

            return null;
        }

        private static Transform FindChild(Transform root, string name)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                {
                    return transforms[i];
                }
            }

            return null;
        }
    }
}
