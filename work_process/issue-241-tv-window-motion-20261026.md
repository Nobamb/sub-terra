# #241 창 TV 열림·역순 닫힘 — 2026-10-26

## 시작 상태와 중복 작업 제외

- 최신 origin/main `8305ee2c`에서 `codex/issue-241-tv-window-lifecycle` 브랜치를 만들었다. 첫 번째 Hub 프로젝트 `C:/Users/jeone/sub-terra/sub-terra`, Unity 6000.5.4f1, 실제 플레이 시작은 Bootstrap이다.
- 10월 22일 인계서의 #211과 #240은 데스크탑에서 이미 각각 기존 PR #233과 새 PR #247에 구현·게시됐다. #245도 PR #246에서 처리됐다. 해당 보관 패치/stash를 재적용하거나 같은 버그를 다시 구현하지 않는다.
- #243 오디오 음량 분리는 10월 26일 조회 시 완료/닫힘 상태다. #158은 팀원의 효과음 작업이므로 이번 범위에 섞지 않는다. #242 걷기 역시 검수 대기 상태를 유지한다.
- 이번 범위는 열린 이슈 #241의 건설창·인벤토리·지상/지하 공용 퀘스트 상세창뿐이다. 보상창·설정창·다른 팝업은 변경하지 않는다.

## 기존 코드에서 확인한 부분

- TerminalWindowOpenMotion은 가로선→펼침만 재생하며 Stop에서 즉시 크기를 복구한다.
- 세 View는 닫기 요청 즉시 오브젝트를 끈다. HUD의 건설창 경로도 View 호출 전에 루트를 꺼서, View만 바꿔도 닫기 연출이 재생되지 않는다.
- 기존 프리팹과 Canvas 기반 View 구조를 유지하고 스크립트 연결만 수정한다. 씬·프리팹 YAML 수정이나 빌더 재실행은 필요하지 않다.

## 구현

- 열기 0.18초: 중앙 빛점과 부드러운 청록빛 → 가로선 확장 → 세로 펼침과 본문 표시.
- 닫기 최대 0.16초: 본문 접힘/페이드 → 가로선 축소 → 마지막 빛점 소멸 후 View가 숨김 처리.
- 같은 진행도를 앞뒤로 움직인다. 열다가 닫기/닫다가 다시 열기는 현재 크기에서 방향만 바꾸며, 취소된 닫기 콜백은 실행하지 않는다. 중복 닫기는 타이머나 콜백을 추가하지 않는다.
- 닫는 동안 루트 CanvasGroup.interactable=false로 버튼을 막고 기존 blocksRaycasts 값은 유지한다. 연출 완료/비활성화 때 원래 크기·중심 보정·alpha·입력 값을 복구한다.
- 이미 표시된 창의 내용 갱신과 퀘스트 페이지 이동은 재시작하지 않는다. Time.unscaledDeltaTime을 사용하며 모션 감소 설정은 즉시 처리하고, 연출 도중 설정을 켜도 정상 종료한다.
- 빛점은 CoreCctvArt.Dot/Soft, 가로선은 기존 CRT 스프라이트를 재사용한다. 새 이미지·음원·패키지는 없다. 본문이 완전히 접히거나 투명해져도 빛이 보이도록 효과를 형제 노드에 두고 창의 중심/정렬 순서를 따른다. 부모 CanvasGroup 게이트는 공유한다. 효과 노드는 재사용하고 소유 창 파괴 시 정리한다.
- HUD는 논리적 닫힘/설치 선택 취소를 기존 시점에 처리하고, 시각 루트 숨김만 연출 완료까지 미룬다. 인벤토리 Binder 유지를 위한 루트 활성 정책도 유지한다. 효과음 호출을 추가하거나 중복시키지 않는다.

## 변경 파일 (Assets/_Project 기준)

- Scripts/App/UI/TerminalWindowOpenMotion.cs
- Scripts/App/UI/Building/BuildingMenuView.cs
- Scripts/App/UI/Inventory/InventoryPanelView.cs
- Scripts/App/UI/Tutorial/DemoObjectiveView.cs
- Scripts/App/UI/HUD/HudPanelChromeController.cs
- Tests/PlayMode/Integration/Bootstrap/Issue213WindowOpenMotionTests.cs: 기존 3건을 닫힘 완료 시점에 맞추고 순서/역전/입력 차단/HUD 연결 검사 2건 추가.
- Tests/PlayMode/Integration/Bootstrap/PromptB112InventoryPanelPlayModeTests.cs: 닫힘 완료 뒤 숨김 여부를 확인하도록 대기만 추가. 광범위한 실제 씬 검사를 반복 실행하려는 변경은 아니다.

## 최소 검사와 한계

- 대상: Issue213WindowOpenMotionTests의 PlayMode 5건만 선택. 실제 BuildingMenu/InventoryPanel/SurfaceBasePanel 프리팹의 연결, 일시정지, 연타·역전, 빛점/선/본문 순서, 닫기 중 버튼 차단, 모션 감소, 부모 비활성화와 HUD의 지연 숨김을 확인한다.
- 첫 실행 PlayMode 5/5 통과, 실패·건너뜀 0, 4.97초, 종료 코드 0. C# 컴파일 오류·검사 중 예외 없음. 결과 경로: work_process/tools/issue241-tv-motion-20261026/. 최신 main의 에셋 가져오기와 첫 컴파일에는 시간이 걸렸지만 이후 검사를 반복하거나 Windows 빌드를 추가하지 않았다.
- **미검증**: 실제 렌더 화면의 밝기·위치·CCTV 팝업과의 느낌 비교·실제 B/I/G 조작·Windows exe. 자동 검사만으로 이슈의 화면 품질까지 완료 판정하지 않는다.
- 수동 체크리스트: Bootstrap → 탐사 후 B/I와 지상·지하 퀘스트 상세를 열어 중앙 점→선→펼침을 확인한다. 닫을 때 선과 점을 거쳐 사라지는지, 연타/재열기에서 크기가 튀지 않는지, 닫는 동안 버튼이 동작하지 않는지, 모션 감소 시 즉시 처리되는지 확인한다. 실제 효과음 시점도 함께 확인한다.
- ProjectSettings·Packages·폰트·씬·프리팹·게임플레이·저장 형식 변경 없음. 사용자 커밋·푸시·PR 게시 승인을 받았다. 새 이슈이므로 독립 PR로 분리하며 위 화면 미검증 상태를 PR에 명시한다.
