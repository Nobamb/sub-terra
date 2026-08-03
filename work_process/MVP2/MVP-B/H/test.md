# Phase H Agent Test

## 1. 정적 검증 항목

| ID | 테스트 항목 | 검증 방법 | 예상 결과 |
| :-- | :-- | :-- | :-- |
| H-S01 | 효과 소유권 | 호출 그래프 검사 | Zone 판정과 상태 변이 분리 |
| H-S02 | tick 정책 | 설정/코드 검사 | 고정 간격 데이터화 |
| H-S03 | Player 연결 | Scene 참조 검사 | Energy/Movement/Visual 연결 |
| H-S04 | 접근성 | Prefab 검사 | 색상 외 아이콘/텍스트 존재 |

## 2. 기능 테스트 항목

### H-F01: 노출 효과

- **준비:** 고정 강도 Gas Zone
- **실행:** Player가 일정 시간 머문다.
- **예상 결과:** 예상 tick 수만큼 전력이 줄고 속도·시야가 변경된다.

### H-F02: 이탈 복원

- **준비:** 효과가 활성화된 Player
- **실행:** Zone 밖으로 이동한다.
- **예상 결과:** 감속과 시야 효과가 즉시 해제되고 누적 정책이 적용된다.

### H-F03: 중첩 Zone

- **준비:** 강도가 다른 Zone 두 개
- **실행:** 중첩 영역에 진입한다.
- **예상 결과:** 명시한 최고 강도/합산 정책대로 한 번만 적용된다.

### H-F04: 보호 업그레이드

- **준비:** 동일 노출, 업그레이드 전/후
- **실행:** 전력/노출 변화를 비교한다.
- **예상 결과:** 카탈로그 효과 값만큼 불이익이 감소한다.

### H-F05: 상태 일치

- **준비:** 실제 Integration Scene
- **실행:** 가스 진입/이탈한다.
- **예상 결과:** Gameplay, GameState, HUD, 드론 Context의 위험 단계가 일치한다.

## 3. 테스트 절차

1. Edit Mode에서 tick/저항 계산을 검증한다.
2. Play Mode에서 Zone 이동, 실제 속도와 UI를 검증한다.
3. 프레임률을 바꿔 같은 경과 시간 결과가 같은지 확인한다.

## 4. 검증 결과 요약

- **상태:** 통과 (2026-08-03, Unity 6000.5.4f1)
- **Phase H Edit Mode:** 16/16 통과
- **Phase H Play Mode:** 1/1 통과
- **전체 App Edit Mode 회귀:** 287/287 통과
- **전체 App Play Mode 회귀:** 27/27 통과
- **H-S01~H-S04:** Zone 판정과 효과 적용 분리, 1초 고정 tick, Energy/Movement/Visual 연결, 텍스트·기호·색상 경고를 확인했다.
- **H-F01:** 강도 비례 전력 감소와 누적 노출이 고정 tick 수만큼 적용되고 전력이 0 아래로 내려가지 않는다.
- **H-F02:** Zone 이탈 즉시 감속·시야 제한이 해제되고 누적 노출은 tick마다 회복한다.
- **H-F03:** 중첩 Zone은 기존 `GasHazardSystem`의 최고 강도 하나만 선택하여 중복 적용하지 않는다.
- **H-F04:** `upgrade.gas.resistance` 카탈로그 효과가 모든 불이익을 비례 감소시키며, 활성 전진기지 상호작용 범위는 정화 보호를 제공한다.
- **H-F05:** Gameplay 효과 상태, GameState RunState, HUD가 같은 위험 단계를 표시하며 누적 한계 도달 시 `GasExposureFailureInputDto`를 한 번 발행한다.
- **Integration Scene:** `GasExposureEffectController`와 입력을 막지 않는 `GasVisionOverlay` 참조를 Unity Editor에서 연결했다.
- **Unity Console:** 오류 0건. 확인된 경고는 Bootstrap 없이 Integration Scene을 여는 기존 테스트 경고와 기존 TMP 글리프 경고다.
- **판정:** Phase H 완료. 실제 피해·행동불능·구조 결과 처리는 Shared 실패 입력을 소비하는 Phase L 범위다.

