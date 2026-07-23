# Phase F — 업그레이드와 진행도

## 1. 개요

`process-B`의 B-5를 구현한다. 데이터 기반 업그레이드 구매, 효과 조회와 심층 구역 잠금 해제를 제공한다.

## 2. 작업 목표

- 드릴 속도·효율, 최대 전력·화물, 드론 스캔·구조 보존, 가스 저항을 단계별 데이터로 관리한다.
- 비용 차감과 레벨 상승을 원자적으로 처리한다.
- A가 B의 구체 클래스를 참조하지 않고 효과를 조회하게 한다.
- 현재 레벨과 잠금 해제 상태를 후속 Save 단계가 저장할 수 있게 한다.

## 3. 구현 범위

- `Scripts/App/Progression/`: `UpgradeState`, `ProgressionService`, 효과 Provider
- `Scripts/App/UI/Progression/`: 업그레이드 목록·상세·구매 결과
- Phase B UpgradeData와 Phase E 자원/골드 결제 경로
- `Tests/EditMode/App/Progression/`, `Tests/PlayMode/Integration/Progression/`

## 4. 권장 구현 방향

1. 각 UpgradeData에 영구 ID, 최대 레벨, 레벨별 비용과 효과 값을 둔다. 현재 레벨은 `UpgradeState`에 ID별 정수로 둔다.
2. 구매 순서를 ID 조회 → 현재/최대 레벨 → 다음 레벨 데이터 → 비용 지불 가능 → 비용 차감 → 레벨 증가 → 이벤트/저장 요청으로 고정한다.
3. 비용 차감 성공 후 레벨 증가가 실패할 수 없도록 모든 검증을 먼저 끝낸다. 데이터 누락은 결제 전에 실패시킨다.
4. 효과 계산은 기본값과 현재 레벨을 입력받는 순수 함수로 분리한다. 정의된 누적/단계 교체 방식 중 하나만 사용한다.
5. `GetDrillSpeedMultiplier`, `GetEnergyEfficiencyMultiplier`, `GetMaximumCargoWeight`, `GetDroneScanRadius`, `GetGasResistance`에 해당하는 합의된 Provider를 구현한다. Shared 변경이 필요하면 먼저 A와 계약을 확정한다.
6. 최대 전력/화물 감소가 가능한 데이터 변경에도 현재 값이 허용 범위를 넘지 않도록 State 보정 책임을 명시한다.
7. 심층 잠금은 명시적인 진행도 조건과 이유를 반환해 Surface Base UI가 “왜 잠김”을 표시하게 한다.
8. 구매 성공 뒤 HUD/인벤토리 파생 최대치와 Gameplay Provider가 같은 프레임에 새 효과를 보게 한다.

## 5. 보안 및 안정성 기준

- 최대 레벨, 비용과 효과는 카탈로그만 신뢰하고 UI에서 전달된 수치를 사용하지 않는다.
- 없는 ID, 데이터 단계 누락, 음수 비용과 범위 밖 레벨은 상태를 변경하지 않는다.
- 배포 후 업그레이드 ID 변경은 Save Migration 없이 하지 않는다.
- 부동소수점 배율은 결정론적인 테스트 허용 오차를 사용한다.

## 6. 완료 기준

- 모든 MVP 업그레이드가 비용과 최대 레벨에 따라 구매된다.
- 효과 Provider와 UI가 현재 레벨의 같은 값을 반환한다.
- 최대 레벨 초과 구매는 비용과 State를 바꾸지 않는다.
- 심층 잠금 해제와 저장 대상 State가 명확히 연결된다.
