Project Sub-Terra
2인 개발 역할 분담 및 병합 안정화 계획

1. 역할 분담의 핵심 원칙
   두 사람이 단순히 기능 목록을 절반씩 나누는 방식은 피한다.
   예를 들어 A가 채굴을 만들고 B가 구조 안정도를 만들면 두 기능이 같은 Tilemap, 같은 Mine Scene, 같은 광물 데이터에 접근하게 되어 충돌 가능성이 높아진다.
   따라서 역할을 다음 두 영역으로 분리한다.
   담당자 A
   탐사 공간에서 실제로 움직이는 게임 월드와 게임플레이 담당

담당자 B
게임 상태, UI, 경제, 저장, AI 설명, 메뉴, 빌드와 배포 담당

정리하면 다음과 같다
담당자 A
Core Gameplay & World Simulation
플레이어 조작
Tilemap
채굴
지하 월드
구조 안정도
붕괴
가스
건설 배치
전력망
월드 오브젝트
탐사 런타임
담당자 B
Game State, UX & Infrastructure
정적 데이터 카탈로그
인벤토리
경제
제작과 업그레이드
드론 판단 및 대사
HUD와 메뉴
로컬 세이브
게임 진행도
Bootstrap
빌드
테스트 통합
배포

2. 권장 담당자 배치
   담당자 A에 적합한 사람
   Unity Editor 조작이 상대적으로 익숙함
   Rigidbody2D, Collider2D, Tilemap에 관심이 있음
   플레이어 이동과 상호작용을 직접 만지는 것을 선호함
   화면에서 즉시 결과가 보이는 작업을 선호함
   물리·좌표·Grid 계산에 거부감이 적음
   담당자 B에 적합한 사람
   데이터 구조와 상태 관리에 익숙함
   UI와 사용자 흐름 설계를 선호함
   저장·불러오기와 예외 처리에 관심이 있음
   AI 연동과 API 구조에 관심이 있음
   빌드, 배포, 문서화와 통합 테스트를 관리할 수 있음
   사용자가 기존에 React, React Native, Supabase, AI API와 문서 설계 경험을 가지고 있으므로 B 역할과 잘 맞는다.
   다만 B도 Unity Canvas, Prefab, ScriptableObject와 Scene 연결은 익혀야 한다.

3. 최종 역할 요약
   영역
   담당자 A
   담당자 B
   Unity 프로젝트 초기 설정
   공동
   공동
   플레이어 이동
   주 담당
   검수
   카메라 추적
   주 담당
   검수
   Tilemap
   주 담당
   데이터 제공
   채굴
   주 담당
   인벤토리 인터페이스 제공
   구조 안정도
   주 담당
   UI 표시
   붕괴
   주 담당
   로그·저장
   가스 위험
   주 담당
   UI·드론 설명
   건설 배치
   주 담당
   비용·건설 UI
   전력망
   주 담당
   상태 UI·업그레이드
   광물 데이터 정의 클래스
   공동 계약
   데이터 에셋 관리
   인벤토리
   이벤트 발생
   주 담당
   판매·경제
   결과 전달
   주 담당
   제작
   배치 실행
   비용·레시피 담당
   업그레이드
   적용 지점 제공
   주 담당
   드론 이동
   주 담당
   검수
   드론 판단
   게임 상태 제공
   주 담당
   드론 대사
   이벤트 제공
   주 담당
   HUD
   데이터 이벤트 제공
   주 담당
   메뉴
   미참여
   주 담당
   로컬 세이브
   월드 스냅샷 제공
   주 담당
   Main Demo Scene
   수정 요청만
   단독 소유
   Gameplay Test Scene
   단독 소유
   사용만
   빌드·배포
   빌드 검수
   주 담당
   게임 밸런스
   공동
   공동
   최종 QA
   공동
   공동

4. 폴더 소유권 분리
   두 사람이 같은 파일을 수정하지 않도록 폴더 소유권을 명확하게 지정한다.
   4.1 담당자 A 소유 폴더
   Assets/\_Project/Scripts/Gameplay/
   ├─ Player/
   ├─ Mining/
   ├─ World/
   ├─ Structural/
   ├─ Hazards/
   ├─ Building/
   ├─ Power/
   └─ Interaction/

Assets/\_Project/Prefabs/Gameplay/
├─ Player/
├─ World/
├─ Buildings/
├─ Hazards/
└─ Effects/

Assets/\_Project/Scenes/Test/Gameplay/

Assets/\_Project/Tilemaps/

Assets/\_Project/Tests/EditMode/Gameplay/

Assets/\_Project/Tests/PlayMode/Gameplay/

담당자 B는 이 폴더 안의 파일을 직접 수정하지 않는다.
수정이 필요할 경우 Issue 또는 요청 문서로 A에게 전달한다.

4.2 담당자 B 소유 폴더
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

담당자 A는 이 폴더 안의 파일을 직접 수정하지 않는다.

4.3 공동 계약 폴더
두 시스템이 서로 통신하기 위한 인터페이스와 DTO는 별도의 공유 폴더에 둔다.
Assets/\_Project/Scripts/Shared/
├─ Contracts/
├─ Events/
├─ DTO/
├─ IDs/
└─ Common/

이 폴더는 자유롭게 수정하지 않는다.
변경 절차:
변경 필요 발견
→ Issue 작성
→ 두 사람 합의
→ 인터페이스 수정 PR 병합
→ A와 B가 각자 구현 수정

Shared 폴더를 수정하는 PR에는 다른 기능 변경을 섞지 않는다.

5. Scene 소유권
   Unity 협업에서 가장 충돌이 자주 발생하는 파일은 .unity Scene이다.
   따라서 Scene을 다음처럼 나눈다.
   담당자 A 소유 Scene
   Gameplay_Player_Test.unity
   Gameplay_Mining_Test.unity
   Gameplay_Structural_Test.unity
   Gameplay_Hazard_Test.unity
   Gameplay_Building_Test.unity

A는 이 Scene에서 기능을 개발하고 검증한다.
담당자 B 소유 Scene
Bootstrap.unity
MainMenu.unity
SurfaceBase.unity
Mine_Demo_Integration.unity

최종 시연 Scene인 Mine_Demo_Integration.unity는 B만 수정한다.
A가 새로운 기능을 추가했을 때는 Scene을 직접 수정하지 않고 다음 중 하나를 제공한다.
완성된 Prefab
설치 방법
필요한 ScriptableObject
자동 설정용 Editor Script
Inspector 연결 목록
예:
Player.prefab을 Mine_Demo_Integration에 배치

필요 참조:

- ForegroundTilemap
- PlayerEnergyService
- MiningRewardReceiver

기본 위치:

- Grid 좌표 (0, 2)

B가 해당 내용을 통합 Scene에 적용한다.

6. Prefab 소유권
   Prefab도 한 사람이 단독 소유한다.
   담당자 A
   Player.prefab
   MiningCursor.prefab
   SupportBeam_Runtime.prefab
   Light_Runtime.prefab
   Charger_Runtime.prefab
   OutpostCore_Runtime.prefab
   GasZone.prefab
   DiggerBot_Runtime.prefab

담당자 B
HUD.prefab
DroneDialoguePanel.prefab
InventoryPanel.prefab
BuildingMenu.prefab
SurfaceBaseUI.prefab
PauseMenu.prefab
SaveSlotPanel.prefab
DiggerBot_View.prefab

드론은 Nested Prefab 방식으로 나눌 수 있다.
DiggerBot_Runtime.prefab
├─ DroneFollower
├─ DroneSensor
├─ Collider2D
├─ LightAnchor
└─ ViewSocket
└─ DiggerBot_View.prefab

A는 드론의 이동과 센서를 담당한다.
B는 드론 외형, 말풍선과 UI 상태 표시를 담당한다.

7. 두 담당자가 연결되는 인터페이스
   두 사람이 서로의 구체 클래스에 직접 접근하면 결합도가 높아진다.
   A는 B의 InventoryManager 클래스를 직접 참조하지 않고 인터페이스만 사용한다.

7.1 채굴 보상 인터페이스
public interface IMiningRewardReceiver
{
void AddMineral(string mineralId, int quantity);
}

A의 채굴 시스템:
타일 채굴 완료
→ mineralId와 quantity 확인
→ IMiningRewardReceiver 호출

B의 인벤토리 시스템:
IMiningRewardReceiver 구현
→ 보유량 증가
→ 중량과 가치 재계산
→ UI 이벤트 발생

7.2 건설 비용 인터페이스
public interface IResourceWallet
{
bool CanAfford(IReadOnlyList<ItemCostDto> costs);
bool TrySpend(IReadOnlyList<ItemCostDto> costs);
}

A는 건설 위치와 배치를 담당한다.
B는 자원 보유 여부와 자원 차감을 담당한다.
A: 설치 가능한 위치인가?
B: 비용을 낼 수 있는가?
A: 실제 시설 배치
B: 자원 차감 결과 기록

7.3 게임 상태 이벤트
public interface IGameplayEventSink
{
void Publish(GameplayEventDto gameplayEvent);
}

A가 전달할 이벤트 예시:
TileMined
MineralDiscovered
GasTriggered
StructuralRiskChanged
BuildingPlaced
OutpostActivated
PlayerRescued
DepthZoneEntered

B는 이 이벤트를 이용한다.
HUD 갱신
드론 분석
자동 저장
튜토리얼 메시지
효과음 재생
계약 진행도 갱신

7.4 월드 저장 인터페이스
public interface IWorldSnapshotProvider
{
WorldSaveData CaptureWorldSnapshot();
void RestoreWorldSnapshot(WorldSaveData saveData);
}

A는 월드 상태를 DTO 형태로 제공한다.
B는 해당 DTO를 JSON으로 저장한다.
B는 Tilemap의 내부 구현을 알 필요가 없다.

7.5 드론 분석 Context
public sealed class DroneContextDto
{
public int depth;
public int currentEnergy;
public int returnEnergyEstimate;

    public float structuralIntegrity;
    public float gasRisk;

    public long unsettledCargoValue;
    public float cargoWeight;

    public float nearestBaseDistance;
    public List<string> nearbyMineralIds;

}

A가 실제 월드 정보를 수집해 DTO를 만든다.
B가 이 Context로 행동 점수와 대사를 생성한다.

8. 프로젝트 초기 공동 작업
   본격적으로 나누기 전에 두 사람이 함께 한 번만 설정해야 한다.
   단계 0-1. Unity 버전 고정
   Unity 6 LTS 버전 확정
   두 사람 모두 같은 버전 설치
   프로젝트 업그레이드는 공동 합의 없이 하지 않음
   단계 0-2. Git 설정
   GitHub 저장소 생성
   Unity .gitignore
   Visible Meta Files
   Force Text Serialization
   Library, Temp, Logs, Obj 제외
   대용량 에셋 도입 시 Git LFS 검토
   단계 0-3. 공통 폴더 생성
   앞서 정의한 폴더를 미리 생성한다.
   빈 폴더는 Git에서 추적하지 않으므로 필요하면 .gitkeep을 넣는다.
   단계 0-4. 공통 데이터 계약 작성
   최소 인터페이스를 먼저 확정한다.
   IMiningRewardReceiver
   IResourceWallet
   IGameplayEventSink
   IWorldSnapshotProvider
   IDroneContextProvider

처음부터 모든 인터페이스를 만들 필요는 없지만, 첫 통합에 필요한 계약은 미리 정한다.
단계 0-5. 기본 통합 Scene 생성
B가 Mine_Demo_Integration.unity를 생성한다.
초기에는 다음 오브젝트만 포함한다.
Main Camera
Global Light 2D
Grid
Foreground Tilemap
Background Tilemap
GameplayRoot
ApplicationRoot
HUDCanvas
EventSystem

9. 단계별 역할 분담

9.1 1단계: 플레이어 이동과 상태 기반 구축
담당자 A
작업
Input System 설정
플레이어 이동
Rigidbody2D
Collider2D
카메라 추적
단순 Tilemap 충돌
Player.prefab
Gameplay_Player_Test.unity
제공 결과
Player.prefab
PlayerController
PlayerMovementConfig
Player 이동 테스트
통합 설치 방법

완료 조건
좌우 이동 가능
벽과 바닥 관통 없음
입력을 놓으면 정상 정지
Console 오류 없음
Test Scene에서 단독 실행 가능

담당자 B
작업
GameBootstrapper
GameState
PlayerState
기본 HUD
전력 수치 표시
광물 수량 표시용 임시 UI
ScriptableObject 데이터 기본 구조
게임 데이터 ID 규칙
제공 결과
Bootstrap Scene
PlayerState
GameDataCatalog
BasicHUD.prefab
ID 검증 도구

완료 조건
새 게임 상태 생성
HUD에서 전력과 골드 표시
ScriptableObject 데이터 로드 가능
중복 ID 검사가 작동

첫 번째 통합
B가 Player.prefab을 통합 Scene에 배치한다.
확인할 내용:
[ ] 플레이어 이동
[ ] HUD 출력
[ ] State 초기화
[ ] Scene 이동 후 전역 서비스 유지

9.2 2단계: 채굴과 인벤토리
담당자 A
작업
Foreground Tilemap
MiningTileData 적용
방향 채굴
채굴 시간
타일 제거
전력 소비 요청
채굴 보상 이벤트
채굴 진행도 제공
제공 결과
MiningSystem
PlayerMiningController
MiningTileResolver
Mining test scene
IMiningRewardReceiver 연결

완료 조건
일반 암석과 광물 구분
광물별 채굴 시간이 다름
채굴 완료 시 타일 제거
보상 이벤트가 한 번만 발생
전력 부족 시 채굴 불가

담당자 B
작업
InventoryState
광물 수량
화물 중량
미정산 가치
Inventory HUD
MineralData ScriptableObject 에셋 생성
구리·철·리튬 데이터 입력
제공 결과
InventoryService
InventoryState
MineralData assets
InventoryPanel.prefab
IMiningRewardReceiver 구현

완료 조건
광물 ID별 수량 저장
채굴 보상 수신
중량 계산
미정산 가치 계산
UI 즉시 갱신

두 번째 통합
플레이어가 구리 타일 채굴
→ A의 MiningSystem이 보상 이벤트 발생
→ B의 InventoryService가 구리 추가
→ HUD 수량과 가치 갱신

이 흐름이 정상 동작해야 다음 단계로 넘어간다.

9.3 3단계: 경제와 구조 안정도
담당자 A
작업
주변 지지 타일 계산
구조 안정도 점수
위험 단계
균열 표시
부분 붕괴
버팀목 영향 범위
구조 상태 이벤트
제공 결과
StructuralIntegritySystem
StructuralRegionState
SupportInfluenceCalculator
붕괴 테스트 Scene
구조 상태 DTO

완료 조건
넓게 파면 안정도가 감소
버팀목 설치 시 안정도 증가
위험 단계가 결정론적으로 계산
전체 Tilemap이 아닌 주변만 재계산
붕괴가 월드 변경점으로 남음

담당자 B
작업
지상 광물 판매
골드 계산
제작 비용
건설 비용 검증
구조 안정도 HUD
드론 구조 경고 템플릿
업그레이드 기본 구조
제공 결과
EconomyService
CraftingService
IResourceWallet
StructuralHUD
Drone warning templates

완료 조건
광물 판매 가능
판매한 광물만 인벤토리에서 감소
골드 증가
버팀목 비용 검증
구조 위험 UI 표시

세 번째 통합
철 채굴
→ 철을 판매하지 않음
→ 버팀목 비용으로 철 사용
→ A가 버팀목 배치
→ 구조 안정도 상승
→ B의 HUD와 드론 경고 갱신

9.4 4단계: 가스와 건설
담당자 A
작업
가스 타일
가스 위험 범위
가스 노출 판정
건설 Preview
Grid 배치
시설 충돌 검사
버팀목·조명·충전기 Runtime Prefab
건물 설치 이벤트
제공 결과
HazardSystem
GasZone
BuildingPlacementSystem
Runtime building prefabs
Building test scene

완료 조건
가스 타일 채굴 시 위험 활성화
위험 범위 안에서 효과 발생
빈 공간에만 시설 설치
자원 부족 시 설치 확정 안 됨
건물 겹침 방지

담당자 B
작업
건설 메뉴
건물별 비용과 설명
설치 선택 상태
가스 HUD
노출 경고
가스 드론 대사
제작 레시피 ScriptableObject
자원 차감
제공 결과
BuildingMenu.prefab
RecipeData
BuildingData assets
Gas warning UI
Resource spending implementation

완료 조건
시설 선택 가능
필요 자원 표시
자원 부족 표시
가스 위험 UI 표시
A의 배치 성공 결과 이후에만 자원 차감 확정
건설 실패 시 자원이 차감되지 않도록 주의한다.

9.5 5단계: 전력망과 전진기지
담당자 A
작업
전진기지 코어
케이블 연결
시설 연결 탐색
공급량과 소비량
조명 작동
충전기 작동
정산 콘솔 상호작용 지점
체크포인트 위치
제공 결과
PowerNetworkSystem
OutpostCore runtime
Cable runtime
PoweredBuilding interface
전력망 디버그 표시

완료 조건
코어와 연결되지 않은 시설은 비활성화
케이블 추가 시 연결 상태 갱신
시설 제거 시 전력망 재계산
연결된 충전기와 조명 작동

담당자 B
작업
전진기지 UI
전력 공급·소비 표시
충전 처리
정산 콘솔 UI
체크포인트 상태
전진기지 설치 시 자동 저장 요청
전진기지 관련 튜토리얼
제공 결과
OutpostPanel
PowerStatusHUD
SettlementPanel
CheckpointState
Autosave event

완료 조건
전력 부족 원인 표시
충전 전후 전력 수치 반영
정산한 광물과 남은 광물 구분
전진기지 설치 후 저장 이벤트 발생

9.6 6단계: AI 드론
담당자 A
작업
드론 플레이어 추적
장애물 대응
주변 광물 스캔
구조 위험 정보 제공
가스 정보 제공
기지 거리 계산
DroneContextDto 생성
제공 결과
DiggerBot_Runtime.prefab
DroneFollower
DroneSensor
DroneContextProvider
Drone gameplay test scene

완료 조건
플레이어를 안정적으로 추적
플레이어와 지나치게 겹치지 않음
상황 데이터가 정확함
잘못된 타일 좌표에서 예외 없음

담당자 B
작업
드론 행동 점수
안전 우선 판단
귀환 추천
버팀목 추천
가스 이탈 추천
판단 근거 UI
템플릿 대사
대사 쿨다운
대사 우선순위
제공 결과
DroneAnalysisService
DroneRecommendation
TemplateDialogueGenerator
DroneDialoguePanel
DroneReasonPanel

완료 조건
동일 상황에서 일관된 추천
중요한 경고가 일반 대사보다 우선
대사가 과도하게 반복되지 않음
추천 근거가 수치와 일치
생성형 AI 없이도 전체 기능 동작

9.7 7단계: 로컬 세이브
담당자 A
작업
IWorldSnapshotProvider 구현
저장 대상:
채굴된 타일
변경된 타일
붕괴 결과
설치 시설
가스 상태
전진기지
월드 Seed
제공 결과
CaptureWorldSnapshot
RestoreWorldSnapshot
WorldSaveData 변환
월드 복원 테스트

완료 조건
월드 전체가 아닌 변경점만 제공
저장 후 채굴한 타일 유지
건물 위치와 상태 복원
존재하지 않는 ID 처리 가능

담당자 B
작업
GameSaveData
PlayerSaveData
ProgressSaveData
DroneSaveData
JSON 저장
임시 파일
백업
자동 저장
세이브 버전
저장 슬롯
이어하기 메뉴
제공 결과
SaveService
LoadService
Save migration
SaveSlotPanel
Open Save Folder editor tool
Clear Save editor tool

완료 조건
정상 저장·로드
저장 중 강제 종료에 대비
정상 파일 실패 시 백업 로드
게임 버전과 세이브 버전 기록
저장 데이터가 없으면 새 게임 생성

9.8 8단계: 데모 통합
담당자 A
작업
천층 폐광 타일 구조
중층 가스 구역
안전 경로
위험 리튬 경로
붕괴 구간
전진기지 설치 공간
심층 진입 잠금 지점
A는 통합 Scene을 직접 수정하지 않고 다음을 전달한다.
완성된 Tilemap Prefab 또는 Tilemap 데이터
월드 생성 Editor Tool
배치 좌표 문서
필요한 Runtime Prefab
완료 조건
핵심 플레이 동선 완주 가능
막히는 구간 없음
위험과 보상의 차이가 명확함

담당자 B
작업
Mine_Demo_Integration.unity 구성
HUD 배치
튜토리얼 메시지
지상 기지
정산·업그레이드
심층 예고
시작과 종료 연출
사운드 기본 연결
메뉴 연결
완료 조건
다음 데모가 완주된다.
탐사 시작
→ 구리·철 채굴
→ 위험한 리튬 경로 선택
→ 구조 균열
→ 버팀목 설치
→ 가스 발생
→ 전진기지 설치
→ 드론 귀환 추천
→ 정산
→ 업그레이드
→ 심층 신호 확인

10. 담당자 A 상세 책임
    담당자 A는 다음 기준으로 작업한다.
    A-1. 기능은 독립 Test Scene에서 완성
    Main Demo Scene에서 바로 기능을 개발하지 않는다.
    기능 개발
    → Gameplay Test Scene 검증
    → Prefab화
    → 사용 방법 작성
    → PR
    → B가 Integration Scene에 배치

A-2. B의 구체 클래스를 참조하지 않음
금지:
InventoryManager.Instance.AddCopper(1);

허용:
rewardReceiver.AddMineral("mineral.copper", 1);

A-3. Inspector 참조를 최소화
가능하면 자동 탐색 또는 명확한 Installer를 둔다.
참조가 필요한 경우 다음을 PR에 작성한다.
필수 Inspector 참조:

- ForegroundTilemap
- PlayerEnergyProvider
- GameplayEventSink

A-4. 월드 성능 책임
전체 Tilemap 매 프레임 탐색 금지
구조 계산은 변경 주변만 실행
시설 연결은 변경 시 재계산
가스 판정 범위 제한
불필요한 Instantiate 반복 방지
A-5. Gameplay 테스트 책임
채굴 보상 중복 방지
타일 좌표 예외 처리
붕괴 재현성
건설 겹침 방지
전력망 연결 갱신
월드 스냅샷 복원

11. 담당자 B 상세 책임
    B-1. 통합 Scene 단독 관리
    B는 Integration Scene의 유일한 수정자다.
    A의 Prefab을 배치한 뒤 필요한 참조를 연결한다.
    B-2. 상태와 UI를 분리
    UI가 직접 게임 상태를 수정하지 않는다.
    UI 버튼
    → Service 요청
    → State 변경
    → 이벤트 발생
    → UI 갱신

B-3. ScriptableObject 에셋 관리
B는 다음 데이터 에셋의 값을 관리한다.
광물
시설
레시피
업그레이드
계약
드론 대사
UI 표시 정보
A가 필요한 데이터 필드를 요청하면 Shared 계약을 검토한 뒤 반영한다.
B-4. 저장 호환성 책임
내부 ID 변경 주의
saveVersion 관리
DTO에 GameObject 참조 금지
Unity Object 직접 직렬화 금지
저장 실패 처리
백업 파일 복원
B-5. 통합 테스트 책임
새 게임
이어하기
Scene 이동
HUD 연결
판매
제작
자동 저장
게임 종료
Windows 빌드
다른 PC 실행

12. 병합 규칙
    12.1 브랜치 구조
    main
    └─ develop
    ├─ feature/a-player-movement
    ├─ feature/a-mining-system
    ├─ feature/a-structural-system
    ├─ feature/b-inventory
    ├─ feature/b-drone-analysis
    └─ feature/b-save-system

A와 B 식별자를 브랜치 이름에 붙여도 좋다.
12.2 PR 크기
하나의 PR에는 하나의 주요 목적만 넣는다.
좋은 예:
feat: add tile mining system

나쁜 예:
feat: mining, inventory, UI, save and scene updates

12.3 병합 순서
두 기능이 연결될 경우 다음 순서로 병합한다.

1. Shared 인터페이스
2. 담당자 A의 Producer 구현
3. 담당자 B의 Consumer 구현
4. B의 Integration Scene 연결
5. 통합 테스트

예:

1. IMiningRewardReceiver 병합
2. A의 MiningSystem 병합
3. B의 InventoryService 병합
4. B가 Scene 연결

   12.4 장기 브랜치 금지
   기능 브랜치를 일주일 이상 방치하지 않는다.
   작업이 길어지면 더 작은 단위로 나눈다.
   12.5 Pull 전 확인
   작업 시작 전:
   git checkout develop
   git pull
   git checkout feature/...

PR 생성 전 develop의 최신 변경을 반영한다.

13. Unity 파일 충돌 방지 규칙
    수정 금지 대상
    합의 없이 다음 파일을 수정하지 않는다.
    ProjectSettings/\*
    Packages/manifest.json
    Packages/packages-lock.json
    공용 Integration Scene
    공용 Prefab
    Shared Contracts

파일 이동 및 이름 변경
Unity 파일은 .meta와 함께 이동해야 한다.
파일 탐색기에서 옮기기보다 Unity Editor 안에서 이동한다.
금지:
Sprite 파일만 이동
.meta 파일 누락

이 경우 GUID가 바뀌어 Prefab과 Scene 참조가 끊길 수 있다.
공용 Scene 수정이 필요한 경우
A는 Scene 파일을 직접 수정하지 않고 다음 형식으로 요청한다.
[Integration 요청]

대상 Scene:
Mine_Demo_Integration

추가 대상:
SupportBeam_Runtime.prefab

부모 오브젝트:
GameplayRoot/Buildings

필수 참조:
ForegroundTilemap
StructuralIntegritySystem

검증 방법:
버팀목 설치 후 안정도 20 이상 증가

14. 일일 작업 흐름
    작업 시작
    GitHub Issue 확인
    담당 파일 확인
    develop 최신화
    기능 브랜치 생성
    작업 범위 재확인
    개발 중
    독립 Test Scene에서 작업
    Console 오류 즉시 처리
    기능 단위 커밋
    공용 파일 수정 필요 시 작업 중단 후 협의
    Codex 결과를 직접 검토
    작업 종료
    테스트 실행
    변경 파일 확인
    불필요한 Scene 변경 확인
    .meta 누락 확인
    PR 작성
    통합 방법 기록

15. PR 작성 형식

## 작업 목적

타일 채굴 완료 시 광물 보상 이벤트를 전달합니다.

## 변경 내용

- MiningSystem 추가
- MiningTileData 연결
- 채굴 진행도 처리
- IMiningRewardReceiver 호출
- 중복 보상 방지 테스트 추가

## 수정한 주요 파일

- Scripts/Gameplay/Mining/MiningSystem.cs
- Scripts/Gameplay/Mining/MiningTileResolver.cs
- Tests/EditMode/Gameplay/MiningSystemTests.cs

## Integration 방법

Player.prefab의 MiningRewardReceiver 필드에
InventoryService를 연결합니다.

## 테스트 결과

- 일반 암석 채굴 성공
- 구리 보상 1회 발생
- 전력 부족 시 채굴 불가
- 범위 밖 Tile 좌표 예외 없음

## 주의사항

ForegroundTilemap 참조가 필요합니다.

16. Codex 사용 규칙
    A와 B 모두 Codex를 사용할 수 있지만 같은 범위를 동시에 작업시키면 안 된다.
    담당자 A용 요청 예시
    Assets/\_Project/Scripts/Gameplay/Mining 영역만 수정하라.

Shared 계약은 변경하지 말고,
현재 IMiningRewardReceiver 인터페이스를 사용해
타일 채굴 보상을 전달하라.

Integration Scene과 App 폴더는 수정하지 마라.

담당자 B용 요청 예시
Assets/\_Project/Scripts/App/Inventory 영역만 수정하라.

Gameplay 폴더는 수정하지 말고,
IMiningRewardReceiver를 구현해 광물 수량과 중량을 관리하라.

Integration Scene 수정은 별도 단계에서 진행한다.

금지 요청
프로젝트 전체를 확인하고 필요한 파일은 전부 알아서 수정해라.

이 요청은 다른 담당자의 소유 영역까지 변경할 위험이 있다.

17. 통합 검증 체크포인트
    체크포인트 1
    이동

- 기본 HUD

체크포인트 2
채굴

- 인벤토리
- 전력 소비

체크포인트 3
구조 안정도

- 버팀목 비용
- 위험 HUD

체크포인트 4
가스

- 건설 메뉴
- 시설 배치

체크포인트 5
전진기지

- 전력망
- 정산

체크포인트 6
드론 Context

- 판단
- 대사

체크포인트 7
월드 스냅샷

- 로컬 저장
- 이어하기

각 체크포인트가 통합된 뒤 다음 단계로 넘어간다.
개별 시스템은 완성됐지만 통합이 안 된 상태에서 다음 기능을 계속 쌓지 않는다.

18. 권장 작업 일정 예시
    정확한 기간보다 단계 완료를 우선하되 다음과 같이 운영할 수 있다.
    Sprint 0
    프로젝트 설정
    Git
    폴더
    Shared 계약
    기본 Scene
    Sprint 1
    A
    플레이어
    카메라
    Tilemap
    B
    Bootstrap
    State
    기본 HUD
    데이터 카탈로그
    Sprint 2
    A
    채굴
    B
    인벤토리
    광물 데이터
    Sprint 3
    A
    구조 안정도
    붕괴
    B
    경제
    제작
    구조 UI
    Sprint 4
    A
    가스
    건설 배치
    B
    건설 메뉴
    레시피
    가스 UI
    Sprint 5
    A
    전력망
    전진기지
    B
    정산
    업그레이드
    기지 UI
    Sprint 6
    A
    드론 이동
    센서
    Context
    B
    드론 판단
    대사
    분석 UI
    Sprint 7
    A
    월드 스냅샷
    B
    로컬 세이브
    메뉴
    이어하기
    Sprint 8
    A
    데모 월드와 밸런스
    B
    통합 Scene
    튜토리얼
    빌드와 배포
    Final Sprint
    공동 QA
    버그 수정
    성능 확인
    시연 리허설
    Windows Release 빌드

19. 작업 중 충돌이 예상되는 영역
    광물 데이터
    A는 광물 데이터를 읽기만 한다.
    B가 ScriptableObject 에셋 값을 관리한다.
    A가 필드 추가를 원하면 직접 수정하지 않고 요청한다.
    건물 Prefab
    A가 Runtime Prefab을 소유한다.
    B는 Runtime Prefab을 직접 수정하지 않고 UI용 아이콘과 BuildingData를 연결한다.
    드론
    A는 이동·센서·Context를 담당한다.
    B는 판단·대사·UI를 담당한다.
    저장
    A는 월드 스냅샷을 만든다.
    B는 파일 저장과 로드를 담당한다.
    Mine Demo Scene
    B만 수정한다.
    A는 Prefab과 설치 지침만 제공한다.

20. 최종 책임 구분
    담당자 A 최종 완료 기준
    [ ] 플레이어가 안정적으로 이동한다.
    [ ] Tilemap을 채굴할 수 있다.
    [ ] 광물이 정확히 구분된다.
    [ ] 구조 안정도가 계산된다.
    [ ] 버팀목이 안정도에 영향을 준다.
    [ ] 부분 붕괴가 발생한다.
    [ ] 가스 위험이 작동한다.
    [ ] 시설을 Grid에 설치할 수 있다.
    [ ] 전력망이 작동한다.
    [ ] 전진기지 Runtime 기능이 작동한다.
    [ ] 드론이 플레이어를 추적한다.
    [ ] 드론 Context가 정확하다.
    [ ] 월드 변경점을 저장 형태로 제공한다.

담당자 B 최종 완료 기준
[ ] 게임 상태가 초기화된다.
[ ] 인벤토리와 중량이 작동한다.
[ ] 광물 판매가 가능하다.
[ ] 제작 비용이 정확히 차감된다.
[ ] 업그레이드가 적용된다.
[ ] HUD가 게임 상태와 일치한다.
[ ] 드론이 적절한 행동을 추천한다.
[ ] 드론 대사와 판단 근거가 표시된다.
[ ] 로컬 저장과 이어하기가 가능하다.
[ ] 메뉴와 Scene 이동이 작동한다.
[ ] 통합 데모가 완주된다.
[ ] Windows 빌드를 생성하고 배포할 수 있다.

21. 공동 최종 QA
    다음 항목은 두 사람이 함께 확인한다.
    [ ] 새 게임 시작
    [ ] 이어하기
    [ ] 이동
    [ ] 채굴
    [ ] 인벤토리 증가
    [ ] 화물 중량
    [ ] 구조 위험
    [ ] 버팀목 설치
    [ ] 가스 노출
    [ ] 조명 설치
    [ ] 충전
    [ ] 전진기지 설치
    [ ] 정산
    [ ] 드론 추천
    [ ] 구조 실패
    [ ] 자동 저장
    [ ] 게임 재실행
    [ ] 월드 복원
    [ ] 해상도 변경
    [ ] Windows 빌드
    [ ] 다른 PC에서 실행

22. 최종 권장 분담
    담당자 A
    지하에서 일어나는 모든 물리적 사건과 상호작용을 책임진다.
    플레이어
    채굴
    Tilemap
    구조
    붕괴
    가스
    건설 배치
    전력망
    전진기지 Runtime
    드론 이동·센서
    월드 스냅샷

담당자 B
게임 전체의 상태와 사용자 경험, 저장 및 배포를 책임진다.
데이터 카탈로그
인벤토리
경제
제작
업그레이드
HUD
메뉴
드론 판단·대사
로컬 세이브
통합 Scene
테스트 통합
빌드
배포

이렇게 나누면 A와 B는 Shared Contracts를 통해서만 연결되고, 같은 Scene·Prefab·스크립트를 동시에 편집하는 상황을 상당 부분 방지할 수 있다.
최종적으로 병합 구조는 다음과 같다.
담당자 A의 Runtime Prefab과 Gameplay 시스템
↓
Shared Contracts
↓
담당자 B의 State·UI·Save·AI 시스템
↓
B가 관리하는 Integration Scene
↓
공동 QA 및 Release Build
