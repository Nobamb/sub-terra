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
    /// prompt-B 31번: 하단 자원 범례 → 게임 가이드 대체,
    /// Digger-Bot 최하단 배치, 우측 가이드 버튼 + 탭/스크롤 가이드 창.
    /// </summary>
    public static class GameGuidePanelBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string DialoguePrefabPath =
            "Assets/_Project/Prefabs/UI/DroneDialoguePanel.prefab";
        private const string CompositePrefabPath =
            "Assets/_Project/Prefabs/UI/DroneAnalysisUI.prefab";
        private const string GameGuidePrefabPath =
            "Assets/_Project/Prefabs/UI/GameGuidePanel.prefab";

        // 기존 자원 범례 위치 = Digger-Bot 최하단.
        private const float DiggerBottomY = 24f;
        private const float DiggerWidth = 760f;
        private const float DiggerHeight = 220f;

        private const float GuideWidth = 920f;
        private const float GuideHeight = 620f;
        private const float GuideFontSize = GameGuidePanelView.GuideFontSize;

        [MenuItem("SubTerra/UI/Build Game Guide Panel (prompt-B 31)")]
        public static void BuildFromMenu()
        {
            var report = Build();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "game-guide-panel-build.txt"),
                report);
        }

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Game Guide Panel (prompt-B 31)");
            sb.AppendLine(UpdateDialoguePrefabBottom());
            sb.AppendLine(UpdateCompositePrefabBottom());
            sb.AppendLine(EnsureGameGuidePrefab());
            sb.AppendLine(UpdateIntegrationScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sb.ToString();
        }

        private static string UpdateDialoguePrefabBottom()
        {
            var root = PrefabUtility.LoadPrefabContents(DialoguePrefabPath);
            try
            {
                ApplyDiggerBottomLayout(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, DialoguePrefabPath);
                return "DroneDialoguePanel diggerBottomY=" + DiggerBottomY;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string UpdateCompositePrefabBottom()
        {
            var root = PrefabUtility.LoadPrefabContents(CompositePrefabPath);
            try
            {
                var dialogue = root.transform.Find("DroneDialoguePanel");
                if (dialogue != null)
                {
                    ApplyDiggerBottomLayout(dialogue);
                }

                PrefabUtility.SaveAsPrefabAsset(root, CompositePrefabPath);
                return "DroneAnalysisUI diggerBottom applied";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string EnsureGameGuidePrefab()
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
            return "GameGuidePanel.prefab refs="
                + (view != null && view.HasRequiredReferences());
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

            // 기존 하단 자원 범례 비활성(가이드로 대체).
            var legend = FindInSceneTransform(scene, "TerrainLegendPanel");
            if (legend != null)
            {
                legend.gameObject.SetActive(false);
                EditorUtility.SetDirty(legend.gameObject);
            }

            // Digger-Bot을 범례가 있던 최하단으로 이동.
            var digger = FindInSceneTransform(scene, "DroneDialoguePanel");
            if (digger != null)
            {
                ApplyDiggerBottomLayout(digger);
            }

            var guide = EnsureGameGuideInCanvas(canvas.transform);
            guide.gameObject.SetActive(false);

            // 우측 토글 버튼 배치: 가이드 / 시설 / 드론 (위에서 아래).
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

            var buildingMenu = FindInSceneTransform(scene, "BuildingMenu");
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

            // 시작 시 가이드는 닫힘, 시설/드론은 기존 정책 유지.
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
            chromeSo.FindProperty("gameGuideRoot").objectReferenceValue = guide.gameObject;
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

            return "IntegrationScene guideWired="
                + chrome.HasRequiredReferences()
                + " diggerY=" + DiggerBottomY
                + " legendHidden=" + (legend == null || !legend.gameObject.activeSelf);
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
                BuildGuideHierarchy(go);
            }

            return go;
        }

        private static void BuildGuideHierarchy(GameObject root)
        {
            var rootRect = root.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = root.AddComponent<RectTransform>();
            }

            // 화면 중앙 모달.
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(GuideWidth, GuideHeight);

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
                new Vector2(20f, -14f),
                new Vector2(700f, 36f),
                22f,
                "Sub-Terra 게임 가이드");
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.55f, 0.92f, 1f);

            var close = EnsureTopRightButton(
                panelRoot.transform,
                "CloseButton",
                new Vector2(-12f, -10f),
                new Vector2(88f, 32f),
                "닫기");

            var tabBar = EnsureChild(panelRoot.transform, "TabBar", typeof(RectTransform));
            var tabBarRect = tabBar.GetComponent<RectTransform>();
            tabBarRect.anchorMin = new Vector2(0f, 1f);
            tabBarRect.anchorMax = new Vector2(1f, 1f);
            tabBarRect.pivot = new Vector2(0.5f, 1f);
            tabBarRect.anchoredPosition = new Vector2(0f, -56f);
            tabBarRect.sizeDelta = new Vector2(-24f, 40f);

            var tabButtons = new Button[GameGuidePanelView.TabCount];
            var tabLabels = new TMP_Text[GameGuidePanelView.TabCount];
            var tabWidth = (GuideWidth - 48f) / GameGuidePanelView.TabCount;
            for (var i = 0; i < GameGuidePanelView.TabCount; i++)
            {
                var tabName = "TabButton_" + i;
                var tab = EnsureChild(
                    tabBar.transform,
                    tabName,
                    typeof(Image),
                    typeof(Button));
                var tabRect = tab.GetComponent<RectTransform>();
                tabRect.anchorMin = tabRect.anchorMax = new Vector2(0f, 0.5f);
                tabRect.pivot = new Vector2(0f, 0.5f);
                tabRect.anchoredPosition = new Vector2(12f + i * tabWidth, 0f);
                tabRect.sizeDelta = new Vector2(tabWidth - 8f, 36f);
                var tabImage = tab.GetComponent<Image>();
                tabImage.color = i == 0
                    ? new Color(0.22f, 0.42f, 0.55f, 1f)
                    : new Color(0.12f, 0.18f, 0.24f, 0.95f);
                tabImage.raycastTarget = true;

                var label = EnsureTmp(
                    tab.transform,
                    "Label",
                    Vector2.zero,
                    tabRect.sizeDelta,
                    15f,
                    GameGuidePanelView.GetTabTitle((GameGuidePanelView.GuideTab)i));
                StretchLabel(label);
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;

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
            scrollRectTf.offsetMin = new Vector2(12f, 12f);
            scrollRectTf.offsetMax = new Vector2(-12f, -108f);
            var scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = new Color(0.02f, 0.04f, 0.06f, 0.55f);
            scrollBg.raycastTarget = true;

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
            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportImage.raycastTarget = true;
            var mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = EnsureChild(viewport.transform, "Content", typeof(RectTransform));
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 400f);

            var body = EnsureTmp(
                content.transform,
                "BodyText",
                new Vector2(0f, -8f),
                new Vector2(GuideWidth - 80f, 400f),
                GuideFontSize,
                GameGuidePanelView.GetTabBody(GameGuidePanelView.GuideTab.Controls));
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.offsetMin = new Vector2(16f, -400f);
            bodyRect.offsetMax = new Vector2(-16f, -8f);
            body.fontSize = GuideFontSize;
            body.alignment = TextAlignmentOptions.TopLeft;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Overflow;
            body.color = new Color(0.92f, 0.94f, 0.96f);
            body.raycastTarget = false;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

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
            EditorUtility.SetDirty(title);
        }

        private static void ApplyDiggerBottomLayout(Transform diggerRoot)
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
            var image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.2f, 0.28f, 0.95f);
            image.raycastTarget = true;

            var labelTmp = EnsureTmp(
                go.transform,
                "Label",
                Vector2.zero,
                size,
                16f,
                label);
            StretchLabel(labelTmp);
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.raycastTarget = false;
            // 라벨 문구는 항상 최신으로 맞춤.
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
            var image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.18f, 0.18f, 0.95f);
            image.raycastTarget = true;

            var labelTmp = EnsureTmp(
                go.transform,
                "Label",
                Vector2.zero,
                size,
                16f,
                label);
            StretchLabel(labelTmp);
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.raycastTarget = false;
            labelTmp.text = label;

            EditorUtility.SetDirty(go);
            return go.GetComponent<Button>();
        }

        private static void StretchLabel(TMP_Text label)
        {
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
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
                ? components
                : new[] { typeof(RectTransform) };
            var hasRect = false;
            foreach (var t in types)
            {
                if (t == typeof(RectTransform))
                {
                    hasRect = true;
                    break;
                }
            }

            if (!hasRect)
            {
                var list = types.ToList();
                list.Insert(0, typeof(RectTransform));
                types = list.ToArray();
            }

            var go = new GameObject(name, types);
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
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = existing.gameObject;
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

            if (string.IsNullOrEmpty(tmp.text) || name == "BodyText" || name == "TitleText"
                || name == "Label")
            {
                tmp.text = defaultText;
            }

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
