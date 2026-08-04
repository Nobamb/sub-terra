using System;
using System.Threading;
using System.Threading.Tasks;
using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;
using UnityEngine;

namespace SubTerra.App.AI
{
    /// <summary>
    /// Phase I 분석을 변경하지 않고 자연어 표현만 선택한다. 어떤 실패도 같은 분석의 템플릿 대사로 닫힌다.
    /// </summary>
    public sealed class CloudDialogueGenerator : IDialogueGenerator
    {
        private readonly TemplateDialogueGenerator templateGenerator;
        private readonly IDialogueTransport transport;
        private readonly CloudDialogueOptions options;
        private readonly CloudDialoguePolicy policy;

        public CloudDialogueGenerator(
            TemplateDialogueGenerator fallbackGenerator,
            IDialogueTransport dialogueTransport,
            CloudDialogueOptions dialogueOptions,
            CloudDialoguePolicy dialoguePolicy)
        {
            templateGenerator = fallbackGenerator
                ?? throw new ArgumentNullException(nameof(fallbackGenerator));
            transport = dialogueTransport
                ?? throw new ArgumentNullException(nameof(dialogueTransport));
            options = dialogueOptions
                ?? throw new ArgumentNullException(nameof(dialogueOptions));
            policy = dialoguePolicy
                ?? throw new ArgumentNullException(nameof(dialoguePolicy));
        }

        public async Task<DialogueGenerationResult> GenerateAsync(
            DroneAnalysisResult analysis,
            CloudDialogueEvent eventType,
            CancellationToken cancellationToken)
        {
            if (analysis == null || !options.CanUseCloud)
            {
                return Template(analysis);
            }

            if (!policy.TryBegin(eventType, out var lease))
            {
                return Template(analysis);
            }

            using (lease)
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeout.CancelAfter(options.TimeoutMilliseconds);
                try
                {
                    var request = CloudDialogueRequestDto.FromAnalysis(
                        analysis,
                        options.Language);
                    var transportResult = await transport.SendAsync(
                        options.Endpoint,
                        JsonUtility.ToJson(request),
                        options.TimeoutMilliseconds,
                        timeout.Token);
                    if (!transportResult.Succeeded
                        || transportResult.StatusCode < 200
                        || transportResult.StatusCode >= 300
                        || !TryReadDialogue(transportResult.ResponseBody, out var dialogue))
                    {
                        return Template(analysis);
                    }

                    return new DialogueGenerationResult(
                        analysis,
                        new DroneDialogueResult(
                            analysis.Dialogue.TemplateId,
                            dialogue,
                            false,
                            false,
                            analysis.Dialogue.IsUrgent),
                        true);
                }
                catch (OperationCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return new DialogueGenerationResult(
                            analysis,
                            new DroneDialogueResult(
                                analysis.Dialogue.TemplateId,
                                string.Empty,
                                true,
                                false,
                                analysis.Dialogue.IsUrgent),
                            false,
                            true);
                    }

                    return Template(analysis);
                }
                catch
                {
                    return Template(analysis);
                }
            }
        }

        private bool TryReadDialogue(string responseBody, out string dialogue)
        {
            dialogue = string.Empty;
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return false;
            }

            CloudDialogueResponseDto response;
            try
            {
                response = JsonUtility.FromJson<CloudDialogueResponseDto>(responseBody);
            }
            catch
            {
                return false;
            }

            if (response == null || string.IsNullOrWhiteSpace(response.dialogue))
            {
                return false;
            }

            var candidate = response.dialogue.Trim();
            if (candidate.Length > options.MaxResponseCharacters)
            {
                return false;
            }

            for (var i = 0; i < candidate.Length; i++)
            {
                var character = candidate[i];
                if ((char.IsControl(character) && !char.IsWhiteSpace(character))
                    || IsMarkupCharacter(character))
                {
                    return false;
                }
            }

            dialogue = candidate;
            return true;
        }

        private static bool IsMarkupCharacter(char character)
        {
            return character == '<'
                || character == '>'
                || character == '{'
                || character == '}'
                || character == '`'
                || character == '*'
                || character == '_'
                || character == '['
                || character == ']';
        }

        private DialogueGenerationResult Template(DroneAnalysisResult analysis)
        {
            return new DialogueGenerationResult(
                analysis,
                templateGenerator.Generate(analysis, true),
                false);
        }
    }
}
