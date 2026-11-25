# PlayMode 플래그에 fixture 이름을 적어도 전체 PlayMode가 돈다

- 날짜: 2026-11-25
- 태그: test, playmode, tooling, workflow, b211
- 실수: 플레이어 PlayMode 3묶음만 돌리려고 `Temp/subterra-run-playmode.flag`에 fixture 이름을 적었다. 실제로는 전체 PlayMode가 돌아 17분 넘게 같은 통합 씬 화면이 반복됐다. 중간에 멈추자 결과 파일은 없었고, `work_process/images` 증거 그림 7장이 다시 쓰이고 새 폴더 3개가 생겼다.
- 원인: `EditModeTestRunnerCommand`는 EditMode 플래그만 내용(묶음 이름)을 읽는다. PlayMode 플래그는 내용을 무시하고 `RunPlayModeTests(전체)`를 부른다. 또 MCP 안에서 `TestRunnerApi.Execute`를 바로 부르면 "User interactions are not supported"로 막혔다. `delayCall`로 미루면 늦게 시작해 그 사이 요청한 플레이 시작과 부딪혔다.
- 다음부터 할 일: PlayMode 일부만 필요하면 Test Runner 창에서 fixture를 골라 돌려 달라고 사용자에게 부탁한다. 아니면 그 시간 동안 다른 편집기 조작을 하지 않는다. 전체 PlayMode는 시간이 오래 걸리고 증거 파일을 바꾸므로 사용자에게 먼저 알리고, 끝나면 `git status`로 `work_process/` 변경을 확인한다.
- 관련: B-211, `2026-11-17-issue306-editmode-flag-filter.md`
