# B-181 게임 중 창·월드 소리 채우기, 기존 소리만 (#158 D-2·E의 B-181 줄)

## 목표
- 2026-11-02 점검에서 남은 게임 중 무음 네 곳을 **이미 있는 소리로만** 채운다. 새 음원·새 SfxId는 없다.
  - A. 충전기·보건소 재사용 대기 창 켜짐·꺼짐·완료
  - B. 구조 비용을 내고 엘리베이터로 순간이동할 때 워프·도착, 구조 버튼 확인음 빼기
  - C. 우클릭으로 시설을 집을 때·이동을 취소할 때
  - D. 드론이 머리 위 말풍선으로 새 말을 할 때
- 소리 id·음량 배율·간격은 연결 지점마다 상수 하나로 둬서 PM이 숫자만 바꿔 끼울 수 있게 한다(아래 ★).
- 필요하면 다른 에이전트에게 나눠 맡겨도 된다. 결과는 한 브랜치로 모은다.

## 브랜치
1. `git fetch`로 원격 `MVP2-fixNNN` 중 가장 큰 번호를 확인한다. 계획 순서는 B-179(`fix124`) → B-179-1(`fix125`) → B-180(`fix126`) → B-180-1(`fix127`) → B-181(`fix128`)이다.
2. B-180-1 커밋(`… (프롬프트 B-180-1)`)이 들어간 `MVP2-fix127`이 있으면 그 위에서 **`MVP2-fix128`**을 만든다.
3. 없으면 `fix127`·`fix126`·`fix125`·`fix124`(`385b4a98`) 중 있는 것의 가장 최신 위에서 **비어 있는 다음 번호** `MVP2-fixNNN`을 만든다. 보고서 맨 위에 기준 브랜치·커밋과 "B-179-1/B-180/B-180-1 포함 여부"를 적고, 계획 순서와 번호가 어긋났으면 뒤에 돌릴 프롬프트가 어느 번호를 써야 하는지 적는다.
4. 기준에 B-178 커밋(`feat: 퀘스트 클리어·심층 해금 창, 보상 지급, 드론 스캔 효과음을 연결 (프롬프트 B-178)`, `4f1bb898`)이 없으면 **멈추고 보고한다.**

## 절대 규칙
1. SfxId 번호·칸 값·버스 음량·음원 파일은 바꾸지 않는다. 새 칸도 만들지 않는다. 경고(Alert) 버스는 쓰지 않는다.
2. App 코드는 `SfxPlayer.Play`, 창 TV 소리는 `WindowTvSfx`. 멈춤 API는 추가하지 않는다.
3. "작게"는 칸 음량을 바꾸지 않고 **재생할 때 배율**로 한다(79·80·11·12는 다른 곳도 쓰므로). 그 배율을 넘길 길이 없으므로 다음을 **더하기만** 한다. 기존 호출은 그대로 1배.
   - `SfxPlayer.Play(SfxId id, float pitch, float volumeScale)`(+ `PlayNow` 같은 모양). 0~1로 자르고 최종 음량에 곱한다.
   - `WindowTvSfx.Play(SfxId id, float volumeScale)`. `source.volume`에 곱한다.
4. **불러오기·이어하기, 초기화(Bind·OnEnable·씬 시작), 조용히 치우는 경로(HideImmediate·ClearState·OnDisable·씬 전환·탐사 실패·전력 회복으로 끝남)에서는 소리를 내지 않는다.** 플레이어가 지금 보고 있는 변화에만 낸다.
5. 씬·프리팹 YAML은 고치지 않는다. 연출 시간·모양, 재사용 대기 규칙, 구조 비용·결제, 시설 이동 규칙, 드론 대사 규칙, B-164~B-180-1 소리 연결은 바꾸지 않는다(아래 B의 구조 버튼 확인음만 예외).
6. 줄 번호는 fix123 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/Scripts/` 기준)
- #158의 `(B-181)` 줄들, `work_process/MVP2/sound/B-178-sfx-quest-drone-result.md`, B-180 보고서(있으면)
- App/Audio/SfxPlayer.cs(`Play` ~113, `PlayNow` ~306, `SecondsSinceLastPlayed` ~79), App/Audio/WindowTvSfx.cs(`Play` ~44)
- 검사 양식: Tests/EditMode/App/Audio/PromptB178SfxTests.cs, PromptB169…(WindowTvSfx 검사)

## A. 재사용 대기 창 (App/UI/Outpost/FacilityCooldownPopupView.cs)
| 순간 | 위치 | 소리 |
|---|---|---|
| 창이 켜지기 시작 | `ShowFacilityCooldown` ~158, 실제로 새로 켤 때(꺼져 있었거나 다른 대상). `sameTarget && targetOn` 조기 return(~167)은 무음 | `CooldownOpenSfx = WindowTvOpen(79)`★, `CooldownTvVolume = 0.6f`★ |
| 대기 시간이 끝나 완료 빛이 돌기 시작 | `Observe` ~314 `completeT = 0f` | `CooldownDoneSfx = UnlockGlow(78)`★ |
| 창이 꺼지기 시작 | `targetOn`이 true→false로 바뀌는 곳: `HideFacilityCooldown` ~215(멀어짐·다른 경로), `Tick` ~247 완료 빛 뒤, `Observe` ~321 | `CooldownCloseSfx = WindowTvClose(80)`★, 같은 배율 |
- 열릴 때는 `OutpostPanelPresenter.ToggleInteractionPanel` ~87의 실패음(UiCancelFail)이 같은 프레임에 이미 난다. 켜짐음은 작게 겹쳐 두되, 너무 뭉개지면 상수 `CooldownOpenDelay`(기본 0)로 조금 늦출 수 있게 둔다.
- 무음: `OnDisable`·`ClearState`·`TurnOff`(~750, 이미 꺼짐 연출이 끝난 뒤), 처음 빌드, 다시 같은 대상 E.
- 완료 빛 뒤 꺼짐은 78 뒤에 80이 이어지는 것이 맞다. 한 번 켜짐에 꺼짐음은 한 번.

## B. 구조 순간이동 (App/Integration/EmergencyRescueRuntimeController.cs, App/UI/EmergencyRescue/)
- `TryRescue` ~230에서 `service.TryRescue`가 성공한 뒤 `MoveToResolvedElevator` ~264 직전에 `RescueWarpSfx = PortalWarp(30)`★ 한 번.
- 도착: 상수 `RescueArriveDelay = 0.35f`★(긴급 탈출과 같은 간격, 실제 시간) 뒤 `RescueArriveSfx = ElevatorArriveDing(60)`★ 한 번. 기다리는 동안 씬이 바뀌거나(`OnDestroy`·`Unbind`), 탐사 실패가 되면 버린다. `EscapeArrive`(38)는 B-165에서 뺀 소리라 쓰지 않는다.
- 구조 버튼: `EmergencyRescuePopupView.AttachButtonSfx` ~364의 `UiConfirm`을 `SfxId.None, hover: true`로. 결과음만 남긴다.
- 결제 실패·비용이 바뀜·엘리베이터 못 찾음(~238-261)은 지금 문구만 뜬다. 버튼 확인음이 빠지면 무음이 되므로 `RescueFailSfx = UiCancelFail(5)`★ 한 번을 낸다(같은 이유 연타는 `FailureFeedback` 키로 0.3초 묶음).
- 무음: `HideAll`·`HideImmediate`·`ResetEpisode`(전력 회복), 팝업 닫힘 소리(B-168)는 그대로.
- 드론 스캔 핑은 B-178 규칙상 엘리베이터 "딩" 0.6초 안이면 자동으로 빠진다(확인만).

## C. 시설 집기·이동 취소 (App/Integration/GameplayBuildingPlacementBridge.cs)
- 집기: `TryBeginMoveFromWorld` ~99에서 `TryBeginMoveAt`이 성공한 뒤 `MovePickupSfx = WorldHoloOpen(11)`★, `MoveHoloVolume = 0.6f`★. 우클릭으로 집기·"이동" 버튼 뒤 클릭으로 집기 둘 다 이 한 곳을 지난다.
- 취소: **시설을 실제로 들고 있었을 때만**(`placementSystem.IsMoving`이 true였을 때) `MoveCancelSfx = WorldHoloClose(12)`★, 같은 배율.
  - 플레이어 입력 경로: 우클릭 취소 `HandleWorldRightClick` ~93, `~227`, 건설창을 닫아 취소(`BuildingMenuBinder.OnDisable` ~68 → `CancelPreview`).
  - 무음: 대상 고르기 모드만 켜졌다가 취소(아직 안 집음), `BeginMoveSelection` ~79 안의 정리, 씬 전환·불러오기·엘리베이터 탑승으로 꺼지는 `OnDisable`(필요하면 `CancelPreview(bool playSfx)`처럼 나눠서 구분하고 어떻게 했는지 적는다).
- 옮겨 놓기 성공(`OnBuildingMoved` 설치음)·놓기 실패(`OnPlacementRejected` 실패음)는 그대로.

## D. 드론 말풍선 (App/UI/Drone/DroneDialogueSocket.cs)
- `SetDialogue` ~68에서 말풍선을 실제로 띄울 때(억제·빈 글자 return 뒤, `SetCanvasVisible(true)` 직전) `DroneBubbleSfx = WorldFacilityTagBeep(13)`★, `DroneBubblePitch = 1.35f`★(시설 이름표 비프보다 높게). 월드 버스 그대로.
- 생략(그 회차만, 늦춰 내지 않음):
  1. 드론 스캔 핑이 `DroneBubblePingGap = 0.3f`★ 안에 났다(`SfxPlayer.SecondsSinceLastPlayed(SfxId.DroneScanPing)`).
  2. 마지막 말풍선 비프에서 `DroneBubbleMinGap = 1.5f`★ 안.
  3. 지금 떠 있는 말풍선과 글자가 같다(같은 대사를 두 경로 ~131·~224에서 다시 넣는 경우).
  4. 바인딩 직후·씬 시작 직후 첫 대사(초기화), 불러오기·이어하기 직후 `DroneBubbleStartupQuiet = 1.0f`★ 안, `UiPauseGate.IsHeld`, 광산 설정창 열림, 말풍선이 실제로 안 보이는 상태(`boundVisible` false·지상).
- 급한 대사(`IsUrgent`, PRIORITY): **지금은 같은 비프**를 낸다(상수 `DroneUrgentBubbleSfx = WorldFacilityTagBeep`★). B-183에서 새 음원으로 바꾼다. 보고서에 "급한 대사 = 같은 비프"라고 적는다.
- Tab 드론 분석 창(`DroneDialoguePanelView.SetDialogue`)은 B-183 몫이라 무음 그대로. 구조 안내 대사(`EmergencyRescueRuntimeController.ShowDroneReminder` ~326)는 위 규칙대로 비프가 난다.

## 손대지 말 것
- SfxId·SfxLibrary 칸, 음원, 쿨타임·구조·이동·드론 대사 규칙, 창 연출 시간표, B-178 드론 핑 생략 규칙, B-179·B-180·B-180-1 대상, `AGENTS.md`·규칙 문서

## 테스트
- EditMode 전체에서 기준 브랜치의 기존 실패 말고 새 실패가 없어야 한다. 기대값을 바꾼 검사(구조 버튼 확인음 검사 등)는 이름과 이유를 적는다.
- 새 EditMode `PromptB181SfxIngameFillTests`, PlayMode가 필요하면 `PromptB181SfxIngameFillPlayModeTests`(소리 요청을 잡는 방식은 B-178 검사를 따름)
  - 배율: 새 `Play` 겹이 0.6이면 음량 0.6배, 기존 호출은 1배, 0~1 밖은 자름
  - A: 새로 켤 때 79 한 번, 같은 대상 다시 0번, 완료 78 한 번 뒤 80 한 번, 멀어져 꺼짐 80 한 번, `OnDisable`·`ClearState` 0번
  - B: 성공 30 한 번 → 0.30초 0번 → 0.40초 60 한 번, 기다리는 중 `Unbind` 0번, 실패 5 한 번, 구조 버튼 클릭 None·호버 있음, `HideAll` 0번
  - C: 집기 11 한 번, 들고 있다 우클릭 취소 12 한 번, 고르기 모드만 취소 0번, 옮겨 놓기 설치음 그대로·12 없음
  - D: 새 대사 13(피치 1.35) 한 번, 핑 0.2초 뒤 0번, 1.0초 안 두 번째 0번, 같은 글자 0번, 첫 대사·정지 중 0번, 급한 대사 13 한 번
- PlayMode는 고친 검사만. 실행 전후 `git status`로 범위 밖 프리팹·폰트·씬·PNG·TimeManager 변경을 확인하고 되돌린다.
- 기존 실패 `PromptB162…NoAlertBus`는 B-180이 처리했으면 그대로, 아니면 기존 실패로만 적는다.

## 사용자 테스트 가이드 (보고서에 그대로 넣기)
1. 전진기지 충전기를 쓰고 바로 다시 E를 누른다 → 실패음과 함께 대기 창이 작은 TV 켜짐 소리로 뜬다.
2. 창 앞에서 대기 시간이 끝날 때까지 기다린다 → 완료 빛과 함께 "반짝" 소리, 이어서 작은 꺼짐 소리가 난다.
3. 대기 창이 떠 있을 때 멀리 걸어간다 → 작은 꺼짐 소리가 한 번 난다. 보건소도 1~3번이 같다.
4. 광산에서 전력이 0이 되어 구조 창이 뜨면 구조 버튼에 마우스를 올린다 → 호버음. 누르면 확인음 없이 워프 소리가 나고 엘리베이터 앞으로 옮겨진 뒤 "딩" 한 번.
5. 화물이 바뀌어 비용이 달라지는 등 구조가 안 되면 실패음이 난다(만들기 어려우면 건너뛰어도 됨).
6. 설치한 시설을 우클릭으로 집는다 → 작은 홀로그램 열림 소리.
7. 들고 있는 채로 우클릭하거나 건설창을 닫아 취소한다 → 작은 닫힘 소리. 다른 자리에 놓으면 예전처럼 설치음, 못 놓는 자리면 실패음.
8. "이동"을 눌러 고르기만 하다가 우클릭으로 취소하면 소리가 없다.
9. 광산에서 돌아다니며 드론 말풍선이 새로 뜰 때를 본다 → 높은 "삑" 한 번. 드론 스캔 "핑"과 같은 순간에는 겹치지 않는다.
10. 말풍선이 연달아 바뀌어도 "삑"이 빽빽하게 쌓이지 않는다. 이어하기로 광산에 들어온 직후에는 나지 않는다.
11. ESC 설정창을 연 동안에는 말풍선 "삑"이 나지 않는다.
12. 효과음 음량·음소거가 이번 소리에 다 적용되고, Unity Console에 새 빨간 오류·노란 경고가 없다. 바꿔 보고 싶은 소리·크기(★)가 있으면 PM에게 알려 준다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 재사용 대기 창·구조 순간이동·시설 집기·드론 말풍선 소리를 연결 (프롬프트 B-181)`). 새 브랜치에 일반 push, 브랜치는 지우지 않는다. PR은 만들지 않는다.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue158-주제.md` 파일 하나로 기록. **`init/rules/lessons.md`에 줄을 추가하지 않는다.**
- **GitHub 이슈 #158은 고치지 않는다.** 체크는 PM이 확인 뒤 한다. 보고서의 대응 표에만 적는다.
- 결과 보고서 `work_process/MVP2/sound/B-181-sfx-ingame-fill-result.md`
  - 기준 브랜치·커밋(B-179·B-180·B-180-1 포함 여부, 만든 번호와 이유), 소리마다 붙인 곳(파일·줄)과 SfxId·배율, ★ 상수 위치, 새로 더한 `Play` 겹
  - C 취소 경로를 어떻게 나눴는지, D 급한 대사 처리("같은 비프"), 구조 실패음을 넣은 경로
  - 테스트 결과, 직접 들어 보지 못한 항목은 "미검증", 위 사용자 테스트 가이드
  - **#158 대응 표**(보고서 안에만): 재사용 대기 창 / 구조 순간이동(D-2 위험·생존) / 시설 이동 집기·취소 / 드론 말풍선 각각 구현·일부·못 함
