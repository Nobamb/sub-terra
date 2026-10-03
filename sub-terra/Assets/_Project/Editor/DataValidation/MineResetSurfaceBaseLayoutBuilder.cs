using SubTerra.App.UI.MainMenu;
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
        public const string MetalPath = "Assets/_Project/Art/UI/MainMenu/Settings/setting-menu-asset.png";
        public const string ButtonOffPath = "Assets/_Project/Art/UI/MainMenu/Settings/button-active-off.png";
        public const string ButtonOnPath = "Assets/_Project/Art/UI/MainMenu/Settings/button-active-on.png";
        public const string CostPlatePath = "Assets/_Project/Art/UI/Gameplay/Quest/quest-reward-plate.png";
        public const string GoldIconPath = "Assets/_Project/Art/UI/SurfaceBase/Icons/icon-gold.png";
        public const string ResetIconPath = "Assets/_Project/Art/UI/SurfaceBase/Icons/icon-reset.png";
        public const string KeepIconPath = "Assets/_Project/Art/UI/SurfaceBase/Icons/icon-cargo.png";
        public const string TimerIconPath = "Assets/_Project/Art/UI/SurfaceBase/Icons/icon-mine.png";

        // 카드(1000x620) 로컬 좌표. 프레임 아트의 구분선(y +115, -97) 사이에 본문, 아래에 버튼을 둔다.
        public const float ContentWidth = 560f;
        public const float TitleY = 156f;
        public const float DescriptionY = 94f;
        public const float CostPanelY = 34f;
        public const float CostPanelHeight = 80f;
        public const float RowHeight = 24f;
        public const float RowFirstY = -26f;
        public const float RowPitch = 26f;
        public const float ButtonY = -155f;
        public const float ButtonHeight = 52f;
        public const float CancelButtonWidth = 220f;
        public const float ConfirmButtonWidth = 310f;
        public const float ButtonGap = 30f;

        private static readonly Color Cyan = new Color(0.4f, 1f, 1f, 1f);
        private static readonly Color DescriptionColor = new Color(0.72f, 0.84f, 0.88f, 1f);
        private static readonly Color RowDescColor = new Color(0.86f, 0.93f, 0.95f, 1f);

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

                var confirmRoot = EnsureConfirmModal(root.transform, out var refs);

                var view = root.GetComponent<SurfaceBaseView>();
                if (view == null)
                {
                    view = root.AddComponent<SurfaceBaseView>();
                }

                var so = new SerializedObject(view);
                so.FindProperty("resetMineButton").objectReferenceValue = resetButton;
                so.FindProperty("resetMineConfirmRoot").objectReferenceValue = confirmRoot;
                so.FindProperty("resetMineConfirmTitleText").objectReferenceValue = refs.Title;
                so.FindProperty("resetMineConfirmBodyText").objectReferenceValue = refs.Description;
                so.FindProperty("resetMineConfirmCostText").objectReferenceValue = refs.Cost;
                so.FindProperty("resetMineConfirmBalanceText").objectReferenceValue = refs.Balance;
                so.FindProperty("resetMineConfirmYesButton").objectReferenceValue = refs.Yes;
                so.FindProperty("resetMineConfirmNoButton").objectReferenceValue = refs.No;
                so.FindProperty("resetMineConfirmAccentRoot").objectReferenceValue = refs.AccentRoot;
                AssignArray(so.FindProperty("resetMineConfirmRowTitleTexts"), refs.RowTitles);
                AssignArray(so.FindProperty("resetMineConfirmRowDescTexts"), refs.RowDescs);
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

        private sealed class ConfirmRefs
        {
            public TMP_Text Title;
            public TMP_Text Description;
            public TMP_Text Cost;
            public TMP_Text Balance;
            public TMP_Text[] RowTitles;
            public TMP_Text[] RowDescs;
            public Button Yes;
            public Button No;
            public GameObject AccentRoot;
        }

        private static void AssignArray(SerializedProperty property, TMP_Text[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static GameObject EnsureConfirmModal(Transform root, out ConfirmRefs refs)
        {
            refs = new ConfirmRefs();
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

            // 상단 X 버튼과 장문 Body 설명은 이번 개편에서 제거한다. 남은 흔적이 없도록 지운다.
            DestroyChild(card, "Close");
            DestroyChild(card, "Body");

            var metal = EnsureChild(card, "MetalPanel", typeof(UnityEngine.UI.RawImage));
            Place(metal, new Vector2(0f, -10f), new Vector2(836f, 404f));
            metal.SetAsFirstSibling();
            var metalImage = metal.GetComponent<UnityEngine.UI.RawImage>();
            metalImage.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(MetalPath);
            // 기존 설정 이미지의 아이콘·장식이 없는 금속 영역만 사용한다. 새 스프라이트는 만들지 않는다.
            metalImage.uvRect = new Rect(0.3f, 0.035f, 0.4f, 0.04f);
            // 세로 결무늬가 글자를 방해하지 않도록 밝기를 낮춘다.
            metalImage.color = new Color(0.55f, 0.6f, 0.62f, 1f);
            metalImage.raycastTarget = false;

            refs.Title = EnsureText(
                card,
                "Title",
                new Vector2(0f, TitleY),
                new Vector2(640f, 48f),
                30f,
                LocalizationService.Get("mine_reset.confirm.title"));
            refs.Title.color = Cyan;

            refs.Description = EnsureText(
                card,
                "Description",
                new Vector2(0f, DescriptionY),
                new Vector2(ContentWidth + 120f, 30f),
                20f,
                LocalizationService.Get("mine_reset.confirm.desc"));
            refs.Description.color = DescriptionColor;
            refs.Description.enableAutoSizing = true;
            refs.Description.fontSizeMin = 15f;
            refs.Description.fontSizeMax = 20f;
            refs.Description.textWrappingMode = TextWrappingModes.NoWrap;

            BuildCostPanel(card, refs);
            BuildInfoRows(card, refs);

            refs.Yes = EnsureButton(
                card,
                "ConfirmButton",
                new Vector2(ContentWidth / 2f - ConfirmButtonWidth / 2f, ButtonY),
                new Vector2(ConfirmButtonWidth, ButtonHeight),
                string.Format(LocalizationService.Get("mine_reset.confirm.create"), MineResetService.GetFeeGold(0)));
            refs.No = EnsureButton(
                card,
                "CancelButton",
                new Vector2(-ContentWidth / 2f + CancelButtonWidth / 2f, ButtonY),
                new Vector2(CancelButtonWidth, ButtonHeight),
                LocalizationService.Get("mine_reset.confirm.no"));
            refs.AccentRoot = SkinConfirmButton(refs.Yes, primary: true);
            SkinConfirmButton(refs.No, primary: false);
            return modal;
        }

        private static void BuildCostPanel(Transform card, ConfirmRefs refs)
        {
            var panel = EnsureChild(card, "CostPanel", typeof(Image));
            Place(panel, new Vector2(0f, CostPanelY), new Vector2(ContentWidth, CostPanelHeight));
            var plate = panel.GetComponent<Image>();
            plate.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CostPlatePath);
            plate.type = Image.Type.Sliced;
            plate.color = Color.white;
            plate.raycastTarget = false;

            var amountRow = EnsureChild(panel, "AmountRow", typeof(HorizontalLayoutGroup));
            Place(amountRow, new Vector2(0f, 15f), new Vector2(ContentWidth - 60f, 46f));
            var layout = amountRow.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var gold = EnsureChild(amountRow, "GoldIcon", typeof(Image), typeof(LayoutElement));
            // 높이는 레이아웃이 아닌 각자의 rect가 정한다. 폭만 그룹이 맡는다.
            Place(gold, Vector2.zero, new Vector2(40f, 40f));
            var goldImage = gold.GetComponent<Image>();
            goldImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldIconPath);
            goldImage.preserveAspect = true;
            goldImage.raycastTarget = false;
            var goldLayout = gold.GetComponent<LayoutElement>();
            goldLayout.preferredWidth = 40f;
            goldLayout.preferredHeight = 40f;

            refs.Cost = EnsureText(
                amountRow,
                "CostText",
                Vector2.zero,
                new Vector2(200f, 54f),
                36f,
                string.Format(LocalizationService.Get("mine_reset.confirm.cost"), MineResetService.GetFeeGold(0)));
            refs.Cost.color = new Color(1f, 0.9f, 0.42f, 1f);
            refs.Cost.fontStyle = FontStyles.Bold;
            refs.Cost.textWrappingMode = TextWrappingModes.NoWrap;
            refs.Cost.alignment = TextAlignmentOptions.MidlineLeft;

            refs.Balance = EnsureText(
                panel,
                "BalanceText",
                new Vector2(0f, -22f),
                new Vector2(ContentWidth - 60f, 24f),
                17f,
                string.Empty);
            refs.Balance.color = new Color(0.78f, 0.88f, 0.9f, 1f);
            refs.Balance.enableAutoSizing = true;
            refs.Balance.fontSizeMin = 13f;
            refs.Balance.fontSizeMax = 17f;
            refs.Balance.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void BuildInfoRows(Transform card, ConfirmRefs refs)
        {
            var iconPaths = new[] { ResetIconPath, KeepIconPath, TimerIconPath };
            var titleKeys = new[]
            {
                "mine_reset.confirm.reset.title",
                "mine_reset.confirm.keep.title",
                "mine_reset.confirm.timer.title"
            };
            var descKeys = new[]
            {
                "mine_reset.confirm.reset.desc",
                "mine_reset.confirm.keep.desc",
                "mine_reset.confirm.timer.desc"
            };
            var names = new[] { "ResetRow", "KeepRow", "TimerRow" };
            refs.RowTitles = new TMP_Text[3];
            refs.RowDescs = new TMP_Text[3];

            // 세 열(아이콘·제목·설명)의 시작 x를 모든 행에서 같게 둔다.
            const float iconSize = 26f;
            var left = -ContentWidth / 2f;
            var iconX = left + iconSize / 2f;
            const float titleWidth = 100f;
            var titleX = left + 40f + titleWidth / 2f;
            var descLeft = left + 40f + titleWidth + 12f;
            var descWidth = ContentWidth / 2f - descLeft;
            var descX = descLeft + descWidth / 2f;

            for (var i = 0; i < 3; i++)
            {
                var row = EnsureChild(card, names[i]);
                Place(row, new Vector2(0f, RowFirstY - RowPitch * i), new Vector2(ContentWidth, RowHeight));

                var icon = EnsureChild(row, "Icon", typeof(Image));
                Place(icon, new Vector2(iconX, 0f), new Vector2(iconSize, iconSize));
                var iconImage = icon.GetComponent<Image>();
                iconImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPaths[i]);
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;

                var title = EnsureText(
                    row,
                    "Title",
                    new Vector2(titleX, 0f),
                    new Vector2(titleWidth, RowHeight),
                    20f,
                    LocalizationService.Get(titleKeys[i]));
                title.color = Cyan;
                title.fontStyle = FontStyles.Bold;
                title.alignment = TextAlignmentOptions.MidlineLeft;
                title.textWrappingMode = TextWrappingModes.NoWrap;
                title.enableAutoSizing = true;
                title.fontSizeMin = 14f;
                title.fontSizeMax = 20f;

                var desc = EnsureText(
                    row,
                    "Desc",
                    new Vector2(descX, 0f),
                    new Vector2(descWidth, RowHeight),
                    18f,
                    LocalizationService.Get(descKeys[i]));
                desc.color = RowDescColor;
                desc.alignment = TextAlignmentOptions.MidlineLeft;
                desc.textWrappingMode = TextWrappingModes.NoWrap;
                desc.enableAutoSizing = true;
                desc.fontSizeMin = 13f;
                desc.fontSizeMax = 18f;

                refs.RowTitles[i] = title;
                refs.RowDescs[i] = desc;
            }
        }

        /// <summary>
        /// 설정·덮어쓰기 팝업과 같은 off/on 스프라이트 페이드(MenuSpriteButtonSkin)를 쓴다.
        /// primary는 청록 테두리 글로우를 상시 얹고, 골드 부족 시 뷰가 AccentRoot를 끈다.
        /// </summary>
        private static GameObject SkinConfirmButton(UnityEngine.UI.Button button, bool primary)
        {
            var image = button.GetComponent<UnityEngine.UI.Image>();
            var onSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonOnPath);
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonOffPath);
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            button.spriteState = default;
            var navigation = button.navigation;
            navigation.mode = UnityEngine.UI.Navigation.Mode.None;
            button.navigation = navigation;

            Transform overlayParent = button.transform;
            RectTransform accentRoot = null;
            if (primary)
            {
                accentRoot = EnsureChild(button.transform, "AccentRoot");
                Stretch(accentRoot);
                var glow = EnsureChild(accentRoot, "AccentGlow", typeof(Image));
                Stretch(glow);
                var glowImage = glow.GetComponent<Image>();
                glowImage.sprite = onSprite;
                glowImage.type = Image.Type.Simple;
                glowImage.color = new Color(1f, 1f, 1f, 0.55f);
                glowImage.raycastTarget = false;
                overlayParent = accentRoot;
            }

            var overlayRect = EnsureChild(overlayParent, "HoverOverlay", typeof(Image));
            Stretch(overlayRect);
            var overlay = overlayRect.GetComponent<Image>();
            overlay.sprite = onSprite;
            overlay.type = Image.Type.Simple;
            overlay.color = new Color(1f, 1f, 1f, 0f);
            overlay.raycastTarget = false;

            var skin = button.GetComponent<MenuSpriteButtonSkin>();
            if (skin == null)
            {
                skin = button.gameObject.AddComponent<MenuSpriteButtonSkin>();
            }

            var skinObject = new SerializedObject(skin);
            skinObject.FindProperty("overlay").objectReferenceValue = overlay;
            skinObject.ApplyModifiedPropertiesWithoutUndo();

            // 글자는 항상 오버레이 위에 그린다.
            var label = button.transform.Find("Label");
            if (primary)
            {
                accentRoot.SetAsFirstSibling();
            }
            else
            {
                overlayRect.SetAsFirstSibling();
            }

            if (label != null)
            {
                label.SetAsLastSibling();
                var text = label.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.fontSize = 22f;
                    text.enableAutoSizing = true;
                    text.fontSizeMin = 15f;
                    text.fontSizeMax = 22f;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }

            return primary ? accentRoot.gameObject : null;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
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
