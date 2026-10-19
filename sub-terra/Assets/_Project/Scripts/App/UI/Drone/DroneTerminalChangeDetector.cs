using SubTerra.App.Drone;

namespace SubTerra.App.UI.Drone
{
    /// <summary>다시 분석한 결과가 직전과 비교해 무엇이 바뀌었는지.</summary>
    public readonly struct DroneTerminalAnalysisChange
    {
        /// <summary>추천 행동이 바뀌었거나 첫 분석이다. 대사·추천·근거를 다시 타이핑한다.</summary>
        public bool RecommendationChanged { get; }
        public int PreviousReasonLineCount { get; }
        public int ReasonLineCount { get; }

        /// <summary>같은 추천에서 근거 줄만 늘었다. 새 줄만 순서대로 보인다.</summary>
        public bool ReasonLinesAdded => !RecommendationChanged && ReasonLineCount > PreviousReasonLineCount;

        public DroneTerminalAnalysisChange(bool recommendationChanged, int previousReasonLineCount, int reasonLineCount)
        {
            RecommendationChanged = recommendationChanged;
            PreviousReasonLineCount = previousReasonLineCount;
            ReasonLineCount = reasonLineCount;
        }
    }

    /// <summary>추천 행동 변경과 PRIORITY 진입을 감지한다. 같은 상태가 이어지면 반복하지 않는다.</summary>
    public sealed class DroneTerminalChangeDetector
    {
        private bool hasAnalysis;
        private DroneAction lastAction;
        private int lastReasonLineCount;
        private DroneOperationalState lastState = DroneOperationalState.Idle;

        public DroneTerminalAnalysisChange ObserveAnalysis(DroneAction action, int reasonLineCount)
        {
            var changed = !hasAnalysis || action != lastAction;
            var previous = hasAnalysis ? lastReasonLineCount : 0;
            hasAnalysis = true;
            lastAction = action;
            lastReasonLineCount = reasonLineCount;
            return new DroneTerminalAnalysisChange(changed, previous, reasonLineCount);
        }

        /// <summary>PRIORITY가 아닌 상태에서 PRIORITY로 들어온 순간에만 true.</summary>
        public bool ObserveState(DroneOperationalState state)
        {
            var entered = state == DroneOperationalState.Priority
                && lastState != DroneOperationalState.Priority;
            lastState = state;
            return entered;
        }
    }
}
