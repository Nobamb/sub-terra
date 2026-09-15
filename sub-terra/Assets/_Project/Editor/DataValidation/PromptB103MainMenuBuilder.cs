using SubTerra.App.UI.MainMenu;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>103번 메인 메뉴만 수정한다. 설정/덮어쓰기 팝업과 기존 배선은 유지한다.</summary>
    public static class PromptB103MainMenuBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/UI/MainMenuPanel.prefab";
        public const string BackgroundPath = "Assets/_Project/Art/UI/MainMenu/MainMenu_Background.png";

        [MenuItem("SubTerra/UI/Build Prompt-B 103 Main Menu")]
        public static void Build()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var content = (RectTransform)root.transform.Find("MenuContent");
                Place(content, 0, 0, 1000, 1000);
                var view = new SerializedObject(root.GetComponent<MainMenuView>());
                view.FindProperty("menuContent").objectReferenceValue = content;
                view.ApplyModifiedPropertiesWithoutUndo();
                var plate = content.GetComponent<UnityEngine.UI.Image>();
                if (plate != null)
                {
                    plate.color = Color.clear;
                    plate.raycastTarget = false;
                }
                var rootImage = root.GetComponent<UnityEngine.UI.Image>();
                if (rootImage != null) rootImage.color = Hex("061117");
                var title = content.Find("Title").GetComponent<TMP_Text>();
                Place(title.rectTransform, 0, 370, 940, 176);
                title.text = "Sub-Terra";
                title.fontSize = 104;
                title.fontStyle = FontStyles.Bold;
                title.characterSpacing = 6;
                title.color = Hex("EEF4F5");
                Text(content, "DepthCopy", "지금은 40미터다.", 0, 285, 760, 58, 29, title.font);
                Text(content, "SignalCopy", "봉인 너머에서 신호가 온다.", 0, 238, 760, 36, 22, title.font).color = Hex("91BDC5");

                for (var i = 0; i < 3; i++)
                {
                    var slot = (RectTransform)content.Find("Slot" + (i + 1));
                    Place(slot, 0, 134 - i * 125, 800, 106);
                    StyleButton(slot, false);
                    var label = slot.Find("Label").GetComponent<TMP_Text>();
                    Place(label.rectTransform, 93, 0, 520, 92);
                    label.fontSize = 23;
                    label.alignment = TextAlignmentOptions.MidlineLeft;
                    label.color = Hex("EEF4F5");
                    label.text = "탐사 기록 " + (i + 1).ToString("00") + "\n<size=75%>— 기록 없음 —</size>";
                    var thumbnail = Rect(slot, "Thumbnail", -266, 0, 160, 90);
                    var raw = Get<UnityEngine.UI.RawImage>(thumbnail);
                    raw.color = Hex("102630");
                    raw.raycastTarget = false;
                    var placeholder = Text(thumbnail, "Placeholder", "NO VISUAL\nRECORD", 0, 0, 154, 80, 15, title.font);
                    placeholder.color = Hex("91A7AE");
                    var selection = Rect(slot, "Selection", 0, 0, 800, 106);
                    var group = Get<CanvasGroup>(selection);
                    group.alpha = i == 0 ? 1 : 0;
                    group.blocksRaycasts = false;
                    Border(selection, Hex("39D8EA"), 2);
                    Text(selection, "Selector", "›", -376, 0, 32, 64, 37, title.font).color = Hex("39D8EA");
                    var card = Get<SaveSlotCardView>(slot);
                    var so = new SerializedObject(card);
                    so.FindProperty("thumbnail").objectReferenceValue = raw;
                    so.FindProperty("placeholder").objectReferenceValue = placeholder.gameObject;
                    so.FindProperty("selection").objectReferenceValue = group;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                var names = new[] { "ContinueButton", "NewGameButton", "SettingsButton", "QuitButton" };
                for (var i = 0; i < names.Length; i++)
                {
                    var button = (RectTransform)content.Find(names[i]);
                    Place(button, i == 3 ? 0 : (i - 1) * 274, i == 3 ? -318 : -224, i == 3 ? 256 : 252, i == 3 ? 58 : 70);
                    StyleButton(button, i == 0);
                    var label = button.Find("Label").GetComponent<TMP_Text>();
                    label.fontSize = i == 3 ? 24 : 28;
                    label.color = Hex("EEF4F5");
                }
                Place(content.Find("MessageText") as RectTransform, 0, -399, 900, 56);
                var message = content.Find("MessageText").GetComponent<TMP_Text>();
                message.fontSize = 22;
                message.color = Hex("C7DFE3");
                var version = content.Find("VersionText").GetComponent<TMP_Text>();
                Place(version.rectTransform, 0, -462, 850, 32);
                version.fontSize = 18;
                version.color = new Color(0.57f, 0.65f, 0.68f, 0.7f);

                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
                if (texture != null)
                {
                    var background = Rect(root.transform, "Background", 0, 0, 1920, 1080);
                    background.SetAsFirstSibling();
                    var raw = Get<UnityEngine.UI.RawImage>(background);
                    raw.texture = texture;
                    raw.raycastTarget = false;
                    raw.color = Color.white;
                    var fit = Get<UnityEngine.UI.AspectRatioFitter>(background);
                    fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
                    fit.aspectRatio = (float)texture.width / texture.height;
                    Get<MenuSignalPulse>(background);
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void StyleButton(RectTransform rect, bool primary)
        {
            var image = rect.GetComponent<UnityEngine.UI.Image>();
            image.sprite = null;
            image.color = Color.white;
            image.raycastTarget = true;
            var button = rect.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = primary ? Hex("194652", 0.96f) : Hex("102630", 0.94f);
            colors.highlightedColor = Hex("245464");
            colors.selectedColor = Hex("20434F");
            colors.pressedColor = Hex("0A2029");
            colors.disabledColor = Hex("102028", 0.72f);
            colors.fadeDuration = 0.15f;
            colors.colorMultiplier = 1;
            button.colors = colors;
            Border(rect, primary ? Hex("59BCCA") : Hex("39636F"), 1);
        }

        private static void Border(RectTransform parent, Color color, float width)
        {
            for (var i = 0; i < 4; i++)
            {
                var edge = Rect(parent, "Edge" + i, 0, 0, 0, 0);
                edge.anchorMin = i == 0 ? new Vector2(0, 1) : i == 3 ? new Vector2(1, 0) : Vector2.zero;
                edge.anchorMax = i == 1 ? new Vector2(1, 0) : i == 2 ? new Vector2(0, 1) : Vector2.one;
                edge.sizeDelta = i < 2 ? new Vector2(0, width) : new Vector2(width, 0);
                var image = Get<UnityEngine.UI.Image>(edge);
                image.color = color;
                image.raycastTarget = false;
            }
        }

        private static TMP_Text Text(Transform parent, string name, string value, float x, float y, float w, float h, float size, TMP_FontAsset font)
        {
            var text = Get<TextMeshProUGUI>(Rect(parent, name, x, y, w, h));
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = Hex("EEF4F5");
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = parent.Find(name) as RectTransform;
            if (rect == null)
            {
                rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(parent, false);
            }
            Place(rect, x, y, w, h);
            return rect;
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private static T Get<T>(Component target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.gameObject.AddComponent<T>();
        }

        private static Color Hex(string value, float alpha = 1)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var color);
            color.a = alpha;
            return color;
        }
    }
}
