using System.IO;
using System.Linq;
using System.Text;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
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
    /// prompt-B 33:
    /// - 시설 창: X 뒤 민트 아이콘·건설 취소 제거, 너비 +10%, 좌/우 간격 +5%
    /// - Surface Base: 하단 텍스트 좌/우 분리
    /// - 업그레이드: 탭 분류, 한국어 비용, 전체 업그레이드 진입
    /// </summary>
    public static class PromptB33LayoutBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string SurfaceBaseScenePath =
            "Assets/_Project/Scenes/App/SurfaceBase.unity";
        private const string BuildingMenuPrefabPath =
            "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";
        private const string SurfaceBasePrefabPath =
            "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        private const string CatalogPath =
            "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        // prompt-B 32: 480 → +10% = 528
        private const float BuildingWidth = 528f;
        private const float BuildingHeight = 560f;
        private const float LeftButtonWidth = 132f;
        private const float LeftColumnX = 20f;
        // 기존 10 + 패널 너비의 5%
        private static readonly float LeftRightGap = 10f + BuildingWidth * 0.05f;
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

        [MenuItem("SubTerra/UI/Build Prompt-B 33 Panel Fixes")]
        public static void BuildFromMenu()
        {
            var report = Build();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "prompt-b-33-layout.txt"),
                report);
        }

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Prompt-B 33 Panel Fixes");
            sb.AppendLine(UpdateMineralDisplayNames());
            sb.AppendLine(UpdateBuildingMenuPrefab());
            sb.AppendLine(UpdateSurfaceBasePrefab());
            sb.AppendLine(UpdateIntegrationScene());
            sb.AppendLine(UpdateSurfaceBaseScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sb.ToString();
        }

        private static string UpdateMineralDisplayNames()
        {
            var pairs = new[]
            {
                ("Assets/_Project/Data/Minerals/Mineral_Copper.asset", "구리"),
                ("Assets/_Project/Data/Minerals/Mineral_Iron.asset", "철"),
                ("Assets/_Project/Data/Minerals/Mineral_Lithium.asset", "리튬")
            };

            var count = 0;
            foreach (var (path, name) in pairs)
            {
                var mineral = AssetDatabase.LoadAssetAtPath<MineralData>(path);
                if (mineral == null)
                {
                    continue;
                }

                mineral.EditorSet(
                    mineral.Id,
                    name,
                    mineral.UnitWeight,
                    mineral.UnitPrice,
                    mineral.Icon);
                EditorUtility.SetDirty(mineral);
                count++;
            }

            return "MineralDisplayNames ko=" + count;
        }

        private static string UpdateBuildingMenuPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(BuildingMenuPrefabPath);
            try
            {
                ApplyBuildingLayout(root);
                PrefabUtility.SaveAsPrefabAsset(root, BuildingMenuPrefabPath);
                return "BuildingMenu width=" + BuildingWidth
                    + " gap=" + LeftRightGap.ToString("0.#")
                    + " noCancel=" + (root.transform.Find("PanelRoot/CancelButton") == null
                        && root.GetComponentsInChildren<Transform>(true)
                            .All(t => t.name != "CancelButton"))
                    + " noSelectedIcon=" + root.GetComponentsInChildren<Transform>(true)
                        .All(t => t.name != "SelectedIcon" || !t.gameObject.activeSelf);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string UpdateSurfaceBasePrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(SurfaceBasePrefabPath);
            try
            {
                ApplySurfaceBaseBottomSplit(root);
                EnsureSurfaceUpgradeTabs(root);
                PrefabUtility.SaveAsPrefabAsset(root, SurfaceBasePrefabPath);
                return "SurfaceBase left/right split + upgrade tabs";
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

            var upgrade = FindTransform(scene, "UpgradePanel");
            if (upgrade != null)
            {
                EnsureUpgradePanelTabs(upgrade.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(previous)
                && previous != IntegrationScenePath
                && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }

            return "Integration building+upgrade updated";
        }

        private static string UpdateSurfaceBaseScene()
        {
            var previous = SceneManager.GetActiveScene().path;
            if (!File.Exists(SurfaceBaseScenePath))
            {
                return "SKIP: SurfaceBase scene missing";
            }

            var scene = EditorSceneManager.OpenScene(SurfaceBaseScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                return "FAIL: open SurfaceBase";
            }

            // Prefab 인스턴스가 있으면 재적용, 없으면 씬 내 패널 검색.
            var panel = Object.FindFirstObjectByType<SurfaceBaseView>(FindObjectsInactive.Include);
            if (panel != null)
            {
                ApplySurfaceBaseBottomSplit(panel.gameObject);
                EnsureSurfaceUpgradeTabs(panel.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(panel.gameObject);
                EditorUtility.SetDirty(panel.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(previous)
                && previous != SurfaceBaseScenePath
                && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }

            return "SurfaceBase scene split applied";
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

            // 민트 아이콘 제거.
            var selectedIcon = panelRoot.Find("SelectedIcon");
            if (selectedIcon != null)
            {
                Object.DestroyImmediate(selectedIcon.gameObject);
            }

            // 건설 취소 제거.
            var cancel = panelRoot.Find("CancelButton");
            if (cancel != null)
            {
                Object.DestroyImmediate(cancel.gameObject);
            }

            // 혹시 다른 경로에 남아 있으면 전부 제거.
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
            EditorUtility.SetDirty(tf);
        }

        /// <summary>
        /// Surface Base 하단: 좌측 경제/상태, 우측 업그레이드 레벨 영역으로 분리.
        /// </summary>
        private static void ApplySurfaceBaseBottomSplit(GameObject root)
        {
            var content = root.transform.Find("SurfaceBaseContent") ?? root.transform;

            // 좌측 열: 목표·심층·최근·메시지·경제
            PlaceAnchored(content, "GoalsText", new Vector2(-220f, -120f), new Vector2(400f, 48f));
            PlaceAnchored(content, "DeepZoneText", new Vector2(-220f, -170f), new Vector2(400f, 42f));
            PlaceAnchored(content, "RecentRunText", new Vector2(-220f, -220f), new Vector2(400f, 42f));
            PlaceAnchored(content, "MessageText", new Vector2(-220f, -270f), new Vector2(400f, 40f));

            var economy = content.Find("EconomyPanel");
            if (economy != null)
            {
                PlaceAnchored(economy, "EcoStatus", new Vector2(-220f, -320f), new Vector2(400f, 40f));
                PlaceAnchored(economy, "EcoDetail", new Vector2(-220f, -360f), new Vector2(400f, 36f));
            }

            // 우측 열: 업그레이드 목록·상세·결과·심층
            var progression = content.Find("ProgressionPanel");
            if (progression != null)
            {
                PlaceAnchored(progression, "UpgradeList", new Vector2(220f, -120f), new Vector2(420f, 48f));
                PlaceAnchored(progression, "UpgradeDetail", new Vector2(220f, -200f), new Vector2(420f, 70f));
                PlaceAnchored(progression, "UpgradeResult", new Vector2(220f, -280f), new Vector2(420f, 40f));
                PlaceAnchored(progression, "ProgDeep", new Vector2(220f, -330f), new Vector2(420f, 40f));
            }
        }

        private static void PlaceAnchored(
            Transform parent,
            string childName,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var child = parent.Find(childName) as RectTransform;
            if (child == null)
            {
                // 재귀 검색.
                foreach (var t in parent.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == childName && t is RectTransform rt)
                    {
                        child = rt;
                        break;
                    }
                }
            }

            if (child == null)
            {
                return;
            }

            child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
            child.pivot = new Vector2(0.5f, 0.5f);
            child.anchoredPosition = anchoredPosition;
            child.sizeDelta = size;
            var tmp = child.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.textWrappingMode = TextWrappingModes.Normal;
            }

            EditorUtility.SetDirty(child);
        }

        private static void EnsureSurfaceUpgradeTabs(GameObject root)
        {
            var progression = root.GetComponentsInChildren<ProgressionPanelView>(true)
                .FirstOrDefault();
            if (progression == null)
            {
                return;
            }

            var panel = progression.transform;
            EnsureCategoryTabs(panel, progression, compact: true);

            // Surface Base에 구매 버튼이 없으면 추가.
            var so = new SerializedObject(progression);
            var purchaseProp = so.FindProperty("purchaseButton");
            if (purchaseProp != null && purchaseProp.objectReferenceValue == null)
            {
                var purchase = CreateButton(
                    panel,
                    "PurchaseButton",
                    new Vector2(220f, -390f),
                    new Vector2(200f, 42f),
                    "업그레이드 구매");
                purchaseProp.objectReferenceValue = purchase;
                var binder = progression.GetComponent<ProgressionPanelBinder>();
                if (binder != null)
                {
                    while (purchase.onClick.GetPersistentEventCount() > 0)
                    {
                        UnityEventTools.RemovePersistentListener(purchase.onClick, 0);
                    }

                    UnityEventTools.AddPersistentListener(purchase.onClick, binder.PurchaseSelected);
                }
            }

            // 카탈로그 전체 업그레이드 버튼 생성(없으면).
            EnsureUpgradeEntryButtons(panel, progression, compact: true);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(progression);
        }

        private static void EnsureUpgradePanelTabs(GameObject upgradeRoot)
        {
            var view = upgradeRoot.GetComponent<ProgressionPanelView>();
            if (view == null)
            {
                return;
            }

            EnsureCategoryTabs(upgradeRoot.transform, view, compact: false);
            EnsureUpgradeEntryButtons(upgradeRoot.transform, view, compact: false);

            // 탭 아래로 엔트리 버튼 재배치.
            RepositionUpgradeEntries(upgradeRoot.transform, startY: -150f, columnX: 22f);
            EditorUtility.SetDirty(view);
        }

        private static void EnsureCategoryTabs(
            Transform parent,
            ProgressionPanelView view,
            bool compact)
        {
            var tabBar = parent.Find("CategoryTabBar");
            if (tabBar == null)
            {
                var go = new GameObject("CategoryTabBar", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                tabBar = go.transform;
            }

            var tabBarRect = tabBar.GetComponent<RectTransform>();
            tabBarRect.anchorMin = tabBarRect.anchorMax = new Vector2(0.5f, 0.5f);
            tabBarRect.pivot = new Vector2(0.5f, 0.5f);
            if (compact)
            {
                tabBarRect.anchoredPosition = new Vector2(220f, -70f);
                tabBarRect.sizeDelta = new Vector2(420f, 36f);
            }
            else
            {
                tabBarRect.anchorMin = tabBarRect.anchorMax = new Vector2(0f, 1f);
                tabBarRect.pivot = new Vector2(0f, 1f);
                tabBarRect.anchoredPosition = new Vector2(22f, -58f);
                tabBarRect.sizeDelta = new Vector2(640f, 40f);
            }

            var buttons = new Button[UpgradeCategoryRules.TabLabels.Length];
            var labels = new TMP_Text[UpgradeCategoryRules.TabLabels.Length];
            var tabWidth = compact ? 100f : 150f;
            for (var i = 0; i < UpgradeCategoryRules.TabLabels.Length; i++)
            {
                var name = "CategoryTab_" + i;
                var existing = tabBar.Find(name);
                Button button;
                if (existing == null)
                {
                    button = CreateButton(
                        tabBar,
                        name,
                        new Vector2(i * (tabWidth + 6f), 0f),
                        new Vector2(tabWidth, 34f),
                        UpgradeCategoryRules.TabLabels[i]);
                }
                else
                {
                    button = existing.GetComponent<Button>();
                    var label = existing.GetComponentInChildren<TMP_Text>(true);
                    if (label != null)
                    {
                        label.text = UpgradeCategoryRules.TabLabels[i];
                    }
                }

                // 탭 클릭 → View.SelectCategoryTab(i)
                while (button.onClick.GetPersistentEventCount() > 0)
                {
                    UnityEventTools.RemovePersistentListener(button.onClick, 0);
                }

                UnityEventTools.AddIntPersistentListener(
                    button.onClick,
                    view.SelectCategoryTab,
                    i);

                buttons[i] = button;
                labels[i] = button.GetComponentInChildren<TMP_Text>(true);
            }

            var so = new SerializedObject(view);
            var tabButtons = so.FindProperty("categoryTabButtons");
            var tabLabels = so.FindProperty("categoryTabLabels");
            if (tabButtons != null)
            {
                tabButtons.arraySize = buttons.Length;
                for (var i = 0; i < buttons.Length; i++)
                {
                    tabButtons.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
                }
            }

            if (tabLabels != null)
            {
                tabLabels.arraySize = labels.Length;
                for (var i = 0; i < labels.Length; i++)
                {
                    tabLabels.GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureUpgradeEntryButtons(
            Transform parent,
            ProgressionPanelView view,
            bool compact)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            if (catalog == null || catalog.Upgrades == null)
            {
                return;
            }

            var binder = parent.GetComponent<ProgressionPanelBinder>()
                ?? parent.GetComponentInParent<ProgressionPanelBinder>();
            if (binder == null)
            {
                return;
            }

            var existing = parent.GetComponentsInChildren<ProgressionUpgradeEntryButton>(true)
                .ToList();
            if (existing.Count >= catalog.Upgrades.Count)
            {
                // 이미 충분하면 스냅샷 레이블만 갱신 가능.
                WireUpgradeButtons(view, existing);
                return;
            }

            // 부족하면 전부 제거하고 재생성.
            for (var i = 0; i < existing.Count; i++)
            {
                if (existing[i] != null)
                {
                    Object.DestroyImmediate(existing[i].gameObject);
                }
            }

            var startY = compact ? -160f : -150f;
            var columnX = compact ? 220f : 22f;
            var entries = new System.Collections.Generic.List<ProgressionUpgradeEntryButton>();
            var row = 0;
            for (var i = 0; i < catalog.Upgrades.Count; i++)
            {
                var upgrade = catalog.Upgrades[i];
                if (upgrade == null || string.IsNullOrEmpty(upgrade.Id))
                {
                    continue;
                }

                var display = ItemDisplayNames.PreferDisplay(upgrade.Id, upgrade.DisplayName);
                var button = CreateButton(
                    parent,
                    "UpgradeEntry_" + upgrade.Id.Replace('.', '_'),
                    new Vector2(columnX, startY - row * 44f),
                    new Vector2(compact ? 280f : 280f, 38f),
                    display);
                var entry = button.gameObject.AddComponent<ProgressionUpgradeEntryButton>();
                entry.EditorSet(upgrade.Id, binder, button.GetComponentInChildren<TMP_Text>());
                entries.Add(entry);
                row++;
            }

            WireUpgradeButtons(view, entries);
        }

        private static void WireUpgradeButtons(
            ProgressionPanelView view,
            System.Collections.Generic.IList<ProgressionUpgradeEntryButton> entries)
        {
            var so = new SerializedObject(view);
            var prop = so.FindProperty("upgradeButtons");
            if (prop == null)
            {
                return;
            }

            prop.arraySize = entries.Count;
            for (var i = 0; i < entries.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        private static void RepositionUpgradeEntries(
            Transform parent,
            float startY,
            float columnX)
        {
            var entries = parent.GetComponentsInChildren<ProgressionUpgradeEntryButton>(true);
            // 카테고리 내 순서는 전체 목록 순을 유지. 활성 필터는 런타임.
            for (var i = 0; i < entries.Length; i++)
            {
                var rect = entries[i].GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }

                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(columnX, startY - i * 44f);
                rect.sizeDelta = new Vector2(280f, 38f);
                EditorUtility.SetDirty(rect);
            }
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.14f, 0.28f, 0.34f, 1f);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var text = labelGo.AddComponent<TextMeshProUGUI>();
            var font = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (font != null)
            {
                text.font = font;
            }

            text.text = label;
            text.fontSize = 16f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            var lr = text.rectTransform;
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
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
