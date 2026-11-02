# B-170 메인 화면 부팅 연출 고침·소리 + 게임 종료 확인 창 + 종료 연출 (#158 E 부팅)

## 목표
- A. 메인 화면 부팅 연출(B-150)이 씬을 연 첫 프레임에 다 끝나 버려 **안 보이는** 문제를 고치고, 길이를 약 2초로 늘린다.
- B. 부팅 연출에 소리를 붙인다: 켜짐 소리 1개 + 글자가 찍힐 때 작은 틱. 새 음원 2개는 묶음 zip에 있다.
- C. 모든 "게임 종료"(메인 화면 종료 버튼, 지상 기지 종료 버튼·O키, 광산 메뉴 "게임 종료(O)"·O키)가 바로 꺼지지 않고 **종료 확인 창**을 먼저 띄운다. 창의 모양·연출·소리는 **전력 고갈 구출 창**("전력이 바닥났습니다")과 같게 한다.
- D. 종료를 누르면 **부팅 연출을 거꾸로** 짧게 재생해 화면이 꺼진 뒤 게임이 끝난다. 아무 키·클릭이면 바로 종료.
- 기준: 최신 `main`(a110a3cd, MVP2-fix114 + 설정 이름 변경)에서 새 브랜치 `MVP2-fix115`를 딴다. B-166~B-169 고침을 되돌리지 않는다.
- 시작 전 확인: `LocalizationService.cs`에 `Add("settings.language", "언어", "Language");` 줄이 있는지 본다(a110a3cd에서 빠졌다). 없으면 고치지 말고 보고서 첫 줄에 적는다(PM이 따로 고침). 이 프롬프트 범위가 아니다.
- 필요하면 다른 에이전트(Codex 등)에게 나눠 맡겨도 된다. 결과는 한 PR로 모은다.

## 절대 규칙
1. 기존 SfxId 번호·칸 값, 버스 음량은 바꾸지 않는다. 새 칸은 84(UpgradeLockRegather) 뒤 85·86만 붙인다.
2. `SfxPlayer`에 멈춤 API를 추가하지 않는다(B-166). 끊거나 줄여야 하는 소리는 따로 AudioSource를 둔다(`WindowTvSfx` 방식).
3. 세이브 형식, 저장 키(`subterra.settings.reduceMotion` 등), 저장 시점 규칙(`QuitPolicy`)은 바꾸지 않는다. 종료 전 저장은 지금처럼 한다.
4. 연출 시간은 모두 비스케일. 소리는 시간표 단계를 넘는 그 프레임에 한 번(프레임이 튀어도 한 번).
5. 줄 번호는 a110a3cd 기준 근사(~). 못 찾으면 "못 찾음"으로 보고한다.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/` 기준)
- Scripts/App/UI/MainMenu/MainMenuIntroDirector.cs(`OnEnable` ~56 → `StartBoot` ~83, `SkipBoot` ~94, `PlayExit` ~106, `Update` ~141 `dt = Time.unscaledDeltaTime` 상한 없음, `SkipPressed` ~178)
- MainMenuBootTimeline.cs(`MainMenuBootSettings` ~8-31, `MaxDuration` 1.0 ~68, `TotalDuration` ~87), MainMenuExitTimeline.cs(이어하기·새 게임 뒤 접힘, 이번에 안 바꿈)
- Prefabs/UI/MainMenuPanel.prefab `bootSettings` ~3000-3017(코드 기본값과 같은 값이 저장돼 있음), Tests/EditMode/App/UI/MainMenu/PromptB150MainMenuTests.cs(`Boot_RunsInOrder_WithinOneSecond` ~49-52)
- 종료 경로: MainMenuBinder.cs(`OnQuitClicked` ~242 → `MainMenuPresenter.RequestQuit` ~267 → `QuitConfirmed` → `OnQuit` ~314), SurfaceBaseBinder.cs(`Update` O키 ~204, `OnQuitClicked` ~472), Integration/UndergroundMenuController.cs(`Update` ESC·X·O ~66-75, `HandleCloseTopPopup` ~88, `IsElevatorRiding` ~171, `RequestQuit` ~215), Save/SaveRuntimeController.cs(`RequestQuit` ~436: 저장 → 에디터면 재생 멈춤, 빌드면 `Application.Quit`)
- 전력 고갈 구출 창: Scripts/App/UI/EmergencyRescue/EmergencyRescuePopupView.cs(`Build` ~127, 등장 `StartOpening` ~358, `Tick` ~319, `BeginClose` ~241, 버튼 ~663-676, `ApplyButtons` ~559), EmergencyRescueTimeline.cs(`IntroDuration` 0.5, `OutroDuration` 0.3, `Intro`·`Outro`·표시 중 글리치 ~104-170), EmergencyRescuePanelView.cs(`Create` ~33), Integration/EmergencyRescueRuntimeController.cs(창 만들기 ~314-321), 스킨 `MineResetTimedPopupSkin`
- Scripts/App/Audio/WindowTvSfx.cs, SfxLibraryDefaults.cs(~218-230 B-169 칸 붙인 방식), Resources/SfxLibrary.asset, Shared/Audio/SfxId.cs(~88-93)
- Scripts/App/UI/UiPauseGate.cs, Shared/AccessibilityPreferences.cs(설정 이름은 이제 "애니메이션 연출 감소")

## A. 부팅 연출 고침
**원인(PM이 확인함):** 씬을 연 첫 프레임의 `unscaledDeltaTime`이 아주 커서(로딩 시간) 0.98초짜리 연출이 첫 화면이 그려지기 전에 끝난다. Play 중 `MainMenuIntroDirector`를 껐다 켜면 정상으로 보인다.
1. 시계는 **첫 화면이 실제로 그려진 다음 프레임부터** 간다(`StartBoot`한 프레임과 그다음 한 프레임의 dt는 버린다). 한 프레임에 더하는 dt는 최대 1/30초로 자른다. 중간에 렉이 걸리면 연출이 그만큼 느려지는 것은 괜찮다.
2. 길이를 약 2.0초로 늘린다: `MainMenuBootSettings` 시각 값을 모두 약 2배로(순서·겹침 비율 유지), `MaxDuration` 2.0. 코드 기본값과 프리팹 `bootSettings` 두 곳을 같이 바꾼다. 테스트 이름·설명의 "1초"도 고친다.
3. 아무 키·클릭으로 건너뛰기는 그대로. 건너뛴 같은 프레임에 메뉴 버튼이 눌리지 않는 규칙(`BlocksInput`)도 그대로.
4. 애니메이션 연출 감소가 켜져 있으면 연출 없이 바로 끝 상태로 보이고, 켜짐 소리(B-1)만 한 번 낸다(틱 없음). B-169 TV 창과 같은 규칙.

## B. 부팅 소리 (새 칸 2개, UI 버스)
- 사용자가 압축 푼 묶음을 `work_process/MVP2/sound/sfx-B170/`에 넣어 둔다. 거기 있는 `main_menu_boot.wav`, `main_menu_type_tick.wav`를 `Assets/_Project/Audio/SFX/UI/`로 복사해 넣고(작업 폴더 쪽 wav는 지운다), `SfxLibraryDefaults.cs`와 `Resources/SfxLibrary.asset` 두 곳에 붙인다. `preview_boot_with_ticks.mp3`·README·프롬프트는 그 폴더에 그대로 둔다. 음원은 고치지 않는다. `init/sound-effect-address.md` 표 끝에 README의 출처 2줄(`main_menu_boot`, `main_menu_type_tick`)을 붙인다(같은 이름 줄이 있으면 고친다).
  - 85 `MainMenuBoot` `main_menu_boot` 0.45 (2.3초. SF 운영체제 켜짐 + 끝에 작게 TV 켜짐 지직을 미리 섞음)
  - 86 `MainMenuTypeTick` `main_menu_type_tick` 0.5 (0.13초 전자 틱 한 번)
1. 시계가 실제로 움직이기 시작한 프레임(A-1)에 `MainMenuBoot` 한 번. 따로 AudioSource 하나로 낸다(칸 음량 × 버스 × 효과음 설정, 음소거 반영). 건너뛰면 0.15초 안에 줄여 멈춘다.
2. 부팅 문구·부제가 타이핑되는 동안 보이는 글자 수가 늘어난 프레임에 `MainMenuTypeTick`(`SfxPlayer.Play`). 0.06초 안에 한 번만. 건너뛰면 더 내지 않는다.
3. 로고 글리치·카드 등장 등 다른 단계에는 소리를 새로 넣지 않는다. 이어하기·새 게임 뒤 접힘(`MainMenuExitTimeline`)은 지금 그대로.

## C. 게임 종료 확인 창
**지금:** 세 곳 모두 종료를 누르면 저장 후 바로 꺼진다. 확인 창이 없다.
**전력 고갈 구출 창(참고 모양):** 어두운 배경(알파 0.78) 위 880×675 카드, `MineResetTimedPopupSkin` 프레임(윗부분 붉은 경고색), 청록·붉은 글리치로 0.5초 끊겨 나타나고, 떠 있는 동안 0.4초 칸마다 가장자리에만 약한 글리치, 닫을 때 0.3초 강한 글리치. 큰 주 버튼("구출 요청" 300×68) + 작은 보조 버튼("닫기" 190×58). 소리: 등장 `UiPopupOpen`(6), 닫기 `UiPopupClose`(7), 주 버튼 `UiConfirm`(4)+호버, 보조 버튼은 호버만.
1. 새 종료 확인 창은 위 모양·연출·소리를 **그대로 재사용**한다. 프레임·글리치 부분을 공용으로 빼서 구출 창과 같이 쓰는 것을 권장한다(구출 창의 보이는 결과는 바뀌면 안 됨). 새 그림을 만들지 않는다. 비용 표·홀로그램 안내는 없다. 카드 높이는 내용에 맞게 줄여도 되지만 프레임이 찌그러지면 원래 크기를 쓴다.
2. 글: 제목 "게임 종료", 본문 "게임을 종료할까요?" + 게임 중이면 "진행 상황은 자동으로 저장됩니다."(메인 화면에서는 이 줄 없음). 버튼: 주 "종료", 보조 "취소".
3. 입력: ESC·X·"취소" = 닫기(닫힘 글리치·소리). "종료" 버튼 클릭 = 종료(D로). Enter·Space 등 키보드로는 종료하지 않는다(UI 기본 Submit으로 "종료" 버튼이 눌리지 않게 버튼 선택/Navigation도 막는다. 실수 방지, 나중에 단축키를 다시 넣을 수 있게 입력 처리는 한 곳에 모아 둔다). 등장 연출이 끝나기 전에는 버튼을 받지 않는다(구출 창과 같음). 창이 떠 있는 동안 O키를 다시 눌러도 아무 일 없다.
4. 하나의 공용 진입점(예: `QuitConfirmFlow.Request()`)을 세 경로가 모두 쓴다. 지금의 `QuitPolicy` 판단을 먼저 한다: 저장 중이면 창을 띄우지 않고 지금처럼 "저장 중입니다…" 문구만.
5. 막기·겹침:
   - 창이 떠 있는 동안 `UiPauseGate`를 잡아 게임을 멈추고, 창이 ESC·X를 직접 처리한다(Enter는 아무 일 없음). 같은 프레임에 설정 창·다른 팝업 닫기가 같이 일어나지 않게 다른 ESC·X 처리보다 먼저 받는다.
   - 이미 `UiPauseGate`가 잡혀 있을 때(시작 브리핑 등)와 엘리베이터 탑승 중(`IsElevatorRiding`, B-166 설정 창 규칙)에는 O키·메뉴 종료 버튼으로 창을 열지 않는다. 설정 창이 열려 있으면 O키는 무시한다.
   - 창은 모든 창보다 위에 그린다(구출 창의 맨 앞으로 가져오기 방식).
6. 종료 버튼(세 곳)의 원래 클릭음은 None으로 바꾸고, 창 등장음이 대신한다(B-169 방식).
7. 애니메이션 연출 감소: 창이 글리치 없이 바로 나타나고 사라진다. 소리는 같게 낸다. (구출 창 자체는 이번에 바꾸지 않는다.)

## D. 종료 연출 (부팅 거꾸로)
1. "종료"를 누르면: `UiConfirm` → 창은 연출 없이 바로 숨김 → **먼저 저장**(지금 `RequestQuit`의 저장 부분과 같은 조건) → 종료 연출 → 종료(에디터는 재생 멈춤, 빌드는 `Application.Quit`, 지금 `StopEditorPlayMode` 경로 그대로). `RequestQuit`을 "저장"과 "끄기"로 나눠도 된다.
2. 연출은 `MainMenuBootTimeline`을 **거꾸로** 샘플한다(시각 t = 전체 × (1 − 진행)). 약 2.5배 빠르게 재생해 0.8초 안에 끝낸다.
   - 메인 화면: 버튼 → 카드 → 부제·로고·문구가 거꾸로 사라지고, 화면이 위아래에서 가로선으로 접힌 뒤 선이 사라진다(`MainMenuIntroDirector`에 거꾸로 재생 추가).
   - 지상 기지·광산: 부팅 화면 요소가 없으므로 같은 시간표의 화면 부분(펼침 → 가로선)만 거꾸로 쓰는 덮개를 맨 위 캔버스에 만들어 0.5초 안에 접는다. 같은 셔터·가로선 모양.
3. 소리: 연출 시작 프레임에 `WindowTvClose`(80)를 `WindowTvSfx`로 한 번(새 음원 없음, 창 TV 꺼짐과 같은 소리).
4. 연출 중 아무 키·클릭이면 바로 종료. 연출 중 다른 입력·창은 받지 않는다. 저장이 끝나지 않았으면 끝날 때까지 접힌 화면에서 기다린다.
5. 애니메이션 연출 감소: 접힘 없이 화면을 바로 검게 하고 `WindowTvClose`를 낸 뒤 0.35초에 종료.

## 손대지 말 것
- 이어하기·새 게임 뒤 접힘 연출, 메인 화면 다른 연출 값, 구출 창의 동작·비용·R키, 설정 창, 업그레이드 창·TV 창 연출과 소리(B-167~169), 기존 칸 값, 세이브 형식, `QuitPolicy`

## 테스트
- EditMode 전체에서 기존 실패(B-169 보고서: 20개) 말고 새 실패가 없어야 한다. SfxId 칸 개수 테스트(B-168·B-169)는 85·86에 맞게 고친다.
- 새 EditMode 테스트 `PromptB170BootAndQuitTests`
  - 부팅: 첫 두 프레임에 큰 dt(5초)를 줘도 시계 0, 그다음 0.5초 dt는 1/30만 더함, 전체 길이 1.8~2.2초, 순서 유지, 건너뛰면 끝 상태
  - 소리: 시계 시작 프레임에 Boot 한 번, 타이핑 중 틱 간격 0.06초 이상, 건너뛰면 틱 0·Boot 소리 줄어 멈춤, 연출 감소면 Boot 1·틱 0, 칸 85·86 값과 파일 이름
  - 종료 창: 세 경로 모두 창을 띄움(바로 종료 안 함), 저장 중이면 창 없음, 탑승 중·`UiPauseGate` 잡힘이면 창 없음, ESC=닫기, Enter로는 종료되지 않음, 등장 중 버튼 무시, 소리 6/7/4 각 1번
  - 종료 연출: 저장이 연출 전에 한 번, 거꾸로 샘플 값이 부팅 값의 역순, 0.8초 안에 끝, 키 입력 시 바로 종료 콜백, 연출 감소면 0.35초 뒤 종료, 종료 콜백 한 번만
- PlayMode는 이번에 추가한 테스트만 돌린다. 실제 종료는 가짜 종료 콜백으로 바꿔 확인한다.

## 수동 확인 체크리스트 (보고서에 그대로 넣기)
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

## #158 대응 (완료 표시용)
| #158 항목 | 이 프롬프트 |
|---|---|
| E 부팅: 가로선 → 부팅 문구 타이핑 → 로고 → 슬롯 카드 → 버튼 순서 큐 | A·B (켜짐 소리 + 타이핑 틱. 로고·카드 개별 큐는 넣지 않음) |
- #158 본문은 이 작업에서 고치거나 닫지 않는다(PM이 고침). 종료 창·종료 연출은 #158에 없는 새 항목이다.

## 완료 후
- PR 제목 예: `feat: 메인 화면 부팅 연출 고침과 게임 종료 확인 창·종료 연출 (프롬프트 B-170)`. 커밋 메시지는 한국어 conventional 형식, 브랜치는 지우지 않는다.
- 결과 보고서 `work_process/MVP2/B-170-boot-quit-result.md`: 기준 커밋, 브랜치, PR 링크, `settings.language` 확인 결과, 테스트 결과, 부팅이 안 보이던 원인을 확인한 방법과 고친 위치(파일·줄), 종료 확인 창이 구출 창과 공유하는 부분, 세 종료 경로를 바꾼 위치, 직접 보고 듣지 못한 항목은 "미검증"으로, 위 수동 확인 체크리스트
