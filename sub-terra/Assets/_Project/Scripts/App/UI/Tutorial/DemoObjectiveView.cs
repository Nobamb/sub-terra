using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private TMP_Text detailsRewardText;
        [SerializeField] private TMP_Text detailsStatusText;
        [SerializeField] private TMP_Text detailsIndexText;
        [SerializeField] private Button detailsPrevButton;
        [SerializeField] private Button detailsNextButton;
        [SerializeField] private GameObject capacityRoot;
        [SerializeField] private TMP_Text capacityTitleText;
        [SerializeField] private TMP_Text capacityBodyText;
        [SerializeField] private GameObject dumpRoot;
        [SerializeField] private TMP_Text dumpSummaryText;
        [SerializeField] private TMP_Text dumpCopperText;
        [SerializeField] private TMP_Text dumpIronText;
        [SerializeField] private TMP_Text dumpLithiumText;
        [SerializeField] private Button dumpCopperOneButton;
        [SerializeField] private Button dumpCopperAllButton;
        [SerializeField] private Button dumpIronOneButton;
        [SerializeField] private Button dumpIronAllButton;
        [SerializeField] private Button dumpLithiumOneButton;
        [SerializeField] private Button dumpLithiumAllButton;
        [SerializeField] private GameObject claimRoot;
        [SerializeField] private TMP_Text claimTitleText;
        [SerializeField] private TMP_Text claimQuestTitleText;
        [SerializeField] private TMP_Text claimRewardText;
        [SerializeField] private TMP_Text claimHintText;
        [SerializeField] private Image basicStatusIcon;
        [SerializeField] private Sprite basicStatusClearSprite;
        [SerializeField] private Sprite basicStatusRingSprite;
        [SerializeField] private Image detailsStatusIcon;
        [SerializeField] private Sprite detailsStatusClearSprite;
        [SerializeField] private Sprite detailsStatusRingSprite;
        [SerializeField] private GameObject detailsClearBadge;
        [SerializeField] private TMP_Text detailsProgressText;
        [SerializeField] private QuestRewardSlotView copperSlot;
        [SerializeField] private QuestRewardSlotView ironSlot;
        [SerializeField] private QuestRewardSlotView lithiumSlot;
        [SerializeField] private QuestRewardSlotView goldSlot;
        [SerializeField] private QuestThumbnailView thumbnailView;

        private int defaultTutorialSort = UiLayerPriority.TutorialGuidance;
        private Canvas guidanceCanvas;
        private Canvas detailsCanvas;
        private Canvas claimCanvas;

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
                progressCountText.text = "진행도 " + model.CompletedCount + " / " + model.TotalCount;
            }

            // 기본 카드는 지금 목표만 보여 준다. 체크는 데모 목표를 모두 끝낸 뒤에만 켠다.
            var cleared = model.IsDemoComplete
                || (model.TotalCount > 0 && model.CompletedCount >= model.TotalCount);
            if (basicStatusIcon != null)
            {
                basicStatusIcon.sprite = cleared ? basicStatusClearSprite : basicStatusRingSprite;
            }
        }

        public void SetGuidanceVisible(bool visible)
        {
            if (guidanceRoot != null)
            {
                if (visible)
                {
                    guidanceRoot.SetActive(true);
                    // 같은 Tutorial Canvas 안에서는 첫 안내 팝업이 항상 마지막에 그려진다.
                    guidanceRoot.transform.SetAsLastSibling();
                    EnsureGuidanceCanvas();
                }
                else
                {
                    guidanceRoot.SetActive(false);
                }
            }
        }

        private void EnsureGuidanceCanvas()
        {
            if (guidanceCanvas == null)
            {
                guidanceCanvas = guidanceRoot.GetComponent<Canvas>();
                if (guidanceCanvas == null)
                {
                    guidanceCanvas = guidanceRoot.AddComponent<Canvas>();
                }
            }

            if (guidanceRoot.GetComponent<GraphicRaycaster>() == null)
            {
                guidanceRoot.AddComponent<GraphicRaycaster>();
            }

            // HUD 전체가 아니라 시작 안내만 독립 정렬해 다른 팝업보다 앞에 표시한다.
            guidanceCanvas.enabled = true;
            guidanceCanvas.overrideSorting = true;
            guidanceCanvas.sortingOrder = UiLayerPriority.IntroductionGuidance;
        }

        private static void EnsurePopupCanvas(GameObject root, ref Canvas canvas)
        {
            if (root == null)
            {
                return;
            }

            if (canvas == null)
            {
                canvas = root.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = root.AddComponent<Canvas>();
                }
            }

            if (root.GetComponent<GraphicRaycaster>() == null)
            {
                root.AddComponent<GraphicRaycaster>();
            }

            canvas.enabled = true;
            canvas.overrideSorting = true;
            canvas.sortingOrder = UiLayerPriority.QuestPopup;
            // World Space로 저장된 중첩 캔버스는 게임 화면 밖으로 빠져 상세창이 안 보인다.
            if (canvas.isRootCanvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
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
                if (visible)
                {
                    detailsRoot.transform.SetAsLastSibling();
                    EnsurePopupCanvas(detailsRoot, ref detailsCanvas);
                }
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

        public void SetDetailsReward(string rewardText)
        {
            if (detailsRewardText != null)
            {
                detailsRewardText.text = rewardText ?? string.Empty;
            }
        }

        public void SetDetailsStatus(string statusText)
        {
            if (detailsStatusText != null)
            {
                detailsStatusText.text = statusText ?? string.Empty;
            }
        }

        public void SetDetailsIndex(string indexText)
        {
            var formatted = string.IsNullOrEmpty(indexText)
                ? string.Empty
                : indexText.Replace("/", " / ");

            if (detailsIndexText != null)
            {
                detailsIndexText.text = formatted;
            }

            if (detailsProgressText != null)
            {
                detailsProgressText.text = formatted;
            }
        }

        public void SetDetailsNavInteractable(bool previousEnabled, bool nextEnabled)
        {
            ApplyNav(detailsPrevButton, previousEnabled);
            ApplyNav(detailsNextButton, nextEnabled);
        }

        /// <summary>상세창의 썸네일, 보상 아이콘, 상태 표시를 기존 퀘스트 데이터로 갱신한다.</summary>
        public void ApplyQuestDetailsVisual(
            string objectiveId,
            QuestReward reward,
            bool cleared,
            bool current,
            int questNumber,
            int totalCount,
            bool isAllComplete = false)
        {
            if (thumbnailView != null)
            {
                thumbnailView.Show(objectiveId, current);
            }

            LayoutRewardSlots(reward);

            if (detailsStatusIcon != null)
            {
                detailsStatusIcon.sprite = cleared ? detailsStatusClearSprite : detailsStatusRingSprite;
            }

            if (detailsClearBadge != null)
            {
                detailsClearBadge.SetActive(cleared);
            }

            if (detailsProgressText != null)
            {
                detailsProgressText.text = questNumber + " / " + totalCount;
            }

            if (detailsStatusText != null)
            {
                var demoFinished = isAllComplete
                    && objectiveId == DemoObjectiveIds.EmergencyEscapeReturn;
                detailsStatusText.text = demoFinished
                    ? "모든 목표 완료"
                    : cleared
                        ? "클리어"
                        : current
                            ? "진행 중"
                            : "미완료";
            }

            if (detailsRewardText != null)
            {
                var anyReward = !reward.IsEmpty;
                detailsRewardText.gameObject.SetActive(!anyReward);
                if (!anyReward)
                {
                    detailsRewardText.text = "없음";
                }
            }

            if (detailsNextActionText != null)
            {
                detailsNextActionText.gameObject.SetActive(
                    !string.IsNullOrEmpty(detailsNextActionText.text));
            }
        }

        private void LayoutRewardSlots(QuestReward reward)
        {
            var slots = new[]
            {
                (slot: copperSlot, amount: reward.Copper),
                (slot: ironSlot, amount: reward.Iron),
                (slot: lithiumSlot, amount: reward.Lithium),
                (slot: goldSlot, amount: reward.Gold)
            };

            var visible = 0;
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].slot != null && slots[i].amount > 0)
                {
                    visible++;
                }
            }

            var rowWidth = 724f;
            var sample = copperSlot != null ? copperSlot.transform.parent as RectTransform : null;
            if (sample != null && sample.sizeDelta.x > 1f)
            {
                rowWidth = sample.sizeDelta.x;
            }

            // 1개도 2개짜리 긴 칸을 쓴다. 2개·3개는 그 개수만으로 행 너비를 채운다.
            const float gap = 12f;
            var columns = visible <= 1 ? 2 : visible;
            var width = (rowWidth - gap * (columns - 1)) / columns;
            var cursor = 0f;
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i].slot;
                if (slot == null)
                {
                    continue;
                }

                slot.SetAmount(slots[i].amount);
                if (slots[i].amount <= 0)
                {
                    continue;
                }

                slot.PlaceInRow(cursor, width);
                cursor += width + gap;
            }
        }

        private static void ApplyNav(Button button, bool enabled)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = enabled;
            button.gameObject.SetActive(enabled);
        }

        public void SetCapacityChoiceVisible(bool visible)
        {
            if (capacityRoot != null)
            {
                capacityRoot.SetActive(visible);
                if (visible)
                {
                    capacityRoot.transform.SetAsLastSibling();
                }
            }
        }

        public void SetCapacityChoiceText(string title, string body)
        {
            if (capacityTitleText != null)
            {
                capacityTitleText.text = title ?? string.Empty;
            }

            if (capacityBodyText != null)
            {
                capacityBodyText.text = body ?? string.Empty;
            }
        }

        public void SetDumpPanelVisible(bool visible)
        {
            if (dumpRoot != null)
            {
                dumpRoot.SetActive(visible);
                if (visible)
                {
                    dumpRoot.transform.SetAsLastSibling();
                }
            }
        }

        public void SetDumpSummary(string summary)
        {
            if (dumpSummaryText != null)
            {
                dumpSummaryText.text = summary ?? string.Empty;
            }
        }

        public void SetDumpRows(IReadOnlyList<QuestDumpRow> rows)
        {
            SetDumpMineral(DataIds.Minerals.Copper, dumpCopperText, dumpCopperOneButton, dumpCopperAllButton, rows);
            SetDumpMineral(DataIds.Minerals.Iron, dumpIronText, dumpIronOneButton, dumpIronAllButton, rows);
            SetDumpMineral(DataIds.Minerals.Lithium, dumpLithiumText, dumpLithiumOneButton, dumpLithiumAllButton, rows);
        }

        public void SetClaimVisible(bool visible)
        {
            if (claimRoot != null)
            {
                claimRoot.SetActive(visible);
                if (visible)
                {
                    claimRoot.transform.SetAsLastSibling();
                    EnsurePopupCanvas(claimRoot, ref claimCanvas);
                }
            }
        }

        public void SetClaimText(string title, string questTitle, string rewardText, string hint)
        {
            if (claimTitleText != null)
            {
                claimTitleText.text = title ?? string.Empty;
            }

            if (claimQuestTitleText != null)
            {
                claimQuestTitleText.text = questTitle ?? string.Empty;
            }

            if (claimRewardText != null)
            {
                claimRewardText.text = rewardText ?? string.Empty;
            }

            if (claimHintText != null)
            {
                claimHintText.text = hint ?? string.Empty;
            }
        }

        private static void SetDumpMineral(
            string mineralId,
            TMP_Text label,
            Button dumpOne,
            Button dumpAll,
            IReadOnlyList<QuestDumpRow> rows)
        {
            var quantity = 0;
            var display = QuestRewardService.DisplayNameOf(mineralId);
            if (rows != null)
            {
                for (var i = 0; i < rows.Count; i++)
                {
                    if (rows[i].MineralId != mineralId)
                    {
                        continue;
                    }

                    quantity = rows[i].Quantity;
                    if (!string.IsNullOrEmpty(rows[i].DisplayName))
                    {
                        display = rows[i].DisplayName;
                    }

                    break;
                }
            }

            if (label != null)
            {
                label.text = display + "  x" + quantity;
            }

            if (dumpOne != null)
            {
                dumpOne.interactable = quantity > 0;
            }

            if (dumpAll != null)
            {
                dumpAll.interactable = quantity > 0;
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
        public event System.Action DetailsPrevRequested;
        public event System.Action DetailsNextRequested;
        public event System.Action CapacityDumpRequested;
        public event System.Action CapacityForfeitRequested;
        public event System.Action DumpClosedRequested;
        public event System.Action<string, int> DumpRequested;
        public event System.Action ClaimConfirmRequested;

        public void OnDetailsPrevClicked()
        {
            DetailsPrevRequested?.Invoke();
        }

        public void OnDetailsNextClicked()
        {
            DetailsNextRequested?.Invoke();
        }

        public void OnCapacityDumpClicked()
        {
            CapacityDumpRequested?.Invoke();
        }

        public void OnCapacityForfeitClicked()
        {
            CapacityForfeitRequested?.Invoke();
        }

        public void OnDumpClosedClicked()
        {
            DumpClosedRequested?.Invoke();
        }

        public void OnDumpCopperOneClicked()
        {
            DumpRequested?.Invoke(DataIds.Minerals.Copper, 1);
        }

        public void OnDumpCopperAllClicked()
        {
            DumpRequested?.Invoke(DataIds.Minerals.Copper, int.MaxValue);
        }

        public void OnDumpIronOneClicked()
        {
            DumpRequested?.Invoke(DataIds.Minerals.Iron, 1);
        }

        public void OnDumpIronAllClicked()
        {
            DumpRequested?.Invoke(DataIds.Minerals.Iron, int.MaxValue);
        }

        public void OnDumpLithiumOneClicked()
        {
            DumpRequested?.Invoke(DataIds.Minerals.Lithium, 1);
        }

        public void OnDumpLithiumAllClicked()
        {
            DumpRequested?.Invoke(DataIds.Minerals.Lithium, int.MaxValue);
        }

        /// <summary>클리어 보상 팝업 닫기/확인. 닫는 시점에 보상을 지급한다.</summary>
        public void OnClaimConfirmClicked()
        {
            ClaimConfirmRequested?.Invoke();
        }

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

        public bool HasRewardLogReferences()
        {
            return HasDetailsReferences()
                && detailsRewardText != null
                && detailsStatusText != null
                && detailsIndexText != null
                && detailsPrevButton != null
                && detailsNextButton != null;
        }

        public bool HasOverflowReferences()
        {
            return capacityRoot != null
                && capacityTitleText != null
                && capacityBodyText != null
                && dumpRoot != null
                && dumpSummaryText != null;
        }

        public bool HasClaimReferences()
        {
            return claimRoot != null
                && claimTitleText != null
                && claimQuestTitleText != null
                && claimRewardText != null
                && claimHintText != null;
        }
    }
}
