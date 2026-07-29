using SubTerra.App.Core;

namespace SubTerra.App.Save
{
    public enum SaveMigrationStatus
    {
        Current = 0,
        Migrated = 1,
        InvalidOldVersion = 2,
        FutureVersion = 3,
        InvalidData = 4
    }

    public sealed class SaveMigrationService
    {
        public SaveMigrationStatus TryMigrate(GameSaveData data)
        {
            if (data == null || data.saveVersion < SaveVersions.First)
            {
                return SaveMigrationStatus.InvalidOldVersion;
            }

            if (data.saveVersion > SaveVersions.Current)
            {
                return SaveMigrationStatus.FutureVersion;
            }

            var migrated = false;
            while (data.saveVersion < SaveVersions.Current)
            {
                switch (data.saveVersion)
                {
                    case 1:
                        MigrateVersion1To2(data);
                        migrated = true;
                        break;
                    default:
                        return SaveMigrationStatus.InvalidOldVersion;
                }
            }

            SaveDataValidator.NormalizeMissingCollections(data);
            return SaveDataValidator.TryValidate(data, out _)
                ? (migrated ? SaveMigrationStatus.Migrated : SaveMigrationStatus.Current)
                : SaveMigrationStatus.InvalidData;
        }

        private static void MigrateVersion1To2(GameSaveData data)
        {
            SaveDataValidator.NormalizeMissingCollections(data);
            if (string.IsNullOrWhiteSpace(data.targetSceneName))
            {
                data.targetSceneName = SceneNames.Integration;
            }

            data.saveVersion = 2;
        }
    }
}
