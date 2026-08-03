# Phase F Agent Test

## 1. 정적 검증 항목

| ID | 테스트 항목 | 검증 방법 | 예상 결과 |
| :-- | :-- | :-- | :-- |
| F-S01 | Support Prefab | 컴포넌트/VisualRoot 검사 | Collider, Support, BuildingInstance 존재 |
| F-S02 | 배치 참조 | Integration Scene 검사 | menu/bridge/system/preview 연결 |
| F-S03 | 비용 순서 | 코드 경로 검사 | 생성 성공 뒤 차감 |
| F-S04 | Snapshot 정의 | 저장 계약 검사 | ID·좌표·상태만 포함 |

## 2. 기능 테스트 항목

### F-F01: Preview와 성공 설치

- **준비:** 충분한 자원과 빈 지면
- **실행:** 버팀목 선택 후 커서를 움직이고 좌클릭한다.
- **예상 결과:** 유효 Preview와 실제 Support가 같은 cell에 나타난다.

### F-F02: 무효 위치

- **준비:** 암석 내부, 공중, 기존 시설, 거리 밖 위치
- **실행:** 각 위치에 설치한다.
- **예상 결과:** 실패 이유가 표시되고 자원/월드가 불변이다.

### F-F03: 구조 보강 효과

- **준비:** 위험 상태와 설치 가능한 근처 위치
- **실행:** Support를 설치하고 재계산한다.
- **예상 결과:** 영향 범위 내 위험 점수만 감소한다.

### F-F04: 중복 클릭

- **준비:** 한 번 설치 가능한 자원
- **실행:** 같은 프레임에 중복 확정을 보낸다.
- **예상 결과:** Prefab/비용/이벤트가 한 번만 발생한다.

### F-F05: 저장 복원

- **준비:** Support 설치 후 저장
- **실행:** 새 Runtime을 구성해 로드한다.
- **예상 결과:** 비용 재차감 없이 같은 위치와 효과로 복원된다.

## 3. 테스트 절차

1. Edit Mode에서 배치 판정과 구조 점수를 검증한다.
2. 실제 UI와 마우스가 포함된 Play Mode 시나리오를 실행한다.
3. 저장 왕복은 M 단계에서 전체 회귀로 다시 실행한다.

## 4. 검증 결과 요약

- **실행일:** 2026-08-03
- **상태:** 통과
- **Edit Mode:** 281 Pass / 0 Fail / 0 Skip
- **Play Mode:** 26 Pass / 0 Fail / 0 Skip

### 항목별 결과

| ID | 결과 | 자동 검증 근거 |
| :-- | :-- | :-- |
| F-S01 | 통과 | Support Prefab의 `BoxCollider2D`, `StructuralSupport`, `BuildingInstance`, `VisualRoot` 검사 |
| F-S02 | 통과 | Integration Scene의 menu/bridge/system/preview와 배치 원점·거리·허용 영역 참조 검사 |
| F-S03 | 통과 | 성공 1회 설치 시 Prefab 1개 생성·비용 1회 차감, 실패 시 미차감 검사 |
| F-S04 | 통과 | `BuildingSnapshotDto` 필드와 Unity Object 참조 부재 검사 |
| F-F01 | 통과 | 실제 placement bridge의 선택/Preview 흐름과 Runtime 설치 경로 검사 |
| F-F02 | 통과 | 지면 없음·점유·거리 밖·허용 영역 밖 실패 enum 및 월드 불변 검사 |
| F-F03 | 통과 | 설치 반경 안은 Stable로 감소하고 반경 밖은 Caution 유지 검사 |
| F-F04 | 통과 | 성공 직후 선택 해제 및 같은 프레임 재호출의 `NoSelection`, 비용/Prefab 1회 검사 |
| F-F05 | 통과 | Snapshot 복원 시 비용 재차감 없이 같은 위치의 Support 효과와 범위 밖 불변 검사 |

### 실행 기록

- Unity 6000.5.4f1 Editor에서 Phase F 에셋 빌더를 실행해 Support Prefab/Data/Integration Scene 참조를 생성·저장했다.
- `SubTerra.App.Tests.EditMode`, `SubTerra.Gameplay.Building.EditModeTests`, `SubTerra.Gameplay.Snapshot.EditModeTests`를 함께 실행했다.
- `SubTerra.App.Tests.PlayMode` 전체 회귀를 실행했다.

