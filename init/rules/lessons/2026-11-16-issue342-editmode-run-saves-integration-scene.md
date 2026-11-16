# EditMode 전체 실행이 통합 씬을 다시 저장해 런타임 UI가 박히고 화면이 하얗게 된다

- 날짜: 2026-11-16
- 태그: scene, test, editmode, builder, hud
- 실수: 씬 좌표 3개만 바꾼 뒤 EditMode 전체를 여러 번 돌리고 씬 diff는 해당 3줄만 grep으로 확인했다. 실제로는 `Mine_Demo_Integration.unity`가 +110,281/−35,401줄로 바뀌어 있었고(시계 숫자·모트·`Stage`·`FxFront` 같은 런타임 UI가 저장됨), 플레이하면 월드 전체가 하얗게 덮였다.
- 원인: `PromptB107QuestUiTests.Scene_UsesQuestFramesWithoutCoveringTheFacilityPanel`이 `PromptB107QuestUiBuilder.Build()`로 통합 씬을 저장한다. 씬이 dirty일 때는 예외로 끝나 무해했지만, 내가 씬을 깨끗하게 저장한 뒤부터는 이 테스트가 끝까지 실행되며 씬을 오염시켰다. 에디터에서 연 씬이나 이전 플레이의 런타임 오브젝트가 함께 직렬화됐다.
- 다음부터 할 일: 씬을 건드린 작업 뒤에는 테스트를 돌릴 때마다 `git diff --stat`로 씬 파일의 총 변경량을 본다(grep으로 내 줄만 보지 않는다). 기대보다 크면 씬을 `git restore`하고 에디터 API로 의도한 값만 다시 적용한다. 플레이 확인은 테스트 실행 뒤에 한다. 플레이 확인 전에는 세이브 폴더를 백업하고 끝나면 원본으로 되돌린다.
- 관련: #342, B-198
