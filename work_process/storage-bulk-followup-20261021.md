# 2026-10-21 비연출 개선 — #216 / #212

## 작업 기준

- 저장소: C:/Users/jeone/sub-terra, 실제 Unity 프로젝트: C:/Users/jeone/sub-terra/sub-terra.
- Unity 6000.5.4f1. 앞선 작업 기준 main ee25ddab 위의 로컬 변경을 보존하고 진행했다. 이번 작업에서 최신 main을 새로 통합하지 않았다.
- 기존 #215/#209/#218/#210/#217/#219 수정 및 #211 재현 검사는 그대로 보존한다. 현재 변경은 미커밋이며 다른 PC에서 받으려면 게시가 필요하다.
- 요청 범위는 건설창 안내와 보관함 조작이다. 연출, ProjectSettings, 패키지, 저장 형식, 시설 이동 판정은 수정하지 않는다.

## #216 건설창 이동 안내 제거

- BuildingMenuView가 런타임에 생성하던 `이동 (우클릭)` 버튼과 생성 전용 코드를 제거했다.
- BuildingMenuBinder의 해당 버튼 연결만 제거했다. 월드 시설 우클릭·호버 안내·이동 상태 메시지·취소 경로는 유지한다.
- 버튼은 고정 위치에 덧붙이던 오버레이여서 목록/상세 영역에 예약 공간이 없었다. 프리팹을 재배치하거나 다시 저장할 필요가 없다.
- 게시 대상: Scripts/App/UI/Building/BuildingMenuView.cs, BuildingMenuBinder.cs, Tests/EditMode/App/UI/PromptB110BuildingMenuTests.cs.

## #212 보관함 일괄 이동

- 기존 수량 +1/+5/+10 옆에 `모두`를 추가했다. 누른 시점의 선택 자원 보유량·보관량 중 큰 값을 지정하고, 실제 이동은 기존 서비스가 출발지 수량으로 제한한다. 지속 토글 모드가 아니다.
- 자원 선택 없이 사용할 수 있는 `전체 보관`·`전체 꺼내기` 버튼을 추가했다. 소지/보관 자원이 없으면 각각 비활성화한다.
- 기존 팝업 톤·버튼 컴포넌트를 사용한다. 상태 줄 왼쪽에 일괄 버튼을 두고, 수량 조작 폭을 조정해 미리보기와 겹치지 않게 했다. 팝업 크기와 목록은 유지한다.
- 전체 보관/꺼내기는 시설 접근 가능 여부, 자원 ID, 수량·합계 오버플로를 전부 먼저 확인한다. InventoryService.TryReduceMany / TryAddManyExact를 재사용해 다종 이동을 한 번에 처리한다.
- 전체 꺼내기는 기존 단일 꺼내기처럼 전량 수용이 가능한 경우에만 처리한다. 화물 공간이 부족하면 어떤 자원도 옮기지 않고, 수량을 줄여 꺼내도록 안내한다. 임의 순서의 부분 꺼내기는 하지 않는다.
- 광물뿐 아니라 보관 가능한 희귀 자원도 포함한다. 기존 판매 제외 규칙을 보관에 적용하지 않는다.
- 처리 중·닫히는 중 입력과 중복 구독을 막는 기존 Presenter/Binder 수명주기를 따른다. 새로운 DTO나 저장 ID는 추가하지 않는다.
- 게시 대상: Scripts/App/Outpost/OutpostService.cs, Scripts/App/UI/Outpost/OutpostPanelPresenter.cs, OutpostPanelBinder.cs, StoragePopupView.cs, Tests/EditMode/App/Outpost/OutpostServiceTests.cs, Tests/PlayMode/Integration/Outpost/PromptB138StoragePopupPlayModeTests.cs.

## 최소 검사

- 안내 버튼 부재, 보유량과 보관량이 서로 다른 경우 및 한쪽 0, 전체 수량 보존·중복 실행, 화물 한도/오버플로/거리 실패의 무변경, 기존 JSON 저장 매핑·복원은 EditMode로 검사했다.
- 기존 단일 꺼내기 실패 원자성, 수량 미리보기, 한글 입력 보호 검사를 함께 선택했다. 실제 사용자 저장은 사용하지 않는다.
- 첫 실행은 선택 11개 중 10개 통과, 버튼 연결 검사 1개 실패였다. EditMode의 SendMessage 수명주기 제약과 기존 Picker의 지연 Destroy가 원인이었다. 관련 게임 코드를 바꾸거나 오류를 무시하지 않고 해당 검사를 PlayMode로 옮겼다.
- 실제 버튼→Binder→Presenter→Service 연결, 모두 수량 지정, 보관/전체 꺼내기, 처리 중·닫힘 중 입력 차단 및 버튼/미리보기 간격을 PlayMode 단독 1개로 재검사해 통과했다. 러너 ExitCode=0, 실패·건너뜀 0이다.
- 최종 결과: EditMode 10개 + PlayMode 1개 = 서로 다른 선택 항목 11개 통과. 기존 통과 항목은 반복하지 않았다. Unity 컴파일 오류 없음, diff 형식 검사 통과. 전체 스위트나 Windows 빌드 검사는 수행하지 않았다.
- 원시 결과: work_process/tools/storage-bulk-20261021/ 및 그 아래 playmode/. 실제 Windows 완전 종료·이어하기와 화면 육안 확인은 남아 있다.
- 팀원 확인: B 메뉴의 이동 버튼 부재와 월드 시설 우클릭 이동 유지, 보관함 모두/전체 버튼의 글자와 간격, 화물 한도 초과 시 안내 후 수량 유지. JSON 매핑 복원 검사는 실제 슬롯 완전 종료 검사를 대체하지 않는다.

## 이후

- #211 지상 업그레이드 지연은 기존 조건에서 미재현이다. 구매 항목·새 게임/이어하기/귀환 경로·구매 직후 예외가 확보되면 기존 기록을 따라 조사한다. 추측 수정은 하지 않았다.
- 새 이슈 게시 시 #216과 #212를 별도 PR로 나누고, 이전 미커밋 작업과 섞지 않는다. 기존 PR 후속 수정은 해당 PR에서 이어간다.


## 2026-10-21 게시 갱신
위의 미커밋·미게시 문구는 당시 기록이다. 현재는 독립 이슈 PR #225~#233으로 게시했으며 #233만 미재현 조사 초안이다. 최신 게시 목록과 데스크탑 실행 기준은 nonvisual-publication-handoff-20261021.md를 따른다.
