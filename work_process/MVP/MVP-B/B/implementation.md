# Phase B 구현 기록

## 구현

- `Assets/_Project/Scripts/App/Core/Data/`
  - `MineralData`, `BuildingData`, `RecipeData`, `UpgradeData`, `DialogueTemplateData` ScriptableObject
  - `GameDataCatalog` (명시 목록 등록, TryGet-by-ID, `IDataCatalogPort`)
  - `CatalogValidator` / `CatalogValidationResult` (이슈 집계, 중복 시 Dictionary 비성공)
  - `DataIdRules`, `DataIds` 영구 ID 상수
  - `ItemCostEntry` App 로컬 비용 구조 (Shared `ItemCostDto` 미존재 → 문서화 후 로컬 유지)
- `Assets/_Project/Data/` 필수 MVP 에셋 등록
  - 광물 3, 시설 6, 업그레이드 4, 레시피 1, 대사 1
  - `Catalog/GameDataCatalog.asset`에 명시 등록 (Resources 암묵 검색 없음)
- `Assets/_Project/Editor/DataValidation/` (Editor 전용 어셈블리)
  - `SubTerra/Data/Validate Game Data Catalog` 메뉴
  - `MvpDataAssetBuilder` 에셋 생성·Bootstrap 연결
  - Edit Mode 테스트 플래그 러너
- `GameBootstrapper`에 `ScriptableObject` 직렬화 필드를 추가하고 `IDataCatalogPort`로만 접근, Bootstrap 씬에 카탈로그 연결
- Edit Mode 테스트 `Tests/EditMode/App/Data/`
  - B-F01~B-F04, 잘못된 ID/수치 집계, 실제 카탈로그 Bootstrap 성공/실패

## 검증

- Edit Mode `SubTerra.App.Tests.EditMode`: **51 passed / 0 failed**
- 프로젝트 카탈로그 auto-build: `valid=True; errors=0; dictInit=True`
- Bootstrap 씬 `gameDataCatalog` → `GameDataCatalog.asset` GUID 연결 확인
- Unity Play Mode: `Scene=MainMenu`, Root/State/실제 Catalog 존재, `Failed=False`
- Play Mode 카탈로그: `Valid=True`, 구리·기본 지지대 조회 및 양쪽 아이콘 참조 확인
- 소유권: Gameplay/Shared 코드 변경 없음. 런타임 asmdef에 UnityEditor 참조 없음
- 민감 문자열 패턴 스캔: 검출 없음

## 감사 후 보강

- 광물·시설 필수 아이콘 누락을 검증 오류로 처리하고 공용 그레이박스 아이콘 에셋을 명시 연결
- 비용 ID·레시피 결과 시설 ID의 실제 카탈로그 등록 여부 검증
- 시설/업그레이드 빈 비용, 업그레이드 레벨 수·순서·효과 값 검증
- 타입별 ID prefix와 타입 간 중복 ID 검증
- MVP 필수 광물·시설·업그레이드뿐 아니라 지원 시설 레시피와 저전력 대사 ID까지 검증
- 대사 표시 이름 누락 검증
- 검증 실패 카탈로그가 `TryGet` 호출로 부분 조회 딕셔너리를 되살리던 결함 수정
- `Assets/_Project/Data` 내 카탈로그 미등록 에셋을 Editor 전체 검증에서 탐지
- 반복 테스트 실행 시 TestRunner 콜백이 누적되던 문제 수정
- Editor 자동화 실패 로그는 로컬 경로가 포함될 수 있는 예외 메시지 대신 예외 타입만 기록

## 후속 참고

- Shared `ItemCostDto` 합의 시 `ItemCostEntry` 교체
- 시설 아이콘 등 시각 에셋은 그레이박스 단계로 prefab placeholder 사용
- ID 변경 시 Save Migration 필수
