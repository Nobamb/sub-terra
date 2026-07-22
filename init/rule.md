Agent 작업 공통 규칙 문서

Part A. Project Sub-Terra Agent 작업 규칙

1. 규칙 우선순위
   Agent는 작업 중 판단이 필요한 상황에서 아래 우선순위를 따른다.
   1-1. 저장소, 비밀정보와 Git 히스토리 보호
   1-2. Unity 프로젝트와 세이브 데이터 안정성
   1-3. 담당자별 파일·Scene·Prefab 소유권
   1-4. 결정론적 게임 규칙과 핵심 플레이 흐름
   1-5. 테스트와 실제 플레이 검증
   1-6. 성능과 빌드 안정성
   1-7. 기능 구현
   1-8. 코드 스타일과 편의성 개선

기능 구현이 가능하더라도 저장소 보호, 세이브 호환성, 소유권 또는 결정론적 게임 규칙을 해치면 구현하지 않는다.

2. 프로젝트 기준
   Project Sub-Terra는 2D 횡스크롤 기반 지하 탐사·채굴·위험 관리·기지 건설 게임이다.

   2-1. 핵심 목표
   - 채굴로 광물과 새로운 길을 얻는 동시에 구조 안정도와 가스 위험을 발생시킨다.
   - 버팀목, 조명, 충전기와 전진기지 건설로 위험한 지하 공간을 안전한 탐사 영역으로 바꾼다.
   - 전력, 화물 가치, 귀환 거리와 위험 사이에서 더 탐사할지 귀환할지 선택하게 한다.
   - Digger-Bot은 실제 게임 상태를 분석해 행동과 근거를 추천하되 게임 결과를 결정하지 않는다.

   2-2. 1차 MVP 범위
   - 천층 폐광 지대와 중층 가스 지대
   - 플레이어 이동, 방향 채굴과 Tilemap 지형
   - 구리·철·리튬, 인벤토리, 화물 중량과 미정산 가치
   - 광물 판매, 시설 제작과 장비·드론 업그레이드
   - 구조 안정도, 균열, 부분 붕괴와 가스 위험
   - 버팀목, 조명, 충전기, 보관함, 정산 콘솔과 전진기지 코어
   - 전력망, 전진기지 귀환과 체크포인트
   - 규칙 기반 드론 분석, 판단 근거 UI와 템플릿 대사
   - 선택적 생성형 AI 대사와 항상 동작하는 템플릿 폴백
   - 로컬 JSON 세이브, 자동 저장, 백업과 세이브 버전 관리
   - Windows x64 Standalone 데모 빌드

   2-3. 1차 MVP 제외 범위
   - 멀티플레이
   - 서버 DB, 회원가입과 로그인
   - 대규모 몬스터 전투
   - 자유 대화형 AI와 로컬 대형 언어 모델
   - 모바일 동시 출시
   - 완전한 절차적 무한 월드
   - 복잡한 유체 기반 가스 확산과 전체 지형 물리 붕괴
   - 완성형 스토리 캠페인
   - 유저 간 거래, 실시간 랭킹과 유료 상품

   2-4. 권장 기술 스택
   - Unity 6.3 LTS
   - Universal Render Pipeline 2D Renderer
   - C#과 MonoBehaviour
   - Unity Input System
   - Grid와 Tilemap
   - Light 2D, Sprite Renderer와 Particle System
   - Unity UI Canvas와 TextMeshPro
   - ScriptableObject 데이터 카탈로그
   - `[Serializable]` Save DTO와 로컬 JSON
   - Unity Test Framework
   - Git과 GitHub
   - Windows x64 Standalone Build Profile

3. 공통 개발 규칙
   3-1. 기존 프로젝트 구조와 소유권을 우선한다.
   - 프로젝트 전용 파일은 `Assets/_Project/` 아래에 둔다.
   - 담당자 A의 월드·게임플레이 코드는 `Scripts/Gameplay/` 아래에 둔다.
   - 담당자 B의 상태·UI·저장·통합 코드는 `Scripts/App/` 아래에 둔다.
   - 두 영역이 공유하는 인터페이스, 이벤트와 DTO는 `Scripts/Shared/`에서 관리한다.
   - 최종 통합 Scene은 담당자 B만 수정하고, 담당자 A는 독립 Test Scene과 Runtime Prefab에서 작업한다.
   - 외부 패키지는 `Plugins/` 또는 `ThirdParty/`로 프로젝트 코드와 분리한다.

   3-2. C#과 Unity 직렬화 규칙을 따른다.
   - 신규 런타임 코드는 C#으로 작성한다.
   - 고정 정의 데이터는 ScriptableObject, 실행 중 상태는 일반 C# 객체, 영구 상태는 `[Serializable]` DTO로 분리한다.
   - 구체 구현이 아닌 Shared 인터페이스와 이벤트를 통해 Gameplay와 App 시스템을 연결한다.
   - 새 타입과 필드는 현재 작업에 필요한 범위만 정의한다.

   3-3. 파일 성격에 맞는 Unity 구조를 사용한다.
   - MonoBehaviour는 Scene 또는 Prefab 수명주기가 필요한 기능에 사용한다.
   - 순수 계산은 가능한 한 일반 C# 클래스로 분리해 Edit Mode 테스트가 가능하게 한다.
   - 정적 게임 수치를 코드에 하드코딩하지 않고 담당 ScriptableObject에서 관리한다.
   - 프로젝트가 이미 사용하는 네임스페이스, Assembly Definition과 직렬화 형식을 따른다.

   3-4. 참조와 의존성은 기존 스타일을 따른다.
   - 담당자 A는 담당자 B의 `InventoryService`, `GameState` 같은 구체 클래스를 직접 참조하지 않는다.
   - 담당자 B는 담당자 A의 Runtime Prefab 내부나 Gameplay 구현을 직접 수정하지 않는다.
   - Inspector 참조가 필요하면 필수 참조 목록과 연결 방법을 PR에 기록한다.
   - 같은 기능을 위한 중복 Service, Manager, State 또는 데이터 경로를 만들지 않는다.

   3-5. 주석은 필요한 곳에만 작성한다.
   - 구조 안정도, 전력망, 저장 복원 순서, 드론 판단 규칙처럼 이해 비용이 큰 코드에는 짧은 한국어 주석을 사용할 수 있다.
   - 모든 함수나 모든 줄에 의무적으로 주석을 달지 않는다.
   - 기존 주석은 해당 코드가 삭제되거나 의미가 틀린 경우에만 정리한다.

   3-6. 로그는 최소화한다.
   - 개발 확인용 로그는 필요한 경우에만 사용하고, 작업 완료 전 불필요한 로그는 제거한다.
   - API Key, Token, Secret, 로컬 세이브 원문과 외부 AI 응답 원문 전체를 로그로 출력하지 않는다.
   - 반복되는 Update 로그, Tilemap 전체 덤프와 매 프레임 상태 출력은 금지한다.
   - Release Profile에서는 디버그 메뉴와 불필요한 상세 로그를 제거한다.

4. 프로젝트 전용 데이터 규칙
   4-1. 정적 게임 데이터
   다음 데이터는 ScriptableObject와 GameDataCatalog를 기준으로 관리한다.
   - 광물과 채굴 타일
   - 건물과 제작 레시피
   - 업그레이드와 계약
   - 위험 요소
   - 드론 대사 템플릿

   4-2. 영구 ID
   표시 이름이나 에셋 이름 대신 다음과 같은 내부 ID를 사용한다.
   - `mineral.copper`, `mineral.iron`, `mineral.lithium`
   - `tile.rock.normal`, `tile.rock.fractured`, `tile.gas.basic`
   - `building.support.basic`, `building.light.basic`, `building.outpost_core.basic`
   - `upgrade.drill.speed`, `upgrade.drone.scan`, `upgrade.drone.rescue`

   4-3. 런타임 상태
   다음 상태를 정적 데이터와 분리한다.
   - `PlayerState`: 전력, 골드, 인벤토리와 업그레이드
   - `RunState`: 깊이, 미정산 화물, 가스 노출과 귀환 상태
   - `WorldState`: 월드 Seed, 변경 타일, 건물과 가스 상태

   4-4. 데이터 변경 원칙
   - 광물 가격, 무게, 채굴 시간과 시설 비용을 코드에 하드코딩하지 않는다.
   - 한 번 배포한 내부 ID는 되도록 변경하지 않는다.
   - ID 변경이 불가피하면 Save Migration과 누락 ID 처리까지 함께 설계한다.
   - 데이터 카탈로그는 게임 시작 시 ID 중복과 누락 참조를 검증한다.

   4-5. DTO와 Unity Object
   - Shared DTO와 Save DTO에는 ID, 수량, 좌표, 수치와 상태만 넣는다.
   - `GameObject`, `MonoBehaviour`, `Sprite`, `TileBase`, `Collider`와 Prefab 참조를 저장하지 않는다.
   - `isPowered`처럼 복원 뒤 계산할 수 있는 값은 저장하지 않고 다시 계산한다.
   - 공통 DTO 변경 시 Producer, Consumer, 저장 호환성과 테스트를 함께 확인한다.

5. Unity Scene과 Prefab 규칙
   5-1. Scene 소유권
   - 담당자 A는 `Scenes/Test/Gameplay/`의 독립 Test Scene에서 기능을 구현한다.
   - 담당자 B는 Bootstrap, MainMenu, SurfaceBase와 Integration Scene을 관리한다.
   - `Mine_Demo_Integration.unity`는 담당자 B만 수정한다.
   - 담당자 A는 통합이 필요한 경우 Runtime Prefab, 설치 지침, 참조 목록과 검증 절차를 전달한다.

   5-2. Prefab 소유권
   - Player, Runtime Building, GasZone과 DiggerBot Runtime Prefab은 담당자 A가 관리한다.
   - HUD, 메뉴, 패널과 DiggerBot View Prefab은 담당자 B가 관리한다.
   - 다른 담당자의 Prefab 내부를 직접 수정하지 않고 Prefab Variant 또는 `ViewSocket`을 활용한다.
   - 기능 Prefab의 시각 요소는 `VisualRoot` 아래에 분리한다.

   5-3. Unity 직렬화 파일
   - Scene과 Prefab은 Force Text Serialization을 사용한다.
   - Unity 파일은 `.meta`와 함께 Unity Editor 안에서 이동하거나 이름을 변경한다.
   - `.meta`를 누락하거나 GUID가 바뀌는 이동을 하지 않는다.
   - Scene과 Prefab YAML을 무리하게 직접 편집하지 않고 Editor API 또는 Unity Editor를 사용한다.

   5-4. 공용 설정
   - `ProjectSettings/*`, `Packages/manifest.json`, `Packages/packages-lock.json`은 합의 없이 수정하지 않는다.
   - Unity 엔진 버전과 패키지 버전을 작업 중 임의로 올리지 않는다.
   - Shared 계약 변경은 별도 Issue와 별도 PR로 먼저 합의·병합한다.
   - 공용 설정 변경은 두 개발자 환경과 Windows 빌드에서 검증한다.

6. 세이브와 외부 서비스 안전 규칙
   6-1. 로컬 세이브 안전성
   - 저장은 `Application.persistentDataPath` 아래의 로컬 JSON을 사용한다.
   - 정식 파일을 직접 덮어쓰기 전에 임시 파일을 기록하고 JSON을 검증한다.
   - 기존 정상 파일은 백업으로 보존한 뒤 임시 파일을 정식 파일로 교체한다.
   - 정상 세이브가 손상되면 백업을 시도하고, 둘 다 실패하면 오류 안내 또는 새 게임으로 복구한다.

   6-2. 세이브 호환성
   - `saveVersion`과 게임 버전을 기록한다.
   - DTO 필드 추가 시 이전 세이브의 누락 값에 안전한 기본값을 적용한다.
   - 저장 ID 변경 시 Save Migration 또는 명시적인 폴백을 제공한다.
   - 월드 전체가 아니라 채굴·붕괴·건설로 바뀐 변경점만 저장한다.

   6-3. 클라우드 AI
   - 생성형 AI 대사는 기본 템플릿 시스템이 완성된 뒤 선택적으로 추가한다.
   - API 키를 Unity 클라이언트나 배포 빌드에 포함하지 않는다.
   - AI가 붕괴, 피해, 구조 안정도, 가스 위험과 귀환 가능성을 계산하게 하지 않는다.
   - API 실패, 오프라인과 호출 제한 상황에서도 템플릿 대사로 전체 게임이 동작해야 한다.

   6-4. 외부 전송
   - 저장 파일, 환경변수, 비밀정보와 사용자 로컬 경로를 외부 서비스로 보내지 않는다.
   - 외부 AI에 diff나 로그를 전송해야 하면 민감정보를 먼저 검사하고 사용자 확인을 받는다.
   - AI 응답은 확정된 추천 행동과 실제 수치를 바꾸지 못하며 자연어 표현에만 사용한다.

7. 게임플레이와 AI 규칙
   7-1. 결정론적 게임플레이
   - 동일한 입력 상태에서는 구조 위험, 붕괴, 피해, 가스 노출과 시설 설치 결과가 재현 가능해야 한다.
   - 붕괴는 전체 지형 물리 시뮬레이션 대신 정해진 규칙과 월드 Seed를 사용한다.
   - 건설은 위치와 비용을 모두 검증하고 Runtime Prefab 생성 성공 뒤 자원을 차감한다.
   - 전력망은 시설 변경 시 재계산하며 연결되지 않은 시설은 비활성화한다.

   7-2. 드론 분석
   - Digger-Bot은 `DroneContextDto`의 실제 상태만 사용한다.
   - 동일한 Context에서는 같은 추천을 반환한다.
   - 즉시 생존 위험, 붕괴, 가스, 전력 부족, 귀환, 탐사 순으로 우선순위를 적용한다.
   - 대사는 새로운 수치나 발견 사실을 만들어내지 않는다.
   - 같은 대사가 과도하게 반복되지 않도록 우선순위와 쿨다운을 적용한다.

   7-3. 시각 요소와 그레이박스
   - 핵심 루프 검증 전에는 Primitive Sprite, 단색 타일, 단순 PNG, 텍스트와 Light 2D를 사용한다.
   - 색상만으로 요소를 구분하지 않고 모양이나 무늬를 함께 다르게 한다.
   - 정식 아트는 `VisualRoot` 내부 교체로 적용하고 게임 로직이 Sprite 이름이나 파일명에 의존하지 않게 한다.
   - 정식 에셋, 애니메이션과 사운드는 핵심 루프가 검증된 뒤 추가한다.

8. 빌드와 실행 규칙
   8-1. Build Profile
   개발 단계에 따라 다음 Profile을 분리한다.
   - Development: 디버그 HUD, 테스트 도구와 상세 로그 포함
   - QA: Release와 유사한 설정, 치트 비활성화와 Save Migration 검증
   - Release: Development Build와 디버그 메뉴 비활성화, 불필요한 로그 제거
   - 최초 배포 대상: Windows x64 Standalone

   8-2. 자동 저장
   - 지상 귀환, 광물 정산, 업그레이드 구매와 전진기지 설치 후 저장을 요청한다.
   - 새 구역 진입, 구조 실패와 정상 종료 요청 시 저장을 고려한다.
   - 주기 저장은 변경 사항이 있을 때만 실행한다.
   - 저장 중 게임플레이 프레임이 불필요하게 멈추지 않는지 확인한다.

   8-3. 배포 결과물
   - 실행 파일만 전달하지 않고 `SubTerra_Data/`, 필수 DLL, README와 CHANGELOG를 함께 포함한다.
   - 버전, 빌드 번호와 Save Version을 빌드 화면 또는 문서에 기록한다.
   - ZIP 배포 전 다른 PC에서 새 게임, 이어하기와 세이브 경로 생성을 검증한다.
   - 자동 배포는 수동 Build Profile 절차가 안정된 뒤 도입한다.

9. 작업 금지 범위
   Agent는 아래 작업을 수행하면 안 된다.
   9-1. 사용자 홈 디렉터리 전체 스캔 금지
   9-2. Git 저장소 밖의 파일 읽기 금지
   9-3. `.env`, credentials, private key와 로컬 세이브 원문 출력 금지
   9-4. API Key, OAuth Token, Secret 값을 로그로 출력 금지
   9-5. 클라우드 AI API 키를 Unity 클라이언트 또는 빌드에 추가 금지
   9-6. 사용자 동의 없이 git commit 자동 실행 금지
   9-7. `git reset --hard`, `rm -rf` 등 파괴적 명령어 실행 또는 구현 금지
   9-8. Release 업로드, itch.io 게시 등 배포성 명령 자동 실행 금지
   9-9. 외부 AI API로 diff, 세이브 데이터, 환경변수와 로컬 경로를 사용자 확인 없이 전송 금지
   9-10. 테스트 목적으로 실제 사용자 Git 히스토리나 세이브 파일을 변경하는 코드 작성 금지
   9-11. 현재 기능과 무관한 대규모 리팩터링 금지
   9-12. 기존 기능과 무관한 새 패키지 또는 라이브러리 추가 금지

10. 공통 보안 원칙
    10-1. 민감정보 탐지
    외부 AI에 보내기 전 diff와 로그에서 민감정보 패턴을 탐지해야 한다.

    탐지 후보:
    - `API_KEY=`
    - `SECRET=`
    - `TOKEN=`
    - `PASSWORD=`
    - `PRIVATE_KEY`
    - `OPENAI_API_KEY`
    - `ANTHROPIC_API_KEY`
    - `AZURE_OPENAI_API_KEY`
    - `DATABASE_URL`
    - `-----BEGIN PRIVATE KEY-----`
    - `Application.persistentDataPath` 아래 저장 파일 원문

    처리 방식:
    1. 위험 패턴 탐지
    2. 사용자에게 경고
    3. 필요 시 해당 값과 로컬 경로 마스킹
    4. 외부 AI 전송 여부 확인

    10-2. Diff 제외 파일
    민감하거나 자동 생성될 가능성이 높은 파일은 AI 분석 대상에서 기본 제외한다.

    기본 제외 후보:
    - `.env`
    - `.env.*`
    - `*.pem`
    - `*.key`
    - `credentials.json`
    - `secrets.json`
    - `save_slot_*.json`
    - `save_slot_*.backup.json`
    - `save_slot_*.tmp`

    10-3. 사용자 확인
    실제 Git 히스토리를 변경하거나 외부 배포·업로드를 수행하기 전에는 사용자 확인을 받는다.

11. Git 작업 규칙
    11-1. `main`은 안정 버전, `develop`은 통합 기준 브랜치로 유지한다.
    11-2. `main` 직접 push는 금지한다.
    11-3. 변경은 기능 브랜치와 Pull Request 단위로 통합하는 것을 기본으로 한다.
    11-4. 하나의 브랜치는 하나의 주요 목적만 담당하고 `feature/a-*`, `feature/b-*`, `fix/*` 형식을 사용한다.
    11-5. Shared 계약, Producer, Consumer, Integration Scene, 통합 테스트 순으로 병합한다.
    11-6. Scene, Prefab과 `.meta` 변경을 PR에서 명시한다.
    11-7. 파일 수를 강제하지 않지만 작업 목적과 다른 담당자 소유 파일은 수정하지 않는다.
    11-8. 작업 완료 후 변경 목적, Inspector 참조, 통합 방법과 검증 결과를 명확히 남긴다.

12. 테스트와 검증 원칙
    12-1. 기본 검증
    - 지정된 Unity 6.3 LTS에서 프로젝트를 열고 초기 Console 오류를 확인한다.
    - 순수 계산은 Edit Mode 테스트, Scene·컴포넌트 연결은 Play Mode 테스트로 검증한다.
    - 테스트 명령이나 자동화 환경이 아직 구성되지 않았다면 그 사실과 수동 검증 결과를 보고한다.
    - Unity Test Framework 실행 뒤 실패 원인과 미실행 항목을 구분해 기록한다.

    12-2. 기능 검증
    - 플레이어 이동, Tilemap 채굴과 광물 보상이 한 번만 발생하는지 확인한다.
    - 구조 위험, 버팀목, 붕괴와 가스 노출이 결정론적으로 작동하는지 확인한다.
    - 건설 실패 시 자원이 차감되지 않고 전력망 변경이 즉시 반영되는지 확인한다.
    - 드론 Context, 추천과 표시된 근거가 실제 State와 일치하는지 확인한다.
    - 새 게임, 자동 저장, 이어하기, 월드 복원과 구조 실패 후 복귀 흐름을 확인한다.

    12-3. 빌드 검증
    - `Mine_Demo_Integration`을 시작부터 심층 신호 확인까지 완주한다.
    - Scene과 Prefab 참조 누락, Missing Script와 Console Error가 없어야 한다.
    - Windows x64 빌드에서 새 게임과 이어하기를 확인한다.
    - 다른 PC에서 실행 파일, Data 폴더, 세이브 생성과 재실행을 검증한다.

13. 병렬 작업과 순차 작업 기준
    13-1. 병렬 가능 작업
    아래 작업은 소유 파일과 Scene이 분리되어 있으면 병렬 진행이 가능하다.
    - 담당자 A의 독립 Gameplay Test Scene 기능 구현
    - 담당자 B의 State, UI와 ScriptableObject 데이터 에셋 구현
    - 서로 다른 소유 폴더의 Edit Mode 테스트 작성
    - Runtime Prefab과 UI Prefab의 독립 작업
    - 문서, 설치 지침과 테스트 케이스 초안 작성
    - Shared 계약을 변경하지 않는 목업 기반 구현

    13-2. 순차 작업 필요
    아래 작업은 의존성이 있으므로 순차 진행한다.
    - Shared 인터페이스 변경 → A의 Producer 구현 → B의 Consumer 구현 → Integration Scene 연결 → 통합 테스트
    - `MiningSystem` 보상 이벤트 → `InventoryService` 수신 → Inventory HUD → 채굴 수직 슬라이스 검증
    - 건설 위치 검증 → 비용 확인 → Runtime Prefab 생성 → 자원 차감 → 건설 UI 검증
    - 월드 스냅샷 → Save DTO → JSON 저장·백업 → 로드 → 월드 복원 검증
    - Drone Context → 결정론적 추천 → 템플릿 대사 → 선택적 클라우드 대사 → 폴백 검증

14. Agent 작업 결과물 제출 형식
    각 Agent는 작업 완료 후 아래 항목을 보고한다.
    14-1. 수정/생성한 파일 목록
    14-2. 주요 변경 내용과 설계 이유
    14-3. 실행한 테스트와 수동 검증 절차
    14-4. 테스트 결과와 Unity Console 상태
    14-5. 필요한 Inspector 참조와 Integration 방법
    14-6. 남은 TODO, 세이브 호환성 또는 검증 제한
    14-7. 다른 담당자가 참고해야 할 사항

보고 예시:
수정 파일:

- `Assets/_Project/Scripts/Gameplay/Mining/MiningSystem.cs`
- `Assets/_Project/Tests/EditMode/Gameplay/MiningSystemTests.cs`

주요 변경:

- 채굴 완료 시 `IMiningRewardReceiver`로 광물 ID와 수량을 한 번만 전달
- Tilemap 범위 밖 좌표와 전력 부족 상태에서 채굴을 시작하지 않도록 처리

테스트:

- Edit Mode `MiningSystemTests` 통과
- `Gameplay_Mining_Test.unity`에서 구리 보상 1회 발생 확인

주의 사항:

- Integration Scene의 Player Prefab에 `ForegroundTilemap`과 `InventoryService` 연결 필요

15. Agent 작업 요청 템플릿
    Agent에게 작업을 요청할 때는 아래 형식을 사용할 수 있다.

Agent 이름:

작업 목표:

수정/생성할 파일:

참조해야 할 파일:

구현해야 할 시스템 또는 함수:

입력:

출력:

금지 사항:

소유권·세이브·보안 규칙:

완료 조건:

테스트 방법:

예상 실패 케이스:
