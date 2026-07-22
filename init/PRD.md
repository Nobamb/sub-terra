Project Sub-Terra
MVP 통합 개발 마스터 플랜

1. 프로젝트 개요
   1.1 프로젝트명
   Project Sub-Terra
   1.2 장르
   2D 횡스크롤 기반 지하 탐사·채굴·위험 관리·기지 건설 게임
   1.3 핵심 콘셉트
   플레이어는 AI 탐사 드론 Digger-Bot과 함께 지하로 내려가 광물을 채굴한다.
   채굴은 새로운 길과 수익을 만들어내지만, 동시에 암반의 구조 안정도를 떨어뜨려 낙석과 붕괴 위험을 발생시킨다. 플레이어는 확보한 광물을 판매하거나 버팀목, 조명, 충전기와 같은 시설 제작에 사용하여 위험한 지하 공간을 안전한 전진기지로 바꿔야 한다.
   Digger-Bot은 현재 깊이, 전력, 구조 안정도, 가스 농도, 화물 가치, 귀환 거리 등을 분석하고 플레이어에게 적절한 행동을 추천한다.
   1.4 핵심 문장
   땅을 파며 위험을 만들어내고, AI 드론의 분석과 기지 건설을 이용해 그 위험한 지하를 안전한 탐사 영역으로 개척하는 게임.

2. MVP 개발 목표
   MVP의 목표는 많은 콘텐츠를 만드는 것이 아니라 다음 핵심 경험이 실제로 재미있는지를 검증하는 것이다.
   채굴
   → 광물 획득
   → 위험 발생
   → AI 드론 분석
   → 플레이어 선택
   → 버팀목·조명·기지 건설
   → 안전 구역 확보
   → 광물 정산
   → 장비 업그레이드
   → 더 깊은 탐사

최초 MVP에서 반드시 증명해야 하는 질문은 다음과 같다.
타일을 파고 광물을 얻는 과정이 재미있는가?
더 탐사할지 귀환할지 실제 고민이 발생하는가?
구조 안정도와 가스 위험이 플레이를 방해하기만 하지 않고 전략을 만드는가?
전진기지를 설치했을 때 뚜렷한 성취감과 편의가 생기는가?
드론의 분석이 장식이 아니라 실제 의사결정에 도움이 되는가?

3. MVP 범위
   3.1 MVP에 포함할 지역
   천층 폐광 지대
   깊이 0~200m
   구리와 철 중심
   비교적 안정적인 암반
   기본 채굴과 건설 학습
   균열과 버팀목 시스템 소개
   중층 가스 지대
   깊이 200~400m
   철과 리튬 중심
   가스 주머니 등장
   높은 수익과 높은 위험
   전진기지와 충전 시설 필요성 강조
   후속 지역 예고
   심층 티타늄 지대
   극심층 발광 다이아 지대
   미확인 인공 구조물
   정체불명의 신호
   티타늄과 발광 다이아는 MVP에서 직접 채굴하지 않더라도, 잠긴 구역과 드론 탐지 신호를 통해 다음 콘텐츠로 예고한다.

3.2 MVP에 반드시 포함할 기능
플레이어
좌우 이동
점프 또는 사다리·엘리베이터 이동
방향 지정 채굴
전력 소비
인벤토리
피해 및 구조 실패
지상 및 전진기지 귀환
지형
Tilemap 기반 암석과 광물
채굴 후 타일 제거
깊이에 따른 광물 배치
파괴 불가능한 기반 암석
가스 타일
구조 균열 타일
경제
구리, 철, 리튬
광물 판매
시설 제작
드릴과 드론 업그레이드
미정산 화물 손실
위험
구조 안정도
균열 단계
부분 붕괴
가스 위험
전력 부족
화물 중량
건설
버팀목
조명
충전기
광물 보관함
정산 콘솔
전진기지 코어
드론
플레이어 추적
위험 감지
주변 광물 분석
귀환 추천
판단 근거 UI
규칙 기반 대사
선택적 생성형 AI 대사
저장
로컬 세이브
자동 저장
백업 세이브
세이브 버전 관리

3.3 MVP에서 제외할 기능
멀티플레이
서버 DB
회원가입과 로그인
대규모 몬스터 전투
자유 대화형 AI
로컬 대형 언어 모델
모바일 동시 출시
완전한 절차적 무한 월드
복잡한 유체 기반 가스 확산
전체 지형 물리 붕괴
완성형 스토리 캠페인
유저 간 거래
실시간 랭킹
유료 상품

4. 개발 기술 스택
   4.1 게임 엔진
   권장 버전
   Unity 6.3 LTS
   2026년 7월 기준 Unity 6.5가 최신 기능 릴리스이지만, Unity 6.3은 LTS 계열이므로 처음 개발하는 협업 프로젝트에서는 최신 기능보다 안정성과 장기 지원을 우선하여 Unity 6.3 LTS를 사용하는 것을 권장한다.
   프로젝트가 이미 다른 Unity 6 버전으로 생성됐다면 특별한 문제가 없는 한 개발 도중 엔진 버전을 자주 변경하지 않는다.
   4.2 렌더링
   Universal Render Pipeline
   2D Renderer
   Light 2D
   Sprite Renderer
   Tilemap Renderer
   Particle System
   URP 2D Renderer는 Light 2D를 통해 스프라이트에 2D 조명을 적용할 수 있으므로, 어두운 지하와 플레이어·드론·설치 조명 표현에 적합하다.
   4.3 언어와 런타임
   C#
   Unity MonoBehaviour
   ScriptableObject
   [Serializable] Save DTO
   비동기 작업은 필요한 범위에서 Task
   일반적인 이벤트는 C# event 또는 UnityEvent
   4.4 입력
   Unity Input System
   키보드·마우스 우선
   게임패드 확장 가능 구조
   Unity 공식 문서에서도 새 프로젝트에는 기존 Input Manager보다 Input System 패키지를 권장한다.
   4.5 레벨 제작
   Grid
   Foreground Tilemap
   Background Tilemap
   Hazard Tilemap
   Building Preview Tilemap 또는 일반 Prefab 배치
   Tilemap은 타일 에셋을 저장·관리하고 Tilemap Renderer와 Tilemap Collider 2D에 연결할 수 있어 2D 채굴형 지형 프로토타입에 적합하다.
   4.6 UI
   MVP에서는 다음 중 하나로 통일한다.
   권장안
   게임 HUD: Unity UI Canvas
   개발용 디버그 패널: Unity UI 또는 간단한 IMGUI
   TextMeshPro
   UI Toolkit과 Canvas를 혼합하면 초기 학습 부담이 늘어날 수 있으므로, MVP 플레이 화면은 Canvas 기반으로 통일한다.
   4.7 데이터와 저장
   고정 게임 데이터: ScriptableObject
   런타임 상태: C# 객체
   영구 저장: 로컬 JSON
   설정값: PlayerPrefs
   서버 DB: MVP 이후 검토
   ScriptableObject는 여러 Scene에서 공유할 에셋 참조와 데이터 카탈로그를 저장하는 직렬화 에셋으로 사용할 수 있다.
   Unity의 JsonUtility는 [Serializable] 클래스와 구조체를 JSON으로 직렬화할 수 있고, Application.persistentDataPath는 실행 간 유지할 데이터를 저장하는 플랫폼별 영구 경로를 제공한다.
   4.8 버전 관리
   Git
   GitHub
   Unity용 .gitignore
   텍스트 기반 Scene·Prefab 직렬화
   Visible Meta Files
   대용량 이미지·음원 추가 시 Git LFS
   4.9 AI 개발 도구
   Codex
   Cursor 또는 VS Code
   Unity Editor
   GitHub Issues 또는 간단한 프로젝트 보드
   Codex는 저장소를 읽고 기능 구현, 버그 수정, 리팩토링, 코드 리뷰, 테스트 작성과 같은 개발 작업을 수행할 수 있으며 IDE, CLI, 앱 환경에서 사용할 수 있다.

5. 전체 시스템 아키텍처
   ┌─────────────────────────────────────┐
   │ Presentation │
   │ HUD / 메뉴 / 드론 대사 / 건설 UI │
   └──────────────────┬──────────────────┘
   │
   ┌──────────────────▼──────────────────┐
   │ Game Systems │
   │ Mining / Building / Power / Hazard │
   │ Inventory / Economy / Drone / Save │
   └──────────────────┬──────────────────┘
   │
   ┌──────────────────▼──────────────────┐
   │ Game State │
   │ PlayerState / WorldState / RunState │
   └──────────────────┬──────────────────┘
   │
   ┌──────────────────▼──────────────────┐
   │ Static Data │
   │ ScriptableObject Catalogs │
   └──────────────────┬──────────────────┘
   │
   ┌──────────────────▼──────────────────┐
   │ Persistence │
   │ Local JSON Save / Backup / Settings │
   └─────────────────────────────────────┘

핵심 원칙은 UI, 게임 로직, 데이터, 저장을 분리하는 것이다.
예를 들어 인벤토리 UI가 직접 광물 수량을 수정하지 않는다.
플레이어가 광물 채굴
→ MiningSystem이 채굴 완료
→ InventorySystem에 광물 추가 요청
→ InventoryState 변경
→ InventoryChanged 이벤트 발생
→ HUD가 변경된 상태 표시

6. Unity 프로젝트 폴더 구조
   Assets/
   ├─ \_Project/
   │ ├─ Art/
   │ │ ├─ Placeholder/
   │ │ ├─ Sprites/
   │ │ ├─ Tiles/
   │ │ ├─ UI/
   │ │ └─ Materials/
   │ │
   │ ├─ Audio/
   │ │ ├─ BGM/
   │ │ ├─ SFX/
   │ │ └─ Mixer/
   │ │
   │ ├─ Data/
   │ │ ├─ Minerals/
   │ │ ├─ Buildings/
   │ │ ├─ Upgrades/
   │ │ ├─ Contracts/
   │ │ ├─ Hazards/
   │ │ └─ Dialogue/
   │ │
   │ ├─ Prefabs/
   │ │ ├─ Player/
   │ │ ├─ Drone/
   │ │ ├─ Buildings/
   │ │ ├─ Hazards/
   │ │ ├─ UI/
   │ │ └─ Effects/
   │ │
   │ ├─ Scenes/
   │ │ ├─ Bootstrap/
   │ │ ├─ MainMenu/
   │ │ ├─ SurfaceBase/
   │ │ ├─ Mine/
   │ │ └─ Test/
   │ │
   │ ├─ Scripts/
   │ │ ├─ Core/
   │ │ ├─ Player/
   │ │ ├─ Mining/
   │ │ ├─ Inventory/
   │ │ ├─ Economy/
   │ │ ├─ Building/
   │ │ ├─ Power/
   │ │ ├─ Hazards/
   │ │ ├─ Structural/
   │ │ ├─ Drone/
   │ │ ├─ AI/
   │ │ ├─ Save/
   │ │ ├─ UI/
   │ │ └─ Utilities/
   │ │
   │ ├─ Settings/
   │ ├─ Tilemaps/
   │ ├─ Tests/
   │ │ ├─ EditMode/
   │ │ └─ PlayMode/
   │ └─ Editor/
   │
   ├─ Plugins/
   └─ ThirdParty/

프로젝트 전용 파일은 \_Project 아래에 모아서 외부 패키지와 분리한다.

7. Scene 구성
   7.1 Bootstrap Scene
   게임 시작 시 가장 먼저 실행되는 Scene이다.
   담당 기능:
   전역 서비스 초기화
   세이브 파일 확인
   설정 불러오기
   데이터 카탈로그 검증
   Main Menu 이동
   포함 오브젝트:
   Bootstrap
   ├─ GameBootstrapper
   ├─ SaveService
   ├─ SceneLoader
   ├─ AudioService
   └─ PersistentManagers

7.2 Main Menu Scene
새 게임
이어하기
설정
종료
개발 빌드 버전 표시
7.3 Surface Base Scene
광물 판매
제작
업그레이드
탐사 시작
계약 확인
심층 잠금 상태 표시
7.4 Mine Scene
MineScene
├─ Grid
│ ├─ BackgroundTilemap
│ ├─ ForegroundTilemap
│ ├─ HazardTilemap
│ └─ BuildingTilemap
│
├─ Player
├─ DiggerBot
├─ MineSystems
│ ├─ MiningSystem
│ ├─ StructuralIntegritySystem
│ ├─ HazardSystem
│ ├─ BuildingSystem
│ ├─ PowerNetworkSystem
│ └─ RunManager
│
├─ MainCamera
├─ GlobalLight2D
├─ HUDCanvas
└─ EventSystem

7.5 Test Scene
기능을 빠르게 검증하기 위한 전용 Scene이다.
이동 테스트
채굴 테스트
붕괴 테스트
가스 테스트
건설 테스트
저장·로드 테스트
모든 기능을 실제 Mine Scene에서만 테스트하면 반복 속도가 느려지므로 독립 테스트 공간을 둔다.

8. 에셋 및 그레이박스 전략
   8.1 MVP 초기 원칙
   정식 이미지, 애니메이션, 사운드를 기다리지 않는다.
   초기에는 다음 요소만 사용한다.
   Unity 기본 Primitive Sprite
   단색 정사각형 타일
   단순 PNG
   텍스트 라벨
   기본 Particle
   Transform 이동·회전·크기 변화
   Light 2D
   8.2 임시 표현 규칙
   게임 요소
   임시 표현
   플레이어
   파란색 캡슐
   드론
   작은 청록색 원
   일반 암석
   회색 사각형
   구리
   갈색 원 무늬 타일
   철
   짙은 회색 십자 무늬
   리튬
   밝은 청록 결정 무늬
   가스
   반투명 연두색 원
   균열
   주황·빨강 선
   버팀목
   세로 직사각형
   조명
   노란 원과 Light 2D
   충전기
   파란 직사각형
   정산기
   초록 직사각형
   기지 코어
   큰 육각형 또는 원

색상만으로 구분하지 않고 모양이나 무늬도 다르게 한다.
8.3 SVG 사용 범위
SVG는 다음에 제한적으로 사용한다.
HUD 아이콘
광물 아이콘
위험 표시
버튼 아이콘
로고
드론 상태 표시
월드 타일, 캐릭터 애니메이션, 파괴 이펙트는 단순 PNG 또는 Sprite 기반으로 시작한다.
8.4 VisualRoot 패턴
기능 Prefab과 시각 요소를 분리한다.
Player.prefab
├─ PlayerController
├─ Rigidbody2D
├─ Collider2D
├─ PlayerMiningController
└─ VisualRoot
└─ PlaceholderSprite

정식 아트가 완성되면 VisualRoot 내부만 교체한다.
DiggerBot.prefab
├─ DroneFollower
├─ DroneAnalyzer
├─ Light2D
└─ VisualRoot
└─ PlaceholderCircle

SupportBeam.prefab
├─ BuildingInstance
├─ StructuralSupport
├─ Collider2D
└─ VisualRoot
└─ PlaceholderRectangle

게임 로직이 Sprite 이름이나 이미지 파일에 의존해서는 안 된다.
8.5 정식 에셋 교체 순서
그레이박스 완성
핵심 루프 플레이 테스트
캐릭터와 타일 비율 확정
아트 가이드 작성
정식 타일셋 제작
플레이어와 드론 Sprite 제작
시설 Sprite 제작
UI 디자인 적용
애니메이션 추가
파티클과 사운드 추가
최종 조명 조정

9. 정적 게임 데이터 구조
   9.1 데이터 원칙
   광물 가격이나 시설 비용을 코드 안에 하드코딩하지 않는다.
   다음 데이터는 ScriptableObject로 관리한다.
   광물
   채굴 타일
   건물
   업그레이드
   제작 레시피
   위험 요소
   계약
   드론 대사 템플릿
   9.2 영구 ID 규칙
   표시 이름이나 에셋 이름이 아닌 내부 ID를 사용한다.
   mineral.copper
   mineral.iron
   mineral.lithium

tile.rock.normal
tile.rock.fractured
tile.gas.basic

building.support.basic
building.light.basic
building.charger.basic
building.storage.basic
building.settlement.basic
building.outpost_core.basic

upgrade.drill.speed
upgrade.drill.efficiency
upgrade.drone.scan
upgrade.drone.rescue

한 번 배포한 ID는 되도록 변경하지 않는다.
9.3 MineralData
[CreateAssetMenu(menuName = "SubTerra/Data/Mineral")]
public sealed class MineralData : ScriptableObject
{
public string id;
public string displayName;

    public Sprite icon;
    public Sprite worldSprite;
    public Color placeholderColor;

    public int sellPrice;
    public float weight;
    public float miningDuration;
    public int requiredDrillLevel;
    public float structuralImpact;

}

9.4 MiningTileData
[CreateAssetMenu(menuName = "SubTerra/Data/Mining Tile")]
public sealed class MiningTileData : ScriptableObject
{
public string id;
public TileBase tileAsset;

    public bool isMineable;
    public float durability;
    public MineralData rewardMineral;
    public int rewardAmount;

    public float structuralImpact;
    public bool containsGas;

}

9.5 BuildingData
[CreateAssetMenu(menuName = "SubTerra/Data/Building")]
public sealed class BuildingData : ScriptableObject
{
public string id;
public string displayName;

    public GameObject prefab;
    public Sprite icon;

    public Vector2Int gridSize;
    public bool requiresPower;
    public int powerConsumption;

    public List<ItemCost> buildCosts;

}

9.6 데이터 카탈로그
[CreateAssetMenu(menuName = "SubTerra/Data/Game Catalog")]
public sealed class GameDataCatalog : ScriptableObject
{
public List<MineralData> minerals;
public List<MiningTileData> miningTiles;
public List<BuildingData> buildings;
public List<UpgradeData> upgrades;
}

게임 시작 시 모든 ID 중복과 누락 참조를 검사한다.

10. 런타임 상태 구조
    정적 데이터와 현재 플레이 상태를 분리한다.
    정적 데이터
    MineralData.sellPrice = 20

런타임 상태
현재 구리 보유량 = 14

10.1 PlayerState
public sealed class PlayerState
{
public int CurrentEnergy { get; private set; }
public int MaxEnergy { get; private set; }

    public long Credits { get; private set; }

    public InventoryState Inventory { get; }
    public UpgradeState Upgrades { get; }

}

10.2 RunState
한 번의 탐사에만 적용되는 데이터다.
public sealed class RunState
{
public int CurrentDepth;
public int MaximumDepth;

    public long UnsettledCargoValue;
    public float CargoWeight;

    public bool IsGasExposed;
    public bool IsReturning;
    public RunResult Result;

}

10.3 WorldState
public sealed class WorldState
{
public long WorldSeed;

    public HashSet<GridPosition> MinedTiles;
    public Dictionary<GridPosition, ChangedTileState> ChangedTiles;
    public Dictionary<string, BuildingRuntimeState> Buildings;
    public Dictionary<string, GasRuntimeState> GasStates;

}

11. 로컬 세이브 구조
    11.1 MVP 저장 원칙
    서버 DB는 사용하지 않는다.
    ScriptableObject
    → 게임의 고정 정의 데이터

C# Runtime State
→ 현재 실행 중 상태

Local JSON
→ 게임 종료 후에도 유지할 진행 데이터

11.2 GameSaveData
[Serializable]
public sealed class GameSaveData
{
public int saveVersion;
public string gameVersion;

    public string saveId;
    public long createdAtUnix;
    public long updatedAtUnix;
    public long playTimeSeconds;

    public PlayerSaveData player;
    public ProgressSaveData progress;
    public WorldSaveData world;
    public DroneSaveData drone;

}

11.3 PlayerSaveData
[Serializable]
public sealed class PlayerSaveData
{
public float positionX;
public float positionY;

    public int currentEnergy;
    public int maxEnergy;
    public long credits;

    public List<InventoryItemSaveData> inventory;
    public List<UpgradeSaveData> upgrades;

}

11.4 WorldSaveData
월드의 모든 타일을 저장하지 않는다.
기본 Tilemap 또는 월드 Seed

- # 플레이어가 변경한 내용
  현재 월드

저장 대상:
제거한 타일
다른 타일로 변경된 좌표
설치 시설
활성화된 가스
발견한 구역
영구 손상된 구조
[Serializable]
public sealed class WorldSaveData
{
public long worldSeed;

    public List<TilePositionSaveData> minedTiles;
    public List<ChangedTileSaveData> changedTiles;
    public List<BuildingSaveData> buildings;
    public List<GasStateSaveData> gasStates;
    public List<string> discoveredChunkIds;

}

11.5 BuildingSaveData
[Serializable]
public sealed class BuildingSaveData
{
public string instanceId;
public string buildingId;

    public int gridX;
    public int gridY;
    public int rotation;

    public int level;
    public int durability;

}

isPowered는 가능하면 저장하지 않고 시설과 케이블을 복원한 뒤 전력망을 다시 계산한다.
11.6 저장 순서
게임 상태 수집
→ GameSaveData DTO 생성
→ JSON 변환
→ 임시 파일 기록
→ 기록 검증
→ 기존 세이브를 백업으로 이동
→ 임시 파일을 정식 세이브로 교체

파일 구성:
save_slot_1.json
save_slot_1.backup.json
save_slot_1.tmp

11.7 자동 저장 시점
지상 귀환
광물 정산
업그레이드 구매
전진기지 코어 설치
새로운 심도 구역 진입
구조 실패
게임 종료 요청
변경 사항이 존재할 때 일정 주기
매 프레임 또는 타일 하나를 캘 때마다 파일을 쓰지 않는다.

12. 게임 핵심 로직
    12.1 채굴 시스템
    처리 순서
    입력 방향 확인
    → 플레이어 앞 Grid 좌표 계산
    → 해당 타일 데이터 조회
    → 채굴 가능 여부 검사
    → 드릴 레벨 검사
    → 전력 검사
    → 채굴 진행
    → 타일 내구도 감소
    → 완료 시 타일 제거
    → 광물 인벤토리 추가
    → 구조 안정도 재계산 요청
    → 가스 또는 특수 이벤트 처리

채굴 취소
플레이어가 이동하거나 버튼을 놓으면 채굴이 취소된다.
MVP에서는 타일 내구도 진행률을 유지하지 않고 초기화해도 된다.

12.2 인벤토리와 화물
광물 획득
→ 수량 증가
→ 총 중량 계산
→ 미정산 가치 계산
→ HUD 갱신

화물 중량에 따라 이동 속도를 조정한다.
0~50% 적재: 정상
50~80% 적재: 약한 감속
80~100% 적재: 강한 감속
최대 초과: 추가 획득 불가

12.3 경제 시스템
광물은 판매와 제작 두 가지 용도를 가진다.
구리
├─ 판매
└─ 케이블·조명 제작

철
├─ 판매
└─ 버팀목·벽체 제작

리튬
├─ 고가 판매
└─ 배터리·드론 업그레이드

플레이어가 모든 광물을 판매하지 않도록 제작 수요를 제공한다.

12.4 전력 시스템
MVP에서는 다음 자원을 공용 전력으로 통합한다.
드릴 에너지
플레이어 조명
드론 스캔
엘리베이터 호출
가스 정화
비상 보호 기능
전력 소모는 시스템별 요청으로 처리한다.
public interface IEnergyConsumer
{
int RequestedEnergy { get; }
bool TryConsumeEnergy();
}

실제 구현에서는 모든 시설이 프레임마다 전력을 요청하지 않고, 일정 간격 또는 상태 변경 시 계산한다.

12.5 구조 안정도
목표
채굴한 모양에 따라 위험이 달라지게 만든다.
MVP 계산 방식
완전한 구조 공학 시뮬레이션을 하지 않는다.
주변 타일을 기준으로 단순 점수를 계산한다.
기본 안정도

- 인접 암석 수
- 아래 지지 타일
- 버팀목 영향

* 빈 공간 크기
* 상부 제거
* 균열 타일
* 위험 광물 채굴

상태 단계
70~100: 안정
40~69: 주의
20~39: 위험
0~19: 붕괴 임박

상태별 표현
안정: 기본 상태
주의: 노란 강조와 미세한 돌가루
위험: 주황 균열과 화면 흔들림
붕괴 임박: 빨간 점멸과 경고음
붕괴: 지정 타일이 낙석 또는 암석으로 변경
전체 Tilemap을 매 프레임 계산하지 않는다.
다음 이벤트가 발생한 주변 범위만 다시 계산한다.
타일 제거
버팀목 설치
버팀목 파괴
붕괴 발생

12.6 가스 시스템
가스 발생
특정 가스 타일 채굴
위험 구역 진입
이벤트 트리거
MVP 표현
실제 유체 시뮬레이션 대신 원형 또는 타일 기반 위험 범위를 사용한다.
가스 효과
전력 지속 감소
시야 감소
이동 속도 감소
장시간 노출 시 피해
대응
위험 범위 이탈
드론 사전 스캔
환기기 설치
보호 업그레이드
우회 경로 사용

12.7 건설 시스템
건설 처리
건설 모드 진입
→ 시설 선택
→ 마우스 위치를 Grid 좌표로 변환
→ 배치 Preview 표시
→ 설치 가능 여부 검사
→ 비용 검사
→ 설치 확정
→ 자원 차감
→ Prefab 생성
→ 관련 시스템 재계산

설치 가능 조건
빈 공간
다른 건물과 겹치지 않음
필요한 바닥 또는 벽 존재
허용된 기지 영역
충분한 자원
필요한 경우 전력 연결 가능

12.8 전력망 시스템
MVP에서는 복잡한 회로를 만들지 않는다.
권장 방식
기지 코어에서 연결된 케이블 또는 일정 반경 내 시설을 탐색한다.
전진기지 코어
→ 연결된 케이블 탐색
→ 연결 시설 목록 생성
→ 총 공급량 계산
→ 총 소비량 계산
→ 시설 활성화 결정

전력망은 다음 상황에서만 재계산한다.
코어 설치·제거
케이블 설치·제거
시설 설치·제거
발전량 변경

12.9 구조 실패
플레이어가 전력을 모두 소모하거나 붕괴·가스로 행동 불능 상태가 되면 구조 실패로 처리한다.
권장 패널티
미정산 광물 30~50% 손실
지상 기지로 복귀
설치된 전진기지는 유지
가장 비싼 광물 일부는 드론 구조 업그레이드로 보호 가능
완전한 세이브 삭제나 모든 자원 손실은 MVP에서 적용하지 않는다.

13. AI 드론 설계
    13.1 드론의 역할
    Digger-Bot은 게임을 대신 플레이하는 AI가 아니다.
    역할은 다음과 같다.
    위험 탐지
    광물 탐지
    귀환 가능성 분석
    행동 추천
    판단 근거 표시
    상황별 대사 출력
    13.2 입력 데이터
    현재 깊이
    현재 전력
    귀환 예상 전력
    기지 거리
    화물 가치
    화물 중량
    구조 안정도
    가스 위험
    인근 광물
    귀환 경로 상태
    최근 경고 무시 횟수

13.3 판단 방식
핵심 판단은 C# 규칙 시스템이 처리한다.
귀환 점수
버팀목 설치 점수
가스 지역 이탈 점수
광물 채굴 점수
계속 하강 점수

예:
전력 부족 +40 귀환
높은 화물 가치 +20 귀환
구조 위험 +25 버팀목
인근 고가 광물 +20 채굴
가스 위험 +50 이탈

13.4 대사 생성 구조
public interface IDroneDialogueGenerator
{
Task<string> GenerateAsync(DroneContext context);
}

구현체:
TemplateDialogueGenerator
CloudDialogueGenerator
LocalModelDialogueGenerator

MVP 기본값
TemplateDialogueGenerator
인터넷 없이 동작
응답 지연 없음
대사 통제 가능
시연 안정성 높음
선택적 해커톤 기능
CloudDialogueGenerator
규칙 엔진이 계산한 결과를 자연스러운 문장으로 변환한다.
Unity 내부 판단
→ 추천 행동과 근거 생성
→ 백엔드에 JSON 전송
→ AI 모델이 문장 생성
→ 대사 출력

AI가 붕괴 여부나 피해를 결정해서는 안 된다.
13.5 폴백
클라우드 대사 요청
→ 지정 시간 내 응답 성공
→ 생성 대사 출력

→ 실패 또는 지연
→ 템플릿 대사 즉시 출력

API 키는 Unity 빌드에 직접 포함하지 않는다. 생성형 AI 연결이 필요해지면 별도 서버 또는 서버리스 함수를 둔다.

14. UI 구조
    14.1 기본 HUD
    현재 깊이
    현재 전력
    화물 중량
    미정산 가치
    구조 안정도
    가스 위험
    선택 시설
    상호작용 안내
    14.2 드론 분석 패널
    추천: 귀환

전력 낮음
화물 가치 높음
구조 안정도 위험
기지 거리 72m

대사만 표시하지 않고 판단 근거를 함께 보여준다.
14.3 건설 UI
시설 아이콘
필요 자원
보유 자원
전력 소비
설치 가능 여부
구조 안정도 변화 예상
14.4 개발용 디버그 UI
개발 빌드에서만 표시한다.
플레이어 좌표
현재 Tile 좌표
FPS
구조 계산 결과
가스 범위
전력망 연결 상태
드론 행동 점수
강제 저장·로드
자원 추가
강제 붕괴

15. 개발 절차
    단계 0. 프로젝트 기준 고정
    작업
    Unity 6.3 LTS 설치
    2D URP 프로젝트 생성
    Git 저장소 생성
    Unity .gitignore 적용
    Force Text 직렬화 설정
    Visible Meta Files 설정
    기본 폴더 생성
    README 작성
    AGENTS.md 작성
    빌드 대상 Windows x64 확정
    완료 조건
    두 개발자 PC에서 동일 프로젝트 열림
    Console 오류 없음
    빈 Windows 빌드 실행 가능
    Git에서 불필요한 Library 폴더 제외

단계 1. 그레이박스 이동
작업
Input Actions 작성
플레이어 이동
Rigidbody2D
Collider2D
카메라 추적
기본 암석 Tilemap
테스트 Scene
완료 조건
플레이어가 타일 위를 안정적으로 이동
벽과 바닥 관통 없음
키보드 입력 동작
테스트 빌드 실행 가능

단계 2. 채굴 수직 슬라이스
작업
MiningTileData
광물 데이터
방향 채굴
채굴 진행 UI
타일 제거
광물 획득
전력 소비
완료 조건
일반 암석, 구리, 철을 구분
타일별 채굴 시간이 다름
전력이 없으면 채굴 불가
채굴 완료 시 인벤토리 증가

단계 3. 인벤토리와 경제
작업
InventoryState
중량 계산
미정산 가치
지상 정산
기본 제작 비용
골드 저장
완료 조건
광물을 판매할 수 있음
판매 후 골드 증가
판매하지 않은 광물로 시설 제작 가능
중량에 따라 이동 속도 변화

단계 4. 구조 안정도
작업
주변 지지 계산
균열 단계
버팀목 데이터
버팀목 설치
부분 붕괴
드론 경고
완료 조건
넓게 파면 위험 증가
버팀목 설치 시 안정도 회복
위험 상태가 시각적으로 구분
붕괴가 항상 동일한 규칙으로 재현 가능

단계 5. 가스와 전력 위험
작업
가스 타일
가스 범위
전력 지속 감소
시야 제한
드론 사전 경고
가스 이탈 처리
완료 조건
가스 위험을 확인 가능
가스 안에 머무르면 명확한 불이익 발생
위험 지역을 우회하거나 빠르게 통과할 이유가 생김

단계 6. 기지 건설
작업
건설 모드
Grid Preview
설치 유효성 검사
조명
충전기
보관함
기지 코어
완료 조건
빈 공간에 시설 설치 가능
자원 부족 시 설치 불가
조명 설치 시 실제 시야 확보
충전기에서 전력 회복
보관함에 광물 보관

단계 7. 전진기지와 전력망
작업
기지 코어
연결 판정
전력 공급
정산 콘솔
체크포인트
저장 트리거
완료 조건
전진기지가 탐사에 실제 이점 제공
전력 없는 시설이 비활성화
전진기지에서 일부 정비와 정산 가능
로드 후 시설과 연결 상태 복원

단계 8. 드론 완성
작업
플레이어 추적
분석 Context 생성
행동 점수
판단 근거 UI
템플릿 대사
대사 우선순위
대사 쿨다운
완료 조건
중요한 위험만 경고
같은 대사가 과도하게 반복되지 않음
귀환 추천 이유가 UI에 표시
드론이 잘못된 수치를 만들어내지 않음

단계 9. 로컬 세이브
작업
Save DTO
JSON 저장
백업
저장 버전
월드 변경점 복원
시설 복원
자동 저장
완료 조건
게임 종료 후 골드·업그레이드 유지
채굴한 타일 유지
설치 시설 유지
손상 세이브 시 백업 로드
이전 버전 저장 데이터 처리 가능

단계 10. 데모 콘텐츠
작업
천층과 가스 지대 연결
탐사 동선
리튬 목표
위험 경로와 안전 경로
전진기지 설치 구간
심층 신호 연출
튜토리얼 메시지
완료 조건
플레이어가 한 번의 플레이에서 다음을 모두 경험한다.
채굴
→ 자원 획득
→ 균열
→ 버팀목
→ 가스
→ 전진기지
→ 드론 귀환 추천
→ 정산
→ 업그레이드
→ 심층 예고

단계 11. 플레이 테스트와 밸런스
확인 항목
채굴 시간이 지루하지 않은가
전력이 너무 빠르거나 느리게 소모되지 않는가
구조 위험을 이해할 수 있는가
버팀목 비용이 적절한가
가스가 불합리하지 않은가
귀환 판단이 발생하는가
전진기지가 충분히 유용한가
드론 경고가 지나치게 많지 않은가
수치는 코드가 아닌 ScriptableObject에서 조정한다.

단계 12. 정식 아트 교체
핵심 루프가 검증된 뒤 진행한다.
아트 방향 확정
타일 크기 확정
캐릭터 Sprite
드론 Sprite
시설 Sprite
UI
애니메이션
파티클
사운드
조명 마감

16. Codex 작업 방식
    16.1 한 번에 전체 게임을 요청하지 않기
    나쁜 요청:
    이 기획대로 게임 전체를 만들어줘.

좋은 요청:
현재 Unity 6.3 LTS 2D URP 프로젝트다.

Foreground Tilemap에서 플레이어가 바라보는 방향의 타일을
일정 시간 채굴하고 제거하는 MiningSystem을 구현해라.

조건:

- MiningTileData를 사용한다.
- 전력이 부족하면 시작하지 않는다.
- 채굴 완료 시 InventorySystem에 보상을 전달한다.
- Tilemap 좌표가 범위를 벗어나도 예외가 발생하지 않아야 한다.
- EditMode 테스트를 작성한다.
- 기존 프로젝트 구조를 먼저 읽고 충돌이 없는 방식으로 구현한다.

  16.2 권장 작업 단위
  하나의 시스템
  하나의 버그
  하나의 리팩토링
  하나의 테스트 묶음
  하나의 Editor 도구
  16.3 Codex 결과 검토
  각 작업 후 확인한다.
  변경 파일 목록
  변경 이유
  Unity Console 오류
  테스트 결과
  Scene 참조 누락
  실제 플레이 동작
  기존 기능 회귀
  저장 데이터 호환성
  16.4 Editor 자동화
  Codex에게 Editor Script도 작성하게 한다.
  Tools > SubTerra > Setup Test Scene
  Tools > SubTerra > Validate Data IDs
  Tools > SubTerra > Create Placeholder Tiles
  Tools > SubTerra > Open Save Folder
  Tools > SubTerra > Clear Local Save

Scene과 Prefab YAML을 무리하게 직접 편집하는 대신 Editor API를 이용해 생성·연결한다.

17. 협업 및 Git 전략
    17.1 브랜치
    main
    develop
    feature/player-movement
    feature/mining
    feature/structural-integrity
    feature/building
    feature/save-system
    fix/issue-name

17.2 기본 흐름
Issue 생성
→ 기능 브랜치 생성
→ 구현
→ 로컬 테스트
→ Pull Request
→ 코드 및 Unity 동작 검토
→ develop 병합
→ 통합 테스트
→ 안정 버전을 main 병합

17.3 Scene 충돌 최소화
한 Scene을 두 사람이 동시에 수정하지 않음
각 기능은 Prefab과 독립 Test Scene에서 작업
공용 Scene 수정 담당자를 지정
Prefab Variant를 활용
Scene 배치가 필요한 변경은 먼저 공유
대규모 자동 포맷 변경 금지
17.4 커밋 규칙
feat: add tile mining system
fix: prevent duplicate mineral rewards
refactor: separate drone analysis from dialogue
test: add structural integrity edit mode tests
docs: update MVP development plan
chore: update Unity project settings

18. 테스트 전략
    Unity Test Framework는 Edit Mode와 Play Mode 테스트를 지원하고, Unity의 프레임과 애플리케이션 루프를 포함한 동작도 테스트할 수 있다.
    18.1 Edit Mode 테스트
    빠른 순수 로직 테스트에 사용한다.
    광물 가격 계산
    인벤토리 중량
    제작 비용
    구조 안정도 점수
    드론 행동 점수
    Save DTO 변환
    ID 중복 검사
    18.2 Play Mode 테스트
    Unity Scene과 컴포넌트가 필요한 기능을 테스트한다.
    플레이어 이동
    Tilemap 채굴
    건물 배치
    조명 활성화
    가스 범위
    전력 연결
    저장 후 월드 복원
    18.3 수동 체크리스트
    매 빌드 전 확인한다.
    [ ] 새 게임 시작
    [ ] 이동과 채굴
    [ ] 광물 획득
    [ ] 버팀목 설치
    [ ] 붕괴 발생
    [ ] 가스 노출
    [ ] 충전
    [ ] 광물 정산
    [ ] 저장 후 종료
    [ ] 이어하기
    [ ] 해상도 변경
    [ ] 개발 빌드 오류 로그 확인

19. 성능 관리
    MVP에서는 과도한 최적화를 하지 않되 다음 원칙은 처음부터 지킨다.
    전체 Tilemap을 매 프레임 탐색하지 않음
    구조 안정도는 변경 주변만 재계산
    드론 분석은 상태 변경 또는 일정 간격에만 실행
    생성형 AI를 매 프레임 호출하지 않음
    Particle과 임시 오브젝트는 필요 시 풀링
    저장은 변경 사항이 있을 때만 수행
    전력망은 시설 변경 시 재계산
    UI Text를 매 프레임 무조건 갱신하지 않음
    최적화는 실제 Profiler 결과를 확인한 뒤 진행한다.

20. 빌드 구성
    Unity Build Profiles는 플랫폼별 빌드 설정 묶음을 관리하는 기능이므로 개발, 테스트, 출시 설정을 분리하는 데 사용한다.
    20.1 Development Profile
    Development Build 활성화
    Script Debugging 필요 시 활성화
    디버그 UI 포함
    상세 로그
    테스트용 치트 기능
    저장 초기화 기능
    20.2 QA Profile
    실제 출시와 유사한 설정
    치트 UI 비활성화
    오류 로그 유지
    버전과 빌드 번호 표시
    세이브 마이그레이션 테스트
    20.3 Release Profile
    Development Build 비활성화
    디버그 메뉴 제거
    불필요한 로그 제거
    출시용 아이콘과 이름
    최종 해상도 및 품질 설정
    압축 및 코드 스트리핑 검증

21. 배포 전략
    21.1 최초 배포 대상
    Windows x64 Standalone
    이유:
    해커톤 시연이 쉬움
    사용자의 개발 환경과 일치
    키보드·마우스 조작 검증이 편함
    모바일 최적화 부담을 뒤로 미룰 수 있음
    로컬 세이브와 외부 AI 테스트가 용이함
    21.2 내부 테스트 배포
    SubTerra_MVP_0.1.0/
    ├─ SubTerra.exe
    ├─ SubTerra_Data/
    ├─ UnityPlayer.dll
    ├─ README.txt
    └─ CHANGELOG.txt

폴더 전체를 ZIP으로 압축해 전달한다.
21.3 외부 데모 배포
권장 후보:
itch.io 비공개 또는 제한 공개 페이지
GitHub Releases
직접 공유 링크
해커톤 제출 플랫폼
초기에는 설치 프로그램 없이 ZIP 배포로 충분하다.
21.4 버전 규칙
0.1.0 채굴 기본 버전
0.2.0 구조 안정도
0.3.0 건설 시스템
0.4.0 드론 분석
0.5.0 통합 MVP
0.5.1 버그 수정

빌드 화면에 다음을 표시한다.
Version 0.5.0
Build 20260722.1
Save Version 3

22. CI/CD 도입 순서
    22.1 MVP 초기
    수동 빌드로 진행한다.
    로컬 테스트
    → Unity Build Profile 실행
    → ZIP 생성
    → 직접 전달

22.2 통합 빈도가 증가한 뒤
Pull Request 테스트 자동 실행
Edit Mode 테스트
Play Mode 핵심 테스트
Windows 빌드 자동 생성
빌드 파일 Artifact 저장
태그 생성 시 Release 빌드
Unity Test Framework 테스트는 CI 파이프라인에서도 실행할 수 있다.
초기부터 자동 배포를 만들기보다, 수동 빌드 절차가 안정된 뒤 동일 절차를 자동화한다.

23. 향후 서버 DB 도입 기준
    다음 기능이 실제 범위에 들어올 때 Supabase 또는 Unity Cloud Save를 검토한다.
    로그인
    여러 PC 간 세이브 동기화
    게임 삭제 후 복원
    리더보드
    업적 검증
    일일·주간 계약
    플레이 통계
    원격 밸런스
    멀티플레이
    유저 간 거래
    사용자별 AI API 사용량
    현재 MVP에서는 로컬 JSON만 사용한다.
    다만 GameSaveData를 독립적으로 설계하여 나중에 같은 데이터를 서버의 JSON 컬럼에 업로드할 수 있도록 한다.

24. 해커톤 데모 흐름
25. 탐사 시작
    플레이어가 지상에서 장비를 준비하고 지하로 내려간다.
    드론:
    “좌측은 안정적인 구리 광맥, 우측은 구조 위험을 동반한 리튬 반응이 감지됩니다.”
26. 위험한 길 선택
    플레이어가 우측으로 이동해 철과 리튬을 채굴한다.
27. 구조 위험 발생
    넓은 공간을 파면서 균열이 발생한다.
    드론:
    “상부 암반 하중이 증가하고 있습니다. 추가 굴착 전 버팀목 설치를 권장합니다.”
28. 버팀목 설치
    플레이어는 철을 판매하지 않고 버팀목 제작에 사용한다.
    구조 안정도가 회복된다.
29. 가스 발생
    리튬 광맥을 채굴하면서 가스가 활성화된다.
    전력이 빠르게 감소한다.
30. 전진기지 설치
    플레이어가 조명, 충전기와 기지 코어를 설치한다.
    어두웠던 공간이 안전한 거점으로 바뀐다.
31. 귀환 판단
    드론 분석:
    추천: 귀환

남은 전력 낮음
화물 가치 높음
가스 위험 높음
기지 거리 72m

8. 정산과 업그레이드
   일부 광물은 판매하고, 일부 리튬은 드론 배터리 업그레이드에 사용한다.
9. 다음 탐사 예고
   심층에서 티타늄과 미확인 신호가 감지되며 데모가 종료된다.

10. 최종 개발 우선순위
    최우선
    이동
    채굴
    광물 획득
    전력
    인벤토리
    판매와 제작
    구조 안정도
    버팀목
    가스
    조명과 충전기
    전진기지
    드론 분석
    로컬 저장
    데모 동선
    그다음
    계약
    희귀 이벤트
    드론 성향
    생성형 AI 대사
    정식 아트
    사운드와 연출
    자동 빌드
    서버 DB

11. 최종 개발 원칙
    게임이 먼저다
    AI 연동, 서버 DB, 정식 아트보다 채굴과 위험 관리가 재미있어야 한다.
    판단은 결정론적으로 처리한다
    붕괴, 피해, 전력, 귀환 가능성은 Unity 로직이 계산한다.
    AI는 설명을 담당한다
    AI는 게임 결과를 결정하지 않고 판단을 자연스럽게 설명한다.
    아트는 교체 가능하게 만든다
    VisualRoot와 데이터 분리를 통해 정식 에셋이 들어와도 로직을 수정하지 않는다.
    저장은 상태만 기록한다
    GameObject나 Prefab 자체를 저장하지 않고 ID, 좌표, 수치만 저장한다.
    월드는 변경점만 저장한다
    모든 타일이 아니라 채굴·붕괴·건설로 바뀐 부분만 기록한다.
    기능은 수직으로 완성한다
    많은 미완성 시스템을 동시에 만드는 대신:
    이동
    → 채굴
    → 획득
    → 정산

처럼 플레이 가능한 작은 단위를 먼저 완성한 뒤 다음 시스템을 연결한다.
Codex는 실행자이고 개발자는 감독자다
Codex가 코드를 작성하더라도 Unity 연결 상태, 게임 감각, 데이터 참조, 저장 호환성과 실제 플레이는 개발자가 검증한다.

27. MVP 완료 정의
    Project Sub-Terra의 MVP는 다음 조건을 모두 만족할 때 완료된 것으로 본다.
    [ ] 새 게임을 시작할 수 있다.
    [ ] 플레이어가 이동하고 광물을 채굴할 수 있다.
    [ ] 광물에 따라 가격과 제작 용도가 다르다.
    [ ] 넓게 채굴하면 구조 위험이 증가한다.
    [ ] 버팀목으로 구조를 안정시킬 수 있다.
    [ ] 가스 지대가 탐사 판단에 영향을 준다.
    [ ] 전력 부족이 귀환 압박을 만든다.
    [ ] 전진기지를 건설할 수 있다.
    [ ] 조명과 충전 시설이 실제로 작동한다.
    [ ] 드론이 위험과 추천 이유를 표시한다.
    [ ] 구조 실패 시 일부 화물을 잃고 복귀한다.
    [ ] 지상 또는 전진기지에서 광물을 정산할 수 있다.
    [ ] 업그레이드 후 더 깊은 구역이 예고된다.
    [ ] 종료 후 다시 실행해도 진행 상황이 유지된다.
    [ ] Windows 빌드가 다른 PC에서 정상 실행된다.
    [ ] 치명적인 Console 오류 없이 데모를 완주할 수 있다.

이 기준을 충족한 뒤 정식 에셋, 생성형 AI, 서버 DB와 추가 콘텐츠를 확장한다.
