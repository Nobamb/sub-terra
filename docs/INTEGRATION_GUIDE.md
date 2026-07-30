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
