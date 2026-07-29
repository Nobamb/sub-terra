using System;
using System.Collections.Generic;

namespace SubTerra.App.Drone
{
    public sealed class DroneReason
    {
        public string Id { get; }
        public string Message { get; }
        public double ActualValue { get; }
        public string Unit { get; }
        public int Score { get; }

        public DroneReason(
            string id,
            string message,
            double actualValue,
            string unit,
            int score)
        {
            Id = id ?? string.Empty;
            Message = message ?? string.Empty;
            ActualValue = actualValue;
            Unit = unit ?? string.Empty;
            Score = score;
        }
    }

    public sealed class DroneActionScore
    {
        private readonly IReadOnlyList<DroneReason> reasons;

        public DroneAction Action { get; }
        public int Score { get; }
        public int SafetyPriority { get; }
        public IReadOnlyList<DroneReason> Reasons => reasons;

        public DroneActionScore(
            DroneAction action,
            int score,
            int safetyPriority,
            IReadOnlyList<DroneReason> reasons)
        {
            Action = action;
            Score = score;
            SafetyPriority = safetyPriority;
            this.reasons = reasons ?? Array.Empty<DroneReason>();
        }
    }

    public sealed class DroneDialogueRequest
    {
        private readonly IReadOnlyDictionary<string, string> tokens;

        public string TemplateId { get; }
        public bool IsUrgent { get; }
        public IReadOnlyDictionary<string, string> Tokens => tokens;

        public DroneDialogueRequest(
            string templateId,
            bool isUrgent,
            IReadOnlyDictionary<string, string> tokens)
        {
            TemplateId = templateId ?? string.Empty;
            IsUrgent = isUrgent;
            this.tokens = tokens ?? new Dictionary<string, string>();
        }
    }

    public sealed class DroneAnalysisResult
    {
        private readonly IReadOnlyList<DroneActionScore> candidates;

        public DroneAction RecommendedAction { get; }
        public DroneActionScore Recommendation { get; }
        public IReadOnlyList<DroneActionScore> Candidates => candidates;
        public DroneDialogueRequest Dialogue { get; }
        public bool UsedFallback { get; }

        public DroneAnalysisResult(
            DroneActionScore recommendation,
            IReadOnlyList<DroneActionScore> candidates,
            DroneDialogueRequest dialogue,
            bool usedFallback)
        {
            Recommendation = recommendation
                ?? throw new ArgumentNullException(nameof(recommendation));
            RecommendedAction = recommendation.Action;
            this.candidates = candidates ?? Array.Empty<DroneActionScore>();
            Dialogue = dialogue
                ?? throw new ArgumentNullException(nameof(dialogue));
            UsedFallback = usedFallback;
        }

        public DroneActionScore FindCandidate(DroneAction action)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Action == action)
                {
                    return candidates[i];
                }
            }

            return null;
        }
    }
}
