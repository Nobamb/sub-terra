using SubTerra.App.UI.SurfaceBase;
using SubTerra.App.Save;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>새 광산 버튼과 확인 모달을 Surface Base 프리팹에만 적용한다.</summary>
    public static class MineResetSurfaceBaseLayoutBuilder
    {
        public const string SurfaceBasePrefabPath =
            "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        public const string SurfaceBaseScenePath =
            "Assets/_Project/Scenes/App/SurfaceBase.unity";
        public const float ResetButtonY = 8f;
        public const float MessageY = -48f;
        public const string FramePath = "Assets/_Project/Art/UI/Gameplay/Quest/Clear/quest-clear-popup-frame.png";
        public const string ClosePath = "Assets/_Project/Art/UI/Gameplay/Quest/Clear/x-button.png";
        public const string MetalPath = "Assets/_Project/Art/UI/MainMenu/Settings/setting-menu-asset.png";
        public const string ButtonOffPath = "Assets/_Project/Art/UI/MainMenu/Settings/button-active-off.png";
        public const string ButtonOnPath = "Assets/_Project/Art/UI/MainMenu/Settings/button-active-on.png";

        [MenuItem("SubTerra/UI/Build Mine Reset (SurfaceBase only)")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SurfaceBasePrefabPath) == null)
            {
                return "SKIP: SurfaceBase prefab missing";
            }

            var root = PrefabUtility.LoadPrefabContents(SurfaceBasePrefabPath);
            try
            {
                var content = root.transform.Find("SurfaceBaseContent") ?? root.transform;
                var resetButton = content.Find("ResetMineButton")?.GetComponent<UnityEngine.UI.Button>() ?? EnsureButton(
                    content,
                    "ResetMineButton",
                    new Vector2(0f, ResetButtonY),
                    new Vector2(320f, 48f),
                    string.Format(LocalizationService.Get("mine_reset.button"), MineResetService.GetFeeGold(0)));

                var confirmRoot = EnsureConfirmModal(root.transform,
                    out var title,
                    out var body,
                    out var yes,
                    out var no,
                    out var close);

                var view = root.GetComponent<SurfaceBaseView>();
                if (view == null)
                {
                    view = root.AddComponent<SurfaceBaseView>();
                }

                var so = new SerializedObject(view);
                so.FindProperty("resetMineButton").objectReferenceValue = resetButton;
                so.FindProperty("resetMineConfirmRoot").objectReferenceValue = confirmRoot;
                so.FindProperty("resetMineConfirmTitleText").objectReferenceValue = title;
                so.FindProperty("resetMineConfirmBodyText").objectReferenceValue = body;
                so.FindProperty("resetMineConfirmYesButton").objectReferenceValue = yes;
                so.FindProperty("resetMineConfirmNoButton").objectReferenceValue = no;
                so.FindProperty("resetMineConfirmCloseButton").objectReferenceValue = close;
                so.ApplyModifiedPropertiesWithoutUndo();

                confirmRoot.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, SurfaceBasePrefabPath);
                return "SurfaceBasePrefab mine reset controls";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject EnsureConfirmModal(
            Transform root,
            out TMP_Text title,
            out TMP_Text body,
            out Button yes,
            out Button no,
            out UnityEngine.UI.Button close)
        {
            var existing = root.Find("ResetMineConfirm");
            var modal = existing != null
                ? existing.gameObject
                : new GameObject(
                    "ResetMineConfirm",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(GraphicRaycaster),
                    typeof(Image));
            if (existing == null)
            {
                modal.transform.SetParent(root, false);
            }

            var modalRect = modal.GetComponent<RectTransform>();
            Stretch(modalRect);
            var backdrop = modal.GetComponent<Image>();
            backdrop.color = new Color(0.01f, 0.015f, 0.025f, 0.88f);
            backdrop.raycastTarget = true;
            var canvas = modal.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 700;

            var card = EnsureChild(modal.transform, "ResetMineCard", typeof(Image));
            PlaceCentered(card, 0f, 1000f, 620f);
            var cardImage = card.GetComponent<Image>();
            cardImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);
            cardImage.type = UnityEngine.UI.Image.Type.Simple;
            cardImage.color = Color.white;
            cardImage.raycastTarget = true;
            // 이 모달은 700 고정 레이어다. 드래그의 자동 Canvas 승격으로 다른 모달을 덮지 않는다.
            var drag = card.GetComponent<SubTerra.App.UI.PopupWindowDrag>();
            if (drag != null) Object.DestroyImmediate(drag);

            var metal = EnsureChild(card, "MetalPanel", typeof(UnityEngine.UI.RawImage));
            Place(metal, new Vector2(0f, -10f), new Vector2(836f, 404f));
            metal.SetAsFirstSibling();
            var metalImage = metal.GetComponent<UnityEngine.UI.RawImage>();
            metalImage.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(MetalPath);
            // 기존 설정 이미지의 아이콘·장식이 없는 금속 영역만 사용한다. 새 스프라이트는 만들지 않는다.
            metalImage.uvRect = new Rect(0.3f, 0.035f, 0.4f, 0.04f);
            metalImage.color = Color.white;
            metalImage.raycastTarget = false;

            title = EnsureText(
                card,
                "Title",
                new Vector2(0f, 156f),
                new Vector2(640f, 48f),
                30f,
                LocalizationService.Get("mine_reset.confirm.title"));
            title.color = new Color(0.4f, 1f, 1f, 1f);
            body = EnsureText(
                card,
                "Body",
                new Vector2(0f, 9f),
                new Vector2(800f, 206f),
                20f,
                LocalizationService.Get("mine_reset.confirm.body"));
            body.alignment = TextAlignmentOptions.MidlineLeft;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Truncate;
            body.enableAutoSizing = false;

            yes = EnsureButton(
                card,
                "ConfirmButton",
                new Vector2(-130f, -158f),
                new Vector2(220f, 56f),
                LocalizationService.Get("mine_reset.confirm.yes"));
            no = EnsureButton(
                card,
                "CancelButton",
                new Vector2(130f, -158f),
                new Vector2(220f, 56f),
                LocalizationService.Get("mine_reset.confirm.no"));
            SkinConfirmButton(yes);
            SkinConfirmButton(no);

            var closeRect = EnsureChild(card, "Close", typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            Place(closeRect, new Vector2(365f, 156f), new Vector2(40f, 40f));
            var closeImage = closeRect.GetComponent<UnityEngine.UI.Image>();
            closeImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ClosePath);
            closeImage.color = Color.white;
            closeImage.preserveAspect = true;
            closeImage.raycastTarget = true;
            close = closeRect.GetComponent<UnityEngine.UI.Button>();
            close.targetGraphic = closeImage;
            close.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            var navigation = close.navigation;
            navigation.mode = UnityEngine.UI.Navigation.Mode.None;
            close.navigation = navigation;
            return modal;
        }

        private static void SkinConfirmButton(UnityEngine.UI.Button button)
        {
            var image = button.GetComponent<UnityEngine.UI.Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonOffPath);
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.color = Color.white;
            button.targetGraphic = image;
            button.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
            var sprites = button.spriteState;
            sprites.highlightedSprite = sprites.selectedSprite = sprites.pressedSprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(ButtonOnPath);
            sprites.disabledSprite = image.sprite;
            button.spriteState = sprites;
            var navigation = button.navigation;
            navigation.mode = UnityEngine.UI.Navigation.Mode.None;
            button.navigation = navigation;
        }

        private static Button EnsureButton(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            string label)
        {
            var rect = EnsureChild(parent, name, typeof(Image), typeof(Button));
            Place(rect, position, size);
            var image = rect.GetComponent<Image>();
            image.color = new Color(0.12f, 0.36f, 0.31f, 1f);
            image.raycastTarget = true;
            var button = rect.GetComponent<Button>();
            button.targetGraphic = image;
            EnsureText(rect, "Label", Vector2.zero, size - new Vector2(20f, 8f), 20f, label);
            return button;
        }

        private static RectTransform EnsureChild(Transform parent, string name, params System.Type[] components)
        {
            var found = parent.Find(name) as RectTransform;
            if (found == null)
            {
                var types = new System.Type[components.Length + 1];
                types[0] = typeof(RectTransform);
                components.CopyTo(types, 1);
                var go = new GameObject(name, types);
                go.transform.SetParent(parent, false);
                found = go.GetComponent<RectTransform>();
            }

            for (var i = 0; i < components.Length; i++)
            {
                if (found.GetComponent(components[i]) == null)
                {
                    found.gameObject.AddComponent(components[i]);
                }
            }

            return found;
        }

        private static TMP_Text EnsureText(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            float fontSize,
            string value)
        {
            var rect = EnsureChild(parent, name, typeof(TextMeshProUGUI));
            Place(rect, position, size);
            var text = rect.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static void PlaceCentered(RectTransform rect, float y, float width, float height)
        {
            if (rect != null)
            {
                Place(rect, new Vector2(0f, y), new Vector2(width, height));
            }
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
