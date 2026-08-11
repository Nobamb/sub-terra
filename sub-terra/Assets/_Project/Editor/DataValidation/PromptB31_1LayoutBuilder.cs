using System.IO;
using System.Linq;
using System.Text;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.HUD;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 31-1:
    /// - 게임 가이드 미표시 수정 + 70% 중앙 모달
    /// - 시설 건설 좌측(퀘스트 하단) 배치·좁은 버튼·전력/취소 제거
    /// - Surface Base 새로고침 → 설정/종료
    /// </summary>
    public static class PromptB31_1LayoutBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string BuildingMenuPrefabPath =
            "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        private const string GameGuidePrefabPath =
            "Assets/_Project/Prefabs/UI/GameGuidePanel.prefab";

        // 스테이터스·퀘스트와 동일 간격.
        private const float StatusTopY = -16f;
        private const float StatusHeight = 260f;
        private const float QuestGap = 12f;
        private const float QuestStartY = StatusTopY - StatusHeight - QuestGap;
        // 퀘스트 마지막 줄(NextAction) y=-94, h=32 → 하단 = QuestStartY-126
        private const float QuestBottomOffset = 126f;
        private const float BuildingTopY = QuestStartY - QuestBottomOffset - QuestGap;
        private const float QuestWidth = 440f;
        private const float BuildingWidth = 440f;
        // 좌측 선택 버튼 6개(마지막 y≈-456,h=34) + 하단 여백까지 수용.
        private const float BuildingHeight = 540f;
        private const float LeftButtonWidth = 132f;

        private const float DiggerBottomY = 24f;
        private const float DiggerWidth = 760f;
        private const float DiggerHeight = 220f;

        [MenuItem("SubTerra/UI/Build Prompt-B 31-1 Layout Fixes")]
        public static void BuildFromMenu()
        {
            var report = Build();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "prompt-b-31-1-layout.txt"),
                report);
        }

        [MenuItem("SubTerra/UI/Repair Integration Game Guide")]
        public static void RepairIntegrationGameGuideFromMenu()
        {
            var report = RepairIntegrationGameGuide();
            Debug.Log("[SubTerra] " + report);
        }

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Prompt-B 31-1 Layout Fixes");

            sb.AppendLine(UpdateBuildingMenuPrefab());
            sb.AppendLine(UpdateGameGuidePrefab());
            // MainMenu+SurfaceBase Prefab/Scene 재조립 (설정·종료 포함).
            sb.AppendLine(PhaseLMenuSceneBuilder.Build());
            sb.AppendLine(UpdateIntegrationScene());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sb.ToString();
        }

        /// <summary>
        /// Restores only the game-guide instance and its HUD toggle references in the
        /// integration scene. This intentionally avoids rebuilding unrelated panels.
        /// </summary>
        public static string RepairIntegrationGameGuide()
        {
            UpdateGameGuidePrefab();

            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                return "FAIL: scene open " + IntegrationScenePath;
            }

            var canvas = FindInScene<Canvas>(scene, "HUDCanvas");
            if (canvas == null)
            {
                return "FAIL: HUDCanvas missing";
            }

            var guide = EnsureGameGuideInCanvas(canvas.transform);
            guide.SetActive(false);

            var openGuide = EnsureSideToggleButton(
                canvas.transform,
                "OpenGameGuideButton",
                new Vector2(-16f, 140f),
                new Vector2(148f, 48f),
                "게임 가이드");
            openGuide.gameObject.SetActive(true);

            var chrome = canvas.GetComponent<HudPanelChromeController>();
            if (chrome == null)
            {
                chrome = canvas.gameObject.AddComponent<HudPanelChromeController>();
            }

            var guideView = guide.GetComponent<GameGuidePanelView>();
            var guideClose = guideView != null ? guideView.CloseButton : null;
            var chromeSo = new SerializedObject(chrome);
            chromeSo.FindProperty("gameGuideView").objectReferenceValue = guideView;
            chromeSo.FindProperty("gameGuideRoot").objectReferenceValue = guide;
            chromeSo.FindProperty("gameGuideCloseButton").objectReferenceValue = guideClose;
            chromeSo.FindProperty("gameGuideOpenButton").objectReferenceValue = openGuide;
            chromeSo.FindProperty("gameGuideOpen").boolValue = false;
            chromeSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(chrome);
            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (!string.IsNullOrEmpty(previous)
                && previous != IntegrationScenePath
                && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }

            return "Integration game guide restored";
        }

        private static string UpdateBuildingMenuPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(BuildingMenuPrefabPath);
            try
            {
                ApplyBuildingMenuLeftLayout(root);
                PrefabUtility.SaveAsPrefabAsset(root, BuildingMenuPrefabPath);
                var view = root.GetComponent<BuildingMenuView>();
                return "BuildingMenu leftLayout close="
                    + (view != null && view.CloseButton != null)
                    + " refs=" + (view != null && view.HasRequiredReferences());
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string UpdateGameGuidePrefab()
        {
            GameObject root;
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(GameGuidePrefabPath);
            if (existing == null)
            {
                root = new GameObject("GameGuidePanel", typeof(RectTransform));
                BuildGuideHierarchy(root);
                PrefabUtility.SaveAsPrefabAsset(root, GameGuidePrefabPath);
                Object.DestroyImmediate(root);
            }
            else
            {
                root = PrefabUtility.LoadPrefabContents(GameGuidePrefabPath);
                try
                {
                    BuildGuideHierarchy(root);
                    PrefabUtility.SaveAsPrefabAsset(root, GameGuidePrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameGuidePrefabPath);
            var view = prefab != null ? prefab.GetComponent<GameGuidePanelView>() : null;
            var rect = prefab != null ? prefab.GetComponent<RectTransform>() : null;
            var seventy =
                rect != null
                && Mathf.Abs(rect.anchorMin.x - 0.15f) < 0.001f
                && Mathf.Abs(rect.anchorMax.x - 0.85f) < 0.001f;
            return "GameGuidePanel 70%=" + seventy
                + " refs=" + (view != null && view.HasRequiredReferences());
        }

        private static string UpdateIntegrationScene()
        {
            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                return "FAIL: scene open " + IntegrationScenePath;
            }

            var canvas = FindInScene<Canvas>(scene, "HUDCanvas");
            if (canvas == null)
            {
                return "FAIL: HUDCanvas missing";
            }

            var legend = FindInSceneTransform(scene, "TerrainLegendPanel");
            if (legend != null)
            {
                legend.gameObject.SetActive(false);
                EditorUtility.SetDirty(legend.gameObject);
            }

            var digger = FindInSceneTransform(scene, "DroneDialoguePanel");
            if (digger != null)
            {
                ApplyDiggerBottom(digger);
            }

            var buildingMenu = FindInSceneTransform(scene, "BuildingMenu");
            if (buildingMenu != null)
            {
                ApplyBuildingMenuLeftLayout(buildingMenu.gameObject);
            }

            var guide = EnsureGameGuideInCanvas(canvas.transform);
            // 시작 시 숨김: SetVisible 경로와 동일하게 root 비활성.
            guide.SetActive(false);

            var openGuide = EnsureSideToggleButton(
                canvas.transform,
                "OpenGameGuideButton",
                new Vector2(-16f, 140f),
                new Vector2(148f, 48f),
                "게임 가이드");
            openGuide.gameObject.SetActive(true);

            var openBuilding = EnsureSideToggleButton(
                canvas.transform,
                "OpenBuildingMenuButton",
                new Vector2(-16f, 80f),
                new Vector2(148f, 48f),
                "시설 건설");
            var openDigger = EnsureSideToggleButton(
                canvas.transform,
                "OpenDiggerBotButton",
                new Vector2(-16f, 20f),
                new Vector2(148f, 48f),
                "드론");

            var chrome = canvas.GetComponent<HudPanelChromeController>();
            if (chrome == null)
            {
                chrome = canvas.gameObject.AddComponent<HudPanelChromeController>();
            }

            var buildingView = buildingMenu != null
                ? buildingMenu.GetComponent<BuildingMenuView>()
                : null;
            var buildingBinder = buildingMenu != null
                ? buildingMenu.GetComponent<BuildingMenuBinder>()
                : null;
            var buildingClose = buildingView != null ? buildingView.CloseButton : null;
            var diggerView = digger != null
                ? digger.GetComponent<DroneDialoguePanelView>()
                : null;
            var diggerClose = digger != null
                ? digger.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(b => b.name == "CloseButton")
                : null;
            var guideView = guide.GetComponent<GameGuidePanelView>();
            var guideClose = guideView != null ? guideView.CloseButton : null;

            var buildingOpen = buildingMenu == null || buildingMenu.gameObject.activeSelf;
            var diggerOpen = digger == null || digger.gameObject.activeSelf;
            openBuilding.gameObject.SetActive(!buildingOpen);
            openDigger.gameObject.SetActive(!diggerOpen);

            var chromeSo = new SerializedObject(chrome);
            chromeSo.FindProperty("buildingMenuView").objectReferenceValue = buildingView;
            chromeSo.FindProperty("buildingMenuBinder").objectReferenceValue = buildingBinder;
            chromeSo.FindProperty("buildingMenuRoot").objectReferenceValue =
                buildingMenu != null ? buildingMenu.gameObject : null;
            chromeSo.FindProperty("buildingCloseButton").objectReferenceValue = buildingClose;
            chromeSo.FindProperty("buildingOpenButton").objectReferenceValue = openBuilding;
            chromeSo.FindProperty("diggerBotView").objectReferenceValue = diggerView;
            chromeSo.FindProperty("diggerBotRoot").objectReferenceValue =
                digger != null ? digger.gameObject : null;
            chromeSo.FindProperty("diggerCloseButton").objectReferenceValue = diggerClose;
            chromeSo.FindProperty("diggerOpenButton").objectReferenceValue = openDigger;
            chromeSo.FindProperty("gameGuideView").objectReferenceValue = guideView;
            chromeSo.FindProperty("gameGuideRoot").objectReferenceValue = guide;
            chromeSo.FindProperty("gameGuideCloseButton").objectReferenceValue = guideClose;
            chromeSo.FindProperty("gameGuideOpenButton").objectReferenceValue = openGuide;
            chromeSo.FindProperty("buildingMenuOpen").boolValue = buildingOpen;
            chromeSo.FindProperty("diggerBotOpen").boolValue = diggerOpen;
            chromeSo.FindProperty("gameGuideOpen").boolValue = false;
            chromeSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(chrome);
            EditorUtility.SetDirty(canvas.gameObject);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (!string.IsNullOrEmpty(previous)
                && previous != IntegrationScenePath
                && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }

            // Apply 직후 직렬화 필드 반영 확인용 재조회.
            var chromeCheck = canvas.GetComponent<HudPanelChromeController>();
            var soCheck = new SerializedObject(chromeCheck);
            var wired = soCheck.FindProperty("buildingMenuRoot").objectReferenceValue != null
                && soCheck.FindProperty("buildingOpenButton").objectReferenceValue != null
                && soCheck.FindProperty("diggerBotRoot").objectReferenceValue != null
                && soCheck.FindProperty("diggerOpenButton").objectReferenceValue != null
                && soCheck.FindProperty("gameGuideRoot").objectReferenceValue != null
                && soCheck.FindProperty("gameGuideOpenButton").objectReferenceValue != null;

            return "IntegrationScene chrome="
                + wired
                + " buildingTopY=" + BuildingTopY
                + " guideHidden=true";
        }

        internal static void ApplyBuildingMenuLeftLayout(GameObject buildingRoot)
        {
            var rect = buildingRoot.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            // 좌측 상단 기준: 퀘스트 하단 + 스테이터스-퀘스트와 동일 간격.
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(16f, BuildingTopY);
            rect.sizeDelta = new Vector2(BuildingWidth, BuildingHeight);
            EditorUtility.SetDirty(rect);

            var panelRoot = buildingRoot.transform.Find("PanelRoot");
            if (panelRoot == null)
            {
                panelRoot = buildingRoot.transform;
            }

            // 좌측 선택 버튼 너비 축소.
            foreach (var button in panelRoot.GetComponentsInChildren<Button>(true))
            {
                if (!button.name.StartsWith("Select_"))
                {
                    continue;
                }

                var br = button.GetComponent<RectTransform>();
                if (br == null)
                {
                    continue;
                }

                br.anchorMin = br.anchorMax = new Vector2(0f, 1f);
                br.pivot = new Vector2(0f, 1f);
                br.sizeDelta = new Vector2(LeftButtonWidth, br.sizeDelta.y > 1f ? br.sizeDelta.y : 32f);
                EditorUtility.SetDirty(br);
            }

            // 목록 텍스트 폭도 버튼 열에 맞춤.
            var listText = panelRoot.Find("BuildingListText") as RectTransform;
            if (listText != null)
            {
                listText.sizeDelta = new Vector2(LeftButtonWidth, listText.sizeDelta.y);
                EditorUtility.SetDirty(listText);
            }

            // 우측 상세 영역 재배치 (좁아진 패널 기준).
            var selection = panelRoot.Find("SelectionText") as RectTransform;
            if (selection != null)
            {
                selection.anchoredPosition = new Vector2(150f, -64f);
                selection.sizeDelta = new Vector2(BuildingWidth - 170f, 200f);
                EditorUtility.SetDirty(selection);
            }

            var availability = panelRoot.Find("AvailabilityText") as RectTransform;
            if (availability != null)
            {
                availability.anchoredPosition = new Vector2(150f, -280f);
                availability.sizeDelta = new Vector2(BuildingWidth - 170f, 60f);
                EditorUtility.SetDirty(availability);
            }

            var status = panelRoot.Find("StatusText") as RectTransform;
            if (status != null)
            {
                status.anchoredPosition = new Vector2(150f, -350f);
                status.sizeDelta = new Vector2(BuildingWidth - 170f, 48f);
                EditorUtility.SetDirty(status);
            }

            // 건설 취소 버튼 제거.
            var cancel = panelRoot.Find("CancelButton");
            if (cancel != null)
            {
                Object.DestroyImmediate(cancel.gameObject);
            }

            var close = EnsureTopRightButton(
                panelRoot,
                "CloseButton",
                new Vector2(-8f, -8f),
                new Vector2(72f, 28f),
                "닫기");

            var view = buildingRoot.GetComponent<BuildingMenuView>();
            if (view != null)
            {
                var so = new SerializedObject(view);
                so.FindProperty("closeButton").objectReferenceValue = close;
                var cancelProp = so.FindProperty("cancelButton");
                if (cancelProp != null)
                {
                    cancelProp.objectReferenceValue = null;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);
            }
        }

        private static void BuildGuideHierarchy(GameObject root)
        {
            var rootRect = root.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = root.AddComponent<RectTransform>();
            }

            // 전체 화면 70% 정중앙 (여백 15%씩).
            rootRect.anchorMin = new Vector2(0.15f, 0.15f);
            rootRect.anchorMax = new Vector2(0.85f, 0.85f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.sizeDelta = Vector2.zero;

            var panelRoot = EnsureChild(root.transform, "PanelRoot", typeof(Image));
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = panelRoot.GetComponent<Image>();
            panelImage.color = new Color(0.04f, 0.07f, 0.11f, 0.97f);
            panelImage.raycastTarget = true;

            var title = EnsureTmp(
                panelRoot.transform,
                "TitleText",
                new Vector2(24f, -16f),
                new Vector2(900f, 40f),
                24f,
                "Sub-Terra 게임 가이드");
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.55f, 0.92f, 1f);

            // 우측 상단 닫기.
            var close = EnsureTopRightButton(
                panelRoot.transform,
                "CloseButton",
                new Vector2(-16f, -12f),
                new Vector2(100f, 36f),
                "닫기");

            var tabBar = EnsureChild(panelRoot.transform, "TabBar", typeof(RectTransform));
            var tabBarRect = tabBar.GetComponent<RectTransform>();
            tabBarRect.anchorMin = new Vector2(0f, 1f);
            tabBarRect.anchorMax = new Vector2(1f, 1f);
            tabBarRect.pivot = new Vector2(0.5f, 1f);
            tabBarRect.anchoredPosition = new Vector2(0f, -64f);
            tabBarRect.sizeDelta = new Vector2(-32f, 44f);

            var tabButtons = new Button[GameGuidePanelView.TabCount];
            var tabLabels = new TMP_Text[GameGuidePanelView.TabCount];
            // 1920 기준 70% 폭으로 탭 폭 추정.
            var guideRefWidth = 1920f * 0.7f;
            var tabWidth = (guideRefWidth - 48f) / GameGuidePanelView.TabCount;
            for (var i = 0; i < GameGuidePanelView.TabCount; i++)
            {
                var tab = EnsureChild(
                    tabBar.transform,
                    "TabButton_" + i,
                    typeof(Image),
                    typeof(Button));
                var tabRect = tab.GetComponent<RectTransform>();
                tabRect.anchorMin = tabRect.anchorMax = new Vector2(0f, 0.5f);
                tabRect.pivot = new Vector2(0f, 0.5f);
                tabRect.anchoredPosition = new Vector2(12f + i * tabWidth, 0f);
                tabRect.sizeDelta = new Vector2(tabWidth - 10f, 38f);
                tab.GetComponent<Image>().color = i == 0
                    ? new Color(0.22f, 0.42f, 0.55f, 1f)
                    : new Color(0.12f, 0.18f, 0.24f, 0.95f);

                var label = EnsureTmp(
                    tab.transform,
                    "Label",
                    Vector2.zero,
                    tabRect.sizeDelta,
                    16f,
                    GameGuidePanelView.GetTabTitle((GameGuidePanelView.GuideTab)i));
                StretchLabel(label);
                label.alignment = TextAlignmentOptions.Center;
                tabButtons[i] = tab.GetComponent<Button>();
                tabLabels[i] = label;
            }

            var scrollGo = EnsureChild(
                panelRoot.transform,
                "ScrollView",
                typeof(Image),
                typeof(ScrollRect));
            var scrollRectTf = scrollGo.GetComponent<RectTransform>();
            scrollRectTf.anchorMin = new Vector2(0f, 0f);
            scrollRectTf.anchorMax = new Vector2(1f, 1f);
            scrollRectTf.offsetMin = new Vector2(16f, 16f);
            scrollRectTf.offsetMax = new Vector2(-16f, -120f);
            scrollGo.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.06f, 0.55f);

            var viewport = EnsureChild(
                scrollGo.transform,
                "Viewport",
                typeof(Image),
                typeof(Mask));
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(4f, 4f);
            viewportRect.offsetMax = new Vector2(-4f, -4f);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = EnsureChild(viewport.transform, "Content", typeof(RectTransform));
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 500f);

            var body = EnsureTmp(
                content.transform,
                "BodyText",
                new Vector2(0f, -8f),
                new Vector2(guideRefWidth - 100f, 500f),
                GameGuidePanelView.GuideFontSize,
                GameGuidePanelView.GetTabBody(GameGuidePanelView.GuideTab.Controls));
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.offsetMin = new Vector2(20f, -500f);
            bodyRect.offsetMax = new Vector2(-20f, -8f);
            body.fontSize = GameGuidePanelView.GuideFontSize;
            body.alignment = TextAlignmentOptions.TopLeft;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Overflow;
            body.color = new Color(0.92f, 0.94f, 0.96f);

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;

            var view = root.GetComponent<GameGuidePanelView>();
            if (view == null)
            {
                view = root.AddComponent<GameGuidePanelView>();
            }

            var so = new SerializedObject(view);
            so.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.FindProperty("bodyText").objectReferenceValue = body;
            so.FindProperty("scrollRect").objectReferenceValue = scroll;
            so.FindProperty("contentRoot").objectReferenceValue = contentRect;
            so.FindProperty("activeTab").enumValueIndex = 0;
            var tabButtonsProp = so.FindProperty("tabButtons");
            tabButtonsProp.arraySize = GameGuidePanelView.TabCount;
            for (var i = 0; i < GameGuidePanelView.TabCount; i++)
            {
                tabButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = tabButtons[i];
            }

            var tabLabelsProp = so.FindProperty("tabLabels");
            tabLabelsProp.arraySize = GameGuidePanelView.TabCount;
            for (var i = 0; i < GameGuidePanelView.TabCount; i++)
            {
                tabLabelsProp.GetArrayElementAtIndex(i).objectReferenceValue = tabLabels[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(root);
        }

        private static GameObject EnsureGameGuideInCanvas(Transform canvas)
        {
            var existing = canvas.Find("GameGuidePanel");
            GameObject go;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameGuidePrefabPath);
            if (existing == null)
            {
                if (prefab != null)
                {
                    go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
                    go.name = "GameGuidePanel";
                }
                else
                {
                    go = new GameObject("GameGuidePanel", typeof(RectTransform));
                    go.transform.SetParent(canvas, false);
                    BuildGuideHierarchy(go);
                }
            }
            else
            {
                go = existing.gameObject;
                // Prefab 인스턴스면 unpack 없이 계층 갱신.
                BuildGuideHierarchy(go);
            }

            return go;
        }

        private static void ApplyDiggerBottom(Transform diggerRoot)
        {
            var rect = diggerRoot as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, DiggerBottomY);
            rect.sizeDelta = new Vector2(DiggerWidth, DiggerHeight);
            EditorUtility.SetDirty(rect);
        }

        private static Button EnsureSideToggleButton(
            Transform canvas,
            string name,
            Vector2 anchoredPos,
            Vector2 size,
            string label)
        {
            var existing = canvas.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(canvas, false);
            }
            else
            {
                go = existing.gameObject;
                if (go.GetComponent<Image>() == null)
                {
                    go.AddComponent<Image>();
                }

                if (go.GetComponent<Button>() == null)
                {
                    go.AddComponent<Button>();
                }
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.12f, 0.2f, 0.28f, 0.95f);

            var labelTmp = EnsureTmp(go.transform, "Label", Vector2.zero, size, 16f, label);
            StretchLabel(labelTmp);
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.text = label;
            EditorUtility.SetDirty(go);
            return go.GetComponent<Button>();
        }

        private static Button EnsureTopRightButton(
            Transform parent,
            string name,
            Vector2 anchoredPos,
            Vector2 size,
            string label)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = existing.gameObject;
                if (go.GetComponent<Image>() == null)
                {
                    go.AddComponent<Image>();
                }

                if (go.GetComponent<Button>() == null)
                {
                    go.AddComponent<Button>();
                }
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.22f, 0.18f, 0.18f, 0.95f);

            var labelTmp = EnsureTmp(go.transform, "Label", Vector2.zero, size, 16f, label);
            StretchLabel(labelTmp);
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.text = label;
            EditorUtility.SetDirty(go);
            return go.GetComponent<Button>();
        }

        private static void StretchLabel(TMP_Text label)
        {
            var r = label.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            r.pivot = new Vector2(0.5f, 0.5f);
        }

        private static GameObject EnsureChild(
            Transform parent,
            string name,
            params System.Type[] components)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                foreach (var type in components)
                {
                    if (existing.GetComponent(type) == null)
                    {
                        existing.gameObject.AddComponent(type);
                    }
                }

                return existing.gameObject;
            }

            var types = components != null && components.Length > 0
                ? components.ToList()
                : new System.Collections.Generic.List<System.Type>();
            if (!types.Contains(typeof(RectTransform)))
            {
                types.Insert(0, typeof(RectTransform));
            }

            var go = new GameObject(name, types.ToArray());
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI EnsureTmp(
            Transform parent,
            string name,
            Vector2 anchoredPos,
            Vector2 size,
            float fontSize,
            string defaultText)
        {
            var existing = parent.Find(name);
            GameObject go = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform));
            if (existing == null)
            {
                go.transform.SetParent(parent, false);
            }

            var rect = go.GetComponent<RectTransform>();
            if (name != "Label" && name != "BodyText")
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = anchoredPos;
                rect.sizeDelta = size;
            }

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = go.AddComponent<TextMeshProUGUI>();
            }

            var font = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (font != null)
            {
                tmp.font = font;
            }

            tmp.text = defaultText;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.raycastTarget = false;
            EditorUtility.SetDirty(tmp);
            return tmp;
        }

        private static Transform FindInSceneTransform(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == objectName)
                    {
                        return t;
                    }
                }
            }

            return null;
        }

        private static T FindInScene<T>(Scene scene, string objectName)
            where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<T>(true))
                {
                    if (component.name == objectName)
                    {
                        return component;
                    }
                }
            }

            return null;
        }
    }
}
