# 한글 조합 표시와 키보드 확정 문자 경로를 함께 검증한다
- 날짜: 2026-11-10
- 태그: issue304, building, TMP, IME, InputSystem, search
- 실수: 조합 문자열 표시와 SetSearchQuery 검사만 통과해 확정된 키보드 문자가 입력값에 들어오지 않는 경로를 놓쳤다.
- 원인: TMP_InputField는 Event.PopEvent로 확정 문자를 처리하지만 Input System TextEvent가 그 경로로 전달되지 않으면 조합 문자열만 표시된다.
- 다음부터 할 일: 실제 InputSystem.QueueTextEvent로 확정 문자를 보내 검증한다. 보완 입력은 TMP가 이미 처리한 문자와 중복되지 않게 하며, 조합→확정→다음 조합, Backspace, 포커스 해제까지 검사한다.
- 관련: BuildingSearchInputField, PromptB110BuildingMenuPlayModeTests.BuildingSearch_CommittedKeyboardTextAndImeCompositionStayInSync, Builds/BuildingUi/baseline, Builds/BuildingUi/play.
