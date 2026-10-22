# 효과음 출처 기록

- 파일을 `Assets/_Project/Audio/SFX/`에 넣을 때마다 이 표의 해당 줄을 채운다. Pixabay 파일도 받은 페이지 주소를 남긴다.
- 같은 이름으로 덮어쓰면 새 줄을 만들지 말고 그 줄의 주소·날짜·비고를 고친다.
- 음원 파일은 고치지 않는다. 게임에 맞추는 조정(음량·피치·재생 구간·페이드아웃)은 `Resources/SfxLibrary.asset`의 칸 값으로만 한다.

| 파일 이름 | 게임 안에서 쓰는 곳 | 받은 페이지 주소 | 받은 날짜 | 비고 |
|---|---|---|---|---|
| ui_click | 공통 버튼 클릭 | https://pixabay.com/sound-effects/film-special-effects-ui-click-retro-514601/ | | UI Click Retro (SoundShelfStudio) |
| ui_hover | 버튼 호버 (선택, 기본 끔) | https://pixabay.com/sound-effects/film-special-effects-soft-ui-hover-tick-608625/ | | Soft UI Hover Tick |
| ui_confirm | 확인·성공 | https://pixabay.com/sound-effects/film-special-effects-confirm-tap-394001/ | | Confirm Tap (ExistentialTaco) |
| ui_cancel_fail | 취소·불가·실패 | https://pixabay.com/sound-effects/film-special-effects-ui-error-pop-515668/ | | UI Error Pop |
| ui_popup_open | 팝업 열림 | https://pixabay.com/sound-effects/film-special-effects-new-notification-010-352755/ | | New Notification 010 (2순위 ...new-notification-08-352461/) |
| ui_popup_close | 팝업 닫힘 | https://pixabay.com/sound-effects/film-special-effects-ui-interface-03-277552/ | | UI Interface 03. 원본 2.2초라 앞 0.1~1.0초만 재생 |
| ui_scan_tick | 시설 이름표 스캔선 틱 | https://pixabay.com/sound-effects/film-special-effects-sci-fi-door-14782/ | | Sci-Fi Door. 들어보고 유지/제거 결정 |
| ui_cctv_on | 코어 CCTV 켜짐 | https://pixabay.com/sound-effects/film-special-effects-tv-shutdown-386167/ | | TV Shutdown |
| ui_mine_reset_whoosh | 광산 초기화 실행 | https://pixabay.com/sound-effects/technology-epic-whoosh-450462/ | | Epic Whoosh. 원본 7.3초라 0.55초부터 1.6초만 재생 |
| world_holo_open | 엘리베이터 홀로그램 펼침 | https://pixabay.com/sound-effects/technology-futuristic-holographic-interface-menu-opening-566063/ | | 원본 2.0초라 앞 1.2초만 재생 |
| world_holo_close | 엘리베이터 홀로그램 수축 | https://pixabay.com/sound-effects/technology-futuristic-hologram-menu-close-ui-614155/ | | |
| world_facility_tag_beep | 시설 근접 라벨 등장 | https://pixabay.com/sound-effects/film-special-effects-short-beep-tone-47916/ | | Short Beep Tone |
| alarm_mine_wail | 위험도 상승 경보 | https://pixabay.com/sound-effects/film-special-effects-factory-alarm-95712/ | | Factory Alarm. 웽 한 번(약 0.4~0.6초). 단계별 반복·높이는 코드가 조절 |
| alert_risk_tick | 위험도 하강 틱 | https://pixabay.com/sound-effects/film-special-effects-ui-ai-system-notification-537631/ | | UI AI System Notification |
| ui_slot_select | 세이브 슬롯 선택 | | | 선정 전. 비어 있으면 ui_click으로 대신 난다 |
