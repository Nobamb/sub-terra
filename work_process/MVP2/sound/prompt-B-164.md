# B-164 효과음 D-1 (이동·조작, 채굴·자원, 반복 소리 멈춤, #158 D-1)

## 목표
- #158 D-1의 소리를 B-156~B-163 효과음 시스템 위에 붙인다. 걷기·점프·착지·사다리·엘리베이터·드릴·블록 파괴·자원 획득·화물 80% 경고, 그리고 반복 소리 멈춤 규칙이다.
- 새 소리 15개는 사용자가 `Assets/_Project/Audio/SFX/World/`에 넣는다. 설명과 출처는 `work_process/MVP2/sound/sfx-D1/README.md`에 있다. **음원은 고치지 않는다.**
- 기준: 최신 `MVP2-fix103`(38da5c09 이후)에서 새 브랜치를 따고 PR을 올린다.
- 필요하면 다른 에이전트(Codex 등)에게 나눠 맡겨도 된다. 결과는 한 PR로 모은다.

## 절대 규칙
1. SfxId 번호는 바꾸지 않는다. 새 칸은 50(PortalClose) 뒤 51부터 붙이고, 3번은 계속 비워 둔다.
2. wav가 위 폴더에 없으면 지어내지 말고 멈춘 뒤 보고한다(38da5c09에는 아직 없다). `drill_loop_fwdrev.wav`가 있으면 쓰지 않는다. 칸을 만들지 말고 파일과 .meta를 지운다.
3. 새 소리는 모두 World 버스에 둔다. 빌더가 `FolderOf(bus)`로 폴더를 찾는다. Alert 버스는 목소리가 하나라 위험도 경보가 끊기므로 쓰지 않는다.
4. 원샷은 Gameplay 코드에서 `SfxRequests.Play`, App 코드에서 `SfxPlayer.Play`로 낸다. Gameplay 어셈블리는 App을 참조하지 못한다. SfxPlayer 규칙(같은 소리 0.05초에 한 번, 동시 8개, ignoreListenerPause)은 그대로 둔다.
5. 반복 소리(드릴, 엘리베이터 승강)는 원샷 풀을 쓰지 않는다. `PortalIdleLoopAudio`처럼 따로 AudioSource를 둔다. 게임 로직, 입력, 연출 시간은 바꾸지 않는다.
6. 실패음은 `UiCancelFail`을 직접 낸다. `FailureFeedback` 입구와 머리 위 노란 경고 새 문구는 이번에 만들지 않는다.
7. 위치를 못 찾거나 규칙대로 안 되면 지어내지 말고 보고서에 "못 찾음"으로 적는다.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/` 기준)
- Scripts/Shared/Audio/SfxId.cs, SfxRequests.cs, Scripts/App/Audio/SfxLibraryDefaults.cs(`LoopFileNames` ~31), SfxPlayer.cs, SfxPlayback.cs(`SfxThrottle`)
- Scripts/App/Integration/PortalIdleLoopAudio.cs(반복 소리 예), Scripts/App/UI/UiPauseGate.cs
- Editor/DataValidation/PromptB156SfxLibraryBuilder.cs(반복 파일은 ~132에서 CompressedInMemory)
- `work_process/MVP2/sound/sfx-D1/README.md`, 저장소 루트 `init/sound-effect-address.md`, #158 "구현 메모"

## 1. 새 칸 (모두 World 버스)
| 번호 | SfxId | 파일 | 길이 | 시작 음량 |
|---|---|---|---|---|
| 51 | Footstep | footstep.wav | 0.39 | 0.65 |
| 52 | Jump | jump.wav | 0.58 | 0.25 |
| 53 | Land | land.wav | 0.55 | 0.45 |
| 54 | LandHeavy | land_heavy.wav | 1.01 | 0.35 |
| 55 | LadderGrab | ladder_grab.wav | 0.20 | 0.9 |
| 56 | LadderClimb | ladder_climb.wav | 0.50 | 0.35 |
| 57 | LadderClimbB | ladder_climb_b.wav | 0.50 | 0.35 |
| 58 | ElevatorDoor | elevator_door.wav | 1.58 | 0.45 |
| 59 | ElevatorMoveLoop | elevator_move_loop.wav | 5.5 반복 | 0.15 |
| 60 | ElevatorArriveDing | elevator_arrive_ding.wav | 1.35 | 0.45 |
| 61 | DrillLoop | drill_loop.wav | 1.85 반복 | 0.35 |
| 62 | BlockBreak | block_break.wav | 1.06 | 0.3 |
| 63 | PickupCommon | pickup_common.wav | 0.68 | 0.35 |
| 64 | PickupRare | pickup_rare.wav | 0.81 | 0.5 |
| 65 | CargoHeavyWarning | cargo_heavy_warning.wav | 0.35 | 0.2 |
- `SfxLibraryDefaults.Create()`에 표 순서로 추가하고 주석에 길이를 적는다. 요약 주석의 칸 수(49 → 64)도 고친다. `LoopFileNames`에 `elevator_move_loop`, `drill_loop`를 넣는다. Fallbacks는 바꾸지 않는다.
- 빌더(SubTerra/Audio/Create Or Refresh Sfx Library)를 돌려 `Resources/SfxLibrary.asset`을 채운다. World 파일은 기존처럼 모노+normalize로 가져온다. 에디터를 못 쓰면 기존 형식대로 직접 쓰고 "미검증(에디터 미실행)"이라고 적는다.
- `init/sound-effect-address.md`에 파일마다 한 줄(파일 / 쓰는 곳 / README의 Pixabay 주소 / 2026-10-25 / 비고)을 남긴다. README가 저장소에 없으면 사용자가 준 묶음의 README를 `work_process/MVP2/sound/sfx-D1/`에 넣는다.

## 2. 연결 위치 (줄 번호는 38da5c09 기준 근사)
① **반복 소리 공통** (새 `Scripts/App/Integration/WorldLoopAudio.cs`)
  - AudioSource 하나에 loop=true. 음량은 칸 음량 × 버스 × `EffectiveVolume(Sfx)`, 음소거면 멈춘다. 페이드인 0.05초, 페이드아웃 0.08초다.
  - 멈춤 판정은 순수 클래스 `WorldLoopGate`로 만든다. 켜짐 조건이 참이고 아래가 모두 아닐 때만 소리가 난다.
    - `UiPauseGate.IsHeld`(ESC·일시정지): 바로 멈추고, 풀리면 조건이 남아 있을 때 다시 낸다.
    - `RunFailureRuntimeController.IsHandling` 또는 체력 0(`State.CanAct == false`): 바로 멈춘다.
    - OnDisable·씬 언로드: 바로 멈춘다(씬 오브젝트에 붙여 씬과 함께 사라지게).
  - `PortalIdleLoopAudio`(~119-122): 일시정지 중 0.35배로 계속 나던 것을 **멈춤**으로 바꾼다. 기존 B-162 테스트 기대값도 함께 고친다.
② **걷기** (Gameplay/Player/PlayerAnimationController.cs `Play` ~224, Walk는 10프레임·초당 10프레임)
  - Walk 상태에서 발이 닿는 두 프레임(기본 0번·5번, 그림을 보고 맞춤)으로 넘어갈 때 Footstep을 한 번 낸다. 1초에 두 걸음이다.
  - 조건: `IsGrounded`, 사다리 아님, 실제 가로 속도가 0.1 넘음(벽을 밀 때 무음), `Time.timeScale > 0`. 피치는 1.0과 0.96을 번갈아 쓴다.
  - **결정: 시설 바닥도 같은 소리다.** 재질별 소리는 이번에 하지 않는다.
③ **점프·착지** (PlayerMovement.cs `ApplyJumpImpulse` ~290, PlayerSurvivalController.cs `TrackFall` ~504-511)
  - 점프: `ApplyJumpImpulse`에서 Jump. 땅·코요테·입력 버퍼·사다리 점프가 모두 이 한 곳을 지나므로 한 번만 난다.
  - 착지: `TrackFall`의 착지 분기에서 `ApplyFall` 결과를 보고 하나만 고른다.
    - 피해 받음(ApplyFall true)이고 살아 있으면 LandHeavy만 낸다(화물 배율은 이미 피해에 반영됨).
    - 피해 없음이고 떨어진 높이가 0.3 이상이면 Land를 낸다.
    - 사다리를 타고 내려와 닿음(`usedLadderDuringFall`)이거나 이 착지로 체력이 0이 되면 무음이다(실패 창 지직에 양보).
  - 순수 클래스 `PlayerMoveSfxGate`(시계 주입)로 판정한다. 점프와 착지 중 먼저 난 소리에서 0.1초 안에 오는 다른 하나는 생략한다(#167 착지 직후 점프).
④ **사다리** (PlayerMovement.cs `EnterLadderMode` ~177, `ExitLadderMode` ~211, PlayerAnimationController.cs `ResolveLadderFrameIndex` ~302)
  - 트리거에 들어가기만 해도 사다리 모드가 켜지므로 순수 클래스 `LadderSfxGate`로 거른다.
  - 잡음(LadderGrab): 공중에서 사다리 모드가 켜질 때, 또는 땅에서 켜진 뒤 처음 위아래로 움직일 때 한 번.
  - 놓음(LadderGrab): 잡음 소리를 냈던 경우에만 사다리 모드가 끝날 때 한 번. 위로 올라섬·점프·옆으로 나감이다. OnDisable·구조·엘리베이터 잠금은 무음이다.
  - 걸음: 등반 순서 인덱스가 2나 6(자세 꼭대기)에 닿을 때마다 LadderClimb과 LadderClimbB를 번갈아 낸다. 최소 간격은 0.18초다. 빈 화물이면 초당 약 4번이다. 반복 소리는 아니다.
⑤ **엘리베이터** (Gameplay/Player/ElevatorController.cs, ElevatorDoorVisual.cs, App/Integration/ElevatorTravelBridge.cs)
  - 광산 → 지상(E): `RequestTravel` 성공(~221 Calling)에서 UiConfirm, 문이 닫히기 시작할 때 ElevatorDoor를 낸다.
  - `Moving`(~234)부터 칸이 올라가는 동안 ElevatorMoveLoop를 낸다(① 사용). 씬 전환이나 `Arrived`·`Blocked`·`Idle`이면 멈춘다.
  - 지상 도착: Surface 씬이 열리고 `runtime.ElevatorState == Arrived`일 때 ElevatorArriveDing을 한 번 낸다. 지상에는 문 그림이 없어 문 소리는 없다. 위치를 못 찾으면 보고한다.
  - 지상 → 광산("탐사 시작"): `SurfaceBaseBinder.TryStartExplorationViaRuntime`(~490-504) 성공 때 UiConfirm과 ElevatorDoor(닫힘)를 낸다. 버튼에 확인음이 이미 있으면 겹치지 않는다. 움직이는 칸이 없으니 승강 루프는 없다.
  - 광산 도착: StartElevator 문이 씬 시작 때 열리기 시작하는 순간(DoorVisual `OnEnable` ~73-83 → `UpdateDoor` ~104-113)에 ElevatorArriveDing과 ElevatorDoor를 낸다.
    - 엘리베이터·탐사 시작으로 들어왔을 때만 낸다(`ElevatorState == Arrived`). 이어하기·불러오기·포탈 도착은 무음이다. 구분이 안 되면 보고한다.
  - 출발 뒤 이동이 거절됨(~262-274): 칸이 내려와 `Blocked`가 되면 루프를 멈추고, 문 열림 ElevatorDoor와 UiCancelFail을 한 번씩 낸다.
  - 출구 막힘(~208-211)과 경로 없음(~215-218): `return false` 직전에 UiCancelFail을 낸다. `StateChanged`는 같은 상태면 안 오니 이벤트에 기대지 말고 그 자리에서 낸다. 연타는 0.3초 간격으로 거른다.
  - 탑승해 플레이어가 잠긴 동안(LockRider ~289)은 드릴 루프가 나지 않게 한다.
⑥ **드릴·블록** (Gameplay/Mining/MiningSystem.cs `ProgressChanged` ~130, `CompleteMining` ~436-500, App/Integration/IntegrationRuntimeBinder.cs ~640-660 바인딩 근처)
  - DrillLoop는 플레이어에 붙인 ①로 낸다. 켜짐 조건은 `miningSystem.IsMining`이고 LateUpdate에서 판단한다.
    - 대상 블록이 바뀌면 Cancelled 다음 바로 Mining이 오므로, 같은 프레임 안이면 끊지 않는다.
    - 완료·취소·실패면 페이드아웃 0.08초로 끝낸다.
  - `TileMined`(~499)에서 BlockBreak를 한 번 낸다. 루프는 같은 프레임에 끊는다.
⑦ **자원 획득** (같은 `TileMined`, App/Core/Data/DataIds.cs `RareItems.IsRare` ~25)
  - `mineralId`가 있고 `quantity > 0`이며 `goldDrop == 0`일 때만 낸다. 희귀면 PickupRare, 아니면 PickupCommon이다. 황금 지형은 골드 팝업 소리가 맡으니 무음이다.
⑧ **채굴 막힘 실패음** (App/Integration/MiningProgressHud.cs `OnProgressChanged` Failed ~98-104, Gameplay/Mining/PlayerMiningController.cs 새로 누름 ~100)
  - UiCancelFail을 낼 이유: NotMineable, DrillLevelTooLow, InsufficientEnergy, InventoryFull, OutOfRange, DeepZoneLocked
  - 무음: InvalidTarget("채굴할 수 없습니다"), TargetChanged, DependencyMissing, InvalidReward, 설치 중이라 문구가 숨겨진 경우
  - 순수 클래스 `MiningFailSfxGate`로 판정한다. 같은 이유가 이어지면 다시 안 낸다. 이유가 바뀌거나, 플레이어가 채굴 키를 새로 누르거나(누른 순간 이벤트를 추가), Failed가 아닌 단계가 오면 다시 낼 수 있다.
⑨ **화물 80%** (IntegrationRuntimeBinder.cs `ApplyCargoSpeed` ~852)
  - 순수 클래스 `CargoHeavySfxGate`로 판정한다. 비율은 `CargoLoadEffectPolicy.EvaluateLoadRatio`를 쓴다.
    - 0.8 미만에서 0.8 이상으로 넘을 때 CargoHeavyWarning을 한 번 낸다.
    - 0.8 미만으로 내려가면 다시 낼 수 있다. 씬 진입·불러오기 첫 관측은 무음이다(가득 참은 ⑧).

## 손대지 말 것
- 음원 파일, 기존 칸(1~50)의 값과 클립, 이미 연결된 A~C 소리, 버스 음량, 설정창, 세이브 형식, 관련 없는 리팩터링
- `FailureFeedback` 입구, 머리 위 노란 경고 새 문구(엘리베이터 출구 막힘 포함), 피격음(D-2. 피격음을 넣을 때 낙하 피해는 LandHeavy만 남겨야 함)
- 사다리 설치음·가스 루프(D-2), 드릴 커서(B-151 보류), E 단계 화물 공간 부족 창

## 테스트
- EditMode 전체에서 기존 실패 20개 말고 새 실패가 없어야 한다. 칸 개수를 세는 기존 테스트는 64칸으로 고친다.
- 새 EditMode 테스트 `PromptB164SfxD1Tests`
  - Defaults: 51~65 번호 중복 없음, 파일 이름이 표와 같고 모두 World 버스, `drill_loop_fwdrev` 칸 없음, 두 반복 파일이 `IsLoopFile`
  - 착지: 피해면 LandHeavy만, 피해 없음·0.3 이상이면 Land, 0.2 낙하·사다리 내려옴·치명 낙하면 무음, 착지 0.05초 뒤 점프면 Jump 무음
  - 사다리: 땅에서 들어와 안 움직이고 나가면 0번, 공중 잡기면 Grab 1번에 놓을 때 1번, 걸음 4번이면 A·B·A·B, 0.1초 간격 두 번이면 1번
  - 채굴 실패: 같은 이유 3번이면 1번, 이유가 바뀌면 다시 1번, 새로 누르면 다시 1번, InvalidTarget이면 0번
  - 화물: 첫 관측 0.9면 0번, 0.79→0.8이면 1번, 0.85→0.9면 0번, 0.7로 내려갔다 0.82면 다시 1번
  - 반복 소리: 일시정지·실패 처리·OnDisable이면 멈춤, 일시정지가 풀리고 조건이 남아 있으면 다시 남. 포탈 대기음도 일시정지에 멈춤
  - 픽업: 구리 Common, 엔진 연료 Rare, goldDrop 있으면 0번
- PlayMode는 이번에 추가한 테스트만 돌린다.

## 수동 확인 체크리스트 (보고서에 그대로 넣기)
- [ ] 광산에서 걸으면 발걸음이 걷기 그림에 맞춰 난다. 벽에 막혀 제자리걸음일 때와 멈췄을 때는 소리가 없다.
- [ ] 점프하면 "휙", 내려서면 "툭"이 난다. 착지하자마자 다시 점프해도 소리가 두 번 겹치지 않는다.
- [ ] 높은 곳에서 떨어져 체력이 깎이면 "쿠웅" 하나만 난다. 화물이 많을 때 더 쉽게 다친다.
- [ ] 사다리를 잡으면 "챙", 오르내리면 나무 발걸음이 번갈아 나고, 놓으면 다시 "챙"이 난다. 사다리 옆을 걸어 지나가기만 하면 소리가 없다.
- [ ] 광산 엘리베이터에서 E를 누르면 확인음과 문 닫힘, 올라가는 동안 승강음이 나고, 지상에 도착하면 "딩"이 난다.
- [ ] 지상에서 "탐사 시작"을 누르면 확인음과 문 닫힘이 나고, 광산에 도착해 문이 열릴 때 "딩"과 문 소리가 난다. 이어하기로 광산에 들어오면 소리가 없다.
- [ ] 엘리베이터 출구를 막고 E를 누르면 실패음이 난다(머리 위 문구는 아직 없음).
- [ ] 블록을 캐는 동안 드릴 소리가 이어지고, 부서지면 드릴이 끊기며 "와장"이 난다. 캐다가 움직이거나 방향을 바꾸면 드릴이 멈춘다.
- [ ] 구리·철·리튬을 얻으면 짧은 획득음, 엔진 연료는 반짝이는 다른 소리가 난다. 황금 지형은 골드 팝업 소리만 난다.
- [ ] 드릴 레벨이 부족한 블록을 계속 누르고 있으면 실패음이 한 번만 난다. 손을 뗐다 다시 누르면 또 난다. 빈 칸을 누르면 소리가 없다.
- [ ] 화물이 80%를 넘는 순간 경고음이 한 번 난다. 더 캐도 다시 안 나고, 팔아서 줄인 뒤 다시 넘으면 또 난다.
- [ ] 드릴·승강음·포탈 대기음이 ESC 일시정지, 체력 0 실패 창, 씬 전환 때 바로 멈춘다. 일시정지를 풀고 계속 캐면 드릴이 다시 난다.
- [ ] 효과음 음량과 음소거가 새 소리에도 적용되고, 위험도 경보가 끊기지 않는다. Console에 빈 칸 경고나 오류가 없다.

## #158 대응 (완료 표시용)
| #158 줄 | 내용 | 이 프롬프트 |
|---|---|---|
| 84 | 반복 소리 멈춤, 포탈 대기음 일시정지 | ① |
| 87 | 발소리 | ② |
| 88-90 | 점프·착지·무거운 착지 | ③ |
| 91-92 | 사다리 잡기·오르내리기·놓기 | ④ |
| 93-95 | 엘리베이터 양방향 출발·승강·도착 | ⑤ |
| 96 | 출구 막힘 실패음 | ⑤ (문구 없음, PM 판단) |
| 99-100 | 드릴 루프, 블록 파괴 | ⑥ |
| 101 | 일반·희귀 자원 | ⑦ |
| 102-103 | 채굴 막힘 실패음 | ⑧ |
| 104 | 화물 80% 경고 | ⑨ |
- #158 본문은 이 작업에서 고치지 않는다(PM이 고침).

## 완료 후
- PR 제목 예: `feat: 효과음 D-1 이동·채굴 (프롬프트 B-164)`. 커밋 메시지는 한국어 conventional 형식으로 쓰고, 브랜치는 지우지 않는다.
- 보고할 것
  - 기준 커밋, 브랜치, PR 링크, 테스트 결과
  - 칸 표, 그리고 소리마다 연결한 클래스·메서드·줄
  - 못 찾은 위치와 직접 들어 보지 못한 항목은 "미검증"으로 구분
  - 걷기 발 닿는 프레임 번호, 사다리 걸음 실제 간격, 음량 조정이 필요해 보이는 소리
  - 위 수동 확인 체크리스트
