using System;
using System.IO;
using System.Linq;
using SubTerra.App.UI;
using SubTerra.App.UI.HUD;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Editor.DataValidation
{
    public static class PromptB105SideMenuBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        public const string ArtFolder = "Assets/_Project/Art/UI/Gameplay/SideMenu/";
        private static readonly string[] Names = { "Open0", "Open1", "Open2", "Open3", "SettingsShortcut", "QuitShortcut" };
        private static readonly string[] Labels = { "시설 [B]", "인벤토리 [I]", "업그레이드 [U]", "게임 가이드 [G]", "설정 (Esc)", "게임 종료 (O)" };

        [InitializeOnLoadMethod]
        private static void WatchRequest()
        {
            EditorApplication.update += () =>
            {
                const string flag = "Temp/prompt-b105-build.flag";
                if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(flag)) return;
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete(flag);
                try { Build(); File.WriteAllText("Temp/prompt-b105-build.done", "PASS"); }
                catch (Exception ex) { File.WriteAllText("Temp/prompt-b105-build.done", ex.ToString()); Debug.LogException(ex); }
            };
        }

        [MenuItem("SubTerra/UI/Build Prompt-B 105 Gameplay Side Menu")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            ImportSprites();
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("Integration scene has unsaved changes.");
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var bar = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RectTransform>(true))
                    .Single(t => t.name == "PanelShortcutBar");
                // 기존 여섯 Button 인스턴스와 이벤트/외부 참조를 보존한다.
                var buttons = Names.Select(n => bar.Find(n).GetComponent<UnityEngine.UI.Button>()).ToArray();
                var host = bar.GetComponentInParent<GameplaySideMenuController>(true);
                if (host == null)
                {
                    var root = Rect("GameplaySideMenu", bar.parent);
                    root.SetSiblingIndex(bar.GetSiblingIndex());
                    host = root.gameObject.AddComponent<GameplaySideMenuController>();
                }
                var hostRect = (RectTransform)host.transform;
                Place(hostRect, new Vector2(315, 560), Vector2.zero, Vector2.one);
                var group = host.GetComponent<CanvasGroup>();
                if (group == null) group = host.gameObject.AddComponent<CanvasGroup>();
                var open = Rect("OpenMenuRoot", hostRect);
                Place(open, new Vector2(315, 560), new Vector2(0, -48), Vector2.one);
                var closed = Rect("ClosedMenuRoot", hostRect);
                Place(closed, new Vector2(90, 270), new Vector2(0, -24), Vector2.one);
                var background = Picture("MenuBackground", open, "game-menu.png");
                Stretch(background.rectTransform);
                background.transform.SetAsFirstSibling();
                var closedBackground = Picture("MenuBackground", closed, "menu-close-state.png");
                Stretch(closedBackground.rectTransform);
                closedBackground.transform.SetAsFirstSibling();
                // 가장자리의 투명 패딩 때문에 실제 프레임과 화면 경계 사이가 벌어지지 않게 한다.
                background.rectTransform.offsetMax = new Vector2(2, 0);
                closedBackground.rectTransform.offsetMax = new Vector2(3, 0);
                background.raycastTarget = closedBackground.raycastTarget = true;
                bar.SetParent(open, false);
                Stretch(bar);
                for (int i = 0; i < buttons.Length; i++)
                {
                    var button = buttons[i];
                    Place((RectTransform)button.transform, new Vector2(270, 70), new Vector2(-22, -65 - i * 77), Vector2.one);
                    Skin(button, "menu-button-active-off.png", "menu-button-active-on.png");
                    // 원본의 투명 상하 패딩은 보존하되 클릭 영역은 이웃 버튼과 겹치지 않는다.
                    foreach (string imageName in new[] { "NormalImage", "HoverImage" })
                        Place((RectTransform)button.transform.Find(imageName), new Vector2(270, 89.91f), Vector2.zero, new Vector2(0.5f, 0.5f));
                    var label = button.GetComponentInChildren<TMP_Text>(true);
                    label.text = Labels[i];
                    label.fontSize = 21f;
                    label.enableAutoSizing = false;
                    label.alignment = TextAlignmentOptions.MidlineLeft;
                    label.color = new Color(0.9f, 0.98f, 1f);
                    label.raycastTarget = false;
                    Stretch(label.rectTransform);
                    label.rectTransform.offsetMin = new Vector2(82, 0);
                    label.rectTransform.offsetMax = new Vector2(-8, 0);
                    var iconRect = Rect("MenuIcon", button.transform);
                    Place(iconRect, new Vector2(32, 32), new Vector2(32, -19), new Vector2(0, 1));
                    var icon = iconRect.GetComponent<SideMenuIcon>();
                    if (icon == null) icon = iconRect.gameObject.AddComponent<SideMenuIcon>();
                    icon.color = new Color(0.25f, 0.94f, 0.98f);
                    icon.raycastTarget = false;
                    var iconSo = new SerializedObject(icon);
                    iconSo.FindProperty("kind").intValue = i;
                    iconSo.ApplyModifiedPropertiesWithoutUndo();
                    label.transform.SetAsLastSibling();
                }
                Toggle(open, host, new Vector2(0, 0), 52);
                Toggle(closed, host, new Vector2(-14, -126), 54);
                Set(host, "openRoot", open); Set(host, "closedRoot", closed); Set(host, "inputGroup", group);
                open.gameObject.SetActive(true);
                closed.gameObject.SetActive(false);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static void Toggle(RectTransform parent, GameplaySideMenuController host, Vector2 position, float size)
        {
            var rect = Rect("MenuToggleButton", parent);
            Place(rect, new Vector2(size, size), position, Vector2.one);
            var button = rect.GetComponent<UnityEngine.UI.Button>();
            if (button == null) button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            Skin(button, "menu-close-off.png", "menu-close-on.png");
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, host.Toggle);
        }

        private static void Skin(UnityEngine.UI.Button button, string off, string on)
        {
            var hitArea = button.GetComponent<UnityEngine.UI.Image>();
            if (hitArea == null) hitArea = button.gameObject.AddComponent<UnityEngine.UI.Image>();
            hitArea.sprite = null;
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;
            button.targetGraphic = hitArea;
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(button);
            var overlay = Picture("HoverImage", button.transform, on);
            Stretch(overlay.rectTransform);
            overlay.color = new Color(1, 1, 1, 0);
            overlay.transform.SetAsFirstSibling();
            var normal = Picture("NormalImage", button.transform, off);
            Stretch(normal.rectTransform);
            normal.transform.SetAsFirstSibling();
            var skin = button.GetComponent<SideMenuButtonView>();
            if (skin == null) skin = button.gameObject.AddComponent<SideMenuButtonView>();
            Set(skin, "normal", normal); Set(skin, "hover", overlay);
        }

        private static void ImportSprites()
        {
            foreach (var file in new[] { "game-menu.png", "menu-close-state.png", "menu-button-active-off.png",
                "menu-button-active-on.png", "menu-close-off.png", "menu-close-on.png" })
            {
                string path = ArtFolder + file;
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var existing = parent.Find(name);
            if (existing != null) return (RectTransform)existing;
            var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            result.SetParent(parent, false);
            result.gameObject.layer = parent.gameObject.layer;
            return result;
        }
        private static UnityEngine.UI.Image Picture(string name, Transform parent, string asset)
        {
            var rect = Rect(name, parent);
            var picture = rect.GetComponent<UnityEngine.UI.Image>();
            if (picture == null) picture = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            picture.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + asset);
            picture.color = Color.white;
            picture.raycastTarget = false;
            return picture;
        }
        private static void Place(RectTransform rect, Vector2 size, Vector2 position, Vector2 anchor)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
        private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
