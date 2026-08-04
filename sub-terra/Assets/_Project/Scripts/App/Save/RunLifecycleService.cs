using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Save
{
    public enum RunReturnTargetKind
    {
        Surface,
        OutpostCheckpoint,
        SurfaceFallback
    }

    public readonly struct RunReturnTarget
    {
        public RunReturnTargetKind Kind { get; }
        public string CheckpointId { get; }
        public int X { get; }
        public int Y { get; }

        public RunReturnTarget(RunReturnTargetKind kind, string checkpointId = "", int x = 0, int y = 0)
        {
            Kind = kind;
            CheckpointId = checkpointId ?? string.Empty;
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Run 시작·귀환·완료의 유일한 상태 전이 지점.
    /// 실제 Scene 로드 실패 시 AbortPendingReturn으로 Active 상태를 그대로 복구한다.
    /// </summary>
    public sealed class RunLifecycleService
    {
        private readonly GameState gameState;

        public RunLifecycleService(GameState state)
        {
            gameState = state;
        }

        public RunLifecyclePhase Phase => gameState?.Run?.LifecyclePhase ?? RunLifecyclePhase.Ready;
        public RunReturnTarget PendingReturnTarget { get; private set; }

        public bool TryBeginExploration(out string reason)
        {
            if (!IsComplete())
            {
                reason = "게임 상태가 준비되지 않았습니다.";
                return false;
            }

            if (Phase == RunLifecyclePhase.Active || Phase == RunLifecyclePhase.Returning)
            {
                reason = "이미 진행 중인 탐사가 있습니다.";
                return false;
            }

            gameState.BeginRun();
            reason = string.Empty;
            return true;
        }

        public bool TryPrepareNormalReturn(OutpostStatusDto status, out RunReturnTarget target, out string reason)
        {
            target = new RunReturnTarget(RunReturnTargetKind.Surface);
            if (!IsComplete() || Phase != RunLifecyclePhase.Active)
            {
                reason = "진행 중인 탐사가 아니어서 귀환할 수 없습니다.";
                return false;
            }

            target = ResolveReturnTarget(status);
            PendingReturnTarget = target;
            gameState.SetRunLifecyclePhase(RunLifecyclePhase.Returning);
            reason = string.Empty;
            return true;
        }

        public bool CompleteNormalReturn(out string reason)
        {
            if (!IsComplete() || Phase != RunLifecyclePhase.Returning)
            {
                reason = "완료할 귀환 요청이 없습니다.";
                return false;
            }

            gameState.SetRunLifecyclePhase(RunLifecyclePhase.Completed);
            reason = string.Empty;
            return true;
        }

        public void AbortPendingReturn()
        {
            if (IsComplete() && Phase == RunLifecyclePhase.Returning)
            {
                gameState.SetRunLifecyclePhase(RunLifecyclePhase.Active);
            }

            PendingReturnTarget = default;
        }

        private bool IsComplete()
        {
            return GameState.IsComplete(gameState);
        }

        private static RunReturnTarget ResolveReturnTarget(OutpostStatusDto status)
        {
            if (status != null
                && status.isActive
                && status.isInInteractionRange
                && !string.IsNullOrWhiteSpace(status.checkpointId))
            {
                return new RunReturnTarget(
                    RunReturnTargetKind.OutpostCheckpoint,
                    status.checkpointId,
                    status.checkpointX,
                    status.checkpointY);
            }

            return status == null || string.IsNullOrWhiteSpace(status.checkpointId)
                ? new RunReturnTarget(RunReturnTargetKind.Surface)
                : new RunReturnTarget(RunReturnTargetKind.SurfaceFallback);
        }
    }
}
