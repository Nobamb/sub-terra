using System.Text;
using SubTerra.App.Drone;
using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.Drone
{
    /// <summary>추천 행동과 같은 분석 결과에 포함된 근거만 표시하는 View.</summary>
    public sealed class DroneReasonPanelView : MonoBehaviour, IDroneReasonView
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private TMP_Text reasonText;

        public void SetAnalysis(DroneAnalysisResult analysis)
        {
            if (analysis == null)
            {
                return;
            }

            if (actionText != null)
            {
                actionText.text = DroneAnalysisService.FormatAction(analysis.RecommendedAction)
                    + "  [" + analysis.Recommendation.Score + "]";
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
            return panelRoot != null && actionText != null && reasonText != null;
        }
    }
}
