using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.App.Save;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// 새 광산 버튼과 '새 광산 구역' 확인 팝업을 Surface Base 프리팹에만 적용한다.
    /// prompt-B 123-2: 컨셉 이미지 기준으로 프레임·광산 배경·육각 입구·발광·패널·버튼 레이어를 나눠 다시 만든다.
    /// 그림은 work_process/.../b123-2-art/make_init_mine_popup_art.py 가 만든다.
    /// </summary>
    public static class MineResetSurfaceBaseLayoutBuilder
    {
        public const string SurfaceBasePrefabPath =
            "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";
        public const string SurfaceBaseScenePath =
            "Assets/_Project/Scenes/App/SurfaceBase.unity";
        public const float ResetButtonY = 8f;
        public const float MessageY = -48f;

        public const string ArtFolder = "Assets/_Project/Art/UI/SurfaceBase/MineReset/";
        public const string FramePath = ArtFolder + "mine-reset-frame.png";
        public const string FrameGlowPath = ArtFolder + "mine-reset-frame-glow.png";
        public const string PanelPath = ArtFolder + "mine-reset-panel.png";
        public const string CavePath = ArtFolder + "mine-reset-cave.png";
        public const string HexMinePath = ArtFolder + "mine-reset-hex-mine.png";
        public const string HexBorderPath = ArtFolder + "mine-reset-hex-border.png";
        public const string HexBorderGlowPath = ArtFolder + "mine-reset-hex-border-glow.png";
        public const string HexCrystalGlowPath = ArtFolder + "mine-reset-hex-glow-crystals.png";
        public const string HexTunnelGlowPath = ArtFolder + "mine-reset-hex-glow-tunnel.png";
        public const string HexRingsPath = ArtFolder + "mine-reset-hex-rings.png";
        public const string CoreGlowPath = ArtFolder + "mine-reset-core-glow.png";
        public const string ScanLinePath = ArtFolder + "mine-reset-scanline.png";
        public const string TitleDividerPath = ArtFolder + "mine-reset-title-divider.png";
        public const string CostPlatePath = ArtFolder + "mine-reset-cost-plate.png";
        public const string InfoPlatePath = ArtFolder + "mine-reset-info-plate.png";
        public const string TimerPlatePath = ArtFolder + "mine-reset-timer-plate.png";
        public const string GoldIconPath = "Assets/_Project/Art/UI/Upgrade/Icons/upgrade-icon-coins.png";
        public const string ResetIconPath = ArtFolder + "mine-reset-badge-reset.png";
        public const string KeepIconPath = ArtFolder + "mine-reset-badge-keep.png";
        public const string TimerIconPath = ArtFolder + "mine-reset-icon-clock.png";
        public const string TimerDividerPath = ArtFolder + "mine-reset-timer-divider.png";
        public const string ButtonCancelPath = ArtFolder + "mine-reset-button-cancel.png";
        public const string ButtonCancelHoverPath = ArtFolder + "mine-reset-button-cancel-hover.png";
        public const string ButtonConfirmPath = ArtFolder + "mine-reset-button-confirm.png";
        public const string ButtonConfirmHoverPath = ArtFolder + "mine-reset-button-confirm-hover.png";
        public const string TitleMaterialPath = ArtFolder + "MineResetTitleGlow.mat";
        public const string CostMaterialPath = ArtFolder + "MineResetCostGlow.mat";
        public const string MotePath = "Assets/_Project/Art/UI/MainMenu/Settings/particle-dot.png";
        public const string FontPath = "Assets/_Project/Fonts/NotoSansKR-Regular_SDF.asset";

        public static readonly string[] CaveGlowPaths =
        {
            ArtFolder + "mine-reset-cave-glow-0.png",
            ArtFolder + "mine-reset-cave-glow-1.png",
            ArtFolder + "mine-reset-cave-glow-2.png"
        };

        // 컨셉 1px = 1.148 단위(1920 기준). 아래 좌표는 make_init_mine_popup_art.py 의 init_mine_popup_layout.json 값이다.
        public static readonly Vector2 CardSize = new Vector2(1332f, 1021f);
        public static readonly Vector4 CaveRect = new Vector4(0f, 152.2f, 1272.3f, 337.6f);
        public static readonly Vector4[] CaveGlowRects =
        {
            new Vector4(361.7f, 51.7f, 147f, 136.7f),
            new Vector4(-418.6f, 29.3f, 79.2f, 89.6f),
            new Vector4(-404.8f, 66f, 67.8f, 75.8f)
        };
        public static readonly Vector2 HexCenter = new Vector2(0f, 134.9f);
        public static readonly Vector2 HexMineSize = new Vector2(427.2f, 323.8f);
        public static readonly Vector4 HexBorderRect = new Vector4(-0.6f, 0.6f, 556.9f, 453.6f);
        public static readonly Vector4 HexRingsRect = new Vector4(0f, 16.1f, 643.1f, 321.5f);
        public static readonly Vector4 TitleDividerRect = new Vector4(0f, 371.5f, 840.6f, 23f);
        public static readonly Vector4 CostPlateRect = new Vector4(0f, -67.8f, 1001.3f, 132.1f);
        public static readonly Vector4 ResetPlateRect = new Vector4(-300.9f, -203.8f, 592.5f, 133.2f);
        public static readonly Vector4 KeepPlateRect = new Vector4(300.9f, -203.8f, 592.5f, 133.2f);
        public static readonly Vector4 TimerPlateRect = new Vector4(0f, -314.1f, 1192f, 82.7f);
        public static readonly Vector4 CancelButtonRect = new Vector4(-275.6f, -414f, 445.1f, 93.7f);
        public static readonly Vector4 ConfirmButtonRect = new Vector4(274.4f, -414f, 458.7f, 97.6f);
        public static readonly Vector2 GoldIconSize = new Vector2(110.2f, 73.5f);
        public static readonly Vector2 BadgeSize = new Vector2(112.5f, 101.1f);

        public const float TitleY = 421.9f;
        public const float DescriptionY = 341.5f;
        public const float TitleFontSize = 60f;
        public const float DescriptionFontSize = 29f;
        public const float CostFontSize = 60f;
        public const float BalanceFontSize = 27f;
        public const float RowTitleFontSize = 29f;
        public const float RowDescFontSize = 25f;
        public const float TimerTitleFontSize = 30f;
        public const float ButtonFontSize = 28f;
        // 안내 패널 안 글자 시작점(패널 중심 기준)과 줄 간격
        public const float RowTextLeft = -144.5f;
        public const float RowTextWidth = 410f;
        public const float RowTitleOffsetY = 21.8f;
        public const float RowDescOffsetY = -21.8f;
        public const float BackdropAlpha = 0.78f;
        public const int MoteCount = 10;

        private static readonly Color TitleColor = new Color(0.84f, 0.98f, 1f, 1f);
        private static readonly Color Cyan = new Color(0.42f, 0.94f, 1f, 1f);
        private static readonly Color DescriptionColor = new Color(0.8f, 0.88f, 0.92f, 1f);
        private static readonly Color RowDescColor = new Color(0.86f, 0.93f, 0.95f, 1f);

        [MenuItem("SubTerra/UI/Build Mine Reset (SurfaceBase only)")]
        public static void BuildFromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        [MenuItem("SubTerra/UI/Build Prompt-B 123-2 Mine Reset Popup (SurfaceBase only)")]
        public static void BuildPromptB1232FromMenu()
        {
            Debug.Log("[SubTerra] " + Build());
        }

        public static string Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SurfaceBasePrefabPath) == null)
            {
                return "SKIP: SurfaceBase prefab missing";
            }

            EnsureArtImportSettings();
            var root = PrefabUtility.LoadPrefabContents(SurfaceBasePrefabPath);
            try
            {
                var content = root.transform.Find("SurfaceBaseContent") ?? root.transform;
                var resetButton = content.Find("ResetMineButton")?.GetComponent<UnityEngine.UI.Button>() ?? EnsureButton(
                    content,
                    "ResetMineButton",
                    new Vector2(0f, ResetButtonY),
                    new Vector2(320f, 48f),
                    string.Format(LocalizationService.Get("mine_reset.button"), MineResetService.GetFeeGold(0)));

                var confirmRoot = EnsureConfirmModal(root.transform, out var refs);

                var view = root.GetComponent<SurfaceBaseView>();
                if (view == null)
                {
                    view = root.AddComponent<SurfaceBaseView>();
                }

                var so = new SerializedObject(view);
                so.FindProperty("resetMineButton").objectReferenceValue = resetButton;
                so.FindProperty("resetMineConfirmRoot").objectReferenceValue = confirmRoot;
                so.FindProperty("resetMineConfirmTitleText").objectReferenceValue = refs.Title;
                so.FindProperty("resetMineConfirmBodyText").objectReferenceValue = refs.Description;
                so.FindProperty("resetMineConfirmCostText").objectReferenceValue = refs.Cost;
                so.FindProperty("resetMineConfirmBalanceText").objectReferenceValue = refs.Balance;
                so.FindProperty("resetMineConfirmYesButton").objectReferenceValue = refs.Yes;
                so.FindProperty("resetMineConfirmNoButton").objectReferenceValue = refs.No;
                so.FindProperty("resetMineConfirmAccentRoot").objectReferenceValue = refs.AccentRoot;
                so.FindProperty("resetMineConfirmMotion").objectReferenceValue = refs.Motion;
                AssignArray(so.FindProperty("resetMineConfirmRowTitleTexts"), refs.RowTitles);
                AssignArray(so.FindProperty("resetMineConfirmRowDescTexts"), refs.RowDescs);
                so.ApplyModifiedPropertiesWithoutUndo();

                // 저장 상태는 완전히 열린 정지 화면. 실행 중에는 열 때마다 처음부터 연출한다.
                refs.Motion.SnapOpen();
                refs.Card.localScale = Vector3.one;
                confirmRoot.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, SurfaceBasePrefabPath);
                return "SurfaceBasePrefab mine reset controls (prompt-B 123-2 popup)";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private sealed class ConfirmRefs
        {
            public RectTransform Card;
            public TMP_Text Title;
            public TMP_Text Description;
            public TMP_Text Cost;
            public TMP_Text Balance;
            public TMP_Text[] RowTitles;
            public TMP_Text[] RowDescs;
            public Button Yes;
            public Button No;
            public GameObject AccentRoot;
            public MineResetPopupMotion Motion;
        }

        private static void AssignArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        /// <summary>이번 팝업 전용 그림 폴더의 상수 경로만 스프라이트로 가져온다.</summary>
        private static void EnsureArtImportSettings()
        {
            var paths = new System.Collections.Generic.List<string>
            {
                FramePath, FrameGlowPath, PanelPath, CavePath, HexMinePath, HexBorderPath, HexBorderGlowPath,
                HexCrystalGlowPath, HexTunnelGlowPath, HexRingsPath, CoreGlowPath, ScanLinePath, TitleDividerPath,
                CostPlatePath, InfoPlatePath, TimerPlatePath, ResetIconPath, KeepIconPath,
                TimerIconPath, TimerDividerPath, ButtonCancelPath, ButtonCancelHoverPath, ButtonConfirmPath,
                ButtonConfirmHoverPath
            };
            paths.AddRange(CaveGlowPaths);
            foreach (var path in paths)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning("[SubTerra] Mine reset art missing: " + path);
                    continue;
                }

                var changed = importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single
                    || importer.mipmapEnabled
                    || !importer.alphaIsTransparency
                    || importer.maxTextureSize != 4096
                    || importer.textureCompression != TextureImporterCompression.CompressedHQ
                    || importer.wrapMode != TextureWrapMode.Clamp;
                if (!changed)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 4096;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        private static GameObject EnsureConfirmModal(Transform root, out ConfirmRefs refs)
        {
            refs = new ConfirmRefs();
            var existing = root.Find("ResetMineConfirm");
            var modal = existing != null
                ? existing.gameObject
                : new GameObject(
                    "ResetMineConfirm",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(GraphicRaycaster),
                    typeof(Image));
            if (existing == null)
            {
                modal.transform.SetParent(root, false);
            }

            var modalRect = modal.GetComponent<RectTransform>();
            Stretch(modalRect);
            var backdrop = modal.GetComponent<Image>();
            backdrop.sprite = null;
            backdrop.color = new Color(0.01f, 0.015f, 0.025f, BackdropAlpha);
            backdrop.raycastTarget = true;
            var canvas = modal.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 700;
            var rootGroup = GetOrAdd<CanvasGroup>(modal);
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;

            // 이전 개편의 카드(프레임 한 장 + 행 3개)는 레이어 구조가 달라 통째로 다시 만든다.
            var oldCard = modal.transform.Find("ResetMineCard");
            if (oldCard != null)
            {
                Object.DestroyImmediate(oldCard.gameObject);
            }

            var card = EnsureChild(modal.transform, "ResetMineCard");
            Place(card, Vector2.zero, CardSize);
            refs.Card = card;

            // 1) 펼쳐지는 패널 본체: RectMask2D 높이만 바뀌고 안의 그림은 제자리·제크기다.
            var body = EnsureChild(card, "Body", typeof(RectMask2D));
            Place(body, Vector2.zero, CardSize);
            var panel = AddImage(body, "Panel", PanelPath, Vector2.zero, CardSize);
            var cave = AddImage(body, "Cave", CavePath, new Vector2(CaveRect.x, CaveRect.y), new Vector2(CaveRect.z, CaveRect.w));
            var caveGlowRoot = EnsureChild(body, "CaveGlows");
            Place(caveGlowRoot, Vector2.zero, CardSize);
            var caveGlows = new Image[CaveGlowPaths.Length];
            for (var i = 0; i < CaveGlowPaths.Length; i++)
            {
                var r = CaveGlowRects[i];
                caveGlows[i] = AddImage(caveGlowRoot, "CaveGlow" + i, CaveGlowPaths[i], new Vector2(r.x, r.y), new Vector2(r.z, r.w));
            }

            AddImage(body, "Frame", FramePath, Vector2.zero, CardSize);
            var frameFlash = AddImage(body, "FrameFlash", FrameGlowPath, Vector2.zero, CardSize);
            panel.color = Color.white;
            cave.color = Color.white;

            // 2) TV 켜짐용 가로 빛과, 펼쳐지는 위아래 가장자리 빛
            var scan = AddImage(card, "ScanLine", ScanLinePath, Vector2.zero, new Vector2(CardSize.x, 30f));
            var edgeTop = AddImage(card, "EdgeTop", ScanLinePath, new Vector2(0f, CardSize.y / 2f), new Vector2(CardSize.x * 0.96f, 26f));
            var edgeBottom = AddImage(card, "EdgeBottom", ScanLinePath, new Vector2(0f, -CardSize.y / 2f), new Vector2(CardSize.x * 0.96f, 26f));

            // 3) 중앙 육각형: 링·입구·발광·테두리. HexScale만 균일 배율로 키운다.
            var hex = EnsureChild(card, "Hex");
            Place(hex, HexCenter, new Vector2(HexRingsRect.z, HexBorderRect.w));
            var rings = AddImage(hex, "HexRings", HexRingsPath, new Vector2(HexRingsRect.x, HexRingsRect.y), new Vector2(HexRingsRect.z, HexRingsRect.w));
            var hexScale = EnsureChild(hex, "HexScale");
            Place(hexScale, Vector2.zero, new Vector2(HexBorderRect.z, HexBorderRect.w));
            var hexMine = AddImage(hexScale, "HexMine", HexMinePath, Vector2.zero, HexMineSize);
            var tunnelGlow = AddImage(hexScale, "HexTunnelGlow", HexTunnelGlowPath, Vector2.zero, HexMineSize);
            var crystalGlow = AddImage(hexScale, "HexCrystalGlow", HexCrystalGlowPath, Vector2.zero, HexMineSize);
            var borderGlow = AddImage(hexScale, "HexBorderGlow", HexBorderGlowPath, new Vector2(HexBorderRect.x, HexBorderRect.y), new Vector2(HexBorderRect.z, HexBorderRect.w));
            var border = AddImage(hexScale, "HexBorder", HexBorderPath, new Vector2(HexBorderRect.x, HexBorderRect.y), new Vector2(HexBorderRect.z, HexBorderRect.w));
            var core = AddImage(hex, "CoreGlow", CoreGlowPath, Vector2.zero, new Vector2(240f, 120f));
            var moteRoot = EnsureChild(hex, "Motes");
            Place(moteRoot, Vector2.zero, new Vector2(HexRingsRect.z, HexBorderRect.w));
            var motes = new Image[MoteCount];
            for (var i = 0; i < MoteCount; i++)
            {
                var size = 5f + (i % 3) * 2f;
                motes[i] = AddImage(moteRoot, "Mote" + i, MotePath, Vector2.zero, new Vector2(size, size));
                motes[i].color = new Color(0.55f, 0.95f, 1f, 0f);
            }

            // 4) 본문(글자·패널·버튼): 알파만 페이드한다.
            var contentRect = EnsureChild(card, "Content", typeof(CanvasGroup));
            Place(contentRect, Vector2.zero, CardSize);
            var contentGroup = contentRect.GetComponent<CanvasGroup>();
            BuildHeader(contentRect, refs);
            BuildCostPanel(contentRect, refs);
            BuildInfoRows(contentRect, refs);
            BuildButtons(contentRect, refs);

            var motion = GetOrAdd<MineResetPopupMotion>(modal);
            var mso = new SerializedObject(motion);
            mso.FindProperty("rootGroup").objectReferenceValue = rootGroup;
            mso.FindProperty("backdrop").objectReferenceValue = backdrop;
            mso.FindProperty("backdropAlpha").floatValue = BackdropAlpha;
            mso.FindProperty("card").objectReferenceValue = card;
            mso.FindProperty("body").objectReferenceValue = body;
            mso.FindProperty("scanLine").objectReferenceValue = scan;
            mso.FindProperty("edgeTop").objectReferenceValue = edgeTop;
            mso.FindProperty("edgeBottom").objectReferenceValue = edgeBottom;
            mso.FindProperty("frameFlash").objectReferenceValue = frameFlash;
            mso.FindProperty("content").objectReferenceValue = contentGroup;
            mso.FindProperty("hexScale").objectReferenceValue = hexScale;
            mso.FindProperty("hexMine").objectReferenceValue = hexMine;
            mso.FindProperty("hexBorder").objectReferenceValue = border;
            mso.FindProperty("hexBorderGlow").objectReferenceValue = borderGlow;
            mso.FindProperty("hexCrystalGlow").objectReferenceValue = crystalGlow;
            mso.FindProperty("hexTunnelGlow").objectReferenceValue = tunnelGlow;
            mso.FindProperty("hexRings").objectReferenceValue = rings;
            mso.FindProperty("coreGlow").objectReferenceValue = core;
            AssignArray(mso.FindProperty("caveGlows"), caveGlows);
            AssignArray(mso.FindProperty("motes"), motes);
            mso.ApplyModifiedPropertiesWithoutUndo();
            refs.Motion = motion;
            return modal;
        }

        private static void BuildHeader(Transform content, ConfirmRefs refs)
        {
            refs.Title = EnsureText(content, "Title", new Vector2(0f, TitleY), new Vector2(900f, 96f), TitleFontSize,
                LocalizationService.Get("mine_reset.confirm.title"));
            refs.Title.color = TitleColor;
            refs.Title.fontStyle = FontStyles.Bold;
            refs.Title.characterSpacing = 4f;
            refs.Title.textWrappingMode = TextWrappingModes.NoWrap;
            refs.Title.enableAutoSizing = true;
            refs.Title.fontSizeMin = 40f;
            refs.Title.fontSizeMax = TitleFontSize;
            ApplyGlowMaterial(refs.Title, TitleMaterialPath, new Color(0.05f, 0.8f, 1f, 0.55f));

            AddImage(content, "TitleDivider", TitleDividerPath,
                new Vector2(TitleDividerRect.x, TitleDividerRect.y), new Vector2(TitleDividerRect.z, TitleDividerRect.w));

            refs.Description = EnsureText(content, "Description", new Vector2(0f, DescriptionY), new Vector2(1000f, 44f),
                DescriptionFontSize, LocalizationService.Get("mine_reset.confirm.desc"));
            refs.Description.color = DescriptionColor;
            refs.Description.enableAutoSizing = true;
            refs.Description.fontSizeMin = 18f;
            refs.Description.fontSizeMax = DescriptionFontSize;
            refs.Description.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void BuildCostPanel(Transform content, ConfirmRefs refs)
        {
            var panel = AddImage(content, "CostPanel", CostPlatePath,
                new Vector2(CostPlateRect.x, CostPlateRect.y), new Vector2(CostPlateRect.z, CostPlateRect.w)).rectTransform;

            var amountRow = EnsureChild(panel, "AmountRow", typeof(HorizontalLayoutGroup));
            Place(amountRow, new Vector2(0f, 14f), new Vector2(880f, 92f));
            var layout = amountRow.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 30f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var gold = AddImage(amountRow, "GoldIcon", GoldIconPath, Vector2.zero, GoldIconSize);
            gold.preserveAspect = true;
            var goldLayout = gold.gameObject.AddComponent<LayoutElement>();
            goldLayout.preferredWidth = GoldIconSize.x;
            goldLayout.preferredHeight = GoldIconSize.y;

            refs.Cost = EnsureText(amountRow, "CostText", Vector2.zero, new Vector2(300f, 92f), CostFontSize,
                SurfaceBaseView.FormatGold("mine_reset.confirm.cost", MineResetService.GetFeeGold(0)));
            refs.Cost.color = new Color(1f, 0.9f, 0.42f, 1f);
            refs.Cost.fontStyle = FontStyles.Bold;
            refs.Cost.textWrappingMode = TextWrappingModes.NoWrap;
            refs.Cost.alignment = TextAlignmentOptions.MidlineLeft;
            ApplyGlowMaterial(refs.Cost, CostMaterialPath, new Color(1f, 0.62f, 0.08f, 0.35f));

            refs.Balance = EnsureText(panel, "BalanceText", new Vector2(0f, -36.1f), new Vector2(880f, 40f),
                BalanceFontSize, string.Empty);
            refs.Balance.color = new Color(0.78f, 0.88f, 0.9f, 1f);
            refs.Balance.enableAutoSizing = true;
            refs.Balance.fontSizeMin = 15f;
            refs.Balance.fontSizeMax = BalanceFontSize;
            refs.Balance.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void BuildInfoRows(Transform content, ConfirmRefs refs)
        {
            refs.RowTitles = new TMP_Text[3];
            refs.RowDescs = new TMP_Text[3];
            var plates = new[] { ResetPlateRect, KeepPlateRect };
            var badges = new[] { ResetIconPath, KeepIconPath };
            var names = new[] { "ResetRow", "KeepRow" };
            var titleKeys = new[] { "mine_reset.confirm.reset.title", "mine_reset.confirm.keep.title" };
            var descKeys = new[] { "mine_reset.confirm.reset.desc", "mine_reset.confirm.keep.desc" };
            for (var i = 0; i < 2; i++)
            {
                var r = plates[i];
                var row = AddImage(content, names[i], InfoPlatePath, new Vector2(r.x, r.y), new Vector2(r.z, r.w)).rectTransform;
                var badge = AddImage(row, "Icon", badges[i], new Vector2(-214.7f, -1.2f), BadgeSize);
                if (i == 1) badge.preserveAspect = true;

                var title = EnsureText(row, "Title", new Vector2(RowTextLeft + RowTextWidth / 2f, RowTitleOffsetY),
                    new Vector2(RowTextWidth, 46f), RowTitleFontSize, LocalizationService.Get(titleKeys[i]));
                title.color = Cyan;
                title.fontStyle = FontStyles.Bold;
                title.alignment = TextAlignmentOptions.MidlineLeft;
                title.textWrappingMode = TextWrappingModes.NoWrap;
                title.enableAutoSizing = true;
                title.fontSizeMin = 20f;
                title.fontSizeMax = RowTitleFontSize;

                var desc = EnsureText(row, "Desc", new Vector2(RowTextLeft + RowTextWidth / 2f, RowDescOffsetY),
                    new Vector2(RowTextWidth, 38f), RowDescFontSize, LocalizationService.Get(descKeys[i]));
                desc.color = RowDescColor;
                desc.alignment = TextAlignmentOptions.MidlineLeft;
                desc.textWrappingMode = TextWrappingModes.NoWrap;
                desc.enableAutoSizing = true;
                desc.fontSizeMin = 15f;
                desc.fontSizeMax = RowDescFontSize;

                refs.RowTitles[i] = title;
                refs.RowDescs[i] = desc;
            }

            // 탐사 시간: 아이콘·제목 | 설명을 가운데 정렬한 한 줄 패널
            var timer = AddImage(content, "TimerRow", TimerPlatePath,
                new Vector2(TimerPlateRect.x, TimerPlateRect.y), new Vector2(TimerPlateRect.z, TimerPlateRect.w)).rectTransform;
            var group = EnsureChild(timer, "Group", typeof(HorizontalLayoutGroup));
            Place(group, new Vector2(0f, -1f), new Vector2(1100f, 60f));
            var layout = group.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 20f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var icon = AddImage(group, "Icon", TimerIconPath, Vector2.zero, new Vector2(52f, 52f));
            icon.preserveAspect = true;
            var iconLayout = icon.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 52f;

            var timerTitle = EnsureText(group, "Title", Vector2.zero, new Vector2(200f, 46f), TimerTitleFontSize,
                LocalizationService.Get("mine_reset.confirm.timer.title"));
            timerTitle.color = Cyan;
            timerTitle.fontStyle = FontStyles.Bold;
            timerTitle.textWrappingMode = TextWrappingModes.NoWrap;
            timerTitle.alignment = TextAlignmentOptions.Midline;

            var divider = AddImage(group, "Divider", TimerDividerPath, Vector2.zero, new Vector2(14f, 40f));
            divider.preserveAspect = true;
            var dividerLayout = divider.gameObject.AddComponent<LayoutElement>();
            dividerLayout.preferredWidth = 52f;

            var timerDesc = EnsureText(group, "Desc", Vector2.zero, new Vector2(400f, 38f), RowDescFontSize,
                LocalizationService.Get("mine_reset.confirm.timer.desc"));
            timerDesc.color = RowDescColor;
            timerDesc.textWrappingMode = TextWrappingModes.NoWrap;
            timerDesc.alignment = TextAlignmentOptions.Midline;

            refs.RowTitles[2] = timerTitle;
            refs.RowDescs[2] = timerDesc;
        }

        private static void BuildButtons(Transform content, ConfirmRefs refs)
        {
            refs.No = EnsureButton(content, "CancelButton",
                new Vector2(CancelButtonRect.x, CancelButtonRect.y), new Vector2(CancelButtonRect.z, CancelButtonRect.w),
                LocalizationService.Get("mine_reset.confirm.no"));
            refs.Yes = EnsureButton(content, "ConfirmButton",
                new Vector2(ConfirmButtonRect.x, ConfirmButtonRect.y), new Vector2(ConfirmButtonRect.z, ConfirmButtonRect.w),
                SurfaceBaseView.FormatGold("mine_reset.confirm.create", MineResetService.GetFeeGold(0)));
            SkinButton(refs.No, ButtonCancelPath, ButtonCancelHoverPath, null);
            refs.AccentRoot = SkinButton(refs.Yes, ButtonCancelPath, ButtonConfirmHoverPath, ButtonConfirmPath);
        }

        /// <summary>
        /// 설정·덮어쓰기 팝업과 같은 hover 오버레이 페이드(MenuSpriteButtonSkin)를 쓴다.
        /// accent가 있으면 청록 실행 버튼 그림을 AccentRoot에 얹고, 골드 부족 시 뷰가 AccentRoot를 꺼 회색 버튼만 남긴다.
        /// </summary>
        private static GameObject SkinButton(Button button, string basePath, string hoverPath, string accentPath)
        {
            var image = button.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(basePath);
            image.type = Image.Type.Simple;
            image.color = accentPath != null ? new Color(0.78f, 0.8f, 0.82f, 1f) : Color.white;
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.spriteState = default;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            Transform overlayParent = button.transform;
            RectTransform accentRoot = null;
            if (accentPath != null)
            {
                accentRoot = EnsureChild(button.transform, "AccentRoot");
                Stretch(accentRoot);
                var glow = EnsureChild(accentRoot, "AccentGlow", typeof(Image));
                Stretch(glow);
                var glowImage = glow.GetComponent<Image>();
                glowImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(accentPath);
                glowImage.type = Image.Type.Simple;
                glowImage.color = Color.white;
                glowImage.raycastTarget = false;
                overlayParent = accentRoot;
            }

            var overlayRect = EnsureChild(overlayParent, "HoverOverlay", typeof(Image));
            Stretch(overlayRect);
            var overlay = overlayRect.GetComponent<Image>();
            overlay.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(hoverPath);
            overlay.type = Image.Type.Simple;
            overlay.color = new Color(1f, 1f, 1f, 0f);
            overlay.raycastTarget = false;

            var skin = GetOrAdd<MenuSpriteButtonSkin>(button.gameObject);
            var skinObject = new SerializedObject(skin);
            skinObject.FindProperty("overlay").objectReferenceValue = overlay;
            skinObject.ApplyModifiedPropertiesWithoutUndo();

            if (accentRoot != null)
            {
                accentRoot.SetAsFirstSibling();
            }
            else
            {
                overlayRect.SetAsFirstSibling();
            }

            var label = button.transform.Find("Label");
            if (label != null)
            {
                label.SetAsLastSibling();
                var text = label.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.fontSize = ButtonFontSize;
                    text.fontStyle = FontStyles.Bold;
                    text.enableAutoSizing = true;
                    text.fontSizeMin = 20f;
                    text.fontSizeMax = ButtonFontSize;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    ((RectTransform)label).sizeDelta = new Vector2(((RectTransform)button.transform).sizeDelta.x - 90f, 56f);
                }
            }

            return accentRoot != null ? accentRoot.gameObject : null;
        }

        /// <summary>TMP 글자 뒤 은은한 발광. 폰트 에셋은 건드리지 않고 별도 머티리얼 프리셋만 만든다.</summary>
        private static void ApplyGlowMaterial(TMP_Text text, string path, Color glow)
        {
            var font = text.font;
            if (font == null || font.material == null)
            {
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(font.material) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = font.material.shader;
            material.SetTexture("_MainTex", font.material.GetTexture("_MainTex"));
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", glow);
            material.SetFloat("_UnderlayOffsetX", 0f);
            material.SetFloat("_UnderlayOffsetY", 0f);
            material.SetFloat("_UnderlayDilate", 0.12f);
            material.SetFloat("_UnderlaySoftness", 0.55f);
            EditorUtility.SetDirty(material);
            text.fontSharedMaterial = material;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.AddComponent<T>();
            }

            return component;
        }

        private static Image AddImage(Transform parent, string name, string spritePath, Vector2 position, Vector2 size)
        {
            var rect = EnsureChild(parent, name, typeof(Image));
            Place(rect, position, size);
            var image = rect.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (image.sprite == null)
            {
                Debug.LogWarning("[SubTerra] Mine reset sprite missing: " + spritePath);
            }

            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            // 장식·발광 레이어는 입력을 가로채지 않는다.
            image.raycastTarget = false;
            return image;
        }

        private static Button EnsureButton(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            string label)
        {
            var rect = EnsureChild(parent, name, typeof(Image), typeof(Button));
            Place(rect, position, size);
            var image = rect.GetComponent<Image>();
            image.color = new Color(0.12f, 0.36f, 0.31f, 1f);
            image.raycastTarget = true;
            var button = rect.GetComponent<Button>();
            button.targetGraphic = image;
            EnsureText(rect, "Label", Vector2.zero, size - new Vector2(20f, 8f), 20f, label);
            return button;
        }

        private static RectTransform EnsureChild(Transform parent, string name, params System.Type[] components)
        {
            var found = parent.Find(name) as RectTransform;
            if (found == null)
            {
                var types = new System.Type[components.Length + 1];
                types[0] = typeof(RectTransform);
                components.CopyTo(types, 1);
                var go = new GameObject(name, types);
                go.layer = parent.gameObject.layer;
                go.transform.SetParent(parent, false);
                found = go.GetComponent<RectTransform>();
            }

            for (var i = 0; i < components.Length; i++)
            {
                if (found.GetComponent(components[i]) == null)
                {
                    found.gameObject.AddComponent(components[i]);
                }
            }

            return found;
        }

        private static TMP_Text EnsureText(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            float fontSize,
            string value)
        {
            var rect = EnsureChild(parent, name, typeof(TextMeshProUGUI));
            Place(rect, position, size);
            var text = rect.GetComponent<TextMeshProUGUI>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null && text.font != font)
            {
                text.font = font;
            }

            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
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
