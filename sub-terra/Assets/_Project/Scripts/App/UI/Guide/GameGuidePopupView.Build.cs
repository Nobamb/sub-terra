using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Guide
{
    public sealed partial class GameGuidePopupView
    {
        private sealed class ScrollParts
        {
            public RectTransform Root;
            public ScrollRect Scroll;
            public RectTransform Viewport;
            public RectTransform Content;
            public Scrollbar Bar;
        }

        private sealed class TabList
        {
            public GuideTabKind Tab;
            public RectTransform Root;
            public ScrollParts Scroll;
            public RectTransform FilterRow;
            public readonly List<GameGuideCardView> Cards = new List<GameGuideCardView>();
            public bool Built;
        }

        private readonly TabList[] lists = new TabList[3];
        private readonly GameGuideButton[] tabButtons = new GameGuideButton[3];
        private readonly GameGuideButton[] filterButtons = new GameGuideButton[3];
        private readonly List<GameGuideCardView> stepTiles = new List<GameGuideCardView>();
        private readonly List<GameGuideButton> miniItems = new List<GameGuideButton>();

        private GameGuideButton closeButton;
        private TMP_Text titleText;
        private RectTransform firstRoot;
        private CanvasGroup firstExpandedGroup;
        private CanvasGroup firstCollapsedGroup;
        private GameGuideButton firstToggle;
        private TMP_Text firstToggleLabel;
        private RectTransform firstToggleChevron;
        private RectTransform detailPanel;
        private ScrollParts detailScroll;

        public GameGuideButton CloseButton => closeButton;
        public IReadOnlyList<GameGuideButton> TabButtons => tabButtons;
        public IReadOnlyList<GameGuideButton> FilterButtons => filterButtons;
        public IReadOnlyList<GameGuideCardView> StepTiles => stepTiles;
        public GameGuideButton FirstToggle => firstToggle;
        public ScrollRect DetailScroll => detailScroll != null ? detailScroll.Scroll : null;
        public string TitleString => titleText != null ? titleText.text : string.Empty;
        public bool FirstExploreVisible => firstRoot != null && firstRoot.gameObject.activeSelf;
        public float FirstExploreHeight => firstRoot != null ? firstRoot.sizeDelta.y : 0f;

        public ScrollRect ListScroll(GuideTabKind tab)
        {
            var list = lists[(int)tab];
            return list != null && list.Scroll != null ? list.Scroll.Scroll : null;
        }

        public IReadOnlyList<GameGuideCardView> Cards(GuideTabKind tab)
        {
            EnsureList(tab);
            return lists[(int)tab].Cards;
        }

        private void Build()
        {
            skin = Resources.Load<MineResetTimedPopupSkin>(MineResetTimedPopupSkin.ResourcePath);
            font = skin != null ? skin.font : null;
            if (font == null)
            {
                font = TMP_Settings.defaultFontAsset;
            }

            sprites = GuideSprites.Load(data);
            canvas = GetComponent<Canvas>();
            backdrop = GetComponent<Image>();
            backdrop.color = ResourceSellUi.WithAlpha(BackdropColor, 0f);
            backdrop.canvasRenderer.cullTransparentMesh = false;

            card = ResourceSellUi.Centered(transform, "Card", Vector2.zero, CardSize);
            blocker = ResourceSellUi.Image(card, "InputBlocker", null, Color.clear);
            blocker.raycastTarget = true;
            blocker.canvasRenderer.cullTransparentMesh = false;

            book = GameGuideBookFx.Build(card, CardSize, skin);

            contentRoot = ResourceSellUi.Centered(card, "Content", Vector2.zero, CardSize);
            tabsRect = MakeGroup("TabsGroup", out tabsGroup);
            listRect = MakeGroup("ListGroup", out listGroup);
            detailRect = MakeGroup("DetailGroup", out detailGroup);

            BuildHeader();
            BuildTabs();
            BuildFirstExplore();
            BuildDetailPanel();
            for (var i = 0; i < lists.Length; i++)
            {
                lists[i] = new TabList { Tab = (GuideTabKind)i };
                BuildListShell(lists[i]);
            }

            BuildFilterChips();
            UiKeyboardSubmitGuard.ConfigureButtonsUnder(contentRoot);
            gameObject.SetActive(false);
        }

        private RectTransform MakeGroup(string name, out CanvasGroup group)
        {
            var image = ResourceSellUi.Image(contentRoot, name, null, Color.clear);
            image.raycastTarget = false;
            group = image.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            return image.rectTransform;
        }

        private void BuildHeader()
        {
            titleText = ResourceSellUi.Label(tabsRect, "Title", 0f, 46f, CardSize.x, 64f, font, 40f, FontStyles.Bold, TitleColor,
                TextAlignmentOptions.Center);
            titleText.text = Title;
            titleText.characterSpacing = 3f;

            closeButton = GameGuideButton.Create(tabsRect, "CloseButton", CardSize.x - Pad - 52f, 54f, 52f, 52f, font, string.Empty,
                20f, GameGuideButton.Kind.Close, ResourceSellArt.Cross(), 26f);
            closeButton.Button.onClick.AddListener(RequestClose);
        }

        private void BuildTabs()
        {
            const float gap = 8f;
            var tabWidth = (BodyWidth - gap * 2f) / 3f;
            for (var i = 0; i < 3; i++)
            {
                var index = i;
                var button = GameGuideButton.Create(tabsRect, "Tab_" + i, Pad + i * (tabWidth + gap), TabY, tabWidth, TabHeight,
                    font, GameGuideCatalog.TabTitle((GuideTabKind)i), 24f, GameGuideButton.Kind.Tab);
                button.Button.onClick.AddListener(() => guideState.SelectTab((GuideTabKind)index));
                tabButtons[i] = button;
            }
        }

        // ------------------------------------------------------------------ 첫 탐사 안내

        private void BuildFirstExplore()
        {
            firstRoot = ResourceSellUi.Place(listRect, "FirstExplore", Pad, BodyY, BodyWidth, FirstExpanded);
            var glow = ResourceSellUi.Image(firstRoot, "Glow", ResourceSellArt.SoftRect(), ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.08f));
            glow.rectTransform.offsetMin = new Vector2(-8f, -8f);
            glow.rectTransform.offsetMax = new Vector2(8f, 8f);
            ResourceSellUi.Image(firstRoot, "Face", ResourceSellArt.ChamferFill(), new Color(0.03f, 0.06f, 0.09f, 0.97f));
            ResourceSellUi.Image(firstRoot, "Border", ResourceSellArt.ChamferOutline(), ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.6f));

            // 펼친 모양: 제목·단계 타일 다섯 개·안내 문구.
            var expandedImage = ResourceSellUi.Image(firstRoot, "Expanded", null, Color.clear);
            expandedImage.raycastTarget = false;
            firstExpandedGroup = expandedImage.gameObject.AddComponent<CanvasGroup>();
            var expanded = expandedImage.rectTransform;
            ResourceSellUi.AddImage(ResourceSellUi.Place(expanded, "Accent", 18f, 14f, 4f, 30f), null, ResourceSellUi.Teal);
            var title = ResourceSellUi.Label(expanded, "Title", 32f, 10f, 620f, 40f, font, 28f, FontStyles.Bold, Color.white,
                TextAlignmentOptions.MidlineLeft);
            title.text = GameGuideCatalog.FirstExploreTitle;
            var steps = GameGuideCatalog.FirstSteps;
            const float arrowGap = 24f;
            var tileGap = (BodyWidth - 28f - steps.Count * GameGuideCardView.StepWidth) / (steps.Count - 1);
            for (var i = 0; i < steps.Count; i++)
            {
                var x = 14f + i * (GameGuideCardView.StepWidth + tileGap);
                var tile = GameGuideCardView.CreateStepTile(expanded, steps[i], x, 54f, font, sprites);
                tile.Clicked += OnCardClicked;
                stepTiles.Add(tile);
                if (i < steps.Count - 1)
                {
                    var arrow = ResourceSellUi.AddImage(
                        ResourceSellUi.Place(expanded, "Arrow" + i, x + GameGuideCardView.StepWidth + (tileGap - arrowGap) * 0.5f,
                            54f + GameGuideCardView.StepHeight * 0.5f - 12f, arrowGap, 24f),
                        GameGuideArt.ArrowRight(), ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.85f));
                    arrow.preserveAspect = true;
                }
            }

            var info = ResourceSellUi.AddImage(ResourceSellUi.Place(expanded, "InfoIcon", 20f, 54f + GameGuideCardView.StepHeight + 8f, 20f, 20f),
                GameGuideArt.Info(), ResourceSellUi.Teal);
            info.preserveAspect = true;
            var hint = ResourceSellUi.Label(expanded, "Hint", 48f, 54f + GameGuideCardView.StepHeight + 4f, 700f, 28f, font, 17f,
                FontStyles.Normal, ResourceSellUi.Teal, TextAlignmentOptions.MidlineLeft);
            hint.text = GameGuideCatalog.FirstExploreHint;

            // 접은 모양: 이름표와 단계 아이콘·제목만.
            var collapsedImage = ResourceSellUi.Image(firstRoot, "Collapsed", null, Color.clear);
            collapsedImage.raycastTarget = false;
            firstCollapsedGroup = collapsedImage.gameObject.AddComponent<CanvasGroup>();
            var collapsed = collapsedImage.rectTransform;
            ResourceSellUi.AddImage(ResourceSellUi.Place(collapsed, "Accent", 18f, 22f, 4f, 32f), null, ResourceSellUi.Teal);
            var label = ResourceSellUi.Label(collapsed, "Label", 32f, 18f, 150f, 40f, font, 24f, FontStyles.Bold, Color.white,
                TextAlignmentOptions.MidlineLeft);
            label.text = GameGuideCatalog.FirstExploreCollapsedLabel;
            const float miniWidth = 184f;
            var miniGap = 24f;
            var startX = 184f;
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                var x = startX + i * (miniWidth + miniGap);
                var mini = GameGuideButton.Create(collapsed, "Mini_" + step.Number, x, 16f, miniWidth, 44f, font, step.Title, 16f,
                    GameGuideButton.Kind.Normal, StepIcon(step.IconKey), 24f);
                var targetId = step.TargetCardId;
                mini.Button.onClick.AddListener(() => OnCardClicked(targetId));
                miniItems.Add(mini);
                if (i < steps.Count - 1)
                {
                    var arrow = ResourceSellUi.AddImage(ResourceSellUi.Place(collapsed, "MiniArrow" + i, x + miniWidth + 4f, 26f, 22f, 24f),
                        GameGuideArt.ArrowRight(), ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.8f));
                    arrow.preserveAspect = true;
                }
            }

            // 접기·펼치기 버튼(오른쪽 위).
            firstToggle = GameGuideButton.Create(firstRoot, "ToggleButton", BodyWidth - 120f, 20f, 106f, 36f, font, "접기", 17f,
                GameGuideButton.Kind.Normal);
            firstToggle.Button.onClick.AddListener(() => guideState.ToggleFirstExplore());
            firstToggleLabel = firstToggle.GetComponentInChildren<TMP_Text>(true);
            firstToggleLabel.rectTransform.anchoredPosition = new Vector2(12f, 0f);
            var chevronRect = ResourceSellUi.Centered(firstToggle.transform, "Chevron", new Vector2(-34f, 0f), new Vector2(18f, 18f));
            ResourceSellUi.AddImage(chevronRect, GameGuideArt.ChevronDown(), Color.white).preserveAspect = true;
            firstToggleChevron = chevronRect;
            var graphics = new List<Graphic>(firstToggle.GetComponentsInChildren<Graphic>(true));
            // 접기 글자·화살표 색을 버튼 상태에 맞춘다.
            var contents = new List<Graphic>();
            foreach (var graphic in graphics)
            {
                if (graphic is TMP_Text || graphic.gameObject.name == "Chevron")
                {
                    contents.Add(graphic);
                }
            }

            firstToggle.Setup(firstToggle.Button, firstToggle.transform.Find("Face").GetComponent<Image>(),
                firstToggle.transform.Find("Border").GetComponent<Image>(), firstToggle.transform.Find("Glow").GetComponent<Image>(),
                null, contents.ToArray(), GameGuideButton.Kind.Normal);
        }

        private Sprite StepIcon(string key)
        {
            switch (key)
            {
                case "icon.copper": return sprites.Get("item:" + DataIds.Minerals.Copper);
                case "icon.elevator": return sprites.Get("fac.elevator");
                default: return sprites.Get(key);
            }
        }

        // ------------------------------------------------------------------ 목록

        private void BuildListShell(TabList list)
        {
            list.Root = ResourceSellUi.Place(listRect, "List_" + list.Tab, Pad, BodyY, ListWidth, BodyHeight);
            var topOffset = list.Tab == GuideTabKind.Resources ? FilterRowHeight + 8f : 0f;
            list.Scroll = MakeScroll(list.Root, "Scroll", 0f, 0f, 0f, topOffset, 14f, true);
            list.Root.gameObject.SetActive(false);
        }

        private void BuildFilterChips()
        {
            var list = lists[(int)GuideTabKind.Resources];
            list.FilterRow = ResourceSellUi.Place(list.Root, "FilterRow", 0f, 0f, ListWidth, FilterRowHeight);
            var names = new[] { "전체", "자원", "시설" };
            for (var i = 0; i < 3; i++)
            {
                var index = i;
                var chip = GameGuideButton.Create(list.FilterRow, "Filter_" + i, i * 102f, 4f, 94f, 40f, font, names[i], 19f,
                    GameGuideButton.Kind.Chip);
                chip.Button.onClick.AddListener(() => guideState.SetFilter((GuideFilter)index));
                filterButtons[i] = chip;
            }
        }

        /// <summary>스크롤 영역을 맨 위로 되돌린다(스크롤바 기본값이 맨 아래라 처음 한 번 필요하다).</summary>
        private static void ResetScroll(ScrollParts parts)
        {
            if (parts == null || parts.Scroll == null)
            {
                return;
            }

            parts.Scroll.StopMovement();
            parts.Content.anchoredPosition = Vector2.zero;
            parts.Scroll.verticalNormalizedPosition = 1f;
            parts.Content.anchoredPosition = Vector2.zero;
        }

        private bool EnsureList(GuideTabKind tab)
        {
            var list = lists[(int)tab];
            if (list.Built)
            {
                return false;
            }

            list.Built = true;
            var defs = GameGuideCatalog.ForTab(tab);
            var layout = tab == GuideTabKind.Resources ? GameGuideCardView.Layout.Icon : GameGuideCardView.Layout.Wide;
            for (var i = 0; i < defs.Count; i++)
            {
                var view = GameGuideCardView.Create(list.Scroll.Content, defs[i], layout, 0f, 0f, font, sprites);
                view.Clicked += OnCardClicked;
                list.Cards.Add(view);
            }

            UiKeyboardSubmitGuard.ConfigureButtonsUnder(list.Root);
            return true;
        }

        /// <summary>현재 필터에 맞는 카드만 보이게 하고 격자 위치·내용 높이를 다시 계산한다.</summary>
        private void LayoutCards(GuideTabKind tab)
        {
            var list = lists[(int)tab];
            var isIcon = tab == GuideTabKind.Resources;
            var cols = isIcon ? 3 : 2;
            var cellW = isIcon ? GameGuideCardView.IconWidth : GameGuideCardView.WideWidth;
            var cellH = isIcon ? GameGuideCardView.IconHeight : GameGuideCardView.WideHeight;
            const float gap = 10f;
            var index = 0;
            for (var i = 0; i < list.Cards.Count; i++)
            {
                var cardView = list.Cards[i];
                var visible = tab != GuideTabKind.Resources || guideState.PassesFilter(cardView.Def);
                if (cardView.gameObject.activeSelf != visible)
                {
                    cardView.gameObject.SetActive(visible);
                }

                if (!visible)
                {
                    continue;
                }

                var row = index / cols;
                var col = index % cols;
                var x = col * (cellW + gap);
                var y = row * (cellH + gap);
                var rect = cardView.Rect;
                rect.anchoredPosition = new Vector2(x + cellW * 0.5f, -(y + cellH * 0.5f));
                index++;
            }

            var rows = (index + cols - 1) / cols;
            var height = rows > 0 ? rows * cellH + (rows - 1) * gap + 6f : 0f;
            list.Scroll.Content.sizeDelta = new Vector2(0f, height);
        }

        // ------------------------------------------------------------------ 스크롤 도우미

        /// <summary>뷰포트·콘텐츠·세로 스크롤바가 있는 스크롤 영역. 내용 캔버스를 따로 둬 목록 갱신이 다른 창을 다시 그리지 않게 한다.</summary>
        private ScrollParts MakeScroll(Transform parent, string name, float left, float bottom, float right, float top,
            float barSpace, bool nestedCanvas)
        {
            var parts = new ScrollParts();
            var root = ResourceSellUi.Image(parent, name, null, Color.clear);
            root.raycastTarget = false;
            parts.Root = root.rectTransform;
            parts.Root.offsetMin = new Vector2(left, bottom);
            parts.Root.offsetMax = new Vector2(-right, -top);

            var viewport = ResourceSellUi.Image(parts.Root, "Viewport", null, Color.clear);
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            parts.Viewport = viewport.rectTransform;
            parts.Viewport.offsetMax = new Vector2(-barSpace, 0f);

            var content = ResourceSellUi.Image(parts.Viewport, "Content", null, Color.clear);
            content.raycastTarget = false;
            parts.Content = content.rectTransform;
            parts.Content.anchorMin = new Vector2(0f, 1f);
            parts.Content.anchorMax = new Vector2(1f, 1f);
            parts.Content.pivot = new Vector2(0.5f, 1f);
            parts.Content.offsetMin = new Vector2(0f, -100f);
            parts.Content.offsetMax = Vector2.zero;
            if (nestedCanvas)
            {
                parts.Content.gameObject.AddComponent<Canvas>();
                parts.Content.gameObject.AddComponent<GraphicRaycaster>();
            }

            var barRect = ResourceSellUi.Image(parts.Root, "Scrollbar", null, new Color(1f, 1f, 1f, 0.06f)).rectTransform;
            barRect.anchorMin = new Vector2(1f, 0f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(1f, 0.5f);
            barRect.sizeDelta = new Vector2(8f, 0f);
            barRect.offsetMin = new Vector2(-8f, 2f);
            barRect.offsetMax = new Vector2(0f, -2f);
            var sliding = ResourceSellUi.Image(barRect, "Sliding", null, Color.clear).rectTransform;
            var handle = ResourceSellUi.Image(sliding, "Handle", ResourceSellArt.ChamferFill(), ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.75f));
            handle.raycastTarget = true;
            var bar = barRect.gameObject.AddComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.handleRect = handle.rectTransform;
            bar.targetGraphic = handle;
            parts.Bar = bar;

            var scroll = parts.Root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = parts.Viewport;
            scroll.content = parts.Content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            parts.Scroll = scroll;
            return parts;
        }

        // ------------------------------------------------------------------ 본문 배치

        private static void SetPlace(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h * 0.5f));
            rect.sizeDelta = new Vector2(w, h);
        }

        /// <summary>
        /// 탭 아래 본문을 배치한다. 제목·탭 영역은 건드리지 않고, 기본 조작 탭에서는 첫 탐사 안내 높이만큼 목록·상세가 내려온다.
        /// </summary>
        private void LayoutBody()
        {
            var controls = guideState.Tab == GuideTabKind.Controls;
            var firstHeight = Mathf.Lerp(FirstCollapsed, FirstExpanded, firstT);
            var lowerTop = controls ? firstHeight + 12f : 0f;
            var lowerHeight = BodyHeight - lowerTop;

            firstRoot.gameObject.SetActive(controls);
            if (controls)
            {
                SetPlace(firstRoot, Pad, BodyY, BodyWidth, firstHeight);
                var expandedAlpha = GameGuideTimeline.Smooth((firstT - 0.45f) / 0.55f);
                var collapsedAlpha = GameGuideTimeline.Smooth((0.55f - firstT) / 0.55f);
                firstExpandedGroup.alpha = expandedAlpha;
                firstExpandedGroup.blocksRaycasts = expandedAlpha > 0.5f;
                firstExpandedGroup.interactable = expandedAlpha > 0.5f;
                firstCollapsedGroup.alpha = collapsedAlpha;
                firstCollapsedGroup.blocksRaycasts = collapsedAlpha > 0.5f;
                firstCollapsedGroup.interactable = collapsedAlpha > 0.5f;
                firstToggleLabel.text = guideState.FirstExploreExpanded ? "접기" : "펼치기";
                firstToggleChevron.localRotation = Quaternion.Euler(0f, 0f, guideState.FirstExploreExpanded ? 180f : 0f);
            }

            for (var i = 0; i < lists.Length; i++)
            {
                var active = (int)guideState.Tab == i;
                if (lists[i].Root.gameObject.activeSelf != active)
                {
                    lists[i].Root.gameObject.SetActive(active);
                }

                if (active)
                {
                    SetPlace(lists[i].Root, Pad, BodyY + lowerTop, ListWidth, lowerHeight);
                }
            }

            SetPlace(detailPanel, Pad + ListWidth + ColumnGap, BodyY + lowerTop, DetailWidth, lowerHeight);
            var filterActive = guideState.Tab == GuideTabKind.Resources;
            lists[(int)GuideTabKind.Resources].FilterRow.gameObject.SetActive(filterActive);
        }
    }
}
