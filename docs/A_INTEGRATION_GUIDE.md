# A Gameplay 통합 지침

최종 Integration Scene은 B/통합 담당자가 조립한다. A는 아래 Gameplay 컴포넌트와 Shared 계약을 제공하며 UI나 App State를 직접 수정하지 않는다.

## 필수 연결

| A 컴포넌트 | B/통합 연결 | 역할 |
| --- | --- | --- |
| `GameplayEventBridge` | `IGameplayEventSink` 구현체 | 채굴·구조·가스·건설·전력 결과 전달 |
| `BuildingPlacementSystem` | `SetResourceWallet(IResourceWallet)` | 비용 확인·성공 후 1회 차감 |
| `DroneSensor` | Shared `IDroneContextProvider.CreateContext()` | 드론 추천용 실제 월드 Context 제공 |
| `WorldSnapshotSystem` | `IWorldSnapshotProvider` | SaveService의 캡처·복원 연결 |

## 이벤트 매핑

- `TileMined`: 타일 ID, 광물 ID, 좌표, 수량
- `StructuralRiskChanged`: 구조 안정도 값
- `GasTriggered`: 가스 구역 ID, 종류, 좌표, 농도
- `BuildingPlaced` / `BuildingPlacementChanged`: 배치 결과 DTO
- `OutpostStatusChanged`: 전력 공급·소비 및 활성 상태

## 조립 순서

1. B bootstrap에서 `EconomyService`를 `BuildingPlacementSystem.SetResourceWallet()`에 전달한다.
2. App의 이벤트 수신 구현체를 `GameplayEventBridge.SetEventSink()`에 전달한다.
3. HUD는 Shared 이벤트/State 값만 표시하고 Tilemap·Collider를 직접 조회하지 않는다.
4. 저장 시 `WorldSnapshotSystem.CaptureSnapshot()`을 호출하고, 로드 직후 `RestoreSnapshot()`을 호출한다.
5. 복원 뒤 A가 구조 안정도·전력망을 다시 계산한다.

## 데모 테스트용 연결

`Gameplay_DemoWorld_Test/DemoSystems`에는 `GameplayEventRecorder`가 연결되어 있다. 이는 A 테스트용이며, 최종 조립에서는 B의 App 이벤트 Sink로 교체한다.
