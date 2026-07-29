using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SubTerra.App.Core.Data;

namespace SubTerra.App.Drone.Dialogue
{
    /// <summary>
    /// 분석 결과가 지정한 템플릿과 사실 토큰만 사용한다. 추천이나 위험 수치를 다시 계산하지 않는다.
    /// </summary>
    public sealed class TemplateDialogueGenerator
    {
        private static readonly Regex TokenPattern =
            new Regex(@"\{([A-Za-z0-9_]+)\}", RegexOptions.Compiled);

        private readonly Dictionary<string, DialogueTemplateData> templates =
            new Dictionary<string, DialogueTemplateData>(StringComparer.Ordinal);
        private readonly Dictionary<string, double> lastShownAt =
            new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly IDroneClock clock;
        private readonly DroneAnalysisSettings settings;

        public TemplateDialogueGenerator(
            IEnumerable<DialogueTemplateData> dialogueTemplates,
            IDroneClock droneClock,
            DroneAnalysisSettings analysisSettings)
        {
            clock = droneClock ?? throw new ArgumentNullException(nameof(droneClock));
            settings = analysisSettings
                ? analysisSettings
                : throw new ArgumentNullException(nameof(analysisSettings));

            if (dialogueTemplates == null)
            {
                return;
            }

            foreach (var template in dialogueTemplates)
            {
                if (template != null && !string.IsNullOrEmpty(template.Id))
                {
                    templates[template.Id] = template;
                }
            }
        }

        public DroneDialogueResult Generate(DroneAnalysisResult analysis)
        {
            return Generate(analysis, false);
        }

        /// <summary>클라우드가 실패한 명시적 요청에서는 기존 표시 쿨다운과 무관하게 같은 분석의 대사를 즉시 만든다.</summary>
        public DroneDialogueResult Generate(DroneAnalysisResult analysis, bool ignoreCooldown)
        {
            if (analysis?.Dialogue == null)
            {
                return Fallback(string.Empty);
            }

            var request = analysis.Dialogue;
            var cooldown = request.IsUrgent
                ? settings.UrgentDialogueRepeatSeconds
                : settings.RegularDialogueCooldownSeconds;
            if (!ignoreCooldown
                && lastShownAt.TryGetValue(request.TemplateId, out var previous)
                && clock.Now - previous < cooldown)
            {
                return new DroneDialogueResult(
                    request.TemplateId,
                    string.Empty,
                    true,
                    false);
            }

            lastShownAt[request.TemplateId] = clock.Now;
            if (!templates.TryGetValue(request.TemplateId, out var template)
                || template == null
                || string.IsNullOrWhiteSpace(template.Template))
            {
                return Fallback(request.TemplateId);
            }

            var missingToken = false;
            var rendered = TokenPattern.Replace(template.Template, match =>
            {
                var key = match.Groups[1].Value;
                if (request.Tokens.TryGetValue(key, out var value)
                    && !string.IsNullOrEmpty(value))
                {
                    return value;
                }

                missingToken = true;
                return string.Empty;
            });

            return missingToken || string.IsNullOrWhiteSpace(rendered)
                ? Fallback(request.TemplateId)
                : new DroneDialogueResult(
                    request.TemplateId,
                    rendered,
                    false,
                    false);
        }

        public IReadOnlyList<DroneDialogueCooldownState> CaptureCooldowns()
        {
            var result = new List<DroneDialogueCooldownState>(lastShownAt.Count);
            foreach (var pair in lastShownAt)
            {
                result.Add(new DroneDialogueCooldownState(pair.Key, pair.Value));
            }

            result.Sort(
                (left, right) => string.CompareOrdinal(left.TemplateId, right.TemplateId));
            return result;
        }

        public bool TryRestoreCooldowns(
            IReadOnlyList<DroneDialogueCooldownState> restored)
        {
            if (restored == null)
            {
                return false;
            }

            var next = new Dictionary<string, double>(StringComparer.Ordinal);
            for (var i = 0; i < restored.Count; i++)
            {
                var entry = restored[i];
                if (string.IsNullOrEmpty(entry.TemplateId)
                    || double.IsNaN(entry.LastShownAt)
                    || double.IsInfinity(entry.LastShownAt)
                    || next.ContainsKey(entry.TemplateId))
                {
                    return false;
                }

                next.Add(entry.TemplateId, entry.LastShownAt);
            }

            lastShownAt.Clear();
            foreach (var pair in next)
            {
                lastShownAt.Add(pair.Key, pair.Value);
            }

            return true;
        }

        private static DroneDialogueResult Fallback(string templateId)
        {
            return new DroneDialogueResult(
                templateId,
                "현재 상태를 확인하고 안전한 위치에서 다시 분석하세요.",
                false,
                true);
        }
    }

    public readonly struct DroneDialogueCooldownState
    {
        public string TemplateId { get; }
        public double LastShownAt { get; }

        public DroneDialogueCooldownState(string templateId, double lastShownAt)
        {
            TemplateId = templateId ?? string.Empty;
            LastShownAt = lastShownAt;
        }
    }
}
