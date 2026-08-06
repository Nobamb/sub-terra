using SubTerra.Shared;
using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.RunFailure
{
    /// <summary>실패 원인, 화물 손실과 구조 목적지를 같은 패널에 표시한다.</summary>
    public sealed class RunFailurePanelView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI detailText;

        private void Awake()
        {
            Hide();
        }

        public bool HasRequiredReferences()
        {
            return canvasGroup != null && titleText != null && detailText != null;
        }

        public void Show(PlayerRescueResultDto rescue)
        {
            if (rescue == null)
            {
                return;
            }

            SetVisible(true);
            titleText.text = CauseText(rescue.cause) + " — 구조 진행";
            var destination = rescue.usedCheckpoint
                ? "전진기지 체크포인트 " + rescue.returnTargetId
                : "Surface Base";
            detailText.text = "미정산 화물 손실 " + rescue.lostValue.ToString("0")
                + " G / 보존율 " + (rescue.preservationRatio * 100f).ToString("0")
                + "%\n복귀 위치: " + destination;
        }

        public void ShowSurfaceFallback()
        {
            SetVisible(true);
            titleText.text = "Scene 전환 실패 — 현장 안전 지점 복귀";
            detailText.text = "입력과 생존 상태를 복구했습니다. 엘리베이터에서 다시 귀환하세요.";
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private static string CauseText(RunFailureCause cause)
        {
            return cause switch
            {
                RunFailureCause.PowerDepleted => "전력 고갈",
                RunFailureCause.StructuralCollapse => "붕괴 피해",
                RunFailureCause.GasExposure => "가스 누적 노출",
                _ => "탐사 실패"
            };
        }
    }
}
