using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.Progression
{
    /// <summary>세이브 가능한 업그레이드 ID/현재 레벨 한 항목.</summary>
    [Serializable]
    public sealed class UpgradeLevelState
    {
        [SerializeField] private string upgradeId;
        [SerializeField] private int level;

        public string UpgradeId => upgradeId;
        public int Level => level;

        public UpgradeLevelState(string upgradeId, int level)
        {
            this.upgradeId = upgradeId ?? string.Empty;
            this.level = level < 0 ? 0 : level;
        }

        internal void SetLevel(int value)
        {
            level = value < 0 ? 0 : value;
        }
    }

    /// <summary>
    /// 업그레이드 런타임/영구 상태. ScriptableObject 정의와 분리하며
    /// Unity JSON이 저장할 수 있도록 Dictionary 대신 직렬화 목록을 사용한다.
    /// </summary>
    [Serializable]
    public sealed class UpgradeState
    {
        [SerializeField] private List<UpgradeLevelState> levels = new List<UpgradeLevelState>();
        [SerializeField] private List<string> unlockedZoneIds = new List<string>();

        public IReadOnlyList<UpgradeLevelState> Levels => levels;
        public IReadOnlyList<string> UnlockedZoneIds => unlockedZoneIds;

        public int GetLevel(string upgradeId)
        {
            if (string.IsNullOrEmpty(upgradeId))
            {
                return 0;
            }

            for (var i = 0; i < levels.Count; i++)
            {
                var entry = levels[i];
                if (entry != null && entry.UpgradeId == upgradeId)
                {
                    return entry.Level;
                }
            }

            return 0;
        }

        /// <summary>
        /// 세이브 복원용. 빈 ID·음수·중복 ID를 거부하고 실패 시 기존 상태를 유지한다.
        /// 카탈로그 최대 레벨 검증은 ProgressionService가 담당한다.
        /// </summary>
        public bool TryRestore(IReadOnlyList<UpgradeLevelState> restored)
        {
            if (restored == null)
            {
                return false;
            }

            var ids = new HashSet<string>();
            var copy = new List<UpgradeLevelState>(restored.Count);
            for (var i = 0; i < restored.Count; i++)
            {
                var entry = restored[i];
                if (entry == null
                    || string.IsNullOrEmpty(entry.UpgradeId)
                    || entry.Level < 0
                    || !ids.Add(entry.UpgradeId))
                {
                    return false;
                }

                copy.Add(new UpgradeLevelState(entry.UpgradeId, entry.Level));
            }

            levels = copy;
            return true;
        }

        public bool IsZoneUnlocked(string zoneId)
        {
            return !string.IsNullOrEmpty(zoneId) && unlockedZoneIds.Contains(zoneId);
        }

        /// <summary>세이브 복원 시 잠금 해제 ID를 별도로 복구한다.</summary>
        public bool TryRestoreUnlockedZones(IReadOnlyList<string> restoredZoneIds)
        {
            if (restoredZoneIds == null)
            {
                return false;
            }

            var ids = new HashSet<string>();
            var copy = new List<string>(restoredZoneIds.Count);
            for (var i = 0; i < restoredZoneIds.Count; i++)
            {
                var id = restoredZoneIds[i];
                if (string.IsNullOrEmpty(id) || !ids.Add(id))
                {
                    return false;
                }

                copy.Add(id);
            }

            unlockedZoneIds = copy;
            return true;
        }

        internal void ApplyPurchasedLevel(string upgradeId, int level)
        {
            for (var i = 0; i < levels.Count; i++)
            {
                var entry = levels[i];
                if (entry != null && entry.UpgradeId == upgradeId)
                {
                    entry.SetLevel(level);
                    return;
                }
            }

            levels.Add(new UpgradeLevelState(upgradeId, level));
        }

        internal bool ApplyZoneUnlock(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || unlockedZoneIds.Contains(zoneId))
            {
                return false;
            }

            unlockedZoneIds.Add(zoneId);
            return true;
        }
    }
}
