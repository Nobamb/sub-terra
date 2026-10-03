# 이슈 #119 — 새 게임 덮어쓰기 확인 팝업 결과

현재 프리팹, Phase L 빌더, 설정창, 퀘스트 클리어창, B-120 시작 브리핑을 기준으로 수정했다. 작업 전 덮어쓰기 전용 리워크 문서가 없는 것을 확인했으며, 이 파일은 새 디자인 요구사항이 아닌 작업 결과 기록이다.

## 변경 파일

Unity 프로젝트 루트 기준:

- `Assets/_Project/Prefabs/UI/MainMenuPanel.prefab`: OverwriteConfirm만 개편.
- `Assets/_Project/Editor/DataValidation/PhaseLMenuSceneBuilder.cs`: 기존 생성 경로에 새 레이아웃 적용. 덮어쓰기 창만 갱신하는 메뉴 추가.
- `Assets/_Project/Editor/DataValidation/PhaseLTestRunner.cs`: 전용 Play Mode 실행 메뉴 및 테스트 후 시작 씬 설정 복원.
- `Assets/_Project/Scripts/App/UI/MainMenu/MainMenuView.cs`: X 버튼을 기존 취소 이벤트에 연결하고 비활성화 시 해제.
- `Assets/_Project/Scripts/App/UI/MainMenu/MainMenuBinder.cs`: 새 게임 전환 중 중복 시작 차단. 시작 실패 시 재시도 허용.
- `Assets/_Project/Tests/EditMode/App/UI/MainMenu/MainMenuStaticStructureTests.cs`: Prompt16 고정값 갱신 및 두 화면 비율의 프레임 내부 배치·본문 넘침 검사.
- `Assets/_Project/Tests/EditMode/App/UI/MainMenu/MainMenuLogicTests.cs`: 확인 게이트 단일 소비와 대기 슬롯 보존 검사.
- `Assets/_Project/Tests/PlayMode/Integration/Bootstrap/Issue119OverwritePopupPlayModeTests.cs`, `.meta`: Bootstrap부터 실행하는 임시 저장소 기반 UI·저장·전환 검사.

`Assets/_Project/Scenes/App/MainMenu.unity`는 수정하지 않았다. 이 씬은 덮어쓰기 레이아웃 오버라이드 없이 프리팹을 상속한다. Editor API로 씬을 열어 새 프레임, 720×400 배치, X 버튼 직렬화 참조를 확인했다.

## 레이아웃과 재사용 에셋

- 원본 프레임의 투명 여백을 포함한 RectTransform은 720×400. 실제 보이는 창은 약 662×292로 작은 확인창을 유지.
- 제목은 기존 `덮어쓰기?`를 재사용. 제목과 46×46 정사각형 X는 y=105에서 정렬하고 금속 모서리 장식 안쪽에 배치.
- 본문은 `슬롯 {n} 세이브를 덮어쓰시겠습니까?`를 유지. TMP 23.4, 내부 여백, 한 줄 표시.
- 확인·취소는 각각 184×50, x=−112/+112, y=−104. 가로 중앙을 기준으로 대칭.
- 확인·취소 호버는 기존 MenuSpriteButtonSkin의 0.2초 이미지 전환. 확대·글리치·추가 테두리 효과 없음.
- X는 설정창과 동일한 이미지와 0.15초 호버 전환 사용.

재사용한 스프라이트:

- `Assets/_Project/Art/UI/Gameplay/Quest/Clear/quest-clear-popup-frame.png`
- `Assets/_Project/Art/UI/MainMenu/Settings/button-active-off.png`
- `Assets/_Project/Art/UI/MainMenu/Settings/button-active-on.png`
- `Assets/_Project/Art/UI/MainMenu/Settings/setting-close-normal.png`
- `Assets/_Project/Art/UI/MainMenu/Settings/setting-close-hover.png`

새 컨셉 이미지·게임용 스프라이트·폰트는 만들지 않았다.

## 기존 동작 연결

- 확인: MainMenuView → MainMenuBinder → MainMenuPresenter.ConfirmOverwriteNewGame → NewGameOverwriteGate → SaveRuntimeController.StartNewGame(confirmOverwrite: true).
- 취소·X 버튼: 동일한 OverwriteCancelClicked → CancelOverwriteNewGame. 창과 대기 게이트만 닫고 세이브·런타임 상태 보존.
- X키: 기존 MainMenuBinder 처리 유지. 설정창 및 조작 설정창 닫기가 우선하며, 이후 PopupWindowSorting의 덮어쓰기 창을 닫음.
- 빈 슬롯: 기존 RequestNewGame 경로로 확인창 없이 바로 시작.
- PopupWindowDrag 유지. 버튼 위에서는 추적을 시작하지 않으며, 2px 마우스 이동을 포함한 취소·X 클릭이 정상 동작함.

## 검증 결과

2026-10-03, Unity 6000.5.4f1:

- Edit Mode 19개 통과, 실패 0개.
- Play Mode 3개 통과, 실패 0개. 최종 NUnit XML도 total=3, passed=3, failed=0.
- 1920×1080과 1280×1024에서 점유 슬롯 팝업, 빈 슬롯의 팝업 없는 시작 검사.
- 본문 넘침·줄바꿈 없음, 제목/X 정렬, 버튼 크기·대칭·프레임 내부 배치 확인.
- 확인 연속 호출 시 새 게임 이벤트와 SurfaceBase 씬 로드 각 1회, 실제 임시 세이브 갱신 확인.
- 빈 슬롯 새 게임 연타도 두 비율에서 각 1회만 시작.
- 취소·X 버튼·X키 시 임시 세이브 바이트와 런타임 State 참조 보존.
- 설정창 정렬이 덮어쓰기 창보다 높고, X키로 설정창 → 덮어쓰기 창 순서로 닫힘.
- 전용 빌더 재실행 전후 프리팹 바이트가 동일함. Phase L의 최초 프리팹 생성 경로도 같은 레이아웃 함수를 호출함.
- 최종 Unity Console Error/Exception 0개.
- 테스트가 만든 임시 씬을 제거하고 EditorBuildSettings를 원복. Bootstrap 시작 설정과 에디터 씬 복원.
- 사용자 기존 변경인 `init/prompt-B.md`는 보존. 다른 프리팹·씬·폰트·패키지·프로젝트 설정 변경 없음.

화면 캡처:

- [1920×1080](evidence/issue119-overwrite-1920x1080.png)
- [1280×1024](evidence/issue119-overwrite-1280x1024.png)

재실행 메뉴:

- `SubTerra/UI/Build Main Menu Overwrite Confirm Layout`
- `SubTerra/Tests/Run Main Menu Overwrite Popup Play Mode`

## 검증 제한

Windows 독립 실행 빌드는 이번 작업에서 실행하지 않았다. 입력 검사는 Unity Input System의 마우스·키보드 이벤트 주입으로 수행했다. 실제 사용자 세이브는 테스트에 사용하거나 변경하지 않았다. 다른 UI를 재생성하는 전체 Phase L Build는 실행하지 않고, 덮어쓰기 전용 경로의 반복 실행을 검증했다.
