using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 33-2:
    /// - 설정창 세로 50%·상하 간격 확대, 드롭다운 흰 네모(Arrow) 제거
    /// - 프레임 드롭다운(자동/30/60/120/144/제한없음)
    /// - Surface Base 단일 영역·크기 +10%, 업그레이드 목록/상세 통합(겹침 제거)
    /// - 장비 업그레이드 창 화면의 40% 크기
    /// </summary>
    public static class PromptB33_2LayoutBuilder
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

        // Content 980x900 → +10%
        private const float SurfaceContentWidth = 980f * 1.1f;
        private const float SurfaceContentHeight = 900f * 1.1f;

        [MenuItem("SubTerra/UI/Build Prompt-B 33-2 Settings Surface Upgrade Fixes")]
        public static void BuildFromMenu()
        {
            var report = Build();
            Debug.Log("[SubTerra] " + report);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Temp"));
            File.WriteAllText(
                Path.Combine(projectRoot, "Temp", "prompt-b-33-2-layout.txt"),
                report);
        }

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Prompt-B 33-2 Fixes");
            sb.AppendLine(UpdateSurfaceBasePrefab());
            sb.AppendLine(UpdateMainMenuPrefab());
            sb.AppendLine(UpdateSurfaceBaseScene());
            sb.AppendLine(UpdateMainMenuScene());
            sb.AppendLine(UpdateIntegrationUpgradePanel());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sb.ToString();
        }

        /// <summary>SurfaceBase prefab·씬만 33-2 설정/본문 레이아웃 적용(MainMenu·Integration 제외).</summary>
        public static string BuildSurfaceBaseOnly()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Prompt-B 33-2 SurfaceBase only");
            sb.AppendLine(UpdateSurfaceBasePrefab());
            sb.AppendLine(UpdateSurfaceBaseScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sb.ToString();
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
                ApplySurfaceBaseSizeAndSingleColumn(root);
                ApplySettingsPanelLayout(root, typeof(SurfaceBaseView));
                PrefabUtility.SaveAsPrefabAsset(root, SurfaceBasePrefabPath);
                return "SurfaceBasePrefab size+10% single-column settings50%";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
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
                ApplySettingsPanelLayout(root, typeof(MainMenuView));
                PrefabUtility.SaveAsPrefabAsset(root, MainMenuPrefabPath);
                return "MainMenuPrefab settings50% frame dropdown";
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
                ApplySurfaceBaseSizeAndSingleColumn(panel.gameObject);
                ApplySettingsPanelLayout(panel.gameObject, typeof(SurfaceBaseView));
                PrefabUtility.RecordPrefabInstancePropertyModifications(panel.gameObject);
                EditorUtility.SetDirty(panel.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            RestoreScene(previous, SurfaceBaseScenePath);
            return "SurfaceBase scene updated";
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
                ApplySettingsPanelLayout(view.gameObject, typeof(MainMenuView));
                PrefabUtility.RecordPrefabInstancePropertyModifications(view.gameObject);
                EditorUtility.SetDirty(view.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            RestoreScene(previous, MainMenuScenePath);
            return "MainMenu scene updated";
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

            ApplyUpgradePanelFortyPercent(upgrade);
            LayoutUpgradePanelContents(upgrade);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            RestoreScene(previous, IntegrationScenePath);
            return "UpgradePanel 40% screen + no-overlap layout";
        }

        /// <summary>
        /// Surface Base 콘텐츠 +10%, Progression 단일 열(목록 숨김·상세만).
        /// </summary>
        private static void ApplySurfaceBaseSizeAndSingleColumn(GameObject root)
        {
            var content = root.transform.Find("SurfaceBaseContent") as RectTransform;
            if (content == null)
            {
                content = root.GetComponent<RectTransform>();
            }

            if (content != null)
            {
                // stretch 루트면 sizeDelta 0일 수 있어, 콘텐츠 전용 크기를 강제한다.
                if (content.name == "SurfaceBaseContent"
                    || content.anchorMin == content.anchorMax)
                {
                    content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
                    content.pivot = new Vector2(0.5f, 0.5f);
                    content.anchoredPosition = Vector2.zero;
                    content.sizeDelta = new Vector2(SurfaceContentWidth, SurfaceContentHeight);
                    EditorUtility.SetDirty(content);
                }
            }

            // 상단 상태 텍스트 — 중앙 단일 열, 겹침 방지.
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
                panel.sizeDelta = new Vector2(760f, 420f);
                EditorUtility.SetDirty(panel);
            }

            // 카테고리 탭 상단 중앙.
            var tabBar = progression.transform.Find("CategoryTabBar") as RectTransform;
            if (tabBar != null)
            {
                tabBar.anchorMin = tabBar.anchorMax = new Vector2(0.5f, 1f);
                tabBar.pivot = new Vector2(0.5f, 1f);
                tabBar.anchoredPosition = new Vector2(0f, -8f);
                tabBar.sizeDelta = new Vector2(720f, 36f);
                EditorUtility.SetDirty(tabBar);
            }

            // 좌측 목록 텍스트/엔트리 숨김 — 상세 카드에 이름·레벨이 포함됨.
            var upgradeList = progression.transform.Find("UpgradeList");
            if (upgradeList != null)
            {
                upgradeList.gameObject.SetActive(false);
            }

            progression.EditorSetHideUpgradeEntryList(true);
            foreach (var entry in progression.GetComponentsInChildren<ProgressionUpgradeEntryButton>(true))
            {
                entry.gameObject.SetActive(false);
            }

            var progSo = new SerializedObject(progression);
            var hideProp = progSo.FindProperty("hideUpgradeEntryList");
            if (hideProp != null)
            {
                hideProp.boolValue = true;
                progSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // 상세 카드 단일 중앙 배치.
            PlaceCentered(progression.transform, "UpgradeDetail", 40f, 700f, 160f);
            PlaceCentered(progression.transform, "UpgradeResult", -70f, 700f, 40f);
            PlaceCentered(progression.transform, "ProgDeep", -120f, 700f, 36f);

            var purchase = progression.transform.Find("PurchaseButton") as RectTransform;
            if (purchase != null)
            {
                purchase.anchorMin = purchase.anchorMax = new Vector2(0.5f, 0.5f);
                purchase.pivot = new Vector2(0.5f, 0.5f);
                purchase.anchoredPosition = new Vector2(0f, -170f);
                purchase.sizeDelta = new Vector2(220f, 44f);
                var label = purchase.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = "업그레이드";
                    EditorUtility.SetDirty(label);
                }

                EditorUtility.SetDirty(purchase);
            }

            // 탭으로만 전환 — 각 탭 첫 항목 자동 선택(Presenter 기존 동작).
            // 상세 텍스트가 이름+레벨+현재→다음+재료를 모두 포함.
            EditorUtility.SetDirty(progression);
        }

        /// <summary>장비 업그레이드 창: 화면 가로·세로 40% 중앙.</summary>
        private static void ApplyUpgradePanelFortyPercent(Transform upgrade)
        {
            var rect = upgrade as RectTransform;
            if (rect == null)
            {
                return;
            }

            // (0.3~0.7) = 화면의 40%.
            rect.anchorMin = new Vector2(0.3f, 0.3f);
            rect.anchorMax = new Vector2(0.7f, 0.7f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            EditorUtility.SetDirty(rect);

            // 배경이 있으면 전체 스트레치.
            var bg = upgrade.GetComponent<Image>();
            if (bg != null)
            {
                EditorUtility.SetDirty(bg);
            }
        }

        private static void LayoutUpgradePanelContents(Transform upgrade)
        {
            // 내부 요소를 상단부터 세로로 배치해 겹침 방지.
            var panelRoot = upgrade.Find("PanelRoot") ?? upgrade;

            PlaceInPanel(panelRoot, "CategoryTabBar", 0.5f, 1f, new Vector2(0f, -16f), new Vector2(-40f, 40f), stretchX: true);
            PlaceInPanel(panelRoot, "UpgradeList", 0.5f, 1f, new Vector2(0f, -70f), new Vector2(-40f, 36f), stretchX: true);

            // 엔트리 버튼: 좌측 열.
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

                r.anchorMin = new Vector2(0f, 1f);
                r.anchorMax = new Vector2(0.45f, 1f);
                r.pivot = new Vector2(0f, 1f);
                r.anchoredPosition = new Vector2(20f, -120f - i * 46f);
                r.offsetMin = new Vector2(20f, r.offsetMin.y);
                r.offsetMax = new Vector2(-10f, r.offsetMax.y);
                r.sizeDelta = new Vector2(0f, 40f);
                EditorUtility.SetDirty(r);
            }

            // 상세·결과·구매·심층: 우측 열.
            PlaceRightColumn(panelRoot, "UpgradeDetail", -120f, 160f);
            PlaceRightColumn(panelRoot, "UpgradeResult", -300f, 48f);
            PlaceRightColumn(panelRoot, "ProgDeep", -360f, 40f);

            var purchase = panelRoot.Find("PurchaseButton") as RectTransform
                ?? upgrade.Find("PurchaseButton") as RectTransform;
            if (purchase != null)
            {
                purchase.anchorMin = new Vector2(0.55f, 1f);
                purchase.anchorMax = new Vector2(1f, 1f);
                purchase.pivot = new Vector2(0.5f, 1f);
                purchase.anchoredPosition = new Vector2(-20f, -420f);
                purchase.sizeDelta = new Vector2(-40f, 44f);
                EditorUtility.SetDirty(purchase);
            }
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
                tf.pivot = new Vector2(0.5f, 1f);
                tf.anchoredPosition = pos;
                tf.sizeDelta = size;
            }

            EditorUtility.SetDirty(tf);
        }

        /// <summary>
        /// 설정 패널 높이 50%, 상하 간격 확대, 드롭다운 화살표(흰 네모) 제거, 프레임 드롭다운.
        /// </summary>
        private static void ApplySettingsPanelLayout(GameObject root, System.Type viewType)
        {
            var view = root.GetComponent(viewType) as MonoBehaviour
                ?? root.GetComponentInChildren(viewType, true) as MonoBehaviour;
            if (view == null)
            {
                return;
            }

            var settingsRoot = FindChildRecursive(root.transform, "SettingsPanel");
            if (settingsRoot == null)
            {
                var soProbe = new SerializedObject(view);
                var prop = soProbe.FindProperty("settingsRoot");
                if (prop != null && prop.objectReferenceValue is GameObject go)
                {
                    settingsRoot = go.transform;
                }
            }

            if (settingsRoot == null)
            {
                return;
            }

            var settingsRect = settingsRoot as RectTransform;
            if (settingsRect != null)
            {
                // 세로 50% (0.25~0.75), 가로는 중앙 고정 폭.
                settingsRect.anchorMin = new Vector2(0.5f, 0.25f);
                settingsRect.anchorMax = new Vector2(0.5f, 0.75f);
                settingsRect.pivot = new Vector2(0.5f, 0.5f);
                settingsRect.anchoredPosition = Vector2.zero;
                settingsRect.sizeDelta = new Vector2(600f, 0f);
                EditorUtility.SetDirty(settingsRect);
            }

            // 언어 드롭다운 흰 네모(Arrow) 제거.
            StripDropdownWhiteArrow(settingsRoot.Find("LanguageDropdown"));

            // 프레임 드롭다운 확보 — 레이아웃 전에 생성해야 PlaceSettingsRow가 적용된다.
            var frameLabel = EnsureFrameRateLabel(settingsRoot);
            var frameDropdown = EnsureFrameRateDropdown(settingsRoot);
            StripDropdownWhiteArrow(frameDropdown != null ? frameDropdown.transform : null);

            // 상하 간격 확대 — 정규화 y로 배치(패널 높이 기준).
            // 패널 local: 상단 +0.5 ~ 하단 -0.5 (anchor stretch 후 sizeDelta.y=0이면
            // anchoredPosition은 부모 높이 기준이 아니라 자체 좌표계이므로
            // stretch 높이에서는 offset 대신 비율 앵커 자식을 쓴다.
            LayoutSettingsChildren(settingsRoot);

            // View 직렬화 연결.
            var so = new SerializedObject(view);
            if (so.FindProperty("frameRateLabel") != null)
            {
                so.FindProperty("frameRateLabel").objectReferenceValue =
                    frameLabel != null ? frameLabel.GetComponent<TMP_Text>() : null;
            }

            if (so.FindProperty("frameRateDropdown") != null)
            {
                so.FindProperty("frameRateDropdown").objectReferenceValue = frameDropdown;
            }

            // 기존 language dropdown 재연결 유지.
            var langDd = settingsRoot.Find("LanguageDropdown");
            if (langDd != null && so.FindProperty("languageDropdown") != null)
            {
                so.FindProperty("languageDropdown").objectReferenceValue =
                    langDd.GetComponent<TMP_Dropdown>();
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        private static void LayoutSettingsChildren(Transform settingsRoot)
        {
            // stretch 패널 기준: anchor Y 비율로 배치해 높이 50%에 맞게 간격 확보.
            // 위→아래: 제목, 음량라벨, 슬라이더, 해상도, 진동억제, 언어, 프레임, BGM, 버튼
            PlaceSettingsRow(settingsRoot, "SettingsTitle", 0.92f, 40f, 420f);
            PlaceSettingsRow(settingsRoot, "MasterVolumeLabel", 0.82f, 28f, 420f);
            PlaceSettingsRow(settingsRoot, "MasterVolume", 0.74f, 28f, 400f);
            PlaceSettingsRow(settingsRoot, "ResolutionLabel", 0.64f, 28f, 280f);
            PlaceSettingsRow(settingsRoot, "ResolutionPrev", 0.64f, 36f, 70f, -210f);
            PlaceSettingsRow(settingsRoot, "ResolutionNext", 0.64f, 36f, 70f, 210f);
            PlaceSettingsRow(settingsRoot, "ReduceMotionGroup", 0.54f, 32f, 400f);
            PlaceSettingsRow(settingsRoot, "LanguageLabel", 0.46f, 24f, 280f);
            PlaceSettingsRow(settingsRoot, "LanguageDropdown", 0.40f, 36f, 280f);
            PlaceSettingsRow(settingsRoot, "FrameRateLabel", 0.32f, 24f, 280f);
            PlaceSettingsRow(settingsRoot, "FrameRateDropdown", 0.26f, 36f, 280f);
            PlaceSettingsRow(settingsRoot, "BgmHint", 0.16f, 40f, 520f);
            PlaceSettingsRow(settingsRoot, "SettingsApply", 0.06f, 40f, 120f, -140f);
            PlaceSettingsRow(settingsRoot, "SettingsCancel", 0.06f, 40f, 120f, 0f);
            PlaceSettingsRow(settingsRoot, "SettingsDefaults", 0.06f, 40f, 120f, 140f);

            // 구 사이클 버튼 숨김.
            var cycle = settingsRoot.Find("LanguageCycle");
            if (cycle != null)
            {
                cycle.gameObject.SetActive(false);
            }
        }

        private static void PlaceSettingsRow(
            Transform parent,
            string name,
            float anchorY,
            float height,
            float width,
            float xOffset = 0f)
        {
            var child = FindChildRecursive(parent, name) as RectTransform;
            if (child == null)
            {
                return;
            }

            child.anchorMin = new Vector2(0.5f, anchorY);
            child.anchorMax = new Vector2(0.5f, anchorY);
            child.pivot = new Vector2(0.5f, 0.5f);
            child.anchoredPosition = new Vector2(xOffset, 0f);
            child.sizeDelta = new Vector2(width, height);
            EditorUtility.SetDirty(child);
        }

        private static void StripDropdownWhiteArrow(Transform dropdownRoot)
        {
            if (dropdownRoot == null)
            {
                return;
            }

            var arrow = dropdownRoot.Find("Arrow");
            if (arrow != null)
            {
                Object.DestroyImmediate(arrow.gameObject);
            }

            // caption 라벨을 전체 폭으로.
            var label = dropdownRoot.Find("Label") as RectTransform;
            if (label != null)
            {
                label.anchorMin = Vector2.zero;
                label.anchorMax = Vector2.one;
                label.offsetMin = new Vector2(10f, 2f);
                label.offsetMax = new Vector2(-10f, -2f);
                EditorUtility.SetDirty(label);
            }
        }

        private static Transform EnsureFrameRateLabel(Transform settingsRoot)
        {
            var existing = settingsRoot.Find("FrameRateLabel");
            if (existing != null)
            {
                var tmp = existing.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = LocalizationService.Get("settings.frame_rate", "프레임");
                    tmp.alignment = TextAlignmentOptions.Center;
                }

                return existing;
            }

            var go = new GameObject("FrameRateLabel", typeof(RectTransform));
            go.transform.SetParent(settingsRoot, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            var font = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (font != null)
            {
                text.font = font;
            }

            text.text = LocalizationService.Get("settings.frame_rate", "프레임");
            text.fontSize = 17f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            return go.transform;
        }

        private static TMP_Dropdown EnsureFrameRateDropdown(Transform settingsRoot)
        {
            var existing = settingsRoot.Find("FrameRateDropdown");
            if (existing != null)
            {
                var dd = existing.GetComponent<TMP_Dropdown>();
                if (dd != null)
                {
                    EnsureFrameOptions(dd);
                    return dd;
                }

                Object.DestroyImmediate(existing.gameObject);
            }

            return CreateDropdown(
                settingsRoot,
                "FrameRateDropdown",
                BuildFrameOptionLabels());
        }

        private static void EnsureFrameOptions(TMP_Dropdown dropdown)
        {
            var labels = BuildFrameOptionLabels();
            if (dropdown.options == null || dropdown.options.Count != labels.Count)
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(labels);
                dropdown.value = 0;
                dropdown.RefreshShownValue();
            }
        }

        private static List<string> BuildFrameOptionLabels()
        {
            var list = new List<string>(6);
            for (var i = 0; i < 6; i++)
            {
                list.Add(LocalizationService.FormatFrameRateOption(i));
            }

            return list;
        }

        private static TMP_Dropdown CreateDropdown(
            Transform parent,
            string name,
            List<string> options)
        {
            var go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(TMP_Dropdown));
            go.transform.SetParent(parent, false);
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

            label.fontSize = 16f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            var lr = label.rectTransform;
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(10f, 2f);
            lr.offsetMax = new Vector2(-10f, -2f);

            // Template
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
            templateRect.sizeDelta = new Vector2(0f, 150f);
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
            contentRect.sizeDelta = new Vector2(0f, 180f);

            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
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
            dropdown.AddOptions(options);
            dropdown.value = 0;
            dropdown.RefreshShownValue();
            template.SetActive(false);
            return dropdown;
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
