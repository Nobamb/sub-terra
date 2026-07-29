using System;

namespace SubTerra.App.AI
{
    public sealed class CloudDialogueOptions
    {
        public bool Enabled { get; }
        public string Endpoint { get; }
        public string Language { get; }
        public int TimeoutMilliseconds { get; }
        public double GlobalCooldownSeconds { get; }
        public double DuplicateEventWindowSeconds { get; }
        public int MaxSessionCalls { get; }
        public int MaxCallsPerEvent { get; }
        public int MaxResponseCharacters { get; }

        public bool CanUseCloud
        {
            get
            {
                if (!Enabled
                    || !Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint))
                {
                    return false;
                }

                return string.Equals(
                    endpoint.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        public CloudDialogueOptions(
            bool enabled,
            string endpoint,
            string language = "ko",
            int timeoutMilliseconds = 1500,
            double globalCooldownSeconds = 2d,
            double duplicateEventWindowSeconds = 10d,
            int maxSessionCalls = 8,
            int maxCallsPerEvent = 2,
            int maxResponseCharacters = 240)
        {
            Enabled = enabled;
            Endpoint = endpoint ?? string.Empty;
            Language = string.IsNullOrWhiteSpace(language) ? "ko" : language;
            TimeoutMilliseconds = Math.Max(1, timeoutMilliseconds);
            GlobalCooldownSeconds = Math.Max(0d, globalCooldownSeconds);
            DuplicateEventWindowSeconds = Math.Max(0d, duplicateEventWindowSeconds);
            MaxSessionCalls = Math.Max(0, maxSessionCalls);
            MaxCallsPerEvent = Math.Max(0, maxCallsPerEvent);
            MaxResponseCharacters = Math.Max(1, maxResponseCharacters);
        }
    }
}
