# B-158 효과음 A 단계 수정 (B-156 플레이 테스트 반영, #158)

## 목표
- B-156 플레이 테스트 결과를 고친다. 새 클립은 만들지 않는다.
  - 취소 버튼이 조용하고 Console에 "UiBack 칸에 클립이 없어" 경고가 뜨는 문제: UiBack을 완전히 없애고 취소는 UiClick으로 바꾼다.
  - 호버·X 버튼 소리가 안 나는 문제: A 화면 버튼에 UiSfx.Attach를 실제로 연결한다.
- 세이브 슬롯 선택에 전용 소리 칸을 만든다. 클립은 사용자가 나중에 고른다.
- CCTV 창 닫힘, 보건소·충전기 사용 불가 때 소리를 넣는다. 위험도 경보가 화면 오른쪽 위 위험도 표시(플레이어 위치 기준)와 같은 기준으로 울리게 한다.
- 기준: 최신 main(B-157까지 합쳐진 1a06b37d 이후)에서 새 브랜치를 딴다.

## 절대 규칙 (먼저 읽기)
1. SfxId 번호는 바꾸지 않는다. UiBack=3은 `[Obsolete]`로 남겨 비워 두고, 새 칸은 맨 끝(16)에 붙인다.
2. 소리 하나에 한 번만 난다. 코드가 이미 SfxPlayer.Play를 부르는 버튼에는 클릭음을 또 붙이지 않는다(호버만).
3. X·ESC로 창이 닫히면 UiPopupClose만 난다 (B-156 규칙). 닫기 버튼에 클릭음을 겹치지 않는다.
4. 게임 로직, 입력 순서, 연출 시간, 비용·쿨타임 판정은 바꾸지 않는다. 성공 시 소리(회복 성공 등)는 그대로 둔다.
5. 시간은 비스케일이다. UiPauseGate로 멈춰도 UI 소리는 난다.

## 먼저 읽을 코드 (sub-terra/Assets/_Project/ 기준)
- Scripts/Shared/Audio/SfxId.cs (UiBack = 3), SfxRequests.cs
- Scripts/App/Audio/SfxLibraryDefaults.cs: UiBack 칸(:35)
- Scripts/App/Audio/SfxPlayer.cs: `clock = () => Time.unscaledTime` (:44), TryResolve의 빈 칸 경고(:375~377)
- Scripts/App/Audio/UiSfx.cs: UiSfxButton.OnPointerClick는 비활성 버튼에 UiCancelFail을 낸다. Attach(click=None)이면 호버만 난다.
- Resources/SfxLibrary.asset: `- id: 3` 칸(:32~39, clip 비어 있음)
- Editor/DataValidation/PromptB156SfxLibraryBuilder.cs: AddMissingEntries가 기존 에셋에 새 칸을 붙인다.
- 저장소 루트 init/sound-effect-address.md: ui_back 줄(:11)
- 메인 화면: UI/MainMenu/MainMenuBinder.cs
  - OnSlotSelectedSfx(:139, UiClick), OnNewGameClicked·OnContinueClicked(UiConfirm)
  - OnOverwriteConfirm(:201, UiConfirm), OnOverwriteCancel(:207, **UiBack :210**)
- 메인 화면 버튼: UI/MainMenu/MainMenuView.cs, OnEnable(:105), 리스너 :113~119 (overwriteCloseButton도 OnOverwriteCancel)
- 지상 기지: UI/SurfaceBase/SurfaceBaseBinder.cs OnResetMineCancelled(**UiBack :450**)
  - SurfaceBaseView.cs: OnEnable(:145), resetMine 버튼(:157~161), 열림·닫힘 소리(:373, :392)
  - resetMineConfirmYesButton은 골드가 부족하면 비활성이다(:486).
- 회복 팝업: UI/Outpost/FacilityServicePopupView.cs. closeButton(:72), Hide가 UiPopupClose(:195)
- CCTV 팝업: UI/Outpost/CoreCctvPopupView.cs. closeButton(:77), OnEnable(:214), Hide(:284)에는 소리 없음
- 전력 바닥 팝업: UI/EmergencyRescue/EmergencyRescuePopupView.cs
  - rescueButton·closeButton은 코드로 만든다(:663~668). BeginClose가 UiPopupClose(:259). PanelView.Bind(:87)
- 시설 사용 불가: UI/Outpost/OutpostPanelPresenter.cs ToggleInteractionPanel(:67)
  - 전력망 미연결: TryGetPowerDisconnectedInteractionMessage → ShowFacilityWarning (:69~75)
  - 재사용 대기: TryGetFacilityCooldownMessage → TryShowFacilityCooldown, 실패하면 ShowFacilityWarning (:77~87)
  - 문구: Outpost/OutpostService.cs (TryGetPowerDisconnectedInteractionMessage :116, FormatFacilityCooldownMessage :198)
- 위험도
  - Gameplay/Structural/StructuralRiskFeedback.cs: OnRiskChanged(:42)가 PlayRiskSfx(:48)를 부른다. 이 값은 **광산 전체** 위험도라 초반에 "위험"에 머문다.
  - Integration/GameplayHazardStatusBridge.cs: ApplyStructuralStatus(:181)가 HUD용 **플레이어 위치** 상태를 만든다. 같은 상태면 일찍 return한다(:197 부근). playerTarget이 없으면 전체 위험도로 대신한다(:155~165).
- 테스트: Tests/EditMode/App/Audio/PromptB156SfxTests.cs, App/PromptB99FacilityCooldownTests.cs, App/Outpost/(OutpostPanelPresenterTests, CoreCctvPopupTests, FacilityServicePopupTests).cs, App/UI/PromptB135EmergencyRescueUiTests.cs

## 1. UiBack 제거 (칸 14개)
- MainMenuBinder.OnOverwriteCancel, SurfaceBaseBinder.OnResetMineCancelled는 `SfxId.UiClick`을 낸다.
- SfxId: `[System.Obsolete("B-158에서 제거. 번호 3은 비워 둔다")] UiBack = 3`. 코드 어디에서도 UiBack을 쓰지 않아 Obsolete 경고가 0이어야 한다.
- SfxLibraryDefaults의 UiBack 줄, SfxLibrary.asset의 `id: 3` 칸 8줄, init 문서의 ui_back 줄을 지운다. 다른 칸의 clip guid·음량은 그대로 둔다.

## 2. 세이브 슬롯 선택 전용 칸 (UiSlotSelect)
- `UiSlotSelect = 16` 추가. 파일 이름 `ui_slot_select`, 버스 UI, 폴더 Audio/SFX/UI/, 음량 0.8
- 클립은 비워 둔다. 빌더가 기존 에셋에 빈 칸으로 붙이고, 파일이 들어오면 이름으로 자동으로 채운다.
- SfxPlayer: 칸이 비어 있고 **대신 낼 칸**이 있으면 경고 없이 그 칸을 재생한다.
  - 대체 표는 SfxLibraryDefaults에 둔다: `UiSlotSelect → UiClick`. 다른 빈 칸은 지금처럼 칸마다 경고 한 번이다.
- MainMenuBinder.OnSlotSelectedSfx는 `UiSlotSelect`를 낸다.
- init 표에 줄 추가: `| ui_slot_select | 세이브 슬롯 선택 | | | 선정 전. 비어 있으면 ui_click으로 대신 난다 |`
- 결과: 칸은 14 + 1 = **15개**다.

## 3. A 화면 버튼에 UiSfx.Attach 연결
- UiSfx.Attach에 `bool failWhenDisabled = true` 인자를 추가한다 (Configure와 UiSfxButton에도). false면 비활성 버튼을 눌러도 무음이다.
- 붙이는 곳은 각 View의 OnEnable(또는 버튼을 코드로 만든 직후)이다. Attach는 중복을 막으니 다시 켜져도 괜찮다.

| 화면 | 버튼 | click | hover | 이유 |
|---|---|---|---|---|
| 메인 | settingsButton, quitButton | UiClick | O | 지금 소리 없음 |
| 메인 | continueButton, newGameButton | None | O | 바인더가 UiConfirm. 비활성 이어하기 클릭은 UiCancelFail |
| 메인 | overwriteConfirm/Cancel/CloseButton | None | O | 바인더가 UiConfirm/UiClick |
| 메인 | slotButtons | 붙이지 않음 | X | 슬롯은 2번 UiSlotSelect만 |
| 지상 | resetMineButton | None | O | 창 열림이 UiPopupOpen |
| 지상 | resetMineConfirmYesButton | None | O | 바인더가 결과음. **failWhenDisabled false** (골드 부족은 지금처럼 무음) |
| 지상 | resetMineConfirmNoButton | None | O | 바인더가 UiClick |
| 회복 팝업 | CloseButton | None | O | Hide가 UiPopupClose |
| CCTV | CloseButton | None | O | 4번에서 UiPopupClose |
| 전력 바닥 | RescueButton | UiConfirm | O | 비활성 클릭은 UiCancelFail |
| 전력 바닥 | CloseButton | None | O | BeginClose가 UiPopupClose |
- 설정창 안 버튼, 삭제 버튼, 업그레이드 창은 이번에 붙이지 않는다 (#158 D·E 몫).

## 4. CCTV 창 닫힘
- CoreCctvPopupView.Hide에서 `state = Exiting` 직후, `isActiveAndEnabled`일 때만 UiPopupClose 한 번 (FacilityServicePopupView.Hide와 같은 방식). HideImmediate는 무음

## 5. 시설 사용 불가 실패음
- OutpostPanelPresenter.ToggleInteractionPanel의 두 일찍 끝나는 분기(전력망 미연결, 재사용 대기)에서 `SfxPlayer.Play(SfxId.UiCancelFail)` 한 번
  - 쿨타임 팝업이 뜨든 3초 문구로 대신하든 한 번이다. 정상 사용(RequestCharge/RequestHeal → SetResult)은 건드리지 않는다.

## 6. 위험도 경보를 HUD 기준으로
- StructuralRiskFeedback.OnRiskChanged에서 PlayRiskSfx 호출과 메서드를 지운다. lastRisk도 쓰는 곳이 없으면 지운다.
  - ResolveRiskSound, ResolveAlarmRepeat, ResolveAlarmInterval, GetPitch, UpdateDangerBgm, 카메라 흔들림은 그대로 둔다.
- 새 순수 C# 클래스 `Scripts/App/Audio/StructuralAlarmGate.cs` (시계 주입, 기본 Time.realtimeSinceStartup)
  - `Evaluate(previous, next)` → RiskSound. ResolveRiskSound를 쓰고 아래 규칙을 더한다.
  - 같은 단계로 다시 오르면 그 단계의 마지막 경보 뒤 3초 안에는 무음 (단계마다 따로 잰다)
  - 내려갈 때 **previous가 CollapseImminent면 틱 없음** (붕괴로 내려간 경우). 다른 하강은 틱 한 번
  - 하강 틱도 3초 안에 또 나면 무음 (경계 칸을 오갈 때 반복 방지)
- GameplayHazardStatusBridge
  - 필드 `alarmLevel = Stable`과 gate를 둔다. ApplyStructuralStatus 맨 위, 일찍 return하기 **전에** 단계를 비교한다.
  - 단계가 다르면(원인 문구만 바뀌면 무음) gate 결과대로 `SfxRequests.PlayAlarm(AlarmMineWail, ResolveAlarmRepeat, ResolveAlarmInterval, GetPitch)` 또는 `SfxRequests.Play(AlertRiskTick)`
  - playerTarget이 없어 전체 위험도로 대신한 상태는 소리도 alarmLevel 갱신도 하지 않는다.
  - 씬에 새로 들어온 브리지의 첫 상태는 Stable에서 바뀐 것으로 친다. 그래서 위험 단계 저장을 이어하면 경보가 한 번 난다. OnEnable에서 alarmLevel을 되돌리지 않는다.

## 7. SfxPlayer 시계
- SfxPlayer.cs:44 `Time.unscaledTime` → `Time.realtimeSinceStartup`. 불러오기처럼 긴 프레임 중에 시작한 소리가 다음 프레임에 바로 끊기지 않게 한다. CreateForTests의 시계 주입은 그대로다.

## 손대지 말 것
- 클립 파일, 다른 칸의 음량·구간 값, 버스 음량, B-157 채널 음량 코드
- 월드 오브젝트 호버, 다른 창의 닫힘 소리, 슬라이더 틱, 카메라 흔들림, BGM 전체
- **마우스 커서 코드** (B-151: GameplayCursor*)
- 세이브 형식, 씬·프리팹 직렬화(SfxLibrary.asset 칸 삭제·추가만 예외)
- 관련 없는 리팩터링과 이름 바꾸기

## 테스트
- EditMode 전체 실행: 기존 실패 목록 외에 새 실패가 없어야 한다.
- PromptB156SfxTests 고치기
  - EmptyClipSlot: UiBack 대신 대체 칸이 없는 빈 칸(예: UiScanTick)으로 경고 한 번을 확인
  - FullPool의 ids 목록에서 UiBack을 UiSlotSelect로 바꾼다 (클립 있는 칸으로 추가)
  - Defaults: 15칸, 번호 중복 없음, UiBack 없음, UiSlotSelect 있음. 주소 문서에 ui_back 없음, ui_slot_select 있음
- 새 EditMode 테스트
  - UiSlotSelect가 비어 있으면 UiClick이 한 번 재생되고 경고가 없다 (LogAssert.NoUnexpectedReceived). 클립이 있으면 자기 소리가 난다.
  - Attach(failWhenDisabled:false) 버튼은 비활성 클릭에 무음, 기본값은 UiCancelFail
  - 메인·지상·회복·CCTV·전력 바닥 View를 켠 뒤 표의 버튼마다 UiSfxButton 설정(click, hover)이 맞다. 슬롯 버튼에는 없다.
  - CoreCctvPopupView: Show → Hide 시 UiPopupClose 한 번, Hide 두 번이어도 한 번
  - Presenter: 전력망 미연결·재사용 대기에서 Toggle 한 번에 UiCancelFail 한 번 (SfxRequests.PlayRequested 기록). PromptB99_Presenter_ShowsCooldownMessageWithoutOpeningPanel, ToggleInteractionPanel_DisconnectedCharger_… 기대값은 그대로 통과
  - StructuralAlarmGate: Stable→Caution/Danger/CollapseImminent 경보, 같은 단계 3초 안 재진입 무음·3초 뒤 경보, CollapseImminent→아래 틱 없음, Danger→Caution 틱
  - StructuralRiskFeedback 소스에 SfxRequests.PlayAlarm이 없다.
- PlayMode는 이번에 추가한 테스트만 실행한다.

## 수동 확인 체크리스트
실행 경로: Bootstrap → 메인 화면 → 새 게임/이어하기 → 지상 기지 → 광산.
- [ ] 메인 화면 슬롯을 누르면 지금은 "딸깍"(일반 클릭)이 난다. ui_slot_select 파일을 넣으면 그 소리로 바뀐다
- [ ] 메인 버튼(이어하기, 새 게임, 설정, 종료)에 마우스를 올리면 작은 호버 소리가 난다. 슬롯에는 호버 소리가 없다
- [ ] 덮어쓰기 창 취소·X, 광산 초기화 취소를 누르면 "딸깍"이 난다. Console에 UiBack 경고가 없다
- [ ] 골드가 부족할 때 꺼진 초기화 확인 버튼을 눌러도 소리가 없다
- [ ] 회복 팝업, CCTV 창, 전력 바닥 팝업의 닫기 버튼을 누르면 닫힘 소리만 한 번 난다
- [ ] 전력 바닥 팝업의 구조 버튼을 누르면 확인 소리가 난다
- [ ] 보건소를 쓴 뒤 바로 다시 E를 누르면 실패음 "삑"이 난다. 전력망에 안 이어진 충전기도 같다
- [ ] 광산에서 위험한 곳으로 걸어가면 주의 "웽" 1번, 위험 2번, 붕괴 임박 3번이고 점점 높다
- [ ] 위험한 곳에서 안전한 곳으로 걸어 나오거나 버팀목을 세우면 "띵"이 한 번 난다
- [ ] 붕괴 임박에서 천장이 무너져 위험도가 내려갈 때는 "띵"이 없다
- [ ] 경계 칸을 빠르게 오가도 경보가 3초 안에 반복되지 않는다
- [ ] 위험 칸에서 저장하고 이어하기로 들어오면 경보가 한 번 끝까지 울린다
- [ ] 커서, BGM 전환, 설정창 배경음·효과음 슬라이더가 이전과 같다

## 완료 후
보고 내용
- 기준 커밋, 변경 파일, 테스트 결과
- 15칸 표 (SfxId, 번호, 파일 이름)와 대체 표
- Attach를 붙인 버튼 목록 (클래스·필드)
- 경보 쿨타임 값 위치
- 남은 문제
직접 확인 못 한 항목은 "미검증"으로 적는다. 커밋은 사용자 확인 후에만 한다.
