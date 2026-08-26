# Phase S Agent Test

## 1. 자동 검증

| ID | 검증 항목 | 테스트 | 기대 결과 |
| :-- | :-- | :-- | :-- |
| S-F01 | 실제 카탈로그 비용·효과 | `PromptB72UpgradeBalanceTests.Catalog_UsesReworkedEffectsAndTieredMineralCosts` | 9개 업그레이드의 모든 단계가 제안서 값과 일치 |
| S-F02 | 누적 전력 절감 | `PromptB72UpgradeBalanceTests.DrillEfficiency_TenMiningActionsReduceActualTotalEnergy` | 비용 2 타일 10개가 16/13/10 전력 소비 |
| S-F03 | 성공 채굴 통합 | `MiningSystemPlayModeTests.PromptB72_EnergyEfficiency_AccumulatesFractionalCostAcrossMining` | 20% 절감 상태에서 10회 커밋 후 16 소모 |
| S-F04 | 심층 선택 조건 | `PromptB72UpgradeBalanceTests.DeepZone_RequiresOnlyProgressAndDrillLevelTwo` | 스캔·가스 요구 없음, 드릴 Lv.2만 존재 |
| S-F05 | 기존 구매·저장 회귀 | App Edit Mode 전체 테스트 | 구매 원자성, 파생 상태, 저장·복원 회귀 없음 |

## 2. 수동 QA

1. 새 게임 첫 귀환의 구리로 Lv.1 여러 개를 동시에 살 수 없는지 확인한다.
2. 드릴 Lv.1 구매 전후 철 채굴 가능 여부와 같은 타일 채굴 시간을 비교한다.
3. 드릴 효율 Lv.1/2/3에서 기본 비용 1~3 타일을 반복 채굴해 HUD 전력 총량을 비교한다.
4. 최대 전력·화물·가스 저항 구매 직후 HUD 최대치와 실제 소비를 확인한다.
5. 드릴 Lv.2와 목표 12개만 충족한 상태에서 심층 잠금이 해제되는지 확인한다.
6. 저장 후 이어하기에서 기존 레벨과 새 효과가 동일하게 복원되는지 확인한다.

## 3. 검증 결과

- **데이터 빌더:** 카탈로그 유효, 오류 0, 업그레이드 9개 생성 확인
- **Edit Mode:** 616 통과, 0 실패, 0 스킵
- **Play Mode:** 5개 Assembly 전체 실행 요청 후 기존 TestRunner 종료 콜백 정지 현상이 재현되어 결과 파일 없이 안전 중단
- **대체 검증:** 누적 전력 계산을 순수 계산 테스트로 분리해 20/35/50%의 10회 총소모를 Edit Mode에서 검증
- **범위 가드:** 빌더·테스트가 dirty 처리한 건물·광물·대사·카탈로그·TMP 폰트와 임시 Test Scene 원복, Scene/Prefab 변경 0

## 4. 남은 검증

- Bootstrap → Surface Base → Mine 실제 플레이에서 단계별 구매 체감과 첫 귀환 구매 가능 개수를 기록해야 한다.
- 스캔 범위·구매 팝업·HUD 변화·탐사 결과 기여도 표시는 후속 P3 범위다.
