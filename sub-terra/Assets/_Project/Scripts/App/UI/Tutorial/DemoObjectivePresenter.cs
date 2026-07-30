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
        private bool inputLocked;

        public bool IsBound => director != null;
        public bool IsGuidanceOpen => guidanceOpen;
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
            inputLocked = false;
            view?.SetGuidanceVisible(false);
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
            ApplyHazardYield();
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

            if (model.IsDemoComplete || model.ObjectiveId == DemoObjectiveIds.DemoEnd)
            {
                view?.SetDemoCompleteVisible(
                    true,
                    model.Description);
                guidanceOpen = model.ShowsDismissibleGuidance;
                view?.SetGuidanceVisible(guidanceOpen && !hazardActive);
                view?.SetGuidanceText(model.Title, model.Description);
            }
            else if (model.ShowsDismissibleGuidance)
            {
                guidanceOpen = true;
                view?.SetGuidanceText(model.Title, model.Description);
                view?.SetGuidanceVisible(!hazardActive);
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
            if (yield && guidanceOpen)
            {
                // 위험 중에는 일반 안내 패널을 숨겨 경고가 가려지지 않게 한다.
                view?.SetGuidanceVisible(false);
            }
            else if (!yield && guidanceOpen)
            {
                view?.SetGuidanceVisible(true);
            }
        }

        private void SetInputLocked(bool locked)
        {
            inputLocked = locked;
            view?.SetInputLocked(locked);
        }
    }
}
