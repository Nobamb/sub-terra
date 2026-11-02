# B-173 교훈 기록을 "교훈 하나당 파일 하나"로 바꾸기 (#261 일부)

## 목표
- 에이전트 안내 문서가 더 이상 `init/rules/lessons.md`에 줄을 추가하라고 하지 않고, `init/rules/lessons/` 폴더에 새 파일을 만들게 한다.
- 코드·씬·프리팹·테스트 동작은 바꾸지 않는다(문서와 주석만).
- 다른 에이전트(Codex 등)에게 맡겨도 된다.

## 브랜치 (사용자가 하나 고름)
- A(권장): 최신 작업 브랜치 `MVP2-fix117`(3d0bc4bf, fix116 포함)에서 이어서 작업. 이 브랜치의 lessons.md 새 5줄도 같이 옮긴다. 일반 push.
- B: 최신 `main`(5cccca03)에서 새 브랜치 `MVP2-fix118`. 이 경우 3번은 fix117 병합 뒤 따로 한다.

## 1. 안내 문서 고치기 (줄 번호는 fix117 3d0bc4bf 기준, main과 같음)
| 파일:줄 | 바꿀 문구 |
|---|---|
| `AGENTS.md:22` | `` - `init/rules/lessons/` — mistake log, one file per lesson. Before each task, pick the files relevant to the task by name/tags and read them. Never append to `init/rules/lessons.md` (frozen). `` |
| `AGENTS.md:65` | `` 5. If you fixed a mistake or regression, add one new file `init/rules/lessons/YYYY-MM-DD-topic.md` (format: `init/rules/rule.md` 16-4). `` |
| `CLAUDE.md:82` 끝부분 | "read `init/rules/lessons.md` … after fixing a mistake or regression." → "before every task read the relevant files in `init/rules/lessons/` (pick by file name/tags; `lessons.md` is a frozen old table) and, after fixing a mistake or regression, add one new file there (format: `init/rules/rule.md` 16-4)." |
| `init/rule.md:6` | `` - 실수 기록: [`rules/lessons/`](rules/lessons/) (교훈 하나당 파일 하나. 옛 표 [`rules/lessons.md`](rules/lessons.md)는 추가 금지) `` |
| `init/rules/rule.md:17` | `` \| 모든 작업 시작 전 (실수 기록 확인) \| [`lessons/`](lessons/) 폴더의 관련 파일 (옛 표 `lessons.md`는 읽기만) \| `` |
| `init/rules/rule.md:19` 끝 | "시작 전에 `lessons.md`를 읽는다." → "시작 전에 `lessons/` 폴더에서 관련 교훈을 읽는다." |
| `init/rules/rule.md:121–123` (16-4) | 아래 "16-4 새 문구" |
| `init/rules/reporting.md:13` | `` 14-9. 이번 작업에서 고친 실수가 있으면 추가한 `init/rules/lessons/` 교훈 파일 경로 (16-4) `` |

16-4 새 문구:
```
- 작업 전 `init/rules/lessons/` 폴더의 파일 이름·태그를 보고 이번 작업과 관련된 교훈을 읽는다. 옛 표 `lessons.md`도 읽기만 한다.
- 실수나 회귀를 고치면 교훈 하나당 새 파일 하나를 만든다. `lessons.md`나 다른 교훈 파일에 줄을 추가하지 않는다.
- 파일 이름: `YYYY-MM-DD-주제.md`. 이슈·PR이 있으면 `YYYY-MM-DD-issueNNN-주제.md` / `YYYY-MM-DD-prNNN-주제.md`. 주제는 영문 소문자와 `-`만.
- 내용: `# 제목` 다음 줄에 `- 날짜:`, `- 태그:`, `- 실수:`, `- 원인:`, `- 다음부터 할 일:`, `- 관련:`. 보통 10줄 안팎, 200줄 이내.
- 공용 목차 파일은 만들지 않는다(충돌 원인).
```
- 줄 번호가 다르면 같은 문구를 찾아 고치고 보고서에 실제 줄을 적는다.

## 2. lessons.md 고정
- 맨 위 제목 바로 아래에 다음 안내를 넣고, 표는 그대로 둔다(옮기기는 #261 후속).
```
> **이 파일에는 더 이상 줄을 추가하지 마세요.** 새 교훈은 `init/rules/lessons/YYYY-MM-DD-주제.md` 파일 하나로 만듭니다(규칙: `init/rules/rule.md` 16-4, #261). 아래 표는 옛 기록이며 읽기만 합니다.
```

## 3. fix117의 다섯 줄 옮기기 (A를 골랐을 때)
- `lessons.md` 끝의 2026-10-29 다섯 줄(30~34행)을 지우고 아래 다섯 파일을 만든다. 양식은 `2026-10-28-issue260-surface-settings-overlay.md`와 같다(제목, 날짜, 태그, 실수, 원인, 다음부터 할 일, 관련).
| 새 파일 (`init/rules/lessons/`) | 태그 | 옮길 줄 (실수) |
|---|---|---|
| `2026-10-29-issue264-png-atomic-replace.md` | unity, editor, asset, png | PNG 직접 덮어쓰기가 에디터 메모리 매핑으로 실패 |
| `2026-10-29-issue264-playmode-helper-access.md` | testing, playmode, save, compile | 새 PlayMode 검사에서 저장 보조 타입·네임스페이스 참조 누락 |
| `2026-10-29-issue264-mining-finish-speed.md` | animation, player, mining, prefab | 채굴 완료 뒤 드릴을 올리는 동작이 느림 |
| `2026-10-29-issue264-mining-finish-start-delay.md` | animation, player, mining | 드릴 회수 속도를 높여도 파괴 뒤 시작 지연이 남음 |
| `2026-10-29-issue264-guide-demo-mining-timing.md` | animation, guide, demo, mining | 실제 캐릭터와 가이드·키 조작 시연의 드릴 회수 타이밍이 다름 |
- 실수·원인·다음부터 할 일은 표의 문장을 그대로 옮긴다. 관련: `#264, B-172`.
- 예시(첫 파일):
```
# 에디터가 쓰는 PNG 직접 덮어쓰기 실패
- 날짜: 2026-10-29
- 태그: unity, editor, asset, png
- 실수: B-172 PNG 직접 덮어쓰기가 에디터 메모리 매핑으로 실패
- 원인: 사용 중인 텍스처 파일을 truncate하고 같은 그림도 다시 씀
- 다음부터 할 일: 동일 바이트는 건너뛰고 새 PNG는 임시 파일로 원자 교체한다
- 관련: #264, B-172
```

## 하지 않는 것
- 기존 25줄 전체 옮기기, `work_process` 예전 기록, 테스트 주석 `(lessons 10-25)`(옮기기 때 함께), 코드·씬·프리팹.

## 확인
1. `git grep -n "lessons.md" -- . ':!work_process'` 결과가 lessons.md 자신, 2번 안내 문구, 새 규칙의 "추가 금지/읽기만" 문장, 테스트 주석만 남는다.
2. 새 교훈 파일 다섯 개가 기존 파일 양식(`2026-10-28-issue260-surface-settings-overlay.md`)과 같다.
3. Unity를 열 필요 없음. `git diff --stat`에 문서만 있다.

## 완료 후
- 한국어 conventional 커밋(`docs: 교훈 기록을 파일별로 바꾸는 안내 반영`). 일반 push, force·브랜치 삭제 금지.
- 보고서 `work_process/MVP2/B-173-lessons-files-result.md`: 고친 파일:줄(전/후), 옮긴 파일, 위 확인 결과. 미검증 항목은 "미검증".
