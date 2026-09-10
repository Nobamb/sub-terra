namespace SubTerra.Shared
{
    public enum ControlScheme
    {
        Classic = 0,
        WasdMove = 1,
        ArrowsMove = 2
    }

    /// <summary>설정 UI와 Gameplay가 공유하는 키 조작 선택값.</summary>
    public static class ControlPreferences
    {
        public static ControlScheme Scheme { get; set; }
        public static bool IsSettingsOpen { get; set; }

        public static ControlScheme FromIndex(int index)
        {
            return index >= 0 && index < 3 ? (ControlScheme)index : ControlScheme.Classic;
        }

        public static ControlScheme Cycle(ControlScheme scheme, int delta)
        {
            return FromIndex((((int)scheme + delta) % 3 + 3) % 3);
        }
    }
}
