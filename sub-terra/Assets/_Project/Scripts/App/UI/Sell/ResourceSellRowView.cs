using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Sell
{
    /// <summary>
    /// 판매 목록 한 행. 표시와 입력 위임만 한다(수량 계산은 ResourceSellSession).
    /// 열 좌표는 Columns 상수 하나를 머리글과 함께 써서 모든 행이 같은 위치에 정렬된다.
    /// </summary>
    public sealed class ResourceSellRowView : MonoBehaviour
    {
        /// <summary>행·머리글 공통 열 좌표(행 왼쪽 기준 x, 폭).</summary>
        public static class Columns
        {
            public const float RowWidth = 1244f;
            public const float RowHeight = 72f;
            public const float RowGap = 12f;
            public const float IconX = 24f;
            public const float IconSize = 48f;
            public const float NameX = 92f;
            public const float NameW = 230f;
            public const float OwnedX = 336f;
            public const float OwnedW = 100f;
            public const float PriceX = 452f;
            public const float PriceW = 130f;
            public const float ControlsX = 612f;
            public const float ControlsW = 452f;
            public const float GoldX = 1084f;
            public const float GoldW = 148f;

            public const float StepW = 46f;
            public const float QtyW = 92f;
            public const float PlusFiveW = 64f;
            public const float PlusTenW = 68f;
            public const float MaxW = 76f;
            public const float Gap = 12f;
            public const float ButtonH = 44f;
        }

        public const int PlusFive = 5;
        public const int PlusTen = 10;

        private Image background;
        private Image outline;
        private Image glow;
        private Image flash;
        private Image icon;
        private TMP_Text nameText;
        private TMP_Text noteText;
        private TMP_Text ownedText;
        private TMP_Text priceText;
        private TMP_Text quantityText;
        private TMP_Text goldText;
        private Image goldCoin;
        private RectTransform goldAnchor;
        private CanvasGroup group;
        private ResourceSellButton minus;
        private ResourceSellButton plus;
        private ResourceSellButton plusFive;
        private ResourceSellButton plusTen;
        private ResourceSellButton max;
        private float active;
        private float activeTarget;

        public string ItemId { get; private set; } = string.Empty;
        public int Quantity { get; private set; }
        public bool IsActiveRow => Quantity > 0;
        public CanvasGroup Group => group;
        public RectTransform GoldAnchor => goldAnchor;
        public ResourceSellButton MinusButton => minus;
        public ResourceSellButton PlusButton => plus;
        public ResourceSellButton PlusFiveButton => plusFive;
        public ResourceSellButton PlusTenButton => plusTen;
        public ResourceSellButton MaxButton => max;
        public string QuantityString => quantityText != null ? quantityText.text : string.Empty;
        public string GoldString => goldText != null ? goldText.text : string.Empty;
        public string OwnedString => ownedText != null ? ownedText.text : string.Empty;
        public string NoteString => noteText != null ? noteText.text : string.Empty;
        public string NameString => nameText != null ? nameText.text : string.Empty;
        public float OutlineAlpha => outline != null ? outline.color.a : 0f;

        public event Action<string, int> AdjustRequested;
        public event Action<string> MaxRequested;

        internal static ResourceSellRowView Create(Transform parent, TMP_FontAsset font, Sprite coin)
        {
            var rect = ResourceSellUi.Place(parent, "SellRow", 0f, 0f, Columns.RowWidth, Columns.RowHeight);
            var row = rect.gameObject.AddComponent<ResourceSellRowView>();
            row.Build(rect, font, coin);
            return row;
        }

        private void Build(RectTransform rect, TMP_FontAsset font, Sprite coin)
        {
            group = gameObject.AddComponent<CanvasGroup>();
            glow = ResourceSellUi.Image(rect, "Glow", ResourceSellArt.SoftRect(), Color.clear);
            glow.rectTransform.offsetMin = new Vector2(-12f, -12f);
            glow.rectTransform.offsetMax = new Vector2(12f, 12f);
            background = ResourceSellUi.Image(rect, "Background", ResourceSellArt.ChamferFill(), ResourceSellUi.RowIdle);
            outline = ResourceSellUi.Image(rect, "Outline", ResourceSellArt.ChamferOutline(), Color.clear);
            flash = ResourceSellUi.Image(rect, "Flash", ResourceSellArt.ChamferFill(), Color.clear);

            var mid = (Columns.RowHeight - Columns.IconSize) * 0.5f;
            icon = ResourceSellUi.AddImage(
                ResourceSellUi.Place(rect, "Icon", Columns.IconX, mid, Columns.IconSize, Columns.IconSize), null, Color.white);
            icon.preserveAspect = true;

            nameText = ResourceSellUi.Label(rect, "Name", Columns.NameX, 6f, Columns.NameW, 38f, font, 22f,
                FontStyles.Bold, ResourceSellUi.TextMain, TextAlignmentOptions.Left);
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 16f;
            nameText.fontSizeMax = 22f;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            noteText = ResourceSellUi.Label(rect, "Note", Columns.NameX, 41f, Columns.NameW + 4f, 22f, font, 15f,
                FontStyles.Normal, ResourceSellUi.TextMuted, TextAlignmentOptions.Left);
            ownedText = ResourceSellUi.Label(rect, "Owned", Columns.OwnedX, 0f, Columns.OwnedW, Columns.RowHeight, font,
                22f, FontStyles.Normal, ResourceSellUi.TextMain, TextAlignmentOptions.Center);
            priceText = ResourceSellUi.Label(rect, "UnitPrice", Columns.PriceX, 0f, Columns.PriceW, Columns.RowHeight,
                font, 22f, FontStyles.Bold, ResourceSellUi.Gold, TextAlignmentOptions.Center);

            var by = (Columns.RowHeight - Columns.ButtonH) * 0.5f;
            var x = Columns.ControlsX;
            minus = ResourceSellButton.Create(rect, "Minus", x, by, Columns.StepW, Columns.ButtonH, font, null, 20f,
                ResourceSellButton.Variant.Normal, ResourceSellArt.Sign(false), 20f);
            x += Columns.StepW + Columns.Gap;

            var qtyRect = ResourceSellUi.Place(rect, "Quantity", x, by, Columns.QtyW, Columns.ButtonH);
            ResourceSellUi.Image(qtyRect, "Field", ResourceSellArt.ChamferFill(), new Color(0.008f, 0.025f, 0.04f, 1f));
            ResourceSellUi.Image(qtyRect, "FieldLine", ResourceSellArt.ChamferOutline(),
                ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.32f));
            var qtyLabel = ResourceSellUi.Centered(qtyRect, "Value", Vector2.zero, new Vector2(Columns.QtyW - 8f, Columns.ButtonH));
            quantityText = ResourceSellUi.Text(qtyLabel, font, 26f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            x += Columns.QtyW + Columns.Gap;

            plus = ResourceSellButton.Create(rect, "Plus", x, by, Columns.StepW, Columns.ButtonH, font, null, 20f,
                ResourceSellButton.Variant.Normal, ResourceSellArt.Sign(true), 20f);
            x += Columns.StepW + Columns.Gap;
            plusFive = ResourceSellButton.Create(rect, "PlusFive", x, by, Columns.PlusFiveW, Columns.ButtonH, font, "+5", 20f,
                ResourceSellButton.Variant.Normal);
            x += Columns.PlusFiveW + Columns.Gap;
            plusTen = ResourceSellButton.Create(rect, "PlusTen", x, by, Columns.PlusTenW, Columns.ButtonH, font, "+10", 20f,
                ResourceSellButton.Variant.Normal);
            x += Columns.PlusTenW + Columns.Gap;
            max = ResourceSellButton.Create(rect, "Max", x, by, Columns.MaxW, Columns.ButtonH, font, "최대", 19f,
                ResourceSellButton.Variant.Normal);

            goldCoin = ResourceSellUi.AddImage(
                ResourceSellUi.Place(rect, "GoldCoin", Columns.GoldX + 4f, (Columns.RowHeight - 30f) * 0.5f, 30f, 30f),
                coin, coin != null ? Color.white : ResourceSellUi.Gold);
            goldCoin.preserveAspect = true;
            goldText = ResourceSellUi.Label(rect, "Gold", Columns.GoldX + 44f, 0f, Columns.GoldW - 44f, Columns.RowHeight,
                font, 24f, FontStyles.Bold, ResourceSellUi.Gold, TextAlignmentOptions.Right);
            goldAnchor = ResourceSellUi.Place(rect, "GoldAnchor", Columns.GoldX + 60f, 0f, 60f, Columns.RowHeight);

            minus.Button.onClick.AddListener(() => Raise(-1));
            plus.Button.onClick.AddListener(() => Raise(1));
            plusFive.Button.onClick.AddListener(() => Raise(PlusFive));
            plusTen.Button.onClick.AddListener(() => Raise(PlusTen));
            max.Button.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(ItemId))
                {
                    MaxRequested?.Invoke(ItemId);
                }
            });
        }

        /// <summary>행 값과 버튼 활성(0이면 −, 최대면 +·+5·+10·최대 비활성)을 갱신한다.</summary>
        public void Bind(ResourceSellLine line, int quantity, bool interactable)
        {
            ItemId = line.ItemId;
            Quantity = quantity;
            if (icon != null)
            {
                icon.sprite = line.Icon;
                icon.enabled = line.Icon != null;
            }

            SetText(nameText, line.DisplayName);
            SetText(noteText, line.Note);
            // 안내가 없는 행은 이름을 세로 가운데에 둔다.
            var hasNote = !string.IsNullOrEmpty(line.Note);
            nameText.rectTransform.anchoredPosition = new Vector2(
                Columns.NameX + Columns.NameW * 0.5f,
                hasNote ? -(6f + 19f) : -Columns.RowHeight * 0.5f);
            noteText.gameObject.SetActive(hasNote);
            SetText(ownedText, ResourceSellUi.Number(line.Owned));
            SetText(priceText, ResourceSellUi.GoldText(line.UnitPrice));
            SetText(quantityText, ResourceSellUi.Number(quantity));
            SetText(goldText, ResourceSellUi.GoldText(ResourceSellQuote.LineGold(line, quantity)));

            var sellable = line.CanSell && interactable;
            minus.Button.interactable = sellable && quantity > 0;
            var below = sellable && quantity < line.MaxSelectable;
            plus.Button.interactable = below;
            plusFive.Button.interactable = below;
            plusTen.Button.interactable = below;
            max.Button.interactable = below;

            var dim = line.CanSell ? 1f : 0.55f;
            nameText.color = ResourceSellUi.WithAlpha(line.CanSell ? ResourceSellUi.TextMain : ResourceSellUi.TextMuted, 1f);
            noteText.color = line.CanSell ? ResourceSellUi.TextMuted : ResourceSellUi.WithAlpha(ResourceSellUi.Error, 0.85f);
            ownedText.color = ResourceSellUi.WithAlpha(ResourceSellUi.TextMain, dim);
            priceText.color = ResourceSellUi.WithAlpha(ResourceSellUi.Gold, dim);
            quantityText.color = quantity > 0 ? Color.white : ResourceSellUi.TextMuted;
            goldText.color = quantity > 0 ? ResourceSellUi.Gold : ResourceSellUi.WithAlpha(ResourceSellUi.Gold, 0.45f);
            if (icon != null)
            {
                icon.color = line.CanSell ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.7f);
            }

            activeTarget = quantity > 0 ? 1f : 0f;
            ApplyActive();
        }

        /// <summary>판매 성공 순간 행을 짧게 청록으로 비춘다(0~1).</summary>
        public void SetFlash(float amount)
        {
            if (flash != null)
            {
                flash.color = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.22f * Mathf.Clamp01(amount));
            }
        }

        public void SnapActive()
        {
            active = activeTarget;
            ApplyActive();
        }

        private void Update()
        {
            if (Mathf.Approximately(active, activeTarget))
            {
                return;
            }

            active = Mathf.MoveTowards(active, activeTarget, Time.unscaledDeltaTime / 0.12f);
            ApplyActive();
        }

        private void OnDisable()
        {
            SetFlash(0f);
            active = activeTarget;
            ApplyActive();
        }

        // 수량이 있는 행: 청록 테두리 + 은은한 배경 + 옅은 번짐.
        private void ApplyActive()
        {
            if (background == null)
            {
                return;
            }

            background.color = Color.Lerp(ResourceSellUi.RowIdle, ResourceSellUi.RowActive, active);
            outline.color = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, Mathf.Lerp(0.16f, 0.9f, active));
            glow.color = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.16f * active);
        }

        private void Raise(int delta)
        {
            if (!string.IsNullOrEmpty(ItemId))
            {
                AdjustRequested?.Invoke(ItemId, delta);
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null && text.text != value)
            {
                text.text = value ?? string.Empty;
            }
        }
    }
}
