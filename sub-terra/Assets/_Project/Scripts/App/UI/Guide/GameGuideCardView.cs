using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.Guide
{
    /// <summary>
    /// 목록 카드 한 장. 조작·메커니즘 카드(제목·정지 썸네일·키 또는 요약)와 자원·시설 카드(아이콘·이름·분류·요약)가
    /// 같은 컴포넌트를 쓴다. 선택은 청록 테두리와 면 색 차이로 구분한다.
    /// </summary>
    public sealed class GameGuideCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public enum Layout
        {
            Wide = 0,
            Icon = 1
        }

        public const float WideWidth = 390f;
        public const float WideHeight = 218f;
        public const float IconWidth = 256f;
        public const float IconHeight = 112f;

        private static readonly Color FaceIdle = new Color(0.035f, 0.07f, 0.10f, 0.96f);
        private static readonly Color FaceHover = new Color(0.06f, 0.13f, 0.18f, 0.98f);
        private static readonly Color FaceSelected = new Color(0.05f, 0.19f, 0.24f, 1f);

        private Image face;
        private Image border;
        private Image glow;
        private float hover;
        private float lastHover = -1f;
        private bool hovered;
        private bool selected;
        private bool lastSelected;

        public GuideCardDef Def { get; private set; }
        public GameGuideStageView Stage { get; private set; }
        public Button Button { get; private set; }
        public Layout CardLayout { get; private set; }
        public bool IsSelected => selected;
        public Color FaceColor => face != null ? face.color : Color.clear;
        public Color BorderColor => border != null ? border.color : Color.clear;
        public RectTransform Rect => (RectTransform)transform;

        public event Action<string> Clicked;

        public void SetSelected(bool value)
        {
            if (selected == value && lastHover >= 0f)
            {
                return;
            }

            selected = value;
            Apply(true);
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;

        public void OnPointerExit(PointerEventData eventData) => hovered = false;

        private void OnDisable()
        {
            hovered = false;
            hover = 0f;
            Apply(true);
        }

        private void Update()
        {
            var target = hovered ? 1f : 0f;
            if (!Mathf.Approximately(hover, target))
            {
                hover = Mathf.MoveTowards(hover, target, Time.unscaledDeltaTime / 0.1f);
                Apply(false);
            }
        }

        private void Apply(bool force)
        {
            if (!force && Mathf.Approximately(hover, lastHover) && selected == lastSelected)
            {
                return;
            }

            lastHover = hover;
            lastSelected = selected;
            var baseFace = selected ? FaceSelected : Color.Lerp(FaceIdle, FaceHover, hover);
            face.color = baseFace;
            border.color = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, selected ? 1f : Mathf.Lerp(0.26f, 0.7f, hover));
            glow.color = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, selected ? 0.34f : hover * 0.14f);
        }

        internal static string IconKeyFor(GuideCardDef def)
        {
            if (def.Kind == GuideCardKind.Resource)
            {
                return def.SourceId == DataIds.Currency.Gold ? "icon.gold" : "item:" + def.SourceId;
            }

            if (def.Kind == GuideCardKind.Facility)
            {
                return string.IsNullOrEmpty(def.SourceId) ? "fac.elevator" : "bld:" + def.SourceId;
            }

            return string.Empty;
        }

        internal static GameGuideCardView Create(
            Transform parent,
            GuideCardDef def,
            Layout layout,
            float x,
            float y,
            TMP_FontAsset font,
            IGuideSprites sprites)
        {
            var width = layout == Layout.Wide ? WideWidth : IconWidth;
            var height = layout == Layout.Wide ? WideHeight : IconHeight;
            var rect = ResourceSellUi.Place(parent, "Card_" + def.Id, x, y, width, height);
            var glowImage = ResourceSellUi.Image(rect, "Glow", ResourceSellArt.SoftRect(), Color.clear);
            glowImage.rectTransform.offsetMin = new Vector2(-10f, -10f);
            glowImage.rectTransform.offsetMax = new Vector2(10f, 10f);
            var faceImage = ResourceSellUi.Image(rect, "Face", ResourceSellArt.ChamferFill(), FaceIdle);
            faceImage.raycastTarget = true;
            var borderImage = ResourceSellUi.Image(rect, "Border", ResourceSellArt.ChamferOutline(), ResourceSellUi.Teal);

            var view = rect.gameObject.AddComponent<GameGuideCardView>();
            view.Def = def;
            view.CardLayout = layout;
            view.face = faceImage;
            view.border = borderImage;
            view.glow = glowImage;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = faceImage;
            button.transition = Selectable.Transition.None;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.onClick.AddListener(() => view.Clicked?.Invoke(def.Id));
            view.Button = button;

            if (layout == Layout.Wide)
            {
                BuildWide(view, rect, def, font, sprites);
            }
            else
            {
                BuildIcon(view, rect, def, font, sprites);
            }

            view.Apply(true);
            return view;
        }

        public const float StepWidth = 236f;
        public const float StepHeight = 164f;

        /// <summary>첫 탐사 안내의 단계 타일(번호·제목·작은 장면·설명). 선택하면 대상 카드로 이동한다.</summary>
        internal static GameGuideCardView CreateStepTile(
            Transform parent,
            GuideFirstStep step,
            float x,
            float y,
            TMP_FontAsset font,
            IGuideSprites sprites)
        {
            var rect = ResourceSellUi.Place(parent, "Step_" + step.Number, x, y, StepWidth, StepHeight);
            var glowImage = ResourceSellUi.Image(rect, "Glow", ResourceSellArt.SoftRect(), Color.clear);
            glowImage.rectTransform.offsetMin = new Vector2(-10f, -10f);
            glowImage.rectTransform.offsetMax = new Vector2(10f, 10f);
            var faceImage = ResourceSellUi.Image(rect, "Face", ResourceSellArt.ChamferFill(), FaceIdle);
            faceImage.raycastTarget = true;
            var borderImage = ResourceSellUi.Image(rect, "Border", ResourceSellArt.ChamferOutline(), ResourceSellUi.Teal);

            var view = rect.gameObject.AddComponent<GameGuideCardView>();
            view.Def = GameGuideCatalog.Get(step.TargetCardId);
            view.CardLayout = Layout.Wide;
            view.face = faceImage;
            view.border = borderImage;
            view.glow = glowImage;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = faceImage;
            button.transition = Selectable.Transition.None;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            var targetId = step.TargetCardId;
            button.onClick.AddListener(() => view.Clicked?.Invoke(targetId));
            view.Button = button;

            var chipRect = ResourceSellUi.Place(rect, "Number", 10f, 10f, 42f, 26f);
            ResourceSellUi.AddImage(chipRect, ResourceSellArt.ChamferFill(), ResourceSellUi.TealDeep);
            var number = ResourceSellUi.Label(chipRect, "Text", 0f, 0f, 42f, 26f, font, 18f, FontStyles.Bold,
                ResourceSellUi.Navy, TextAlignmentOptions.Center);
            number.text = step.Number;
            var title = ResourceSellUi.Label(rect, "Title", 60f, 8f, StepWidth - 68f, 30f, font, 20f, FontStyles.Bold,
                Color.white, TextAlignmentOptions.MidlineLeft);
            title.text = step.Title;
            title.overflowMode = TextOverflowModes.Ellipsis;

            var stageWidth = StepWidth - 20f;
            view.Stage = GameGuideStageView.Create(rect, "Thumb", 10f, 42f, stageWidth, stageWidth * 0.375f, font, sprites);
            if (GuideDemoLibrary.TryGet(step.DemoId, out var clip))
            {
                view.Stage.Bind(clip, false);
                view.Stage.ShowThumb();
            }

            var caption = ResourceSellUi.Label(rect, "Caption", 10f, 42f + stageWidth * 0.375f + 4f, stageWidth, 28f, font,
                16f, FontStyles.Normal, ResourceSellUi.TextMuted, TextAlignmentOptions.MidlineLeft);
            caption.text = step.Caption;
            caption.overflowMode = TextOverflowModes.Ellipsis;
            view.Apply(true);
            return view;
        }

        private static void BuildWide(GameGuideCardView view, RectTransform rect, GuideCardDef def, TMP_FontAsset font,
            IGuideSprites sprites)
        {
            var title = ResourceSellUi.Label(rect, "Title", 16f, 10f, WideWidth - 32f, 32f, font, 24f, FontStyles.Bold,
                Color.white, TextAlignmentOptions.MidlineLeft);
            title.text = def.Title;
            title.overflowMode = TextOverflowModes.Ellipsis;

            view.Stage = GameGuideStageView.Create(rect, "Thumb", 12f, 46f, WideWidth - 24f, (WideWidth - 24f) * 0.375f, font, sprites);
            if (GuideDemoLibrary.TryGet(def.DemoId, out var clip))
            {
                view.Stage.Bind(clip, false);
                view.Stage.ShowThumb();
            }

            const float rowY = 46f + (WideWidth - 24f) * 0.375f + 6f;
            if (!string.IsNullOrEmpty(def.Keys))
            {
                GuideKeyCapView.BuildRow(rect, def.Keys, 16f, rowY, 28f, font, null);
            }
            else
            {
                var summary = ResourceSellUi.Label(rect, "Summary", 16f, rowY, WideWidth - 32f, 28f, font, 17f,
                    FontStyles.Normal, ResourceSellUi.TextMuted, TextAlignmentOptions.MidlineLeft);
                summary.text = def.Summary;
                summary.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private static void BuildIcon(GameGuideCardView view, RectTransform rect, GuideCardDef def, TMP_FontAsset font,
            IGuideSprites sprites)
        {
            var iconRect = ResourceSellUi.Place(rect, "Icon", 12f, 16f, 80f, 80f);
            var icon = ResourceSellUi.AddImage(iconRect, sprites != null ? sprites.Get(IconKeyFor(def)) : null, Color.white);
            icon.preserveAspect = true;
            icon.enabled = icon.sprite != null;

            var name = ResourceSellUi.Label(rect, "Name", 104f, 10f, IconWidth - 112f, 32f, font, 22f, FontStyles.Bold,
                Color.white, TextAlignmentOptions.MidlineLeft);
            name.text = def.Title;
            name.enableAutoSizing = true;
            name.fontSizeMax = 22f;
            name.fontSizeMin = 15f;

            var tagText = def.Kind == GuideCardKind.Resource ? "자원" : "시설";
            var tagRect = ResourceSellUi.Place(rect, "Tag", 104f, 46f, 52f, 24f);
            ResourceSellUi.AddImage(tagRect, ResourceSellArt.ChamferFill(), new Color(0.10f, 0.20f, 0.27f, 1f));
            var tagBorder = ResourceSellUi.Image(tagRect, "Line", ResourceSellArt.ChamferOutline(),
                ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.35f));
            tagBorder.raycastTarget = false;
            var tag = ResourceSellUi.Label(tagRect, "Text", 0f, 0f, 52f, 24f, font, 15f, FontStyles.Bold,
                new Color(0.78f, 0.92f, 0.97f, 1f), TextAlignmentOptions.Center);
            tag.text = tagText;

            if (!string.IsNullOrEmpty(def.Keys))
            {
                GuideKeyCapView.BuildRow(rect, def.Keys, 164f, 45f, 26f, font, null);
            }

            var summary = ResourceSellUi.Label(rect, "Summary", 104f, 72f, IconWidth - 112f, 36f, font, 14f,
                FontStyles.Normal, ResourceSellUi.TextMuted, TextAlignmentOptions.TopLeft);
            summary.textWrappingMode = TextWrappingModes.Normal;
            summary.text = def.Summary;
        }
    }
}
