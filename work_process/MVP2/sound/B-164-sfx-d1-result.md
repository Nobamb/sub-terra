# B-164 효과음 D-1 결과 (이동·조작, 채굴·자원, 반복 소리 멈춤)

- 기준 커밋: `38da5c09` (MVP2-fix103 = main)
- 브랜치: `MVP2-fix104`
- PR: 아직 없음(커밋·PR은 사용자 확인 후)
- 음원: 사용자가 넣은 `Audio/SFX/World/` wav 15개를 그대로 사용. `drill_loop_fwdrev.wav`는 폴더에 없어 지울 것이 없었다.

## 칸 (모두 World 버스, 버스 음량 1.0)

| 번호 | SfxId | 파일 | 길이 | 음량 | 가져오기 | 최종 RMS(칸 음량 반영) |
|---|---|---|---|---|---|---|
| 51 | Footstep | footstep.wav | 0.39s | 0.65 | 모노·풀어 둠 | -26.1 dB |
| 52 | Jump | jump.wav | 0.58s | 0.25 | 모노·풀어 둠 | -23.7 dB |
| 53 | Land | land.wav | 0.55s | 0.45 | 모노·풀어 둠 | -23.1 dB |
| 54 | LandHeavy | land_heavy.wav | 1.01s | 0.35 | 모노·풀어 둠 | -20.0 dB |
| 55 | LadderGrab | ladder_grab.wav | 0.20s | 0.9 | 모노·풀어 둠 | -22.5 dB |
| 56 | LadderClimb | ladder_climb.wav | 0.50s | 0.35 | 모노·풀어 둠 | -28.0 dB |
| 57 | LadderClimbB | ladder_climb_b.wav | 0.50s | 0.35 | 모노·풀어 둠 | -27.9 dB |
| 58 | ElevatorDoor | elevator_door.wav | 1.58s | 0.45 | 모노·풀어 둠 | -22.2 dB |
| 59 | ElevatorMoveLoop | elevator_move_loop.wav | 5.50s 반복 | 0.15 | 모노·압축 유지 | -23.0 dB |
| 60 | ElevatorArriveDing | elevator_arrive_ding.wav | 1.35s | 0.45 | 모노·풀어 둠 | -23.5 dB |
| 61 | DrillLoop | drill_loop.wav | 1.85s 반복 | 0.35 | 모노·압축 유지 | -20.8 dB |
| 62 | BlockBreak | block_break.wav | 1.06s | 0.3 | 모노·풀어 둠 | -25.2 dB |
| 63 | PickupCommon | pickup_common.wav | 0.68s | 0.35 | 모노·풀어 둠 | -23.8 dB |
| 64 | PickupRare | pickup_rare.wav | 0.81s | 0.5 | 모노·풀어 둠 | -21.2 dB |
| 65 | CargoHeavyWarning | cargo_heavy_warning.wav | 0.34s | 0.2 | 모노·풀어 둠 | -20.9 dB |

- 비교: 기존 World 칸 storage_ore_pop -26.7 dB, portal_idle_loop -21.4 dB. 새 소리는 -28 ~ -20 dB 안에 있다.
- `SfxLibraryDefaults.Create()` 64칸, `LoopFileNames`에 `elevator_move_loop`·`drill_loop` 추가. Fallbacks는 그대로.
- 빌더(`PromptB156SfxLibraryBuilder.EnsureAsset`)로 `Resources/SfxLibrary.asset` 51~65 칸을 채웠다(에디터 실행).

## 연결 위치

| 소리 | 클래스·메서드(줄) | 조건 |
|---|---|---|
| 반복 소리 공통 | `App/Integration/WorldLoopAudio.cs` 새 파일. 판정은 순수 클래스 `WorldLoopGate` | LateUpdate. 일시정지(`UiPauseGate.IsHeld`)·`RunFailureRuntimeController.IsHandling`·`State.CanAct == false`·음소거·OnDisable이면 바로 멈춤, 풀리고 조건이 남으면 다시. 페이드인 0.05초, 조건이 꺼지면 페이드아웃 0.08초 |
| 포탈 대기음 | `PortalIdleLoopAudio.Tick`(109) | 일시정지 중 0.35배 → 멈춤. `pausedVolumeScale` 필드 삭제 |
| Footstep | `PlayerAnimationController.TryPlayFootstep`(280) | Walk 상태에서 발 닿는 프레임(2·7)으로 넘어갈 때. 땅·사다리 아님·가로 속도 > 0.1·`timeScale > 0`. 피치 1.0/0.96 번갈아 |
| Jump | `PlayerMovement.ApplyJumpImpulse`(323) | `PlayerMoveSfxGate.ResolveJump` |
| Land / LandHeavy | `PlayerSurvivalController.TrackFall`(513) | `ApplyFall` 결과로 하나만. 피해면 LandHeavy, 없음·0.3 이상이면 Land, 사다리로 내려옴·치명 낙하는 무음. 0.1초 안 겹침 생략 |
| LadderGrab(잡음) | `PlayerMovement.EnterLadderMode`(207), `FixedUpdate`(139) | `LadderSfxGate`. 공중에서 켜짐, 또는 땅에서 켜진 뒤 처음 위아래 입력 |
| LadderGrab(놓음) | `PlayerMovement.ExitLadderMode`(234) | 잡음 소리를 냈을 때만. 인자 없는 `ExitLadder()`(OnDisable·위치 복원)는 무음 |
| LadderClimb / B | `PlayerAnimationController.ResolveLadderFrameIndex`(354) | 등반 순서 인덱스 2·6(3프레임은 1·3). A·B 번갈아, 최소 0.18초 |
| UiConfirm + ElevatorDoor(광산 출발) | `ElevatorController.RequestTravel`(235-236) | Calling 직후. 문이 이때 닫히기 시작 |
| ElevatorMoveLoop | `IntegrationRuntimeBinder.BindWorldSfx`(882) → 엘리베이터 오브젝트의 `WorldLoopAudio` | `ElevatorController.State == Moving` |
| ElevatorDoor + UiCancelFail(이동 거절) | `ElevatorController.TravelRoutine`(290-291) | 칸이 내려와 Blocked |
| UiCancelFail(출구 막힘·경로 없음) | `ElevatorController.PlayTravelFailSfx`(316) | `return false` 직전, 0.3초 간격 |
| ElevatorArriveDing(지상) | `SaveRuntimeController.PlayArrivalDingWhenSurfaceOpens`(1102) | 광산 → 지상 이동 성공 뒤 SurfaceBase가 활성이고 `ElevatorState == Arrived` |
| UiConfirm + ElevatorDoor(탐사 시작) | `SurfaceBaseBinder.TryStartExplorationViaRuntime`(506-507) | 성공 시. 탐사 시작 버튼에는 기존 확인음이 없다(UiSfxButton 없음) |
| ElevatorArriveDing + ElevatorDoor(광산 도착) | `ElevatorDoorVisual.BeginTransition`(281-282) | 문이 처음 열리기 시작하는 순간. `ElevatorController.Awake`에서 런타임이 Arrived일 때만 예약(이어하기·불러오기는 Idle이라 무음) |
| DrillLoop | `IntegrationRuntimeBinder.BindWorldSfx`(871) → 플레이어의 `WorldLoopAudio` | `miningSystem.IsMining && movement.CanMove`(엘리베이터 잠금 중 무음) |
| BlockBreak / Pickup | `IntegrationRuntimeBinder.OnTileMinedForSfx`(900-901) | `TileMined`. 픽업은 `MiningPickupSfx`: mineralId 있음·quantity > 0·goldDrop == 0. 희귀면 Rare |
| UiCancelFail(채굴 막힘) | `MiningProgressHud.OnProgressChanged`(115), `PlayerMiningController.MiningPressed`(100) | `MiningFailSfxGate`. NotMineable·DrillLevelTooLow·InsufficientEnergy·InventoryFull·OutOfRange·DeepZoneLocked만. 문구를 숨기면 무음 |
| CargoHeavyWarning | `IntegrationRuntimeBinder.ApplyCargoSpeed`(925) | `CargoHeavySfxGate`, 0.8 미만 → 이상일 때. 첫 관측 무음 |

## 측정·결정

- 걷기 발 닿는 프레임: **2·7**. 프레임별 발 벌어짐(px)은 86, 102, **126**, 104, 110, 120, 112, 103, 113, 81로 최대가 2번과 5번이다. 5번에 맞추면 간격이 0.3/0.7초로 고르지 않아, 가장 큰 2번과 다섯 프레임 뒤 7번을 골랐다(1초에 두 걸음). `footstepFrames` 인스펙터 값으로 바꿀 수 있다.
- 사다리 걸음 간격: 프리팹 `ladderDistancePerFrame` 0.25, `ladderSpeed` 4 → 꼭대기 자세가 1칸마다 → 빈 화물이면 0.25초(초당 4번). 최소 간격 0.18초에 걸리지 않는다.
- 광산 도착: `ElevatorController.Awake`는 `ResolvePort` 뒤 `SetState(Idle)`로 덮어써서 상태만 보면 늘 Idle이다. 그래서 Awake에서 포트(런타임) 상태를 직접 읽어 도착음을 예약했다.
- 반복 파일 가져오기: wav가 코드보다 먼저 가져와져 `drill_loop`·`elevator_move_loop`가 처음에 DecompressOnLoad로 들어갔다. 컴파일 뒤 다시 가져와 CompressedInMemory를 확인했다.

## 음량 조정이 필요해 보이는 소리(미검증, 들어 보지 못함)

- LadderClimb / B(-28 dB)와 Footstep(-26 dB)은 자주 나는 소리라 일부러 작다. 드릴·BGM에 묻히면 0.45 정도로 올린다.
- LadderGrab 0.9는 README 메모대로 너무 크면 0.6으로 낮춘다.
- 탐사 시작 뒤 지상 문 소리(1.58초)와 광산 도착 문 소리가 로딩이 짧으면 겹칠 수 있다.

## 테스트

- EditMode 전체: **2050 통과 / 20 실패**. 실패 20개는 B-163 보고서의 기존 실패와 같은 묶음(B-162 BriefingClose 값, 건설 배치, 패널 폭, HUD, B-85·B-89·B-107·B-138·B-150 등). 새 실패 없음. B-163 기준(2037 통과)보다 새 테스트 13개가 늘었다.
- 새 `PromptB164SfxD1Tests` 13개 통과: 칸·가져오기 설정, 착지·점프 겹침, 사다리 잡기·놓기·걸음, 채굴 실패음, 화물 80%, 픽업, 반복 소리(일시정지·실패·OnDisable·페이드), 포탈 대기음 일시정지.
- 고친 기존 테스트: `PromptB156SfxTests` 칸 수 49 → 64, `PromptB162SfxCStageTests` 포탈 대기음 일시정지 기대값(0.35배 → 멈춤).
- PlayMode: 이번에 추가한 PlayMode 테스트가 없어 돌리지 않았다.
- 실제 플레이로 들어 보지 않았다. 아래 체크리스트는 모두 **미검증**이다.

## 못 찾음·미검증

- 못 찾은 위치는 없다.
- 광산 도착 문이 열리는 시점과 "딩"이 맞는지, 지상 도착 "딩"이 화면 전환과 맞는지는 플레이에서 확인하지 못했다(미검증).
- 엘리베이터 이동이 거절되어 칸이 내려오는 경로는 재현하지 못했다(미검증).

## 수동 확인 체크리스트

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

## #158 대응

| #158 줄 | 내용 | 상태 |
|---|---|---|
| 84 | 반복 소리 멈춤, 포탈 대기음 일시정지 | 구현(①) |
| 87 | 발소리 | 구현(②) |
| 88-90 | 점프·착지·무거운 착지 | 구현(③) |
| 91-92 | 사다리 잡기·오르내리기·놓기 | 구현(④) |
| 93-95 | 엘리베이터 양방향 출발·승강·도착 | 구현(⑤) |
| 96 | 출구 막힘 실패음 | 구현(⑤, 문구 없음) |
| 99-100 | 드릴 루프, 블록 파괴 | 구현(⑥) |
| 101 | 일반·희귀 자원 | 구현(⑦) |
| 102-103 | 채굴 막힘 실패음 | 구현(⑧) |
| 104 | 화물 80% 경고 | 구현(⑨) |
