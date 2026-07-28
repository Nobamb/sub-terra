using System;
using System.Collections.Generic;

namespace SubTerra.App.Outpost
{
    /// <summary>전진기지 보관함의 광물별 영구 수량.</summary>
    [Serializable]
    public sealed class OutpostStorageEntryState
    {
        public string MineralId { get; private set; }
        public int Quantity { get; private set; }

        public OutpostStorageEntryState(string mineralId, int quantity)
        {
            MineralId = mineralId ?? string.Empty;
            Quantity = quantity < 0 ? 0 : quantity;
        }

        internal void SetQuantity(int quantity)
        {
            Quantity = quantity < 0 ? 0 : quantity;
        }
    }

    /// <summary>
    /// 저장 가능한 전진기지 상태. Runtime 연결 판정은 A가 다시 계산하므로 보관하지 않는다.
    /// </summary>
    [Serializable]
    public sealed class OutpostState
    {
        private readonly List<OutpostStorageEntryState> storage =
            new List<OutpostStorageEntryState>();
        private readonly List<string> installedOutpostIds = new List<string>();

        public string CheckpointId { get; private set; } = string.Empty;
        public int CheckpointX { get; private set; }
        public int CheckpointY { get; private set; }

        public IReadOnlyList<OutpostStorageEntryState> Storage => storage;
        public IReadOnlyList<string> InstalledOutpostIds => installedOutpostIds;

        public int GetStorageQuantity(string mineralId)
        {
            var index = FindStorageIndex(mineralId);
            return index >= 0 ? storage[index].Quantity : 0;
        }

        internal void SetStorageQuantity(string mineralId, int quantity)
        {
            if (string.IsNullOrEmpty(mineralId))
            {
                return;
            }

            var index = FindStorageIndex(mineralId);
            if (quantity <= 0)
            {
                if (index >= 0)
                {
                    storage.RemoveAt(index);
                }

                return;
            }

            if (index >= 0)
            {
                storage[index].SetQuantity(quantity);
            }
            else
            {
                storage.Add(new OutpostStorageEntryState(mineralId, quantity));
            }
        }

        internal bool HasInstalledOutpost(string instanceId)
        {
            return !string.IsNullOrEmpty(instanceId) && installedOutpostIds.Contains(instanceId);
        }

        internal void RecordInstallation(
            string instanceId,
            string checkpointId,
            int checkpointX,
            int checkpointY)
        {
            installedOutpostIds.Add(instanceId);
            CheckpointId = checkpointId ?? string.Empty;
            CheckpointX = checkpointX;
            CheckpointY = checkpointY;
        }

        private int FindStorageIndex(string mineralId)
        {
            if (string.IsNullOrEmpty(mineralId))
            {
                return -1;
            }

            for (var i = 0; i < storage.Count; i++)
            {
                if (storage[i].MineralId == mineralId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
