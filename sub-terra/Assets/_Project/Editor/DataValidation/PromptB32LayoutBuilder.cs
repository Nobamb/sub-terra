using System.IO;
using System.Linq;
using System.Text;
using SubTerra.App.Core.Data;
using SubTerra.App.UI;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Inventory;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 32:
    /// - 우측 중앙 게임 가이드 / 시설 건설 재열기 버튼 제거
    /// - 우측 상단 단축키(B/I/G)로 패널 토글
    /// - 시설 창: "닫기" 제거, X만 유지하며 창 전체 닫기
    /// - 시설 창 너비·높이 +20px
    /// - 화물(I): 광물 아이콘(지형 비주얼) + 수량 표시
    /// </summary>
    public static class PromptB32LayoutBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string BuildingMenuPrefabPath =
            "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        private const string InventoryPanelPrefabPath =
            "Assets/_Project/Prefabs/UI/InventoryPanel.prefab";
        private const string CatalogPath =
            "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        // 31-2 기준 460x540 + 20.
        private const float BuildingWidth = 480f;
        private const float BuildingHeight = 560f;
        private const float LeftButtonWidth = 132f;
        private const float LeftColumnX = 20f;
        private const float LeftRightGap = 10f;
        private const float RightMargin = 16f;
        private static readonly float RightColumnX =
            LeftColumnX + LeftButtonWidth + LeftRightGap;
        private static readonly float RightColumnWidth =
            BuildingWidth - RightColumnX - RightMargin;

        private const float StatusTopY = -16f;
        private const float StatusHeight = 260f;
        private const float QuestGap = 12f;
        private const float QuestStartY = StatusTopY - StatusHeight - QuestGap;
        private const float QuestBottomOffset = 126f;
        private const float BuildingTopY = QuestStartY - QuestBottomOffset - QuestGap;

        private static readonly string[] SideButtonsToRemove =
        {
            "OpenGameGuideButton",
            "OpenBuildingMenuButton"
        };

        [MenuItem("SubTerra/UI/Build Prompt-B 32 Panel Fixes")]
        public static void BuildFromMenu()
        {
            var report = Build();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "prompt-b-32-layout.txt"),
                report);
        }

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Prompt-B 32 Panel Fixes");
            sb.AppendLine(AssignMineralIconsFromTerrainVisuals());
            sb.AppendLine(UpdateBuildingMenuPrefab());
            sb.AppendLine(UpdateInventoryPanelPrefab());
            sb.AppendLine(UpdateIntegrationScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sb.ToString();
        }

        /// <summary>광물 데이터 아이콘을 지형 비주얼 스프라이트(Copper/Iron/Lithium)로 맞춘다.</summary>
        private static string AssignMineralIconsFromTerrainVisuals()
        {
            var pairs = new[]
            {
                ("Assets/_Project/Data/Minerals/Mineral_Copper.asset",
                    "Assets/_Project/Visuals/Graybox/Terrain/CopperVisual.asset"),
                ("Assets/_Project/Data/Minerals/Mineral_Iron.asset",
                    "Assets/_Project/Visuals/Graybox/Terrain/IronVisual.asset"),
                ("Assets/_Project/Data/Minerals/Mineral_Lithium.asset",
                    "Assets/_Project/Visuals/Graybox/Terrain/LithiumVisual.asset")
            };

            var assigned = 0;
            foreach (var (mineralPath, visualPath) in pairs)
            {
                var mineral = AssetDatabase.LoadAssetAtPath<MineralData>(mineralPath);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(visualPath)
                    .OfType<Sprite>()
                    .ToArray();
                if (mineral == null || sprites.Length == 0)
                {
                    continue;
                }

                var sprite = sprites[0];
                mineral.EditorSet(
                    mineral.Id,
                    mineral.DisplayName,
                    mineral.UnitWeight,
                    mineral.UnitPrice,
                    sprite);
                EditorUtility.SetDirty(mineral);
                assigned++;
            }

            return "MineralIcons assigned=" + assigned + "/" + pairs.Length;
        }

        private static string UpdateBuildingMenuPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(BuildingMenuPrefabPath);
            try
            {
                ApplyBuildingSizeAndLayout(root);
                RemoveKoreanCloseButtons(root.transform);
                EnsureXCloseButton(root.transform, out var xClose);
                var view = root.GetComponent<BuildingMenuView>();
                if (view != null)
                {
                    var so = new SerializedObject(view);
                    so.FindProperty("closeButton").objectReferenceValue = xClose;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(view);
                }

                PrefabUtility.SaveAsPrefabAsset(root, BuildingMenuPrefabPath);
                return "BuildingMenu prefab size=" + BuildingWidth + "x" + BuildingHeight
                    + " xClose=" + (xClose != null)
                    + " noKoreanClose=" + !HasKoreanClose(root.transform);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string UpdateInventoryPanelPrefab()
        {
            // 아이콘이 반영된 카탈로그로 패널을 재생성한다.
            var report = InventoryPanelPrefabBuilder.BuildPrefab();
            var root = PrefabUtility.LoadPrefabContents(InventoryPanelPrefabPath);
            try
            {
                EnsureXCloseButton(root.transform, out var xClose);
                var view = root.GetComponent<InventoryPanelView>();
                if (view != null)
                {
                    var so = new SerializedObject(view);
                    so.FindProperty("closeButton").objectReferenceValue = xClose;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(view);
                }

                PrefabUtility.SaveAsPrefabAsset(root, InventoryPanelPrefabPath);
                return report + " inventoryXClose=" + (xClose != null);
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

            // 1) 우측 중앙 재열기 버튼 제거.
            var removedSide = 0;
            foreach (var name in SideButtonsToRemove)
            {
                var t = canvas.transform.Find(name);
                if (t == null)
                {
                    t = FindInSceneTransform(scene, name);
                }

                if (t != null)
                {
                    Object.DestroyImmediate(t.gameObject);
                    removedSide++;
                }
            }

            // 2) 시설 건설 패널: 크기 + 닫기 정리 + X 유지.
            var building = FindInSceneTransform(scene, "BuildingPanel")
                ?? FindInSceneTransform(scene, "BuildingMenu");
            Button buildingX = null;
            if (building != null)
            {
                ApplyBuildingSizeAndLayout(building.gameObject);
                RemoveKoreanCloseButtons(building);
                EnsureXCloseButton(building, out buildingX);
                var view = building.GetComponent<BuildingMenuView>();
                if (view != null)
                {
                    var so = new SerializedObject(view);
                    so.FindProperty("closeButton").objectReferenceValue = buildingX;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(view);
                }
            }

            // 3) 인벤토리: 아이콘 반영 prefab으로 교체·동기화.
            var inventory = EnsureInventoryPanel(canvas.transform, scene);
            Button inventoryX = null;
            if (inventory != null)
            {
                EnsureXCloseButton(inventory.transform, out inventoryX);
                inventory.SetActive(false);
            }

            // 4) 게임 가이드 (전체 화면 70% 중앙 패널).
            var guide = FindInSceneTransform(scene, "GameGuidePanel");
            Button guideClose = null;
            if (guide != null)
            {
                guide.gameObject.SetActive(false);
                var guideView = guide.GetComponent<GameGuidePanelView>();
                guideClose = guideView != null ? guideView.CloseButton : null;
                // 가이드 닫기가 "닫기" 텍스트여도 유지(가이드는 닫기 버튼이 표준).
            }

            // 5) Chrome 컨트롤러 배선.
            var chrome = canvas.GetComponent<HudPanelChromeController>();
            if (chrome == null)
            {
                chrome = canvas.gameObject.AddComponent<HudPanelChromeController>();
            }

            var digger = FindInSceneTransform(scene, "DroneDialoguePanel")
                ?? FindInSceneTransform(scene, "DiggerBotPanel");
            var openDigger = canvas.transform.Find("OpenDiggerBotButton")
                ?.GetComponent<Button>();

            var buildingView = building != null
                ? building.GetComponent<BuildingMenuView>()
                : null;
            var buildingBinder = building != null
                ? building.GetComponent<BuildingMenuBinder>()
                : null;
            var diggerView = digger != null
                ? digger.GetComponent<SubTerra.App.UI.Drone.DroneDialoguePanelView>()
                : null;
            // 드론 패널 닫기는 "닫기" 또는 × 모두 허용.
            var diggerClose = digger != null
                ? digger.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(b => b.name == "CloseButton")
                : null;

            var invView = inventory != null
                ? inventory.GetComponent<InventoryPanelView>()
                : null;
            var guideViewComp = guide != null
                ? guide.GetComponent<GameGuidePanelView>()
                : null;

            var chromeSo = new SerializedObject(chrome);
            chromeSo.FindProperty("buildingMenuView").objectReferenceValue = buildingView;
            chromeSo.FindProperty("buildingMenuBinder").objectReferenceValue = buildingBinder;
            chromeSo.FindProperty("buildingMenuRoot").objectReferenceValue =
                building != null ? building.gameObject : null;
            chromeSo.FindProperty("buildingCloseButton").objectReferenceValue = buildingX;
            chromeSo.FindProperty("buildingOpenButton").objectReferenceValue = null;
            chromeSo.FindProperty("diggerBotView").objectReferenceValue = diggerView;
            chromeSo.FindProperty("diggerBotRoot").objectReferenceValue =
                digger != null ? digger.gameObject : null;
            chromeSo.FindProperty("diggerCloseButton").objectReferenceValue = diggerClose;
            chromeSo.FindProperty("diggerOpenButton").objectReferenceValue = openDigger;
            chromeSo.FindProperty("gameGuideView").objectReferenceValue = guideViewComp;
            chromeSo.FindProperty("gameGuideRoot").objectReferenceValue =
                guide != null ? guide.gameObject : null;
            chromeSo.FindProperty("gameGuideCloseButton").objectReferenceValue = guideClose;
            chromeSo.FindProperty("gameGuideOpenButton").objectReferenceValue = null;
            chromeSo.FindProperty("inventoryPanelView").objectReferenceValue = invView;
            chromeSo.FindProperty("inventoryPanelRoot").objectReferenceValue = inventory;
            chromeSo.FindProperty("inventoryCloseButton").objectReferenceValue = inventoryX;
            chromeSo.FindProperty("buildingMenuOpen").boolValue =
                building != null && building.gameObject.activeSelf;
            chromeSo.FindProperty("diggerBotOpen").boolValue =
                digger == null || digger.gameObject.activeSelf;
            chromeSo.FindProperty("gameGuideOpen").boolValue = false;
            chromeSo.FindProperty("inventoryPanelOpen").boolValue = false;
            chromeSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(chrome);

            // 6) 우측 상단 PanelShortcutBar → chrome 토글.
            WireShortcutBarToChrome(scene, chrome);

            // 7) PanelToggleController: Building/Inventory/GameGuide 루트 비워 이중 토글 방지.
            //    Upgrade(U)만 유지.
            var ptc = Object.FindFirstObjectByType<PanelToggleController>(
                FindObjectsInactive.Include);
            if (ptc != null)
            {
                var ptcSo = new SerializedObject(ptc);
                var panels = ptcSo.FindProperty("panels");
                for (var i = 0; i < panels.arraySize; i++)
                {
                    var panel = panels.GetArrayElementAtIndex(i);
                    var id = (RuntimePanelId)panel.FindPropertyRelative("panelId").enumValueIndex;
                    if (id == RuntimePanelId.Building
                        || id == RuntimePanelId.Inventory
                        || id == RuntimePanelId.GameGuide)
                    {
                        panel.FindPropertyRelative("panelRoot").objectReferenceValue = null;
                        panel.FindPropertyRelative("visibleOnStart").boolValue = false;
                    }
                }

                ptcSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(ptc);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (!string.IsNullOrEmpty(previous)
                && previous != IntegrationScenePath
                && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }

            var buildingSizeOk = building != null
                && building is RectTransform brt
                && Mathf.Abs(brt.sizeDelta.x - BuildingWidth) < 0.5f
                && Mathf.Abs(brt.sizeDelta.y - BuildingHeight) < 0.5f;

            return "IntegrationScene removedSide=" + removedSide
                + " buildingX=" + (buildingX != null)
                + " buildingSizeOk=" + buildingSizeOk
                + " noKoreanClose=" + (building == null || !HasKoreanClose(building))
                + " inventoryWired=" + (invView != null && inventory != null)
                + " guideWired=" + (guide != null)
                + " chromeRefs=" + chrome.HasRequiredReferences();
        }

        private static void WireShortcutBarToChrome(Scene scene, HudPanelChromeController chrome)
        {
            var bar = FindInSceneTransform(scene, "PanelShortcutBar");
            if (bar == null || chrome == null)
            {
                return;
            }

            foreach (var button in bar.GetComponentsInChildren<Button>(true))
            {
                // 기존 persistent 리스너 제거 후 chrome에 재연결.
                while (button.onClick.GetPersistentEventCount() > 0)
                {
                    UnityEventTools.RemovePersistentListener(button.onClick, 0);
                }

                var label = button.GetComponentInChildren<TMP_Text>(true);
                var text = label != null ? label.text : button.name;
                if (text.Contains("시설") || text.Contains("[B]"))
                {
                    UnityEventTools.AddPersistentListener(
                        button.onClick,
                        chrome.ToggleBuildingMenu);
                }
                else if (text.Contains("화물")
                    || text.Contains("인벤토리")
                    || text.Contains("[I]")
                    || text.Contains("(I)"))
                {
                    // prompt-B 33-1: 라벨은 인벤토리(I)를 권장하되 구 라벨도 배선한다.
                    var invLabel = button.GetComponentInChildren<TMP_Text>(true);
                    if (invLabel != null
                        && (invLabel.text.Contains("화물") || invLabel.text.Contains("[I]")))
                    {
                        invLabel.text = "인벤토리(I)";
                        EditorUtility.SetDirty(invLabel);
                    }

                    UnityEventTools.AddPersistentListener(
                        button.onClick,
                        chrome.ToggleInventoryPanel);
                }
                else if (text.Contains("가이드") || text.Contains("[G]"))
                {
                    UnityEventTools.AddPersistentListener(
                        button.onClick,
                        chrome.ToggleGameGuide);
                }
                else if (text.Contains("업그레이드") || text.Contains("[U]"))
                {
                    var ptc = bar.GetComponentInParent<PanelToggleController>();
                    if (ptc != null)
                    {
                        UnityEventTools.AddPersistentListener(
                            button.onClick,
                            ptc.ToggleUpgrade);
                    }
                }

                EditorUtility.SetDirty(button);
            }
        }

        private static GameObject EnsureInventoryPanel(Transform canvas, Scene scene)
        {
            var existing = FindInSceneTransform(scene, "InventoryPanel");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPanelPrefabPath);
            GameObject go;
            if (existing == null)
            {
                var parent = canvas.Find("PanelLayout") != null
                    ? canvas.Find("PanelLayout")
                    : canvas;
                if (prefab != null)
                {
                    go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    go.name = "InventoryPanel";
                }
                else
                {
                    go = new GameObject("InventoryPanel", typeof(RectTransform));
                    go.transform.SetParent(parent, false);
                }
            }
            else
            {
                go = existing.gameObject;
                // Prefab 아이콘 행이 최신인지 확인하고 뷰 참조를 유지한 채 아이콘만 갱신.
                RefreshInventoryIcons(go);
            }

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

            return go;
        }

        private static void RefreshInventoryIcons(GameObject inventoryRoot)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            if (catalog == null || catalog.Minerals == null)
            {
                return;
            }

            foreach (var row in inventoryRoot.GetComponentsInChildren<InventoryStackRowView>(true))
            {
                if (row == null)
                {
                    continue;
                }

                MineralData mineral = null;
                for (var i = 0; i < catalog.Minerals.Count; i++)
                {
                    if (catalog.Minerals[i] != null
                        && catalog.Minerals[i].Id == row.MineralId)
                    {
                        mineral = catalog.Minerals[i];
                        break;
                    }
                }

                if (mineral == null || mineral.Icon == null)
                {
                    continue;
                }

                var iconImage = row.GetComponentInChildren<Image>(true);
                // 첫 Image는 행 배경일 수 있으므로 Icon 자식 우선.
                var iconTf = row.transform.Find("Icon");
                if (iconTf != null)
                {
                    iconImage = iconTf.GetComponent<Image>();
                }

                if (iconImage == null)
                {
                    continue;
                }

                iconImage.sprite = mineral.Icon;
                iconImage.enabled = true;
                iconImage.preserveAspect = true;
                EditorUtility.SetDirty(iconImage);
            }
        }

        private static void ApplyBuildingSizeAndLayout(GameObject buildingRoot)
        {
            var rect = buildingRoot.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            // 좌측 퀘스트 하단 배치 유지.
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

        private static void RemoveKoreanCloseButtons(Transform root)
        {
            var toDestroy = root.GetComponentsInChildren<Button>(true)
                .Where(IsKoreanClose)
                .Select(b => b.gameObject)
                .Distinct()
                .ToList();

            for (var i = 0; i < toDestroy.Count; i++)
            {
                Object.DestroyImmediate(toDestroy[i]);
            }
        }

        private static bool HasKoreanClose(Transform root)
        {
            return root.GetComponentsInChildren<Button>(true).Any(IsKoreanClose);
        }

        private static bool IsKoreanClose(Button button)
        {
            if (button == null || button.name != "CloseButton")
            {
                return false;
            }

            var tmp = button.GetComponentInChildren<TMP_Text>(true);
            var text = tmp != null ? tmp.text : string.Empty;
            return text.Contains("닫기");
        }

        private static bool IsXLabel(Button button)
        {
            var tmp = button.GetComponentInChildren<TMP_Text>(true);
            var text = tmp != null ? tmp.text : string.Empty;
            return text == "×" || text == "x" || text == "X" || text == "✕";
        }

        private static void EnsureXCloseButton(Transform root, out Button xClose)
        {
            var xButtons = root.GetComponentsInChildren<Button>(true)
                .Where(b => b.name == "CloseButton" && IsXLabel(b))
                .ToList();

            // 중복 X 버튼이 있으면 하나만 남긴다.
            xClose = xButtons.Count > 0 ? xButtons[0] : null;
            for (var i = 1; i < xButtons.Count; i++)
            {
                Object.DestroyImmediate(xButtons[i].gameObject);
            }

            if (xClose != null)
            {
                // 루트 직계 자식으로 올려 패널 전체와 함께 보이도록 한다.
                if (xClose.transform.parent != root)
                {
                    xClose.transform.SetParent(root, false);
                }

                LayoutXButton(xClose);
                return;
            }

            // 기존 CloseButton이 "닫기"였다면 이미 제거됐을 수 있음 → X 생성.
            var go = new GameObject(
                "CloseButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            go.transform.SetParent(root, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.18f, 0.18f, 0.95f);
            image.raycastTarget = true;
            xClose = go.GetComponent<Button>();
            LayoutXButton(xClose);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            var font = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (font != null)
            {
                label.font = font;
            }

            label.text = "×";
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            var lr = label.rectTransform;
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = Vector2.zero;
            lr.offsetMax = Vector2.zero;
            EditorUtility.SetDirty(go);
        }

        private static void LayoutXButton(Button button)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-12f, -10f);
            rect.sizeDelta = new Vector2(36f, 36f);
            EditorUtility.SetDirty(rect);
            EditorUtility.SetDirty(button);
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
