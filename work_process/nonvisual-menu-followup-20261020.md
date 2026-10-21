# 2026-10-20 비연출 조작 개선 — #210 / #217 / #219

## 기준·환경

- 최신 main ee25ddab 기준, 첫 번째 Unity 프로젝트 C:/Users/jeone/sub-terra/sub-terra, Unity 6000.5.4f1.
- 로컬 브랜치 codex/issue-215-structural-tile-visual. 앞선 #215/#209/#218 및 #211 검사 변경은 미커밋 상태로 보존했다.
- Unity 재실행을 줄이기 위해 이번 세 작업을 함께 검사한다. 게시할 때는 새 이슈별 PR로 분리한다. 아직 커밋·푸시·PR 등록하지 않았다.

## #210 메인 메뉴 커서

- GameplayCursorController.IsGameplayScene에 MainMenu를 포함했다. Bootstrap만 기존 OS 커서를 유지한다.
- 기존 드릴/집게 스킨·최상위 오버레이를 재사용한다. GraphicRaycaster 없음, raycastTarget/blocksRaycasts 비활성 규칙을 유지한다.
- 파일: Scripts/App/UI/GameplayCursor/GameplayCursorController.cs, Tests/EditMode/App/UI/PromptB151GameplayCursorTests.cs.

## #217 보상 창 X 닫기

- UndergroundMenuController.HandleCloseTopPopup의 최상위 창 처리에서 보상 창을 인식하지 못하고 반환하던 경로를 수정했다.
- DemoObjectiveView.TryCloseClaimPopup은 실제 최상위 창이 활성 보상 창에 속할 때만 기존 OnClaimConfirmClicked를 호출한다. 더 위의 다른 창을 건너뛰어 보상을 닫지 않는다.
- 닫기 연출 중 추가 X 입력은 소비하되 다시 지급 요청을 보내지 않는다. 실제 지급은 기존 Presenter/QuestRewardService를 그대로 사용하며 중복 요청 검사를 보강했다.
- 레이아웃·보상 수치·기존 마우스 닫기 동작은 변경하지 않았다.
- 파일: Scripts/App/Integration/UndergroundMenuController.cs, Scripts/App/UI/Tutorial/DemoObjectiveView.cs, Tests/EditMode/App/UI/PromptB89UndergroundMenuTests.cs, Tests/EditMode/App/Tutorial/PromptB95QuestRewardTests.cs.

## #219 빈 슬롯의 큰 시작 버튼

- Presenter가 선택 슬롯의 실제 적격성을 읽어 빈 슬롯은 활성화된 '새 탐사 시작', 정상·복구 가능 슬롯은 '이어하기'로 표시한다. 손상 슬롯의 큰 버튼은 비활성을 유지한다.
- 큰 버튼은 빈 슬롯일 때 기존 RequestNewGame 경로를 사용하고, 저장 슬롯일 때 기존 ContinueRequested 경로를 사용한다. 작은 새 게임 버튼의 덮어쓰기 확인은 유지한다.
- 삭제·덮어쓰기 확인 중에는 큰 버튼으로 확인을 우회할 수 없다. 삭제 완료 Refresh/슬롯 전환 때 문구·활성 상태를 다시 갱신한다.
- IMainMenuView에 SetPrimaryAction을 추가해 문구/클릭 가능 여부를 명시적으로 전달한다. 모든 구현과 테스트 대역을 함께 갱신했다.
- 버튼 위치·크기·이미지, 저장 형식, 실제 신규 슬롯 생성/이어하기 서비스는 변경하지 않았다.
- 파일: Scripts/App/UI/MainMenu/MainMenuPresenter.cs, MainMenuView.cs, Tests/EditMode/App/UI/MainMenu/MainMenuLogicTests.cs, PromptB150MainMenuTests.cs.

## 최소 검증·팀원 확인

- 커서 범위/입력 비차단, 최상위 보상 창 X 처리/기존 팝업 우선순위/중복 지급 방지, 빈 슬롯 시작·손상 슬롯 차단·삭제 후 갱신·덮어쓰기 보존에 관련된 EditMode만 선택했다.
- 첫 컴파일의 검사 파일 네임스페이스 누락을 수정했다. 첫 검사 실행은 12개 중 11개 통과했다. #217 검사는 EditMode에서 Awake가 실행되지 않아 기존 닫기 연출의 초기화가 빠진 상태로 실패했다. 런타임 코드를 우회하지 않고 Awake/OnEnable을 명시 호출하도록 검사 fixture를 수정했다.
- 2026-10-21 작업 재개 시 #217 단독 재검사 결과를 확인했다. 1/1 통과, 실패·건너뜀 0, ExitCode=0이다. 앞서 통과한 11개를 포함해 선택 항목 12개가 모두 최종 통과했다. 전체 테스트 스위트를 실행한 결과는 아니다.
- 로컬 원시 결과: work_process/tools/nonvisual-followup-20261020/final/ 및 claim-recheck/. 추가 Unity 실행 없이 기존 완료 결과를 확인했다.
- 검사 저장은 임시 폴더를 사용하고 사용자 저장 슬롯은 변경하지 않는다. Windows 빌드·전체 Demo·실제 화면 검사는 수행하지 않는다.
- 팀원 확인: Bootstrap→메인 메뉴 커서/클릭, 빈 슬롯과 저장 슬롯 왕복 선택→큰 버튼 문구·진입 슬롯, 보상 창에서 X 연타→보상 1회 지급.
- #216 시설 이동 안내 제거·#212 보관함 모두/전체 이동은 2026-10-21 후속 구현과 선택 검사까지 완료했다. storage-bulk-followup-20261021.md 참고. #211은 재현 정보가 확보되면 앞선 작업 기록을 따라 다시 조사한다. 연출·배경·썸네일 교체는 후순위다.


## 2026-10-21 게시 갱신
위의 미커밋·미게시 문구는 당시 기록이다. 현재는 독립 이슈 PR #225~#233으로 게시했으며 #233만 미재현 조사 초안이다. 최신 게시 목록과 데스크탑 실행 기준은 nonvisual-publication-handoff-20261021.md를 따른다.
