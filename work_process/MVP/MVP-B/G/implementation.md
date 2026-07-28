# Phase G 구현 기록

## 1. 구현 범위

- `BuildingData`에 시설 설명을 추가하고 MVP 6종 시설의 이름·설명·비용·전력 데이터를 메뉴 원천으로 사용했다.
- `BuildingMenuPresenter`가 시설 목록, 보유량/필요량, 선택 상태, A 위치 유효성, B 비용 가능 여부를 조합한다.
- `GameplayBuildingPlacementBridge`가 A의 `BuildingPlacementSystem`을 호출하고 Preview 시작·갱신·취소·성공·실패를 Shared DTO로 변환한다.
- A가 Runtime Prefab을 생성한 뒤에만 Bridge의 `IResourceWallet.TrySpend`가 호출되므로 실패/취소와 성공 이벤트 중복으로 재결제되지 않는다.
- `GameplayHazardStatusBridge`가 A의 구조·가스·전력 이벤트와 `OutpostStatusDto`를 위험 HUD 및 상호작용 안내로 전달한다.
- `BuildingMenu.prefab`을 생성하고 `HUDCanvas.prefab`에 구조·가스·전력 통합 View를 연결했다.

## 2. 책임 경계

- 위치, 지형, 구조, 가스, 전력망 계산은 A 구현을 그대로 사용한다.
- B UI는 `BuildingPlacementResultDto`, A의 위험 enum, `PowerNetworkSnapshot`, `OutpostStatusDto`를 표시용 읽기 모델로 변환할 뿐 Gameplay 임계치나 설치 결과를 다시 계산하지 않는다.
- `Scripts/Gameplay/`와 A Runtime Prefab은 수정하지 않았다.
- 시설 비용은 `BuildingData.BuildCosts` 한 경로만 사용하며 메뉴와 실제 지갑 어댑터가 같은 데이터를 읽는다.

## 3. Unity 연결 방법

통합 Scene의 B 소유 Root에 아래 컴포넌트를 둔다.

1. `GameplayBuildingPlacementBridge`
   - `placementSystem`: A의 `BuildingPlacementSystem`
   - `preview`: A의 `BuildingPlacementPreview`
   - `sceneReferences`: A의 `BuildingPlacementSceneReferences`
   - `bindings`: 6종 시설 영구 ID와 A의 `BuildingPlacementDefinition`
2. A의 `BuildingPlacementSystem.resourceWalletBehaviour`에 위 Bridge를 연결한다.
3. `GameplayHazardStatusBridge`
   - `structuralSystem`: A의 `StructuralIntegritySystem`
   - `gasSystem`: A의 `GasHazardSystem`
   - `powerSystem`: A의 `PowerNetworkSystem`
4. `BuildingUiIntegrationBinder`
   - 카탈로그, 두 Bridge, `BuildingMenuBinder`, `HazardHudBinder`를 연결한다.
   - App 서비스 생성 뒤 `BindTo(IResourceWallet, InventoryService, GameState)`를 한 번 호출한다.

통합 Scene 파일은 현재 저장소에 없으므로 A Scene/Prefab 참조를 임의 추측해 생성하지 않았다. 실제 A 선택/취소 연결은 Play Mode 대역이 아닌 `BuildingPlacementSystem` 인스턴스로 검증했다.

## 4. 검증 결과

- App Edit Mode: 143개 통과, 실패 0
- App Play Mode: 9개 통과, 실패 0
- G 기능 검증:
  - 시설 선택 → Preview 시작 → 취소 및 상태 초기화
  - 위치 유효/자원 부족 사유 분리
  - 생성 실패 시 무결제와 선택 초기화
  - 성공 이벤트 중복 시 UI 재결제 없음
  - 구조·가스·전력 상태와 가스 Critical 우선 표시
  - 전진기지 실제 공급/소비 수치 및 충전기 상호작용 안내
  - Prefab 필수 참조와 6종 시설 버튼 확인

## 5. 알려진 제한

- 프로젝트 기본 `LiberationSans SDF`에는 한글 글리프가 없어 Play Mode 렌더 중 TMP 누락 글리프 경고가 발생한다. G 로직/테스트 오류는 아니지만, 배포 UI에는 라이선스가 확인된 한글 TMP Font Asset과 fallback 설정이 필요하다.
- 최종 Integration Scene이 추가되면 위 Inspector 참조를 연결한 수동 입력 테스트를 한 번 더 수행해야 한다.
