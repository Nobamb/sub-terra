# Sub-Terra C단계 효과음 (정리본, 2026-10-23)

공통: 44.1kHz 16bit WAV. 앞 무음 제거(소리 시작 전 약 2.5ms만 남김) + 2ms 페이드인, 뒤 무음 정리 후 짧은 페이드아웃. 피크 -1dBFS 이하. 음 높이·속도는 바꾸지 않음.
음량(dBFS)은 피크 / 전체 평균(RMS) / 가장 큰 0.1초 평균.

| 파일 | 쓰는 장면 | 원래 → 새 길이 | 한 일 | 피크 / RMS / 0.1초 최대 |
|---|---|---|---|---|
| run_fail_collapse_glitch.wav | HP 0 쓰러짐 + 화면 지직. **광산 타이머 만료에도 같이 씀** | 1.23s → 1.17s | 앞 무음 0.03s 제거. 뒤쪽 작은 "틱틱" 잔글리치는 소리의 일부라 남김. 리미터로 약 +3dB | -1.0 / -16.0 / -12.5 |
| run_fail_blackout.wav | 화면 암전 "뚝". **타이머 만료에도 같이 씀** | 1.28s → 0.83s | 앞 무음 0.09s 제거. 끝(원본 0.95s 뒤)에 붙은 작은 잡음 꼬리 잘라내고 0.12s 페이드 | -2.1 / -13.6 / -9.0 |
| run_fail_row_tick.wav | 실패 창에 잃은 자원 한 줄씩 올라올 때 | 0.19s → 0.19s | 원본이 작아 약 +6~8dB 키움(빠른 리미터, 클리핑 없음). 끝에 0.05s 여유 | -1.0 / -14.4 / -12.0 |
| escape_arrive.wav | 긴급 탈출 워프(PortalWarp) 직후, 카메라가 엘리베이터·전진기지 도착 지점으로 바뀐 순간 "뿌웅↓" | 0.43s → 0.46s | 앞 무음 정리, +2dB, 끝에 0.05s 여유 | -1.1 / -13.7 / -11.0 |
| briefing_glitch_tick.wav | 시작 브리핑 창이 뜰 때 지직 | 3.10s → 0.47s | **2026-10-24 교체.** 새 원본(glitch-sound-effect-hd-379466) 안 두 번째 지직 덩어리(원본 1.63~2.11s)만 사용. 앞 무음 제거, 2ms 페이드인, 끝 0.04s 페이드. +8dB 후 리미터 | -1.0 / -17.3 / -14.7 |
| briefing_glitch_tick_short.wav | 시작 브리핑 창이 떠 있는 동안 가끔 나는 짧은 지직 | 3.10s → 0.47s | **2026-10-24 교체.** 위 파일과 완전히 같은 소리(같은 바이트) | -1.0 / -17.3 / -14.7 |
| briefing_close.wav | 시작 브리핑 창 닫힘 | 원본 1.23s | **2026-10-24 교체.** 원본을 digital-glitch-sfx-438248로 바꿈(run_fail_collapse_glitch와 같은 원본). 자른 구간·음량은 측정 안 함 | 측정 안 함 |
| portal_idle_loop.wav | 긴급 탈출 포탈 근처 대기음(반복 재생) | 82s → 20.01s (모노) | 원본 0.74~10.74s 10초 + 그 10초를 거꾸로 붙임 → 정방향→역방향→정방향으로 끝없이 이어짐. 이음매는 파형이 꺾이지 않는 지점에 맞춰 튀는 소리 없음. 원본 맨 앞 0.7초는 서서히 커지는 부분이라 반복할 때마다 소리가 꺼지는 게 들려서 건너뜀 | -1.0 / -13.3 / -9.9 |
| sell_open_coin_stack.wav | 판매 창 열림: 금화 하나가 쌓일 때마다 1번 (4번) | 1.10s → 0.30s | 앞 무음 0.03s 제거(맨 앞이 바로 "칭"). 울림은 0.3s까지 남기고 마지막 0.14s 페이드. 리미터로 약 +3dB. 한 파일 안에 "칭-칭" 작은 튕김 2번(0.04s 간격)이 들어 있음 | -1.0 / -18.1 / -15.5 |
| sell_open_coin_burst.wav | 판매 창 열림: 창과 금화가 부딪혀 터지는 순간 1번 | 2.09s → 1.42s | 원본 0.185~1.61s 사용(앞 무음 0.185s 제거, 동전이 멈추는 자연스러운 끝까지, 끝 0.05s 페이드). 리미터로 약 +5dB | -1.0 / -21.7 / -15.5 |

## 새 파일 없이 기존 소리 재사용
- **return_base_tv_on (지상기지 복귀, 화면 다시 켜짐)** → `SfxId.UiPopupOpen`(보건소·충전기 서비스 팝업이 열릴 때 나는 소리) 재사용.
  - TV가 켜지는 느낌을 더 살리고 싶으면 `SfxId.UiCctvOn`(코어 CCTV 화면이 켜지는 소리)도 쓸 수 있음.
- 광산 타이머 만료: `run_fail_collapse_glitch` → `run_fail_blackout` 재사용 (새 파일 없음). 몸 발광 연출은 게임에 없음.
- 번개 착지는 게임에 없어서 뺐음.


## 판매 창 열림 소리 (지상기지 판매·정산 콘솔 같은 창)
- 열림 연출 1.15초 기준 재생 시각:
  - 금화 4개가 쌓일 때 `sell_open_coin_stack`을 **0.15 / 0.23 / 0.31 / 0.39초**에 1번씩 (0.08초 간격).
    - 파일이 0.30초라 앞 소리가 끝나기 전에 다음 소리가 겹쳐 남. 끊지 말고 겹치게 두기(효과음 채널 8개라 충분).
    - 금화마다 음을 살짝 올리기 추천: 1.00 → 1.04 → 1.08 → 1.12 (키캡처럼 "쌓여 올라가는" 느낌, 같은 소리 4번 반복이 덜 기계적).
    - 4번 겹치면 합쳐서 커지니 음량은 0.5 안팎에서 시작.
  - 창과 금화가 부딪히는 **0.56초**에 `sell_open_coin_burst` 1번.
    - 앞 0.4초(크게 짤랑짤랑)가 금화 터짐 화면(0.56~0.96초)과 겹치고, 뒤로는 동전이 돌다 멈추는 "드르르" 소리가 약 1.4초까지 이어짐(창이 다 열린 1.05초 뒤로 약 0.9초 더).
    - 길게 느껴지면 재생 길이 1.0초 + 끝 0.3초 페이드로 줄이기.
- 닫힘: 새 파일 없음 → 기존 `SfxId.UiPopupClose` 재사용.

## portal_idle_loop 동작
- 긴급 탈출 포탈 시설 근처에서만 남. 가까울수록 크고 또렷하게, 멀어질수록 작아짐.
- 포탈이 여러 개면 가장 가까운 한 곳 소리만 남(겹치지 않음).
- Unity: AudioClip Loop 켜고 반복 재생. 경보(Alert) 채널은 쓰지 않음.

## 출처
| 파일 | Pixabay | 원본 파일명 |
|---|---|---|
| run_fail_collapse_glitch | https://pixabay.com/sound-effects/film-special-effects-digital-glitch-sfx-438248/ | kave_msri-digital-glitch-sfx-438248.mp3 |
| run_fail_blackout | https://pixabay.com/sound-effects/film-special-effects-power-off-386180/ | dragon-studio-power-off-386180.mp3 |
| run_fail_row_tick | https://pixabay.com/sound-effects/film-special-effects-beep-313342/ | musheran-beep-313342.mp3 |
| escape_arrive | https://pixabay.com/sound-effects/technology-sci-fi-power-down-blip-608638/ | turning_pages-sci-fi-power-down-blip-608638.mp3 |
| briefing_glitch_tick (+ _short) | https://pixabay.com/sound-effects/film-special-effects-glitch-sound-effect-hd-379466/ | virtual_vibes-glitch-sound-effect-hd-379466.mp3 |
| briefing_close | https://pixabay.com/sound-effects/film-special-effects-digital-glitch-sfx-438248/ | kave_msri-digital-glitch-sfx-438248.mp3 |
| portal_idle_loop | https://pixabay.com/sound-effects/film-special-effects-black-hole-55468/ | freesound_community-black-hole-55468.mp3 |
| sell_open_coin_stack | https://pixabay.com/sound-effects/film-special-effects-drop-coin-384921/ | freesound_gamestudio-drop-coin-384921.mp3 |
| sell_open_coin_burst | https://pixabay.com/sound-effects/film-special-effects-spinning-coin-on-table-352448/ | universfield-spinning-coin-on-table-352448.mp3 |
