using System.IO;
using System.Linq;
using System.Text;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
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
    /// prompt-B 33-3:
    /// - 장비 업그레이드 창 너비 70%, 탭 오버플로 해소
    /// - 좌측 하위 목록 Y를 드릴 탭 기준으로 고정
    /// - 업그레이드 버튼을 우측 설명란 아래 배치
    /// - 심층 구역 전용 탭, 창/Surface 공통 심층 잔여 문구 제거
    /// - Surface Base 하단은 7종 장비 레벨 요약만
    /// </summary>
    public static class PromptB33_3LayoutBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string SurfaceBaseScenePath =
            "Assets/_Project/Scenes/App/SurfaceBase.unity";
        private const string SurfaceBasePrefabPath =
            "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        private const string CatalogPath =
            "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        // 화면 가로 70% (0.15~0.85), 세로는 여유 있게 50%
        private const float UpgradeAnchorMinX = 0.15f;
        private const float UpgradeAnchorMaxX = 0.85f;
        private const float UpgradeAnchorMinY = 0.25f;
        private const float UpgradeAnchorMaxY = 0.75f;

        private const float EntryStartY = -120f;
        private const float EntryRowHeight = 46f;
        private const float EntryColumnX = 20f;
        private const float EntryWidth = 280f;
        private const float EntryHeight = 40f;

        [MenuItem("SubTerra/UI/Build Prompt-B 33-3 Upgrade Layout Fixes")]
        public static void BuildFromMenu()
        {
            var report = Build();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "prompt-b-33-3-layout.txt"),
                report);
        }

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Prompt-B 33-3 Upgrade Layout Fixes");
            sb.AppendLine(UpdateIntegrationUpgradePanel());
            sb.AppendLine(UpdateSurfaceBasePrefab());
            sb.AppendLine(UpdateSurfaceBaseScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sb.ToString();
        }

        private static string UpdateIntegrationUpgradePanel()
        {
            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                return "FAIL: open integration";
            }

            var upgrade = FindTransform(scene, "UpgradePanel");
            if (upgrade == null)
            {
                RestoreScene(previous, IntegrationScenePath);
                return "SKIP: UpgradePanel missing";
            }

            ApplyUpgradePanelSeventyPercent(upgrade);
            EnsureDeepZoneTab(upgrade.gameObject, showDeepZoneTab: true);
            LayoutUpgradePanelContents(upgrade);
            HideAlwaysVisibleDeepZoneChrome(upgrade);

            var view = upgrade.GetComponent<ProgressionPanelView>();
            if (view != null)
            {
                view.EditorSetHideUpgradeEntryList(false);
                view.EditorSetLevelsOnlySummary(false);
                view.EditorSetHideDeepZoneTab(false);
                var so = new SerializedObject(view);
                SetBool(so, "hideUpgradeEntryList", false);
                SetBool(so, "levelsOnlySummary", false);
                SetBool(so, "hideDeepZoneTab", false);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            RestoreScene(previous, IntegrationScenePath);
            return "UpgradePanel width=70% deep-tab fixed-list purchase-under-detail";
        }

        private static string UpdateSurfaceBasePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SurfaceBasePrefabPath) == null)
            {
                return "SKIP: SurfaceBase prefab missing";
            }

            var root = PrefabUtility.LoadPrefabContents(SurfaceBasePrefabPath);
            try
            {
                ApplySurfaceBaseLevelsOnly(root);
                PrefabUtility.SaveAsPrefabAsset(root, SurfaceBasePrefabPath);
                return "SurfaceBasePrefab levels-only + deep-zone removed";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string UpdateSurfaceBaseScene()
        {
            if (!File.Exists(SurfaceBaseScenePath))
            {
                return "SKIP: SurfaceBase scene missing";
            }

            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(SurfaceBaseScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                return "FAIL: open SurfaceBase";
            }

            var panel = Object.FindFirstObjectByType<SurfaceBaseView>(FindObjectsInactive.Include);
            if (panel != null)
            {
                ApplySurfaceBaseLevelsOnly(panel.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(panel.gameObject);
                EditorUtility.SetDirty(panel.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            RestoreScene(previous, SurfaceBaseScenePath);
            return "SurfaceBase scene levels-only + deep-zone removed";
        }

        /// <summary>장비 업그레이드 창: 가로 70% 중앙.</summary>
        private static void ApplyUpgradePanelSeventyPercent(Transform upgrade)
        {
            var rect = upgrade as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(UpgradeAnchorMinX, UpgradeAnchorMinY);
            rect.anchorMax = new Vector2(UpgradeAnchorMaxX, UpgradeAnchorMaxY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            EditorUtility.SetDirty(rect);
        }

        private static void LayoutUpgradePanelContents(Transform upgrade)
        {
            var panelRoot = upgrade.Find("PanelRoot") ?? upgrade;

            // 상단 탭 바: 5탭이 들어가도록 전체 폭 스트레치.
            PlaceInPanel(
                panelRoot,
                "CategoryTabBar",
                0.5f,
                1f,
                new Vector2(0f, -12f),
                new Vector2(-32f, 42f),
                stretchX: true);

            // 탭 내부 버튼 균등 배치.
            var tabBar = FindChildRecursive(panelRoot, "CategoryTabBar");
            if (tabBar != null)
            {
                LayoutCategoryTabButtons(tabBar, tabCount: UpgradeCategoryRules.TabLabels.Length);
            }

            PlaceInPanel(
                panelRoot,
                "Title",
                0.5f,
                1f,
                new Vector2(0f, -56f),
                new Vector2(-40f, 32f),
                stretchX: true);

            PlaceInPanel(
                panelRoot,
                "UpgradeList",
                0f,
                1f,
                new Vector2(20f, -90f),
                new Vector2(EntryWidth, 32f),
                stretchX: false);

            // 좌측 엔트리: 드릴 탭과 동일한 시작 Y에 카테고리 내 순서로 배치.
            // 런타임 ApplyCategoryFilterToButtons가 탭 전환 시 다시 정렬한다.
            var entries = upgrade.GetComponentsInChildren<ProgressionUpgradeEntryButton>(true)
                .OrderBy(e => e.transform.GetSiblingIndex())
                .ToList();
            for (var i = 0; i < entries.Count; i++)
            {
                var r = entries[i].GetComponent<RectTransform>();
                if (r == null)
                {
                    continue;
                }

                r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0f, 1f);
                r.anchoredPosition = new Vector2(EntryColumnX, EntryStartY - i * EntryRowHeight);
                r.sizeDelta = new Vector2(EntryWidth, EntryHeight);
                entries[i].gameObject.SetActive(true);
                EditorUtility.SetDirty(r);
            }

            // 우측 설명란.
            PlaceRightColumn(panelRoot, "UpgradeDetail", -100f, 170f);
            PlaceRightColumn(panelRoot, "UpgradeResult", -290f, 48f);

            // 심층 문구는 심층 탭 전용 — 기본 숨김, 위치는 우측 설명란 자리.
            var deep = FindChildRecursive(panelRoot, "DeepZone") as RectTransform
                ?? FindChildRecursive(panelRoot, "ProgDeep") as RectTransform
                ?? FindChildRecursive(panelRoot, "DeepZoneText") as RectTransform;
            if (deep != null)
            {
                deep.anchorMin = new Vector2(0.5f, 1f);
                deep.anchorMax = new Vector2(1f, 1f);
                deep.pivot = new Vector2(0.5f, 1f);
                deep.anchoredPosition = new Vector2(-16f, -100f);
                deep.sizeDelta = new Vector2(-32f, 200f);
                deep.gameObject.SetActive(false);
                EditorUtility.SetDirty(deep);
            }

            // 업그레이드 버튼: 우측 설명란·결과 아래, 상하 간격 유지.
            var purchase = panelRoot.Find("PurchaseButton") as RectTransform
                ?? upgrade.Find("PurchaseButton") as RectTransform;
            if (purchase != null)
            {
                purchase.anchorMin = new Vector2(0.55f, 1f);
                purchase.anchorMax = new Vector2(1f, 1f);
                purchase.pivot = new Vector2(0.5f, 1f);
                purchase.anchoredPosition = new Vector2(-20f, -360f);
                purchase.sizeDelta = new Vector2(-40f, 48f);
                purchase.gameObject.SetActive(true);
                var label = purchase.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = "업그레이드";
                    EditorUtility.SetDirty(label);
                }

                EditorUtility.SetDirty(purchase);
            }
        }

        private static void LayoutCategoryTabButtons(Transform tabBar, int tabCount)
        {
            var tabBarRect = tabBar as RectTransform;
            if (tabBarRect != null)
            {
                tabBarRect.anchorMin = new Vector2(0f, 1f);
                tabBarRect.anchorMax = new Vector2(1f, 1f);
                tabBarRect.pivot = new Vector2(0.5f, 1f);
                EditorUtility.SetDirty(tabBarRect);
            }

            // 균등 분배: 각 탭 너비 = (1/N), 간격 약간.
            for (var i = 0; i < tabCount; i++)
            {
                var name = "CategoryTab_" + i;
                var child = tabBar.Find(name) as RectTransform;
                if (child == null)
                {
                    continue;
                }

                var minX = i / (float)tabCount;
                var maxX = (i + 1) / (float)tabCount;
                child.anchorMin = new Vector2(minX, 0f);
                child.anchorMax = new Vector2(maxX, 1f);
                child.pivot = new Vector2(0.5f, 0.5f);
                child.offsetMin = new Vector2(3f, 2f);
                child.offsetMax = new Vector2(-3f, -2f);
                child.anchoredPosition = Vector2.zero;
                child.sizeDelta = Vector2.zero;
                EditorUtility.SetDirty(child);
            }
        }

        /// <summary>
        /// 심층 구역 탭(5번째)을 보장하고 View 직렬화 배열에 연결한다.
        /// </summary>
        private static void EnsureDeepZoneTab(GameObject upgradeRoot, bool showDeepZoneTab)
        {
            var view = upgradeRoot.GetComponent<ProgressionPanelView>();
            if (view == null)
            {
                return;
            }

            var parent = upgradeRoot.transform;
            var tabBar = parent.Find("CategoryTabBar");
            if (tabBar == null)
            {
                var panelRoot = parent.Find("PanelRoot");
                if (panelRoot != null)
                {
                    tabBar = panelRoot.Find("CategoryTabBar");
                }
            }

            if (tabBar == null)
            {
                var go = new GameObject("CategoryTabBar", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                tabBar = go.transform;
            }

            var labels = UpgradeCategoryRules.TabLabels;
            var buttons = new Button[labels.Length];
            var labelTexts = new TMP_Text[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                var name = "CategoryTab_" + i;
                var existing = tabBar.Find(name);
                Button button;
                if (existing == null)
                {
                    button = CreateButton(
                        tabBar,
                        name,
                        Vector2.zero,
                        new Vector2(120f, 36f),
                        labels[i]);
                }
                else
                {
                    button = existing.GetComponent<Button>();
                    var label = existing.GetComponentInChildren<TMP_Text>(true);
                    if (label != null)
                    {
                        label.text = labels[i];
                    }

                    existing.gameObject.SetActive(true);
                }

                while (button.onClick.GetPersistentEventCount() > 0)
                {
                    UnityEventTools.RemovePersistentListener(button.onClick, 0);
                }

                UnityEventTools.AddIntPersistentListener(
                    button.onClick,
                    view.SelectCategoryTab,
                    i);

                buttons[i] = button;
                labelTexts[i] = button.GetComponentInChildren<TMP_Text>(true);

                if (!showDeepZoneTab && i == (int)UpgradeCategory.DeepZone)
                {
                    button.gameObject.SetActive(false);
                }
            }

            // DeepZone 텍스트 참조 확보.
            var deepText = FindDeepZoneText(parent);
            if (deepText == null)
            {
                deepText = CreateText(
                    parent,
                    "DeepZone",
                    new Vector2(0f, 0f),
                    new Vector2(300f, 160f),
                    16f,
                    string.Empty);
                deepText.gameObject.SetActive(false);
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
                tabLabels.arraySize = labelTexts.Length;
                for (var i = 0; i < labelTexts.Length; i++)
                {
                    tabLabels.GetArrayElementAtIndex(i).objectReferenceValue = labelTexts[i];
                }
            }

            var deepProp = so.FindProperty("deepZoneText");
            if (deepProp != null && deepText != null)
            {
                deepProp.objectReferenceValue = deepText;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);

            // 엔트리 버튼이 부족하면 카탈로그 기준으로 보강.
            EnsureUpgradeEntryButtons(parent, view);
        }

        private static void EnsureUpgradeEntryButtons(Transform parent, ProgressionPanelView view)
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
                WireUpgradeButtons(view, existing);
                return;
            }

            for (var i = 0; i < existing.Count; i++)
            {
                if (existing[i] != null)
                {
                    Object.DestroyImmediate(existing[i].gameObject);
                }
            }

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
                    new Vector2(EntryColumnX, EntryStartY - row * EntryRowHeight),
                    new Vector2(EntryWidth, EntryHeight),
                    display);
                var entry = button.gameObject.AddComponent<ProgressionUpgradeEntryButton>();
                entry.EditorSet(upgrade.Id, binder, button.GetComponentInChildren<TMP_Text>());
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
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

        /// <summary>
        /// 업그레이드 창에서 탭과 무관하게 항상 보이던 심층 문구를 숨긴다.
        /// 심층 탭 선택 시에만 View가 다시 켠다.
        /// </summary>
        private static void HideAlwaysVisibleDeepZoneChrome(Transform upgrade)
        {
            foreach (var name in new[] { "DeepZone", "ProgDeep", "DeepZoneText" })
            {
                var t = FindChildRecursive(upgrade, name);
                if (t != null && t.GetComponent<TMP_Text>() != null)
                {
                    // deepZoneText 전용 오브젝트만 기본 숨김(심층 탭에서 켬).
                    t.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Surface Base:
        /// - 심층 구역 업그레이드 텍스트·버튼 제거(심층은 업그레이드 창 탭 전용)
        /// - 하단은 7종 장비 레벨 요약만(필요 자원·변화치 없음)
        /// </summary>
        private static void ApplySurfaceBaseLevelsOnly(GameObject root)
        {
            var host = root.transform.Find("SurfaceBaseContent") ?? root.transform;

            PlaceCentered(host, "GoalsText", 220f, 720f, 40f);
            PlaceCentered(host, "EnergyText", 175f, 720f, 36f);
            PlaceCentered(host, "DeepZoneText", 130f, 720f, 36f);
            PlaceCentered(host, "RecentRunText", 90f, 720f, 36f);
            PlaceCentered(host, "MessageText", 50f, 720f, 32f);

            var economy = host.Find("EconomyPanel");
            if (economy != null)
            {
                PlaceCentered(economy, "EcoStatus", 10f, 720f, 36f);
                PlaceCentered(economy, "EcoDetail", -25f, 720f, 32f);
            }

            var progression = root.GetComponentsInChildren<ProgressionPanelView>(true)
                .FirstOrDefault();
            if (progression == null)
            {
                return;
            }

            var panel = progression.GetComponent<RectTransform>();
            if (panel != null)
            {
                panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
                panel.pivot = new Vector2(0.5f, 0.5f);
                panel.anchoredPosition = new Vector2(0f, -200f);
                panel.sizeDelta = new Vector2(760f, 340f);
                EditorUtility.SetDirty(panel);
            }

            // 탭·구매·엔트리 숨김 — 하단 레벨 요약만.
            var tabBar = progression.transform.Find("CategoryTabBar");
            if (tabBar != null)
            {
                tabBar.gameObject.SetActive(false);
            }

            EnsureDeepZoneTab(progression.gameObject, showDeepZoneTab: false);

            var purchase = progression.transform.Find("PurchaseButton");
            if (purchase != null)
            {
                purchase.gameObject.SetActive(false);
            }

            foreach (var entry in progression.GetComponentsInChildren<ProgressionUpgradeEntryButton>(true))
            {
                entry.gameObject.SetActive(false);
            }

            // Progression 내부 심층 업그레이드 문구 제거.
            foreach (var name in new[] { "ProgDeep", "DeepZone" })
            {
                var t = FindChildRecursive(progression.transform, name);
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                }
            }

            // 레벨 요약: 드릴 속도 ~ 가스 저항 레벨만.
            PlaceCentered(progression.transform, "UpgradeList", 20f, 700f, 240f);
            var list = FindChildRecursive(progression.transform, "UpgradeList");
            if (list != null)
            {
                list.gameObject.SetActive(true);
                var tmp = list.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.alignment = TextAlignmentOptions.TopLeft;
                    tmp.fontSize = 18f;
                    tmp.textWrappingMode = TextWrappingModes.Normal;
                    EditorUtility.SetDirty(tmp);
                }
            }

            var detail = FindChildRecursive(progression.transform, "UpgradeDetail");
            if (detail != null)
            {
                detail.gameObject.SetActive(false);
            }

            var result = FindChildRecursive(progression.transform, "UpgradeResult");
            if (result != null)
            {
                result.gameObject.SetActive(false);
            }

            progression.EditorSetHideUpgradeEntryList(true);
            progression.EditorSetLevelsOnlySummary(true);
            progression.EditorSetHideDeepZoneTab(true);

            var so = new SerializedObject(progression);
            SetBool(so, "hideUpgradeEntryList", true);
            SetBool(so, "levelsOnlySummary", true);
            SetBool(so, "hideDeepZoneTab", true);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(progression);
        }

        private static void PlaceRightColumn(
            Transform parent,
            string name,
            float y,
            float height)
        {
            var tf = FindChildRecursive(parent, name) as RectTransform;
            if (tf == null)
            {
                return;
            }

            tf.anchorMin = new Vector2(0.5f, 1f);
            tf.anchorMax = new Vector2(1f, 1f);
            tf.pivot = new Vector2(0.5f, 1f);
            tf.anchoredPosition = new Vector2(-16f, y);
            tf.sizeDelta = new Vector2(-32f, height);
            var tmp = tf.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.textWrappingMode = TextWrappingModes.Normal;
                tmp.overflowMode = TextOverflowModes.Overflow;
            }

            EditorUtility.SetDirty(tf);
        }

        private static void PlaceInPanel(
            Transform parent,
            string name,
            float anchorX,
            float anchorY,
            Vector2 pos,
            Vector2 size,
            bool stretchX)
        {
            var tf = FindChildRecursive(parent, name) as RectTransform;
            if (tf == null)
            {
                return;
            }

            if (stretchX)
            {
                tf.anchorMin = new Vector2(0f, anchorY);
                tf.anchorMax = new Vector2(1f, anchorY);
                tf.pivot = new Vector2(0.5f, 1f);
                tf.anchoredPosition = pos;
                tf.sizeDelta = new Vector2(size.x, size.y);
            }
            else
            {
                tf.anchorMin = tf.anchorMax = new Vector2(anchorX, anchorY);
                tf.pivot = new Vector2(0f, 1f);
                tf.anchoredPosition = pos;
                tf.sizeDelta = size;
            }

            EditorUtility.SetDirty(tf);
        }

        private static void PlaceCentered(
            Transform parent,
            string childName,
            float y,
            float width,
            float height)
        {
            var child = FindChildRecursive(parent, childName) as RectTransform;
            if (child == null)
            {
                return;
            }

            child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
            child.pivot = new Vector2(0.5f, 0.5f);
            child.anchoredPosition = new Vector2(0f, y);
            child.sizeDelta = new Vector2(width, height);
            var tmp = child.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.Normal;
            }

            EditorUtility.SetDirty(child);
        }

        private static TMP_Text FindDeepZoneText(Transform parent)
        {
            foreach (var name in new[] { "DeepZone", "ProgDeep", "DeepZoneText" })
            {
                var t = FindChildRecursive(parent, name);
                if (t != null)
                {
                    var tmp = t.GetComponent<TMP_Text>();
                    if (tmp != null)
                    {
                        return tmp;
                    }
                }
            }

            return null;
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
            text.fontSize = 15f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            var lr = text.rectTransform;
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            float fontSize,
            string content)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            var font = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (font != null)
            {
                text.font = font;
            }

            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void SetBool(SerializedObject so, string property, bool value)
        {
            var prop = so.FindProperty(property);
            if (prop != null)
            {
                prop.boolValue = value;
            }
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            var direct = parent.Find(name);
            if (direct != null)
            {
                return direct;
            }

            foreach (Transform child in parent)
            {
                var found = FindChildRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
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

        private static void RestoreScene(string previous, string current)
        {
            if (!string.IsNullOrEmpty(previous)
                && previous != current
                && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }
    }
}
