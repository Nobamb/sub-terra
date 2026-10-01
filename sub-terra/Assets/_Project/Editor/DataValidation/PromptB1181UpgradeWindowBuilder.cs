using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// prompt-B 118-1: 업그레이드 창 통일(지상 기지 창 기준) + 설정창 형태 프레임 + 설정창과 같은 정사각 X 버튼
    /// + 노드 호버 빛·아이콘 연출 파츠. 트리 구성은 PromptB118UpgradeTreeBuilder가 하고, 이 클래스는 거기서 호출된다.
    /// 메뉴는 B-118 빌드 진입점을 그대로 실행한다(대상: SurfaceBasePanel.prefab, Mine_Demo_Integration.unity의 UpgradePanel).
    /// </summary>
    public static class PromptB1181UpgradeWindowBuilder
    {
        public const string ArtFolder = PromptB118UpgradeTreeBuilder.ArtFolder;
        public const string FramePath = ArtFolder + "upgrade-window-frame.png";
        public const string GasIconPath = ArtFolder + "Icons/upgrade-icon-gas.png";
        public const string GasHandPath = ArtFolder + "Icons/upgrade-icon-gas-hand.png";
        public const string BoxBodyPath = ArtFolder + "Icons/upgrade-icon-box-body.png";
        public const string BoxLidPath = ArtFolder + "Icons/upgrade-icon-box-lid.png";
        public const string PlugLitPath = ArtFolder + "Icons/upgrade-icon-plug-lit.png";
        public const string SparkPath = ArtFolder + "Fx/upgrade-fx-spark.png";
        public const string RingPath = ArtFolder + "Fx/upgrade-fx-ring.png";
        public const string ShardPath = ArtFolder + "Fx/upgrade-fx-shard.png";
        public const string CoinPath = ArtFolder + "Fx/upgrade-fx-coin.png";
        public const string PlusPath = ArtFolder + "Fx/upgrade-fx-plus.png";
        public const string CloseNormalPath = PromptB104SettingsMenuBuilder.CloseNormalPath;
        public const string CloseHoverPath = PromptB104SettingsMenuBuilder.CloseHoverPath;
        private const string SettingsArt = "Assets/_Project/Art/UI/MainMenu/Settings/";

        /// <summary>지상 기지 업그레이드 창 크기. 지하 창도 같은 크기로 맞춘다.</summary>
        public static readonly Vector2 WindowSize = new Vector2(1500f, 820f);

        /// <summary>기존 닫기 버튼(72×58)의 세로 길이를 한 변으로 쓰는 정사각형.</summary>
        public const float CloseSize = 58f;
        public static readonly Vector2 ClosePosition = new Vector2(-62f, -48f);

        /// <summary>프레임 9-slice 경계(L,B,R,T). 모서리 사선과 옆면 홈이 늘어나지 않도록 넉넉히 잡는다.</summary>
        public static readonly Vector4 FrameBorder = new Vector4(200f, 330f, 200f, 290f);

        /// <summary>설정창(1448px 그림을 1080 크기로 표시)과 같은 선 굵기가 되도록 줄여 그린다.</summary>
        public const float FramePixelsPerUnit = 1.34f;

        public static readonly Vector2 IconCenter = new Vector2(0f, -42f);
        public static readonly Vector2 IconSize = new Vector2(60f, 60f);

        /// <summary>상자 뚜껑이 열릴 때 도는 뒤쪽 경첩 위치(아이콘 그림 기준 정규화 좌표).</summary>
        private static readonly Vector2 LidHinge = new Vector2(0.93f, 0.66f);

        // ------------------------------------------------------------------ 메뉴

        [MenuItem("SubTerra/UI/Build Prompt-B 118-1 Upgrade Window (Surface Base)")]
        public static void BuildSurfaceBaseFromMenu() => Debug.Log(PromptB118UpgradeTreeBuilder.BuildSurfaceBase());

        [MenuItem("SubTerra/UI/Build Prompt-B 118-1 Upgrade Window (Mine Integration)")]
        public static void BuildMineIntegrationFromMenu() => Debug.Log(PromptB118UpgradeTreeBuilder.BuildMineIntegration());

        // ------------------------------------------------------------------ 에셋

        internal static void EnsureArt()
        {
            PromptB118UpgradeTreeBuilder.ImportSprite(FramePath, FrameBorder);
            foreach (var path in new[]
                     {
                         GasIconPath, GasHandPath, BoxBodyPath, BoxLidPath, PlugLitPath,
                         SparkPath, RingPath, ShardPath, CoinPath, PlusPath
                     })
            {
                PromptB118UpgradeTreeBuilder.ImportSprite(path, Vector4.zero);
            }
        }

        // ------------------------------------------------------------------ 창 외형

        /// <summary>
        /// 창 카드에 설정창 형태 프레임을 입히고 닫기 버튼을 설정창 X 버튼으로 바꾼다.
        /// 지하 창(mine)은 지상 기지 창과 같은 크기·위치·구매 버튼 모양으로 맞춘다. 버튼의 onClick 배선은 건드리지 않는다.
        /// </summary>
        internal static void ApplyWindowChrome(RectTransform panel, bool mine)
        {
            if (mine)
            {
                PromptB118UpgradeTreeBuilder.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), Vector2.zero, WindowSize);
                panel.localScale = Vector3.one;
            }

            var frame = panel.GetComponent<Image>();
            if (frame == null)
            {
                frame = panel.gameObject.AddComponent<Image>();
            }

            frame.sprite = PromptB118UpgradeTreeBuilder.LoadSprite(FramePath);
            frame.type = Image.Type.Sliced;
            frame.fillCenter = true;
            frame.pixelsPerUnitMultiplier = FramePixelsPerUnit;
            frame.color = Color.white;
            frame.raycastTarget = true;

            var close = panel.Find(mine ? "CloseButton" : "CloseUpgradeButton");
            if (close == null)
            {
                throw new System.InvalidOperationException("Close button not found under " + panel.name);
            }

            SkinCloseButton((RectTransform)close);

            if (mine)
            {
                var purchase = panel.Find("PurchaseButton");
                if (purchase != null)
                {
                    // 지상 기지 구매 버튼과 같은 설정창 버튼 그림 + 호버 연출.
                    PromptB117SurfaceBaseBuilder.StyleButton(purchase.GetComponent<Button>(),
                        SettingsArt + "button-active-off.png", SettingsArt + "button-active-on.png", new Vector2(280f, 62f));
                }
            }
            else if (panel.parent != null && panel.parent.GetComponent<UpgradeTreeScrollForwarder>() == null)
            {
                // 지상 기지 모달의 어두운 배경 위 스크롤도 확대/축소로 넘긴다.
                panel.parent.gameObject.AddComponent<UpgradeTreeScrollForwarder>();
            }

            EditorUtility.SetDirty(panel.gameObject);
        }

        /// <summary>설정창 SettingsClose와 같은 X 그림 + 호버 시 청록 X로 바뀌는 오버레이.</summary>
        private static void SkinCloseButton(RectTransform close)
        {
            foreach (var name in new[] { "Frame", "HoverFrame", "InnerLight", "HoverParticles", "Label", "HoverOverlay" })
            {
                var child = close.Find(name);
                if (child != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            var feedback = close.GetComponent<SurfaceBaseButtonFeedback>();
            if (feedback != null)
            {
                Object.DestroyImmediate(feedback);
            }

            var outline = close.GetComponent<Outline>();
            if (outline != null)
            {
                Object.DestroyImmediate(outline);
            }

            PromptB118UpgradeTreeBuilder.SetRect(close, Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f),
                ClosePosition, new Vector2(CloseSize, CloseSize));
            close.localScale = Vector3.one;
            close.SetAsLastSibling();

            var image = close.GetComponent<Image>();
            if (image == null)
            {
                image = close.gameObject.AddComponent<Image>();
            }

            image.sprite = PromptB118UpgradeTreeBuilder.LoadSprite(CloseNormalPath);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = true;

            var hover = PromptB118UpgradeTreeBuilder.NewImage("HoverOverlay", close,
                PromptB118UpgradeTreeBuilder.LoadSprite(CloseHoverPath), Color.white);
            PromptB118UpgradeTreeBuilder.Stretch(hover.rectTransform);
            hover.preserveAspect = true;

            var button = close.GetComponent<Button>();
            button.targetGraphic = hover;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.85f, 0.95f, 1f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.15f;
            button.colors = colors;
        }

        // ------------------------------------------------------------------ 노드 호버

        /// <summary>노드 가운데의 은은한 청록 빛. 기본은 투명이며 호버할 때만 보인다.</summary>
        internal static Image AddHoverGlow(Transform parent, Sprite glowSprite)
        {
            var glow = PromptB118UpgradeTreeBuilder.NewImage("HoverGlow", parent, glowSprite, UpgradeTreeTween.Cyan);
            PromptB118UpgradeTreeBuilder.SetRect(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(150f, 150f));
            UpgradeTreeTween.SetAlpha(glow, 0f);
            return glow;
        }

        /// <summary>아이콘과 같은 자리·크기의 연출층(뒤/앞). 아이콘이 움직여도 연출 기준점은 고정된다.</summary>
        internal static RectTransform AddIconLayer(Transform body, string name)
        {
            var layer = PromptB118UpgradeTreeBuilder.NewRect(name, body);
            PromptB118UpgradeTreeBuilder.SetRect(layer, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 0.5f), IconCenter, IconSize);
            return layer;
        }

        internal static void AddHoverFx(
            RectTransform root,
            string upgradeId,
            Image icon,
            Image hoverGlow,
            Image blindGlow,
            RectTransform fxBack,
            RectTransform fxFront,
            Sprite glowSprite)
        {
            var kind = UpgradeIconFx.KindFor(upgradeId);
            var fx = root.gameObject.AddComponent<UpgradeNodeHoverFx>();
            fx.Configure(kind);

            Image overlayA = null;
            Image overlayB = null;
            Sprite particle = null;
            Sprite ring = null;
            Sprite[] items = System.Array.Empty<Sprite>();
            switch (kind)
            {
                case UpgradeIconFxKind.DrillEfficiency:
                    overlayA = Overlay(fxFront, "PlugLit", PlugLitPath, new Vector2(0.5f, 0.5f));
                    particle = Load(SparkPath);
                    break;
                case UpgradeIconFxKind.MaximumCargo:
                    overlayA = Overlay(fxFront, "BoxBody", BoxBodyPath, new Vector2(0.5f, 0.5f));
                    overlayB = Overlay(fxFront, "BoxLid", BoxLidPath, LidHinge);
                    items = CargoItems();
                    break;
                case UpgradeIconFxKind.CargoYield:
                    particle = Load(ShardPath);
                    break;
                case UpgradeIconFxKind.CargoGold:
                    particle = Load(CoinPath);
                    break;
                case UpgradeIconFxKind.DroneScan:
                    ring = Load(RingPath);
                    break;
                case UpgradeIconFxKind.DroneRescue:
                    items = CargoItems();
                    break;
                case UpgradeIconFxKind.GasResistance:
                    overlayA = Overlay(fxFront, "GasHand", GasHandPath, new Vector2(0.5f, 0.5f));
                    break;
                case UpgradeIconFxKind.MaximumEnergy:
                    particle = Load(SparkPath);
                    break;
                case UpgradeIconFxKind.MaximumHealth:
                case UpgradeIconFxKind.HealthRegeneration:
                    particle = Load(PlusPath);
                    break;
            }

            var so = new SerializedObject(fx);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("hoverGlow").objectReferenceValue = hoverGlow;
            so.FindProperty("blindHoverGlow").objectReferenceValue = blindGlow;
            so.FindProperty("backLayer").objectReferenceValue = fxBack;
            so.FindProperty("frontLayer").objectReferenceValue = fxFront;
            so.FindProperty("overlayA").objectReferenceValue = overlayA;
            so.FindProperty("overlayB").objectReferenceValue = overlayB;
            so.FindProperty("particleSprite").objectReferenceValue = particle;
            so.FindProperty("ringSprite").objectReferenceValue = ring;
            so.FindProperty("glowSprite").objectReferenceValue = glowSprite;
            var itemsProperty = so.FindProperty("itemSprites");
            itemsProperty.arraySize = items.Length;
            for (var i = 0; i < items.Length; i++)
            {
                itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Image Overlay(RectTransform layer, string name, string spritePath, Vector2 pivot)
        {
            var image = PromptB118UpgradeTreeBuilder.NewImage(name, layer, Load(spritePath), Color.white);
            PromptB118UpgradeTreeBuilder.Stretch(image.rectTransform);
            image.rectTransform.pivot = pivot;
            image.rectTransform.anchoredPosition = Vector2.zero;
            image.preserveAspect = true;
            UpgradeTreeTween.SetAlpha(image, 0f);
            image.enabled = false;
            return image;
        }

        private static Sprite[] CargoItems()
        {
            return new[]
            {
                Load("Assets/_Project/Art/Icons/icon_copper.png"),
                Load("Assets/_Project/Art/Icons/icon_iron.png"),
                Load("Assets/_Project/Art/Icons/icon_lithium.png")
            };
        }

        private static Sprite Load(string path)
        {
            return PromptB118UpgradeTreeBuilder.LoadSprite(path);
        }
    }
}
