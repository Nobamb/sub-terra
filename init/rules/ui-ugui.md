# UI·uGUI·Layout Builder 규칙

2-5. UI Prefab·Scene 변경 범위 보호 (의도하지 않은 레이아웃 회귀 방지)
   시설 건설 창 하나만 고쳐야 하는데 Surface Base·설정 창·인벤토리·세이브 슬롯 프리팹이 한꺼번에 바뀌는 회귀가 발생한 바 있다.
   Agent와 개발자는 UI 작업에서 아래 규칙을 반드시 지킨다.

   2-5-1. 변경 범위는 요청 문장과 동일해야 한다
   - 작업 요청에 명시된 UI만 수정한다. 요청에 없는 Surface Base, Main Menu, 설정 창, 인벤토리, 세이브 슬롯, 드론 패널 등은 열거나 저장하지 않는다.
   - “시설 건설 창 너비 조정”처럼 단일 패널 요청이면 `BuildingMenu` 프리팹과 그 패널이 배치된 Integration Scene 인스턴스만 대상이다.
   - 요청에 없는 Prefab·Scene이 dirty 되어도 커밋에 넣지 않고 원상 복구한다.
   - “관련 파일이 같이 수정됐다”는 이유로 범위를 확장하지 않는다. 확장이 필요하면 별도 요청·별도 커밋으로 분리한다.

   2-5-2. Layout Builder·Editor 스크립트 작성 규칙
   - 빌더는 대상 경로를 상수 배열 또는 명시적 상수로만 관리한다. 예: `BuildingMenuPrefabPath`, `IntegrationScenePath`.
   - `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset`은 위 상수 경로에만 호출한다. 프로젝트 전체 Prefab 순회 저장을 금지한다.
   - `EditorSceneManager.OpenScene` 후 저장은 해당 작업 Scene 경로 한 개만 대상으로 한다. 열려 있던 다른 Scene을 저장하지 않는다.
   - 빌더 종료 시 이전에 열어 둔 Scene으로 되돌리거나, 최소한 작업 Scene만 `SaveScene`한다.
   - 한 빌더 안에서 Surface Base 레이아웃 + 설정 창 + 시설 창을 동시에 고치지 않는다. 단계·프롬프트 단위로 빌더를 분리한다.
   - 메뉴 항목 이름에 대상 UI를 적는다. 예: `SubTerra/UI/Build Prompt-B 35 Building Panel Layout`.

   2-5-3. Unity Editor 작업 중 부작용 차단
   - UI 작업 전 수정 대상 Prefab·Scene 목록을 먼저 적고, 그 목록 밖 에셋은 Hierarchy/Project에서 불필요하게 열지 않는다.
   - TextMeshPro 폰트 아틀라스 재생성(`Font Asset` 갱신, SDF 재베이크)은 UI Prefab 다수를 dirty 시킬 수 있다. 폰트 변경이 작업 목표가 아니면 수행하지 않는다.
   - 폰트를 꼭 갱신했다면 `git status`로 영향받은 Prefab 전량을 확인하고, 이번 작업 대상이 아닌 파일은 모두 restore 한다.
   - Prefab Mode에서 편집 후 Apply/Save 시 상위 Canvas나 다른 패널 인스턴스까지 덮어쓰지 않았는지 Inspector와 Hierarchy를 확인한다.
   - Play Mode 진입·종료, Domain Reload, 스크립트 재컴파일 직후 자동 저장된 Scene·Prefab이 있는지 `git status`로 확인한다.
   - Integration Scene을 연 상태에서 무관한 패널 인스턴스를 선택·이동·리사이즈하지 않는다.

   2-5-4. 커밋 전 필수 검증 (범위 가드)
   - `git status`와 `git diff --stat`로 변경 파일 목록을 확인한다. 요청 범위 밖 Prefab·Scene·Font 에셋이 있으면 커밋하지 않는다.
   - 특히 아래 파일은 “이번 요청에 명시되지 않았다면” 커밋 금지다.
     - `Prefabs/UI/SurfaceBasePanel.prefab`
     - `Prefabs/UI/MainMenuPanel.prefab`
     - `Prefabs/UI/InventoryPanel.prefab`
     - `Prefabs/UI/SaveSlotPanel.prefab`
     - `Scenes/App/SurfaceBase.unity`
     - `Scenes/App/MainMenu.unity`
     - `Fonts/*_SDF.asset` 및 공용 TMP 설정
   - 범위 밖 파일이 수정됐으면 `git restore -- <path>`로 되돌린 뒤, 의도한 파일만 stage 한다.
   - 커밋 메시지·요약에 실제 변경 대상 UI 이름을 적고, 목록 밖 파일이 0개임을 확인한다.
   - Agent는 커밋 직전에 변경 파일 목록을 사용자에게 보여 주거나, 규칙상 범위 이탈 시 커밋을 중단하고 보고한다.

   2-5-5. 회귀 탐지와 복구
   - 핵심 UI(Surface Base 본문 크기, 설정 창 앵커·너비, 해상도·프레임 드롭다운 존재 여부 등)는 Edit Mode 테스트 또는 레이아웃 검증 빌더로 고정값을 검사한다.
   - 의도하지 않은 UI 회귀가 보이면 main(또는 합의된 기준 브랜치)의 해당 Prefab·Scene만 checkout/restore 하고, 이후 공식 Layout Builder 체인으로 필요한 단계만 재적용한다.
   - 복구 후 반드시 설정 창의 해상도·언어·프레임 드롭다운 배선, Surface Base 본문 크기, 요청 대상 패널(예: 시설 건설)이 여전히 올바른지 교차 확인한다.
   - “전체가 이상하니 모든 UI Prefab을 한 번에 다시 빌드”하지 않는다. 깨진 파일만 기준 브랜치에서 복구한 뒤 필요한 빌더만 실행한다.

   2-5-6. 허용되는 예외
   - 사용자가 여러 UI를 한 요청에 명시한 경우(예: Surface Base와 설정 창을 main 기준으로 복원)에만 복수 경로를 수정한다. 이때도 빌더·커밋 메시지에 대상 목록을 전부 적는다.
   - Shared 계약 변경, 폰트 교체, 전역 테마처럼 다수 Prefab이 필연적으로 바뀌는 작업은 별도 PR로 분리하고, 변경 파일 목록과 회귀 테스트 결과를 반드시 첨부한다.
   - 예외 작업에서도 2-5-4의 커밋 전 목록 확인은 생략하지 않는다.
