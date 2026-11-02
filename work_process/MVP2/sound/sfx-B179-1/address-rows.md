# init/sound-effect-address.md 반영할 줄 (B-179-1, 2026-11-02)

- `settings_slider_drag` 줄은 아래 내용으로 **고친다**. 나머지 3줄은 표 맨 아래에 **붙인다**.

| 파일 이름 | 게임 안에서 쓰는 곳 | 받은 페이지 주소 | 받은 날짜 | 비고 |
|---|---|---|---|---|
| settings_slider_drag | 설정 음량 슬라이더가 방향키·스크롤 등으로 한 번 바뀔 때(짧게 한 번) | https://pixabay.com/sound-effects/film-special-effects-sci-fi-door-14782/ | 2026-10-31 | ui_scan_tick(Sci-Fi Door)을 늘린 B-179 소리(0.88초)를 B-179-1에서 앞 0~0.544초 + 끝 0.714초~를 10ms 교차 페이드로 붙여 0.70초로(모노, 최고점 -1dBFS). UI 폴더 |
| settings_slider_drag_head | 설정 음량 슬라이더를 잡고 끌기 시작할 때(시작 "탁"+첫 웅웅) | https://pixabay.com/sound-effects/film-special-effects-sci-fi-door-14782/ | 2026-11-02 | B-179 settings_slider_drag의 0~0.544초(떨림이 작은 곳의 0 교차점에서 자름, 0.544초, 모노, 최고점 -1dBFS). UI 폴더, Normalize 끔 |
| settings_slider_drag_loop | 설정 음량 슬라이더를 잡고 있는 동안 반복 | https://pixabay.com/sound-effects/film-special-effects-sci-fi-door-14782/ | 2026-11-02 | B-179 settings_slider_drag의 0.544~0.714초 "웅" 한 번(0.1696초, 7481샘플). 앞 10ms에 끝 다음 10ms를 섞어 이음새 없이 반복. UI 폴더, Normalize 끔 |
| settings_slider_drag_tail | 설정 음량 슬라이더에서 손을 뗄 때 끝 꼬리 | https://pixabay.com/sound-effects/film-special-effects-sci-fi-door-14782/ | 2026-11-02 | B-179 settings_slider_drag의 0.714초~끝(0.164초). 반복 조각 끝과 샘플이 그대로 이어짐. UI 폴더, Normalize 끔 |
