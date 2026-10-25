# B-165 효과음 D-1 고침 (점프음 교체, 큰 착지·일반 획득음 음량, 탈출 뒤 이상한 소리, ESC 때 반복 소리 멈춤)

## 목표
- MVP2-fix105(B-164)를 직접 플레이해 보고 나온 효과음 문제 5가지를 고친다.
- 바뀐 음원 3개(jump, land_heavy, pickup_common)는 사용자가 `Assets/_Project/Audio/SFX/World/`에 같은 이름으로 덮어써서 넣는다. 설명과 출처는 `work_process/MVP2/sound/sfx-D1/README.md` 맨 아래 "2026-10-25 고침"에 있다. **음원은 고치지 않는다.**
- 기준: 최신 `MVP2-fix105`(b98957d0 이후)에서 새 브랜치를 따고 PR을 올린다.
- 필요하면 다른 에이전트(Codex 등)에게 나눠 맡겨도 된다. 결과는 한 PR로 모은다.

## 절대 규칙
1. SfxId 번호와 칸 수(64칸)는 바꾸지 않는다. 새 칸을 만들지 않는다.
2. 음량은 `SfxLibraryDefaults.cs`와 `Resources/SfxLibrary.asset` **두 곳 모두** 같은 값으로 고친다. .meta로는 음량을 바꿀 수 없다.
3. World 파일 가져오기 설정(모노+normalize)은 그대로 둔다. 새 wav가 아직 폴더에 없으면 음량만 고치고 "음원 미반영"이라고 보고한다.
4. 게임 로직, 입력, 연출 시간은 바꾸지 않는다. ESC 설정창이 게임을 멈추게 바꾸지 않는다(소리만 멈춘다).
5. 원인을 코드로 확인하지 못한 부분은 지어내지 말고 "미확인"으로 보고한다.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/` 기준)
- `work_process/MVP2/sound/B-164-sfx-d1-result.md`(직전 보고서)
- Scripts/App/Audio/SfxLibraryDefaults.cs(~155-183), Resources/SfxLibrary.asset
- Scripts/App/Integration/WorldLoopAudio.cs(~80 `isPaused`), PortalIdleLoopAudio.cs(~43 `isPaused`)
- Scripts/App/Integration/UndergroundMenuController.cs(~62-85, ~146-165), Scripts/Shared/ControlPreferences.cs(~14), Scripts/App/UI/MainMenu/ControlSchemePanel.cs(~54-58)
- Scripts/App/UI/EmergencyEscape/EmergencyEscapePanelBinder.cs(~92-96, ~255-261, ~296-330), Scripts/App/Integration/EmergencyEscapePortalRuntimeBridge.cs(~198-238, `TeleportPlayer` ~405)
- Scripts/Gameplay/Player/PlayerSurvivalController.cs(`TrackFall` ~480-521)

## 1. 점프음 교체 (칸 52 Jump)
- 파일 이름은 그대로 `jump.wav`(0.50초, 가벼운 "휙" 바람 소리). 칸 음량 0.25 → **0.4**. 주석 길이도 0.58 → 0.50으로 고친다.
- PM이 후보 A(Pixabay whoosh-end-384629)로 확정했다. 출처 줄은 `init/sound-effect-address.md`에 이미 고쳐져 있다.

## 2. 큰 착지음 음량 (칸 54 LandHeavy)
- 칸 음량 0.35 → **0.7**. 새 land_heavy.wav는 노트북 스피커에서도 들리게 중음을 키운 파일이다.
- 측정(모노+normalize 뒤 칸 음량 반영, 가장 큰 순간 크기): 이전 -17.4 LUFS → 새 약 -11 LUFS. 위험도 경보(-11.2)·실패음(-13.3)과 비슷하거나 조금 크다.

## 3. 일반 자원 획득음 음량 (칸 63 PickupCommon)
- 칸 음량 0.35 → **0.65**. 새 pickup_common.wav는 귀에 안 들리는 아주 낮은 저음(40~80Hz)을 빼고 획득음 부분을 키운 파일이다.
- 원인: 같은 프레임에 나는 BlockBreak(-20.6 LUFS, 1.06초)에 획득음 부분(약 -24.7 LUFS)이 묻혔다. SfxPlayer는 소리마다 따로 0.05초 제한을 두므로 같은 프레임이라 빠지는 것은 아니다. 코드 순서는 바꾸지 않는다.
- 엔진 연료(PickupRare 0.5, -13.6 LUFS)보다 조금 작게(약 -16 LUFS) 맞췄다.

## 4. 긴급 탈출 성공 뒤 "실패 같은" 소리
- 코드상 이동이 성공하면 실패음(UiCancelFail)은 나지 않는다(`ConfirmSelectedDestination` ~316-325 성공 분기). 성공 뒤 나는 소리는 아래 셋뿐이다.
  - PortalWarp(바로), **EscapeArrive(0.35초 뒤, `TickArriveSfx` ~255-261)**, 도착 지점에서 바닥으로 살짝 떨어질 때의 Land "툭"(B-164 새 소리. 엘리베이터 가운데나 거점 +1칸 위로 옮겨지기 때문)
  - EscapeArrive 음원 `escape_arrive`는 "sci-fi power down blip"이다. 전원이 꺼지듯 내려가는 소리라 실패음처럼 들린다. **이것이 원인일 가능성이 가장 크다.**
- 먼저 확인: 에디터에서 탈출을 해 보고, `SfxPlayer.PlayNow`에 개발용 임시 로그를 달아 성공 뒤 어떤 SfxId가 나는지 보고서에 적는다. 확인 뒤 로그는 지운다.
- 고침(기본): 성공 뒤 EscapeArrive를 **내지 않는다**(예약·`TickArriveSfx` 호출 제거). 칸·파일은 남긴다. `PromptB162SfxCStageTests`의 EscapeArrive 기대값도 같이 고친다.
- 로그에서 다른 소리(예: UiCancelFail, LandHeavy)가 나왔다면 그 경로를 고치고 이유를 보고한다. 착지 "툭"은 그대로 둔다.

## 5. ESC 설정창을 열어도 드릴·승강음이 계속 남
- 원인: 광산의 ESC는 `UndergroundMenuController.OpenSettings`(~146-156)로 설정창만 연다. `UiPauseGate`를 잡지 않고 시간도 멈추지 않는다. 그런데 `WorldLoopAudio`(~80)와 `PortalIdleLoopAudio`(~43)는 `UiPauseGate.IsHeld`만 일시정지로 본다. 그래서 설정창이 떠 있어도 반복 소리가 멈추지 않는다.
  - 엘리베이터는 설정창이 열려도 계속 올라가므로 승강음이 그대로 난다.
  - 드릴은 `PlayerMiningController`(~76)가 설정창이 열리면 채굴을 취소하므로 원래는 멈춰야 한다. 플레이에서 계속 났다면 그 이유도 확인해 보고한다.
- 고침: 두 곳의 정지 판정을 `UiPauseGate.IsHeld || ControlPreferences.IsSettingsOpen`으로 바꾼다. 설정창이 열리면 바로 멈추고, 닫히고 조건이 남아 있으면 다시 난다(페이드인 0.05초). 지상·메인 메뉴 설정창도 같은 플래그를 쓰므로 함께 적용된다.
- 원샷 효과음과 BGM은 건드리지 않는다.

## 손대지 말 것
- 다른 칸(1~51, 53, 55~62, 64~65)의 값과 클립, 버스 음량, 설정창 동작, 게임 일시정지 구조, 세이브 형식, 관련 없는 리팩터링
- escape_arrive 파일 교체(PM이 새 소리를 고르면 따로 한다), `FailureFeedback` 입구, 머리 위 노란 경고 문구
- 36~40m 블록 통과 버그(별도 이슈)

## 테스트
- EditMode 전체에서 기존 실패 20개 말고 새 실패가 없어야 한다.
- 새 EditMode 테스트 `PromptB165SfxD1FixTests`
  - Defaults: Jump 0.4, LandHeavy 0.7, PickupCommon 0.65. asset 값도 같음
  - 반복 소리: 설정창 열림(ControlPreferences.IsSettingsOpen = true)이면 멈춤, 닫히고 조건이 남으면 다시 남. 포탈 대기음도 같음. 테스트 끝에 플래그를 false로 돌려놓기
  - 탈출 성공: `ConfirmSelectedDestination` 성공 뒤 시간을 1초 흘려도 EscapeArrive·UiCancelFail 요청이 없다(가짜 port 사용)
- PlayMode는 이번에 추가한 테스트만 돌린다.

## 수동 확인 체크리스트 (보고서에 그대로 넣기)
- [ ] 점프하면 가볍게 "휙" 하는 바람 소리가 난다. 예전 "뿅" 소리가 아니다. 착지 "툭"과 겹쳐 거슬리지 않는다.
- [ ] 높은 곳에서 떨어져 체력이 깎이면 "쿠웅"이 위험도 경보만큼 또렷하게 들린다. "툭"과 같이 나지 않는다.
- [ ] 구리·철·리튬을 캐면 "와장" 소리 위로 획득음이 분명히 들린다. 엔진 연료 소리보다는 조금 작다.
- [ ] 긴급 탈출 포탈로 엘리베이터와 거점에 각각 이동해 본다. 워프 소리 뒤에 실패처럼 들리는 소리가 더 나지 않는다.
- [ ] 드릴로 캐는 중 ESC를 누르면 드릴 소리가 바로 멈춘다. 설정창을 닫고 계속 캐면 다시 난다.
- [ ] 엘리베이터가 올라가는 중 ESC를 누르면 승강음이 멈추고, 닫으면 아직 움직이는 중일 때 다시 난다.
- [ ] 포탈 옆에서 ESC를 누르면 포탈 대기음이 멈추고, 닫으면 다시 난다.
- [ ] 설정창의 효과음 음량·음소거가 바뀐 소리에도 그대로 적용된다. Console에 경고나 오류가 없다.

## #158 대응
- 새 줄 없음. D-1 줄(84, 88-90, 101)의 마무리 고침이다. #158 본문은 이 작업에서 고치지 않는다(PM이 고침).

## 완료 후
- PR 제목 예: `fix: 효과음 D-1 고침 점프·착지·획득음, 탈출 뒤 소리, ESC 반복 소리 (프롬프트 B-165)`. 커밋 메시지는 한국어 conventional 형식으로 쓰고, 브랜치는 지우지 않는다.
- 결과 보고서 `work_process/MVP2/sound/B-165-sfx-d1-fix-result.md`에 적을 것
  - 기준 커밋, 브랜치, PR 링크, 테스트 결과
  - 바꾼 칸 값 표(이전 → 새), 음원 반영 여부
  - 탈출 성공 뒤 실제로 난 SfxId 로그와 최종 원인, 고친 위치(파일·줄)
  - 드릴이 설정창에서 계속 났던 이유(확인됐으면)
  - 직접 들어 보지 못한 항목은 "미검증"으로 구분
  - 위 수동 확인 체크리스트
