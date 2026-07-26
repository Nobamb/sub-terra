namespace SubTerra.App.Economy
{
    /// <summary>판매·제작·차감 거래 결과 분류.</summary>
    public enum EconomyTransactionStatus
    {
        /// <summary>인벤토리 차감과 골드/비용 반영이 모두 성공했다.</summary>
        Success = 0,

        /// <summary>보유 수량 부족으로 지불·판매 불가.</summary>
        InsufficientResources = 1,

        /// <summary>빈 ID·0·음수·카탈로그 누락 등 잘못된 요청.</summary>
        InvalidRequest = 2,

        /// <summary>골드 지급 시 정수 오버플로 위험이 있어 거부.</summary>
        GoldOverflow = 3,

        /// <summary>Runtime Prefab/배치 생성 실패. 자원은 차감하지 않음.</summary>
        PlacementFailed = 4,

        /// <summary>배치 성공 후 재검증에서 차감 실패(경합 등). 상태는 차감 전으로 유지.</summary>
        SpendFailed = 5,

        /// <summary>처리 중 중복 제출이 거절됨.</summary>
        Busy = 6,

        /// <summary>카탈로그 또는 필수 의존성이 없다.</summary>
        DependencyMissing = 7
    }

    /// <summary>거래 종류. 자동 저장 구독자가 종류별로 분기할 수 있게 한다.</summary>
    public enum EconomyTransactionKind
    {
        Sell = 0,
        Spend = 1,
        Craft = 2
    }

    /// <summary>
    /// 판매·제작 거래 결과.
    /// 성공 시에만 ChangedItemId/ChangedQuantity/GoldDelta가 의미 있는 값을 갖는다.
    /// UI는 Status와 UserMessage만 표시하고, Diagnostic은 디버그용으로 분리한다.
    /// </summary>
    public readonly struct EconomyTransactionResult
    {
        public EconomyTransactionStatus Status { get; }
        public EconomyTransactionKind Kind { get; }
        public string ChangedItemId { get; }
        public int ChangedQuantity { get; }
        public int GoldDelta { get; }
        public string UserMessage { get; }
        public string Diagnostic { get; }

        public bool IsSuccess => Status == EconomyTransactionStatus.Success;

        public EconomyTransactionResult(
            EconomyTransactionStatus status,
            EconomyTransactionKind kind,
            string changedItemId,
            int changedQuantity,
            int goldDelta,
            string userMessage,
            string diagnostic)
        {
            Status = status;
            Kind = kind;
            ChangedItemId = changedItemId ?? string.Empty;
            ChangedQuantity = changedQuantity;
            GoldDelta = goldDelta;
            UserMessage = userMessage ?? string.Empty;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public static EconomyTransactionResult Fail(
            EconomyTransactionStatus status,
            EconomyTransactionKind kind,
            string userMessage,
            string diagnostic = null)
        {
            return new EconomyTransactionResult(
                status,
                kind,
                string.Empty,
                0,
                0,
                userMessage,
                diagnostic);
        }

        public static EconomyTransactionResult OkSell(
            string mineralId,
            int quantity,
            int goldGained,
            string userMessage = null)
        {
            return new EconomyTransactionResult(
                EconomyTransactionStatus.Success,
                EconomyTransactionKind.Sell,
                mineralId,
                quantity,
                goldGained,
                userMessage ?? "판매 완료",
                null);
        }

        public static EconomyTransactionResult OkSpend(
            string summaryItemId,
            int totalQuantity,
            string userMessage = null)
        {
            return new EconomyTransactionResult(
                EconomyTransactionStatus.Success,
                EconomyTransactionKind.Spend,
                summaryItemId,
                totalQuantity,
                0,
                userMessage ?? "비용 차감 완료",
                null);
        }

        public static EconomyTransactionResult OkCraft(
            string buildingId,
            string userMessage = null)
        {
            return new EconomyTransactionResult(
                EconomyTransactionStatus.Success,
                EconomyTransactionKind.Craft,
                buildingId,
                1,
                0,
                userMessage ?? "제작·설치 완료",
                null);
        }
    }

    /// <summary>
    /// Phase K 자동 저장 요청 훅.
    /// 실제 JSON 세이브는 구현하지 않고, 성공 거래 직후 한 번만 발행한다.
    /// </summary>
    public readonly struct EconomyAutoSaveRequest
    {
        public EconomyTransactionKind Kind { get; }
        public string PrimaryItemId { get; }
        public int Quantity { get; }
        public int GoldDelta { get; }

        public EconomyAutoSaveRequest(
            EconomyTransactionKind kind,
            string primaryItemId,
            int quantity,
            int goldDelta)
        {
            Kind = kind;
            PrimaryItemId = primaryItemId ?? string.Empty;
            Quantity = quantity;
            GoldDelta = goldDelta;
        }
    }
}
