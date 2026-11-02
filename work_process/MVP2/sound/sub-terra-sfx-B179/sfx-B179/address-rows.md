# init/sound-effect-address.md 에 추가할 줄 (B-179 설정 슬라이더 드래그, 2026-10-31)

표 맨 아래(B-178 줄이 있으면 그 다음, 없으면 main_menu_type_tick 줄 다음)에 그대로 붙이면 됨.

| 파일 이름 | 게임 안에서 쓰는 곳 | 받은 페이지 주소 | 받은 날짜 | 비고 |
|---|---|---|---|---|
| settings_slider_drag | 설정 음량 슬라이더를 끄는 동안 한 번(드래그마다 한 번, 끝나도 다시 안 남) | https://pixabay.com/sound-effects/film-special-effects-sci-fi-door-14782/ | 2026-10-31 | ui_scan_tick(Sci-Fi Door)을 다시 다듬음. 앞 무음 0.088초 제거, 가운데 0.152~0.508초만 음 높이 그대로 0.55배속으로 늘림(0.67→0.88초, 모노, 최고점 -1dBFS, 이음새 8ms 교차 페이드). UI 폴더 |
