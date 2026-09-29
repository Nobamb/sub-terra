using SubTerra.Shared;
using SubTerra.Shared.Localization;
using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// 설정 값을 AudioListener / 접근성 / 해상도 / 언어에 적용하고 PlayerPrefs에 저장한다.
    /// Main Menu·Surface Base가 동일 경로를 쓰도록 한곳에 모은다.
    /// </summary>
    public static class SettingsRuntimeApplier
    {
        private const string PrefMasterVolume = "subterra.settings.masterVolume";
        private const string PrefReduceMotion = "subterra.settings.reduceMotion";
        private const string PrefResAuto = "subterra.settings.resAuto";
        private const string PrefResWidth = "subterra.settings.resWidth";
        private const string PrefResHeight = "subterra.settings.resHeight";
        private const string PrefLanguage = "subterra.settings.language";
        private const string PrefFrameRate = "subterra.settings.frameRate";
        public const string PrefControls = "subterra.settings.controls";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyPersistedControlsOnLoad()
        {
            ApplyPersistedControlScheme();
        }

        /// <summary>저장된 조작 방식을 Gameplay가 읽기 전에 복원한다.</summary>
        public static void ApplyPersistedControlScheme()
        {
            ControlPreferences.Scheme = LoadControlScheme();
            ControlPreferences.IsSettingsOpen = false;
        }

        public static ControlScheme LoadControlScheme()
        {
            return ControlPreferences.FromIndex(PlayerPrefs.GetInt(PrefControls, 0));
        }

        /// <summary>키 조작 방식만 즉시 저장한다. 설정 창 적용을 기다리지 않는다.</summary>
        public static void SaveControlScheme(ControlScheme scheme)
        {
            scheme = ControlPreferences.FromIndex((int)scheme);
            ControlPreferences.Scheme = scheme;
            PlayerPrefs.SetInt(PrefControls, (int)scheme);
            PlayerPrefs.Save();
        }

        public static SettingsValues LoadOrDefaults()
        {
            var values = SettingsValues.CreateDefaults();
            if (PlayerPrefs.HasKey(PrefMasterVolume))
            {
                values.MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMasterVolume, 0.5f));
            }

            if (PlayerPrefs.HasKey(PrefReduceMotion))
            {
                values.ReduceMotion = PlayerPrefs.GetInt(PrefReduceMotion, 0) != 0;
            }

            ResolutionPresets.ApplyStored(
                values,
                PlayerPrefs.HasKey(PrefResAuto),
                PlayerPrefs.GetInt(PrefResAuto, 1) != 0,
                PlayerPrefs.HasKey(PrefResWidth) && PlayerPrefs.HasKey(PrefResHeight),
                PlayerPrefs.GetInt(PrefResWidth, 1920),
                PlayerPrefs.GetInt(PrefResHeight, 1080));

            if (PlayerPrefs.HasKey(PrefLanguage))
            {
                values.LanguageCode = PlayerPrefs.GetString(PrefLanguage, GameLanguageCodes.Korean);
            }

            if (PlayerPrefs.HasKey(PrefFrameRate))
            {
                values.FrameRate = FrameRatePresets.FromSavedValue(
                    PlayerPrefs.GetInt(PrefFrameRate, 0));
            }

            values.Controls = LoadControlScheme();
            return values;
        }

        public static void Save(SettingsValues values)
        {
            if (values == null)
            {
                return;
            }

            PlayerPrefs.SetFloat(PrefMasterVolume, Mathf.Clamp01(values.MasterVolume));
            PlayerPrefs.SetInt(PrefReduceMotion, values.ReduceMotion ? 1 : 0);
            PlayerPrefs.SetInt(PrefResAuto, values.ResolutionAutomatic ? 1 : 0);
            PlayerPrefs.SetInt(PrefResWidth, values.ResolutionWidth);
            PlayerPrefs.SetInt(PrefResHeight, values.ResolutionHeight);
            PlayerPrefs.SetString(
                PrefLanguage,
                string.IsNullOrEmpty(values.LanguageCode)
                    ? GameLanguageCodes.Korean
                    : values.LanguageCode);
            PlayerPrefs.SetInt(PrefFrameRate, FrameRatePresets.ToSavedValue(values.FrameRate));
            SaveControlScheme(values.Controls);
        }

        /// <summary>슬라이더 드래그 중 즉시 음량만 미리듣기.</summary>
        public static void PreviewMasterVolume(float volume01)
        {
            AudioListener.volume = Mathf.Clamp01(volume01);
        }

        /// <summary>적용 확정. 음량·접근성·언어·해상도·프레임을 반영한다.</summary>
        public static void Apply(SettingsValues values, bool applyResolution)
        {
            if (values == null)
            {
                return;
            }

            AudioListener.volume = Mathf.Clamp01(values.MasterVolume);
            AccessibilityPreferences.ReduceMotion = values.ReduceMotion;
            SaveControlScheme(values.Controls);
            LocalizationService.SetLanguageCode(
                string.IsNullOrEmpty(values.LanguageCode)
                    ? GameLanguageCodes.Korean
                    : values.LanguageCode);

            ApplyFrameRate(values.FrameRate);

            // 에디터 Game 뷰 해상도는 바꾸지 않는다. 플레이어 빌드에서만 적용한다.
            if (applyResolution && !Application.isEditor)
            {
                int nativeWidth;
                int nativeHeight;
                ReadNativeResolution(out nativeWidth, out nativeHeight);
                int width;
                int height;
                ResolveAppliedResolution(values, nativeWidth, nativeHeight, out width, out height);
                if (width > 0 && height > 0)
                {
                    Screen.SetResolution(width, height, Screen.fullScreenMode);
                }
            }

            Save(values);
        }

        /// <summary>자동이면 모니터 크기, 아니면 저장한 고정 크기를 고른다.</summary>
        public static void ResolveAppliedResolution(
            SettingsValues values,
            int nativeWidth,
            int nativeHeight,
            out int width,
            out int height)
        {
            if (values != null && values.ResolutionAutomatic && nativeWidth > 0 && nativeHeight > 0)
            {
                width = nativeWidth;
                height = nativeHeight;
                return;
            }

            if (values != null
                && !values.ResolutionAutomatic
                && values.ResolutionWidth > 0
                && values.ResolutionHeight > 0)
            {
                width = values.ResolutionWidth;
                height = values.ResolutionHeight;
                return;
            }

            width = 1920;
            height = 1080;
        }

        private static void ReadNativeResolution(out int width, out int height)
        {
            width = 0;
            height = 0;
            var display = Display.main;
            if (display != null)
            {
                width = display.systemWidth;
                height = display.systemHeight;
            }

            if (width > 0 && height > 0)
            {
                return;
            }

            var current = Screen.currentResolution;
            width = current.width;
            height = current.height;
        }

        /// <summary>
        /// prompt-B 33-2: Auto=VSync(모니터 주사율), 고정 FPS, Unlimited=제한 없음.
        /// </summary>
        public static void ApplyFrameRate(FrameRateMode mode)
        {
            if (FrameRatePresets.UsesVSync(mode))
            {
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = -1;
                return;
            }

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = FrameRatePresets.ToTargetFrameRate(mode);
        }

        public static void RestoreAppliedVolume(SettingsValues applied)
        {
            if (applied == null)
            {
                return;
            }

            AudioListener.volume = Mathf.Clamp01(applied.MasterVolume);
        }
    }
}
