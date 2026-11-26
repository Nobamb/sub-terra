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
        private static GameLanguage? previewLanguage;
        private static readonly Dictionary<string, string> Korean = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal);
        private static bool tablesReady;

        public static GameLanguage Current
        {
            get
            {
                EnsureTables();
                return previewLanguage ?? current;
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

        /// <summary>Render the settings draft without notifying listeners of an applied language change.</summary>
        public static void PreviewLanguage(string code, Action render)
        {
            var previous = previewLanguage;
            previewLanguage = GameLanguageCodes.FromCode(code);
            try { render(); }
            finally { previewLanguage = previous; }
        }

        public static string Get(string key, string fallback = null)
        {
            EnsureTables();
            if (string.IsNullOrEmpty(key))
            {
                return fallback ?? string.Empty;
            }

            Dictionary<string, string> primary = Current == GameLanguage.English ? English : Korean;
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

        public static string Text(string korean, string english) =>
            Current == GameLanguage.English ? english : korean;

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
            Add("settings.reduce_animation_effects", "애니메이션 연출 감소", "Reduce Animation Effects");
            Add("settings.language", "언어", "Language");
            Add("settings.language.ko", "한국어", "Korean");
            Add("settings.language.en", "English", "English");
            Add("settings.frame_rate", "프레임", "Frame Rate");
            Add("settings.frame.auto", "자동(기본값)", "Auto (Default)");
            Add("settings.frame.30", "30", "30");
            Add("settings.frame.60", "60", "60");
            Add("settings.frame.120", "120", "120");
            Add("settings.frame.144", "144", "144");
            Add("settings.frame.165", "165", "165");
            Add("settings.frame.180", "180", "180");
            Add("settings.frame.240", "240", "240");
            Add("settings.frame.unlimited", "제한없음", "Unlimited");
            Add("settings.resolution.auto", "자동(기본값)", "Auto (Default)");
            Add("settings.apply", "적용", "Apply");
            Add("settings.cancel", "취소", "Cancel");
            Add("settings.defaults", "기본값", "Defaults");
            Add("settings.bgm_volume", "배경음", "Music");
            Add("settings.sfx_volume", "효과음", "Sound Effects");
            Add("settings.bgm_hint", "배경음은 배경음 슬라이더로 조절됩니다.", "Music is adjusted with the Music slider.");

            Add("main_menu.continue", "이어하기", "Continue");
            Add("main_menu.start_expedition", "새 탐사 시작", "Start Expedition");
            Add("main_menu.new_game", "새 게임", "New Game");
            Add("main_menu.settings", "설정(esc)", "Settings (Esc)");
            Add("main_menu.quit", "종료", "Quit");
            Add("main_menu.slot_title", "탐사 기록 {0:00}", "Expedition Log {0:00}");
            Add("main_menu.slot_unknown", "Slot ?", "Slot ?");
            Add("main_menu.slot_empty", "비어 있는 기록", "Empty Log");
            Add("main_menu.slot_start_hint", "새 탐사를 시작하세요.", "Start a new expedition.");
            Add("main_menu.slot_corrupted", "기록 손상 · 이어하기 불가", "Corrupted log · Cannot continue");
            Add("main_menu.slot_gold", "골드", "Gold");
            Add("main_menu.slot_depth", "최대 깊이", "Max Depth");
            Add("main_menu.slot_objectives", "목표", "Goals");
            Add("main_menu.slot_last_save", "마지막 저장", "Last Saved");
            Add("main_menu.signal_missing", "신호 없음", "NO SIGNAL");
            Add("main_menu.signal_corrupted", "신호 손상", "CORRUPTED SIGNAL");
            Add("main_menu.slot_status_empty", "세이브 없음", "No save");
            Add("main_menu.slot_status_ready", "이어하기 가능", "Ready to continue");
            Add("main_menu.slot_status_backup", "백업으로 복구 가능", "Recoverable from backup");
            Add("main_menu.slot_status_corrupted", "세이브 손상 — 복구 불가", "Save corrupted — unrecoverable");
            Add("main_menu.slot_status_invalid", "잘못된 슬롯", "Invalid slot");
            Add("main_menu.selected_slot", "선택 슬롯 {0} — {1}", "Slot {0} selected — {1}");
            Add("main_menu.selected_slot_corrupted", " (이어하기 불가, 새 게임 시 확인 필요)", " (Confirm overwrite to start a new game)");
            Add("main_menu.overwrite_title", "덮어쓰기?", "Overwrite Save?");
            Add("main_menu.overwrite_question", "슬롯 {0} 세이브를 덮어쓰시겠습니까?", "Overwrite the save in slot {0}?");
            Add("main_menu.overwrite_status", "슬롯 {0}에 세이브가 있습니다. 덮어쓸까요?", "Slot {0} contains a save. Overwrite it?");
            Add("main_menu.confirm", "확인", "Confirm");
            Add("main_menu.delete_title", "기록 삭제?", "Delete Log?");
            Add("main_menu.delete", "삭제", "Delete");
            Add("main_menu.delete_question", "{0}을 삭제할까요?", "Delete {0}?");
            Add("main_menu.delete_warning", "되돌릴 수 없습니다.", "This cannot be undone.");
            Add("main_menu.delete_success", "{0}을 삭제했습니다.", "Deleted {0}.");
            Add("main_menu.delete_failed", "{0}을 삭제하지 못했습니다.", "Could not delete {0}.");
            Add("main_menu.start_failed", "새 게임을 시작할 수 없습니다.", "Could not start a new game.");
            Add("main_menu.start_cancelled", "새 게임이 취소되었습니다.", "New game cancelled.");
            Add("main_menu.continue_failed", "이어하기 실패: {0}", "Could not continue: {0}");
            Add("main_menu.quit_blocked", "저장 중입니다. 잠시 후 다시 시도하세요.", "Saving. Try again in a moment.");
            Add("quit_confirm.title", "게임 종료", "Quit Game");
            Add("quit_confirm.question", "게임을 종료할까요?", "Quit the game?");
            Add("quit_confirm.autosave", "진행 상황은 자동으로 저장됩니다.", "Your progress will be saved automatically.");

            English["building.support.basic.description"] = "Reduces fall damage to about 10% of normal. Stand on the pad and press Up or Down to adjust its height.";
            English["building.ladder.basic.description"] = "Climb up and down vertical shafts. Places a five-tile section.";
            English["building.light.basic.description"] = "Lights an underground area when powered.";
            English["building.charger.basic.description"] = "Recharges your equipment when connected to the power grid.";
            English["building.clinic.basic.description"] = "Restores you to full health when connected to the power grid.";
            English["building.storage.basic.description"] = "Deposit and withdraw minerals collected during expeditions.";
            Add("building.settlement.basic.description", "들고 있는 광물을 판매해 골드를 얻습니다.", "Sell carried minerals for gold.");
            English["building.outpost_core.basic.description"] = "Powers connected facilities, creates a gas purification zone and serves as a checkpoint.";
            Add("building.escape_portal.emergency.description",
                "E로 목적지 선택 창을 엽니다. {0}G와 최대 전력의 {1}%를 내고 엘리베이터나 설치한 전진기지로 이동합니다.",
                "Press E to choose a destination. Spend {0}G and {1}% of maximum power to travel to the elevator or a built outpost.");

            English["dialogue.drone.cargo.full"] = "Cargo: {cargoWeight}/{maxCargoWeight}. Your cargo is full. Return to base.";
            English["dialogue.drone.survival.emergency"] = "{reason} Check whether you can take the recommended action: {action}.";
            English["dialogue.drone.explore"] = "Depth: {depth}. No immediate risk detected. You can continue descending.";
            English["dialogue.drone.gas.warning"] = "{safetyGuidance}";
            English["dialogue.drone.lithium"] = "Nearby signal detected: {mineralId}.";
            English["dialogue.drone.outpost"] = "Nearest base: {nearestBaseDistance}m. Consider building an outpost.";
            English["dialogue.drone.return"] = "Unsold cargo value: {unsettledCargoValue}. Return to base.";
            English["dialogue.drone.structural.warning"] = "{safetyGuidance}";
            English["dialogue.low_power.warning"] = "Power: {currentEnergy}. Estimated return cost: {returnEnergyEstimate}. Recommendation: {action}.";

            Add("hud.energy", "전력", "Power");
            Add("hud.health", "체력", "Health");
            Add("hud.oxygen", "산소", "Oxygen");
            Add("hud.oxygen_safe", "산소 위험도 안전", "Oxygen: Safe");
            Add("hud.oxygen_caution", "산소 위험도 주의", "Oxygen: Caution");
            Add("hud.oxygen_critical", "산소 위험도 위험", "Oxygen: Critical");
            Add("hud.cargo", "화물", "Cargo");
            Add("quest.progress", "진행도", "Progress");
            Add("quest.mission_hint", "MISSION · 방법 보기", "MISSION · View Details");
            Add("quest.next_action", "다음 행동: {0}", "Next: {0}");
            Add("quest.cleared", "클리어", "Cleared");
            Add("quest.in_progress", "진행 중", "In Progress");
            Add("quest.incomplete", "미완료", "Incomplete");
            Add("quest.all_goals", "모든 목표 완료", "All Goals Complete");
            Add("quest.claim_title", "퀘스트 클리어", "Quest Complete");
            Add("quest.claim_description", "퀘스트 목표를 달성했습니다.", "You completed the quest objective.");
            Add("quest.claim_hint", "닫으면 보상이 지급됩니다.", "Close this window to receive your reward.");
            Add("quest.reward_header", "클리어 보상", "Completion Rewards");
            Add("quest.reward", "보상", "Reward");
            Add("quest.reward_none", "없음", "None");
            Add("quest.reward_copper", "구리", "Copper");
            Add("quest.reward_iron", "철", "Iron");
            Add("quest.reward_lithium", "리튬", "Lithium");
            Add("quest.reward_gold", "골드", "Gold");
            English["quest.briefing_title"] = "Survivor Briefing";
            English["quest.briefing_body"] = "No resources remain on the surface.\n"
                + "The cultivation facility preserves humanity's genetic information.\n"
                + "Successful genetic engineering could bring humanity back.\n"
                + "The resources we need are now underground.\n\n"
                + "Descend. Mine what you can. Endure, and return alive.";
            Add("quest.briefing_confirm", "작업 시작", "Begin Work");
            English["quest.demo_complete_title"] = "All Demo Objectives Complete";
            English["quest.demo_complete_body"] = "You made it back through the emergency escape portal.";
            // Korean objective text lives in DemoObjectiveCatalog and remains the fallback.
            AddQuestEnglish("mine_block", "Remove a Block", "Press Enter or click to remove one block.", "Aim at a block, then press Enter or click.");
            AddQuestEnglish("mine_copper", "Mine Copper", "Mine a copper block to collect copper.", "Find and mine a copper block.");
            AddQuestEnglish("upgrade_drill_speed", "Upgrade Drill Speed", "Open the upgrade window and upgrade drill speed once.", "Buy a drill speed upgrade.");
            AddQuestEnglish("travel_to_surface", "Travel to the Surface", "Enter the elevator and press the interact key (E) to return to the surface base.", "Press E while inside the elevator.");
            AddQuestEnglish("return_to_mine", "Resume Mining", "Return to the mine, step out of the elevator, and keep mining.", "Move beyond the three dark blocks beneath the elevator.");
            AddQuestEnglish("mine_iron", "Mine Iron", "Mine an iron block to collect iron.", "Find and mine an iron block.");
            AddQuestEnglish("place_support_in_danger", "Install a Cushion Pad", "Place a cushion pad. Landing on it reduces fall damage to about 10%.", "Install one cushion pad.");
            AddQuestEnglish("place_ladder", "Install a Ladder", "Select a ladder in the build menu and place it.", "Place a ladder in a vertical passage.");
            AddQuestEnglish("place_light_at_depth", "Install a Deep-Mine Light", "Descend to at least 10 m and install a light.", "Place a light at a depth of 10 m or more.");
            AddQuestEnglish("store_mineral", "Store Minerals", "Install a storage chest, then store at least one mineral.", "Place a storage chest and deposit a mineral.");
            AddQuestEnglish("install_outpost_core", "Install an Outpost", "Install an outpost to activate a checkpoint.", "Place an outpost from the build menu.");
            AddQuestEnglish("charge_near_outpost", "Recharge at an Outpost", "Place a charger near the outpost you just built, then recharge your power.", "Place a charger within 10 tiles of the outpost and recharge.");
            AddQuestEnglish("heal_near_outpost", "Heal at an Outpost", "Install a clinic near an outpost and restore your health.", "Place a clinic within 10 tiles of the outpost and heal.");
            AddQuestEnglish("unlock_deep_zone", "Unlock the Deep Zone", "Reach Drill Speed Lv. 2, Drone Scan Lv. 2 and Gas Resistance Lv. 1 to unlock the deep zone.", "Buy the required upgrades.");
            AddQuestEnglish("mine_lithium", "Mine Lithium", "Mine a lithium block in the deep zone to collect lithium.", "Mine a deep-zone lithium deposit.");
            AddQuestEnglish("purify_gas_with_outpost", "Purify Gas", "Build an outpost within five tiles of a lithium or gas block before mining it. Approach the released gas while inside the purification area.", "Build an outpost, mine a nearby lithium or gas block, then approach the gas inside the purification area.");
            AddQuestEnglish("sell_at_settlement", "Sell Minerals at an Outpost", "Install a settlement terminal within 10 tiles of the outpost, then sell minerals.", "Place a settlement terminal and sell minerals.");
            AddQuestEnglish("emergency_escape_return", "Return by Emergency Portal", "Use an emergency escape portal to return to an outpost or the elevator.", "Enter the portal and choose a destination.");
            // B-209 새 게임 전 광부 선택 화면.
            Add("miner_select.ops", "SUB-TERRA OPS", "SUB-TERRA OPS");
            Add("miner_select.title", "광부 선택", "Select Miner");
            Add("miner_select.registration", "MINER REGISTRATION", "MINER REGISTRATION");
            Add("miner_select.subtitle", "탐사를 시작할 광부를 선택하세요.", "Choose the miner who will lead the expedition.");
            Add("miner_select.male", "남자", "Male");
            Add("miner_select.female", "여자", "Female");
            Add("miner_select.hint", "광부를 선택해 주세요", "Select a miner");
            Add("miner_select.start", "탐사 시작", "Start Expedition");
            Add("miner_select.back", "돌아가기", "Back");
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
            Add("mine_reset.button", "새 광산 초기화 ({0}G)", "New Mine ({0}G)");
            Add("mine_reset.confirm.title", "새 광산 구역", "New Mine Area");
            Add(
                "mine_reset.confirm.desc",
                "기존 광산을 초기화하고 새로운 구역을 생성합니다.",
                "Resets the current mine and generates a new area.");
            Add("mine_reset.confirm.cost", "{0:N0} G", "{0:N0} G");
            Add("mine_reset.confirm.balance", "보유 {0:N0} G → 이용 후 {1:N0} G", "Owned {0:N0} G → After {1:N0} G");
            Add("mine_reset.confirm.shortage", "보유 {0:N0} G · {1:N0} G 부족", "Owned {0:N0} G · {1:N0} G short");
            Add("mine_reset.confirm.reset.title", "초기화", "Reset");
            Add(
                "mine_reset.confirm.reset.desc",
                "채굴한 타일 · 지하 시설 · 가스",
                "Mined tiles · Facilities · Gas");
            Add("mine_reset.confirm.keep.title", "유지", "Kept");
            Add(
                "mine_reset.confirm.keep.desc",
                "업그레이드 · 심층 해금 · 보유 광물",
                "Upgrades · Deep access · Minerals");
            Add("mine_reset.confirm.timer.title", "탐사 시간", "Timer");
            Add("mine_reset.confirm.timer.desc", "{0}시간으로 다시 시작", "Restarts at {0} hours");
            Add("mine_reset.confirm.create", "새 광산 생성 · {0:N0} G", "Create New Mine · {0:N0} G");
            Add("mine_reset.confirm.no", "취소", "Cancel");
            Add("mine_reset.success", "새 광산이 배치되었습니다. 이용료 {0}G.", "New mine laid out. Fee {0}G.");
            Add("mine_reset.fail.gold", "골드가 부족합니다. {0}G 필요 (보유 {1}G).", "Not enough gold. Need {0}G (have {1}G).");
            Add("mine_reset.fail.busy", "지금은 새 광산을 열 수 없습니다.", "Cannot open a new mine right now.");
            Add("mine_reset.fail.surface", "지상 기지에서만 새 광산을 열 수 있습니다.", "New mines can only be opened at Surface Base.");
            Add("mine_reset.clock.label", "광산 초기화까지", "Mine Reset In");
            Add("guide.build.close", "B 또는 ESC로 창을 닫으면 선택이 취소됩니다.",
                "Press B or Esc to close Build and clear the selection.");
            Add("guide.upgrade.close", "ESC 또는 오른쪽 위 X 버튼으로 닫습니다.",
                "Press Esc or use the X button in the upper right.");
            Add("mine_reset.timed.title", "탐사 시간 종료", "Exploration Time Over");
            Add(
                "mine_reset.timed.highlight",
                "새로운 광산 구역이 생성되었습니다.",
                "A new mine area has been generated.");
            // {0}은 MineResetService.CycleDurationSeconds에서 계산한 시간 수다.
            Add(
                "mine_reset.timed.body.mine",
                "탐사 시간 {0}시간이 만료되어 지상 기지로 이동했습니다.\n채굴한 타일과 설치한 지하 시설이 초기화되었습니다.",
                "The {0}-hour exploration time expired and you were moved to Surface Base.\nMined tiles and placed underground facilities were reset.");
            Add(
                "mine_reset.timed.body.surface",
                "탐사 시간 {0}시간이 만료되어 광산이 초기화되었습니다.\n채굴한 타일과 설치한 지하 시설이 초기화되었습니다.",
                "The {0}-hour exploration time expired and the mine was reset.\nMined tiles and placed underground facilities were reset.");
            // {0}은 다음 유료 초기화 비용(G)이다.
            Add(
                "mine_reset.timed.footer",
                "다음 유료 초기화 비용: {0:N0} G",
                "Next paid reset cost: {0:N0} G");
            Add("mine_reset.timed.ok", "확인", "OK");
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
                    return Get("settings.frame.165", "165");
                case 6:
                    return Get("settings.frame.180", "180");
                case 7:
                    return Get("settings.frame.240", "240");
                case 8:
                    return Get("settings.frame.unlimited", "제한없음");
                default:
                    return Get("settings.frame.auto", "자동(기본값)");
            }
        }

        private static void AddQuestEnglish(string id, string title, string description, string hint)
        {
            var key = "demo.quest." + id;
            English[key + ".title"] = title;
            English[key + ".description"] = description;
            English[key + ".hint"] = hint;
        }

        private static void Add(string key, string ko, string en)
        {
            Korean[key] = ko;
            English[key] = en;
        }
    }
}
