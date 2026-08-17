using SubTerra.App.Tutorial;
using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 현재 목표 HUD + dismiss 가능 안내 패널.
    /// Canvas sort order는 튜토리얼 기본값이며, 위험 HUD가 더 높게 유지되어야 한다.
    /// </summary>
    public sealed class DemoObjectiveView : MonoBehaviour, IDemoObjectiveView
    {
        [SerializeField] private TMP_Text objectiveTitleText;
        [SerializeField] private TMP_Text objectiveBodyText;
        [SerializeField] private TMP_Text nextActionText;
        [SerializeField] private TMP_Text progressCountText;
        [SerializeField] private GameObject guidanceRoot;
        [SerializeField] private TMP_Text guidanceTitleText;
        [SerializeField] private TMP_Text guidanceBodyText;
        [SerializeField] private GameObject demoCompleteRoot;
        [SerializeField] private TMP_Text demoCompleteText;
        [SerializeField] private Canvas tutorialCanvas;
        [SerializeField] private CanvasGroup guidanceCanvasGroup;
        [SerializeField] private GameObject detailsRoot;
        [SerializeField] private TMP_Text detailsTitleText;
        [SerializeField] private TMP_Text detailsBodyText;
        [SerializeField] private TMP_Text detailsNextActionText;

        private int defaultTutorialSort = UiLayerPriority.TutorialGuidance;

        private void Awake()
        {
            if (tutorialCanvas != null)
            {
                defaultTutorialSort = tutorialCanvas.sortingOrder;
                if (defaultTutorialSort < UiLayerPriority.TutorialGuidance)
                {
                    tutorialCanvas.sortingOrder = UiLayerPriority.TutorialGuidance;
                    defaultTutorialSort = tutorialCanvas.sortingOrder;
                }
            }
        }

        public void SetObjective(DemoObjectiveReadModel model)
        {
            if (objectiveTitleText != null)
            {
                objectiveTitleText.text = model.Title ?? string.Empty;
            }

            if (objectiveBodyText != null)
            {
                objectiveBodyText.text = model.Description ?? string.Empty;
            }

            if (nextActionText != null)
            {
                nextActionText.text = model.NextActionHint ?? string.Empty;
            }

            if (progressCountText != null)
            {
                progressCountText.text = model.CompletedCount + " / " + model.TotalCount;
            }
        }

        public void SetGuidanceVisible(bool visible)
        {
            if (guidanceRoot != null)
            {
                guidanceRoot.SetActive(visible);
            }
        }

        public void SetGuidanceText(string title, string body)
        {
            if (guidanceTitleText != null)
            {
                guidanceTitleText.text = title ?? string.Empty;
            }

            if (guidanceBodyText != null)
            {
                guidanceBodyText.text = body ?? string.Empty;
            }
        }

        public void SetInputLocked(bool locked)
        {
            // 전역 입력 잠금을 쓰지 않는다. 안내 패널 자체 클릭만 허용.
            if (guidanceCanvasGroup != null)
            {
                guidanceCanvasGroup.blocksRaycasts = !locked && guidanceRoot != null && guidanceRoot.activeSelf;
                guidanceCanvasGroup.interactable = !locked;
            }
        }

        public void SetHazardYield(bool yieldToHazard)
        {
            if (tutorialCanvas == null)
            {
                return;
            }

            // 위험 중에도 목표 문구는 보이되, sort를 위험보다 낮게 유지한다.
            tutorialCanvas.sortingOrder = yieldToHazard
                ? Mathf.Min(defaultTutorialSort, UiLayerPriority.TutorialGuidance)
                : defaultTutorialSort;
        }

        public void SetDemoCompleteVisible(bool visible, string summary)
        {
            if (demoCompleteRoot != null)
            {
                demoCompleteRoot.SetActive(visible);
            }

            if (demoCompleteText != null)
            {
                demoCompleteText.text = summary ?? string.Empty;
            }
        }

        public void SetDetailsVisible(bool visible)
        {
            if (detailsRoot != null)
            {
                detailsRoot.SetActive(visible);
            }
        }

        public void SetDetailsText(string title, string body, string nextAction)
        {
            if (detailsTitleText != null)
            {
                detailsTitleText.text = title ?? string.Empty;
            }

            if (detailsBodyText != null)
            {
                detailsBodyText.text = body ?? string.Empty;
            }

            if (detailsNextActionText != null)
            {
                detailsNextActionText.text = string.IsNullOrEmpty(nextAction)
                    ? string.Empty
                    : "다음 행동: " + nextAction;
            }
        }

        /// <summary>UI Button OnClick 연결용.</summary>
        public void OnDismissClicked()
        {
            // Presenter 연결은 Binder가 담당. View 단독 dismiss는 이벤트만 남긴다.
            DismissRequested?.Invoke();
        }

        public event System.Action DismissRequested;

        /// <summary>좌측 상단 목표 영역 Button OnClick 연결용.</summary>
        public void OnObjectiveDetailsClicked()
        {
            DetailsRequested?.Invoke();
        }

        /// <summary>중앙 목표 상세창 X Button OnClick 연결용.</summary>
        public void OnDetailsDismissClicked()
        {
            DetailsDismissRequested?.Invoke();
        }

        public event System.Action DetailsRequested;
        public event System.Action DetailsDismissRequested;

        public bool HasRequiredReferences()
        {
            return objectiveTitleText != null || nextActionText != null;
        }

        public bool HasDetailsReferences()
        {
            return detailsRoot != null
                && detailsTitleText != null
                && detailsBodyText != null
                && detailsNextActionText != null;
        }
    }
}
