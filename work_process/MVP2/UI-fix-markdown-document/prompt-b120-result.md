# Prompt-B 120 — 시작 브리핑 UI 개편 결과

## 기존 구조 조사

- 시작 브리핑은 `DemoObjectiveView`의 `GuidancePanel`(제목 "생존자 브리핑")이다. 첫 퀘스트(`demo.quest.mine_block`)의 `ShowsDismissibleGuidance`가 켜져 있을 때 `DemoObjectivePresenter.Render`가 연다. 닫기는 버튼 → `DemoObjectiveView.OnDismissClicked` → `TutorialDirectorBinder` → `DemoObjectivePresenter.DismissGuidance` 순서다.
- 표시 조건과 세이브 상태는 바꾸지 않았다. 별도의 "확인 완료" 저장 필드는 없고, 첫 퀘스트가 진행 중인 세이브를 이어하면 다시 열리는 기존 동작 그대로다. 같은 세션 안에서는 닫은 뒤 진행 이벤트로 다시 뜨지 않도록 Presenter가 닫은 목표 ID를 기억한다.
- 기존에는 일시정지·입력 차단이 없었다. 광산 초기화 타이머는 `Time.unscaledDeltaTime`으로 누적되어 `timeScale`로는 멈추지 않는다.

## 변경 파일

| 구분 | 파일 |
| --- | --- |
| 신규 | `Scripts/App/UI/Tutorial/StartBriefingPopupMotion.cs` (연출 MonoBehaviour) |
| 신규 | `Scripts/App/UI/Tutorial/StartBriefingTimeline.cs` (시간표·강도 곡선·글리치 플래너·상태 전이, 순수 C#) |
| 신규 | `Scripts/App/UI/UiPauseGate.cs` (정지 소유권 목록, 해제 시 자기 정지만 복원) |
| 신규 | `Scripts/App/Integration/PauseInputSuspender.cs` (정지 중 월드 입력 컴포넌트 비활성) |
| 신규 | `Editor/DataValidation/PromptB120StartBriefingBuilder.cs` (`SubTerra/UI/Build Prompt-B 120 Start Briefing`) |
| 신규 | `Tests/EditMode/App/Tutorial/PromptB120StartBriefingTests.cs` |
| 수정 | `DemoObjectiveView` / `DemoObjectivePresenter` (닫기 연출 뒤 종료 처리, X 단축키, 재표시 방지) |
| 수정 | `DemoObjectiveCatalog` (확정 본문·버튼 문구), `UiLayerPriority` (브리핑 정렬 32,700) |
| 수정 | `SaveRuntimeController` (정지 중 광산 초기화 타이머 누적 중단) |
| 수정 | 키 입력 처리 `PanelToggleController`, `HudPanelChromeController`, `GameplaySideMenuController`, `MineResetClockOverlay`, `ExplorationMinimap`, `EmergencyRescueRuntimeController`, `GameplayBuildingPlacementBridge`, `UndergroundMenuController` (정지 중 무시, X는 브리핑 닫기) |
| 수정 | `IntegrationRuntimeBinder` (`PauseInputSuspender` 부착) |
| 수정 | `DemoObjectiveTransitionTests` (옛 본문 단언을 확정 본문 기준으로 변경) |
| Scene | `Mine_Demo_Integration.unity` — `DemoObjectiveRoot/GuidancePanel` 하위만 재구성 (씬 커밋은 기능 커밋과 분리 권장) |

`GuidancePanel`의 `PopupWindowDrag`/`PopupWindowRaycastGate`는 제거했다(전체 화면 모달이라 드래그 대상이 아님).

## 구현 방식

- **레이어**: `GuidancePanel`(전체 화면 딤 + 입력 차단) → `Window`(화면 비율에 맞춰 한 번에 스케일) → 효과 레이어(`FrameGlow`, `FrameGhostA/B`, `Frame`, `EdgeNoise`×10, `SignalLine`/`SignalMote`×8) → `Content`(CanvasGroup: 제목·본문·버튼). 화면 글리치 `ScreenGlitch`는 `Window` 위에 있지만 등장 후 0.45초만 켜지고 본문이 보이기 전에 꺼진다. 모든 효과 이미지는 `raycastTarget=false`.
- **본문 보호**: 글리치는 프레임/장식 이미지의 위치·알파·색에만 적용한다. `Content`는 알파만 바뀐다.
- **프레임**: 퀘스트 완료창의 `quest-clear-popup-frame` 그림, 버튼은 설정창의 `button-active-off/on` + 기존 호버 연출(`StyleButton`)을 재사용했다.
- **등장(약 1.28초)**: 화면 글리치 3회(0~0.44초; 가로 노이즈 띠 + 실제 화면 캡처를 가로로 어긋나게 보여 주는 조각) → 청록 입자가 중앙으로 집결(0.46~0.63) → 가로로 확산(~0.80) → 프레임이 위아래로 열림(0.80~1.08, 열리는 순간 프레임 어긋남·가장자리 노이즈 1회 강제) → 본문·버튼 페이드인(~1.28).
- **지속**: 강도 0.22. `BriefingGlitchPlanner`가 간격(강도에 반비례, 0.35~1.9배 편차, 18% 확률로 연속 발생)과 종류(가장자리 노이즈 / 프레임 어긋남 / 청록빛 깜빡임)를 무작위로 정한다. 가장자리 띠는 퍼린 노이즈를 문턱값으로 잘라 불규칙하게 흐른다. 화면 전체 흔들림은 없다.
- **닫기(0.4초)**: 글리치 강도·빈도 감쇠, 본문·버튼 0.24초 페이드아웃, 프레임 청록빛 소등, 마지막에 전체 알파 0. 이후 `DismissRequested`(기존 종료 처리)를 **한 번** 호출한다. 닫기는 연출이 끝난 `Shown` 상태에서만 한 번 접수되어 연속 클릭·X 단축키·버튼이 겹쳐도 중복되지 않는다.
- **정지/입력**: 연출이 시작되면(HUD가 보일 때) `UiPauseGate`가 `timeScale=0`을 적용한다. 연출은 비스케일 시간(프레임당 진행량 0.05초 제한)이라 정지 중에도 재생된다. 해제는 자기가 적용한 정지만 되돌리고, 다른 시스템이 이미 정지/배속을 바꿨다면 건드리지 않는다. 광산 초기화 타이머는 게이트가 잡힌 동안 누적하지 않는다. 월드 입력(이동·채굴·건설·엘리베이터·포탈)은 `PauseInputSuspender`가 끄고, 키 단축키(B/I/G/U/T/M/R/`/`/ESC/O)는 각 컨트롤러가 무시한다. 배경 HUD 버튼은 전체 화면 딤이 클릭을 막는다.
- **정리**: 패널이 비활성화되면(`OnDisable`) 코루틴·화면 복사본·정지 소유권을 해제하고 시각 상태를 초기화한다. 종료 콜백은 부르지 않는다. 다시 열리면 `OnEnable`에서 처음부터 시작한다.

## 검증

- Unity Game View(Play Mode, Bootstrap → 슬롯 이어하기 → Mine_Demo_Integration)에서 등장 → 지속 → 닫기를 프레임 단위로 캡처해 확인했다. 실행 영상 대신 GIF: [prompt-b120-start-briefing-demo.gif](prompt-b120-start-briefing-demo.gif) (1600×1200에서 촬영, 프레임 간격은 실시간이 아니라 캡처 간격 기준).
- 확인한 것: 본문 원문·줄 구분이 확정 문구와 일치(`PromptB120StartBriefingTests`), 본문 가독성, 화면 글리치 3회, 집결·확산·프레임 개방·페이드인 순서, 닫기 시 본문 페이드아웃·청록빛 소등.
- 정지/타이머: 등장 후 `timeScale=0`·월드 입력 비활성, 정지 1.2초 동안 광산 초기화 남은 시간 변화 0, 닫은 뒤 `timeScale=1` 복원·타이머 재개.
- 중복: 닫기 버튼 2회 + 단축키 경로 호출에도 `DismissRequested`는 1회.
- 화면 비율: 16:9(3840×2160), 4:3(1600×1200), 1:1(1200×1200)에서 프레임·본문·버튼이 같은 기준으로 맞는 것을 확인했다.
- EditMode: `PromptB120StartBriefingTests`(시간표, 플래너, 상태 전이, 정지 소유권) 추가.

## 남은 문제 / 확인 필요

- 기존 세이브 상태 유지: "이미 확인한 브리핑"을 저장하는 필드가 원래 없어, 첫 퀘스트 진행 중 세이브를 불러오면 브리핑이 다시 뜬다(기존 동작). 로드 때 재생을 막으려면 세이브 스키마에 확인 여부를 추가해야 하며 이번 범위에서는 하지 않았다.
- 검증에는 슬롯 3 세이브를 사용했다(전후로 파일을 백업·복원).
- 화면 어긋남 조각은 화면 캡처를 오버레이 직전에 GPU 복사해 쓴다. 복사에 실패하면 청록 띠로 대체된다.
- Windows 빌드와 다른 PC 검증은 하지 않았다.
