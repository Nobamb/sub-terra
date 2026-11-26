# ESC 입력은 한 창에서만 처리한다

- 날짜: 2026-11-26
- 태그: ui, input, shortcut, popup, localization
- 실수: 광산 메뉴와 긴급 탈출 창이 같은 ESC 입력을 각각 읽고 있었다.
- 원인: 설정 열기와 팝업 닫기가 서로 다른 입력 경로에 있었고 긴급 탈출 창도 자체적으로 ESC를 읽었다.
- 다음부터 할 일: 공용 입력 경로에서 맨 위 닫기 대상 하나를 선택하고, 제외 창은 정렬 목록에 그대로 둔다. 다국어 조작 문구와 기존 입력 검증도 함께 갱신한다.
- 관련: #397, #398, `UndergroundMenuController`, `EmergencyEscapePanelBinder`
