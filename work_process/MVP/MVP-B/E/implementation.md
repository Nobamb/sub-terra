# Phase E 구현 기록

## 구현

- `Assets/_Project/Scripts/Shared/`
  - `Contracts/IResourceWallet.cs` — `CanAfford`(무변경) / `TrySpend`(성공 시에만 차감)
  - `DTO/ItemCostDto.cs` — 영구 ID + 수량 비용 DTO (Unity 비의존)
- `Assets/_Project/Scripts/App/Economy/`
  - `EconomyService` — `IResourceWallet` 구현 + `TrySellMineral`
    - 판매 가격은 카탈로그 `UnitPrice`만 사용 (UI 단가 파라미터 없음)
    - 사전 검증(보유량·골드 오버플로) 후 인벤 차감 → 골드 지급 한 경로
    - `TrySpend`는 `CostAggregator` 합산 후 `InventoryService.TryReduceMany` 일괄 차감
    - 성공 시 `TransactionCompleted` + `AutoSaveRequested` 각 1회, 실패 시 상태 이벤트 없음
  - `CraftingService` — `CanAfford` → `TryPlace` → `TrySpend` 순서. 배치 실패 시 차감 없음. busy 재진입 가드
  - `CostAggregator` — 동일 ID 합산, 빈 ID·비양수·오버플로 거부
  - `IBuildingPlacementGate` — A 배치/Prefab 성공 여부 추상화 (테스트 대역 가능)
  - `ItemCostMapping` — App `ItemCostEntry` ↔ Shared `ItemCostDto`
  - `EconomyTransactionResult` / `EconomyAutoSaveRequest` — 결과·자동 저장 훅
- `Assets/_Project/Scripts/App/Inventory/InventoryService.cs`
  - `TryReduceMany` — 전 항목 사전 검증 후 일괄 차감, `InventoryChanged` 1회
- `Assets/_Project/Scripts/App/UI/Economy/`
  - `EconomyPanelPresenter` — 서비스 호출만, State/Inventory 직접 쓰기 없음, busy 중복 클릭 가드
  - `EconomyPanelView` / `EconomyPanelBinder` / `IEconomyPanelView`
- 테스트
  - Edit Mode: `EconomyServiceTests`, `CraftingOrchestrationTests`, `EconomyStaticStructureTests`
  - Play Mode: `EconomyPlayModeTests` (판매 성공/실패 메시지, 배치 실패 시 자원 유지)

## 처리 흐름

### 판매

```
UI RequestSell(id, qty)
→ EconomyService.TrySellMineral
→ catalog.UnitPrice (카탈로그 전용)
→ 보유량·골드 오버플로 사전 검증
→ InventoryService.TryReduceMineral
→ GameState.AddGold(unitPrice * qty)
→ TransactionCompleted + AutoSaveRequested (성공 1회)
```

### 시설 비용 / 제작

```
CraftingService.TryCraftBuilding
→ IResourceWallet.CanAfford(costs)   // 무변경
→ IBuildingPlacementGate.TryPlace     // 실패 시 종료, Spend 미호출
→ IResourceWallet.TrySpend(costs)     // 합산·재검증·일괄 차감
→ CraftCompleted / AutoSave (Spend 성공 경로)
```

## 예외 정책

| 입력/상황 | 결과 | State | AutoSave |
| --- | --- | --- | --- |
| 잘못된 ID·0·음수 수량 | InvalidRequest | 불변 | 없음 |
| 보유 부족 (판매/Spend) | InsufficientResources | 불변 | 없음 |
| 골드 오버플로 | GoldOverflow | 불변 | 없음 |
| 동일 ID 비용 합산 후 부족 | Insufficient | 불변 | 없음 |
| 배치 실패 | PlacementFailed | 불변, Spend 미호출 | 없음 |
| 처리 중 재진입 | Busy | 불변 | 없음 |
| 판매/차감 성공 | Success | 변경 | 1회 |

## 검증

- Edit Mode `SubTerra.App.Tests.EditMode`: **117 passed / 0 failed** (E-F01~F05, E-S01~S05 포함)
- Play Mode `SubTerra.App.Tests.PlayMode`: **6 passed / 0 failed** (EconomyPlayModeTests 포함)
- Shared `IResourceWallet` / `ItemCostDto` 추가 (Gameplay 참조 가능 최소 표면)
- 한국어 주석: 트랜잭션·원자성·합산·배치 후 차감 순서에 집중

## 설계 메모

- 부분 차감 금지: `TryReduceMany`가 검증 실패 시 `SetQuantity`를 한 번도 호출하지 않음.
- `CanAfford`는 이벤트·State를 건드리지 않으며 자원 예약을 만들지 않음.
- 2단계 예약 결제 계약은 Shared 합의 없이 추가하지 않음 (place 성공 후 Spend).
- Phase K 실제 JSON 세이브는 미구현 — `AutoSaveRequested` 훅만 제공.

## 남은 한계

- A 배치 시스템 실배선·Integration Scene은 M 단계.
- 업그레이드 상점·Surface Base 전체 플로우는 후속.
- Prefab 시각 패널은 View 컴포넌트만 제공, 통합 Scene 배치는 후속.
- 배치 성공 후 Spend 실패 시 Prefab 롤백은 A측 책임(문서화).
