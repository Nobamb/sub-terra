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
        private bool hazardActive;
        private bool guidanceOpen;
        private bool detailsOpen;
        private bool inputLocked;

        public bool IsBound => director != null;
        public bool IsGuidanceOpen => guidanceOpen;
        public bool IsDetailsOpen => detailsOpen;
        public bool IsInputLocked => inputLocked;
        public bool HazardActive => hazardActive;

        public DemoObjectivePresenter(IDemoObjectiveView objectiveView)
        {
            view = objectiveView;
        }

        public void Bind(DemoObjectiveDirector objectiveDirector)
        {
            Unbind();
            director = objectiveDirector;
            if (director != null)
            {
                director.ProgressChanged += OnProgressChanged;
            }

            inputLocked = false;
            view?.SetInputLocked(false);
            Render(director?.ReadModel ?? default);
        }

        public void Unbind()
        {
            if (director != null)
            {
                director.ProgressChanged -= OnProgressChanged;
                director = null;
            }

            guidanceOpen = false;
            detailsOpen = false;
            inputLocked = false;
            view?.SetGuidanceVisible(false);
            view?.SetDetailsVisible(false);
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

            var model = director.ReadModel;
            detailsOpen = true;
            view?.SetDetailsText(model.Title, model.Description, model.NextActionHint);
            view?.SetDetailsVisible(true);
        }

        public void CloseDetails()
        {
            detailsOpen = false;
            view?.SetDetailsVisible(false);
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

            if (model.ObjectiveId == DemoObjectiveIds.PathGuide)
            {
                director.NotifyGuidanceAcknowledged();
            }
            else if (model.ObjectiveId == DemoObjectiveIds.ReturnRecommend)
            {
                director.NotifyReturnRecommendationAcknowledged();
            }
            else if (model.ObjectiveId == DemoObjectiveIds.DemoEnd
                || model.IsDemoComplete)
            {
                director.NotifyDemoEndAcknowledged();
            }
            else if (model.ObjectiveId == DemoObjectiveIds.ExploreStart)
            {
                // 시작 안내는 닫기만 하고, 탐사 준비 신호는 Binder가 보낸다.
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

        private void Render(DemoObjectiveReadModel model)
        {
            view?.SetObjective(model);
            view?.SetDetailsText(model.Title, model.Description, model.NextActionHint);

            if (model.IsDemoComplete || model.ObjectiveId == DemoObjectiveIds.DemoEnd)
            {
                view?.SetDemoCompleteVisible(
                    true,
                    model.Description);
                guidanceOpen = false;
                view?.SetGuidanceVisible(false);
            }
            else if (model.ShowsDismissibleGuidance)
            {
                guidanceOpen = true;
                view?.SetGuidanceText(model.Title, model.Description);
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
