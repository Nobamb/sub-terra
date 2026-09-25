using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.HUD;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SubTerra.App.Tests.PlayMode
{
    /// <summary>
    /// prompt-B 110: 실제 Integration Scene에서 재구성한 시설 건설창의
    /// 선택·비용 갱신·상태 배너·B 단축키·해상도별 배치를 확인하고 캡처를 남긴다.
    /// </summary>
    public sealed class PromptB110BuildingMenuPlayModeTests
    {
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";
        private const string QaSizeLabel = "PromptB110 QA";

        private Keyboard keyboard;
#if UNITY_EDITOR
        private UnityEditor.EditorWindow gameView;
        private object sizeGroup;
        private PropertyInfo selectedSize;
        private int previousSize;
        private int customSizeCount;
#endif

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            GameBootstrapper.ResetInstanceForTests();
#if UNITY_EDITOR
            if (gameView != null)
            {
                selectedSize.SetValue(gameView, previousSize);
                RemoveAddedGameViewSizes();
            }
#endif
            yield return null;
        }

        [UnityTest]
        public IEnumerator BuildingMenu_SelectionCostsAvailabilityAndLayout()
        {
            GameBootstrapper.ResetInstanceForTests();
            var runtimeRoot = new GameObject("PromptB110_TestRuntime");
            var bootstrap = runtimeRoot.AddComponent<GameBootstrapper>();
            bootstrap.enabled = false;
#if UNITY_EDITOR
            var bootstrapSo = new UnityEditor.SerializedObject(bootstrap);
            bootstrapSo.FindProperty("gameDataCatalog").objectReferenceValue =
                UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();
#endif
            var state = GameState.CreateNew();
            state.BeginRun();
            bootstrap.TryReplaceState(state);
            var save = runtimeRoot.AddComponent<SaveRuntimeController>();
            yield return null;
            yield return null;
            // 슬롯을 시작/불러오지 않아 실제 사용자 저장 파일에 쓰지 않는다.
            Assert.That(save.ActiveSlot, Is.Zero);
            save.SetReady(true);
            SceneManager.LoadScene(SceneNames.Integration);
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(1f);

            SetGameViewSize(1920, 1080);
            yield return new WaitForSecondsRealtime(0.3f);
            DismissIntroGuidance();
            yield return null;

            var chrome = Object.FindAnyObjectByType<HudPanelChromeController>();
            Assert.That(chrome, Is.Not.Null);
            chrome.OpenBuildingMenu();
            yield return null;
            var view = Object.FindAnyObjectByType<BuildingMenuView>();
            var binder = view.GetComponent<BuildingMenuBinder>();
            Assert.That(view, Is.Not.Null);
            Assert.That(binder.IsBound, Is.True);
            Assert.That(view.HasStructuredReferences(), Is.True);

            var panel = view.transform.Find("PanelRoot");
            var detailName = panel.Find("DetailName").GetComponent<TMP_Text>();
            var availability = panel.Find("AvailabilityText").GetComponent<TMP_Text>();
            var costSection = panel.Find("CostSection").gameObject;
            Assert.That(detailName.text, Is.EqualTo("시설 미선택"));
            Assert.That(costSection.activeSelf, Is.False);

            var evidence = Path.GetFullPath("../work_process/MVP2/UI-fix-markdown-document/evidence/prompt-b110");
            Directory.CreateDirectory(evidence);
            yield return Capture(Path.Combine(evidence, "building-idle-1920x1080.png"));

            // 버팀목 선택: 초기 인벤토리는 비어 있어 자원 부족이 문구·기호와 함께 보여야 한다.
            var support = Entry(panel, DataIds.Buildings.SupportBasic);
            support.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(binder.Presenter.SelectedBuildingId, Is.EqualTo(DataIds.Buildings.SupportBasic));
            Assert.That(support.GetComponent<BuildingMenuEntryVisual>().IsSelected, Is.True);
            AssertOnlySelected(panel, DataIds.Buildings.SupportBasic);
            Assert.That(detailName.text, Is.Not.EqualTo("시설 미선택"));
            Assert.That(costSection.activeSelf, Is.True);
            Assert.That(availability.text, Does.Contain("자원 부족"));
            Assert.That(panel.Find("AvailabilityBanner/AvailabilityBadge/AvailabilityGlyph")
                .GetComponent<TMP_Text>().text, Is.EqualTo(BuildingMenuDisplayFormatter.BlockedGlyph));
            AssertNotTruncated(panel);
            yield return Capture(Path.Combine(evidence, "building-need-resources-1920x1080.png"));

            // 자원을 얻으면 비용 행·목록 상태·설치 배너가 즉시 바뀌어야 한다.
            var added = save.InventoryService.TryAddMineralExact(DataIds.Minerals.Copper, 6);
            Assert.That(added.DidChange, Is.True, "copper add");
            yield return null;
            var row0 = panel.Find("CostSection/CostRow_0/State").GetComponent<TMP_Text>();
            Assert.That(row0.text, Does.Contain("충분"));
            Assert.That(support.Find("EntryState").GetComponent<TMP_Text>().text,
                Is.EqualTo(BuildingMenuDisplayFormatter.ReadyGlyph));
            Assert.That(availability.text, Does.Not.Contain("자원 부족"));
            yield return Capture(Path.Combine(evidence, "building-affordable-1920x1080.png"));

            // 비용 2종 중 일부만 부족한 시설: 부족한 광물만 경고 표시.
            var core = Entry(panel, DataIds.Buildings.OutpostCoreBasic);
            core.GetComponent<Button>().onClick.Invoke();
            yield return null;
            AssertOnlySelected(panel, DataIds.Buildings.OutpostCoreBasic);
            Assert.That(panel.Find("CostSection/CostRow_0").gameObject.activeSelf, Is.True);
            Assert.That(panel.Find("CostSection/CostRow_1").gameObject.activeSelf, Is.True);
            Assert.That(panel.Find("CostSection/CostRow_2").gameObject.activeSelf, Is.False);
            Assert.That(panel.Find("CostSection/CostRow_1/State").GetComponent<TMP_Text>().text, Does.Contain("부족"));
            Assert.That(availability.text, Does.Contain("자원 부족"));
            AssertNotTruncated(panel);
            yield return Capture(Path.Combine(evidence, "building-partial-1920x1080.png"));

            foreach (var resolution in new[] { new Vector2Int(2560, 1440), new Vector2Int(1366, 768), new Vector2Int(1920, 1080) })
            {
                SetGameViewSize(resolution.x, resolution.y);
                yield return new WaitForSecondsRealtime(0.3f);
                Canvas.ForceUpdateCanvases();
                Assert.That(Screen.width, Is.EqualTo(resolution.x));
                AssertInsideScreenAndClearOfHud(view);
                AssertNotTruncated(panel);
                yield return Capture(Path.Combine(evidence, $"building-selected-{resolution.x}x{resolution.y}.png"));
            }

            // B 단축키: 닫으면 선택이 취소되고, 다시 열면 미선택 상태로 시작한다.
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return PressB();
            Assert.That(chrome.IsBuildingMenuOpen, Is.False);
            Assert.That(binder.Presenter.SelectedBuildingId, Is.Empty);
            yield return PressB();
            Assert.That(chrome.IsBuildingMenuOpen, Is.True);
            Assert.That(detailName.text, Is.EqualTo("시설 미선택"));
            AssertOnlySelected(panel, string.Empty);

            // X 버튼 닫기.
            view.CloseButton.onClick.Invoke();
            yield return null;
            Assert.That(chrome.IsBuildingMenuOpen, Is.False);
            Assert.That(save.ActiveSlot, Is.Zero);
        }

        private IEnumerator PressB()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.B));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private static IEnumerator Capture(string path)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(0.2f);
        }

        private static Transform Entry(Transform panel, string buildingId)
        {
            var entry = panel.Find("Select_" + buildingId);
            Assert.That(entry, Is.Not.Null, buildingId);
            return entry;
        }

        private static void AssertOnlySelected(Transform panel, string buildingId)
        {
            foreach (var visual in panel.GetComponentsInChildren<BuildingMenuEntryVisual>(true))
            {
                Assert.That(visual.IsSelected, Is.EqualTo(visual.BuildingId == buildingId), visual.name);
            }
        }

        private static void AssertNotTruncated(Transform panel)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(false))
            {
                text.ForceMeshUpdate();
                Assert.That(text.isTextTruncated, Is.False, text.name + " truncated: " + text.text);
            }
        }

        private static void AssertInsideScreenAndClearOfHud(BuildingMenuView view)
        {
            var rect = (RectTransform)view.transform;
            var menu = ScreenRect(rect);
            Assert.That(menu.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(menu.yMin, Is.GreaterThanOrEqualTo(0f), "건설창 하단이 화면 밖입니다.");
            Assert.That(menu.xMax, Is.LessThanOrEqualTo(Screen.width));
            Assert.That(menu.yMax, Is.LessThanOrEqualTo(Screen.height));

            var quest = GameObject.Find("QuestSummaryButton");
            if (quest != null && quest.activeInHierarchy)
            {
                Assert.That(menu.Overlaps(ScreenRect((RectTransform)quest.transform)), Is.False, "퀘스트 카드와 겹칩니다.");
            }

            var side = Object.FindAnyObjectByType<GameplaySideMenuController>();
            var open = side != null ? side.transform.Find("OpenMenuRoot") as RectTransform : null;
            if (open != null && open.gameObject.activeInHierarchy)
            {
                Assert.That(menu.Overlaps(ScreenRect(open)), Is.False, "우측 메뉴와 겹칩니다.");
            }
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

#if UNITY_EDITOR
        // RemoveCustomSize는 전체(내장+사용자) 인덱스를 받는다. 무한 반복을 막기 위해 횟수를 제한한다.
        private void RemoveAddedGameViewSizes()
        {
            var groupType = sizeGroup.GetType();
            int total = (int)groupType.GetMethod("GetTotalCount").Invoke(sizeGroup, null);
            int builtin = (int)groupType.GetMethod("GetBuiltinCount").Invoke(sizeGroup, null);
            for (int index = total - 1; index >= builtin; index--)
            {
                var size = groupType.GetMethod("GetGameViewSize").Invoke(sizeGroup, new object[] { index });
                var labelProperty = size.GetType().GetProperty("baseText");
                if (labelProperty == null)
                {
                    return;
                }

                var label = labelProperty.GetValue(size) as string;
                if (label != QaSizeLabel)
                {
                    continue;
                }

                try
                {
                    groupType.GetMethod("RemoveCustomSize").Invoke(sizeGroup, new object[] { index });
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning("[PromptB110] Game View 크기 정리 실패: " + exception.GetType().Name);
                    return;
                }
            }
        }
#endif

        private static void DismissIntroGuidance()
        {
            var objectiveView = Object.FindAnyObjectByType<SubTerra.App.UI.Tutorial.DemoObjectiveView>();
            if (objectiveView == null)
            {
                return;
            }

            foreach (var button in objectiveView.GetComponentsInChildren<Button>(false))
            {
                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null && label.text == "닫기")
                {
                    button.onClick.Invoke();
                    return;
                }
            }
        }

        private void SetGameViewSize(int width, int height)
        {
#if UNITY_EDITOR
            var editorAssembly = typeof(UnityEditor.Editor).Assembly;
            var viewType = editorAssembly.GetType("UnityEditor.GameView");
            if (gameView == null)
            {
                gameView = UnityEditor.EditorWindow.GetWindow(viewType);
                selectedSize = viewType.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                previousSize = (int)selectedSize.GetValue(gameView);
                var sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                var singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
                var sizes = singleton.GetProperty("instance").GetValue(null);
                var groupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");
                sizeGroup = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { System.Enum.Parse(groupType, "Standalone") });
                customSizeCount = (int)sizeGroup.GetType().GetMethod("GetCustomCount").Invoke(sizeGroup, null);
            }
            var sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            var modeType = editorAssembly.GetType("UnityEditor.GameViewSizeType");
            var size = System.Activator.CreateInstance(sizeType, new[] { System.Enum.Parse(modeType, "FixedResolution"), (object)width, height, QaSizeLabel });
            sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup, new[] { size });
            int count = (int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup, null);
            selectedSize.SetValue(gameView, count - 1);
            gameView.Repaint();
#endif
        }
    }
}
