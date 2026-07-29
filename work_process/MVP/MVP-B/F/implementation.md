# Phase F 구현 기록

## 구현

- `Assets/_Project/Data/Upgrades/`
  - 드릴 속도·전력 효율, 최대 전력·화물, 드론 스캔·구조 보존, 가스 저항의 7개 단계별 에셋
  - 각 레벨의 `effectValue`는 해당 레벨의 누적 최종 보너스이며 현재 레벨은 에셋에 저장하지 않음
- `Assets/_Project/Scripts/Shared/Contracts/IUpgradeEffectProvider.cs`
  - Gameplay(A)이 App(B)의 구체 클래스를 참조하지 않고 7개 효과를 조회하는 계약
- `Assets/_Project/Scripts/App/Progression/`
  - `UpgradeState` — ID별 현재 레벨과 영구 해금 구역을 Unity JSON 직렬화 가능한 목록으로 보관
  - `ProgressionService` — 정의/현재 레벨/다음 단계/비용 전량 검증 후 `IResourceWallet` 1회 차감, 레벨 커밋, 이벤트와 저장 요청 발행
  - `UpgradeEffectProvider` — 현재 레벨의 데이터 값을 배율·최대치·저항 값으로 변환
  - `DeepZoneUnlockRule` — 목표 1개, 드론 스캔 2레벨, 가스 저항 1레벨의 MVP 심층 해금 조건과 미충족 이유
  - `ProgressionDerivedStateSynchronizer` — 구매 직후 최대 전력은 `GameState`, 최대 화물은 `InventoryService` 기존 이벤트 경로로 즉시 반영
  - `UpgradeSnapshot`, `ProgressionPurchaseResult` — UI 읽기 모델과 구매 결과
- `Assets/_Project/Scripts/App/UI/Progression/`
  - 업그레이드 목록·상세·비용·구매 결과·심층 잠금을 표시하는 View/Presenter/Binder
  - Presenter는 State/Inventory/지갑을 직접 변경하지 않고 `ProgressionService`만 호출

## 구매 처리 흐름

```
UI RequestPurchase(id)
→ 카탈로그 ID 조회
→ 현재/최대 레벨과 다음 단계 순번 검증
→ effectValue와 비용 목록 검증·동일 ID 합산
→ IResourceWallet.CanAfford     // 무변경
→ IResourceWallet.TrySpend      // 전량 일괄 차감
→ UpgradeState 레벨 커밋
→ PurchaseCompleted / UpgradeChanged / AutoSaveRequested
→ Provider·HUD·Inventory가 같은 프레임의 새 레벨 조회
```

## 예외 정책

| 입력/상황 | 결과 | 비용·레벨 | 저장 요청 |
| --- | --- | --- | --- |
| 빈/없는 ID | InvalidRequest / UpgradeNotFound | 불변 | 없음 |
| 최대 레벨 | MaximumLevel | 불변 | 없음 |
| 단계 수·순번·효과·비용 누락 | InvalidDefinition | 불변 | 없음 |
| 자원 부족 | InsufficientResources | 불변 | 없음 |
| 사전 검사 뒤 지갑 차감 실패 | SpendFailed | 불변 | 없음 |
| 정상 구매 | Success | 비용 1회 차감, 레벨 +1 | 1회 |

## 심층 잠금

- 읽기: `GetDeepZoneAccess`가 접근 가능 여부와 첫 미충족 이유를 반환
- 영구 반영: `TryUnlockDeepZone`가 조건 충족 시 `zone.deep`을 `UpgradeState.UnlockedZoneIds`에 한 번 기록
- 저장 연결: 현재 레벨과 해금 ID 모두 `[Serializable]`/`[SerializeField]` 값으로 Phase K가 그대로 읽을 수 있음

## 검증

- Unity 카탈로그: **7 upgrades / valid=True / errors=0**
- Edit Mode: **129 passed / 0 failed / 0 skipped**
  - F-F01~F05, F-S01~S05
  - 비용 원자성, 최대 레벨, 단계 누락, 7개 효과, 심층 경계, JSON 직렬화
  - 최대 전력·화물의 기존 State 이벤트 즉시 반영
- Play Mode: **7 passed / 0 failed / 0 skipped**
  - 구매 성공 프레임에 UI 레벨·효과와 Shared Provider 값 일치

## 후속 단계 연결

- Phase K는 `UpgradeState.Levels`, `UpgradeState.UnlockedZoneIds`와 `AutoSaveRequested`를 Save DTO/파일 저장에 연결한다.
- Phase L은 Progression View/Binder를 Surface Base 패널에 배치한다.
- Phase M은 Shared `IUpgradeEffectProvider`를 실제 Gameplay 소비자에 주입한다.
