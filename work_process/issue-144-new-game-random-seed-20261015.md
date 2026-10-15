# #144 새 게임 랜덤 시드 — 2026-10-15

## 기준 및 상태

- Git 루트 C:/Users/jeone/sub-terra, Unity 프로젝트는 하위 sub-terra/ (6000.5.4f1).
- 기준 origin/main e4c352e2, 작업 브랜치 codex/new-game-random-seed.
- 현재 커밋·푸시·PR 게시 전. #185의 아이콘 변경을 이번 브랜치에 가져오지 않았다.
- 사용자 Clinic/Light/Settlement 데이터 에셋 3개는 기존 변경 그대로 보존하며 이번 범위에서 제외.

## 문제와 변경

기존 StartNewGame은 MineWorldCache를 비운 뒤 지상 기지를 저장했다. 첫 지하 진입 시 새 시드가 없어 Scene 생성기의 고정 시드 월드를 그대로 사용했다.

- MineResetService.TryCreateNewGameWorld: 기존 IMineResetSeedSource/유효 시드 재시도 로직으로 새 WorldSnapshotDto를 생성. 현재 DTO 생성기 버전 기본값 1 및 빈 변경 목록 사용.
- StartNewGame: 슬롯 덮어쓰기 승인 조건을 통과한 뒤 시드를 준비한다. 생성 실패 시 런타임/슬롯 변경 전에 실패. 새 상태 적용 후 캐시에 초기 월드를 넣어 지상 첫 저장에 포함.
- 초기화 비용 차감/초기화 횟수/타이머 초기화 이벤트/시설 쿨다운 삭제를 호출하지 않음. 새 게임과 유료·시간 초기화를 구분.
- 이어하기/재탐사는 기존 저장 시드+생성기 버전+변경점을 복원. 기존 슬롯을 로드할 때 새 시드를 발급하지 않음. 영구 ID/세이브 형식/기존 시드 없는 저장 처리 유지.

## 초기 시드가 덮이는 순서 문제 보완

TryElevatorTravel은 도착 자동 저장 코루틴을 먼저 시작한다. 이후 시작되는 탐사 복원 코루틴보다 저장이 먼저 재개되면 Scene의 기본 시드를 캡처해 유효 캐시를 덮을 수 있다. 시드 생성만 추가하면 이 경로에서 다시 고정 맵이 될 수 있다.

- 씬 로드/탐사 시작 이벤트 전에 pendingMineWorldRestore 설정.
- 복원 대기/진행 중 CaptureContext는 Provider를 캡처하거나 Mapper에 전달하지 않고 캐시 폴백만 사용.
- 동기 Restore 처리가 끝난 뒤 보호 플래그 해제. 탐사 진입이 거부되면 플래그 해제.
- 복원 대기 중 다시 진입 요청을 받으면 거부하되 기존 보호 상태는 유지.
- 기존 일반 탐사 저장/귀환 캡처 경로는 복원 완료 뒤 그대로 사용.

## 수정 파일 (sub-terra/Assets/_Project/ 기준)

- Scripts/App/Save/MineResetService.cs
- Scripts/App/Save/SaveRuntimeController.cs
- Tests/EditMode/App/Save/MineResetServiceTests.cs
- Tests/EditMode/App/Save/MineWorldCacheTests.cs
- Tests/EditMode/App/Save/SaveRuntimeWiringTests.cs

## 최소 검사 결과

- SubTerra.App 런타임 소스 경량 컴파일 성공.
- 변경한 검사 3개 파일의 소스 컴파일 성공(App.Tests.EditMode 전체 실행/컴파일로 보고하지 않음).
- 현재 DLL로 아래 순수 관리형 검사 3개 실행 PASS:
  - NewGameWorld_EachCreationUsesFreshSeedAndNoChanges
  - NewGameWorld_RetriesZeroSeedAndFailsWithoutUsableSource
  - NewGameWorld_RuntimeSourceProducesDifferentConsecutiveSeeds
- 추가 회귀 검사 소스:
  - 실제 임시 슬롯 JSON으로 지상 첫 저장→로드→새 캐시에 시드 유지 및 이전 채굴/시설 미혼입.
  - 복원 전 Provider가 있어도 저장 컨텍스트는 캐시만 사용, 연속 진입이 보호를 풀지 않음, 완료 후 Provider 캡처 복귀.
- 위 JSON/Unity 오브젝트 검사는 실제 NUnit/Unity에서 미실행. 새 게임→실제 지형 비교→완전 종료·이어하기까지 확인한 결과가 아님.
- 생성 csproj 목록 대신 실제 asmdef 소유 소스를 임시 컴파일 입력으로 사용. csproj/캐시 DLL은 변경하지 않음.
- diff 검사 통과. 임시 컴파일/관리형 실행 출력 정리.
- 씬/프리팹/이미지/.meta/ProjectSettings/Packages 변경 없음. 추가 Inspector 연결 불필요.

## 이어서 확정한 작업

사용자 답변으로 #148은 드릴2+드론 스캔2+가스 저항1, #157은 자원 정산기/전진기지로 확정했다. 표시 명칭은 추후 변경 가능하며 영구 ID는 유지한다.
구현 및 최소 검사 결과는 `work_process/issue-148-157-unlock-facility-names-20261015.md`에 기록했다.

- 사용자 승인 없이 팀원에게 메시지를 직접 발송하지 않았다.
