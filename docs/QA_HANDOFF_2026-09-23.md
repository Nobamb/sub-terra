# 데스크톱 작업 인계 — 2026-09-23

## 시작 위치

- 저장소: `C:\Users\jeone\sub-terra`
- Unity 프로젝트: `C:\Users\jeone\sub-terra\sub-terra`
- 작업 브랜치: `feature/player-idle-asset`
- 현재 HEAD: `1f6cdbd` — `병합: 최신 main 반영`
- 기준 main: `origin/main`의 `b812548`을 현재 HEAD가 포함한다.

`origin/feature/player-idle-asset`은 아직 `2934389 feat: 저장 슬롯 삭제 확인 기능 추가`에 있다. 아래의 미검증 수정은 커밋하거나 푸시하지 않았다.

## 현재 로컬 수정 — 검증 후 별도 커밋

### 1. 첫 채굴 중 NullReferenceException 방어

대상 파일: `Assets/_Project/Scripts/App/UI/Economy/EconomyPanelPresenter.cs`

광산 진입 뒤 이전 Surface Base의 `EconomyPanelView`가 파괴됐는데도 인벤토리 이벤트가 남아 있으면, 인터페이스 참조를 통한 UI 호출에서 예외가 날 수 있다. `TryGetLiveView`로 Unity의 파괴된 Object를 판별하고, 파괴된 View면 이벤트를 `Unbind`하도록 보완했다.

추가 테스트:

- `Assets/_Project/Tests/EditMode/App/Economy/EconomyPanelPresenterLifetimeTests.cs`
- Surface UI를 파괴한 뒤 인벤토리 추가 이벤트가 예외 없이 구독 해제되는지 확인한다.

### 2. Enter 채굴 입력 다중 장치 방어

대상 파일: `Assets/_Project/Scripts/Gameplay/Mining/PlayerMiningController.cs`

PlayMode 자동 검수에서 테스트가 만든 키보드가 `Keyboard.current`가 아닐 때 Enter 입력을 놓쳤다. `InputSystem.devices`를 순회하며 `device is Keyboard keyboard`로 키보드 장치만 확인하도록 바꿨다.

> Unity API 업데이터가 `Keyboard.all`을 `InputSystem.devices`로 바꾼 경우, 반드시 `InputDevice`를 순회한 뒤 `device is Keyboard keyboard` 형식으로 필터링한다. `foreach (Keyboard keyboard in InputSystem.devices)`는 컴파일되지 않는다.

## 이미 확인한 결과

- 최신 main 병합 후 Unity 스크립트 컴파일 성공. 경고 1건: `PlayerMovement.groundCheckRadius` 미사용(CS0414, 기존 경고).
- 새 QA Windows 빌드 생성 성공.
  - 실행 파일: `sub-terra/Builds/Windows-x64/Qa/20260922-145912/sub-terra/sub-terra.exe`
  - ZIP: `sub-terra/Builds/Windows-x64/Qa/20260922-145912/sub-terra-windows-x64-Qa-20260922-145912.zip`
- 격리된 새 게임 저장 검수: 화물 0, 미정산 가치 0, 인벤토리 수량 0, 이어하기 정상.
- 채굴·HUD PlayMode 검사: 25건 중 24건 통과.
  - 실패: `MiningSystemPlayModeTests.E_F03_KeyboardAndMouse_UseTheSameTimedCompletionPath`
  - 원인: `Keyboard.current`가 테스트에서 누른 키보드가 아닐 수 있음.
  - 위 Enter 입력 보완 후에는 아직 재검증 전.

## 아직 완료로 처리하면 안 되는 항목

1. Unity API 업데이터 적용 뒤 Console에 빨간 컴파일 오류가 없는지 확인.
2. 위 2개 로컬 수정의 EditMode/PlayMode 재검증.
3. 수정 반영 새 QA 빌드 생성.
4. 새 빌드의 실제 화면 검수와 새 `Player.log` 확인.
5. 통과한 수정만 별도 커밋·푸시.

이전 `Player.log`는 삭제된 옛 QA 빌드(`20260922-070019`) 기록이므로 이번 검수 증거로 사용하지 않는다.

## 데스크톱 재개 순서

1. Unity API 동의 창이 나오면 **`Yes, just for these files`**를 선택한다.
2. 임포트·컴파일이 끝난 후 Console에서 빨간 오류가 0개인지 확인한다.
3. `Window > General > Test Runner`에서 다음 테스트를 먼저 실행한다.
   - `MiningSystemPlayModeTests.E_F03_KeyboardAndMouse_UseTheSameTimedCompletionPath`
   - `EconomyPanelPresenterLifetimeTests.InventoryChange_AfterSurfaceViewDestroyed_UnbindsWithoutException`
4. 통과하면 `SubTerra > Tests > Run Play Mode Tests`를 실행한다.
5. `SubTerra > Phase P > Build Windows > QA`로 새 QA 빌드를 만든다.
6. 새 빌드에서 순서대로 검수한다.
   - 저장 슬롯 우상단 `×`와 삭제 확인
   - 빈 슬롯 새 게임의 화물·미정산 가치·자원 수량 0
   - 첫 블록 채굴 예외 없음
   - 채굴 → 인벤토리 → 판매 → 업그레이드
   - 사다리·엘리베이터·시설·HUD·우측 메뉴·드론
   - 36m 해금 → 38m 스캔 → 엔진 연료 → 전체 판매 제외 → 개별 판매
   - 종료 → 이어하기, 월드 변경과 엔진 연료 보존
7. 새 실행 뒤 `C:\Users\jeone\AppData\LocalLow\DefaultCompany\sub-terra\Player.log`만 확인한다.

## 보존 위치

- 병합 전 백업: `C:\Users\jeone\sub-terra\backups\before-main-sync-20260922-231650`
- 병합 전 stash: `stash@{0}` (`WIP: 최신 main 병합 전 작업 보관 20260922`)

이 백업과 stash는 검증·커밋이 끝나기 전까지 삭제하지 않는다.
