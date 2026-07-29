using System;

namespace SubTerra.App.Save
{
    public enum SaveStatus
    {
        Success = 0,
        InvalidSlot = 1,
        CaptureFailed = 2,
        TemporaryWriteFailed = 3,
        TemporaryValidationFailed = 4,
        BackupFailed = 5,
        PromoteFailed = 6,
        Cancelled = 7
    }

    public enum LoadStatus
    {
        Success = 0,
        RecoveredFromBackup = 1,
        NotFound = 2,
        BothCopiesInvalid = 3,
        FutureVersion = 4,
        InvalidSlot = 5,
        IoFailure = 6
    }

    [Flags]
    public enum SaveRecoveryChoice
    {
        None = 0,
        Retry = 1,
        StartNewGame = 2
    }

    public sealed class SaveResult
    {
        public SaveStatus Status { get; }
        public int SlotId { get; }
        public bool IsSuccess => Status == SaveStatus.Success;

        public SaveResult(SaveStatus status, int slotId)
        {
            Status = status;
            SlotId = slotId;
        }
    }

    public sealed class LoadResult
    {
        public LoadStatus Status { get; }
        public int SlotId { get; }
        public RestoredSaveState State { get; }
        public SaveRecoveryChoice RecoveryChoices { get; }
        public bool IsSuccess =>
            Status == LoadStatus.Success || Status == LoadStatus.RecoveredFromBackup;
        public bool UsedBackup => Status == LoadStatus.RecoveredFromBackup;

        public LoadResult(
            LoadStatus status,
            int slotId,
            RestoredSaveState state = null,
            SaveRecoveryChoice recoveryChoices = SaveRecoveryChoice.None)
        {
            Status = status;
            SlotId = slotId;
            State = state;
            RecoveryChoices = recoveryChoices;
        }
    }

    public sealed class SaveSlotMetadata
    {
        public int SlotId { get; }
        public bool HasSave { get; }
        public bool IsRecoverableFromBackup { get; }
        public int SaveVersion { get; }
        public long SavedAtUtc { get; }
        public int Gold { get; }
        public int Depth { get; }
        /// <summary>Phase K Load 결과. 빈/정상/백업/손상 구분을 Main Menu가 그대로 쓴다.</summary>
        public LoadStatus LoadStatus { get; }

        /// <summary>정상 또는 백업 복구 가능일 때만 이어하기 활성.</summary>
        public bool CanContinue =>
            LoadStatus == LoadStatus.Success
            || LoadStatus == LoadStatus.RecoveredFromBackup;

        public SaveSlotMetadata(
            int slotId,
            bool hasSave,
            bool isRecoverableFromBackup,
            int saveVersion,
            long savedAtUtc,
            int gold,
            int depth,
            LoadStatus loadStatus = LoadStatus.NotFound)
        {
            SlotId = slotId;
            HasSave = hasSave;
            IsRecoverableFromBackup = isRecoverableFromBackup;
            SaveVersion = saveVersion;
            SavedAtUtc = savedAtUtc;
            Gold = gold;
            Depth = depth;
            LoadStatus = loadStatus;
        }
    }
}

