using SubTerra.App.Save;
using SubTerra.Shared;

namespace SubTerra.App.Run
{
    /// <summary>활성 체크포인트가 있으면 우선하고, 아니면 Surface 안전 지점으로 폴백한다.</summary>
    public static class RescueCoordinator
    {
        public static RunReturnTarget ResolveReturnTarget(OutpostStatusDto status)
        {
            if (status != null
                && status.isActive
                && !string.IsNullOrWhiteSpace(status.checkpointId))
            {
                return new RunReturnTarget(
                    RunReturnTargetKind.OutpostCheckpoint,
                    status.checkpointId,
                    status.checkpointX,
                    status.checkpointY);
            }

            return new RunReturnTarget(
                status == null
                    ? RunReturnTargetKind.Surface
                    : RunReturnTargetKind.SurfaceFallback);
        }
    }
}
