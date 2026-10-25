# B-166 효과음 D-1 두 번째 고침 결과 (탈출 성공 뒤 실패음, 엘리베이터 이동 중 ESC)

- 기준 커밋: `d662c376` (MVP2-fix106, B-165)
- 브랜치: `MVP2-fix107`
- PR: 아직 없음(커밋·PR은 사용자 확인 후. 이 PC에 `gh`가 없어 PR은 웹에서 만들어야 한다)

## 재현 방법(실제 입력)

- 프롬프트가 적은 `InputTestFixture`는 쓸 수 없었다. `Unity.InputSystem.TestFramework`는 `Packages/manifest.json`의 `testables`에 패키지를 넣어야 열리는데, 규칙상 manifest를 고칠 수 없다. (`ControlTerminalPlayModeTests`도 실제로는 InputTestFixture를 쓰지 않는다.)
- 대신 프로젝트에 있는 `UiTestEnvironment`(`Tests/PlayMode/Integration/Bootstrap/UiPlayModeTestSupport.cs`) 방식을 썼다. 가상 `Keyboard`·`Mouse`를 `InputSystem.AddDevice`로 붙이고 `QueueStateEvent`로 키·버튼 상태를 넣는다. 클릭은 `InputSystemUIInputModule` → `EventSystem` → `ExecuteEvents`를, Enter·E·ESC는 각 컴포넌트의 `Keyboard.current`·입력 액션을 그대로 탄다(아래 스택 참고). `ConfirmSelectedDestination()`을 코드로 부르지 않는다.
- 탈출 창은 `EmergencyEscapePortalRuntimeBridge.TryOpenEscapePanel`로 열고(포탈 진입·전력 준비 생략), 목적지 선택(↓)과 확인(클릭/Enter)은 실제 입력으로 했다. 거점은 카탈로그의 `building.outpost_core.basic` 런타임 프리팹을 플레이어 옆에 깔았다.
- 엘리베이터 성공 경로는 B-137과 같이 Bootstrap → 메모리 세이브(실제 세이브 파일 미사용)로 새 게임 → 지상 "탐사 시작" 실제 클릭 → 광산 → E 탑승으로 했다.
- 원인 로그: `SfxPlayer.PlayNow`에 임시 로그(SfxId, 프레임, `Environment.StackTrace`)를 달았다. 격리 런타임(슬롯 0)에는 `SfxPlayer`가 없어 탈출 검사의 요청은 `PlayNow`까지 가지 않았으므로, 같은 내용을 검사 쪽 `SfxRequests.PlayRequested` 구독에서도 남겼다. 확인 뒤 두 임시 로그는 모두 지웠다.
- 실행: 새 메뉴 `SubTerra/Tests/Run Prompt-B 166 Sfx Fix PlayMode Tests`(또는 `Temp/subterra-prompt-b166-playmode.flag`)로 이 묶음만 돌렸다.

## A. 탈출 성공 뒤 실패음

### 고치기 전 로그 (4가지 + Enter 길게)

| 목적지 | 입력 | 1.5초 동안 요청 | 결과 |
|---|---|---|---|
| 엘리베이터 | 마우스 클릭 | PortalWarp, **UiCancelFail** (같은 프레임 931) | 재현 |
| 거점 | 마우스 클릭 | PortalWarp, **UiCancelFail** (같은 프레임 2173), WorldFacilityTagBeep, UiScanTick, WorldHoloClose, Land | 재현 |
| 엘리베이터 | Enter(짧게) | PortalWarp | 없음 |
| 거점 | Enter(짧게) | PortalWarp, WorldFacilityTagBeep, UiScanTick, WorldHoloClose, Land | 없음 |
| 엘리베이터 | Enter 0.9초 누름 | PortalWarp(프레임 2785, 17.559s), **UiCancelFail**(프레임 2903, 18.068s, +0.51초) | 재현 |
| 거점 | Enter 0.9초 누름 | PortalWarp, WorldFacilityTagBeep, UiScanTick, WorldHoloClose, Land | 없음 |

마우스 클릭 UiCancelFail 스택(엘리베이터·거점 같음):

```
SfxRequests.Play  (Shared/Audio/SfxRequests.cs:23)
SfxPlayer.Play    (App/Audio/SfxPlayer.cs:107)
UiSfxButton.OnPointerClick (App/Audio/UiSfx.cs:98)
ExecuteEvents.Execute (IPointerClickHandler)
InputSystemUIInputModule.ProcessPointerButton (InputSystemUIInputModule.cs:731)
InputSystemUIInputModule.ProcessPointer / Process
EventSystem.Update
```

Enter 길게 UiCancelFail 스택:

```
SfxRequests.Play
SfxPlayer.Play
MiningProgressHud.OnProgressChanged (App/Integration/MiningProgressHud.cs:115)
MiningSystem.Publish / Fail (Gameplay/Mining/MiningSystem.cs:627, 621)
MiningSystem.TryStartMining (MiningSystem.cs:189)
MiningSystem.TryStartMiningFrom (MiningSystem.cs:245)
PlayerMiningController.TryStartPendingTarget (PlayerMiningController.cs:240)
PlayerMiningController.Update (PlayerMiningController.cs:130)
```

### 최종 원인

1. **마우스(엘리베이터·거점 둘 다, 주원인)**: 이동 버튼 한 오브젝트에 `Button`과 `UiSfxButton`이 붙어 있어 같은 클릭에서 `Button.OnPointerClick` → `onClick` → `ConfirmSelectedDestination` → `TryEscapeTo` → `panelBinder.Close()` → `BeginClose`가 창을 `interactable = false`로 만든다. 바로 뒤에 같은 클릭으로 `UiSfxButton.OnPointerClick`이 불려 "막힌 버튼을 눌렀다"고 보고 `UiCancelFail`을 냈다. 워프음과 같은 프레임이라 겹쳐 들린다. 프롬프트의 가장 유력한 원인과 같다.
2. **Enter를 닫기 연출(0.5초)보다 오래 누를 때(엘리베이터)**: 탈출 창이 `UiPauseGate`를 잡는 동안 꺼졌던 `PlayerMiningController`가 닫기 연출 뒤 다시 켜지면서, 아직 눌린 Enter(채굴 키)를 새 채굴 입력으로 읽고 엘리베이터 칸(보호 칸)을 캐려다 `NotMineable` 실패음을 냈다. 짧게 누르면 나지 않는다. 거점 도착 자리에서는 이번 배치(빈 공간)에서 실패하지 않아 나지 않았다(거점 옆에 캘 수 없는 칸이 있으면 같은 경로로 날 수 있다).

### 고친 위치

- `Scripts/App/Audio/UiSfx.cs`
  - 21: `clickedFrame` 필드
  - 88: `OnClicked`(onClick 리스너)에서 그 프레임을 기록
  - 97-102: `OnPointerClick`에서 같은 프레임에 onClick이 불렸으면 표시를 한 번 쓰고 실패음 없이 돌아간다. 처음부터 막힌 버튼(흐린 목적지)은 onClick이 불리지 않으므로 지금처럼 실패음이 난다.
- `Scripts/Gameplay/Mining/PlayerMiningController.cs`
  - 32-38: `SuppressEnterUntilReleased()` 추가
  - 164-171: Enter를 한 번 뗄 때까지 Enter를 채굴 키로 보지 않는다(방향키·마우스 채굴은 그대로).
- `Scripts/App/Integration/EmergencyEscapePortalRuntimeBridge.cs:234-239`: 탈출 성공(`TryEscapeTo`)에서 플레이어의 `PlayerMiningController.SuppressEnterUntilReleased()`를 부른다. 마우스로 이동했으면 Enter가 안 눌려 있어 첫 프레임에 바로 풀린다.

### 고친 뒤 로그

| 검사 | 요청 |
|---|---|
| 엘리베이터 마우스 | PortalWarp |
| 엘리베이터 Enter | PortalWarp |
| 엘리베이터 Enter 0.9초 | PortalWarp |
| 거점 마우스 / Enter | PortalWarp, WorldFacilityTagBeep, UiScanTick, WorldHoloClose, Land |
| 흐린 목적지 클릭 | UiCancelFail 1회 |

- 거점 도착 뒤 WorldFacilityTagBeep·UiScanTick·WorldHoloClose·Land는 거점 시설 태그·스캔·홀로그램·착지 소리로 원래 있던 소리다. 바꾸지 않았다.

## B. 엘리베이터 이동 중 ESC

### 설정창을 여는 경로

| 경로 | 위치 | 처리 |
|---|---|---|
| 광산 ESC | `UndergroundMenuController.Update`(68) → `HandleEscape`(76) → `OpenSettings` | 막음 |
| 광산 우측 메뉴 "설정(esc)" 버튼(`SettingsShortcut`) | 빌더 `PromptB89UndergroundMenuBuilder`가 `onClick`에 `UndergroundMenuController.OpenSettings`를 영속 연결 | 막음(같은 `OpenSettings`) |
| 지상 ESC / 설정 버튼 | `SurfaceBaseBinder.Update`(199) / `SettingsClicked` → `OnSettingsClicked` | 막지 않음(아래 로그로 틈 없음 확인) |
| 메인 메뉴 | `MainMenuBinder` | 해당 없음 |

### 막은 위치

- `Scripts/Gameplay/Player/ElevatorController.cs:70-73`: `IsRiding` = `Calling` 또는 `Moving` 또는 `travelCommitted`(이동 확정 뒤 씬 언로드까지). 이동이 거절·지연 로드 실패로 `Blocked`가 되면 `travelCommitted`가 풀려 false가 된다. 출구가 막혀 `RequestTravel`이 바로 `Blocked`가 되면 처음부터 false다.
- `Scripts/App/Integration/UndergroundMenuController.cs`
  - 150-151: `OpenSettings` 맨 앞에서 탑승 중이면 아무것도 하지 않고 돌아간다(소리 없음). ESC와 설정 버튼이 모두 여기를 지난다.
  - 170-180: `IsElevatorRiding()`이 씬의 `ElevatorController.IsRiding`만 본다(판정 한 곳).
- 닫기는 막지 않았다. 이미 열린 설정창은 탑승 중에도 ESC/X로 닫힌다.

### ESC 로그 (고친 뒤, 실제 E·ESC 입력)

```
board press E state=Idle hasRider=True
after E state=Calling frame=169
esc calling   frame=172 elevator=Calling runtime=Arrived settingsOpen=False flag=False scene=Mine_Demo_Integration
esc moving    frame=249 elevator=Moving  runtime=Arrived settingsOpen=False flag=False scene=Mine_Demo_Integration
esc committed frame=753 elevator=Moving  runtime=Moving  settingsOpen=False flag=False scene=Mine_Demo_Integration
surface scene active frame=817 settingsOpen=False
(지상 UI 준비 뒤 ESC → 설정창 열림 확인, 다시 ESC로 닫음)
```

이동 거절(슬롯 0 격리 런타임, 런타임이 이동을 거절):

```
after E state=Calling frame=1013
esc rejected-ride frame=1016 elevator=Calling runtime=Idle settingsOpen=False flag=False
rejected state=Blocked frame=1978
esc after rejected state=Blocked settingsOpen=True
```

고치기 전에는 같은 Calling 단계 ESC에서 `settingsOpen=True flag=True`였다.

### 지상 "탐사 시작" 뒤 틈

```
explore click frame=28 scene=SurfaceBase runtimeElevator=Arrived
explore+esc   frame=29 scene=Mine_Demo_Integration settingsOpen=True
```

- 클릭한 바로 다음 프레임에는 이미 광산 씬이라 지상 설정창이 열리는 틈은 없었다. 그래서 `SurfaceBaseBinder`는 고치지 않았다.
- 이 ESC는 광산 도착 첫 프레임에 읽혀 광산 설정창을 연다. 도착 쪽은 탑승 구간(Calling~씬 전환)이 아니라 프롬프트 범위 밖으로 보고 그대로 뒀다. 막아야 하면 따로 정한다.

### 설정창이 열린 상태에서 E 탑승

- 확인 로그: `probe settings open=True flag=True pauseHeld=False elevatorEnabled=True` → E → `state=Moving`.
- **막혀 있지 않다(미확인 동작, 그대로 둠).** 광산 설정창은 `UiPauseGate`를 잡지 않아 `PauseInputSuspender`가 엘리베이터 입력을 끄지 않고, `ElevatorController`는 `ControlPreferences.IsSettingsOpen`을 보지 않는다. 설정창을 연 채로 E를 누르면 탑승이 시작된다(설정창은 열린 채 이동, ESC로 닫기는 된다).

## 테스트

- 새 PlayMode `PromptB166SfxFixPlayModeTests`(`Tests/PlayMode/Integration/Traversal/`) 8개 통과
  - `Escape_Elevator_MouseClick_NoFailSfx`, `Escape_Elevator_Enter_NoFailSfx`, `Escape_Outpost_MouseClick_NoFailSfx`, `Escape_Outpost_Enter_NoFailSfx`: 이동 성공(골드 차감) 뒤 1.5초 동안 UiCancelFail 0회
  - `Escape_Elevator_EnterHeldPastClose_NoFailSfx`: Enter 0.9초 누름 뒤 UiCancelFail 0회
  - `Escape_DimmedDestination_MouseClick_PlaysFailOnce`: 골드 0으로 흐린 이동 버튼 클릭 → UiCancelFail 1회
  - `Elevator_EscWhileRiding_DoesNotOpenSettings_AndArrivesAtSurface`: Calling·Moving·이동 확정 단계 ESC → 설정창 안 열림, 지상 도착 뒤 ESC로 열림
  - `Elevator_TravelRejected_EscOpensSettingsAgain`: 탑승 중 ESC 막힘 → 거절(Blocked) 뒤 ESC로 열림
  - 고치기 전에는 마우스 2개·Enter 길게·엘리베이터 2개가 실패했다.
  - 끝에 `ControlPreferences.IsSettingsOpen = false`, `UiPauseGate.Release(emergency-escape-panel)`, 가상 장치·입력 설정 원복(`UiTestEnvironment.Dispose`).
- 새 EditMode `PromptB166SfxFixTests` 3개 통과: onClick이 같은 클릭에서 버튼을 막아도 실패음 없음 / 처음부터 막힌 버튼은 실패음 1회 / 표시는 한 번만 쓰임
- EditMode 전체: **2058 통과 / 19 실패**. 실패 19개는 B-165 기준 실행(2055 / 19)과 이름까지 같다. 새 실패 없음.
- 관련 PlayMode 회귀(`SubTerra/Tests/Validation` quick, 필터: ElevatorController·ElevatorHologramPrompt·EmergencyEscapePortal·PromptB105SideMenu·ElevatorRoundTrip·PromptB166): **30 / 30 통과**.
  - 처음 돌렸을 때는 Gameplay 엘리베이터·홀로그램 검사 7개가 실패했다. 따로 돌리면 12/12, B-166 묶음을 빼면 22/22 통과였다. 원인은 B-166 검사가 끝난 뒤 광산 씬(플레이어·엘리베이터)을 남겨, 뒤에 도는 검사의 엘리베이터가 남은 플레이어·이동 포트를 잡은 것이었다(제품 코드 문제 아님). B-166 TearDown에서 빈 씬을 만들고 나머지 씬을 내리도록 고친 뒤 30/30 통과했다.
- PlayMode 전체(daily)는 돌리지 않았다(미실행).
- 새 에디터 메뉴: `SubTerra/Tests/Run Prompt-B 166 Sfx Fix PlayMode Tests` (`Editor/DataValidation/PromptB166SfxFixTestRunner.cs`).

## 바꾸지 않은 것

- SfxId·64칸·클립·음량·버스, `SfxPlayer`, 설정창 UI, 일시정지 구조, 엘리베이터 이동 시간·연출, 세이브 형식, 씬·프리팹.

## 직접 들어 보지 못한 항목

- 아래 체크리스트는 모두 **미검증**이다(에디터 검사로 요청만 확인했고, 귀로 들어 보지 않았다).
- 광산 우측 메뉴 "설정(esc)" 버튼을 탑승 중에 누르면 설정창은 열리지 않지만, 그 버튼에 클릭음이 붙어 있다면 클릭음은 날 수 있다(미확인).

## 수동 확인 체크리스트

- [ ] 긴급 탈출 포탈에서 **마우스로** "이동"을 눌러 엘리베이터로 간다. 워프 소리 뒤에 실패음이 나지 않는다.
- [ ] 같은 방법으로 거점으로 간다. 실패음이 나지 않는다.
- [ ] **Enter로** 엘리베이터, 거점에 각각 간다. 실패음이 나지 않는다.
- [ ] 골드나 전력이 모자라 흐려진 목적지에서 "이동"을 누르면 실패음이 난다(원래대로).
- [ ] 엘리베이터에서 E를 누르고 문이 닫히는 중, 올라가는 중에 ESC를 눌러 본다. 설정창이 열리지 않고 그대로 지상에 도착해 "딩"이 난다.
- [ ] 지상 기지에 도착한 뒤에는 ESC로 설정창이 정상적으로 열린다.
- [ ] 엘리베이터 출구가 막혀 탑승이 안 되면(실패음) 그 자리에서 ESC로 설정창이 열린다.
- [ ] (회귀) 드릴로 캐는 중 ESC → 드릴 소리가 멈추고, 닫고 계속 캐면 다시 난다.
- [ ] (회귀) 포탈 옆에서 ESC → 포탈 대기음이 멈추고, 닫으면 다시 난다.
