Project Sub-Terra
담당자 B 전용 개발 작업지시서

1. 담당자 B의 역할
   역할명
   Game State, UX & Infrastructure 담당
   핵심 책임
   담당자 B는 게임 전체의 상태, 사용자 경험, 저장, 통합과 배포를 책임진다.
   게임 시작
   → 상태 초기화
   → 데이터 로드
   → HUD 표시
   → 광물 인벤토리 관리
   → 판매·제작·업그레이드
   → 드론 판단과 대사
   → 로컬 저장
   → Scene 통합
   → 테스트
   → Windows 빌드 및 배포

B는 플레이어 물리, Tilemap 채굴, 구조 안정도 계산, 가스 범위, 건물 배치 알고리즘을 직접 구현하지 않는다.
이 기능들은 담당자 A가 제공하는 Runtime Prefab, 이벤트, 인터페이스와 DTO를 통해 사용한다.

2. 담당자 B의 최종 목표
   담당자 B의 작업이 완료되면 다음이 가능해야 한다.
   게임 시작 시 상태와 데이터가 초기화된다.
   새 게임과 이어하기가 가능하다.
   채굴 보상을 인벤토리에 저장한다.
   광물 수량, 중량과 미정산 가치를 계산한다.
   광물을 판매하고 시설 제작 비용을 지불한다.
   장비와 드론을 업그레이드한다.
   HUD가 실제 게임 상태와 일치한다.
   드론이 게임 상황을 분석하고 행동을 추천한다.
   게임 진행 상황을 로컬 JSON으로 저장한다.
   담당자 A의 Gameplay 시스템을 Integration Scene에 연결한다.
   Windows 실행 빌드를 만들고 배포한다.

3. 담당자 B의 소유 영역
   3.1 소유 폴더
   Assets/\_Project/Scripts/App/
   ├─ Core/
   ├─ State/
   ├─ Inventory/
   ├─ Economy/
   ├─ Progression/
   ├─ Drone/
   ├─ AI/
   ├─ Save/
   ├─ UI/
   ├─ Audio/
   └─ Build/

Assets/\_Project/Data/

Assets/\_Project/Prefabs/UI/

Assets/\_Project/Scenes/
├─ Bootstrap/
├─ MainMenu/
├─ SurfaceBase/
└─ Integration/

Assets/\_Project/Tests/EditMode/App/

Assets/\_Project/Tests/PlayMode/Integration/

Assets/\_Project/Editor/
├─ Build/
├─ DataValidation/
└─ SaveTools/

3.2 소유 Scene
Bootstrap.unity
MainMenu.unity
SurfaceBase.unity
Mine_Demo_Integration.unity

Mine_Demo_Integration.unity는 담당자 B만 수정한다.
3.3 소유 Prefab
BasicHUD.prefab
InventoryPanel.prefab
StructuralHUD.prefab
GasWarningPanel.prefab
BuildingMenu.prefab
OutpostPanel.prefab
SettlementPanel.prefab
DroneDialoguePanel.prefab
DroneReasonPanel.prefab
PauseMenu.prefab
SaveSlotPanel.prefab
DiggerBot_View.prefab

4. 담당자 B가 직접 수정하지 않는 영역
   다음은 담당자 A 소유이므로 직접 수정하지 않는다.
   Assets/\_Project/Scripts/Gameplay/
   Assets/\_Project/Prefabs/Gameplay/
   Assets/\_Project/Scenes/Test/Gameplay/
   Assets/\_Project/Tilemaps/

특히 다음 Runtime Prefab 내부를 직접 수정하지 않는다.
Player.prefab
SupportBeam_Runtime.prefab
Light_Runtime.prefab
Charger_Runtime.prefab
OutpostCore_Runtime.prefab
GasZone.prefab
DiggerBot_Runtime.prefab

변경이 필요하면 담당자 A에게 Issue를 작성한다.
Shared 계약도 합의 없이 변경하지 않는다.
Assets/\_Project/Scripts/Shared/

5. 담당자 B의 개발 원칙
   5.1 Integration Scene 단독 소유
   담당자 A의 기능을 최종 데모 Scene에 연결하는 작업은 B만 수행한다.
   A가 제공하는 것은 다음과 같다.
   Runtime Prefab
   설치 방법
   필수 참조 목록
   이벤트 목록
   검증 절차
   B는 이를 읽고 Integration Scene에 연결한다.
   5.2 UI와 상태 분리
   UI가 직접 데이터를 변경하지 않는다.
   UI 입력
   → Service 요청
   → State 변경
   → 이벤트 발생
   → UI 갱신

금지 예시:
creditsText.text = "999999";
playerCredits = 999999;

권장 예시:
판매 버튼
→ EconomyService.Sell
→ PlayerState.Credits 변경
→ CreditsChanged 이벤트
→ HUD 갱신

5.3 정적 데이터와 런타임 상태 분리
MineralData
→ 구리 가격, 무게, 아이콘

InventoryState
→ 현재 구리 보유량

ScriptableObject에는 정의 데이터를 저장하고, 플레이어별 진행 상황은 State와 SaveData에 저장한다.
5.4 Unity Object를 세이브하지 않기
저장 파일에는 다음만 넣는다.
ID
수량
위치
레벨
내구도
상태
시간
다음은 넣지 않는다.
GameObject
Sprite
TileBase
Prefab 참조
MonoBehaviour
Collider

6. 담당자 B의 공용 계약 확인
   작업 시작 전에 다음 Shared 계약을 확인한다.
   public interface IMiningRewardReceiver
   {
   void AddMineral(string mineralId, int quantity);
   }

public interface IResourceWallet
{
bool CanAfford(IReadOnlyList<ItemCostDto> costs);
bool TrySpend(IReadOnlyList<ItemCostDto> costs);
}

public interface IGameplayEventSink
{
void Publish(GameplayEventDto gameplayEvent);
}

public interface IWorldSnapshotProvider
{
WorldSaveData CaptureWorldSnapshot();
void RestoreWorldSnapshot(WorldSaveData saveData);
}

public interface IDroneContextProvider
{
DroneContextDto CreateContext();
}

B는 이 인터페이스의 Consumer 또는 구현체를 작성한다.

7. 단계별 개발 절차

B-0. Bootstrap과 기본 프로젝트 상태
목표
게임 시작 시 전역 서비스, 데이터와 State가 안전하게 초기화되도록 한다.
구현 항목
Bootstrap.unity
GameBootstrapper
SceneLoader
GameState
PlayerState
ProgressState
RunState
서비스 초기화 순서
데이터 카탈로그 로드
세이브 파일 존재 여부 확인
Main Menu 이동
전역 오브젝트 중복 방지
권장 초기화 순서
Bootstrap Scene 실행
→ 설정 로드
→ GameDataCatalog 로드
→ 데이터 ID 검증
→ SaveService 초기화
→ 기존 세이브 확인
→ GameState 생성 또는 복원
→ MainMenu 이동

완료 조건
[ ] 게임 시작 시 NullReference 오류가 없다.
[ ] 전역 서비스가 중복 생성되지 않는다.
[ ] Scene 이동 후 필요한 상태가 유지된다.
[ ] 세이브가 없으면 새 게임 상태를 만들 수 있다.
[ ] 데이터 ID 검증 실패 시 원인을 로그로 표시한다.

B-1. ScriptableObject 데이터 카탈로그
목표
광물, 시설, 레시피, 업그레이드와 대사 정의 데이터를 코드 수정 없이 관리할 수 있게 한다.
구현 항목
GameDataCatalog
MineralData
BuildingData
RecipeData
UpgradeData
DialogueTemplateData
내부 ID 규칙
데이터 조회 서비스
ID 중복 검증 Editor Tool
누락 참조 검증
내부 ID 예시
mineral.copper
mineral.iron
mineral.lithium

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

담당 원칙
A는 데이터를 읽기만 한다.
B가 실제 ScriptableObject 에셋 값을 관리한다.
배포 이후 내부 ID는 되도록 변경하지 않는다.
표시 이름과 내부 ID를 분리한다.
완료 조건
[ ] 구리·철·리튬 데이터가 존재한다.
[ ] 시설 비용 데이터가 존재한다.
[ ] 업그레이드 단계가 데이터로 관리된다.
[ ] 중복 ID를 자동으로 찾을 수 있다.
[ ] 누락된 Prefab 또는 아이콘을 찾을 수 있다.

B-2. 기본 HUD와 Game State
목표
플레이어가 현재 게임 상태를 쉽게 파악할 수 있게 한다.
HUD 항목
현재 전력
최대 전력
현재 깊이
골드
화물 중량
미정산 가치
구조 안정도
가스 위험
선택한 건설 시설
상호작용 안내
구현 원칙
매 프레임 모든 Text를 갱신하지 않는다.
다음 상태 변경 이벤트에 반응해 필요한 요소만 갱신한다.
EnergyChanged
CreditsChanged
InventoryChanged
DepthChanged
StructuralRiskChanged
GasExposureChanged
BuildingSelectionChanged

완료 조건
[ ] HUD가 State와 일치한다.
[ ] Scene 이동 후 UI 참조가 정상적으로 재연결된다.
[ ] 표시할 데이터가 없을 때 안전한 기본값을 사용한다.
[ ] 해상도가 변경되어도 UI가 화면 밖으로 나가지 않는다.

B-3. 인벤토리와 화물 시스템
목표
담당자 A가 전달한 광물 보상을 저장하고 중량과 가치를 계산한다.
구현 항목
InventoryState
InventoryService
IMiningRewardReceiver 구현
광물 ID별 수량
총 화물 중량
최대 적재량
미정산 가치
인벤토리 UI
광물 보관함 이동 처리
정산 완료 처리
처리 흐름
A의 MiningSystem
→ AddMineral(mineralId, quantity)
→ MineralData 조회
→ 수량 증가
→ 총 중량 재계산
→ 미정산 가치 재계산
→ InventoryChanged 이벤트
→ HUD 갱신

예외 처리
존재하지 않는 광물 ID
음수 수량
최대 적재량 초과
중복 지급
데이터 카탈로그 누락
완료 조건
[ ] 광물 ID별 수량이 정확하다.
[ ] 중량과 가치가 자동으로 계산된다.
[ ] 최대 적재량을 초과하지 않는다.
[ ] UI가 즉시 갱신된다.
[ ] 존재하지 않는 ID가 들어와도 게임이 중단되지 않는다.

B-4. 경제, 판매와 제작
목표
광물을 판매하거나 시설 제작 비용으로 사용할 수 있게 한다.
구현 항목
EconomyService
CraftingService
IResourceWallet 구현
광물 판매
골드 지급
시설 비용 검사
자원 차감
제작 레시피
판매·제작 UI
거래 결과 이벤트
판매 흐름
판매할 광물 선택
→ 보유량 확인
→ 판매 가격 계산
→ 인벤토리 차감
→ 골드 증가
→ 상태 이벤트
→ UI 갱신
→ 자동 저장 요청

건설 비용 흐름
A가 설치 위치 검증
→ B의 CanAfford 호출
→ 설치 가능
→ A가 Runtime Prefab 생성
→ 생성 성공
→ B의 TrySpend 호출
→ 자원 차감

설치 실패 시 자원이 차감되지 않아야 한다.
완료 조건
[ ] 판매한 광물만 감소한다.
[ ] 골드가 정확히 증가한다.
[ ] 시설 비용이 정확히 검사된다.
[ ] 설치 실패 시 자원이 유지된다.
[ ] 판매와 제작 결과가 저장 대상에 반영된다.

B-5. 업그레이드와 진행도
목표
탐사 결과를 통해 플레이어와 드론이 성장하도록 한다.
MVP 업그레이드
드릴 속도
드릴 전력 효율
최대 전력
최대 화물 중량
드론 스캔 범위
드론 구조 보존량
가스 저항
구현 항목
UpgradeState
ProgressionService
업그레이드 비용
최대 레벨
현재 레벨
효과 계산
A 시스템이 사용할 효과 조회 인터페이스
업그레이드 UI
심층 구역 잠금 해제
A와 연결되는 방식
A의 시스템이 B의 구체 클래스를 참조하지 않도록 단순 Provider를 제공한다.
GetDrillSpeedMultiplier
GetEnergyEfficiencyMultiplier
GetMaximumCargoWeight
GetDroneScanRadius
GetGasResistance

완료 조건
[ ] 업그레이드 구매 시 비용이 차감된다.
[ ] 최대 레벨을 초과하지 않는다.
[ ] 효과가 실제 Gameplay에 적용된다.
[ ] 저장 후에도 레벨이 유지된다.
[ ] 심층 잠금 해제가 진행도와 연결된다.

B-6. 건설 메뉴와 위험 UI
목표
담당자 A의 건설 배치 시스템과 구조·가스 상태를 사용자에게 명확하게 표시한다.
구현 항목
건설 항목 목록
건설 비용
보유 자원
시설 설명
전력 소비량
선택 상태
설치 가능 여부 표시
구조 안정도 HUD
가스 경고
전력 연결 상태
충전기·정산 콘솔 상호작용 UI
건설 연결 흐름
BuildingMenu에서 시설 선택
→ BuildingData 전달
→ A의 BuildingPlacementSystem이 Preview 시작
→ 위치 유효성 확인
→ 비용 검사
→ 설치 결과 이벤트
→ UI 상태 초기화

완료 조건
[ ] 시설을 메뉴에서 선택할 수 있다.
[ ] 자원 부족 여부가 표시된다.
[ ] 구조 위험 단계가 이해하기 쉽게 표시된다.
[ ] 가스 노출 시 즉시 경고가 표시된다.
[ ] 설치 후 선택 상태가 정상적으로 종료된다.

B-7. 전진기지 UI와 정산
목표
전진기지를 실제 탐사 거점으로 활용할 수 있게 한다.
구현 항목
Outpost Panel
전력 공급량·소비량
연결된 시설 목록
충전 UI
광물 보관함 UI
정산 콘솔
체크포인트 상태
전진기지 설치 튜토리얼
자동 저장 요청
A와 역할 구분
A:
시설 연결 여부
시설 활성화
상호작용 가능 거리
코어와 케이블 Runtime
B:
플레이어 전력 증가
광물 이동
광물 정산
체크포인트 State
UI
자동 저장
완료 조건
[ ] 전진기지 전력 상태가 표시된다.
[ ] 연결되지 않은 시설의 원인이 표시된다.
[ ] 충전 후 전력 수치가 변경된다.
[ ] 보관함에 넣은 광물이 별도로 관리된다.
[ ] 정산한 광물은 골드로 전환된다.
[ ] 기지 설치 후 자동 저장이 요청된다.

B-8. 드론 판단과 대사
목표
담당자 A가 제공한 DroneContextDto를 분석하여 일관된 추천과 설명을 만든다.
구현 항목
DroneAnalysisService
행동 점수 계산
안전 우선 성향
귀환 추천
버팀목 추천
가스 이탈 추천
고가 광물 탐사 추천
판단 근거
대사 템플릿
대사 우선순위
대사 쿨다운
Drone Dialogue UI
행동 후보
ReturnToBase
InstallSupport
LeaveGasZone
MineNearbyMineral
BuildOutpost
ContinueDescending
Recharge

점수 예시
전력 부족 귀환 +40
높은 미정산 화물 가치 귀환 +20
구조 위험 버팀목 +30
가스 위험 이탈 +50
인근 리튬 채굴 +20
기지와 가까움 충전 +15

판단 원칙
게임 결과는 A와 Unity 규칙 시스템이 결정한다.
B의 드론 시스템은 추천만 한다.
동일한 Context에서는 같은 추천을 낸다.
대사는 새로운 수치나 사실을 만들어내지 않는다.
대사 우선순위
즉시 생존 위험
→ 붕괴 임박
→ 가스 위험
→ 전력 부족
→ 귀환 추천
→ 희귀 광물 발견
→ 일반 탐사 대사

완료 조건
[ ] 동일 상황에서 일관된 추천이 나온다.
[ ] 판단 근거와 실제 수치가 일치한다.
[ ] 위험 대사가 일반 대사보다 우선한다.
[ ] 같은 대사가 과도하게 반복되지 않는다.
[ ] 생성형 AI 없이도 전체 기능이 작동한다.

B-9. 선택적 클라우드 AI 대사
적용 시점
기본 템플릿 드론 시스템이 완성된 후에만 추가한다.
구조
DroneAnalysisService
→ 추천 행동과 근거 확정
→ CloudDialogueGenerator 요청
→ 성공 시 자연어 대사
→ 실패 시 TemplateDialogueGenerator

금지 사항
API 키를 Unity 클라이언트에 직접 포함하지 않는다.
AI가 구조 위험이나 피해 여부를 다시 계산하게 하지 않는다.
매 프레임 API를 호출하지 않는다.
API 실패가 게임 진행을 막게 하지 않는다.
호출 이벤트
새로운 심도 구역
가스 발견
붕괴 임박
고가 광물 발견
전력 부족
전진기지 설치
플레이어가 직접 분석 요청
완료 조건
[ ] API 실패 시 템플릿 대사가 즉시 출력된다.
[ ] 게임 핵심 로직은 인터넷 없이 작동한다.
[ ] 호출 횟수 제한과 쿨다운이 존재한다.
[ ] API 키가 빌드에 포함되지 않는다.

B-10. 로컬 세이브 시스템
목표
게임 종료 후에도 플레이어 진행도와 월드 변경 상태를 유지한다.
구현 항목
GameSaveData
PlayerSaveData
ProgressSaveData
DroneSaveData
A의 WorldSaveData
SaveService
LoadService
JSON 변환
임시 파일
백업 파일
Save Version
저장 슬롯
자동 저장
이어하기 메뉴
세이브 마이그레이션
파일 구조
save_slot_1.json
save_slot_1.backup.json
save_slot_1.tmp

저장 순서
State 수집
→ A의 CaptureWorldSnapshot 호출
→ GameSaveData 생성
→ tmp 파일 기록
→ JSON 검증
→ 기존 파일 backup 이동
→ tmp를 정식 파일로 교체

로드 순서
정상 세이브 확인
→ 로드 성공
→ State 복원
→ Scene 로드
→ A의 RestoreWorldSnapshot 호출

정상 세이브 실패
→ backup 시도

backup 실패
→ 오류 안내 또는 새 게임

자동 저장 시점
지상 귀환
광물 정산
업그레이드 구매
전진기지 설치
새로운 구역 진입
구조 실패
게임 종료 요청
일정 주기이며 변경 사항이 존재할 때
완료 조건
[ ] 골드와 업그레이드가 유지된다.
[ ] 인벤토리가 유지된다.
[ ] 채굴한 타일이 유지된다.
[ ] 설치한 시설이 유지된다.
[ ] 저장 실패 시 백업을 시도한다.
[ ] saveVersion이 기록된다.
[ ] 누락된 데이터에 기본값을 적용할 수 있다.

B-11. Main Menu와 Surface Base
Main Menu 기능
새 게임
이어하기
저장 슬롯
설정
종료
게임 버전 표시
Surface Base 기능
광물 판매
시설 제작
업그레이드
계약 또는 목표 확인
탐사 시작
심층 잠금 상태
최근 탐사 결과
완료 조건
[ ] 새 게임이 정상 시작된다.
[ ] 이어하기가 정상 작동한다.
[ ] 세이브가 없으면 이어하기가 비활성화된다.
[ ] 지상에서 판매와 업그레이드가 가능하다.
[ ] 탐사 시작 시 Mine Scene으로 이동한다.

B-12. Integration Scene 구성
목표
A가 제공한 Gameplay 시스템과 B의 State·UI·Save 시스템을 하나의 플레이 가능한 Scene으로 연결한다.
기본 구조
Mine_Demo_Integration
├─ Main Camera
├─ Global Light 2D
├─ Grid
│ ├─ Background Tilemap
│ ├─ Foreground Tilemap
│ ├─ Hazard Tilemap
│ └─ Building Tilemap
├─ GameplayRoot
│ ├─ Player
│ ├─ DiggerBot_Runtime
│ ├─ WorldSystems
│ └─ RuntimeBuildings
├─ ApplicationRoot
│ ├─ GameStateBridge
│ ├─ InventoryService
│ ├─ EconomyService
│ ├─ DroneAnalysisService
│ └─ SaveBridge
├─ HUDCanvas
└─ EventSystem

통합 순서

1. A의 Prefab 배치
2. 필수 Tilemap 참조 연결
3. Shared 인터페이스 구현체 연결
4. Gameplay 이벤트를 B의 EventSink에 연결
5. HUD와 State 연결
6. Save Provider 연결
7. 통합 테스트

주의사항
A의 Runtime Prefab 내부를 수정하지 않는다.
필요한 변경은 Prefab Variant 또는 ViewSocket을 활용한다.
Scene 연결만 변경한 PR에는 unrelated 코드 변경을 넣지 않는다.
통합 이후 Console Warning도 가능한 범위에서 제거한다.

B-13. 데모 흐름과 튜토리얼
필수 데모 흐름
탐사 시작
→ 구리·철 채굴
→ 안전 경로와 위험 경로 안내
→ 리튬 경로 선택
→ 구조 균열
→ 버팀목 설치
→ 가스 발생
→ 전진기지 설치
→ 드론 귀환 추천
→ 광물 정산
→ 배터리 업그레이드
→ 심층 신호 확인

B가 담당하는 연출
시작 안내
목표 UI
튜토리얼 문구
드론 대사
정산 결과
업그레이드 결과
심층 잠금 표시
데모 종료 화면
기본 사운드 연결
완료 조건
[ ] 처음 플레이하는 사람이 목표를 이해한다.
[ ] 튜토리얼이 조작을 과도하게 방해하지 않는다.
[ ] 위험 상황에서 필요한 UI가 즉시 표시된다.
[ ] 데모 시작부터 종료까지 막힘 없이 완주된다.

B-14. 빌드와 배포
최초 대상
Windows x64 Standalone
Build Profile
Development
Development Build
디버그 HUD
자원 추가 도구
강제 저장·로드
상세 로그
QA
실제 Release와 유사한 설정
디버그 치트 비활성화
오류 로그 유지
Save Migration 테스트
Release
Development Build 비활성화
디버그 메뉴 제거
최종 아이콘
최종 앱 이름
버전·빌드 번호
불필요한 로그 제거
배포 파일
SubTerra_MVP_0.5.0/
├─ SubTerra.exe
├─ SubTerra_Data/
├─ UnityPlayer.dll
├─ README.txt
└─ CHANGELOG.txt

폴더 전체를 ZIP으로 압축한다.
배포 후보
GitHub Releases
itch.io 비공개 페이지
해커톤 제출 플랫폼
직접 공유 링크
완료 조건
[ ] Windows 빌드가 생성된다.
[ ] 개발 PC 밖의 다른 PC에서 실행된다.
[ ] 세이브 경로가 정상적으로 생성된다.
[ ] 새 게임과 이어하기가 빌드에서도 작동한다.
[ ] 필수 DLL과 Data 폴더가 누락되지 않는다.

8. 담당자 B의 테스트 책임
   Edit Mode 테스트
   인벤토리 수량
   중량 계산
   판매 가격
   제작 비용
   업그레이드 최대 레벨
   드론 점수
   대사 우선순위
   Save DTO
   Save Migration
   데이터 ID 검증
   Play Mode 및 Integration 테스트
   Bootstrap
   Scene 이동
   HUD 연결
   채굴 보상 수신
   판매·제작
   건설 UI와 A 시스템 연결
   드론 Context와 추천
   자동 저장
   이어하기
   월드 복원
   구조 실패 후 복귀
   매 빌드 전 체크
   [ ] 새 게임
   [ ] 이어하기
   [ ] 이동
   [ ] 채굴
   [ ] 인벤토리
   [ ] 판매
   [ ] 제작
   [ ] 버팀목
   [ ] 가스
   [ ] 전진기지
   [ ] 드론 추천
   [ ] 자동 저장
   [ ] 게임 재실행
   [ ] 월드 복원
   [ ] 해상도 변경
   [ ] 다른 PC 실행

9. 담당자 B의 Git 작업 절차
   작업 시작
   git checkout develop
   git pull
   git checkout -b feature/b-기능명

브랜치 예시
feature/b-bootstrap
feature/b-game-data
feature/b-inventory
feature/b-economy
feature/b-building-ui
feature/b-drone-analysis
feature/b-save-system
feature/b-integration-scene
feature/b-windows-build

커밋 예시
feat: add inventory state and mineral rewards
feat: add safe drone recommendation rules
feat: add local save backup recovery
fix: prevent resource spending on failed placement
test: add save migration tests
build: add Windows QA build profile

PR 전
App 테스트 실행
Integration 테스트 실행
Scene 변경 사항 확인
A 소유 파일 변경 여부 확인
Console 오류 확인
세이브 호환성 확인
PR 작성

10. 담당자 B의 A 작업물 수령 절차
    A의 PR이 병합되면 다음 순서로 작업한다.
1. A의 인수인계 문서 확인
1. Test Scene에서 A 기능 단독 검증
1. Runtime Prefab과 필수 참조 확인
1. Integration Scene 백업 또는 별도 브랜치 생성
1. Prefab 배치
1. Shared 인터페이스 연결
1. 이벤트와 UI 연결
1. 저장 Provider 연결
1. 전체 흐름 테스트
1. Integration PR 생성

A 기능에 문제가 있으면 Runtime Prefab을 직접 수정하지 않고 재현 절차를 포함한 Issue를 작성한다.

11. 담당자 B의 A에게 보내는 수정 요청 형식
    [B → A 수정 요청]

기능명:
BuildingPlacementSystem

발생 위치:
Mine_Demo_Integration

재현 절차:

1. 조명을 선택한다.
2. 기존 버팀목 위에 Preview를 이동한다.
3. 설치 버튼을 누른다.

현재 결과:
건물이 겹친 상태로 설치됨.

기대 결과:
겹침 판정으로 설치가 취소되어야 함.

관련 데이터:
building.light.basic

수정 요청 범위:
Gameplay/Building 내부만 수정 요청.

Integration Scene은 B가 유지함.

12. 담당자 B용 Codex 작업 규칙
    권장 프롬프트
    Project Sub-Terra Unity 프로젝트다.

Assets/\_Project/Scripts/App/Inventory,
Assets/\_Project/Tests/EditMode/App/Inventory 범위만 수정하라.

Shared의 IMiningRewardReceiver를 구현하되 변경하지 마라.
Gameplay 폴더와 Runtime Prefab은 수정하지 마라.
Integration Scene도 이번 작업에서는 수정하지 마라.

구현 후:

1. 변경 파일
2. 상태 변경 흐름
3. 예외 처리
4. 테스트 결과
5. A 시스템과 연결하는 방법
   을 정리하라.

Integration용 프롬프트
Mine_Demo_Integration Scene 연결 작업을 검토하라.

코드와 Runtime Prefab은 수정하지 말고,
현재 제공된 인터페이스와 Prefab을 사용해 필요한 연결 목록만 제안하라.

Scene YAML을 직접 수정하지 말고
Unity Editor에서 수행할 단계 또는 Editor Script를 작성하라.

금지 프롬프트
게임 전체를 확인하고 부족한 Gameplay 코드도 모두 수정해라.

B의 Codex가 A의 Gameplay 영역까지 수정하게 해서는 안 된다.

13. 담당자 B 최종 완료 체크리스트
    [ ] Bootstrap이 정상 작동한다.
    [ ] 데이터 카탈로그가 로드된다.
    [ ] 중복 ID를 검증할 수 있다.
    [ ] 인벤토리와 중량이 작동한다.
    [ ] 광물 판매가 가능하다.
    [ ] 건설 비용이 정확히 처리된다.
    [ ] 업그레이드가 실제 Gameplay에 적용된다.
    [ ] HUD가 State와 일치한다.
    [ ] 구조와 가스 경고가 표시된다.
    [ ] 드론이 적절한 행동을 추천한다.
    [ ] 대사 우선순위와 쿨다운이 작동한다.
    [ ] 로컬 저장과 백업이 작동한다.
    [ ] 이어하기가 가능하다.
    [ ] A의 월드 상태가 복원된다.
    [ ] Main Menu와 Surface Base가 작동한다.
    [ ] Integration Scene을 완주할 수 있다.
    [ ] Windows 빌드가 다른 PC에서 실행된다.

14. 담당자 B의 최종 인도물
    Bootstrap Scene
    Main Menu Scene
    Surface Base Scene
    Mine Demo Integration Scene
    GameDataCatalog
    Mineral·Building·Recipe·Upgrade Data
    GameState와 PlayerState
    InventoryService
    EconomyService
    CraftingService
    ProgressionService
    DroneAnalysisService
    TemplateDialogueGenerator
    HUD와 모든 UI Prefab
    SaveService와 LoadService
    Save Migration
    Build Profiles
    Windows Release ZIP
    README
    CHANGELOG
    Integration QA 결과

담당자 B의 개발 완료 기준은 개별 시스템이 존재하는 것이 아니라, 담당자 A의 작업물을 안정적으로 통합하여 새 게임부터 저장·재실행·데모 종료까지 전체 게임 흐름을 완주할 수 있는 상태가 되는 것이다.
