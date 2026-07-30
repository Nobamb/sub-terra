# Phase L — 구조 실패·드론 구조·화물 손실

## 1. 개요

PRD는 전력 고갈, 붕괴, 가스 장기 노출 시 구조 실패와 일부 미정산 화물 손실을 요구하지만 현재 이를 총괄하는 Runtime 흐름이 없다.

## 2. 작업 목표

- Player health/행동 가능 상태와 실패 조건을 정의한다.
- 붕괴·가스·전력 고갈을 같은 Run 실패 Orchestrator에 연결한다.
- 미정산 화물 30~50% 손실과 드론 구조 업그레이드 보호 효과를 결정론적으로 계산한다.
- 체크포인트 또는 Surface Base로 안전하게 복귀한다.

## 3. 구현 범위

- `PlayerSurvivalState`, damage source와 invulnerability 정책
- `RunFailureService`/`RescueCoordinator`
- cargo loss calculator와 보호 우선순위
- 입력 잠금, 실패 UI, 복귀 target, autosave
- `PlayerRescued` Shared event와 tutorial/result 연동

## 4. 권장 구현 방향

1. 피해 판정은 Gameplay, Inventory 손실과 Run 전이는 App Service가 소유한다.
2. 손실은 광물 ID/수량/가치를 사용한 순수 결정론 계산으로 만든다.
3. 보호 업그레이드는 `IUpgradeEffectProvider` 값을 적용한다.
4. 실패 처리에는 idempotency token을 사용해 중복 이벤트를 막는다.
5. 설치된 전진기지와 월드 변경점은 유지하고 미정산 화물만 정책대로 손실한다.

## 5. 보안 및 안정성 기준

- 실패 처리 중 추가 입력·채굴·거래를 차단한다.
- 손실량은 보유량을 넘지 않고 음수/오버플로가 없다.
- 저장 실패가 세이브 삭제나 전체 진행 손실로 이어지지 않는다.
- 실패 애니메이션/시간 초과 뒤에도 안전 폴백 귀환이 보장된다.

## 6. 완료 기준

- 세 가지 실패 원인이 각각 같은 구조 흐름으로 진입한다.
- 손실/보호 수치가 UI와 Inventory 결과에 일치한다.
- 복귀 위치가 유효하고 월드 시설은 유지된다.
- 중복 실패 이벤트가 손실·저장·Scene 전환을 반복하지 않는다.

