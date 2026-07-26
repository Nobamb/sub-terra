# Phase D 구현 기록

## 구현

- `Assets/_Project/Scripts/App/Inventory/`
  - `InventoryService` — Shared `IMiningRewardReceiver` B측 구현. `AddMineral` → 검증 → 용량 내 완전 단위 수락 → 합산 1회 → 이벤트 1회
  - `InventoryState` — 영구 ID별 수량(0 이상, 0은 저장 안 함), 최대 적재(기본 50), 현재 중량·미정산 가치 캐시. mutable Dictionary 비공개
  - `InventoryCalculator` — 총중량·미정산 가치 단일 계산 경로 (`수량 × MineralData 단위값`)
  - `InventoryMutationResult` / `InventoryMutationStatus` — Success / PartialAccept / CapacityFull / InvalidId / InvalidQuantity / OverflowRisk / Insufficient
  - `IMineralCatalogLookup` + `InMemoryMineralCatalog`(테스트) + `GameDataCatalogMineralLookup`(프로덕션 어댑터)
  - `TryReduceMineral` — 보관함·정산용 원자적 감소 공통 경로
- `Assets/_Project/Scripts/App/UI/Inventory/`
  - `InventoryPanelView` / `InventoryPanelPresenter` / `InventoryPanelBinder`
  - HUD는 기존 Phase C `GameState.InventoryChanged` 경로, 패널은 `InventoryService.InventoryChanged` 스냅샷
  - Bind/Unbind 대칭, Update 폴링 없음, UI State 쓰기 없음
- Prefab: `Assets/_Project/Prefabs/UI/InventoryPanel.prefab` (Editor `InventoryPanelPrefabBuilder`)
- 테스트
  - Edit Mode: `InventoryServiceTests`, `InventoryStaticStructureTests`, `InventoryUiSyncTests`
  - Play Mode: `InventoryPlayModeTests` (Shared 경계 지급 → HUD·패널 동기화·재바인드)

## 처리 흐름

```
A MiningSystem
→ IMiningRewardReceiver.AddMineral(id, qty)
→ InventoryService.TryAddMineral
→ catalog.TryGetMineral
→ 양수 수량·오버플로·잔여 적재 검증
→ 완전 단위만 수락 (accepted/rejected)
→ InventoryState 스택 갱신
→ InventoryCalculator 합산 1회
→ GameState.SetInventory(weight, value)  // HUD InventoryChanged 1회
→ InventoryService.InventoryChanged(snapshot)  // Panel 1회
```

## 예외 정책

| 입력 | 결과 | State | 이벤트 |
| --- | --- | --- | --- |
| 알 수 없는 ID | InvalidId | 불변 | 없음 |
| 0 / 음수 | InvalidQuantity | 불변 | 없음 |
| 정수 오버플로 위험 | OverflowRisk | 불변 | 없음 |
| 한 단위도 못 넣음 | CapacityFull (accepted=0) | 불변 | 없음 |
| 일부만 수용 | PartialAccept | 변경 | 1회 |
| 전부 수용 | Success | 변경 | 1회 |

Shared `AddMineral`에 지급 ID가 없어 중복 지급 제거는 구현하지 않음(계약 보완 대기).

## 검증

- Edit Mode `SubTerra.App.Tests.EditMode`: **93 passed / 0 failed** (D-F01~F04, D-S01~S05 포함)
- Play Mode `SubTerra.App.Tests.PlayMode`: **4 passed / 0 failed** (InventoryPlayModeTests 포함)
- InventoryPanel prefab `HasRequiredReferences=True`
- Shared/Gameplay 폴더 미변경

## 설계 메모

- 중량·가치는 Inventory 계층에서만 계산. HUD formatter·Panel presenter는 스냅샷 포맷만 담당.
- `GameState`의 cargo/unsettled 읽기 모델은 성공 변이 시 `SetInventory` 한 번으로 밀어 넣어 Phase C HUD와 일치.
- Economy / Outpost / Save는 동일 `InventoryService` API(`TryAddMineral` / `TryReduceMineral` / `GetSnapshot`)만 사용하면 됨.

## 남은 한계

- Integration Scene Player→receiver 배선은 M 단계.
- 세이브 DTO·마이그레이션은 후속 Save 단계.
- `IResourceWallet` 판매·제작 차감은 Phase E.
- 기본 최대 적재 50. 업그레이드로 확장하는 것은 후속.
- Unity MCP 미연결 세션; Prefab·테스트는 Editor batchmode로 검증.
