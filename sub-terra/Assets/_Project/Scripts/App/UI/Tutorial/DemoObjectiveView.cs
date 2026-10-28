using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
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
        [SerializeField] private StartBriefingPopupMotion guidanceMotion;
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
        [SerializeField] private CargoFullPopupView capacityPopup;
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
        [SerializeField] private CargoDiscardPopupView dumpPopup;
        [SerializeField] private GameObject claimRoot;
        [SerializeField] private TMP_Text claimTitleText;
        [SerializeField] private TMP_Text claimQuestTitleText;
        [SerializeField] private TMP_Text claimRewardText;
        [SerializeField] private TMP_Text claimHintText;
        [SerializeField] private TMP_Text claimProgressText;
        [SerializeField] private RectTransform claimRewardRow;
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
        private bool capacityPopupHooked;
        // 화물 부족 창이 접히는 0.2초 동안 다음 창을 미뤄 두 창이 겹쳐 보이지 않게 한다.
        private bool dumpAfterCapacityClose;
        private bool claimAfterCapacityClose;
        // 화물 버리기 창(B-153)이 닫히는 동안(페이지 넘김·완료 표시·접기) 다음 창을 미룬다.
        private bool dumpPopupHooked;
        private bool capacityAfterDumpClose;
        private bool claimAfterDumpClose;
        // 두 창 사이 이동은 같은 단말의 페이지 넘김이다. 클릭 처리 중에만 켠다.
        private bool pageToDump;
        private bool pageToCapacity;
        private bool dumpRequestInFlight;
        private CargoShortfallModel dumpShortfall;
        private QuestReward dumpReward;

        private void Awake()
        {
            ApplyCompactSummary();
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
                objectiveBodyText.text = model.IsDemoComplete ? model.Description ?? string.Empty : model.NextActionHint ?? string.Empty;
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

        public void ApplyCompactSummary()
        {
            if (objectiveTitleText == null) return;
            var card = objectiveTitleText.transform.parent as RectTransform;
            if (card == null || card.name != "QuestSummaryButton") return;
            card.sizeDelta = new Vector2(460f, 116f);
            card.localScale = Vector3.one;
            card.anchoredPosition = new Vector2(16f, -232f);
            PlaceSummaryChild(card, "QuestMissionLabel", new Vector2(36f, -10f), new Vector2(170f, 32f));
            var mission = card.Find("QuestMissionLabel");
            if (mission != null && mission.TryGetComponent<TMP_Text>(out var missionText))
            {
                missionText.text = "MISSION · 방법 보기";
                missionText.fontSize = 18f;
            }
            PlaceSummaryChild(card, "ProgressCount", new Vector2(-36f, -10f), new Vector2(170f, 32f));
            ApplySummaryTextSpacing(card);
            if (objectiveBodyText != null && objectiveBodyText.transform.parent == card)
            {
                objectiveBodyText.gameObject.SetActive(true);
                objectiveBodyText.fontSize = 16f;
                objectiveBodyText.enableAutoSizing = true;
                objectiveBodyText.fontSizeMin = 12f;
                objectiveBodyText.fontSizeMax = 16f;
                objectiveBodyText.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        public static void ApplySummaryTextSpacing(RectTransform card)
        {
            // 헤더 아래 2px, 제목/설명 사이 2px, 카드 아래 14px 여백을 남긴다.
            PlaceSummaryChild(card, "ObjectiveTitle", new Vector2(78f, -44f), new Vector2(346f, 30f));
            PlaceSummaryChild(card, "ObjectiveBody", new Vector2(78f, -76f), new Vector2(346f, 26f));
            PlaceSummaryChild(card, "QuestStatusIcon", new Vector2(32f, -55.9f), new Vector2(32f, 32f));
        }

        private static void PlaceSummaryChild(RectTransform card, string name, Vector2 position, Vector2 size)
        {
            if (!(card.Find(name) is RectTransform child)) return;
            child.anchoredPosition = position;
            child.sizeDelta = size;
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
            if (detailsRoot == null)
            {
                return;
            }

            if (!visible)
            {
                TerminalWindowOpenMotion.Close(detailsRoot, HideDetailsImmediately);
                return;
            }

            var wasVisible = detailsRoot.activeSelf && !TerminalWindowOpenMotion.IsClosingWindow(detailsRoot);
            detailsRoot.SetActive(true);
            detailsRoot.transform.SetAsLastSibling();
            EnsurePopupCanvas(detailsRoot, ref detailsCanvas);
            // 인벤토리·시설처럼 방금 연 창이 이미 떠 있는 팝업보다 앞에 온다.
            PopupWindowSorting.BringToFront(detailsCanvas);
            if (!wasVisible) TerminalWindowOpenMotion.Open(detailsRoot);
        }

        private void HideDetailsImmediately()
        {
            if (detailsCanvas != null) PopupWindowSorting.Remove(detailsCanvas);
            if (detailsRoot != null) detailsRoot.SetActive(false);
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
            capacityAfterDumpClose = visible && IsDumpClosing();
            if (capacityAfterDumpClose)
            {
                return;
            }

            var popup = ResolveCapacityPopup();
            if (popup != null)
            {
                if (visible)
                {
                    popup.Show(false);
                    capacityRoot.transform.SetAsLastSibling();
                }
                else
                {
                    popup.Close(pageToDump);
                }

                return;
            }

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

        /// <summary>화물 부족 팝업(B-152) 보상 카드·게이지·포기 문구를 갱신한다.</summary>
        public void SetCapacityChoice(CargoShortfallModel shortfall, QuestReward reward)
        {
            var popup = ResolveCapacityPopup();
            if (popup != null)
            {
                popup.Render(shortfall, reward);
            }
        }

        private CargoFullPopupView ResolveCapacityPopup()
        {
            if (capacityPopup == null && capacityRoot != null)
            {
                capacityPopup = capacityRoot.GetComponent<CargoFullPopupView>();
                if (capacityPopup == null && Application.isPlaying)
                {
                    // 빌더를 다시 돌리지 않은 씬에서도 새 화물 부족 창을 쓴다.
                    capacityPopup = capacityRoot.AddComponent<CargoFullPopupView>();
                }
            }

            if (capacityPopup != null && !capacityPopupHooked)
            {
                capacityPopupHooked = true;
                capacityPopup.Closed += OnCapacityPopupClosed;
            }

            return capacityPopup;
        }

        private bool IsCapacityClosing()
        {
            return capacityPopup != null && capacityPopup.IsClosing;
        }

        private void OnCapacityPopupClosed()
        {
            if (dumpAfterCapacityClose)
            {
                dumpAfterCapacityClose = false;
                ShowDumpPanel(capacityPopup != null && capacityPopup.LastCloseWasPage);
            }

            if (claimAfterCapacityClose)
            {
                claimAfterCapacityClose = false;
                SetClaimVisible(true);
            }
        }

        private void OnDestroy()
        {
            if (capacityPopup != null && capacityPopupHooked)
            {
                capacityPopup.Closed -= OnCapacityPopupClosed;
            }

            if (dumpPopup != null && dumpPopupHooked)
            {
                dumpPopup.Closed -= OnDumpPopupClosed;
            }
        }

        public void SetDumpPanelVisible(bool visible)
        {
            dumpAfterCapacityClose = visible && IsCapacityClosing();
            if (dumpAfterCapacityClose)
            {
                return;
            }

            if (visible)
            {
                ShowDumpPanel(false);
                return;
            }

            var popup = ResolveDumpPopup();
            if (popup != null)
            {
                // 버리기 처리 중에 닫히면 부족이 0이 되어 지급된 것이다. 완료 표시를 잠깐 보여 준 뒤 접는다.
                popup.Close(dumpRequestInFlight
                    ? CargoDiscardPopupView.CloseMode.Complete
                    : pageToCapacity ? CargoDiscardPopupView.CloseMode.Page : CargoDiscardPopupView.CloseMode.Fold);
                return;
            }

            if (dumpRoot != null)
            {
                dumpRoot.SetActive(false);
            }
        }

        private void ShowDumpPanel(bool page)
        {
            var popup = ResolveDumpPopup();
            if (popup != null)
            {
                popup.Show(page);
                dumpRoot.transform.SetAsLastSibling();
                return;
            }

            if (dumpRoot != null)
            {
                dumpRoot.SetActive(true);
                dumpRoot.transform.SetAsLastSibling();
            }
        }

        private CargoDiscardPopupView ResolveDumpPopup()
        {
            if (dumpPopup == null && dumpRoot != null)
            {
                dumpPopup = dumpRoot.GetComponent<CargoDiscardPopupView>();
                if (dumpPopup == null && Application.isPlaying)
                {
                    // 빌더를 다시 돌리지 않은 씬에서도 새 화물 버리기 창을 쓴다.
                    dumpPopup = dumpRoot.AddComponent<CargoDiscardPopupView>();
                }
            }

            if (dumpPopup != null && !dumpPopupHooked)
            {
                dumpPopupHooked = true;
                dumpPopup.Closed += OnDumpPopupClosed;
                dumpPopup.EnsureReady();
                HookMissingDumpListeners(dumpPopup);
            }

            return dumpPopup;
        }

        // 빌더가 영구 연결한 버튼은 그대로 두고, 실행 중에 새로 생긴 버튼만 코드로 잇는다.
        private void HookMissingDumpListeners(CargoDiscardPopupView popup)
        {
            var minerals = new[] { DataIds.Minerals.Copper, DataIds.Minerals.Iron, DataIds.Minerals.Lithium };
            for (var m = 0; m < minerals.Length; m++)
            {
                for (var k = 0; k < CargoDiscardText.Kinds.Length; k++)
                {
                    var button = popup.GetButton(minerals[m], CargoDiscardText.Kinds[k]);
                    if (button == null || button.onClick.GetPersistentEventCount() > 0)
                    {
                        continue;
                    }

                    var mineralId = minerals[m];
                    var kind = CargoDiscardText.Kinds[k];
                    button.onClick.AddListener(() => RequestDump(mineralId, kind));
                }
            }

            var back = popup.BackButton;
            if (back != null && back.onClick.GetPersistentEventCount() == 0)
            {
                back.onClick.AddListener(OnDumpClosedClicked);
            }
        }

        private bool IsDumpClosing()
        {
            return dumpPopup != null && dumpPopup.IsClosing;
        }

        private void OnDumpPopupClosed()
        {
            if (capacityAfterDumpClose)
            {
                capacityAfterDumpClose = false;
                var popup = ResolveCapacityPopup();
                if (popup != null)
                {
                    popup.Show(dumpPopup.LastCloseWasPage);
                    capacityRoot.transform.SetAsLastSibling();
                }
                else
                {
                    SetCapacityChoiceVisible(true);
                }
            }

            if (claimAfterDumpClose)
            {
                claimAfterDumpClose = false;
                SetClaimVisible(true);
            }
        }

        public void SetDumpSummary(string summary)
        {
            if (dumpSummaryText != null)
            {
                dumpSummaryText.text = summary ?? string.Empty;
            }
        }

        /// <summary>화물 버리기 창(B-153) 게이지·헤더 보상 값. 다음 SetDumpRows에서 함께 그린다.</summary>
        public void SetDumpChoice(CargoShortfallModel shortfall, QuestReward reward)
        {
            dumpShortfall = shortfall;
            dumpReward = reward;
        }

        public void SetDumpRows(IReadOnlyList<QuestDumpRow> rows)
        {
            var popup = ResolveDumpPopup();
            if (popup != null)
            {
                popup.Render(dumpShortfall, dumpReward, rows);
                return;
            }

            SetDumpMineral(DataIds.Minerals.Copper, dumpCopperText, dumpCopperOneButton, dumpCopperAllButton, rows);
            SetDumpMineral(DataIds.Minerals.Iron, dumpIronText, dumpIronOneButton, dumpIronAllButton, rows);
            SetDumpMineral(DataIds.Minerals.Lithium, dumpLithiumText, dumpLithiumOneButton, dumpLithiumAllButton, rows);
        }

        public void SetClaimVisible(bool visible)
        {
            claimAfterCapacityClose = visible && IsCapacityClosing();
            claimAfterDumpClose = visible && !claimAfterCapacityClose && IsDumpClosing();
            if (claimAfterCapacityClose || claimAfterDumpClose)
            {
                return;
            }

            if (claimRoot != null)
            {
                if (visible)
                {
                    if (claimRoot.activeSelf)
                    {
                        var motion = claimRoot.GetComponent<QuestClearPopupMotion>();
                        if (motion != null && motion.IsClosing) motion.Open();
                    }
                    claimRoot.SetActive(true);
                    claimRoot.transform.SetAsLastSibling();
                    EnsurePopupCanvas(claimRoot, ref claimCanvas);
                    PopupWindowSorting.BringToFront(claimCanvas);
                }
                else if (claimRoot.activeSelf)
                {
                    var motion = claimRoot.GetComponent<QuestClearPopupMotion>();
                    if (motion != null) motion.Close();
                    else
                    {
                        if (claimCanvas != null)
                        {
                            PopupWindowSorting.Remove(claimCanvas);
                        }

                        claimRoot.SetActive(false);
                    }
                }
            }
        }

        public bool TryCloseDetailsPopup(Canvas topWindow)
        {
            if (detailsRoot == null || !detailsRoot.activeInHierarchy
                || !PopupWindowSorting.Contains(topWindow, detailsRoot)) return false;
            if (!TerminalWindowOpenMotion.IsClosingWindow(detailsRoot)) OnDetailsDismissClicked();
            return true;
        }

        public bool TryCloseClaimPopup(Canvas topWindow)
        {
            if (claimRoot == null || !claimRoot.activeInHierarchy
                || !PopupWindowSorting.Contains(topWindow, claimRoot)) return false;
            var motion = claimRoot.GetComponent<QuestClearPopupMotion>();
            // 닫히는 동안의 연타는 소비하되 지급 요청은 다시 보내지 않는다.
            if (motion == null || !motion.IsClosing) OnClaimConfirmClicked();
            return true;
        }

        public bool TryCloseTopPopup()
        {
            if (TryCloseGuidance())
            { return true; }
            if (detailsRoot != null && detailsRoot.activeInHierarchy
                && claimRoot != null && claimRoot.activeInHierarchy
                && claimRoot.transform.GetSiblingIndex() > detailsRoot.transform.GetSiblingIndex())
            { OnClaimConfirmClicked(); return true; }
            if (detailsRoot != null && detailsRoot.activeInHierarchy)
            { OnDetailsDismissClicked(); return true; }
            if (claimRoot != null && claimRoot.activeInHierarchy)
            { OnClaimConfirmClicked(); return true; }
            if (dumpRoot != null && dumpRoot.activeInHierarchy)
            { OnDumpClosedClicked(); return true; }
            return false;
        }

        public void SetClaimProgress(int index, int count)
        {
            if (claimProgressText != null)
            {
                claimProgressText.text = "진행도  " + index + " / " + count;
            }
        }

        public void SetClaimRewards(QuestReward reward)
        {
            if (claimRewardRow == null) return;

            var names = new[] { "Copper", "Iron", "Lithium", "Gold" };
            var labels = new[] { "구리", "철", "리튬", "골드" };
            var amounts = new[] { reward.Copper, reward.Iron, reward.Lithium, reward.Gold };
            var visible = 0;
            for (var i = 0; i < amounts.Length; i++)
            {
                if (amounts[i] > 0) visible++;
            }

            for (var i = 0; i < names.Length; i++)
            {
                var item = claimRewardRow.Find(names[i]) as RectTransform;
                if (item == null) continue;
                item.gameObject.SetActive(amounts[i] > 0);
                if (amounts[i] <= 0) continue;
                var label = item.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = labels[i] + " × " + amounts[i];
            }

            if (claimRewardText != null) claimRewardText.gameObject.SetActive(visible == 0);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(claimRewardRow);
        }

        public void SetClaimText(string title, string questTitle, string rewardText, string hint)
        {
            ApplyClaimLayout(claimRoot != null ? claimRoot.transform : null);
            if (claimRewardRow != null)
                claimRewardRow.anchoredPosition = new Vector2(0f, ClaimRewardRowY);
            if (claimHintText != null)
                ApplyClaimHint(claimHintText);
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

        public const float ClaimRewardRowY = -420f;
        public const float ClaimHintY = -302f;

        // 빌더와 실행 중 팝업이 같은 간격을 사용한다. 안내는 설명 바로 아래, 보상은 하단 구획에 둔다.
        public static void ApplyClaimLayout(Transform panel)
        {
            if (panel == null) return;
            SetClaimY(panel, "ClaimQuestTitle", -214f);
            SetClaimY(panel, "ClaimDescription", -266f);
            SetClaimY(panel, "ClaimReward", ClaimRewardRowY + 22f);
            SetClaimY(panel, "ClaimRewardRow", ClaimRewardRowY);
            var hint = panel.Find("ClaimHint");
            if (hint != null) ApplyClaimHint(hint.GetComponent<TMP_Text>());
        }

        private static void ApplyClaimHint(TMP_Text hint)
        {
            if (hint == null) return;
            var rect = hint.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(330f, ClaimHintY);
            rect.sizeDelta = new Vector2(620f, 28f);
            hint.alignment = TextAlignmentOptions.MidlineLeft;
            hint.fontSize = 16f;
            hint.color = new Color(0.83f, 0.94f, 0.96f, 1f);
        }

        private static void SetClaimY(Transform panel, string name, float y)
        {
            var rect = panel.Find(name) as RectTransform;
            if (rect != null)
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
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
            // 연출이 있는 시작 브리핑은 닫기 연출이 끝난 뒤에 종료 처리를 한 번만 알린다.
            var motion = ResolveGuidanceMotion();
            if (motion != null && guidanceRoot != null && guidanceRoot.activeInHierarchy)
            {
                motion.RequestClose(RaiseDismissRequested);
                return;
            }

            // Presenter 연결은 Binder가 담당. View 단독 dismiss는 이벤트만 남긴다.
            RaiseDismissRequested();
        }

        /// <summary>X 단축키용. 시작 브리핑이 떠 있으면 닫기를 요청하고 true를 돌려 다른 팝업을 건드리지 않게 한다.</summary>
        public bool TryCloseGuidance()
        {
            if (guidanceRoot == null || !guidanceRoot.activeInHierarchy)
            {
                return false;
            }

            OnDismissClicked();
            return true;
        }

        private void RaiseDismissRequested()
        {
            DismissRequested?.Invoke();
        }

        private StartBriefingPopupMotion ResolveGuidanceMotion()
        {
            if (guidanceMotion == null && guidanceRoot != null)
            {
                guidanceMotion = guidanceRoot.GetComponent<StartBriefingPopupMotion>();
            }

            return guidanceMotion;
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
            if (!AcceptCapacityChoice())
            {
                return;
            }

            // 화물 부족 → 화물 버리기는 같은 단말의 페이지 넘김이다. 접지 않고 내용만 바꾼다.
            pageToDump = true;
            try
            {
                CapacityDumpRequested?.Invoke();
            }
            finally
            {
                pageToDump = false;
            }

            if (!dumpAfterCapacityClose && capacityPopup != null)
            {
                capacityPopup.FoldInsteadOfPage();
            }
        }

        public void OnCapacityForfeitClicked()
        {
            if (!AcceptCapacityChoice())
            {
                return;
            }

            CapacityForfeitRequested?.Invoke();
        }

        // 열기 연출 중·이미 고른 뒤의 클릭은 막는다. 두 버튼 중 하나만 한 번 통과한다.
        private bool AcceptCapacityChoice()
        {
            var popup = ResolveCapacityPopup();
            return popup == null || popup.TryAcceptChoice();
        }

        /// <summary>돌아가기·X 키. 화물 부족 창으로 페이지를 넘기고, 이미 닫히는 중이면 무시한다.</summary>
        public void OnDumpClosedClicked()
        {
            var popup = ResolveDumpPopup();
            if (popup != null && !popup.TryAcceptBack())
            {
                return;
            }

            pageToCapacity = true;
            try
            {
                DumpClosedRequested?.Invoke();
            }
            finally
            {
                pageToCapacity = false;
            }

            if (popup != null && !capacityAfterDumpClose)
            {
                popup.FoldInsteadOfPage();
            }
        }

        public void OnDumpCopperOneClicked() => RequestDump(DataIds.Minerals.Copper, CargoDiscardKind.One);
        public void OnDumpCopperFiveClicked() => RequestDump(DataIds.Minerals.Copper, CargoDiscardKind.Five);
        public void OnDumpCopperTenClicked() => RequestDump(DataIds.Minerals.Copper, CargoDiscardKind.Ten);
        public void OnDumpCopperNeededClicked() => RequestDump(DataIds.Minerals.Copper, CargoDiscardKind.Needed);
        public void OnDumpCopperAllClicked() => RequestDump(DataIds.Minerals.Copper, CargoDiscardKind.All);
        public void OnDumpIronOneClicked() => RequestDump(DataIds.Minerals.Iron, CargoDiscardKind.One);
        public void OnDumpIronFiveClicked() => RequestDump(DataIds.Minerals.Iron, CargoDiscardKind.Five);
        public void OnDumpIronTenClicked() => RequestDump(DataIds.Minerals.Iron, CargoDiscardKind.Ten);
        public void OnDumpIronNeededClicked() => RequestDump(DataIds.Minerals.Iron, CargoDiscardKind.Needed);
        public void OnDumpIronAllClicked() => RequestDump(DataIds.Minerals.Iron, CargoDiscardKind.All);
        public void OnDumpLithiumOneClicked() => RequestDump(DataIds.Minerals.Lithium, CargoDiscardKind.One);
        public void OnDumpLithiumFiveClicked() => RequestDump(DataIds.Minerals.Lithium, CargoDiscardKind.Five);
        public void OnDumpLithiumTenClicked() => RequestDump(DataIds.Minerals.Lithium, CargoDiscardKind.Ten);
        public void OnDumpLithiumNeededClicked() => RequestDump(DataIds.Minerals.Lithium, CargoDiscardKind.Needed);
        public void OnDumpLithiumAllClicked() => RequestDump(DataIds.Minerals.Lithium, CargoDiscardKind.All);

        // 모든 수량 버튼이 같은 DumpRequested(id, 수량) 경로를 쓴다. 연출·처리 중 클릭은 문(gate)에서 버린다.
        private void RequestDump(string mineralId, CargoDiscardKind kind)
        {
            var popup = ResolveDumpPopup();
            var quantity = popup != null ? popup.RequestCount(mineralId, kind) : CargoDiscardText.FixedCount(kind);
            if (quantity <= 0 || (popup != null && !popup.TryAcceptDump()))
            {
                return;
            }

            dumpRequestInFlight = true;
            try
            {
                DumpRequested?.Invoke(mineralId, quantity);
            }
            finally
            {
                dumpRequestInFlight = false;
                if (popup != null)
                {
                    popup.ReleaseDump();
                }
            }
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
                && claimHintText != null
                && claimProgressText != null
                && claimRewardRow != null;
        }
    }
}
