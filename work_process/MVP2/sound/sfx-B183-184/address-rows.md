# init/sound-effect-address.md 반영할 줄 (B-183·B-184, 2026-11-02)

- `ui_slot_select` 줄(지금 "선정 전")은 아래 내용으로 **고친다**(B-183). 나머지는 표 맨 아래에 **붙인다**: B-183이 95~98 네 줄, B-184가 99~101 세 줄.

| 파일 이름 | 게임 안에서 쓰는 곳 | 받은 페이지 주소 | 받은 날짜 | 비고 |
|---|---|---|---|---|
| ui_slot_select | 메인 메뉴에서 세이브 슬롯을 고를 때 쇠 프레임 "딸깍" | https://pixabay.com/sound-effects/film-special-effects-steampunk-mechanical-gadget-188052/ | 2026-11-02 | Steampunk Mechanical Gadget (DavidDumaisAudio). 원본 0.020~0.320초, 끝 0.12초 페이드(1.27→0.30초, 모노, 최고점 -1dBFS). UI 폴더 |
| empty_slot_noise_loop | 메인 메뉴 빈 슬롯을 고르거나 마우스를 올린 동안만 TV 지직 노이즈(반복) | https://pixabay.com/sound-effects/film-special-effects-tv-static-noise-152056/ | 2026-11-02 | TV Static Noise (Universfield). 원본 10.1초부터 2.0초, 0.25초 교차 페이드로 이음새 없는 반복(15.05→2.00초, 모노, RMS -24dBFS). UI 폴더, Normalize 끔 |
| drone_priority_alert | Tab 드론 분석 창이 PRIORITY로 바뀌는 순간 경고음 한 번 | https://pixabay.com/sound-effects/film-special-effects-ui-warning-beep-515666/ | 2026-11-02 | UI Warning Beep (SoundShelfStudio). 원본 0~0.60초, 끝 0.08초 페이드(1.23→0.60초, 모노, 최고점 -1dBFS). UI 폴더 |
| drone_bubble_urgent | 드론 머리 위 말풍선의 급한 말(단순 알림, 예: 전력 부족) "삐빅" | https://pixabay.com/sound-effects/musical-ui-double-digital-beep-491835/ | 2026-11-02 | UI - double digital beep (YUSUF_SFX). 원본 0.080~0.310초, 끝 0.02초 페이드(1.23→0.23초, 모노, 최고점 -1dBFS). World 폴더 |
| drone_bubble_urgent_serious | 드론 머리 위 말풍선의 심각한 급한 말(비상·붕괴 위험·가스) 낮은 "삐빅" | https://pixabay.com/sound-effects/musical-ui-double-digital-beep-491835/ | 2026-11-02 | drone_bubble_urgent와 같은 원본·같은 자리. 두 음을 각각 7반음 낮춰 원래 박자에 다시 놓음(약 1975→1318Hz, 0.23초, 모노, 최고점 -1dBFS). World 폴더 |
| scan_mark_click | 드론 스캔 표시가 하나씩 나타날 때 작은 "찰칵"(가까운 것부터, 한 번에 몇 개만) | https://pixabay.com/sound-effects/film-special-effects-camera-click-569825/ | 2026-11-02 | camera-click (sTeVeMotion). 원본 0.088~0.188초, 끝 0.03초 페이드(0.50→0.10초, 모노, 최고점 -1dBFS). World 폴더 |
| gas_flow_loop | 가스가 있는 동안 "치익"(퍼지는 동안 0→최대, 사라지기 약 2초 전부터 작아짐, 반복) | https://pixabay.com/sound-effects/film-special-effects-gas-flow-411772/ | 2026-11-02 | Gas flow (spinopel). 원본 2.0초부터 4.0초, 0.5초 교차 페이드로 이음새 없는 반복(8.72→4.00초, 모노, 원래 크기 RMS -22.3dBFS). World 폴더, Normalize 끔 |
| unstable_block_shake_loop | 무너질 블록이 흔들리는 동안 우르르 반복 | https://pixabay.com/sound-effects/nature-earthquake-rumble-amp-cracking-379298/ | 2026-11-02 | Earthquake Rumble & Cracking (TheMediaGuy). 원본 1.7초부터 1.5초, 0.25초 교차 페이드로 이음새 없는 반복(7.11→1.50초, 모노, RMS -22dBFS). World 폴더, Normalize 끔 |
