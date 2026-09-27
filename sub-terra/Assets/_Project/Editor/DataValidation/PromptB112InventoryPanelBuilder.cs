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
    /// prompt-B 112: 인벤토리 창(InventoryPanel.prefab)만 재구성한다.
    /// 헤더 + 적재량 게이지 카드 + 미정산 가치 카드 + 스크롤 자원 목록 구조로 바꾸고,
    /// 기존 PanelRoot·행·텍스트·닫기 버튼 오브젝트는 그대로 재사용해 Scene 참조를 보존한다.
    /// </summary>
    public static class PromptB112InventoryPanelBuilder
    {
        public const string InventoryPanelPrefabPath = "Assets/_Project/Prefabs/UI/InventoryPanel.prefab";
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        public const float PanelWidth = 560f;
        public const float PanelHeight = 532f;
        // 화면 중앙의 플레이어·드론 대사와 겹치지 않도록 건설창(좌)·우측 메뉴 사이 오른쪽으로 둔다.
        public const float PanelOffsetX = 330f;
        public const float HeaderHeight = 54f;
        public const float Margin = 16f;
        public const float ContentWidth = PanelWidth - Margin * 2f;
        public const float CardTop = -68f;
        public const float CardHeight = 112f;
        public const float CargoCardWidth = 352f;
        public const float CardGap = 10f;
        public const float ValueCardWidth = ContentWidth - CargoCardWidth - CardGap;
        public const float ListLabelTop = -194f;
        public const float ListTop = -224f;
        public const float ListHeight = 250f;
        public const float RowHeight = 58f;
        public const float RowSpacing = 6f;
        public const float FooterTop = -486f;

        private static readonly Color PanelColor = new Color(0.022f, 0.045f, 0.065f, 1f);
        private static readonly Color HeaderColor = new Color(0.035f, 0.085f, 0.115f, 1f);
        private static readonly Color CardColor = new Color(0.04f, 0.085f, 0.115f, 1f);
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
            var ownedKinds = BuildListHeader(panel, font);
            var rows = BuildList(panel, font);
            BuildFooter(panel, font);
            StyleWeightHelp(panel, font);
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
            so.FindProperty("ownedKindsText").objectReferenceValue = ownedKinds;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);

            return "Prompt-B 112 InventoryPanel rebuilt: rows=" + rows + " size=" + PanelWidth + "x" + PanelHeight;
        }

        private static void BuildHeader(Transform panel, TMP_FontAsset font)
        {
            var band = EnsureImage(panel, "HeaderBand", HeaderColor);
            band.rectTransform.anchorMin = new Vector2(0f, 1f);
            band.rectTransform.anchorMax = new Vector2(1f, 1f);
            band.rectTransform.pivot = new Vector2(0.5f, 1f);
            band.rectTransform.anchoredPosition = Vector2.zero;
            band.rectTransform.sizeDelta = new Vector2(0f, HeaderHeight);

            var accent = EnsureImage(panel, "HeaderAccent", Cyan);
            PlaceTopLeft(accent.rectTransform, Margin, -11f, 4f, 32f);

            var tag = EnsureText(panel, "HeaderTag", font);
            Style(tag, 11f, Cyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            tag.characterSpacing = 5f;
            tag.text = "CARGO INVENTORY";
            PlaceTopLeft(tag.rectTransform, 28f, -6f, 260f, 18f);

            var title = EnsureText(panel, "Title", font);
            Style(title, 22f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            title.text = "인벤토리";
            PlaceTopLeft(title.rectTransform, 28f, -19f, 260f, 34f);

            var hint = EnsureText(panel, "HotkeyHint", font);
            Style(hint, 13f, SubText, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
            hint.text = "<color=#73F0FF>[I]</color> 열기 / 닫기";
            PlaceTopRight(hint.rectTransform, -56f, -15f, 140f, 24f);

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
            var card = EnsureImage(panel, "CargoCard", CardColor);
            PlaceTopLeft(card.rectTransform, Margin, CardTop, CargoCardWidth, CardHeight);
            EnsureOutline(card.gameObject, new Color(0.3f, 0.85f, 1f, 0.3f), 1f);
            var inner = CargoCardWidth - 28f;

            var label = EnsureText(card.transform, "CargoLabel", font);
            Style(label, 12.5f, Cyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            label.characterSpacing = 3f;
            label.text = "CARGO LOAD <color=#6B8C99>· 적재량</color>";
            PlaceTopLeft(label.rectTransform, 14f, -10f, 240f, 18f);

            var amount = EnsureText(card.transform, "CargoAmountText", font);
            Style(amount, 30f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            amount.text = InventoryPanelDisplayFormatter.LoadAmount(0f, 0f);
            PlaceTopLeft(amount.rectTransform, 14f, -30f, 220f, 40f);

            var percent = EnsureText(card.transform, "CargoPercentText", font);
            Style(percent, 22f, SubText, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
            percent.text = "0%";
            PlaceTopRight(percent.rectTransform, -14f, -32f, 100f, 36f);

            var track = EnsureImage(card.transform, "CargoBarTrack", new Color(1f, 1f, 1f, 0.08f));
            PlaceTopLeft(track.rectTransform, 14f, -74f, inner, 10f);
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
            Style(state, 13.5f, SubText, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            state.text = InventoryPanelDisplayFormatter.StateLine(0f, 0f);
            PlaceTopLeft(state.rectTransform, 14f, -88f, inner, 20f);

            return new GaugeRefs(amount, percent, state, fill);
        }

        private static TMP_Text BuildValueCard(Transform panel, TMP_FontAsset font)
        {
            var card = EnsureImage(panel, "ValueCard", CardColor);
            PlaceTopLeft(card.rectTransform, Margin + CargoCardWidth + CardGap, CardTop, ValueCardWidth, CardHeight);
            EnsureOutline(card.gameObject, new Color(0.3f, 0.85f, 1f, 0.3f), 1f);
            var inner = ValueCardWidth - 28f;

            var accent = EnsureImage(card.transform, "ValueAccent", Gold);
            accent.rectTransform.anchorMin = new Vector2(0f, 0f);
            accent.rectTransform.anchorMax = new Vector2(0f, 1f);
            accent.rectTransform.pivot = new Vector2(0f, 0.5f);
            accent.rectTransform.anchoredPosition = Vector2.zero;
            accent.rectTransform.sizeDelta = new Vector2(3f, 0f);

            var label = EnsureText(card.transform, "ValueLabel", font);
            Style(label, 12.5f, Cyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            label.characterSpacing = 3f;
            label.text = "UNSETTLED";
            PlaceTopLeft(label.rectTransform, 14f, -10f, inner, 18f);

            var caption = EnsureText(card.transform, "ValueCaption", font);
            Style(caption, 13f, SubText, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            caption.text = "미정산 가치";
            PlaceTopLeft(caption.rectTransform, 14f, -28f, inner, 20f);

            var amount = EnsureText(card.transform, "UnsettledAmountText", font);
            Style(amount, 30f, Gold, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            amount.enableAutoSizing = true;
            amount.fontSizeMin = 18f;
            amount.fontSizeMax = 30f;
            amount.text = InventoryPanelDisplayFormatter.UnsettledAmount(0f);
            PlaceTopLeft(amount.rectTransform, 14f, -56f, inner, 42f);
            return amount;
        }

        private static TMP_Text BuildListHeader(Transform panel, TMP_FontAsset font)
        {
            var label = EnsureText(panel, "ListSectionLabel", font);
            Style(label, 12.5f, Cyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            label.characterSpacing = 3f;
            label.text = "RESOURCES <color=#6B8C99>· 보유 자원</color>";
            PlaceTopLeft(label.rectTransform, Margin, ListLabelTop, 300f, 18f);

            var owned = EnsureText(panel, "OwnedKindsText", font);
            Style(owned, 13f, SubText, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
            owned.text = InventoryPanelDisplayFormatter.OwnedKinds(0, 0);
            PlaceTopRight(owned.rectTransform, -Margin, ListLabelTop, 200f, 18f);

            var divider = EnsureImage(panel, "ListDivider", CyanLine);
            PlaceTopLeft(divider.rectTransform, Margin, ListLabelTop - 22f, ContentWidth, 1.5f);
            return owned;
        }

        private static int BuildList(Transform panel, TMP_FontAsset font)
        {
            var scroll = EnsureRect(panel, "StackScroll");
            PlaceTopLeft(scroll, Margin, ListTop, ContentWidth, ListHeight);

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
            bg.color = new Color(0.03f, 0.05f, 0.065f, 1f);
            // 스크롤 휠 입력을 받기 위해 행 배경은 레이캐스트를 유지한다.
            bg.raycastTarget = true;
            EnsureOutline(go, new Color(0.3f, 0.85f, 1f, 0.18f), 1f);

            var accent = EnsureImage(go.transform, "Accent", Cyan);
            accent.rectTransform.anchorMin = new Vector2(0f, 0f);
            accent.rectTransform.anchorMax = new Vector2(0f, 1f);
            accent.rectTransform.pivot = new Vector2(0f, 0.5f);
            accent.rectTransform.anchoredPosition = Vector2.zero;
            accent.rectTransform.sizeDelta = new Vector2(3f, 0f);
            accent.enabled = false;

            var frame = EnsureImage(go.transform, "IconFrame", new Color(0.02f, 0.05f, 0.07f, 1f));
            PlaceTopLeft(frame.rectTransform, 12f, -7f, 44f, 44f);
            EnsureOutline(frame.gameObject, new Color(0.3f, 0.85f, 1f, 0.35f), 1f);

            var icon = go.transform.Find("Icon").GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            PlaceTopLeft(icon.rectTransform, 16f, -11f, 36f, 36f);

            var name = go.transform.Find("Name").GetComponent<TMP_Text>();
            name.font = font;
            Style(name, 18f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            name.enableAutoSizing = true;
            name.fontSizeMin = 14f;
            name.fontSizeMax = 18f;
            PlaceTopLeft(name.rectTransform, 68f, -6f, 280f, 26f);

            var weight = EnsureText(go.transform, "UnitWeight", font);
            Style(weight, 13f, SubText, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            float unitWeight;
            weight.text = weights.TryGetValue(row.MineralId, out unitWeight)
                ? InventoryPanelDisplayFormatter.UnitWeight(unitWeight)
                : string.Empty;
            PlaceTopLeft(weight.rectTransform, 68f, -32f, 280f, 20f);

            var quantity = go.transform.Find("Quantity").GetComponent<TMP_Text>();
            quantity.font = font;
            Style(quantity, 24f, Color.white, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
            quantity.text = InventoryPanelDisplayFormatter.Quantity(0);
            PlaceTopRight(quantity.rectTransform, -16f, -4f, 150f, 30f);

            var state = EnsureText(go.transform, "State", font);
            Style(state, 12.5f, SubText, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
            state.text = InventoryPanelDisplayFormatter.QuantityState(0);
            PlaceTopRight(state.rectTransform, -16f, -34f, 150f, 18f);

            // 배경 → 강조 막대 → 아이콘 틀 → 아이콘 → 글자 순으로 그린다.
            accent.transform.SetSiblingIndex(0);
            frame.transform.SetSiblingIndex(1);
            icon.transform.SetSiblingIndex(2);

            row.EditorSetDetailReferences(bg, accent, weight, state);
            // Outline 사본이 비치지 않도록 미보유 배경도 불투명하게 둔다.
            var rowSo = new SerializedObject(row);
            rowSo.FindProperty("emptyBackground").colorValue = bg.color;
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

        private static void BuildFooter(Transform panel, TMP_FontAsset font)
        {
            var divider = EnsureImage(panel, "ControlsDivider", CyanFaint);
            PlaceTopLeft(divider.rectTransform, Margin, FooterTop, ContentWidth, 1f);

            var hint = EnsureText(panel, "ControlsHint", font);
            Style(hint, 14f, SubText, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            hint.text = "<color=#73F0FF>I</color> 닫기   <color=#FFB833>?</color> 위에 마우스를 올리면 화물 무게 안내";
            PlaceTopLeft(hint.rectTransform, Margin, FooterTop - 8f, ContentWidth, 24f);
        }

        private static void StyleWeightHelp(Transform panel, TMP_FontAsset font)
        {
            var icon = panel.Find("WeightHelpIcon");
            var tooltip = panel.Find("WeightTooltip");
            if (icon == null || tooltip == null)
            {
                return;
            }

            // 적재량 카드 우측 상단에 붙여 무게 도움말임을 드러낸다.
            PlaceTopLeft((RectTransform)icon, Margin + CargoCardWidth - 14f - 24f, CardTop - 7f, 24f, 24f);
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
            label.fontSize = 17f;

            PlaceTopLeft((RectTransform)tooltip, Margin, CardTop - 36f, 420f, 150f);
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
                "HeaderBand", "HeaderAccent", "HeaderTag", "Title", "HotkeyHint", "HeaderDivider",
                "CargoCard", "ValueCard", "CargoSummaryText", "UnsettledValueText",
                "ListSectionLabel", "OwnedKindsText", "ListDivider", "StackScroll", "StacksText",
                "ControlsDivider", "ControlsHint",
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

        private static void PlaceTopRight(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
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
