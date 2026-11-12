# 구조 판정과 설치 미리보기 사이의 어셈블리 경계

- 날짜: 2026-11-12
- 태그: structural, building, asmdef, compile
- 실수: 건설 미리보기에서 구조 어셈블리의 `internal` 안전 거리 상수를 참조해 컴파일에 실패했다.
- 원인: 두 코드가 Gameplay 폴더에 있어 같은 어셈블리라고 판단했다.
- 다음부터 할 일: 공통 판정을 연결하기 전에 양쪽 `.asmdef`와 멤버 접근 범위를 확인하고, 한쪽에만 정의한 판정 기준을 필요한 범위에서 공개한다.
- 관련: #281, StructuralIntegritySystem, BuildingPlacementPreview
