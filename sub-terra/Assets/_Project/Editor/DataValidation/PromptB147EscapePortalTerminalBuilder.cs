#if UNITY_EDITOR
using System;
using SubTerra.App.Integration;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.App.UI.MainMenu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 147: 긴급 탈출 포탈 창을 웜홀 인트로 + 터미널 접속형 창으로 다시 만든다.
    /// EmergencyEscapePanel 프리팹만 수정한다. 루트 오브젝트와 View/Binder 컴포넌트는 그대로 두어
    /// Integration 씬의 참조(IntegrationRuntimeBinder·브리지·메뉴 컨트롤러)가 깨지지 않는다.
    /// </summary>
    public static class PromptB147EscapePortalTerminalBuilder
    {
        public const string PanelPrefabPath = "Assets/_Project/Prefabs/UI/EmergencyEscapePanel.prefab";

        private const string MineResetArt = "Assets/_Project/Art/UI/SurfaceBase/MineReset/";
        private const string FramePath = MineResetArt + "mine-reset-frame.png";
        private const string PanelPath = MineResetArt + "mine-reset-panel.png";
        private const string ConfirmButtonPath = MineResetArt + "mine-reset-button-confirm.png";
        // Codex 이미지 생성으로 만든 이동 버튼 호버 그림. 버튼 안쪽만 밝아지고 바깥에는 빛이 없다.
        public const string ConfirmHoverPath = "Assets/_Project/Art/UI/EmergencyEscape/escape-portal-button-hover.png";
        private const string FontPath = "Assets/_Project/Fonts/NotoSansKR-Regular_SDF.asset";

        public static readonly Vector2 WindowSize = new Vector2(1000f, 600f);
        public const float SideMargin = 56f;
        public const float RowHeight = 44f;
        public const float RowSpacing = 4f;
        /// <summary>이동·닫기 버튼 높이. 이전 44는 낮아 답답해 넉넉하게 키웠다.</summary>
        public const float ButtonHeight = 60f;
        /// <summary>닫기 버튼을 없애고 이동 버튼 하나만 둔다. 가로는 원본 그림 비율이 크게 늘어지지 않는 폭.</summary>
        public const float ConfirmButtonWidth = 340f;
        public const float HoleSize = 520f;
        public const float HoleCoreSize = 168f;
        /// <summary>EscapeWormholeArt.CoreRingRadius와 같은 값(밝은 고리가 코어 반지름에서 차지하는 비율).</summary>
        private const float CoreRingRatio = 0.88f;

        private static readonly Color TitleColor = new Color(0.84f, 0.98f, 1f, 1f);
        private static readonly Color Cyan = new Color(0.42f, 0.94f, 1f, 1f);
        private static readonly Color TerminalColor = new Color(0.45f, 1f, 0.82f, 1f);
        private static readonly Color MutedText = new Color(0.55f, 0.67f, 0.74f, 1f);

        [MenuItem("SubTerra/UI/Build Prompt-B 147 Escape Portal Terminal")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            var root = PrefabUtility.LoadPrefabContents(PanelPrefabPath);
            try
            {
                ApplyTo(root);
                PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            return "Prompt-B 147 escape portal terminal built: " + PanelPrefabPath;
        }

        public static void ApplyTo(GameObject root)
        {
            var view = root.GetComponent<EmergencyEscapePanelView>();
            var binder = root.GetComponent<EmergencyEscapePanelBinder>();
            if (view == null || binder == null)
            {
                throw new InvalidOperationException("EmergencyEscapePanel의 View/Binder를 찾을 수 없습니다.");
            }

            // 루트와 컴포넌트는 두고 자식만 다시 만든다.
            for (var i = root.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = WindowSize;

            var panelRoot = NewRect("PanelRoot", rootRect, Vector2.zero, WindowSize);
            // 다른 팝업(32,100~)과 광산 초기화 시계(32,000)보다 앞에 그린다. 설정 창(32,760)만 더 위다.
            var rootCanvas = panelRoot.gameObject.AddComponent<Canvas>();
            rootCanvas.overrideSorting = true;
            rootCanvas.sortingOrder = EmergencyEscapePanelView.SortingOrder;
            panelRoot.gameObject.AddComponent<GraphicRaycaster>();

            // 웜홀: 창 뒤의 청록색 블랙홀. 아래 순서대로 그려진다(뒤 → 앞).
            // 빛 번짐 → 바깥 가스 소용돌이 → 안쪽 밝은 원반 → 솟아오르는 입자 → 검은 사건의 지평선.
            // 그림은 런타임에 View.ApplyArt가 절차 생성해 입힌다. 루트는 돌리지 않고 소용돌이 레이어만 각자 속도로 돈다.
            var wormhole = NewRect("Wormhole", panelRoot, Vector2.zero, new Vector2(HoleSize, HoleSize));
            var wormholeGroup = wormhole.gameObject.AddComponent<CanvasGroup>();
            wormholeGroup.interactable = false;
            wormholeGroup.blocksRaycasts = false;
            var glow = NewRect("Glow", wormhole, Vector2.zero, new Vector2(860f, 860f));
            var glowImage = AddImage(glow, null, new Color(0.08f, 0.82f, 0.9f, 0.32f), false);
            var outerSwirl = NewRect("SwirlOuter", wormhole, Vector2.zero, new Vector2(HoleSize + 40f, HoleSize + 40f));
            var outerSwirlImage = AddImage(outerSwirl, null, new Color(1f, 1f, 1f, 0.9f), false);
            var innerSwirl = NewRect("SwirlInner", wormhole, Vector2.zero, new Vector2(HoleSize * 0.7f, HoleSize * 0.7f));
            var innerSwirlImage = AddImage(innerSwirl, null, Color.white, false);
            var particles = NewRect("Particles", wormhole, Vector2.zero, new Vector2(HoleSize, HoleSize));
            // 그래픽 컴포넌트는 CanvasRenderer를 스스로 붙이지 않는 경우가 있어 먼저 붙인다. 없으면 입자가 그려지지 않는다.
            particles.gameObject.AddComponent<CanvasRenderer>();
            var particleGraphic = particles.gameObject.AddComponent<EscapeWormholeParticleGraphic>();
            var particleObject = new SerializedObject(particleGraphic);
            particleObject.FindProperty("ringRadius").floatValue = HoleCoreSize * 0.5f * CoreRingRatio;
            particleObject.ApplyModifiedPropertiesWithoutUndo();
            var core = NewRect("Core", wormhole, Vector2.zero, new Vector2(HoleCoreSize, HoleCoreSize));
            var coreImage = AddImage(core, null, Color.white, false);

            var window = NewRect("Window", panelRoot, Vector2.zero, WindowSize);
            var windowGroup = window.gameObject.AddComponent<CanvasGroup>();

            var shield = Stretch("InputShield", window);
            AddImage(shield, null, new Color(0f, 0f, 0f, 0f), true);
            var panel = Stretch("Panel", window);
            panel.offsetMin = new Vector2(16f, 16f);
            panel.offsetMax = new Vector2(-16f, -16f);
            AddImage(panel, LoadSprite(PanelPath), Color.white, false);
            var hex = Stretch("HexPattern", window);
            hex.offsetMin = new Vector2(20f, 20f);
            hex.offsetMax = new Vector2(-20f, -20f);
            var hexImage = hex.gameObject.AddComponent<RawImage>();
            hexImage.color = new Color(0.35f, 0.9f, 1f, 0.045f);
            hexImage.raycastTarget = false;
            var frame = Stretch("Frame", window);
            AddImage(frame, LoadSprite(FramePath), Color.white, false);

            var title = AddText(window, "Title", font, TopLeft(SideMargin, 30f, 480f, 52f), 36f, TitleColor, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.text = "긴급 탈출 포탈";
            var divider = NewTop("Divider", window, SideMargin, 88f, WindowSize.x - SideMargin * 2f, 2f);
            AddImage(divider, null, new Color(0.3f, 0.9f, 1f, 0.35f), false);
            var cornerClose = BuildCornerCloseButton(window, font);

            // ---- 왼쪽: 이동 목적지 목록 (코어 팝업의 '연결된 시설' 자리) ----
            const float bodyTop = 104f;
            const float bodyHeight = 444f;
            const float listWidth = 296f;
            const float headerHeight = 46f;
            var listPanel = NewTop("ListPanel", window, SideMargin, bodyTop, listWidth, bodyHeight);
            AddImage(listPanel, null, new Color(0.02f, 0.055f, 0.085f, 0.78f), false);
            var listBorder = Stretch("Border", listPanel);
            var listBorderImage = AddBorder(listBorder, new Color(0.25f, 0.55f, 0.65f, 0.55f));
            var header = AddText(listPanel, "Header", font, TopLeft(18f, 8f, listWidth - 36f, headerHeight - 8f), 22f, Cyan, TextAlignmentOptions.MidlineLeft);
            header.text = "이동 목적지";
            header.fontStyle = FontStyles.Bold;
            var headerLine = NewTop("HeaderLine", listPanel, 14f, headerHeight, listWidth - 28f, 1f);
            AddImage(headerLine, null, new Color(0.3f, 0.9f, 1f, 0.28f), false);

            const float viewportTop = headerHeight + 6f;
            var scroll = NewTop("ListScroll", listPanel, 10f, viewportTop, listWidth - 20f, bodyHeight - viewportTop - 8f);
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            var viewport = Stretch("Viewport", scroll);
            viewport.gameObject.AddComponent<RectMask2D>();
            AddImage(viewport, null, new Color(0f, 0f, 0f, 0f), true);
            var content = NewRect("Content", viewport, Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = RowSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false;
            scrollRect.scrollSensitivity = 36f;
            var rowTemplate = BuildRowTemplate(content, font);

            // ---- 오른쪽: CCTV ----
            var cctvLeft = SideMargin + listWidth + 16f;
            var cctvWidth = WindowSize.x - SideMargin - cctvLeft;
            // 버튼 높이를 늘린 만큼 CCTV를 조금 줄여 비용·결과 줄과 버튼이 겹치지 않게 한다.
            const float cctvHeight = 318f;
            var cctv = NewTop("CctvArea", window, cctvLeft, bodyTop, cctvWidth, cctvHeight);
            AddImage(cctv, null, new Color(0.01f, 0.025f, 0.04f, 1f), false);
            var videoRect = Stretch("Video", cctv);
            videoRect.offsetMin = new Vector2(4f, 4f);
            videoRect.offsetMax = new Vector2(-4f, -4f);
            var video = videoRect.gameObject.AddComponent<RawImage>();
            video.color = Color.black;
            video.raycastTarget = false;
            var scanRect = Stretch("Scanlines", videoRect);
            var scan = scanRect.gameObject.AddComponent<RawImage>();
            scan.color = new Color(0.6f, 1f, 1f, 0.16f);
            scan.raycastTarget = false;
            var vignetteRect = Stretch("Vignette", videoRect);
            var vignetteImage = AddImage(vignetteRect, null, Color.white, false);
            var cctvBorder = Stretch("Border", cctv);
            var cctvBorderImage = AddBorder(cctvBorder, new Color(0.25f, 0.55f, 0.65f, 0.7f));
            var cctvStatus = AddText(cctv, "CctvStatus", font, Vector2.zero, new Vector2(400f, 36f), 22f, MutedText, TextAlignmentOptions.Center);
            var line1 = AddText(cctv, "TerminalLine1", font, TopLeft(16f, 12f, 420f, 28f), 18f, TerminalColor, TextAlignmentOptions.MidlineLeft);
            var line2 = AddText(cctv, "TerminalLine2", font, TopLeft(16f, 40f, 420f, 28f), 18f, TerminalColor, TextAlignmentOptions.MidlineLeft);
            var destinationName = AddText(cctv, "DestinationName", font, TopLeft(16f, cctvHeight - 44f, 400f, 30f), 22f, Cyan, TextAlignmentOptions.MidlineLeft);
            destinationName.fontStyle = FontStyles.Bold;

            var rec = NewRect("Rec", cctv, Vector2.zero, new Vector2(90f, 28f));
            rec.anchorMin = rec.anchorMax = new Vector2(1f, 1f);
            rec.pivot = new Vector2(1f, 1f);
            rec.anchoredPosition = new Vector2(-16f, -12f);
            var recGroup = rec.gameObject.AddComponent<CanvasGroup>();
            recGroup.interactable = false;
            recGroup.blocksRaycasts = false;
            var recDotRect = NewRect("Dot", rec, Vector2.zero, new Vector2(14f, 14f));
            recDotRect.anchorMin = recDotRect.anchorMax = new Vector2(0f, 0.5f);
            recDotRect.pivot = new Vector2(0f, 0.5f);
            recDotRect.anchoredPosition = new Vector2(4f, 0f);
            var recDot = AddImage(recDotRect, null, new Color(1f, 0.22f, 0.2f, 1f), false);
            var recLabel = AddText(rec, "Label", font, Vector2.zero, Vector2.zero, 20f, new Color(1f, 0.55f, 0.5f, 1f), TextAlignmentOptions.MidlineLeft);
            recLabel.rectTransform.anchorMin = Vector2.zero;
            recLabel.rectTransform.anchorMax = Vector2.one;
            recLabel.rectTransform.offsetMin = new Vector2(26f, 0f);
            recLabel.rectTransform.offsetMax = Vector2.zero;
            recLabel.text = "REC";

            // ---- 비용·결과·버튼 ----
            var cost = AddText(window, "CostText", font, TopLeft(cctvLeft, bodyTop + cctvHeight + 8f, cctvWidth, 28f), 22f, TitleColor, TextAlignmentOptions.MidlineLeft);
            cost.text = "비용: 100G + 전력 10";
            var result = AddText(window, "ResultText", font, TopLeft(cctvLeft, bodyTop + cctvHeight + 36f, cctvWidth, 26f), 20f, TerminalColor, TextAlignmentOptions.MidlineLeft);
            var buttonY = bodyTop + bodyHeight - ButtonHeight;
            // 닫기는 우상단 ×와 Esc가 맡는다. 이동 버튼만 오른쪽 칸 가운데에 둔다.
            var confirm = BuildConfirmButton(window, font, cctvLeft + (cctvWidth - ConfirmButtonWidth) * 0.5f, buttonY, ConfirmButtonWidth, out var confirmHover);

            // 웜홀은 창 뒤에 깔린다.
            wormhole.SetAsFirstSibling();
            panelRoot.gameObject.SetActive(false);

            var viewObject = new SerializedObject(view);
            Set(viewObject, "panelRoot", panelRoot.gameObject);
            Set(viewObject, "windowRect", window);
            Set(viewObject, "windowGroup", windowGroup);
            Set(viewObject, "wormholeRect", wormhole);
            Set(viewObject, "wormholeGroup", wormholeGroup);
            Set(viewObject, "wormholeGlow", glowImage);
            Set(viewObject, "listContent", content);
            Set(viewObject, "listScroll", scrollRect);
            Set(viewObject, "rowTemplate", rowTemplate);
            Set(viewObject, "terminalLine1", line1);
            Set(viewObject, "terminalLine2", line2);
            Set(viewObject, "costText", cost);
            Set(viewObject, "resultText", result);
            Set(viewObject, "destinationNameText", destinationName);
            Set(viewObject, "cctvStatusText", cctvStatus);
            Set(viewObject, "video", video);
            Set(viewObject, "confirmButton", confirm);
            Set(viewObject, "confirmHoverOverlay", confirmHover.gameObject);
            Set(viewObject, "hexPattern", hexImage);
            Set(viewObject, "scanlines", scan);
            Set(viewObject, "vignette", vignetteImage);
            Set(viewObject, "recGroup", recGroup);
            Set(viewObject, "recDot", recDot);
            Set(viewObject, "wormholeCore", coreImage);
            Set(viewObject, "wormholeParticles", particleGraphic);
            // 바깥 가스는 느리게, 안쪽 원반은 빠르게 돌려 한 덩어리로 도는 선풍기 느낌을 없앤다.
            var swirls = viewObject.FindProperty("swirlLayers");
            swirls.arraySize = 2;
            SetSwirlLayer(swirls.GetArrayElementAtIndex(0), outerSwirl, outerSwirlImage, 0, 0.45f);
            SetSwirlLayer(swirls.GetArrayElementAtIndex(1), innerSwirl, innerSwirlImage, 1, 1f);
            var borders = viewObject.FindProperty("borderPanels");
            borders.arraySize = 2;
            borders.GetArrayElementAtIndex(0).objectReferenceValue = listBorderImage;
            borders.GetArrayElementAtIndex(1).objectReferenceValue = cctvBorderImage;
            viewObject.ApplyModifiedPropertiesWithoutUndo();

            var binderObject = new SerializedObject(binder);
            Set(binderObject, "view", view);
            Set(binderObject, "confirmButton", confirm);
            Set(binderObject, "closeCornerButton", cornerClose);
            binderObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button BuildCornerCloseButton(RectTransform parent, TMP_FontAsset font)
        {
            var rect = NewRect("CornerCloseButton", parent, Vector2.zero, new Vector2(36f, 36f));
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-SideMargin, -36f);
            var image = AddImage(rect, null, new Color(0.04f, 0.13f, 0.17f, 0.92f), true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var label = AddText(rect, "Label", font, Vector2.zero, Vector2.zero, 24f, Cyan, TextAlignmentOptions.Center);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.text = "×";
            // 전진기지 코어 팝업의 × 버튼과 같은 호버: 안쪽 청록 오버레이가 페이드 인한다(바깥 빛 없음).
            CoreCctvPopupBuilder.ApplyCloseButtonHover(button);
            return button;
        }

        private static Image AddBorder(RectTransform rect, Color color)
        {
            // 스프라이트는 런타임에 코어 CCTV와 같은 테두리 그림이 입혀진다(View.ApplyArt).
            var image = AddImage(rect, null, color, false);
            image.type = Image.Type.Sliced;
            image.fillCenter = false;
            return image;
        }

        private static EmergencyEscapeDestinationRowView BuildRowTemplate(RectTransform parent, TMP_FontAsset font)
        {
            var row = NewRect("RowTemplate", parent, Vector2.zero, new Vector2(0f, RowHeight));
            var background = AddImage(row, null, new Color(0.03f, 0.07f, 0.1f, 0.85f), true);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = RowHeight;
            element.minHeight = RowHeight;
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            // 방향키는 목록 선택 전용이다. EventSystem 네비게이션이 버튼을 가로채지 않게 한다.
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var bar = NewRect("SelectionBar", row, Vector2.zero, new Vector2(4f, 0f));
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(0f, 1f);
            bar.pivot = new Vector2(0f, 0.5f);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = new Vector2(4f, 0f);
            var barImage = AddImage(bar, null, new Color(0.42f, 0.94f, 1f, 0f), false);

            var label = AddText(row, "Label", font, Vector2.zero, Vector2.zero, 22f, TitleColor, TextAlignmentOptions.MidlineLeft);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(20f, 0f);
            labelRect.offsetMax = new Vector2(-200f, 0f);

            var reason = AddText(row, "Reason", font, Vector2.zero, Vector2.zero, 18f, new Color(1f, 0.55f, 0.4f, 1f), TextAlignmentOptions.MidlineRight);
            var reasonRect = reason.rectTransform;
            reasonRect.anchorMin = new Vector2(1f, 0f);
            reasonRect.anchorMax = new Vector2(1f, 1f);
            reasonRect.pivot = new Vector2(1f, 0.5f);
            reasonRect.sizeDelta = new Vector2(180f, 0f);
            reasonRect.anchoredPosition = new Vector2(-16f, 0f);

            var rowView = row.gameObject.AddComponent<EmergencyEscapeDestinationRowView>();
            var so = new SerializedObject(rowView);
            Set(so, "button", button);
            Set(so, "background", background);
            Set(so, "selectionBar", barImage);
            Set(so, "label", label);
            Set(so, "reasonLabel", reason);
            so.ApplyModifiedPropertiesWithoutUndo();
            row.gameObject.SetActive(false);
            return rowView;
        }

        // 이동 버튼: 호버하면 Codex가 만든 호버 그림이 버튼 영역 안에서만 페이드 인한다(바깥 번짐 없음).
        private static Button BuildConfirmButton(
            RectTransform parent, TMP_FontAsset font, float x, float y, float width, out RectTransform hoverOverlay)
        {
            var rect = NewTop("ConfirmButton", parent, x, y, width, ButtonHeight);
            var image = AddImage(rect, LoadSprite(ConfirmButtonPath), Color.white, true);
            image.preserveAspect = false;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            hoverOverlay = Stretch("HoverOverlay", rect);
            var overlayImage = AddImage(hoverOverlay, LoadHoverSprite(), new Color(1f, 1f, 1f, 0f), false);
            overlayImage.preserveAspect = false;
            var skin = rect.gameObject.AddComponent<MenuSpriteButtonSkin>();
            skin.SetOverlay(overlayImage);

            var label = AddText(rect, "Label", font, Vector2.zero, Vector2.zero, 24f, TitleColor, TextAlignmentOptions.Center);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.fontStyle = FontStyles.Bold;
            label.text = "이동";
            return button;
        }

        private static Sprite LoadHoverSprite()
        {
            var importer = AssetImporter.GetAtPath(ConfirmHoverPath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled
                || !importer.alphaIsTransparency))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return LoadSprite(ConfirmHoverPath);
        }

        private static void AddOutline(RectTransform parent, Color color)
        {
            AddStrip(parent, "OutlineTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -2f), new Vector2(0f, 0f), color);
            AddStrip(parent, "OutlineBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 2f), color);
            AddStrip(parent, "OutlineLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(2f, 0f), color);
            AddStrip(parent, "OutlineRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-2f, 0f), new Vector2(0f, 0f), color);
        }

        private static void AddStrip(
            RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var rect = NewRect(name, parent, Vector2.zero, Vector2.zero);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            AddImage(rect, null, color, false);
        }

        private static Vector4 TopLeft(float x, float y, float width, float height)
        {
            return new Vector4(x, y, width, height);
        }

        private static TextMeshProUGUI AddText(
            RectTransform parent, string name, TMP_FontAsset font, Vector4 topLeft,
            float fontSize, Color color, TextAlignmentOptions alignment)
        {
            var rect = NewTop(name, parent, topLeft.x, topLeft.y, topLeft.z, topLeft.w);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(text, font, fontSize, color, alignment);
            return text;
        }

        private static TextMeshProUGUI AddText(
            RectTransform parent, string name, TMP_FontAsset font, Vector2 position, Vector2 size,
            float fontSize, Color color, TextAlignmentOptions alignment)
        {
            var rect = NewRect(name, parent, position, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(text, font, fontSize, color, alignment);
            return text;
        }

        private static void ConfigureText(
            TextMeshProUGUI text, TMP_FontAsset font, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
        }

        private static Image AddImage(RectTransform rect, Sprite sprite, Color color, bool raycast)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform NewTop(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = NewRect(name, parent, Vector2.zero, new Vector2(width, height));
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            return rect;
        }

        private static RectTransform Stretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException("스프라이트를 찾을 수 없습니다: " + path);
            }

            return sprite;
        }

        private static void SetSwirlLayer(SerializedProperty layer, RectTransform rect, Image image, int art, float speed)
        {
            layer.FindPropertyRelative("rect").objectReferenceValue = rect;
            layer.FindPropertyRelative("image").objectReferenceValue = image;
            layer.FindPropertyRelative("art").intValue = art;
            layer.FindPropertyRelative("speed").floatValue = speed;
        }

        private static void Set(SerializedObject target, string field, UnityEngine.Object value)
        {
            var property = target.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException("직렬화 필드가 없습니다: " + field);
            }

            property.objectReferenceValue = value;
        }
    }
}
#endif
