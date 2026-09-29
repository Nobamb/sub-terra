using System.Collections.Generic;
using SubTerra.Shared;
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
        Unlimited = 5,
        Fps165 = 6,
        Fps180 = 7,
        Fps240 = 8
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
        /// <summary>참이면 실행 시점의 모니터 해상도를 쓴다. 가로·세로는 그때 읽는다.</summary>
        public bool ResolutionAutomatic { get; set; }
        /// <summary>언어 코드. 기본 "ko", 영어 준비 "en".</summary>
        public string LanguageCode { get; set; }
        /// <summary>프레임 모드. 기본 Auto(모니터 주사율).</summary>
        public FrameRateMode FrameRate { get; set; }
        public ControlScheme Controls { get; set; }

        public static SettingsValues CreateDefaults()
        {
            return new SettingsValues
            {
                MasterVolume = 0.5f,
                ReduceMotion = false,
                ResolutionWidth = 1920,
                ResolutionHeight = 1080,
                ResolutionAutomatic = true,
                LanguageCode = GameLanguageCodes.Korean,
                FrameRate = FrameRateMode.Auto,
                Controls = ControlScheme.Classic
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
                ResolutionAutomatic = ResolutionAutomatic,
                LanguageCode = LanguageCode,
                FrameRate = FrameRate,
                Controls = Controls
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
            ResolutionAutomatic = other.ResolutionAutomatic;
            LanguageCode = other.LanguageCode;
            FrameRate = other.FrameRate;
            Controls = other.Controls;
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
            FrameRateMode.Fps165,
            FrameRateMode.Fps180,
            FrameRateMode.Fps240,
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

        /// <summary>PlayerPrefs에 넣는 값. 표시 순서가 아니라 열거형 번호라 기존 저장(0~5)이 유지된다.</summary>
        public static int ToSavedValue(FrameRateMode mode)
        {
            return (int)mode;
        }

        public static FrameRateMode FromSavedValue(int saved)
        {
            switch (saved)
            {
                case (int)FrameRateMode.Fps30:
                    return FrameRateMode.Fps30;
                case (int)FrameRateMode.Fps60:
                    return FrameRateMode.Fps60;
                case (int)FrameRateMode.Fps120:
                    return FrameRateMode.Fps120;
                case (int)FrameRateMode.Fps144:
                    return FrameRateMode.Fps144;
                case (int)FrameRateMode.Unlimited:
                    return FrameRateMode.Unlimited;
                case (int)FrameRateMode.Fps165:
                    return FrameRateMode.Fps165;
                case (int)FrameRateMode.Fps180:
                    return FrameRateMode.Fps180;
                case (int)FrameRateMode.Fps240:
                    return FrameRateMode.Fps240;
                default:
                    return FrameRateMode.Auto;
            }
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
                case FrameRateMode.Fps165:
                    return 165;
                case FrameRateMode.Fps180:
                    return 180;
                case FrameRateMode.Fps240:
                    return 240;
                case FrameRateMode.Unlimited:
                    return -1;
                default:
                    // Auto: VSync로 모니터 주사율에 맞춤.
                    return -1;
            }
        }

        public static System.Collections.Generic.List<string> BuildOptionLabels()
        {
            var list = new System.Collections.Generic.List<string>(All.Length);
            for (var i = 0; i < All.Length; i++)
            {
                list.Add(LocalizationService.FormatFrameRateOption(i));
            }

            return list;
        }

        public static bool UsesVSync(FrameRateMode mode)
        {
            return mode == FrameRateMode.Auto;
        }
    }

    /// <summary>선택 가능한 해상도 프리셋. 0번은 모니터 해상도.</summary>
    public static class ResolutionPresets
    {
        public const int AutomaticIndex = 0;

        public static readonly IReadOnlyList<(int width, int height)> All =
            new List<(int, int)>
            {
                (0, 0),
                (960, 540),
                (1024, 768),
                (800, 600),
                (1280, 720),
                (1600, 900),
                (1920, 1080),
                (2560, 1440),
                (3840, 2160),
                (2560, 1080),
                (3440, 1440)
            };

        public static bool IsAutomaticIndex(int index)
        {
            return index == AutomaticIndex;
        }

        public static int IndexFor(bool automatic, int width, int height)
        {
            if (automatic)
            {
                return AutomaticIndex;
            }

            return FindIndex(width, height);
        }

        public static int FindIndex(int width, int height)
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (IsAutomaticIndex(i))
                {
                    continue;
                }

                if (All[i].width == width && All[i].height == height)
                {
                    return i;
                }
            }

            return FallbackIndex();
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

        public static (bool automatic, int width, int height) Cycle(
            bool automatic,
            int width,
            int height,
            int delta)
        {
            int index = IndexFor(automatic, width, height);
            int count = All.Count;
            index = (index + delta) % count;
            if (index < 0)
            {
                index += count;
            }

            var preset = All[index];
            return (IsAutomaticIndex(index), preset.width, preset.height);
        }

        /// <summary>자동 플래그가 없는 예전 저장은 가로·세로를 고정 해상도로 읽는다.</summary>
        public static void ApplyStored(
            SettingsValues values,
            bool hasAutomaticFlag,
            bool automatic,
            bool hasSize,
            int width,
            int height)
        {
            if (values == null)
            {
                return;
            }

            if (hasAutomaticFlag)
            {
                values.ResolutionAutomatic = automatic;
            }
            else if (hasSize)
            {
                values.ResolutionAutomatic = false;
            }

            if (!values.ResolutionAutomatic && hasSize && width > 0 && height > 0)
            {
                values.ResolutionWidth = width;
                values.ResolutionHeight = height;
            }
        }

        public static string FormatOption(int index)
        {
            if (IsAutomaticIndex(index))
            {
                return LocalizationService.Get("settings.resolution.auto", "자동(기본값)");
            }

            var preset = Get(index);
            return LocalizationService.FormatResolutionOption(preset.width, preset.height);
        }

        private static int FallbackIndex()
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i].width == 1920 && All[i].height == 1080)
                {
                    return i;
                }
            }

            return AutomaticIndex;
        }

        public static System.Collections.Generic.List<string> BuildOptionLabels()
        {
            var list = new System.Collections.Generic.List<string>(All.Count);
            for (var i = 0; i < All.Count; i++)
            {
                list.Add(FormatOption(i));
            }

            return list;
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
            // 세션 초안이 1번으로 남아 있어도, 이미 저장한 2·3번을 다시 보여 준다.
            draft.Controls = SettingsRuntimeApplier.LoadControlScheme();
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
