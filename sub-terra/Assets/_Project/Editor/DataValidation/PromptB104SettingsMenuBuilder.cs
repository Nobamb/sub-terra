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
    /// <summary>104번: 지정된 세 위치의 Settings 하위만 수정한다.</summary>
    public static class PromptB104SettingsMenuBuilder
    {
        public const string MainPrefab = "Assets/_Project/Prefabs/UI/MainMenuPanel.prefab";
        public const string SurfacePrefab = "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        public const string ScenePath = "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string ArtPath = "Assets/_Project/Art/UI/MainMenu/Settings/setting-menu-asset.png";
        public static readonly Vector2 CardSize = new Vector2(1080, 810);
        public const float SwitchOnX = 18f;
        public const float SwitchOffX = -18f;
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
            };
        }

        [MenuItem("SubTerra/UI/Build Prompt-B 104 Settings Menu")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath);
            if (importer == null) throw new InvalidOperationException("Missing settings layout asset: " + ArtPath);
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
            }
            Debug.Log("[PromptB104] Settings menu updated in two prefabs and UndergroundSettings only.");
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
            art.color = Color.white;
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
            var slider = Reference<Slider>(serialized, "masterVolumeSlider");
            Move(slider.transform, card, 113, 210, 362, 30);
            var track = slider.transform.Find("Background").GetComponent<Image>();
            Stretch(track.rectTransform, 0, 11, 0, 11);
            track.color = new Color(0.005f, 0.025f, 0.035f);
            Stroke(track.gameObject, new Color(0.17f, 0.38f, 0.43f));
            Stretch((RectTransform)slider.fillRect.parent, 3, 11, 3, 11);
            slider.fillRect.GetComponent<Image>().color = Cyan;
            var handle = slider.handleRect.GetComponent<Image>();
            handle.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            handle.color = new Color(0.86f, 0.97f, 1f);
            Stretch((RectTransform)slider.handleRect.parent, 12, 3, 12, 3);
            slider.handleRect.sizeDelta = new Vector2(22, 0);
            Stroke(handle.gameObject, Ink);

            Label(serialized, "resolutionLabel", card, -252, 86);
            Label(serialized, "frameRateLabel", card, -252, 35);
            Label(serialized, "languageLabel", card, -252, -144);
            Dropdown(Reference<TMP_Dropdown>(serialized, "resolutionDropdown"), card, 86);
            Dropdown(Reference<TMP_Dropdown>(serialized, "frameRateDropdown"), card, 35);
            Dropdown(Reference<TMP_Dropdown>(serialized, "languageDropdown"), card, -144);

            var group = Find(root, "ReduceMotionGroup");
            Move(group, card, 0, -17, 760, 36);
            var motionLabel = Reference<TMP_Text>(serialized, "reduceMotionLabel");
            Move(motionLabel.transform, group, -252, 0, 260, 34);
            StyleText(motionLabel, 22, TextAlignmentOptions.MidlineLeft);
            var toggle = Reference<Toggle>(serialized, "reduceMotionToggle");
            Move(toggle.transform, group, 347, 0, 72, 30);
            toggle.transition = Selectable.Transition.None;
            var switchTrack = toggle.targetGraphic as Image;
            Place(switchTrack.rectTransform, 0, 0, 72, 30);
            switchTrack.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PromptB103MainMenuBuilder.CutCornerPlatePath);
            switchTrack.type = Image.Type.Sliced;
            switchTrack.pixelsPerUnitMultiplier = 1f;
            switchTrack.color = new Color(0.05f, 0.18f, 0.22f, 1f);
            Stroke(switchTrack.gameObject, Cyan);
            if (toggle.graphic != null) toggle.graphic.gameObject.SetActive(false);
            toggle.graphic = null;
            var knob = Rect(switchTrack.transform, "SwitchHandle", SwitchOffX, 0, 22, 22);
            var knobImage = Get<Image>(knob.gameObject);
            knobImage.sprite = handle.sprite;
            knobImage.color = new Color(0.55f, 0.82f, 0.88f);
            knobImage.raycastTarget = false;

            Button(card, "ChangeControls", "키 조작 변경", 129, -231, 520, 50, font);
            SkinButton(Reference<Button>(serialized, "settingsDefaultsButton"), card, -300, -348, 218, 56);
            SkinButton(Reference<Button>(serialized, "settingsCancelButton"), card, 30, -348, 234, 56);
            SkinButton(Reference<Button>(serialized, "settingsApplyButton"), card, 300, -348, 234, 56, true);
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
            Set(skinData, "volumeCaption", caption);
            var titles = skinData.FindProperty("sectionTitles");
            titles.arraySize = headings.Length;
            for (int i = 0; i < headings.Length; i++) titles.GetArrayElementAtIndex(i).objectReferenceValue = headings[i];
            skinData.ApplyModifiedPropertiesWithoutUndo();
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
            foreach (var name in new[] { "EdgeTop", "EdgeBottom", "EdgeLeft", "EdgeRight" })
                DestroyChild(rect, name);
            var outline = rect.GetComponent<Outline>();
            if (outline != null) UnityEngine.Object.DestroyImmediate(outline);
            var image = Get<Image>(rect.gameObject);
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PromptB103MainMenuBuilder.CutCornerPlatePath);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            var button = Get<Button>(rect.gameObject);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(0.22f, 0.72f, 0.8f, 0.22f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.18f, 0.55f, 0.62f, 0.12f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            button.colors = colors;
            var label = Text(rect, "Label", "×", 0, 2, 46, 46, 30, font, TextAlignmentOptions.Center);
            label.color = Cyan;
            return button;
        }

        private static Button Button(RectTransform parent, string name, string label,
            float x, float y, float w, float h, TMP_FontAsset font)
        {
            var rect = Rect(parent, name, x, y, w, h);
            var button = Get<Button>(rect.gameObject);
            button.targetGraphic = Get<Image>(rect.gameObject);
            Text(rect, "Label", label, 0, 0, w - 12, h - 4, 22, font, TextAlignmentOptions.Center);
            SkinButton(button, parent, x, y, w, h);
            return button;
        }

        private static void SkinButton(Button button, Transform parent, float x, float y, float w, float h, bool primary = false)
        {
            Move(button.transform, parent, x, y, w, h);
            foreach (var name in new[] { "EdgeTop", "EdgeBottom", "EdgeLeft", "EdgeRight" })
                DestroyChild(button.transform, name);
            var image = button.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PromptB103MainMenuBuilder.CutCornerPlatePath);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            var colors = button.colors;
            colors.normalColor = primary ? new Color(0.05f, 0.38f, 0.46f, 0.98f) : new Color(0.04f, 0.11f, 0.15f, 0.88f);
            colors.highlightedColor = new Color(0.1f, 0.42f, 0.5f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.02f, 0.16f, 0.2f, 1f);
            colors.disabledColor = new Color(0.12f, 0.16f, 0.18f, 0.45f);
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;
            Stroke(image.gameObject, primary ? Cyan : MutedCyan);
            var text = button.GetComponentInChildren<TMP_Text>(true);
            Place(text.rectTransform, 0, 0, w - 12, h - 4);
            StyleText(text, 22, TextAlignmentOptions.Center);
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
