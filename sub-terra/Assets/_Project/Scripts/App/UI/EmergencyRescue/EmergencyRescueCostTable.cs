using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyRescue
{
    /// <summary>
    /// 구출 비용 표: 자원 아이콘·이름 / 차감량 / 현재 보유량 → 구출 후 잔량.
    /// 모든 줄이 같은 열 위치를 쓰고 숫자는 오른쪽 정렬이라 자릿수가 달라도 열이 맞는다.
    /// 값은 EmergencyRescueCostRows가 기존 계산 결과를 옮긴 문자열 그대로 보여 준다.
    /// </summary>
    internal sealed class EmergencyRescueCostTable
    {
        // 카드 중앙 기준 열 좌표. 이름은 왼쪽 정렬, 숫자 열은 오른쪽 끝 정렬.
        public const float PlateHalfWidth = 354f;
        public const float IconCenterX = -330f;
        public const float NameLeftX = -306f;
        public const float NameRightX = -130f;
        public const float DeductRightX = -10f;
        public const float BeforeRightX = 120f;
        public const float ArrowCenterX = 154f;
        public const float AfterRightX = 350f;
        public const float DeductWidth = 120f;
        public const float BeforeWidth = 130f;
        public const float AfterWidth = 170f;

        public const float HeaderY = 117f;
        public const float HeaderHeight = 30f;
        public const float RowsTopY = 102f;
        public const float RowsBottomY = -142f;
        public const float MaxRowHeight = 40f;
        public const string ArrowGlyph = "→";

        private static readonly Color PlateEven = new Color(0.03f, 0.10f, 0.14f, 0.78f);
        private static readonly Color PlateOdd = new Color(0.02f, 0.07f, 0.10f, 0.6f);
        private static readonly Color DeductColor = new Color(1f, 0.56f, 0.50f, 1f);
        private static readonly Color AfterColor = new Color(0.66f, 1f, 0.97f, 1f);
        private static readonly Color GoldColor = new Color(1f, 0.82f, 0.32f, 1f);

        private sealed class Row
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Plate;
            public Image Icon;
            public TMP_Text Name;
            public TMP_Text Deduct;
            public TMP_Text Before;
            public TMP_Text Arrow;
            public TMP_Text After;
        }

        private readonly RectTransform root;
        private readonly TMP_FontAsset font;
        private readonly List<Row> rows = new List<Row>();
        private readonly GameObject header;
        private readonly TMP_Text emptyNote;

        public EmergencyRescueCostTable(Transform parent, TMP_FontAsset font)
        {
            this.font = font;
            root = EmergencyRescueUi.Rect(parent, "CostTable", Vector2.zero, new Vector2(PlateHalfWidth * 2f, 300f));

            RectTransform headerRect = EmergencyRescueUi.Rect(root, "Header", Vector2.zero, Vector2.zero);
            header = headerRect.gameObject;
            AddHeaderText(headerRect, "자원", NameLeftX, NameRightX, TextAlignmentOptions.MidlineLeft);
            AddHeaderText(headerRect, "차감량", DeductRightX - DeductWidth, DeductRightX, TextAlignmentOptions.MidlineRight);
            AddHeaderText(headerRect, "현재 보유", BeforeRightX - BeforeWidth, BeforeRightX, TextAlignmentOptions.MidlineRight);
            AddHeaderText(headerRect, "구출 후 잔량", AfterRightX - AfterWidth, AfterRightX, TextAlignmentOptions.MidlineRight);
            EmergencyRescueUi.Image(
                headerRect, "HeaderLine", null, new Vector2(0f, HeaderY - HeaderHeight * 0.5f),
                new Vector2(PlateHalfWidth * 2f - 8f, 1.5f),
                EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Teal, 0.4f));

            emptyNote = EmergencyRescueUi.Text(
                root, "EmptyNote", font, 24f, FontStyles.Bold, AfterColor,
                TextAlignmentOptions.Center, new Vector2(0f, 20f), new Vector2(680f, 80f));
            emptyNote.textWrappingMode = TextWrappingModes.Normal;
            emptyNote.gameObject.SetActive(false);
        }

        public int VisibleRowCount { get; private set; }
        public bool HeaderVisible => header != null && header.activeSelf;
        public bool EmptyNoteVisible => emptyNote != null && emptyNote.gameObject.activeSelf;
        public IReadOnlyList<GameObject> RowObjects
        {
            get
            {
                var list = new List<GameObject>(rows.Count);
                for (var i = 0; i < rows.Count; i++)
                {
                    list.Add(rows[i].Root);
                }

                return list;
            }
        }

        public static float RowHeightFor(int count)
        {
            if (count <= 0)
            {
                return MaxRowHeight;
            }

            return Mathf.Min(MaxRowHeight, (RowsTopY - RowsBottomY) / count);
        }

        public void Apply(
            IReadOnlyList<EmergencyRescueCostRow> data,
            string emptyMessage,
            Func<string, Sprite> iconResolver)
        {
            var count = data != null ? data.Count : 0;
            VisibleRowCount = count;
            header.SetActive(count > 0);
            emptyNote.gameObject.SetActive(count == 0);
            emptyNote.text = emptyMessage ?? string.Empty;

            var rowHeight = RowHeightFor(count);
            // 줄 수가 적으면 머리줄+행 묶음을 표 영역 가운데로 내려 위아래 여백을 맞춘다.
            var block = HeaderHeight + rowHeight * count;
            var shift = count > 0 ? -Mathf.Max(0f, (RowsTopY + HeaderHeight - RowsBottomY - block) * 0.5f) : 0f;
            ((RectTransform)header.transform).anchoredPosition = new Vector2(0f, shift);
            for (var i = 0; i < count; i++)
            {
                Row row = EnsureRow(i);
                row.Root.SetActive(true);
                var y = RowsTopY + shift - rowHeight * (i + 0.5f);
                row.Rect.anchoredPosition = new Vector2(0f, y);
                row.Rect.sizeDelta = new Vector2(PlateHalfWidth * 2f, rowHeight - 4f);
                row.Plate.color = i % 2 == 0 ? PlateEven : PlateOdd;
                EmergencyRescueCostRow item = data[i];

                Sprite icon = item.IsGold
                    ? EmergencyRescueArt.Coin()
                    : (iconResolver != null ? iconResolver(item.ResourceId) : null);
                row.Icon.sprite = icon;
                row.Icon.enabled = icon != null;
                row.Icon.color = item.IsGold ? GoldColor : Color.white;
                row.Icon.preserveAspect = true;
                row.Name.text = item.Name;
                row.Deduct.text = item.Deduct;
                row.Before.text = item.Before;
                row.Arrow.text = ArrowGlyph;
                row.After.text = item.After;
            }

            for (var i = count; i < rows.Count; i++)
            {
                rows[i].Root.SetActive(false);
            }
        }

        private void AddHeaderText(
            RectTransform parent, string text, float left, float right, TextAlignmentOptions alignment)
        {
            TMP_Text label = EmergencyRescueUi.Text(
                parent, "Header" + text, font, 18f, FontStyles.Bold, EmergencyRescueUi.TextMuted, alignment,
                new Vector2((left + right) * 0.5f, HeaderY), new Vector2(right - left, HeaderHeight));
            label.text = text;
        }

        private Row EnsureRow(int index)
        {
            while (rows.Count <= index)
            {
                rows.Add(CreateRow(rows.Count));
            }

            return rows[index];
        }

        private Row CreateRow(int index)
        {
            RectTransform rect = EmergencyRescueUi.Rect(
                root, "Row" + index, Vector2.zero, new Vector2(PlateHalfWidth * 2f, MaxRowHeight - 4f));
            var row = new Row { Root = rect.gameObject, Rect = rect };
            row.Plate = rect.gameObject.AddComponent<Image>();
            row.Plate.raycastTarget = false;
            row.Icon = EmergencyRescueUi.Image(rect, "Icon", null, new Vector2(IconCenterX, 0f), new Vector2(30f, 30f), Color.white);
            row.Name = AddCell(rect, "Name", NameLeftX, NameRightX, TextAlignmentOptions.MidlineLeft, EmergencyRescueUi.TextMain, FontStyles.Bold);
            row.Name.enableAutoSizing = true;
            row.Name.fontSizeMin = 16f;
            row.Name.fontSizeMax = 24f;
            row.Deduct = AddCell(rect, "Deduct", DeductRightX - DeductWidth, DeductRightX, TextAlignmentOptions.MidlineRight, DeductColor, FontStyles.Bold);
            row.Before = AddCell(rect, "Before", BeforeRightX - BeforeWidth, BeforeRightX, TextAlignmentOptions.MidlineRight, EmergencyRescueUi.TextMain, FontStyles.Normal);
            row.Arrow = AddCell(rect, "Arrow", ArrowCenterX - 18f, ArrowCenterX + 18f, TextAlignmentOptions.Center, EmergencyRescueUi.Teal, FontStyles.Bold);
            row.After = AddCell(rect, "After", AfterRightX - AfterWidth, AfterRightX, TextAlignmentOptions.MidlineRight, AfterColor, FontStyles.Bold);
            return row;
        }

        private TMP_Text AddCell(
            RectTransform parent,
            string name,
            float left,
            float right,
            TextAlignmentOptions alignment,
            Color color,
            FontStyles style)
        {
            return EmergencyRescueUi.Text(
                parent, name, font, 24f, style, color, alignment,
                new Vector2((left + right) * 0.5f, 0f), new Vector2(right - left, 36f));
        }
    }
}
