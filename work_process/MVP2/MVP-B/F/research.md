# Phase F — 버팀목 월드 배치와 구조 보강

## 1. 개요

배치 시스템과 Preview는 존재하지만, 구매/선택 후 실제 월드 클릭 설치와 구조 위험 회복이 Integration Scene에서 하나의 사용자 흐름으로 검증되어야 한다.

## 2. 작업 목표

- 건설 메뉴에서 버팀목을 선택하면 커서 Preview를 표시한다.
- 유효/무효 위치와 실패 이유를 즉시 보여준다.
- 좌클릭 성공 시 Runtime Support를 생성하고 비용을 한 번만 차감한다.
- 설치된 Support가 주변 구조 계산에 실제 영향을 준다.

## 3. 구현 범위

- Building menu→placement bridge→Gameplay placement 연결
- Support 전용 Runtime Prefab, Collider, `StructuralSupport`, VisualRoot
- 지면/겹침/거리/허용 영역/자원 검증
- 취소/Scene 종료/설치 성공 시 선택과 Preview 초기화
- 설치 이벤트, Snapshot 등록과 tutorial signal

## 4. 권장 구현 방향

1. 기존 `BuildingPlacementSystem`, Preview, `GameplayBuildingPlacementBridge`를 확장한다.
2. App는 비용과 선택 UI를, Gameplay는 위치 판정과 Prefab 생성을 소유한다.
3. 성공 순서는 위치 검증 → 비용 가능 확인 → Prefab 생성 → 비용 차감 → 등록이다.
4. Support 영향 반경과 강도는 데이터/Prefab에 두고 UI는 예상 변화만 표시한다.
5. 설치 거리 제한은 Player 위치와 cell center를 사용한다.

## 5. 보안 및 안정성 기준

- 배치 실패/생성 실패/중복 클릭 시 자원 차감이 없다.
- UI가 구조 안정도를 직접 변경하지 않는다.
- 설치 성공 후 `StructuralIntegritySystem.RegisterSupport`가 한 번 호출된다.
- 복원 설치는 비용을 차감하지 않고 occupied cells를 재구성한다.

## 6. 완료 기준

- 실제 Integration Scene에서 메뉴→Preview→클릭→Support 생성이 동작한다.
- 버팀목 범위 내 위험 점수가 낮아지고 범위 밖은 변하지 않는다.
- 모든 실패 이유가 사용자에게 표시된다.
- 저장/로드 후 위치와 구조 보강 효과가 유지된다.

