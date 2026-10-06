using System;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 보관함 팝업의 자원 한 행(아이콘·이름·수량·무게). 누르면 그 자원을 선택한다(선택 경로는 Binder → Presenter).
    /// 열 좌표는 Columns 하나를 머리글과 함께 써서 두 목록이 같은 위치에 정렬된다.
    /// </summary>
    public sealed class StorageCargoRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static class Columns
        {
            public const float RowWidth = 596f;
            public const float RowHeight = 52f;
            public const float RowGap = 6f;
            public const float IconX = 14f;
            public const float IconSize = 36f;
            public const float NameX = 62f;
            public const float NameW = 250f;
            public const float QuantityX = 318f;
            public const float QuantityW = 110f;
            public const float WeightX = 446f;
            public const float WeightW = 130f;
        }

        private const float HoverSeconds = 0.12f;

        private Button button;
        private Image background;
        private Image outline;
        private Image accent;
        private Image icon;
        private TMP_Text nameText;
        private TMP_Text quantityText;
        private TMP_Text weightText;
        private bool hovered;
        private bool selected;
        private float hover;
        private float lastHover = -1f;
        private bool lastSelected;

        public string MineralId { get; private set; } = string.Empty;
        public Button Button => button;
        public RectTransform IconRect => icon != null ? icon.rectTransform : null;
        public bool IsSelected => selected;
        public string NameString => nameText != null ? nameText.text : string.Empty;
        public string QuantityString => quantityText != null ? quantityText.text : string.Empty;
        public string WeightString => weightText != null ? weightText.text : string.Empty;

        public event Action<string> Clicked;

        internal static StorageCargoRowView Create(Transform parent, TMP_FontAsset font)
        {
            var rect = ResourceSellUi.Place(parent, "Row", 0f, 0f, Columns.RowWidth, Columns.RowHeight);
            var view = rect.gameObject.AddComponent<StorageCargoRowView>();
            view.background = ResourceSellUi.Image(rect, "Back", ResourceSellArt.ChamferFill(), ResourceSellUi.RowIdle);
            view.background.raycastTarget = true;
            view.outline = ResourceSellUi.Image(rect, "Outline", ResourceSellArt.ChamferOutline(),
                ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.16f));
            view.accent = ResourceSellUi.AddImage(ResourceSellUi.Place(rect, "Accent", 0f, 8f, 4f, Columns.RowHeight - 16f), null,
                ResourceSellUi.Teal);
            view.accent.enabled = false;

            view.icon = ResourceSellUi.AddImage(
                ResourceSellUi.Place(rect, "Icon", Columns.IconX, (Columns.RowHeight - Columns.IconSize) * 0.5f, Columns.IconSize,
                    Columns.IconSize), null, Color.white);
            view.icon.preserveAspect = true;
            view.nameText = ResourceSellUi.Label(rect, "Name", Columns.NameX, 0f, Columns.NameW, Columns.RowHeight, font, 21f,
                FontStyles.Bold, ResourceSellUi.TextMain, TextAlignmentOptions.Left);
            view.nameText.overflowMode = TextOverflowModes.Ellipsis;
            view.quantityText = ResourceSellUi.Label(rect, "Quantity", Columns.QuantityX, 0f, Columns.QuantityW, Columns.RowHeight,
                font, 21f, FontStyles.Bold, Color.white, TextAlignmentOptions.Right);
            view.weightText = ResourceSellUi.Label(rect, "Weight", Columns.WeightX, 0f, Columns.WeightW, Columns.RowHeight, font,
                19f, FontStyles.Normal, ResourceSellUi.TextMuted, TextAlignmentOptions.Right);

            view.button = rect.gameObject.AddComponent<Button>();
            view.button.targetGraphic = view.background;
            view.button.transition = Selectable.Transition.None;
            var navigation = view.button.navigation;
            navigation.mode = Navigation.Mode.None;
            view.button.navigation = navigation;
            view.button.onClick.AddListener(() => view.Clicked?.Invoke(view.MineralId));
            view.Apply(true);
            return view;
        }

        public void Bind(StorageCargoLine line, Sprite iconSprite, bool isSelected)
        {
            MineralId = line.MineralId;
            nameText.text = line.DisplayName;
            quantityText.text = line.Quantity.ToString("N0") + "개";
            weightText.text = StorageTransferPreview.Weight(line.Weight);
            icon.sprite = iconSprite != null ? iconSprite : StorageBoxArt.Gem();
            icon.color = iconSprite != null ? Color.white : ResourceSellUi.Teal;
            selected = isSelected;
            Apply(true);
        }

        public void SetSelected(bool isSelected)
        {
            if (selected == isSelected)
            {
                return;
            }

            selected = isSelected;
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
            var target = hovered && button != null && button.IsInteractable() ? 1f : 0f;
            hover = Mathf.MoveTowards(hover, target, Time.unscaledDeltaTime / HoverSeconds);
            Apply(false);
        }

        // 매번 hover·selected에서 새로 계산하므로 빠른 호버에도 색이 누적되지 않는다.
        private void Apply(bool force)
        {
            if (!force && Mathf.Approximately(hover, lastHover) && selected == lastSelected)
            {
                return;
            }

            lastHover = hover;
            lastSelected = selected;
            var baseColor = selected ? ResourceSellUi.RowActive : ResourceSellUi.RowIdle;
            background.color = Color.Lerp(baseColor, ResourceSellUi.RowActive, hover * 0.6f);
            var line = selected ? 0.85f : Mathf.Lerp(0.16f, 0.5f, hover);
            outline.color = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, line);
            accent.enabled = selected;
        }
    }
}
