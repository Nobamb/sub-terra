using SubTerra.App.Core;
using SubTerra.App.Tutorial;

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
                    case 2:
                        MigrateVersion2To3(data);
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

            // 구버전 월드 메타: Seed/생성기 버전 누락 시 안전한 기본값.
            if (data.world != null)
            {
                if (data.world.generatorVersion <= 0)
                {
                    data.world.generatorVersion = 1;
                }

                if (string.IsNullOrEmpty(data.world.version))
                {
                    data.world.version = "1.2";
                }
            }

            // 신규 Progress 필드 기본값(빈 목표 ID, 미완료 데모).
            if (data.progress != null && data.progress.currentObjectiveId == null)
            {
                data.progress.currentObjectiveId = string.Empty;
            }

            data.saveVersion = 2;
        }

        private static void MigrateVersion2To3(GameSaveData data)
        {
            SaveDataValidator.NormalizeMissingCollections(data);
            var progress = data.progress;
            if (progress == null)
            {
                data.saveVersion = 3;
                return;
            }

            if (progress.isDemoComplete)
            {
                progress.completedObjectives = DemoObjectiveIds.RequiredCount;
                progress.currentObjectiveId = DemoObjectiveIds.DemoEnd;
                data.saveVersion = 3;
                return;
            }

            var objectiveIndex = DemoObjectiveCatalog.IndexOf(progress.currentObjectiveId);
            var isAfterInsertedClinic = objectiveIndex
                > DemoObjectiveCatalog.IndexOf(DemoObjectiveIds.HealNearOutpost);
            var chargerWasAlreadyCompleted = progress.completedObjectives
                > DemoObjectiveCatalog.IndexOf(DemoObjectiveIds.ChargeNearOutpost);
            if (isAfterInsertedClinic || chargerWasAlreadyCompleted)
            {
                progress.completedObjectives = System.Math.Min(
                    DemoObjectiveIds.RequiredCount,
                    progress.completedObjectives + 1);
            }

            data.saveVersion = 3;
        }
    }
}
