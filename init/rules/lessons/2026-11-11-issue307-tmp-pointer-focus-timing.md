# TMP 검색 클릭음은 PointerDown에서 포커스 상태를 판단한다
- 날짜: 2026-11-11
- 태그: ui, audio, TMP, search, pointer, testing, issue307
- 실수: 초기 구현 검토에서 PointerClick 시점의 isFocused로 검색창 최초 클릭을 구분하려 했다.
- 원인: TMP는 PointerDown에서 선택하고 다음 LateUpdate에서 입력을 활성화한다. 마우스를 누르고 있는 동안 포커스가 생기면 PointerClick에서는 첫 클릭도 이미 포커스된 상태다.
- 다음부터 할 일: PointerDown의 비포커스 상태를 사용하고 입력 활성화 전 연속 누름도 한 번만 처리한다. EditMode에서는 규칙을 격리하고, 실제 TMP 포커스·누름 유지·재클릭은 PlayMode의 마우스 입력으로 확인한다.
- 관련: #307, B-192, UiSearchSortClickSfx, PromptB192SearchSortClickSfxPlayModeTests.
