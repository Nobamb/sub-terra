using System.Collections.Generic;

namespace SubTerra.App.Tutorial
{
    /// <summary>
    /// 필수 데모 13단계 목표 표.
    /// 시작/완료 신호·문구·다음 목표를 코드 표로 고정해 Edit Mode에서 검증한다.
    /// </summary>
    public static class DemoObjectiveCatalog
    {
        private static readonly Dictionary<string, DemoObjectiveDefinition> ById;
        private static readonly DemoObjectiveDefinition[] OrderedDefinitions;

        static DemoObjectiveCatalog()
        {
            OrderedDefinitions = new[]
            {
                new DemoObjectiveDefinition(
                    DemoObjectiveIds.ExploreStart,
                    "탐사 시작",
                    "지하 탐사를 시작합니다. 드릴로 앞길을 여세요.",
                    "이동·채굴로 탐사를 시작하세요",
                    DemoProgressSignal.ExplorationStarted,
                    DemoObjectiveIds.MineCopperIron,
                    showsDismissibleGuidance: true),
                new DemoObjectiveDefinition(
                    DemoObjectiveIds.MineCopperIron,
                    "구리·철 확보",
                    "구리와 철을 각각 1개 이상 채굴해 인벤토리에 담으세요.",
                    "구리 타일과 철 타일을 채굴하세요",
                    DemoProgressSignal.CopperAndIronCollected,
                    DemoObjectiveIds.PathGuide),
                new DemoObjectiveDefinition(
                    DemoObjectiveIds.PathGuide,
                    "경로 안내",
                    "안전 경로와 구조 위험 구간 안내를 확인하세요.",
                    "안내를 닫고 채굴 중 나타나는 균열 경고를 확인하세요",
                    DemoProgressSignal.PathGuidanceAcknowledged,
                    DemoObjectiveIds.StructuralCrack,
                    showsDismissibleGuidance: true),
                new DemoObjectiveDefinition(
                    DemoObjectiveIds.StructuralCrack,
                    "구조 균열 인지",
                    "구조 안정도가 위험 수준으로 바뀌면 경고를 확인하세요.",
                    "균열 경고가 보이면 버팀목을 준비하세요",
                    DemoProgressSignal.StructuralHazardObserved,
                    DemoObjectiveIds.PlaceSupport),
                new DemoObjectiveDefinition(
                    DemoObjectiveIds.PlaceSupport,
                    "버팀목 설치",
                    "버팀목을 설치해 구조 위험을 완화하세요.",
                    "건설 메뉴에서 버팀목을 배치하세요",
                    DemoProgressSignal.SupportPlaced,
                    DemoObjectiveIds.GasEncounter),
                new DemoObjectiveDefinition(
                    DemoObjectiveIds.GasEncounter,
                    "가스 구간 대응",
                    "가스 위험 구간에 진입한 뒤 안전 구역으로 이탈하세요.",
                    "가스 경고가 사라질 때까지 위험 범위에서 벗어나세요",
                    DemoProgressSignal.GasHazardResolved,
                    DemoObjectiveIds.OutpostInstall),
                new DemoObjectiveDefinition(
                    DemoObjectiveIds.OutpostInstall,
                    "전진기지 설치",
                    "전진기지 코어를 설치해 체크포인트를 만드세요.",
                    "전진기지 코어를 배치·활성화하세요",
                    DemoProgressSignal.OutpostInstalled,
                    DemoObjectiveIds.ReturnRecommend),
                new DemoObjectiveDefinition(
                    DemoObjectiveIds.ReturnRecommend,
                    "귀환 추천",
                    "드론이 귀환을 권할 때 근거를 확인하세요.",
                    "귀환 안내를 확인하거나 드론 추천을 보세요",
                    DemoProgressSignal.ReturnRecommendationPresented,
                    DemoObjectiveIds.Settlement,
                    showsDismissibleGuidance: true),
                new DemoObjectiveDefinition(
                    DemoObjectiveIds.Settlement,
                    "화물 정산",
                    "정산 콘솔에서 화물을 정산해 골드를 받으세요.",
                    "전진기지에서 정산을 완료하세요",
                    DemoProgressSignal.SettlementSucceeded,
                    DemoObjectiveIds.BatteryUpgrade),
                // 심층 잠금(DeepZoneUnlockRule.Mvp)과 동일한 조건이다.
                new DemoObjectiveDefinition(
                    DemoObjectiveIds.BatteryUpgrade,
                    "심층 대비 업그레이드",
                    "리튬 채굴과 심층 진입에 필요한 드릴 속도 2레벨, 드론 스캔 2레벨, 가스 저항 1레벨을 구매하세요.",
                    "업그레이드 패널에서 드릴·드론 스캔·가스 저항을 맞추세요",
                    DemoProgressSignal.BatteryUpgradeSucceeded,
                    DemoObjectiveIds.MineLithium),
                new DemoObjectiveDefinition(
                    DemoObjectiveIds.MineLithium,
                    "리튬 확보",
                    "구조·가스 대응과 장비 준비를 마친 뒤 리튬을 1개 이상 확보하세요.",
                    "심층 리튬 광맥을 찾아 채굴하세요",
                    DemoProgressSignal.LithiumCollected,
                    DemoObjectiveIds.DeepSignal),
                new DemoObjectiveDefinition(
                    DemoObjectiveIds.DeepSignal,
                    "심층 신호",
                    "심층 구역 잠금이 실제로 해제되면 신호를 확인하세요.",
                    "조건 충족 후 심층 잠금 해제를 확인하세요",
                    DemoProgressSignal.DeepZoneUnlocked,
                    DemoObjectiveIds.DemoEnd),
                new DemoObjectiveDefinition(
                    DemoObjectiveIds.DemoEnd,
                    "데모 종료",
                    "핵심 루프를 완주했습니다. 다음 콘텐츠를 기대해 주세요.",
                    "종료 화면을 닫아 데모를 완료하세요",
                    DemoProgressSignal.DemoCompleted,
                    string.Empty,
                    isTerminal: true,
                    showsDismissibleGuidance: true)
            };

            ById = new Dictionary<string, DemoObjectiveDefinition>(OrderedDefinitions.Length);
            for (var i = 0; i < OrderedDefinitions.Length; i++)
            {
                ById[OrderedDefinitions[i].Id] = OrderedDefinitions[i];
            }
        }

        public static IReadOnlyList<DemoObjectiveDefinition> All => OrderedDefinitions;

        public static bool TryGet(string objectiveId, out DemoObjectiveDefinition definition)
        {
            if (string.IsNullOrEmpty(objectiveId))
            {
                definition = null;
                return false;
            }

            return ById.TryGetValue(objectiveId, out definition);
        }

        public static DemoObjectiveDefinition GetRequired(string objectiveId)
        {
            return TryGet(objectiveId, out var definition) ? definition : null;
        }

        public static int IndexOf(string objectiveId)
        {
            for (var i = 0; i < OrderedDefinitions.Length; i++)
            {
                if (OrderedDefinitions[i].Id == objectiveId)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 저장 ID가 없거나 알 수 없으면 완료 개수로 가장 가까운 안전 목표를 고른다.
        /// 완료 개수가 범위 밖이면 첫 목표 또는 종료 목표로 폴백한다.
        /// </summary>
        public static string ResolveObjectiveId(string savedObjectiveId, int completedCount)
        {
            if (completedCount < 0)
            {
                completedCount = 0;
            }

            if (completedCount >= OrderedDefinitions.Length)
            {
                return DemoObjectiveIds.DemoEnd;
            }

            if (TryGet(savedObjectiveId, out _))
            {
                if (IndexOf(savedObjectiveId) == completedCount)
                {
                    return savedObjectiveId;
                }

                // prompt-B 53 이전 순서의 known ID/count 조합은 같은 의미의 새 단계로 이동한다.
                if (LegacyIndexOf(savedObjectiveId) == completedCount)
                {
                    return savedObjectiveId == DemoObjectiveIds.MineLithium
                        ? DemoObjectiveIds.StructuralCrack
                        : savedObjectiveId;
                }
            }

            return OrderedDefinitions[completedCount].Id;
        }

        private static int LegacyIndexOf(string objectiveId)
        {
            if (objectiveId == DemoObjectiveIds.ExploreStart) return 0;
            if (objectiveId == DemoObjectiveIds.MineCopperIron) return 1;
            if (objectiveId == DemoObjectiveIds.PathGuide) return 2;
            if (objectiveId == DemoObjectiveIds.MineLithium) return 3;
            if (objectiveId == DemoObjectiveIds.StructuralCrack) return 4;
            if (objectiveId == DemoObjectiveIds.PlaceSupport) return 5;
            if (objectiveId == DemoObjectiveIds.GasEncounter) return 6;
            if (objectiveId == DemoObjectiveIds.OutpostInstall) return 7;
            if (objectiveId == DemoObjectiveIds.ReturnRecommend) return 8;
            if (objectiveId == DemoObjectiveIds.Settlement) return 9;
            if (objectiveId == DemoObjectiveIds.BatteryUpgrade) return 10;
            if (objectiveId == DemoObjectiveIds.DeepSignal) return 11;
            if (objectiveId == DemoObjectiveIds.DemoEnd) return 12;
            return -1;
        }

        public static DemoObjectiveReadModel ToReadModel(
            string objectiveId,
            int completedCount,
            bool isDemoComplete)
        {
            var resolved = ResolveObjectiveId(objectiveId, completedCount);
            if (!TryGet(resolved, out var definition))
            {
                definition = OrderedDefinitions[0];
            }

            return new DemoObjectiveReadModel(
                definition.Id,
                definition.Title,
                definition.Description,
                definition.NextActionHint,
                completedCount,
                DemoObjectiveIds.RequiredCount,
                definition.IsTerminal,
                isDemoComplete,
                definition.ShowsDismissibleGuidance);
        }
    }
}
