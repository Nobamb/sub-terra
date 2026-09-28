# `init/rule.md` 분리 내역

기준: `Nobamb/sub-terra`의 `MVP2-fix42` 브랜치 `init/rule.md` (SHA `0813f6e3e57f0b4994893f01062b81ddc6643950`). 같은 시점의 `main`에서도 이 파일의 SHA가 같았다. 기존 조항 번호와 문장은 유지하고, 위치와 문서 제목 및 라우팅 설명만 추가했다.

## 주요 규칙 이동 표

| 기존 조항 | 주요 내용 | 새 위치 |
| --- | --- | --- |
| 1, 1-1 ~ 1-8 | 보호·안정성·소유권·게임 규칙 등 우선순위 | `init/rule.md` |
| 2 서두, 2-1 ~ 2-4 | 게임 정의, 목표, MVP 포함·제외 범위, 권장 기술 스택 | `init/rules/project-scope.md` |
| 2-5, 2-5-1 ~ 2-5-6 | UI 변경 범위, Builder, Editor 부작용, 커밋 전 범위 검증, 복구, 예외 | `init/rules/ui-ugui.md` |
| 3 서두, 3-1 | 프로젝트 폴더 구조와 A/B/Shared 소유권 | `init/rule.md` |
| 3-2 ~ 3-6 | C# 직렬화·구조·의존성·주석·로그 | `init/rules/code.md` |
| 4, 4-1 ~ 4-5 | ScriptableObject, 영구 ID, 런타임 상태, DTO, 데이터 변경 | `init/rules/data-save-gameplay.md` |
| 5, 5-1 ~ 5-5 | Scene·Prefab 소유권, YAML·.meta, 공용 설정, Integration 바인딩 | `init/rules/unity-project.md` |
| 6, 6-1 ~ 6-4 | 로컬 JSON 세이브, 호환성, 클라우드 AI, 외부 전송 | `init/rules/data-save-gameplay.md` |
| 7 서두, 7-1 ~ 7-2 | 결정론적 플레이와 Digger-Bot 분석 | `init/rules/data-save-gameplay.md` |
| 7-3 | 시각 요소·그레이박스·정식 에셋 교체 | `init/rules/unity-project.md` |
| 8, 8-1 ~ 8-3 | Build Profile, 자동 저장, 배포 결과물 | `init/rules/testing-qa.md` |
| 9, 9-1 ~ 9-12 | 공통 금지 범위와 보안 경계 | `init/rule.md` |
| 10, 10-1 ~ 10-3 | 민감정보 탐지, Diff 제외, 사용자 확인 | `init/rule.md` |
| 11, 11-1 ~ 11-8 | 브랜치, PR, 병합·보고 | `init/rules/git-commit.md` |
| 12, 12-1 ~ 12-3 | 기본·기능·빌드 검증 | `init/rules/testing-qa.md` |
| 13, 13-1 ~ 13-2 | 병렬 작업과 순차 의존성 | `init/rules/project-scope.md` |
| 14, 14-1 ~ 14-7 및 예시 | 결과물 제출 형식 | `init/rules/reporting.md` |
| 15 및 템플릿 | Agent 작업 요청 형식 | `init/rules/reporting.md` |

## 중복과 적용 범위

- 2-5-4의 UI 변경 파일 검사는 11의 일반 Git 규칙보다 구체적이다. 원문을 두 파일에 복제하지 않았으며 UI 작업 때 `ui-ugui.md`와 `git-commit.md`를 함께 읽도록 라우팅했다.
- 3-1과 5-1/5-2에는 A/B 담당 경계가 반복된다. 3-1은 모든 작업의 구조·소유권으로 진입점에 두고, Scene·Prefab의 구체적 소유권은 `unity-project.md`에 그대로 남겼다.
- 3-6, 9-3/9-4, 10-1에는 비밀정보 출력·로그 제한이 여러 차례 나타난다. 원문 문구를 삭제하면 적용 범위가 달라질 수 있어 각 조항을 그대로 보존했다.
- 4-5와 6-2는 DTO와 세이브 호환성을 함께 다룬다. 두 조항을 `data-save-gameplay.md`에 모았고 원문 표현을 유지했다.
- 5-5와 12-3은 Integration Scene과 빌드 검증을 각각 다룬다. 작업 성격에 따라 두 문서를 함께 읽도록 분리했다.
- 9-6과 10-3의 사용자 확인은 유사하지만 커밋과 Git 히스토리 변경·외부 배포를 각각 명시한다. 둘 다 공통 규칙에 남겼다.

## 충돌 또는 해석 확인이 필요한 부분

다음은 원문을 임의로 고치지 않고 기록한 사항이다.

1. **`git restore`와 파괴적 명령의 범위:** 2-5-4와 2-5-5는 범위 밖 Prefab·Scene의 복구를 위해 `git restore` / checkout을 지시한다. 9-7은 `git reset --hard`, `rm -rf` 등의 파괴적 명령을 금지한다. 사용자의 미커밋 변경까지 되돌리는 `git restore`가 허용되는지, 되돌리기 전 어떤 확인이 필요한지 원문에 명시되어 있지 않다.
2. **외부 전송의 조건:** 6-4는 저장 파일·환경변수·비밀정보·사용자 로컬 경로의 외부 서비스 전송을 금지한다. 9-9는 diff·세이브 데이터·환경변수·로컬 경로의 *사용자 확인 없는* 외부 AI 전송을 금지하고, 10-1은 마스킹과 확인 절차를 안내한다. 확인을 받아도 저장 파일 원문 전송을 허용하는지 조항별 표현이 다르다.
3. **다른 최상위 지침과의 차이:** `CLAUDE.md`는 기능 브랜치의 PR을 `main`으로 보내는 흐름을 설명하지만 11-1은 `develop`을 통합 기준 브랜치로 지정한다. `AGENTS.md`는 `ProjectSettings/*`와 `Packages/manifest.json`, `packages-lock.json`의 변경을 금지하지만 5-4는 합의가 있다면 변경할 수 있다는 표현이다. 이번 분리에서는 어느 쪽도 수정하지 않았다.

## 검증

- 기존 1~15의 번호가 붙은 모든 조항과 번호가 없는 본문 줄을 새 문서에서 원문과 대조했다. 원문의 비어 있지 않은 368개 줄은 각각 한 번씩 배치됐다.
- 기존 번호가 붙은 각 제목·하위 조항은 새 파일 전체에서 정확히 한 번 등장한다.
- 새 `init/rule.md`는 96줄이며, 세부 규칙은 8개 독립 파일이다.
- 이번 자료는 Markdown 분리 작업이다. Unity Editor 실행, 게임 테스트와 Windows 빌드는 수행하지 않았다.
