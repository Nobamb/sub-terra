# B-181 게임 중 창·월드 소리 채우기 결과 (기존 소리만)

## 기준 브랜치
- 기준: `MVP2-fix127` (`d2f54697 feat: 메인 메뉴 호버와 업그레이드·퀘스트 효과음을 고침 (프롬프트 B-180-1)`)
- B-178(`4f1bb898`) 포함, B-179·B-179-1·B-180(`22f2f5f5`)·B-180-1(`d2f54697`) 포함.
- 만든 브랜치: **`MVP2-fix128`** (계획 순서와 같은 번호. 원격에 fix128은 없었다)
- 새 음원·새 SfxId·칸 값·버스 음량 변경 없음. 경고(Alert) 버스 사용 없음.

## 새로 더한 `Play` 겹 (기존 호출은 그대로 1배)
| 위치 | 내용 |
|---|---|
| `Shared/Audio/SfxRequests.cs:13·29` | `ScaledPlayRequested(id, pitch, volumeScale)` 이벤트, `Play(id, pitch, volumeScale)`, `ClampVolumeScale`(0~1, NaN은 0) |
| `App/Audio/SfxPlayer.cs:121` | `SfxPlayer.Play(SfxId id, float pitch, float volumeScale)` |
| `App/Audio/SfxPlayer.cs:334` | `PlayNow(SfxId id, float pitch, float volumeScale)` — 버스 음량에 배율을 곱해 최종 음량에 반영. 기존 `PlayNow(id, pitch)`는 1배로 이 겹을 부른다. `LastVolumeScale`(검사용 읽기) |
| `App/Audio/WindowTvSfx.cs:51` | `WindowTvSfx.Play(SfxId id, float volumeScale)` — `source.volume`에 곱함. 기존 `Play(id)`는 1배. `LastVolumeScale`(검사용 읽기) |
- 배율이 있는 요청은 기존 `PlayRequested`가 아니라 새 `ScaledPlayRequested`로 간다. 기존 검사의 소리 횟수 기록에 섞이지 않는다.
- 멈춤 API는 더하지 않았다.

## A. 재사용 대기 창 (`App/UI/Outpost/FacilityCooldownPopupView.cs`)
| 순간 | 위치 | 소리 |
|---|---|---|
| 새로 켬(꺼져 있었거나·꺼지는 중이었거나·다른 대상) | `ShowFacilityCooldown` ~193 판정, ~229 `RequestOpenSfx` | `CooldownOpenSfx = WindowTvOpen(79)`, 배율 `CooldownTvVolume = 0.6` |
| 같은 대상 다시 E | ~167 조기 return | 무음 |
| 완료 빛 시작 | `Observe` ~387 `completeT = 0f` | `CooldownDoneSfx = UnlockGlow(78)`, 1배 |
| 꺼지기 시작 | `Hide`(~258, 멀어짐·다른 경로), `Tick` 완료 빛 뒤 ~321, `Observe` 만료 ~393 | `CooldownCloseSfx = WindowTvClose(80)`, 0.6배 |
- ★ 상수: 파일 맨 위 ~24-31 (`CooldownOpenSfx`, `CooldownCloseSfx`, `CooldownDoneSfx`, `CooldownTvVolume`, `CooldownOpenDelay = 0`)
- `CooldownOpenDelay`가 0보다 크면 켜짐음을 `Tick`의 elapsed 기준으로 늦춰 낸다(~303). 그 사이 꺼지면 버린다.
- 꺼짐음은 켜짐음을 낸 창에서만, 한 번만 난다(`closeSfxArmed`). 완료 빛 뒤에는 78 → 80 순서다.
- 무음: `OnDisable`·`ClearState`(무장 해제 ~835), `TurnOff`, 처음 빌드, 같은 대상 다시 E.
- 바인딩 해제(씬 전환·불러오기): `OutpostPanelPresenter.Unbind` ~66이 `HideFacilityCooldown(quiet: true)`를 부른다. 새 선택 인터페이스 `IFacilityCooldownQuietHide`(`IFacilityCooldownPopupView.cs`)를 `OutpostPanelView`·`FacilityCooldownPopupView`가 구현하고, 꺼짐 연출은 같지만 꺼짐음만 없다. 이 인터페이스가 없는 View는 기존 `HideFacilityCooldown`을 쓴다.
- 실패음(UiCancelFail)과 켜짐음은 같은 프레임에 겹친다. 기본값은 늦추지 않음(0)이다.

## B. 구조 순간이동
- 성공: `EmergencyRescueRuntimeController.TryRescue`에서 `service.TryRescue` 성공 뒤, `MoveToResolvedElevator` 직전에 `SfxPlayer.Play(RescueWarpSfx)` → `PortalWarp(30)` 한 번.
- 도착음 없음(PR #283 피드백 반영): 처음에는 0.35초 뒤 `ElevatorArriveDing(60)`을 냈지만, `portal_warp.wav`는 0~0.3초가 작게 차오르는 구간(RMS 0.02~0.05)이고 소리의 중심은 0.35~0.9초(RMS 약 0.25)여서 "딩"이 워프를 덮었다. 엘리베이터를 탄 것도 아니므로 구조 도착 "딩"과 `EmergencyRescueArrivalSfx.cs`를 지웠다.
  - 엘리베이터 문·지상 도착의 기존 "딩"은 그대로다. `EscapeArrive`(38)도 쓰지 않는다.
- 구조 버튼: `EmergencyRescuePopupView.AttachButtonSfx` ~362를 `UiSfx.Attach(rescueButton, SfxId.None, hover: true)`로 바꿨다. 결과음만 남는다.
- 실패음을 넣은 경로(`RescueFailSfx = UiCancelFail(5)`, `FailureFeedback` 키 `emergency.rescue`, 0.3초 묶음):
  1. 엘리베이터 못 찾음 ~257
  2. 표시 비용과 현재 비용이 다름 ~266
  3. 결제 실패(`service.TryRescue` false, 화물 바뀜 포함) ~279
- 무음 그대로: `HideAll`·`HideImmediate`·`ResetEpisode`. 팝업 닫힘 소리(B-168)는 그대로다.
- ★ 상수: `EmergencyRescueRuntimeController.cs` ~29-35.
- 드론 스캔 핑: `DroneScanPingSfx`가 `SecondsSinceLastPlayed(ElevatorArriveDing)`로 0.6초 안이면 빠진다. 도착음을 `SfxPlayer.Play`로 내므로 이 기록에 남는다(코드로만 확인).

## C. 시설 집기·이동 취소 (`App/Integration/GameplayBuildingPlacementBridge.cs`)
- 집기: `TryBeginMoveFromWorld` ~115, `TryBeginMoveAt` 성공 뒤 `MovePickupSfx = WorldHoloOpen(11)`, `MoveHoloVolume = 0.6`. 우클릭 집기와 "이동" 버튼 뒤 클릭 집기가 둘 다 이곳을 지난다.
- 취소: `CancelPreview(bool playSfx)` ~441. 시설을 **실제로 들고 있었을 때만**(`placementSystem.IsMoving`) `MoveCancelSfx = WorldHoloClose(12)`, 0.6배.
- 취소 경로는 이렇게 나눴다.
  - 소리 경로 `CancelPreview(true)`: 우클릭 취소 `HandleWorldRightClick` ~98, 카메라 없는 우클릭 ~234, ESC 취소 `UndergroundMenuController` ~156.
  - 건설창 닫기 경로: 닫기(X·토글)는 `BuildingMenuPresenter.CancelSelection` ~145를 지나 `IBuildingFacilityMovePort.CancelMove(true)`를 부른다. `BuildingMenuBinder.OnDisable`보다 이 경로가 먼저 실제 취소를 한다. 그래서 명세의 "건설창 닫기 → OnDisable" 대신 이곳에 소리를 붙였다.
  - 무음 경로: 인터페이스 `CancelPreview()`는 무음(`CancelPreview(false)`)이다. 바인딩 해제(`BuildingMenuPresenter.Unbind`), 선택 교체, `BeginMoveSelection` 안의 정리 ~84, `BuildingMenuBinder.OnDisable`(씬 전환·불러오기·탑승으로 꺼짐), 브리지 자신의 `OnDisable`(`ClearRuntimeSelection`)이 여기에 들어간다.
  - 대상 고르기 모드만 켜졌다가 취소하면 `IsMoving`이 false라 무음이다.
- `IBuildingFacilityMovePort`에 `CancelMove(bool playSfx)`를 더했다(구현은 브리지 하나뿐).
- 옮겨 놓기 성공(설치음)·놓기 실패(실패음)는 그대로다.
- ★ 상수: 파일 ~52-55.
- 명세에 없던 추가: ESC로 들고 있던 시설을 취소해도 플레이어 입력이므로 취소음을 낸다.

## D. 드론 말풍선 (`App/UI/Drone/DroneDialogueSocket.cs`)
- `SetDialogue` ~128, `SetCanvasVisible(true)` 직전 `TryPlayBubbleSfx`: `DroneBubbleSfx = WorldFacilityTagBeep(13)`, 피치 `DroneBubblePitch = 1.35`, 월드 버스 그대로(배율 없음).
- 생략(그 회차만, 늦춰 내지 않음):
  1. 드론 스캔 핑이 `DroneBubblePingGap = 0.3`초 안(`SfxPlayer.SecondsSinceLastPlayed(DroneScanPing)`)
  2. 마지막 비프에서 `DroneBubbleMinGap = 1.5`초 안
  3. 떠 있는 말풍선과 글자가 같음
  4. 바인딩·씬 시작 뒤 첫 대사, `Awake`·`SetVisible(false→true)`(바인딩·불러오기·이어하기) 뒤 `DroneBubbleStartupQuiet = 1.0`초 안, `UiPauseGate.IsHeld`, 광산 설정창 열림(`ControlPreferences.IsSettingsOpen`), 바인딩 전 말풍선(`boundVisible` false)
- **급한 대사 = 같은 비프**: `DroneUrgentBubbleSfx = WorldFacilityTagBeep`(★). B-183에서 새 음원으로 바꾼다.
- Tab 드론 분석 창(`DroneDialoguePanelView`)은 무음 그대로다. 구조 안내 대사(`ShowDroneReminder`)는 위 규칙대로 비프가 난다.
- ★ 상수: 파일 ~20-31.

## 테스트
- 새 EditMode `Tests/EditMode/App/Audio/PromptB181SfxIngameFillTests.cs` (26개): ★ 상수, 배율(SfxPlayer 0.6배·1배·자름, WindowTvSfx 0.6배), A 5개 + Presenter 연결, B 4개, C 5개, D 8개.
- 기대값을 바꾼 기존 검사: `PromptB158SfxFixTests.EmergencyRescue_RescueConfirmsAndCloseHoversOnly_SurvivesBind`를 `EmergencyRescue_RescueAndCloseHoverOnly_SurvivesBind`로 바꿨다. 이유: B-181에서 구조 버튼 확인음(UiConfirm)을 빼고 호버만 남겼다.
- 실행(에디터 열린 상태, 플래그 실행기):
  - B-181 + B-158 묶음: **45 통과 / 0 실패**
  - 전체 EditMode: **2336 통과 / 24 실패**. 24개 모두 `work_process/MVP2/B-172-verification/editmode-full.txt`의 기준 실패 목록에 있다. **새 실패 0개.** `PromptB162…NoAlertBus`는 이번 실패 목록에 없다(B-180에서 처리됨).
  - PlayMode: 바꾼 PlayMode 검사가 없어 돌리지 않았다.
- 전체 EditMode 실행이 범위 밖 파일을 건드렸다: `NotoSansKR-Regular_SDF.asset`, `InventoryPanel.prefab`, `MainMenuPanel.prefab`, `ViewSocket.prefab`, `Mine_Demo_Integration.unity`, `ProjectSettings/TimeManager.asset`, `work_process/MVP2/103-main-menu/*.png`. 커밋에는 넣지 않는다. 되돌리기(`git restore`)는 사용자 확인이 필요하다.

### 미검증
- 실제 플레이에서 들어 본 항목 전부(아래 가이드 1~12). 특히 다음은 귀로 확인해야 한다.
  - 실패음과 대기 창 켜짐음이 겹칠 때 뭉개지는지(`CooldownOpenDelay`)
  - 0.6배 크기
  - 말풍선 비프 피치 1.35가 이름표 비프와 구분되는지
- 구조 성공 뒤 워프 소리는 사용자가 실제 게임에서 들리는 것을 확인했다. 구조 직후 엘리베이터 홀로그램 켜짐음(`WorldHoloOpen`, 0~0.4초가 큼)이 워프 앞부분(0~0.3초 거의 무음)과 겹쳐 한 소리처럼 들릴 수 있다.

## 사용자 테스트 가이드
1. 전진기지 충전기를 쓰고 바로 다시 E를 누른다 → 실패음과 함께 대기 창이 작은 TV 켜짐 소리로 뜬다.
2. 창 앞에서 대기 시간이 끝날 때까지 기다린다 → 완료 빛과 함께 "반짝" 소리, 이어서 작은 꺼짐 소리가 난다.
3. 대기 창이 떠 있을 때 멀리 걸어간다 → 작은 꺼짐 소리가 한 번 난다. 보건소도 1~3번이 같다.
4. 광산에서 전력이 0이 되어 구조 창이 뜨면 구조 버튼에 마우스를 올린다 → 호버음. 누르면 확인음 없이 워프 소리 한 번이 나고 엘리베이터 앞으로 옮겨진다(도착 "딩" 없음).
5. 화물이 바뀌어 비용이 달라지는 등 구조가 안 되면 실패음이 난다(만들기 어려우면 건너뛰어도 됨).
6. 설치한 시설을 우클릭으로 집는다 → 작은 홀로그램 열림 소리.
7. 들고 있는 채로 우클릭하거나 건설창을 닫아 취소한다 → 작은 닫힘 소리. 다른 자리에 놓으면 예전처럼 설치음, 못 놓는 자리면 실패음.
8. "이동"을 눌러 고르기만 하다가 우클릭으로 취소하면 소리가 없다.
9. 광산에서 돌아다니며 드론 말풍선이 새로 뜰 때를 본다 → 높은 "삑" 한 번. 드론 스캔 "핑"과 같은 순간에는 겹치지 않는다.
10. 말풍선이 연달아 바뀌어도 "삑"이 빽빽하게 쌓이지 않는다. 이어하기로 광산에 들어온 직후에는 나지 않는다.
11. ESC 설정창을 연 동안에는 말풍선 "삑"이 나지 않는다.
12. 효과음 음량·음소거가 이번 소리에 다 적용되고, Unity Console에 새 빨간 오류·노란 경고가 없다. 바꿔 보고 싶은 소리·크기(★)가 있으면 PM에게 알려 준다.

## #158 대응 표 (보고서 안에만, 이슈는 고치지 않음)
| 항목 | 상태 | 비고 |
|---|---|---|
| 재사용 대기 창 켜짐·꺼짐·완료 | 구현 | 79/80 0.6배, 78 |
| 구조 순간이동 (D-2 위험·생존) | 구현 | 30 한 번(도착 딩 없음), 실패 5, 구조 버튼 확인음 제거 |
| 시설 이동 집기·취소 | 구현 | 11/12 0.6배, 들고 있을 때만 취소음 |
| 드론 말풍선 | 구현 | 13 피치 1.35, 급한 대사 = 같은 비프(B-183에서 교체) |
