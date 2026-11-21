# 편집기가 연 채로 새 스크립트 여러 개를 연달아 만들면 하나가 컴파일 목록에서 빠질 수 있다
- 날짜: 2026-11-21
- 태그: editor, compilation, mcp, tooling, issue359
- 실수: 편집기를 연 채 새 `.cs` 세 개를 몇 초 간격으로 만들었는데, 그중 `DamageHitGlitchOverlay.cs`만 `SubTerra.App` 컴파일에서 빠졌다. 참조하던 `DamageHitGlitchController`가 CS0246으로 계속 실패했고, `ImportAsset(ForceUpdate)`, `Refresh(ForceUpdate)`, 정리 빌드를 다시 요청해도 고쳐지지 않았다.
- 원인: 첫 파일 저장으로 자동 새로 고침·컴파일이 시작된 사이에 다음 파일이 들어왔다. 그 파일은 MonoScript로는 가져왔지만(`GetClass()`는 null) Bee 소스 목록(`Library/Bee/artifacts/*/SubTerra.App.rsp`)에는 들어가지 않았다. 컴파일이 실패하는 동안에는 목록이 다시 만들어지지 않아 같은 상태가 반복됐다.
- 다음부터 할 일: 새 타입이 "찾을 수 없음"이면 먼저 `SubTerra.App.rsp`에 그 파일이 있는지 확인한다. 없으면 백업한 뒤 `.cs`와 `.meta`를 지우고 Refresh하고, 다시 넣고 Refresh한다(`AssetDatabase.DeleteAsset`은 MCP에서 확인 창 때문에 실패한다). 새 파일 여러 개는 다 쓴 뒤 Refresh를 한 번만 한다.
- 관련: #359, B-207
