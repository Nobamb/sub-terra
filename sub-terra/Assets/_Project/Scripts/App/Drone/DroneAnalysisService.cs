using System;
using System.Collections.Generic;
using System.Globalization;
using SubTerra.App.Core.Data;
using SubTerra.Shared;

namespace SubTerra.App.Drone
{
    /// <summary>
    /// A가 제공한 실제 상태만 점수화한다. 시간·난수·컬렉션 열거 순서는 추천에 관여하지 않는다.
    /// </summary>
    public sealed class DroneAnalysisService
    {
        private static readonly DroneAction[] TieBreakOrder =
        {
            DroneAction.LeaveGasZone,
            DroneAction.InstallSupport,
            DroneAction.Recharge,
            DroneAction.ReturnToBase,
            DroneAction.MineNearbyMineral,
            DroneAction.BuildOutpost,
            DroneAction.ContinueDescending
        };

        private readonly DroneAnalysisSettings settings;

        public DroneAnalysisService(DroneAnalysisSettings analysisSettings)
        {
            settings = analysisSettings
                ? analysisSettings
                : throw new ArgumentNullException(nameof(analysisSettings));
        }

        public DroneAnalysisResult Analyze(DroneContextDto context)
        {
            if (context == null)
            {
                return CreateFallback();
            }

            var builders = CreateBuilders();
            var hasStructural = IsUnitValue(context.structuralIntegrity);
            var hasGas = IsUnitValue(context.gasRisk);
            var hasEnergy = context.currentEnergy >= 0 && context.returnEnergyEstimate >= 0;
            var hasCargoValue = context.unsettledCargoValue >= 0;
            var hasCargoCapacity = context.cargoWeight >= 0f
                && !float.IsNaN(context.cargoWeight)
                && !float.IsInfinity(context.cargoWeight)
                && context.maxCargoWeight > 0f
                && !float.IsNaN(context.maxCargoWeight)
                && !float.IsInfinity(context.maxCargoWeight);
            var hasDistance = context.nearestBaseDistance >= 0f
                && !float.IsNaN(context.nearestBaseDistance)
                && !float.IsInfinity(context.nearestBaseDistance);
            var hasDepth = context.depth >= 0;
            var hasCompleteSafetyContext = hasStructural && hasGas && hasEnergy;
            var lowEnergy = hasEnergy
                && context.currentEnergy <= context.returnEnergyEstimate + settings.EnergyReserve;
            var structuralWarning = hasStructural
                && context.structuralIntegrity <= settings.StructuralWarningThreshold;
            var structuralCritical = hasStructural
                && context.structuralIntegrity <= settings.StructuralCriticalThreshold;
            var gasWarning = hasGas && context.gasRisk >= settings.GasWarningThreshold;
            var gasCritical = hasGas && context.gasRisk >= settings.GasCriticalThreshold;
            var cargoFull = hasCargoCapacity
                && context.cargoWeight >= context.maxCargoWeight;
            var lithiumNearby = ContainsOrdinal(
                context.nearbyMineralIds,
                DataIds.Minerals.Lithium);

            if (gasWarning)
            {
                var score = settings.GasExitScore
                    + (gasCritical ? settings.CriticalRiskBonus : 0);
                builders[DroneAction.LeaveGasZone].Add(
                    "gas_risk",
                    "가스 위험 " + Format(context.gasRisk),
                    context.gasRisk,
                    "risk",
                    score,
                    gasCritical ? 400 : 300);
            }

            if (structuralWarning)
            {
                var score = settings.SupportScore
                    + (structuralCritical ? settings.CriticalRiskBonus : 0);
                builders[DroneAction.InstallSupport].Add(
                    "structural_integrity",
                    "구조 안정도 " + Format(context.structuralIntegrity),
                    context.structuralIntegrity,
                    "ratio",
                    score,
                    structuralCritical ? 350 : 250);
            }

            if (lowEnergy)
            {
                var message = "현재 전력 " + context.currentEnergy
                    + ", 귀환 예상 " + context.returnEnergyEstimate;
                if (context.returnPathAvailable)
                {
                    builders[DroneAction.ReturnToBase].Add(
                        "low_energy",
                        message,
                        context.currentEnergy,
                        "energy",
                        settings.LowEnergyReturnScore,
                        200);
                }
                else
                {
                    builders[DroneAction.BuildOutpost].Add(
                        "return_path_unavailable",
                        message + ", 귀환 경로 없음",
                        context.currentEnergy,
                        "energy",
                        settings.LowEnergyReturnScore,
                        450);
                }

                if (hasDistance
                    && context.nearestBaseDistance <= settings.NearbyBaseDistance)
                {
                    builders[DroneAction.Recharge].Add(
                        "base_nearby",
                        "가장 가까운 기지 " + Format(context.nearestBaseDistance) + "m",
                        context.nearestBaseDistance,
                        "m",
                        settings.RechargeScore,
                        210);
                }
            }

            if (hasCargoValue
                && context.unsettledCargoValue >= settings.HighCargoValueThreshold
                && context.returnPathAvailable)
            {
                builders[DroneAction.ReturnToBase].Add(
                    "valuable_cargo",
                    "미정산 가치 " + context.unsettledCargoValue,
                    context.unsettledCargoValue,
                    "gold",
                    settings.CargoReturnScore,
                    0);
            }

            if (cargoFull && context.returnPathAvailable)
            {
                builders[DroneAction.ReturnToBase].Add(
                    "inventory_full",
                    "화물 " + Format(context.cargoWeight)
                        + "/" + Format(context.maxCargoWeight),
                    context.cargoWeight,
                    "weight",
                    settings.CargoReturnScore,
                    100);
            }

            if (lithiumNearby)
            {
                builders[DroneAction.MineNearbyMineral].Add(
                    "nearby_lithium",
                    "인근 광물 " + DataIds.Minerals.Lithium,
                    1,
                    "detected",
                    settings.LithiumScore,
                    0);
            }

            if (hasDepth
                && hasDistance
                && context.depth >= settings.OutpostMinimumDepth
                && context.nearestBaseDistance >= settings.OutpostDistance)
            {
                builders[DroneAction.BuildOutpost].Add(
                    "outpost_gap",
                    "심도 " + context.depth + ", 기지 거리 "
                        + Format(context.nearestBaseDistance) + "m",
                    context.nearestBaseDistance,
                    "m",
                    settings.OutpostScore,
                    0);
            }

            if (!context.returnPathAvailable && !lowEnergy)
            {
                builders[DroneAction.BuildOutpost].Add(
                    "return_path_unavailable",
                    "현재 귀환 경로를 사용할 수 없습니다.",
                    double.NaN,
                    string.Empty,
                    settings.OutpostScore,
                    150);
            }

            var usedFallback = !hasCompleteSafetyContext;
            if (usedFallback)
            {
                var hasKnownImmediateRisk = gasWarning || structuralWarning || lowEnergy;
                builders[DroneAction.ReturnToBase].Add(
                    "context_incomplete",
                    "핵심 안전 상태가 불완전해 추가 탐사를 보류합니다.",
                    double.NaN,
                    string.Empty,
                    0,
                    hasKnownImmediateRisk ? 100 : 500);
            }

            if (hasCompleteSafetyContext
                && !gasWarning
                && !structuralWarning
                && !lowEnergy)
            {
                builders[DroneAction.ContinueDescending].Add(
                    "exploration_safe",
                    hasDepth ? "현재 심도 " + context.depth : "확인된 즉시 위험 없음",
                    hasDepth ? context.depth : double.NaN,
                    hasDepth ? "depth" : string.Empty,
                    settings.DescendScore,
                    0);
            }

            var candidates = BuildCandidates(builders);
            var recommendation = SelectRecommendation(candidates);
            var dialogue = CreateDialogueRequest(
                context,
                recommendation,
                structuralWarning,
                structuralCritical,
                gasWarning,
                lowEnergy,
                cargoFull,
                lithiumNearby);
            return new DroneAnalysisResult(recommendation, candidates, dialogue, usedFallback);
        }

        private DroneAnalysisResult CreateFallback()
        {
            var reason = new DroneReason(
                "context_unavailable",
                "드론 상태를 확인할 수 없어 추가 탐사를 보류합니다.",
                double.NaN,
                string.Empty,
                0);
            var recommendation = new DroneActionScore(
                DroneAction.ReturnToBase,
                0,
                500,
                new[] { reason });
            var candidates = new List<DroneActionScore>();
            for (var i = 0; i < TieBreakOrder.Length; i++)
            {
                candidates.Add(TieBreakOrder[i] == DroneAction.ReturnToBase
                    ? recommendation
                    : new DroneActionScore(
                        TieBreakOrder[i],
                        0,
                        0,
                        Array.Empty<DroneReason>()));
            }

            var tokens = new Dictionary<string, string>
            {
                ["action"] = FormatAction(DroneAction.ReturnToBase),
                ["reason"] = reason.Message
            };
            return new DroneAnalysisResult(
                recommendation,
                candidates,
                new DroneDialogueRequest(DataIds.Dialogue.DroneEmergency, true, tokens),
                true);
        }

        private static Dictionary<DroneAction, CandidateBuilder> CreateBuilders()
        {
            var builders = new Dictionary<DroneAction, CandidateBuilder>();
            for (var i = 0; i < TieBreakOrder.Length; i++)
            {
                builders.Add(TieBreakOrder[i], new CandidateBuilder(TieBreakOrder[i]));
            }

            return builders;
        }

        private static IReadOnlyList<DroneActionScore> BuildCandidates(
            IReadOnlyDictionary<DroneAction, CandidateBuilder> builders)
        {
            var candidates = new List<DroneActionScore>(TieBreakOrder.Length);
            for (var i = 0; i < TieBreakOrder.Length; i++)
            {
                candidates.Add(builders[TieBreakOrder[i]].Build());
            }

            return candidates;
        }

        private static DroneActionScore SelectRecommendation(
            IReadOnlyList<DroneActionScore> candidates)
        {
            var best = candidates[0];
            for (var i = 1; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate.SafetyPriority > best.SafetyPriority
                    || (candidate.SafetyPriority == best.SafetyPriority
                        && candidate.Score > best.Score))
                {
                    best = candidate;
                }
            }

            return best;
        }

        private static DroneDialogueRequest CreateDialogueRequest(
            DroneContextDto context,
            DroneActionScore recommendation,
            bool structuralWarning,
            bool structuralCritical,
            bool gasWarning,
            bool lowEnergy,
            bool cargoFull,
            bool lithiumNearby)
        {
            string templateId;
            var urgent = false;
            if (HasReason(recommendation, "context_incomplete"))
            {
                templateId = DataIds.Dialogue.DroneEmergency;
                urgent = true;
            }
            else if (lowEnergy && !context.returnPathAvailable)
            {
                templateId = DataIds.Dialogue.DroneEmergency;
                urgent = true;
            }
            else if (structuralCritical)
            {
                templateId = DataIds.Dialogue.DroneStructuralWarning;
                urgent = true;
            }
            else if (gasWarning)
            {
                templateId = DataIds.Dialogue.DroneGasWarning;
                // 가스 진입은 일반 탐사 대사의 전역 쿨다운을 기다리지 않고 즉시 알린다.
                urgent = true;
            }
            else if (structuralWarning)
            {
                templateId = DataIds.Dialogue.DroneStructuralWarning;
            }
            else if (lowEnergy)
            {
                templateId = DataIds.Dialogue.LowPowerWarning;
                urgent = true;
            }
            else if (cargoFull && HasReason(recommendation, "inventory_full"))
            {
                templateId = DataIds.Dialogue.DroneCargoFull;
            }
            else if (recommendation.Action == DroneAction.ReturnToBase)
            {
                templateId = DataIds.Dialogue.DroneReturn;
            }
            else if (lithiumNearby)
            {
                templateId = DataIds.Dialogue.DroneLithium;
            }
            else if (recommendation.Action == DroneAction.BuildOutpost)
            {
                templateId = DataIds.Dialogue.DroneOutpost;
            }
            else
            {
                templateId = DataIds.Dialogue.DroneExplore;
            }

            var tokens = new Dictionary<string, string>
            {
                ["action"] = FormatAction(recommendation.Action),
                ["reason"] = recommendation.Reasons.Count > 0
                    ? recommendation.Reasons[0].Message
                    : string.Empty
            };

            if (context.currentEnergy >= 0)
            {
                tokens["currentEnergy"] =
                    context.currentEnergy.ToString(CultureInfo.InvariantCulture);
            }

            if (context.returnEnergyEstimate >= 0)
            {
                tokens["returnEnergyEstimate"] =
                    context.returnEnergyEstimate.ToString(CultureInfo.InvariantCulture);
            }

            if (IsUnitValue(context.structuralIntegrity))
            {
                tokens["structuralIntegrity"] = Format(context.structuralIntegrity);
            }

            if (IsUnitValue(context.gasRisk))
            {
                tokens["gasRisk"] = Format(context.gasRisk);
            }

            if (context.unsettledCargoValue >= 0)
            {
                tokens["unsettledCargoValue"] =
                    context.unsettledCargoValue.ToString(CultureInfo.InvariantCulture);
            }

            if (context.cargoWeight >= 0f
                && !float.IsNaN(context.cargoWeight)
                && !float.IsInfinity(context.cargoWeight))
            {
                tokens["cargoWeight"] = Format(context.cargoWeight);
            }

            if (context.maxCargoWeight > 0f
                && !float.IsNaN(context.maxCargoWeight)
                && !float.IsInfinity(context.maxCargoWeight))
            {
                tokens["maxCargoWeight"] = Format(context.maxCargoWeight);
            }

            if (context.nearestBaseDistance >= 0f
                && !float.IsNaN(context.nearestBaseDistance)
                && !float.IsInfinity(context.nearestBaseDistance))
            {
                tokens["nearestBaseDistance"] = Format(context.nearestBaseDistance);
            }

            if (context.depth >= 0)
            {
                tokens["depth"] = context.depth.ToString(CultureInfo.InvariantCulture);
            }

            if (lithiumNearby)
            {
                tokens["mineralId"] = DataIds.Minerals.Lithium;
            }

            return new DroneDialogueRequest(templateId, urgent, tokens);
        }

        private static bool HasReason(DroneActionScore candidate, string reasonId)
        {
            for (var i = 0; i < candidate.Reasons.Count; i++)
            {
                if (string.Equals(
                    candidate.Reasons[i].Id,
                    reasonId,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsUnitValue(float value)
        {
            return !float.IsNaN(value)
                && !float.IsInfinity(value)
                && value >= 0f
                && value <= 1f;
        }

        private static bool ContainsOrdinal(IReadOnlyList<string> values, string expected)
        {
            if (values == null)
            {
                return false;
            }

            for (var i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], expected, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Format(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value)
                ? string.Empty
                : value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        public static string FormatAction(DroneAction action)
        {
            switch (action)
            {
                case DroneAction.ReturnToBase:
                    return "기지로 귀환";
                case DroneAction.InstallSupport:
                    return "버팀목 설치";
                case DroneAction.LeaveGasZone:
                    return "가스 구역 이탈";
                case DroneAction.MineNearbyMineral:
                    return "인근 광물 채굴";
                case DroneAction.BuildOutpost:
                    return "전진기지 설치";
                case DroneAction.Recharge:
                    return "충전";
                default:
                    return "하강 계속";
            }
        }

        private sealed class CandidateBuilder
        {
            private readonly List<DroneReason> reasons = new List<DroneReason>();
            private int score;
            private int safetyPriority;

            public DroneAction Action { get; }

            public CandidateBuilder(DroneAction action)
            {
                Action = action;
            }

            public void Add(
                string id,
                string message,
                double actualValue,
                string unit,
                int addedScore,
                int addedSafetyPriority)
            {
                score += addedScore;
                safetyPriority = Math.Max(safetyPriority, addedSafetyPriority);
                reasons.Add(new DroneReason(
                    id,
                    message,
                    actualValue,
                    unit,
                    addedScore));
            }

            public DroneActionScore Build()
            {
                return new DroneActionScore(Action, score, safetyPriority, reasons.ToArray());
            }
        }
    }
}
