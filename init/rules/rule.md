# Project Sub-Terra Agent 작업 규칙

이 문서는 모든 Agent가 작업 전 읽는 진입점이다. 아래 공통 규칙은 작업 분야에 관계없이 적용한다. 관련 세부 문서를 작업 전에 읽고, 한 작업이 여러 분야에 걸치면 해당 파일을 모두 읽는다. 원래 조항 번호는 이동 추적을 위해 유지했다. 이동 내역은 저장소 루트의 `RULE_MIGRATION.md`에 기록되어 있다.

## 세부 규칙 라우팅

| 작업 상황 | 추가로 읽을 파일 |
| --- | --- |
| 요구사항, MVP 범위, 담당자 역할, 병렬·순차 작업 계획 | [`rules/project-scope.md`](rules/project-scope.md) |
| Git, 브랜치, PR, 변경 파일 범위, 커밋·병합 | [`rules/git-commit.md`](rules/git-commit.md) |
| Unity Scene·Prefab·공용 설정·직렬화·시각 에셋 | [`rules/unity-project.md`](rules/unity-project.md) |
| UI, uGUI/TMP, Layout Builder, UI Prefab·Scene 변경 또는 복구 | [`rules/ui-ugui.md`](rules/ui-ugui.md)와 [`rules/unity-project.md`](rules/unity-project.md); Builder 코드도 바꾸면 [`rules/code.md`](rules/code.md) |
| C# 코드·Shared 계약·구조·로그 | [`rules/code.md`](rules/code.md) |
| GameDataCatalog, ID, DTO, 세이브, 외부 AI·전송, 결정론적 게임플레이·Digger-Bot | [`rules/data-save-gameplay.md`](rules/data-save-gameplay.md); 코드 수정도 있으면 [`rules/code.md`](rules/code.md) |
| Build Profile, 자동 저장, 배포 결과물, 기능·Play/Edit Mode·Windows 빌드 검증 | [`rules/testing-qa.md`](rules/testing-qa.md) |
| 작업 결과 제출 또는 Agent 작업 요청 작성 | [`rules/reporting.md`](rules/reporting.md) |

저장소 파일을 변경하는 작업은 `rules/git-commit.md`를, 작업 결과를 제출할 때는 `rules/reporting.md`를 추가로 읽는다. UI 변경에는 `rules/ui-ugui.md`의 커밋 범위 검증 및 회귀 복구 규칙도 적용한다. 작업의 규칙 간 충돌이 보이면 임의로 완화하지 말고 보고한다.

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

3. 공통 개발 규칙

3-1. 기존 프로젝트 구조와 소유권을 우선한다.
   - 프로젝트 전용 파일은 `Assets/_Project/` 아래에 둔다.
   - 담당자 A의 월드·게임플레이 코드는 `Scripts/Gameplay/` 아래에 둔다.
   - 담당자 B의 상태·UI·저장·통합 코드는 `Scripts/App/` 아래에 둔다.
   - 두 영역이 공유하는 인터페이스, 이벤트와 DTO는 `Scripts/Shared/`에서 관리한다.
   - 최종 통합 Scene은 담당자 B만 수정하고, 담당자 A는 독립 Test Scene과 Runtime Prefab에서 작업한다.
   - 외부 패키지는 `Plugins/` 또는 `ThirdParty/`로 프로젝트 코드와 분리한다.

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
