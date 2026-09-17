using System;
using System.IO;
using System.Linq;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>104-4번: 메인/설정 버튼 페이드, 하단 버튼 좌우 키, 슬라이더 필 영역 그라데이션 글로우</summary>
    public static class PromptB104SettingsMenuBuilder
    {
        public const string MainPrefab = "Assets/_Project/Prefabs/UI/MainMenuPanel.prefab";
        public const string SurfacePrefab = "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        public const string ScenePath = "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string ArtPath = "Assets/_Project/Art/UI/MainMenu/Settings/setting-menu-asset.png";
        public const string CloseNormalPath = "Assets/_Project/Art/UI/MainMenu/Settings/setting-close-normal.png";
        public const string CloseHoverPath = "Assets/_Project/Art/UI/MainMenu/Settings/setting-close-hover.png";
        public const string ToggleOnPath = "Assets/_Project/Art/UI/MainMenu/Settings/toggle-active-on.png";
        public const string ToggleOffPath = "Assets/_Project/Art/UI/MainMenu/Settings/toggle-active-off.png";
        public const string ToggleHandlePath = "Assets/_Project/Art/UI/MainMenu/Settings/toggle-handle.png";
        public const string ButtonOffPath = "Assets/_Project/Art/UI/MainMenu/Settings/button-active-off.png";
        public const string ButtonOnPath = "Assets/_Project/Art/UI/MainMenu/Settings/button-active-on.png";
        public const string ButtonWideOffPath = "Assets/_Project/Art/UI/MainMenu/Settings/button-wide-off.png";
        public const string ButtonWideOnPath = "Assets/_Project/Art/UI/MainMenu/Settings/button-wide-on.png";
        public const string ParticleDotPath = "Assets/_Project/Art/UI/MainMenu/Settings/particle-dot.png";
        public const string SliderKnobPath = "Assets/_Project/Art/UI/MainMenu/Settings/slider-knob.png";
        public const string SliderGlowPath = "Assets/_Project/Art/UI/MainMenu/Settings/slider-glow.png";
        public const string SliderFillGlowPath = "Assets/_Project/Art/UI/MainMenu/Settings/slider-fill-glow.png";

        public static readonly Vector2 CardSize = new Vector2(1080, 810);
        public const float SwitchOnX = 16f;
        public const float SwitchOffX = -16f;
        public const float SwitchWidth = 62f;
        public const float SwitchHeight = 30f;
        private const string BuildFlag = "Temp/prompt-b104-build.flag";
        private const string BuildDone = "Temp/prompt-b104-build.done";
        private const string CaptureFlag = "Temp/prompt-b104-capture.flag";
        private const string CaptureDone = "Temp/prompt-b104-capture.done";
        private static readonly Color Ink = new Color(0.87f, 0.97f, 0.99f);
        private static readonly Color Cyan = new Color(0.29f, 0.88f, 0.95f);
        private static readonly Color MutedCyan = new Color(0.31f, 0.62f, 0.7f);
        private static bool watchingFlag;
        private static double captureStarted;

        [InitializeOnLoadMethod]
        private static void WatchFlag()
        {
            if (watchingFlag) return;
            watchingFlag = true;
            EditorApplication.update += PollFlag;
        }

        private static void PollFlag()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (File.Exists(BuildFlag))
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                    return;
                }
                File.Delete(BuildFlag);
                try
                {
                    Build();
                    File.WriteAllText(BuildDone, "ok " + DateTime.Now.ToString("o"));
                }
                catch (Exception exception)
                {
                    File.WriteAllText(BuildDone, exception.GetType().Name + ": " + exception.Message);
                    Debug.LogException(exception);
                }
                return;
            }

            const string StopFlag = "Temp/prompt-b104-stop.flag";
            if (File.Exists(StopFlag))
            {
                File.Delete(StopFlag);
                EditorApplication.isPlaying = false;
                File.WriteAllText("Temp/prompt-b104-stop.done", "stopped " + DateTime.Now.ToString("o"));
                return;
            }

            const string TestFlag = "Temp/prompt-b104-test.flag";
            const string TestDone = "Temp/prompt-b104-test.done";
            if (File.Exists(TestFlag))
            {
                File.Delete(TestFlag);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                    File.WriteAllText(TestFlag, "wait");
                    return;
                }
                try
                {
                    var testType = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.FullName == "SubTerra.App.Tests.UI.PromptB104SettingsMenuTests");
                    if (testType == null) throw new InvalidOperationException("Test type not found");
                    var instance = Activator.CreateInstance(testType);
                    var method = testType.GetMethod("Skin_PreservesExistingDraftControlsAndCancelEvents");
                    method.Invoke(instance, new object[] { MainPrefab });
                    method.Invoke(instance, new object[] { SurfacePrefab });
                    var navMethod = testType.GetMethod("Prompt104_4_MainMenuActionButtonsUseOffOnSpritesAndKeyboardNav");
                    if (navMethod != null) navMethod.Invoke(instance, null);
                    var footerMethod = testType.GetMethod("Prompt104_4_SettingsFooterNavAndFillGlow");
                    if (footerMethod != null)
                    {
                        footerMethod.Invoke(instance, new object[] { MainPrefab });
                        footerMethod.Invoke(instance, new object[] { SurfacePrefab });
                    }

                    var templateTestType = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.FullName == "SubTerra.App.Tests.UI.SurfaceBaseSettingsDropdownTemplateTests");
                    if (templateTestType != null)
                    {
                        var tInst = Activator.CreateInstance(templateTestType);
                        var blockerMethod = templateTestType.GetMethod("SettingsPanel_IsAFullScreenInputBlocker_WithAnOpaqueCentralCard");
                        blockerMethod.Invoke(tInst, new object[] { MainPrefab });
                        blockerMethod.Invoke(tInst, new object[] { SurfacePrefab });
                    }

                    File.WriteAllText(TestDone, "PASS: all edit mode tests passed " + DateTime.Now.ToString("o"));
                }
                catch (Exception ex)
                {
                    var actual = ex.InnerException ?? ex;
                    File.WriteAllText(TestDone, "FAIL: " + actual.GetType().Name + ": " + actual.Message + "\n" + actual.StackTrace);
                    Debug.LogException(actual);
                }
                return;
            }

            if (!File.Exists(CaptureFlag)) return;
            if (captureStarted <= 0d) captureStarted = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - captureStarted > 90d)
            {
                File.Delete(CaptureFlag);
                File.WriteAllText(CaptureDone, "timeout");
                captureStarted = 0d;
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
                return;
            }

            var binder = UnityEngine.Object.FindAnyObjectByType<MainMenuBinder>();
            if (binder == null || binder.Presenter == null) return;
            File.Delete(CaptureFlag);
            captureStarted = 0d;
            binder.Presenter.OpenSettings();
            EditorApplication.delayCall += () =>
            {
                ScreenCapture.CaptureScreenshot("Assets/Temp/prompt-b104-final.png");
                File.WriteAllText(CaptureDone, "ok " + DateTime.Now.ToString("o"));
                EditorApplication.isPlaying = false;
            };
        }

        [MenuItem("SubTerra/UI/Build Prompt-B 104 Settings Menu")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            SetupTextureImporters();

            foreach (string path in new[] { MainPrefab, SurfacePrefab })
            {
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Apply(prefab.GetComponent<MainMenuView>() as Component ?? prefab.GetComponent<SurfaceBaseView>());
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }

            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("Save existing Integration scene edits before running the builder.");
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var view = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SurfaceBaseView>(true))
                    .Single(v => v.name == "UndergroundSettings");
                Apply(view);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (!SceneManager.GetActiveScene().IsValid() || !SceneManager.GetActiveScene().isLoaded)
                    EditorSceneManager.OpenScene("Assets/_Project/Scenes/App/MainMenu.unity", OpenSceneMode.Single);
            }
            Debug.Log("[PromptB104] Settings menu updated in two prefabs and UndergroundSettings only.");
        }

        private static void SetupTextureImporters()
        {
            WriteFillGlowTexture();
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                var textureSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(textureSettings);
                importer.SaveAndReimport();
            }

            foreach (string path in new[]
            {
                CloseNormalPath, CloseHoverPath, ToggleOnPath, ToggleOffPath, ToggleHandlePath,
                ButtonOffPath, ButtonOnPath, ButtonWideOffPath, ButtonWideOnPath, ParticleDotPath,
                SliderKnobPath, SliderGlowPath, SliderFillGlowPath
            })
            {
                if (File.Exists(path)) AssetDatabase.ImportAsset(path);
                SetAsSprite(path, Vector4.zero);
            }
            AssetDatabase.Refresh();
        }

        private static void SetAsSprite(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (importer.spriteBorder != border) { importer.spriteBorder = border; dirty = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            if (textureSettings.spriteMeshType != SpriteMeshType.FullRect)
            {
                textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(textureSettings);
                dirty = true;
            }
            if (dirty) importer.SaveAndReimport();
        }

        private static void Apply(Component view)
        {
            var serialized = new SerializedObject(view);
            var root = ((GameObject)serialized.FindProperty("settingsRoot").objectReferenceValue).GetComponent<RectTransform>();
            root.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);
            var card = (RectTransform)root.Find("SettingsCard");
            Place(card, 0, -50, CardSize.x, CardSize.y);
            card.SetAsFirstSibling();
            var art = card.GetComponent<Image>();
            art.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
            art.type = Image.Type.Simple;
            art.preserveAspect = true;
            art.color = new Color(1f, 1f, 1f, 0.90f); // 104-1번: 투명도 10% 은은한 반투명 연출
            art.raycastTarget = true;

            var title = Find(root, "SettingsTitle").GetComponent<TMP_Text>();
            var font = title.font;
            Move(title.transform, card, 0, 365, 500, 60);
            StyleText(title, 40, TextAlignmentOptions.Center);
            Text(card, "Subtitle", "SYSTEM CONFIGURATION", 0, 321, 340, 24, 14, font, TextAlignmentOptions.Center).characterSpacing = 4;
            var headings = new[]
            {
                Text(card, "AudioTitle", "오디오", -244, 265, 170, 38, 26, font),
                Text(card, "DisplayTitle", "디스플레이", -219, 138, 220, 38, 26, font),
                Text(card, "SystemTitle", "시스템", -244, -86, 170, 38, 26, font),
                Text(card, "ControlsTitle", "조작", -244, -212, 170, 38, 26, font)
            };
            foreach (var heading in headings) heading.color = Cyan;
            var caption = Text(card, "VolumeCaption", "마스터 음량", -252, 210, 260, 34, 22, font);
            var volume = Reference<TMP_Text>(serialized, "masterVolumeLabel");
            Move(volume.transform, card, 354, 210, 60, 34);
            StyleText(volume, 21, TextAlignmentOptions.Right);
            volume.text = "50%";
            volume.color = Cyan;

            // 슬라이더: 필 영역만 위아래로 그라데이션 글로우. 안쪽 50% → 바깥 0%.
            var slider = Reference<Slider>(serialized, "masterVolumeSlider");
            Move(slider.transform, card, 113, 210, 362, 30);
            var track = slider.transform.Find("Background").GetComponent<Image>();
            Stretch(track.rectTransform, 0, 11, 0, 11);
            track.color = new Color(0.005f, 0.025f, 0.035f);
            Stroke(track.gameObject, new Color(0.17f, 0.38f, 0.43f));
            Stretch((RectTransform)slider.fillRect.parent, 3, 11, 3, 11);
            slider.fillRect.GetComponent<Image>().color = Cyan;
            DestroyChild(slider.transform, "SliderShadow");

            var fillGlow = slider.fillRect.Find("FillGlow") as RectTransform;
            if (fillGlow == null)
                fillGlow = new GameObject("FillGlow", typeof(RectTransform)).GetComponent<RectTransform>();
            fillGlow.SetParent(slider.fillRect, false);
            fillGlow.anchorMin = new Vector2(0f, 0.5f);
            fillGlow.anchorMax = new Vector2(1f, 0.5f);
            fillGlow.pivot = new Vector2(0.5f, 0.5f);
            fillGlow.anchoredPosition = Vector2.zero;
            fillGlow.sizeDelta = new Vector2(0f, 40f);
            fillGlow.localScale = Vector3.one;
            fillGlow.localRotation = Quaternion.identity;
            fillGlow.SetAsLastSibling();
            var fillGlowImg = Get<Image>(fillGlow.gameObject);
            fillGlowImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SliderFillGlowPath);
            fillGlowImg.type = Image.Type.Simple;
            fillGlowImg.preserveAspect = false;
            fillGlowImg.color = Color.white;
            fillGlowImg.raycastTarget = false;
            foreach (var oldFx in fillGlow.GetComponents<Shadow>())
                UnityEngine.Object.DestroyImmediate(oldFx);
            var oldOutline = fillGlow.GetComponent<Outline>();
            if (oldOutline != null) UnityEngine.Object.DestroyImmediate(oldOutline);

            // 슬라이더 노브 핸들: 가로/세로 동일한 1:1 원형(22x22)
            var handle = slider.handleRect.GetComponent<Image>();
            handle.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SliderKnobPath);
            handle.color = Color.white;
            handle.preserveAspect = true;
            Stretch((RectTransform)slider.handleRect.parent, 11, 4, 11, 4);
            slider.handleRect.sizeDelta = new Vector2(22, 0);
            var handleOutline = handle.GetComponent<Outline>();
            if (handleOutline != null) UnityEngine.Object.DestroyImmediate(handleOutline);

            Label(serialized, "resolutionLabel", card, -252, 86);
            Label(serialized, "frameRateLabel", card, -252, 35);
            Label(serialized, "languageLabel", card, -252, -144);
            Dropdown(Reference<TMP_Dropdown>(serialized, "resolutionDropdown"), card, 86);
            Dropdown(Reference<TMP_Dropdown>(serialized, "frameRateDropdown"), card, 35);
            Dropdown(Reference<TMP_Dropdown>(serialized, "languageDropdown"), card, -144);

            // 토글: 컨셉 on 스프라이트 + 회색 off 스프라이트, 원은 별도 핸들로 이동
            var group = Find(root, "ReduceMotionGroup");
            Move(group, card, 0, -17, 760, 36);
            var motionLabel = Reference<TMP_Text>(serialized, "reduceMotionLabel");
            Move(motionLabel.transform, group, -252, 0, 260, 34);
            StyleText(motionLabel, 22, TextAlignmentOptions.MidlineLeft);
            var toggle = Reference<Toggle>(serialized, "reduceMotionToggle");
            Move(toggle.transform, group, 347, 0, SwitchWidth, SwitchHeight);
            toggle.transition = Selectable.Transition.None;

            var switchGlowRect = Rect(toggle.transform, "SwitchGlow", 0, 0, 78, 42);
            switchGlowRect.SetAsFirstSibling();
            var switchGlowImg = Get<Image>(switchGlowRect.gameObject);
            switchGlowImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SliderGlowPath);
            switchGlowImg.type = Image.Type.Sliced;
            switchGlowImg.color = new Color(0.29f, 0.88f, 0.95f, 0.12f);
            switchGlowImg.raycastTarget = false;

            var switchTrack = toggle.targetGraphic as Image;
            Place(switchTrack.rectTransform, 0, 0, SwitchWidth, SwitchHeight);
            switchTrack.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ToggleOffPath);
            switchTrack.type = Image.Type.Simple;
            switchTrack.preserveAspect = true;
            switchTrack.color = Color.white;
            var switchTrackOutline = switchTrack.GetComponent<Outline>();
            if (switchTrackOutline != null) UnityEngine.Object.DestroyImmediate(switchTrackOutline);

            var onRect = Rect(switchTrack.transform, "SwitchOn", 0, 0, SwitchWidth, SwitchHeight);
            onRect.SetAsFirstSibling();
            var switchOnImg = Get<Image>(onRect.gameObject);
            switchOnImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ToggleOnPath);
            switchOnImg.type = Image.Type.Simple;
            switchOnImg.preserveAspect = true;
            switchOnImg.color = new Color(1f, 1f, 1f, 0f);
            switchOnImg.raycastTarget = false;

            if (toggle.graphic != null) toggle.graphic.gameObject.SetActive(false);
            toggle.graphic = null;
            var knob = Rect(switchTrack.transform, "SwitchHandle", SwitchOffX, 0, 22, 21);
            knob.SetAsLastSibling();
            var knobImage = Get<Image>(knob.gameObject);
            knobImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ToggleHandlePath);
            knobImage.color = Color.white;
            knobImage.preserveAspect = true;
            knobImage.raycastTarget = false;

            SkinSpriteButton(Button(card, "ChangeControls", "키 조작 변경", 129, -231, 520, 61, font),
                ButtonWideOffPath, ButtonWideOnPath, 520, 61);
            SkinSpriteButton(Reference<Button>(serialized, "settingsDefaultsButton"), card, -300, -348, 218, 56,
                ButtonOffPath, ButtonOnPath);
            SkinSpriteButton(Reference<Button>(serialized, "settingsCancelButton"), card, 30, -348, 234, 56,
                ButtonOffPath, ButtonOnPath);
            SkinSpriteButton(Reference<Button>(serialized, "settingsApplyButton"), card, 300, -348, 234, 56,
                ButtonOffPath, ButtonOnPath);
            var close = CloseButton(card, font);

            foreach (var name in new[] { "BgmHint", "ResolutionPrev", "ResolutionNext", "LanguageCycle" })
            {
                var old = Find(root, name);
                if (old != null) old.gameObject.SetActive(false);
            }

            var skin = Get<SettingsMenuSkin>(root.gameObject);
            var skinData = new SerializedObject(skin);
            Set(skinData, "card", card);
            Set(skinData, "closeButton", close);
            Set(skinData, "cancelButton", Reference<Button>(serialized, "settingsCancelButton"));
            Set(skinData, "reduceMotion", toggle);
            Set(skinData, "switchHandle", knob);
            Set(skinData, "switchTrack", switchTrack);
            Set(skinData, "switchOnImage", switchOnImg);
            Set(skinData, "switchGlow", switchGlowImg);
            Set(skinData, "volumeSlider", slider);
            Set(skinData, "volumeFillGlow", fillGlowImg);
            var shadowProp = skinData.FindProperty("volumeShadow");
            if (shadowProp != null) shadowProp.objectReferenceValue = null;
            var footer = skinData.FindProperty("footerButtons");
            footer.arraySize = 3;
            footer.GetArrayElementAtIndex(0).objectReferenceValue = Reference<Button>(serialized, "settingsDefaultsButton");
            footer.GetArrayElementAtIndex(1).objectReferenceValue = Reference<Button>(serialized, "settingsCancelButton");
            footer.GetArrayElementAtIndex(2).objectReferenceValue = Reference<Button>(serialized, "settingsApplyButton");
            Set(skinData, "volumeCaption", caption);
            skinData.FindProperty("particleSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(ParticleDotPath);
            skinData.FindProperty("switchOnX").floatValue = SwitchOnX;
            skinData.FindProperty("switchOffX").floatValue = SwitchOffX;
            var titles = skinData.FindProperty("sectionTitles");
            titles.arraySize = headings.Length;
            for (int i = 0; i < headings.Length; i++) titles.GetArrayElementAtIndex(i).objectReferenceValue = headings[i];
            skinData.ApplyModifiedPropertiesWithoutUndo();

            if (view is MainMenuView)
                SkinMainMenuActionButtons(view.transform);
        }

        private static void SkinMainMenuActionButtons(Transform root)
        {
            var content = root.Find("MenuContent");
            if (content == null) return;
            var names = new[] { "ContinueButton", "NewGameButton", "SettingsButton", "QuitButton" };
            foreach (var name in names)
            {
                var buttonTransform = content.Find(name);
                if (buttonTransform == null) continue;
                var button = Get<Button>(buttonTransform.gameObject);
                var rect = (RectTransform)buttonTransform;
                SkinSpriteButton(button, ButtonOffPath, ButtonOnPath, rect.sizeDelta.x, rect.sizeDelta.y);
            }
        }

        private static void Label(SerializedObject view, string field, RectTransform card, float x, float y)
        {
            var text = Reference<TMP_Text>(view, field);
            Move(text.transform, card, x, y, 260, 34);
            StyleText(text, 22, TextAlignmentOptions.MidlineLeft);
        }

        private static void Dropdown(TMP_Dropdown dropdown, RectTransform card, float y)
        {
            Move(dropdown.transform, card, 165, y, 440, 38);
            var image = dropdown.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PromptB103MainMenuBuilder.CutCornerPlatePath);
            image.type = Image.Type.Sliced;
            image.color = new Color(0.018f, 0.06f, 0.075f, 0.95f);
            Stroke(image.gameObject, new Color(0.23f, 0.52f, 0.6f));
            StyleText(dropdown.captionText, 20, TextAlignmentOptions.MidlineLeft);
            StyleText(dropdown.itemText, 20, TextAlignmentOptions.MidlineLeft);
            var caption = dropdown.captionText.rectTransform;
            caption.anchorMin = Vector2.zero;
            caption.anchorMax = Vector2.one;
            caption.offsetMin = new Vector2(16, 0);
            caption.offsetMax = new Vector2(-36, 0);
            DestroyChild(dropdown.transform, "ChevronLeft");
            DestroyChild(dropdown.transform, "ChevronRight");
            var arrow = Rect(dropdown.transform, "DropdownArrow", 196, 0, 12, 14);
            arrow.localRotation = Quaternion.Euler(0, 0, -90f);
            var raw = Get<RawImage>(arrow.gameObject);
            raw.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PromptB103MainMenuBuilder.TriangleArrowPath);
            raw.color = Ink;
            raw.raycastTarget = false;
            StyleDropdownList(dropdown);
        }

        private static void StyleDropdownList(TMP_Dropdown dropdown)
        {
            if (dropdown.template == null) return;

            // 104-1번: 드롭다운(프레임 포함) 템플릿이 아래 방향으로 펼쳐지도록 설정
            dropdown.template.anchorMin = new Vector2(0f, 0f);
            dropdown.template.anchorMax = new Vector2(1f, 0f);
            dropdown.template.pivot = new Vector2(0.5f, 1f);
            dropdown.template.anchoredPosition = new Vector2(0f, -2f);

            var image = dropdown.template.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.02f, 0.055f, 0.07f, 1f);
                Stroke(image.gameObject, Cyan);
            }
            var item = dropdown.template.GetComponentInChildren<Toggle>(true);
            if (item == null) return;
            var colors = item.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(0.18f, 0.62f, 0.7f, 0.85f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.1f, 0.4f, 0.46f, 0.9f);
            item.colors = colors;
        }

        private static Button CloseButton(RectTransform card, TMP_FontAsset font)
        {
            var rect = Rect(card, "SettingsClose", 429, 358, 46, 46);
            foreach (var name in new[] { "EdgeTop", "EdgeBottom", "EdgeLeft", "EdgeRight", "Label" })
                DestroyChild(rect, name);
            var outline = rect.GetComponent<Outline>();
            if (outline != null) UnityEngine.Object.DestroyImmediate(outline);

            // 104-2번: setting-menu.png의 사실적인 SF 메탈 질감 X버튼 전용 노멀 에셋 적용
            var image = Get<Image>(rect.gameObject);
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CloseNormalPath);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;

            // 호버 시 중앙에서부터 은은하게 퍼지는 청록 발광 X버튼 에셋 크로스페이드 오버레이
            var hoverOverlay = Rect(rect, "HoverOverlay", 0, 0, 46, 46);
            var hoverImg = Get<Image>(hoverOverlay.gameObject);
            hoverImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CloseHoverPath);
            hoverImg.type = Image.Type.Simple;
            hoverImg.preserveAspect = true;
            hoverImg.raycastTarget = false;

            var button = Get<Button>(rect.gameObject);
            button.targetGraphic = hoverImg;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.selectedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.95f, 1f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            colors.fadeDuration = 0.15f; // 호버 시 이미지 변경시간
            button.colors = colors;

            return button;
        }

        private static Button Button(RectTransform parent, string name, string label,
            float x, float y, float w, float h, TMP_FontAsset font)
        {
            var rect = Rect(parent, name, x, y, w, h);
            var button = Get<Button>(rect.gameObject);
            button.targetGraphic = Get<Image>(rect.gameObject);
            Text(rect, "Label", label, 0, 0, w - 12, h - 4, 22, font, TextAlignmentOptions.Center);
            return button;
        }

        private static void SkinSpriteButton(Button button, Transform parent, float x, float y, float w, float h,
            string offPath, string onPath)
        {
            Move(button.transform, parent, x, y, w, h);
            SkinSpriteButton(button, offPath, onPath, w, h);
        }

        private static void SkinSpriteButton(Button button, string offPath, string onPath, float w, float h)
        {
            foreach (var name in new[]
            {
                "EdgeTop", "EdgeBottom", "EdgeLeft", "EdgeRight",
                "Edge0", "Edge1", "Edge2", "Edge3", "InnerGlow", "HoverHalo", "EdgeGlow"
            })
                DestroyChild(button.transform, name);

            var oldOutline = button.GetComponent<Outline>();
            if (oldOutline != null) UnityEngine.Object.DestroyImmediate(oldOutline);
            var oldShadow = button.GetComponent<Shadow>();
            if (oldShadow != null) UnityEngine.Object.DestroyImmediate(oldShadow);

            var image = button.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(offPath);
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = true;

            var hover = Rect(button.transform, "HoverOverlay", 0, 0, w, h);
            var hoverImg = Get<Image>(hover.gameObject);
            hoverImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(onPath);
            hoverImg.type = Image.Type.Simple;
            hoverImg.preserveAspect = false;
            hoverImg.color = new Color(1f, 1f, 1f, 0f);
            hoverImg.raycastTarget = false;
            hover.SetAsLastSibling();

            var text = button.GetComponentInChildren<TMP_Text>(true);
            text.transform.SetAsLastSibling();
            Place(text.rectTransform, 0, 0, w - 12, h - 4);
            StyleText(text, 22, TextAlignmentOptions.Center);

            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            var skin = Get<MenuSpriteButtonSkin>(button.gameObject);
            var skinData = new SerializedObject(skin);
            skinData.FindProperty("overlay").objectReferenceValue = hoverImg;
            skinData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteFillGlowTexture()
        {
            const int width = 64;
            const int height = 64;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float vertical = Mathf.Abs(((y + 0.5f) / height) - 0.5f) * 2f;
                float verticalAlpha = 0.5f * 0.5f * (1f + Mathf.Cos(Mathf.Clamp01(vertical) * Mathf.PI));
                for (int x = 0; x < width; x++)
                {
                    float nx = (x + 0.5f) / width;
                    float edge = Mathf.Clamp01(Mathf.Min(nx, 1f - nx) / 0.08f);
                    float alpha = verticalAlpha * edge;
                    byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                    pixels[y * width + x] = new Color32(74, 224, 242, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(SliderFillGlowPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SliderFillGlowPath);
        }

        private static void Border(RectTransform parent, Color color, float width)
        {
            for (var i = 0; i < 4; i++)
            {
                var edge = Rect(parent, "Edge" + i, 0, 0, 0, 0);
                edge.anchorMin = i == 0 ? new Vector2(0, 1) : i == 3 ? new Vector2(1, 0) : Vector2.zero;
                edge.anchorMax = i == 1 ? new Vector2(1, 0) : i == 2 ? new Vector2(0, 1) : Vector2.one;
                edge.sizeDelta = i < 2 ? new Vector2(0, width) : new Vector2(width, 0);
                var image = Get<Image>(edge.gameObject);
                image.color = color;
                image.raycastTarget = false;
            }
        }

        private static TMP_Text Text(Transform parent, string name, string value, float x, float y,
            float width, float height, float size, TMP_FontAsset font, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var text = Get<TextMeshProUGUI>(Rect(parent, name, x, y, width, height).gameObject);
            text.font = font;
            text.text = value;
            StyleText(text, size, align);
            return text;
        }

        private static void StyleText(TMP_Text text, float size, TextAlignmentOptions align)
        {
            text.fontSize = size;
            text.enableAutoSizing = false;
            text.alignment = align;
            text.color = Ink;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void Stroke(GameObject obj, Color color)
        {
            var outline = Get<Outline>(obj);
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1, -1);
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = parent.Find(name) as RectTransform;
            if (rect == null) rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            Move(rect, parent, x, y, w, h);
            return rect;
        }

        private static void Move(Transform target, Transform parent, float x, float y, float w, float h)
        {
            target.SetParent(parent, false);
            Place((RectTransform)target, x, y, w, h);
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void DestroyChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        private static T Get<T>(GameObject obj) where T : Component => obj.GetComponent<T>() ?? obj.AddComponent<T>();
        private static T Reference<T>(SerializedObject view, string field) where T : UnityEngine.Object => (T)view.FindProperty(field).objectReferenceValue;
        private static void Set(SerializedObject obj, string field, UnityEngine.Object value) => obj.FindProperty(field).objectReferenceValue = value;
    }
}
