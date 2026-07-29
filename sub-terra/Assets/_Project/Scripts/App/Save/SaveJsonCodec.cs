using System;
using UnityEngine;

namespace SubTerra.App.Save
{
    public sealed class SaveJsonCodec
    {
        private readonly SaveMigrationService migrations;

        public SaveJsonCodec(SaveMigrationService migrationService)
        {
            migrations = migrationService
                ?? throw new ArgumentNullException(nameof(migrationService));
        }

        public string Serialize(GameSaveData data)
        {
            return data == null ? string.Empty : JsonUtility.ToJson(data, true);
        }

        public SaveMigrationStatus TryDeserialize(
            string json,
            out GameSaveData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                return SaveMigrationStatus.InvalidData;
            }

            try
            {
                data = JsonUtility.FromJson<GameSaveData>(json);
            }
            catch
            {
                return SaveMigrationStatus.InvalidData;
            }

            return migrations.TryMigrate(data);
        }
    }
}
