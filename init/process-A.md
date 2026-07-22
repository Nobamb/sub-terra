Project Sub-Terra
담당자 A 전용 개발 작업지시서

1. 담당자 A의 역할
   역할명
   Core Gameplay & World Simulation 담당
   핵심 책임
   담당자 A는 지하 탐사 화면에서 실제로 일어나는 모든 물리적 사건과 상호작용을 구현한다.
   플레이어 조작
   → Tilemap 탐사
   → 채굴
   → 구조 안정도 변화
   → 붕괴
   → 가스 위험
   → 시설 배치
   → 전력망 작동
   → 드론 이동·환경 감지
   → 월드 상태 저장용 스냅샷 제공

A는 인벤토리, 경제, UI, 메뉴, 로컬 파일 저장을 직접 구현하지 않는다.
A가 만든 시스템은 인터페이스와 이벤트를 통해 담당자 B의 시스템에 결과를 전달한다.

2. 담당자 A의 최종 목표
   담당자 A의 작업이 완료되면 다음이 가능해야 한다.
   플레이어가 지하 공간을 이동한다.
   플레이어가 암석과 광물을 채굴한다.
   채굴된 타일이 Tilemap에서 제거된다.
   채굴 방식에 따라 구조 안정도가 달라진다.
   위험한 공간에서 균열과 붕괴가 발생한다.
   버팀목을 설치하면 구조가 안정된다.
   가스 타일을 건드리면 위험 구역이 활성화된다.
   조명, 충전기, 기지 코어를 그리드에 설치할 수 있다.
   연결된 시설만 전력을 공급받는다.
   드론이 플레이어를 따라다니며 환경 정보를 수집한다.
   변경된 월드 상태를 저장 가능한 데이터로 변환한다.

3. 담당자 A의 소유 영역
   3.1 소유 폴더
   담당자 A는 다음 폴더를 주로 수정한다.
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

3.2 소유 Scene
Gameplay_Player_Test.unity
Gameplay_Mining_Test.unity
Gameplay_Structural_Test.unity
Gameplay_Hazard_Test.unity
Gameplay_Building_Test.unity
Gameplay_Power_Test.unity
Gameplay_Drone_Test.unity
Gameplay_WorldSave_Test.unity

3.3 소유 Prefab
Player.prefab
MiningCursor.prefab
SupportBeam_Runtime.prefab
Light_Runtime.prefab
Charger_Runtime.prefab
Storage_Runtime.prefab
SettlementConsole_Runtime.prefab
OutpostCore_Runtime.prefab
PowerCable_Runtime.prefab
GasZone.prefab
DiggerBot_Runtime.prefab

4. 담당자 A가 직접 수정하지 않는 영역
   다음 영역은 담당자 B 소유이므로 직접 수정하지 않는다.
   Assets/\_Project/Scripts/App/
   Assets/\_Project/Data/
   Assets/\_Project/Prefabs/UI/
   Assets/\_Project/Scenes/Integration/
   Assets/\_Project/Scenes/MainMenu/
   Assets/\_Project/Scenes/SurfaceBase/
   Assets/\_Project/Scenes/Bootstrap/

특히 다음 파일은 수정하지 않는다.
Mine_Demo_Integration.unity
HUD.prefab
InventoryPanel.prefab
BuildingMenu.prefab
SaveSlotPanel.prefab
GameSaveData 관련 파일

공용 파일도 사전 합의 없이 수정하지 않는다.
Assets/\_Project/Scripts/Shared/
ProjectSettings/\*
Packages/manifest.json
Packages/packages-lock.json

Shared 계약 수정이 필요하면 먼저 Issue를 작성한다.

5. 담당자 A의 개발 원칙
   5.1 독립 Test Scene 우선
   기능은 최종 데모 Scene에서 개발하지 않는다.
   독립 Test Scene에서 구현
   → 기능 검증
   → Prefab으로 완성
   → 테스트 작성
   → 통합 방법 문서화
   → 담당자 B에게 전달

5.2 다른 시스템의 구체 클래스를 직접 참조하지 않기
금지 예시:
InventoryManager.Instance.AddCopper(1);

허용 예시:
rewardReceiver.AddMineral("mineral.copper", 1);

A는 인벤토리 구현 방식이나 UI 구조를 몰라도 작동해야 한다.
5.3 시각 요소와 로직 분리
모든 Runtime Prefab에 VisualRoot를 둔다.
SupportBeam_Runtime.prefab
├─ BuildingInstance
├─ StructuralSupport
├─ Collider2D
└─ VisualRoot
└─ PlaceholderRectangle

정식 이미지가 추가되어도 VisualRoot 내부만 교체할 수 있어야 한다.
5.4 결정론적 로직
동일한 입력 상태에서는 같은 결과가 나오도록 구현한다.
특히 다음은 언어 모델이나 무작위 문장 생성에 맡기지 않는다.
붕괴 여부
피해량
구조 안정도
가스 노출
전력 연결
시설 설치 가능 여부
귀환 거리

6. 작업 시작 전 공동 계약 확인
   담당자 A는 작업을 시작하기 전에 다음 인터페이스가 존재하는지 확인한다.
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

인터페이스가 부족하거나 수정이 필요하면 구현을 강제로 진행하지 않고 Shared 계약 변경 Issue를 먼저 작성한다.

7. 단계별 개발 절차

A-0. 개발 환경 확인
작업
지정된 Unity 6 LTS 버전으로 프로젝트를 연다.
Console 오류 여부를 확인한다.
develop 브랜치를 최신화한다.
새로운 기능 브랜치를 만든다.
A 소유 폴더만 수정하도록 Codex 작업 범위를 제한한다.
Gameplay Test Scene이 정상적으로 열리는지 확인한다.
브랜치 예시
feature/a-player-movement
feature/a-mining-system
feature/a-structural-integrity
feature/a-gas-hazard
feature/a-building-placement
feature/a-power-network
feature/a-drone-runtime
feature/a-world-snapshot

완료 조건
[ ] Unity 버전이 팀 기준과 같다.
[ ] Console 초기 오류가 없다.
[ ] develop 최신 변경이 반영됐다.
[ ] 다른 담당자 소유 파일이 변경되지 않았다.

A-1. 플레이어 이동 시스템
목표
플레이어가 Tilemap 지형 위에서 안정적으로 이동하도록 만든다.
구현 항목
Input System 기반 좌우 이동
Rigidbody2D
Collider2D
지면 판정
점프 또는 수직 이동 방식
플레이어 방향 전환
카메라 추적
이동 속도 변경용 외부 입력 지점
Player.prefab
Gameplay_Player_Test.unity
권장 컴포넌트
Player
├─ PlayerController
├─ PlayerMovement
├─ PlayerInteractionOrigin
├─ Rigidbody2D
├─ CapsuleCollider2D
└─ VisualRoot

외부에 제공할 기능
현재 위치
바라보는 방향
이동 가능 여부
화물 중량에 따른 속도 배율 적용 지점
가스 노출에 따른 속도 배율 적용 지점
완료 조건
[ ] 좌우 이동이 가능하다.
[ ] 벽과 바닥을 통과하지 않는다.
[ ] 입력을 놓으면 정상적으로 감속 또는 정지한다.
[ ] 방향 전환이 정상적으로 표시된다.
[ ] 다른 시스템이 이동 속도 배율을 전달할 수 있다.
[ ] Test Scene에서 단독으로 실행된다.

인수인계 항목
Player.prefab
필요한 Input Action 이름
Prefab의 필수 Inspector 참조
통합 Scene 기본 배치 좌표
카메라 연결 방법

A-2. Tilemap 및 채굴 시스템
목표
플레이어가 바라보는 방향의 타일을 채굴하고 광물 보상을 외부 시스템에 전달한다.
구현 항목
Foreground Tilemap 참조
Background Tilemap 구분
Grid 좌표 계산
MiningTileData 조회
채굴 가능 여부 확인
채굴 시간
드릴 등급 검사 지점
전력 소비 요청
채굴 진행도
채굴 취소
타일 제거
보상 이벤트
특수 타일 이벤트
Gameplay_Mining_Test.unity
처리 순서
채굴 입력
→ 플레이어 앞 좌표 계산
→ Tile 데이터 조회
→ 채굴 가능 여부 확인
→ 드릴 및 전력 조건 확인
→ 채굴 시간 누적
→ 완료 시 타일 제거
→ 광물 보상 전달
→ 구조 재계산 이벤트
→ 가스·희귀 이벤트 검사

반드시 방지할 오류
입력 한 번에 보상이 여러 번 지급되는 문제
제거된 타일을 다시 채굴하는 문제
Tilemap 범위를 벗어날 때 예외가 발생하는 문제
전력이 부족한데 채굴이 진행되는 문제
채굴 취소 후 진행도가 비정상적으로 남는 문제
완료 조건
[ ] 일반 암석과 광물이 구분된다.
[ ] 구리·철·리튬의 채굴 시간이 다르다.
[ ] 채굴 완료 시 타일이 한 번만 제거된다.
[ ] 광물 보상이 한 번만 전달된다.
[ ] 전력 부족 시 채굴이 시작되지 않는다.
[ ] 제거된 타일은 월드 변경점에 기록된다.

B에게 전달할 데이터
mineralId
quantity
tilePosition
depth
structuralImpact
specialEventType

A-3. 구조 안정도 및 붕괴
목표
채굴한 공간의 형태와 버팀목 배치에 따라 구조 위험이 달라지게 만든다.
구현 항목
타일 주변 지지 수 계산
상부 암석 존재 여부
빈 공간 크기 반영
균열 타일 가중치
광물별 구조 영향
버팀목 영향 범위
구조 안정도 수치
위험 단계 변환
부분 붕괴
낙석 타일 생성
구조 이벤트 발행
Gameplay_Structural_Test.unity
MVP 위험 단계
70~100: 안정
40~69: 주의
20~39: 위험
0~19: 붕괴 임박

성능 원칙
전체 Tilemap을 매 프레임 검사하지 않는다.
다음 이벤트 발생 위치 주변만 재계산한다.
타일 제거
버팀목 설치
버팀목 제거
붕괴 발생
특수 구조 타일 변경
붕괴 처리 원칙
전체 지형 물리 시뮬레이션을 하지 않는다.
위험 구역의 지정 타일만 암석 또는 낙석으로 변경한다.
월드 Seed 또는 고정 Random Seed를 사용해 재현 가능하게 만든다.
붕괴 결과를 월드 변경점에 남긴다.
완료 조건
[ ] 넓은 공간을 파면 위험도가 높아진다.
[ ] 버팀목 설치 시 위험도가 낮아진다.
[ ] 위험 단계 변경 이벤트가 발생한다.
[ ] 붕괴 결과가 항상 규칙에 따라 발생한다.
[ ] 변경 주변만 재계산한다.
[ ] 붕괴 결과를 저장할 수 있다.

A-4. 가스 위험 시스템
목표
특정 타일 채굴 시 가스 위험 구역을 활성화하고 플레이어의 노출 상태를 전달한다.
구현 항목
가스 타일 식별
가스 주머니 활성화
원형 또는 Tile 범위 위험 구역
플레이어 진입·이탈 판정
가스 강도
남은 지속 시간
중화 여부
가스 이벤트
GasZone.prefab
Gameplay_Hazard_Test.unity
외부로 전달할 상태
isExposed
gasRisk
gasType
gasZoneId
remainingDuration

가스 효과 적용 방식
A는 실제 노출 여부와 강도를 계산한다.
B 또는 공용 상태 시스템이 다음 효과를 반영할 수 있도록 이벤트를 전달한다.
전력 지속 감소
가스 경고 UI
드론 위험 분석
이동 속도 감소
피해 처리
완료 조건
[ ] 가스 타일 채굴 시 위험 구역이 생성된다.
[ ] 플레이어 진입과 이탈이 감지된다.
[ ] 활성·중화 상태를 저장할 수 있다.
[ ] 여러 가스 구역이 있어도 ID가 겹치지 않는다.
[ ] 범위 밖 검사로 과도한 연산이 발생하지 않는다.

A-5. 건설 배치 시스템
목표
플레이어가 파낸 빈 공간에 시설을 그리드 방식으로 설치할 수 있게 한다.
구현 항목
마우스 좌표를 Grid 좌표로 변환
건설 Preview
설치 가능·불가능 색상
빈 공간 확인
타일 충돌 확인
기존 건물 중복 확인
시설 크기 확인
바닥 또는 벽 조건 확인
비용 확인 요청
설치 성공 후 자원 차감 요청
Runtime Prefab 생성
건물 설치 이벤트
Gameplay_Building_Test.unity
설치 처리 순서
건설 항목 선택
→ Preview 표시
→ 위치 유효성 검사
→ IResourceWallet.CanAfford 확인
→ 설치 확정 입력
→ Runtime Prefab 생성 시도
→ 생성 성공
→ IResourceWallet.TrySpend 호출
→ BuildingPlaced 이벤트 발행

자원 차감은 설치 성공 이후 확정한다.
건설 실패 사례
공간 부족
다른 건물과 겹침
기반 타일 없음
허용되지 않은 구역
자원 부족
Runtime Prefab 누락
유효하지 않은 Building ID
완료 조건
[ ] 건설 Preview가 Grid에 맞춰 움직인다.
[ ] 설치 가능 여부가 시각적으로 구분된다.
[ ] 건물이 서로 겹치지 않는다.
[ ] 설치 실패 시 자원이 차감되지 않는다.
[ ] 성공한 건물에 고유 instanceId가 생성된다.
[ ] 설치 결과가 월드 변경점에 기록된다.

A-6. 전력망과 전진기지 Runtime
목표
전진기지 코어와 케이블에 연결된 시설만 작동하게 한다.
구현 항목
Outpost Core
Power Cable
시설 연결 탐색
공급량
소비량
활성·비활성 판정
조명 작동
충전기 상호작용 지점
정산 콘솔 상호작용 지점
보관함 상호작용 지점
체크포인트 위치
전력망 디버그 표시
Gameplay_Power_Test.unity
재계산 시점
코어 설치·제거
케이블 설치·제거
시설 설치·제거
공급량 변경
시설 소비량 변경

매 프레임 전체 네트워크를 재계산하지 않는다.
A가 담당하는 부분
시설 연결 여부
전력 공급 상태
시설의 물리적 활성화
Light 2D On/Off
충전기와 콘솔의 상호작용 가능 상태
B가 담당하는 부분
전력 수치 UI
충전 후 플레이어 State 변경
정산 UI
체크포인트 상태
자동 저장 요청
완료 조건
[ ] 코어에 연결되지 않은 시설은 비활성화된다.
[ ] 케이블 연결 시 시설 상태가 갱신된다.
[ ] 시설 제거 시 네트워크가 다시 계산된다.
[ ] 조명과 충전기가 연결 여부에 따라 작동한다.
[ ] 공급량보다 소비량이 많을 때 우선순위 처리가 가능하다.

A-7. 드론 Runtime, 이동 및 센서
목표
드론이 플레이어를 따라다니며 게임 월드 정보를 수집해 B의 분석 시스템에 제공한다.
구현 항목
플레이어 추적
부드러운 이동
플레이어와 겹침 방지
벽 관통 방지 또는 단순 우회
주변 광물 탐색
구조 안정도 수집
가스 위험 수집
기지 거리 계산
현재 깊이 계산
귀환 경로 거리 추정
DroneContextDto 생성
DiggerBot_Runtime.prefab
Gameplay_Drone_Test.unity
드론 Context 예시
depth
currentEnergy
returnEnergyEstimate
structuralIntegrity
gasRisk
unsettledCargoValue
cargoWeight
nearestBaseDistance
nearbyMineralIds
returnPathAvailable

A는 Context의 실제 환경값을 제공한다.
추천 행동과 대사 생성은 B가 담당한다.
완료 조건
[ ] 드론이 플레이어를 안정적으로 따라간다.
[ ] 플레이어와 지나치게 겹치지 않는다.
[ ] 잘못된 좌표에서도 예외가 발생하지 않는다.
[ ] 주변 광물 정보가 실제 Tilemap과 일치한다.
[ ] 구조·가스·거리 정보가 실제 상태와 일치한다.
[ ] Context 생성이 매 프레임 과도하게 호출되지 않는다.

A-8. 월드 스냅샷과 복원
목표
B의 SaveService가 저장할 수 있도록 월드 변경 상태를 DTO로 제공한다.
저장 대상
월드 Seed
채굴된 타일
붕괴로 변경된 타일
설치 건물
건물 내구도
가스 활성·중화 상태
전진기지
발견한 Chunk 또는 구역
영구 구조 손상
저장하지 않을 대상
GameObject 자체
MonoBehaviour 참조
TileBase Unity Object
Collider
Sprite
Light 2D
현재 전력망 계산 결과
isPowered와 같은 계산 가능한 값은 복원 후 다시 계산한다.
구현 항목
WorldSaveData CaptureWorldSnapshot();
void RestoreWorldSnapshot(WorldSaveData saveData);

복원 순서
기본 Tilemap 또는 Seed 월드 로드
→ 채굴된 타일 제거
→ 변경 타일 적용
→ 건물 Runtime Prefab 복원
→ 가스 상태 복원
→ 구조 안정도 재계산
→ 전력망 재계산

완료 조건
[ ] 채굴한 타일이 복원 후 제거된 상태로 남는다.
[ ] 설치 시설의 ID와 위치가 복원된다.
[ ] 붕괴 결과가 복원된다.
[ ] 가스 상태가 복원된다.
[ ] 누락된 ID가 있어도 게임 전체가 중단되지 않는다.
[ ] 월드 전체가 아닌 변경점만 저장한다.

A-9. 데모 월드 제작
목표
MVP의 전체 핵심 루프를 경험할 수 있는 지하 월드를 제공한다.
필수 구역
시작 엘리베이터 구역
구리 학습 구역
철 채굴 구역
안전 경로
위험한 리튬 경로
구조 균열 발생 구역
버팀목 설치 공간
가스 주머니 구역
전진기지 설치 공간
심층 잠금 구역
제작 방식
A는 최종 Integration Scene을 직접 수정하지 않는다.
다음 중 하나로 전달한다.
Tilemap Prefab
월드 생성 Editor Tool
좌표 데이터
Scene Additive용 월드 Scene
배치 지침서
완료 조건
[ ] 데모 동선을 처음부터 끝까지 이동할 수 있다.
[ ] 안전 경로와 위험 경로의 차이가 명확하다.
[ ] 리튬을 얻기 위해 위험을 감수할 이유가 있다.
[ ] 버팀목과 전진기지가 실제로 필요하다.
[ ] 플레이가 막히는 지형이 없다.

8. 담당자 A의 테스트 책임
   Edit Mode 테스트
   Grid 좌표 변환
   채굴 가능 여부
   광물 보상 중복 방지
   구조 안정도 점수
   버팀목 영향 계산
   건설 충돌 검사
   전력 연결 탐색
   월드 DTO 변환
   Play Mode 테스트
   플레이어 이동
   Tilemap 채굴
   가스 구역 진입·이탈
   건물 배치
   조명 활성화
   충전기 상호작용
   드론 추적
   월드 복원
   매 PR 전 수동 확인
   [ ] Console Error 없음
   [ ] Test Scene 단독 실행
   [ ] 다른 담당자 폴더 미수정
   [ ] Prefab 참조 누락 없음
   [ ] .meta 파일 누락 없음
   [ ] Integration 방법 문서화

9. 담당자 A의 Git 작업 절차
   작업 시작
   git checkout develop
   git pull
   git checkout -b feature/a-기능명

작업 중
하나의 브랜치에는 하나의 주요 기능만 넣는다.
Integration Scene을 열어 저장하지 않는다.
공용 파일이 자동 변경되었는지 수시로 확인한다.
기능 단위로 커밋한다.
커밋 예시
feat: add player grid mining
feat: add structural support calculation
fix: prevent duplicate mineral rewards
test: add building overlap tests
refactor: isolate gas zone detection

PR 전
Unity 테스트 실행
Console 오류 확인
변경 파일 확인
develop 최신 변경 반영
충돌 확인
통합 지침 작성
PR 생성

10. 담당자 A의 인수인계 형식
    [A → B Integration 요청]

기능명:
구조 안정도 및 버팀목

추가 Prefab:
SupportBeam_Runtime.prefab

필수 부모:
GameplayRoot/Buildings

필수 참조:

- ForegroundTilemap
- StructuralIntegritySystem
- GameplayEventSink

발행 이벤트:

- StructuralRiskChanged
- BuildingPlaced
- CollapseTriggered

통합 검증:

1. 넓은 공간을 채굴한다.
2. 안정도가 위험 단계로 내려가는지 확인한다.
3. 버팀목을 설치한다.
4. 안정도가 20 이상 증가하는지 확인한다.

주의사항:
Integration Scene은 수정하지 않았음.

11. 담당자 A용 Codex 작업 규칙
    권장 프롬프트
    Project Sub-Terra Unity 프로젝트다.

Assets/\_Project/Scripts/Gameplay/Mining,
Assets/\_Project/Tests/EditMode/Gameplay/Mining 범위만 수정하라.

현재 Shared 계약인 IMiningRewardReceiver를 변경하지 말고 사용하라.
App, Data, UI, Save, Integration Scene은 수정하지 마라.

구현 후:

1. 변경 파일 목록
2. 설계 이유
3. 테스트 방법
4. 필요한 Inspector 참조
5. B 담당자의 Integration 방법
   을 정리하라.

금지 프롬프트
프로젝트 전체를 알아서 수정해 채굴과 인벤토리를 완성해라.

A의 Codex가 B의 인벤토리와 UI를 직접 수정하게 해서는 안 된다.

12. 담당자 A 최종 완료 체크리스트
    [ ] 플레이어 이동이 안정적이다.
    [ ] Tilemap 채굴이 작동한다.
    [ ] 광물 보상이 인터페이스로 전달된다.
    [ ] 구조 안정도가 계산된다.
    [ ] 버팀목이 구조에 영향을 준다.
    [ ] 부분 붕괴가 작동한다.
    [ ] 가스 위험이 작동한다.
    [ ] 시설을 Grid에 설치할 수 있다.
    [ ] 전력망이 작동한다.
    [ ] 전진기지 Runtime 기능이 작동한다.
    [ ] 드론이 플레이어를 추적한다.
    [ ] 드론 Context가 정확하다.
    [ ] 월드 변경점을 DTO로 제공한다.
    [ ] 각 기능이 독립 Test Scene에서 실행된다.
    [ ] Integration Scene을 직접 수정하지 않았다.
    [ ] 담당자 B에게 통합 지침을 전달했다.

13. 담당자 A의 최종 인도물
    Player.prefab
    DiggerBot_Runtime.prefab
    Runtime Building Prefabs
    Gameplay Test Scenes
    MiningSystem
    StructuralIntegritySystem
    HazardSystem
    BuildingPlacementSystem
    PowerNetworkSystem
    DroneContextProvider
    WorldSnapshotProvider
    Gameplay EditMode Tests
    Gameplay PlayMode Tests
    Demo World Data 또는 생성 도구
    Integration 설치 지침

담당자 A의 개발 완료 기준은 기능이 코드로 존재하는 것이 아니라, 담당자 B가 제공받은 Prefab과 지침만으로 Integration Scene에 안정적으로 연결할 수 있는 상태가 되는 것이다.
