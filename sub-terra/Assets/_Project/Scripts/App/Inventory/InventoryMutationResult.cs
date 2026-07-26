namespace SubTerra.App.Inventory
{
    /// <summary>Add/Reduce 결과 분류. Shared void API 뒤에서 B 내부 진단용.</summary>
    public enum InventoryMutationStatus
    {
        /// <summary>요청 수량을 전부 수락했다.</summary>
        Success = 0,

        /// <summary>적재 한도로 일부만 수락했다. Accepted/Rejected 확인.</summary>
        PartialAccept = 1,

        /// <summary>카탈로그에 없는 영구 ID.</summary>
        InvalidId = 2,

        /// <summary>0 또는 음수 수량.</summary>
        InvalidQuantity = 3,

        /// <summary>정수 합산 오버플로 위험이 있는 수량.</summary>
        OverflowRisk = 4,

        /// <summary>감소 시 보유 수량 부족.</summary>
        Insufficient = 5,

        /// <summary>카탈로그 포트 자체가 없다.</summary>
        CatalogMissing = 6,

        /// <summary>잔여 적재량이 단위 중량 미만이라 한 단위도 못 넣음.</summary>
        CapacityFull = 7
    }

    /// <summary>
    /// 인벤토리 변이 결과. Shared AddMineral은 void이므로 상세는 이 타입과 LastResult로 노출한다.
    /// </summary>
    public readonly struct InventoryMutationResult
    {
        public InventoryMutationStatus Status { get; }
        public string MineralId { get; }
        public int RequestedQuantity { get; }
        public int AcceptedQuantity { get; }
        public int RejectedQuantity { get; }
        public string Diagnostic { get; }

        /// <summary>스택·합산이 실제로 바뀌었으면 true. 이벤트 발행 기준.</summary>
        public bool DidChange => AcceptedQuantity > 0
            && (Status == InventoryMutationStatus.Success
                || Status == InventoryMutationStatus.PartialAccept);

        public InventoryMutationResult(
            InventoryMutationStatus status,
            string mineralId,
            int requestedQuantity,
            int acceptedQuantity,
            int rejectedQuantity,
            string diagnostic)
        {
            Status = status;
            MineralId = mineralId ?? string.Empty;
            RequestedQuantity = requestedQuantity;
            AcceptedQuantity = acceptedQuantity;
            RejectedQuantity = rejectedQuantity;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public static InventoryMutationResult Invalid(
            InventoryMutationStatus status,
            string mineralId,
            int requested,
            string diagnostic)
        {
            return new InventoryMutationResult(status, mineralId, requested, 0, 0, diagnostic);
        }

        public static InventoryMutationResult Accepted(
            InventoryMutationStatus status,
            string mineralId,
            int requested,
            int accepted,
            string diagnostic = null)
        {
            var rejected = requested > accepted ? requested - accepted : 0;
            return new InventoryMutationResult(status, mineralId, requested, accepted, rejected, diagnostic);
        }
    }
}
