using System.Collections.Generic;
using SubTerra.Shared.Audio;

namespace SubTerra.App.Audio
{
    /// <summary>
    /// 칸마다 파일 이름과 처음 채울 설정값. 새 SfxLibrary 에셋을 만들 때만 쓰이며,
    /// 이미 만든 에셋의 값은 인스펙터에서 고친다(빌더가 덮어쓰지 않는다).
    /// 음량은 클립 평균 세기(RMS)를 맞추는 방향으로, 2초가 넘는 클립은 앞부분만 재생하도록 잡았다.
    /// </summary>
    public static class SfxLibraryDefaults
    {
        public const string ClipRoot = "Assets/_Project/Audio/SFX";

        public static string FolderOf(SfxBus bus)
        {
            switch (bus)
            {
                case SfxBus.World: return "World";
                case SfxBus.Alert: return "Alert";
                default: return "UI";
            }
        }

        /// <summary>칸이 비어 있을 때 경고 없이 대신 낼 칸. 표에 없는 빈 칸은 무음이다.</summary>
        public static readonly IReadOnlyDictionary<SfxId, SfxId> Fallbacks = new Dictionary<SfxId, SfxId>
        {
            { SfxId.UiSlotSelect, SfxId.UiClick },
        };

        /// <summary>길게 반복 재생하는 파일. 통째로 풀어 두지 않고 압축된 채 메모리에 둔다.</summary>
        private static readonly HashSet<string> LoopFileNames = new HashSet<string>
        {
            "portal_idle_loop",
        };

        public static bool IsLoopFile(string fileName) => fileName != null && LoopFileNames.Contains(fileName);

        /// <summary>SfxId 순서대로 49칸(None·비워 둔 3번 제외). 파일 이름은 확장자 제외.</summary>
        public static IReadOnlyList<(SfxEntry Entry, string FileName)> Create()
        {
            return new List<(SfxEntry, string)>
            {
                // 원본 1.10초. 소리는 0.2초 안에 끝나고 가장 작은 클립이라 음량 최대.
                (new SfxEntry(SfxId.UiClick, SfxBus.UI, volume: 1f), "ui_click"),
                // 원본 0.12초.
                (new SfxEntry(SfxId.UiHover, SfxBus.UI, volume: 0.7f), "ui_hover"),
                (new SfxEntry(SfxId.UiConfirm, SfxBus.UI, volume: 0.55f), "ui_confirm"),
                (new SfxEntry(SfxId.UiCancelFail, SfxBus.UI, volume: 0.6f), "ui_cancel_fail"),
                (new SfxEntry(SfxId.UiPopupOpen, SfxBus.UI, volume: 0.5f), "ui_popup_open"),
                // 원본 2.16초 → 소리가 있는 앞 1초 안쪽만. 0.1초 지점부터 0.9초.
                (new SfxEntry(SfxId.UiPopupClose, SfxBus.UI, volume: 0.55f,
                    startTime: 0.1f, maxDuration: 0.9f, fadeOut: 0.25f), "ui_popup_close"),
                (new SfxEntry(SfxId.UiScanTick, SfxBus.UI, volume: 0.5f), "ui_scan_tick"),
                (new SfxEntry(SfxId.UiCctvOn, SfxBus.UI, volume: 0.45f), "ui_cctv_on"),
                // 원본 7.32초, 앞 0.6초는 무음이고 이후 천천히 커진다 → 0.55초부터 1.6초만.
                (new SfxEntry(SfxId.UiMineResetWhoosh, SfxBus.UI, volume: 0.4f,
                    startTime: 0.55f, maxDuration: 1.6f, fadeOut: 0.4f), "ui_mine_reset_whoosh"),
                // 원본 2.04초 → 소리가 이어지는 앞 1.2초만.
                (new SfxEntry(SfxId.WorldHoloOpen, SfxBus.World, volume: 1f,
                    maxDuration: 1.2f, fadeOut: 0.35f), "world_holo_open"),
                (new SfxEntry(SfxId.WorldHoloClose, SfxBus.World, volume: 0.7f), "world_holo_close"),
                (new SfxEntry(SfxId.WorldFacilityTagBeep, SfxBus.World, volume: 0.3f), "world_facility_tag_beep"),
                // 원본 1.80초 → 웽 한 번(0.55초)만. 반복 간격이 더 짧으면 SfxPlayer가 간격 안에서 끝낸다.
                (new SfxEntry(SfxId.AlarmMineWail, SfxBus.Alert, volume: 1f,
                    maxDuration: 0.55f, fadeOut: 0.12f), "alarm_mine_wail"),
                // 원본 1.70초 중 소리는 0.5초까지.
                (new SfxEntry(SfxId.AlertRiskTick, SfxBus.Alert, volume: 0.9f,
                    maxDuration: 0.6f, fadeOut: 0.1f), "alert_risk_tick"),
                // 파일이 아직 없는 칸. 비어 있는 동안 UiClick으로 대신 나고, 파일이 들어오면 같은 이름으로 채워진다.
                (new SfxEntry(SfxId.UiSlotSelect, SfxBus.UI, volume: 0.8f), "ui_slot_select"),

                // B-160. 경보 반복을 끊지 않도록 Alert 버스는 쓰지 않는다. 길이는 다듬은 파일 기준.
                // 0.99초.
                (new SfxEntry(SfxId.SellConfirm, SfxBus.UI, volume: 0.6f), "sell_confirm"),
                // 1.80초.
                (new SfxEntry(SfxId.StorageBoxRise, SfxBus.World, volume: 0.5f), "storage_box_rise"),
                // 0.31초.
                (new SfxEntry(SfxId.StorageLidOpen, SfxBus.World, volume: 0.7f), "storage_lid_open"),
                // 1.34초.
                (new SfxEntry(SfxId.StorageTakeAll, SfxBus.World, volume: 0.6f), "storage_take_all"),
                // 1.40초. 앞 0.6초 SF 소리 뒤 세 장 넘김. B-161: SF가 넘김에 묻혀 음량을 올렸다.
                (new SfxEntry(SfxId.GuidePageFlipX3, SfxBus.UI, volume: 0.8f), "guide_page_flip_x3"),
                // 0.28초.
                (new SfxEntry(SfxId.GuidePageFlip, SfxBus.UI, volume: 0.5f), "guide_page_flip_loop"),
                // 0.98초, 째깍 4번 → 0.6초만. B-161: 짧은 째깍이 벨·BGM에 묻혀 음량을 올렸다.
                (new SfxEntry(SfxId.TimerTick, SfxBus.UI, volume: 0.9f,
                    maxDuration: 0.6f, fadeOut: 0.1f), "timer_tick"),
                // 0.40초.
                (new SfxEntry(SfxId.TimerTickSingle, SfxBus.UI, volume: 0.9f), "timer_tick_single"),
                // 1.43초.
                (new SfxEntry(SfxId.TimerEnd, SfxBus.UI, volume: 0.6f), "timer_end"),
                // 1.72초(삑 0.12초 + 무음) → 목소리를 오래 잡지 않게 0.3초만.
                (new SfxEntry(SfxId.MinimapBeep, SfxBus.UI, volume: 0.4f, maxDuration: 0.3f), "minimap_beep"),
                // 0.43초.
                (new SfxEntry(SfxId.KeycapPop, SfxBus.UI, volume: 0.45f), "keycap_pop"),
                // 2.42초 → 1.3초만.
                (new SfxEntry(SfxId.PortalActivate, SfxBus.World, volume: 0.5f,
                    maxDuration: 1.3f, fadeOut: 0.3f), "portal_activate"),
                // 0.37초.
                (new SfxEntry(SfxId.PortalSwitch, SfxBus.UI, volume: 0.5f), "portal_switch"),
                // 1.93초.
                (new SfxEntry(SfxId.PortalWarp, SfxBus.World, volume: 0.6f), "portal_warp"),
                // 0.62초.
                (new SfxEntry(SfxId.GoldLever, SfxBus.World, volume: 0.6f), "gold_lever"),
                // 1.09초 → 0.6초만.
                (new SfxEntry(SfxId.GoldReelStop, SfxBus.World, volume: 0.5f,
                    maxDuration: 0.6f, fadeOut: 0.2f), "gold_reel_stop"),
                // 0.89초.
                (new SfxEntry(SfxId.GoldCoinPop, SfxBus.World, volume: 0.45f), "gold_coin_pop"),
                // 0.70초.
                (new SfxEntry(SfxId.StorageOrePop, SfxBus.World, volume: 0.5f), "storage_ore_pop"),

                // B-162. Alert 버스는 쓰지 않는다. 길이는 다듬은 파일 기준(원본 → 새 길이).
                // 1.23 → 1.17초.
                (new SfxEntry(SfxId.RunFailGlitch, SfxBus.UI, volume: 0.6f), "run_fail_collapse_glitch"),
                // 1.28 → 0.83초.
                (new SfxEntry(SfxId.RunFailBlackout, SfxBus.UI, volume: 0.6f), "run_fail_blackout"),
                // 0.19초.
                (new SfxEntry(SfxId.RunFailRowTick, SfxBus.UI, volume: 0.5f), "run_fail_row_tick"),
                // 0.43 → 0.46초.
                (new SfxEntry(SfxId.EscapeArrive, SfxBus.World, volume: 0.6f), "escape_arrive"),
                // 1.57 → 1.02초.
                (new SfxEntry(SfxId.BriefingGlitch, SfxBus.UI, volume: 0.5f), "briefing_glitch_tick"),
                // 0.42초(위 소리의 가장 큰 부분).
                (new SfxEntry(SfxId.BriefingGlitchShort, SfxBus.UI, volume: 0.45f), "briefing_glitch_tick_short"),
                // 1.93 → 1.82초 → 1.0초만.
                (new SfxEntry(SfxId.BriefingClose, SfxBus.UI, volume: 1.0f,
                    maxDuration: 1.0f, fadeOut: 0.3f), "briefing_close"),
                // 1.10 → 0.30초. 4번 겹쳐 나므로 음량을 낮게 잡았다.
                (new SfxEntry(SfxId.SellCoinStack, SfxBus.UI, volume: 0.5f), "sell_open_coin_stack"),
                // 2.09 → 1.42초. 끝까지 재생.
                (new SfxEntry(SfxId.SellCoinBurst, SfxBus.UI, volume: 0.55f), "sell_open_coin_burst"),
                // 82 → 20.01초 반복용. 원샷 풀이 아니라 PortalIdleLoopAudio가 따로 튼다.
                (new SfxEntry(SfxId.PortalIdleLoop, SfxBus.World, volume: 0.35f), "portal_idle_loop"),

                // B-163. 모두 UI 버스(Alert는 경보 반복을 끊는다). 길이는 다듬은 파일 기준.
                // 1.00초. 순간 소리라 평균이 작다.
                (new SfxEntry(SfxId.StorageClose, SfxBus.UI, volume: 0.7f), "storage_close"),
                // 0.80초.
                (new SfxEntry(SfxId.StoragePartialNotice, SfxBus.UI, volume: 0.65f), "storage_partial_notice"),
                // 0.80초("삐삐삐삐" 한 묶음).
                (new SfxEntry(SfxId.Timer5MinAlert, SfxBus.UI, volume: 0.6f), "timer_5min_alert"),
                // 0.90초. 키캡 소리를 덮지 않게 작게.
                (new SfxEntry(SfxId.KeysHoloOpen, SfxBus.UI, volume: 0.45f), "keys_holo_open"),
                // 0.62초.
                (new SfxEntry(SfxId.KeysPageTurn, SfxBus.UI, volume: 0.65f), "keys_page_turn"),
                // 0.71초.
                (new SfxEntry(SfxId.PortalClose, SfxBus.UI, volume: 0.55f), "portal_close"),
            };
        }
    }
}
