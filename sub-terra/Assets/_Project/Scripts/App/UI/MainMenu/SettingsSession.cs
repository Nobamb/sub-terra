namespace SubTerra.App.UI.MainMenu
{
    /// <summary>MVP 설정 값. 키 리바인딩·언어팩 등 비MVP 항목은 두지 않는다.</summary>
    public sealed class SettingsValues
    {
        public float MasterVolume { get; set; }
        public int ResolutionWidth { get; set; }
        public int ResolutionHeight { get; set; }

        public static SettingsValues CreateDefaults()
        {
            return new SettingsValues
            {
                MasterVolume = 1f,
                ResolutionWidth = 1920,
                ResolutionHeight = 1080
            };
        }

        public SettingsValues Clone()
        {
            return new SettingsValues
            {
                MasterVolume = MasterVolume,
                ResolutionWidth = ResolutionWidth,
                ResolutionHeight = ResolutionHeight
            };
        }

        public void CopyFrom(SettingsValues other)
        {
            if (other == null)
            {
                return;
            }

            MasterVolume = other.MasterVolume;
            ResolutionWidth = other.ResolutionWidth;
            ResolutionHeight = other.ResolutionHeight;
        }
    }

    /// <summary>
    /// 설정 초안/적용 분리. Apply 전까지 런타임에 반영하지 않으며 Cancel은 초안만 되돌린다.
    /// </summary>
    public sealed class SettingsSession
    {
        private readonly SettingsValues applied;
        private readonly SettingsValues draft;

        public SettingsValues Applied => applied;
        public SettingsValues Draft => draft;
        public bool IsOpen { get; private set; }

        public SettingsSession(SettingsValues initial = null)
        {
            applied = (initial ?? SettingsValues.CreateDefaults()).Clone();
            draft = applied.Clone();
        }

        public void Open()
        {
            draft.CopyFrom(applied);
            IsOpen = true;
        }

        public void Apply()
        {
            applied.CopyFrom(draft);
            IsOpen = false;
        }

        public void Cancel()
        {
            draft.CopyFrom(applied);
            IsOpen = false;
        }

        public void ResetDefaults()
        {
            draft.CopyFrom(SettingsValues.CreateDefaults());
        }
    }
}
