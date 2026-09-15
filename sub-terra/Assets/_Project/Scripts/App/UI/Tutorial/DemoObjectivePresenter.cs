using System;
using SubTerra.App.Tutorial;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 목표 State 읽기 전용 표시 + dismiss 가능한 안내.
    /// 장시간 입력 잠금을 걸지 않으며, 위험 중에는 안내 입력을 양보한다.
    /// </summary>
    public sealed class DemoObjectivePresenter : IDisposable
    {
        private readonly IDemoObjectiveView view;
        private DemoObjectiveDirector director;
        private QuestRewardService rewards;
        private bool hazardActive;
        private bool guidanceOpen;
        private bool detailsOpen;
        private bool capacityOpen;
        private bool dumpOpen;
        private bool claimOpen;
        private bool inputLocked;
        private int viewedIndex;

        public bool IsBound => director != null;
        public bool IsGuidanceOpen => guidanceOpen;
        public bool IsDetailsOpen => detailsOpen;
        public bool IsCapacityOpen => capacityOpen;
        public bool IsDumpOpen => dumpOpen;
        public bool IsClaimOpen => claimOpen;
        public bool IsInputLocked => inputLocked;
        public bool HazardActive => hazardActive;
        public int ViewedIndex => viewedIndex;

        public DemoObjectivePresenter(IDemoObjectiveView objectiveView)
        {
            view = objectiveView;
        }

        public void Bind(DemoObjectiveDirector objectiveDirector)
        {
            Bind(objectiveDirector, null);
        }

        public void Bind(DemoObjectiveDirector objectiveDirector, QuestRewardService rewardService)
        {
            Unbind();
            director = objectiveDirector;
            rewards = rewardService;
            if (director != null)
            {
                director.ProgressChanged += OnProgressChanged;
            }

            if (rewards != null)
            {
                rewards.CapacityBlocked += OnCapacityBlocked;
                rewards.GrantResolved += OnGrantResolved;
                rewards.ClaimOffered += OnClaimOffered;
            }

            inputLocked = false;
            viewedIndex = 0;
            view?.SetInputLocked(false);
            Render(director?.ReadModel ?? default);
            rewards?.SyncFromProgress();
        }

        public void Unbind()
        {
            if (director != null)
            {
                director.ProgressChanged -= OnProgressChanged;
                director = null;
            }

            if (rewards != null)
            {
                rewards.CapacityBlocked -= OnCapacityBlocked;
                rewards.GrantResolved -= OnGrantResolved;
                rewards.ClaimOffered -= OnClaimOffered;
                rewards = null;
            }

            guidanceOpen = false;
            detailsOpen = false;
            capacityOpen = false;
            dumpOpen = false;
            claimOpen = false;
            inputLocked = false;
            viewedIndex = 0;
            view?.SetGuidanceVisible(false);
            view?.SetDetailsVisible(false);
            view?.SetCapacityChoiceVisible(false);
            view?.SetDumpPanelVisible(false);
            view?.SetClaimVisible(false);
            view?.SetInputLocked(false);
            view?.SetDemoCompleteVisible(false, string.Empty);
        }

        public void Dispose()
        {
            Unbind();
        }

        /// <summary>구조·가스 긴급 상태가 켜지면 튜토리얼이 가리지 않도록 양보한다.</summary>
        public void SetHazardActive(bool active)
        {
            hazardActive = active;
            if (hazardActive && detailsOpen)
            {
                detailsOpen = false;
                view?.SetDetailsVisible(false);
            }

            ApplyHazardYield();
        }

        public void OpenDetails()
        {
            if (director == null || hazardActive)
            {
                return;
            }

            viewedIndex = ResolveCurrentIndex();
            detailsOpen = true;
            RenderDetails();
            view?.SetDetailsVisible(true);
        }

        public void CloseDetails()
        {
            detailsOpen = false;
            view?.SetDetailsVisible(false);
        }

        public void ShowPreviousQuest()
        {
            if (!detailsOpen || viewedIndex <= 0)
            {
                return;
            }

            viewedIndex--;
            RenderDetails();
        }

        public void ShowNextQuest()
        {
            if (!detailsOpen || viewedIndex >= DemoObjectiveIds.RequiredCount - 1)
            {
                return;
            }

            viewedIndex++;
            RenderDetails();
        }

        public void ChooseDumpInventory()
        {
            if (rewards == null || !rewards.HasPending)
            {
                return;
            }

            capacityOpen = false;
            dumpOpen = true;
            view?.SetCapacityChoiceVisible(false);
            RenderDump();
            view?.SetDumpPanelVisible(true);
        }

        public void ChooseForfeitReward()
        {
            if (rewards == null || !rewards.HasPending)
            {
                CloseOverflow();
                return;
            }

            var result = rewards.ForfeitPending();
            if (result.NeedsPlayerChoice)
            {
                ShowCapacity(result);
                return;
            }

            CloseOverflow();
            if (result.IsAwaitingClaim)
            {
                ShowClaim(result);
            }
        }

        /// <summary>클리어 보상 팝업을 닫으면 그때 보상을 지급한다.</summary>
        public void ConfirmClaim()
        {
            if (rewards == null || !rewards.HasPending)
            {
                CloseClaim();
                return;
            }

            CloseClaim();
            var result = rewards.ClaimPending();
            if (result.NeedsPlayerChoice)
            {
                ShowCapacity(result);
                return;
            }

            if (result.IsAwaitingClaim)
            {
                ShowClaim(result);
            }
        }

        public void DumpMineral(string mineralId, int quantity)
        {
            if (rewards == null || !dumpOpen || string.IsNullOrEmpty(mineralId))
            {
                return;
            }

            var dumpQuantity = quantity;
            if (dumpQuantity == int.MaxValue)
            {
                dumpQuantity = QuantityOf(mineralId);
            }

            if (dumpQuantity <= 0)
            {
                return;
            }

            rewards.Dump(mineralId, dumpQuantity);
            if (rewards.CanFitPending())
            {
                var retry = rewards.RetryPending();
                if (retry.NeedsPlayerChoice)
                {
                    RenderDump();
                    return;
                }

                CloseOverflow();
                return;
            }

            RenderDump();
        }

        public void CloseDumpPanel()
        {
            dumpOpen = false;
            view?.SetDumpPanelVisible(false);
            if (rewards == null)
            {
                CloseOverflow();
                return;
            }

            var retry = rewards.RetryPending();
            if (retry.NeedsPlayerChoice)
            {
                ShowCapacity(retry);
                return;
            }

            CloseOverflow();
            if (retry.IsAwaitingClaim)
            {
                ShowClaim(retry);
            }
        }

        public void DismissGuidance()
        {
            if (director == null)
            {
                return;
            }

            var model = director.ReadModel;
            guidanceOpen = false;
            view?.SetGuidanceVisible(false);
            // dismiss 직후 입력 잠금이 남지 않게 한다.
            SetInputLocked(false);

            if (model.ObjectiveId == DemoObjectiveIds.DemoEnd
                || model.IsDemoComplete)
            {
                director.NotifyDemoEndAcknowledged();
            }
        }

        public void Refresh()
        {
            Render(director?.ReadModel ?? default);
        }

        private void OnProgressChanged(DemoObjectiveReadModel model)
        {
            Render(model);
        }

        private void OnCapacityBlocked(QuestRewardGrantResult result)
        {
            if (!result.NeedsPlayerChoice)
            {
                return;
            }

            ShowCapacity(result);
        }

        private void OnClaimOffered(QuestRewardGrantResult result)
        {
            if (!result.IsAwaitingClaim)
            {
                return;
            }

            ShowClaim(result);
        }

        private void OnGrantResolved(QuestRewardGrantResult result)
        {
            if (result.NeedsPlayerChoice)
            {
                return;
            }

            if (!rewards.HasPending)
            {
                CloseOverflow();
                CloseClaim();
            }

            if (detailsOpen)
            {
                RenderDetails();
            }
        }

        private void ShowClaim(QuestRewardGrantResult result)
        {
            dumpOpen = false;
            capacityOpen = false;
            claimOpen = true;
            view?.SetDumpPanelVisible(false);
            view?.SetCapacityChoiceVisible(false);
            var questTitle = DemoObjectiveCatalog.TryGet(result.ObjectiveId, out var definition)
                ? definition.Title
                : result.ObjectiveId;
            view?.SetClaimText(
                "퀘스트 클리어",
                questTitle,
                "클리어 보상: " + result.Reward.FormatKorean(),
                "닫으면 보상이 지급됩니다.");
            view?.SetClaimVisible(true);
        }

        private void CloseClaim()
        {
            claimOpen = false;
            view?.SetClaimVisible(false);
        }

        private void ShowCapacity(QuestRewardGrantResult result)
        {
            dumpOpen = false;
            claimOpen = false;
            capacityOpen = true;
            view?.SetDumpPanelVisible(false);
            view?.SetClaimVisible(false);
            view?.SetCapacityChoiceText(
                "화물 공간이 부족합니다",
                "기존 자원을 버리고 퀘스트 보상("
                + result.Reward.FormatKorean()
                + ")을 받을지, 퀘스트 보상을 버릴지 선택하세요.\n"
                + "판매가 아니라 버리는 것만 가능합니다.");
            view?.SetCapacityChoiceVisible(true);
        }

        private void CloseOverflow()
        {
            capacityOpen = false;
            dumpOpen = false;
            view?.SetCapacityChoiceVisible(false);
            view?.SetDumpPanelVisible(false);
        }

        private void RenderDetails()
        {
            if (viewedIndex < 0)
            {
                viewedIndex = 0;
            }

            if (viewedIndex >= DemoObjectiveCatalog.All.Count)
            {
                viewedIndex = DemoObjectiveCatalog.All.Count - 1;
            }

            var definition = DemoObjectiveCatalog.All[viewedIndex];
            var completed = director?.CompletedCount ?? 0;
            var isDemoComplete = director != null && director.IsDemoComplete;
            var isCleared = viewedIndex < completed || isDemoComplete;
            var isCurrent = !isDemoComplete && viewedIndex == completed;
            var status = isCleared ? "클리어" : isCurrent ? "진행 중" : "미완료";
            var nextAction = isCleared ? string.Empty : definition.NextActionHint;
            view?.SetDetailsText(definition.Title, definition.Description, nextAction);
            view?.SetDetailsStatus(status);
            view?.SetDetailsReward("클리어 보상: " + definition.Reward.FormatKorean());
            view?.SetDetailsIndex((viewedIndex + 1) + "/" + DemoObjectiveIds.RequiredCount);
            view?.SetDetailsNavInteractable(
                viewedIndex > 0,
                viewedIndex < DemoObjectiveIds.RequiredCount - 1);
        }

        private void RenderDump()
        {
            view?.SetDumpSummary(rewards != null ? rewards.FormatDumpSummary() : string.Empty);
            view?.SetDumpRows(rewards != null ? rewards.GetDumpRows() : null);
        }

        private int ResolveCurrentIndex()
        {
            if (director == null)
            {
                return 0;
            }

            if (director.IsDemoComplete)
            {
                return DemoObjectiveIds.RequiredCount - 1;
            }

            var index = DemoObjectiveCatalog.IndexOf(director.CurrentObjectiveId);
            return index < 0 ? 0 : index;
        }

        private int QuantityOf(string mineralId)
        {
            var rows = rewards?.GetDumpRows();
            if (rows == null)
            {
                return 0;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].MineralId == mineralId)
                {
                    return rows[i].Quantity;
                }
            }

            return 0;
        }

        private void Render(DemoObjectiveReadModel model)
        {
            view?.SetObjective(model);
            if (detailsOpen)
            {
                viewedIndex = ResolveCurrentIndex();
                RenderDetails();
            }
            else
            {
                view?.SetDetailsText(model.Title, model.Description, model.NextActionHint);
            }

            if (model.ShowsDismissibleGuidance)
            {
                guidanceOpen = true;
                view?.SetGuidanceText(model.GuidanceTitle, model.GuidanceBody);
                // 닫기형 안내는 위험 중에도 켠다. 숨기면 경로 안내가 닫히지 못한다.
                view?.SetGuidanceVisible(true);
                view?.SetDemoCompleteVisible(false, string.Empty);
            }
            else
            {
                guidanceOpen = false;
                view?.SetGuidanceVisible(false);
                view?.SetDemoCompleteVisible(false, string.Empty);
            }

            // 안내는 닫기 가능. 전역 입력을 장시간 잠그지 않는다.
            SetInputLocked(false);
            ApplyHazardYield();
        }

        private void ApplyHazardYield()
        {
            var yield = UiLayerPriority.ShouldYieldTutorialInput(hazardActive);
            view?.SetHazardYield(yield);
            if (!guidanceOpen)
            {
                return;
            }

            // 닫기형 안내는 위험 HUD보다 아래 정렬만 하고 패널은 유지한다.
            if (yield && !ShouldKeepGuidanceVisibleDuringHazard())
            {
                view?.SetGuidanceVisible(false);
                return;
            }

            view?.SetGuidanceVisible(true);
        }

        private bool ShouldKeepGuidanceVisibleDuringHazard()
        {
            return director != null && director.ReadModel.ShowsDismissibleGuidance;
        }

        private void SetInputLocked(bool locked)
        {
            inputLocked = locked;
            view?.SetInputLocked(locked);
        }
    }
}
