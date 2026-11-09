# 검색 입력 차단을 전역 정지와 분리하고 한글 조합 문자열까지 검색한다
- 날짜: 2026-11-09
- 태그: inventory, search, audio, input, TMP, IME
- 실수: 인벤토리 검색창을 선택하면 기존 효과음이 끊겼고, 한글을 확정하기 전에는 검색 결과가 갱신되지 않았다.
- 원인: 단축키 차단에 전역 UiPauseGate를 사용했고, 검색은 TMP_InputField.text의 확정 문자열과 완성형 부분 일치만 확인했다.
- 다음부터 할 일: 검색은 입력 전용 소유권으로 월드 입력·단축키만 막는다. TMP 표시 문자열의 IME 조합과 초성·혼합 입력을 검증하고, 포커스 해제·창 닫힘·다른 일시정지와의 중첩도 확인한다.
- 관련: InventoryPanelView, InventoryManifestQuery, UiPauseGate, PauseInputSuspender, PromptB112InventoryPanelTests, PromptB112InventoryPanelPlayModeTests.
