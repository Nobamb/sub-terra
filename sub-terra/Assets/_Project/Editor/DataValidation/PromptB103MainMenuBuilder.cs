using SubTerra.App.UI.MainMenu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>103-1번 메인 메뉴 비주얼 개선 빌더. 설정/덮어쓰기 팝업과 기존 배선은 완벽 유지한다.</summary>
    public static class PromptB103MainMenuBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/UI/MainMenuPanel.prefab";
        public const string BackgroundPath = "Assets/_Project/Art/UI/MainMenu/MainMenu_Background.png";
        public const string TitlePath = "Assets/_Project/Art/UI/MainMenu/MainMenu_Title.png";
        public const string TitleGlowPath = "Assets/_Project/Art/UI/MainMenu/MainMenu_Title_Glow.png";
        public const string DividerPath = "Assets/_Project/Art/UI/MainMenu/MainMenu_DividerLine.png";
        public const string CutCornerPlatePath = "Assets/_Project/Art/UI/MainMenu/UI_CutCorner_Plate.png";
        public const string TriangleArrowPath = "Assets/_Project/Art/UI/MainMenu/UI_Triangle_Arrow.png";
        public const string SlotInnerGlowPath = "Assets/_Project/Art/UI/MainMenu/UI_Slot_InnerGlow.png";

        [MenuItem("SubTerra/UI/Build Prompt-B 103 Main Menu")]
        public static void Build()
        {
            Debug.Log("[PromptB103MainMenuBuilder] Starting Build...");
            SetupTextureImporters();

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var content = (RectTransform)root.transform.Find("MenuContent");
                Place(content, 0, 0, 1000, 1000);
                var view = new SerializedObject(root.GetComponent<MainMenuView>());
                view.FindProperty("menuContent").objectReferenceValue = content;
                view.ApplyModifiedPropertiesWithoutUndo();

                var plate = content.GetComponent<Image>();
                if (plate != null)
                {
                    plate.color = Color.clear;
                    plate.raycastTarget = false;
                }
                var rootImage = root.GetComponent<Image>();
                if (rootImage != null) rootImage.color = Hex("061117");

                // 1. 기존 텍스트 삭제 ("지금은 40미터다. 봉인 너머에서 신호가 온다.")
                var depthCopy = content.Find("DepthCopy");
                if (depthCopy != null) Object.DestroyImmediate(depthCopy.gameObject);
                var signalCopy = content.Find("SignalCopy");
                if (signalCopy != null) Object.DestroyImmediate(signalCopy.gameObject);

                // 2. Sub-Terra 타이틀: 텍스트 -> 메탈릭 스텐실 로고 이미지 + 청록백색 글로우 + 부상 파티클
                var titleTransform = content.Find("Title");
                var titleRect = titleTransform as RectTransform;
                if (titleRect == null)
                {
                    titleRect = Rect(content, "Title", 0, 360, 592, 120);
                }
                else
                {
                    Place(titleRect, 0, 360, 592, 120);
                    var oldTmp = titleRect.GetComponent<TMP_Text>();
                    if (oldTmp != null) Object.DestroyImmediate(oldTmp);
                }

                // 타이틀 청록 그림자/글로우 (로고 정중앙 위치, 592x120 로고 전체를 조금 감싸는 648x150 크기)
                var shadowRect = Rect(titleRect, "TitleShadow", 0, 0, 648, 150);
                var shadowRaw = Get<RawImage>(shadowRect);
                shadowRaw.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TitleGlowPath);
                shadowRaw.color = Hex("19D2E6", 0.40f);
                shadowRaw.raycastTarget = false;
                Get<MenuTitleAura>(shadowRect);

                // 타이틀 글로우 아우라 (로고 뒤)
                var glowRect = Rect(titleRect, "TitleGlow", 0, 0, 660, 140);
                var glowRaw = Get<RawImage>(glowRect);
                glowRaw.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TitleGlowPath);
                glowRaw.color = Hex("A0F0FF", 0.45f);
                glowRaw.raycastTarget = false;
                Get<MenuTitleAura>(glowRect);

                // 타이틀 글로우에 부착된 청록색 그림자 효과
                var glowShadow = Get<Shadow>(glowRect);
                glowShadow.effectColor = Hex("1BE7FF", 0.55f);
                glowShadow.effectDistance = Vector2.zero;
                glowShadow.useGraphicAlpha = true;

                // 타이틀 메인 로고 이미지
                var logoRect = Rect(titleRect, "TitleLogo", 0, 0, 592, 120);
                var logoRaw = Get<RawImage>(logoRect);
                logoRaw.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TitlePath);
                logoRaw.color = Color.white;
                logoRaw.raycastTarget = false;

                // 계층 순서: TitleShadow(맨 아래) -> TitleGlow -> TitleLogo
                shadowRect.SetSiblingIndex(0);
                glowRect.SetSiblingIndex(1);
                logoRect.SetSiblingIndex(2);

                // 타이틀 파티클 (글자 주변에서 위로 피어오르는 청록색 파티클)
                Get<MenuTitleParticles>(titleRect);

                // 3. Sub-Terra와 세이브 슬롯 사이 청록빛 실선 디바이더
                var dividerRect = Rect(content, "TitleDivider", 0, 248, 760, 16);
                var dividerRaw = Get<RawImage>(dividerRect);
                dividerRaw.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(DividerPath);
                dividerRaw.color = Color.white;
                dividerRaw.raycastTarget = false;

                // 4. 세이브 슬롯 3개 구성
                var cutCornerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CutCornerPlatePath);
                var triangleTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TriangleArrowPath);
                var innerGlowTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SlotInnerGlowPath);
                var defaultFont = content.Find("MessageText")?.GetComponent<TMP_Text>()?.font;

                for (var i = 0; i < 3; i++)
                {
                    var slot = (RectTransform)content.Find("Slot" + (i + 1));
                    Place(slot, 0, 134 - i * 125, 800, 106);

                    // 슬롯 버튼 스타일: 2px 모서리 컷 9-slice 스프라이트 + 50% 반투명도
                    StyleSlotButton(slot, cutCornerSprite);

                    // 텍스트 & 썸네일: 100% 불투명하게 선명한 가독성 유지
                    var label = slot.Find("Label").GetComponent<TMP_Text>();
                    Place(label.rectTransform, 93, 0, 520, 92);
                    label.fontSize = 23;
                    label.alignment = TextAlignmentOptions.MidlineLeft;
                    label.color = Hex("EEF4F5", 1.0f);
                    label.text = "탐사 기록 " + (i + 1).ToString("00") + "\n<size=75%>— 기록 없음 —</size>";

                    var thumbnail = Rect(slot, "Thumbnail", -266, 0, 160, 90);
                    var raw = Get<RawImage>(thumbnail);
                    raw.color = Hex("102630", 1.0f);
                    raw.raycastTarget = false;

                    var placeholder = Text(thumbnail, "Placeholder", "NO VISUAL\nRECORD", 0, 0, 154, 80, 15, defaultFont);
                    placeholder.color = Hex("91A7AE", 1.0f);

                    // 슬롯 선택 레이어
                    var selection = Rect(slot, "Selection", 0, 0, 800, 106);
                    var group = Get<CanvasGroup>(selection);
                    group.alpha = i == 0 ? 1 : 0;
                    group.blocksRaycasts = false;

                    // 선택 시 내부 청록빛 확산 글로우
                    var innerGlowRect = Rect(selection, "InnerGlow", 0, 0, 800, 106);
                    innerGlowRect.SetAsFirstSibling();
                    var innerGlowRaw = Get<RawImage>(innerGlowRect);
                    innerGlowRaw.texture = innerGlowTex;
                    innerGlowRaw.color = Hex("39D8EA", 0.28f);
                    innerGlowRaw.raycastTarget = false;

                    // 기본 1px 테두리
                    Border(selection, Hex("39D8EA"), 1);

                    // 프롬프트 103-2: 모서리 끝을 감싸는 외곽선 두께 3px
                    AddCornerBrackets(selection, Hex("39D8EA"), 3f, 20f, 400f, 53f);

                    // 왼쪽 선택 화살표: 텍스트 기호가 아닌 정삼각형 도형
                    var oldSelector = selection.Find("Selector");
                    if (oldSelector != null) Object.DestroyImmediate(oldSelector.gameObject);

                    var arrowRect = Rect(selection, "TriangleSelector", -370, 0, 16, 18);
                    var arrowRaw = Get<RawImage>(arrowRect);
                    arrowRaw.texture = triangleTex;
                    arrowRaw.color = Hex("39D8EA");
                    arrowRaw.raycastTarget = false;

                    // SaveSlotCardView 바인딩
                    var card = Get<SaveSlotCardView>(slot);
                    var so = new SerializedObject(card);
                    so.FindProperty("thumbnail").objectReferenceValue = raw;
                    so.FindProperty("placeholder").objectReferenceValue = placeholder.gameObject;
                    so.FindProperty("selection").objectReferenceValue = group;
                    so.FindProperty("innerGlow").objectReferenceValue = innerGlowRaw;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // 5. 메뉴 하단 조작 버튼 4개
                var names = new[] { "ContinueButton", "NewGameButton", "SettingsButton", "QuitButton" };
                for (var i = 0; i < names.Length; i++)
                {
                    var button = (RectTransform)content.Find(names[i]);
                    Place(button, i == 3 ? 0 : (i - 1) * 274, i == 3 ? -318 : -224, i == 3 ? 256 : 252, i == 3 ? 58 : 70);
                    StyleActionButton(button, i == 0, cutCornerSprite);
                    var label = button.Find("Label").GetComponent<TMP_Text>();
                    label.fontSize = i == 3 ? 24 : 28;
                    label.color = Hex("EEF4F5");
                }

                // 메시지 및 버전 정보 텍스트
                Place(content.Find("MessageText") as RectTransform, 0, -399, 900, 56);
                var message = content.Find("MessageText").GetComponent<TMP_Text>();
                message.fontSize = 22;
                message.color = Hex("C7DFE3");

                var version = content.Find("VersionText").GetComponent<TMP_Text>();
                Place(version.rectTransform, 0, -462, 850, 32);
                version.fontSize = 18;
                version.color = new Color(0.57f, 0.65f, 0.68f, 0.7f);

                // 메인 배경화면 설정
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
                if (texture != null)
                {
                    var background = Rect(root.transform, "Background", 0, 0, 1920, 1080);
                    background.SetAsFirstSibling();
                    var raw = Get<RawImage>(background);
                    raw.texture = texture;
                    raw.raycastTarget = false;
                    raw.color = Color.white;
                    var fit = Get<AspectRatioFitter>(background);
                    fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    fit.aspectRatio = (float)texture.width / texture.height;
                    Get<MenuSignalPulse>(background);
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[PromptB103MainMenuBuilder] Build Completed Successfully!");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupTextureImporters()
        {
            SetAsSprite(CutCornerPlatePath, new Vector4(6, 6, 6, 6));
            SetAsDefaultTexture(TitlePath);
            SetAsDefaultTexture(TitleGlowPath);
            SetAsDefaultTexture(DividerPath);
            SetAsDefaultTexture(TriangleArrowPath);
            SetAsDefaultTexture(SlotInnerGlowPath);
            AssetDatabase.Refresh();
        }

        private static void SetAsSprite(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != border)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteBorder = border;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static void SetAsDefaultTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            if (importer.alphaIsTransparency != true)
            {
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static void StyleSlotButton(RectTransform rect, Sprite cutCornerSprite)
        {
            var image = rect.GetComponent<Image>();
            image.sprite = cutCornerSprite;
            image.type = cutCornerSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Hex("102630", 0.50f);
            image.raycastTarget = true;

            var button = rect.GetComponent<Button>();
            button.targetGraphic = image;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            var colors = button.colors;
            colors.normalColor = Hex("102630", 0.50f);
            colors.highlightedColor = Hex("1C4252", 0.65f);
            colors.selectedColor = Hex("102630", 0.55f);
            colors.pressedColor = Hex("0A1C24", 0.70f);
            colors.disabledColor = Hex("102028", 0.35f);
            colors.fadeDuration = 0.15f;
            colors.colorMultiplier = 1;
            button.colors = colors;

            Border(rect, Hex("294D58", 0.65f), 1);
        }

        private static void StyleActionButton(RectTransform rect, bool primary, Sprite cutCornerSprite)
        {
            var image = rect.GetComponent<Image>();
            image.sprite = cutCornerSprite;
            image.type = cutCornerSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = primary ? Hex("194652", 0.75f) : Hex("102630", 0.60f);
            image.raycastTarget = true;

            var button = rect.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = primary ? Hex("194652", 0.75f) : Hex("102630", 0.60f);
            colors.highlightedColor = Hex("245464", 0.88f);
            colors.selectedColor = Hex("20434F", 0.80f);
            colors.pressedColor = Hex("0A2029", 0.85f);
            colors.disabledColor = Hex("102028", 0.40f);
            colors.fadeDuration = 0.15f;
            colors.colorMultiplier = 1;
            button.colors = colors;

            Border(rect, primary ? Hex("59BCCA", 0.90f) : Hex("39636F", 0.75f), 1);
        }

        private static void Border(RectTransform parent, Color color, float width)
        {
            for (var i = 0; i < 4; i++)
            {
                var edge = Rect(parent, "Edge" + i, 0, 0, 0, 0);
                edge.anchorMin = i == 0 ? new Vector2(0, 1) : i == 3 ? new Vector2(1, 0) : Vector2.zero;
                edge.anchorMax = i == 1 ? new Vector2(1, 0) : i == 2 ? new Vector2(0, 1) : Vector2.one;
                edge.sizeDelta = i < 2 ? new Vector2(0, width) : new Vector2(width, 0);
                var image = Get<Image>(edge);
                image.color = color;
                image.raycastTarget = false;
            }
        }

        private static void AddCornerBrackets(RectTransform parent, Color color, float width, float armLength, float halfW, float halfH)
        {
            var container = Rect(parent, "CornerBrackets", 0, 0, halfW * 2, halfH * 2);
            // TL
            BracketLine(container, "TL_H", -halfW + armLength * 0.5f, halfH - width * 0.5f, armLength, width, color);
            BracketLine(container, "TL_V", -halfW + width * 0.5f, halfH - armLength * 0.5f, width, armLength, color);
            // TR
            BracketLine(container, "TR_H", halfW - armLength * 0.5f, halfH - width * 0.5f, armLength, width, color);
            BracketLine(container, "TR_V", halfW - width * 0.5f, halfH - armLength * 0.5f, width, armLength, color);
            // BL
            BracketLine(container, "BL_H", -halfW + armLength * 0.5f, -halfH + width * 0.5f, armLength, width, color);
            BracketLine(container, "BL_V", -halfW + width * 0.5f, -halfH + armLength * 0.5f, width, armLength, color);
            // BR
            BracketLine(container, "BR_H", halfW - armLength * 0.5f, -halfH + width * 0.5f, armLength, width, color);
            BracketLine(container, "BR_V", halfW - width * 0.5f, -halfH + armLength * 0.5f, width, armLength, color);
        }

        private static void BracketLine(RectTransform parent, string name, float x, float y, float w, float h, Color color)
        {
            var rect = Rect(parent, name, x, y, w, h);
            var img = Get<Image>(rect);
            img.color = color;
            img.raycastTarget = false;
        }

        private static TMP_Text Text(Transform parent, string name, string value, float x, float y, float w, float h, float size, TMP_FontAsset font)
        {
            var text = Get<TextMeshProUGUI>(Rect(parent, name, x, y, w, h));
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = Hex("EEF4F5");
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = parent.Find(name) as RectTransform;
            if (rect == null)
            {
                rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(parent, false);
            }
            Place(rect, x, y, w, h);
            return rect;
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private static T Get<T>(Component target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.gameObject.AddComponent<T>();
        }

        private static Color Hex(string value, float alpha = 1)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var color);
            color.a = alpha;
            return color;
        }

        [InitializeOnLoadMethod]
        private static void WatchBuildFlag()
        {
            EditorApplication.update += PollBuildFlag;
        }

        private static void PollBuildFlag()
        {
            if (System.IO.File.Exists("Temp/subterra-build-103-menu.flag"))
            {
                try
                {
                    System.IO.File.Delete("Temp/subterra-build-103-menu.flag");
                    Build();
                    System.IO.File.WriteAllText("Temp/subterra-build-103-menu.done", System.DateTime.Now.ToString("o"));
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[PromptB103MainMenuBuilder] Build flag failed: " + ex);
                }
            }
        }
    }
}
