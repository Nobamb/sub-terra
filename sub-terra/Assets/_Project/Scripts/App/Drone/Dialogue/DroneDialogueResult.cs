namespace SubTerra.App.Drone.Dialogue
{
    public sealed class DroneDialogueResult
    {
        public string TemplateId { get; }
        public string Text { get; }
        public bool IsSuppressed { get; }
        public bool UsedFallback { get; }
        public bool IsUrgent { get; }

        public DroneDialogueResult(
            string templateId,
            string text,
            bool isSuppressed,
            bool usedFallback,
            bool isUrgent = false)
        {
            TemplateId = templateId ?? string.Empty;
            Text = text ?? string.Empty;
            IsSuppressed = isSuppressed;
            UsedFallback = usedFallback;
            IsUrgent = isUrgent;
        }
    }
}
