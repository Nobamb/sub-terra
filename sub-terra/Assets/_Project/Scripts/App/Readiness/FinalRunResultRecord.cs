using System.Collections.Generic;
using System.Text;

namespace SubTerra.App.Readiness
{
    public enum FinalRunStepStatus
    {
        Pending = 0,
        Passed = 1,
        Failed = 2,
        Blocked = 3,
        Skipped = 4
    }

    /// <summary>최종 완주 시나리오의 한 단계 결과.</summary>
    public sealed class FinalRunStepResult
    {
        public string StepId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public FinalRunStepStatus Status { get; set; } = FinalRunStepStatus.Pending;
        public string Detail { get; set; } = string.Empty;
    }

    /// <summary>
    /// B~P가 확장하는 실제 입력 기반 최종 완주 결과 기록 형식.
    /// Phase A는 뼈대와 격리 계약만 확정한다.
    /// </summary>
    public sealed class FinalRunResultRecord
    {
        public const string EntryPathContract =
            "Bootstrap → MainMenu → SurfaceBase → Mine_Demo_Integration → Return/Fail → Save → Continue";

        public string ScenarioId { get; set; } = "mvp2-full-demo";
        public string EntryPath { get; set; } = EntryPathContract;
        public string IsolatedSaveRoot { get; set; } = string.Empty;
        public bool UsedIsolatedSaveRoot { get; set; }
        public string OverallStatus { get; set; } = "pending";
        public List<FinalRunStepResult> Steps { get; set; } = new List<FinalRunStepResult>();

        /// <summary>README §8 필수 최종 완주 시나리오 뼈대 단계를 채운다.</summary>
        public static FinalRunResultRecord CreateSkeleton(string isolatedSaveRoot)
        {
            var record = new FinalRunResultRecord
            {
                IsolatedSaveRoot = isolatedSaveRoot ?? string.Empty,
                UsedIsolatedSaveRoot = FinalRunTestPaths.IsIsolatedTempRoot(isolatedSaveRoot),
                OverallStatus = "skeleton",
                Steps = new List<FinalRunStepResult>
                {
                    Step("01-new-game", "Bootstrap에서 새 게임 후 Surface Base 진입"),
                    Step("02-elevator", "시작 엘리베이터로 Mine 하강"),
                    Step("03-shallow-mine", "상층 구리 채굴 및 전력·화물 확인"),
                    Step("04-ladder-return", "수직 굴착 뒤 사다리/발판 복귀 경로"),
                    Step("05-support", "균열 경고 후 버팀목 설치"),
                    Step("06-mid-gas", "중층 철/가스 효과와 드론 경고"),
                    Step("07-facilities", "조명·충전기·보관함·정산기·코어 설치"),
                    Step("08-outpost", "전진기지 충전/보관/정산/체크포인트"),
                    Step("09-deep-signal", "심층 리튬과 잠긴 신호"),
                    Step("10-return", "정상 귀환 또는 구조 실패 후 Surface 복귀"),
                    Step("11-economy-save", "판매·업그레이드 후 저장 및 종료"),
                    Step("12-continue", "이어하기로 월드·시설·진행 복원")
                }
            };
            return record;
        }

        public string FormatText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("FinalRunResultRecord");
            sb.AppendLine("ScenarioId: " + ScenarioId);
            sb.AppendLine("EntryPath: " + EntryPath);
            sb.AppendLine("IsolatedSaveRoot: " + IsolatedSaveRoot);
            sb.AppendLine("UsedIsolatedSaveRoot: " + UsedIsolatedSaveRoot);
            sb.AppendLine("OverallStatus: " + OverallStatus);
            sb.AppendLine("Steps:");
            for (var i = 0; i < Steps.Count; i++)
            {
                var step = Steps[i];
                sb.AppendLine(
                    $"- [{step.Status}] {step.StepId}: {step.Description}" +
                    (string.IsNullOrEmpty(step.Detail) ? string.Empty : " (" + step.Detail + ")"));
            }

            return sb.ToString();
        }

        private static FinalRunStepResult Step(string id, string description)
        {
            return new FinalRunStepResult
            {
                StepId = id,
                Description = description,
                Status = FinalRunStepStatus.Pending,
                Detail = "Phase A skeleton — filled by later stages"
            };
        }
    }
}
