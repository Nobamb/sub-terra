# B-165 효과음 D-1 고침 결과 (점프음 교체, 큰 착지·일반 획득음 음량, 탈출 뒤 소리, ESC 반복 소리 멈춤)

- 기준 커밋: `b98957d0` (MVP2-fix105, B-164)
- 브랜치: `MVP2-fix106`
- PR: 아직 없음(커밋·PR은 사용자 확인 후)
- 음원: 사용자가 `Audio/SFX/World/`에 덮어쓴 `jump.wav`(0.50s), `land_heavy.wav`(1.01s), `pickup_common.wav`(0.68s)를 그대로 사용(**음원 반영됨**). `.meta`는 바뀌지 않아 가져오기 설정(모노·normalize·DecompressOnLoad)이 그대로다.

## 바꾼 칸 값 (World 버스)

| 번호 | SfxId | 파일 | 이전 음량 | 새 음량 | 위치 |
|---|---|---|---|---|---|
| 52 | Jump | jump.wav (0.58s → 0.50s) | 0.25 | **0.4** | `SfxLibraryDefaults.cs:157`, `SfxLibrary.asset` id 52 |
| 54 | LandHeavy | land_heavy.wav | 0.35 | **0.7** | `SfxLibraryDefaults.cs:161`, `SfxLibrary.asset` id 54 |
| 63 | PickupCommon | pickup_common.wav | 0.35 | **0.65** | `SfxLibraryDefaults.cs:179`, `SfxLibrary.asset` id 63 |

- 에셋 값은 에디터에서 `SerializedObject`로 세 칸 `volume`만 바꿨다(빌더는 빈 칸만 채우므로 음량을 고치지 않는다). 64칸·번호·클립은 그대로.
- Jump 길이 주석을 0.58 → 0.50으로 고쳤다. land_heavy·pickup_common은 길이가 같아 주석은 그대로.

## 탈출 성공 뒤 소리

### 로그(에디터 확인)

`SfxPlayer.PlayNow`에 임시 로그를 달고, 임시 PlayMode 검사로 실제 흐름(Bootstrap → 새 게임(임시 경로 세이브) → Mine_Demo_Integration)에서 씬의 `EmergencyEscapePortalRuntimeBridge.TryOpenEscapePanel` → `EmergencyEscapePanelBinder.ConfirmSelectedDestination`으로 **엘리베이터**로 탈출했다. 확인 뒤 로그와 임시 검사·세이브는 지웠다.

고치기 전:

```
5.97 PortalActivate     (창 열림)
---- confirm ----
6.19 PortalWarp
6.56 EscapeArrive       (+0.37초, TickArriveSfx)
6.71 WorldHoloOpen      (+0.52초, ElevatorHologramPrompt.BeginShow: 엘리베이터 칸 안으로 옮겨져 "E 엘리베이터" 홀로그램이 뜸)
```

고친 뒤:

```
4.88 PortalActivate
---- confirm ----
5.09 PortalWarp
5.60 WorldHoloOpen
```

- UiCancelFail·LandHeavy는 나지 않았다. 이 확인에서는 Land "툭"도 나지 않았다(도착 지점과 바닥 높이 차가 작아서로 보임).
- **최종 원인**: 성공 0.35초 뒤 나는 `EscapeArrive`("sci-fi power down blip")가 전원이 꺼지는 소리라 실패음처럼 들림.
- **고친 위치**: `Scripts/App/UI/EmergencyEscape/EmergencyEscapePanelBinder.cs`
  - 성공 분기(299-300)에서 도착음 예약을 지우고 PortalWarp만 낸다.
  - 쓰지 않게 된 `arriveSfx`·`ArriveSfxDelay`·`ArriveSfx`·`TickArriveSfx()`와 `Update`·`OnDisable`의 호출을 지웠다. 칸 38(EscapeArrive)과 `escape_arrive.wav`는 남겼다.
- 참고: 엘리베이터 도착 뒤 `WorldHoloOpen`(엘리베이터 홀로그램 열림음)은 원래 있던 소리라 그대로 뒀다. 이것도 거슬리면 따로 정한다.
- **거점(전진기지 코어)으로의 탈출은 새 게임에 거점이 없어 로그로 확인하지 못했다(미확인).** 코드상 같은 성공 분기를 타므로 EscapeArrive는 나지 않는다.

## ESC 설정창 반복 소리

- 원인(코드·에디터 확인): 광산 ESC는 `UndergroundMenuController.OpenSettings`로 설정창만 연다. 에디터 확인에서 설정창을 열면 `ControlPreferences.IsSettingsOpen = True`, `UiPauseGate.IsHeld = False`였다. 반복 소리는 `UiPauseGate.IsHeld`만 정지로 봤으므로 멈추지 않았다.
- 고침: 정지 판정을 `UiPauseGate.IsHeld || ControlPreferences.IsSettingsOpen`으로 바꿨다.
  - `Scripts/App/Integration/WorldLoopAudio.cs:82` (드릴·엘리베이터 승강음)
  - `Scripts/App/Integration/PortalIdleLoopAudio.cs:45` (포탈 대기음)
  - 열리면 바로 멈추고, 닫힌 뒤 조건이 남으면 0.05초 페이드인으로 다시 난다. 지상·메인 메뉴 설정창도 같은 플래그(`ControlSchemePanel.OnEnable/OnDisable`)를 쓰므로 함께 적용된다. 원샷 효과음·BGM·게임 진행(엘리베이터 이동 등)은 바꾸지 않았다.
- **드릴이 설정창에서 계속 났던 이유: 미확인.** 코드상 `PlayerMiningController.Update`(76-83)가 설정창이 열린 동안 매 프레임 `CancelMining()`을 부르므로, 드릴 반복음은 고치기 전에도 0.08초 페이드아웃으로 멈춰야 한다. 에디터 확인에서는 검사 코드로 채굴을 시작하지 못해(`TryStartMiningInDirection` 실패) 설정창을 연 상태의 드릴 소리는 직접 보지 못했다. 이번 고침으로 설정창이 열리면 드릴 반복음도 바로 멈춘다.

## 테스트

- EditMode 전체: **2055 통과 / 19 실패**. 실패는 모두 이전부터 있던 묶음(B-162 BriefingClose 값, 건설 배치·IntegrationWiring, 패널 폭 B-32·B-35, HUD B-106·구조 상태, B-85·B-89·B-101·B-107·B-138, 판매 패널, 시설 타일맵)이다. B-164 기준(2050 / 20)보다 실패가 하나 적다. B-164 보고서에 20개 이름이 모두 적혀 있지 않아 어느 것이 통과했는지는 미확인. 새 실패 없음.
- 새 `PromptB165SfxD1FixTests` 4개 통과
  - `Volumes_DefaultsAndAssetMatch`: Jump 0.4, LandHeavy 0.7, PickupCommon 0.65(Defaults·asset 같음), 64칸
  - `WorldLoop_StopsWhileSettingsOpen_ResumesAfterClose`: 실제 정지 판정으로 설정창 열림 → 멈춤, 닫힘 → 다시(페이드인)
  - `PortalIdleLoop_StopsWhileSettingsOpen_ResumesAfterClose`
  - `Escape_Success_NoArriveOrFailSfxAfterOneSecond`: 가짜 port로 성공 뒤 Update 60프레임 동안 EscapeArrive·UiCancelFail 없음
  - 모든 검사는 끝에 `ControlPreferences.IsSettingsOpen = false`로 되돌린다.
- 고친 기존 검사
  - `PromptB164SfxD1Tests` 칸 표의 세 음량
  - `PromptB162SfxCStageTests`: `Escape_ArriveOnce035SecondsAfterWarp` → `Escape_WarpOnly_NoArriveAfterSuccess`(성공 뒤 도착음 없음), 실패 검사는 Update를 돌리는 방식으로 바꿈
- PlayMode: 이번에 남긴 PlayMode 검사가 없어 돌리지 않았다(확인용 임시 검사는 위 로그를 얻은 뒤 지움).
- 직접 들어 보지 않았다. 아래 체크리스트는 모두 **미검증**이다. 음량 측정값(LUFS)은 프롬프트에 적힌 PM 측정치를 따랐고 다시 재지 않았다.

## 수동 확인 체크리스트

- [ ] 점프하면 가볍게 "휙" 하는 바람 소리가 난다. 예전 "뿅" 소리가 아니다. 착지 "툭"과 겹쳐 거슬리지 않는다.
- [ ] 높은 곳에서 떨어져 체력이 깎이면 "쿠웅"이 위험도 경보만큼 또렷하게 들린다. "툭"과 같이 나지 않는다.
- [ ] 구리·철·리튬을 캐면 "와장" 소리 위로 획득음이 분명히 들린다. 엔진 연료 소리보다는 조금 작다.
- [ ] 긴급 탈출 포탈로 엘리베이터와 거점에 각각 이동해 본다. 워프 소리 뒤에 실패처럼 들리는 소리가 더 나지 않는다.
- [ ] 드릴로 캐는 중 ESC를 누르면 드릴 소리가 바로 멈춘다. 설정창을 닫고 계속 캐면 다시 난다.
- [ ] 엘리베이터가 올라가는 중 ESC를 누르면 승강음이 멈추고, 닫으면 아직 움직이는 중일 때 다시 난다.
- [ ] 포탈 옆에서 ESC를 누르면 포탈 대기음이 멈추고, 닫으면 다시 난다.
- [ ] 설정창의 효과음 음량·음소거가 바뀐 소리에도 그대로 적용된다. Console에 경고나 오류가 없다.

## #158 대응

- 새 줄 없음. D-1 줄(84, 88-90, 101)의 마무리 고침이다. #158 본문은 고치지 않았다.
