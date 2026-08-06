using System.IO;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Inventory;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 31-2:
    /// - 시설 건설 좌측 버튼 위 목록 텍스트 제거
    /// - 우측 상세 영역 +20px 폭, 좌측 버튼과 10px 간격
    /// - Integration Scene에 인벤토리 패널 배치 (I 키 토글용)
    /// </summary>
    public static class PromptB31_2LayoutBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string BuildingMenuPrefabPath =
            "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        private const string InventoryPanelPrefabPath =
            "Assets/_Project/Prefabs/UI/InventoryPanel.prefab";

        // 31-1 기준 440 + 20.
        private const float BuildingWidth = 460f;
        private const float BuildingHeight = 540f;
        private const float LeftButtonWidth = 132f;
        private const float LeftColumnX = 20f;
        private const float LeftRightGap = 10f;
        private const float RightMargin = 16f;
        private static readonly float RightColumnX =
            LeftColumnX + LeftButtonWidth + LeftRightGap;
        private static readonly float RightColumnWidth =
            BuildingWidth - RightColumnX - RightMargin;

        // 31-1 퀘스트 하단 위치 재사용.
        private const float StatusTopY = -16f;
        private const float StatusHeight = 260f;
        private const float QuestGap = 12f;
        private const float QuestStartY = StatusTopY - StatusHeight - QuestGap;
        private const float QuestBottomOffset = 126f;
        private const float BuildingTopY = QuestStartY - QuestBottomOffset - QuestGap;

        [MenuItem("SubTerra/UI/Build Prompt-B 31-2 Layout Fixes")]
        public static void BuildFromMenu()
        {
            var report = Build();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "prompt-b-31-2-layout.txt"),
                report);
        }

        public static string Build()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Prompt-B 31-2 Layout Fixes");
            sb.AppendLine(UpdateBuildingMenuPrefab());
            sb.AppendLine(UpdateInventoryPanelPrefab());
            sb.AppendLine(UpdateIntegrationScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sb.ToString();
        }

        private static string UpdateBuildingMenuPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(BuildingMenuPrefabPath);
            try
            {
                ApplyBuildingMenuLayout(root);
                PrefabUtility.SaveAsPrefabAsset(root, BuildingMenuPrefabPath);
                var view = root.GetComponent<BuildingMenuView>();
                return "BuildingMenu width=" + BuildingWidth
                    + " rightX=" + RightColumnX
                    + " listHidden=true"
                    + " refs=" + (view != null && view.HasRequiredReferences());
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string UpdateInventoryPanelPrefab()
        {
            // Prefab이 없으면 Phase D 빌더로 생성.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPanelPrefabPath) == null)
            {
                InventoryPanelPrefabBuilder.BuildPrefab();
            }

            var root = PrefabUtility.LoadPrefabContents(InventoryPanelPrefabPath);
            try
            {
                EnsureInventoryCloseButton(root);
                PrefabUtility.SaveAsPrefabAsset(root, InventoryPanelPrefabPath);
                var view = root.GetComponent<InventoryPanelView>();
                return "InventoryPanel close="
                    + (view != null && view.CloseButton != null)
                    + " refs=" + (view != null && view.HasRequiredReferences());
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

            var buildingMenu = FindInSceneTransform(scene, "BuildingMenu");
            if (buildingMenu != null)
            {
                ApplyBuildingMenuLayout(buildingMenu.gameObject);
            }

            var inventory = EnsureInventoryInCanvas(canvas.transform);
            // I 키로 열기 전 숨김.
            inventory.SetActive(false);

            var chrome = canvas.GetComponent<HudPanelChromeController>();
            if (chrome == null)
            {
                chrome = canvas.gameObject.AddComponent<HudPanelChromeController>();
            }

            var invView = inventory.GetComponent<InventoryPanelView>();
            var invClose = invView != null ? invView.CloseButton : null;

            var chromeSo = new SerializedObject(chrome);
            chromeSo.FindProperty("inventoryPanelView").objectReferenceValue = invView;
            chromeSo.FindProperty("inventoryPanelRoot").objectReferenceValue = inventory;
            chromeSo.FindProperty("inventoryCloseButton").objectReferenceValue = invClose;
            chromeSo.FindProperty("inventoryPanelOpen").boolValue = false;
            chromeSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(chrome);
            EditorUtility.SetDirty(canvas.gameObject);

            var invWired = invView != null && inventory != null;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (!string.IsNullOrEmpty(previous)
                && previous != IntegrationScenePath
                && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }

            return "IntegrationScene inventoryWired=" + invWired
                + " buildingWidth=" + BuildingWidth
                + " rightGap=" + LeftRightGap;
        }

        internal static void ApplyBuildingMenuLayout(GameObject buildingRoot)
        {
            var rect = buildingRoot.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

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

            // 좌측 버튼 위 목록 텍스트 제거(숨김). 버튼 라벨만 남긴다.
            var listText = panelRoot.Find("BuildingListText");
            if (listText != null)
            {
                listText.gameObject.SetActive(false);
                EditorUtility.SetDirty(listText.gameObject);
            }

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
                // X는 좌측 열 정렬, 너비만 유지/축소.
                br.anchoredPosition = new Vector2(LeftColumnX, br.anchoredPosition.y);
                br.sizeDelta = new Vector2(
                    LeftButtonWidth,
                    br.sizeDelta.y > 1f ? br.sizeDelta.y : 32f);
                EditorUtility.SetDirty(br);
            }

            // 우측 상세: 좌측 버튼 끝 + 10px, 패널 +20px에 맞춰 폭 확장.
            PlaceRightText(panelRoot, "SelectionText", -64f, 200f);
            PlaceRightText(panelRoot, "AvailabilityText", -280f, 60f);
            PlaceRightText(panelRoot, "StatusText", -350f, 48f);

            var view = buildingRoot.GetComponent<BuildingMenuView>();
            if (view != null)
            {
                EditorUtility.SetDirty(view);
            }
        }

        private static void PlaceRightText(
            Transform panelRoot,
            string name,
            float anchoredY,
            float height)
        {
            var tf = panelRoot.Find(name) as RectTransform;
            if (tf == null)
            {
                return;
            }

            tf.anchorMin = tf.anchorMax = new Vector2(0f, 1f);
            tf.pivot = new Vector2(0f, 1f);
            tf.anchoredPosition = new Vector2(RightColumnX, anchoredY);
            tf.sizeDelta = new Vector2(RightColumnWidth, height);
            EditorUtility.SetDirty(tf);
        }

        private static GameObject EnsureInventoryInCanvas(Transform canvas)
        {
            var existing = canvas.Find("InventoryPanel");
            GameObject go;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPanelPrefabPath);
            if (existing == null)
            {
                if (prefab != null)
                {
                    go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
                    go.name = "InventoryPanel";
                }
                else
                {
                    go = new GameObject("InventoryPanel", typeof(RectTransform));
                    go.transform.SetParent(canvas, false);
                    go.AddComponent<InventoryPanelView>();
                    go.AddComponent<InventoryPanelBinder>();
                }
            }
            else
            {
                go = existing.gameObject;
            }

            // 화면 중앙 배치.
            var rect = go.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                if (rect.sizeDelta.x < 10f || rect.sizeDelta.y < 10f)
                {
                    rect.sizeDelta = new Vector2(420f, 320f);
                }

                EditorUtility.SetDirty(rect);
            }

            EnsureInventoryCloseButton(go);
            return go;
        }

        private static void EnsureInventoryCloseButton(GameObject root)
        {
            var panelRoot = root.transform.Find("PanelRoot");
            if (panelRoot == null)
            {
                panelRoot = root.transform;
            }

            var close = EnsureTopRightButton(
                panelRoot,
                "CloseButton",
                new Vector2(-8f, -8f),
                new Vector2(72f, 28f),
                "닫기");

            var view = root.GetComponent<InventoryPanelView>();
            if (view != null)
            {
                var so = new SerializedObject(view);
                so.FindProperty("closeButton").objectReferenceValue = close;
                if (so.FindProperty("panelRoot") != null
                    && so.FindProperty("panelRoot").objectReferenceValue == null
                    && panelRoot != null)
                {
                    so.FindProperty("panelRoot").objectReferenceValue = panelRoot.gameObject;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);
            }
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

            var labelTmp = EnsureTmp(go.transform, "Label", size, label);
            var lr = labelTmp.rectTransform;
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = Vector2.zero;
            lr.offsetMax = Vector2.zero;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.text = label;
            EditorUtility.SetDirty(go);
            return go.GetComponent<Button>();
        }

        private static TextMeshProUGUI EnsureTmp(
            Transform parent,
            string name,
            Vector2 size,
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
            tmp.fontSize = 16f;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
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
