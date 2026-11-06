using System;
using System.Collections.Generic;
using System.Linq;
using SubTerra.App.UI;
using SubTerra.App.UI.Building;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 188 (#269): 시설 건설창(BuildingMenu.prefab)만 아이콘 레일 건설 모드로 바꾼다.
    /// 왼쪽 레일(아이콘+`>>`) · 고른 시설 카드 · `>>` 큰 창(목록·검색 / 상세)으로 나누고,
    /// 기존 Select_* 버튼·BuildingMenuEntryButton·View 참조는 옮겨서 그대로 재사용한다.
    /// 루트 RectTransform은 Integration Scene 인스턴스 값을 유지하므로 Scene은 수정하지 않는다.
    /// </summary>
    public static class PromptB188BuildingModeBuilder
    {
        public const string BuildingMenuPrefabPath = "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";

        public const float Padding = 8f;
        public const float IconSize = 48f;
        public const float IconSpacing = 6f;
        public const float ExpandHeight = 36f;
        public const float RailWidth = IconSize + Padding * 2f;
        public const float Gap = 8f;
        public const float SideX = RailWidth + Gap;
        public const float CardWidth = 300f;
        public const float CatalogWidth = 520f;
        public const float CatalogInnerWidth = CatalogWidth - 32f;
        public const float RowHeight = 64f;
        public const float RowSpacing = 6f;
        public const int CostRowCount = 3;

        public static readonly Color PanelColor = new Color(0.022f, 0.045f, 0.065f, 0.94f);
        public static readonly Color Cyan = new Color(0.45f, 0.95f, 1f, 1f);
        private static readonly Color CyanLine = new Color(0f, 0.85f, 1f, 0.4f);
        private static readonly Color FrameOutline = new Color(0.2f, 0.75f, 0.9f, 0.55f);
        private static readonly Color EntryNormal = new Color(0.06f, 0.11f, 0.145f, 1f);
        private static readonly Color EntryHover = new Color(0.09f, 0.17f, 0.21f, 1f);
        private static readonly Color EntryPressed = new Color(0.08f, 0.3f, 0.36f, 1f);
        private static readonly Color HighlightFill = new Color(0.045f, 0.24f, 0.3f, 1f);
        private static readonly Color RowBack = new Color(0.05f, 0.1f, 0.14f, 0.95f);
        private static readonly Color BodyText = new Color(0.8f, 0.89f, 0.93f, 1f);
        private static readonly Color SubText = new Color(0.6f, 0.78f, 0.85f, 1f);
        private static readonly Color ConditionText = new Color(0.7f, 0.95f, 1f, 1f);
        private static readonly Color EnoughText = new Color(0.56f, 0.96f, 0.78f, 1f);

        // B-110 머리말·열 구분·조작 안내 장식. 레일 모드에는 쓰지 않는다.
        private static readonly string[] ObsoleteDecorations =
        {
            "HeaderBand", "HeaderAccent", "HeaderTag", "HotkeyHint", "HeaderDivider",
            "ListSectionLabel", "DetailSectionLabel", "ColumnDivider", "ControlsDivider", "ControlsHint",
            "CornerTL_H", "CornerTL_V", "CornerTR_H", "CornerTR_V",
            "CornerBL_H", "CornerBL_V", "CornerBR_H", "CornerBR_V"
        };

        public static float RailHeight(int entryCount)
        {
            return Padding + entryCount * IconSize + Mathf.Max(0, entryCount - 1) * IconSpacing
                + Padding + 2f + Padding + ExpandHeight + Padding;
        }

        public static float IconY(int index)
        {
            return -Padding - index * (IconSize + IconSpacing);
        }

        [MenuItem("SubTerra/UI/Build Prompt-B 188 Building Mode Rail")]
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
            var binder = root.GetComponent<BuildingMenuBinder>();
            var panel = root.transform.Find("PanelRoot");
            if (view == null || binder == null || panel == null)
            {
                throw new InvalidOperationException("BuildingMenuView·BuildingMenuBinder·PanelRoot가 없습니다.");
            }

            var viewSo = new SerializedObject(view);
            var selectionText = viewSo.FindProperty("selectionText").objectReferenceValue as TMP_Text;
            if (selectionText == null || selectionText.font == null)
            {
                throw new InvalidOperationException("SelectionText 글꼴을 찾을 수 없습니다.");
            }

            var font = selectionText.font;
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            PreparePanelRoot(panel);
            foreach (var name in ObsoleteDecorations)
            {
                var obsolete = panel.Find(name);
                if (obsolete != null)
                {
                    UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
                }
            }

            var entries = CollectEntries(root);
            var rail = BuildRail(panel, entries, font, out var expandButton, out var expandLabel);
            var side = BuildSidePane(panel, root.transform, font, knob);
            var catalog = BuildCatalog(panel, root.transform, view, binder, entries, font);
            var hologram = BuildRailHologram(panel, font);
            foreach (var entry in entries)
            {
                EnsureComponent<BuildingRailHoverRelay>(entry.gameObject).EditorSet(view);
            }

            var close = root.transform.Find("CloseButton");
            if (close != null)
            {
                // 테스트·HUD가 쓰는 X 버튼 참조는 남기고 레일 모드에서는 숨긴다(B 키가 닫기).
                close.gameObject.SetActive(false);
            }

            // 순서: 숨김 목록 텍스트 → 카드/칩 → 큰 창 → 레일 → 호버 이름표(맨 위, 클릭 안 받음).
            var buildingListText = panel.Find("BuildingListText");
            if (buildingListText != null)
            {
                buildingListText.SetSiblingIndex(0);
                buildingListText.gameObject.SetActive(false);
            }

            side.Root.SetAsLastSibling();
            catalog.Window.transform.SetAsLastSibling();
            rail.SetAsLastSibling();
            hologram.transform.SetAsLastSibling();

            var motion = root.GetComponent<BuildingModeSlideMotion>();
            if (motion == null)
            {
                motion = root.AddComponent<BuildingModeSlideMotion>();
            }

            viewSo.Update();
            SetArray(viewSo, "entries", entries.Select(e => (UnityEngine.Object)e).ToList());
            Set(viewSo, "detailIcon", side.Icon);
            Set(viewSo, "detailNameText", side.Name);
            Set(viewSo, "detailOperatingConditionText", side.Condition);
            Set(viewSo, "costSection", side.CostSection);
            SetArray(viewSo, "costRows", side.CostRows.Select(r => (UnityEngine.Object)r).ToList());
            Set(viewSo, "availabilityBanner", side.Banner);
            Set(viewSo, "availabilityAccent", side.Accent);
            Set(viewSo, "availabilityBadge", side.Badge);
            Set(viewSo, "availabilityGlyph", side.Glyph);
            Set(viewSo, "availabilityText", side.AvailabilityText);
            Set(viewSo, "statusText", side.StatusText);
            Set(viewSo, "slideMotion", motion);
            Set(viewSo, "rail", rail);
            Set(viewSo, "expandButton", expandButton);
            Set(viewSo, "expandLabel", expandLabel);
            Set(viewSo, "hintChip", side.Hint);
            Set(viewSo, "card", side.Card);
            Set(viewSo, "statusChip", side.StatusChip);
            Set(viewSo, "catalogWindow", catalog.Window);
            Set(viewSo, "listPage", catalog.ListPage);
            Set(viewSo, "searchField", catalog.Search);
            SetArray(viewSo, "catalogRows", catalog.Rows.Select(r => (UnityEngine.Object)r).ToList());
            Set(viewSo, "emptyResult", catalog.Empty);
            Set(viewSo, "detailPage", catalog.DetailPage);
            Set(viewSo, "backButton", catalog.Back);
            Set(viewSo, "detailThumb", catalog.Thumb);
            Set(viewSo, "detailPageName", catalog.Name);
            Set(viewSo, "detailPageCondition", catalog.Condition);
            Set(viewSo, "detailPageFunction", catalog.Function);
            SetArray(viewSo, "detailCostRows", catalog.CostRows.Select(r => (UnityEngine.Object)r).ToList());
            Set(viewSo, "detailPageStatus", catalog.Status);
            Set(viewSo, "detailPageStatusAccent", catalog.StatusAccent);
            Set(viewSo, "railHologram", hologram);
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);

            return "Prompt-B 188 BuildingMenu building mode: rail=" + entries.Count
                + " catalogRows=" + catalog.Rows.Count + " detailCostRows=" + catalog.CostRows.Count;
        }

        private static void PreparePanelRoot(Transform panel)
        {
            // PanelRoot는 배치용 빈 틀이다. 500×560 전체가 클릭을 삼키지 않게 배경·드래그를 뗀다.
            var drag = panel.GetComponent<PopupWindowDrag>();
            if (drag != null)
            {
                UnityEngine.Object.DestroyImmediate(drag);
            }

            var outline = panel.GetComponent<Outline>();
            if (outline != null)
            {
                UnityEngine.Object.DestroyImmediate(outline);
            }

            var image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = null;
                image.color = new Color(0f, 0f, 0f, 0f);
                image.raycastTarget = false;
                image.enabled = false;
            }
        }

        private static List<BuildingMenuEntryVisual> CollectEntries(GameObject root)
        {
            // B-110 세로 순서(버팀목 → 긴급 탈출 포탈)를 그대로 유지한다. 재실행 시에는 레일 안 순서를 쓴다.
            var visuals = root.GetComponentsInChildren<BuildingMenuEntryVisual>(true).ToList();
            return visuals
                .OrderByDescending(v => ((RectTransform)v.transform).anchoredPosition.y)
                .ToList();
        }

        private static RectTransform BuildRail(
            Transform panel,
            List<BuildingMenuEntryVisual> entries,
            TMP_FontAsset font,
            out Button expandButton,
            out TMP_Text expandLabel)
        {
            var height = RailHeight(entries.Count);
            var rail = EnsureImage(panel, "Rail", PanelColor);
            rail.raycastTarget = true;
            PlaceTopLeft(rail.rectTransform, 0f, 0f, RailWidth, height);
            EnsureOutline(rail.gameObject, FrameOutline, 1.5f);

            var accent = EnsureImage(rail.transform, "RailAccent", Cyan);
            accent.rectTransform.anchorMin = new Vector2(0f, 1f);
            accent.rectTransform.anchorMax = new Vector2(1f, 1f);
            accent.rectTransform.pivot = new Vector2(0.5f, 1f);
            accent.rectTransform.anchoredPosition = Vector2.zero;
            accent.rectTransform.sizeDelta = new Vector2(0f, 3f);

            for (var i = 0; i < entries.Count; i++)
            {
                var go = entries[i].gameObject;
                go.transform.SetParent(rail.transform, false);
                PlaceTopLeft((RectTransform)go.transform, Padding, IconY(i), IconSize, IconSize);
                StyleButton(go);
                EnsureOutline(go, new Color(0.3f, 0.85f, 1f, 0.22f), 1f);

                var highlight = EnsureImage(go.transform, "SelectedHighlight", HighlightFill);
                Stretch(highlight.rectTransform, 0f);
                EnsureOutline(highlight.gameObject, new Color(0.45f, 0.95f, 1f, 0.95f), 1.5f);
                var bar = EnsureImage(highlight.transform, "SelectedBar", Cyan);
                bar.rectTransform.anchorMin = new Vector2(0f, 0f);
                bar.rectTransform.anchorMax = new Vector2(0f, 1f);
                bar.rectTransform.pivot = new Vector2(0f, 0.5f);
                bar.rectTransform.anchoredPosition = Vector2.zero;
                bar.rectTransform.sizeDelta = new Vector2(3f, 0f);
                highlight.transform.SetSiblingIndex(0);
                highlight.gameObject.SetActive(false);

                var icon = EnsureImage(go.transform, "Icon", Color.white);
                icon.preserveAspect = true;
                Stretch(icon.rectTransform, 6f);

                // 레일에는 아이콘만 둔다. 이름·비용 글자는 참조 호환용으로 남기고 숨긴다.
                foreach (var hidden in new[] { "Label", "CostSummary", "EntryState" })
                {
                    var child = go.transform.Find(hidden);
                    if (child != null)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            var divider = EnsureImage(rail.transform, "RailDivider", CyanLine);
            PlaceTopLeft(divider.rectTransform, Padding, -(height - Padding - ExpandHeight - Padding - 2f), IconSize, 1.5f);

            var expand = EnsureImage(rail.transform, "ExpandButton", Color.white);
            PlaceTopLeft(expand.rectTransform, Padding, -(height - Padding - ExpandHeight), IconSize, ExpandHeight);
            expand.raycastTarget = true;
            StyleButton(expand.gameObject);
            EnsureOutline(expand.gameObject, new Color(0.3f, 0.85f, 1f, 0.6f), 1f);
            expandButton = expand.GetComponent<Button>();

            expandLabel = EnsureText(expand.transform, "Label", font);
            Style(expandLabel, 18f, Cyan, TextAlignmentOptions.Center, FontStyles.Bold);
            expandLabel.text = ">>";
            Stretch(expandLabel.rectTransform, 0f);

            return rail.rectTransform;
        }

        /// <summary>
        /// 레일 아이콘 호버 이름표: 아이콘에서 뻗는 연결선 + 반투명 청록 판 + 주사선 + 시설명.
        /// 모든 그래픽은 raycast를 끄고 CanvasGroup도 막지 않아 클릭·설치 판정에 끼지 않는다.
        /// </summary>
        private static BuildingRailHologram BuildRailHologram(Transform panel, TMP_FontAsset font)
        {
            var root = EnsureRect(panel, "RailHoverHologram");
            PlaceTopLeft(root, RailWidth + BuildingRailHologram.Gap, -Padding - IconSize * 0.5f,
                BuildingRailHologram.ConnectorLength + 120f, BuildingRailHologram.BodyHeight);
            root.pivot = new Vector2(0f, 0.5f);
            var group = EnsureComponent<CanvasGroup>(root.gameObject);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            group.ignoreParentGroups = false;

            var connector = EnsureImage(root, "Connector", new Color(0.45f, 0.95f, 1f, 0.75f));
            connector.rectTransform.anchorMin = connector.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            connector.rectTransform.pivot = new Vector2(0f, 0.5f);
            connector.rectTransform.anchoredPosition = Vector2.zero;
            connector.rectTransform.sizeDelta = new Vector2(BuildingRailHologram.ConnectorLength, 1.5f);

            var node = EnsureImage(root, "ConnectorNode", Cyan);
            node.rectTransform.anchorMin = node.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            node.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            node.rectTransform.anchoredPosition = Vector2.zero;
            node.rectTransform.sizeDelta = new Vector2(4f, 4f);

            // 아래 카드·힌트 글자가 비치지 않도록 판은 거의 불투명하게, 홀로그램 느낌은 빛·테두리·줄무늬·주사선으로 낸다.
            var glow = EnsureImage(root, "Glow", new Color(0.3f, 0.9f, 1f, 0.12f));
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            glow.rectTransform.pivot = new Vector2(0f, 0.5f);
            glow.transform.SetSiblingIndex(0);

            var body = EnsureImage(root, "Body", new Color(0.02f, 0.13f, 0.17f, 1f));
            body.rectTransform.anchorMin = body.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            body.rectTransform.pivot = new Vector2(0f, 0.5f);
            body.rectTransform.anchoredPosition = new Vector2(BuildingRailHologram.ConnectorLength, 0f);
            body.rectTransform.sizeDelta = new Vector2(120f, BuildingRailHologram.BodyHeight);
            EnsureOutline(body.gameObject, new Color(0.45f, 0.95f, 1f, 0.85f), 1f);

            var accent = EnsureImage(body.transform, "Accent", Cyan);
            accent.rectTransform.anchorMin = new Vector2(0f, 0f);
            accent.rectTransform.anchorMax = new Vector2(0f, 1f);
            accent.rectTransform.pivot = new Vector2(0f, 0.5f);
            accent.rectTransform.anchoredPosition = Vector2.zero;
            accent.rectTransform.sizeDelta = new Vector2(2f, 0f);

            // 가로 줄무늬 두 줄로 홀로그램 화면 느낌만 낸다.
            for (var i = 0; i < 2; i++)
            {
                var stripe = EnsureImage(body.transform, "Stripe_" + i, new Color(0.45f, 0.95f, 1f, 0.07f));
                stripe.rectTransform.anchorMin = new Vector2(0f, 1f);
                stripe.rectTransform.anchorMax = new Vector2(1f, 1f);
                stripe.rectTransform.pivot = new Vector2(0.5f, 1f);
                stripe.rectTransform.anchoredPosition = new Vector2(0f, -(BuildingRailHologram.BodyHeight / 3f) * (i + 1));
                stripe.rectTransform.sizeDelta = new Vector2(0f, 1f);
            }

            var scan = EnsureImage(body.transform, "ScanLine", new Color(0.6f, 1f, 1f, 0.22f));
            scan.rectTransform.anchorMin = new Vector2(0f, 1f);
            scan.rectTransform.anchorMax = new Vector2(1f, 1f);
            scan.rectTransform.pivot = new Vector2(0.5f, 1f);
            scan.rectTransform.anchoredPosition = Vector2.zero;
            scan.rectTransform.sizeDelta = new Vector2(0f, 3f);
            scan.rectTransform.localScale = Vector3.zero;

            var name = EnsureText(body.transform, "NameText", font);
            Style(name, 16f, new Color(0.78f, 1f, 1f, 1f), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            name.characterSpacing = 1f;
            name.text = string.Empty;
            name.maxVisibleCharacters = 0;
            Stretch(name.rectTransform, 0f);
            name.rectTransform.offsetMin = new Vector2(BuildingRailHologram.TextPadding, 0f);
            name.rectTransform.offsetMax = new Vector2(-BuildingRailHologram.TextPadding, 0f);

            var hologram = EnsureComponent<BuildingRailHologram>(root.gameObject);
            hologram.EditorSet(group, body.rectTransform, name, scan.rectTransform, glow.rectTransform);
            EditorUtility.SetDirty(hologram);
            root.gameObject.SetActive(true);
            return hologram;
        }

        private sealed class SideRefs
        {
            public RectTransform Root;
            public GameObject Hint;
            public GameObject Card;
            public GameObject StatusChip;
            public Image Icon;
            public TMP_Text Name;
            public TMP_Text Condition;
            public GameObject CostSection;
            public List<BuildingMenuCostRowView> CostRows;
            public Image Banner;
            public Image Accent;
            public Image Badge;
            public TMP_Text Glyph;
            public TMP_Text AvailabilityText;
            public TMP_Text StatusText;
        }

        private static SideRefs BuildSidePane(Transform panel, Transform root, TMP_FontAsset font, Sprite knob)
        {
            var refs = new SideRefs();
            var side = EnsureRect(panel, "SidePane");
            PlaceTopLeft(side, SideX, 0f, CardWidth, 10f);
            var sideLayout = EnsureComponent<VerticalLayoutGroup>(side.gameObject);
            ConfigureVertical(sideLayout, new RectOffset(0, 0, 0, 0), 6f);
            var sideFitter = EnsureComponent<ContentSizeFitter>(side.gameObject);
            sideFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sideFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            refs.Root = side;

            // 미선택: 큰 안내 패널 없이 짧은 힌트만.
            var hint = EnsureImage(side, "HintChip", PanelColor);
            hint.raycastTarget = true;
            EnsureOutline(hint.gameObject, FrameOutline, 1f);
            EnsureLayoutElement(hint.gameObject, 38f);
            var hintText = EnsureText(hint.transform, "HintText", font);
            Style(hintText, 15f, SubText, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            hintText.text = "시설을 고르세요";
            Stretch(hintText.rectTransform, 0f);
            hintText.rectTransform.offsetMin = new Vector2(14f, 0f);
            hintText.rectTransform.offsetMax = new Vector2(-10f, 0f);
            refs.Hint = hint.gameObject;

            // 선택 카드: 아이콘·시설명 → 용도 → 운영 조건 → 건설 비용 → 설치 상태.
            var card = EnsureImage(side, "Card", PanelColor);
            card.raycastTarget = true;
            EnsureOutline(card.gameObject, FrameOutline, 1.5f);
            var cardLayout = EnsureComponent<VerticalLayoutGroup>(card.gameObject);
            ConfigureVertical(cardLayout, new RectOffset(12, 12, 12, 12), 8f);
            refs.Card = card.gameObject;
            var inner = CardWidth - 24f;

            var header = EnsureRect(card.transform, "CardHeader");
            EnsureLayoutElement(header.gameObject, 44f);
            var frame = Adopt(root, "DetailIconFrame", header);
            var frameImage = EnsureImage(frame.parent, frame.name, new Color(0.03f, 0.08f, 0.11f, 1f));
            PlaceTopLeft(frameImage.rectTransform, 0f, 0f, 44f, 44f);
            EnsureOutline(frame.gameObject, new Color(0.3f, 0.85f, 1f, 0.45f), 1f);
            refs.Icon = EnsureImage(frame, "DetailIcon", Color.white);
            refs.Icon.preserveAspect = true;
            Stretch(refs.Icon.rectTransform, 4f);

            var name = Adopt(root, "DetailName", header).GetComponent<TMP_Text>();
            Style(name, 19f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            name.enableAutoSizing = true;
            name.fontSizeMin = 14f;
            name.fontSizeMax = 19f;
            name.rectTransform.anchorMin = new Vector2(0f, 0f);
            name.rectTransform.anchorMax = new Vector2(1f, 1f);
            name.rectTransform.pivot = new Vector2(0f, 0.5f);
            name.rectTransform.offsetMin = new Vector2(54f, 0f);
            name.rectTransform.offsetMax = Vector2.zero;
            refs.Name = name;

            var description = Adopt(root, "SelectionText", card.transform).GetComponent<TMP_Text>();
            Style(description, 14.5f, BodyText, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            description.textWrappingMode = TextWrappingModes.Normal;
            description.lineSpacing = 2f;
            description.text = string.Empty;

            refs.Condition = Adopt(root, "DetailOperatingCondition", card.transform).GetComponent<TMP_Text>();
            Style(refs.Condition, 13.5f, ConditionText, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            refs.Condition.text = string.Empty;
            refs.Condition.gameObject.SetActive(false);

            var costSection = Adopt(root, "CostSection", card.transform);
            var costLayout = EnsureComponent<VerticalLayoutGroup>(costSection.gameObject);
            ConfigureVertical(costLayout, new RectOffset(0, 0, 0, 0), 4f);
            var costDivider = EnsureImage(costSection, "CostDivider", CyanLine);
            EnsureLayoutElement(costDivider.gameObject, 1.5f);
            var costLabel = EnsureText(costSection, "CostLabel", font);
            Style(costLabel, 12.5f, Cyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            costLabel.text = "건설 비용 <color=#6B8C99>· 보유 / 필요</color>";
            EnsureLayoutElement(costLabel.gameObject, 18f);
            refs.CostRows = BuildCostRows(costSection, font, inner, 28f, "CostRow_");
            refs.CostSection = costSection.gameObject;
            costSection.gameObject.SetActive(false);

            var banner = Adopt(root, "AvailabilityBanner", card.transform);
            refs.Banner = EnsureImage(banner.parent, banner.name, new Color(0.1f, 0.14f, 0.16f, 0.92f));
            EnsureLayoutElement(banner.gameObject, 54f);
            refs.Accent = EnsureImage(banner, "AvailabilityAccent", new Color(0.45f, 0.62f, 0.7f, 1f));
            refs.Accent.rectTransform.anchorMin = new Vector2(0f, 0f);
            refs.Accent.rectTransform.anchorMax = new Vector2(0f, 1f);
            refs.Accent.rectTransform.pivot = new Vector2(0f, 0.5f);
            refs.Accent.rectTransform.anchoredPosition = Vector2.zero;
            refs.Accent.rectTransform.sizeDelta = new Vector2(4f, 0f);
            refs.Badge = EnsureImage(banner, "AvailabilityBadge", new Color(0.45f, 0.62f, 0.7f, 1f));
            refs.Badge.sprite = knob;
            refs.Badge.type = Image.Type.Simple;
            refs.Badge.rectTransform.anchorMin = refs.Badge.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            refs.Badge.rectTransform.pivot = new Vector2(0f, 0.5f);
            refs.Badge.rectTransform.anchoredPosition = new Vector2(12f, 0f);
            refs.Badge.rectTransform.sizeDelta = new Vector2(26f, 26f);
            refs.Glyph = EnsureText(refs.Badge.transform, "AvailabilityGlyph", font);
            Style(refs.Glyph, 17f, new Color(0.02f, 0.06f, 0.08f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
            refs.Glyph.text = BuildingMenuDisplayFormatter.IdleGlyph;
            Stretch(refs.Glyph.rectTransform, 0f);

            refs.AvailabilityText = Adopt(root, "AvailabilityText", banner).GetComponent<TMP_Text>();
            Style(refs.AvailabilityText, 14.5f, new Color(0.72f, 0.84f, 0.9f, 1f), TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            refs.AvailabilityText.enableAutoSizing = true;
            refs.AvailabilityText.fontSizeMin = 11f;
            refs.AvailabilityText.fontSizeMax = 14.5f;
            refs.AvailabilityText.textWrappingMode = TextWrappingModes.Normal;
            refs.AvailabilityText.lineSpacing = 0f;
            refs.AvailabilityText.text = BuildingMenuDisplayFormatter.AvailabilityText(BuildingAvailabilityDisplayKind.Idle, string.Empty);
            Stretch(refs.AvailabilityText.rectTransform, 0f);
            refs.AvailabilityText.rectTransform.offsetMin = new Vector2(46f, 3f);
            refs.AvailabilityText.rectTransform.offsetMax = new Vector2(-8f, -3f);
            card.gameObject.SetActive(false);

            // 설치 결과 메시지는 상태와 겹치지 않게 카드 밑 짧은 줄로 따로 둔다.
            var statusChip = EnsureImage(side, "StatusChip", PanelColor);
            statusChip.raycastTarget = true;
            EnsureOutline(statusChip.gameObject, FrameOutline, 1f);
            var statusLayout = EnsureComponent<VerticalLayoutGroup>(statusChip.gameObject);
            ConfigureVertical(statusLayout, new RectOffset(12, 10, 7, 7), 0f);
            refs.StatusText = Adopt(root, "StatusText", statusChip.transform).GetComponent<TMP_Text>();
            Style(refs.StatusText, 13.5f, new Color(0.75f, 0.9f, 0.95f, 1f), TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            refs.StatusText.textWrappingMode = TextWrappingModes.Normal;
            refs.StatusText.text = string.Empty;
            statusChip.gameObject.SetActive(false);
            refs.StatusChip = statusChip.gameObject;

            hint.transform.SetSiblingIndex(0);
            card.transform.SetSiblingIndex(1);
            statusChip.transform.SetSiblingIndex(2);
            return refs;
        }

        private static List<BuildingMenuCostRowView> BuildCostRows(
            Transform parent,
            TMP_FontAsset font,
            float width,
            float height,
            string prefix)
        {
            var rows = new List<BuildingMenuCostRowView>();
            var wide = width > 400f;
            for (var i = 0; i < CostRowCount; i++)
            {
                var bg = EnsureImage(parent, prefix + i, RowBack);
                EnsureLayoutElement(bg.gameObject, height);
                PlaceTopLeft(bg.rectTransform, 0f, -i * (height + 4f), width, height);

                var icon = EnsureImage(bg.transform, "Icon", Color.white);
                icon.preserveAspect = true;
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                icon.rectTransform.pivot = new Vector2(0f, 0.5f);
                icon.rectTransform.anchoredPosition = new Vector2(6f, 0f);
                icon.rectTransform.sizeDelta = new Vector2(20f, 20f);

                var name = EnsureText(bg.transform, "Name", font);
                Style(name, 14.5f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                PlaceMiddleLeft(name.rectTransform, 32f, wide ? 120f : 64f, height);

                var amount = EnsureText(bg.transform, "Amount", font);
                Style(amount, 14.5f, BodyText, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
                PlaceMiddleLeft(amount.rectTransform, wide ? 170f : 102f, wide ? 160f : 90f, height);

                var state = EnsureText(bg.transform, "State", font);
                Style(state, 13.5f, EnoughText, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
                state.rectTransform.anchorMin = state.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                state.rectTransform.pivot = new Vector2(1f, 0.5f);
                state.rectTransform.anchoredPosition = new Vector2(-8f, 0f);
                state.rectTransform.sizeDelta = new Vector2(wide ? 120f : 70f, height);

                var track = EnsureImage(bg.transform, "FillTrack", new Color(1f, 1f, 1f, 0.08f));
                track.rectTransform.anchorMin = new Vector2(0f, 0f);
                track.rectTransform.anchorMax = new Vector2(1f, 0f);
                track.rectTransform.pivot = new Vector2(0.5f, 0f);
                track.rectTransform.anchoredPosition = Vector2.zero;
                track.rectTransform.sizeDelta = new Vector2(0f, 2f);
                var fill = EnsureImage(track.transform, "Fill", EnoughText);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = Vector2.one;
                fill.rectTransform.pivot = new Vector2(0f, 0.5f);
                fill.rectTransform.offsetMin = Vector2.zero;
                fill.rectTransform.offsetMax = Vector2.zero;

                var row = EnsureComponent<BuildingMenuCostRowView>(bg.gameObject);
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

            return rows;
        }

        private sealed class CatalogRefs
        {
            public GameObject Window;
            public GameObject ListPage;
            public TMP_InputField Search;
            public List<BuildingMenuCatalogRowView> Rows;
            public GameObject Empty;
            public GameObject DetailPage;
            public Button Back;
            public Image Thumb;
            public TMP_Text Name;
            public TMP_Text Condition;
            public TMP_Text Function;
            public List<BuildingMenuCostRowView> CostRows;
            public TMP_Text Status;
            public Image StatusAccent;
        }

        private static CatalogRefs BuildCatalog(
            Transform panel,
            Transform root,
            BuildingMenuView view,
            BuildingMenuBinder binder,
            List<BuildingMenuEntryVisual> entries,
            TMP_FontAsset font)
        {
            var refs = new CatalogRefs();
            var height = RailHeight(entries.Count);
            var window = EnsureImage(panel, "CatalogWindow", PanelColor);
            window.raycastTarget = true;
            PlaceTopLeft(window.rectTransform, SideX, 0f, CatalogWidth, height);
            EnsureOutline(window.gameObject, FrameOutline, 1.5f);
            var accent = EnsureImage(window.transform, "CatalogAccent", Cyan);
            accent.rectTransform.anchorMin = new Vector2(0f, 1f);
            accent.rectTransform.anchorMax = new Vector2(1f, 1f);
            accent.rectTransform.pivot = new Vector2(0.5f, 1f);
            accent.rectTransform.anchoredPosition = Vector2.zero;
            accent.rectTransform.sizeDelta = new Vector2(0f, 3f);
            refs.Window = window.gameObject;

            // 목록 + 검색
            var list = EnsureRect(window.transform, "ListPage");
            Stretch(list, 0f);
            refs.ListPage = list.gameObject;
            var title = Adopt(root, "Title", list).GetComponent<TMP_Text>();
            Style(title, 20f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            title.text = "시설 목록 <color=#6B8C99><size=70%>· " + entries.Count + "</size></color>";
            PlaceTopLeft(title.rectTransform, 16f, -12f, CatalogInnerWidth, 28f);

            refs.Search = BuildSearchField(list, font);

            var scrollTop = 96f;
            var scrollHeight = height - scrollTop - 12f;
            var scroll = EnsureRect(list, "CatalogScroll");
            PlaceTopLeft(scroll, 16f, -scrollTop, CatalogInnerWidth, scrollHeight);
            var scrollRect = EnsureComponent<ScrollRect>(scroll.gameObject);
            var viewport = EnsureImage(scroll, "Viewport", new Color(0f, 0f, 0f, 0f));
            viewport.raycastTarget = true;
            Stretch(viewport.rectTransform, 0f);
            viewport.rectTransform.offsetMax = new Vector2(-12f, 0f);
            EnsureComponent<RectMask2D>(viewport.gameObject);
            var content = EnsureRect(viewport.transform, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var contentLayout = EnsureComponent<VerticalLayoutGroup>(content.gameObject);
            ConfigureVertical(contentLayout, new RectOffset(0, 0, 0, 0), RowSpacing);
            var contentFitter = EnsureComponent<ContentSizeFitter>(content.gameObject);
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollbar = BuildScrollbar(scroll);
            scrollRect.content = content;
            scrollRect.viewport = viewport.rectTransform;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false;
            scrollRect.scrollSensitivity = 32f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollRect.verticalScrollbarSpacing = 0f;

            refs.Rows = new List<BuildingMenuCatalogRowView>();
            var rowWidth = CatalogInnerWidth - 12f;
            for (var i = 0; i < entries.Count; i++)
            {
                var id = entries[i].BuildingId;
                refs.Rows.Add(BuildCatalogRow(content, view, binder, id, i, rowWidth, font));
            }

            var empty = EnsureText(list, "EmptyResult", font);
            Style(empty, 15f, SubText, TextAlignmentOptions.Center, FontStyles.Normal);
            empty.text = "맞는 시설이 없습니다.";
            PlaceTopLeft(empty.rectTransform, 16f, -scrollTop - 24f, CatalogInnerWidth, 32f);
            empty.gameObject.SetActive(false);
            refs.Empty = empty.gameObject;

            // 상세
            var detail = EnsureRect(window.transform, "DetailPage");
            Stretch(detail, 0f);
            refs.DetailPage = detail.gameObject;

            var back = EnsureImage(detail, "BackButton", Color.white);
            back.raycastTarget = true;
            PlaceTopLeft(back.rectTransform, 16f, -12f, 128f, 32f);
            StyleButton(back.gameObject);
            EnsureOutline(back.gameObject, new Color(0.3f, 0.85f, 1f, 0.6f), 1f);
            var backLabel = EnsureText(back.transform, "Label", font);
            Style(backLabel, 15f, Cyan, TextAlignmentOptions.Center, FontStyles.Bold);
            backLabel.text = "< 시설 목록";
            Stretch(backLabel.rectTransform, 0f);
            refs.Back = back.GetComponent<Button>();

            var thumbFrame = EnsureImage(detail, "ThumbFrame", new Color(0.03f, 0.08f, 0.11f, 1f));
            PlaceTopLeft(thumbFrame.rectTransform, 16f, -56f, 136f, 136f);
            EnsureOutline(thumbFrame.gameObject, new Color(0.3f, 0.85f, 1f, 0.45f), 1f);
            refs.Thumb = EnsureImage(thumbFrame.transform, "DetailThumb", Color.white);
            refs.Thumb.preserveAspect = true;
            Stretch(refs.Thumb.rectTransform, 10f);
            refs.Thumb.enabled = false;

            var textX = 168f;
            var textWidth = CatalogWidth - textX - 16f;
            refs.Name = EnsureText(detail, "DetailPageName", font);
            Style(refs.Name, 22f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            refs.Name.enableAutoSizing = true;
            refs.Name.fontSizeMin = 16f;
            refs.Name.fontSizeMax = 22f;
            refs.Name.text = string.Empty;
            PlaceTopLeft(refs.Name.rectTransform, textX, -56f, textWidth, 34f);

            refs.Condition = EnsureText(detail, "DetailPageCondition", font);
            Style(refs.Condition, 14f, ConditionText, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            refs.Condition.text = string.Empty;
            PlaceTopLeft(refs.Condition.rectTransform, textX, -92f, textWidth, 24f);
            refs.Condition.gameObject.SetActive(false);

            var functionLabel = EnsureText(detail, "FunctionLabel", font);
            Style(functionLabel, 12.5f, Cyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            functionLabel.text = "기능";
            PlaceTopLeft(functionLabel.rectTransform, textX, -120f, textWidth, 18f);

            refs.Function = EnsureText(detail, "DetailPageFunction", font);
            Style(refs.Function, 14.5f, BodyText, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            refs.Function.enableAutoSizing = true;
            refs.Function.fontSizeMin = 11.5f;
            refs.Function.fontSizeMax = 14.5f;
            refs.Function.textWrappingMode = TextWrappingModes.Normal;
            refs.Function.lineSpacing = 2f;
            refs.Function.text = string.Empty;
            PlaceTopLeft(refs.Function.rectTransform, textX, -140f, textWidth, 52f);

            // 건설 비용 행 수(1~3)에 맞춰 설치 상태가 바로 아래에 붙도록 세로 레이아웃으로 쌓는다.
            foreach (var legacy in new[] { "DetailCostSection", "DetailStatus" })
            {
                var old = detail.Find(legacy);
                if (old != null)
                {
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                }
            }

            var body = EnsureRect(detail, "DetailBody");
            PlaceTopLeft(body, 16f, -206f, CatalogInnerWidth, 10f);
            var bodyLayout = EnsureComponent<VerticalLayoutGroup>(body.gameObject);
            ConfigureVertical(bodyLayout, new RectOffset(0, 0, 0, 0), 4f);
            var bodyFitter = EnsureComponent<ContentSizeFitter>(body.gameObject);
            bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var costDivider = EnsureImage(body, "CostDivider", CyanLine);
            EnsureLayoutElement(costDivider.gameObject, 1.5f);
            var costLabel = EnsureText(body, "CostLabel", font);
            Style(costLabel, 12.5f, Cyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            costLabel.text = "건설 비용 <color=#6B8C99>· 보유 / 필요</color>";
            EnsureLayoutElement(costLabel.gameObject, 18f);
            refs.CostRows = BuildCostRows(body, font, CatalogInnerWidth, 30f, "DetailCostRow_");

            var gap = EnsureRect(body, "StatusGap");
            EnsureLayoutElement(gap.gameObject, 6f);
            var statusBack = EnsureImage(body, "DetailStatus", new Color(0.05f, 0.1f, 0.14f, 0.95f));
            EnsureLayoutElement(statusBack.gameObject, 56f);
            refs.StatusAccent = EnsureImage(statusBack.transform, "DetailStatusAccent", new Color(0.45f, 0.62f, 0.7f, 1f));
            refs.StatusAccent.rectTransform.anchorMin = new Vector2(0f, 0f);
            refs.StatusAccent.rectTransform.anchorMax = new Vector2(0f, 1f);
            refs.StatusAccent.rectTransform.pivot = new Vector2(0f, 0.5f);
            refs.StatusAccent.rectTransform.anchoredPosition = Vector2.zero;
            refs.StatusAccent.rectTransform.sizeDelta = new Vector2(4f, 0f);
            refs.Status = EnsureText(statusBack.transform, "DetailPageStatus", font);
            Style(refs.Status, 15f, new Color(0.72f, 0.84f, 0.9f, 1f), TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            refs.Status.enableAutoSizing = true;
            refs.Status.fontSizeMin = 11.5f;
            refs.Status.fontSizeMax = 15f;
            refs.Status.textWrappingMode = TextWrappingModes.Normal;
            refs.Status.text = BuildingMenuDisplayFormatter.AvailabilityText(BuildingAvailabilityDisplayKind.Idle, string.Empty);
            Stretch(refs.Status.rectTransform, 0f);
            refs.Status.rectTransform.offsetMin = new Vector2(16f, 4f);
            refs.Status.rectTransform.offsetMax = new Vector2(-10f, -4f);
            detail.gameObject.SetActive(false);

            window.gameObject.SetActive(false);
            return refs;
        }

        private static TMP_InputField BuildSearchField(Transform list, TMP_FontAsset font)
        {
            var back = EnsureImage(list, "SearchField", new Color(0.04f, 0.09f, 0.12f, 1f));
            back.raycastTarget = true;
            PlaceTopLeft(back.rectTransform, 16f, -50f, CatalogInnerWidth, 36f);
            EnsureOutline(back.gameObject, new Color(0.3f, 0.85f, 1f, 0.45f), 1f);

            var area = EnsureRect(back.transform, "TextArea");
            Stretch(area, 0f);
            area.offsetMin = new Vector2(12f, 4f);
            area.offsetMax = new Vector2(-12f, -4f);
            EnsureComponent<RectMask2D>(area.gameObject);

            var placeholder = EnsureText(area, "Placeholder", font);
            Style(placeholder, 15f, new Color(0.45f, 0.6f, 0.66f, 1f), TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            placeholder.text = "시설명 검색";
            Stretch(placeholder.rectTransform, 0f);

            var text = EnsureText(area, "Text", font);
            Style(text, 15f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            text.text = string.Empty;
            Stretch(text.rectTransform, 0f);

            var input = EnsureComponent<TMP_InputField>(back.gameObject);
            input.targetGraphic = back;
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.fontAsset = font;
            input.pointSize = 15f;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.characterLimit = 24;
            input.caretColor = Cyan;
            input.customCaretColor = true;
            input.selectionColor = new Color(0.2f, 0.75f, 0.9f, 0.45f);
            input.richText = false;
            input.restoreOriginalTextOnEscape = false;
            var navigation = input.navigation;
            navigation.mode = Navigation.Mode.None;
            input.navigation = navigation;
            var colors = input.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.85f, 1f, 1f, 1f);
            colors.selectedColor = new Color(0.85f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.95f, 1f, 1f);
            input.colors = colors;
            input.SetTextWithoutNotify(string.Empty);
            return input;
        }

        private static Scrollbar BuildScrollbar(RectTransform scroll)
        {
            var track = EnsureImage(scroll, "Scrollbar", new Color(1f, 1f, 1f, 0.06f));
            track.raycastTarget = true;
            track.rectTransform.anchorMin = new Vector2(1f, 0f);
            track.rectTransform.anchorMax = new Vector2(1f, 1f);
            track.rectTransform.pivot = new Vector2(1f, 0.5f);
            track.rectTransform.anchoredPosition = Vector2.zero;
            track.rectTransform.sizeDelta = new Vector2(6f, 0f);
            var area = EnsureRect(track.transform, "SlidingArea");
            Stretch(area, 0f);
            var handle = EnsureImage(area, "Handle", new Color(0.45f, 0.95f, 1f, 0.55f));
            handle.raycastTarget = true;
            Stretch(handle.rectTransform, 0f);
            var bar = EnsureComponent<Scrollbar>(track.gameObject);
            bar.handleRect = handle.rectTransform;
            bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.BottomToTop;
            var navigation = bar.navigation;
            navigation.mode = Navigation.Mode.None;
            bar.navigation = navigation;
            return bar;
        }

        private static BuildingMenuCatalogRowView BuildCatalogRow(
            RectTransform content,
            BuildingMenuView view,
            BuildingMenuBinder binder,
            string buildingId,
            int index,
            float width,
            TMP_FontAsset font)
        {
            var bg = EnsureImage(content, "Row_" + buildingId, Color.white);
            bg.raycastTarget = true;
            bg.transform.SetSiblingIndex(index);
            EnsureLayoutElement(bg.gameObject, RowHeight);
            PlaceTopLeft(bg.rectTransform, 0f, -index * (RowHeight + RowSpacing), width, RowHeight);
            StyleButton(bg.gameObject);
            bg.GetComponent<Button>().colors = WithNormal(bg.GetComponent<Button>().colors, RowBack);
            EnsureOutline(bg.gameObject, new Color(0.3f, 0.85f, 1f, 0.22f), 1f);

            var highlight = EnsureImage(bg.transform, "SelectedHighlight", HighlightFill);
            Stretch(highlight.rectTransform, 0f);
            EnsureOutline(highlight.gameObject, new Color(0.45f, 0.95f, 1f, 0.95f), 1.5f);
            var bar = EnsureImage(highlight.transform, "SelectedBar", Cyan);
            bar.rectTransform.anchorMin = new Vector2(0f, 0f);
            bar.rectTransform.anchorMax = new Vector2(0f, 1f);
            bar.rectTransform.pivot = new Vector2(0f, 0.5f);
            bar.rectTransform.anchoredPosition = Vector2.zero;
            bar.rectTransform.sizeDelta = new Vector2(4f, 0f);
            highlight.transform.SetSiblingIndex(0);
            highlight.gameObject.SetActive(false);

            var icon = EnsureImage(bg.transform, "Icon", Color.white);
            icon.preserveAspect = true;
            PlaceTopLeft(icon.rectTransform, 10f, -8f, 48f, 48f);

            var costWidth = 136f;
            var textWidth = width - 68f - costWidth - 14f;
            var name = EnsureText(bg.transform, "Name", font);
            Style(name, 16f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            name.enableAutoSizing = true;
            name.fontSizeMin = 13f;
            name.fontSizeMax = 16f;
            PlaceTopLeft(name.rectTransform, 68f, -6f, textWidth, 22f);

            var purpose = EnsureText(bg.transform, "Purpose", font);
            Style(purpose, 12.5f, SubText, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            purpose.enableAutoSizing = true;
            purpose.fontSizeMin = 10.5f;
            purpose.fontSizeMax = 12.5f;
            purpose.textWrappingMode = TextWrappingModes.Normal;
            PlaceTopLeft(purpose.rectTransform, 68f, -29f, textWidth, 31f);

            var cost = EnsureText(bg.transform, "Costs", font);
            Style(cost, 12.5f, BodyText, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
            cost.enableAutoSizing = true;
            cost.fontSizeMin = 10.5f;
            cost.fontSizeMax = 12.5f;
            cost.lineSpacing = -4f;
            cost.rectTransform.anchorMin = cost.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            cost.rectTransform.pivot = new Vector2(1f, 0.5f);
            cost.rectTransform.anchoredPosition = new Vector2(-10f, 0f);
            cost.rectTransform.sizeDelta = new Vector2(costWidth, RowHeight - 10f);

            var entry = EnsureComponent<BuildingMenuEntryButton>(bg.gameObject);
            entry.EditorSet(buildingId, binder);
            EditorUtility.SetDirty(entry);
            var row = EnsureComponent<BuildingMenuCatalogRowView>(bg.gameObject);
            var so = new SerializedObject(row);
            so.FindProperty("view").objectReferenceValue = view;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("nameText").objectReferenceValue = name;
            so.FindProperty("purposeText").objectReferenceValue = purpose;
            so.FindProperty("costText").objectReferenceValue = cost;
            so.FindProperty("selectedHighlight").objectReferenceValue = highlight.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            return row;
        }

        private static ColorBlock WithNormal(ColorBlock colors, Color normal)
        {
            colors.normalColor = normal;
            colors.selectedColor = normal;
            return colors;
        }

        private static void StyleButton(GameObject go)
        {
            var image = go.GetComponent<Image>();
            image.sprite = null;
            image.color = Color.white;
            image.raycastTarget = true;
            var button = EnsureComponent<Button>(go);
            button.targetGraphic = image;
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
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }

        private static void ConfigureVertical(VerticalLayoutGroup layout, RectOffset padding, float spacing)
        {
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childScaleWidth = false;
            layout.childScaleHeight = false;
        }

        private static void EnsureLayoutElement(GameObject go, float preferredHeight)
        {
            var element = EnsureComponent<LayoutElement>(go);
            element.minHeight = preferredHeight;
            element.preferredHeight = preferredHeight;
            element.flexibleHeight = 0f;
        }

        /// <summary>이전 실행 위치와 상관없이 이름으로 찾아 새 부모 아래로 옮긴다. 참조 GUID·fileID는 유지된다.</summary>
        private static Transform Adopt(Transform root, string name, Transform parent)
        {
            var found = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name && t != root);
            if (found == null)
            {
                throw new InvalidOperationException(name + "을(를) 찾을 수 없습니다.");
            }

            if (found.parent != parent)
            {
                found.SetParent(parent, false);
            }

            found.gameObject.SetActive(true);
            return found;
        }

        private static void Set(SerializedObject so, string property, UnityEngine.Object value)
        {
            var prop = so.FindProperty(property);
            if (prop == null)
            {
                throw new InvalidOperationException("BuildingMenuView." + property + " 필드가 없습니다.");
            }

            prop.objectReferenceValue = value;
        }

        private static void SetArray(SerializedObject so, string property, List<UnityEngine.Object> values)
        {
            var prop = so.FindProperty(property);
            if (prop == null)
            {
                throw new InvalidOperationException("BuildingMenuView." + property + " 필드가 없습니다.");
            }

            prop.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
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

            image.enabled = true;
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
            // Ellipsis는 줄 높이가 칸보다 크면 줄 전체를 숨기므로 Overflow로 둔다.
            text.overflowMode = TextOverflowModes.Overflow;
            text.characterSpacing = 0f;
            text.lineSpacing = 0f;
            text.raycastTarget = false;
            text.richText = true;
            EditorUtility.SetDirty(text);
        }

        private static void EnsureOutline(GameObject target, Color color, float distance)
        {
            var outline = EnsureComponent<Outline>(target);
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

        private static void PlaceMiddleLeft(RectTransform rect, float x, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
