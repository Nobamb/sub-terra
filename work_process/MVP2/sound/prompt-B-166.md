# B-166 효과음 D-1 두 번째 고침 (탈출 성공 뒤 실패음, 엘리베이터 이동 중 ESC)

## 목표
- MVP2-fix106(B-165)을 직접 플레이해 보고 남은 문제 2가지를 고친다.
  - A. 긴급 탈출 포탈로 **엘리베이터, 거점 둘 다** 이동에 성공했는데 워프 소리 뒤에 실패음이 난다.
  - B. 엘리베이터가 올라가는 중에 ESC를 눌러도 이동 소리가 계속 난다(드릴과 포탈 대기음은 설정창을 열면 바로 멈췄다). → 탑승 중에는 설정창을 못 열게 한다.
- 기준: 최신 `MVP2-fix106`(d662c376 이후)에서 새 브랜치를 따고 PR을 올린다.
- 필요하면 다른 에이전트(Codex 등)에게 나눠 맡겨도 된다. 결과는 한 PR로 모은다.

## 절대 규칙
1. **실제 입력 경로로 재현한 뒤 고친다.** `ConfirmSelectedDestination()`을 코드로 직접 부르는 확인은 인정하지 않는다. B-165가 그렇게 확인해서 원인을 놓쳤다. PlayMode에서 `InputTestFixture`(예: `ControlTerminalPlayModeTests`)로 마우스 클릭과 Enter 키를 실제로 눌러, EventSystem과 Input System을 타게 한다.
2. 다른 소리 칸의 값과 클립, 음량, 버스는 바꾸지 않는다. SfxId와 64칸도 그대로 둔다.
3. 원인을 로그로 확인하지 못한 부분은 지어내지 말고 "미확인"으로 보고한다.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/Scripts/` 기준)
- `work_process/MVP2/sound/B-165-sfx-d1-fix-result.md`, `B-164-sfx-d1-result.md`
- App/UI/EmergencyEscape/EmergencyEscapePanelBinder.cs(~182-201 `Close`, ~252-313 `Confirm`, ~341-349 `UiSfx.Attach`)
- App/UI/EmergencyEscape/EmergencyEscapePanelView.cs(~376-409 `BeginClose`, ~661-668 `SetWindowInteractable`, ~691-702 `RefreshConfirm`)
- App/Integration/EmergencyEscapePortalRuntimeBridge.cs(`TryEscapeTo` ~198-238, 안에서 `panelBinder.Close()`를 부른다)
- App/Audio/UiSfx.cs(~85-100 `OnClicked`, `OnPointerClick`)
- Gameplay/Mining/PlayerMiningController.cs(~153-162 Enter도 채굴 키), App/Integration/PauseInputSuspender.cs, App/Integration/MiningProgressHud.cs(~109-116)
- Gameplay/Player/ElevatorController.cs(~226-305 `RequestTravel`, `TravelRoutine`), ElevatorDoorVisual.cs(~206-251)
- App/Save/SaveRuntimeController.cs(~1035-1084 `TryElevatorTravel`, ~1163-1191 `LoadAfterClockOff`), App/Core/BootstrapContracts.cs(~63 `LoadScene`)
- App/Integration/IntegrationRuntimeBinder.cs(~879-888), WorldLoopAudio.cs(~82), UndergroundMenuController.cs(~62-74, ~146-156), App/UI/MainMenu/ControlSchemePanel.cs(~54-58)

## A. 탈출 성공 뒤 실패음
- 가장 유력한 원인(코드로 확인, 실제 클릭으로 재확인 필요): **마우스로 "이동" 버튼을 눌렀을 때**
  1. 같은 버튼 오브젝트에 `Button`과 `UiSfxButton`이 둘 다 클릭 이벤트를 받는다. `Button`이 먼저 붙어 있어 먼저 불린다.
  2. `Button.onClick`에서 이동하고, `TryEscapeTo` 안에서 `Close()`를 부른다. 그러면 `BeginClose`가 `windowGroup.interactable = false`로 만든다.
  3. 바로 다음에 같은 클릭으로 `UiSfxButton.OnPointerClick`이 불린다. 이제 버튼이 비활성으로 보이니까 "막힌 버튼을 눌렀다"고 판단해 `UiCancelFail`을 낸다.
  4. 엘리베이터와 거점은 같은 버튼, 같은 성공 경로라 둘 다 난다.
- 다른 후보(로그로 확인)
  - Enter로 이동했다면: Enter는 채굴 키이기도 하다. 닫기 연출(0.5초)이 끝나 채굴 입력이 다시 켜질 때 Enter가 아직 눌려 있으면, 도착한 자리 옆 칸(엘리베이터, 시설 같은 보호 칸)을 캐려다 `NotMineable` 실패음이 날 수 있다.
  - 그 밖에 엘리베이터 입력(E), 착지음 같은 것
- 먼저 확인: `SfxPlayer.PlayNow`에 개발용 임시 로그를 단다(SfxId, 프레임, `Environment.StackTrace`). 아래 4가지를 실제 입력으로 해 보고 로그를 보고서에 붙인다. 확인 뒤 로그는 지운다.
  - 엘리베이터: 마우스 클릭, Enter
  - 거점: 마우스 클릭, Enter. 테스트 준비에서 거점 코어를 하나 깔아 둔다(B-165는 거점이 없어서 확인하지 못했다).
- 고침(기본): `UiSfxButton`은 **그 클릭으로 onClick이 실제로 불렸으면** 실패음을 내지 않는다. 예를 들어 `OnClicked`에서 프레임을 기록해 두고, 같은 프레임의 `OnPointerClick`에서는 건너뛴다. 처음부터 비활성이던 버튼(흐린 목적지)을 누르면 지금처럼 실패음이 나야 한다.
- 로그에서 Enter 채굴 경로가 나오면 그것도 고친다. 기본 방법은 탈출 창이 닫힌 뒤 Enter를 한 번 뗄 때까지 채굴 입력으로 보지 않는 것이다. 다른 경로가 나오면 그 경로를 고치고 이유를 적는다.

## B. 엘리베이터 이동 중 ESC
- **결정(PM): 엘리베이터를 타고 있는 동안에는 설정창을 열 수 없다.** 탑승 연출은 몇 초뿐이고 지상 기지에서 바로 열 수 있으니 이 구간만 막는다. 엘리베이터를 기다리게 하거나 문 소리를 끊는 처리는 하지 않는다.
- 코드로 확인한 사실
  - 승강음은 `ElevatorController.State == Moving`일 때만 `WorldLoopAudio`가 낸다. 광산 이동 구간은 문 닫힘 → 객실 1.1~2.4초 상승 → 시계 끄기(최대 1초) → `SceneManager.LoadScene`으로 지상 씬이 열린다.
  - 지금은 이 사이에 ESC로 설정창이 열리고(`UndergroundMenuController` ~146-156), 문 소리(원샷)와 "딩"이 이어져 ESC가 안 먹힌 것처럼 들린다.
- 고침
  1. 탑승이 확정된 순간(`RequestTravel` 성공, Calling)부터 씬이 바뀔 때까지 설정창 열기를 막는다. ESC를 눌러도 아무 일도 없고 소리도 내지 않는다.
  2. 막는 조건은 한 곳에서 판정한다(예: `ElevatorController.IsRiding` 또는 런타임 상태). `UndergroundMenuController`의 ESC 경로와, 설정창을 여는 다른 경로(메뉴 버튼 등)가 있으면 모두 같은 조건을 본다. 찾은 경로를 보고서에 적는다.
  - 지상에서 "탐사 시작"을 누른 뒤 광산 씬으로 넘어가기 전에도 ESC로 설정창이 열리는 틈이 있으면(`SurfaceBaseBinder` ~208) 같은 규칙으로 막는다.
  3. 이동이 거절되거나 막혀서 탑승이 취소되면(Blocked, 경로 없음) 바로 다시 열 수 있어야 한다.
  4. 이미 설정창이 열린 상태에서는 E 탑승이 원래 막혀 있는지 확인하고 보고한다. 안 막혀 있으면 그대로 두고 "미확인 동작"으로 적는다.
- 확인 로그: 실제 키 입력으로 E 탑승 후 Calling·Moving 단계에서 ESC를 눌러 `elevator.State`, `IsSettingsOpen`, 씬 전환 프레임을 남긴다.

## 손대지 말 것
- 다른 칸의 값과 클립, 버스 음량, 설정창 UI, 게임 일시정지 구조, 엘리베이터 이동 시간·연출, `SfxPlayer`(특정 소리 끊기 추가 안 함), 세이브 형식
- escape_arrive 재사용, 홀로그램 소리, `FailureFeedback` 입구, 36~40m 블록 버그(#245)

## 테스트
- EditMode 전체에서 기존 실패 19개 말고 새 실패가 없어야 한다.
- 새 PlayMode 테스트 `PromptB166SfxFixPlayModeTests`(실제 입력, `InputTestFixture`)
  - 탈출: 엘리베이터와 거점 각각, 마우스 클릭과 Enter로 이동 성공 → 1.5초 동안 `UiCancelFail` 요청 0회. 흐린 목적지에서 버튼 클릭 → `UiCancelFail` 1회
  - 엘리베이터: Calling·Moving 중 ESC → 설정창이 열리지 않음(`IsSettingsOpen` false), 지상 씬으로 정상 이동. 이동이 거절되면 ESC로 다시 열림
- EditMode: `UiSfxButton` 같은 프레임 onClick 뒤에는 실패음 없음, 원래 비활성 버튼은 실패음 있음
- 테스트 끝에 `ControlPreferences.IsSettingsOpen = false`, `UiPauseGate`를 원래대로 돌려놓는다.

## 수동 확인 체크리스트 (보고서에 그대로 넣기)
- [ ] 긴급 탈출 포탈에서 **마우스로** "이동"을 눌러 엘리베이터로 간다. 워프 소리 뒤에 실패음이 나지 않는다.
- [ ] 같은 방법으로 거점으로 간다. 실패음이 나지 않는다.
- [ ] **Enter로** 엘리베이터, 거점에 각각 간다. 실패음이 나지 않는다.
- [ ] 골드나 전력이 모자라 흐려진 목적지에서 "이동"을 누르면 실패음이 난다(원래대로).
- [ ] 엘리베이터에서 E를 누르고 문이 닫히는 중, 올라가는 중에 ESC를 눌러 본다. 설정창이 열리지 않고 그대로 지상에 도착해 "딩"이 난다.
- [ ] 지상 기지에 도착한 뒤에는 ESC로 설정창이 정상적으로 열린다.
- [ ] 엘리베이터 출구가 막혀 탑승이 안 되면(실패음) 그 자리에서 ESC로 설정창이 열린다.
- [ ] (회귀) 드릴로 캐는 중 ESC → 드릴 소리가 멈추고, 닫고 계속 캐면 다시 난다.
- [ ] (회귀) 포탈 옆에서 ESC → 포탈 대기음이 멈추고, 닫으면 다시 난다.

## 완료 후
- PR 제목 예: `fix: 탈출 성공 뒤 실패음, 엘리베이터 이동 중 ESC 소리 (프롬프트 B-166)`. 커밋 메시지는 한국어 conventional 형식으로 쓰고, 브랜치는 지우지 않는다.
- 결과 보고서 `work_process/MVP2/sound/B-166-sfx-d1-fix2-result.md`에 적을 것
  - 기준 커밋, 브랜치, PR 링크, 테스트 결과
  - A: 4가지(목적지 2 × 입력 2)의 실제 입력 로그와 스택, 최종 원인, 고친 위치(파일·줄)
  - B: 설정창을 여는 경로 목록과 막은 위치(파일·줄), Calling·Moving 단계 ESC 로그
  - 직접 들어 보지 못한 항목은 "미검증"으로 구분
  - 위 수동 확인 체크리스트
