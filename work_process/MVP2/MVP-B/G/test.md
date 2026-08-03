# Phase G Agent Test

## 1. 정적 검증 항목

| ID | 테스트 항목 | 검증 방법 | 예상 결과 |
| :-- | :-- | :-- | :-- |
| G-S01 | 임계치 데이터 | 코드/에셋 검사 | 단일 데이터 원천 |
| G-S02 | Overlay 분리 | Scene 검사 | 원본/균열 Tilemap 분리 |
| G-S03 | 이벤트 DTO | Shared 검사 | cell/severity, Unity Object 없음 |
| G-S04 | 국소 계산 | 소스/Profiler 검사 | 전체 맵 매 프레임 탐색 없음 |

## 2. 기능 테스트 항목

### G-F01: 위험 단계 전이

- **준비:** 고정 타일 배치와 채굴 순서
- **실행:** 위험 점수를 단계별로 증가시킨다.
- **예상 결과:** 안정→주의→위험→임박 표현이 순서대로 바뀐다.

### G-F02: 결정론적 붕괴

- **준비:** 같은 Seed와 같은 채굴 이벤트
- **실행:** 두 Runtime에서 붕괴를 발생시킨다.
- **예상 결과:** 붕괴 cell 목록이 동일하다.

### G-F03: Support 완화

- **준비:** 균열이 있는 영역
- **실행:** 영향 범위 안/밖에 Support를 설치한다.
- **예상 결과:** 안쪽만 위험과 균열이 감소한다.

### G-F04: 경계 보호

- **준비:** 경계/코어 인접 붕괴 후보
- **실행:** 임계 상태를 발생시킨다.
- **예상 결과:** 보호 대상은 제거되지 않는다.

### G-F05: 충돌과 Snapshot

- **준비:** 붕괴 직전 Player와 저장 Provider
- **실행:** 붕괴 후 저장/복원한다.
- **예상 결과:** 충돌체와 복원 타일 상태가 일치한다.

## 3. 테스트 절차

1. Edit Mode에서 점수와 후보 결정을 검증한다.
2. Play Mode에서 overlay, collider, event를 검증한다.
3. 모션 감소 설정에서 화면 흔들림 비활성화를 확인한다.

## 4. 검증 결과 요약

- **상태:** 통과 (2026-08-03, Unity 6000.5.4f1)
- **Edit Mode:** `SubTerra.Gameplay.Structural.EditModeTests` 13/13 통과
- **Play Mode:** `SubTerra.Gameplay.Structural.PlayModeTests` 2/2 통과
- **Scene 검사:** Structural Test, DemoWorld Test, Integration Scene 모두 임계치 에셋·분리 Overlay·AudioSource 연결 통과. Overlay에는 Collider가 없다.
- **G-S01~G-S04:** 단일 `StructuralRiskSettings`, Shared 붕괴 DTO, 변경 셀 주변 국소 재계산, 별도 Overlay로 통과
- **G-F01~G-F04:** 4단계 전이, 동일 Seed/상태의 동일 cell, Support 국소 완화, 경계/코어 보호로 통과
- **G-F05:** 붕괴 직후 `TilemapCollider2D` 갱신과 Snapshot 캡처·복원 후 제거 타일 일치를 Play Mode에서 통과
- **접근성:** 모션 감소 설정에서 화면 흔들림 요청이 발생하지 않음을 검증
- **판정:** Phase G 완료. 실제 Player 피해·행동불능·실패 처리는 Shared 붕괴 이벤트를 소비하는 Phase L 범위다.

