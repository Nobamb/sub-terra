# Project Sub-Terra 프로젝트 구조 안내

이 문서는 `init/` 폴더의 기획서, 작업 규칙, 2인 개발 프로세스와 담당자 A/B 작업지시서를 기준으로 정리한 프로젝트 구조 안내서다.

현재 저장소에는 기획 문서가 먼저 작성되어 있으므로, 아래 구조는 구현 단계에서 생성할 권장 Unity 프로젝트 구조를 설명한다. 실제 구현 중 기존 구조가 생기면 기존 구조를 우선하고, 이 문서는 그 구조에 맞춰 갱신한다.

## 1. 프로젝트 기준

- 프로젝트명: Project Sub-Terra
- 게임 성격: 2D 횡스크롤 기반 지하 탐사·채굴·위험 관리·기지 건설 게임
- 권장 스택: Unity 6.3 LTS, URP 2D Renderer, C#, Input System, Tilemap, Light 2D, Unity UI Canvas, TextMeshPro, Unity Test Framework
- 데이터·저장 기준: ScriptableObject 정적 데이터, C# 런타임 상태, `[Serializable]` DTO, 로컬 JSON 세이브
- 초기 빌드 대상: Windows x64 Standalone

## 2. 전체 폴더 구조

```text
Sub-Terra/
├─ Assets/
│  ├─ _Project/                              # 프로젝트 전용 에셋
│  │  ├─ Art/
│  │  │  ├─ Placeholder/                     # 그레이박스용 임시 에셋
│  │  │  ├─ Sprites/                         # 플레이어, 드론, 시설 Sprite
│  │  │  ├─ Tiles/                           # 암석, 광물, 균열 타일
│  │  │  ├─ UI/                              # HUD와 메뉴 이미지
│  │  │  └─ Materials/
│  │  ├─ Audio/
│  │  │  ├─ BGM/
│  │  │  ├─ SFX/
│  │  │  └─ Mixer/
│  │  ├─ Data/                               # 담당자 B 소유 ScriptableObject 에셋
│  │  │  ├─ Minerals/
│  │  │  ├─ Buildings/
│  │  │  ├─ Recipes/
│  │  │  ├─ Upgrades/
│  │  │  ├─ Contracts/
│  │  │  ├─ Hazards/
│  │  │  └─ Dialogue/
│  │  ├─ Prefabs/
│  │  │  ├─ Gameplay/                        # 담당자 A 소유 Runtime Prefab
│  │  │  │  ├─ Player/
│  │  │  │  ├─ World/
│  │  │  │  ├─ Buildings/
│  │  │  │  ├─ Hazards/
│  │  │  │  └─ Effects/
│  │  │  └─ UI/                              # 담당자 B 소유 HUD와 메뉴 Prefab
│  │  ├─ Scenes/
│  │  │  ├─ Bootstrap/                       # 담당자 B 소유
│  │  │  ├─ MainMenu/                        # 담당자 B 소유
│  │  │  ├─ SurfaceBase/                     # 담당자 B 소유
│  │  │  ├─ Integration/                     # 담당자 B 단독 소유 통합 Scene
│  │  │  └─ Test/
│  │  │     └─ Gameplay/                     # 담당자 A 소유 독립 Test Scene
│  │  ├─ Scripts/
│  │  │  ├─ Gameplay/                        # 담당자 A: 월드와 물리적 상호작용
│  │  │  │  ├─ Player/
│  │  │  │  ├─ Mining/
│  │  │  │  ├─ World/
│  │  │  │  ├─ Structural/
│  │  │  │  ├─ Hazards/
│  │  │  │  ├─ Building/
│  │  │  │  ├─ Power/
│  │  │  │  └─ Interaction/
│  │  │  ├─ App/                             # 담당자 B: 상태, UX와 기반 기능
│  │  │  │  ├─ Core/
│  │  │  │  ├─ State/
│  │  │  │  ├─ Inventory/
│  │  │  │  ├─ Economy/
│  │  │  │  ├─ Progression/
│  │  │  │  ├─ Drone/
│  │  │  │  ├─ AI/
│  │  │  │  ├─ Save/
│  │  │  │  ├─ UI/
│  │  │  │  ├─ Audio/
│  │  │  │  └─ Build/
│  │  │  └─ Shared/                          # 합의 후 변경하는 공동 계약
│  │  │     ├─ Contracts/
│  │  │     ├─ Events/
│  │  │     ├─ DTO/
│  │  │     ├─ IDs/
│  │  │     └─ Common/
│  │  ├─ Settings/                           # URP, Input과 프로젝트용 설정 에셋
│  │  ├─ Tilemaps/                           # 담당자 A 소유 Tilemap 에셋
│  │  ├─ Tests/
│  │  │  ├─ EditMode/
│  │  │  │  ├─ Gameplay/
│  │  │  │  └─ App/
│  │  │  └─ PlayMode/
│  │  │     ├─ Gameplay/
│  │  │     └─ Integration/
│  │  └─ Editor/
│  │     ├─ Build/
│  │     ├─ DataValidation/
│  │     └─ SaveTools/
│  ├─ Plugins/                               # 플러그인
│  └─ ThirdParty/                            # 외부 에셋과 패키지
├─ Packages/
│  ├─ manifest.json
│  └─ packages-lock.json
├─ ProjectSettings/
├─ docs/                                     # 구현 중 유지할 개발 문서
│  ├─ INTEGRATION_GUIDE.md
│  ├─ DATA_MODEL.md
│  ├─ SAVE_FORMAT.md
│  ├─ RELEASE_CHECKLIST.md
│  └─ CHANGELOG.md
├─ init/                                     # 초기 기획, 규칙과 작업지시서
├─ .gitignore
├─ AGENTS.md
└─ README.md
```

## 3. `init/` 문서 구조

`init/` 폴더는 구현 전 기준 문서를 보관한다.

```text
init/
├─ PRD.md             # 게임 기획안, 시스템 설계와 MVP 범위
├─ rule.md            # Agent 공통 작업, Unity, 데이터, 세이브와 Git 규칙
├─ process.md         # 2인 개발 역할 분담과 병합 안정화 계획
├─ process-A.md       # 담당자 A: Core Gameplay & World Simulation 작업지시서
├─ process-B.md       # 담당자 B: Game State, UX & Infrastructure 작업지시서
├─ prompt-B.md        # 담당자 B 작업용 프롬프트 초안 또는 보조 문서
└─ structure.md       # 현재 문서
```

`init/`의 내용은 초기 기준이다. 구현이 시작되면 지속적으로 바뀌는 통합 방법, 데이터 모델, 세이브 형식과 배포 절차는 `docs/`로 옮기거나 복사해 관리한다.

## 4. 주요 Scene 구조

### 4.1 Gameplay Test Scene

- `Gameplay_Player_Test.unity`: 이동, 물리 충돌과 카메라 추적
- `Gameplay_Mining_Test.unity`: Tilemap 채굴, 전력 소비와 보상 이벤트
- `Gameplay_Structural_Test.unity`: 구조 안정도, 버팀목과 부분 붕괴
- `Gameplay_Hazard_Test.unity`: 가스 구역 활성화와 노출 판정
- `Gameplay_Building_Test.unity`: Grid Preview, 충돌 검사와 시설 배치
- `Gameplay_Power_Test.unity`: 전력망, 조명, 충전기와 전진기지 Runtime
- `Gameplay_Drone_Test.unity`: 드론 추적, 센서와 Context 생성
- `Gameplay_WorldSave_Test.unity`: 월드 스냅샷과 복원

Gameplay Test Scene은 담당자 A가 소유한다. 기능은 최종 데모 Scene에서 바로 개발하지 않고 독립 Scene에서 검증한 뒤 Runtime Prefab과 통합 지침으로 담당자 B에게 전달한다.

### 4.2 앱과 메뉴 Scene

- `Bootstrap.unity`: 전역 서비스, 데이터 카탈로그와 세이브 초기화
- `MainMenu.unity`: 새 게임, 이어하기, 설정, 종료와 버전 표시
- `SurfaceBase.unity`: 광물 판매, 제작, 업그레이드와 탐사 시작
- `Mine_Demo_Integration.unity`: 전체 Gameplay, State, UI와 Save를 연결한 최종 시연 Scene

위 Scene은 담당자 B가 소유하며 `Mine_Demo_Integration.unity`는 B만 수정한다. 담당자 A는 Scene 수정 대신 Prefab, 좌표, 필수 참조와 검증 절차를 제공한다.

### 4.3 통합 Scene

```text
Mine_Demo_Integration
├─ Main Camera
├─ Global Light 2D
├─ Grid
│  ├─ Background Tilemap
│  ├─ Foreground Tilemap
│  ├─ Hazard Tilemap
│  └─ Building Tilemap
├─ GameplayRoot
│  ├─ Player
│  ├─ DiggerBot_Runtime
│  ├─ WorldSystems
│  │  ├─ MiningSystem
│  │  ├─ StructuralIntegritySystem
│  │  ├─ HazardSystem
│  │  ├─ BuildingPlacementSystem
│  │  └─ PowerNetworkSystem
│  └─ RuntimeBuildings
├─ ApplicationRoot
│  ├─ GameStateBridge
│  ├─ InventoryService
│  ├─ EconomyService
│  ├─ DroneAnalysisService
│  └─ SaveBridge
├─ HUDCanvas
└─ EventSystem
```

통합 Scene은 A의 Runtime Prefab과 Gameplay 시스템을 Shared 계약을 통해 B의 State·UI·Save 시스템에 연결한다. Scene YAML을 직접 편집하기보다 Unity Editor 또는 검토 가능한 Editor Script로 배치한다.

## 5. Prefab 배치 기준

`Prefabs/`는 런타임 기능과 사용자 인터페이스의 소유권을 기준으로 나눈다.

- `Prefabs/Gameplay/Player/`: Player와 MiningCursor Runtime Prefab
- `Prefabs/Gameplay/Buildings/`: 버팀목, 조명, 충전기, 보관함, 정산 콘솔, 기지 코어와 케이블
- `Prefabs/Gameplay/Hazards/`: GasZone과 붕괴·위험 Runtime Prefab
- `Prefabs/Gameplay/World/`: DiggerBot Runtime과 월드 오브젝트
- `Prefabs/Gameplay/Effects/`: 채굴, 붕괴와 건설 효과
- `Prefabs/UI/`: HUD, Inventory, Building, Outpost, Drone, Menu와 Save Slot 패널

Gameplay Runtime Prefab은 담당자 A, UI Prefab은 담당자 B가 소유한다. 기능 컴포넌트와 시각 요소는 `VisualRoot` 또는 `ViewSocket`으로 분리하고, 다른 담당자의 Prefab 내부를 직접 수정하지 않는다.

## 6. 게임 시스템과 상태 계층

게임플레이 결과는 UI나 세이브 파일을 직접 수정하지 않고 Shared 계약과 App Service를 거친다.

```text
입력과 Unity 물리
→ Scripts/Gameplay 시스템
→ Scripts/Shared 인터페이스·이벤트·DTO
→ Scripts/App Service
→ PlayerState / RunState / WorldState
→ HUD·메뉴 또는 SaveService
```

예를 들어 채굴 완료 시 `MiningSystem`은 `IMiningRewardReceiver`에 광물 ID와 수량을 전달한다. `InventoryService`가 `InventoryState`를 변경하고 이벤트를 발행한 뒤 HUD가 변경된 상태만 표시한다.

## 7. 정적 데이터와 로컬 세이브 구조

`Assets/_Project/Data/`는 코드 수정 없이 조정할 고정 게임 데이터의 원천으로 관리한다.

- `Minerals/`: 가격, 무게, 채굴 시간, 드릴 요구 등급과 구조 영향
- `Buildings/`: Runtime Prefab, Grid 크기, 전력 소비와 건설 비용
- `Recipes/`: 시설과 업그레이드 제작 비용
- `Upgrades/`: 드릴, 전력, 화물과 드론 효과
- `Hazards/`: 구조와 가스 위험 정의
- `Dialogue/`: 규칙 기반 드론 대사 템플릿

ScriptableObject는 정의 데이터만 저장한다. 현재 광물 수량, 골드, 진행도와 월드 변경점은 State와 Save DTO에 저장한다. 로컬 세이브는 `Application.persistentDataPath` 아래에서 임시 파일 기록, JSON 검증, 백업 교체 순서로 처리한다.

## 8. 공통 계약과 영구 ID

공통 계약은 `Scripts/Shared/Contracts/`, `Events/`, `DTO/`, `IDs/`에서 일관되게 관리한다.

핵심 인터페이스:

- `IMiningRewardReceiver`: 채굴 보상을 App 인벤토리에 전달
- `IResourceWallet`: 건설 비용 확인과 성공 후 자원 차감
- `IGameplayEventSink`: 구조, 가스, 건설과 탐사 이벤트 전달
- `IWorldSnapshotProvider`: 월드 변경점 캡처와 복원
- `IDroneContextProvider`: 실제 월드 상태로 드론 분석 Context 생성

영구 ID 예시:

- `mineral.copper`, `mineral.iron`, `mineral.lithium`
- `tile.rock.normal`, `tile.rock.fractured`, `tile.gas.basic`
- `building.support.basic`, `building.light.basic`, `building.charger.basic`
- `building.storage.basic`, `building.settlement.basic`, `building.outpost_core.basic`
- `upgrade.drill.speed`, `upgrade.drill.efficiency`, `upgrade.drone.scan`, `upgrade.drone.rescue`

Shared 계약과 배포된 ID를 임의로 변경하지 않는다. 변경이 필요하면 별도 합의와 PR을 거치고 Producer, Consumer, ScriptableObject, Save Migration과 테스트를 함께 수정한다.

## 9. 개발자별 책임 영역

### 담당자 A 주 담당

- `Assets/_Project/Scripts/Gameplay/`
- `Assets/_Project/Prefabs/Gameplay/`
- `Assets/_Project/Scenes/Test/Gameplay/`
- `Assets/_Project/Tilemaps/`
- `Assets/_Project/Tests/EditMode/Gameplay/`
- `Assets/_Project/Tests/PlayMode/Gameplay/`
- 플레이어 이동과 Tilemap 채굴
- 구조 안정도, 붕괴와 가스 위험
- 건설 배치, 전력망과 전진기지 Runtime
- 드론 이동·센서와 `DroneContextDto`
- 월드 스냅샷과 데모 월드 데이터

### 담당자 B 주 담당

- `Assets/_Project/Scripts/App/`
- `Assets/_Project/Data/`
- `Assets/_Project/Prefabs/UI/`
- `Assets/_Project/Scenes/Bootstrap/`
- `Assets/_Project/Scenes/MainMenu/`
- `Assets/_Project/Scenes/SurfaceBase/`
- `Assets/_Project/Scenes/Integration/`
- `Assets/_Project/Tests/EditMode/App/`
- `Assets/_Project/Tests/PlayMode/Integration/`
- `Assets/_Project/Editor/`
- 상태, 인벤토리, 경제, 제작과 업그레이드
- HUD, 메뉴, 드론 판단·대사와 로컬 세이브
- 통합 Scene, Build Profile, Windows 빌드와 배포

### 공동 관리

- `Assets/_Project/Scripts/Shared/`
- `ProjectSettings/`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `.gitignore`
- `AGENTS.md`
- `README.md`
- `docs/`
- 게임 밸런스와 최종 QA

공동 파일은 한 사람이 단독으로 크게 바꾸지 않는다. Shared 계약, Unity·패키지 버전, 영구 ID, Save Version, Build Profile과 통합 흐름은 반드시 합의 후 변경한다.

## 10. 주요 데이터 흐름

### 10.1 채굴과 인벤토리

1. 플레이어가 방향을 지정해 Foreground Tilemap의 타일을 채굴한다.
2. `MiningSystem`은 MiningTileData, 드릴 등급과 전력 조건을 확인한다.
3. 완료 시 타일을 한 번만 제거하고 `IMiningRewardReceiver`에 광물 ID와 수량을 전달한다.
4. `InventoryService`가 수량, 화물 중량과 미정산 가치를 다시 계산한다.
5. `InventoryChanged` 이벤트를 받은 HUD가 표시를 갱신한다.

### 10.2 건설과 자원 차감

1. 사용자가 Building Menu에서 시설을 선택한다.
2. `BuildingPlacementSystem`이 Grid Preview, 빈 공간, 기반 타일과 건물 겹침을 검사한다.
3. `IResourceWallet.CanAfford`로 비용 지불 가능 여부를 확인한다.
4. Runtime Prefab 생성에 성공한 뒤에만 `TrySpend`로 자원을 차감한다.
5. `BuildingPlaced` 이벤트를 발행하고 구조 안정도 또는 전력망을 다시 계산한다.

### 10.3 저장과 복원

1. `SaveService`가 PlayerState, ProgressState와 RunState를 수집한다.
2. A의 `CaptureWorldSnapshot`으로 채굴·붕괴·건설·가스 변경점을 받는다.
3. `GameSaveData`를 임시 JSON에 기록하고 검증한 뒤 기존 파일을 백업한다.
4. 로드 시 State를 복원하고 Integration Scene을 연다.
5. `RestoreWorldSnapshot` 적용 후 구조 안정도와 전력망처럼 계산 가능한 상태를 다시 계산한다.

### 10.4 드론 분석

1. A의 `IDroneContextProvider`가 깊이, 전력, 구조, 가스, 화물과 기지 거리의 실제 값을 수집한다.
2. B의 `DroneAnalysisService`가 결정론적 점수와 안전 우선순위로 추천 행동을 정한다.
3. 판단 근거 UI와 `TemplateDialogueGenerator`가 같은 수치에 기반한 설명을 표시한다.
4. 선택적 클라우드 AI는 확정된 추천과 근거를 자연어로만 표현한다.
5. API 실패 또는 오프라인에서는 즉시 템플릿 대사로 폴백한다.

## 11. 생성되지만 구조 설명에서 제외할 폴더

아래 폴더는 Unity와 개발 도구가 자동 생성하므로 핵심 구조 문서에서 중심으로 다루지 않는다.

```text
Library/         # Unity 임포트 캐시와 로컬 프로젝트 데이터
Temp/            # Unity 임시 파일
Logs/            # Unity 로컬 로그
Obj/             # C# 중간 빌드 결과
Build/           # 로컬 빌드 결과
Builds/          # 공유 전 검증할 빌드 결과
UserSettings/    # 개발자별 Unity Editor 설정
.vs/             # Visual Studio 로컬 설정
.idea/           # JetBrains Rider 로컬 설정
```

자동 생성 폴더와 로컬 빌드 결과는 Git 추적 대상에서 제외하는 것을 기본으로 한다. 단, Build Profile 같은 재현 가능한 설정 에셋은 프로젝트 구조 안에서 추적한다.

## 12. 구조 변경 원칙

- 기존 Unity 프로젝트 구조가 생기면 그 구조를 우선한다.
- 새 폴더는 책임과 소유자가 명확할 때만 만든다.
- 월드와 물리적 상호작용은 `Scripts/Gameplay/`에 둔다.
- 상태, UI, 경제, 저장과 빌드 로직은 `Scripts/App/`에 둔다.
- 두 영역 사이의 연결은 `Scripts/Shared/`의 인터페이스, 이벤트와 DTO만 사용한다.
- 정적 정의 데이터, 런타임 State와 Save DTO를 서로 섞지 않는다.
- 최종 Integration Scene과 다른 담당자의 Prefab을 소유자 합의 없이 수정하지 않는다.
- Unity 에셋은 `.meta`와 함께 관리하고 Scene·Prefab YAML 직접 편집을 피한다.
- Shared 계약, 영구 ID와 Save DTO 변경은 호환성 테스트를 함께 진행한다.
