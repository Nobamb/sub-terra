namespace SubTerra.App.Tutorial
{
    /// <summary>단일 데모 목표 정의. 정적 표 항목이며 런타임 State와 분리한다.</summary>
    public sealed class DemoObjectiveDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public string NextActionHint { get; }
        public DemoProgressSignal RequiredSignal { get; }
        public string NextObjectiveId { get; }
        public bool IsTerminal { get; }
        public bool ShowsDismissibleGuidance { get; }

        public DemoObjectiveDefinition(
            string id,
            string title,
            string description,
            string nextActionHint,
            DemoProgressSignal requiredSignal,
            string nextObjectiveId,
            bool isTerminal = false,
            bool showsDismissibleGuidance = false)
        {
            Id = id ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            NextActionHint = nextActionHint ?? string.Empty;
            RequiredSignal = requiredSignal;
            NextObjectiveId = nextObjectiveId ?? string.Empty;
            IsTerminal = isTerminal;
            ShowsDismissibleGuidance = showsDismissibleGuidance;
        }
    }

    /// <summary>UI 읽기 전용 목표 표시 모델.</summary>
    public readonly struct DemoObjectiveReadModel
    {
        public string ObjectiveId { get; }
        public string Title { get; }
        public string Description { get; }
        public string NextActionHint { get; }
        public int CompletedCount { get; }
        public int TotalCount { get; }
        public bool IsTerminal { get; }
        public bool IsDemoComplete { get; }
        public bool ShowsDismissibleGuidance { get; }

        public DemoObjectiveReadModel(
            string objectiveId,
            string title,
            string description,
            string nextActionHint,
            int completedCount,
            int totalCount,
            bool isTerminal,
            bool isDemoComplete,
            bool showsDismissibleGuidance)
        {
            ObjectiveId = objectiveId ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            NextActionHint = nextActionHint ?? string.Empty;
            CompletedCount = completedCount < 0 ? 0 : completedCount;
            TotalCount = totalCount < 0 ? 0 : totalCount;
            IsTerminal = isTerminal;
            IsDemoComplete = isDemoComplete;
            ShowsDismissibleGuidance = showsDismissibleGuidance;
        }
    }

    /// <summary>전이 시도 결과. Advanced가 false면 현재 목표가 유지된다.</summary>
    public readonly struct DemoTransitionResult
    {
        public bool Advanced { get; }
        public string PreviousObjectiveId { get; }
        public string CurrentObjectiveId { get; }
        public int CompletedCount { get; }
        public bool IsTerminal { get; }
        public bool IsDemoComplete { get; }
        public string RejectReason { get; }

        public DemoTransitionResult(
            bool advanced,
            string previousObjectiveId,
            string currentObjectiveId,
            int completedCount,
            bool isTerminal,
            bool isDemoComplete,
            string rejectReason)
        {
            Advanced = advanced;
            PreviousObjectiveId = previousObjectiveId ?? string.Empty;
            CurrentObjectiveId = currentObjectiveId ?? string.Empty;
            CompletedCount = completedCount < 0 ? 0 : completedCount;
            IsTerminal = isTerminal;
            IsDemoComplete = isDemoComplete;
            RejectReason = rejectReason ?? string.Empty;
        }

        public static DemoTransitionResult Rejected(
            string currentObjectiveId,
            int completedCount,
            bool isTerminal,
            bool isDemoComplete,
            string reason)
        {
            return new DemoTransitionResult(
                false,
                currentObjectiveId,
                currentObjectiveId,
                completedCount,
                isTerminal,
                isDemoComplete,
                reason);
        }
    }
}
