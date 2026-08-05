using System.IO;
using System.Linq;
using System.Text;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 30번: HUD 창 겹침 해소, 퀘스트 위치, Digger-Bot 통합, 닫기/재오픈 버튼.
    /// Scene/Prefab YAML 직접 편집 대신 Editor API로 적용한다.
    /// </summary>
    public static class HudPanelChromeLayoutBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string HudCanvasPrefabPath =
            "Assets/_Project/Prefabs/UI/HUDCanvas.prefab";
        private const string DialoguePrefabPath =
            "Assets/_Project/Prefabs/UI/DroneDialoguePanel.prefab";
        private const string CompositePrefabPath =
            "Assets/_Project/Prefabs/UI/DroneAnalysisUI.prefab";
        private const string BuildingMenuPrefabPath =
            "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";

        // 스테이터스(BasicHUD) 아래 여백을 두고 퀘스트를 붙인다.
        private const float StatusTopY = -16f;
        private const float StatusHeight = 260f;
        private const float QuestGap = 12f;
        private const float QuestStartY = StatusTopY - StatusHeight - QuestGap;

        // prompt-B 31: 자원 범례 자리에 Digger-Bot을 최하단 배치한다.
        private const float DiggerBottomY = 24f;
        private const float DiggerWidth = 760f;
        private const float DiggerHeight = 220f;

        [MenuItem("SubTerra/UI/Build HUD Panel Chrome Layout (prompt-B 30)")]
        public static void BuildFromMenu()
        {
            var report = Build();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "hud-panel-chrome-layout.txt"),
                report);
        }

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("HUD Panel Chrome Layout (prompt-B 30)");

            sb.AppendLine(UpdateDialoguePrefab());
            sb.AppendLine(UpdateBuildingMenuPrefab());
            sb.AppendLine(UpdateCompositePrefab());
            sb.AppendLine(UpdateIntegrationScene());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sb.ToString();
        }

        private static string UpdateDialoguePrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(DialoguePrefabPath);
            try
            {
                ApplyDiggerBotLayout(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, DialoguePrefabPath);
                var view = root.GetComponent<DroneDialoguePanelView>();
                return "DroneDialoguePanel integratedReason="
                    + (view != null && view.HasIntegratedReasonTexts());
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string UpdateBuildingMenuPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(BuildingMenuPrefabPath);
            try
            {
                EnsureBuildingCloseButton(root);
                PrefabUtility.SaveAsPrefabAsset(root, BuildingMenuPrefabPath);
                var view = root.GetComponent<BuildingMenuView>();
                return "BuildingMenu closeButton="
                    + (view != null && view.CloseButton != null);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string UpdateCompositePrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CompositePrefabPath);
            try
            {
                var dialogue = root.transform.Find("DroneDialoguePanel");
                if (dialogue != null)
                {
                    ApplyDiggerBotLayout(dialogue);
                }

                var reason = root.transform.Find("DroneReasonPanel");
                if (reason != null)
                {
                    // 우측 단독 추천 창 제거(비활성). 근거는 Digger-Bot 통합 창에 표시.
                    reason.gameObject.SetActive(false);
                }

                var binder = root.GetComponent<DroneUiBinder>();
                if (binder != null && dialogue != null)
                {
                    var dialogueView = dialogue.GetComponent<DroneDialoguePanelView>();
                    var so = new SerializedObject(binder);
                    so.FindProperty("dialogueView").objectReferenceValue = dialogueView;
                    // 통합 창이 근거를 담당하므로 별도 reason 참조는 비운다.
                    so.FindProperty("reasonView").objectReferenceValue = null;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, CompositePrefabPath);
                return "DroneAnalysisUI reasonHidden=true diggerIntegrated=true";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
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

            // 스테이터스는 현 위치 유지 (BasicHUD top-left 16,-16 / 420x260).
            var basicHud = FindInSceneTransform(scene, "BasicHUD");
            if (basicHud != null)
            {
                var rt = basicHud as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(16f, StatusTopY);
                    rt.sizeDelta = new Vector2(420f, StatusHeight);
                    EditorUtility.SetDirty(rt);
                }
            }

            LayoutQuestBelowStatus(scene);

            // prompt-B 31: 하단 자원 범례는 가이드로 대체 → 비활성.
            var legend = FindInSceneTransform(scene, "TerrainLegendPanel");
            if (legend != null)
            {
                legend.gameObject.SetActive(false);
                EditorUtility.SetDirty(legend.gameObject);
            }

            var buildingMenu = FindInSceneTransform(scene, "BuildingMenu");
            if (buildingMenu != null)
            {
                // prompt-B 31-1: 좌측 퀘스트 하단 배치.
                PromptB31_1LayoutBuilder.ApplyBuildingMenuLeftLayout(buildingMenu.gameObject);
            }

            var digger = FindInSceneTransform(scene, "DroneDialoguePanel");
            if (digger != null)
            {
                ApplyDiggerBotLayout(digger);
            }

            var reasonPanel = FindInSceneTransform(scene, "DroneReasonPanel");
            if (reasonPanel != null)
            {
                reasonPanel.gameObject.SetActive(false);
                EditorUtility.SetDirty(reasonPanel.gameObject);
            }

            var droneBinder = FindInScene<DroneUiBinder>(scene, "DroneAnalysisUI");
            if (droneBinder == null)
            {
                droneBinder = Object.FindObjectsByType<DroneUiBinder>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .FirstOrDefault();
            }

            if (droneBinder != null && digger != null)
            {
                var so = new SerializedObject(droneBinder);
                so.FindProperty("dialogueView").objectReferenceValue =
                    digger.GetComponent<DroneDialoguePanelView>();
                so.FindProperty("reasonView").objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(droneBinder);
            }

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

            // 시작 시 패널이 열려 있으면 열기 버튼은 숨김.
            openBuilding.gameObject.SetActive(false);
            openDigger.gameObject.SetActive(false);

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
            var diggerView = digger != null
                ? digger.GetComponent<DroneDialoguePanelView>()
                : null;
            var diggerClose = digger != null
                ? digger.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(b => b.name == "CloseButton")
                : null;
            var buildingClose = buildingView != null ? buildingView.CloseButton : null;

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
            chromeSo.FindProperty("buildingMenuOpen").boolValue = true;
            chromeSo.FindProperty("diggerBotOpen").boolValue = true;
            chromeSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(chrome);
            EditorUtility.SetDirty(canvas.gameObject);

            // HUDCanvas prefab에도 동일 레이아웃을 반영할 수 있으면 갱신.
            TryUpdateHudCanvasPrefabIfPresent();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (!string.IsNullOrEmpty(previous)
                && previous != IntegrationScenePath
                && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }

            return "IntegrationScene layout applied chromeRefs="
                + chrome.HasRequiredReferences();
        }

        private static void TryUpdateHudCanvasPrefabIfPresent()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HudCanvasPrefabPath) == null)
            {
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(HudCanvasPrefabPath);
            try
            {
                var basic = root.transform.Find("BasicHUD") as RectTransform;
                if (basic != null)
                {
                    basic.anchorMin = basic.anchorMax = new Vector2(0f, 1f);
                    basic.pivot = new Vector2(0f, 1f);
                    basic.anchoredPosition = new Vector2(16f, StatusTopY);
                    basic.sizeDelta = new Vector2(420f, StatusHeight);
                }

                var building = root.transform.Find("BuildingMenu");
                if (building != null)
                {
                    EnsureBuildingCloseButton(building.gameObject);
                }

                var digger = FindChildRecursive(root.transform, "DroneDialoguePanel");
                if (digger != null)
                {
                    ApplyDiggerBotLayout(digger);
                }

                var reason = FindChildRecursive(root.transform, "DroneReasonPanel");
                if (reason != null)
                {
                    reason.gameObject.SetActive(false);
                }

                PrefabUtility.SaveAsPrefabAsset(root, HudCanvasPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void LayoutQuestBelowStatus(Scene scene)
        {
            var objectiveRoot = FindInSceneTransform(scene, "DemoObjectiveRoot");
            if (objectiveRoot == null)
            {
                return;
            }

            // 안내/완료 모달은 중앙 유지, 목표 HUD 텍스트만 스테이터스 바로 아래로.
            PlaceObjectiveText(
                objectiveRoot,
                "ObjectiveTitle",
                new Vector2(16f, QuestStartY),
                new Vector2(360f, 34f),
                20);
            PlaceObjectiveText(
                objectiveRoot,
                "ProgressCount",
                new Vector2(380f, QuestStartY),
                new Vector2(80f, 34f),
                16);
            PlaceObjectiveText(
                objectiveRoot,
                "ObjectiveBody",
                new Vector2(16f, QuestStartY - 38f),
                new Vector2(440f, 52f),
                15);
            PlaceObjectiveText(
                objectiveRoot,
                "NextAction",
                new Vector2(16f, QuestStartY - 94f),
                new Vector2(440f, 32f),
                14);

            EditorUtility.SetDirty(objectiveRoot.gameObject);
        }

        private static void PlaceObjectiveText(
            Transform root,
            string name,
            Vector2 anchoredPos,
            Vector2 size,
            float fontSize)
        {
            var t = root.Find(name) as RectTransform;
            if (t == null)
            {
                foreach (var child in root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (child.name == name)
                    {
                        t = child;
                        break;
                    }
                }
            }

            if (t == null)
            {
                return;
            }

            t.anchorMin = t.anchorMax = new Vector2(0f, 1f);
            t.pivot = new Vector2(0f, 1f);
            t.anchoredPosition = anchoredPos;
            t.sizeDelta = size;
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.fontSize = fontSize;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.raycastTarget = false;
                EditorUtility.SetDirty(tmp);
            }

            EditorUtility.SetDirty(t);
        }

        private static void ApplyDiggerBotLayout(Transform diggerRoot)
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

            var panelRoot = diggerRoot.Find("PanelRoot");
            if (panelRoot == null)
            {
                panelRoot = diggerRoot;
            }

            var speaker = EnsureTmp(
                panelRoot,
                "SpeakerText",
                new Vector2(20f, -12f),
                new Vector2(620f, 28f),
                20,
                "Digger-Bot");
            speaker.fontStyle = FontStyles.Bold;
            speaker.color = new Color(0.35f, 0.9f, 1f);

            var dialogue = EnsureTmp(
                panelRoot,
                "DialogueText",
                new Vector2(20f, -44f),
                new Vector2(720f, 48f),
                18,
                "분석 대기 중");
            dialogue.textWrappingMode = TextWrappingModes.Normal;

            var action = EnsureTmp(
                panelRoot,
                "ActionText",
                new Vector2(20f, -96f),
                new Vector2(720f, 28f),
                17,
                "추천: 분석 대기 중");
            action.color = new Color(0.45f, 1f, 0.7f);

            var reason = EnsureTmp(
                panelRoot,
                "ReasonText",
                new Vector2(20f, -128f),
                new Vector2(720f, 72f),
                15,
                "상태 정보 없음");
            reason.textWrappingMode = TextWrappingModes.Normal;

            var close = EnsureTopRightButton(
                panelRoot,
                "CloseButton",
                new Vector2(-12f, -10f),
                new Vector2(88f, 32f),
                "닫기");

            var view = diggerRoot.GetComponent<DroneDialoguePanelView>();
            if (view == null)
            {
                view = diggerRoot.gameObject.AddComponent<DroneDialoguePanelView>();
            }

            var so = new SerializedObject(view);
            so.FindProperty("panelRoot").objectReferenceValue =
                panelRoot != null ? panelRoot.gameObject : diggerRoot.gameObject;
            so.FindProperty("dialogueText").objectReferenceValue = dialogue;
            so.FindProperty("actionText").objectReferenceValue = action;
            so.FindProperty("reasonText").objectReferenceValue = reason;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(close);
        }

        private static void EnsureBuildingCloseButton(GameObject buildingMenuRoot)
        {
            var panelRoot = buildingMenuRoot.transform.Find("PanelRoot");
            if (panelRoot == null)
            {
                panelRoot = buildingMenuRoot.transform;
            }

            var close = EnsureTopRightButton(
                panelRoot,
                "CloseButton",
                new Vector2(-12f, -10f),
                new Vector2(88f, 32f),
                "닫기");

            var view = buildingMenuRoot.GetComponent<BuildingMenuView>();
            if (view == null)
            {
                return;
            }

            var so = new SerializedObject(view);
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
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
                18,
                label);
            var labelRect = labelTmp.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.raycastTarget = false;

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
                16,
                label);
            var labelRect = labelTmp.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.raycastTarget = false;

            EditorUtility.SetDirty(go);
            return go.GetComponent<Button>();
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
            if (rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one
                && name == "Label")
            {
                // stretch label for buttons — keep stretch
            }
            else if (name != "Label")
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

            if (string.IsNullOrEmpty(tmp.text))
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

        private static Transform FindChildRecursive(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
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
