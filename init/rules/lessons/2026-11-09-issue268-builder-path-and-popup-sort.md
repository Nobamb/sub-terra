# 프리팹 경로를 옮긴 뒤 다른 빌더가 옛 경로·옛 행 스타일을 계속 씀
- 날짜: 2026-11-09
- 태그: UI, uGUI, 인벤토리, 빌더, TMP_Dropdown, issue268
- 실수: B-190 첫 커밋에서 `StackRows`를 `StackScroll/Viewport` 아래로 옮겼는데, 엔진 연료 행을 넣는 B-102 빌더(`PromptB102SealedGlyphBuilder.AddInventoryRareRow`)는 옛 경로 `PanelRoot/StackRows`를 찾았다. 또 옛 행을 복제만 하고 명세표 배치를 다시 입히지 않아, 이 빌더가 돌면 엔진 연료 행이 옛 스타일로 돌아갈 수 있었다. 정렬 드롭다운을 넣을 때도 인벤토리 창이 팝업 정렬(32100+) Canvas로 올라가 있어 TMP 기본 목록(30000)이 창 뒤에 그려져 보이지 않았다.
  드롭다운·검색 처리를 처음에 `InventoryPanelView.Update`로 넣어 인벤토리 UI 폴더의 폴링 금지 테스트(`InventoryPanelSources_DoNotPollInUpdate`)를 깨뜨렸다.
- 원인: 프리팹 자식 경로를 바꾸면서 `Editor/` 전체에서 그 경로를 쓰는 다른 빌더를 검색하지 않았다. 드롭다운은 프리팹만 보고 실제 런타임 Canvas 정렬 순서를 확인하지 않았다.
- 다음부터 할 일: 프리팹 자식을 옮기거나 이름을 바꾸면 `Editor/`·`Tests/`에서 옛 경로 문자열을 grep 하고, 행을 복제하는 빌더는 끝에 해당 레이아웃 빌더(`PromptB112InventoryPanelBuilder.ApplyTo`)를 다시 호출한다. 팝업 창 안의 TMP_Dropdown은 펼친 목록·차단막 정렬을 창 Canvas보다 높게 올리고(설정 메뉴 `RaiseDropdownLists`와 같은 방식), 실제 Play 화면에서 펼친 상태를 캡처해 확인한다. 인벤토리 UI에는 `Update`를 쓰지 않고 입력 이벤트(onSelect/onEndEdit)나 하위 클래스 재정의로 처리하며, 코드 작성 전 해당 폴더의 정적 구조 테스트를 먼저 읽는다.
- 관련: #268, #275, B-190, work_process/MVP2/ui/B-190-inventory-manifest-result.md
