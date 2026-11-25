using System;
using System.Collections.Generic;
using System.Globalization;
using SubTerra.App.Core.Data;
using SubTerra.Shared;
using static SubTerra.Shared.Localization.LocalizationService;

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
            var engineFuelNearby = ContainsOrdinal(
                context.nearbyMineralIds,
                DataIds.RareItems.EngineFuel);

            if (gasWarning)
            {
                var score = settings.GasExitScore
                    + (gasCritical ? settings.CriticalRiskBonus : 0);
                builders[DroneAction.LeaveGasZone].Add(
                    "gas_risk",
                    Text("가스 위험 ", "Gas risk: ") + Format(context.gasRisk),
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
                    StructuralGuidance,
                    context.structuralIntegrity,
                    "ratio",
                    score,
                    structuralCritical ? 350 : 250);
            }

            if (lowEnergy)
            {
                var message = Text("현재 전력 ", "Power: ") + context.currentEnergy
                    + Text(", 귀환 예상 ", ", estimated return cost: ") + context.returnEnergyEstimate;
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
                        message + Text(", 귀환 경로 없음", ", no return route"),
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
                        Text("가장 가까운 기지 ", "Nearest base: ") + Format(context.nearestBaseDistance) + "m",
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
                    Text("미정산 가치 ", "Unsold cargo value: ") + context.unsettledCargoValue,
                    context.unsettledCargoValue,
                    "gold",
                    settings.CargoReturnScore,
                    0);
            }

            if (cargoFull && context.returnPathAvailable)
            {
                builders[DroneAction.ReturnToBase].Add(
                    "inventory_full",
                    Text("화물 ", "Cargo: ") + Format(context.cargoWeight)
                        + "/" + Format(context.maxCargoWeight),
                    context.cargoWeight,
                    "weight",
                    settings.CargoReturnScore,
                    100);
            }

            if (engineFuelNearby)
            {
                builders[DroneAction.MineNearbyMineral].Add(
                    "rare_signal",
                    Text("희귀 신호 / 엔진 연료 후보", "Rare signal / possible engine fuel"),
                    1,
                    "detected",
                    settings.LithiumScore + 5,
                    0);
            }
            else if (lithiumNearby)
            {
                builders[DroneAction.MineNearbyMineral].Add(
                    "nearby_lithium",
                    Text("인근 광물 ", "Nearby mineral: ") + ItemDisplayNames.LocalizedMineral(DataIds.Minerals.Lithium),
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
                    Text("심도 ", "Depth: ") + context.depth + Text(", 기지 거리 ", ", distance to base: ")
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
                    Text("현재 귀환 경로를 사용할 수 없습니다.", "The return route is currently unavailable."),
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
                    Text("핵심 안전 상태가 불완전해 추가 탐사를 보류합니다.", "Pause exploration until safety conditions are known."),
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
                    hasDepth ? Text("현재 심도 ", "Current depth: ") + context.depth : Text("확인된 즉시 위험 없음", "No immediate risk detected"),
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
                lithiumNearby,
                engineFuelNearby);
            return new DroneAnalysisResult(recommendation, candidates, dialogue, usedFallback);
        }

        private DroneAnalysisResult CreateFallback()
        {
            var reason = new DroneReason(
                "context_unavailable",
                Text("드론 상태를 확인할 수 없어 추가 탐사를 보류합니다.", "Pause exploration until the drone status can be checked."),
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
            bool lithiumNearby,
            bool engineFuelNearby)
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
            else if (engineFuelNearby)
            {
                templateId = DataIds.Dialogue.DroneExplore;
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
                    : string.Empty,
                ["safetyGuidance"] = CreateSafetyGuidance(
                    context,
                    structuralWarning,
                    gasWarning)
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

            if (engineFuelNearby)
            {
                tokens["mineralId"] = DataIds.RareItems.EngineFuel;
            }
            else if (lithiumNearby)
            {
                tokens["mineralId"] = DataIds.Minerals.Lithium;
            }

            return new DroneDialogueRequest(templateId, urgent, tokens);
        }

        private static string CreateSafetyGuidance(
            DroneContextDto context,
            bool structuralWarning,
            bool gasWarning)
        {
            var guidance = string.Empty;
            if (gasWarning)
            {
                guidance = Text("가스 위험 ", "Gas risk: ") + Format(context.gasRisk)
                    + Text(". 가스 구역에서 이탈하거나 " + ItemDisplayNames.OutpostName + "를 설치해 안전지대를 확보하세요.",
                        ". Leave the gas zone or build an outpost to create a safe area.");
            }

            if (structuralWarning)
            {
                if (guidance.Length > 0)
                {
                    guidance += " ";
                }

                guidance += StructuralGuidance;
            }

            return guidance;
        }

        private static string StructuralGuidance => Text("주변을 살피고 안전한 쪽으로 이동하세요.", "Check your surroundings and move to safety.");

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
                    return Text("기지로 귀환", "Return to Base");
                case DroneAction.InstallSupport:
                    return Text("완충판 설치", "Install a Cushion Pad");
                case DroneAction.LeaveGasZone:
                    return Text("가스 구역 이탈", "Leave the Gas Zone");
                case DroneAction.MineNearbyMineral:
                    return Text("인근 광물 채굴", "Mine Nearby Minerals");
                case DroneAction.BuildOutpost:
                    return Text("전진기지 설치", "Build an Outpost");
                case DroneAction.Recharge:
                    return Text("충전", "Recharge");
                default:
                    return Text("하강 계속", "Continue Descending");
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
