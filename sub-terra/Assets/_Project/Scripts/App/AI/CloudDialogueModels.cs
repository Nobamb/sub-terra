using System;
using System.Collections.Generic;
using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;

namespace SubTerra.App.AI
{
    [Serializable]
    public sealed class CloudDialogueFactDto
    {
        public string key;
        public double value;
        public string unit;
    }

    /// <summary>
    /// 외부 전송 allowlist. Phase I의 확정 행동, 근거 ID, 근거 수치와 언어 외 필드는 두지 않는다.
    /// </summary>
    [Serializable]
    public sealed class CloudDialogueRequestDto
    {
        public string actionKey;
        public string[] reasonKeys;
        public CloudDialogueFactDto[] facts;
        public string language;

        public static CloudDialogueRequestDto FromAnalysis(
            DroneAnalysisResult analysis,
            string language)
        {
            if (analysis == null)
            {
                throw new ArgumentNullException(nameof(analysis));
            }

            var reasonKeys = new List<string>();
            var facts = new List<CloudDialogueFactDto>();
            var reasons = analysis.Recommendation.Reasons;
            for (var i = 0; i < reasons.Count; i++)
            {
                var reason = reasons[i];
                if (string.IsNullOrWhiteSpace(reason.Id))
                {
                    continue;
                }

                reasonKeys.Add(reason.Id);
                if (!double.IsNaN(reason.ActualValue)
                    && !double.IsInfinity(reason.ActualValue))
                {
                    facts.Add(new CloudDialogueFactDto
                    {
                        key = reason.Id,
                        value = reason.ActualValue,
                        unit = reason.Unit
                    });
                }
            }

            return new CloudDialogueRequestDto
            {
                actionKey = analysis.RecommendedAction.ToString(),
                reasonKeys = reasonKeys.ToArray(),
                facts = facts.ToArray(),
                language = string.IsNullOrWhiteSpace(language) ? "ko" : language
            };
        }
    }

    [Serializable]
    public sealed class CloudDialogueResponseDto
    {
        public string dialogue;
    }

    public sealed class DialogueGenerationResult
    {
        public DroneAnalysisResult Analysis { get; }
        public DroneDialogueResult Dialogue { get; }
        public bool UsedCloud { get; }
        public bool WasCancelled { get; }

        public DialogueGenerationResult(
            DroneAnalysisResult analysis,
            DroneDialogueResult dialogue,
            bool usedCloud,
            bool wasCancelled = false)
        {
            Analysis = analysis;
            Dialogue = dialogue;
            UsedCloud = usedCloud;
            WasCancelled = wasCancelled;
        }
    }
}
