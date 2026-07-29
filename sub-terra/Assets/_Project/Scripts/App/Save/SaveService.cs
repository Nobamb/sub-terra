using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SubTerra.App.Save
{
    public interface ISaveWriter
    {
        Task<SaveResult> SaveAsync(
            int slotId,
            SaveCaptureContext context,
            CancellationToken cancellationToken);
    }

    public sealed class SaveService : ISaveWriter
    {
        private readonly ISaveFileSystem fileSystem;
        private readonly SavePathPolicy paths;
        private readonly SaveDataMapper mapper;
        private readonly SaveJsonCodec json;

        public SaveService(
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

        public static SaveService CreateDefault()
        {
            var migrations = new SaveMigrationService();
            return new SaveService(
                new PhysicalSaveFileSystem(),
                new SavePathPolicy(Application.persistentDataPath),
                new SaveDataMapper(new SystemSaveClock()),
                new SaveJsonCodec(migrations));
        }

        public SaveResult Save(int slotId, SaveCaptureContext context)
        {
            if (!paths.TryGetPaths(slotId, out var slotPaths))
            {
                return new SaveResult(SaveStatus.InvalidSlot, slotId);
            }

            GameSaveData data;
            try
            {
                data = mapper.Capture(context);
            }
            catch
            {
                return new SaveResult(SaveStatus.CaptureFailed, slotId);
            }

            if (data == null || !SaveDataValidator.TryValidate(data, out _))
            {
                return new SaveResult(SaveStatus.CaptureFailed, slotId);
            }

            try
            {
                fileSystem.CreateDirectory(paths.RootDirectory);
                fileSystem.WriteAllText(slotPaths.Temporary, json.Serialize(data));
            }
            catch
            {
                TryDelete(slotPaths.Temporary);
                return new SaveResult(SaveStatus.TemporaryWriteFailed, slotId);
            }

            try
            {
                var readBack = fileSystem.ReadAllText(slotPaths.Temporary);
                var migration = json.TryDeserialize(readBack, out var validated);
                if (migration == SaveMigrationStatus.FutureVersion
                    || migration == SaveMigrationStatus.InvalidData
                    || migration == SaveMigrationStatus.InvalidOldVersion
                    || validated.saveVersion != SaveVersions.Current)
                {
                    TryDelete(slotPaths.Temporary);
                    return new SaveResult(SaveStatus.TemporaryValidationFailed, slotId);
                }
            }
            catch
            {
                TryDelete(slotPaths.Temporary);
                return new SaveResult(SaveStatus.TemporaryValidationFailed, slotId);
            }

            if (fileSystem.FileExists(slotPaths.Normal))
            {
                try
                {
                    if (fileSystem.FileExists(slotPaths.Backup))
                    {
                        fileSystem.DeleteFile(slotPaths.Backup);
                    }

                    fileSystem.MoveFile(slotPaths.Normal, slotPaths.Backup);
                }
                catch
                {
                    TryDelete(slotPaths.Temporary);
                    return new SaveResult(SaveStatus.BackupFailed, slotId);
                }
            }

            try
            {
                fileSystem.MoveFile(slotPaths.Temporary, slotPaths.Normal);
                return new SaveResult(SaveStatus.Success, slotId);
            }
            catch
            {
                TryDelete(slotPaths.Temporary);
                return new SaveResult(SaveStatus.PromoteFailed, slotId);
            }
        }

        public Task<SaveResult> SaveAsync(
            int slotId,
            SaveCaptureContext context,
            CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested
                ? Task.FromResult(new SaveResult(SaveStatus.Cancelled, slotId))
                : Task.FromResult(Save(slotId, context));
        }

        public bool DeleteSlot(int slotId)
        {
            if (!paths.TryGetPaths(slotId, out var slotPaths))
            {
                return false;
            }

            var temporaryDeleted = TryDelete(slotPaths.Temporary);
            var normalDeleted = TryDelete(slotPaths.Normal);
            var backupDeleted = TryDelete(slotPaths.Backup);
            return temporaryDeleted && normalDeleted && backupDeleted;
        }

        private bool TryDelete(string path)
        {
            try
            {
                if (fileSystem.FileExists(path))
                {
                    fileSystem.DeleteFile(path);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
