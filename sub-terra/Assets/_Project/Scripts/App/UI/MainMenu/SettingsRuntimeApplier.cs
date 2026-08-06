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
        private const string PrefResWidth = "subterra.settings.resWidth";
        private const string PrefResHeight = "subterra.settings.resHeight";
        private const string PrefLanguage = "subterra.settings.language";

        public static SettingsValues LoadOrDefaults()
        {
            var values = SettingsValues.CreateDefaults();
            if (PlayerPrefs.HasKey(PrefMasterVolume))
            {
                values.MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMasterVolume, 1f));
            }

            if (PlayerPrefs.HasKey(PrefReduceMotion))
            {
                values.ReduceMotion = PlayerPrefs.GetInt(PrefReduceMotion, 0) != 0;
            }

            if (PlayerPrefs.HasKey(PrefResWidth) && PlayerPrefs.HasKey(PrefResHeight))
            {
                values.ResolutionWidth = PlayerPrefs.GetInt(PrefResWidth, 1920);
                values.ResolutionHeight = PlayerPrefs.GetInt(PrefResHeight, 1080);
            }

            if (PlayerPrefs.HasKey(PrefLanguage))
            {
                values.LanguageCode = PlayerPrefs.GetString(PrefLanguage, GameLanguageCodes.Korean);
            }

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
            PlayerPrefs.SetInt(PrefResWidth, values.ResolutionWidth);
            PlayerPrefs.SetInt(PrefResHeight, values.ResolutionHeight);
            PlayerPrefs.SetString(
                PrefLanguage,
                string.IsNullOrEmpty(values.LanguageCode)
                    ? GameLanguageCodes.Korean
                    : values.LanguageCode);
            PlayerPrefs.Save();
        }

        /// <summary>슬라이더 드래그 중 즉시 음량만 미리듣기.</summary>
        public static void PreviewMasterVolume(float volume01)
        {
            AudioListener.volume = Mathf.Clamp01(volume01);
        }

        /// <summary>적용 확정. 음량·접근성·언어·해상도를 반영한다.</summary>
        public static void Apply(SettingsValues values, bool applyResolution)
        {
            if (values == null)
            {
                return;
            }

            AudioListener.volume = Mathf.Clamp01(values.MasterVolume);
            AccessibilityPreferences.ReduceMotion = values.ReduceMotion;
            LocalizationService.SetLanguageCode(
                string.IsNullOrEmpty(values.LanguageCode)
                    ? GameLanguageCodes.Korean
                    : values.LanguageCode);

            if (applyResolution
                && values.ResolutionWidth > 0
                && values.ResolutionHeight > 0)
            {
                // Editor Game 뷰 해상도는 건드리지 않는다. 플레이어 빌드에서만 실제 변경.
                if (!Application.isEditor)
                {
                    Screen.SetResolution(
                        values.ResolutionWidth,
                        values.ResolutionHeight,
                        Screen.fullScreenMode);
                }
            }

            Save(values);
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
