using System;
using System.Collections.Generic;

namespace SubTerra.App.Outpost
{
    /// <summary>충전소/보건소 인스턴스별 재사용 대기. 시설마다 별도 타이머를 가진다.</summary>
    [Serializable]
    public sealed class FacilityCooldownState
    {
        public string InstanceId { get; private set; }
        public double RemainingSeconds { get; private set; }

        public FacilityCooldownState(string instanceId, double remainingSeconds)
        {
            InstanceId = instanceId ?? string.Empty;
            RemainingSeconds = SanitizeRemaining(remainingSeconds);
        }

        internal void Reset(double remainingSeconds)
        {
            RemainingSeconds = SanitizeRemaining(remainingSeconds);
        }

        internal void AddElapsed(double deltaSeconds)
        {
            if (deltaSeconds <= 0d
                || double.IsNaN(deltaSeconds)
                || double.IsInfinity(deltaSeconds))
            {
                return;
            }

            RemainingSeconds -= deltaSeconds;
            if (RemainingSeconds < 0d)
            {
                RemainingSeconds = 0d;
            }
        }

        internal static double SanitizeRemaining(double remainingSeconds)
        {
            if (remainingSeconds <= 0d
                || double.IsNaN(remainingSeconds)
                || double.IsInfinity(remainingSeconds))
            {
                return 0d;
            }

            return remainingSeconds;
        }
    }

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
        private readonly List<FacilityCooldownState> facilityCooldowns =
            new List<FacilityCooldownState>();

        public string CheckpointId { get; private set; } = string.Empty;
        public int CheckpointX { get; private set; }
        public int CheckpointY { get; private set; }

        public IReadOnlyList<OutpostStorageEntryState> Storage => storage;
        public IReadOnlyList<string> InstalledOutpostIds => installedOutpostIds;
        public IReadOnlyList<FacilityCooldownState> FacilityCooldowns => facilityCooldowns;

        public int GetStorageQuantity(string mineralId)
        {
            var index = FindStorageIndex(mineralId);
            return index >= 0 ? storage[index].Quantity : 0;
        }

        /// <summary>세이브 복원용. 전체 입력을 먼저 검증하고 성공할 때만 기존 상태를 교체한다.</summary>
        public bool TryRestore(
            IReadOnlyList<OutpostStorageEntryState> restoredStorage,
            IReadOnlyList<string> restoredOutpostIds,
            string checkpointId,
            int checkpointX,
            int checkpointY,
            IReadOnlyList<FacilityCooldownState> restoredCooldowns = null)
        {
            if (restoredStorage == null || restoredOutpostIds == null)
            {
                return false;
            }

            var mineralIds = new HashSet<string>(StringComparer.Ordinal);
            var storageCopy = new List<OutpostStorageEntryState>(restoredStorage.Count);
            for (var i = 0; i < restoredStorage.Count; i++)
            {
                var entry = restoredStorage[i];
                if (entry == null
                    || string.IsNullOrEmpty(entry.MineralId)
                    || entry.Quantity <= 0
                    || !mineralIds.Add(entry.MineralId))
                {
                    return false;
                }

                storageCopy.Add(
                    new OutpostStorageEntryState(entry.MineralId, entry.Quantity));
            }

            var outpostIds = new HashSet<string>(StringComparer.Ordinal);
            var outpostCopy = new List<string>(restoredOutpostIds.Count);
            for (var i = 0; i < restoredOutpostIds.Count; i++)
            {
                var id = restoredOutpostIds[i];
                if (string.IsNullOrEmpty(id) || !outpostIds.Add(id))
                {
                    return false;
                }

                outpostCopy.Add(id);
            }

            if (!TryCopyCooldowns(restoredCooldowns, out var cooldownCopy))
            {
                return false;
            }

            storage.Clear();
            storage.AddRange(storageCopy);
            installedOutpostIds.Clear();
            installedOutpostIds.AddRange(outpostCopy);
            facilityCooldowns.Clear();
            facilityCooldowns.AddRange(cooldownCopy);
            CheckpointId = checkpointId ?? string.Empty;
            CheckpointX = checkpointX;
            CheckpointY = checkpointY;
            return true;
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

        /// <summary>해당 시설 인스턴스의 남은 재사용 대기. 만료됐으면 false.</summary>
        public bool TryGetFacilityCooldownRemaining(string instanceId, out double remainingSeconds)
        {
            remainingSeconds = 0d;
            var index = FindCooldownIndex(instanceId);
            if (index < 0)
            {
                return false;
            }

            remainingSeconds = facilityCooldowns[index].RemainingSeconds;
            return remainingSeconds > 0d;
        }

        /// <summary>성공한 충전/회복 사용을 해당 인스턴스에만 기록한다. 다른 시설 타이머는 건드리지 않는다.</summary>
        internal void RecordFacilityUse(string instanceId, double cooldownSeconds)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return;
            }

            var remaining = FacilityCooldownState.SanitizeRemaining(cooldownSeconds);
            if (remaining <= 0d)
            {
                var expired = FindCooldownIndex(instanceId);
                if (expired >= 0)
                {
                    facilityCooldowns.RemoveAt(expired);
                }

                return;
            }

            var index = FindCooldownIndex(instanceId);
            if (index >= 0)
            {
                facilityCooldowns[index].Reset(remaining);
                return;
            }

            facilityCooldowns.Add(new FacilityCooldownState(instanceId, remaining));
        }

        /// <summary>플레이 경과만큼 시설별 타이머를 줄인다. 만료 항목은 제거한다.</summary>
        internal void AddElapsed(double deltaSeconds)
        {
            if (deltaSeconds <= 0d
                || double.IsNaN(deltaSeconds)
                || double.IsInfinity(deltaSeconds))
            {
                return;
            }

            for (var i = facilityCooldowns.Count - 1; i >= 0; i--)
            {
                facilityCooldowns[i].AddElapsed(deltaSeconds);
                if (facilityCooldowns[i].RemainingSeconds <= 0d)
                {
                    facilityCooldowns.RemoveAt(i);
                }
            }
        }

        /// <summary>광산 초기화 시 인스턴스 ID가 다시 쓰이므로 대기 기록을 비운다.</summary>
        internal void ClearFacilityCooldowns()
        {
            facilityCooldowns.Clear();
        }

        private int FindCooldownIndex(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return -1;
            }

            for (var i = 0; i < facilityCooldowns.Count; i++)
            {
                if (facilityCooldowns[i].InstanceId == instanceId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool TryCopyCooldowns(
            IReadOnlyList<FacilityCooldownState> restoredCooldowns,
            out List<FacilityCooldownState> copy)
        {
            copy = new List<FacilityCooldownState>();
            if (restoredCooldowns == null)
            {
                return true;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < restoredCooldowns.Count; i++)
            {
                var entry = restoredCooldowns[i];
                if (entry == null
                    || string.IsNullOrEmpty(entry.InstanceId)
                    || entry.RemainingSeconds <= 0d
                    || !ids.Add(entry.InstanceId))
                {
                    copy = null;
                    return false;
                }

                copy.Add(new FacilityCooldownState(entry.InstanceId, entry.RemainingSeconds));
            }

            return true;
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
