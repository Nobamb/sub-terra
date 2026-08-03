using System.Collections.Generic;

namespace SubTerra.App.Readiness
{
    /// <summary>
    /// work_process/MVP2/MVP-B/README.md 필수 기능 표의 기계 판독 기준선.
    /// 상태는 증거 태그 규칙으로만 산출하며, 대역 증거만으로 완료 처리하지 않는다.
    /// </summary>
    public static class Mvp2EssentialFeatureMatrix
    {
        /// <summary>README §2 현재 상태 감사 요약의 필수 행.</summary>
        public static IReadOnlyList<ReadinessFeatureEntry> CreateBaselineEntries()
        {
            return new List<ReadinessFeatureEntry>
            {
                Entry(
                    "terrain-40m",
                    "40줄 지형",
                    "B",
                    requiresRestore: true,
                    EvidenceKind.Definition | EvidenceKind.SurrogateTest | EvidenceKind.RuntimePrefab,
                    "통합 Scene에 40줄 암석이 있으나 특수 타일은 소수 고정 좌표"),
                Entry(
                    "depth-distribution",
                    "깊이별 자원·가스",
                    "B",
                    requiresRestore: true,
                    EvidenceKind.None,
                    "Seed 기반 생성기와 상/중/심층 분포 규칙이 없음"),
                Entry(
                    "elevator-ladder",
                    "엘리베이터·사다리",
                    "C",
                    requiresRestore: false,
                    EvidenceKind.None,
                    "Player는 좌우 이동/점프만 제공"),
                Entry(
                    "camera-confiner",
                    "카메라 추적·맵 경계",
                    "D",
                    requiresRestore: false,
                    EvidenceKind.Definition | EvidenceKind.RuntimePrefab,
                    "PlayerCameraFollow SmoothDamp는 있으나 경계 제한이 없음"),
                Entry(
                    "mining-energy-loop",
                    "방향 채굴·전력",
                    "E",
                    requiresRestore: false,
                    EvidenceKind.Definition | EvidenceKind.SurrogateTest | EvidenceKind.RuntimePrefab,
                    "클릭/Enter 타일 제거는 있으나 채굴 시간·드릴·전력이 한 흐름으로 미연결"),
                Entry(
                    "cargo-speed",
                    "화물 중량 감속",
                    "E",
                    requiresRestore: false,
                    EvidenceKind.Definition | EvidenceKind.SurrogateTest,
                    "SetCargoSpeedMultiplier API만 있고 호출 경로가 없음"),
                Entry(
                    "support-placement",
                    "버팀목 Preview/설치",
                    "F",
                    requiresRestore: true,
                    EvidenceKind.Definition | EvidenceKind.SurrogateTest | EvidenceKind.RuntimePrefab,
                    "Preview·배치 시스템·Support Prefab은 있으나 통합 입력 사용자 흐름 재증명 필요"),
                Entry(
                    "structural-collapse",
                    "균열·부분 붕괴",
                    "G",
                    requiresRestore: true,
                    EvidenceKind.Definition | EvidenceKind.SurrogateTest
                        | EvidenceKind.RuntimePrefab | EvidenceKind.Restore | EvidenceKind.Play,
                    "4단계 경고·분리 균열 Overlay·결정론적 타일 제거·Collider/Snapshot·Shared 붕괴 이벤트 검증 완료"),
                Entry(
                    "gas-hazard-effects",
                    "가스 위험 실제 효과",
                    "H",
                    requiresRestore: true,
                    EvidenceKind.Definition | EvidenceKind.SurrogateTest | EvidenceKind.RuntimePrefab,
                    "Zone과 HUD 이벤트는 있으나 전력/이동/시야/피해 효과가 없음"),
                Entry(
                    "building-power-grid",
                    "시설·전력망 Runtime",
                    "I",
                    requiresRestore: true,
                    EvidenceKind.Definition | EvidenceKind.SurrogateTest,
                    "Power graph와 일부 Prefab은 있으나 BuildingData 다수가 공용 placeholder"),
                Entry(
                    "outpost-return",
                    "전진기지·귀환",
                    "I,J",
                    requiresRestore: true,
                    EvidenceKind.Definition | EvidenceKind.SurrogateTest,
                    "App 서비스와 패널은 있으나 월드 상호작용·체크포인트 귀환 경로 미증명"),
                Entry(
                    "drone-world-popup",
                    "드론 World Space 알림",
                    "K",
                    requiresRestore: false,
                    EvidenceKind.Definition | EvidenceKind.SurrogateTest,
                    "규칙 엔진과 화면 UI는 있으나 머리 위 팝업·실제 위험 트리거 없음"),
                Entry(
                    "structural-failure-rescue",
                    "구조 실패/구조",
                    "L",
                    requiresRestore: true,
                    EvidenceKind.None,
                    "이벤트 enum과 업그레이드 효과는 있으나 피해·화물 손실·복귀 Orchestrator 없음"),
                Entry(
                    "world-save-roundtrip",
                    "월드 저장·복원",
                    "M",
                    requiresRestore: true,
                    EvidenceKind.Definition | EvidenceKind.SurrogateTest | EvidenceKind.Restore,
                    "변경점 DTO는 있으나 Seed=0·기본 월드 왕복 증거 부족"),
                Entry(
                    "surface-economy-progress",
                    "지상 경제·진행",
                    "N",
                    requiresRestore: true,
                    EvidenceKind.Definition | EvidenceKind.SurrogateTest,
                    "메뉴·판매·업그레이드 서비스는 있으나 한 Run 귀환 결과 연결 검증 필요"),
                Entry(
                    "demo-full-run",
                    "데모 완주·밸런스",
                    "O",
                    requiresRestore: false,
                    EvidenceKind.Definition | EvidenceKind.SurrogateTest,
                    "목표 State와 UI는 있으나 실제 입력 기반 전체 완주 증거 부족"),
                Entry(
                    "windows-deploy",
                    "Windows 배포",
                    "P",
                    requiresRestore: false,
                    EvidenceKind.None,
                    "Build Profile과 다른 PC 실행 결과물이 저장소에서 확인되지 않음")
            };
        }

        /// <summary>README §4 PRD 최종 완료 조건 추적 행(기능 ID 집합).</summary>
        public static IReadOnlyList<string> RequiredPrdCompletionConditionIds()
        {
            return new[]
            {
                "new-game-start",
                "move-and-mine",
                "mineral-price-craft",
                "structural-risk-grows",
                "support-stabilizes",
                "gas-affects-exploration",
                "power-pressure-return",
                "build-outpost",
                "light-charger-work",
                "drone-shows-reasons",
                "failure-loss-return",
                "settle-minerals",
                "upgrade-deep-zone",
                "save-survives-restart",
                "windows-other-pc",
                "demo-no-fatal-console"
            };
        }

        /// <summary>PRD 완료 조건 → 담당 단계 매핑(누락 0 검증용).</summary>
        public static IReadOnlyDictionary<string, string> PrdCompletionConditionStages()
        {
            return new Dictionary<string, string>
            {
                ["new-game-start"] = "A,J,N,O",
                ["move-and-mine"] = "C,E",
                ["mineral-price-craft"] = "B,E,N",
                ["structural-risk-grows"] = "G",
                ["support-stabilizes"] = "F,G",
                ["gas-affects-exploration"] = "H,K",
                ["power-pressure-return"] = "E,H,J,K",
                ["build-outpost"] = "I",
                ["light-charger-work"] = "I",
                ["drone-shows-reasons"] = "K",
                ["failure-loss-return"] = "L",
                ["settle-minerals"] = "I,J,N",
                ["upgrade-deep-zone"] = "N,O",
                ["save-survives-restart"] = "M",
                ["windows-other-pc"] = "P",
                ["demo-no-fatal-console"] = "O,P"
            };
        }

        private static ReadinessFeatureEntry Entry(
            string id,
            string name,
            string stage,
            bool requiresRestore,
            EvidenceKind evidence,
            string notes)
        {
            return new ReadinessFeatureEntry(id, name, stage, requiresRestore, evidence, notes);
        }
    }
}
