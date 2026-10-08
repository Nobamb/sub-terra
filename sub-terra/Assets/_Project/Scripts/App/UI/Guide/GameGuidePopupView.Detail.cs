using System.Collections.Generic;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Guide
{
    public sealed partial class GameGuidePopupView
    {
        private const float DetailInner = DetailWidth - 14f - 14f - 14f;

        private static readonly Color BodyColor = new Color(0.88f, 0.95f, 0.98f, 1f);
        private static readonly Color LabelColor = new Color(0.50f, 0.90f, 1f, 1f);
        private static readonly Color WarnColor = new Color(1f, 0.55f, 0.48f, 1f);

        private RectTransform detailBody;
        private CanvasGroup detailBodyGroup;
        private RectTransform detailRows;
        private GameGuideStageView detailStage;
        private string shownCardId = string.Empty;
        private GuideFilter shownFilter = GuideFilter.All;
        private GuideDetailModel shownModel;
        private float shownHeight;

        public string ShownCardId => shownCardId;
        public GuideDetailModel ShownDetail => shownModel;
        public float DetailContentHeight => shownHeight;
        public GameGuideStageView DetailStage => detailStage;
        public float DetailAlpha => detailBodyGroup != null ? detailBodyGroup.alpha : 0f;
        public RectTransform DetailPanelRect => detailPanel;

        private void BuildDetailPanel()
        {
            detailPanel = ResourceSellUi.Place(detailRect, "DetailPanel", Pad + ListWidth + ColumnGap, BodyY, DetailWidth, BodyHeight);
            var glow = ResourceSellUi.Image(detailPanel, "Glow", ResourceSellArt.SoftRect(), ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.07f));
            glow.rectTransform.offsetMin = new Vector2(-8f, -8f);
            glow.rectTransform.offsetMax = new Vector2(8f, 8f);
            ResourceSellUi.Image(detailPanel, "Face", ResourceSellArt.ChamferFill(), new Color(0.03f, 0.06f, 0.09f, 0.97f));
            ResourceSellUi.Image(detailPanel, "Border", ResourceSellArt.ChamferOutline(), ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.6f));

            detailScroll = MakeScroll(detailPanel, "Scroll", 14f, 14f, 14f, 14f, 14f, true);
            detailBody = ResourceSellUi.Image(detailScroll.Content, "Body", null, Color.clear).rectTransform;
            detailBody.GetComponent<Image>().raycastTarget = false;
            detailBodyGroup = detailBody.gameObject.AddComponent<CanvasGroup>();
            detailStage = GameGuideStageView.Create(detailBody, "Stage", 0f, 54f, DetailInner, DetailInner * 0.375f, font, sprites);
            detailRows = ResourceSellUi.Image(detailBody, "Rows", null, Color.clear).rectTransform;
            detailRows.GetComponent<Image>().raycastTarget = false;
        }

        private void OnCardClicked(string cardId)
        {
            if (state != PopupState.Open && !(state == PopupState.Opening && detailGroup.interactable))
            {
                return;
            }

            guideState.SelectCard(cardId);
        }

        private void OnStateChanged()
        {
            if (state == PopupState.Hidden)
            {
                return;
            }

            RefreshAll(false);
        }

        private void ApplyDetailFade()
        {
            detailBodyGroup.alpha = GameGuideTimeline.Smooth(detailFade);
        }

        /// <summary>
        /// 선택 상태에 맞춰 탭·필터·목록·상세를 갱신한다. force면 상세를 다시 만들고 첫 탐사 안내 높이를 즉시 맞춘다.
        /// 등장 연출은 다시 재생하지 않고, 상세 내용만 짧게 페이드한다.
        /// </summary>
        private void RefreshAll(bool force)
        {
            var tab = guideState.Tab;
            firstTarget = guideState.FirstExploreExpanded ? 1f : 0f;
            if (force)
            {
                firstT = firstTarget;
            }

            var built = EnsureList(tab);
            var filterChanged = guideState.Filter != shownFilter;
            shownFilter = guideState.Filter;
            LayoutBody();
            LayoutCards(tab);
            if (built || filterChanged)
            {
                ResetScroll(lists[(int)tab].Scroll);
            }

            for (var i = 0; i < tabButtons.Length; i++)
            {
                tabButtons[i].SetSelected((int)tab == i);
            }

            for (var i = 0; i < filterButtons.Length; i++)
            {
                filterButtons[i].SetSelected((int)guideState.Filter == i);
            }

            var currentId = guideState.CurrentCardId;
            var list = lists[(int)tab];
            for (var i = 0; i < list.Cards.Count; i++)
            {
                list.Cards[i].SetSelected(list.Cards[i].Def.Id == currentId);
            }

            if (force || currentId != shownCardId)
            {
                RebuildDetail(currentId, !force);
            }

            var reveal = guideState.ConsumeReveal();
            if (!string.IsNullOrEmpty(reveal))
            {
                RevealCard(list, reveal);
            }
        }

        private void RevealCard(TabList list, string cardId)
        {
            if (list.Scroll == null || !list.Built)
            {
                return;
            }

            for (var i = 0; i < list.Cards.Count; i++)
            {
                var cardView = list.Cards[i];
                if (cardView.Def.Id != cardId || !cardView.gameObject.activeSelf)
                {
                    continue;
                }

                var rect = cardView.Rect;
                var top = -(rect.anchoredPosition.y + rect.sizeDelta.y * 0.5f);
                var bottom = top + rect.sizeDelta.y;
                var viewport = list.Scroll.Viewport.rect.height;
                var contentHeight = list.Scroll.Content.sizeDelta.y;
                var position = list.Scroll.Content.anchoredPosition;
                if (top < position.y || bottom > position.y + viewport)
                {
                    var wanted = top - (viewport - rect.sizeDelta.y) * 0.5f;
                    position.y = Mathf.Clamp(wanted, 0f, Mathf.Max(0f, contentHeight - viewport));
                    list.Scroll.Content.anchoredPosition = position;
                }

                return;
            }
        }

        // ------------------------------------------------------------------ 상세 내용

        private void RebuildDetail(string cardId, bool fade)
        {
            shownCardId = cardId;
            StopDemo();
            for (var i = detailRows.childCount - 1; i >= 0; i--)
            {
                var child = detailRows.GetChild(i).gameObject;
                child.SetActive(false);
                GuideUtil.Dispose(child);
            }

            if (!GameGuideCatalog.TryGet(cardId, out var def))
            {
                shownModel = null;
                detailStage.Bind(null, false);
                return;
            }

            var model = GuideDetailBuilder.Build(def, data);
            shownModel = model;
            var y = 0f;

            // 큰 제목 + 키캡.
            var title = ResourceSellUi.Label(detailRows, "Title", 0f, y, DetailInner, 46f, font, 34f, FontStyles.Bold, Color.white,
                TextAlignmentOptions.MidlineLeft);
            title.text = model.Title;
            title.overflowMode = TextOverflowModes.Overflow;
            var capsWidth = string.IsNullOrEmpty(model.Keys) ? 0f : GuideKeyCapView.MeasureRow(model.Keys, 30f);
            var titleWidth = Mathf.Min(DetailInner - capsWidth - 14f, title.GetPreferredValues(model.Title).x + 4f);
            title.rectTransform.sizeDelta = new Vector2(titleWidth, 46f);
            title.rectTransform.anchoredPosition = new Vector2(titleWidth * 0.5f, -(y + 23f));
            if (capsWidth > 0f)
            {
                GuideKeyCapView.BuildRow(detailRows, model.Keys, titleWidth + 14f, y + 8f, 30f, font, null);
            }

            y += 54f;

            // 시연 화면.
            var stageHeight = DetailInner * 0.375f;
            detailStage.AsRect().anchoredPosition = new Vector2(DetailInner * 0.5f, -(y + stageHeight * 0.5f));
            if (GuideDemoLibrary.TryGet(model.DemoId, out var clip))
            {
                detailStage.gameObject.SetActive(true);
                detailStage.Bind(clip, true);
                detailStage.Show(clip.ThumbTime);
                y += stageHeight + 16f;
            }
            else
            {
                detailStage.Bind(null, false);
                detailStage.gameObject.SetActive(false);
            }

            // 조작·설명 → 경고 → 팁.
            for (var i = 0; i < model.Lines.Count; i++)
            {
                var line = model.Lines[i];
                if (line.Kind == GuideLineKind.Step || line.Kind == GuideLineKind.Text || line.Kind == GuideLineKind.Data)
                {
                    y = AddLine(line, i, y);
                }
            }

            for (var i = 0; i < model.Lines.Count; i++)
            {
                if (model.Lines[i].Kind == GuideLineKind.Warning)
                {
                    y = AddLine(model.Lines[i], i, y);
                }
            }

            for (var i = 0; i < model.Lines.Count; i++)
            {
                if (model.Lines[i].Kind == GuideLineKind.Tip)
                {
                    y = AddLine(model.Lines[i], i, y);
                }
            }

            y = AddRelated(model, y);
            shownHeight = y + 6f;
            detailScroll.Content.sizeDelta = new Vector2(0f, shownHeight);
            detailBody.anchorMin = new Vector2(0f, 1f);
            detailBody.anchorMax = new Vector2(0f, 1f);
            detailBody.pivot = new Vector2(0f, 1f);
            detailBody.anchoredPosition = Vector2.zero;
            detailBody.sizeDelta = new Vector2(DetailInner, shownHeight);
            detailRows.anchorMin = detailRows.anchorMax = new Vector2(0f, 1f);
            detailRows.pivot = new Vector2(0f, 1f);
            detailRows.anchoredPosition = Vector2.zero;
            detailRows.sizeDelta = new Vector2(DetailInner, shownHeight);
            ResetScroll(detailScroll);
            UiKeyboardSubmitGuard.ConfigureButtonsUnder(detailRows);

            if (fade)
            {
                detailFade = 0.15f;
                ApplyDetailFade();
            }
            else
            {
                detailFade = 1f;
                ApplyDetailFade();
            }

            if (settled)
            {
                PlayDemo();
            }
        }

        private float AddLine(GuideLine line, int index, float y)
        {
            switch (line.Kind)
            {
                case GuideLineKind.Step: return AddStep(line, index, y);
                case GuideLineKind.Data: return AddData(line, y);
                case GuideLineKind.Tip: return AddNote(line.Text, y, false);
                case GuideLineKind.Warning: return AddNote(line.Text, y, true);
                default: return AddParagraph(line.Text, y);
            }
        }

        private TMP_Text Wrapped(Transform parent, string name, float x, float y, float w, string text, float size,
            FontStyles style, Color color, out float height)
        {
            var label = ResourceSellUi.Label(parent, name, x, y, w, 24f, font, size, style, color, TextAlignmentOptions.TopLeft);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.text = text;
            height = Mathf.Ceil(label.GetPreferredValues(text, w, 0f).y);
            var rect = label.rectTransform;
            rect.sizeDelta = new Vector2(w, height);
            rect.anchoredPosition = new Vector2(x + w * 0.5f, -(y + height * 0.5f));
            return label;
        }

        private float AddParagraph(string text, float y)
        {
            Wrapped(detailRows, "Text", 0f, y, DetailInner, text, 19f, FontStyles.Normal, BodyColor, out var height);
            return y + height + 12f;
        }

        private float AddStep(GuideLine line, int index, float y)
        {
            var number = 0;
            for (var i = 0; i <= index && i < shownModel.Lines.Count; i++)
            {
                if (shownModel.Lines[i].Kind == GuideLineKind.Step)
                {
                    number++;
                }
            }

            var chip = ResourceSellUi.Place(detailRows, "StepNumber", 0f, y + 1f, 30f, 30f);
            ResourceSellUi.AddImage(chip, ResourceSellArt.ChamferFill(), new Color(0.05f, 0.24f, 0.30f, 1f));
            var chipLine = ResourceSellUi.Image(chip, "Line", ResourceSellArt.ChamferOutline(), ResourceSellUi.Teal);
            chipLine.raycastTarget = false;
            var digit = ResourceSellUi.Label(chip, "Digit", 0f, 0f, 30f, 30f, font, 18f, FontStyles.Bold, ResourceSellUi.Teal,
                TextAlignmentOptions.Center);
            digit.text = number.ToString();

            var x = 42f;
            var width = DetailInner - x;
            var cursor = y;
            if (!string.IsNullOrEmpty(line.Label))
            {
                Wrapped(detailRows, "StepLabel", x, cursor, width, line.Label, 19f, FontStyles.Bold, Color.white, out var labelHeight);
                cursor += labelHeight + 2f;
            }

            Wrapped(detailRows, "StepText", x, cursor, width, line.Text, 18f, FontStyles.Normal, BodyColor, out var textHeight);
            cursor += textHeight;
            return Mathf.Max(cursor, y + 32f) + 12f;
        }

        private float AddData(GuideLine line, float y)
        {
            const float labelWidth = 112f;
            Wrapped(detailRows, "DataLabel", 0f, y, labelWidth, line.Label, 17f, FontStyles.Bold, LabelColor, out var labelHeight);
            Wrapped(detailRows, "DataText", labelWidth + 10f, y, DetailInner - labelWidth - 10f, line.Text, 18f,
                FontStyles.Normal, BodyColor, out var textHeight);
            var rowBottom = y + Mathf.Max(labelHeight, textHeight);
            var divider = ResourceSellUi.Place(detailRows, "Divider", 0f, rowBottom + 6f, DetailInner, 1f);
            ResourceSellUi.AddImage(divider, null, new Color(0.42f, 0.94f, 1f, 0.12f));
            return rowBottom + 14f;
        }

        private float AddNote(string text, float y, bool warning)
        {
            var accent = warning ? WarnColor : ResourceSellUi.Teal;
            var textX = 44f;
            var textWidth = DetailInner - textX - 12f;
            var label = ResourceSellUi.Label(detailRows, "Measure", 0f, 0f, textWidth, 24f, font, 18f, FontStyles.Normal, BodyColor,
                TextAlignmentOptions.TopLeft);
            label.textWrappingMode = TextWrappingModes.Normal;
            var height = Mathf.Ceil(label.GetPreferredValues(text, textWidth, 0f).y);
            GuideUtil.Dispose(label.gameObject);

            var boxHeight = Mathf.Max(46f, height + 24f);
            var box = ResourceSellUi.Place(detailRows, warning ? "Warning" : "Tip", 0f, y, DetailInner, boxHeight);
            ResourceSellUi.AddImage(box, ResourceSellArt.ChamferFill(), ResourceSellUi.WithAlpha(accent, warning ? 0.14f : 0.11f));
            var line = ResourceSellUi.Image(box, "Line", ResourceSellArt.ChamferOutline(), ResourceSellUi.WithAlpha(accent, 0.7f));
            line.raycastTarget = false;
            var icon = ResourceSellUi.AddImage(ResourceSellUi.Place(box, "Icon", 12f, (boxHeight - 24f) * 0.5f, 24f, 24f),
                warning ? GameGuideArt.Warning() : GameGuideArt.Info(), accent);
            icon.preserveAspect = true;
            Wrapped(box, "Text", textX, 12f, textWidth, text, 18f, FontStyles.Normal, warning ? new Color(1f, 0.86f, 0.82f, 1f) : BodyColor, out _);
            return y + boxHeight + 12f;
        }

        private float AddRelated(GuideDetailModel model, float y)
        {
            if (model.Related.Count == 0)
            {
                return y;
            }

            var header = ResourceSellUi.Label(detailRows, "RelatedHeader", 0f, y + 4f, DetailInner, 24f, font, 17f, FontStyles.Bold,
                ResourceSellUi.TextMuted, TextAlignmentOptions.MidlineLeft);
            header.text = "관련 안내";
            y += 34f;
            var x = 0f;
            var rowY = y;
            for (var i = 0; i < model.Related.Count; i++)
            {
                var target = model.Related[i];
                var measure = ResourceSellUi.Label(detailRows, "Measure", 0f, 0f, 400f, 30f, font, 18f, FontStyles.Bold, Color.white,
                    TextAlignmentOptions.Center);
                var width = measure.GetPreferredValues(target.Title).x;
                GuideUtil.Dispose(measure.gameObject);
                var buttonWidth = Mathf.Min(DetailInner, width + 82f);
                if (x > 0f && x + buttonWidth > DetailInner)
                {
                    x = 0f;
                    rowY += 48f;
                }

                var button = GameGuideButton.Create(detailRows, "Related_" + target.Id, x, rowY, buttonWidth, 40f, font, target.Title,
                    18f, GameGuideButton.Kind.Normal, GameGuideArt.BookGlyph(), 22f);
                var targetId = target.Id;
                button.Button.onClick.AddListener(() => OnCardClicked(targetId));
                x += buttonWidth + 10f;
            }

            return rowY + 48f;
        }
    }

    internal static class RectExtensions
    {
        public static RectTransform AsRect(this Component component)
        {
            return (RectTransform)component.transform;
        }
    }
}
