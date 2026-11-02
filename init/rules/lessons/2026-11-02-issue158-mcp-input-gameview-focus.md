# MCP로 넣은 마우스 입력은 Game 뷰 포커스가 없으면 버려진다
- 날짜: 2026-11-02
- 태그: unity-mcp, input-system, ugui, hover, audio, issue158
- 실수: B-180-1 메인 메뉴 호버 무음을 재현하려고 MCP에서 InputSystem.QueueStateEvent로 마우스를 옮겼지만, 결과가 실제 마우스 위치에 덮이거나 무시되어 원인을 오판할 뻔했다.
- 원인: 프로젝트 입력 설정이 PointersAndKeyboardsRespectGameViewFocus라서 Unity 창·Game 뷰에 포커스가 없으면 가상 장치 입력도 UI 모듈에 닿지 않는다. ExecuteEvents로 직접 보낸 진입 이벤트는 실제 입력 경로를 검증하지 못한다.
- 다음부터 할 일: 실제 포인터 경로는 EditorApplication.update 감시(하이라이트 전환·SecondsSinceLastPlayed)를 걸어 두고 사용자가 직접 호버하게 해서 확인한다. 재현되지 않으면 공통 코드를 추측으로 고치지 않고 확인한 경로와 미재현을 기록한다.
- 관련: #158, #271, B-180-1, MainMenuView.AttachButtonSfx, UiSfxButton.
