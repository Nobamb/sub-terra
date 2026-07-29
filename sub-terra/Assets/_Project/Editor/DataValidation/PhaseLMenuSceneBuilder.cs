using System.Collections.Generic;
using System.Linq;
using SubTerra.App.Core;
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

            CreateText(root.transform, "Title", new Vector2(0f, -40f), new Vector2(700f, 56f), 34f, "Sub-Terra");
            var slotButtons = new Button[3];
            var slotTexts = new TMP_Text[3];
            for (var i = 0; i < 3; i++)
            {
                slotButtons[i] = CreateButton(
                    root.transform,
                    "Slot" + (i + 1),
                    new Vector2(0f, -120f - i * 70f),
                    new Vector2(560f, 56f),
                    "Slot " + (i + 1) + "  Empty",
                    out slotTexts[i]);
            }

            var continueButton = CreateButton(
                root.transform, "ContinueButton", new Vector2(-200f, -360f), new Vector2(170f, 50f), "이어하기", out _);
            var newGameButton = CreateButton(
                root.transform, "NewGameButton", new Vector2(0f, -360f), new Vector2(170f, 50f), "새 게임", out _);
            var settingsButton = CreateButton(
                root.transform, "SettingsButton", new Vector2(200f, -360f), new Vector2(170f, 50f), "설정", out _);
            var quitButton = CreateButton(
                root.transform, "QuitButton", new Vector2(0f, -430f), new Vector2(200f, 48f), "종료", out _);
            var message = CreateText(root.transform, "MessageText", new Vector2(0f, -490f), new Vector2(640f, 40f), 16f, string.Empty);
            var version = CreateText(root.transform, "VersionText", new Vector2(0f, -540f), new Vector2(300f, 32f), 14f, "v0.0.0");

            var overwriteRoot = new GameObject("OverwriteConfirm", typeof(RectTransform), typeof(Image));
            overwriteRoot.transform.SetParent(root.transform, false);
            var overwriteRect = overwriteRoot.GetComponent<RectTransform>();
            overwriteRect.anchorMin = overwriteRect.anchorMax = new Vector2(0.5f, 0.5f);
            overwriteRect.sizeDelta = new Vector2(480f, 220f);
            overwriteRoot.GetComponent<Image>().color = new Color(0.1f, 0.05f, 0.05f, 0.98f);
            overwriteRoot.SetActive(false);
            var overwriteMsg = CreateText(
                overwriteRoot.transform, "OverwriteMessage", new Vector2(0f, 40f), new Vector2(420f, 60f), 18f, "덮어쓰기?");
            var overwriteYes = CreateButton(
                overwriteRoot.transform, "OverwriteYes", new Vector2(-100f, -50f), new Vector2(140f, 44f), "확인", out _);
            var overwriteNo = CreateButton(
                overwriteRoot.transform, "OverwriteNo", new Vector2(100f, -50f), new Vector2(140f, 44f), "취소", out _);

            var settingsRoot = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            settingsRoot.transform.SetParent(root.transform, false);
            var settingsRect = settingsRoot.GetComponent<RectTransform>();
            settingsRect.anchorMin = settingsRect.anchorMax = new Vector2(0.5f, 0.5f);
            settingsRect.sizeDelta = new Vector2(520f, 280f);
            settingsRoot.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.98f);
            settingsRoot.SetActive(false);
            CreateText(settingsRoot.transform, "SettingsTitle", new Vector2(0f, 100f), new Vector2(400f, 40f), 22f, "설정");
            var volumeGo = new GameObject("MasterVolume", typeof(RectTransform), typeof(Slider));
            volumeGo.transform.SetParent(settingsRoot.transform, false);
            var volumeRect = volumeGo.GetComponent<RectTransform>();
            volumeRect.anchorMin = volumeRect.anchorMax = new Vector2(0.5f, 0.5f);
            volumeRect.anchoredPosition = new Vector2(0f, 30f);
            volumeRect.sizeDelta = new Vector2(360f, 24f);
            var volumeSlider = volumeGo.GetComponent<Slider>();
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = 1f;
            var resLabel = CreateText(
                settingsRoot.transform, "ResolutionLabel", new Vector2(0f, -10f), new Vector2(360f, 30f), 16f, "1920 x 1080");
            var apply = CreateButton(
                settingsRoot.transform, "SettingsApply", new Vector2(-140f, -90f), new Vector2(120f, 40f), "적용", out _);
            var cancel = CreateButton(
                settingsRoot.transform, "SettingsCancel", new Vector2(0f, -90f), new Vector2(120f, 40f), "취소", out _);
            var defaults = CreateButton(
                settingsRoot.transform, "SettingsDefaults", new Vector2(140f, -90f), new Vector2(120f, 40f), "기본값", out _);

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
            so.FindProperty("settingsRoot").objectReferenceValue = settingsRoot;
            so.FindProperty("masterVolumeSlider").objectReferenceValue = volumeSlider;
            so.FindProperty("resolutionLabel").objectReferenceValue = resLabel;
            so.FindProperty("settingsApplyButton").objectReferenceValue = apply;
            so.FindProperty("settingsCancelButton").objectReferenceValue = cancel;
            so.FindProperty("settingsDefaultsButton").objectReferenceValue = defaults;
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

            CreateText(root.transform, "Title", new Vector2(0f, -36f), new Vector2(600f, 48f), 30f, "Surface Base");
            var goals = CreateText(root.transform, "GoalsText", new Vector2(0f, -100f), new Vector2(640f, 40f), 18f, "목표");
            var deep = CreateText(root.transform, "DeepZoneText", new Vector2(0f, -150f), new Vector2(640f, 40f), 18f, "심층");
            var recent = CreateText(root.transform, "RecentRunText", new Vector2(0f, -200f), new Vector2(640f, 40f), 18f, "최근 탐사");
            var message = CreateText(root.transform, "MessageText", new Vector2(0f, -250f), new Vector2(640f, 40f), 16f, string.Empty);
            var explore = CreateButton(
                root.transform, "ExploreButton", new Vector2(0f, -320f), new Vector2(240f, 56f), "탐사 시작", out _);
            var refresh = CreateButton(
                root.transform, "RefreshButton", new Vector2(0f, -390f), new Vector2(180f, 44f), "새로고침", out _);

            // 기존 Economy/Progression View 컴포넌트를 같은 패널에 부착해 Service 재사용 경로를 유지한다.
            var economyGo = new GameObject("EconomyPanel", typeof(RectTransform));
            economyGo.transform.SetParent(root.transform, false);
            var ecoView = economyGo.AddComponent<EconomyPanelView>();
            var ecoBinder = economyGo.AddComponent<EconomyPanelBinder>();
            var ecoStatus = CreateText(economyGo.transform, "EcoStatus", new Vector2(-220f, -460f), new Vector2(300f, 36f), 14f, "판매/제작");
            var ecoDetail = CreateText(economyGo.transform, "EcoDetail", new Vector2(-220f, -500f), new Vector2(300f, 30f), 12f, string.Empty);
            var ecoSo = new SerializedObject(ecoView);
            ecoSo.FindProperty("statusMessageText").objectReferenceValue = ecoStatus;
            ecoSo.FindProperty("statusDetailText").objectReferenceValue = ecoDetail;
            ecoSo.ApplyModifiedPropertiesWithoutUndo();
            var ecoBinderSo = new SerializedObject(ecoBinder);
            ecoBinderSo.FindProperty("view").objectReferenceValue = ecoView;
            ecoBinderSo.ApplyModifiedPropertiesWithoutUndo();

            var progGo = new GameObject("ProgressionPanel", typeof(RectTransform));
            progGo.transform.SetParent(root.transform, false);
            var progView = progGo.AddComponent<ProgressionPanelView>();
            var progBinder = progGo.AddComponent<ProgressionPanelBinder>();
            var progList = CreateText(progGo.transform, "UpgradeList", new Vector2(220f, -460f), new Vector2(300f, 36f), 14f, "업그레이드");
            var progDetail = CreateText(progGo.transform, "UpgradeDetail", new Vector2(220f, -500f), new Vector2(300f, 30f), 12f, string.Empty);
            var progResult = CreateText(progGo.transform, "UpgradeResult", new Vector2(220f, -530f), new Vector2(300f, 28f), 12f, string.Empty);
            var progDeep = CreateText(progGo.transform, "ProgDeep", new Vector2(220f, -560f), new Vector2(300f, 28f), 12f, string.Empty);
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
            viewSo.FindProperty("deepZoneText").objectReferenceValue = deep;
            viewSo.FindProperty("recentRunText").objectReferenceValue = recent;
            viewSo.FindProperty("messageText").objectReferenceValue = message;
            viewSo.FindProperty("exploreButton").objectReferenceValue = explore;
            viewSo.FindProperty("refreshButton").objectReferenceValue = refresh;
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
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath);
            if (prefab != null)
            {
                PrefabUtility.InstantiatePrefab(prefab, canvasRoot.transform);
            }

            // Phase K 슬롯 패널도 유지해 기존 이어하기 경로를 보존한다.
            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhaseKSaveSlotPrefabBuilder.PrefabPath);
            if (slotPrefab != null)
            {
                var slot = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, canvasRoot.transform);
                var slotRect = slot.GetComponent<RectTransform>();
                if (slotRect != null)
                {
                    slotRect.anchoredPosition = new Vector2(520f, 0f);
                    slotRect.localScale = Vector3.one * 0.75f;
                }
            }

            CreateEventSystem("MainMenuEventSystem");
            EditorSceneManager.SaveScene(scene);
        }

        private static void WireSurfaceBaseScene()
        {
            EnsureSceneAsset(SurfaceBaseScenePath);
            var scene = EditorSceneManager.OpenScene(SurfaceBaseScenePath, OpenSceneMode.Single);
            DestroyOwned("SurfaceBaseCanvas");
            DestroyOwned("SurfaceBaseEventSystem");

            var canvasRoot = CreateCanvas("SurfaceBaseCanvas");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SurfaceBasePrefabPath);
            if (prefab != null)
            {
                PrefabUtility.InstantiatePrefab(prefab, canvasRoot.transform);
            }

            CreateEventSystem("SurfaceBaseEventSystem");
            EditorSceneManager.SaveScene(scene);
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

        private static Button CreateButton(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            string label,
            out TMP_Text text)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.14f, 0.28f, 0.34f, 1f);
            text = CreateText(go.transform, "Label", Vector2.zero, size, 18f, label);
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
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
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
