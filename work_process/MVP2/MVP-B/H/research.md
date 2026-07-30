# Phase H — 가스 위험의 실제 플레이 효과

## 1. 개요

Gas Zone과 HUD 경고는 존재하지만 PRD의 전력 감소, 시야 제한, 이동 감속, 장시간 노출 피해가 Player 상태에 연결되지 않았다.

## 2. 작업 목표

- 가스 진입 즉시 경고하고 강도에 따라 효과를 적용한다.
- 전력 지속 감소, 이동 감속, 시야 제한을 실제 상태에 반영한다.
- 누적 노출이 피해/구조 실패 입력으로 전달된다.
- 이탈·보호 업그레이드·전진기지 시설로 대응 가능하게 한다.

## 3. 구현 범위

- `GasExposureEffectController` 또는 동등한 단일 적용기
- fixed interval Energy drain과 exposure timer
- Player hazard speed multiplier 연결
- vignette/fog/Light2D 시야 표현
- gas resistance upgrade 적용
- 환기/정화 시설은 MVP 데이터가 준비된 경우에만 포함

## 4. 권장 구현 방향

1. GasHazardSystem은 Zone 판정만, Effect Controller는 Player/App 상태 변이를 담당한다.
2. 매 프레임 직접 차감하지 않고 누적 시간의 고정 tick으로 처리한다.
3. 최고 강도 Zone 하나 또는 명시적 합산 정책을 데이터로 고정한다.
4. 경고 UI는 실제 exposure state와 남은 안전 시간을 표시한다.
5. 피해와 실패 최종 처리는 L 단계에 전달한다.

## 5. 보안 및 안정성 기준

- Zone 중첩 시 효과가 프레임 수에 따라 달라지지 않는다.
- 이탈/Scene 종료/비활성화 시 감속과 시야 효과가 즉시 복원된다.
- 전력은 0 아래로 내려가지 않으며 이벤트 폭주가 없다.
- 가스 시각 효과는 색상 외 모양/아이콘/텍스트를 함께 사용한다.

## 6. 완료 기준

- 가스 안에 머무르면 전력·속도·시야에 명확한 불이익이 생긴다.
- 위험 이탈 후 일시 효과가 정상 해제된다.
- 보호 업그레이드가 데이터 정의만큼 효과를 줄인다.
- 드론/위험 HUD/Run State가 동일 exposure를 표시한다.

