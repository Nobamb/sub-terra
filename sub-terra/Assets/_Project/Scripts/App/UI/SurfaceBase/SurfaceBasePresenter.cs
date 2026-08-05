using System;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;

namespace SubTerra.App.UI.SurfaceBase
{
    /// <summary>J가 확정해 저장한 Run 결과를 Surface가 읽기 전용으로 소비하는 모델.</summary>
    public readonly struct SurfaceRunResultReadModel
    {
        public bool HasCompletedReturn { get; }
        public int MaximumDepth { get; }
        public float CargoWeight { get; }
        public float UnsettledValue { get; }
        public bool IsSafe { get; }
        public string StructuralRisk { get; }
        public string GasExposure { get; }

        public SurfaceRunResultReadModel(bool hasCompletedReturn, int maximumDepth, float cargoWeight, float unsettledValue, bool isSafe, string structuralRisk, string gasExposure)
        {
            HasCompletedReturn = hasCompletedReturn;
            MaximumDepth = Math.Max(0, maximumDepth);
            CargoWeight = Math.Max(0f, cargoWeight);
            UnsettledValue = Math.Max(0f, unsettledValue);
            IsSafe = isSafe;
            StructuralRisk = structuralRisk ?? string.Empty;
            GasExposure = gasExposure ?? string.Empty;
        }
    }

    public interface ISurfaceBaseView
    {
        void SetGoals(int completedObjectives, string summary);
        void SetEnergy(int current, int max, int explorationCost);
        void SetDeepZoneLock(bool unlocked, string reason);
        void SetRecentRun(int depth, bool isSafe, string structural, string gas);
        void SetReturnResult(SurfaceRunResultReadModel result);
        void SetExplorationBusy(bool busy);
        void SetMessage(string message);
    }

    /// <summary>
    /// Surface Base 읽기 모델·탐사 시작 UI.
    /// 탐사 단일 비행 가드는 SaveRuntimeController.TryStartExploration 한 곳만 사용한다.
    /// 판매/제작/업그레이드는 Economy/Progression Presenter를 재사용하며 여기서 경제 연산을 하지 않는다.
    /// </summary>
    public sealed class SurfaceBasePresenter : IDisposable
    {
        private readonly ISurfaceBaseView view;
        private GameState state;
        private ProgressionService progression;
        private int explorationEnergyCost;

        public SurfaceBasePresenter(ISurfaceBaseView surfaceView)
        {
            view = surfaceView ?? throw new ArgumentNullException(nameof(surfaceView));
        }

        public void Bind(
            GameState gameState,
            ProgressionService progressionService,
            int energyCost = SaveRuntimeController.MineElevatorEnergyCost)
        {
            Unbind();
            state = gameState;
            progression = progressionService;
            explorationEnergyCost = Math.Max(0, energyCost);
            if (state != null)
            {
                state.EnergyChanged += OnEnergyChanged;
            }

            RefreshReadModel();
        }

        public void Unbind()
        {
            if (state != null)
            {
                state.EnergyChanged -= OnEnergyChanged;
            }

            state = null;
            progression = null;
        }

        public void RefreshReadModel()
        {
            if (state == null)
            {
                view.SetGoals(0, "상태 없음");
                view.SetEnergy(0, 0, explorationEnergyCost);
                view.SetDeepZoneLock(false, "상태 없음");
                view.SetRecentRun(0, true, "-", "-");
                return;
            }

            RenderEnergy(state.GetEnergy());

            var completed = state.Progress.CompletedObjectives;
            var objectiveId = state.Progress.CurrentObjectiveId;
            var goalSummary = string.IsNullOrEmpty(objectiveId)
                ? "완료 목표 " + completed + "개"
                : "완료 " + completed + " · 현재 " + objectiveId;
            view.SetGoals(completed, goalSummary);

            ZoneAccessResult access;
            if (progression != null)
            {
                // 읽기만이 아니라 조건 충족 시 실제 잠금 커밋(DeepZoneAccessChanged)까지 수행한다.
                access = progression.TryUnlockDeepZone(completed);
            }
            else
            {
                access = new ZoneAccessResult(false, false, "진행도 서비스 없음");
            }

            view.SetDeepZoneLock(access.IsUnlocked, access.Reason);
            view.SetRecentRun(
                state.Run.MaximumDepth,
                state.Run.IsSafe,
                state.Run.StructuralRisk.ToString(),
                state.Run.GasExposure.ToString());
            view.SetReturnResult(CreateReturnResult(state));
        }

        private void OnEnergyChanged(EnergyReadModel energy)
        {
            RenderEnergy(energy);
        }

        private void RenderEnergy(EnergyReadModel energy)
        {
            view.SetEnergy(energy.Current, energy.Max, explorationEnergyCost);
        }

        private static SurfaceRunResultReadModel CreateReturnResult(GameState gameState)
        {
            if (gameState?.Run == null || gameState.Run.LifecyclePhase != RunLifecyclePhase.Completed)
            {
                return default;
            }

            var inventory = gameState.GetInventory();
            return new SurfaceRunResultReadModel(
                true,
                gameState.Run.MaximumDepth,
                inventory.CargoWeight,
                inventory.UnsettledValue,
                gameState.Run.IsSafe,
                gameState.Run.StructuralRisk.ToString(),
                gameState.Run.GasExposure.ToString());
        }

        /// <summary>
        /// 탐사 시작. startExploration은 런타임 단일 가드(TryStartExploration)를 호출해야 한다.
        /// 성공 시 busy 유지(Scene 전환), 실패 시 busy 해제·메시지 표시로 재시도 가능하게 한다.
        /// </summary>
        /// <param name="startExploration">
        /// (success, reason) — success=false면 reason을 사용자 메시지로 쓴다.
        /// null이면 즉시 실패 처리한다.
        /// </param>
        public bool RequestExplorationStart(Func<(bool success, string reason)> startExploration)
        {
            if (state == null)
            {
                CompleteExplorationFailure("탐사를 시작할 수 없습니다. 상태가 없습니다.");
                return false;
            }

            if (startExploration == null)
            {
                CompleteExplorationFailure("탐사 시작 경로가 없습니다.");
                return false;
            }

            // 실제 가드/로드 전에 busy를 켜 연타 UI를 막되, 실패 시 반드시 해제한다.
            view.SetExplorationBusy(true);
            view.SetMessage("Calling · 엘리베이터 호출 중");

            var result = startExploration();
            if (!result.success)
            {
                CompleteExplorationFailure(
                    string.IsNullOrEmpty(result.reason)
                        ? "탐사 진입 실패"
                        : result.reason);
                return false;
            }

            return true;
        }

        /// <summary>탐사 실패 시 busy 해제. 버튼 재활성으로 재시도 가능.</summary>
        public void CompleteExplorationFailure(string message)
        {
            view.SetExplorationBusy(false);
            view.SetMessage(string.IsNullOrEmpty(message) ? "탐사 진입 실패" : message);
        }

        public void Dispose()
        {
            Unbind();
        }
    }
}
