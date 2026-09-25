using System;
using System.Collections.Generic;
using System.Linq;
using SubTerra.App.UI.Building;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 110: 시설 건설창(BuildingMenu.prefab)만 재구성한다.
    /// 좌측 아이콘 목록 + 우측 상세/비용/설치 상태 카드 구조로 바꾸고,
    /// 기존 Select_* 버튼·BuildingMenuEntryButton·View 참조는 그대로 재사용한다.
    /// 루트 크기·위치는 Integration Scene 인스턴스 값을 유지하므로 Scene은 수정하지 않는다.
    /// </summary>
    public static class PromptB110BuildingMenuBuilder
    {
        public const string BuildingMenuPrefabPath = "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";

        public const float PanelWidth = 528f * 1.1f;
        public const float PanelHeight = 560f;
        public const float HeaderHeight = 54f;
        public const float LeftColumnX = 16f;
        public const float EntryWidth = 200f;
        public const float EntryHeight = 46f;
        public const float EntrySpacing = 4f;
        public const float FirstEntryY = -88f;
        public const float RightColumnX = 240f;
        public const float RightColumnWidth = PanelWidth - RightColumnX - 16f;
        public const float LeftRightGap = RightColumnX - (LeftColumnX + EntryWidth);
        public const int CostRowCount = 3;

        public static readonly Color PanelColor = new Color(0.022f, 0.045f, 0.065f, 1f);
        public static readonly Color HeaderColor = new Color(0.035f, 0.085f, 0.115f, 1f);
        public static readonly Color Cyan = new Color(0.45f, 0.95f, 1f, 1f);
        private static readonly Color CyanLine = new Color(0f, 0.85f, 1f, 0.4f);
        private static readonly Color CyanFaint = new Color(0.2f, 0.8f, 0.95f, 0.2f);
        private static readonly Color EntryNormal = new Color(0.06f, 0.11f, 0.145f, 1f);
        private static readonly Color EntryHover = new Color(0.1f, 0.21f, 0.26f, 1f);
        private static readonly Color EntryPressed = new Color(0.08f, 0.3f, 0.36f, 1f);
        private static readonly Color BodyText = new Color(0.8f, 0.89f, 0.93f, 1f);
        private static readonly Color SubText = new Color(0.6f, 0.78f, 0.85f, 1f);

        [MenuItem("SubTerra/UI/Build Prompt-B 110 Building Menu Rework")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static float EntryY(int index)
        {
            return FirstEntryY - index * (EntryHeight + EntrySpacing);
        }

        public static string Build()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode first.");
            }

            var root = PrefabUtility.LoadPrefabContents(BuildingMenuPrefabPath);
            try
            {
                var report = Apply(root);
                PrefabUtility.SaveAsPrefabAsset(root, BuildingMenuPrefabPath);
                return report;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string Apply(GameObject root)
        {
            var view = root.GetComponent<BuildingMenuView>();
            var panel = root.transform.Find("PanelRoot");
            if (view == null || panel == null)
            {
                throw new InvalidOperationException("BuildingMenuView 또는 PanelRoot가 없습니다.");
            }

            var title = panel.Find("Title").GetComponent<TMP_Text>();
            var font = title.font;
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = null;
            panelImage.color = PanelColor;
            panelImage.raycastTarget = true;
            EnsureOutline(panel.gameObject, new Color(0.2f, 0.75f, 0.9f, 0.55f), 1.5f);

            BuildHeader(panel, title, font);
            var entries = BuildEntries(panel, font);
            BuildSectionLabels(panel, font, entries.Count);
            var detail = BuildDetail(panel, font);
            var costs = BuildCosts(panel, font);
            var banner = BuildAvailability(panel, font, knob);
            BuildFooter(panel, font);
            BuildCorners(panel);
            StyleClose(root.transform, font);
            OrderSiblings(panel, entries);

            // 장식·텍스트가 시설 버튼 클릭을 가로채지 않게 한다.
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.GetComponent<Button>() == null && graphic.transform != panel)
                {
                    graphic.raycastTarget = false;
                }
            }

            var so = new SerializedObject(view);
            so.FindProperty("entries").arraySize = entries.Count;
            for (var i = 0; i < entries.Count; i++)
            {
                so.FindProperty("entries").GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            }

            so.FindProperty("detailIcon").objectReferenceValue = detail.Icon;
            so.FindProperty("detailNameText").objectReferenceValue = detail.Name;
            so.FindProperty("detailPowerText").objectReferenceValue = detail.PowerText;
            so.FindProperty("detailPowerChip").objectReferenceValue = detail.PowerChip;
            so.FindProperty("costSection").objectReferenceValue = costs.Section;
            so.FindProperty("costRows").arraySize = costs.Rows.Count;
            for (var i = 0; i < costs.Rows.Count; i++)
            {
                so.FindProperty("costRows").GetArrayElementAtIndex(i).objectReferenceValue = costs.Rows[i];
            }

            so.FindProperty("availabilityBanner").objectReferenceValue = banner.Banner;
            so.FindProperty("availabilityAccent").objectReferenceValue = banner.Accent;
            so.FindProperty("availabilityBadge").objectReferenceValue = banner.Badge;
            so.FindProperty("availabilityGlyph").objectReferenceValue = banner.Glyph;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);

            return "Prompt-B 110 BuildingMenu rebuilt: entries=" + entries.Count + " costRows=" + costs.Rows.Count;
        }

        private static void BuildHeader(Transform panel, TMP_Text title, TMP_FontAsset font)
        {
            var band = EnsureImage(panel, "HeaderBand", HeaderColor);
            band.rectTransform.anchorMin = new Vector2(0f, 1f);
            band.rectTransform.anchorMax = new Vector2(1f, 1f);
            band.rectTransform.pivot = new Vector2(0.5f, 1f);
            band.rectTransform.anchoredPosition = Vector2.zero;
            band.rectTransform.sizeDelta = new Vector2(0f, HeaderHeight);

            var accent = EnsureImage(panel, "HeaderAccent", Cyan);
            PlaceTopLeft(accent.rectTransform, 16f, -11f, 4f, 32f);

            var tag = EnsureText(panel, "HeaderTag", font);
            Style(tag, 11f, Cyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            tag.characterSpacing = 5f;
            tag.text = "FACILITY CONSTRUCTION";
            PlaceTopLeft(tag.rectTransform, 28f, -6f, 260f, 18f);

            Style(title, 22f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            title.text = "시설 건설";
            PlaceTopLeft(title.rectTransform, 28f, -19f, 260f, 34f);

            var hint = EnsureText(panel, "HotkeyHint", font);
            Style(hint, 13f, SubText, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
            hint.text = "<color=#73F0FF>[B]</color> 열기 / 닫기";
            PlaceTopRight(hint.rectTransform, -56f, -15f, 140f, 24f);

            var divider = EnsureImage(panel, "HeaderDivider", new Color(0f, 0.85f, 1f, 0.55f));
            divider.rectTransform.anchorMin = new Vector2(0f, 1f);
            divider.rectTransform.anchorMax = new Vector2(1f, 1f);
            divider.rectTransform.pivot = new Vector2(0.5f, 1f);
            divider.rectTransform.anchoredPosition = new Vector2(0f, -HeaderHeight);
            divider.rectTransform.sizeDelta = new Vector2(0f, 1.5f);
        }

        private static void BuildSectionLabels(Transform panel, TMP_FontAsset font, int entryCount)
        {
            var list = EnsureText(panel, "ListSectionLabel", font);
            Style(list, 11f, Cyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            list.characterSpacing = 3f;
            list.text = "FACILITIES <color=#6B8C99>· " + entryCount + "</color>";
            PlaceTopLeft(list.rectTransform, LeftColumnX, -62f, EntryWidth, 18f);

            var detail = EnsureText(panel, "DetailSectionLabel", font);
            Style(detail, 11f, Cyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            detail.characterSpacing = 3f;
            detail.text = "DETAILS";
            PlaceTopLeft(detail.rectTransform, RightColumnX, -62f, RightColumnWidth, 18f);

            var column = EnsureImage(panel, "ColumnDivider", CyanFaint);
            PlaceTopLeft(column.rectTransform, 228f, -64f, 1f, 480f);
        }

        private static List<BuildingMenuEntryVisual> BuildEntries(Transform panel, TMP_FontAsset font)
        {
            // 기존 세로 순서(버팀목 → 긴급 탈출 포탈)를 그대로 유지한다.
            var buttons = panel.GetComponentsInChildren<BuildingMenuEntryButton>(true)
                .OrderByDescending(b => ((RectTransform)b.transform).anchoredPosition.y)
                .ToList();
            var result = new List<BuildingMenuEntryVisual>();
            for (var i = 0; i < buttons.Count; i++)
            {
                var go = buttons[i].gameObject;
                var rect = (RectTransform)go.transform;
                PlaceTopLeft(rect, LeftColumnX, EntryY(i), EntryWidth, EntryHeight);

                var bg = go.GetComponent<Image>();
                bg.sprite = null;
                bg.color = Color.white;
                bg.raycastTarget = true;
                var button = go.GetComponent<Button>();
                button.targetGraphic = bg;
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = EntryNormal;
                colors.highlightedColor = EntryHover;
                colors.pressedColor = EntryPressed;
                colors.selectedColor = EntryNormal;
                colors.disabledColor = new Color(0.08f, 0.09f, 0.1f, 0.7f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.08f;
                button.colors = colors;
                EnsureOutline(go, new Color(0.3f, 0.85f, 1f, 0.22f), 1f);

                // Outline은 면 사본을 뒤에 그리므로 강조 면은 불투명해야 테두리만 보인다.
                var highlight = EnsureImage(go.transform, "SelectedHighlight", new Color(0.05f, 0.22f, 0.28f, 1f));
                Stretch(highlight.rectTransform);
                EnsureOutline(highlight.gameObject, new Color(0.45f, 0.95f, 1f, 0.95f), 1.5f);
                var bar = EnsureImage(highlight.transform, "SelectedBar", Cyan);
                bar.rectTransform.anchorMin = new Vector2(0f, 0f);
                bar.rectTransform.anchorMax = new Vector2(0f, 1f);
                bar.rectTransform.pivot = new Vector2(0f, 0.5f);
                bar.rectTransform.anchoredPosition = Vector2.zero;
                bar.rectTransform.sizeDelta = new Vector2(4f, 0f);
                highlight.gameObject.SetActive(false);

                var icon = EnsureImage(go.transform, "Icon", Color.white);
                icon.preserveAspect = true;
                PlaceTopLeft(icon.rectTransform, 9f, -6f, 34f, 34f);

                var label = EnsureText(go.transform, "Label", font);
                Style(label, 16f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                label.enableAutoSizing = true;
                label.fontSizeMin = 12f;
                label.fontSizeMax = 16f;
                PlaceTopLeft(label.rectTransform, 50f, -2f, 104f, 24f);

                var cost = EnsureText(go.transform, "CostSummary", font);
                Style(cost, 12f, SubText, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
                cost.text = string.Empty;
                PlaceTopLeft(cost.rectTransform, 50f, -25f, 142f, 19f);

                var state = EnsureText(go.transform, "EntryState", font);
                Style(state, 12f, new Color(0.56f, 0.96f, 0.78f, 1f), TextAlignmentOptions.MidlineRight, FontStyles.Bold);
                state.text = string.Empty;
                PlaceTopRight(state.rectTransform, -8f, -2f, 42f, 24f);

                var visual = go.GetComponent<BuildingMenuEntryVisual>();
                if (visual == null)
                {
                    visual = go.AddComponent<BuildingMenuEntryVisual>();
                }

                var so = new SerializedObject(visual);
                so.FindProperty("icon").objectReferenceValue = icon;
                so.FindProperty("nameText").objectReferenceValue = label;
                so.FindProperty("costText").objectReferenceValue = cost;
                so.FindProperty("stateText").objectReferenceValue = state;
                so.FindProperty("selectedHighlight").objectReferenceValue = highlight.gameObject;
                so.ApplyModifiedPropertiesWithoutUndo();

                // 배경 → 선택 강조 → 아이콘·글자 순으로 그린다.
                highlight.transform.SetSiblingIndex(0);
                result.Add(visual);
            }

            return result;
        }

        private readonly struct DetailRefs
        {
            public readonly Image Icon;
            public readonly TMP_Text Name;
            public readonly TMP_Text PowerText;
            public readonly GameObject PowerChip;

            public DetailRefs(Image icon, TMP_Text name, TMP_Text powerText, GameObject powerChip)
            {
                Icon = icon;
                Name = name;
                PowerText = powerText;
                PowerChip = powerChip;
            }
        }

        private static DetailRefs BuildDetail(Transform panel, TMP_FontAsset font)
        {
            var frame = EnsureImage(panel, "DetailIconFrame", new Color(0.03f, 0.08f, 0.11f, 1f));
            PlaceTopLeft(frame.rectTransform, RightColumnX, -86f, 76f, 76f);
            EnsureOutline(frame.gameObject, new Color(0.3f, 0.85f, 1f, 0.45f), 1f);
            var icon = EnsureImage(frame.transform, "DetailIcon", Color.white);
            icon.preserveAspect = true;
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            icon.rectTransform.anchoredPosition = Vector2.zero;
            icon.rectTransform.sizeDelta = new Vector2(62f, 62f);
            icon.enabled = false;

            var name = EnsureText(panel, "DetailName", font);
            Style(name, 21f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            name.enableAutoSizing = true;
            name.fontSizeMin = 15f;
            name.fontSizeMax = 21f;
            name.text = "시설 미선택";
            PlaceTopLeft(name.rectTransform, RightColumnX + 88f, -86f, RightColumnWidth - 88f, 34f);

            var chip = EnsureImage(panel, "DetailPowerChip", new Color(0.04f, 0.17f, 0.21f, 1f));
            PlaceTopLeft(chip.rectTransform, RightColumnX + 88f, -126f, 132f, 24f);
            EnsureOutline(chip.gameObject, new Color(0.3f, 0.85f, 1f, 0.4f), 1f);
            var power = EnsureText(chip.transform, "DetailPowerText", font);
            Style(power, 13f, new Color(0.7f, 0.95f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
            power.text = string.Empty;
            Stretch(power.rectTransform);
            chip.gameObject.SetActive(false);

            var body = panel.Find("SelectionText").GetComponent<TMP_Text>();
            Style(body, 15f, BodyText, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            body.enableAutoSizing = true;
            body.fontSizeMin = 12f;
            body.fontSizeMax = 15f;
            body.lineSpacing = 4f;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Ellipsis;
            PlaceTopLeft(body.rectTransform, RightColumnX, -172f, RightColumnWidth, 76f);

            return new DetailRefs(icon, name, power, chip.gameObject);
        }

        private readonly struct CostRefs
        {
            public readonly GameObject Section;
            public readonly List<BuildingMenuCostRowView> Rows;

            public CostRefs(GameObject section, List<BuildingMenuCostRowView> rows)
            {
                Section = section;
                Rows = rows;
            }
        }

        private static CostRefs BuildCosts(Transform panel, TMP_FontAsset font)
        {
            var section = EnsureRect(panel, "CostSection");
            PlaceTopLeft(section, RightColumnX, -256f, RightColumnWidth, 150f);

            var divider = EnsureImage(section, "CostDivider", CyanLine);
            PlaceTopLeft(divider.rectTransform, 0f, 0f, RightColumnWidth, 1.5f);

            var label = EnsureText(section, "CostLabel", font);
            Style(label, 11f, Cyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            label.characterSpacing = 3f;
            label.text = "REQUIRED <color=#6B8C99>· 필요 자원</color>";
            PlaceTopLeft(label.rectTransform, 0f, -7f, RightColumnWidth, 18f);

            var rows = new List<BuildingMenuCostRowView>();
            for (var i = 0; i < CostRowCount; i++)
            {
                var bg = EnsureImage(section, "CostRow_" + i, new Color(0.05f, 0.1f, 0.14f, 0.95f));
                PlaceTopLeft(bg.rectTransform, 0f, -30f - i * 40f, RightColumnWidth, 36f);

                var icon = EnsureImage(bg.transform, "Icon", Color.white);
                icon.preserveAspect = true;
                PlaceTopLeft(icon.rectTransform, 8f, -5f, 24f, 24f);

                var name = EnsureText(bg.transform, "Name", font);
                Style(name, 15f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                PlaceTopLeft(name.rectTransform, 40f, -1f, 60f, 32f);

                var amount = EnsureText(bg.transform, "Amount", font);
                Style(amount, 15f, BodyText, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
                PlaceTopLeft(amount.rectTransform, 100f, -1f, 136f, 32f);

                var state = EnsureText(bg.transform, "State", font);
                Style(state, 13f, new Color(0.56f, 0.96f, 0.78f, 1f), TextAlignmentOptions.MidlineRight, FontStyles.Bold);
                PlaceTopRight(state.rectTransform, -8f, -1f, 84f, 32f);

                var track = EnsureImage(bg.transform, "FillTrack", new Color(1f, 1f, 1f, 0.08f));
                track.rectTransform.anchorMin = new Vector2(0f, 0f);
                track.rectTransform.anchorMax = new Vector2(1f, 0f);
                track.rectTransform.pivot = new Vector2(0.5f, 0f);
                track.rectTransform.anchoredPosition = Vector2.zero;
                track.rectTransform.sizeDelta = new Vector2(0f, 3f);
                var fill = EnsureImage(track.transform, "Fill", new Color(0.56f, 0.96f, 0.78f, 1f));
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = Vector2.one;
                fill.rectTransform.pivot = new Vector2(0f, 0.5f);
                fill.rectTransform.offsetMin = Vector2.zero;
                fill.rectTransform.offsetMax = Vector2.zero;

                var row = bg.GetComponent<BuildingMenuCostRowView>();
                if (row == null)
                {
                    row = bg.gameObject.AddComponent<BuildingMenuCostRowView>();
                }

                var so = new SerializedObject(row);
                so.FindProperty("icon").objectReferenceValue = icon;
                so.FindProperty("nameText").objectReferenceValue = name;
                so.FindProperty("amountText").objectReferenceValue = amount;
                so.FindProperty("stateText").objectReferenceValue = state;
                so.FindProperty("fill").objectReferenceValue = fill.rectTransform;
                so.FindProperty("fillImage").objectReferenceValue = fill;
                so.ApplyModifiedPropertiesWithoutUndo();
                bg.gameObject.SetActive(false);
                rows.Add(row);
            }

            section.gameObject.SetActive(false);
            return new CostRefs(section.gameObject, rows);
        }

        private readonly struct BannerRefs
        {
            public readonly Image Banner;
            public readonly Image Accent;
            public readonly Image Badge;
            public readonly TMP_Text Glyph;

            public BannerRefs(Image banner, Image accent, Image badge, TMP_Text glyph)
            {
                Banner = banner;
                Accent = accent;
                Badge = badge;
                Glyph = glyph;
            }
        }

        private static BannerRefs BuildAvailability(Transform panel, TMP_FontAsset font, Sprite knob)
        {
            var banner = EnsureImage(panel, "AvailabilityBanner", new Color(0.1f, 0.14f, 0.16f, 0.92f));
            PlaceTopLeft(banner.rectTransform, RightColumnX, -410f, RightColumnWidth, 64f);

            var accent = EnsureImage(banner.transform, "AvailabilityAccent", new Color(0.45f, 0.62f, 0.7f, 1f));
            accent.rectTransform.anchorMin = new Vector2(0f, 0f);
            accent.rectTransform.anchorMax = new Vector2(0f, 1f);
            accent.rectTransform.pivot = new Vector2(0f, 0.5f);
            accent.rectTransform.anchoredPosition = Vector2.zero;
            accent.rectTransform.sizeDelta = new Vector2(4f, 0f);

            var badge = EnsureImage(banner.transform, "AvailabilityBadge", new Color(0.45f, 0.62f, 0.7f, 1f));
            badge.sprite = knob;
            badge.type = Image.Type.Simple;
            PlaceTopLeft(badge.rectTransform, 16f, -16f, 32f, 32f);

            var glyph = EnsureText(badge.transform, "AvailabilityGlyph", font);
            Style(glyph, 20f, new Color(0.02f, 0.06f, 0.08f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
            glyph.text = BuildingMenuDisplayFormatter.IdleGlyph;
            Stretch(glyph.rectTransform);

            var text = panel.Find("AvailabilityText").GetComponent<TMP_Text>();
            Style(text, 15f, new Color(0.72f, 0.84f, 0.9f, 1f), TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            text.enableAutoSizing = true;
            text.fontSizeMin = 11f;
            text.fontSizeMax = 15f;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.lineSpacing = 2f;
            text.text = BuildingMenuDisplayFormatter.AvailabilityText(BuildingAvailabilityDisplayKind.Idle, string.Empty);
            PlaceTopLeft(text.rectTransform, RightColumnX + 60f, -413f, RightColumnWidth - 70f, 58f);

            return new BannerRefs(banner, accent, badge, glyph);
        }

        private static void BuildFooter(Transform panel, TMP_FontAsset font)
        {
            var status = panel.Find("StatusText").GetComponent<TMP_Text>();
            Style(status, 13f, new Color(0.75f, 0.9f, 0.95f, 1f), TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            status.enableAutoSizing = true;
            status.fontSizeMin = 11f;
            status.fontSizeMax = 13f;
            status.textWrappingMode = TextWrappingModes.Normal;
            status.overflowMode = TextOverflowModes.Ellipsis;
            PlaceTopLeft(status.rectTransform, RightColumnX, -480f, RightColumnWidth, 26f);

            var divider = EnsureImage(panel, "ControlsDivider", CyanFaint);
            PlaceTopLeft(divider.rectTransform, RightColumnX, -512f, RightColumnWidth, 1f);

            var hint = EnsureText(panel, "ControlsHint", font);
            Style(hint, 12f, SubText, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            hint.enableAutoSizing = true;
            hint.fontSizeMin = 10f;
            hint.fontSizeMax = 12f;
            hint.text = "<color=#73F0FF>좌클릭</color> 설치   <color=#73F0FF>C</color> 근접 설치   <color=#73F0FF>B</color> 닫기";
            PlaceTopLeft(hint.rectTransform, RightColumnX, -518f, RightColumnWidth, 24f);
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
                Style(label, 24f, new Color(0.75f, 0.97f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Normal);
                label.text = "×";
            }
        }

        private static void OrderSiblings(Transform panel, List<BuildingMenuEntryVisual> entries)
        {
            var order = new List<string>
            {
                "HeaderBand", "HeaderAccent", "HeaderTag", "Title", "HotkeyHint", "HeaderDivider",
                "ListSectionLabel", "DetailSectionLabel", "ColumnDivider", "BuildingListText"
            };
            order.AddRange(entries.Select(e => e.name));
            order.AddRange(new[]
            {
                "DetailIconFrame", "DetailName", "DetailPowerChip", "SelectionText", "CostSection",
                "AvailabilityBanner", "AvailabilityText", "StatusText", "ControlsDivider", "ControlsHint",
                "CornerTL_H", "CornerTL_V", "CornerTR_H", "CornerTR_V",
                "CornerBL_H", "CornerBL_V", "CornerBR_H", "CornerBR_V"
            });

            for (var i = 0; i < order.Count; i++)
            {
                var child = panel.Find(order[i]);
                if (child != null)
                {
                    child.SetSiblingIndex(i);
                }
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
            // Ellipsis는 줄 높이가 칸보다 크면 줄 전체를 숨기므로 한 줄 라벨은 Overflow로 둔다.
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
