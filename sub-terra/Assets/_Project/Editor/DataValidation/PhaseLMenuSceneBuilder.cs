using System.Collections.Generic;
using System.Linq;
using SubTerra.App.Core;
using SubTerra.App.UI;
using SubTerra.App.UI.Economy;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.Save;
using SubTerra.App.UI.SurfaceBase;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// Phase L MainMenu / SurfaceBase Scene·Prefab 조립.
    /// YAML 직접 편집 대신 Editor API로만 생성한다.
    /// </summary>
    public static class PhaseLMenuSceneBuilder
    {
        public const string MainMenuScenePath =
            "Assets/_Project/Scenes/App/MainMenu.unity";
        public const string SurfaceBaseScenePath =
            "Assets/_Project/Scenes/App/SurfaceBase.unity";
        public const string MainMenuPrefabPath =
            "Assets/_Project/Prefabs/UI/MainMenuPanel.prefab";
        public const string SurfaceBasePrefabPath =
            "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";

        [MenuItem("SubTerra/UI/Build Phase L Menu Scenes")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            var previous = SceneManager.GetActiveScene().path;
            EnsureKoreanFontFallback();
            PhaseKSaveSlotPrefabBuilder.Build();
            BuildMainMenuPrefab();
            BuildSurfaceBasePrefab();
            WireMainMenuScene();
            WireSurfaceBaseScene();
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!string.IsNullOrEmpty(previous)
                && System.IO.File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }

            return "Phase L MainMenu+SurfaceBase scenes/prefabs wired.";
        }

        public static string BuildMainMenuPrefab()
        {
            var root = new GameObject("MainMenuPanel", typeof(RectTransform), typeof(Image));
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.07f, 0.96f);

            var content = CreatePanel(
                root.transform,
                "MenuContent",
                Vector2.zero,
                new Vector2(940f, 820f),
                new Color(0.035f, 0.075f, 0.105f, 0.98f));
            CreateText(content.transform, "Title", new Vector2(0f, 330f), new Vector2(760f, 76f), 44f, "Sub-Terra");
            var slotButtons = new Button[3];
            var slotTexts = new TMP_Text[3];
            for (var i = 0; i < 3; i++)
            {
                slotButtons[i] = CreateButton(
                    content.transform,
                    "Slot" + (i + 1),
                    new Vector2(0f, 210f - i * 90f),
                    new Vector2(728f, 73f),
                    "Slot " + (i + 1) + "  Empty",
                    out slotTexts[i],
                    23f);
            }

            var continueButton = CreateButton(
                content.transform, "ContinueButton", new Vector2(-240f, -100f), new Vector2(221f, 65f), "이어하기", out _, 23f);
            var newGameButton = CreateButton(
                content.transform, "NewGameButton", new Vector2(0f, -100f), new Vector2(221f, 65f), "새 게임", out _, 23f);
            var settingsButton = CreateButton(
                content.transform, "SettingsButton", new Vector2(240f, -100f), new Vector2(221f, 65f), "설정", out _, 23f);
            var quitButton = CreateButton(
                content.transform, "QuitButton", new Vector2(0f, -188f), new Vector2(260f, 62f), "종료", out _, 22f);
            var message = CreateText(content.transform, "MessageText", new Vector2(0f, -258f), new Vector2(780f, 54f), 20f, string.Empty);
            var version = CreateText(content.transform, "VersionText", new Vector2(0f, -318f), new Vector2(360f, 38f), 18f, "v0.0.0");

            var overwriteRoot = new GameObject("OverwriteConfirm", typeof(RectTransform), typeof(Image));
            overwriteRoot.transform.SetParent(root.transform, false);
            var overwriteRect = overwriteRoot.GetComponent<RectTransform>();
            overwriteRect.anchorMin = overwriteRect.anchorMax = new Vector2(0.5f, 0.5f);
            overwriteRect.pivot = new Vector2(0.5f, 0.5f);
            overwriteRect.anchoredPosition = Vector2.zero;
            overwriteRect.sizeDelta = new Vector2(624f, 286f);
            overwriteRoot.GetComponent<Image>().color = new Color(0.1f, 0.05f, 0.05f, 0.98f);
            overwriteRoot.SetActive(false);
            var overwriteMsg = CreateText(
                overwriteRoot.transform, "OverwriteMessage", new Vector2(0f, 28.6f), new Vector2(546f, 78f), 23.4f, "덮어쓰기?");
            var overwriteYes = CreateButton(
                overwriteRoot.transform, "OverwriteYes", new Vector2(-120f, -28.6f), new Vector2(182f, 57.2f), "확인", out _, 23.4f);
            var overwriteNo = CreateButton(
                overwriteRoot.transform, "OverwriteNo", new Vector2(120f, -28.6f), new Vector2(182f, 57.2f), "취소", out _, 23.4f);

            var settings = BuildSettingsPanel(root.transform);

            var view = root.AddComponent<MainMenuView>();
            var so = new SerializedObject(view);
            so.FindProperty("panelRoot").objectReferenceValue = root;
            AssignArray(so.FindProperty("slotButtons"), slotButtons);
            AssignArray(so.FindProperty("slotTexts"), slotTexts);
            so.FindProperty("continueButton").objectReferenceValue = continueButton;
            so.FindProperty("newGameButton").objectReferenceValue = newGameButton;
            so.FindProperty("settingsButton").objectReferenceValue = settingsButton;
            so.FindProperty("quitButton").objectReferenceValue = quitButton;
            so.FindProperty("messageText").objectReferenceValue = message;
            so.FindProperty("versionText").objectReferenceValue = version;
            so.FindProperty("overwriteConfirmRoot").objectReferenceValue = overwriteRoot;
            so.FindProperty("overwriteMessageText").objectReferenceValue = overwriteMsg;
            so.FindProperty("overwriteConfirmButton").objectReferenceValue = overwriteYes;
            so.FindProperty("overwriteCancelButton").objectReferenceValue = overwriteNo;
            AssignSettingsToView(so, settings);
            so.ApplyModifiedPropertiesWithoutUndo();

            var binder = root.AddComponent<MainMenuBinder>();
            var binderSo = new SerializedObject(binder);
            binderSo.FindProperty("view").objectReferenceValue = view;
            binderSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, MainMenuPrefabPath);
            Object.DestroyImmediate(root);
            return MainMenuPrefabPath;
        }

        public static string BuildSurfaceBasePrefab()
        {
            var root = new GameObject("SurfaceBasePanel", typeof(RectTransform), typeof(Image));
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0.03f, 0.06f, 0.05f, 0.95f);

            var content = CreatePanel(
                root.transform,
                "SurfaceBaseContent",
                Vector2.zero,
                new Vector2(980f, 900f),
                new Color(0.045f, 0.095f, 0.08f, 0.98f));
            CreateText(content.transform, "Title", new Vector2(0f, 380f), new Vector2(760f, 64f), 38f, "Surface Base");
            var energy = CreateText(
                content.transform,
                "EnergyText",
                new Vector2(0f, 315f),
                new Vector2(840f, 48f),
                22f,
                "전력 100 / 100  ·  지하행 5 소모  ·  도착 예상 95");
            var goals = CreateText(content.transform, "GoalsText", new Vector2(0f, 255f), new Vector2(800f, 52f), 22f, "목표");
            var deep = CreateText(content.transform, "DeepZoneText", new Vector2(0f, 205f), new Vector2(800f, 46f), 21f, "심층");
            var recent = CreateText(content.transform, "RecentRunText", new Vector2(0f, 155f), new Vector2(800f, 46f), 21f, "최근 탐사");
            var message = CreateText(content.transform, "MessageText", new Vector2(0f, 105f), new Vector2(800f, 46f), 19f, string.Empty);
            var explore = CreateButton(
                content.transform, "ExploreButton", new Vector2(0f, 35f), new Vector2(360f, 73f), "지하 탐사 시작 · 전력 5", out _, 21f);
            // prompt-B 31-1: 새로고침 제거 → Main Menu와 동일한 설정·종료.
            var settingsButton = CreateButton(
                content.transform, "SettingsButton", new Vector2(-130f, -45f), new Vector2(220f, 57f), "설정", out _, 20f);
            var quitButton = CreateButton(
                content.transform, "QuitButton", new Vector2(130f, -45f), new Vector2(220f, 57f), "종료", out _, 20f);

            var settings = BuildSettingsPanel(root.transform);

            // 기존 Economy/Progression View 컴포넌트를 같은 패널에 부착해 Service 재사용 경로를 유지한다.
            var economyGo = new GameObject("EconomyPanel", typeof(RectTransform));
            economyGo.transform.SetParent(content.transform, false);
            Stretch(economyGo.GetComponent<RectTransform>());
            var ecoView = economyGo.AddComponent<EconomyPanelView>();
            var ecoBinder = economyGo.AddComponent<EconomyPanelBinder>();
            var ecoStatus = CreateText(economyGo.transform, "EcoStatus", new Vector2(0f, -105f), new Vector2(800f, 42f), 18f, "판매 / 제작");
            var ecoDetail = CreateText(economyGo.transform, "EcoDetail", new Vector2(0f, -150f), new Vector2(800f, 36f), 16f, string.Empty);
            var ecoSo = new SerializedObject(ecoView);
            ecoSo.FindProperty("statusMessageText").objectReferenceValue = ecoStatus;
            ecoSo.FindProperty("statusDetailText").objectReferenceValue = ecoDetail;
            ecoSo.ApplyModifiedPropertiesWithoutUndo();
            var ecoBinderSo = new SerializedObject(ecoBinder);
            ecoBinderSo.FindProperty("view").objectReferenceValue = ecoView;
            ecoBinderSo.ApplyModifiedPropertiesWithoutUndo();

            var progGo = new GameObject("ProgressionPanel", typeof(RectTransform));
            progGo.transform.SetParent(content.transform, false);
            Stretch(progGo.GetComponent<RectTransform>());
            var progView = progGo.AddComponent<ProgressionPanelView>();
            var progBinder = progGo.AddComponent<ProgressionPanelBinder>();
            var progList = CreateText(progGo.transform, "UpgradeList", new Vector2(0f, -215f), new Vector2(800f, 44f), 18f, "드릴 / 드론 업그레이드");
            var progDetail = CreateText(progGo.transform, "UpgradeDetail", new Vector2(0f, -260f), new Vector2(800f, 36f), 16f, string.Empty);
            var progResult = CreateText(progGo.transform, "UpgradeResult", new Vector2(0f, -305f), new Vector2(800f, 36f), 16f, string.Empty);
            var progDeep = CreateText(progGo.transform, "ProgDeep", new Vector2(0f, -350f), new Vector2(800f, 36f), 16f, string.Empty);
            var progSo = new SerializedObject(progView);
            progSo.FindProperty("upgradeListText").objectReferenceValue = progList;
            progSo.FindProperty("detailText").objectReferenceValue = progDetail;
            progSo.FindProperty("resultText").objectReferenceValue = progResult;
            progSo.FindProperty("deepZoneText").objectReferenceValue = progDeep;
            progSo.FindProperty("panelRoot").objectReferenceValue = progGo;
            progSo.ApplyModifiedPropertiesWithoutUndo();
            var progBinderSo = new SerializedObject(progBinder);
            progBinderSo.FindProperty("view").objectReferenceValue = progView;
            progBinderSo.ApplyModifiedPropertiesWithoutUndo();

            var view = root.AddComponent<SurfaceBaseView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("goalsText").objectReferenceValue = goals;
            viewSo.FindProperty("energyText").objectReferenceValue = energy;
            viewSo.FindProperty("deepZoneText").objectReferenceValue = deep;
            viewSo.FindProperty("recentRunText").objectReferenceValue = recent;
            viewSo.FindProperty("messageText").objectReferenceValue = message;
            viewSo.FindProperty("exploreButton").objectReferenceValue = explore;
            viewSo.FindProperty("settingsButton").objectReferenceValue = settingsButton;
            viewSo.FindProperty("quitButton").objectReferenceValue = quitButton;
            AssignSettingsToView(viewSo, settings);
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            var binder = root.AddComponent<SurfaceBaseBinder>();
            var binderSo = new SerializedObject(binder);
            binderSo.FindProperty("view").objectReferenceValue = view;
            binderSo.FindProperty("economyBinder").objectReferenceValue = ecoBinder;
            binderSo.FindProperty("progressionBinder").objectReferenceValue = progBinder;
            binderSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, SurfaceBasePrefabPath);
            Object.DestroyImmediate(root);
            return SurfaceBasePrefabPath;
        }

        private static void WireMainMenuScene()
        {
            EnsureSceneAsset(MainMenuScenePath);
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            EnsureMainCamera(scene);
            DestroyOwned("MainMenuCanvas");
            DestroyOwned("MainMenuEventSystem");
            DestroyOwned("SaveMenuCanvas");
            DestroyOwned("SaveMenuEventSystem");

            var gameplayHud = GameObject.Find("HUDCanvas");
            if (gameplayHud != null)
            {
                gameplayHud.SetActive(false);
            }

            var canvasRoot = CreateCanvas("MainMenuCanvas");
            var safeAreaRoot = CreateSafeAreaRoot(canvasRoot.transform);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath);
            if (prefab != null)
            {
                PrefabUtility.InstantiatePrefab(prefab, safeAreaRoot.transform);
            }

            // Phase K 슬롯 패널은 비활성화하여 Phase L 메인 메뉴 패널과 중복 겹침을 방지한다.
            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhaseKSaveSlotPrefabBuilder.PrefabPath);
            if (slotPrefab != null)
            {
                var slot = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, safeAreaRoot.transform);
                slot.SetActive(false);
            }

            CreateEventSystem("MainMenuEventSystem");
            EditorSceneManager.SaveScene(scene);
        }

        private static void WireSurfaceBaseScene()
        {
            EnsureSceneAsset(SurfaceBaseScenePath);
            var scene = EditorSceneManager.OpenScene(SurfaceBaseScenePath, OpenSceneMode.Single);
            EnsureMainCamera(scene);
            DestroyOwned("SurfaceBaseCanvas");
            DestroyOwned("SurfaceBaseEventSystem");

            var canvasRoot = CreateCanvas("SurfaceBaseCanvas");
            var safeAreaRoot = CreateSafeAreaRoot(canvasRoot.transform);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SurfaceBasePrefabPath);
            if (prefab != null)
            {
                PrefabUtility.InstantiatePrefab(prefab, safeAreaRoot.transform);
            }

            CreateEventSystem("SurfaceBaseEventSystem");
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureMainCamera(Scene scene)
        {
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (cameras != null && cameras.Any(c => c.gameObject.scene == scene))
            {
                return;
            }

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.04f, 0.07f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camGo.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void EnsureKoreanFontFallback()
        {
            KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
        }

        private static void EnsureSceneAsset(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            void Ensure(string path)
            {
                if (scenes.All(entry => entry.path != path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            Ensure("Assets/_Project/Scenes/Bootstrap/Bootstrap.unity");
            Ensure(MainMenuScenePath);
            Ensure(SurfaceBaseScenePath);
            Ensure(PhaseKSaveRuntimeSceneBuilder.IntegrationScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static GameObject CreateCanvas(string name)
        {
            var canvasRoot = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return canvasRoot;
        }

        private static GameObject CreateSafeAreaRoot(Transform canvasRoot)
        {
            var safeAreaRoot = new GameObject(
                "SafeArea",
                typeof(RectTransform),
                typeof(SafeAreaFitter));
            safeAreaRoot.transform.SetParent(canvasRoot, false);

            var rect = safeAreaRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return safeAreaRoot;
        }

        private static void CreateEventSystem(string name)
        {
            // Scene당 활성 EventSystem 하나. 중복 생성 금지.
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var root = new GameObject(name, typeof(EventSystem));
            var inputModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
            {
                root.AddComponent(inputModuleType);
            }
            else
            {
                root.AddComponent<StandaloneInputModule>();
            }
        }

        private static void DestroyOwned(string name)
        {
            var root = GameObject.Find(name);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        private sealed class SettingsPanelRefs
        {
            public GameObject Root;
            public Slider VolumeSlider;
            public TMP_Text VolumeLabel;
            public Toggle ReduceMotionToggle;
            public TMP_Text ReduceMotionLabel;
            public TMP_Text ResolutionLabel;
            public Button ResolutionPrev;
            public Button ResolutionNext;
            public TMP_Text LanguageLabel;
            public Button LanguageCycle;
            public TMP_Text BgmHint;
            public Button Apply;
            public Button Cancel;
            public Button Defaults;
        }

        /// <summary>
        /// prompt-B 31-3: 마스터 음량(즉시 수치 반영), 해상도 전환, 화면 진동 억제, 언어 선택 UI.
        /// </summary>
        private static SettingsPanelRefs BuildSettingsPanel(Transform parent)
        {
            var settingsRoot = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            settingsRoot.transform.SetParent(parent, false);
            var settingsRect = settingsRoot.GetComponent<RectTransform>();
            settingsRect.anchorMin = settingsRect.anchorMax = new Vector2(0.5f, 0.5f);
            settingsRect.pivot = new Vector2(0.5f, 0.5f);
            settingsRect.anchoredPosition = Vector2.zero;
            settingsRect.sizeDelta = new Vector2(560f, 420f);
            settingsRoot.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.98f);
            settingsRoot.SetActive(false);

            CreateText(settingsRoot.transform, "SettingsTitle", new Vector2(0f, 170f), new Vector2(420f, 40f), 24f, "설정");

            var volumeLabel = CreateText(
                settingsRoot.transform,
                "MasterVolumeLabel",
                new Vector2(0f, 120f),
                new Vector2(420f, 28f),
                18f,
                "마스터 음량: 100%");

            var volumeGo = new GameObject("MasterVolume", typeof(RectTransform), typeof(Slider));
            volumeGo.transform.SetParent(settingsRoot.transform, false);
            var volumeRect = volumeGo.GetComponent<RectTransform>();
            volumeRect.anchorMin = volumeRect.anchorMax = new Vector2(0.5f, 0.5f);
            volumeRect.anchoredPosition = new Vector2(0f, 80f);
            volumeRect.sizeDelta = new Vector2(400f, 28f);
            var volumeSlider = volumeGo.GetComponent<Slider>();
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = 1f;
            // 트랙/핸들 최소 시각 요소 (Slider 동작용).
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(volumeGo.transform, false);
            var bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.3f, 1f);
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(volumeGo.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-5f, 0f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.25f, 0.65f, 0.85f, 1f);
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(volumeGo.transform, false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20f, 0f);
            handle.GetComponent<Image>().color = Color.white;
            volumeSlider.fillRect = fillRect;
            volumeSlider.handleRect = handleRect;
            volumeSlider.targetGraphic = handle.GetComponent<Image>();
            volumeSlider.direction = Slider.Direction.LeftToRight;

            var resLabel = CreateText(
                settingsRoot.transform,
                "ResolutionLabel",
                new Vector2(0f, 30f),
                new Vector2(280f, 28f),
                17f,
                "해상도: 1920 x 1080");
            var resPrev = CreateButton(
                settingsRoot.transform,
                "ResolutionPrev",
                new Vector2(-210f, 30f),
                new Vector2(70f, 36f),
                "<",
                out _,
                20f);
            var resNext = CreateButton(
                settingsRoot.transform,
                "ResolutionNext",
                new Vector2(210f, 30f),
                new Vector2(70f, 36f),
                ">",
                out _,
                20f);

            var reduceMotionGo = new GameObject("ReduceMotion", typeof(RectTransform), typeof(Toggle));
            reduceMotionGo.transform.SetParent(settingsRoot.transform, false);
            var reduceMotionRect = reduceMotionGo.GetComponent<RectTransform>();
            reduceMotionRect.anchorMin = reduceMotionRect.anchorMax = new Vector2(0.5f, 0.5f);
            reduceMotionRect.anchoredPosition = new Vector2(0f, -20f);
            reduceMotionRect.sizeDelta = new Vector2(400f, 32f);
            var reduceMotionToggle = reduceMotionGo.GetComponent<Toggle>();
            var checkBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            checkBg.transform.SetParent(reduceMotionGo.transform, false);
            var checkBgRect = checkBg.GetComponent<RectTransform>();
            checkBgRect.anchorMin = new Vector2(0f, 0.5f);
            checkBgRect.anchorMax = new Vector2(0f, 0.5f);
            checkBgRect.pivot = new Vector2(0f, 0.5f);
            checkBgRect.anchoredPosition = Vector2.zero;
            checkBgRect.sizeDelta = new Vector2(28f, 28f);
            checkBg.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.3f, 1f);
            var checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmark.transform.SetParent(checkBg.transform, false);
            var checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.offsetMin = new Vector2(4f, 4f);
            checkmarkRect.offsetMax = new Vector2(-4f, -4f);
            checkmark.GetComponent<Image>().color = new Color(0.3f, 0.85f, 0.5f, 1f);
            reduceMotionToggle.targetGraphic = checkBg.GetComponent<Image>();
            reduceMotionToggle.graphic = checkmark.GetComponent<Image>();
            reduceMotionToggle.isOn = false;
            var reduceLabel = CreateText(
                reduceMotionGo.transform,
                "Label",
                new Vector2(40f, 0f),
                new Vector2(340f, 28f),
                16f,
                "화면 진동 억제");
            reduceLabel.alignment = TextAlignmentOptions.Left;

            var languageLabel = CreateText(
                settingsRoot.transform,
                "LanguageLabel",
                new Vector2(-40f, -70f),
                new Vector2(280f, 28f),
                17f,
                "언어: 한국어");
            var languageCycle = CreateButton(
                settingsRoot.transform,
                "LanguageCycle",
                new Vector2(180f, -70f),
                new Vector2(120f, 36f),
                "전환",
                out _,
                16f);

            var bgmHint = CreateText(
                settingsRoot.transform,
                "BgmHint",
                new Vector2(0f, -115f),
                new Vector2(500f, 40f),
                14f,
                "BGM 4종(타이틀/기지/탐사/위험)은 마스터 음량으로 조절됩니다.");

            var apply = CreateButton(
                settingsRoot.transform, "SettingsApply", new Vector2(-140f, -165f), new Vector2(120f, 40f), "적용", out _);
            var cancel = CreateButton(
                settingsRoot.transform, "SettingsCancel", new Vector2(0f, -165f), new Vector2(120f, 40f), "취소", out _);
            var defaults = CreateButton(
                settingsRoot.transform, "SettingsDefaults", new Vector2(140f, -165f), new Vector2(120f, 40f), "기본값", out _);

            return new SettingsPanelRefs
            {
                Root = settingsRoot,
                VolumeSlider = volumeSlider,
                VolumeLabel = volumeLabel,
                ReduceMotionToggle = reduceMotionToggle,
                ReduceMotionLabel = reduceLabel,
                ResolutionLabel = resLabel,
                ResolutionPrev = resPrev,
                ResolutionNext = resNext,
                LanguageLabel = languageLabel,
                LanguageCycle = languageCycle,
                BgmHint = bgmHint,
                Apply = apply,
                Cancel = cancel,
                Defaults = defaults
            };
        }

        private static void AssignSettingsToView(SerializedObject so, SettingsPanelRefs settings)
        {
            so.FindProperty("settingsRoot").objectReferenceValue = settings.Root;
            so.FindProperty("masterVolumeSlider").objectReferenceValue = settings.VolumeSlider;
            so.FindProperty("masterVolumeLabel").objectReferenceValue = settings.VolumeLabel;
            so.FindProperty("reduceMotionToggle").objectReferenceValue = settings.ReduceMotionToggle;
            so.FindProperty("reduceMotionLabel").objectReferenceValue = settings.ReduceMotionLabel;
            so.FindProperty("resolutionLabel").objectReferenceValue = settings.ResolutionLabel;
            so.FindProperty("resolutionPrevButton").objectReferenceValue = settings.ResolutionPrev;
            so.FindProperty("resolutionNextButton").objectReferenceValue = settings.ResolutionNext;
            so.FindProperty("languageLabel").objectReferenceValue = settings.LanguageLabel;
            so.FindProperty("languageCycleButton").objectReferenceValue = settings.LanguageCycle;
            so.FindProperty("bgmHintLabel").objectReferenceValue = settings.BgmHint;
            so.FindProperty("settingsApplyButton").objectReferenceValue = settings.Apply;
            so.FindProperty("settingsCancelButton").objectReferenceValue = settings.Cancel;
            so.FindProperty("settingsDefaultsButton").objectReferenceValue = settings.Defaults;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            string label,
            out TMP_Text text,
            float fontSize = 18f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.14f, 0.28f, 0.34f, 1f);
            text = CreateText(go.transform, "Label", Vector2.zero, size, fontSize, label);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.alignment = TextAlignmentOptions.Center;
            return go.GetComponent<Button>();
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            float fontSize,
            string value)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            var fontAsset = KoreanFontAssetUtility.GetOrCreateKoreanFontAsset();
            if (fontAsset != null)
            {
                text.font = fontAsset;
            }
            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreatePanel(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AssignArray<T>(SerializedProperty property, T[] values)
            where T : Object
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
