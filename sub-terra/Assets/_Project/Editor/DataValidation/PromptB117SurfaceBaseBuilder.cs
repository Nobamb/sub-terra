using System.IO;
using SubTerra.App.UI;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>117번 지상 기지 화면만 변경한다. 모달의 기존 서비스 참조는 보존한다.</summary>
    public static class PromptB117SurfaceBaseBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        public const string ArtPath = "Assets/_Project/Art/UI/SurfaceBase/";
        private const string SettingsArt = "Assets/_Project/Art/UI/MainMenu/Settings/";

        [MenuItem("SubTerra/UI/Build Prompt-B 117 Surface Base")]
        public static void BuildFromMenu() => Debug.Log(Build());

        public static string Build()
        {
            ImportArt();
            ImportIcons();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Apply(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return "Prompt-B 117 SurfaceBasePanel updated";
        }

        [MenuItem("SubTerra/UI/Refine Surface Base Header and Action Icons")]
        public static void RefineFromMenu() => Debug.Log(Refine());

        public static string Refine()
        {
            ImportIcons();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ApplyMainControls(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return "Surface Base header and action icons updated";
        }

        private static void ImportIcons()
        {
            foreach (var name in new[] { "cargo", "gold", "mine", "sell", "upgrade", "reset", "settings", "quit" })
            {
                var path = ArtPath + "Icons/icon-" + name + ".png";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static void ImportArt()
        {
            var source = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../work_process/MVP2/UI-fix-markdown-document/concept-image/in-game/surface-base"));
            Directory.CreateDirectory(ArtPath);
            foreach (var name in new[] { "surface-base-background.png", "surface-base-top-frame-basic.png",
                "Exploration-button.png", "mine-init-button.png" })
            {
                var path = ArtPath + name;
                File.Copy(Path.Combine(source, name), path, true);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 4096;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static void Apply(GameObject root)
        {
            var font = root.transform.Find("SurfaceBaseContent/Title").GetComponent<TMP_Text>().font;
            var content = (RectTransform)root.transform.Find("SurfaceBaseContent");
            Stretch(content);
            root.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            root.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            content.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            content.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;

            var bg = Image(root.transform, "SurfaceBackground", Sprite(ArtPath + "surface-base-background.png"));
            Stretch(bg.rectTransform);
            var fit = Ensure<UnityEngine.UI.AspectRatioFitter>(bg.gameObject);
            fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 1672f / 941f;
            bg.transform.SetAsFirstSibling();

            var header = Image(content, "TopFrame", Sprite(ArtPath + "surface-base-top-frame-basic.png"));
            Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(1920f, 640f));
            header.transform.SetAsFirstSibling();
            ApplyMainControls(root);

            foreach (var name in new[] { "GoalsText", "EnergyText", "DeepZoneText", "RecentRunText" })
                content.Find(name).gameObject.SetActive(false);
            var message = content.Find("MessageText").GetComponent<TMP_Text>();
            Place(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -375f), new Vector2(1000f, 44f));
            TextStyle(message, 22f, TextAlignmentOptions.Center);

            BuildUpgradeModal(content, font, out var modal, out var close);
            var economy = new SerializedObject(content.Find("EconomyPanel").GetComponent<SubTerra.App.UI.Economy.EconomyPanelView>());
            Ref(economy, "levelSummaryRoot", null);
            economy.ApplyModifiedPropertiesWithoutUndo();
            var view = new SerializedObject(root.GetComponent<SurfaceBaseView>());
            Ref(view, "upgradeCloseButton", close);
            Ref(view, "upgradeRoot", modal);
            view.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyMainControls(GameObject root)
        {
            var content = root.transform.Find("SurfaceBaseContent");
            var font = content.Find("Title").GetComponent<TMP_Text>().font;
            var header = (RectTransform)content.Find("TopFrame");
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = Vector2.one;
            header.sizeDelta = new Vector2(0f, 640f);
            var title = content.Find("Title").GetComponent<TMP_Text>();
            title.text = "지상 기지";
            TextStyle(title, 46f, TextAlignmentOptions.MidlineLeft);
            Place(title.rectTransform, new Vector2(128f / 1920f, 1f), new Vector2(0f, -70f), new Vector2(260f, 70f), new Vector2(0f, 0.5f));
            var subtitle = Text(content, "EnglishTitle", font, "S U R F A C E   B A S E", 22f);
            Place(subtitle.rectTransform, new Vector2(398f / 1920f, 1f), new Vector2(0f, -65f), new Vector2(370f, 44f), new Vector2(0f, 0.5f));

            // 헤더 우측 패널(약 x 1214~1848, 모서리 사선 제외 안전폭 ~1810)에 맞춘 중심 x(1920 기준 px).
            const float cargoX = 1262f, goldX = 1455f, settingsX = 1690f, quitX = 1772f;
            var cargo = Text(content, "CargoText", font, "화물 0", 23f);
            TextStyle(cargo, 23f, TextAlignmentOptions.MidlineLeft);
            Place(cargo.rectTransform, new Vector2(cargoX / 1920f, 1f), new Vector2(35f, -66f), new Vector2(145f, 48f), new Vector2(0f, 0.5f));
            var gold = Text(content, "GoldText", font, "골드 <color=#FFE66B>0G</color>", 23f);
            TextStyle(gold, 23f, TextAlignmentOptions.MidlineLeft);
            Place(gold.rectTransform, new Vector2(goldX / 1920f, 1f), new Vector2(35f, -66f), new Vector2(145f, 48f), new Vector2(0f, 0.5f));
            var cargoIcon = Image(content, "CargoIcon", Sprite(ArtPath + "Icons/icon-cargo.png"));
            Place(cargoIcon.rectTransform, new Vector2(cargoX / 1920f, 1f), new Vector2(0f, -66f), new Vector2(46f, 46f));
            cargoIcon.preserveAspect = true;
            var goldIcon = Image(content, "GoldIcon", Sprite(ArtPath + "Icons/icon-gold.png"));
            Place(goldIcon.rectTransform, new Vector2(goldX / 1920f, 1f), new Vector2(0f, -66f), new Vector2(46f, 46f));
            goldIcon.preserveAspect = true;

            var explore = content.Find("ExploreButton").GetComponent<UnityEngine.UI.Button>();
            Place((RectTransform)explore.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(850f, 210f));
            var exploreIcon = Image(explore.transform, "Icon", Sprite(ArtPath + "Icons/icon-mine.png"));
            StyleButton(explore, ArtPath + "Exploration-button.png", ArtPath + "Exploration-button.png", new Vector2(850f, 287f),
                exploreIcon, bright: true, glowColor: new Color(0.45f, 0.9f, 1f));
            Label(explore, "지하 탐사 시작", 62f);
            var exploreLabel = explore.GetComponentInChildren<TMP_Text>();
            exploreLabel.fontStyle = FontStyles.Bold;
            exploreLabel.horizontalAlignment = HorizontalAlignmentOptions.Left;
            // 글리프 실측 영역 기준으로 세로 중앙을 맞추고, 프레임 안쪽 패널 중심(이미지 중심보다 약 6px 아래)에 둔다.
            exploreLabel.verticalAlignment = VerticalAlignmentOptions.Geometry;
            Place(exploreLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-110f, -6f), new Vector2(470f, 126f), new Vector2(0f, 0.5f));
            Place(exploreIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-245f, 0f), new Vector2(154f, 134f));
            exploreIcon.preserveAspect = true;

            var sell = content.Find("OpenSellButton").GetComponent<UnityEngine.UI.Button>();
            Place((RectTransform)sell.transform, new Vector2(0.5f, 0.5f), new Vector2(-213f, -145f), new Vector2(386f, 98f));
            var sellIcon = Image(sell.transform, "Icon", Sprite(ArtPath + "Icons/icon-sell.png"));
            StyleButton(sell, SettingsArt + "button-active-off.png", SettingsArt + "button-active-on.png", new Vector2(386f, 98f), sellIcon);
            Label(sell, "자원 판매", 30f);
            ButtonIcon(sell, sellIcon);
            var upgrade = Button(content, "UpgradeButton");
            Place((RectTransform)upgrade.transform, new Vector2(0.5f, 0.5f), new Vector2(213f, -145f), new Vector2(386f, 98f));
            var upgradeIcon = Image(upgrade.transform, "Icon", Sprite(ArtPath + "Icons/icon-upgrade.png"));
            StyleButton(upgrade, SettingsArt + "button-active-off.png", SettingsArt + "button-active-on.png", new Vector2(386f, 98f), upgradeIcon);
            Label(upgrade, "업그레이드", 30f, font);
            ButtonIcon(upgrade, upgradeIcon);

            var reset = content.Find("ResetMineButton").GetComponent<UnityEngine.UI.Button>();
            Place((RectTransform)reset.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -282f), new Vector2(530f, 112f));
            var resetIcon = Image(reset.transform, "Icon", Sprite(ArtPath + "Icons/icon-reset.png"));
            StyleButton(reset, ArtPath + "mine-init-button.png", ArtPath + "mine-init-button.png", new Vector2(530f, 177f),
                resetIcon, bright: true, glowColor: new Color(0.5f, 1f, 0.6f), spin: true);
            Label(reset, "새 광산 초기화", 29f);
            var resetLabel = reset.transform.Find("Label").GetComponent<TMP_Text>();
            resetLabel.alignment = TextAlignmentOptions.Center;
            resetLabel.verticalAlignment = VerticalAlignmentOptions.Geometry;
            resetLabel.enableAutoSizing = false;
            resetLabel.fontSize = 29f;
            resetLabel.color = new Color(0.35f, 0.97f, 0.97f);
            resetIcon.preserveAspect = true;
            var fee = Text(reset.transform, "FeeLabel", font, "500G", 29f);
            fee.alignment = TextAlignmentOptions.Center;
            fee.verticalAlignment = VerticalAlignmentOptions.Geometry;
            fee.enableAutoSizing = false;
            fee.fontSize = 29f;
            fee.color = new Color(1f, 0.9f, 0.42f);
            // 아이콘·이름·비용을 같은 간격으로 묶어 버튼 중앙에 둔다(언어가 바뀌어도 유지).
            resetIcon.rectTransform.sizeDelta = new Vector2(46f, 46f);
            resetIcon.transform.SetSiblingIndex(resetIcon.transform.parent.childCount - 1);
            resetLabel.transform.SetAsLastSibling();
            fee.transform.SetAsLastSibling();
            var resetRow = Ensure<UnityEngine.UI.HorizontalLayoutGroup>(reset.gameObject);
            resetRow.childAlignment = TextAnchor.MiddleCenter;
            resetRow.spacing = 16f;
            // 프레임 그림의 안쪽 패널 중심이 버튼 중심보다 약 3px 위라 아래 여백으로 내용 중심을 맞춘다.
            resetRow.padding = new RectOffset(0, 0, 0, 6);
            resetRow.childControlWidth = true;
            resetRow.childControlHeight = false;
            resetRow.childForceExpandWidth = false;
            resetRow.childForceExpandHeight = false;
            Ensure<UnityEngine.UI.LayoutElement>(resetIcon.gameObject).preferredWidth = 46f;
            resetLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            foreach (var name in new[] { "Frame", "HoverFrame", "InnerLight", "HoverParticles" })
                Ensure<UnityEngine.UI.LayoutElement>(reset.transform.Find(name).gameObject).ignoreLayout = true;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)reset.transform);

            IconButton(content.Find("SettingsButton").GetComponent<UnityEngine.UI.Button>(), settingsX - 1920f, ArtPath + "Icons/icon-settings.png");
            IconButton(content.Find("QuitButton").GetComponent<UnityEngine.UI.Button>(), quitX - 1920f, ArtPath + "Icons/icon-quit.png");
            var view = new SerializedObject(root.GetComponent<SurfaceBaseView>());
            Ref(view, "cargoText", cargo);
            Ref(view, "goldText", gold);
            Ref(view, "upgradeButton", upgrade);
            Ref(view, "resetMineFeeText", fee);
            view.ApplyModifiedPropertiesWithoutUndo();
            root.GetComponent<SurfaceBaseView>().SetMineResetButtonFee(SubTerra.App.Save.MineResetService.FeeGold);
        }

        private static void BuildUpgradeModal(Transform content, TMP_FontAsset font, out GameObject modal, out UnityEngine.UI.Button close)
        {
            var backdrop = Image(content, "UpgradeModal", null);
            Stretch(backdrop.rectTransform);
            backdrop.color = new Color(0.005f, 0.02f, 0.035f, 0.88f);
            backdrop.raycastTarget = true;
            modal = backdrop.gameObject;
            var canvas = Ensure<Canvas>(modal);
            canvas.overrideSorting = true;
            canvas.sortingOrder = UiLayerPriority.ModalPanel;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
            Ensure<UnityEngine.UI.GraphicRaycaster>(modal);
            var card = content.Find("ProgressionPanel") ?? backdrop.transform.Find("ProgressionPanel");
            card.SetParent(backdrop.transform, false);
            Place((RectTransform)card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 680f));
            var plate = Ensure<UnityEngine.UI.Image>(card.gameObject);
            plate.sprite = Sprite("Assets/_Project/Art/UI/MainMenu/UI_CutCorner_Plate.png");
            plate.type = UnityEngine.UI.Image.Type.Sliced;
            plate.color = new Color(0.06f, 0.16f, 0.19f, 1f);
            plate.raycastTarget = true;
            Ensure<PopupWindowDrag>(card.gameObject);
            var title = Text(card, "UpgradeTitle", font, "장비 업그레이드", 32f);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(-250f, -48f), new Vector2(460f, 64f));
            close = Button(card, "CloseUpgradeButton");
            Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-62f, -48f), new Vector2(72f, 58f));
            StyleButton(close, SettingsArt + "button-active-off.png", SettingsArt + "button-active-on.png", new Vector2(72f, 58f));
            Label(close, "X", 24f, font);
            var view = card.GetComponent<ProgressionPanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("levelsOnlySummary").boolValue = false;
            so.FindProperty("hideUpgradeEntryList").boolValue = false;
            so.FindProperty("hideDeepZoneTab").boolValue = false;
            Ref(so, "panelRoot", modal);
            so.ApplyModifiedPropertiesWithoutUndo();
            card.Find("UpgradeList").gameObject.SetActive(false);
            var tabs = (RectTransform)card.Find("CategoryTabBar");
            tabs.gameObject.SetActive(true);
            Place(tabs, new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(960f, 48f));
            int index = 0;
            foreach (Transform tab in tabs)
            {
                Place((RectTransform)tab, new Vector2(0.5f, 0.5f), new Vector2(-384f + index++ * 192f, 0f), new Vector2(180f, 44f));
                tab.gameObject.SetActive(true);
            }
            Place((RectTransform)card.Find("UpgradeDetail"), new Vector2(0.5f, 0.5f), new Vector2(210f, -10f), new Vector2(500f, 330f));
            TextStyle(card.Find("UpgradeDetail").GetComponent<TMP_Text>(), 21f, TextAlignmentOptions.TopLeft);
            Place((RectTransform)card.Find("UpgradeResult"), new Vector2(0.5f, 0.5f), new Vector2(190f, -280f), new Vector2(580f, 50f));
            card.Find("UpgradeResult").gameObject.SetActive(true);
            Place((RectTransform)card.Find("ProgDeep"), new Vector2(0.5f, 0.5f), new Vector2(200f, 0f), new Vector2(520f, 300f));
            var purchase = card.Find("PurchaseButton").GetComponent<UnityEngine.UI.Button>();
            Place((RectTransform)purchase.transform, new Vector2(0.5f, 0.5f), new Vector2(210f, -220f), new Vector2(280f, 62f));
            StyleButton(purchase, SettingsArt + "button-active-off.png", SettingsArt + "button-active-on.png", new Vector2(280f, 62f));
            while (purchase.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(purchase.onClick, 0);
            UnityEventTools.AddPersistentListener(purchase.onClick, card.GetComponent<ProgressionPanelBinder>().PurchaseSelected);
            modal.SetActive(false);
        }

        private static void IconButton(UnityEngine.UI.Button button, float x, string icon)
        {
            Place((RectTransform)button.transform, new Vector2(1f + x / 1920f, 1f), new Vector2(0f, -66f), new Vector2(72f, 62f));
            StyleButton(button, SettingsArt + "button-active-off.png", SettingsArt + "button-active-on.png", new Vector2(72f, 62f));
            button.GetComponentInChildren<TMP_Text>(true).gameObject.SetActive(false);
            var image = Image(button.transform, "Icon", Sprite(icon));
            Place(image.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f));
            image.preserveAspect = true;
        }

        private static void ButtonIcon(UnityEngine.UI.Button button, UnityEngine.UI.Image icon)
        {
            icon.preserveAspect = true;
            icon.rectTransform.sizeDelta = new Vector2(50f, 50f);
            Ensure<UnityEngine.UI.LayoutElement>(icon.gameObject).preferredWidth = 50f;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.alignment = TextAlignmentOptions.Center;
            label.verticalAlignment = VerticalAlignmentOptions.Geometry;
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.sizeDelta = new Vector2(label.rectTransform.sizeDelta.x, 66f);
            icon.transform.SetAsLastSibling();
            label.transform.SetAsLastSibling();
            // 아이콘과 글자의 간격(32px)은 기존 그대로 두고 둘을 한 묶음으로 버튼 중앙에 둔다.
            var row = Ensure<UnityEngine.UI.HorizontalLayoutGroup>(button.gameObject);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.spacing = 32f;
            row.padding = new RectOffset();
            row.childControlWidth = true;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            foreach (var name in new[] { "Frame", "HoverFrame", "InnerLight", "HoverParticles" })
                Ensure<UnityEngine.UI.LayoutElement>(button.transform.Find(name).gameObject).ignoreLayout = true;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)button.transform);
        }

        internal static void StyleButton(UnityEngine.UI.Button button, string normal, string hover, Vector2 frameSize,
            UnityEngine.UI.Image icon = null, bool bright = false, Color? glowColor = null, bool spin = false)
        {
            var hit = Ensure<UnityEngine.UI.Image>(button.gameObject);
            hit.color = Color.clear;
            hit.raycastTarget = true;
            button.targetGraphic = hit;
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            var plate = Image(button.transform, "Frame", Sprite(normal));
            Place(plate.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, frameSize);
            plate.color = new Color(0.82f, 0.82f, 0.82f, 1f);
            plate.transform.SetAsFirstSibling();
            var highlight = Image(button.transform, "HoverFrame", Sprite(hover));
            Place(highlight.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, frameSize);
            highlight.color = Color.clear;
            highlight.transform.SetSiblingIndex(1);
            var mask = Child(button.transform, "InnerLight");
            Stretch(mask);
            mask.offsetMin = new Vector2(35f, 15f);
            mask.offsetMax = new Vector2(-35f, -15f);
            Ensure<UnityEngine.UI.RectMask2D>(mask.gameObject);
            mask.SetSiblingIndex(2);
            var sweep = Image(mask, "Sweep", Sprite(SettingsArt + "slider-glow.png"));
            Stretch(sweep.rectTransform);
            sweep.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            sweep.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            sweep.rectTransform.sizeDelta = new Vector2(bright ? 180f : 90f, 0f);
            sweep.color = Color.clear;
            UnityEngine.UI.Image glow = null;
            if (bright)
            {
                glow = Image(mask, "HoverGlow", null);
                Stretch(glow.rectTransform);
                glow.color = Color.clear;
                glow.transform.SetSiblingIndex(0);
            }
            var particles = Child(button.transform, "HoverParticles");
            Stretch(particles);
            particles.offsetMin = new Vector2(35f, 15f);
            particles.offsetMax = new Vector2(-35f, -15f);
            particles.SetSiblingIndex(3);
            if (icon != null)
            {
                float idle = 0.45f;
                icon.color = new Color(idle, idle, idle, 1f);
            }
            var skin = new SerializedObject(Ensure<SurfaceBaseButtonFeedback>(button.gameObject));
            Ref(skin, "plate", plate);
            Ref(skin, "highlight", highlight);
            Ref(skin, "sweep", sweep);
            Ref(skin, "icon", icon);
            Ref(skin, "glow", glow);
            Ref(skin, "particleRoot", particles);
            Ref(skin, "particleSprite", Sprite(SettingsArt + "particle-dot.png"));
            skin.FindProperty("glowColor").colorValue = glowColor ?? Color.white;
            skin.FindProperty("hoverBoost").floatValue = bright ? 1f : 0f;
            skin.FindProperty("spinIconOnHover").boolValue = spin;
            skin.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Label(UnityEngine.UI.Button button, string value, float size, TMP_FontAsset font = null)
        {
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) label = Text(button.transform, "Label", font, value, size);
            label.text = value;
            TextStyle(label, size, TextAlignmentOptions.Center);
            Place(label.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                ((RectTransform)button.transform).sizeDelta - new Vector2(70f, 12f));
            label.transform.SetAsLastSibling();
        }

        private static TMP_Text Text(Transform parent, string name, TMP_FontAsset font, string value, float size)
        {
            var text = Ensure<TextMeshProUGUI>(Child(parent, name).gameObject);
            text.font = font;
            text.text = value;
            TextStyle(text, size, TextAlignmentOptions.Center);
            return text;
        }

        private static void TextStyle(TMP_Text text, float size, TextAlignmentOptions alignment)
        {
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.8f;
            text.fontSizeMax = size;
            text.alignment = alignment;
            text.color = new Color(0.9f, 0.99f, 1f);
            text.raycastTarget = false;
        }

        private static UnityEngine.UI.Image Image(Transform parent, string name, Sprite sprite)
        {
            var image = Ensure<UnityEngine.UI.Image>(Child(parent, name).gameObject);
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static UnityEngine.UI.Button Button(Transform parent, string name) =>
            Ensure<UnityEngine.UI.Button>(Child(parent, name).gameObject);
        private static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
        private static T Ensure<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }
        private static void Ref(SerializedObject so, string name, Object value) => so.FindProperty(name).objectReferenceValue = value;

        private static RectTransform Child(Transform parent, string name)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null) return existing;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
