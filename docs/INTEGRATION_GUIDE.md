# Integration Guide — Mine_Demo_Integration

B 소유 통합 Scene `Assets/_Project/Scenes/App/Mine_Demo_Integration.unity`에
A Runtime Prefab과 B State·UI·Save 서비스를 연결한다.
A의 `Prefabs/Gameplay/` 및 `Scripts/Gameplay/` 내부는 수정하지 않는다.

## Scene 계층 (기준 Root)

| Root / 노드 | 역할 |
| --- | --- |
| `GameplayRoot` | A Runtime·월드 시스템 |
| `GameplayRoot/Grid` | Tilemap 부모 |
| `…/BackgroundTilemap` | 배경 레이어 |
| `…/ForegroundTilemap` | 채굴 지형 (A Mining 사용) |
| `…/HazardTilemap` | 위험 표시 레이어 |
| `…/BuildingTilemap` | 시설 점유 표시 레이어 |
| `GameplayRoot/RuntimeBuildings` | 배치 시설 인스턴스 부모 |
| `GameplayRoot/WorldSystems` | Mining/Structural/Gas/Building/Power/Snapshot/EventBridge |
| `GameplayRoot/Player` | 이동·채굴 입력 |
| `GameplayRoot/DiggerBot_Runtime` | `DroneSensor` |
| `GameplayRoot/OutpostCore_Demo` | 전진기지 데모 코어 |
| `ApplicationRoot` | B binder·bridge (`IntegrationRuntimeBinder` 등) |
| `HUDCanvas` | 읽기 전용 HUD + `CanvasGroup` 게이트 |
| `IntegrationEventSystem` | UI EventSystem (Scene당 1개) |
| `Main Camera` / `Global Light 2D` | 렌더 |

## Shared 5경계 연결표

| 계약 | A Producer | B Consumer | 연결 방식 |
| --- | --- | --- | --- |
| `IMiningRewardReceiver` | `MiningSystem.rewardReceiverBehaviour` | `IntegrationRuntimeBinder` → `InventoryService` | Inspector + `AddMineral` 위임 |
| `IResourceWallet` | `BuildingPlacementSystem.SetResourceWallet` | `EconomyService` (`SaveRuntimeController`) | binder `WireContracts` |
| `IGameplayEventSink` | `GameplayEventBridge.eventSinkBehaviour` | `IntegrationRuntimeBinder` → fan-out (`GameplayHazardStatusBridge`, `OutpostRuntimeBridge`) | Inspector + `IntegrationEventFanOut` |
| `IWorldSnapshotProvider` | `WorldSnapshotSystem` | `SaveRuntimeController.Resolve()` / binder 직렬 참조 | Scene 탐색 + Save Capture |
| `IDroneContextProvider` | `DroneSensor` | `DroneContextProviderAdapter` (B) | adapter `BindTo(sensor)` |

## 설치 참조 (ApplicationRoot)

`IntegrationRuntimeBinder` 직렬 필드:

- `buildingPlacementSystem` → WorldSystems
- `hudBinder` → HUDCanvas
- `hazardBridge` / `outpostBridge` / `droneContextAdapter`
- `buildingUiBinder` → BuildingUiIntegrationBinder
- `placementBridge` → GameplayBuildingPlacementBridge
- `droneSensor` → DiggerBot_Runtime
- `worldSnapshotProviderBehaviour` → WorldSnapshotSystem
- `hudCanvasGroup` → HUDCanvas CanvasGroup
- `deferredInputBehaviours` → PlayerMovement, PlayerMiningController

A 쪽 직렬 참조 (통합 시 확인):

- `MiningSystem.rewardReceiverBehaviour` → ApplicationRoot binder
- `GameplayEventBridge.eventSinkBehaviour` → ApplicationRoot binder
- `BuildingPlacementSystem.buildingRoot` → RuntimeBuildings
- `BuildingPlacementSystem.resourceWalletBehaviour` → 비움 (Shared wallet만 사용)
- `BuildingPlacementSystem.restoreDefinitions` → SupportPillarPlacement 등 복원 가능한 정의 1개 이상 (비어 있으면 continue 시 시설 복원 불가)

건설 경로:

- `GameplayBuildingPlacementBridge` + `BuildingPlacementPreview` + bindings(`building.support.basic`)
- `BuildingUiIntegrationBinder` + `BuildingMenu` Prefab (HUD 하위)
- 지갑: `SetResourceWallet(EconomyService)` + bridge `BindWallet`

테스트 전용 컴포넌트 `BuildingTestResourceWallet`, `GameplayEventRecorder`는 Integration에서 비활성.

## 저장·복원 순서

이어하기(`SaveRuntimeController.BeginContinue` / `ContinueService`):

1. **UI 게이트 닫기** — `ILoadedUiGate.SetReady(false)` (Integration HUD/입력 비활성 유지)
2. **B State 준비** — `RestoreBState` (GameState, Inventory, Upgrades, Drone 쿨다운)
3. **Scene 로드** — 세이브의 `targetSceneName` (Integration 포함)
4. **A 월드 복원** — `IWorldSnapshotProvider.RestoreSnapshot` → `IIntegrationRestoreListener.NotifyWorldRestored`
5. **파생 재계산** — `IDerivedStateRecalculator.Recalculate` → `NotifyDerivedRecalculated`
6. **UI/입력 활성** — `SetReady(true)` 후 binder가 `IsUiReady`를 확인한 뒤에만 `ActivateUi`

`IntegrationRuntimeBinder.ActivateUi`는 게이트/`IsUiReady` 없이 HUD를 강제 열지 않는다.
`IsUiReady` 타임아웃 시 UI를 닫은 채로 경고만 남긴다.

새 게임 → Surface Base → 탐사는 SurfaceBase 진입 시 이미 `IsUiReady=true`인 경우가 많다.

## 엘리베이터 왕복 저장·복원 순서

지상↔지하 엘리베이터 왕복 후에도 채굴·시설 변경점이 유지되어야 한다.
`MineWorldCache` + `SaveRuntimeController`가 담당한다.

### 지하 → 지상 귀환 (`TryReturnToSurface` / `TryElevatorTravel` → SurfaceBase)

1. **전력·Busy 검사** — `ElevatorTravelSession.TryCall` (귀환 비용 0)
2. **World Capture (Scene 언로드 전)** — `IWorldSnapshotProvider.CaptureSnapshot` → `MineWorldCache.ReplaceFromProvider`
3. **Scene 로드** — Surface Base (`TryDepart`)
4. **도착 후 자동 저장** — Provider가 없어도 `MineWorldFallback` 캐시로 `world.miningChanges` 유지
5. 지상 도착 시 전력 완충 (기존 정책 유지)

### 지상 → 지하 재진입 (`TryStartExploration`)

1. **전력 차감 5** + Scene 로드(Integration)
2. **Run 수명주기** — `TryBeginExploration`
3. **1프레임 대기** — `MineLayerTilemapGenerator.Awake` 풀 맵 생성 이후
4. **World Restore** — `TryRestoreMineWorld(cachedMineWorld)` (캐시 우선, 의미 없으면 신규 탐사)
5. **Notify + Recalculate** — Continue와 동일 게이트 신호

### Surface Base에서 저장할 때

- Provider null + 캐시 있음 → 캐시 world를 파일에 기록 (빈 스냅샷으로 덮지 않음)
- Provider null + 캐시 없음 → 빈 `WorldSnapshotDto` (구 호환)
- 새 게임 / 슬롯 전환 시 캐시 Clear 후, 로드된 세이브 world로 Seed

### 이어하기와의 공유

Continue 경로의 월드 복원은 `TryRestoreMineWorld`를 사용한다.
복원 소스 우선순위: **1) 메모리 캐시 2) 세이브 world**.

## Surface Base 새 광산 초기화

`ResetMineButton`은 Surface Base에서만 노출되며 확인 모달을 거쳐 실행된다.

1. `SurfaceBaseBinder`가 설정·판매 모달과 저장/엘리베이터 Busy를 확인한다.
2. 골드가 500G 미만이면 상태를 바꾸지 않고 부족 메시지를 표시한다.
3. 확인 시 `SaveRuntimeController.TryResetMine` → `MineResetService.TryReset`을 호출한다.
4. 성공하면 골드 500G만 차감하고 `MineWorldCache`를 새 non-zero 시드의 빈 `WorldSnapshotDto`로 교체한다.
5. `AutoSaveReason.MineReset`으로 즉시 저장한다. 다음 탐사는 기존 `TryRestoreMineWorld` 경로에서 새 시드를 재생성한다.

초기화 대상은 Mine world 변경점(채굴/변경 타일, 붕괴, 시설, 가스, 발견 구역, 전력 케이블)뿐이다.
인벤토리, 업그레이드, 심층 해금, 진행도, 전진기지, 최대 심도는 유지한다.
일반 엘리베이터 왕복과 이어하기는 기존 Mine persist 동작을 그대로 유지한다.

## 중복 방지

- Bootstrap `GameBootstrapper` / `SaveRuntimeController`는 DontDestroyOnLoad 단일 인스턴스
- Integration binder는 Inventory/Economy를 **생성하지 않고** `SaveRuntimeController` 인스턴스를 사용
- EventSystem은 Integration Scene에 1개 (`IntegrationEventSystem`)
- Scene 재진입 시 전역 서비스 재생성 금지 (`EnsureGameplayServices` 멱등)

## 수동 검증 절차

1. Bootstrap → Main Menu → 새 게임 → Surface Base → 탐사 시작 → Integration 진입
2. 구리 타일 1개 완전 채굴 → 인벤 수량·중량·미정산 가치·HUD 일치, 보상 1회
3. 자원 부족 위치/비용으로 버팀목 시도 → 차감 없음 → 유효 위치·충분 자원으로 설치 → Runtime 1회 + 비용 1회
4. 저장 후 이어하기 → State·채굴 타일·시설 복원, 중복 보상 없음
5. Integration ↔ SurfaceBase 왕복 시 EventSystem/전역 서비스 중복 없음, Console Error 없음
6. Hierarchy에서 4 Tilemap·Missing Script 없음 확인

## 자동 테스트

| 테스트 | 위치 | 범위 |
| --- | --- | --- |
| `IntegrationWiringTests` | EditMode/App/Integration | M-S01/S02/S04/S05, fan-out, gate, F01/F02/F04 단위 |
| `SaveRuntimeWiringTests` | EditMode/App/Save | Bootstrap/MainMenu/Integration 배선 |
| `MineDemoIntegrationPlayModeTests` | PlayMode/Integration/MineDemo | 실제 서비스 경로 최소 루프 |

## A 인수인계 참고

- `docs/A_INTEGRATION_GUIDE.md` — A 컴포넌트·이벤트 매핑
- `docs/A_DEMO_WORLD_GUIDE.md` — 데모 월드 좌표

Gameplay 규칙(붕괴·전력망·가스 확산) 재구현 금지. 문제는 A 재현 Issue로 전달한다.
