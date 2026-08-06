using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SubTerra.App.UI;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Inventory;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Shared.Localization;
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
    /// prompt-B 33-1:
    /// - 화물(I) → 인벤토리(I) 버튼 텍스트
    /// - 중복 InventoryPanel 제거, 시작 시 숨김, I/버튼/X 토글
    /// - Surface Base 업그레이드 좌(목록 50%)/우(상세 50%) 배치
    /// - 설정: 화면 진동 억제 좌텍스트·우체크 중앙 정렬, 언어 드롭다운
    /// </summary>
    public static class PromptB33_1LayoutBuilder
    {
        public const string IntegrationScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string SurfaceBaseScenePath =
            "Assets/_Project/Scenes/App/SurfaceBase.unity";
        public const string MainMenuScenePath =
            "Assets/_Project/Scenes/App/MainMenu.unity";
        private const string SurfaceBasePrefabPath =
            "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        private const string MainMenuPrefabPath =
            "Assets/_Project/Prefabs/UI/MainMenuPanel.prefab";

        [MenuItem("SubTerra/UI/Build Prompt-B 33-1 Inventory Surface Settings Fixes")]
        public static void BuildFromMenu()
        {
            var report = Build();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "prompt-b-33-1-layout.txt"),
                report);
        }

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Prompt-B 33-1 Fixes");
            sb.AppendLine(UpdateIntegrationScene());
            sb.AppendLine(UpdateSurfaceBasePrefab());
            sb.AppendLine(UpdateSurfaceBaseScene());
            sb.AppendLine(UpdateMainMenuPrefab());
            sb.AppendLine(UpdateMainMenuScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sb.ToString();
        }

        private static string UpdateIntegrationScene()
        {
            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(IntegrationScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                return "FAIL: open integration";
            }

            var canvas = FindInScene<Canvas>(scene, "HUDCanvas");
            if (canvas == null)
            {
                return "FAIL: HUDCanvas missing";
            }

            // 1) 중복 InventoryPanel 제거: PanelLayout 아래 정식 1개만 유지.
            var allInventory = FindAllTransforms(scene, "InventoryPanel");
            GameObject kept = null;
            var removed = 0;
            foreach (var inv in allInventory)
            {
                if (inv == null)
                {
                    continue;
                }

                // PanelLayout 하위를 우선 보존.
                var underLayout = inv.parent != null && inv.parent.name == "PanelLayout";
                if (kept == null && underLayout)
                {
                    kept = inv.gameObject;
                    continue;
                }

                if (kept == null)
                {
                    kept = inv.gameObject;
                    continue;
                }

                // 나머지(특히 HUDCanvas 직계)는 제거 — 상시 표시 버그 원인.
                Object.DestroyImmediate(inv.gameObject);
                removed++;
            }

            if (kept == null)
            {
                // 없으면 생성 요청 없이 경고만.
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                return "WARN: no InventoryPanel found, removed=" + removed;
            }

            // 루트는 활성 유지, PanelRoot·X는 숨김(시작 시 닫힘).
            kept.SetActive(true);
            var invView = kept.GetComponent<InventoryPanelView>();
            if (invView != null)
            {
                invView.SetVisible(false);
            }
            else
            {
                var panelRoot = kept.transform.Find("PanelRoot");
                if (panelRoot != null)
                {
                    panelRoot.gameObject.SetActive(false);
                }
            }

            Button inventoryX = null;
            EnsureXCloseButton(kept.transform, out inventoryX);
            if (invView != null)
            {
                var so = new SerializedObject(invView);
                so.FindProperty("closeButton").objectReferenceValue = inventoryX;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(invView);
                invView.SetVisible(false);
            }

            // 2) 단축키 바: 인벤토리(I) 텍스트 + chrome 토글.
            var chrome = canvas.GetComponent<HudPanelChromeController>();
            if (chrome == null)
            {
                chrome = canvas.gameObject.AddComponent<HudPanelChromeController>();
            }

            Button inventoryShortcut = null;
            var bar = FindTransform(scene, "PanelShortcutBar");
            if (bar != null)
            {
                foreach (var button in bar.GetComponentsInChildren<Button>(true))
                {
                    while (button.onClick.GetPersistentEventCount() > 0)
                    {
                        UnityEventTools.RemovePersistentListener(button.onClick, 0);
                    }

                    var label = button.GetComponentInChildren<TMP_Text>(true);
                    var text = label != null ? label.text : button.name;
                    if (text.Contains("시설") || text.Contains("[B]") || text.Contains("(B)"))
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
                        if (label != null)
                        {
                            label.text = "인벤토리(I)";
                            EditorUtility.SetDirty(label);
                        }

                        UnityEventTools.AddPersistentListener(
                            button.onClick,
                            chrome.ToggleInventoryPanel);
                        inventoryShortcut = button;
                    }
                    else if (text.Contains("가이드") || text.Contains("[G]") || text.Contains("(G)"))
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

            // 3) Chrome 배선 — 단일 InventoryPanel만.
            var building = FindTransform(scene, "BuildingPanel")
                ?? FindTransform(scene, "BuildingMenu");
            var digger = FindTransform(scene, "DroneDialoguePanel")
                ?? FindTransform(scene, "DiggerBotPanel");
            var guide = FindTransform(scene, "GameGuidePanel");
            var openDigger = canvas.transform.Find("OpenDiggerBotButton")
                ?.GetComponent<Button>();

            Button buildingX = null;
            if (building != null)
            {
                EnsureXCloseButton(building, out buildingX);
            }

            var diggerClose = digger != null
                ? digger.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(b => b.name == "CloseButton")
                : null;
            var guideView = guide != null ? guide.GetComponent<GameGuidePanelView>() : null;
            var guideClose = guideView != null ? guideView.CloseButton : null;

            var chromeSo = new SerializedObject(chrome);
            chromeSo.FindProperty("buildingMenuView").objectReferenceValue =
                building != null
                    ? building.GetComponent<SubTerra.App.UI.Building.BuildingMenuView>()
                    : null;
            chromeSo.FindProperty("buildingMenuBinder").objectReferenceValue =
                building != null
                    ? building.GetComponent<SubTerra.App.UI.Building.BuildingMenuBinder>()
                    : null;
            chromeSo.FindProperty("buildingMenuRoot").objectReferenceValue =
                building != null ? building.gameObject : null;
            chromeSo.FindProperty("buildingCloseButton").objectReferenceValue = buildingX;
            chromeSo.FindProperty("buildingOpenButton").objectReferenceValue = null;
            chromeSo.FindProperty("diggerBotView").objectReferenceValue =
                digger != null
                    ? digger.GetComponent<SubTerra.App.UI.Drone.DroneDialoguePanelView>()
                    : null;
            chromeSo.FindProperty("diggerBotRoot").objectReferenceValue =
                digger != null ? digger.gameObject : null;
            chromeSo.FindProperty("diggerCloseButton").objectReferenceValue = diggerClose;
            chromeSo.FindProperty("diggerOpenButton").objectReferenceValue = openDigger;
            chromeSo.FindProperty("gameGuideView").objectReferenceValue = guideView;
            chromeSo.FindProperty("gameGuideRoot").objectReferenceValue =
                guide != null ? guide.gameObject : null;
            chromeSo.FindProperty("gameGuideCloseButton").objectReferenceValue = guideClose;
            chromeSo.FindProperty("gameGuideOpenButton").objectReferenceValue = null;
            chromeSo.FindProperty("inventoryPanelView").objectReferenceValue = invView;
            chromeSo.FindProperty("inventoryPanelRoot").objectReferenceValue = kept;
            chromeSo.FindProperty("inventoryCloseButton").objectReferenceValue = inventoryX;
            chromeSo.FindProperty("inventoryOpenButton").objectReferenceValue = inventoryShortcut;
            chromeSo.FindProperty("inventoryPanelOpen").boolValue = false;
            chromeSo.FindProperty("gameGuideOpen").boolValue = false;
            chromeSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(chrome);

            // 4) PanelToggleController Inventory 루트 비움(이중 토글 방지).
            var ptcAll = Object.FindFirstObjectByType<PanelToggleController>(
                FindObjectsInactive.Include);
            if (ptcAll != null)
            {
                var ptcSo = new SerializedObject(ptcAll);
                var panels = ptcSo.FindProperty("panels");
                for (var i = 0; i < panels.arraySize; i++)
                {
                    var panel = panels.GetArrayElementAtIndex(i);
                    var id = (RuntimePanelId)panel.FindPropertyRelative("panelId").enumValueIndex;
                    if (id == RuntimePanelId.Inventory
                        || id == RuntimePanelId.Building
                        || id == RuntimePanelId.GameGuide)
                    {
                        panel.FindPropertyRelative("panelRoot").objectReferenceValue = null;
                        panel.FindPropertyRelative("visibleOnStart").boolValue = false;
                    }
                }

                ptcSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(ptcAll);
            }

            // 최종 가시성: 인벤토리 닫힘.
            if (invView != null)
            {
                invView.SetVisible(false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // 저장 직후 현재 씬 기준으로 검증한다.
            var remaining = FindAllTransforms(scene, "InventoryPanel").Count;
            var labels = Object.FindObjectsByType<TMP_Text>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Count(t => t != null && t.text != null && t.text.Contains("인벤토리(I)"));
            var chromeOk = invView != null
                && inventoryX != null
                && inventoryShortcut != null
                && !chrome.IsInventoryPanelOpen;

            RestoreScene(previous, IntegrationScenePath);

            return "Integration removedDup=" + removed
                + " inventoryCount=" + remaining
                + " inventoryLabel=" + labels
                + " chromeWired=" + chromeOk;
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
                ApplySurfaceUpgradeSplitLayout(root);
                ApplySettingsLayout(root, typeof(SurfaceBaseView));
                PrefabUtility.SaveAsPrefabAsset(root, SurfaceBasePrefabPath);
                return "SurfaceBasePrefab upgrade-split + settings dropdown";
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
                ApplySurfaceUpgradeSplitLayout(panel.gameObject);
                ApplySettingsLayout(panel.gameObject, typeof(SurfaceBaseView));
                PrefabUtility.RecordPrefabInstancePropertyModifications(panel.gameObject);
                EditorUtility.SetDirty(panel.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            RestoreScene(previous, SurfaceBaseScenePath);
            return "SurfaceBase scene updated";
        }

        private static string UpdateMainMenuPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath) == null)
            {
                return "SKIP: MainMenu prefab missing";
            }

            var root = PrefabUtility.LoadPrefabContents(MainMenuPrefabPath);
            try
            {
                ApplySettingsLayout(root, typeof(MainMenuView));
                PrefabUtility.SaveAsPrefabAsset(root, MainMenuPrefabPath);
                return "MainMenuPrefab settings dropdown";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string UpdateMainMenuScene()
        {
            if (!File.Exists(MainMenuScenePath))
            {
                return "SKIP: MainMenu scene missing";
            }

            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                return "FAIL: open MainMenu";
            }

            var view = Object.FindFirstObjectByType<MainMenuView>(FindObjectsInactive.Include);
            if (view != null)
            {
                ApplySettingsLayout(view.gameObject, typeof(MainMenuView));
                PrefabUtility.RecordPrefabInstancePropertyModifications(view.gameObject);
                EditorUtility.SetDirty(view.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            RestoreScene(previous, MainMenuScenePath);
            return "MainMenu scene updated";
        }

        /// <summary>
        /// Surface Base ProgressionPanel: 좌 목록 50% / 우 상세 50% / 하단 심층 안내.
        /// </summary>
        private static void ApplySurfaceUpgradeSplitLayout(GameObject root)
        {
            var progression = root.GetComponentsInChildren<ProgressionPanelView>(true)
                .FirstOrDefault();
            if (progression == null)
            {
                return;
            }

            var panel = progression.transform as RectTransform;
            if (panel == null)
            {
                return;
            }

            // 패널 자체를 하단 중앙 넓게.
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(0f, -180f);
            panel.sizeDelta = new Vector2(900f, 360f);
            EditorUtility.SetDirty(panel);

            var halfW = 430f;
            var leftX = -225f;
            var rightX = 225f;

            // 탭: 상단 전체.
            var tabBar = panel.Find("CategoryTabBar") as RectTransform;
            if (tabBar != null)
            {
                tabBar.anchorMin = tabBar.anchorMax = new Vector2(0.5f, 1f);
                tabBar.pivot = new Vector2(0.5f, 1f);
                tabBar.anchoredPosition = new Vector2(0f, -8f);
                tabBar.sizeDelta = new Vector2(860f, 36f);
                EditorUtility.SetDirty(tabBar);
            }

            // 좌측 목록 텍스트(폴백).
            PlaceChild(panel, "UpgradeList", new Vector2(leftX, 80f), new Vector2(halfW, 40f));
            // 좌측 엔트리 버튼들.
            var entries = panel.GetComponentsInChildren<ProgressionUpgradeEntryButton>(true)
                .OrderBy(e => e.transform.GetSiblingIndex())
                .ToList();
            var visibleIndex = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                var rect = entries[i].GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }

                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(leftX, 40f - visibleIndex * 44f);
                rect.sizeDelta = new Vector2(halfW, 40f);
                EditorUtility.SetDirty(rect);
                visibleIndex++;
            }

            // 우측 상세 카드.
            PlaceChild(panel, "UpgradeDetail", new Vector2(rightX, 40f), new Vector2(halfW, 140f));
            PlaceChild(panel, "UpgradeResult", new Vector2(rightX, -90f), new Vector2(halfW, 40f));

            var purchase = panel.Find("PurchaseButton") as RectTransform;
            if (purchase == null)
            {
                // 없으면 생성.
                var btn = CreateButton(
                    panel,
                    "PurchaseButton",
                    new Vector2(rightX, -140f),
                    new Vector2(200f, 42f),
                    "업그레이드");
                purchase = btn.GetComponent<RectTransform>();
                var binder = progression.GetComponent<ProgressionPanelBinder>();
                if (binder != null)
                {
                    while (btn.onClick.GetPersistentEventCount() > 0)
                    {
                        UnityEventTools.RemovePersistentListener(btn.onClick, 0);
                    }

                    UnityEventTools.AddPersistentListener(btn.onClick, binder.PurchaseSelected);
                }

                var so = new SerializedObject(progression);
                so.FindProperty("purchaseButton").objectReferenceValue = btn;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                purchase.anchorMin = purchase.anchorMax = new Vector2(0.5f, 0.5f);
                purchase.pivot = new Vector2(0.5f, 0.5f);
                purchase.anchoredPosition = new Vector2(rightX, -140f);
                purchase.sizeDelta = new Vector2(200f, 42f);
                var label = purchase.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = "업그레이드";
                    EditorUtility.SetDirty(label);
                }

                EditorUtility.SetDirty(purchase);
            }

            // 최하단 심층 잠금 안내.
            PlaceChild(panel, "ProgDeep", new Vector2(0f, -160f), new Vector2(860f, 36f));

            // 다른 하단 텍스트(목표 등)와 겹치지 않도록 좌측 상태 영역 보정.
            var content = root.transform.Find("SurfaceBaseContent") ?? root.transform;
            PlaceChild(content, "GoalsText", new Vector2(0f, 160f), new Vector2(700f, 40f));
            PlaceChild(content, "EnergyText", new Vector2(0f, 120f), new Vector2(700f, 36f));
            PlaceChild(content, "DeepZoneText", new Vector2(0f, 80f), new Vector2(700f, 36f));
            PlaceChild(content, "RecentRunText", new Vector2(0f, 40f), new Vector2(700f, 36f));
            PlaceChild(content, "MessageText", new Vector2(0f, 4f), new Vector2(700f, 32f));

            EditorUtility.SetDirty(progression);
        }

        /// <summary>
        /// 화면 진동 억제: 텍스트 좌 / 체크 우, 그룹 중앙.
        /// 언어: 드롭다운 중앙, 기존 높이 유지.
        /// </summary>
        private static void ApplySettingsLayout(GameObject root, System.Type viewType)
        {
            var view = root.GetComponent(viewType) as MonoBehaviour
                ?? root.GetComponentInChildren(viewType, true) as MonoBehaviour;
            if (view == null)
            {
                return;
            }

            var settingsRoot = FindChildRecursive(root.transform, "SettingsPanel")
                ?? FindChildRecursive(root.transform, "SettingsRoot");
            if (settingsRoot == null)
            {
                // View 직렬화 필드로 탐색.
                var soProbe = new SerializedObject(view);
                var rootProp = soProbe.FindProperty("settingsRoot");
                if (rootProp != null && rootProp.objectReferenceValue is GameObject go)
                {
                    settingsRoot = go.transform;
                }
            }

            if (settingsRoot == null)
            {
                return;
            }

            // --- Reduce motion: 텍스트 왼쪽, 체크 오른쪽, 그룹 중앙 ---
            var reduceMotion = FindChildRecursive(settingsRoot, "ReduceMotion");
            var reduceLabel = FindChildRecursive(settingsRoot, "ReduceMotionLabel")
                ?? (reduceMotion != null ? reduceMotion.Find("Label") : null);
            var reduceToggle = reduceMotion != null
                ? reduceMotion.GetComponent<Toggle>()
                : null;

            // 그룹 컨테이너 확보.
            Transform group = settingsRoot.Find("ReduceMotionGroup");
            if (group == null)
            {
                var groupGo = new GameObject("ReduceMotionGroup", typeof(RectTransform));
                groupGo.transform.SetParent(settingsRoot, false);
                group = groupGo.transform;
            }

            var groupRect = group.GetComponent<RectTransform>();
            groupRect.anchorMin = groupRect.anchorMax = new Vector2(0.5f, 0.5f);
            groupRect.pivot = new Vector2(0.5f, 0.5f);
            groupRect.anchoredPosition = new Vector2(0f, -20f);
            groupRect.sizeDelta = new Vector2(400f, 32f);
            EditorUtility.SetDirty(groupRect);

            // 라벨을 그룹 왼쪽.
            if (reduceLabel != null)
            {
                reduceLabel.SetParent(group, false);
                var lr = reduceLabel as RectTransform;
                if (lr != null)
                {
                    lr.anchorMin = lr.anchorMax = new Vector2(0f, 0.5f);
                    lr.pivot = new Vector2(0f, 0.5f);
                    lr.anchoredPosition = new Vector2(0f, 0f);
                    lr.sizeDelta = new Vector2(320f, 28f);
                    var tmp = reduceLabel.GetComponent<TMP_Text>();
                    if (tmp != null)
                    {
                        tmp.text = "화면 진동 억제";
                        tmp.alignment = TextAlignmentOptions.Left;
                        EditorUtility.SetDirty(tmp);
                    }

                    EditorUtility.SetDirty(lr);
                }
            }

            // 체크박스를 그룹 오른쪽.
            if (reduceMotion != null)
            {
                // Toggle 루트를 그룹 하위로 옮기고 체크 영역만 우측에.
                reduceMotion.SetParent(group, false);
                var tr = reduceMotion as RectTransform;
                if (tr != null)
                {
                    tr.anchorMin = tr.anchorMax = new Vector2(1f, 0.5f);
                    tr.pivot = new Vector2(1f, 0.5f);
                    tr.anchoredPosition = new Vector2(0f, 0f);
                    tr.sizeDelta = new Vector2(28f, 28f);
                    EditorUtility.SetDirty(tr);
                }

                // 내부 Background를 전체 채움.
                var bg = reduceMotion.Find("Background") as RectTransform;
                if (bg != null)
                {
                    bg.anchorMin = Vector2.zero;
                    bg.anchorMax = Vector2.one;
                    bg.pivot = new Vector2(0.5f, 0.5f);
                    bg.offsetMin = bg.offsetMax = Vector2.zero;
                    bg.anchoredPosition = Vector2.zero;
                    bg.sizeDelta = Vector2.zero;
                    EditorUtility.SetDirty(bg);
                }

                // 예전 라벨이 Toggle 안에 있으면 그룹으로 이미 뺐거나 비활성.
                var innerLabel = reduceMotion.Find("Label");
                if (innerLabel != null && reduceLabel != null && innerLabel != reduceLabel)
                {
                    Object.DestroyImmediate(innerLabel.gameObject);
                }
            }

            // --- Language dropdown ---
            var languageLabel = FindChildRecursive(settingsRoot, "LanguageLabel");
            var languageCycle = FindChildRecursive(settingsRoot, "LanguageCycle");
            if (languageCycle != null)
            {
                languageCycle.gameObject.SetActive(false);
            }

            var dropdownTf = settingsRoot.Find("LanguageDropdown");
            TMP_Dropdown dropdown;
            if (dropdownTf == null)
            {
                dropdown = CreateLanguageDropdown(settingsRoot);
            }
            else
            {
                dropdown = dropdownTf.GetComponent<TMP_Dropdown>();
                if (dropdown == null)
                {
                    dropdown = CreateLanguageDropdown(settingsRoot);
                }
            }

            var ddRect = dropdown.GetComponent<RectTransform>();
            ddRect.anchorMin = ddRect.anchorMax = new Vector2(0.5f, 0.5f);
            ddRect.pivot = new Vector2(0.5f, 0.5f);
            // 기존 언어 행 높이(약 36) 유지, 중앙.
            ddRect.anchoredPosition = new Vector2(0f, -70f);
            ddRect.sizeDelta = new Vector2(280f, 36f);
            EditorUtility.SetDirty(ddRect);

            if (languageLabel != null)
            {
                var ll = languageLabel as RectTransform;
                if (ll != null)
                {
                    ll.anchorMin = ll.anchorMax = new Vector2(0.5f, 0.5f);
                    ll.pivot = new Vector2(0.5f, 0.5f);
                    ll.anchoredPosition = new Vector2(0f, -48f);
                    ll.sizeDelta = new Vector2(280f, 24f);
                    var tmp = languageLabel.GetComponent<TMP_Text>();
                    if (tmp != null)
                    {
                        tmp.text = "언어";
                        tmp.alignment = TextAlignmentOptions.Center;
                        EditorUtility.SetDirty(tmp);
                    }

                    EditorUtility.SetDirty(ll);
                }
            }

            // View 직렬화 연결.
            var so = new SerializedObject(view);
            var reduceToggleProp = so.FindProperty("reduceMotionToggle");
            if (reduceToggleProp != null && reduceToggle != null)
            {
                reduceToggleProp.objectReferenceValue = reduceToggle;
            }

            var reduceLabelProp = so.FindProperty("reduceMotionLabel");
            if (reduceLabelProp != null && reduceLabel != null)
            {
                reduceLabelProp.objectReferenceValue = reduceLabel.GetComponent<TMP_Text>();
            }

            var langDropProp = so.FindProperty("languageDropdown");
            if (langDropProp != null)
            {
                langDropProp.objectReferenceValue = dropdown;
            }

            var langCycleProp = so.FindProperty("languageCycleButton");
            if (langCycleProp != null)
            {
                langCycleProp.objectReferenceValue = null;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        private static TMP_Dropdown CreateLanguageDropdown(Transform settingsRoot)
        {
            var existing = settingsRoot.Find("LanguageDropdown");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var go = new GameObject(
                "LanguageDropdown",
                typeof(RectTransform),
                typeof(Image),
                typeof(TMP_Dropdown));
            go.transform.SetParent(settingsRoot, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.16f, 0.22f, 0.28f, 1f);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            var font = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (font != null)
            {
                label.font = font;
            }

            label.text = LocalizationService.FormatLanguage(GameLanguage.Korean);
            label.fontSize = 16f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            var lr = label.rectTransform;
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(10f, 2f);
            lr.offsetMax = new Vector2(-28f, -2f);

            var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            arrowGo.transform.SetParent(go.transform, false);
            var ar = arrowGo.GetComponent<RectTransform>();
            ar.anchorMin = ar.anchorMax = new Vector2(1f, 0.5f);
            ar.pivot = new Vector2(1f, 0.5f);
            ar.anchoredPosition = new Vector2(-8f, 0f);
            ar.sizeDelta = new Vector2(16f, 16f);
            arrowGo.GetComponent<Image>().color = new Color(0.8f, 0.85f, 0.9f, 1f);

            // Template (최소 구성).
            var template = new GameObject(
                "Template",
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect));
            template.transform.SetParent(go.transform, false);
            var templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, 2f);
            templateRect.sizeDelta = new Vector2(0f, 90f);
            template.GetComponent<Image>().color = new Color(0.1f, 0.14f, 0.18f, 0.98f);

            var viewport = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask));
            viewport.transform.SetParent(template.transform, false);
            var vpRect = viewport.GetComponent<RectTransform>();
            StretchFull(vpRect);
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 56f);

            var item = new GameObject(
                "Item",
                typeof(RectTransform),
                typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 28f);

            var itemBg = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            itemBg.transform.SetParent(item.transform, false);
            StretchFull(itemBg.GetComponent<RectTransform>());
            itemBg.GetComponent<Image>().color = new Color(0.2f, 0.28f, 0.34f, 1f);

            var itemLabelGo = new GameObject("Item Label", typeof(RectTransform));
            itemLabelGo.transform.SetParent(item.transform, false);
            var itemLabel = itemLabelGo.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                itemLabel.font = font;
            }

            itemLabel.fontSize = 15f;
            itemLabel.alignment = TextAlignmentOptions.Center;
            itemLabel.color = Color.white;
            StretchFull(itemLabel.rectTransform);

            var itemToggle = item.GetComponent<Toggle>();
            itemToggle.targetGraphic = itemBg.GetComponent<Image>();
            itemToggle.isOn = true;

            var scroll = template.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = vpRect;
            scroll.horizontal = false;
            scroll.vertical = true;

            var dropdown = go.GetComponent<TMP_Dropdown>();
            dropdown.targetGraphic = image;
            dropdown.captionText = label;
            dropdown.itemText = itemLabel;
            dropdown.template = templateRect;
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>
            {
                LocalizationService.FormatLanguage(GameLanguage.Korean),
                LocalizationService.FormatLanguage(GameLanguage.English)
            });
            dropdown.value = 0;
            dropdown.RefreshShownValue();
            template.SetActive(false);

            return dropdown;
        }

        private static void PlaceChild(
            Transform parent,
            string childName,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var child = FindChildRecursive(parent, childName) as RectTransform;
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

        private static void EnsureXCloseButton(Transform root, out Button xClose)
        {
            var xButtons = root.GetComponentsInChildren<Button>(true)
                .Where(b => b.name == "CloseButton")
                .ToList();
            xClose = xButtons.FirstOrDefault(IsXLabel) ?? xButtons.FirstOrDefault();
            for (var i = 0; i < xButtons.Count; i++)
            {
                if (xButtons[i] != xClose)
                {
                    // 중복 Close 제거(X 우선 유지).
                    if (IsXLabel(xButtons[i]) && xClose != null && xButtons[i] != xClose)
                    {
                        Object.DestroyImmediate(xButtons[i].gameObject);
                    }
                }
            }

            if (xClose != null)
            {
                if (xClose.transform.parent != root)
                {
                    xClose.transform.SetParent(root, false);
                }

                var label = xClose.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = "×";
                }

                LayoutXButton(xClose);
                return;
            }

            var go = new GameObject(
                "CloseButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            go.transform.SetParent(root, false);
            go.GetComponent<Image>().color = new Color(0.22f, 0.18f, 0.18f, 0.95f);
            xClose = go.GetComponent<Button>();
            LayoutXButton(xClose);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            var font = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (font != null)
            {
                tmp.font = font;
            }

            tmp.text = "×";
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            StretchFull(tmp.rectTransform);
        }

        private static bool IsXLabel(Button button)
        {
            var tmp = button.GetComponentInChildren<TMP_Text>(true);
            var text = tmp != null ? tmp.text : string.Empty;
            return text == "×" || text == "x" || text == "X" || text == "✕";
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

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
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

        private static List<Transform> FindAllTransforms(Scene scene, string objectName)
        {
            var list = new List<Transform>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == objectName)
                    {
                        list.Add(t);
                    }
                }
            }

            return list;
        }

        private static Transform FindTransform(Scene scene, string objectName)
        {
            return FindAllTransforms(scene, objectName).FirstOrDefault();
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
