using System;
using System.IO;

namespace SubTerra.App.Save
{
    public sealed class SaveSlotPaths
    {
        public string Normal { get; }
        public string Backup { get; }
        public string Temporary { get; }

        public SaveSlotPaths(string normal, string backup, string temporary)
        {
            Normal = normal;
            Backup = backup;
            Temporary = temporary;
        }
    }

    public sealed class SavePathPolicy
    {
        public const int MinimumSlot = 1;
        public const int MaximumSlot = 3;

        private readonly string rootDirectory;

        public SavePathPolicy(string saveRootDirectory)
        {
            if (string.IsNullOrWhiteSpace(saveRootDirectory))
            {
                throw new ArgumentException("Save root is required.", nameof(saveRootDirectory));
            }

            rootDirectory = Path.GetFullPath(saveRootDirectory);
        }

        public bool IsValidSlot(int slotId)
        {
            return slotId >= MinimumSlot && slotId <= MaximumSlot;
        }

        public bool TryGetPaths(int slotId, out SaveSlotPaths paths)
        {
            paths = null;
            if (!IsValidSlot(slotId))
            {
                return false;
            }

            var fileName = "save_slot_" + slotId;
            paths = new SaveSlotPaths(
                Path.Combine(rootDirectory, fileName + ".json"),
                Path.Combine(rootDirectory, fileName + ".backup.json"),
                Path.Combine(rootDirectory, fileName + ".tmp"));
            return true;
        }

        public string RootDirectory => rootDirectory;
    }
}
