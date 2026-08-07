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
    public sealed class DroneDialoguePanelView : MonoBehaviour, IDroneDialogueView, IDroneReasonView
    {
        /// <summary>하단 digger-bot 창 대사 기본 글자 크기(말풍선보다 크게).</summary>
        public const float PanelDialogueFontSize = 26f;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private Button closeButton;

        /// <summary>닫기(X) 버튼. HudPanelChromeController가 배선한다.</summary>
        public Button CloseButton => closeButton;

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

            dialogueText.text = dialogue.Text;
            // 말풍선보다 큰 digger-bot 창 전용 글자 크기 유지.
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
                actionText.text = "추천: "
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

                builder.Append("• ")
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
    }
}
