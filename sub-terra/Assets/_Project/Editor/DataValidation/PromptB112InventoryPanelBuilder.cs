using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.Inventory;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 112 → B-190: 인벤토리 창(InventoryPanel.prefab)만 재구성한다.
    /// 헤더 + 가로 적재 현황(미정산 스탬프 포함) + 화물칸 명세표(적재 내역) 구조로 바꾸고,
    /// 기존 PanelRoot·행·텍스트·닫기 버튼 오브젝트는 그대로 재사용해 Scene 참조를 보존한다.
    /// </summary>
    public static class PromptB112InventoryPanelBuilder
    {
        public const string InventoryPanelPrefabPath = "Assets/_Project/Prefabs/UI/InventoryPanel.prefab";
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        // SurfaceBasePanel 인스턴스가 루트 크기를 덮어쓰므로 창 크기는 유지한다.
        public const float PanelWidth = 560f;
        public const float PanelHeight = 532f;
        // 화면 중앙의 플레이어·드론 대사와 겹치지 않도록 건설창(좌)·우측 메뉴 사이 오른쪽으로 둔다.
        public const float PanelOffsetX = 330f;
        public const float HeaderHeight = 54f;
        public const float Margin = 16f;
        public const float ContentWidth = PanelWidth - Margin * 2f;

        // A. 적재 현황(가로 전체) + 오른쪽 미정산 스탬프
        public const float CardTop = -64f;
        public const float CardHeight = 118f;
        public const float CardPadding = 14f;
        public const float StampWidth = 124f;
        public const float StampHeight = 92f;
        public const float StampInset = 12f;
        public const float GaugeWidth = ContentWidth - CardPadding - StampWidth - StampInset - 18f;

        // B. 적재 내역 표
        public const float ListFrameTop = -192f;
        public const float ListFrameHeight = PanelHeight + ListFrameTop - Margin;
        public const float ListInset = 10f;
        public const float ListLabelTop = -200f;
        public const float ColumnHeaderTop = -230f;
        public const float ColumnHeaderHeight = 20f;
        public const float ListTop = -256f;
        public const float ListHeight = PanelHeight + ListTop - Margin - 8f;
        public const float RowWidth = ContentWidth - ListInset * 2f;
        public const float RowHeight = 60f;
        public const float RowSpacing = 2f;

        // 행 안 열(행 왼쪽 기준). 숫자 열은 오른쪽 끝을 맞춘다.
        public const float IconX = 12f;
        public const float IconSize = 40f;
        public const float NameX = 62f;
        public const float UnitWeightRight = 284f;
        public const float UnitWeightWidth = 70f;
        public const float QuantityRight = 380f;
        public const float QuantityWidth = 84f;
        public const float LineWeightRight = RowWidth - 12f;
        public const float LineWeightWidth = 104f;
        public const float NameWidth = UnitWeightRight - UnitWeightWidth - NameX - 10f;

        public const string EmptyHintText = "적재된 자원이 없습니다";

        private static readonly Color PanelColor = new Color(0.022f, 0.045f, 0.065f, 1f);
        private static readonly Color HeaderColor = new Color(0.035f, 0.085f, 0.115f, 1f);
        private static readonly Color CardColor = new Color(0.04f, 0.085f, 0.115f, 1f);
        private static readonly Color ListFrameColor = new Color(0.03f, 0.065f, 0.09f, 1f);
        private static readonly Color RowColor = new Color(0.05f, 0.11f, 0.14f, 0.55f);
        private static readonly Color StampColor = new Color(0.13f, 0.09f, 0.03f, 1f);
        public static readonly Color Cyan = new Color(0.45f, 0.95f, 1f, 1f);
        private static readonly Color CyanLine = new Color(0f, 0.85f, 1f, 0.4f);
        private static readonly Color CyanFaint = new Color(0.2f, 0.8f, 0.95f, 0.2f);
        private static readonly Color Amber = new Color(1f, 0.72f, 0.2f, 1f);
        private static readonly Color Gold = new Color(1f, 0.84f, 0.45f, 1f);
        private static readonly Color BodyText = new Color(0.8f, 0.89f, 0.93f, 1f);
        private static readonly Color SubText = new Color(0.6f, 0.78f, 0.85f, 1f);

        [MenuItem("SubTerra/UI/Build Prompt-B 112 Inventory Panel Rework")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode first.");
            }

            var root = PrefabUtility.LoadPrefabContents(InventoryPanelPrefabPath);
            try
            {
                var report = ApplyTo(root);
                PrefabUtility.SaveAsPrefabAsset(root, InventoryPanelPrefabPath);
                return report;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>목록 영역에 한 번에 보이는 행 수(간격 포함).</summary>
        public static float VisibleRowCount()
        {
            return (ListHeight + RowSpacing) / (RowHeight + RowSpacing);
        }

        /// <summary>기본 인벤토리 빌더가 프리팹을 재생성해도 같은 구조를 다시 입힌다.</summary>
        public static string ApplyTo(GameObject root)
        {
            var view = root.GetComponent<InventoryPanelView>();
            var panel = root.transform.Find("PanelRoot");
            var cargo = panel != null ? panel.Find("CargoSummaryText") : null;
            if (view == null || panel == null || cargo == null)
            {
                throw new InvalidOperationException("InventoryPanelView/PanelRoot/CargoSummaryText가 없습니다.");
            }

            var font = cargo.GetComponent<TMP_Text>().font;

            var rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            rootRect.anchoredPosition = new Vector2(PanelOffsetX, 0f);

            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = null;
            panelImage.color = PanelColor;
            panelImage.raycastTarget = true;
            EnsureOutline(panel.gameObject, new Color(0.2f, 0.75f, 0.9f, 0.55f), 1.5f);

            BuildHeader(panel, font);
            var gauge = BuildCargoCard(panel, font);
            var value = BuildValueCard(panel, font);
            var listHeader = BuildListHeader(panel, font);
            var rows = BuildList(panel, font);
            RemoveFooter(panel);
            StyleWeightHelp(panel);
            BuildCorners(panel);
            StyleClose(root.transform, font);

            // 문자열 계약(HudFormatter 요약)은 유지하되 화면에는 새 카드만 표시한다.
            cargo.gameObject.SetActive(false);
            var unsettled = panel.Find("UnsettledValueText");
            if (unsettled != null)
            {
                unsettled.gameObject.SetActive(false);
            }

            OrderSiblings(panel);

            var so = new SerializedObject(view);
            so.FindProperty("cargoAmountText").objectReferenceValue = gauge.Amount;
            so.FindProperty("cargoPercentText").objectReferenceValue = gauge.Percent;
            so.FindProperty("cargoStateText").objectReferenceValue = gauge.State;
            so.FindProperty("cargoFill").objectReferenceValue = gauge.Fill.rectTransform;
            so.FindProperty("cargoFillImage").objectReferenceValue = gauge.Fill;
            so.FindProperty("unsettledAmountText").objectReferenceValue = value;
            so.FindProperty("ownedKindsText").objectReferenceValue = listHeader.Kinds;
            so.FindProperty("emptyHintText").objectReferenceValue = listHeader.Empty;
            so.FindProperty("columnHeaderRoot").objectReferenceValue = listHeader.Columns;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);

            return "Prompt-B 190 InventoryPanel manifest rebuilt: rows=" + rows + " rowHeight=" + RowHeight
                + " visibleRows=" + VisibleRowCount().ToString("0.0") + " size=" + PanelWidth + "x" + PanelHeight;
        }

        private static void BuildHeader(Transform panel, TMP_FontAsset font)
        {
            var band = EnsureImage(panel, "HeaderBand", HeaderColor);
            band.rectTransform.anchorMin = new Vector2(0f, 1f);
            band.rectTransform.anchorMax = new Vector2(1f, 1f);
            band.rectTransform.pivot = new Vector2(0.5f, 1f);
            band.rectTransform.anchoredPosition = Vector2.zero;
            band.rectTransform.sizeDelta = new Vector2(0f, HeaderHeight);

            // 조작 안내는 게임 가이드에만 둔다(B-190). 왼쪽 강조 막대는 가운데 제목으로 대체한다.
            RemoveChild(panel, "HotkeyHint");
            RemoveChild(panel, "HeaderAccent");

            var title = EnsureText(panel, "Title", font);
            Style(title, 22f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            title.text = "인벤토리";
            PlaceTopCenter(title.rectTransform, 0f, -5f, 260f, 28f);

            var tag = EnsureText(panel, "HeaderTag", font);
            Style(tag, 10.5f, SubText, TextAlignmentOptions.Center, FontStyles.Bold);
            tag.characterSpacing = 3f;
            tag.text = "CARGO INVENTORY";
            PlaceTopCenter(tag.rectTransform, 0f, -33f, 200f, 14f);

            var left = EnsureImage(panel, "HeaderTagLineL", CyanLine);
            PlaceTopCenter(left.rectTransform, -112f, -40f, 60f, 1f);
            var right = EnsureImage(panel, "HeaderTagLineR", CyanLine);
            PlaceTopCenter(right.rectTransform, 112f, -40f, 60f, 1f);

            var divider = EnsureImage(panel, "HeaderDivider", new Color(0f, 0.85f, 1f, 0.55f));
            divider.rectTransform.anchorMin = new Vector2(0f, 1f);
            divider.rectTransform.anchorMax = new Vector2(1f, 1f);
            divider.rectTransform.pivot = new Vector2(0.5f, 1f);
            divider.rectTransform.anchoredPosition = new Vector2(0f, -HeaderHeight);
            divider.rectTransform.sizeDelta = new Vector2(0f, 1.5f);
        }

        private readonly struct GaugeRefs
        {
            public readonly TMP_Text Amount;
            public readonly TMP_Text Percent;
            public readonly TMP_Text State;
            public readonly Image Fill;

            public GaugeRefs(TMP_Text amount, TMP_Text percent, TMP_Text state, Image fill)
            {
                Amount = amount;
                Percent = percent;
                State = state;
                Fill = fill;
            }
        }

        private static GaugeRefs BuildCargoCard(Transform panel, TMP_FontAsset font)
        {
            // A. 적재 현황: 현재/최대 → % → 게이지 → 남은 적재량·상태 순으로 읽힌다.
            var card = EnsureImage(panel, "CargoCard", CardColor);
            PlaceTopLeft(card.rectTransform, Margin, CardTop, ContentWidth, CardHeight);
            EnsureOutline(card.gameObject, new Color(0.3f, 0.85f, 1f, 0.3f), 1f);

            var label = EnsureText(card.transform, "CargoLabel", font);
            Style(label, 16f, Cyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            label.text = "적재 현황";
            PlaceTopLeft(label.rectTransform, CardPadding, -10f, 72f, 20f);

            var amount = EnsureText(card.transform, "CargoAmountText", font);
            Style(amount, 32f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            amount.text = InventoryPanelDisplayFormatter.LoadAmount(0f, 0f);
            PlaceTopLeft(amount.rectTransform, CardPadding, -30f, GaugeWidth - 110f, 42f);

            var percent = EnsureText(card.transform, "CargoPercentText", font);
            Style(percent, 24f, SubText, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
            percent.text = "0%";
            PlaceTopLeft(percent.rectTransform, CardPadding + GaugeWidth - 100f, -34f, 100f, 36f);

            var track = EnsureImage(card.transform, "CargoBarTrack", new Color(1f, 1f, 1f, 0.08f));
            PlaceTopLeft(track.rectTransform, CardPadding, -76f, GaugeWidth, 10f);
            EnsureOutline(track.gameObject, new Color(0.3f, 0.85f, 1f, 0.25f), 1f);

            var fill = EnsureImage(track.transform, "CargoBarFill", Cyan);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            // 거의 가득 기준선(80%)을 막대 위에 표시한다.
            var tick = EnsureImage(track.transform, "NearFullTick", new Color(Amber.r, Amber.g, Amber.b, 0.85f));
            tick.rectTransform.anchorMin = new Vector2(InventoryPanelDisplayFormatter.NearFullRatio, 0f);
            tick.rectTransform.anchorMax = new Vector2(InventoryPanelDisplayFormatter.NearFullRatio, 1f);
            tick.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            tick.rectTransform.anchoredPosition = Vector2.zero;
            tick.rectTransform.sizeDelta = new Vector2(2f, 6f);

            var state = EnsureText(card.transform, "CargoStateText", font);
            Style(state, 14f, SubText, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            state.text = InventoryPanelDisplayFormatter.StateLine(0f, 0f);
            PlaceTopLeft(state.rectTransform, CardPadding, -92f, GaugeWidth, 20f);

            return new GaugeRefs(amount, percent, state, fill);
        }

        private static TMP_Text BuildValueCard(Transform panel, TMP_FontAsset font)
        {
            // 미정산 가치: 적재 현황 오른쪽 끝의 작은 스탬프. 적재량 숫자(32)보다 작게 둔다.
            var card = EnsureImage(panel, "ValueCard", StampColor);
            PlaceTopLeft(card.rectTransform,
                Margin + ContentWidth - StampInset - StampWidth,
                CardTop - (CardHeight - StampHeight) * 0.5f,
                StampWidth,
                StampHeight);
            EnsureOutline(card.gameObject, new Color(Amber.r, Amber.g, Amber.b, 0.75f), 1.5f);
            RemoveChild(card.transform, "ValueAccent");
            var inner = StampWidth - 20f;

            var label = EnsureText(card.transform, "ValueLabel", font);
            Style(label, 13f, Amber, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            label.text = "미정산 가치";
            PlaceTopLeft(label.rectTransform, 10f, -8f, inner, 18f);

            var amount = EnsureText(card.transform, "UnsettledAmountText", font);
            Style(amount, 24f, Gold, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            amount.enableAutoSizing = true;
            amount.fontSizeMin = 13f;
            amount.fontSizeMax = 24f;
            amount.text = InventoryPanelDisplayFormatter.UnsettledAmount(0f);
            PlaceTopLeft(amount.rectTransform, 10f, -28f, inner, 34f);

            var caption = EnsureText(card.transform, "ValueCaption", font);
            Style(caption, 11f, SubText, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            caption.text = "정산 시 받을 금액";
            PlaceTopLeft(caption.rectTransform, 10f, -66f, inner, 16f);
            return amount;
        }

        private readonly struct ListHeaderRefs
        {
            public readonly TMP_Text Kinds;
            public readonly TMP_Text Empty;
            public readonly GameObject Columns;

            public ListHeaderRefs(TMP_Text kinds, TMP_Text empty, GameObject columns)
            {
                Kinds = kinds;
                Empty = empty;
                Columns = columns;
            }
        }

        private static ListHeaderRefs BuildListHeader(Transform panel, TMP_FontAsset font)
        {
            var frame = EnsureImage(panel, "ListFrame", ListFrameColor);
            PlaceTopLeft(frame.rectTransform, Margin, ListFrameTop, ContentWidth, ListFrameHeight);
            EnsureOutline(frame.gameObject, new Color(0.3f, 0.85f, 1f, 0.3f), 1f);

            var label = EnsureText(panel, "ListSectionLabel", font);
            Style(label, 17f, Cyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            label.text = "적재 내역";
            PlaceTopLeft(label.rectTransform, Margin + ListInset + 4f, ListLabelTop, 82f, 24f);

            var kinds = EnsureText(panel, "OwnedKindsText", font);
            Style(kinds, 14f, SubText, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            kinds.text = InventoryPanelDisplayFormatter.LoadedKinds(0);
            PlaceTopLeft(kinds.rectTransform, Margin + ListInset + 90f, ListLabelTop, 80f, 24f);

            RemoveChild(panel, "ListDivider");

            // 표 머리: 자원 | 개당 무게 | 수량 | 이 줄 무게. 숫자 열은 행과 같은 오른쪽 끝을 쓴다.
            var columns = EnsureRect(panel, "ColumnHeader");
            PlaceTopLeft(columns, Margin + ListInset, ColumnHeaderTop, RowWidth, ColumnHeaderHeight + 4f);
            HeaderCell(columns, "ColResource", font, "자원", IconX, 120f, TextAlignmentOptions.MidlineLeft);
            HeaderCell(columns, "ColUnitWeight", font, "개당 무게",
                UnitWeightRight - UnitWeightWidth, UnitWeightWidth, TextAlignmentOptions.MidlineRight);
            HeaderCell(columns, "ColQuantity", font, "수량",
                QuantityRight - QuantityWidth, QuantityWidth, TextAlignmentOptions.MidlineRight);
            HeaderCell(columns, "ColLineWeight", font, "이 줄 무게",
                LineWeightRight - LineWeightWidth, LineWeightWidth, TextAlignmentOptions.MidlineRight);
            var rule = EnsureImage(columns, "ColumnRule", CyanLine);
            PlaceTopLeft(rule.rectTransform, 0f, -(ColumnHeaderHeight + 2f), RowWidth, 1.5f);

            var empty = EnsureText(panel, "EmptyHint", font);
            Style(empty, 14f, SubText, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            empty.text = EmptyHintText;
            PlaceTopLeft(empty.rectTransform, Margin + ListInset + 4f, ColumnHeaderTop, RowWidth - 8f, 22f);
            return new ListHeaderRefs(kinds, empty, columns.gameObject);
        }

        private static void HeaderCell(Transform parent, string name, TMP_FontAsset font, string text,
            float x, float width, TextAlignmentOptions alignment)
        {
            var cell = EnsureText(parent, name, font);
            Style(cell, 12.5f, SubText, alignment, FontStyles.Bold);
            cell.text = text;
            PlaceTopLeft(cell.rectTransform, x, 0f, width, ColumnHeaderHeight);
        }

        private static int BuildList(Transform panel, TMP_FontAsset font)
        {
            var scroll = EnsureRect(panel, "StackScroll");
            PlaceTopLeft(scroll, Margin + ListInset, ListTop, RowWidth, ListHeight);

            var viewport = EnsureRect(scroll, "Viewport");
            Stretch(viewport);
            // 빈 공간에서도 휠 스크롤을 받도록 투명 Image로 레이캐스트한다.
            var viewportImage = viewport.GetComponent<Image>();
            if (viewportImage == null)
            {
                viewportImage = viewport.gameObject.AddComponent<Image>();
            }

            viewportImage.sprite = null;
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = true;
            if (viewport.GetComponent<RectMask2D>() == null)
            {
                viewport.gameObject.AddComponent<RectMask2D>();
            }

            // 기존 StackRows를 Content로 재사용한다(행 오브젝트·참조 보존).
            var content = (RectTransform)panel.Find("StackRows");
            if (content == null)
            {
                content = (RectTransform)scroll.Find("Viewport/StackRows");
            }

            if (content == null)
            {
                throw new InvalidOperationException("StackRows가 없습니다.");
            }

            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.spacing = RowSpacing;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scroll.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            }

            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;
            scrollRect.inertia = false;

            var weights = LoadUnitWeights();
            var rows = content.GetComponentsInChildren<InventoryStackRowView>(true);
            foreach (var row in rows)
            {
                BuildRow(row, font, weights);
            }

            return rows.Length;
        }

        private static void BuildRow(InventoryStackRowView row, TMP_FontAsset font, Dictionary<string, float> weights)
        {
            var go = row.gameObject;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, RowHeight);

            var bg = go.GetComponent<Image>();
            bg.sprite = null;
            bg.color = RowColor;
            // 스크롤 휠 입력을 받기 위해 행 배경은 레이캐스트를 유지한다.
            bg.raycastTarget = true;
            var rowOutline = go.GetComponent<Outline>();
            if (rowOutline != null)
            {
                UnityEngine.Object.DestroyImmediate(rowOutline);
            }

            // 명세표에서는 보유 상태 문구·강조 막대 대신 행 아래 규칙선으로 구분한다.
            RemoveChild(go.transform, "Accent");
            RemoveChild(go.transform, "State");

            var ruleLine = EnsureImage(go.transform, "RowRule", CyanFaint);
            ruleLine.rectTransform.anchorMin = new Vector2(0f, 0f);
            ruleLine.rectTransform.anchorMax = new Vector2(1f, 0f);
            ruleLine.rectTransform.pivot = new Vector2(0.5f, 0f);
            ruleLine.rectTransform.anchoredPosition = Vector2.zero;
            ruleLine.rectTransform.sizeDelta = new Vector2(-16f, 1f);

            var iconTop = -(RowHeight - IconSize) * 0.5f;
            var frame = EnsureImage(go.transform, "IconFrame", new Color(0.02f, 0.05f, 0.07f, 1f));
            PlaceTopLeft(frame.rectTransform, IconX - 2f, iconTop + 2f, IconSize + 4f, IconSize + 4f);
            EnsureOutline(frame.gameObject, new Color(0.3f, 0.85f, 1f, 0.35f), 1f);

            var icon = go.transform.Find("Icon").GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            PlaceTopLeft(icon.rectTransform, IconX, iconTop, IconSize, IconSize);

            // 긴 이름만 두 줄로 줄이고 숫자 열을 침범하지 않는다.
            var name = go.transform.Find("Name").GetComponent<TMP_Text>();
            name.font = font;
            Style(name, 17f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            name.textWrappingMode = TextWrappingModes.Normal;
            name.enableAutoSizing = true;
            name.fontSizeMin = 12f;
            name.fontSizeMax = 17f;
            name.maxVisibleLines = 2;
            PlaceTopLeft(name.rectTransform, NameX, -4f, NameWidth, RowHeight - 8f);

            var weight = EnsureText(go.transform, "UnitWeight", font);
            Style(weight, 18f, BodyText, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
            float unitWeight;
            if (!weights.TryGetValue(row.MineralId, out unitWeight))
            {
                unitWeight = 0f;
            }

            weight.text = InventoryPanelDisplayFormatter.UnitWeight(unitWeight);
            PlaceTopLeft(weight.rectTransform, UnitWeightRight - UnitWeightWidth, 0f, UnitWeightWidth, RowHeight);

            var quantity = go.transform.Find("Quantity").GetComponent<TMP_Text>();
            quantity.font = font;
            Style(quantity, 20f, Color.white, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
            quantity.text = InventoryPanelDisplayFormatter.Quantity(0);
            quantity.enableAutoSizing = true;
            quantity.fontSizeMin = 12f;
            quantity.fontSizeMax = 20f;
            PlaceTopLeft(quantity.rectTransform, QuantityRight - QuantityWidth, 0f, QuantityWidth, RowHeight);

            var line = EnsureText(go.transform, "LineWeight", font);
            Style(line, 20f, Cyan, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
            line.text = InventoryPanelDisplayFormatter.LineWeight(0, unitWeight);
            line.enableAutoSizing = true;
            line.fontSizeMin = 12f;
            line.fontSizeMax = 20f;
            PlaceTopLeft(line.rectTransform, LineWeightRight - LineWeightWidth, 0f, LineWeightWidth, RowHeight);

            // 배경 → 규칙선 → 아이콘 틀 → 아이콘 → 글자 순으로 그린다.
            ruleLine.transform.SetSiblingIndex(0);
            frame.transform.SetSiblingIndex(1);
            icon.transform.SetSiblingIndex(2);

            row.EditorSetDetailReferences(bg, weight, line);
            var rowSo = new SerializedObject(row);
            rowSo.FindProperty("ownedBackground").colorValue = RowColor;
            rowSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(row);
        }

        private static Dictionary<string, float> LoadUnitWeights()
        {
            var result = new Dictionary<string, float>();
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            if (catalog == null)
            {
                return result;
            }

            AppendWeights(result, catalog.Minerals);
            AppendWeights(result, catalog.RareItems);
            return result;
        }

        private static void AppendWeights(Dictionary<string, float> target, IReadOnlyList<MineralData> items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                if (item != null && !string.IsNullOrEmpty(item.Id))
                {
                    target[item.Id] = item.UnitWeight;
                }
            }
        }

        private static void RemoveFooter(Transform panel)
        {
            // B-190: 하단 조작 안내(I 닫기 · 휠 이동)는 창에 상시로 두지 않는다.
            RemoveChild(panel, "ControlsDivider");
            RemoveChild(panel, "ControlsHint");
        }

        private static void StyleWeightHelp(Transform panel)
        {
            var icon = panel.Find("WeightHelpIcon");
            var tooltip = panel.Find("WeightTooltip");
            if (icon == null || tooltip == null)
            {
                return;
            }

            // ?는 "적재 현황" 제목 옆에 두고, 도움말은 창 아래에 띄워 카드·표 숫자를 덮지 않는다.
            PlaceTopLeft((RectTransform)icon, Margin + CardPadding + 78f, CardTop - 9f, 22f, 22f);
            var image = icon.GetComponent<Image>();
            image.color = Amber;
            image.raycastTarget = true;
            var outline = icon.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(0.05f, 0.075f, 0.11f, 1f);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            var label = icon.Find("Label").GetComponent<TMP_Text>();
            label.fontSize = 16f;

            PlaceTopLeft((RectTransform)tooltip, Margin, -PanelHeight - 8f, ContentWidth, 130f);
            var tipImage = tooltip.GetComponent<Image>();
            tipImage.color = new Color(0.03f, 0.055f, 0.08f, 0.98f);
            tipImage.raycastTarget = false;
            EnsureOutline(tooltip.gameObject, new Color(1f, 0.72f, 0.2f, 0.6f), 1f);
            var description = tooltip.Find("Description").GetComponent<TMP_Text>();
            description.fontSize = 14.5f;
            description.lineSpacing = 4f;
            description.color = BodyText;
        }

        private static void BuildCorners(Transform panel)
        {
            const float length = 22f;
            const float thickness = 3f;
            var corners = new[]
            {
                ("TL", new Vector2(0f, 1f)),
                ("TR", new Vector2(1f, 1f)),
                ("BL", new Vector2(0f, 0f)),
                ("BR", new Vector2(1f, 0f))
            };
            foreach (var (id, anchor) in corners)
            {
                var h = EnsureImage(panel, "Corner" + id + "_H", Cyan);
                var v = EnsureImage(panel, "Corner" + id + "_V", Cyan);
                foreach (var image in new[] { h, v })
                {
                    image.rectTransform.anchorMin = image.rectTransform.anchorMax = anchor;
                    image.rectTransform.pivot = anchor;
                    image.rectTransform.anchoredPosition = Vector2.zero;
                }

                h.rectTransform.sizeDelta = new Vector2(length, thickness);
                v.rectTransform.sizeDelta = new Vector2(thickness, length);
            }
        }

        private static void StyleClose(Transform root, TMP_FontAsset font)
        {
            var close = root.Find("CloseButton");
            if (close == null)
            {
                return;
            }

            var rect = (RectTransform)close;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-12f, -11f);
            rect.sizeDelta = new Vector2(32f, 32f);

            var image = close.GetComponent<Image>();
            image.sprite = null;
            image.color = Color.white;
            image.raycastTarget = true;
            var button = close.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color(0.06f, 0.13f, 0.17f, 1f);
            colors.highlightedColor = new Color(0.12f, 0.3f, 0.36f, 1f);
            colors.pressedColor = new Color(0.2f, 0.45f, 0.5f, 1f);
            colors.selectedColor = colors.normalColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            EnsureOutline(close.gameObject, new Color(0.3f, 0.85f, 1f, 0.6f), 1f);

            var label = close.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.font = font;
                Style(label, 24f, new Color(0.75f, 0.97f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Normal);
                label.text = "×";
            }
        }

        private static void OrderSiblings(Transform panel)
        {
            var order = new[]
            {
                "HeaderBand", "HeaderTagLineL", "HeaderTagLineR", "HeaderTag", "Title", "HeaderDivider",
                "CargoCard", "ValueCard", "CargoSummaryText", "UnsettledValueText",
                "ListFrame", "ListSectionLabel", "OwnedKindsText", "ColumnHeader", "EmptyHint",
                "StackScroll", "StacksText",
                "CornerTL_H", "CornerTL_V", "CornerTR_H", "CornerTR_V",
                "CornerBL_H", "CornerBL_V", "CornerBR_H", "CornerBR_V",
                "WeightHelpIcon"
            };

            for (var i = 0; i < order.Length; i++)
            {
                var child = panel.Find(order[i]);
                if (child != null)
                {
                    child.SetSiblingIndex(i);
                }
            }

            // 도움말 팝업은 항상 가장 위에 그린다.
            var tooltip = panel.Find("WeightTooltip");
            if (tooltip != null)
            {
                tooltip.SetAsLastSibling();
            }
        }

        private static void RemoveChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static RectTransform EnsureRect(Transform parent, string name)
        {
            var found = parent.Find(name);
            if (found != null)
            {
                return (RectTransform)found;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image EnsureImage(Transform parent, string name, Color color)
        {
            var rect = EnsureRect(parent, name);
            var image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text EnsureText(Transform parent, string name, TMP_FontAsset font)
        {
            var rect = EnsureRect(parent, name);
            var text = rect.GetComponent<TMP_Text>();
            if (text == null)
            {
                text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            }

            text.font = font;
            text.raycastTarget = false;
            text.richText = true;
            return text;
        }

        private static void Style(TMP_Text text, float size, Color color, TextAlignmentOptions alignment, FontStyles style)
        {
            text.enableAutoSizing = false;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.characterSpacing = 0f;
            text.raycastTarget = false;
            text.richText = true;
            EditorUtility.SetDirty(text);
        }

        private static void EnsureOutline(GameObject target, Color color, float distance)
        {
            var outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        private static void PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void PlaceTopCenter(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
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
