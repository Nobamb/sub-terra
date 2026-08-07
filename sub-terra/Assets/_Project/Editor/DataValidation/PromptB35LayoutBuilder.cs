using System.IO;
using System.Linq;
using System.Text;
using SubTerra.App.UI.Building;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 35:
    /// - 시설 건설 창 너비 +10%
    /// - 좌측 버튼 영역과 우측 설명 영역 간격 +10%
    /// </summary>
    public static class PromptB35LayoutBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string BuildingMenuPrefabPath =
            "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";

        // prompt-B 33: 528, gap = 10 + 528*0.05 = 36.4
        // prompt-B 35: 너비·간격 각각 +10%
        public const float BuildingWidth = 528f * 1.1f;
        public const float BuildingHeight = 560f;
        public const float LeftButtonWidth = 132f;
        public const float LeftColumnX = 20f;
        public static readonly float LeftRightGap = (10f + 528f * 0.05f) * 1.1f;
        private const float RightMargin = 16f;
        public static readonly float RightColumnX =
            LeftColumnX + LeftButtonWidth + LeftRightGap;
        public static readonly float RightColumnWidth =
            BuildingWidth - RightColumnX - RightMargin;

        private const float StatusTopY = -16f;
        private const float StatusHeight = 260f;
        private const float QuestGap = 12f;
        private const float QuestStartY = StatusTopY - StatusHeight - QuestGap;
        private const float QuestBottomOffset = 126f;
        private const float BuildingTopY = QuestStartY - QuestBottomOffset - QuestGap;

        [MenuItem("SubTerra/UI/Build Prompt-B 35 Building Panel Layout")]
        public static void BuildFromMenu()
        {
            var report = Build();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "prompt-b-35-layout.txt"),
                report);
        }

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Prompt-B 35 Building Panel Layout");
            sb.AppendLine(UpdateBuildingMenuPrefab());
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
                ApplyBuildingLayout(root);
                PrefabUtility.SaveAsPrefabAsset(root, BuildingMenuPrefabPath);
                return "BuildingMenu width=" + BuildingWidth.ToString("0.#")
                    + " gap=" + LeftRightGap.ToString("0.#")
                    + " rightX=" + RightColumnX.ToString("0.#");
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
                return "FAIL: open integration";
            }

            var building = FindTransform(scene, "BuildingPanel")
                ?? FindTransform(scene, "BuildingMenu");
            if (building != null)
            {
                ApplyBuildingLayout(building.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(previous)
                && previous != IntegrationScenePath
                && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }

            return building != null
                ? "Integration BuildingPanel width=" + BuildingWidth.ToString("0.#")
                : "FAIL: BuildingPanel missing";
        }

        private static void ApplyBuildingLayout(GameObject buildingRoot)
        {
            var rect = buildingRoot.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(16f, BuildingTopY);
                rect.sizeDelta = new Vector2(BuildingWidth, BuildingHeight);
                EditorUtility.SetDirty(rect);
            }

            var panelRoot = buildingRoot.transform.Find("PanelRoot");
            if (panelRoot == null)
            {
                panelRoot = buildingRoot.transform;
            }

            // 민트 아이콘·취소 버튼이 남아 있으면 제거(이전 단계 잔여).
            foreach (var t in panelRoot.GetComponentsInChildren<Transform>(true)
                         .Where(x => x.name == "CancelButton" || x.name == "SelectedIcon")
                         .Select(x => x.gameObject)
                         .Distinct()
                         .ToList())
            {
                Object.DestroyImmediate(t);
            }

            var listText = panelRoot.Find("BuildingListText");
            if (listText != null)
            {
                listText.gameObject.SetActive(false);
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
                br.anchoredPosition = new Vector2(LeftColumnX, br.anchoredPosition.y);
                br.sizeDelta = new Vector2(
                    LeftButtonWidth,
                    br.sizeDelta.y > 1f ? br.sizeDelta.y : 32f);
                EditorUtility.SetDirty(br);
            }

            PlaceRightText(panelRoot, "SelectionText", -64f, 200f);
            PlaceRightText(panelRoot, "AvailabilityText", -280f, 60f);
            PlaceRightText(panelRoot, "StatusText", -350f, 48f);

            // X 닫기 버튼이 넓어진 패널 우측 안에 남도록 보정.
            var close = panelRoot.Find("CloseButton") as RectTransform
                ?? buildingRoot.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "CloseButton") as RectTransform;
            if (close != null)
            {
                close.anchorMin = close.anchorMax = new Vector2(1f, 1f);
                close.pivot = new Vector2(1f, 1f);
                close.anchoredPosition = new Vector2(-8f, -8f);
                close.sizeDelta = new Vector2(
                    close.sizeDelta.x > 1f ? close.sizeDelta.x : 36f,
                    close.sizeDelta.y > 1f ? close.sizeDelta.y : 36f);
                EditorUtility.SetDirty(close);
            }

            var view = buildingRoot.GetComponent<BuildingMenuView>();
            if (view != null)
            {
                var so = new SerializedObject(view);
                var iconProp = so.FindProperty("selectedIcon");
                if (iconProp != null)
                {
                    iconProp.objectReferenceValue = null;
                }

                var cancelProp = so.FindProperty("cancelButton");
                if (cancelProp != null)
                {
                    cancelProp.objectReferenceValue = null;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
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
            var tmp = tf.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.textWrappingMode = TextWrappingModes.Normal;
            }

            EditorUtility.SetDirty(tf);
        }

        private static Transform FindTransform(Scene scene, string objectName)
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
    }
}
