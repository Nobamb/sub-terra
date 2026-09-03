using System.Collections.Generic;

namespace SubTerra.App.Tutorial
{
    /// <summary>prompt-B 60/86에서 지정한 데모 퀘스트 18개의 문구와 순서.</summary>
    public static class DemoObjectiveCatalog
    {
        public const string IntroductionGuidanceTitle = "생존자 브리핑";
        public const string DemoCompleteTitle = "모든 데모 목표 완료";
        public const string DemoCompleteDescription =
            "긴급 탈출 포탈을 통한 귀환까지 완료했습니다.";
        public const string IntroductionGuidanceBody =
            "당신은 재앙 이후, 얼마 남지 않은 생존자입니다. 인류는 정점의 기술력을 가졌지만, 지상의 자원은 모두 사라졌습니다. 남은 것은 지하 광산과, 인간의 유전자가 보존된 배양시설뿐입니다.\n"
            + "광산의 자원으로 배양시설을 다시 가동해야, 인류를 되살릴 수 있습니다.\n"
            + "[조작 안내] 자원 근처에서 Enter 또는 마우스 클릭으로 채굴할 수 있습니다. 희귀 자원을 모으고 장비를 업그레이드해, 더 깊은 곳으로 나아가십시오.";

        private static readonly Dictionary<string, DemoObjectiveDefinition> ById;
        private static readonly DemoObjectiveDefinition[] OrderedDefinitions;

        static DemoObjectiveCatalog()
        {
            OrderedDefinitions = new[]
            {
                Define(
                    DemoObjectiveIds.MineBlock,
                    "블록 제거",
                    "Enter 키 또는 마우스 클릭으로 블록 하나를 제거하세요.",
                    "제거할 블록을 향해 Enter 또는 클릭",
                    DemoProgressSignal.BlockMined,
                    DemoObjectiveIds.MineCopper,
                    true,
                    IntroductionGuidanceTitle,
                    IntroductionGuidanceBody),
                Define(DemoObjectiveIds.MineCopper, "구리 채취", "구리 블록을 직접 채굴해 구리를 획득하세요.", "구리 블록을 찾아 채굴", DemoProgressSignal.CopperMined, DemoObjectiveIds.UpgradeDrillSpeed),
                Define(DemoObjectiveIds.UpgradeDrillSpeed, "드릴 속도 업그레이드", "업그레이드 창을 열고 드릴 속도를 1회 높이세요.", "업그레이드 창에서 드릴 속도 구매", DemoProgressSignal.DrillSpeedUpgraded, DemoObjectiveIds.TravelToSurface),
                Define(DemoObjectiveIds.TravelToSurface, "지상으로 이동", "엘리베이터를 이용해 지상 기지로 이동하세요.", "엘리베이터에서 지상 기지 선택", DemoProgressSignal.SurfaceReachedByElevator, DemoObjectiveIds.ReturnToMine),
                Define(DemoObjectiveIds.ReturnToMine, "광산 탐사 재개", "다시 광산으로 돌아온 뒤, 엘리베이터에서 벗어나 채굴을 이어가주세요.", "광산 도착 후 엘리베이터 아래 검은색 블록 3칸에서 벗어나기", DemoProgressSignal.MineReachedByElevator, DemoObjectiveIds.MineIron),
                Define(DemoObjectiveIds.MineIron, "철 채취", "철 블록을 직접 채굴해 철을 획득하세요.", "철 블록을 찾아 채굴", DemoProgressSignal.IronMined, DemoObjectiveIds.PlaceSupportInDanger),
                Define(DemoObjectiveIds.PlaceSupportInDanger, "위험 지대 보강", "구조 위험 경고가 활성화된 지대에 버팀목을 설치하세요.", "위험 경고가 표시된 곳에 버팀목 배치", DemoProgressSignal.SupportPlacedInDanger, DemoObjectiveIds.PlaceLadder),
                Define(DemoObjectiveIds.PlaceLadder, "사다리 설치", "건설 메뉴에서 사다리를 선택해 설치하세요.", "이동할 수직 통로에 사다리 배치", DemoProgressSignal.LadderPlaced, DemoObjectiveIds.PlaceLightAtDepth),
                Define(DemoObjectiveIds.PlaceLightAtDepth, "심부 조명 설치", "지하 10m 이상 내려간 뒤 조명을 설치하세요.", "깊이 표시가 10m 이상일 때 조명 배치", DemoProgressSignal.LightPlacedAtDepth, DemoObjectiveIds.StoreMineral),
                Define(DemoObjectiveIds.StoreMineral, "광물 보관", "보관함을 설치한 뒤 아무 광물이나 1개 이상 보관하세요.", "보관함 배치 후 보관함에서 광물 맡기기", DemoProgressSignal.MineralStored, DemoObjectiveIds.InstallOutpostCore),
                Define(DemoObjectiveIds.InstallOutpostCore, "전진기지 코어 설치", "전진기지 코어를 설치해 체크포인트를 활성화하세요.", "건설 메뉴에서 전진기지 코어 배치", DemoProgressSignal.OutpostCoreInstalled, DemoObjectiveIds.ChargeNearOutpost),
                Define(DemoObjectiveIds.ChargeNearOutpost, "전진기지에서 충전", "방금 설치한 전진기지 코어 근처에 충전기를 설치한 뒤 전력을 충전하세요.", "코어 10칸 안에 충전기 배치 후 충전", DemoProgressSignal.ChargedNearOutpost, DemoObjectiveIds.HealNearOutpost),
                Define(DemoObjectiveIds.HealNearOutpost, "전진기지에서 회복", "전진기지 코어 근처에 보건소를 설치한 뒤 체력을 회복하세요.", "코어 10칸 안에 보건소 배치 후 회복", DemoProgressSignal.HealedNearOutpost, DemoObjectiveIds.UnlockDeepZone),
                Define(DemoObjectiveIds.UnlockDeepZone, "심층 구역 해금", "드릴 속도 2레벨, 드론 스캔 2레벨, 가스 저항 1레벨을 갖춰 심층 구역을 해금하세요.", "필요 업그레이드를 구매해 심층 잠금 해제", DemoProgressSignal.DeepZoneUnlocked, DemoObjectiveIds.MineLithium),
                Define(DemoObjectiveIds.MineLithium, "리튬 채취", "심층 구역에서 리튬 블록을 직접 채굴하세요.", "심층 리튬 광맥 채굴", DemoProgressSignal.LithiumMined, DemoObjectiveIds.PurifyGasWithOutpost),
                Define(DemoObjectiveIds.PurifyGasWithOutpost, "가스 정화", "리튬 또는 가스 블록 5칸 안에 전진기지 코어를 먼저 설치하고, 그 블록을 채굴한 뒤 나온 가스에 접근해 코어의 정화 효과를 받으세요.", "코어 설치 → 근처 리튬/가스 블록 채굴 → 정화 범위에서 가스 접근", DemoProgressSignal.GasPurifiedByOutpost, DemoObjectiveIds.SellAtSettlement),
                Define(DemoObjectiveIds.SellAtSettlement, "전진기지 자원 판매", "전진기지 코어 10칸 안에 정산 콘솔을 설치한 뒤 광물을 판매하세요.", "정산 콘솔 배치 후 광물 판매", DemoProgressSignal.MineralSoldAtSettlement, DemoObjectiveIds.EmergencyEscapeReturn),
                new DemoObjectiveDefinition(
                    DemoObjectiveIds.EmergencyEscapeReturn,
                    "긴급 탈출 귀환",
                    "긴급 탈출 포탈을 사용해 전진기지 코어 또는 엘리베이터로 귀환하세요.",
                    "포탈 탑승 후 목적지를 선택해 긴급 이동",
                    DemoProgressSignal.EmergencyEscapeSucceeded,
                    string.Empty,
                    isTerminal: true)
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

        public static DemoObjectiveDefinition GetRequired(string objectiveId) =>
            TryGet(objectiveId, out var definition) ? definition : null;

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

        /// <summary>이전 13단계 세이브를 포함해 ID/개수가 어긋나면 완료 개수 기준 새 퀘스트로 안전하게 이동한다.</summary>
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

            return TryGet(savedObjectiveId, out _)
                && IndexOf(savedObjectiveId) == completedCount
                    ? savedObjectiveId
                    : OrderedDefinitions[completedCount].Id;
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

            // 마지막 퀘스트 완료 뒤에도 같은 영구 ID를 유지하므로, 완료 전 작업 문구를
            // 그대로 내보내면 실제 State가 완료됐어도 HUD에서는 미완료처럼 보인다.
            var title = isDemoComplete ? DemoCompleteTitle : definition.Title;
            var description = isDemoComplete
                ? DemoCompleteDescription
                : definition.Description;
            var nextActionHint = isDemoComplete ? string.Empty : definition.NextActionHint;

            return new DemoObjectiveReadModel(
                definition.Id,
                title,
                description,
                nextActionHint,
                completedCount,
                DemoObjectiveIds.RequiredCount,
                definition.IsTerminal,
                isDemoComplete,
                definition.ShowsDismissibleGuidance,
                definition.GuidanceTitle,
                definition.GuidanceBody);
        }

        private static DemoObjectiveDefinition Define(
            string id,
            string title,
            string description,
            string hint,
            DemoProgressSignal signal,
            string nextId,
            bool guidance = false,
            string guidanceTitle = "",
            string guidanceBody = "")
        {
            return new DemoObjectiveDefinition(
                id,
                title,
                description,
                hint,
                signal,
                nextId,
                showsDismissibleGuidance: guidance,
                guidanceTitle: guidanceTitle,
                guidanceBody: guidanceBody);
        }
    }
}
