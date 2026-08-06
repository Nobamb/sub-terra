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
    /// prompt-B 33-4 및 후속 폴리시:
    /// - Surface Base 레벨 요약 중복 제거, Title(Surface Base) 제거
    /// - 지하탐사시작 버튼 뒤 호출 메시지 비가시
    /// - 업그레이드 창 너비 90%, X 버튼과 탭 바 간격
    /// - 하위 탭(엔트리) 클릭 선택 보장·배선 재연결
    /// - 심층 구역 텍스트 단일 영역(detail)으로 병합
    /// </summary>
    public static class PromptB33_4LayoutBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string SurfaceBaseScenePath =
            "Assets/_Project/Scenes/App/SurfaceBase.unity";
        private const string SurfaceBasePrefabPath =
            "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        private const string CatalogPath =
            "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        // 거의 전체 화면(96% 가로) — X/탭 간격과 좌측 하위 탭 클릭 영역 확보
        private const float UpgradeAnchorMinX = 0.02f;
        private const float UpgradeAnchorMaxX = 0.98f;
        private const float UpgradeAnchorMinY = 0.10f;
        private const float UpgradeAnchorMaxY = 0.90f;
        // 탭 바 우측을 X 버튼 영역보다 넉넉히 비운다.
        private const float TabBarRightInset = 88f;
        private const int UpgradeModalSortOrder = 450;

        private const float EntryStartY = -120f;
        private const float EntryRowHeight = 50f;
        private const float EntryColumnX = 24f;
        private const float EntryWidth = 360f;
        private const float EntryHeight = 44f;

        [MenuItem("SubTerra/UI/Build Prompt-B 33-4 Surface Upgrade Fixes")]
        public static void BuildFromMenu()
        {
            var report = Build();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "prompt-b-33-4-layout.txt"),
                report);
        }

        /// <summary>
        /// MVP2-fix 전체 체인: 33-2 설정/본문 → 33-3 레벨요약 → 33-4 중복/메시지 정리.
        /// SurfaceBase prefab·씬 + Integration UpgradePanel 까지 한 번에 맞춘다.
        /// </summary>
        [MenuItem("SubTerra/UI/Rebuild SurfaceBase From MVP2-fix Chain")]
        public static void RebuildSurfaceBaseFromMvp2FixChainMenu()
        {
            var report = RebuildSurfaceBaseFromMvp2FixChain();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "surfacebase-mvp2fix-chain.txt"),
                report);
        }

        public static string RebuildSurfaceBaseFromMvp2FixChain()
        {
            var sb = new StringBuilder();
            sb.AppendLine("SurfaceBase MVP2-fix rebuild chain (33-2 → 33-3 → 33-4)");

            // 33-2: 크기/단일열 + 설정 패널(프레임 드롭다운 포함)
            sb.AppendLine(RebuildSurfaceBaseWithSettings());
            // 33-3: 하단 레벨 요약만
            sb.AppendLine(PromptB33_3LayoutBuilder.BuildSurfaceBaseOnly());
            // 33-4: 타이틀 제거·메시지 위치 + Integration Upgrade 96%
            sb.AppendLine(Build());
            return sb.ToString();
        }

        /// <summary>SurfaceBase prefab/씬에 33-2 설정·본문 레이아웃만 재적용.</summary>
        public static string RebuildSurfaceBaseWithSettings()
        {
            var sb = new StringBuilder();
            sb.AppendLine(PromptB33_2LayoutBuilder.BuildSurfaceBaseOnly());
            return sb.ToString().TrimEnd();
        }

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Prompt-B 33-4 Surface / Upgrade Fixes");
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

            ApplyUpgradePanelWide(upgrade);
            EnsureModalCanvas(upgrade);
            EnsureCloseButtonHitArea(upgrade);
            EnsureCategoryTabsAndEntries(upgrade.gameObject);
            LayoutUpgradeContents(upgrade);
            MergeDeepZoneIntoDetailOnly(upgrade);

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
                // panelRoot를 자기 자신으로 고정해 BringToFront/Canvas가 올바르게 붙게 한다.
                var panelRootProp = so.FindProperty("panelRoot");
                if (panelRootProp != null)
                {
                    panelRootProp.objectReferenceValue = upgrade.gameObject;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            RestoreScene(previous, IntegrationScenePath);
            return "UpgradePanel width=96% modal-canvas entries-pointer-click deepzone-merged";
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
                ApplySurfaceBaseDuplicateAndMessageFixes(root);
                PrefabUtility.SaveAsPrefabAsset(root, SurfaceBasePrefabPath);
                return "SurfaceBasePrefab single-level-summary + message-below-explore";
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
                ApplySurfaceBaseDuplicateAndMessageFixes(panel.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(panel.gameObject);
                EditorUtility.SetDirty(panel.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            RestoreScene(previous, SurfaceBaseScenePath);
            return "SurfaceBase scene single-level-summary + message-below-explore";
        }

        private static void ApplyUpgradePanelWide(Transform upgrade)
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
            upgrade.SetAsLastSibling();
            EditorUtility.SetDirty(rect);
        }

        /// <summary>다른 HUD에 묻히지 않도록 전용 Canvas + GraphicRaycaster를 붙인다.</summary>
        private static void EnsureModalCanvas(Transform upgrade)
        {
            var canvas = upgrade.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = upgrade.gameObject.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = UpgradeModalSortOrder;

            if (upgrade.GetComponent<GraphicRaycaster>() == null)
            {
                upgrade.gameObject.AddComponent<GraphicRaycaster>();
            }

            var image = upgrade.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                var c = image.color;
                if (c.a < 0.9f)
                {
                    c.a = 0.97f;
                    image.color = c;
                }
            }

            EditorUtility.SetDirty(upgrade.gameObject);
        }

        /// <summary>심층 전용 텍스트 오브젝트는 끄고 상세 카드만 쓰도록 정리한다.</summary>
        private static void MergeDeepZoneIntoDetailOnly(Transform upgrade)
        {
            foreach (var name in new[] { "DeepZone", "ProgDeep", "DeepZoneText" })
            {
                var t = FindChildRecursive(upgrade, name);
                if (t == null)
                {
                    continue;
                }

                t.gameObject.SetActive(false);
                var tmp = t.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = string.Empty;
                    EditorUtility.SetDirty(tmp);
                }

                EditorUtility.SetDirty(t);
            }

            var detail = FindChildRecursive(upgrade, "UpgradeDetail") as RectTransform;
            if (detail != null)
            {
                detail.gameObject.SetActive(true);
                detail.anchorMin = new Vector2(0.5f, 1f);
                detail.anchorMax = new Vector2(1f, 1f);
                detail.pivot = new Vector2(0.5f, 1f);
                detail.anchoredPosition = new Vector2(-16f, -100f);
                detail.sizeDelta = new Vector2(-32f, 240f);
                var tmp = detail.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.raycastTarget = false;
                    tmp.textWrappingMode = TextWrappingModes.Normal;
                    EditorUtility.SetDirty(tmp);
                }

                EditorUtility.SetDirty(detail);
            }
        }

        /// <summary>X 버튼을 우측 상단에 크게 두어 창 가장자리에서도 쉽게 누르게 한다.</summary>
        private static void EnsureCloseButtonHitArea(Transform upgrade)
        {
            var close = FindChildRecursive(upgrade, "CloseButton") as RectTransform;
            if (close == null)
            {
                return;
            }

            close.anchorMin = close.anchorMax = new Vector2(1f, 1f);
            close.pivot = new Vector2(1f, 1f);
            close.anchoredPosition = new Vector2(-14f, -14f);
            close.sizeDelta = new Vector2(56f, 56f);
            close.SetAsLastSibling();

            var image = close.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                image.color = new Color(0.18f, 0.22f, 0.28f, 1f);
            }

            var label = close.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "×";
                label.fontSize = 28f;
                label.raycastTarget = false;
                EditorUtility.SetDirty(label);
            }

            EditorUtility.SetDirty(close);
        }

        private static void LayoutUpgradeContents(Transform upgrade)
        {
            var panelRoot = upgrade.Find("PanelRoot") ?? upgrade;

            // 탭 바: 좌측 여백 + 우측은 X 버튼 영역만큼 inset.
            var tabBar = FindChildRecursive(panelRoot, "CategoryTabBar") as RectTransform;
            if (tabBar != null)
            {
                tabBar.anchorMin = new Vector2(0f, 1f);
                tabBar.anchorMax = new Vector2(1f, 1f);
                tabBar.pivot = new Vector2(0.5f, 1f);
                tabBar.anchoredPosition = new Vector2(0f, -12f);
                tabBar.sizeDelta = new Vector2(0f, 42f);
                tabBar.offsetMin = new Vector2(16f, tabBar.offsetMin.y);
                tabBar.offsetMax = new Vector2(-TabBarRightInset, tabBar.offsetMax.y);
                LayoutCategoryTabButtons(tabBar, UpgradeCategoryRules.TabLabels.Length);
                EditorUtility.SetDirty(tabBar);
            }

            // 제목도 X 버튼과 겹치지 않게 우측 여백.
            PlaceInPanel(
                panelRoot,
                "Title",
                0.5f,
                1f,
                new Vector2(0f, -56f),
                new Vector2(-(TabBarRightInset + 16f), 32f),
                stretchX: true);

            // 엔트리 버튼이 선택 UI이므로 텍스트 목록은 숨김.
            var list = FindChildRecursive(panelRoot, "UpgradeList");
            if (list != null)
            {
                list.gameObject.SetActive(false);
            }

            // 좌측 전용 컨테이너 — 다른 텍스트/결과 영역과 겹치지 않게 분리.
            var entryRoot = EnsureEntryListRoot(panelRoot);
            // 카탈로그 생성 순서를 유지한다(이름 정렬 금지).
            var entries = entryRoot.GetComponentsInChildren<ProgressionUpgradeEntryButton>(true)
                .OrderBy(e => e.transform.GetSiblingIndex())
                .ToList();
            if (entries.Count == 0)
            {
                entries = upgrade.GetComponentsInChildren<ProgressionUpgradeEntryButton>(true)
                    .OrderBy(e => e.transform.GetSiblingIndex())
                    .ToList();
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                entry.transform.SetParent(entryRoot, false);
                var r = entry.GetComponent<RectTransform>();
                if (r == null)
                {
                    continue;
                }

                r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0f, 1f);
                r.anchoredPosition = new Vector2(0f, -i * EntryRowHeight);
                r.sizeDelta = new Vector2(EntryWidth, EntryHeight);
                r.localScale = Vector3.one;
                entry.gameObject.SetActive(true);
                entry.EnsureInteractable();
                EditorUtility.SetDirty(r);
            }

            entryRoot.SetAsLastSibling();
            var closeTf = FindChildRecursive(upgrade, "CloseButton");
            if (closeTf != null)
            {
                closeTf.SetAsLastSibling();
            }

            // 설명+필요재료를 넣을 수 있게 상세 카드 높이 확대.
            PlaceRightColumn(panelRoot, "UpgradeDetail", -100f, 260f);
            PlaceRightColumn(panelRoot, "UpgradeResult", -380f, 48f);

            var deep = FindChildRecursive(panelRoot, "DeepZone") as RectTransform
                ?? FindChildRecursive(panelRoot, "ProgDeep") as RectTransform
                ?? FindChildRecursive(panelRoot, "DeepZoneText") as RectTransform;
            if (deep != null)
            {
                deep.anchorMin = new Vector2(0.5f, 1f);
                deep.anchorMax = new Vector2(1f, 1f);
                deep.pivot = new Vector2(0.5f, 1f);
                deep.anchoredPosition = new Vector2(-16f, -100f);
                deep.sizeDelta = new Vector2(-32f, 220f);
                deep.gameObject.SetActive(false);
                EditorUtility.SetDirty(deep);
            }

            var purchase = panelRoot.Find("PurchaseButton") as RectTransform
                ?? upgrade.Find("PurchaseButton") as RectTransform;
            if (purchase != null)
            {
                purchase.anchorMin = new Vector2(0.55f, 1f);
                purchase.anchorMax = new Vector2(1f, 1f);
                purchase.pivot = new Vector2(0.5f, 1f);
                purchase.anchoredPosition = new Vector2(-20f, -410f);
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

        private static void EnsureCategoryTabsAndEntries(GameObject upgradeRoot)
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

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);

            EnsureUpgradeEntryButtons(parent, view);
        }

        private static Transform EnsureEntryListRoot(Transform panelRoot)
        {
            var existing = panelRoot.Find("EntryListRoot") as RectTransform;
            if (existing != null)
            {
                existing.anchorMin = new Vector2(0f, 0f);
                existing.anchorMax = new Vector2(0f, 1f);
                existing.pivot = new Vector2(0f, 1f);
                existing.anchoredPosition = new Vector2(EntryColumnX, EntryStartY);
                existing.sizeDelta = new Vector2(EntryWidth + 8f, 0f);
                existing.offsetMin = new Vector2(EntryColumnX, 24f);
                existing.offsetMax = new Vector2(EntryColumnX + EntryWidth + 8f, EntryStartY);
                // top-left fixed column
                existing.anchorMin = new Vector2(0f, 1f);
                existing.anchorMax = new Vector2(0f, 1f);
                existing.pivot = new Vector2(0f, 1f);
                existing.anchoredPosition = new Vector2(EntryColumnX, EntryStartY);
                existing.sizeDelta = new Vector2(EntryWidth + 8f, 420f);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var go = new GameObject("EntryListRoot", typeof(RectTransform));
            go.transform.SetParent(panelRoot, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(EntryColumnX, EntryStartY);
            rect.sizeDelta = new Vector2(EntryWidth + 8f, 420f);
            return rect;
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

            var entryRoot = EnsureEntryListRoot(parent.Find("PanelRoot") ?? parent);

            // 기존 엔트리를 제거하고 카탈로그 기준으로 재생성해 선택 배선을 확실히 한다.
            var existing = parent.GetComponentsInChildren<ProgressionUpgradeEntryButton>(true);
            for (var i = 0; i < existing.Length; i++)
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
                    entryRoot,
                    "UpgradeEntry_" + upgrade.Id.Replace('.', '_'),
                    new Vector2(0f, -row * EntryRowHeight),
                    new Vector2(EntryWidth, EntryHeight),
                    display);
                var entry = button.gameObject.AddComponent<ProgressionUpgradeEntryButton>();
                entry.EditorSet(upgrade.Id, binder, button.GetComponentInChildren<TMP_Text>());
                entry.EnsureInteractable();
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(0f, -row * EntryRowHeight);
                rect.sizeDelta = new Vector2(EntryWidth, EntryHeight);
                // 클릭 영역이 라벨보다 항상 앞에 오도록
                button.transform.SetAsLastSibling();
                entries.Add(entry);
                row++;
            }

            entryRoot.SetAsLastSibling();

            var so = new SerializedObject(view);
            var prop = so.FindProperty("upgradeButtons");
            if (prop != null)
            {
                prop.arraySize = entries.Count;
                for (var i = 0; i < entries.Count; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(view);
        }

        private static void ApplySurfaceBaseDuplicateAndMessageFixes(GameObject root)
        {
            var host = root.transform.Find("SurfaceBaseContent") ?? root.transform;

            // 설정/종료 사이·상단에 보이는 "Surface Base" 타이틀 제거.
            // SettingsPanel 내부 제목은 유지한다.
            foreach (var title in host.GetComponentsInChildren<TMP_Text>(true))
            {
                if (title == null)
                {
                    continue;
                }

                var inSettings = false;
                var p = title.transform;
                while (p != null)
                {
                    if (p.name == "SettingsPanel" || p.name == "settingsRoot")
                    {
                        inSettings = true;
                        break;
                    }

                    p = p.parent;
                }

                if (inSettings)
                {
                    continue;
                }

                var text = (title.text ?? string.Empty).Trim();
                var isSurfaceTitle = title.name == "Title"
                    || text.Equals("Surface Base", System.StringComparison.OrdinalIgnoreCase);
                if (!isSurfaceTitle)
                {
                    continue;
                }

                title.text = string.Empty;
                title.gameObject.SetActive(false);
                EditorUtility.SetDirty(title);
            }

            // 탐사 시작 메시지는 버튼 아래 분리 배치(겹침 방지).
            var explore = FindChildRecursive(host, "ExploreButton") as RectTransform;
            var message = FindChildRecursive(host, "MessageText") as RectTransform;
            if (message != null)
            {
                message.anchorMin = message.anchorMax = new Vector2(0.5f, 0.5f);
                message.pivot = new Vector2(0.5f, 0.5f);
                // ExploreButton(y≈35, h≈73) 아래로 내려 버튼 뒤에서 비치지 않게 한다.
                var messageY = explore != null
                    ? explore.anchoredPosition.y - explore.sizeDelta.y * 0.5f - 28f
                    : -20f;
                message.anchoredPosition = new Vector2(0f, messageY);
                message.sizeDelta = new Vector2(720f, 36f);
                var tmp = message.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = string.Empty;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.raycastTarget = false;
                    EditorUtility.SetDirty(tmp);
                }

                EditorUtility.SetDirty(message);
            }

            if (explore != null)
            {
                var image = explore.GetComponent<Image>();
                if (image != null)
                {
                    // 버튼이 반투명이면 뒤 텍스트가 비치므로 불투명 유지.
                    var c = image.color;
                    c.a = 1f;
                    image.color = c;
                    EditorUtility.SetDirty(image);
                }
            }

            var progression = root.GetComponentsInChildren<ProgressionPanelView>(true)
                .FirstOrDefault();
            if (progression == null)
            {
                return;
            }

            // 좌측 중복 상세 카드 비활성, 중앙 레벨 요약만 유지.
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

            foreach (var name in new[] { "ProgDeep", "DeepZone", "DeepZoneText" })
            {
                var t = FindChildRecursive(progression.transform, name);
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                }
            }

            var tabBar = progression.transform.Find("CategoryTabBar");
            if (tabBar != null)
            {
                tabBar.gameObject.SetActive(false);
            }

            var purchase = progression.transform.Find("PurchaseButton");
            if (purchase != null)
            {
                purchase.gameObject.SetActive(false);
            }

            foreach (var entry in progression.GetComponentsInChildren<ProgressionUpgradeEntryButton>(true))
            {
                entry.gameObject.SetActive(false);
            }

            PlaceCentered(progression.transform, "UpgradeList", 20f, 700f, 240f);
            var list = FindChildRecursive(progression.transform, "UpgradeList");
            if (list != null)
            {
                list.gameObject.SetActive(true);
                var tmp = list.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.fontSize = 18f;
                    tmp.textWrappingMode = TextWrappingModes.Normal;
                    tmp.raycastTarget = false;
                    EditorUtility.SetDirty(tmp);
                }
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

        private static void PlaceRightColumn(Transform parent, string name, float y, float height)
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
                tmp.raycastTarget = false;
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
