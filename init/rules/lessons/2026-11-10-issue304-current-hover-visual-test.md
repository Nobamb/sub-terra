# 호버 대상을 바꾼 뒤 현재 이름표를 검사한다
- 날짜: 2026-11-10
- 태그: issue304, building, PlayMode, Visual, hover
- 실수: 카드 프레임 화면 검사 중 시설 호버를 다른 버튼으로 옮기자 기존 Visual 검사가 실패했다.
- 원인: 시설별 이름표가 분리됐는데 검사는 첫 시설의 이름표 참조를 계속 사용했다.
- 다음부터 할 일: 호버 대상을 바꾼 뒤 View의 현재 RailHologram을 검사한다. 카드 겹침 캡처 분기도 실제로 실행한다.
- 관련: PromptB110BuildingMenuPlayModeTests.VerifyStatesAndInput, Builds/BuildingUi/shared-frame-visual.
