using System.Linq;
using System.Text;
using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Drone
{
    /// <summary>
    /// Digger-Bot 창: 템플릿 대사와 (통합된) 추천 행동·근거를 함께 표시한다.
    /// 드론 머리 위 말풍선보다 큰 글자로 같은 대사를 하단 중앙에 보여 준다.
    /// </summary>
    public sealed class DroneDialoguePanelView : MonoBehaviour,
        IDroneDialogueView,
        IDroneReasonView,
        IDroneOperationalStateView
    {
        /// <summary>하단 digger-bot 창 대사 기본 글자 크기.</summary>
        public const float PanelDialogueFontSize = 20f;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private Button closeButton;

        private bool terminalSkinApplied;
        private DroneOperationalState operationalState = DroneOperationalState.Idle;
        private TMP_Text speakerText;

        private static readonly Color TerminalBackground = new Color(0.012f, 0.035f, 0.055f, 0.975f);
        private static readonly Color TerminalHeader = new Color(0.018f, 0.12f, 0.16f, 0.99f);
        private static readonly Color AccentCyan = new Color(0.18f, 0.84f, 0.92f, 1f);
        private static readonly Color AlertAmber = new Color(1f, 0.66f, 0.16f, 1f);

        /// <summary>닫기(X) 버튼. HudPanelChromeController가 배선한다.</summary>
        public Button CloseButton => closeButton;

        private void Awake()
        {
            ApplyTerminalSkin();
        }

        public bool IsVisible
        {
            get
            {
                var root = panelRoot != null ? panelRoot : gameObject;
                return root != null && root.activeSelf;
            }
        }

        public void SetDialogue(DroneDialogueResult dialogue)
        {
            // 쿨다운으로 억제된 결과는 기존 문구를 유지한다(창을 열었을 때 빈 칸 방지).
            if (dialogueText == null || dialogue == null || dialogue.IsSuppressed)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(dialogue.Text))
            {
                return;
            }

            ApplyTerminalSkin();
            dialogueText.text = dialogue.Text;
            // 창 제목과 같은 크기의 본문 글자 크기를 유지한다.
            dialogueText.fontSize = PanelDialogueFontSize;
        }

        public void SetAnalysis(DroneAnalysisResult analysis)
        {
            if (analysis == null)
            {
                return;
            }

            if (actionText != null)
            {
                actionText.text = "AI 추천 // "
                    + DroneAnalysisService.FormatAction(analysis.RecommendedAction)
                    + "  ["
                    + analysis.Recommendation.Score
                    + "]";
            }

            if (reasonText == null)
            {
                return;
            }

            var builder = new StringBuilder();
            var reasons = analysis.Recommendation.Reasons;
            for (var i = 0; i < reasons.Count; i++)
            {
                if (i > 0)
                {
                    builder.AppendLine();
                }

                builder.Append("[")
                    .Append((i + 1).ToString("00"))
                    .Append("] ")
                    .Append(reasons[i].Message)
                    .Append(" (+")
                    .Append(reasons[i].Score)
                    .Append(')');
            }

            reasonText.text = builder.Length > 0
                ? builder.ToString()
                : "추가 위험 근거 없음";
        }

        public void SetVisible(bool visible)
        {
            ApplyTerminalSkin();
            (panelRoot != null ? panelRoot : gameObject).SetActive(visible);
        }

        public bool HasRequiredReferences()
        {
            // 추천 텍스트는 통합 레이아웃에서 주입되며, 없으면 대사만 표시해도 동작한다.
            return panelRoot != null && dialogueText != null;
        }

        public bool HasIntegratedReasonTexts()
        {
            return actionText != null && reasonText != null;
        }

        public void SetOperationalState(DroneOperationalState state)
        {
            operationalState = state;
            ApplyHeaderState();
        }

        private void ApplyTerminalSkin()
        {
            if (terminalSkinApplied)
            {
                return;
            }

            var root = panelRoot != null ? panelRoot : gameObject;
            var panelRect = root.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }

            var background = root.GetComponent<Image>();
            if (background == null)
            {
                background = root.AddComponent<Image>();
            }

            background.color = TerminalBackground;
            background.raycastTarget = false;

            var outline = root.GetComponent<Outline>();
            if (outline == null)
            {
                outline = root.AddComponent<Outline>();
            }

            outline.effectColor = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.72f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;

            var shadow = root.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = root.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(3f, -4f);
            shadow.useGraphicAlpha = true;

            var signalLine = FindOrCreateImage("TerminalSignalLine", panelRect);
            SetAnchors(signalLine.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(14f, -3f), new Vector2(-14f, 0f));
            signalLine.color = AccentCyan;
            signalLine.transform.SetAsFirstSibling();

            var header = FindOrCreateImage("TerminalHeader", panelRect);
            SetAnchors(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -36f), Vector2.zero);
            header.color = TerminalHeader;
            header.transform.SetAsFirstSibling();

            speakerText = root.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(text => text.name == "SpeakerText");
            if (speakerText != null)
            {
                speakerText.fontSize = 18f;
                speakerText.fontStyle = FontStyles.Bold;
                speakerText.alignment = TextAlignmentOptions.MidlineLeft;
                SetAnchors(speakerText.rectTransform, new Vector2(0f, 1f), new Vector2(0.82f, 1f),
                    new Vector2(18f, -34f), new Vector2(0f, -2f));
            }

            StyleText(dialogueText, new Color(0.92f, 0.98f, 1f, 1f));
            StyleText(actionText, new Color(0.34f, 0.94f, 0.84f, 1f));
            StyleText(reasonText, new Color(0.7f, 0.86f, 0.9f, 1f));
            StyleCloseButton(header.rectTransform);
            terminalSkinApplied = true;
            ApplyHeaderState();
        }

        private void ApplyHeaderState()
        {
            if (speakerText == null)
            {
                return;
            }

            var label = operationalState switch
            {
                DroneOperationalState.Scanning => "SCANNING",
                DroneOperationalState.Priority => "PRIORITY",
                _ => "IDLE"
            };
            speakerText.text = "DIGGER-BOT  //  AI ANALYSIS  //  " + label;
            speakerText.color = operationalState == DroneOperationalState.Priority
                ? AlertAmber
                : AccentCyan;
        }

        private void StyleText(TMP_Text text, Color color)
        {
            if (text == null)
            {
                return;
            }

            text.color = color;
            text.raycastTarget = false;
        }

        private void StyleCloseButton(RectTransform header)
        {
            if (closeButton == null || header == null)
            {
                return;
            }

            if (closeButton.transform.parent != header)
            {
                closeButton.transform.SetParent(header, false);
            }

            var image = closeButton.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.color = new Color(0.07f, 0.18f, 0.22f, 0.98f);
            }

            // ColorTint는 전용 피드백의 0.12초 전환과 경쟁하므로 사용하지 않는다.
            closeButton.transition = Selectable.Transition.None;

            var rect = closeButton.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.one;
                rect.anchorMax = Vector2.one;
                rect.pivot = Vector2.one;
                rect.anchoredPosition = new Vector2(-5f, -3f);
                rect.sizeDelta = new Vector2(28f, 28f);
            }

            var label = closeButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "X";
                label.fontSize = 18f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = AccentCyan;
                label.fontStyle = FontStyles.Bold;
                label.raycastTarget = false;
            }

            var feedback = closeButton.GetComponent<DroneTerminalCloseButtonFeedback>();
            if (feedback == null)
            {
                feedback = closeButton.gameObject.AddComponent<DroneTerminalCloseButtonFeedback>();
            }

            feedback.Configure(image, label);
        }

        private Image FindOrCreateImage(string name, RectTransform parent)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null && existing.TryGetComponent<Image>(out var existingImage))
            {
                existingImage.raycastTarget = false;
                return existingImage;
            }

            var element = new GameObject(name, typeof(RectTransform), typeof(Image));
            element.transform.SetParent(parent, false);
            var image = element.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void SetAnchors(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
