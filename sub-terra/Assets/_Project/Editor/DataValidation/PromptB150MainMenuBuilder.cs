using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// B-150 메인화면 개편 빌더. B103 결과 위에 부팅 연출·자석 철제 프레임·슬롯 삭제·지상 기지식 버튼·배경 분위기를 얹는다.
    /// MainMenuPanel.prefab 한 곳만 수정하며, B103 빌더도 저장 직전에 이 Apply를 호출한다.
    /// </summary>
    public static class PromptB150MainMenuBuilder
    {
        public const string PrefabPath = PromptB103MainMenuBuilder.PrefabPath;
        public const string ArtPath = "Assets/_Project/Art/UI/MainMenu/B150/";
        public const string CornerPath = ArtPath + "slot-frame-corner.png";
        public const string CornerGlowPath = ArtPath + "slot-frame-corner-glow.png";
        public const string BarPath = ArtPath + "slot-frame-bar.png";
        public const string BarGlowPath = ArtPath + "slot-frame-bar-glow.png";
        public const string DangerOffPath = ArtPath + "button-danger-off.png";
        public const string DangerOnPath = ArtPath + "button-danger-on.png";
        public const string SignalIconPath = ArtPath + "icon-signal.png";
        public const string NewRecordIconPath = ArtPath + "icon-new-record.png";
        public const string TrashIconPath = ArtPath + "icon-trash.png";
        public const string QuitDangerIconPath = ArtPath + "icon-quit-danger.png";
        public const string ExplorationButtonPath = PromptB117SurfaceBaseBuilder.ArtPath + "Exploration-button.png";
        public const string SettingsIconPath = PromptB117SurfaceBaseBuilder.ArtPath + "Icons/icon-settings.png";
        public const string ButtonOffPath = "Assets/_Project/Art/UI/MainMenu/Settings/button-active-off.png";
        public const string ButtonOnPath = "Assets/_Project/Art/UI/MainMenu/Settings/button-active-on.png";

        public const string Subtitle = "봉인 너머에서 신호가 온다.";
        public static readonly float[] SlotY = { 170f, 52f, -66f };
        public static readonly Vector2 SlotSize = new Vector2(800f, 112f);
        public static readonly Vector2 ContinueSize = new Vector2(560f, 116f);
        public static readonly Vector2 SecondarySize = new Vector2(252f, 70f);
        public const float ContinueY = -198f;
        public const float SecondaryY = -322f;
        public const float SecondaryX = 272f;
        public static readonly string[] FramePieceNames = { "CornerTL", "CornerTR", "CornerBL", "CornerBR", "BarTop", "BarBottom" };

        private static readonly Vector4 BarBorder = new Vector4(32f, 0f, 32f, 0f);

        [MenuItem("SubTerra/UI/Build Prompt-B 150 Main Menu")]
        public static void Build()
        {
            SetupTextureImporters();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Apply(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[PromptB150MainMenuBuilder] Build completed.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void SetupTextureImporters()
        {
            foreach (var path in new[] { CornerPath, CornerGlowPath, DangerOffPath, DangerOnPath,
                         SignalIconPath, NewRecordIconPath, TrashIconPath, QuitDangerIconPath })
                ImportSprite(path, Vector4.zero);
            ImportSprite(BarPath, BarBorder);
            ImportSprite(BarGlowPath, BarBorder);
        }

        public static void Apply(GameObject root)
        {
            var content = (RectTransform)root.transform.Find("MenuContent");
            var font = content.Find("MessageText").GetComponent<TMP_Text>().font;
            var menuGroup = Get<CanvasGroup>(content);

            // 1. 부팅 문구·부제
            var header = Text(content, "BootHeader", font, "SUB-TERRA OPS // 신호 연결됨", 0f, 462f, 700f, 30f, 18f);
            header.color = Hex("59D8E6");
            header.characterSpacing = 4f;
            var subtitle = Text(content, "Subtitle", font, Subtitle, 0f, 282f, 720f, 38f, 24f);
            subtitle.color = Hex("C7DFE3");
            var title = (RectTransform)content.Find("Title");
            var logoGroup = Get<CanvasGroup>(title);
            logoGroup.blocksRaycasts = false;

            // 2. 슬롯 카드
            var cards = new SaveSlotCardView[3];
            for (var i = 0; i < 3; i++) cards[i] = BuildCard((RectTransform)content.Find("Slot" + (i + 1)), i, font);

            // 3. 아래 버튼: 이어하기(특별 버튼) / 새 게임·설정(일반) / 종료(빨강)
            var cont = Button(content, "ContinueButton", 0f, ContinueY, ContinueSize);
            var contIcon = Icon(cont.transform, SignalIconPath);
            PromptB117SurfaceBaseBuilder.StyleButton(cont, ExplorationButtonPath, ExplorationButtonPath,
                new Vector2(ContinueSize.x, ContinueSize.x * 728f / 2159f), contIcon, bright: true,
                glowColor: new Color(0.45f, 0.9f, 1f));
            Place(contIcon.rectTransform, -196f, 0f, 74f, 74f);
            ButtonLabel(cont, "이어하기", 40f, 40f, -4f, 330f, Hex("E6FBFF"), bold: true);

            var labels = new[] { "새 게임", "설정(esc)", "종료" };
            var names = new[] { "NewGameButton", "SettingsButton", "QuitButton" };
            var icons = new[] { NewRecordIconPath, SettingsIconPath, QuitDangerIconPath };
            var secondary = new CanvasGroup[3];
            for (var i = 0; i < 3; i++)
            {
                var danger = i == 2;
                var button = Button(content, names[i], (i - 1) * SecondaryX, SecondaryY, SecondarySize);
                var icon = Icon(button.transform, icons[i]);
                PromptB117SurfaceBaseBuilder.StyleButton(button, danger ? DangerOffPath : ButtonOffPath,
                    danger ? DangerOnPath : ButtonOnPath, SecondarySize, icon,
                    glowColor: danger ? new Color(1f, 0.35f, 0.3f) : (Color?)null);
                Place(icon.rectTransform, -82f, 0f, 38f, 38f);
                ButtonLabel(button, labels[i], 26f, 18f, 0f, 160f, danger ? Hex("FFD3CF") : Hex("E6FBFF"), bold: false);
                var feedback = new SerializedObject(button.GetComponent<SurfaceBaseButtonFeedback>());
                feedback.FindProperty("particleColor").colorValue = danger ? new Color(1f, 0.36f, 0.32f) : new Color(0.29f, 0.88f, 0.95f);
                feedback.ApplyModifiedPropertiesWithoutUndo();
                secondary[i] = Get<CanvasGroup>(button.transform);
            }
            var feedbackCont = new SerializedObject(cont.GetComponent<SurfaceBaseButtonFeedback>());
            feedbackCont.FindProperty("particleColor").colorValue = new Color(0.29f, 0.88f, 0.95f);
            feedbackCont.ApplyModifiedPropertiesWithoutUndo();

            Place((RectTransform)content.Find("MessageText"), 0f, -404f, 900f, 56f);
            Place((RectTransform)content.Find("VersionText"), 0f, -462f, 850f, 32f);

            // 4. 배경 분위기(배경 바로 위, 메뉴 뒤)
            var atmosphere = BuildAtmosphere(root.transform);

            // 5. 부팅/전환 셔터(메뉴 위, 팝업 아래)
            var overlay = Stretch(root.transform, "BootOverlay");
            overlay.SetSiblingIndex(content.GetSiblingIndex() + 1);
            var top = Stretch(overlay, "TopShutter");
            var topImage = Get<Image>(top);
            topImage.color = Hex("010406");
            topImage.raycastTarget = true;
            top.anchorMin = new Vector2(0f, 0.5f);
            var bottom = Stretch(overlay, "BottomShutter");
            var bottomImage = Get<Image>(bottom);
            bottomImage.color = Hex("010406");
            bottomImage.raycastTarget = true;
            bottom.anchorMax = new Vector2(1f, 0.5f);
            var line = Stretch(overlay, "ScanLine");
            line.anchorMin = new Vector2(0f, 0.5f);
            line.anchorMax = new Vector2(1f, 0.5f);
            line.sizeDelta = new Vector2(0f, 3f);
            var lineImage = Get<Image>(line);
            lineImage.color = Hex("C8FBFF", 0f);
            lineImage.raycastTarget = false;
            Get<Shadow>(line).effectColor = Hex("39D8EA", 0.8f);
            overlay.gameObject.SetActive(false);

            // 6. 삭제 확인 창: 덮어쓰기 확인 창을 그대로 복제해 같은 스타일을 쓴다.
            var delete = BuildDeleteConfirm(root.transform);

            var director = Get<MainMenuIntroDirector>(root.transform);
            var so = new SerializedObject(director);
            Ref(so, "overlayRoot", overlay.gameObject);
            Ref(so, "topShutter", top);
            Ref(so, "bottomShutter", bottom);
            Ref(so, "scanLine", line);
            Ref(so, "scanLineGraphic", lineImage);
            Ref(so, "menuGroup", menuGroup);
            Ref(so, "headerText", header);
            Ref(so, "logoGroup", logoGroup);
            Ref(so, "logoGlitchTarget", title.Find("TitleLogo"));
            Ref(so, "subtitleText", subtitle);
            Ref(so, "continueGroup", Get<CanvasGroup>(cont.transform));
            SetArray(so, "cards", cards);
            SetArray(so, "secondaryGroups", secondary);
            so.ApplyModifiedPropertiesWithoutUndo();

            var view = new SerializedObject(root.GetComponent<MainMenuView>());
            Ref(view, "intro", director);
            Ref(view, "deleteConfirmRoot", delete.gameObject);
            Ref(view, "deleteMessageText", delete.Find("DeleteMessage").GetComponent<TMP_Text>());
            Ref(view, "deleteConfirmButton", delete.Find("DeleteYes").GetComponent<UnityEngine.UI.Button>());
            Ref(view, "deleteCancelButton", delete.Find("DeleteNo").GetComponent<UnityEngine.UI.Button>());
            Ref(view, "deleteCloseButton", delete.Find("DeleteClose").GetComponent<UnityEngine.UI.Button>());
            view.ApplyModifiedPropertiesWithoutUndo();
            atmosphere.SetSiblingIndex(1);
        }

        private static SaveSlotCardView BuildCard(RectTransform slot, int index, TMP_FontAsset font)
        {
            Place(slot, 0f, SlotY[index], SlotSize.x, SlotSize.y);
            var selection = slot.Find("Selection");
            // 선택 표시는 철제 프레임 하나로 한다. 화살표·코너 브래킷·선택 테두리는 제거.
            foreach (var name in new[] { "TriangleSelector", "CornerBrackets", "Edge0", "Edge1", "Edge2", "Edge3" })
                DestroyChild(selection, name);
            var innerGlow = (RectTransform)selection.Find("InnerGlow");
            Place(innerGlow, 0f, 0f, SlotSize.x, SlotSize.y);
            Place((RectTransform)selection, 0f, 0f, SlotSize.x, SlotSize.y);

            var label = slot.Find("Label").GetComponent<TMP_Text>();
            Place(label.rectTransform, 83f, 0f, 500f, 104f);
            label.fontSize = 23f;
            label.lineSpacing = -6f;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            var thumbnail = (RectTransform)slot.Find("Thumbnail");
            Place(thumbnail, -266f, 0f, 160f, 90f);
            var noise = Stretch(thumbnail, "SignalNoise");
            noise.SetAsFirstSibling();
            var noiseRaw = Get<RawImage>(noise);
            noiseRaw.color = Hex("8FE9F5", 0f);
            noiseRaw.raycastTarget = false;
            var placeholder = thumbnail.Find("Placeholder").GetComponent<TMP_Text>();
            placeholder.text = "신호 없음";
            placeholder.color = Hex("C7DFE3");
            placeholder.transform.SetAsLastSibling();

            var flash = Stretch(slot, "ExitFlash");
            var flashImage = Get<Image>(flash);
            flashImage.color = Hex("BFF8FF", 0f);
            flashImage.raycastTarget = false;

            // 휴지통: 오른쪽 위. 평소 흐리게, 선택 카드에서만 또렷하게.
            var deleteRect = Rect(slot, "DeleteButton", SlotSize.x * 0.5f - 30f, SlotSize.y * 0.5f - 24f, 40f, 40f);
            var hit = Get<Image>(deleteRect);
            hit.sprite = null;
            hit.color = Color.clear;
            hit.raycastTarget = true;
            var deleteButton = Get<UnityEngine.UI.Button>(deleteRect);
            deleteButton.targetGraphic = hit;
            deleteButton.transition = Selectable.Transition.None;
            var nav = deleteButton.navigation;
            nav.mode = Navigation.Mode.None;
            deleteButton.navigation = nav;
            var deleteGroup = Get<CanvasGroup>(deleteRect);
            var trash = Rect(deleteRect, "Icon", 0f, 0f, 26f, 26f);
            var trashImage = Get<Image>(trash);
            trashImage.sprite = Sprite(TrashIconPath);
            trashImage.preserveAspect = true;
            trashImage.raycastTarget = false;

            var frame = Stretch(slot, "MagnetFrame");
            var pieces = new RectTransform[6];
            var glows = new Image[6];
            var halfW = SlotSize.x * 0.5f;
            var halfH = SlotSize.y * 0.5f;
            for (var p = 0; p < 4; p++)
            {
                var sx = p % 2 == 0 ? -1f : 1f;
                var sy = p < 2 ? 1f : -1f;
                pieces[p] = Piece(frame, FramePieceNames[p], CornerPath, CornerGlowPath,
                    sx * (halfW - 15.4f), sy * (halfH - 15.4f), 46f, 46f, new Vector3(-sx, sy, 1f), false, out glows[p]);
            }

            pieces[4] = Piece(frame, "BarTop", BarPath, BarGlowPath, 0f, halfH - 0.5f, 260f, 15.4f, Vector3.one, true, out glows[4]);
            pieces[5] = Piece(frame, "BarBottom", BarPath, BarGlowPath, 0f, -halfH + 0.5f, 260f, 15.4f, new Vector3(1f, -1f, 1f), true, out glows[5]);

            // 계층 순서: 판 → 텍스트/썸네일 → 선택 글로우 → 번쩍임 → 휴지통 → 철제 프레임
            selection.SetAsLastSibling();
            flash.SetAsLastSibling();
            deleteRect.SetAsLastSibling();
            frame.SetAsLastSibling();

            var intro = Get<CanvasGroup>(slot);
            var card = Get<SaveSlotCardView>(slot);
            var so = new SerializedObject(card);
            Ref(so, "introGroup", intro);
            Ref(so, "signalNoise", noiseRaw);
            Ref(so, "deleteButton", deleteButton);
            Ref(so, "deleteGroup", deleteGroup);
            Ref(so, "exitFlash", flashImage);
            SetArray(so, "framePieces", pieces);
            SetArray(so, "frameGlows", glows);
            so.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }

        private static RectTransform Piece(RectTransform parent, string name, string sprite, string glowSprite,
            float x, float y, float w, float h, Vector3 scale, bool sliced, out Image glow)
        {
            var rect = Rect(parent, name, x, y, w, h);
            rect.localScale = scale;
            var image = Get<Image>(rect);
            image.sprite = Sprite(sprite);
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = sliced ? 3.1f : 1f;
            image.color = Color.white;
            image.raycastTarget = false;
            var glowRect = Stretch(rect, "SeamGlow");
            glow = Get<Image>(glowRect);
            glow.sprite = Sprite(glowSprite);
            glow.type = image.type;
            glow.pixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier;
            glow.color = Hex("73F2FF", 0f);
            glow.raycastTarget = false;
            // 선택 전에는 숨겨 두고 SaveSlotCardView.ApplyFrame이 켠다.
            rect.gameObject.SetActive(false);
            return rect;
        }

        private static RectTransform BuildAtmosphere(Transform root)
        {
            var atmosphere = Stretch(root, "Atmosphere");
            var backdrop = Rect(atmosphere, "Backdrop", 0f, 0f, 1500f, 1150f);
            var backdropImage = Get<Image>(backdrop);
            // 텍스처는 런타임 절차 생성이라 에디터에서는 투명하게 두고 MainMenuAtmosphere.Awake가 채운다.
            backdropImage.color = Color.clear;
            backdropImage.raycastTarget = false;
            var vignette = Get<Image>(Stretch(atmosphere, "Vignette"));
            vignette.raycastTarget = false;
            vignette.color = Color.clear;
            var scan = Get<RawImage>(Stretch(atmosphere, "Scanlines"));
            scan.raycastTarget = false;
            scan.color = Color.clear;
            var burst = Get<RawImage>(Stretch(atmosphere, "StaticBurst"));
            burst.raycastTarget = false;
            burst.color = new Color(1f, 1f, 1f, 0f);
            var dust = Stretch(atmosphere, "Dust");
            var component = Get<MainMenuAtmosphere>(atmosphere);
            var so = new SerializedObject(component);
            Ref(so, "backdrop", backdropImage);
            Ref(so, "vignette", vignette);
            Ref(so, "scanlines", scan);
            Ref(so, "staticBurst", burst);
            Ref(so, "dustRoot", dust);
            so.ApplyModifiedPropertiesWithoutUndo();
            return atmosphere;
        }

        private static RectTransform BuildDeleteConfirm(Transform root)
        {
            var overwrite = root.Find("OverwriteConfirm");
            var existing = root.Find("DeleteConfirm") as RectTransform;
            if (existing == null)
            {
                existing = (RectTransform)Object.Instantiate(overwrite.gameObject, root).transform;
                existing.name = "DeleteConfirm";
                Rename(existing, "OverwriteMessage", "DeleteMessage");
                Rename(existing, "OverwriteYes", "DeleteYes");
                Rename(existing, "OverwriteNo", "DeleteNo");
                Rename(existing, "OverwriteTitle", "DeleteTitle");
                Rename(existing, "OverwriteClose", "DeleteClose");
            }

            existing.SetSiblingIndex(overwrite.GetSiblingIndex() + 1);
            existing.gameObject.SetActive(false);
            existing.Find("DeleteTitle").GetComponent<TMP_Text>().text = "기록 삭제?";
            var message = existing.Find("DeleteMessage").GetComponent<TMP_Text>();
            var size = ((RectTransform)overwrite.Find("OverwriteMessage")).sizeDelta;
            message.rectTransform.sizeDelta = new Vector2(size.x, 84f);
            message.enableWordWrapping = true;
            message.text = "탐사 기록 01을 삭제할까요?\n<size=85%><color=#E6A23C>되돌릴 수 없습니다.</color></size>";
            existing.Find("DeleteYes").GetComponentInChildren<TMP_Text>(true).text = "삭제";
            existing.Find("DeleteNo").GetComponentInChildren<TMP_Text>(true).text = "취소";
            return existing;
        }

        private static UnityEngine.UI.Button Button(RectTransform content, string name, float x, float y, Vector2 size)
        {
            var rect = (RectTransform)content.Find(name);
            Place(rect, x, y, size.x, size.y);
            // B103/B104가 만든 컷코너 판·테두리·호버 오버레이는 지상 기지 스타일로 대체한다.
            foreach (var child in new[] { "HoverOverlay", "Edge0", "Edge1", "Edge2", "Edge3" })
                DestroyChild(rect, child);
            var skin = rect.GetComponent<MenuSpriteButtonSkin>();
            if (skin != null) Object.DestroyImmediate(skin);
            var image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
            }

            var button = rect.GetComponent<UnityEngine.UI.Button>();
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            return button;
        }

        private static Image Icon(Transform parent, string path)
        {
            var image = Get<Image>(Rect(parent, "Icon", 0f, 0f, 40f, 40f));
            image.sprite = Sprite(path);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void ButtonLabel(UnityEngine.UI.Button button, string value, float size, float x, float y, float width,
            Color color, bool bold)
        {
            var label = button.transform.Find("Label").GetComponent<TMP_Text>();
            label.text = value;
            label.fontSize = size;
            label.enableAutoSizing = false;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.verticalAlignment = VerticalAlignmentOptions.Geometry;
            label.raycastTarget = false;
            Place(label.rectTransform, x, y, width, ((RectTransform)button.transform).sizeDelta.y - 8f);
            label.transform.SetAsLastSibling();
        }

        private static TMP_Text Text(Transform parent, string name, TMP_FontAsset font, string value,
            float x, float y, float w, float h, float size)
        {
            var text = Get<TextMeshProUGUI>(Rect(parent, name, x, y, w, h));
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.enableAutoSizing = false;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static void ImportSprite(string path, Vector4 border)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[PromptB150MainMenuBuilder] Missing art: " + path);
                return;
            }

            var changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteBorder != border
                || importer.mipmapEnabled
                || importer.textureCompression != TextureImporterCompression.Uncompressed;
            if (!changed) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = border;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void Rename(Transform root, string from, string to)
        {
            var child = root.Find(from);
            if (child != null) child.name = to;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        private static RectTransform Stretch(Transform parent, string name)
        {
            var rect = Rect(parent, name, 0f, 0f, 0f, 0f);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
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

        private static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static void Ref(SerializedObject so, string name, Object value) =>
            so.FindProperty(name).objectReferenceValue = value;

        private static void SetArray(SerializedObject so, string name, Object[] values)
        {
            var property = so.FindProperty(name);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static Color Hex(string value, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var color);
            color.a = alpha;
            return color;
        }
    }
}
