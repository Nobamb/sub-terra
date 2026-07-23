# Phase M — Integration Scene 구성

## 1. 개요

`process-B`의 B-12를 구현한다. A의 검증된 Runtime Prefab과 B의 State·UI·Save 서비스를 `Mine_Demo_Integration.unity` 하나에 연결한다. 이 Scene은 B만 수정한다.

## 2. 작업 목표

- Grid/Tilemap, GameplayRoot, ApplicationRoot, HUDCanvas와 EventSystem의 기준 계층을 만든다.
- Shared 인터페이스와 이벤트를 통해 A Producer와 B Consumer를 연결한다.
- 저장 복원과 HUD 활성화의 순서를 보장한다.
- A Runtime Prefab 내부를 수정하지 않고 전체 플레이 루프를 통합한다.

## 3. 구현 범위

- `Scenes/Integration/Mine_Demo_Integration.unity`
- 필요한 `Scripts/App/Core/Integration/` bridge와 B 소유 Prefab Variant/View
- `Tests/PlayMode/Integration/`
- `docs/INTEGRATION_GUIDE.md`의 실제 참조·설치·검증 정보

## 4. 권장 구현 방향

1. A의 인수인계 문서, Test Scene, Runtime Prefab, 필수 Tilemap/좌표/참조, 이벤트와 검증 절차를 먼저 확인한다. 단독 Test Scene에서 A 기능을 재현한 뒤 통합을 시작한다.
2. Integration 전용 브랜치와 Scene 변경 전 상태를 확보한다. Unity Editor에서 Scene을 만들고 YAML을 직접 편집하지 않는다.
3. 기준 계층을 만든다: Main Camera, Global Light 2D, Grid의 Background/Foreground/Hazard/Building Tilemap, GameplayRoot, ApplicationRoot, HUDCanvas, EventSystem.
4. A Runtime Prefab을 원본 그대로 배치하고 Tilemap, 카메라, RuntimeBuildings Root 등 인수인계된 참조를 연결한다.
5. ApplicationRoot에 GameStateBridge, InventoryService, EconomyService, DroneAnalysisService, SaveBridge를 배치한다. Bootstrap의 전역 인스턴스와 중복 생성되지 않도록 Scene 로컬 binder 역할을 구분한다.
6. `IMiningRewardReceiver`, `IResourceWallet`, `IGameplayEventSink`, `IWorldSnapshotProvider`, `IDroneContextProvider`의 Producer/Consumer 참조를 하나씩 연결하고 각 연결 직후 최소 시나리오를 실행한다.
7. HUD를 State 이벤트에 연결하고 최초 렌더를 확인한다. 구조/가스/건설/드론 이벤트가 같은 EventSink를 통해 올바른 읽기 State로 가는지 확인한다.
8. 이어하기 진입에서는 B State가 준비된 뒤 Scene을 열고, A 월드 복원과 파생 계산 완료 후 HUD/입력을 활성화한다.
9. Prefab 변경이 필요하면 B 시각 요소는 ViewSocket 또는 B 소유 Variant로 해결한다. Gameplay 동작 변경은 A에게 재현 Issue를 보낸다.
10. Missing 참조, 중복 EventSystem/서비스, 비활성 객체와 Console Warning을 정리하되 관련 없는 코드 변경은 같은 PR에 넣지 않는다.
11. 실제 Scene 계층과 참조 목록, A/B 연결표, 저장 복원 순서와 수동 검증을 `docs/INTEGRATION_GUIDE.md`에 갱신한다.

## 5. 보안 및 안정성 기준

- A Prefab/Gameplay 코드와 Shared 계약을 합의 없이 변경하지 않는다.
- Scene 오브젝트를 Save DTO에 직접 넣지 않는다.
- 중복 이벤트와 중복 결제로 이어질 수 있는 Inspector 참조를 검증한다.
- Scene/Prefab `.meta`를 함께 추적하고 프로젝트 버전을 변경하지 않는다.

## 6. 완료 기준

- A Runtime의 이동·채굴·건설·위험·드론·월드 저장이 B 서비스와 계약으로 연결된다.
- HUD, 경제, 추천과 자동 저장이 실제 Gameplay 이벤트에 반응한다.
- 새 게임과 이어하기 모두 같은 Integration Scene에서 동작한다.
- Console Error가 없고 예상하지 않은 Warning과 Missing 참조가 없다.
