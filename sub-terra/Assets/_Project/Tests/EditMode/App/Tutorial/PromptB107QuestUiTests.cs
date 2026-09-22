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
        public void Crossfade_FadesAcrossPointThreeSecondsAndBack()
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

            Assert.That(QuestSpriteCrossfade.DurationSeconds, Is.EqualTo(0.3f));
            fade.SetPointed(true);
            fade.Tick(0.15f);
            Assert.That(hover.color.a, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(normal.color.a, Is.EqualTo(1f).Within(0.001f));
            fade.Tick(0.15f);
            Assert.That(hover.color.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(normal.color.a, Is.EqualTo(1f).Within(0.001f));
            fade.SetPointed(false);
            fade.Tick(0.3f);
            Assert.That(hover.color.a, Is.EqualTo(0f).Within(0.001f));
            Assert.That(normal.color.a, Is.EqualTo(1f).Within(0.001f));

            button.interactable = false;
            fade.SetPointed(true);
            fade.Tick(0.3f);
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
                Assert.That(summaryRect.anchoredPosition.y, Is.LessThan(-264f));
                Assert.That(bottom, Is.GreaterThan(-426f));
                Assert.That(summaryRect.sizeDelta.x, Is.EqualTo(PromptB107QuestUiBuilder.SummaryCardWidth));
                Assert.That(summaryRect.sizeDelta.y, Is.EqualTo(PromptB107QuestUiBuilder.SummaryCardHeight));
                Assert.That(summaryRect.sizeDelta.x, Is.EqualTo(460f * 1.25f).Within(0.1f));

                var summaryFade = summary.GetComponent<QuestSpriteCrossfade>();
                Assert.That(summaryFade, Is.Not.Null);
                Assert.That(Image(summary.transform, "NormalImage").sprite.name, Is.EqualTo("quest-active-off"));
                Assert.That(Image(summary.transform, "HoverImage").sprite.name, Is.EqualTo("quest-active-on"));
                Assert.That(Image(summary.transform, "HoverImage").color.a, Is.EqualTo(0f));
                Assert.That(Image(summary.transform, "NormalImage").raycastTarget, Is.True);
                Assert.That(Image(summary.transform, "HoverImage").raycastTarget, Is.False);
                Assert.That(summary.GetComponent<CanvasRenderer>().cullTransparentMesh, Is.False);
                Assert.That(summary.transform.Find("ObjectiveTitle"), Is.Not.Null);
                var mission = summary.transform.Find("QuestMissionLabel").GetComponent<RectTransform>();
                var progress = summary.transform.Find("ProgressCount").GetComponent<RectTransform>();
                var missionText = mission.GetComponent<TMP_Text>();
                var progressText = progress.GetComponent<TMP_Text>();
                Assert.That(mission.anchoredPosition.x, Is.GreaterThanOrEqualTo(48f));
                Assert.That(progress.anchoredPosition.x, Is.LessThanOrEqualTo(-48f));
                Assert.That(mission.anchoredPosition.y, Is.EqualTo(PromptB107QuestUiBuilder.SummaryHeaderY).Within(0.5f));
                Assert.That(progress.anchoredPosition.y, Is.EqualTo(PromptB107QuestUiBuilder.SummaryHeaderY).Within(0.5f));
                Assert.That(missionText.fontSize, Is.EqualTo(21f).Within(0.1f));
                Assert.That(progressText.fontSize, Is.EqualTo(21f).Within(0.1f));
                Assert.That(summary.transform.Find("QuestMissionMarks").gameObject.activeSelf, Is.False);
                var statusIcon = summary.transform.Find("QuestStatusIcon").GetComponent<RectTransform>();
                var titleRect = summary.transform.Find("ObjectiveTitle").GetComponent<RectTransform>();
                var bodyText = summary.transform.Find("ObjectiveBody").GetComponent<TMP_Text>();
                var titleCenter = titleRect.anchoredPosition.y - titleRect.sizeDelta.y * 0.5f;
                var bodyCenter = bodyText.rectTransform.anchoredPosition.y - bodyText.fontSize * 0.55f;
                var iconCenter = statusIcon.anchoredPosition.y - statusIcon.sizeDelta.y * 0.5f;
                Assert.That(titleRect.anchoredPosition.y, Is.LessThan(-64f));
                Assert.That(bodyText.rectTransform.anchoredPosition.y, Is.LessThan(-100f));
                Assert.That(iconCenter, Is.EqualTo((titleCenter + bodyCenter) * 0.5f).Within(1f));
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
                Assert.That(Image(close.transform, "NormalImage").raycastTarget, Is.True);
                Assert.That(Image(prev.transform, "NormalImage").raycastTarget, Is.True);
                Assert.That(Image(next.transform, "NormalImage").raycastTarget, Is.True);
                Assert.That(details.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(prev.GetComponent<Button>().onClick.GetPersistentMethodName(0),
                    Is.EqualTo(nameof(DemoObjectiveView.OnDetailsPrevClicked)));
                Assert.That(next.GetComponent<Button>().onClick.GetPersistentMethodName(0),
                    Is.EqualTo(nameof(DemoObjectiveView.OnDetailsNextClicked)));
                Assert.That(close.GetComponent<Button>().onClick.GetPersistentMethodName(0),
                    Is.EqualTo(nameof(DemoObjectiveView.OnDetailsDismissClicked)));
                var thumbnail = Find(scene, "QuestThumbnail").GetComponent<RectTransform>();
                Assert.That(thumbnail.GetComponent<QuestThumbnailView>().EntryCount, Is.EqualTo(18));
                Assert.That(thumbnail.sizeDelta.y, Is.EqualTo(PromptB107QuestUiBuilder.ThumbnailHeight));
                Assert.That(Find(scene, "QuestThumbnailDots").activeSelf, Is.False);
                Assert.That(Find(scene, "RewardSlotCopper"), Is.Not.Null);
                Assert.That(Find(scene, "RewardSlotIron"), Is.Not.Null);
                Assert.That(Find(scene, "RewardSlotLithium"), Is.Not.Null);
                Assert.That(Find(scene, "RewardSlotGold"), Is.Not.Null);
                Assert.That(Find(scene, "QuestStatusPlate"), Is.Not.Null);
                Assert.That(Find(scene, "QuestStatusDivider"), Is.Not.Null);
                Assert.That(Find(scene, "QuestMissionDivider"), Is.Not.Null);
                Assert.That(Find(scene, "QuestRewardDivider"), Is.Not.Null);
                Assert.That(Find(scene, "QuestThumbnailDots"), Is.Not.Null);
                Assert.That(Find(scene, "QuestBrandText"), Is.Not.Null);
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
                    Assert.That(FindChild(clone.transform, "ObjectiveTitle").GetComponent<TMP_Text>().text,
                        Is.EqualTo(current.Title));
                    Assert.That(FindChild(clone.transform, "ObjectiveBody").GetComponent<TMP_Text>().text,
                        Is.EqualTo(current.Description));
                    Assert.That(FindChild(clone.transform, "QuestStatusIcon").GetComponent<Image>().sprite.name,
                        Is.EqualTo("quest-status-ring"));

                    // 진행 중 퀘스트 상세 테스트: 3번째 퀘스트 탐색 시 3 / 18 표시 확인
                    cloneView.ApplyQuestDetailsVisual(current.Id, current.Reward, false, true, 3, 18, false);
                    var widthForThree = AssertRewardsFill(clone.transform, 3);
                    Assert.That(FindChild(clone.transform, "QuestDetailsProgress").GetComponent<TMP_Text>().text,
                        Is.EqualTo("3 / 18"));
                    Assert.That(FindChild(clone.transform, "QuestDetailsStatus").GetComponent<TMP_Text>().text,
                        Is.EqualTo("진행 중"));
                    Assert.That(FindChild(clone.transform, "QuestClearBadge").gameObject.activeSelf, Is.False);

                    var escape = DemoObjectiveCatalog.GetRequired(DemoObjectiveIds.EmergencyEscapeReturn);
                    cloneView.SetDetailsText(escape.Title, escape.Description, string.Empty);
                    cloneView.ApplyQuestDetailsVisual(escape.Id, escape.Reward, true, false, 18, 18, true);
                    var widthForTwo = AssertRewardsFill(clone.transform, 2);
                    Assert.That(widthForTwo, Is.GreaterThan(widthForThree));
                    Assert.That(FindChild(clone.transform, "QuestDetailsTitle").GetComponent<TMP_Text>().text,
                        Is.EqualTo(escape.Title));
                    Assert.That(FindChild(clone.transform, "QuestDetailsBody").GetComponent<TMP_Text>().text,
                        Is.EqualTo(escape.Description));
                    Assert.That(FindChild(clone.transform, "RewardSlotLithium").gameObject.activeSelf, Is.True);
                    Assert.That(FindChild(clone.transform, "RewardSlotGold").gameObject.activeSelf, Is.True);
                    Assert.That(FindChild(clone.transform, "RewardSlotCopper").gameObject.activeSelf, Is.False);
                    Assert.That(FindChild(clone.transform, "RewardSlotLithium").Find("Amount").GetComponent<TMP_Text>().text,
                        Is.EqualTo("5"));
                    Assert.That(FindChild(clone.transform, "RewardSlotGold").Find("Amount").GetComponent<TMP_Text>().text,
                        Is.EqualTo("300"));
                    Assert.That(FindChild(clone.transform, "QuestDetailsStatus").GetComponent<TMP_Text>().text,
                        Is.EqualTo("모든 목표 완료"));
                    Assert.That(FindChild(clone.transform, "QuestDetailsProgress").GetComponent<TMP_Text>().text,
                        Is.EqualTo("18 / 18"));
                    Assert.That(FindChild(clone.transform, "QuestClearBadge").gameObject.activeSelf, Is.True);
                    Assert.That(FindChild(clone.transform, "QuestThumbnail").Find("Primary").GetComponent<Image>().sprite,
                        Is.Not.Null);
                    Assert.That(FindChild(clone.transform, "QuestThumbnail").Find("Tertiary").gameObject.activeSelf, Is.False);
                    var thumbnailView = FindChild(clone.transform, "QuestThumbnail").GetComponent<QuestThumbnailView>();
                    var image = FindChild(clone.transform, "QuestThumbnail").Find("Primary").GetComponent<UnityEngine.UI.Image>();
                    foreach (var id in DemoObjectiveIds.Ordered)
                    {
                        thumbnailView.Show(id);
                        Assert.That(AssetDatabase.GetAssetPath(image.sprite), Is.EqualTo(PromptB1072QuestThumbnailBuilder.PathFor(id)));
                        Assert.That(image.sprite.rect.width, Is.EqualTo(1448f));
                        Assert.That(image.sprite.rect.height, Is.EqualTo(472f));
                        Assert.That(image.rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
                        Assert.That(image.rectTransform.anchorMax, Is.EqualTo(Vector2.one));
                        Assert.That(image.preserveAspect, Is.True);
                        Assert.That(image.raycastTarget, Is.False);
                    }
                    thumbnailView.Show("unknown.quest");
                    Assert.That(image.gameObject.activeSelf, Is.False);
                    Assert.That(FindChild(clone.transform, "QuestThumbnail").Find("Placeholder").gameObject.activeSelf, Is.True);
                    thumbnailView.Show(escape.Id);

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
            summary.Tick(hover ? QuestSpriteCrossfade.DurationSeconds : 0f);
            FindChild(placed.transform, "QuestDetailsPanel").gameObject.SetActive(details);
            foreach (var tmp in placed.GetComponentsInChildren<TMP_Text>(true))
            {
                tmp.ForceMeshUpdate();
            }
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
                    "prompt-b107-2");
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

        private static float AssertRewardsFill(Transform root, int visibleCount)
        {
            var row = FindChild(root, "QuestRewardRow").GetComponent<RectTransform>();
            var names = new[] { "RewardSlotCopper", "RewardSlotIron", "RewardSlotLithium", "RewardSlotGold" };
            RectTransform first = null;
            RectTransform last = null;
            var count = 0;
            var width = 0f;
            for (var i = 0; i < names.Length; i++)
            {
                var slot = FindChild(root, names[i]);
                if (!slot.gameObject.activeSelf)
                {
                    continue;
                }

                var rect = slot.GetComponent<RectTransform>();
                if (first == null)
                {
                    first = rect;
                    width = rect.sizeDelta.x;
                }

                Assert.That(rect.sizeDelta.x, Is.EqualTo(width).Within(0.5f));
                Assert.That(rect.anchoredPosition.x + rect.sizeDelta.x, Is.LessThanOrEqualTo(row.sizeDelta.x + 0.5f));
                last = rect;
                count++;
            }

            Assert.That(count, Is.EqualTo(visibleCount));
            Assert.That(first.anchoredPosition.x, Is.EqualTo(0f).Within(0.5f));
            var right = last.anchoredPosition.x + last.sizeDelta.x;
            Assert.That(right, Is.EqualTo(row.sizeDelta.x).Within(1f));
            return width;
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
