# Sub-Terra B단계 효과음 (정리본, 2026-10-23)

공통: 44.1kHz 16bit WAV(원본 채널 유지), 앞 무음 제거(소리 시작 전 약 2.5ms만 남김), 2ms 페이드인, 뒤 무음·잡음 꼬리 제거 후 페이드아웃, 피크 약 -1dBFS 이하로 맞추고 음량을 비슷하게 맞춤(작은 UI 소리는 조금 작게). 음 높이·속도는 바꾸지 않음.
`portal_switch`, `storage_lid_open`은 음량을 키우기 전 원본에서 작업함.

| 파일 | 용도 | 원래 길이 → 새 길이 | 한 일 |
|---|---|---|---|
| sell_confirm.wav | 판매 확정 | 1.10s → 0.99s | 앞뒤 무음 정리 |
| storage_box_rise.wav | 보관함 상자 올라옴 | 5.11s → 1.80s | 잠금 풀리는 "치익" 부분(원본 0.80~2.60s)만 남김, 앞 40ms 페이드인, 끝 0.4s 페이드아웃 |
| storage_lid_open.wav | 보관함 뚜껑 열림 | 0.48s → 0.31s | 앞 무음 정리(삐걱 + 탁 남김). 원본이 탁 소리 직후 바로 끊겨 있어 끝에 짧은 페이드만 넣음 |
| storage_ore_pop.wav | 광물 튀어나옴 | 1.02s → 0.70s | 꼬리 무음 정리 |
| storage_take_all.wav | 전체 보관·꺼내기 | 2.11s → 1.34s | 작게 남은 굴러가는 꼬리 자르고 0.15s 페이드아웃 |
| guide_page_flip.wav | 가이드 페이지 넘김(전체) | 1.22s → 1.10s | 앞뒤 정리 |
| guide_page_flip_intro.wav | 넘김 시작 SF 느낌 부분 | → 0.60s | 원본 0.02~0.62s(넘기기 직전까지) |
| guide_page_flip_loop.wav | 넘김 1번(반복 재생용) | → 0.28s | 원본 0.64~0.92s, 조용한 지점에서 자르고 짧은 페이드 |
| guide_page_flip_x3.wav | 시작 + 3번 넘김 미리 합친 것 | → 1.70s | intro 뒤에 loop를 0.4s 간격으로 3번(2·3번째는 조금 작게) |
| timer_tick.wav | 타이머 째깍(째깍-딱 2회) | 1.04s → 0.98s | 앞뒤 정리 |
| timer_tick_single.wav | 1초마다 1번 재생용 째깍 | → 0.23s | 첫 째깍 1번만 |
| timer_end.wav | 타이머 끝 | 4.56s → 1.43s | 벨 3번까지만, 끝 페이드 |
| minimap_beep.mp3 | 미니맵 삑 | 1.72s → 1.72s | 사용자 선택으로 원본 그대로 사용(자르지 않음, mp3) |
| keycap_pop.wav | 키캡 톡 | 1.03s → 0.43s | 잔향 꼬리 정리 |
| portal_activate.wav | 포탈 작동 | 3.02s → 2.42s | 충전 고조 끝(약 2.1s) 뒤 감쇠를 0.3s 페이드로 정리 |
| portal_warp.wav | 포탈 워프 | 3.50s → 1.93s | 워프 본체 남기고 긴 잔향 꼬리 0.4s 페이드 |
| portal_switch.wav | 포탈 목적지 변경 | 0.41s → 0.37s | 앞 무음 정리, 음량 키움 |
| gold_lever.wav | 골드 레버 당김 | 0.71s → 0.62s | 꼬리 정리 |
| gold_reel_stop.wav | 골드 숫자 떨어짐 | 2.23s → 1.09s | 소리가 가장 큰 두 번째 구간(원본 0.84~1.92s)만, 끝 0.3s 페이드 |
| gold_coin_pop.wav | 골드 코인 튀어나옴 | 2.22s → 0.89s | 앞 무음 0.25s 제거, 동전 쏟아지는 부분만 남기고 0.25s 페이드 |

## 출처
| 슬롯 | Pixabay | 원본 파일명 |
|---|---|---|
| sell_confirm | https://pixabay.com/sound-effects/film-special-effects-cash-register-kaching-376867/ | dragon-studio-cash-register-kaching-376867.mp3 |
| storage_box_rise | https://pixabay.com/sound-effects/film-special-effects-sci-fi-door-unlock-and-hydraulic-open-2-546619/ | vadim_makes_sound-sci-fi-door-unlock-and-hydraulic-open-2-546619.mp3 |
| storage_lid_open | https://pixabay.com/sound-effects/film-special-effects-box-open-90674/ | freesound_community-box-open-90674.mp3 |
| storage_ore_pop | https://pixabay.com/sound-effects/film-special-effects-gem-release-384932/ | freesound_gamestudio-gem-release-384932.mp3 |
| storage_take_all | https://pixabay.com/sound-effects/film-special-effects-heavy-rock-rolling-515254/ | dragon-studio-heavy-rock-rolling-515254.mp3 |
| guide_page_flip | https://pixabay.com/sound-effects/film-special-effects-flipping-book-page-499646/ | dragon-studio-flipping-book-page-499646.mp3 |
| timer_tick | https://pixabay.com/sound-effects/film-special-effects-tick-tock-94356/ | freesound_community-tick-tock-94356.mp3 |
| timer_end | https://pixabay.com/sound-effects/film-special-effects-generic-alarm-clock-86759/ | freesound_community-generic-alarm-clock-86759.mp3 |
| minimap_beep | https://pixabay.com/sound-effects/film-special-effects-digital-beep-151921/ | universfield-digital-beep-151921.mp3 |
| keycap_pop | https://pixabay.com/sound-effects/film-special-effects-keyboard-click-327728/ | creatorshome-keyboard-click-327728.mp3 |
| portal_activate | https://pixabay.com/sound-effects/technology-energy-charge-1-482503/ | yodguard-energy-charge-1-482503.mp3 |
| portal_warp | https://pixabay.com/sound-effects/film-special-effects-sci-fi-portal-jump-05-416165/ | dragon-studio-sci-fi-portal-jump-05-416165.mp3 |
| portal_switch | https://pixabay.com/sound-effects/film-special-effects-tuning-in-blip-201632/ | crigence-tuning-in-blip-201632.mp3 |
| gold_lever | https://pixabay.com/sound-effects/film-special-effects-clank1-91862/ | freesound_community-clank1-91862.mp3 |
| gold_reel_stop | https://pixabay.com/sound-effects/film-special-effects-money-counting-machine-sfx-406495/ | mubeenstudio-money-counting-machine-sfx-406495.mp3 |
| gold_coin_pop | https://pixabay.com/sound-effects/film-special-effects-coins-drop-1-404409/ | u_xg7ssi08yr-coins-drop-1-404409.mp3 |
