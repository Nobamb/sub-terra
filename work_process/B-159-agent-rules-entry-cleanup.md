# B-159 AI 작업 규칙 진입 문서 정리 (문서만)

## 목표
- `init/rules/rule.md` 16번(작업 태도)과 17번(화면 확인 효율), `init/rules/lessons.md`는 이미 main에 있다. 하지만 진입 문서(CLAUDE.md, AGENTS.md)에서 이 문서들로 가는 안내가 없다. 이 안내를 넣는다.
- 옛 통합본 `init/rule.md`를 "폐기됨" 안내문으로 바꾼다. 남은 경로 오류와 깨진 링크도 고친다.
- 기준: 최신 main에서 새 브랜치를 딴다 (예: `docs/b159-agent-rules-entry`). 작업이 끝나면 PR을 연다.
- 일부를 Codex 등 다른 에이전트에 맡겨도 된다. 맡겼다면 결과는 직접 확인한다.

## 절대 규칙 (먼저 읽기)
1. **문서만 바꾼다.** 코드, 씬, 프리팹, 에셋, 설정은 건드리지 않는다.
2. **조항 번호는 바꾸지 않는다.** 번호가 비어 있는 곳은 일부러 비운 것이다. 이번 작업에서는 새 조항을 만들지 않고 기존 조항에 줄만 붙인다. 나중에 새 조항이 생기면 18번부터다.
3. 아래 문구는 그대로 넣는다. 말투나 들여쓰기는 주변 줄에 맞춰도 되지만, 뜻은 바꾸지 않는다.
4. 파일은 삭제하지 않는다. 브랜치도 삭제하지 않는다. main에 직접 push하지 않는다.
5. 지난 기록은 고치지 않는다. 예: `init/prompt-B.md`에 쌓인 옛 요청문, `work_process/` 결과 문서. 진입점을 안내하는 문서만 고친다.

## 먼저 읽을 파일
- `CLAUDE.md`: "## Rules that matter most"
- `AGENTS.md`: "## Sources of truth", "## Verification"
- `init/rules/rule.md`: 첫 문단, 세부 규칙 라우팅 표, 16·17번
- `init/rules/reporting.md`: 14번
- `init/rules/RULE_MIGRATION.md`: 주요 규칙 이동 표
- `init/rules/lessons.md`: 작업 전에 읽는다 (16-4)
- `init/rule.md`: 옛 통합본, 446줄. 16·17번이 없다.

## 1. CLAUDE.md
"## Rules that matter most" 목록 맨 끝에 추가:
```
- Work attitude (`init/rules/rule.md` §16–17): read related code/docs/issues before coding; push back with reasons when a request conflicts with the PRD, rules, or other PRs; don't call work "완료" on passing tests alone — verify in actual play and report anything unchecked as "미검증"; read `init/rules/lessons.md` before every task and add one line (`날짜 | 실수 | 원인 | 다음부터 할 일`) after fixing a mistake or regression.
```

## 2. AGENTS.md
"## Sources of truth" 목록의 `init/rules` 줄 바로 아래에 추가:
```
- `init/rules/rule.md` — entry point; §16 work attitude and §17 screen-check rules apply to every task
- `init/rules/lessons.md` — mistake log; read before every task, append one line after fixing a mistake
```
"## Verification"의 4번을 아래 두 줄로 바꾼다:
```
4. Report changed files, verification performed, and unresolved issues. Mark anything not verified in actual play or on screen as "미검증" with a manual checklist.
5. If you fixed a mistake or regression, add one line to `init/rules/lessons.md`.
```

## 3. init/rules/reporting.md
14번의 `14-7` 줄 바로 아래에 추가 (들여쓰기는 14-7과 같게):
```
    14-8. 직접 확인하지 못한 항목은 "미검증"으로 따로 적고 수동 체크리스트를 남긴다 (16-3)
    14-9. 이번 작업에서 고친 실수가 있으면 `lessons.md`에 추가한 줄 (16-4)
```

## 4. init/rules/rule.md
4-1. 라우팅 표 마지막 줄 아래에 새 줄을 추가한다 (표의 열 폭 맞춤은 선택):
```
| 모든 작업 시작 전 (실수 기록 확인) | [`lessons.md`](lessons.md) |
```
표 아래 문단 끝에 한 문장을 추가한다:
```
모든 작업은 16번 작업 태도 규칙을 따르고, 시작 전에 `lessons.md`를 읽는다.
```

4-2. 첫 문단의 위치 오류를 고친다. `RULE_MIGRATION.md`는 저장소 루트가 아니라 `init/rules/`에 있다.
- 지금: `` 이동 내역은 저장소 루트의 `RULE_MIGRATION.md`에 기록되어 있다. ``
- 바꿈: `` 이동 내역은 같은 폴더의 [`RULE_MIGRATION.md`](RULE_MIGRATION.md)에 기록되어 있다. ``

4-3. 깨진 상대 링크를 고친다. 이 파일은 `init/rules/` 안에 있다. 그래서 `(rules/code.md)` 같은 링크는 `init/rules/rules/code.md`를 가리켜 열리지 않는다.
- 라우팅 표의 링크 대상 `(rules/xxx.md)`를 모두 `(xxx.md)`로 바꾼다. 예: `` [`rules/code.md`](rules/code.md) `` → `` [`code.md`](code.md) ``
- 대상 파일: project-scope, git-commit, unity-project, ui-ugui, code, data-save-gameplay, testing-qa, reporting
- 표 아래 문단에 있는 백틱 경로 `rules/git-commit.md`, `rules/reporting.md`, `rules/ui-ugui.md`도 `git-commit.md` 같은 같은 폴더 기준 이름으로 맞춘다.
- 다른 `init/rules/*.md` 파일에도 같은 형태의 깨진 링크가 있으면 같이 고치고 보고한다.

## 5. init/rules/RULE_MIGRATION.md (권장)
"## 주요 규칙 이동 표" 표 아래에 추가:
```
분리 이후 추가된 조항:

| 조항 | 주요 내용 | 위치 |
| --- | --- | --- |
| 16, 16-1 ~ 16-4 | 작업 태도: 정보 없이 코딩 금지, 무조건 동의 금지, 테스트만으로 완료 금지, 같은 실수 반복 금지 | `init/rules/rule.md` |
| 17 | 화면 확인 효율 | `init/rules/rule.md` |
```
이 파일의 "## 검증"에 있는 지난 수치("새 `init/rule.md`는 96줄" 등)는 기록이므로 고치지 않는다.

## 6. 옛 통합본 init/rule.md 정리
6-1. 먼저 저장소 전체에서 `init/rule.md`를 진입점으로 가리키는 곳을 찾는다:
```
rg -n "init/rule\.md|\(rule\.md\)|\.\./rule\.md" --glob '!sub-terra/Library/**' --glob '!sub-terra/Temp/**'
```
- 이미 알고 있는 곳: `README.md` 220번 줄 근처의 규칙 표 `` [`init/rule.md`](init/rule.md) ``. 여기를 `` [`init/rules/rule.md`](init/rules/rule.md) ``로 바꾼다.
- CLAUDE.md, AGENTS.md, `docs/`, `.agents/skills/`, `init/process*.md` 등 지금 쓰는 안내 문서에서 찾은 곳도 모두 `init/rules/rule.md`로 바꾼다.
- `init/prompt-B.md`의 옛 요청문과 `work_process/` 결과 문서는 바꾸지 않는다 (절대 규칙 5). 몇 군데인지만 보고한다.

6-2. 그다음 `init/rule.md`의 내용 전체를 아래 안내문으로 바꾼다. 파일은 지우지 않는다:
```
# (폐기됨) Project Sub-Terra Agent 작업 규칙

이 파일은 더 이상 쓰지 않는다. 규칙은 [`init/rules/rule.md`](rules/rule.md)에서 시작해 `init/rules/` 아래 세부 문서로 나뉘어 있다.

- 진입점: [`rules/rule.md`](rules/rule.md)
- 실수 기록: [`rules/lessons.md`](rules/lessons.md)
- 옛 조항이 어디로 옮겨졌는지: [`rules/RULE_MIGRATION.md`](rules/RULE_MIGRATION.md)

옛 전체 본문은 Git 기록에 남아 있다 (`git log -- init/rule.md`).
```

## 범위 밖
- 조항 내용 수정이나 새 조항 추가 (16·17번 문구는 그대로 둔다)
- RULE_MIGRATION.md "충돌 또는 해석 확인" 항목 정리 (develop/main 차이, ProjectSettings 표현 등)
- lessons.md 기존 줄 수정
- 코드, 테스트, Unity 에셋

## 확인
- `git diff --stat`에 문서 파일만 있다: CLAUDE.md, AGENTS.md, README.md, init/rule.md, init/rules/rule.md, reporting.md, RULE_MIGRATION.md, 그리고 6-1에서 찾은 문서
- 이번에 고치거나 추가한 링크는 모두 실제 파일로 이어진다. 상대 경로를 파일 위치 기준으로 하나씩 확인한다. 예: 각 링크 대상에 `test -f`를 실행한다. GitHub PR의 Files 화면에서 눌러 보면 더 좋다.
- `init/rules/rule.md`의 조항 번호 목록(1, 3, 9, 10, 16, 17)이 그대로다.
- 다시 검색했을 때, 안내 문서에서 `init/rule.md`를 진입점으로 가리키는 곳이 0개다.

## 완료 후
- PR 제목 예: `docs: AI 작업 규칙 진입 문서 정리 (B-159)`. 커밋 메시지는 한국어 conventional 형식으로 쓴다.
- 보고 내용
  - 기준 커밋, 브랜치, PR 링크
  - 바뀐 파일마다 무엇을 바꿨는지 한 줄
  - 6-1 검색 결과: 바꾼 곳과 일부러 남긴 곳(옛 기록)의 개수
  - 확인한 링크 목록과 결과
  - 직접 확인하지 못한 항목은 "미검증"으로 적는다. 예: GitHub 화면에서 링크를 눌러 보지 못한 경우
