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

        /// <summary>SfxId 순서대로 15칸(None·비워 둔 3번 제외). 파일 이름은 확장자 제외.</summary>
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
            };
        }
    }
}
