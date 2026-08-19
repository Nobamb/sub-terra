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

        /// <summary>해상도 드롭다운 옵션 라벨 (예: 1920 x 1080).</summary>
        public static string FormatResolutionOption(int width, int height)
        {
            return width + " x " + height;
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
            Add("economy.sell.title", "광물 판매", "Sell Minerals");
            Add("economy.sell.owned", "보유 {0}", "Owned {0}");
            Add("economy.sell.unit_price", "단가 {0}G", "Unit {0}G");
            Add("economy.sell.preview", "예상 골드 +{0}", "Preview +{0}G");
            Add("economy.sell.selected", "선택 판매", "Sell Selected");
            Add("economy.sell.all", "전체 판매 · +{0}G", "Sell All · +{0}G");
            Add("economy.sell.empty", "판매할 광물이 없습니다. 탐사 후 귀환하세요.", "No minerals to sell. Return from exploration.");
            Add("economy.sell.denied", "Surface Base에서만 판매할 수 있습니다.", "Sell only at Surface Base.");
            Add("economy.sell.qty_max", "최대", "Max");
            Add("economy.sell.partial", "부분 판매: {0}/{1} 성공 · +{2}G", "Partial sell: {0}/{1} ok · +{2}G");
            Add("economy.sell.all_ok", "{0}종 판매 · +{1}G", "Sold {0} kinds · +{1}G");
            Add("economy.sell.credits", "골드 {0}", "Credits {0}");
            Add("mine_reset.button", "새 광산 초기화 (500G)", "New Mine (500G)");
            Add("mine_reset.confirm.title", "새 광산 구역", "New Mine Area");
            Add(
                "mine_reset.confirm.body",
                "이용료 500G를 내고 지하를 새로 배치합니다.\n캔 타일, 지하 시설, 붕괴와 가스 상태가 사라집니다.\n업그레이드, 심층 해금, 보유 광물, 남은 골드는 유지됩니다.\n현재 골드 {0} → {1}",
                "Pay 500G to lay out a new underground area.\nMined tiles, underground structures, collapses, and gas states will be removed.\nUpgrades, deep-zone access, minerals, and remaining gold are kept.\nCurrent gold {0} → {1}");
            Add("mine_reset.confirm.yes", "확인", "Confirm");
            Add("mine_reset.confirm.no", "취소", "Cancel");
            Add("mine_reset.success", "새 광산이 배치되었습니다. 이용료 500G.", "New mine laid out. Fee 500G.");
            Add("mine_reset.fail.gold", "골드가 부족합니다. 500G 필요 (보유 {0}G).", "Not enough gold. Need 500G (have {0}G).");
            Add("mine_reset.fail.busy", "지금은 새 광산을 열 수 없습니다.", "Cannot open a new mine right now.");
            Add("mine_reset.fail.surface", "지상 기지에서만 새 광산을 열 수 있습니다.", "New mines can only be opened at Surface Base.");
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
