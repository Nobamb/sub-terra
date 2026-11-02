# B-170 결과: 메인 화면 부팅 연출 고침·소리 + 게임 종료 확인 창 + 종료 연출

- `settings.language` 확인: `Scripts/Shared/Localization/LocalizationService.cs:104`에 `Add("settings.language", "언어", "Language");` 줄이 **있다**(a938c241에서 다시 들어감).
- 기준 커밋: `8e1f5f92` (최신 main, Merge #256). 프롬프트가 적은 a110a3cd 뒤에 #251·#253·#249·#256 PR 4개가 더 들어간 상태다.
- 브랜치: `MVP2-fix115`
- PR: 미생성(사용자 요청으로 코드·보고서까지만, 커밋 안 함)

## 작업 나눔
| 맡은 쪽 | 일 |
|---|---|
| Haiku 하위 에이전트 | B 데이터 연결: wav 2개 복사·작업 폴더 wav 삭제, `.meta`(새 GUID), `SfxId` 85·86, `SfxLibraryDefaults`, `Resources/SfxLibrary.asset`, `init/sound-effect-address.md` 2줄, 기존 칸 개수 검사 3곳(83→85) |
| codex(gpt-6-luna) 이미지 생성 | 종료 확인 창 가운데 전원 아이콘 `Resources/UI/QuitConfirm/quit-power-icon.png`(512×512, 투명 배경, 청록 홀로그램). 후보 2개 중 150px에서 더 또렷한 쪽 |
| codex(gpt-6-luna) | 새 EditMode 검사 `PromptB170BootAndQuitTests` 24개 작성(생성자 누락 1건은 직접 고침) |
| 직접 | 부팅 시계·연출·소리, 공용 글리치 분리, 종료 창·종료 연출·세 경로 연결, 검사 실행·플레이 확인·보고서 |

**스펙과 다른 점(사용자 승인):** C-1은 "새 그림을 만들지 않는다"이지만, 프레임 그림(`mine-reset-frame.png`)에 9-슬라이스 경계가 없어 카드를 줄이면 모서리가 찌그러진다. 그래서 880×675를 그대로 썼다. 본문이 두 줄이라 가운데가 비어 어색했고, 사용자가 "퀄리티가 낮아질 때는 이미지 생성 사용 가능"이라고 해 전원 아이콘 1장을 넣었다. 프레임·글리치·버튼 그림은 구출 창 것을 그대로 쓴다. 아이콘이 없으면 글만 가운데로 모이도록 처리했다.

## A. 부팅이 안 보이던 원인과 고친 곳
- **원인 확인 방법:** Play에서 MainMenu 씬을 다시 열고 임시 측정기로 프레임마다 `unscaledDeltaTime`과 시계를 기록했다. 씬을 연 직후 두 프레임의 dt가 **0.179초, 0.454초**였다. 고치기 전 코드라면 0.98초 연출 중 0.63초가 첫 화면이 그려지기 전에 지나간다.
- **고친 곳**
  - `MainMenuBootTimeline.cs:124` `Tick(raw)`: 처음 2프레임(`DiscardedFrames`)의 dt는 버리고, 그 뒤는 `MaxStep`(1/30초)으로 자른다. 시계가 처음 움직인 프레임에만 true를 돌려준다.
  - `MainMenuIntroDirector.cs:205`: `Boot.Advance(dt)` 대신 `Boot.Tick(dt)`를 쓴다. 고친 뒤 측정에서 두 프레임은 시계 0으로 남고, 다음 프레임부터 0.003초씩 나아갔다.
  - 길이: `MainMenuBootSettings` 값을 모두 2배로 했다(순서·겹침 비율 그대로, `cardRise` px는 그대로). `MaxDuration` 2.0(`:68`), 합계 1.96초. 프리팹 `bootSettings`는 새 빌더 `SubTerra/MVP2/B-170 Apply Main Menu Boot Timing`(`Editor/DataValidation/PromptB170BootTimingBuilder.cs`)으로 맞췄다. diff는 16줄이다.
  - 연출 감소(`:101`): 바로 끝 화면을 보이고 켜짐 소리만 한 번 낸다(틱 없음).
- 실제 화면(Play, Bootstrap부터): 0.4초 가로선과 셔터 펼침, 1.3초 로고·부제·카드 1장째 등장, 2초 안에 버튼까지 나온다.

## B. 부팅 소리
- 85 `MainMenuBoot`(0.45), 86 `MainMenuTypeTick`(0.5), UI 버스.
- 켜짐: `Scripts/App/Audio/MainMenuBootSfx.cs`. 전용 AudioSource를 쓰므로 SfxPlayer에 멈춤 API를 더하지 않았다. 음량은 칸 × 버스 × 효과음 설정이고 음소거를 반영한다. 건너뛰면 `FadeOut()`이 0.15초에 줄여 멈춘다(`MainMenuIntroDirector.cs:123`). 줄이기는 소리 쪽 러너가 비스케일 시간으로 돌리므로 연출 오브젝트가 꺼져도 끝까지 간다.
- 틱: 부팅 문구·부제의 타이핑 글자 수가 늘어난 프레임에 `SfxPlayer.Play`로 낸다. 간격은 **예정 시각(시계)** 기준 0.06초 이상이다(`MainMenuTypeTickGate`, B-169 교훈). "신호 연결됨" 문구 교체는 세지 않는다.
- 실제 Play: 부팅마다 `MainMenuBootSfx.StartedCount`가 1씩 올랐다.

## C. 게임 종료 확인 창
- 공용 진입점: `Scripts/App/UI/QuitConfirm/QuitConfirmFlow.cs` `Request(inGame, menuOutro, callerBlocked)`. 순서는 `QuitSequence`(순수 계산)가 정한다.
- **구출 창과 공유하는 부분:** 새 `Scripts/App/UI/EmergencyRescue/GlitchPopupChrome.cs`로 딤 배경(0.78), 카드 바탕, `MineResetTimedPopupSkin` 프레임과 윗부분 붉은 경고색, 청록·붉은 잔상, 어긋나는 띠, 번쩍임, 가로 조각, 등장·표시·닫기 글리치 적용을 옮겼다. 구출 창(`EmergencyRescuePopupView.cs:89`)과 종료 창이 같이 쓴다. 계층 이름·순서·수치는 그대로 옮겼고, B-135 구출 창 검사가 모두 통과한다. 시간표는 기존 `EmergencyRescueTimeline` 그대로(등장 0.5, 닫기 0.3, 표시 중 0.4초 칸).
- 창: `QuitConfirmPopupView.cs`. 제목 "게임 종료", 본문 "게임을 종료할까요?", 게임 중이면 "진행 상황은 자동으로 저장됩니다.". 주 버튼 "종료"(300×68, `UiConfirm`+호버), 보조 버튼 "취소"(190×58, 호버만). 등장 `UiPopupOpen`, 닫기 `UiPopupClose`. 정렬은 32_767로 설정 창(32_760)과 모든 팝업보다 위다.
- 입력: `QuitConfirmFlow.HandleInput` 한 곳에서 처리한다. ESC·X는 닫기다. `[DefaultExecutionOrder(-400)]`이라 다른 처리(-300 등)보다 먼저 받고, `IsBlockingInput`이 그 프레임과 창이 떠 있는 동안 메인 메뉴·지상·광산·비상 탈출 창의 단축키를 막는다. 버튼은 `Navigation.None`과 클릭 뒤 선택 해제로 Enter·Space(Submit)가 닿지 않는다. 등장이 끝나기 전 버튼은 받지 않는다. 떠 있는 동안 O키는 Busy로 무시한다.
- 막기: `UiPauseGate`가 이미 잡혀 있거나, 엘리베이터 탑승 중이거나(`UndergroundMenuController.RegisterQuitRideCheck`가 `IsElevatorRiding`을 등록), 설정 창이 열려 있으면 열지 않는다. 저장 중이면 창 없이 지금처럼 "저장 중입니다" 문구만 낸다(메인·지상. 광산은 원래 문구가 없어 그대로).
- 연출 감소: 글리치 없이 바로 나타나고 사라진다. 소리는 같다.
- **세 종료 경로를 바꾼 곳**
  - 메인 화면: `MainMenuBinder.cs:321`(`OnQuit`. presenter의 저장 중 판단 뒤), ESC·X 양보 `:109`, 버튼 클릭음 None `MainMenuView.cs:170`
  - 지상 기지: `SurfaceBaseBinder.cs:546`(버튼·O키 공통 `OnQuitClicked`), 양보 `:206`, 버튼 클릭음 None `SurfaceBaseView.cs:262`
  - 광산: `UndergroundMenuController.cs:231`(`RequestQuit`. 씬의 "게임 종료(O)" 버튼 연결 이름은 그대로라 씬 YAML 미변경), 양보 `:76`, 버튼 클릭음 None `:47`(런타임에 `QuitShortcut`을 찾아 바꿈)

## D. 종료 연출
- "종료" 버튼 → `UiConfirm` → 창 즉시 숨김 → `SaveRuntimeController.SaveBeforeQuit()`(`:448`, `RequestQuit`의 저장 조건 그대로) → 연출 → `ShutdownApplication()`(`:462`, 에디터는 재생 멈춤, 빌드는 `Application.Quit`). 기존 `RequestQuit()`은 두 메서드를 차례로 부른다.
- 메인 화면: `MainMenuIntroDirector.BeginQuitOutro/ApplyQuitOutro`가 부팅 시간표를 t = 전체 × (1 − 진행)으로 거꾸로 샘플한다. 2.5배 속도로 0.784초.
- 지상·광산: `QuitOutroOverlay.cs`. 같은 시간표의 화면 부분(가로선 → 펼침)만 거꾸로 써서 0.45초에 접는다. 셔터 색 #010406, 가로선 #C8FBFF와 그림자 #39D8EA로 메인 화면 BootOverlay와 같다.
- 시작 프레임에 `WindowTvSfx.Play(WindowTvClose)`를 한 번 낸다. 연출 중 아무 키·클릭이면 바로 끈다(종료를 누른 프레임은 제외). 저장이 안 끝났으면 접힌 화면에서 기다린다. 연출 감소면 바로 검게 한 뒤 0.35초에 끈다.
- 실제 Play(메인 화면): 버튼과 카드가 거꾸로 사라지며 접혔고 `WindowTvClose`가 났다. 연출이 끝나자 에디터 재생이 멈췄다.

## 테스트
- 새 `PromptB170BootAndQuitTests` **24개 통과**(부팅 6, 소리·칸 3, 종료 창 8, 종료 연출 5, 정적 연결 2).
- 바꾼 기존 검사
  - `PromptB150MainMenuTests`: `Boot_RunsInOrder_WithinOneSecond` → `WithinTwoSeconds`, 카드 간격 0.08 → 0.16
  - `PromptB158SfxFixTests`: 메인 메뉴 종료 버튼 클릭음 UiClick → None(C-6)
  - 칸 개수 83 → 85: `PromptB156SfxTests:286`, `PromptB165SfxD1FixTests:91`, `PromptB168SfxD2Tests:85`
- **EditMode 전체:** B-170 적용 후 2145 통과 / 22 실패이고, 그중 1개(B-158)는 위처럼 기대값을 고쳤다. 같은 작업트리에서 B-170 변경을 stash한 **기준선은 2122 통과 / 21 실패**다. B-158을 빼면 실패 21개 목록이 정확히 같다(새 실패 없음). 기준선 21개는 B-169 보고서의 20개에 그 뒤 들어간 PR의 영향이 더해진 것으로 보인다. 예: `PromptB162SfxCStageTests.Defaults_CSlotsMatchTable…`은 6c94d07a에서 `BriefingClose` 음량이 1.0으로 바뀐 뒤 계속 실패한다.
- 순서 영향: `PromptB85EmergencyRescueChipTests`, `PromptB89UndergroundMenuTests`는 전체 실행에서만 실패하고 묶음만 돌리면 통과한다. 기준선에서도 같다.
- 전체 실행이 범위 밖 파일(`NotoSansKR-Regular_SDF.asset`, `InventoryPanel.prefab`, `ViewSocket.prefab`, `Mine_Demo_Integration.unity`, `ProjectSettings/TimeManager.asset`, 103 메인 메뉴 PNG 2장)을 dirty로 만들어 `git restore`로 되돌렸다.
- PlayMode: 새 검사는 넣지 않았다. 대신 Bootstrap부터 실제 Play로 메인 화면 경로를 확인했다(아래).

## 직접 확인한 것 / 미검증
- 확인(에디터 Play, 메인 화면): 부팅이 약 2초 동안 보임(캡처), 켜짐 소리 재생 요청 1회, 종료 버튼 → 글리치 등장 창(캡처), 취소 → 닫힘 뒤 일시정지 해제·timeScale 1, 종료 → 거꾸로 접힘(캡처)·TV 꺼짐 소리 요청·재생 멈춤. Console에 새 오류·경고 없음(기존 컴파일 경고만).
- **미검증**
  - 소리를 귀로 들은 것(켜짐 끝의 TV 지직, 틱 크기, 건너뛸 때 줄어드는 느낌, 효과음 음량·음소거 반영)
  - 지상 기지·광산에서의 실제 Play(사용자 세이브를 건드리지 않으려고 이어하기를 하지 않았다). 단위 검사와 정적 연결 검사만 통과했다.
  - 엘리베이터 탑승 중·시작 브리핑 중 O키 무시, 저장 중 문구(검사로만 확인)
  - 연출 감소를 켠 실제 Play
  - Windows 빌드에서 `Application.Quit` 경로
  - 다시 켰을 때 직전 진행이 저장돼 있는지(세이브를 건드리는 확인이라 하지 않음)

## 수동 확인 체크리스트
- [ ] 게임을 켜면 메인 화면이 가로선 → 위아래로 펼쳐짐 → 문구 타이핑 → 로고 → 카드 → 버튼 순서로 약 2초 동안 보인다(처음 켤 때 바로 끝나 있지 않다).
- [ ] 켜질 때 SF 켜짐 소리가 나고, 끝 무렵 작은 TV 지직이 섞인다. 글자가 찍힐 때 작은 틱이 난다(시끄럽지 않다).
- [ ] 부팅 중 아무 키·클릭이면 바로 끝 화면이 되고 소리가 부드럽게 멈춘다. 그 클릭으로 메뉴 버튼이 눌리지 않는다.
- [ ] 메인 화면 종료 버튼 → 전력 고갈 창과 같은 모양의 "게임 종료" 창이 글리치와 함께 뜬다.
- [ ] 지상 기지 종료 버튼·O키, 광산 메뉴 "게임 종료(O)"·O키도 같은 창이 뜬다.
- [ ] "취소"·ESC·X → 창이 글리치로 닫히고 게임이 이어진다. 설정 창이 같이 열리거나 닫히지 않는다.
- [ ] 창이 떠 있을 때 Enter를 눌러도 꺼지지 않는다.
- [ ] "종료" 버튼 클릭 → 화면이 부팅의 반대로 접히며 TV 꺼짐 소리가 나고 게임이 꺼진다(에디터는 Play가 멈춘다). 다시 켜면 직전 진행이 저장돼 있다.
- [ ] 종료 연출 중 아무 키 → 바로 꺼진다.
- [ ] 엘리베이터를 타는 중·시작 브리핑 중에는 O키로 종료 창이 뜨지 않는다.
- [ ] 저장 중에 종료를 누르면 창 대신 "저장 중입니다" 문구가 나온다.
- [ ] 설정에서 "애니메이션 연출 감소"를 켜면 부팅은 바로 끝 화면 + 켜짐 소리만, 종료 창은 글리치 없이, 종료는 바로 검은 화면 + 꺼짐 소리 뒤 꺼진다.
- [ ] 효과음 음량·음소거가 새 소리에 적용되고 Console에 새 경고나 오류가 없다.

## #158 대응
| #158 항목 | 이 작업 |
|---|---|
| E 부팅: 가로선 → 부팅 문구 타이핑 → 로고 → 슬롯 카드 → 버튼 순서 큐 | A·B(켜짐 소리 + 타이핑 틱. 로고·카드 개별 큐는 넣지 않음) |
