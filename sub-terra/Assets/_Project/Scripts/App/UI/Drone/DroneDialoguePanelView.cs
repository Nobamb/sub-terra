using System.Text;
using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;
using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.Drone
{
    /// <summary>
    /// Digger-Bot 창: 템플릿 대사와 (통합된) 추천 행동·근거를 함께 표시한다.
    /// 우측 단독 드론 추천 패널 대신 이 창으로 합친다.
    /// </summary>
    public sealed class DroneDialoguePanelView : MonoBehaviour, IDroneDialogueView, IDroneReasonView
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private TMP_Text reasonText;

        public void SetDialogue(DroneDialogueResult dialogue)
        {
            if (dialogueText != null && dialogue != null && !dialogue.IsSuppressed)
            {
                dialogueText.text = dialogue.Text;
            }
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
