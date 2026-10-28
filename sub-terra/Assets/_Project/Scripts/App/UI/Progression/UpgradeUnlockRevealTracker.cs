using System.Collections.Generic;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// B-169: 업그레이드 노드마다 풀린 모습으로 그렸는지와, 해금 연출을 기다리는 노드를 런타임 메모리로만 추적한다.
    /// 처음 그릴 때·다시 묶은 직후는 연출 없이 그대로 그리고, 그린 뒤 잠김 → 풀림이 된 노드만 대기로 둔다.
    /// 대기는 연출을 실제로 시작할 때(MarkStarted) 지운다. 저장하지 않는다(DeepZoneRevealTracker와 같은 방식).
    /// </summary>
    public sealed class UpgradeUnlockRevealTracker
    {
        private readonly Dictionary<string, bool> drawnUnlocked = new Dictionary<string, bool>();
        private readonly List<string> pending = new List<string>();

        public bool HasPending => pending.Count > 0;
        public int PendingCount => pending.Count;
        /// <summary>풀린 순서대로의 대기 노드.</summary>
        public IReadOnlyList<string> Pending => pending;

        public bool HasDrawn(string upgradeId) => !string.IsNullOrEmpty(upgradeId) && drawnUnlocked.ContainsKey(upgradeId);
        public bool IsPending(string upgradeId) => !string.IsNullOrEmpty(upgradeId) && pending.Contains(upgradeId);

        /// <summary>노드를 그릴 때마다 부른다. 해금 연출을 기다려 잠긴 모습으로 둬야 하면 true.</summary>
        public bool Observe(string upgradeId, bool unlocked)
        {
            if (string.IsNullOrEmpty(upgradeId))
            {
                return false;
            }

            if (!drawnUnlocked.TryGetValue(upgradeId, out var wasUnlocked))
            {
                drawnUnlocked[upgradeId] = unlocked;
                return false;
            }

            drawnUnlocked[upgradeId] = unlocked;
            if (!unlocked)
            {
                pending.Remove(upgradeId);
                return false;
            }

            if (!wasUnlocked && !pending.Contains(upgradeId))
            {
                pending.Add(upgradeId);
            }

            return pending.Contains(upgradeId);
        }

        /// <summary>연출을 시작한 노드를 대기에서 뺀다. 한 번 뺀 노드는 다시 잠겼다 풀리기 전까지 대기가 되지 않는다.</summary>
        public bool MarkStarted(string upgradeId)
        {
            return pending.Remove(upgradeId);
        }

        /// <summary>불러오기·다시 묶기 직후. 다음 그리기를 첫 그리기로 본다.</summary>
        public void Reset()
        {
            drawnUnlocked.Clear();
            pending.Clear();
        }
    }
}
