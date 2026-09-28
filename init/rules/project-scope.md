# 프로젝트 범위·작업 순서

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
   - Unity 6.5 LTS
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
