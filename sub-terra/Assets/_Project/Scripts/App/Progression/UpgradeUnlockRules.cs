using System.Collections.Generic;
using SubTerra.App.Core.Data;

namespace SubTerra.App.Progression
{
    /// <summary>
    /// 업그레이드 트리 해금 판정. 구매 처리와 UI 표시가 같은 규칙을 쓰도록 한곳에 둔다.
    /// 해금 여부는 현재 레벨에서 매번 계산하므로 별도 저장 필드가 없고,
    /// 이미 구매한(레벨 1 이상) 항목은 새 조건 때문에 다시 잠기지 않는다.
    /// </summary>
    public static class UpgradeUnlockRules
    {
        public static bool HasRequirement(UpgradeData data)
        {
            return data != null
                && !string.IsNullOrEmpty(data.UnlockRequirementUpgradeId)
                && data.UnlockRequiredLevel > 0;
        }

        public static bool IsUnlocked(UpgradeData data, UpgradeState state)
        {
            if (data == null)
            {
                return false;
            }

            if (!HasRequirement(data))
            {
                return true;
            }

            if (state == null)
            {
                return false;
            }

            // 구매 이력이 있으면 기존 세이브라도 잠그지 않는다.
            if (state.GetLevel(data.Id) > 0)
            {
                return true;
            }

            return state.GetLevel(data.UnlockRequirementUpgradeId) >= data.UnlockRequiredLevel;
        }

        /// <summary>
        /// 미해금 노드에 보여 줄 조건 문구. 숨겨진 항목의 이름·효과는 담지 않는다.
        /// 조건이 되는 업그레이드 자체가 잠겨 있으면 그 이름도 숨긴다.
        /// </summary>
        public static string BuildLockedReason(
            UpgradeData data,
            IUpgradeCatalog catalog,
            UpgradeState state)
        {
            if (!HasRequirement(data) || IsUnlocked(data, state))
            {
                return string.Empty;
            }

            var requirementId = data.UnlockRequirementUpgradeId;
            if (catalog == null || !catalog.TryGetUpgrade(requirementId, out var requirement)
                || requirement == null)
            {
                return "해금 조건 확인 불가";
            }

            if (!IsUnlocked(requirement, state))
            {
                return "상위 업그레이드 해금 필요";
            }

            var name = ItemDisplayNames.PreferDisplay(requirement.Id, requirement.DisplayName);
            return name + " Lv." + data.UnlockRequiredLevel + " 필요";
        }

        /// <summary>현재 해금 상태인 업그레이드 ID를 채운다. 구매 전후 비교로 신규 해금을 찾는 데 쓴다.</summary>
        public static void CollectUnlocked(
            IUpgradeCatalog catalog,
            UpgradeState state,
            HashSet<string> output)
        {
            if (catalog == null || catalog.Upgrades == null || output == null)
            {
                return;
            }

            for (var i = 0; i < catalog.Upgrades.Count; i++)
            {
                var data = catalog.Upgrades[i];
                if (data != null && !string.IsNullOrEmpty(data.Id) && IsUnlocked(data, state))
                {
                    output.Add(data.Id);
                }
            }
        }

        /// <summary>
        /// 카탈로그의 해금 규칙 검증. 알 수 없는 조건 ID, 최대 레벨을 넘는 요구 레벨,
        /// 순환 조건이 있으면 진단 문구를 반환하고, 문제없으면 빈 문자열이다.
        /// </summary>
        public static string Validate(IReadOnlyList<UpgradeData> upgrades)
        {
            if (upgrades == null)
            {
                return string.Empty;
            }

            var byId = new Dictionary<string, UpgradeData>();
            for (var i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i] != null && !string.IsNullOrEmpty(upgrades[i].Id))
                {
                    byId[upgrades[i].Id] = upgrades[i];
                }
            }

            for (var i = 0; i < upgrades.Count; i++)
            {
                var data = upgrades[i];
                if (data == null || !HasRequirement(data))
                {
                    continue;
                }

                if (data.UnlockRequirementUpgradeId == data.Id)
                {
                    return data.Id + ": 자기 자신을 해금 조건으로 사용할 수 없습니다.";
                }

                if (!byId.TryGetValue(data.UnlockRequirementUpgradeId, out var requirement))
                {
                    return data.Id + ": 알 수 없는 해금 조건 " + data.UnlockRequirementUpgradeId;
                }

                if (data.UnlockRequiredLevel > requirement.MaxLevel)
                {
                    return data.Id + ": 요구 레벨이 " + requirement.Id + " 최대 레벨을 넘습니다.";
                }

                // 조건 체인을 따라가며 순환을 찾는다.
                var visited = new HashSet<string> { data.Id };
                var cursor = requirement;
                while (cursor != null && HasRequirement(cursor))
                {
                    if (!visited.Add(cursor.Id))
                    {
                        return data.Id + ": 해금 조건이 순환합니다.";
                    }

                    byId.TryGetValue(cursor.UnlockRequirementUpgradeId, out cursor);
                }
            }

            for (var i = 0; i < upgrades.Count; i++)
            {
                var data = upgrades[i];
                if (data == null || string.IsNullOrEmpty(data.TreeParentId))
                {
                    continue;
                }

                if (!byId.ContainsKey(data.TreeParentId))
                {
                    return data.Id + ": 알 수 없는 트리 연결 대상 " + data.TreeParentId;
                }
            }

            return string.Empty;
        }
    }
}
