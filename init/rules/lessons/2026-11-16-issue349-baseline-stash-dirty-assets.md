# 베이스라인 비교용 stash 뒤 pop이 테스트가 더럽힌 에셋 때문에 충돌한다

- 날짜: 2026-11-16
- 태그: testing, git, editmode, scene, issue349
- 실수: 기존 실패와 새 실패를 가르려고 `git stash push -u`로 내 변경을 치운 채 EditMode 전체를 돌린 뒤 `git stash pop`을 했더니, 테스트가 다시 저장한 `Mine_Demo_Integration.unity`·`SurfaceBasePanel.prefab`·폰트 SDF 등과 충돌해 pop이 중간에 멈췄다(추적 안 된 새 파일만 먼저 복원됨).
- 원인: EditMode 전체 실행이 씬·프리팹·폰트·ProjectSettings를 다시 저장한다(#342 교훈과 같은 뿌리). stash 안에도 이전 실행이 더럽힌 파일이 섞여 있었다.
- 다음부터 할 일: 베이스라인 실행 뒤 pop 전에 테스트가 더럽힌 추적 파일을 `git checkout HEAD -- <파일>`로 되돌린다. 내 변경은 stash 전에 `git diff`/추적 안 된 파일 목록을 스크래치에 백업하고, pop이 실패하면 `git checkout stash@{0} -- <내 파일만>`으로 골라 복원한다(씬·프리팹·폰트는 복원하지 않는다). 가능하면 stash 대신 별도 worktree에서 베이스라인을 돌린다.
- 관련: #349, B-199, 2026-11-16-issue342-editmode-run-saves-integration-scene.md
