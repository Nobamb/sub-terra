# Prompt-B 103 — 메인 메뉴 디자인 수정

## 구현 결과

- 폐광·전초기지와 봉인된 청록 신호를 표현하는 단일 배경을 생성했다. 이미지에는 UI와 글자를 넣지 않았다.
- 기존 MainMenuPanel의 제목, 슬롯 3개, 버튼 4개를 유지하며 탐사 기록 카드로 재배치했다.
- `지금은 40미터다.`, `봉인 너머에서 신호가 온다.` 문구를 추가했다.
- 슬롯은 번호·Gold·Depth·마지막 저장 시간을 표시한다. 빈 슬롯과 손상 슬롯의 상태도 구분한다.
- 선택 슬롯은 청록 테두리·화살표·밝기 증가와 0.2초 전환으로 강조한다. 버튼은 짧은 색상 전환을 사용한다.
- 배경의 청록 채널을 0.94~1.00 사이에서 천천히 변화시킨다. 먼지·안개·패럴랙스 레이어는 추가하지 않았다.
- 배경은 비율 유지/중앙 크롭, 메뉴 콘텐츠는 화면 높이에 맞춰 축소한다.

## 변경 파일

Unity 프로젝트 기준 경로다. 신규 Unity 에셋과 C#에는 Editor가 생성한 `.meta`가 포함된다.

| 분류 | 파일 |
| --- | --- |
| 생성 이미지 | `Assets/_Project/Art/UI/MainMenu/MainMenu_Background.png` |
| 수정 Prefab | `Assets/_Project/Prefabs/UI/MainMenuPanel.prefab` |
| 수정 Scene | 없음. MainMenu 씬의 기존 프리팹 인스턴스가 변경을 상속한다. |
| 수정 Script | `Scripts/App/UI/MainMenu/MainMenuView.cs`, `MainMenuPresenter.cs` |
| 수정 Script | `Scripts/App/Save/SaveService.cs`, `SaveRuntimeController.cs` |
| 신규 Script | `Scripts/App/UI/MainMenu/SaveSlotCardView.cs`, `MenuSignalPulse.cs` |
| 신규 Script | `Scripts/App/Save/SaveThumbnailService.cs` |
| 신규 Editor | `Editor/DataValidation/PromptB103MainMenuBuilder.cs` |
| 수정 테스트 | `Tests/EditMode/App/Save/SaveServiceTests.cs` |
| 수정 테스트 | `Tests/EditMode/App/UI/MainMenu/MainMenuStaticStructureTests.cs` |

Script/Editor/Tests 경로는 모두 `Assets/_Project/` 아래에 있다.

기존 메인 메뉴 Binder, 설정 팝업, 덮어쓰기 확인창과 폰트를 재사용했다. 기존 Prefab 객체 삭제는 0개이며 Settings/Overwrite 자식 객체의 직렬화 데이터는 유지된다. SurfaceBase/Integration 씬·공유 폰트·ProjectSettings는 수정하지 않았다.

## 썸네일과 저장 안정성

- 저장 위치: 기존 세이브 루트의 `save_slot_{1~3}_thumbnail.png`.
- 기본 루트는 `Application.persistentDataPath`. 개발 실행의 `-subterra-save-root`도 기존 SavePathPolicy를 공유한다.
- 규격: 512×288 PNG. JSON에 이미지 데이터를 넣지 않는다. Save Schema v5 유지.
- 수동 저장/자동 저장 모두 SaveService의 최종 파일 승격 성공 이후 `Saved` 이벤트를 발생시킨다.
- 광산 Integration 카메라가 있을 때만 실제 월드 화면을 캡처한다. 지상/메뉴 저장은 마지막 탐사 이미지를 유지한다.
- 새 게임의 최초 저장 성공 후에는 이전 플레이의 썸네일을 삭제한다.
- 임시 PNG를 쓴 후 교체하며, 저장 실패 시 썸네일 콜백을 호출하지 않는다.
- 이미지 누락·손상·로드 실패는 `NO VISUAL RECORD`로 표시한다. 이어하기 자격은 기존 LoadService 결과만으로 판단한다.
- 메뉴에서는 슬롯별 이미지를 한 번 디코딩하며 메뉴 객체 파괴 시 Texture2D를 해제한다.

## 검증

`editmode-results.xml`: **37개 통과, 실패 0개**.

- 기존 메뉴 로직: 슬롯 선택, 이어하기 자격, 새 게임 덮어쓰기 확인, 설정, 종료 정책.
- MainMenu 프리팹 참조와 씬의 EventSystem, 기존 확인창 구조.
- 기존 세이브 라운드트립·백업·실패 처리·자동 저장 테스트.
- 썸네일 성공 후 저장, 실패 시 보존, 슬롯 분리, 손상/누락 상태의 정상 로드.
- 실제 RenderPipeline 캡처의 이미지 갱신과 카메라 targetTexture/aspect/RenderTexture.active 복원.
- 1920×1080, 2560×1440, 1366×768, 2560×1080 콘텐츠 경계와 장식의 raycast 설정.

Bootstrap에서 Play 후 메인 메뉴 Binder 연결, 슬롯 선택, 선택 강조가 한 슬롯에만 남는 상태, 설정 열기, 새 게임 확인창과 취소를 확인했다. 실제 메뉴 텍스트 overflow는 0개였다.

광산 씬을 편집 모드로 일시 로드해 실제 Main Camera의 512×288 캡처를 검증했다. `capture-check/save_slot_1_thumbnail.png`에서 플레이어·드론·광산과 Overlay HUD 제외를 확인했다. 해당 씬은 저장하지 않았다.

## 스크린샷

- `main-menu-final.png`: Bootstrap에서 실행한 실제 메뉴. 기존 세이브에는 아직 썸네일이 없어 대체 표시가 보인다.
- `settings.png`, `overwrite-confirm.png`: 기존 팝업이 메뉴 위에서 표시되는 실행 화면.
- `layout-1920x1080.png`, `layout-2560x1440.png`, `layout-1366x768.png`, `layout-2560x1080.png`: Unity Preview Scene에서 같은 프리팹을 해상도별 렌더한 배치 검증 이미지. 세이브 데이터를 주입하지 않은 정적 프리뷰다.
- `capture-check/save_slot_1_thumbnail.png`: 실제 광산 카메라의 편집 모드 캡처.

## 통합 방법 및 검증 제한

- 이미 MainMenuPanel.prefab에 참조와 새 컴포넌트가 연결되어 있다. 별도 Inspector 작업은 필요 없다.
- 재적용 메뉴: `SubTerra/UI/Build Prompt-B 103 Main Menu`. 이 빌더는 MainMenuPanel.prefab만 저장한다.
- 기존 세이브의 이미지는 다음 광산 저장 성공 시 생성된다. 썸네일이 없는 동안에도 이어하기는 정상이다.
- 실제 사용자 세이브를 덮어쓰는 새 게임 확정이나 플레이 진행을 통한 저장 왕복은 실행하지 않았다. 관련 정책·저장 성공 연결·캡처·파일 로드는 독립된 테스트로 검증했다.
- 전체 게임 테스트와 Windows 빌드는 실행하지 않았다.
- 광산 편집 모드 캡처 중 기존 `PowerConnectionText`의 U+2715 글리프 누락 경고가 발생했다. 메인 메뉴 작업 범위 밖이므로 공용 폰트는 변경하지 않았다.
- 개발 도구 검증 코드에서 발생한 일시적 오류와 별개로 최종 C# 컴파일 및 37개 테스트는 통과했다.
- C# `git diff --check`는 통과했다. Unity가 직렬화한 Prefab의 빈 YAML 값 뒤 공백은 기본 출력으로 유지했다.

## 이미지 생성

내장 image_gen 도구를 사용했다. 반환된 배경의 원본 크기는 1672×941이며 Unity에서 비율을 유지해 표시한다. 2560×1440 렌더에서 확대 품질을 확인했다. 최종 배경은 프로젝트 Assets에 복사했으며, 생성 프롬프트는 `image-prompt.md`에 기록했다.
