# Phase B — ScriptableObject 데이터 카탈로그

## 1. 개요

`process-B`의 B-1을 구현한다. 광물, 시설, 레시피, 업그레이드, 대사 정의를 ScriptableObject 에셋으로 만들고, 영구 ID로 안전하게 조회·검증하는 단일 카탈로그를 구축한다.

## 2. 작업 목표

- 표시 이름과 저장/연동용 ID를 분리한다.
- 구리·철·리튬, MVP 시설과 업그레이드 정의를 코드 수정 없이 편집한다.
- 중복 ID와 필수 참조 누락을 실행 전 자동으로 찾는다.
- A는 구체 App 클래스가 아니라 합의된 데이터/Shared 경계로 읽을 수 있게 한다.

## 3. 구현 범위

- `Scripts/App/Core/Data/`: 데이터 조회와 검증 결과
- `Data/Minerals/`, `Buildings/`, `Recipes/`, `Upgrades/`, `Dialogue/`
- `Editor/DataValidation/`: 검증 창 또는 메뉴 명령
- `Tests/EditMode/App/Data/`

필수 ID는 `mineral.copper`, `mineral.iron`, `mineral.lithium`, process-B에 열거된 MVP 건물/업그레이드 ID다.

## 4. 권장 구현 방향

1. `MineralData`, `BuildingData`, `RecipeData`, `UpgradeData`, `DialogueTemplateData`의 최소 필드를 표로 먼저 확정한다. ID, 표시명과 각 타입의 계산 필드만 넣는다.
2. ID는 소문자 점 구분 규칙으로 정규화된 값을 에셋에 저장한다. 런타임에 표시명으로 조회하지 않는다.
3. `GameDataCatalog`가 각 데이터 목록을 보유하고 초기화 시 ID별 Dictionary를 한 번 만든다. 조회 실패는 `TryGet...` 결과와 진단 메시지로 반환한다.
4. 비용은 공용 `ItemCostDto`가 이미 있으면 그대로 사용한다. Shared에 없거나 시그니처가 다르면 임의 변경하지 않고 필요한 계약을 문서화한다.
5. 광물 에셋에 단위 중량과 판매 단가, 시설에 Runtime Prefab·전력·비용, 업그레이드에 단계별 비용·효과·최대 레벨, 대사에 상황 키·우선순위·템플릿을 둔다.
6. Unity Editor에서 최소 데이터 에셋을 생성하고 카탈로그에 명시적으로 등록한다. `Resources.LoadAll` 같은 암묵적 전역 검색은 사용하지 않는다.
7. 검증기는 빈 ID, 형식 오류, 타입 내/타입 간 중복 정책, null Prefab/아이콘, 빈 비용, 잘못된 수치와 카탈로그 미등록 에셋을 보고한다.
8. 검증 결과는 에셋 경로와 필드명을 포함하되, 한 오류 때문에 나머지 검사가 중단되지 않게 모아서 보여준다.
9. Phase A의 카탈로그 로더에 실제 구현을 연결하고 검증 실패 시 Main Menu 진입이 차단되는지 확인한다.

## 5. 보안 및 안정성 기준

- 배포된 ID는 이름 변경 대상으로 취급하지 않는다. 변경 시 Save Migration과 Producer/Consumer 테스트가 필요하다.
- ScriptableObject에 현재 보유량, 레벨, 골드 같은 플레이어 상태를 기록하지 않는다.
- Editor 전용 검증 코드는 런타임 어셈블리에 포함하지 않는다.
- 음수 중량·가격, 0 이하 수량 비용과 누락 Prefab을 배포 전에 실패로 처리한다.

## 6. 완료 기준

- 필수 광물·시설·업그레이드·대사 에셋이 카탈로그에서 ID로 조회된다.
- 중복 ID와 누락 참조를 Editor 도구와 자동 테스트가 찾는다.
- 초기화 시 검증 결과가 Phase A 흐름에 연결된다.
- 표시 이름을 바꿔도 영구 ID와 조회 결과는 변하지 않는다.
