using SubTerra.App.Core.Data;
using SubTerra.App.UI;
using SubTerra.App.UI.Outpost;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// 전진기지 코어 CCTV 팝업(CorePopup)을 OutpostPanel 프리팹에만 추가한다.
    /// 충전기·보건소 서비스 팝업과 정산 콘솔·보관함이 쓰는 기존 계층은 건드리지 않는다.
    /// 프레임·패널은 다른 개선된 팝업과 같은 그림, 스캔라인·노이즈·육각형 무늬는 런타임에 만든다.
    /// </summary>
    public static class CoreCctvPopupBuilder
    {
        public const string OutpostPrefabPath = "Assets/_Project/Prefabs/UI/OutpostPanel.prefab";
        public const string PopupName = "CorePopup";

        private const string MineResetArt = "Assets/_Project/Art/UI/SurfaceBase/MineReset/";
        private const string FramePath = MineResetArt + "mine-reset-frame.png";
        private const string PanelPath = MineResetArt + "mine-reset-panel.png";
        private const string FontPath = "Assets/_Project/Fonts/NotoSansKR-Regular_SDF.asset";
        private const string BuildingDataPath = "Assets/_Project/Data/Buildings/";

        public static readonly Vector2 WindowSize = new Vector2(1000f, 600f);
        public const float SideMargin = 56f;
        public const float BodyTop = 104f;
        public const float BodyHeight = 444f;
        public const float ListWidth = 296f;
        public const float BodyGap = 16f;
        public const float ListHeaderHeight = 46f;
        public const int BarCount = 6;

        private static readonly Color TitleColor = new Color(0.84f, 0.98f, 1f, 1f);
        private static readonly Color Cyan = new Color(0.42f, 0.94f, 1f, 1f);
        private static readonly Color TerminalColor = new Color(0.45f, 1f, 0.82f, 1f);
        private static readonly Color MutedText = new Color(0.55f, 0.67f, 0.74f, 1f);
        private static readonly string[] FacilityBuildingAssets =
        {
            "Building_Charger_Basic.asset",
            "Building_Clinic_Basic.asset",
            "Building_Settlement_Basic.asset"
        };

        [MenuItem("SubTerra/UI/Build Core CCTV Popup (OutpostPanel only)")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            var root = PrefabUtility.LoadPrefabContents(OutpostPrefabPath);
            try
            {
                ApplyTo(root);
                PrefabUtility.SaveAsPrefabAsset(root, OutpostPrefabPath);
                AssetDatabase.SaveAssets();
                return "Core CCTV popup built: " + OutpostPrefabPath;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>OutpostPanel 루트에 CorePopup을 (다시) 만들고 View에 연결한다.</summary>
        public static void ApplyTo(GameObject outpostRoot)
        {
            if (outpostRoot == null)
            {
                throw new System.InvalidOperationException("OutpostPanel 루트가 없습니다.");
            }

            var outpostView = outpostRoot.GetComponent<OutpostPanelView>();
            if (outpostView == null)
            {
                throw new System.InvalidOperationException("OutpostPanelView를 찾을 수 없습니다.");
            }

            var existing = outpostRoot.transform.Find(PopupName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var popup = BuildPopup(outpostRoot.transform, font);
            popup.SetActive(false);

            var viewObject = new SerializedObject(outpostView);
            viewObject.FindProperty("corePopup").objectReferenceValue =
                popup.GetComponent<CoreCctvPopupView>();
            viewObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildPopup(Transform parent, TMP_FontAsset font)
        {
            var window = NewRect(PopupName, parent, Vector2.zero, WindowSize);
            var windowGroup = window.gameObject.AddComponent<CanvasGroup>();
            window.gameObject.AddComponent<PopupWindowDrag>();
            var view = window.gameObject.AddComponent<CoreCctvPopupView>();

            // 연출 중에도 뒤쪽 게임 화면으로 클릭이 새지 않도록 창 전체를 덮는 투명 차단막.
            var shield = Stretch("InputShield", window);
            AddImage(shield, null, new Color(0f, 0f, 0f, 0f), true);

            var panelGlow = NewRect("PanelGlow", window, Vector2.zero, new Vector2(1120f, 720f));
            var panelGlowImage = AddImage(panelGlow, null, new Color(0.3f, 0.9f, 1f, 0f), false);

            // 프레임 계층: 이 계층만 가로선 → 세로로 펼친다. 글자·아이콘은 여기에 없다.
            var backdrop = Stretch("PanelBackdrop", window);
            var backdropGroup = backdrop.gameObject.AddComponent<CanvasGroup>();
            var panel = Stretch("Panel", backdrop);
            panel.offsetMin = new Vector2(16f, 16f);
            panel.offsetMax = new Vector2(-16f, -16f);
            AddImage(panel, LoadSprite(PanelPath), Color.white, false);
            var hex = Stretch("HexPattern", backdrop);
            hex.offsetMin = new Vector2(20f, 20f);
            hex.offsetMax = new Vector2(-20f, -20f);
            var hexImage = AddRaw(hex, new Color(0.35f, 0.9f, 1f, 0.045f));
            var frame = Stretch("Frame", backdrop);
            AddImage(frame, LoadSprite(FramePath), Color.white, false);
            var edgeNoise = new[]
            {
                BuildEdgeStrip(backdrop, "EdgeNoiseTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -26f), new Vector2(-18f, -16f)),
                BuildEdgeStrip(backdrop, "EdgeNoiseBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 16f), new Vector2(-18f, 26f)),
                BuildEdgeStrip(backdrop, "EdgeNoiseLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(16f, 26f), new Vector2(26f, -26f)),
                BuildEdgeStrip(backdrop, "EdgeNoiseRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-26f, 26f), new Vector2(-16f, -26f))
            };

            var flash = NewRect("OpenFlash", window, Vector2.zero, new Vector2(900f, 330f));
            var flashImage = AddImage(flash, null, new Color(0.42f, 0.94f, 1f, 0f), false);

            // 내부 요소: 크기가 고정된 Inner를 높이만 변하는 Clip으로 잘라 보여 준다.
            var clip = NewRect("ContentClip", window, Vector2.zero, new Vector2(0f, 0f));
            clip.gameObject.AddComponent<RectMask2D>();
            var inner = NewRect("ContentInner", clip, Vector2.zero, WindowSize);

            var titleBar = Stretch("TitleBar", inner);
            var titleGroup = titleBar.gameObject.AddComponent<CanvasGroup>();
            var title = AddText(titleBar, "Title", font, TopLeft(SideMargin, 30f, 480f, 52f), 36f, TitleColor, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.text = CoreCctvPopupView.TitleLabel;
            var divider = NewTop("Divider", titleBar, SideMargin, 88f, WindowSize.x - SideMargin * 2f, 2f);
            AddImage(divider, null, new Color(0.3f, 0.9f, 1f, 0.35f), false);
            var close = BuildCloseButton(titleBar, font);

            var body = Stretch("Body", inner);
            var bodyGroup = body.gameObject.AddComponent<CanvasGroup>();

            // ---- 왼쪽: 연결된 시설 ----
            var listPanel = NewTop("ListPanel", body, SideMargin, BodyTop, ListWidth, BodyHeight);
            var listGroup = listPanel.gameObject.AddComponent<CanvasGroup>();
            AddImage(listPanel, null, new Color(0.02f, 0.055f, 0.085f, 0.78f), false);
            var listBorder = Stretch("Border", listPanel);
            var listBorderImage = AddBorder(listBorder, new Color(0.25f, 0.55f, 0.65f, 0.55f));
            var header = AddText(listPanel, "Header", font, TopLeft(18f, 8f, ListWidth - 36f, ListHeaderHeight - 8f), 22f, Cyan, TextAlignmentOptions.MidlineLeft);
            header.text = "연결된 시설";
            header.fontStyle = FontStyles.Bold;
            var headerLine = NewTop("HeaderLine", listPanel, 14f, ListHeaderHeight, ListWidth - 28f, 1f);
            AddImage(headerLine, null, new Color(0.3f, 0.9f, 1f, 0.28f), false);

            const float viewportTop = ListHeaderHeight + 6f;
            const float scrollbarWidth = 6f;
            var viewportSize = new Vector2(ListWidth - 20f - scrollbarWidth, BodyHeight - viewportTop - 8f);
            var scroll = NewTop("ListScroll", listPanel, 10f, viewportTop, viewportSize.x, viewportSize.y);
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            var viewport = Stretch("Viewport", scroll);
            viewport.gameObject.AddComponent<RectMask2D>();
            AddImage(viewport, null, new Color(0f, 0f, 0f, 0f), true);
            var content = NewRect("Content", viewport, Vector2.zero, new Vector2(0f, 0f));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var template = BuildItemTemplate(content, font);
            var emptyLabel = AddText(scroll, "EmptyLabel", font, TopLeft(0f, 0f, viewportSize.x, viewportSize.y), 22f, MutedText, TextAlignmentOptions.Center);
            emptyLabel.text = CoreCctvPopupView.EmptyListLabel;
            emptyLabel.alpha = 0f;

            var scrollbar = BuildScrollbar(listPanel, viewportTop, viewportSize.y, scrollbarWidth);
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false;
            scrollRect.scrollSensitivity = 36f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollRect.verticalScrollbarSpacing = 0f;

            // ---- 오른쪽: CCTV ----
            var cctvLeft = SideMargin + ListWidth + BodyGap;
            var cctvWidth = WindowSize.x - SideMargin - cctvLeft;
            var cctv = NewTop("CctvArea", body, cctvLeft, BodyTop, cctvWidth, BodyHeight);
            var screenGroup = cctv.gameObject.AddComponent<CanvasGroup>();
            AddImage(cctv, null, new Color(0.01f, 0.025f, 0.04f, 1f), false);
            var screen = Stretch("Screen", cctv);
            screen.offsetMin = new Vector2(4f, 4f);
            screen.offsetMax = new Vector2(-4f, -4f);
            screen.gameObject.AddComponent<RectMask2D>();
            AddImage(screen, null, Color.black, false);

            var videoRect = Stretch("Video", screen);
            var video = AddRaw(videoRect, new Color(0f, 0f, 0f, 0f));
            var ghostFarRect = Stretch("GhostFar", screen);
            var ghostFar = AddRaw(ghostFarRect, new Color(1f, 1f, 1f, 0f));
            var ghostNearRect = Stretch("GhostNear", screen);
            var ghostNear = AddRaw(ghostNearRect, new Color(1f, 1f, 1f, 0f));
            var scanRect = Stretch("Scanlines", screen);
            var scanlines = AddRaw(scanRect, new Color(0f, 0f, 0f, 0f));
            var noiseRect = Stretch("Noise", screen);
            var noise = AddRaw(noiseRect, new Color(1f, 1f, 1f, 0f));
            noise.uvRect = new Rect(0f, 0f, 3f, 2f);
            var vignetteRect = Stretch("Vignette", screen);
            var vignetteImage = AddImage(vignetteRect, null, new Color(1f, 1f, 1f, 0f), false);

            var barsRoot = Stretch("Bars", screen);
            var bars = new Image[BarCount];
            for (var i = 0; i < BarCount; i++)
            {
                var bar = NewRect("Bar" + i, barsRoot, Vector2.zero, new Vector2(0f, 4f));
                bars[i] = AddImage(bar, null, new Color(0.82f, 1f, 1f, 0f), false);
            }

            var terminal = Stretch("Terminal", screen);
            var terminalGroup = terminal.gameObject.AddComponent<CanvasGroup>();
            terminalGroup.alpha = 0f;
            var lines = new TMP_Text[CoreCctvTimeline.TerminalLineCount];
            for (var i = 0; i < lines.Length; i++)
            {
                lines[i] = AddText(terminal, "Line" + i, font, TopLeft(28f, 26f + i * 36f, cctvWidth - 80f, 32f), 24f, TerminalColor, TextAlignmentOptions.MidlineLeft);
            }

            var connected = AddText(screen, "ConnectedText", font, Vector2.zero, new Vector2(cctvWidth - 40f, 60f), 42f, Cyan, TextAlignmentOptions.Center);
            connected.fontStyle = FontStyles.Bold;
            connected.text = CoreCctvPopupView.ConnectedLabel;
            connected.alpha = 0f;

            var rec = NewTop("Rec", screen, 20f, 16f, 90f, 28f);
            var recGroup = rec.gameObject.AddComponent<CanvasGroup>();
            recGroup.alpha = 0f;
            var dot = NewTop("Dot", rec, 0f, 7f, 14f, 14f);
            var recDot = AddImage(dot, null, new Color(1f, 0.22f, 0.2f, 1f), false);
            var recLabel = AddText(rec, "Label", font, TopLeft(22f, 0f, 68f, 28f), 20f, new Color(1f, 0.3f, 0.28f, 1f), TextAlignmentOptions.MidlineLeft);
            recLabel.text = "REC";
            recLabel.fontStyle = FontStyles.Bold;

            var cctvBorder = Stretch("Border", cctv);
            var cctvBorderImage = AddBorder(cctvBorder, new Color(0.3f, 0.75f, 0.9f, 0.85f));

            var openLine = NewRect("OpenLine", window, Vector2.zero, new Vector2(0f, 3f));
            var openLineImage = AddImage(openLine, null, new Color(0.42f, 0.94f, 1f, 0f), false);

            var viewObject = new SerializedObject(view);
            Set(viewObject, "windowGroup", windowGroup);
            Set(viewObject, "panelBackdrop", backdrop);
            Set(viewObject, "panelBackdropGroup", backdropGroup);
            Set(viewObject, "panelGlow", panelGlowImage);
            Set(viewObject, "contentClip", clip);
            Set(viewObject, "titleGroup", titleGroup);
            Set(viewObject, "bodyGroup", bodyGroup);
            Set(viewObject, "openLine", openLineImage);
            Set(viewObject, "flashGlow", flashImage);
            Set(viewObject, "hexPattern", hexImage);
            SetArray(viewObject, "edgeNoise", edgeNoise);
            Set(viewObject, "closeButton", close);
            Set(viewObject, "listGroup", listGroup);
            Set(viewObject, "listScroll", scrollRect);
            Set(viewObject, "listViewport", viewport);
            Set(viewObject, "listContent", content);
            Set(viewObject, "listScrollbar", scrollbar);
            Set(viewObject, "itemTemplate", template);
            Set(viewObject, "emptyLabel", emptyLabel);
            SetArray(viewObject, "borderPanels", new Object[] { listBorderImage, cctvBorderImage });
            Set(viewObject, "screenGroup", screenGroup);
            Set(viewObject, "video", video);
            Set(viewObject, "ghostNear", ghostNear);
            Set(viewObject, "ghostFar", ghostFar);
            Set(viewObject, "scanlines", scanlines);
            Set(viewObject, "noise", noise);
            Set(viewObject, "vignette", vignetteImage);
            SetArray(viewObject, "bars", bars);
            Set(viewObject, "terminalGroup", terminalGroup);
            SetArray(viewObject, "terminalLines", lines);
            Set(viewObject, "connectedText", connected);
            Set(viewObject, "recGroup", recGroup);
            Set(viewObject, "recDot", recDot);
            SetFallbackIcons(viewObject);
            viewObject.ApplyModifiedPropertiesWithoutUndo();
            return window.gameObject;
        }

        private static CoreCctvFacilityItem BuildItemTemplate(RectTransform parent, TMP_FontAsset font)
        {
            var item = NewRect("ItemTemplate", parent, Vector2.zero, Vector2.zero);
            item.anchorMin = new Vector2(0f, 1f);
            item.anchorMax = new Vector2(1f, 1f);
            item.pivot = new Vector2(0.5f, 1f);
            item.anchoredPosition = Vector2.zero;
            item.sizeDelta = new Vector2(0f, CoreCctvListLayout.ItemHeight);
            var group = item.gameObject.AddComponent<CanvasGroup>();

            var glow = NewRect("Glow", item, Vector2.zero, Vector2.zero);
            glow.anchorMin = Vector2.zero;
            glow.anchorMax = Vector2.one;
            glow.offsetMin = new Vector2(-10f, -10f);
            glow.offsetMax = new Vector2(10f, 10f);
            var glowImage = AddImage(glow, null, new Color(0.3f, 0.9f, 1f, 0f), false);
            glowImage.type = Image.Type.Sliced;

            var background = Stretch("Background", item);
            var backgroundImage = AddImage(background, null, new Color(0.035f, 0.075f, 0.115f, 0.86f), true);
            var border = Stretch("Border", item);
            var borderImage = AddBorder(border, new Color(0.25f, 0.46f, 0.56f, 0.38f));

            var iconRect = NewRect("Icon", item, Vector2.zero, new Vector2(52f, 52f));
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(16f, 0f);
            var icon = AddImage(iconRect, null, Color.white, false);
            icon.preserveAspect = true;

            var nameRect = NewRect("Name", item, Vector2.zero, Vector2.zero);
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(82f, 0f);
            nameRect.offsetMax = new Vector2(-12f, 0f);
            var name = nameRect.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(name, font, 26f, new Color(0.62f, 0.73f, 0.8f, 1f), TextAlignmentOptions.MidlineLeft);
            name.overflowMode = TextOverflowModes.Ellipsis;

            var button = item.gameObject.AddComponent<Button>();
            button.targetGraphic = backgroundImage;
            button.transition = Selectable.Transition.None;

            var component = item.gameObject.AddComponent<CoreCctvFacilityItem>();
            var serialized = new SerializedObject(component);
            Set(serialized, "rect", item);
            Set(serialized, "group", group);
            Set(serialized, "button", button);
            Set(serialized, "background", backgroundImage);
            Set(serialized, "border", borderImage);
            Set(serialized, "glow", glowImage);
            Set(serialized, "icon", icon);
            Set(serialized, "nameText", name);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            item.gameObject.SetActive(false);
            return component;
        }

        private static Scrollbar BuildScrollbar(RectTransform parent, float top, float height, float width)
        {
            var bar = NewTop("ListScrollbar", parent, 0f, top, width, height);
            bar.anchorMin = bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(1f, 1f);
            bar.anchoredPosition = new Vector2(-8f, -top);
            AddImage(bar, null, new Color(0.3f, 0.8f, 0.9f, 0.10f), false);
            var area = Stretch("SlidingArea", bar);
            var handle = Stretch("Handle", area);
            var handleImage = AddImage(handle, null, new Color(0.42f, 0.94f, 1f, 0.75f), true);
            var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.transition = Selectable.Transition.None;
            var navigation = scrollbar.navigation;
            navigation.mode = Navigation.Mode.None;
            scrollbar.navigation = navigation;
            return scrollbar;
        }

        private static Button BuildCloseButton(RectTransform parent, TMP_FontAsset font)
        {
            var rect = NewRect("CloseButton", parent, Vector2.zero, new Vector2(36f, 36f));
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-SideMargin, -36f);
            var image = AddImage(rect, null, new Color(0.04f, 0.13f, 0.17f, 0.92f), true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = AddText(rect, "Label", font, Vector2.zero, Vector2.zero, 24f, Cyan, TextAlignmentOptions.Center);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.text = "×";
            ApplyCloseButtonHover(button);
            return button;
        }

        public static void ApplyCloseButtonHover(Button button)
        {
            var existing = button.transform.Find("HoverOverlay");
            var rect = existing != null
                ? (RectTransform)existing
                : NewRect("HoverOverlay", button.transform, Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();
            var image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = AddImage(rect, null, Cyan, false);
            }
            image.raycastTarget = false;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.85f, 0.95f, 1f, 0.24f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            colors.fadeDuration = 0.15f;
            button.colors = colors;
        }

        private static RawImage BuildEdgeStrip(
            RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = NewRect(name, parent, Vector2.zero, Vector2.zero);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return AddRaw(rect, new Color(0.45f, 0.95f, 1f, 0f));
        }

        private static void SetFallbackIcons(SerializedObject view)
        {
            var property = view.FindProperty("fallbackIcons");
            property.arraySize = FacilityBuildingAssets.Length;
            for (var i = 0; i < FacilityBuildingAssets.Length; i++)
            {
                var data = AssetDatabase.LoadAssetAtPath<BuildingData>(BuildingDataPath + FacilityBuildingAssets[i]);
                if (data == null)
                {
                    throw new System.InvalidOperationException("시설 데이터를 찾을 수 없습니다: " + FacilityBuildingAssets[i]);
                }

                var element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("buildingId").stringValue = data.Id;
                element.FindPropertyRelative("sprite").objectReferenceValue = data.Icon;
            }
        }

        private static Vector4 TopLeft(float x, float y, float width, float height)
        {
            return new Vector4(x, y, width, height);
        }

        private static TextMeshProUGUI AddText(
            RectTransform parent,
            string name,
            TMP_FontAsset font,
            Vector4 topLeft,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            var rect = NewTop(name, parent, topLeft.x, topLeft.y, topLeft.z, topLeft.w);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(text, font, fontSize, color, alignment);
            return text;
        }

        private static TextMeshProUGUI AddText(
            RectTransform parent,
            string name,
            TMP_FontAsset font,
            Vector2 position,
            Vector2 size,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment)
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

        private static Image AddBorder(RectTransform rect, Color color)
        {
            var image = AddImage(rect, null, color, false);
            image.type = Image.Type.Sliced;
            image.fillCenter = false;
            return image;
        }

        private static Image AddImage(RectTransform rect, Sprite sprite, Color color, bool raycast)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static RawImage AddRaw(RectTransform rect, Color color)
        {
            var raw = rect.gameObject.AddComponent<RawImage>();
            raw.color = color;
            raw.raycastTarget = false;
            return raw;
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

        /// <summary>왼쪽 위 기준 배치. 부모의 왼쪽 위에서 x만큼 오른쪽, y만큼 아래.</summary>
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
                throw new System.InvalidOperationException("스프라이트를 찾을 수 없습니다: " + path);
            }

            return sprite;
        }

        private static void Set(SerializedObject target, string field, Object value)
        {
            var property = target.FindProperty(field);
            if (property == null)
            {
                throw new System.InvalidOperationException("직렬화 필드가 없습니다: " + field);
            }

            property.objectReferenceValue = value;
        }

        private static void SetArray(SerializedObject target, string field, Object[] values)
        {
            var property = target.FindProperty(field);
            if (property == null)
            {
                throw new System.InvalidOperationException("직렬화 필드가 없습니다: " + field);
            }

            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
