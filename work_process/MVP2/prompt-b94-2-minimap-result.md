# prompt-B 94-2 작업 결과

## 변경 내용

- 미니맵 투명도를 Graphic.color 틴트 대신 `CanvasGroup.alpha`로 고정했다. 지상·지하 모두 기본 50%이며, 지하 암전 오버레이 바로 위에 그려 지하에서만 더 어두워지지 않게 했다.
- 미니맵 하단에 `닫기: M` 안내를 표시한다. 숨긴 상태에서는 투명도 0으로 안내도 함께 사라진다.
- `Ctrl + M`을 누르고 있는 동안에만 불투명(100%)으로 올린다. 이 입력은 맵 켜기/끄기를 토글하지 않는다. 손을 떼면 다시 50%다.
- 게임 가이드 기본 조작 탭에 미니맵 단축키를 추가했다.

## 파일

`sub-terra/Assets/_Project/` 기준:

- `Scripts/App/Integration/ExplorationMinimap.cs`: CanvasGroup 투명도, 닫기 안내, Ctrl+M 홀드, 암전 오버레이 위 배치.
- `Scripts/App/Integration/IntegrationRuntimeBinder.cs`: 생성 시 CanvasGroup 포함. 형제 순서는 미니맵이 스스로 맞춘다.
- `Scripts/App/UI/HUD/GameGuidePanelView.cs`: 기본 조작 탭에 M / Ctrl+M 안내.
- `Tests/EditMode/App/UI/PromptB94MinimapTests.cs`: 투명도, 안내, Ctrl+M, 가이드 문구 검증.
- `Editor/DataValidation/PromptB94MinimapTestRunner.cs`: EditMode 전용 실행 메뉴·플래그.

## 검증 및 연결

- Unity EditMode 9개 통과. 결과: `sub-terra/Temp/prompt-b94-editmode-results.txt`.
- Unity 컴파일 오류 없음. 기존 obsolete API 경고는 별개로 남아 있다.
- Scene, Prefab, 폰트 에셋은 변경하지 않았다. 가이드 본문은 런타임 문자열이다.
- 게임 세이브 형식은 변경하지 않는다. Inspector 추가 배선은 없다.
- Bootstrap부터 실제 플레이 완주 및 Windows 빌드 검증은 수행하지 않았다.
- 사용자가 수정한 `init/prompt-B.md`는 그대로 유지했다. 커밋·배포는 수행하지 않았다.
