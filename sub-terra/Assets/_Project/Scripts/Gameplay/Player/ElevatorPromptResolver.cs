using SubTerra.Shared;

namespace SubTerra.Gameplay.Player
{
    public enum ElevatorPromptKind
    {
        None,
        Board,
        Return
    }

    /// <summary>엘리베이터 상태에서 월드 홀로그램 안내 종류와 문구를 결정하는 순수 규칙.</summary>
    public static class ElevatorPromptResolver
    {
        public const string BoardLabel = "E키를 눌러 탑승";
        public const string ReturnLabel = "E키를 눌러 귀환";

        /// <param name="travelInProgress">탑승자가 잠겼거나 Scene 전환이 승인된 상태.</param>
        public static ElevatorPromptKind Resolve(
            ElevatorTravelState state,
            bool hasRider,
            bool travelInProgress,
            ElevatorDestination destination)
        {
            if (!hasRider || travelInProgress)
            {
                return ElevatorPromptKind.None;
            }

            // Calling/Moving/Blocked 중에는 E 안내를 띄우지 않는다.
            if (state != ElevatorTravelState.Idle && state != ElevatorTravelState.Arrived)
            {
                return ElevatorPromptKind.None;
            }

            return destination == ElevatorDestination.SurfaceBase
                ? ElevatorPromptKind.Return
                : ElevatorPromptKind.Board;
        }

        public static string GetLabel(ElevatorPromptKind kind)
        {
            return kind switch
            {
                ElevatorPromptKind.Board => BoardLabel,
                ElevatorPromptKind.Return => ReturnLabel,
                _ => string.Empty
            };
        }
    }
}
