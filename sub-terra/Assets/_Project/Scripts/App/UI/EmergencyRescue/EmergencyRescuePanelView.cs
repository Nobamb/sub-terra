using System;
using System.Text;
using SubTerra.App.Run;
using SubTerra.App.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyRescue
{
    /// <summary>전력 고갈 구출 팝업과 캐릭터 머리 위 재호출 칩을 소유하는 런타임 View.</summary>
    public sealed class EmergencyRescuePanelView : MonoBehaviour
    {
        private static readonly Color OverlayColor = new(0.01f, 0.02f, 0.035f, 0.78f);
        private static readonly Color CardColor = new(0.055f, 0.075f, 0.105f, 0.98f);
        private static readonly Color RescueColor = new(0.72f, 0.12f, 0.12f, 1f);
        private static readonly Color CloseColor = new(0.18f, 0.22f, 0.28f, 1f);

        private GameObject popupRoot;
        private GameObject rescueChip;
        private TMP_Text costText;
        private TMP_Text messageText;
        private Button rescueButton;
        private Button closeButton;
        private Button chipButton;

        public bool IsOpen => popupRoot != null && popupRoot.activeSelf;
        public bool IsChipVisible => rescueChip != null && rescueChip.activeSelf;

        public static EmergencyRescuePanelView Create(
            Transform canvasRoot,
            TMP_FontAsset font)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var root = new GameObject(
                "EmergencyRescueOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Canvas),
                typeof(GraphicRaycaster));
            root.transform.SetParent(canvasRoot, false);
            var rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);
            var popupCanvas = root.GetComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = UiLayerPriority.EmergencyRescueModal;
            var blocker = root.GetComponent<Image>();
            blocker.color = OverlayColor;
            blocker.raycastTarget = true;

            var view = root.AddComponent<EmergencyRescuePanelView>();
            view.popupRoot = root;
            view.BuildPopup(font);
            view.BuildChip(canvasRoot, font);
            root.SetActive(false);
            return view;
        }

        public void SetFollowTarget(Transform player)
        {
            if (rescueChip == null)
            {
                return;
            }

            var follow = rescueChip.GetComponent<EmergencyRescueChipFollow>();
            if (follow != null)
            {
                follow.SetTarget(player);
            }
        }

        public void Bind(Action rescue, Action close, Action reopen)
        {
            ReplaceListener(rescueButton, rescue);
            ReplaceListener(closeButton, close);
            ReplaceListener(chipButton, reopen);
        }

        public void Show(EmergencyRescueCost cost, string message = null)
        {
            if (costText != null)
            {
                costText.text = FormatCost(cost);
            }

            SetMessage(string.IsNullOrWhiteSpace(message)
                ? "현재 위치에서 엘리베이터로 즉시 구출됩니다."
                : message);
            if (popupRoot != null)
            {
                popupRoot.SetActive(true);
                popupRoot.transform.SetAsLastSibling();
            }
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
            }
        }

        public void Close()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        public void SetChipVisible(bool visible)
        {
            if (rescueChip != null)
            {
                rescueChip.SetActive(visible);
                if (visible)
                {
                    rescueChip.transform.SetAsLastSibling();
                }
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (rescueButton != null)
            {
                rescueButton.interactable = interactable;
            }
        }

        private void OnDestroy()
        {
            if (rescueChip != null)
            {
                Destroy(rescueChip);
            }
        }

        public static string FormatCost(EmergencyRescueCost cost)
        {
            if (cost == null)
            {
                return "비용 정보를 불러올 수 없습니다.";
            }

            var builder = new StringBuilder();
            builder.Append("골드 ")
                .Append(cost.GoldCharged)
                .Append("G  (")
                .Append(cost.GoldBefore)
                .Append('→')
                .Append(cost.GoldAfter)
                .Append(')');

            for (var i = 0; i < cost.Minerals.Count; i++)
            {
                EmergencyRescueMineralCost mineral = cost.Minerals[i];
                builder.AppendLine()
                    .Append(string.IsNullOrWhiteSpace(mineral.DisplayName)
                        ? mineral.MineralId
                        : mineral.DisplayName)
                    .Append(' ')
                    .Append(mineral.Charged)
                    .Append("  (")
                    .Append(mineral.Before)
                    .Append('→')
                    .Append(mineral.After)
                    .Append(')');
            }

            if (cost.IsFree)
            {
                builder.AppendLine().Append("보유 골드와 미정산 화물이 없어 무료로 구출됩니다.");
            }
            else
            {
                builder.AppendLine().Append("※ 미정산 광물은 종류별 80%가 차감됩니다.");
            }

            return builder.ToString();
        }

        private void BuildPopup(TMP_FontAsset font)
        {
            var card = CreateImage("Card", transform, CardColor);
            RectTransform cardRect = card.rectTransform;
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(680f, 440f);
            cardRect.anchoredPosition = Vector2.zero;

            TMP_Text title = CreateText("Title", card.transform, font, 32f, FontStyles.Bold);
            SetRect(title.rectTransform, new Vector2(28f, -28f), new Vector2(-28f, -82f));
            title.alignment = TextAlignmentOptions.Center;
            title.text = "전력이 바닥났습니다";
            title.color = new Color(1f, 0.48f, 0.43f, 1f);

            messageText = CreateText("Message", card.transform, font, 20f, FontStyles.Normal);
            SetRect(messageText.rectTransform, new Vector2(34f, -92f), new Vector2(-34f, -142f));
            messageText.alignment = TextAlignmentOptions.Center;

            costText = CreateText("Cost", card.transform, font, 21f, FontStyles.Normal);
            SetRect(costText.rectTransform, new Vector2(56f, -154f), new Vector2(-56f, -330f));
            costText.alignment = TextAlignmentOptions.TopLeft;
            costText.color = new Color(0.93f, 0.96f, 1f, 1f);
            costText.textWrappingMode = TextWrappingModes.Normal;

            rescueButton = CreateButton(
                "RescueButton",
                card.transform,
                font,
                "구출 요청",
                RescueColor,
                new Vector2(-150f, 34f));
            closeButton = CreateButton(
                "CloseButton",
                card.transform,
                font,
                "닫기",
                CloseColor,
                new Vector2(150f, 34f));
        }

        private void BuildChip(Transform canvasRoot, TMP_FontAsset font)
        {
            Transform parent = canvasRoot != null ? canvasRoot : transform;
            var chipImage = CreateImage("EmergencyRescueChip", parent, RescueColor);
            rescueChip = chipImage.gameObject;
            RectTransform rect = chipImage.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(120f, 42f);
            rect.anchoredPosition = Vector2.zero;
            chipImage.raycastTarget = true;

            var canvas = rescueChip.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = UiLayerPriority.CriticalHazard;
            rescueChip.AddComponent<GraphicRaycaster>();
            rescueChip.AddComponent<EmergencyRescueChipFollow>();

            chipButton = rescueChip.AddComponent<Button>();
            chipButton.targetGraphic = chipImage;
            TMP_Text label = CreateText("Label", rescueChip.transform, font, 20f, FontStyles.Bold);
            Stretch(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            label.text = "구출  R";
            label.raycastTarget = false;
            rescueChip.SetActive(false);
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            float size,
            FontStyles style)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string label,
            Color color,
            Vector2 position)
        {
            Image image = CreateImage(name, parent, color);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(220f, 58f);
            rect.anchoredPosition = position;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TMP_Text text = CreateText("Label", image.transform, font, 21f, FontStyles.Bold);
            Stretch(text.rectTransform);
            text.alignment = TextAlignmentOptions.Center;
            text.text = label;
            return button;
        }

        private static void ReplaceListener(Button button, Action action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (action != null)
            {
                button.onClick.AddListener(() => action());
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 topLeft, Vector2 bottomRight)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(topLeft.x, bottomRight.y);
            rect.offsetMax = new Vector2(bottomRight.x, topLeft.y);
        }
    }
}
