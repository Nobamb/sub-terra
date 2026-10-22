using SubTerra.App.Tutorial;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 화물 게이지(B-152에서 분리, B-153과 공용): 화물함 아이콘 + "화물 적재량" + 현재/최대 + 고정 칸 + 초과 칸 + 부족 문구.
    /// 칸 계산은 CargoShortfallModel.Layout이 하고, 이 클래스는 그리기만 한다.
    /// </summary>
    internal sealed class CargoGaugeView
    {
        public const float Height = 132f;

        private const float RowX = 128f;
        private const float RowY = 58f;
        private const float RowH = 30f;
        private const float CellGap = 4f;
        private const float OverflowGap = 10f;

        private static readonly Color OverflowBase = new Color(0.13f, 0.08f, 0.02f, 1f);

        private sealed class Cell
        {
            public RectTransform Root;
            public Image Fill;
            public Image Line;
            public Image Stripes;
        }

        private CargoFullPopupSkin skin;
        private RectTransform row;
        private Cell[] mainCells;
        private Cell[] overflowCells;
        private Image overflowBracket;
        private Image overflowGlow;
        private string clearedLabel;

        public RectTransform Root { get; private set; }
        public TMP_Text LoadText { get; private set; }
        public TMP_Text ShortfallText { get; private set; }
        public CargoShortfallModel Model { get; private set; }
        public CargoGaugeLayout Layout { get; private set; }
        public int LitCellCount { get; private set; }

        /// <summary>clearedLabel: 부족이 없을 때 부족 문구 자리에 청록으로 보일 글자(B-152는 비움).</summary>
        public static CargoGaugeView Build(
            RectTransform parent, float x, float y, float width, CargoFullPopupSkin skin, TMP_FontAsset font, string clearedLabel)
        {
            var gauge = new CargoGaugeView { skin = skin, clearedLabel = clearedLabel };
            var panel = ResourceSellUi.Place(parent, "CargoGauge", x, y, width, Height);
            gauge.Root = panel;
            CargoPopupChrome.Plate(panel, skin, 0.3f);
            var icon = ResourceSellUi.AddImage(ResourceSellUi.Place(panel, "CargoIcon", 20f, 22f, 88f, 88f),
                skin != null ? skin.cargoIcon : null, Color.white);
            icon.preserveAspect = true;
            icon.enabled = skin != null && skin.cargoIcon != null;
            var textMain = CargoPopupChrome.TextMain(skin);
            var caption = ResourceSellUi.Label(panel, "Caption", RowX, 16f, 300f, 32f, font, 20f, FontStyles.Normal,
                textMain, TextAlignmentOptions.Left);
            caption.text = CargoFullPopupText.LoadCaption;
            gauge.LoadText = ResourceSellUi.Label(panel, "Load", width - 24f - 320f, 10f, 320f, 42f, font, 28f, FontStyles.Bold,
                textMain, TextAlignmentOptions.Right);
            gauge.LoadText.enableAutoSizing = true;
            gauge.LoadText.fontSizeMin = 18f;
            gauge.LoadText.fontSizeMax = 28f;
            gauge.ShortfallText = ResourceSellUi.Label(panel, "Shortfall", width - 24f - 420f, 96f, 420f, 28f, font, 20f,
                FontStyles.Bold, CargoPopupChrome.Amber(skin), TextAlignmentOptions.Right);
            gauge.ShortfallText.enableAutoSizing = true;
            gauge.ShortfallText.fontSizeMin = 14f;
            gauge.ShortfallText.fontSizeMax = 20f;

            gauge.row = ResourceSellUi.Place(panel, "GaugeRow", RowX, RowY, width - RowX - 24f, RowH);
            gauge.overflowGlow = ResourceSellUi.AddImage(ResourceSellUi.Place(gauge.row, "OverflowGlow", 0f, 0f, 10f, 10f),
                ResourceSellArt.SoftRect(), Color.clear);
            gauge.overflowGlow.enabled = false;
            gauge.mainCells = new Cell[CargoGaugeLayout.MaxCells];
            for (var i = 0; i < gauge.mainCells.Length; i++)
            {
                gauge.mainCells[i] = NewCell(gauge.row, "Cell" + i, false);
            }

            gauge.overflowCells = new Cell[CargoGaugeLayout.MaxOverflowCells];
            for (var i = 0; i < gauge.overflowCells.Length; i++)
            {
                gauge.overflowCells[i] = NewCell(gauge.row, "Overflow" + i, true);
            }

            gauge.overflowBracket = ResourceSellUi.AddImage(ResourceSellUi.Place(gauge.row, "OverflowBracket", 0f, 0f, 10f, 10f),
                CargoFullPopupArt.Outline(), CargoPopupChrome.Amber(skin));
            gauge.overflowBracket.type = Image.Type.Sliced;
            gauge.overflowBracket.pixelsPerUnitMultiplier = 0.5f;
            return gauge;
        }

        /// <summary>값과 칸 배치를 바꾼다. 색은 Apply가 입힌다.</summary>
        public void SetModel(CargoShortfallModel model, int cellCount, int maxOverflowCells)
        {
            Model = model;
            Layout = model.Layout(cellCount, maxOverflowCells);
            LoadText.text = CargoFullPopupText.LoadLabel(model);
            ShortfallText.text = model.IsShort
                ? CargoFullPopupText.ShortfallLabel(model)
                : clearedLabel ?? string.Empty;
            LayoutCells();
        }

        /// <summary>fill: 채운 칸 비율(열기 연출), flash: 초과 칸 섬광, cleared: 부족이 0이 된 순간의 한 번 밝아짐.</summary>
        public void Apply(float fill, float flash, float cleared = 0f)
        {
            var teal = CargoPopupChrome.Teal(skin);
            var empty = skin != null ? skin.cellEmpty : new Color(0.04f, 0.08f, 0.11f, 1f);
            var lit = Mathf.Clamp(Mathf.CeilToInt(Layout.FilledCells * Mathf.Clamp01(fill) - 0.001f), 0, Layout.FilledCells);
            LitCellCount = lit;
            for (var i = 0; i < mainCells.Length && i < Layout.CellCount; i++)
            {
                var on = i < lit;
                // 부족이 풀린 순간 게이지 끝 칸이 한 번 밝아진다.
                var edge = on && i == lit - 1 ? cleared : 0f;
                var cell = mainCells[i];
                cell.Fill.color = on ? Color.Lerp(teal, Color.white, 0.6f * edge) : empty;
                cell.Line.color = CargoPopupChrome.WithAlpha(teal, on ? 0.95f : 0.22f);
            }

            var amber = CargoPopupChrome.Amber(skin);
            var bright = Color.Lerp(amber, Color.white, 0.55f * flash);
            for (var i = 0; i < overflowCells.Length && i < Layout.OverflowCells; i++)
            {
                var cell = overflowCells[i];
                cell.Fill.color = Color.Lerp(OverflowBase, amber * 0.45f, flash);
                cell.Stripes.color = CargoPopupChrome.WithAlpha(bright, 0.85f + 0.15f * flash);
                cell.Line.color = CargoPopupChrome.WithAlpha(bright, 0.9f);
            }

            overflowBracket.color = CargoPopupChrome.WithAlpha(bright, 0.9f);
            overflowGlow.color = CargoPopupChrome.WithAlpha(amber, 0.45f * flash);
            overflowGlow.enabled = flash > 0.001f;
            ShortfallText.color = Model.IsShort ? amber : Color.Lerp(teal, Color.white, 0.5f * cleared);
        }

        private void LayoutCells()
        {
            var width = row.rect.width > 1f ? row.rect.width : row.sizeDelta.x;
            var units = Layout.CellCount + Layout.OverflowCells;
            var extra = Layout.OverflowCells > 0 ? OverflowGap : 0f;
            var cellW = (width - extra - (units - 1) * CellGap) / Mathf.Max(1, units);
            for (var i = 0; i < mainCells.Length; i++)
            {
                var visible = i < Layout.CellCount;
                mainCells[i].Root.gameObject.SetActive(visible);
                if (visible)
                {
                    CargoPopupChrome.SetRect(mainCells[i].Root, i * (cellW + CellGap), 0f, cellW, RowH);
                }
            }

            var overflowX = Layout.CellCount * (cellW + CellGap) - CellGap + extra;
            for (var i = 0; i < overflowCells.Length; i++)
            {
                var visible = i < Layout.OverflowCells;
                overflowCells[i].Root.gameObject.SetActive(visible);
                if (visible)
                {
                    CargoPopupChrome.SetRect(overflowCells[i].Root, overflowX + i * (cellW + CellGap), 0f, cellW, RowH);
                }
            }

            var showOverflow = Layout.OverflowCells > 0;
            overflowBracket.gameObject.SetActive(showOverflow);
            overflowGlow.gameObject.SetActive(showOverflow);
            if (showOverflow)
            {
                var w = Layout.OverflowCells * (cellW + CellGap) - CellGap;
                CargoPopupChrome.SetRect(overflowBracket.rectTransform, overflowX - 4f, -4f, w + 8f, RowH + 8f);
                CargoPopupChrome.SetRect(overflowGlow.rectTransform, overflowX - 18f, -18f, w + 36f, RowH + 36f);
            }
        }

        private static Cell NewCell(Transform parent, string cellName, bool overflow)
        {
            var root = ResourceSellUi.Place(parent, cellName, 0f, 0f, 10f, RowH);
            var fill = ResourceSellUi.Image(root, "Fill", overflow ? null : CargoFullPopupArt.Cell(), Color.clear);
            fill.type = overflow ? Image.Type.Simple : Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = 0.5f;
            Image stripes = null;
            if (overflow)
            {
                stripes = ResourceSellUi.Image(root, "Stripes", CargoFullPopupArt.Stripes(), Color.clear);
                stripes.type = Image.Type.Tiled;
                stripes.pixelsPerUnitMultiplier = 0.5f;
                stripes.rectTransform.offsetMin = new Vector2(2f, 2f);
                stripes.rectTransform.offsetMax = new Vector2(-2f, -2f);
            }

            var line = ResourceSellUi.Image(root, "Line", CargoFullPopupArt.Outline(), Color.clear);
            line.type = Image.Type.Sliced;
            line.pixelsPerUnitMultiplier = 0.5f;
            return new Cell { Root = root, Fill = fill, Line = line, Stripes = stripes };
        }
    }
}
