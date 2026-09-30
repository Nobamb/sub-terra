# Prompt-B 117 지상 기지 UI 작업 결과

`surface-base-concept.png`의 배치 우선순위를 따라 상단 상태 표시, 중앙 탐사 버튼, 판매/업그레이드 보조 버튼, 하단 광산 초기화 버튼으로 정리했다. 기존 탐사·판매·설정·종료·초기화 서비스 경로를 유지하고, 기존 Progression View/Binder를 업그레이드 버튼으로 여는 모달에 연결했다.

## 변경 파일

Unity 경로는 `sub-terra/` 기준이다.

| 구분 | 파일 |
| --- | --- |
| 수정 Prefab | `Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab` |
| 수정 Script | `Assets/_Project/Scripts/App/UI/SurfaceBase/SurfaceBaseView.cs` |
| 수정 Script | `Assets/_Project/Scripts/App/UI/SurfaceBase/SurfaceBaseBinder.cs` |
| 추가 Script | `Assets/_Project/Scripts/App/UI/SurfaceBase/SurfaceBaseButtonFeedback.cs` 및 `.meta` |
| 추가 Builder | `Assets/_Project/Editor/DataValidation/PromptB117SurfaceBaseBuilder.cs` 및 `.meta` |
| 추가 이미지 | `Assets/_Project/Art/UI/SurfaceBase/surface-base-background.png` |
| 추가 이미지 | `Assets/_Project/Art/UI/SurfaceBase/surface-base-top-frame-basic.png` |
| 추가 이미지 | `Assets/_Project/Art/UI/SurfaceBase/Exploration-button.png` |
| 추가 이미지 | `Assets/_Project/Art/UI/SurfaceBase/mine-init-button.png` |
| 이미지 메타데이터 | 위 이미지 4개의 `.meta`, `SurfaceBase.meta` |
| 추가 테스트 | `Assets/_Project/Tests/EditMode/App/UI/PromptB117SurfaceBaseTests.cs` 및 `.meta` |
| 갱신 테스트 | `Assets/_Project/Tests/EditMode/App/UI/MineResetSurfaceBaseTests.cs` |
| 갱신 테스트 | `Assets/_Project/Tests/EditMode/App/UI/PromptB35_2SurfaceSettingsLayoutTests.cs` |
| 갱신 테스트 | `Assets/_Project/Tests/EditMode/App/UI/PromptB42UiLayerTests.cs` |
| 갱신 테스트 | `Assets/_Project/Tests/EditMode/App/UI/PromptBSellPanelLayoutTests.cs` |
| 갱신 테스트 | `Assets/_Project/Tests/EditMode/App/UI/MainMenu/MainMenuStaticStructureTests.cs` |

Scene 수정은 없다. `SurfaceBase.unity`의 기존 Prefab 인스턴스가 변경된 화면을 사용한다. ProjectSettings, Packages, 폰트, Shared 및 Gameplay 파일은 변경하지 않았다. 기존 Prefab GUID는 유지했다.

## 추가 UI와 재사용 에셋

- 추가: `SurfaceBackground`, `TopFrame`, `EnglishTitle`, `CargoText`, `GoldText`, 자원 아이콘 Image.
- 추가 버튼: `UpgradeButton`, `CloseUpgradeButton`.
- 추가 모달: `UpgradeModal`; 기존 `ProgressionPanel`과 서비스 바인더를 그 안에 배치했다.
- 설정/종료/화물/업그레이드 아이콘은 `Art/UI/Gameplay/SideMenu/`의 기존 에셋을 재사용했다.
- 골드/판매 아이콘은 기존 `Art/FX/gold_coin_01.png`를 재사용했다.
- 판매/업그레이드 및 설정/종료 버튼 프레임은 기존 Settings 버튼의 기본/호버 에셋을 재사용했다.
- 새로 생성한 래스터 아이콘은 없다. 지정된 PNG 4개를 원본 그대로 Assets에 복사했다.
- 기존 목표·전력·심층·최근 탐사 요약은 기본 화면에서 숨겼다. 연결된 데이터 계산과 탐사 전력 검증은 유지된다.

## 버튼 기능

| 버튼 | 연결 기능 |
| --- | --- |
| 지하 탐사 시작 | 기존 `SurfaceBasePresenter` → `SaveRuntimeController.TryStartExploration` |
| 자원 판매 | 기존 `EconomyPanelView` 판매 모달, 선택/수량/판매 서비스 |
| 업그레이드 | 기존 `ProgressionPanelBinder/Presenter`의 장비 선택·구매 모달 열기 |
| 업그레이드 창 X | 업그레이드 모달 닫기. 키보드 Esc/X 닫기도 연결 |
| 업그레이드 창 구매 | 기존 `ProgressionPanelBinder.PurchaseSelected` |
| 새 광산 초기화 | 기존 비용 견적·확인 모달·`TryResetMine` 경로 |
| 설정 | 기존 SettingsSession 설정창. 업그레이드 창을 닫은 뒤 표시 |
| 종료 | 기존 QuitPolicy·저장 런타임의 종료 요청 |

화물/골드는 `GameState.InventoryChanged`와 `CreditsChanged`에 구독하며, 화면 종료 때 구독을 해제한다. UI에서 경제 상태를 변경하지 않는다. 초기화 비용은 기존 런타임 견적 라벨을 사용한다.

## 상태 연출

- 주요 버튼 6개와 업그레이드 모달 닫기/구매 버튼에 `SurfaceBaseButtonFeedback` 적용.
- 기본 프레임 위에 호버 프레임을 0.2초 동안 페이드한다.
- 포인터 호버 및 선택 포커스에 같은 강조를 적용한다.
- 마스크 내부에서 약한 빛이 1.8초 주기로 이동한다.
- 비활성 버튼은 프레임을 어둡게 하고 호버 강조를 해제한다.
- `Time.unscaledDeltaTime`을 사용해 게임 시간 배율의 영향을 받지 않는다.
- 장식 Image/TMP는 레이캐스트를 받지 않는다.

## 검증

- Unity Test Runner EditMode: **29/29 통과**, 실패/스킵 0.
- 테스트 결과: 저장소 `Temp/PromptB117/editmode-results.xml`.
- 판매 선택/수량/정산 회귀, 설정 드롭다운, 초기화 UI 연결, 자원 표시, 업그레이드 닫기, 모달 우선순위 검증.
- Bootstrap 재생 → 서비스 준비 → SurfaceBase 로드에서 기지/Progression Binder의 실제 바인딩 확인.
- 실제 업그레이드 버튼으로 창을 열고 드릴 선택 시 설명·필요 재료·구매 가능 상태 표시 확인.
- 주요 버튼 6개 위치에서 EventSystem 레이캐스트의 첫 대상이 해당 버튼임을 확인.
- 판매창 및 설정창 열기 확인. 호버 강조 1, 비활성 시 강조 0 확인.
- 1920×1080, 3440×1440, 1440×1080 렌더링 확인. 배경 비율 보존 및 주요 요소 화면 내 배치 확인.
- 지정 이미지 Sprite 참조 4개 정상, Missing Script 0, 필수 기존 View 참조 정상.
- 최종 Unity Console Error/Exception 0.
- `git status`, 관련 diff, Script/Test의 `git diff --check` 확인. 전체 diff 검사에서 Unity가 직렬화한 빈 `m_Name`의 공백과 사용자가 수정한 프롬프트의 줄 끝 공백이 표시되며, YAML을 수작업으로 정리하지 않았다.

## 재적용 및 추가 확인

재적용 메뉴: `SubTerra/UI/Build Prompt-B 117 Surface Base`. 이 빌더는 해당 Prefab과 지정 이미지 폴더만 저장한다. 과거 레이아웃 빌더를 실행하면 구 디자인으로 돌아갈 수 있으므로, 새 디자인에는 117 빌더를 마지막에 적용한다.

검증은 Bootstrap부터 시작해야 한다. SurfaceBase 씬 단독 재생에는 서비스 런타임이 없다.

이번 작업에서 Windows 빌드와 실제 세이브 슬롯을 사용하는 탐사/초기화/종료의 전체 흐름은 실행하지 않았다. 해당 기능 연결을 유지했으며, 판매 정산은 독립 테스트 상태에서 검증했다. 일반 게임 흐름의 저장 결과는 기존 통합 QA 절차로 확인할 수 있다.

캡처: 저장소 `Temp/PromptB117/`의 `surface-base-1920.png`, `surface-base-3440.png`, `surface-base-1440.png`, `surface-base-upgrade-bound.png`.
