namespace SubTerra.App.Tutorial
{
    /// <summary>
    /// 이벤트 신호만으로 13단계 목표를 전진시키는 순수 전이 로직.
    /// 타일·구조·가스·설치 성공을 계산·복제하지 않는다.
    /// </summary>
    public sealed class DemoObjectiveTransitionEngine
    {
        private string currentObjectiveId;
        private int completedCount;
        private bool isDemoComplete;

        public string CurrentObjectiveId => currentObjectiveId;
        public int CompletedCount => completedCount;
        public bool IsDemoComplete => isDemoComplete;

        public DemoObjectiveTransitionEngine()
        {
            Reset();
        }

        public void Reset()
        {
            currentObjectiveId = DemoObjectiveIds.ExploreStart;
            completedCount = 0;
            isDemoComplete = false;
        }

        /// <summary>세이브 복원. unknown ID는 카탈로그 폴백으로 안전 목표에 붙인다.</summary>
        public void Restore(string savedObjectiveId, int savedCompletedCount, bool savedDemoComplete = false)
        {
            if (savedCompletedCount < 0)
            {
                savedCompletedCount = 0;
            }

            completedCount = savedCompletedCount;
            isDemoComplete = savedDemoComplete;
            currentObjectiveId = DemoObjectiveCatalog.ResolveObjectiveId(
                savedObjectiveId,
                completedCount);

            if (isDemoComplete)
            {
                currentObjectiveId = DemoObjectiveIds.DemoEnd;
                if (completedCount < DemoObjectiveIds.RequiredCount)
                {
                    completedCount = DemoObjectiveIds.RequiredCount;
                }
            }
        }

        public DemoObjectiveReadModel GetReadModel()
        {
            return DemoObjectiveCatalog.ToReadModel(
                currentObjectiveId,
                completedCount,
                isDemoComplete);
        }

        /// <summary>
        /// 현재 목표가 요구하는 신호와 일치할 때만 정확히 한 단계 전진한다.
        /// 순서 밖 신호는 거부하며 무단 스킵하지 않는다.
        /// </summary>
        public DemoTransitionResult TryAdvance(DemoProgressSignal signal)
        {
            if (isDemoComplete)
            {
                return DemoTransitionResult.Rejected(
                    currentObjectiveId,
                    completedCount,
                    true,
                    true,
                    "demo-already-complete");
            }

            if (signal == DemoProgressSignal.None)
            {
                return DemoTransitionResult.Rejected(
                    currentObjectiveId,
                    completedCount,
                    false,
                    false,
                    "empty-signal");
            }

            if (!DemoObjectiveCatalog.TryGet(currentObjectiveId, out var current))
            {
                currentObjectiveId = DemoObjectiveCatalog.ResolveObjectiveId(
                    currentObjectiveId,
                    completedCount);
                if (!DemoObjectiveCatalog.TryGet(currentObjectiveId, out current))
                {
                    return DemoTransitionResult.Rejected(
                        currentObjectiveId,
                        completedCount,
                        false,
                        false,
                        "unknown-current");
                }
            }

            if (current.RequiredSignal != signal)
            {
                return DemoTransitionResult.Rejected(
                    currentObjectiveId,
                    completedCount,
                    current.IsTerminal,
                    false,
                    "signal-mismatch");
            }

            var previous = currentObjectiveId;
            completedCount++;

            if (current.IsTerminal || string.IsNullOrEmpty(current.NextObjectiveId))
            {
                isDemoComplete = true;
                currentObjectiveId = DemoObjectiveIds.DemoEnd;
                return new DemoTransitionResult(
                    true,
                    previous,
                    currentObjectiveId,
                    completedCount,
                    true,
                    true,
                    string.Empty);
            }

            currentObjectiveId = current.NextObjectiveId;
            var nextIsTerminal = DemoObjectiveCatalog.TryGet(currentObjectiveId, out var next)
                && next.IsTerminal;
            return new DemoTransitionResult(
                true,
                previous,
                currentObjectiveId,
                completedCount,
                nextIsTerminal,
                false,
                string.Empty);
        }

#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
        /// <summary>Development 전용 강제 1단계 전진. QA/Release 경로에서는 컴파일되지 않는다.</summary>
        public DemoTransitionResult DebugForceAdvance()
        {
            if (isDemoComplete)
            {
                return DemoTransitionResult.Rejected(
                    currentObjectiveId,
                    completedCount,
                    true,
                    true,
                    "demo-already-complete");
            }

            if (!DemoObjectiveCatalog.TryGet(currentObjectiveId, out var current))
            {
                return DemoTransitionResult.Rejected(
                    currentObjectiveId,
                    completedCount,
                    false,
                    false,
                    "unknown-current");
            }

            return TryAdvance(current.RequiredSignal);
        }
#endif
    }
}
