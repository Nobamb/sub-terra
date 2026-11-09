# B-183 메인 화면 슬롯·드론 분석 창·급한 말풍선 소리, 새 음원 (#158 D-2·E의 B-183 줄)

## 목표
- PM이 고른 새 음원으로 남은 메인 화면·드론 소리를 채운다.
  - A. 세이브 슬롯을 고를 때 쇠 프레임 "딸깍"(비어 있던 `ui_slot_select` 칸 16을 채움)
  - B. 빈 슬롯 "신호 없음" TV 노이즈. **PM 해석: 늘 깔리는 배경음이 아니라, 플레이어가 빈 슬롯을 "쓸 때"(고를 때·마우스를 올린 동안)에만 난다.** 보고서 맨 앞에도 이 해석을 적는다.
  - C. Tab 드론 분석 창: 가로선 열림·타이핑·근거 줄·접히며 닫힘은 **기존 소리**, PRIORITY로 바뀌는 순간만 **새 경고음** 한 번
  - D. 드론 머리 위 말풍선의 급한 말: 단순 알림은 "삐빅", **심각한 말은 같은 박자의 낮은 "삐빅"**
- 소리 id·음량 배율·간격은 연결 지점마다 상수 하나로 둬서 PM이 숫자만 바꿔 끼울 수 있게 한다(아래 ★).
- 필요하면 다른 에이전트에게 나눠 맡겨도 된다. 결과는 한 브랜치로 모은다.

## 브랜치
1. `git fetch`로 원격 `MVP2-fixNNN` 중 가장 큰 번호를 확인한다. 계획 순서는 B-179-1(`fix125`) → B-180(`fix126`) → B-180-1(`fix127`) → B-181(`fix128`) → B-182(`fix129`, 화물 창) → B-183(`fix130`)이다.
2. 가장 큰 번호의 `git log`에서 `(프롬프트 B-179-1)`·`(프롬프트 B-180)`·`(프롬프트 B-180-1)`·`(프롬프트 B-181)`·`(프롬프트 B-182)` 커밋이 들어 있는지 본다.
3. **B-181 커밋이 들어 있는 가장 최신 브랜치** 위에서 **비어 있는 다음 번호** `MVP2-fixNNN`을 만든다(B-182까지 있으면 보통 fix130). 보고서 맨 위에 기준 브랜치·커밋, 위 다섯 커밋 포함 여부, 만든 번호와 이유를 적는다.
4. 멈추고 보고: 기준에 B-179 커밋(`b1471534`)이 없을 때, B-181 커밋이 든 브랜치가 하나도 없을 때(D가 B-181의 말풍선 비프 자리를 바꾸기 때문).
5. B-182가 아직 없으면 없는 채로 진행하고, B-182가 뒤에 어떤 번호를 써야 하는지 보고서에 적는다.

## 절대 규칙
1. 기존 SfxId 번호·버스 음량은 바꾸지 않는다. 바뀌는 칸은 16(파일 채움·음량)만, 새 칸은 **95~98** 네 개. 99~101은 B-184 몫이니 쓰지 않는다.
2. **새 음원은 고치지 않는다.** 길이·이음새는 다듬은 파일 그대로. 조정은 칸 값과 상수로만.
3. 원샷은 `SfxPlayer.Play`(B-181이 더한 `volumeScale` 겹 사용 가능), 창 TV 소리는 `WindowTvSfx`. `SfxPlayer`에 멈춤·반복 API를 더하지 않는다. 반복 소리는 따로 AudioSource(기존 `WorldLoopAudio`·`GasPulseAudio` 방식).
4. 경고(Alert) 버스는 쓰지 않는다(광산 경보를 끊지 않도록. 96도 UI 버스).
5. **불러오기·이어하기, 초기화(Bind·OnEnable·씬 시작), 조용히 치우는 경로(HideImmediate·SetVisible(false)·OnDisable·씬 전환)에서는 소리를 내지 않는다.**
6. 씬·프리팹 YAML은 고치지 않는다. 슬롯·세이브 규칙, 드론 대사·긴급도 판단, 창 연출 시간표, B-164~B-182 소리 연결은 바꾸지 않는다(아래 D의 상수 교체만 예외).
7. 줄 번호는 fix124 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 음원 넣기
- 사용자가 `sub-terra-sfx-B183-184.zip`을 풀어 `work_process/MVP2/sound/sfx-B183-184/`에 한 번 넣어 둔다(B-184도 같은 폴더를 씀). 없으면 멈추고 보고한다.
- B-183은 아래 5개 wav만 옮긴다(UI → `Assets/_Project/Audio/SFX/UI/`, World → `…/SFX/World/`). 나머지 3개(`scan_mark_click`·`gas_flow_loop`·`unstable_block_shake_loop`)와 mp3·README·`address-rows.md`는 작업 폴더에 그대로 둔다(B-184가 씀).
- 임포트: 기존 빌더(`PromptB156SfxLibraryBuilder`)를 따르되 **95는 Normalize 끔 + PCM**(반복 소리가 +10dB 이상 커짐). 빌더 예외에 넣고, `LoopFileNames`에는 넣지 않는다.
- `init/sound-effect-address.md`: `address-rows.md`대로 `ui_slot_select` 줄은 고치고 95~98 네 줄은 표 끝에 붙인다.

| 칸 | 이름 | 파일 | 버스 | 음량 | 길이 | 쓰는 때 |
|---|---|---|---|---|---|---|
| 16 | UiSlotSelect(그대로) | ui_slot_select | UI | 0.8 → **0.65** | 0.300초 | 슬롯 고름 |
| 95 | EmptySlotNoiseLoop | empty_slot_noise_loop | UI | 0.4 | 2.000초 반복 | 빈 슬롯 쓸 때 |
| 96 | DronePriorityAlert | drone_priority_alert | UI | 0.5 | 0.600초 | 분석 창 PRIORITY 진입 |
| 97 | DroneBubbleUrgent | drone_bubble_urgent | World | 0.6 | 0.230초 | 급한 말(단순) |
| 98 | DroneBubbleUrgentSerious | drone_bubble_urgent_serious | World | 0.6 | 0.230초 | 급한 말(심각, 7반음 낮음) |
- `Shared/Audio/SfxId.cs`, `App/Audio/SfxLibraryDefaults.cs`(파일 끝, 길이 주석), `Resources/SfxLibrary.asset` 세 곳에 같게. `Fallbacks`의 16→UiClick은 그대로 둬도 된다(파일이 있으면 안 쓰임).

## 먼저 읽을 것 (`sub-terra/Assets/_Project/Scripts/` 기준)
- #158의 `(B-183)` 줄, `work_process/MVP2/sound/sfx-B183-184/README.md`, B-181 보고서(`B-181-sfx-ingame-fill-result.md`)
- App/UI/MainMenu: `MainMenuBinder.OnSlotSelectedSfx` ~142, `MainMenuView` 슬롯 연결 ~112·카드 호버 `OnCardHovered`·`OnCardUnhovered` ~447-457·키보드 탐색 ~470, `MainMenuPresenter.SelectSlot` ~84, `SaveSlotCardView`(`SaveSlotCardKind.Empty` ~36·~108, 노이즈 그림 ~203)
- App/UI/Drone/DroneDialoguePanelView.cs: `SetVisible` ~215(열기 ~246), `TryBeginAnimatedClose` ~255, `SetOperationalState` ~279(PRIORITY 진입), `SetAnalysis` ~177(다시 찍기·근거 줄 추가), `BeginReveal` ~428, `ApplyTyping` ~571, DroneTerminalTimeline.cs(열기 0.30초·닫기 0.25초·근거 줄 0.07초 간격)
- App/UI/Drone/DroneDialogueSocket.cs `SetDialogue` ~68(B-181의 `DroneUrgentBubbleSfx`), App/Drone/DroneAnalysisService.cs `CreateDialogueRequest` ~330-473, App/Core/Data/DataIds.cs `Dialogue` ~71-74
- App/UI/Drone/DroneRecommendationPresenter.cs ~131-132·~224-225, App/Integration/EmergencyRescueRuntimeController.cs `ShowDroneReminder` ~326
- 반복 소리 방식: App/Integration/GasPulseAudio.cs(음량 = 칸 × 버스 × `EffectiveVolume(Sfx)`, 음소거 즉시 멈춤)

## A. 슬롯 "딸깍"
- 파일만 넣으면 기존 `OnSlotSelectedSfx`가 16을 낸다. 연타로 쌓이지 않게 `SlotSelectMinGap = 0.12f`★ 안의 두 번째는 생략(`SfxPlayer.SecondsSinceLastPlayed`).
- 마우스·위/아래 키 둘 다 같은 소리. 메뉴 시작·이어하기 뒤 처음 선택 복원(`MainMenuPresenter` ~81 같은 초기화 경로)은 무음.

## B. 빈 슬롯 노이즈 (새 작은 클래스, 예: `App/Audio/EmptySlotNoiseAudio`, 메인 메뉴에 붙임)
- AudioSource 1개, `loop = true`, `spatialBlend 0`, 클립 95. 음량은 매 프레임 `칸 × 버스 × EffectiveVolume(Sfx) × 페이드`.
- 언제 나나(빈 카드 = `SaveSlotCardKind.Empty`):
  1. 빈 슬롯을 **고를 때**(클릭·키): "딸깍"과 함께 `EmptySlotSelectSeconds = 0.6f`★ 동안 나고 `EmptySlotFadeOut = 0.25f`★로 사라진다.
  2. 빈 카드에 **마우스를 올린 동안**: `EmptySlotHoverScale = 0.6f`★ 배로 계속 나고, 마우스가 나가면 같은 페이드로 사라진다. `EmptySlotHoverNoise = true`★로 끌 수 있게.
  3. 그 외(빈 슬롯이 선택돼 있기만 함, 데이터 있는 카드)는 무음. 늘 깔리는 소리가 아니다.
- 무음·바로 멈춤(약 0.05초): 부팅 연출 중(`intro.BlocksInput`), 설정·덮어쓰기·삭제 확인창이 열림(`IsAnyModalOpen`), 새 게임·이어하기 시작, 씬 바뀜·`OnDisable`, 효과음·마스터 음소거, 클립 없음.
- 슬롯 삭제로 카드가 빈 칸이 되는 순간은 무음(B-180 소리 그대로).

## C. Tab 드론 분석 창 (DroneDialoguePanelView.cs)
| 순간 | 위치 | 소리 |
|---|---|---|
| 가로선이 펼쳐지며 열림 | `SetVisible(true)` ~246 `window.Open()`을 실제로 부를 때 | `DroneTerminalOpenSfx = WindowTvOpen(79)`★, `WindowTvSfx`, `DroneTerminalTvScale = 0.6f`★ |
| 글자가 찍힘 | `ApplyTyping` ~571, 보이는 글자 수가 늘어난 프레임 | `DroneTerminalTypeSfx = MainMenuTypeTick(86)`★, `DroneTerminalTypeGap = 0.06f`★, 배율 0.5★ |
| 근거 줄이 하나씩 붙음 | 보이는 근거 줄 수가 늘 때(열기·`appendActive`) | `DroneTerminalReasonSfx = KeycapPop(27)`★, 줄마다 피치 +0.03★ |
| PRIORITY로 바뀜 | `SetOperationalState` ~279, 깜빡임이 실제로 시작하는 `if` 안 | **96 한 번**, `DronePriorityMinGap = 2.0f`★ |
| 접히며 닫힘 | `TryBeginAnimatedClose` ~255가 true일 때 | `DroneTerminalCloseSfx = WindowTvClose(80)`★, 같은 배율 |
- 무음: `SetVisible(false)`·`HideImmediate`·`ResetTerminalFx`, 열기 연출을 건너뛸 때(`SkipTerminalReveal` ~489 뒤 남은 타이핑), 창이 닫혀 있을 때의 PRIORITY 진입, 같은 추천의 대사만 조용히 바뀔 때(`SetDialogue` ~157).
- 타이핑 틱은 한 프레임에 여러 글자가 늘어도 한 번. 닫히는 중에는 타이핑·근거 줄 소리를 멈춘다.

## D. 급한 말풍선 "삐빅" (DroneDialogueSocket.cs)
- B-181의 `DroneUrgentBubbleSfx = WorldFacilityTagBeep`을 아래로 바꾼다. 급한 말은 피치 1.0(새 음원 그대로), B-181의 생략 규칙(핑 0.3초·1.5초 간격·같은 글자·초기화·정지)은 그대로.
- 심각/단순 구분은 순수 함수 하나(예: `DroneUrgentBubbleRule.IsSerious(DroneDialogueResult)`)로, 대사의 `IsUrgent`와 `TemplateId`만 본다.
  - `DroneSeriousUrgentTemplates`★ = `DataIds.Dialogue.DroneEmergency`(전력 0·돌아갈 길 없음·정보 부족 비상), `DroneStructuralWarning`(붕괴 위험이 심각할 때만 급함으로 옴), `DroneGasWarning`(가스)
  - `IsUrgent` && 위 목록 → **98 낮은 "삐빅"**. `IsUrgent` && 그 밖(지금은 `LowPowerWarning` 전력 부족, 클라우드 대사도 같은 TemplateId를 따름) → **97 "삐빅"**. `IsUrgent` false → B-181 보통 비프 그대로.
- 대사가 들어오는 세 길 모두 이 판단 하나를 지난다: `DroneRecommendationPresenter` ~131·~224(분석 대사), `EmergencyRescueRuntimeController.ShowDroneReminder` ~326(구조 안내, 지금 `isUrgent=false`라 보통 비프. 바꾸지 않는다).
- 분석 창(C)이 열려 있어 같은 순간 96이 났으면(`DronePriorityBubbleGap = 0.3f`★ 안) 말풍선 "삐빅"은 생략한다. 경고음 하나만.

## 손대지 말 것
- 16 말고 기존 칸 값, 음원 파일, 슬롯·세이브·삭제 규칙, 드론 긴급도 판단(`DroneAnalysisService`)·대사 쿨다운, 창 연출 시간표, B-178 핑 생략 규칙, B-179~B-182 대상, `AGENTS.md`·규칙 문서

## 테스트
- EditMode 전체에서 기준 브랜치의 기존 실패 말고 새 실패가 없어야 한다. 기대값을 바꾼 검사(16 음량, B-181 급한 말 13 검사 등)는 이름과 이유를 적는다.
- 새 EditMode `PromptB183SfxMenuDroneTests`, 필요하면 `PromptB183SfxMenuDronePlayModeTests`(소리 요청을 잡는 방식은 B-178·B-181 검사를 따름)
  - 칸: 16 파일 이름·0.65, 95~98 버스·음량·파일, 95 임포트 Normalize 끔
  - A: 0.05초 간격 두 번 클릭 → 16 한 번, 0.2초 간격 → 두 번
  - B: 빈 슬롯 선택 → 소리 켜짐 후 0.6초+페이드 뒤 꺼짐, 빈 카드 호버 중 켜짐·나가면 꺼짐, 데이터 카드 0번, 모달 열림·음소거 즉시 멈춤
  - C: 열기 79 한 번, 타이핑 틱 간격 ≥0.06초, 근거 줄 수만큼 27, PRIORITY 진입 96 한 번·2초 안 재진입 0번·창 닫힘 중 0번, 닫기 80 한 번, `SetVisible(false)` 0번
  - D: `IsSerious` 표(비상·심각 붕괴·가스 → 98, 전력 부족 → 97, 급하지 않음 → 13), 96 직후 말풍선 0번, 구조 안내 → 13
- PlayMode는 고친 검사만. 실행 전후 `git status`로 범위 밖 프리팹·폰트·씬·PNG·TimeManager 변경을 확인하고 되돌린다.

## 사용자 테스트 가이드 (보고서에 그대로 넣기)
1. 메인 화면에서 데이터가 있는 슬롯을 누른다 → 쇠 프레임 "딸깍" 한 번. 빠르게 여러 번 눌러도 소리가 겹겹이 쌓이지 않는다.
2. 위/아래 키로 슬롯을 옮겨도 같은 "딸깍".
3. 빈 슬롯("신호 없음")을 누른다 → "딸깍"과 함께 TV "지직"이 잠깐 났다가 사라진다.
4. 빈 슬롯에 마우스를 올려 둔다 → 작은 "지직"이 계속 나고, 마우스를 빼면 사라진다. 데이터 있는 슬롯에서는 안 난다.
5. 아무것도 안 하고 메인 화면을 두면 "지직"이 나지 않는다. 설정창을 열면 바로 멈춘다.
6. 광산에서 Tab으로 드론 분석 창을 연다 → 작은 TV 켜짐 소리, 글자가 찍히는 "틱틱", 근거 줄이 붙을 때 "톡톡".
7. 창을 연 채 위험한 상황(가스·붕괴 위험·전력 부족)을 만든다 → 창이 PRIORITY로 깜빡일 때 경고음 한 번.
8. Tab으로 닫는다 → 작은 꺼짐 소리.
9. 창을 닫은 채 전력이 부족해진다 → 말풍선과 함께 "삐빅".
10. 가스 근처나 붕괴 직전 블록 옆에서 드론이 급한 말을 한다 → 더 낮은 "삐빅".
11. 보통 말풍선은 예전처럼 높은 "삑" 그대로. 분석 창이 열려 있으면 경고음과 "삐빅"이 겹치지 않는다.
12. 효과음 음량·음소거가 이번 소리에 다 적용되고, Unity Console에 새 빨간 오류·노란 경고가 없다. 바꿔 보고 싶은 소리·크기(★)가 있으면 PM에게 알려 준다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 슬롯 딸깍·빈 슬롯 노이즈·드론 분석 창·급한 말풍선 소리를 연결 (프롬프트 B-183)`). 새 브랜치에 일반 push, 브랜치는 지우지 않는다. PR은 만들지 않는다.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue158-주제.md` 파일 하나로 기록. **`init/rules/lessons.md`에 줄을 추가하지 않는다.**
- **GitHub 이슈 #158은 고치지 않는다.** 체크는 PM이 확인 뒤 한다. 보고서의 대응 표에만 적는다.
- 결과 보고서 `work_process/MVP2/sound/B-183-sfx-menu-drone-result.md`
  - 맨 앞: 빈 슬롯 노이즈 해석("쓸 때만": 고를 때 잠깐 + 마우스를 올린 동안)
  - 기준 브랜치·커밋(B-179-1·B-180·B-180-1·B-181·B-182 포함 여부, 만든 번호와 이유), 소리마다 붙인 곳(파일·줄)과 SfxId·배율, ★ 상수 위치
  - D 심각/단순 목록과 판단 함수 위치, 임포트 예외(95), 테스트 결과, 직접 들어 보지 못한 항목은 "미검증", 위 사용자 테스트 가이드
  - **#158 대응 표**(보고서 안에만): 슬롯 딸깍 / 빈 슬롯 노이즈 / 드론 분석 창(열림·타이핑·근거 줄·PRIORITY·닫힘) / 급한 말풍선 각각 구현·일부·못 함
