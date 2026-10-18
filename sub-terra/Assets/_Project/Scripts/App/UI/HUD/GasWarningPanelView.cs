using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 가스 경고 패널 View. 등급 텍스트와 경고 GO 활성만 담당한다.
    /// </summary>
    public sealed class GasWarningPanelView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI gasRiskText;
        [SerializeField] private GameObject warningRoot;

        private RectTransform panelRect;
        private Vector2 restingAnchoredPosition;
        private bool restingCaptured;

        public TextMeshProUGUI GasRiskText => gasRiskText;
        public GameObject WarningRoot => warningRoot;
        public Vector2 RestingAnchoredPosition => restingAnchoredPosition;

        private bool clearanceSent;
        private bool clearanceOnScreen;

        private void LateUpdate()
        {
            SyncClockClearance();
        }

        /// <summary>시계 표시 상태가 바뀌었으면 경고 위치를 맞춘다.</summary>
        public void SyncClockClearance()
        {
            var onScreen = GasWarningClockClearance.ClockOnScreen;
            if (clearanceSent && restingCaptured && onScreen == clearanceOnScreen)
            {
                return;
            }

            clearanceSent = true;
            clearanceOnScreen = onScreen;
            ApplyClockClearance(onScreen);
        }

        /// <summary>
        /// 시계가 화면에 있으면 경고를 시계 아래에 두고, 없으면 잡았던 원래 위치로 되돌린다.
        /// </summary>
        public void ApplyClockClearance(bool clockOnScreen)
        {
            CaptureRestingPosition();
            if (panelRect == null)
            {
                return;
            }

            var next = GasWarningClockLayout.Position(clockOnScreen, restingAnchoredPosition);
            if (panelRect.anchoredPosition == next)
            {
                return;
            }

            panelRect.anchoredPosition = next;
        }

        private void CaptureRestingPosition()
        {
            if (restingCaptured)
            {
                return;
            }

            panelRect = transform as RectTransform;
            if (panelRect == null)
            {
                return;
            }

            restingAnchoredPosition = panelRect.anchoredPosition;
            restingCaptured = true;
        }

        public void SetGasRisk(string text)
        {
            if (gasRiskText != null)
            {
                gasRiskText.text = text ?? string.Empty;
            }
        }

        public void SetGasWarningVisible(bool visible)
        {
            if (warningRoot != null)
            {
                warningRoot.SetActive(visible);
            }
        }

        public bool HasRequiredReferences()
        {
            return gasRiskText != null && warningRoot != null;
        }
    }
}
