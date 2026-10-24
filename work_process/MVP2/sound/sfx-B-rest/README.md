# Sub-Terra B단계 남은 효과음 (정리본, 2026-10-24)

공통: 44.1kHz 16bit WAV(스테레오). 앞 무음 제거(소리 시작 전 약 2.5ms만 남김) + 2ms 페이드인, 뒤 무음 정리 후 짧은 페이드아웃. 20Hz 아래 저음(직류 성분) 제거. 피크 -1dBFS 이하. 음 높이·속도는 바꾸지 않음.
음량(dBFS)은 피크 / 전체 평균(RMS) / 가장 큰 0.1초 평균.
mp3는 들어보기용(192kbps). 게임에는 wav를 넣음.

## 넣을 곳
- 게임 파일(wav 6개): `Assets/_Project/Audio/SFX/UI/`
- 이 묶음 전체(wav + mp3 + README): `work_process/MVP2/sound/sfx-B-rest/`

| 파일 | 쓰는 장면 | 원래 → 새 길이 | 한 일 | 피크 / RMS / 0.1초 최대 |
|---|---|---|---|---|
| storage_close.wav | 보관함을 닫을 때: 뚜껑이 "탁" 닫히고 상자가 다시 내려감 | 1.49s → 1.00s | 앞 무음 0.14s 제거. "탁"(닫힘) + 이어지는 "딸깍"(잠김) 두 덩어리까지 남기고 뒤 작은 울림은 1.0s에서 0.15s 페이드. 순간 소리라 평균이 작아서 약 +4dB 올리고 리미터 | -1.0 / -22.2 / -17.5 |
| storage_partial_notice.wav | 보관함에서 광물을 꺼냈는데 화물 공간이 모자라 일부만 꺼내졌을 때 1번. 실패음보다 부드러운 "주의" 느낌 | 0.94s → 0.80s | 앞 무음 0.02s 제거, 끝 0.08s 페이드 | -1.0 / -13.8 / -7.5 |
| timer_5min_alert.wav | 광산 남은 시간이 5분이 되는 순간 1번 | 7.60s → 0.80s | 원본은 "삐삐삐삐" 4번짜리 묶음이 약 0.95초마다 계속 반복되는 알람. **첫 묶음 1개만 사용**: 원본 0.02~0.82s("삐삐삐삐" 4번 + 짧은 여운), 끝 0.15s 페이드. 원본에 한쪽으로 치우친 직류 성분이 있어 제거(자를 때 "툭" 소리 방지) | -1.0 / -16.7 / -13.9 |
| keys_holo_open.wav | 키 조작 창이 열릴 때 홀로그램이 펼쳐지는 소리. 키캡 소리보다 먼저, 작게 | 2.04s → 0.90s | 앞 0.9초만 사용(소리가 가장 큰 앞부분 + 줄어드는 부분), 마지막 0.3s 페이드 | -1.0 / -16.3 / -13.1 |
| keys_page_turn.wav | 키 조작 창에서 이전/다음 조작 방식으로 넘길 때 | 1.38s → 0.62s | 앞 무음 0.03s 제거, 뒤 무음 정리, 끝 0.08s 페이드 | -1.0 / -11.9 / -8.1 |
| portal_close.wav | 긴급 탈출 포탈 창을 이동 없이 닫을 때: 음이 아래로 내려가며 빨려 들어가는 소리 | 0.89s → 0.71s | 앞 무음 0.02s 제거, 뒤 무음 정리, 끝 0.06s 페이드 | -1.0 / -15.0 / -12.3 |

## 다른 소리와 헷갈리지 않는지
- timer_5min_alert는 "삐-삐-삐-삐" 짧은 전자음(약 2.3kHz)이 0.4초 동안 4번 울리고 끝남.
  - 1초마다 나는 째깍(tick-tock-94356)은 아주 높은 "틱" 소리라 다름.
  - 시간 끝 벨(generic-alarm-clock-86759)은 4초 넘게 "따르릉" 계속 울리는 종소리라 다름.
- storage_partial_notice는 부드럽고 낮은 "주의" 소리라 판매·보관 실패음(기존 실패음)보다 덜 날카로움.

## 음량 추천 (게임 안에서)
- 파일은 모두 피크 -1dBFS로 맞췄음. 실제 크기는 게임 볼륨 값으로 조절.
- keys_holo_open은 키캡 소리를 가리지 않게 0.4~0.5 정도에서 시작 추천.
- storage_partial_notice, keys_page_turn은 0.1초 최대가 커서(-7~-8) 0.6~0.7 정도에서 시작 추천.
- timer_5min_alert는 경보(Alert) 채널 말고 일반 효과음 채널로 (경보 채널은 한 번에 1개라 끝 벨과 부딪힐 수 있음).

## 출처
| 파일 | Pixabay | 원본 파일명 |
|---|---|---|
| storage_close | https://pixabay.com/sound-effects/film-special-effects-close-big-military-case-47806/ | freesound_community-close-big-military-case-47806.mp3 |
| storage_partial_notice | https://pixabay.com/sound-effects/film-special-effects-soft-ui-warning-alert-1-614153/ | vadim_makes_sound-soft-ui-warning-alert-1-614153.mp3 |
| timer_5min_alert | https://pixabay.com/sound-effects/film-special-effects-digital-alarm-clock-151927/ | universfield-digital-alarm-clock-151927.mp3 |
| keys_holo_open | https://pixabay.com/sound-effects/technology-futuristic-holographic-interface-menu-opening-566063/ | vadim_makes_sound-futuristic-holographic-interface-menu-opening-566063.mp3 |
| keys_page_turn | https://pixabay.com/sound-effects/film-special-effects-ui-swipe-navigation-soft-523625/ | soundshelfstudio-ui-swipe-navigation-soft-523625.mp3 |
| portal_close | https://pixabay.com/sound-effects/technology-futuristic-hologram-menu-close-ui-614155/ | vadim_makes_sound-futuristic-hologram-menu-close-ui-614155.mp3 |
