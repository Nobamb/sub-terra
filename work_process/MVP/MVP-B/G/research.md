# Phase G — 건설 메뉴와 위험 UI

## 1. 개요

`process-B`의 B-6을 구현한다. B의 데이터·경제·UI를 A의 건설 배치, 구조 안정도와 가스 결과에 연결하되 Gameplay 계산을 다시 구현하지 않는다.

## 2. 작업 목표

- 시설 목록, 비용, 설명, 전력, 보유 자원과 선택 상태를 표시한다.
- A의 Preview/유효성 결과와 B의 비용 가능 여부를 함께 보여준다.
- 구조·가스·전력 연결 상태를 즉시 이해할 수 있는 HUD로 표현한다.
- 설치 성공/취소 뒤 선택과 UI 상태를 확실히 초기화한다.

## 3. 구현 범위

- `Scripts/App/UI/Building/`, `Scripts/App/UI/Hazards/`
- `Prefabs/UI/BuildingMenu.prefab`, `StructuralHUD.prefab`, `GasWarningPanel.prefab`
- Phase B BuildingData, Phase E `IResourceWallet`, Phase C HUD 이벤트
- `Tests/PlayMode/Integration/BuildingUI/`

A의 `BuildingPlacementSystem`, 구조·가스 계산, Runtime Prefab 내부는 수정하지 않는다.

## 4. 권장 구현 방향

1. A 인수인계에서 시설 선택 입력, Preview 시작/취소, 위치 유효성, 설치 결과, 구조·가스·전력 이벤트 시그니처와 필수 참조를 확인한다.
2. BuildingMenu는 카탈로그의 시설 목록을 읽어 항목을 만들고, 선택 시 ID 또는 합의된 DTO만 A 경계에 전달한다.
3. “설치 가능”은 A의 위치 유효성과 B의 `CanAfford`가 모두 참일 때만 표시한다. 각각의 실패 이유를 구분한다.
4. A가 Runtime Prefab 생성 성공을 알린 뒤에만 `TrySpend`가 실행되는 연결을 만든다. 실패/취소에서는 결제하지 않는다.
5. 설치 성공, 취소, Scene 종료에서 선택 State와 Preview를 모두 해제한다. UI만 닫히고 A Preview가 남지 않게 양쪽 계약을 호출한다.
6. 구조 안정도와 가스 노출 값은 A 이벤트의 실제 수치를 State에 저장해 등급·색·텍스트로 표현한다. UI가 타일이나 Collider를 조회해 재계산하지 않는다.
7. 색만으로 위험을 전달하지 말고 아이콘/문구/수치를 함께 쓴다. 급박한 가스 경고는 일반 건설 정보보다 시각 우선순위를 높인다.
8. 전력 연결 상태와 충전기·정산 콘솔 상호작용 안내는 A가 제공한 활성/원인 값 그대로 표시한다.

## 5. 보안 및 안정성 기준

- 카탈로그에 없는 시설이나 null Runtime Prefab은 선택·설치를 막는다.
- 설치 버튼 연타와 성공 이벤트 중복으로 이중 결제되지 않게 한다.
- A 소유 Prefab을 수정하지 않고 필요한 B 시각 요소는 ViewSocket 또는 B 소유 Variant에 둔다.
- 이벤트 구독 수명과 Scene 전환 해제를 검증한다.

## 6. 완료 기준

- 시설을 선택하고 Preview를 시작·취소·성공할 수 있다.
- 자원 부족과 위치 부적합 이유가 구분되어 표시된다.
- 성공 후 한 번만 결제되고 선택 상태가 종료된다.
- 구조·가스·전력 경고가 A의 실제 상태와 즉시 일치한다.
