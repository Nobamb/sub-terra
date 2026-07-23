# Phase C — 기본 HUD와 Game State

## 1. 개요

`process-B`의 B-2를 구현한다. HUD를 State의 읽기 전용 표현으로 만들고 상태 변경 이벤트가 발생한 항목만 갱신한다.

## 2. 작업 목표

- 전력, 깊이, 골드, 화물, 가치, 구조, 가스, 건설 선택과 상호작용 안내를 표시한다.
- UI가 State를 직접 변경하지 않게 한다.
- Scene 로드 후 UI 구독과 참조가 다시 연결되고, 파괴된 UI가 이벤트에 남지 않게 한다.
- 여러 해상도에서 안전 영역과 레이아웃을 유지한다.

## 3. 구현 범위

- `Scripts/App/State/`: 변경 이벤트와 읽기 모델
- `Scripts/App/UI/HUD/`: presenter/view 바인딩
- `Prefabs/UI/BasicHUD.prefab`, `StructuralHUD.prefab`, `GasWarningPanel.prefab`
- `Tests/EditMode/App/UI/`, `Tests/PlayMode/Integration/HUD/`

## 4. 권장 구현 방향

1. 각 HUD 값의 원천 State와 표시 형식을 표로 정한다. 데이터가 없을 때 `0`, `안전`, `선택 없음` 등 명시적 기본값을 정한다.
2. `EnergyChanged`, `CreditsChanged`, `InventoryChanged`, `DepthChanged`, `StructuralRiskChanged`, `GasExposureChanged`, `BuildingSelectionChanged` 이벤트의 payload를 현재 값 또는 읽기 모델로 통일한다.
3. Presenter는 `OnEnable`에서 구독하고 최초 전체 렌더를 한 번 수행하며, `OnDisable`에서 반드시 해제한다.
4. `Update()`에서 텍스트를 재설정하지 않는다. 같은 값 설정 시 이벤트를 중복 발행하지 않도록 State/Service 경계에서 막는다.
5. View는 TextMeshPro, Slider, 경고 GameObject 등 직렬화 참조만 관리하고 상태 계산을 하지 않는다.
6. Canvas Scaler와 Anchor를 설정하고 16:9, 16:10, 21:9, 최소 지원 해상도에서 겹침과 화면 이탈을 확인한다.
7. Scene 전환 시 새 HUD가 전역 GameState를 찾아 구독하도록 ApplicationRoot 또는 명시적 바인더를 사용한다.
8. 구조·가스 값은 A의 이벤트/DTO를 받아 B의 표시 State로 변환한다. A의 계산을 UI에서 재계산하지 않는다.

## 5. 보안 및 안정성 기준

- 누락 참조는 값 조작으로 숨기지 않고 Prefab 검증 오류로 드러낸다.
- 사용자에게 보여주는 숫자는 실제 State에서 파생하며 디버그용 임의 값과 분리한다.
- 이벤트 구독 해제로 파괴된 UI 호출과 메모리 누수를 막는다.
- 매 프레임 문자열 할당을 피한다.

## 6. 완료 기준

- 모든 HUD 항목이 State의 현재 값과 일치한다.
- 변경된 항목만 즉시 갱신되고 같은 값에는 불필요한 갱신이 없다.
- Scene 왕복과 HUD 재생성 후 중복 구독이나 MissingReferenceException이 없다.
- 대상 해상도에서 주요 HUD가 화면 안에 있고 읽을 수 있다.
