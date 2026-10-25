# 효과음 출처 기록

- 파일을 `Assets/_Project/Audio/SFX/`에 넣을 때마다 이 표의 해당 줄을 채운다. Pixabay 파일도 받은 페이지 주소를 남긴다.
- 같은 이름으로 덮어쓰면 새 줄을 만들지 말고 그 줄의 주소·날짜·비고를 고친다.
- 음원 파일은 고치지 않는다. 게임에 맞추는 조정(음량·피치·재생 구간·페이드아웃)은 `Resources/SfxLibrary.asset`의 칸 값으로만 한다.

| 파일 이름 | 게임 안에서 쓰는 곳 | 받은 페이지 주소 | 받은 날짜 | 비고 |
|---|---|---|---|---|
| ui_click | 공통 버튼 클릭 | https://pixabay.com/sound-effects/technology-sci-fi-ui-click-sound-596258/ | 2026-10-25 | Sci-Fi UI Click Sound (SoundShelfStudio). 앞 무음 정리, "딸깍" 본체 0.2초만, 모노로 합침, 리미터 약 +6dB(1.39→0.20초). 파일 형식은 기존과 같은 mp3. 이전 원본 ui-click-retro-514601에서 교체 |
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
| sell_confirm | 판매 확정 | https://pixabay.com/sound-effects/film-special-effects-cash-register-kaching-376867/ | 2026-10-23 | 앞뒤 무음 정리(1.10→0.99초) |
| storage_box_rise | 보관함 상자 올라옴 | https://pixabay.com/sound-effects/film-special-effects-sci-fi-door-unlock-and-hydraulic-open-2-546619/ | 2026-10-23 | 원본 0.80~2.60초만 남김(5.11→1.80초), 페이드 인·아웃 |
| storage_lid_open | 보관함 뚜껑 열림 | https://pixabay.com/sound-effects/film-special-effects-box-open-90674/ | 2026-10-23 | 앞 무음 정리(0.48→0.31초), 끝 짧은 페이드 |
| storage_ore_pop | 보관함 광물 튀어나옴 | https://pixabay.com/sound-effects/film-special-effects-gem-release-384932/ | 2026-10-23 | 꼬리 무음 정리(1.02→0.70초) |
| storage_take_all | 전체 보관·꺼내기 | https://pixabay.com/sound-effects/film-special-effects-heavy-rock-rolling-515254/ | 2026-10-23 | 굴러가는 꼬리 자르고 페이드(2.11→1.34초) |
| guide_page_flip_x3 | 게임 가이드 첫 등장(세 장 넘김) | https://pixabay.com/sound-effects/film-special-effects-flipping-book-page-499646/ | 2026-10-23 | 넘김 시작 SF 부분 + 넘김 1번을 0.4초 간격 3번 합침(1.70초). B-161에서 앞 SF 부분 약 +9.5dB |
| guide_page_flip_loop | 게임 가이드 다시 열기(한 장 넘김) | https://pixabay.com/sound-effects/film-special-effects-flipping-book-page-499646/ | 2026-10-23 | 원본 0.64~0.92초 넘김 1번(0.28초) |
| timer_tick | 3시간 만료 시계 연출 시침 회전 | https://pixabay.com/sound-effects/film-special-effects-tick-tock-94356/ | 2026-10-23 | 앞뒤 정리(1.04→0.98초). 앞 0.6초만 재생. B-161에서 +10dB 후 리미터 |
| timer_tick_single | 광산 초기화 시계 막판 1초 째깍 | https://pixabay.com/sound-effects/film-special-effects-tick-tock-94356/ | 2026-10-23 | 첫 째깍 1번만(0.23초). B-161에서 +10dB 후 리미터 |
| timer_end | 3시간 만료 시계 흔들림 벨 | https://pixabay.com/sound-effects/film-special-effects-generic-alarm-clock-86759/ | 2026-10-23 | 벨 3번까지만, 끝 페이드(4.56→1.43초) |
| minimap_beep | 미니맵 M 단계 전환 | https://pixabay.com/sound-effects/film-special-effects-digital-beep-151921/ | 2026-10-23 | 다듬지 않은 원본 mp3(1.72초). 앞 0.3초만 재생 |
| keycap_pop | 키 조작 창 키캡 튀어나옴 | https://pixabay.com/sound-effects/film-special-effects-keyboard-click-327728/ | 2026-10-23 | 잔향 꼬리 정리(1.03→0.43초) |
| portal_activate | 긴급 탈출 포탈 창 열림 | https://pixabay.com/sound-effects/technology-energy-charge-1-482503/ | 2026-10-23 | 고조 뒤 감쇠 페이드(3.02→2.42초). 앞 1.3초만 재생 |
| portal_switch | 긴급 탈출 목적지 변경 | https://pixabay.com/sound-effects/film-special-effects-tuning-in-blip-201632/ | 2026-10-23 | 앞 무음 정리, 음량 키움(0.41→0.37초) |
| portal_warp | 긴급 탈출 이동 | https://pixabay.com/sound-effects/film-special-effects-sci-fi-portal-jump-05-416165/ | 2026-10-23 | 잔향 꼬리 페이드(3.50→1.93초) |
| gold_lever | 골드 팝업 레버 | https://pixabay.com/sound-effects/film-special-effects-clank1-91862/ | 2026-10-23 | 꼬리 정리(0.71→0.62초) |
| gold_reel_stop | 골드 팝업 숫자 착지 | https://pixabay.com/sound-effects/film-special-effects-money-counting-machine-sfx-406495/ | 2026-10-23 | 원본 0.84~1.92초만(2.23→1.09초). 앞 0.6초만 재생 |
| gold_coin_pop | 골드 팝업 금화 분출 | https://pixabay.com/sound-effects/film-special-effects-coins-drop-1-404409/ | 2026-10-23 | 앞 무음 제거, 쏟아지는 부분만(2.22→0.89초) |
| run_fail_collapse_glitch | 탐사 실패 창 등장 글리치 | https://pixabay.com/sound-effects/film-special-effects-digital-glitch-sfx-438248/ | 2026-10-23 | 앞 무음 정리, 리미터 약 +3dB(1.23→1.17초) |
| run_fail_blackout | 탐사 실패 후 화면 암전 | https://pixabay.com/sound-effects/film-special-effects-power-off-386180/ | 2026-10-23 | 앞 무음 정리, 끝 잡음 꼬리 자르고 페이드(1.28→0.83초) |
| run_fail_row_tick | 탐사 실패 창 잃은 자원 줄 등장 | https://pixabay.com/sound-effects/film-special-effects-beep-313342/ | 2026-10-23 | 원본이 작아 약 +6~8dB(0.19초) |
| escape_arrive | 긴급 탈출 도착(워프 0.35초 뒤) | https://pixabay.com/sound-effects/technology-sci-fi-power-down-blip-608638/ | 2026-10-23 | 앞 무음 정리, +2dB(0.43→0.46초) |
| briefing_glitch_tick | 시작 브리핑 창 등장 글리치 | https://pixabay.com/sound-effects/film-special-effects-glitch-sound-effect-hd-379466/ | 2026-10-24 | 원본 안 두 번째 지직(1.63~2.11초)만, +8dB 후 리미터(3.10→0.47초). 이전 원본 glitch-sound-2-589408에서 교체 |
| briefing_glitch_tick_short | 시작 브리핑 표시 중 짧은 글리치 | https://pixabay.com/sound-effects/film-special-effects-glitch-sound-effect-hd-379466/ | 2026-10-24 | briefing_glitch_tick과 같은 파일(같은 바이트, 0.47초). 이전 원본 glitch-sound-2-589408에서 교체 |
| briefing_close | 시작 브리핑 창 닫힘 | https://pixabay.com/sound-effects/film-special-effects-digital-glitch-sfx-438248/ | 2026-10-24 | 받은 mp3를 다듬지 않고 진짜 PCM wav로 바꿈(2026-10-25, 1.23초, 소리·길이·음량 그대로). run_fail_collapse_glitch와 같은 원본. 칸 값: 앞 1.0초만 재생, 끝 0.3초 페이드. 이전 원본 glitch-and-distortion-sound-effects-547046에서 교체 |
| sell_open_coin_stack | 판매 창 열림 금화 쌓임(4번) | https://pixabay.com/sound-effects/film-special-effects-drop-coin-384921/ | 2026-10-23 | 울림 0.3초까지, 리미터 약 +3dB(1.10→0.30초). 피치 1.00→1.12 |
| sell_open_coin_burst | 판매 창 열림 금화 터짐 | https://pixabay.com/sound-effects/film-special-effects-spinning-coin-on-table-352448/ | 2026-10-23 | 원본 0.185~1.61초, 리미터 약 +5dB(2.09→1.42초). 끝까지 재생 |
| portal_idle_loop | 긴급 탈출 포탈 근처 대기음(반복) | https://pixabay.com/sound-effects/film-special-effects-black-hole-55468/ | 2026-10-23 | 원본 0.74~10.74초 정방향+역방향 이음(82→20.01초, 모노). 압축된 채 메모리 |
| storage_close | 보관함 닫힘(뚜껑 닫힘·상자 내려감) | https://pixabay.com/sound-effects/film-special-effects-futuristic-hydraulic-sci-fi-door-opening-1-614165/ | 2026-10-25 | 수압 문 열림 소리 앞 0.9초를 거꾸로 뒤집음("쉬이익" 후 "척" 잠김), 앞 0.12초 페이드인(2.18→0.90초). 상자 올라옴(546619)과 같은 제작자. 칸 값 음량 0.35. 이전 원본 close-big-military-case-47806에서 교체 |
| storage_partial_notice | 보관함 전체 꺼내기가 일부만 꺼내졌을 때 | https://pixabay.com/sound-effects/film-special-effects-soft-ui-warning-alert-1-614153/ | 2026-10-24 | 앞 무음 정리, 끝 페이드(0.94→0.80초) |
| timer_5min_alert | 광산 남은 시간 5분 알림 | https://pixabay.com/sound-effects/film-special-effects-digital-alarm-clock-151927/ | 2026-10-24 | 반복 알람 중 첫 "삐삐삐삐" 묶음만, 직류 성분 제거(7.60→0.80초) |
| keys_holo_open | 키 조작 창 열림(홀로그램 펼침) | https://pixabay.com/sound-effects/technology-futuristic-holographic-interface-menu-opening-566063/ | 2026-10-24 | 앞 0.9초만, 끝 0.3초 페이드(2.04→0.90초) |
| keys_page_turn | 키 조작 창 이전/다음 넘김 | https://pixabay.com/sound-effects/film-special-effects-ui-swipe-navigation-soft-523625/ | 2026-10-24 | 앞뒤 무음 정리, 끝 페이드(1.38→0.62초) |
| portal_close | 긴급 탈출 포탈 창을 이동 없이 닫음 | https://pixabay.com/sound-effects/technology-futuristic-hologram-menu-close-ui-614155/ | 2026-10-24 | 앞뒤 무음 정리, 끝 페이드(0.89→0.71초) |
| footstep | 걷기 한 걸음 | https://pixabay.com/sound-effects/film-special-effects-st1-footstep-sfx-323053/ | 2026-10-25 | 한 걸음, 약 +5dB 리미터(0.42→0.39초). World 폴더 |
| jump | 점프 도약 | https://pixabay.com/sound-effects/film-special-effects-jump-615208/ | 2026-10-25 | 앞 0.6초만, 끝 페이드(1.75→0.58초). World 폴더 |
| land | 보통 착지 | https://pixabay.com/sound-effects/film-special-effects-character-fall-impact-250069/ | 2026-10-25 | 앞 0.6초만, 끝 페이드(1.27→0.55초). World 폴더 |
| land_heavy | 세게 떨어짐(다침) | https://pixabay.com/sound-effects/film-special-effects-heavy-body-fall-352446/ | 2026-10-25 | 앞 무음 정리, 본체 약 1초 + 꼬리 페이드(2.40→1.01초). World 폴더 |
| ladder_grab | 사다리 잡기·놓기 | https://pixabay.com/sound-effects/film-special-effects-metal-clang-sound-81634/ | 2026-10-25 | "챙"만, 약 +5dB 리미터(1.44→0.20초). World 폴더 |
| ladder_climb | 사다리 한 칸 오르기/내리기 | https://pixabay.com/sound-effects/film-special-effects-footsteps-on-wood-397989/ | 2026-10-25 | 나무 발걸음 중 첫 걸음만(4.34→0.50초). 변형 ladder_climb_b는 두 번째 걸음. World 폴더 |
| ladder_climb_b | 사다리 한 칸 오르기/내리기(ladder_climb과 번갈아) | https://pixabay.com/sound-effects/film-special-effects-footsteps-on-wood-397989/ | 2026-10-25 | 같은 원본의 두 번째 걸음만(4.34→0.50초). World 폴더 |
| elevator_door | 엘리베이터 문 열림·닫힘 | https://pixabay.com/sound-effects/technology-elevator-door-opening-2-sounds-included-462112/ | 2026-10-25 | 첫 번째 문 소리 앞 1.6초만, 열림·닫힘 공용(9.40→1.58초). World 폴더 |
| elevator_move_loop | 엘리베이터 이동 중(반복) | https://pixabay.com/sound-effects/city-elevator-ride-137245/ | 2026-10-25 | 원본 14~20초를 0.5초 겹쳐 이음, 한 바퀴 5.5초. World 폴더 |
| elevator_arrive_ding | 엘리베이터 도착 | https://pixabay.com/sound-effects/film-special-effects-lift-bell-107838/ | 2026-10-25 | 울림 1.35초까지, 끝 페이드(3.67→1.35초). World 폴더 |
| drill_loop | 채굴 드릴(반복) | https://pixabay.com/sound-effects/city-jack-hammer-breaking-up-concrete-short-burst-103896/ | 2026-10-25 | 원본 2~4초를 0.15초 겹쳐 이음, 한 바퀴 1.85초. 비교용 정방향+역방향 2.0초 버전 있음. World 폴더 |
| block_break | 블록 부서짐 | https://pixabay.com/sound-effects/film-special-effects-rock-destroy-6409/ | 2026-10-25 | 부스러기까지, 끝 페이드(2.98→1.06초). World 폴더 |
| pickup_common | 구리·철·리튬 획득 | https://pixabay.com/sound-effects/film-special-effects-item-pickup-3-540193/ | 2026-10-25 | 앞 0.7초만, 끝 페이드(1.54→0.68초). World 폴더 |
| pickup_rare | 엔진 연료(희귀) 획득 | https://pixabay.com/sound-effects/film-special-effects-diamond-found-190255/ | 2026-10-25 | 첫 반짝임만, 자연 페이드(5.72→0.81초). World 폴더 |
| cargo_heavy_warning | 화물 80% 넘음 경고 | https://pixabay.com/sound-effects/film-special-effects-warning-sound-6686/ | 2026-10-25 | 2번 반복 중 첫 번째만(1.03→0.35초). World 폴더 |
