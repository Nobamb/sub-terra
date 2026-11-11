# 열린 드롭다운의 재클릭은 차단막을 통해 검증한다
- 날짜: 2026-11-11
- 태그: ui, audio, dropdown, pointer, testing, issue307
- 실수: 열린 드롭다운 헤더 클릭을 본체에 직접 전달하는 검사만으로는 실제 마우스의 닫기 경로를 확인할 수 없었다.
- 원인: TMP는 펼친 목록 아래에 투명 차단막을 만들고, 헤더 재클릭과 바깥 클릭 모두 차단막에서 받아 닫는다.
- 다음부터 할 일: 실제 마우스로 헤더 재클릭과 바깥 클릭을 각각 검사한다. 차단막에서 헤더의 화면 영역을 판별해 필요한 소리만 요청하고, 가상 메서드를 확장할 때는 설치된 TMP의 정확한 시그니처를 먼저 확인한다.
- 관련: #307, B-192 후속 사용자 요청, InventorySortDropdown.CreateBlocker(Canvas), PromptB192SearchSortClickSfxPlayModeTests.
