using SubTerra.App.UI;
using SubTerra.App.UI.Outpost;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Editor.DataValidation
{
    /// <summary>
    /// 보건소·충전기 서비스 팝업(ServicePopup)을 OutpostPanel 프리팹에만 추가한다.
    /// 전진기지 코어·정산 콘솔·보관함이 쓰는 기존 PanelRoot 계층은 건드리지 않는다.
    /// 그림은 기존 '새 광산 구역' 팝업 프레임·패널과 HUD 게이지, 업그레이드 아이콘을 그대로 쓴다.
    /// </summary>
    public static class FacilityServicePopupBuilder
    {
        public const string OutpostPrefabPath = "Assets/_Project/Prefabs/UI/OutpostPanel.prefab";
        public const string PopupName = "ServicePopup";

        private const string MineResetArt = "Assets/_Project/Art/UI/SurfaceBase/MineReset/";
        private const string HudArt = "Assets/_Project/Art/UI/Gameplay/HUD/";
        private const string FramePath = MineResetArt + "mine-reset-frame.png";
        private const string PanelPath = MineResetArt + "mine-reset-panel.png";
        private const string HeartPath = "Assets/_Project/Art/UI/Upgrade/Icons/upgrade-icon-heart.png";
        private const string BoltPath = "Assets/_Project/Art/UI/Upgrade/Icons/upgrade-icon-bolt.png";
        private const string FontPath = "Assets/_Project/Fonts/NotoSansKR-Regular_SDF.asset";

        public static readonly Vector2 WindowSize = new Vector2(560f, 430f);
        public const float IconSlotY = 128f;
        public static readonly Vector2 IconSize = new Vector2(84f, 84f);
        public static readonly Vector2 GaugeSize = new Vector2(380f, 50f);

        private static readonly Color TitleColor = new Color(0.84f, 0.98f, 1f, 1f);
        private static readonly Color DescriptionColor = new Color(0.8f, 0.88f, 0.92f, 1f);
        private static readonly Color Cyan = new Color(0.42f, 0.94f, 1f, 1f);

        [MenuItem("SubTerra/UI/Build Facility Service Popup (OutpostPanel only)")]
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
                return "Facility service popup built: " + OutpostPrefabPath;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>OutpostPanel 루트에 ServicePopup을 (다시) 만들고 View에 연결한다.</summary>
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
            viewObject.FindProperty("servicePopup").objectReferenceValue =
                popup.GetComponent<FacilityServicePopupView>();
            viewObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildPopup(Transform parent, TMP_FontAsset font)
        {
            var window = NewRect(PopupName, parent, Vector2.zero, WindowSize);
            var windowGroup = window.gameObject.AddComponent<CanvasGroup>();
            window.gameObject.AddComponent<PopupWindowDrag>();
            var view = window.gameObject.AddComponent<FacilityServicePopupView>();

            // 연출 중에도 뒤쪽 게임 화면으로 클릭이 새지 않도록 창 전체를 덮는 투명 차단막.
            var shield = Stretch("InputShield", window);
            var shieldImage = shield.gameObject.AddComponent<Image>();
            shieldImage.color = new Color(0f, 0f, 0f, 0f);
            shieldImage.raycastTarget = true;

            var backdrop = Stretch("PanelBackdrop", window);
            var backdropGroup = backdrop.gameObject.AddComponent<CanvasGroup>();
            var panel = Stretch("Panel", backdrop);
            panel.offsetMin = new Vector2(16f, 16f);
            panel.offsetMax = new Vector2(-16f, -16f);
            AddImage(panel, LoadSprite(PanelPath), Color.white, false);
            var frame = Stretch("Frame", backdrop);
            AddImage(frame, LoadSprite(FramePath), Color.white, false);

            var panelGlow = NewRect("PanelGlow", window, Vector2.zero, new Vector2(640f, 520f));
            var panelGlowImage = AddImage(panelGlow, null, new Color(0.3f, 1f, 0.8f, 0f), false);

            var content = Stretch("Content", window);
            var contentGroup = content.gameObject.AddComponent<CanvasGroup>();
            contentGroup.alpha = 1f;
            var title = AddText(content, "Title", font, new Vector2(0f, 54f), new Vector2(400f, 40f), 36f, TitleColor);
            title.fontStyle = FontStyles.Bold;
            title.text = FacilityServicePopupView.ClinicTitle;
            var divider = NewRect("Divider", content, new Vector2(0f, 28f), new Vector2(320f, 2f));
            AddImage(divider, null, new Color(0.3f, 0.9f, 1f, 0.35f), false);
            var description = AddText(content, "Description", font, new Vector2(0f, -4f), new Vector2(480f, 30f), 22f, DescriptionColor);
            description.text = FacilityServicePopupView.ClinicDescription;
            var healthGauge = BuildGauge(content, "HealthGauge", "hp");
            var energyGauge = BuildGauge(content, "EnergyGauge", "energy");
            var value = AddText(content, "ValueText", font, new Vector2(0f, -108f), new Vector2(480f, 32f), 26f, Color.white);
            value.fontStyle = FontStyles.Bold;
            var result = AddText(content, "ResultText", font, new Vector2(0f, -166f), new Vector2(480f, 56f), 22f, new Color(0.45f, 1f, 0.65f));
            result.textWrappingMode = TextWrappingModes.Normal;
            result.alignment = TextAlignmentOptions.Top;
            var close = BuildCloseButton(content, font);

            // 아이콘 뒤쪽 효과: 아이콘 윤곽이 효과에 묻히지 않도록 모두 아이콘보다 아래에 둔다.
            var fxBehind = Stretch("FxBehind", window);
            var iconGlow = NewRect("IconGlow", fxBehind, new Vector2(0f, IconSlotY), new Vector2(170f, 170f));
            var iconGlowImage = AddImage(iconGlow, null, new Color(0.3f, 1f, 0.8f, 0f), false);
            var flashGlow = NewRect("FlashGlow", fxBehind, new Vector2(0f, IconSlotY), new Vector2(300f, 300f));
            var flashGlowImage = AddImage(flashGlow, null, new Color(0.2f, 0.95f, 1f, 0f), false);
            var flashRing = NewRect("FlashRing", fxBehind, new Vector2(0f, IconSlotY), new Vector2(200f, 200f));
            var flashRingImage = AddImage(flashRing, null, new Color(0.8f, 1f, 1f, 0f), false);
            var arcGlow = AddLine(Stretch("ArcGlow", fxBehind), 8f);
            var arcCore = AddLine(Stretch("ArcCore", fxBehind), 2.4f);

            var iconRect = NewRect("Icon", window, new Vector2(0f, IconSlotY), IconSize);
            var iconImage = AddImage(iconRect, LoadSprite(HeartPath), Color.white, false);
            iconImage.preserveAspect = true;

            var fxFront = Stretch("FxFront", window);
            var ecgGlow = AddLine(Stretch("EcgGlow", fxFront), 9f);
            var ecgCore = AddLine(Stretch("EcgCore", fxFront), 2.6f);

            var viewObject = new SerializedObject(view);
            Set(viewObject, "windowGroup", windowGroup);
            Set(viewObject, "panelBackdrop", backdrop);
            Set(viewObject, "panelBackdropGroup", backdropGroup);
            Set(viewObject, "panelGlow", panelGlowImage);
            Set(viewObject, "content", contentGroup);
            Set(viewObject, "iconRect", iconRect);
            Set(viewObject, "icon", iconImage);
            Set(viewObject, "heartSprite", LoadSprite(HeartPath));
            Set(viewObject, "boltSprite", LoadSprite(BoltPath));
            Set(viewObject, "iconGlowRect", iconGlow);
            Set(viewObject, "iconGlow", iconGlowImage);
            Set(viewObject, "flashRingRect", flashRing);
            Set(viewObject, "flashRing", flashRingImage);
            Set(viewObject, "flashGlowRect", flashGlow);
            Set(viewObject, "flashGlow", flashGlowImage);
            Set(viewObject, "ecgCore", ecgCore);
            Set(viewObject, "ecgGlow", ecgGlow);
            Set(viewObject, "arcCore", arcCore);
            Set(viewObject, "arcGlow", arcGlow);
            Set(viewObject, "titleText", title);
            Set(viewObject, "descriptionText", description);
            Set(viewObject, "valueText", value);
            Set(viewObject, "resultText", result);
            Set(viewObject, "healthGauge", healthGauge.gameObject);
            Set(viewObject, "healthFill", healthGauge.Find("Fill").GetComponent<Image>());
            Set(viewObject, "energyGauge", energyGauge.gameObject);
            Set(viewObject, "energyFill", energyGauge.Find("Fill").GetComponent<Image>());
            Set(viewObject, "closeButton", close);
            viewObject.ApplyModifiedPropertiesWithoutUndo();
            return window.gameObject;
        }

        /// <summary>HUD 게이지와 같은 구성: 빈 틀 + 채움(가로로 잘림) + 테두리.</summary>
        private static RectTransform BuildGauge(RectTransform parent, string name, string kind)
        {
            var gauge = NewRect(name, parent, new Vector2(0f, -62f), GaugeSize);
            var empty = Stretch("Empty", gauge);
            AddImage(empty, LoadSprite(HudArt + kind + "-empty.png"), Color.white, false);
            var fill = Stretch("Fill", gauge);
            var fillImage = AddImage(fill, LoadSprite(HudArt + kind + "-bar.png"), Color.white, false);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 1f;
            var frame = Stretch("Frame", gauge);
            AddImage(frame, LoadSprite(HudArt + "bar-frame.png"), Color.white, false);
            return gauge;
        }

        private static Button BuildCloseButton(RectTransform parent, TMP_FontAsset font)
        {
            var rect = NewRect("CloseButton", parent, Vector2.zero, new Vector2(36f, 36f));
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-34f, -34f);
            var image = AddImage(rect, null, new Color(0.04f, 0.13f, 0.17f, 0.92f), true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = AddText(rect, "Label", font, Vector2.zero, Vector2.zero, 24f, Cyan);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.text = "×";
            return button;
        }

        private static FacilityServiceLineGraphic AddLine(RectTransform rect, float thickness)
        {
            var line = rect.gameObject.AddComponent<FacilityServiceLineGraphic>();
            line.raycastTarget = false;
            line.Thickness = thickness;
            line.color = new Color(1f, 1f, 1f, 0f);
            return line;
        }

        private static TextMeshProUGUI AddText(
            RectTransform parent,
            string name,
            TMP_FontAsset font,
            Vector2 position,
            Vector2 size,
            float fontSize,
            Color color)
        {
            var rect = NewRect(name, parent, position, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
            return text;
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
    }
}
