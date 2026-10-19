using System;
using UnityEngine;

namespace SubTerra.App.Save
{
    public sealed class LoadService
    {
        private readonly ISaveFileSystem fileSystem;
        private readonly SavePathPolicy paths;
        private readonly SaveDataMapper mapper;
        private readonly SaveJsonCodec json;

        public LoadService(
            ISaveFileSystem saveFileSystem,
            SavePathPolicy pathPolicy,
            SaveDataMapper dataMapper,
            SaveJsonCodec jsonCodec)
        {
            fileSystem = saveFileSystem ?? throw new ArgumentNullException(nameof(saveFileSystem));
            paths = pathPolicy ?? throw new ArgumentNullException(nameof(pathPolicy));
            mapper = dataMapper ?? throw new ArgumentNullException(nameof(dataMapper));
            json = jsonCodec ?? throw new ArgumentNullException(nameof(jsonCodec));
        }

        public static LoadService CreateDefault()
        {
            var migrations = new SaveMigrationService();
            return new LoadService(
                new PhysicalSaveFileSystem(),
                new SavePathPolicy(Application.persistentDataPath),
                new SaveDataMapper(new SystemSaveClock()),
                new SaveJsonCodec(migrations));
        }

        public LoadResult Load(int slotId)
        {
            if (!paths.TryGetPaths(slotId, out var slotPaths))
            {
                return new LoadResult(LoadStatus.InvalidSlot, slotId);
            }

            var normal = TryLoadFile(slotPaths.Normal, out var normalState);
            if (normal == FileLoadStatus.Success)
            {
                return new LoadResult(LoadStatus.Success, slotId, normalState);
            }

            if (normal == FileLoadStatus.FutureVersion)
            {
                return new LoadResult(
                    LoadStatus.FutureVersion,
                    slotId,
                    recoveryChoices: SaveRecoveryChoice.Retry);
            }

            var backup = TryLoadFile(slotPaths.Backup, out var backupState);
            if (backup == FileLoadStatus.Success)
            {
                return new LoadResult(
                    LoadStatus.RecoveredFromBackup,
                    slotId,
                    backupState);
            }

            if (backup == FileLoadStatus.FutureVersion)
            {
                return new LoadResult(
                    LoadStatus.FutureVersion,
                    slotId,
                    recoveryChoices: SaveRecoveryChoice.Retry);
            }

            if (normal == FileLoadStatus.Missing && backup == FileLoadStatus.Missing)
            {
                return new LoadResult(
                    LoadStatus.NotFound,
                    slotId,
                    recoveryChoices: SaveRecoveryChoice.StartNewGame);
            }

            var ioFailure =
                normal == FileLoadStatus.IoFailure || backup == FileLoadStatus.IoFailure;
            return new LoadResult(
                ioFailure ? LoadStatus.IoFailure : LoadStatus.BothCopiesInvalid,
                slotId,
                recoveryChoices: SaveRecoveryChoice.Retry | SaveRecoveryChoice.StartNewGame);
        }

        public SaveSlotMetadata GetSlotMetadata(int slotId)
        {
            if (!paths.TryGetPaths(slotId, out var slotPaths))
            {
                return new SaveSlotMetadata(
                    slotId,
                    false,
                    false,
                    0,
                    0,
                    0,
                    0,
                    LoadStatus.InvalidSlot);
            }

            var result = Load(slotId);
            if (!result.IsSuccess || result.State == null)
            {
                // 빈 슬롯·손상·미래 버전을 LoadStatus로 보존해 Main Menu 이어하기 표시에 쓴다.
                return new SaveSlotMetadata(
                    slotId,
                    false,
                    false,
                    0,
                    0,
                    0,
                    0,
                    result.Status);
            }

            var source = result.UsedBackup ? slotPaths.Backup : slotPaths.Normal;
            try
            {
                var raw = fileSystem.ReadAllText(source);
                var status = json.TryDeserialize(raw, out var data);
                if (status == SaveMigrationStatus.FutureVersion
                    || status == SaveMigrationStatus.InvalidData
                    || data == null)
                {
                    return new SaveSlotMetadata(
                        slotId,
                        false,
                        false,
                        0,
                        0,
                        0,
                        0,
                        result.Status);
                }

                return new SaveSlotMetadata(
                    slotId,
                    true,
                    result.UsedBackup,
                    data.saveVersion,
                    data.savedAtUtc,
                    data.player.gold,
                    data.run.depth,
                    result.Status,
                    HasJsonKey(raw, "maximumDepth") ? data.run.maximumDepth : -1,
                    HasJsonKey(raw, "completedObjectives") ? data.progress.completedObjectives : -1);
            }
            catch
            {
                return new SaveSlotMetadata(
                    slotId,
                    false,
                    false,
                    0,
                    0,
                    0,
                    0,
                    LoadStatus.IoFailure);
            }
        }

        // B-150: 파일에 실제로 기록된 값만 메타에 싣는다. 키가 없는 옛 세이브는 -1("-")로 둔다.
        private static bool HasJsonKey(string raw, string key)
        {
            return !string.IsNullOrEmpty(raw)
                && raw.IndexOf("\"" + key + "\"", StringComparison.Ordinal) >= 0;
        }

        private FileLoadStatus TryLoadFile(string path, out RestoredSaveState state)
        {
            state = null;
            if (!fileSystem.FileExists(path))
            {
                return FileLoadStatus.Missing;
            }

            try
            {
                var migration = json.TryDeserialize(fileSystem.ReadAllText(path), out var data);
                if (migration == SaveMigrationStatus.FutureVersion)
                {
                    return FileLoadStatus.FutureVersion;
                }

                if (migration == SaveMigrationStatus.InvalidData
                    || migration == SaveMigrationStatus.InvalidOldVersion
                    || !mapper.TryRestore(data, out state))
                {
                    return FileLoadStatus.Invalid;
                }

                return FileLoadStatus.Success;
            }
            catch
            {
                return FileLoadStatus.IoFailure;
            }
        }

        private enum FileLoadStatus
        {
            Success,
            Missing,
            Invalid,
            FutureVersion,
            IoFailure
        }
    }
}
