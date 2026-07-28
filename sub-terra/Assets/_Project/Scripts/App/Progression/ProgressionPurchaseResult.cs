namespace SubTerra.App.Progression
{
    public enum ProgressionPurchaseStatus
    {
        Success = 0,
        InvalidRequest = 1,
        UpgradeNotFound = 2,
        MaximumLevel = 3,
        InvalidDefinition = 4,
        InsufficientResources = 5,
        SpendFailed = 6,
        DependencyMissing = 7,
        Busy = 8
    }

    /// <summary>업그레이드 구매 결과. UI 메시지와 진단 문자열을 분리한다.</summary>
    public readonly struct ProgressionPurchaseResult
    {
        public ProgressionPurchaseStatus Status { get; }
        public string UpgradeId { get; }
        public int PreviousLevel { get; }
        public int CurrentLevel { get; }
        public float EffectValue { get; }
        public string UserMessage { get; }
        public string Diagnostic { get; }

        public bool IsSuccess => Status == ProgressionPurchaseStatus.Success;

        private ProgressionPurchaseResult(
            ProgressionPurchaseStatus status,
            string upgradeId,
            int previousLevel,
            int currentLevel,
            float effectValue,
            string userMessage,
            string diagnostic)
        {
            Status = status;
            UpgradeId = upgradeId ?? string.Empty;
            PreviousLevel = previousLevel;
            CurrentLevel = currentLevel;
            EffectValue = effectValue;
            UserMessage = userMessage ?? string.Empty;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public static ProgressionPurchaseResult Success(
            string upgradeId,
            int previousLevel,
            int currentLevel,
            float effectValue)
        {
            return new ProgressionPurchaseResult(
                ProgressionPurchaseStatus.Success,
                upgradeId,
                previousLevel,
                currentLevel,
                effectValue,
                "업그레이드 구매 완료",
                string.Empty);
        }

        public static ProgressionPurchaseResult Fail(
            ProgressionPurchaseStatus status,
            string upgradeId,
            int currentLevel,
            string userMessage,
            string diagnostic)
        {
            return new ProgressionPurchaseResult(
                status,
                upgradeId,
                currentLevel,
                currentLevel,
                0f,
                userMessage,
                diagnostic);
        }
    }

    /// <summary>Phase K 저장 시스템이 구독할 업그레이드 구매 완료 알림.</summary>
    public readonly struct ProgressionAutoSaveRequest
    {
        public string UpgradeId { get; }
        public int Level { get; }

        public ProgressionAutoSaveRequest(string upgradeId, int level)
        {
            UpgradeId = upgradeId ?? string.Empty;
            Level = level;
        }
    }
}
