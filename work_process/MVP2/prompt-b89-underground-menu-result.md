# prompt-B 89 작업 결과

## 변경

- 지하 우측 상단 세로 단축키 바에 `설정(Esc)`, `게임 종료(O)` 추가.
- 설정은 지상 SettingsPanel 배치를 복제하고 SurfaceBaseView, SettingsSession, SettingsRuntimeApplier를 재사용한다. 지상 판매·탐사 Binder는 복제본에서 제거한다.
- Esc는 설정창 또는 기존 시설·인벤토리·가이드·드론·업그레이드·시설 상호작용·긴급 구출/탈출 창을 먼저 닫는다. 닫을 창이 없으면 설정을 연다.
- 종료 버튼과 O는 지상과 같은 QuitPolicy → SaveRuntimeController.RequestQuit 경로를 사용한다. TMP 검색 입력 중 O는 종료하지 않는다.
- 긴급 탈출의 IsOpen이 항상 활성인 부모 대신 실제 PanelRoot를 확인하도록 수정했다. 이 수정이 없으면 Esc가 설정을 열지 못한다.

## 파일

프로젝트 `sub-terra/Assets/_Project/` 기준:

- `Scripts/App/Integration/UndergroundMenuController.cs` 및 meta: 설정 세션, 단축키, 창 닫기 우선순위.
- `Editor/DataValidation/PromptB89UndergroundMenuBuilder.cs` 및 meta: Integration 씬만 저장하는 재생성 메뉴.
- `Tests/EditMode/App/UI/PromptB89UndergroundMenuTests.cs` 및 meta: 4개 회귀 테스트.
- `Scripts/App/UI/EmergencyEscape/EmergencyEscapePanelView.cs`, `EmergencyEscapePanelBinder.cs`: 실제 표시 상태 조회.
- `Scenes/App/Mine_Demo_Integration.unity`: 새 버튼 및 UndergroundSettings 배선.

## 검증

- Unity EditMode: 4/4 통과. 결과 `sub-terra/Temp/prompt-b89-editmode.xml`.
- 버튼 영속 리스너 각 1개, 설정·종료 대상 일치, 세로 버튼 비겹침 검증.
- 열린 창 닫기 → 다음 Esc로 설정 열기 → 다음 Esc로 닫기 및 음량 미리듣기 취소 검증.
- 설정 컨트롤 참조와 지상 Binder 제외 확인.
- Bootstrap에서 Play 시작 후 저장 슬롯을 활성화하지 않고 Integration을 로드하는 제한된 스모크 수행. 실제 설정 버튼 클릭으로 창이 열린 것을 확인하고 `sub-terra/Temp/prompt-b89-settings.png` 화면을 검토했다.
- 최종 Unity Console Error/Exception 0건. Inspector의 HUD 참조, 기존 deferred inputs 2개, 새 컨트롤러 참조 6개 확인.
- 씬 오브젝트 ID 비교: 기존 오브젝트 삭제 없음. Unity 자동 변경 폰트와 EditorBuildSettings는 복원했다. 기존 사용자 변경 `init/prompt-B.md`는 유지했다.

## 통합 및 제한

- 씬 배선 완료. 추가 수동 Inspector 연결은 필요 없다.
- 재생성: `SubTerra/UI/Build Prompt-B 89 Underground Menu`.
- 세이브 형식 변경 없음. 커밋/배포 미실행.
- 실제 키보드 입력 및 Windows 실행 파일의 종료·종료 저장까지 완주한 검증은 미실행이다. EditMode의 합성 키 입력은 에디터 입력 처리 때문에 검증되지 않아, Esc 동작은 동일한 HandleEscape 경로로 검증했다.
- 실제 새 게임 저장 슬롯을 이용한 전체 탐사 흐름은 이번 스모크 범위에 포함하지 않았다.
- 씬 YAML은 Unity 직렬화 형식을 유지했다. `git diff --check`의 씬 빈 문자열 trailing whitespace는 Unity 생성 형식이며 C# 변경에는 공백 오류가 없다.
