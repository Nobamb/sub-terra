using System.Collections.Generic;
using SubTerra.Shared.Localization;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// 프레임 제한 모드. Auto는 모니터 주사율(VSync), Unlimited는 제한 없음.
    /// </summary>
    public enum FrameRateMode
    {
        Auto = 0,
        Fps30 = 1,
        Fps60 = 2,
        Fps120 = 3,
        Fps144 = 4,
        Unlimited = 5
    }

    /// <summary>
    /// MVP 설정 값.
    /// 마스터 음량(향후 4종 BGM 공통), 해상도, 화면 진동 억제, 언어, 프레임을 포함한다.
    /// </summary>
    public sealed class SettingsValues
    {
        public float MasterVolume { get; set; }
        public bool ReduceMotion { get; set; }
        public int ResolutionWidth { get; set; }
        public int ResolutionHeight { get; set; }
        /// <summary>언어 코드. 기본 "ko", 영어 준비 "en".</summary>
        public string LanguageCode { get; set; }
        /// <summary>프레임 모드. 기본 Auto(모니터 주사율).</summary>
        public FrameRateMode FrameRate { get; set; }

        public static SettingsValues CreateDefaults()
        {
            return new SettingsValues
            {
                MasterVolume = 1f,
                ReduceMotion = false,
                ResolutionWidth = 1920,
                ResolutionHeight = 1080,
                LanguageCode = GameLanguageCodes.Korean,
                FrameRate = FrameRateMode.Auto
            };
        }

        public SettingsValues Clone()
        {
            return new SettingsValues
            {
                MasterVolume = MasterVolume,
                ReduceMotion = ReduceMotion,
                ResolutionWidth = ResolutionWidth,
                ResolutionHeight = ResolutionHeight,
                LanguageCode = LanguageCode,
                FrameRate = FrameRate
            };
        }

        public void CopyFrom(SettingsValues other)
        {
            if (other == null)
            {
                return;
            }

            MasterVolume = other.MasterVolume;
            ReduceMotion = other.ReduceMotion;
            ResolutionWidth = other.ResolutionWidth;
            ResolutionHeight = other.ResolutionHeight;
            LanguageCode = other.LanguageCode;
            FrameRate = other.FrameRate;
        }
    }

    /// <summary>프레임 모드 인덱스·표시 이름 헬퍼.</summary>
    public static class FrameRatePresets
    {
        public static readonly FrameRateMode[] All =
        {
            FrameRateMode.Auto,
            FrameRateMode.Fps30,
            FrameRateMode.Fps60,
            FrameRateMode.Fps120,
            FrameRateMode.Fps144,
            FrameRateMode.Unlimited
        };

        public static int ToIndex(FrameRateMode mode)
        {
            for (var i = 0; i < All.Length; i++)
            {
                if (All[i] == mode)
                {
                    return i;
                }
            }

            return 0;
        }

        public static FrameRateMode FromIndex(int index)
        {
            if (index < 0 || index >= All.Length)
            {
                return FrameRateMode.Auto;
            }

            return All[index];
        }

        public static int ToTargetFrameRate(FrameRateMode mode)
        {
            switch (mode)
            {
                case FrameRateMode.Fps30:
                    return 30;
                case FrameRateMode.Fps60:
                    return 60;
                case FrameRateMode.Fps120:
                    return 120;
                case FrameRateMode.Fps144:
                    return 144;
                case FrameRateMode.Unlimited:
                    return -1;
                default:
                    // Auto: VSync로 모니터 주사율에 맞춤.
                    return -1;
            }
        }

        public static bool UsesVSync(FrameRateMode mode)
        {
            return mode == FrameRateMode.Auto;
        }
    }

    /// <summary>선택 가능한 해상도 프리셋.</summary>
    public static class ResolutionPresets
    {
        public static readonly IReadOnlyList<(int width, int height)> All =
            new List<(int, int)>
            {
                (1280, 720),
                (1600, 900),
                (1920, 1080),
                (2560, 1440)
            };

        public static int FindIndex(int width, int height)
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i].width == width && All[i].height == height)
                {
                    return i;
                }
            }

            return 2; // 1920x1080 기본
        }

        public static (int width, int height) Get(int index)
        {
            if (index < 0)
            {
                index = 0;
            }

            if (index >= All.Count)
            {
                index = All.Count - 1;
            }

            return All[index];
        }

        public static (int width, int height) Cycle(int width, int height, int delta)
        {
            int index = FindIndex(width, height);
            int count = All.Count;
            index = (index + delta) % count;
            if (index < 0)
            {
                index += count;
            }

            return All[index];
        }
    }

    /// <summary>
    /// 설정 초안/적용 분리. Apply 전까지 런타임에 확정 반영하지 않으며 Cancel은 초안만 되돌린다.
    /// 음량 슬라이더는 미리듣기용으로 즉시 반영할 수 있다.
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
