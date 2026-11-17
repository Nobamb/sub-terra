# 편집기 EditMode 전체 실행이 씬·프리팹을 다시 저장한다 — 플래그에 fixture 이름을 넣어 일부만 돌린다
- 날짜: 2026-11-17
- 태그: test, editmode, scene, prefab, workflow
- 실수: 채굴 코드만 바꾸고 `Temp/subterra-run-editmode.flag`를 빈 내용으로 만들어 EditMode 전체를 실행했다. 끝나고 보니 `Mine_Demo_Integration.unity`, UI 프리팹 4개, `NotoSansKR-Regular_SDF.asset`, `TimeManager.asset`, 이미지 2장이 dirty가 되어 있었다.
- 원인: 전체 실행에 포함된 빌더·씬 저장 테스트가 에셋을 다시 저장한다(#342와 같은 경로).
- 다음부터 할 일: 플래그 파일 내용에 `네임스페이스.Fixture` 접두를 넣으면(`SubTerra.App.Tests.EditMode.Readiness` 등) 그 그룹만 실행된다. 실행 뒤에는 `git status`로 의도하지 않은 에셋 변경을 확인해 `git restore`한다. 새 테스트가 안 보이면 Console 오류와 `Library/ScriptAssemblies/*.dll` 시각을 확인하고 `Temp/subterra-refresh-assets.flag`로 컴파일을 유도한다. 기존 실패가 내 변경 때문인지는 변경을 `git stash push -u -- <경로>`로 잠시 빼고 같은 fixture를 돌려 비교한다.
- 관련: #306, B-202, #342
