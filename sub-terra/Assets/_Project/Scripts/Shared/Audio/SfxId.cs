namespace SubTerra.Shared.Audio
{
    /// <summary>
    /// 효과음 칸 ID. 번호는 저장·설정 에셋에 직렬화되므로 바꾸지 않고, 새 소리는 끝에만 추가한다.
    /// </summary>
    public enum SfxId
    {
        None = 0,
        UiClick = 1,
        UiHover = 2,
        [System.Obsolete("B-158에서 제거. 번호 3은 비워 둔다")]
        UiBack = 3,
        UiConfirm = 4,
        UiCancelFail = 5,
        UiPopupOpen = 6,
        UiPopupClose = 7,
        UiScanTick = 8,
        UiCctvOn = 9,
        UiMineResetWhoosh = 10,
        WorldHoloOpen = 11,
        WorldHoloClose = 12,
        WorldFacilityTagBeep = 13,
        AlarmMineWail = 14,
        AlertRiskTick = 15,
        UiSlotSelect = 16
    }
}
