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
        Busy = 8,
        /// <summary>prompt-B 118: 트리 해금 조건 미충족. 비용은 차감되지 않는다.</summary>
        Locked = 9
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
        /// <summary>이 구매로 새로 해금된 업그레이드 ID. UI 연출 전용이며 상태 판정에는 쓰지 않는다.</summary>
        public System.Collections.Generic.IReadOnlyList<string> NewlyUnlockedUpgradeIds { get; }

        public bool IsSuccess => Status == ProgressionPurchaseStatus.Success;

        private ProgressionPurchaseResult(
            ProgressionPurchaseStatus status,
            string upgradeId,
            int previousLevel,
            int currentLevel,
            float effectValue,
            string userMessage,
            string diagnostic,
            System.Collections.Generic.IReadOnlyList<string> newlyUnlocked = null)
        {
            Status = status;
            UpgradeId = upgradeId ?? string.Empty;
            PreviousLevel = previousLevel;
            CurrentLevel = currentLevel;
            EffectValue = effectValue;
            UserMessage = userMessage ?? string.Empty;
            Diagnostic = diagnostic ?? string.Empty;
            NewlyUnlockedUpgradeIds = newlyUnlocked ?? System.Array.Empty<string>();
        }

        public static ProgressionPurchaseResult Success(
            string upgradeId,
            int previousLevel,
            int currentLevel,
            float effectValue,
            System.Collections.Generic.IReadOnlyList<string> newlyUnlocked = null)
        {
            return new ProgressionPurchaseResult(
                ProgressionPurchaseStatus.Success,
                upgradeId,
                previousLevel,
                currentLevel,
                effectValue,
                "업그레이드 구매 완료",
                string.Empty,
                newlyUnlocked);
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
