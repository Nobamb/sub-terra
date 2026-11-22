# 플레이 확인을 스크립트로 이어하기할 때 동기 Continue()를 쓰면 HUD가 영영 안 켜진다

- 날짜: 2026-11-22
- 태그: playmode, mcp, save, continue, testing, issue266
- 실수: B-208 플레이 확인에서 MCP로 `SaveRuntimeController.Continue(1)`을 불러 광산에 들어갔다. `Integration UI kept disabled: IsUiReady never became true` 경고가 나고 플레이어가 공중 상태로 남았으며 수동 저장도 써지지 않았다. 처음엔 성별 변경 탓인지 의심했다.
- 원인: 동기 `Continue()`는 씬 활성화를 기다리지 않는 옛 경로라 `uiReady`를 켜지 않는다. 메인 메뉴가 실제로 쓰는 것은 `BeginContinue(slot, callback)`(씬 로드 → 월드 복원 → `uiReady = true`)이다.
- 다음부터 할 일: 스크립트로 이어하기를 재현할 때는 `BeginContinue`를 쓰고, 다음 호출에서 `IsUiReady`가 true인지 먼저 확인한 뒤 기능을 본다. 플레이 전 세이브 폴더를 백업하고 끝나면 바이트 비교로 되돌린다. 또 숨겨진 UI(`activeInHierarchy=false`)는 `Update`가 돌지 않으니 상태 변화 반영을 `OnEnable`에서도 확인한다.
- 관련: #266, B-208, `2026-11-16-issue342-editmode-run-saves-integration-scene.md`
