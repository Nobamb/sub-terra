using System;
using System.Collections.Generic;

namespace SubTerra.Shared.Localization
{
    /// <summary>
    /// 간단 키-기반 로컬라이제이션.
    /// 현재 한국어 본문을 채우고, 동일 키에 영어를 추가할 수 있도록 구조를 둔다.
    /// </summary>
    public static class LocalizationService
    {
        private static GameLanguage current = GameLanguage.Korean;
        private static readonly Dictionary<string, string> Korean = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal);
        private static bool tablesReady;

        public static GameLanguage Current
        {
            get
            {
                EnsureTables();
                return current;
            }
        }

        public static event Action LanguageChanged;

        public static void SetLanguage(GameLanguage language)
        {
            EnsureTables();
            if (current == language)
            {
                return;
            }

            current = language;
            LanguageChanged?.Invoke();
        }

        public static void SetLanguageCode(string code)
        {
            SetLanguage(GameLanguageCodes.FromCode(code));
        }

        public static string Get(string key, string fallback = null)
        {
            EnsureTables();
            if (string.IsNullOrEmpty(key))
            {
                return fallback ?? string.Empty;
            }

            Dictionary<string, string> primary = current == GameLanguage.English ? English : Korean;
            if (primary.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            // 영어 미번역 키는 한국어로 폴백한다.
            if (Korean.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            return fallback ?? key;
        }

        public static string FormatMasterVolume(float volume01)
        {
            int percent = (int)Math.Round(Math.Max(0f, Math.Min(1f, volume01)) * 100f);
            return Get("settings.master_volume", "마스터 음량") + ": " + percent + "%";
        }

        public static string FormatResolution(int width, int height)
        {
            return Get("settings.resolution", "해상도") + ": " + width + " x " + height;
        }

        public static string FormatLanguage(GameLanguage language)
        {
            return language == GameLanguage.English
                ? Get("settings.language.en", "English")
                : Get("settings.language.ko", "한국어");
        }

        private static void EnsureTables()
        {
            if (tablesReady)
            {
                return;
            }

            tablesReady = true;
            Add("settings.title", "설정", "Settings");
            Add("settings.master_volume", "마스터 음량", "Master Volume");
            Add("settings.resolution", "해상도", "Resolution");
            Add("settings.reduce_motion", "화면 진동 억제", "Suppress Screen Shake");
            Add("settings.language", "언어", "Language");
            Add("settings.language.ko", "한국어", "Korean");
            Add("settings.language.en", "English", "English");
            Add("settings.frame_rate", "프레임", "Frame Rate");
            Add("settings.frame.auto", "자동(기본값)", "Auto (Default)");
            Add("settings.frame.30", "30", "30");
            Add("settings.frame.60", "60", "60");
            Add("settings.frame.120", "120", "120");
            Add("settings.frame.144", "144", "144");
            Add("settings.frame.unlimited", "제한없음", "Unlimited");
            Add("settings.apply", "적용", "Apply");
            Add("settings.cancel", "취소", "Cancel");
            Add("settings.defaults", "기본값", "Defaults");
            Add("settings.bgm_hint", "BGM 4종(타이틀/기지/탐사/위험)은 마스터 음량으로 조절됩니다.", "Four BGMs (title/base/mine/danger) follow master volume.");
        }

        public static string FormatFrameRateOption(int optionIndex)
        {
            switch (optionIndex)
            {
                case 1:
                    return Get("settings.frame.30", "30");
                case 2:
                    return Get("settings.frame.60", "60");
                case 3:
                    return Get("settings.frame.120", "120");
                case 4:
                    return Get("settings.frame.144", "144");
                case 5:
                    return Get("settings.frame.unlimited", "제한없음");
                default:
                    return Get("settings.frame.auto", "자동(기본값)");
            }
        }

        private static void Add(string key, string ko, string en)
        {
            Korean[key] = ko;
            English[key] = en;
        }
    }
}
