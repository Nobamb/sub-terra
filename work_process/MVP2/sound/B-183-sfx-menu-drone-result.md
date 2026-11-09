# B-183 메인 메뉴·드론 효과음 적용 결과

빈 슬롯 노이즈는 **쓸 때만** 난다. 고를 때 0.6초간 재생한 뒤 0.25초로 사라지고, 마우스를 올린 동안에는 0.6배로 재생하다가 벗어나면 0.25초로 사라진다. 빈 슬롯이 선택돼 있기만 할 때는 무음이다.

## 기준과 범위

- 기준: `origin/MVP2-fix129`, `f3b7f857` (`fix: adjust cargo popup feedback`). `git fetch` 후 가장 큰 원격 번호가 129임을 확인했다.
- 선행 포함: B-179 `b1471534`, B-179-1 `2b474db7`, B-180 `22f2f5f5`, B-180-1 `d2f54697`, B-181 `7c2c2957` 및 구조 도착음 수정 `43e0efda`, B-182 `0ed7ac9d` 및 후속 수정 `f3b7f857`.
- 새 브랜치: `MVP2-fix130`. 비어 있는 다음 번호이며 B-181·B-182를 모두 포함한다.
- **번호 충돌 해결(사용자 승인):** 기존 95번은 B-180-1의 `LockedNodeRattle`이다. 이 번호와 B-184 예약 99~101을 보존하고, B-183 신규 네 칸은 **102~105**로 배정했다. 원문 95~98과 다른 유일한 번호 배정이다. B-184가 같은 폴더를 사용하더라도 99~101은 그대로 사용할 수 있다.
- B-183 WAV 5개만 복사했다. 각 파일은 작업 폴더의 원본과 SHA-256이 동일하다. B-184 WAV 3개·MP3·README·address-rows는 작업 폴더에 남겼다.
- GitHub [#158](https://github.com/Nobamb/sub-terra/issues/158)의 B-183 대응 줄을 읽었으며 이슈를 수정하지 않았다.
- 사용자 후속 요청에 따라 B-183 구현·검증 결과를 커밋·푸시하고 main 대상 PR을 작성한다. 작업 시작부터 있던 `init/prompt-B.md` 수정과 B-183/B-184 원본 문서·음원은 커밋 범위에서 제외하고 로컬에 보존했다.

## 연결 지점과 조절 값

아래 경로는 `sub-terra/Assets/_Project/Scripts/` 기준이다.

| 동작 | 파일·지점 | 효과음과 값 |
|---|---|---|
| 슬롯 선택 딸깍 | `App/UI/MainMenu/MainMenuBinder.cs:150`, `OnSlotSelectedSfx` | 16 `UiSlotSelect`, 칸 음량 0.65. ★ `SlotSelectMinGap = 0.12` (`:25`). 실제 재생 기록으로 연타를 생략한다. 마우스·키보드가 기존 같은 이벤트를 사용한다. |
| 빈 슬롯 선택 | 같은 메서드 → `MainMenuView.PlaySlotSelectedNoise` (`:266`) → `EmptySlotNoiseAudio.SelectSlot` | 102 `EmptySlotNoiseLoop`, UI, 칸 음량 0.4. 선택된 카드가 `Empty`인 경우에만 요청한다. |
| 빈 슬롯 호버 | `App/UI/MainMenu/MainMenuView.cs:467`, `OnCardHovered`·`OnCardUnhovered` | 102. 기존 카드 이벤트에 연결한다. 데이터 있는 카드·초기 선택 복원·삭제로 빈 슬롯이 되는 순간은 무음. |
| 반복 소스 수명 | `App/Audio/EmptySlotNoiseAudio.cs`, `MainMenuView.Update` (`:394`) | 소스 하나, loop=true, spatialBlend=0, ignoreListenerPause=true. 음량 = 칸 × 버스 × `EffectiveVolume(Sfx)` × 페이드. ★ `EmptySlotNoiseSfx`, `EmptySlotSelectSeconds=0.6`, `EmptySlotFadeOut=0.25`, `EmptySlotHoverScale=0.6`, `EmptySlotHoverNoise=true` (파일 맨 위). |
| 노이즈 중단 | `MainMenuView.SetSettingsVisible`, `SetOverwriteConfirmVisible`, `SetDeleteConfirmVisible`, `PlayExitTransition`, `OnDisable`; `EmptySlotNoiseAudio.Tick` | 모달·부팅/전환 입력 차단·음소거·클립 없음·비활성화 때 즉시 정지. 중단한 선택/호버를 뒤늦게 재생하지 않는다. |
| 드론 분석 창 열기 | `App/UI/Drone/DroneDialoguePanelView.cs:264`, 실제 `window.Open()` 직후 | ★ `DroneTerminalOpenSfx=WindowTvOpen(79)`, `DroneTerminalTvScale=0.6`. `WindowTvSfx` 사용. |
| 타이핑 | 같은 파일 `ApplyTyping` → `PlayTypingSfx` (`:682`) | ★ `DroneTerminalTypeSfx=MainMenuTypeTick(86)`, `DroneTerminalTypeGap=0.06`, `DroneTerminalTypeScale=0.5`. 한 프레임에 글자가 여러 개 늘어도 틱 한 번. 커서 깜빡임은 세지 않는다. |
| 근거 줄 | 같은 `PlayTypingSfx` | ★ `DroneTerminalReasonSfx=KeycapPop(27)`, `DroneTerminalReasonPitchStep=0.03`. 실제 보이는 줄 수 증가만큼 요청하고 첫 줄 1.0부터 높인다. 열기·추천 변경·근거 추가 경로를 공통으로 관찰한다. |
| PRIORITY 진입 | 같은 파일 `SetOperationalState` (`:300`), 실제 깜빡임 시작 분기 | 103 `DronePriorityAlert`, UI, 칸 음량 0.5. ★ `DronePrioritySfx`, `DronePriorityMinGap=2.0`. 창이 닫혀 있거나 접히는 중이면 무음. |
| 드론 창 닫기 | 같은 파일 `TryBeginAnimatedClose` (`:274`) | ★ `DroneTerminalCloseSfx=WindowTvClose(80)`, TV 0.6배. 이미 닫는 중이면 재요청하지 않는다. `SetVisible(false)`·OnDisable·Reset·건너뛰기에는 추가 소리가 없다. |
| 급한 말풍선 | `App/UI/Drone/DroneDialogueSocket.cs:190`, `TryPlayBubbleSfx` | ★ `DroneUrgentBubbleSfx=104`, `DroneSeriousUrgentBubbleSfx=105`, `DroneUrgentBubblePitch=1.0`, `DronePriorityBubbleGap=0.3` (파일 맨 위). 103이 0.3초 안에 실제 재생됐으면 급한 말풍선 소리는 생략한다. |

## 말풍선 분류

- 순수 함수: `App/UI/Drone/DroneUrgentBubbleRule.cs`, `IsSerious(DroneDialogueResult)`.
- ★ `DroneSeriousUrgentTemplates`: `DataIds.Dialogue.DroneEmergency`, `DroneStructuralWarning`, `DroneGasWarning`.
- `IsUrgent=true`이고 위 목록이면 105(낮은 삐빅), 나머지 급한 말(전력 부족 포함)은 104(삐빅). 클라우드 대사도 결과의 TemplateId와 IsUrgent만 사용한다.
- `IsUrgent=false`는 기존 13 `WorldFacilityTagBeep`, 피치 1.35다. 구조 안내의 `isUrgent=false`는 그대로이며 일반 비프를 사용한다.
- 분석/클라우드/구조 안내가 모두 기존 `DroneDialogueSocket.SetDialogue` 진입점을 사용한다. Producer와 긴급도 판단·대사 쿨다운은 수정하지 않았다.
- 기존 B-181의 첫 대사·바인딩 후 1초·핑 후 0.3초·비프 간 1.5초·동일 글자·정지/설정 중 생략 규칙을 보존했다.

## 에셋·임포트·통합

- `Shared/Audio/SfxId.cs`, `App/Audio/SfxLibraryDefaults.cs`, `Resources/SfxLibrary.asset`에 동일한 번호·파일·버스·음량을 적용했다. 새 칸의 길이는 102=2.000초, 103=0.600초, 104/105=0.230초다.
- `Editor/DataValidation/PromptB156SfxLibraryBuilder.cs`의 임포트 예외에 `empty_slot_noise_loop`를 추가했다. **102(원문 95)는 Normalize 끔 + PCM + DecompressOnLoad**, `LoopFileNames`에는 넣지 않았다.
- `Editor/DataValidation/PromptB183SfxLibraryBuilder.cs`의 `SubTerra > Audio > Apply Prompt-B 183 Menu Drone Sfx`로 기존 빌더를 호출하고 16번 음량만 0.65로 갱신했다. 에셋은 Unity Editor API로 저장했다.
- 기존 칸 95·버스 음량·폴백(16→UiClick)은 유지했다. Alert 버스와 SfxPlayer 반복/중단 API는 추가하지 않았다.
- `init/sound-effect-address.md`의 ui_slot_select 출처를 교체하고 B-183 4줄만 추가했다.
- 씬·프리팹 변경 및 신규 Inspector 수동 배선은 필요 없다. 메인 메뉴 View가 런타임에 노이즈 컴포넌트를 붙인다. 기존 드론 창/말풍선 참조를 그대로 사용한다.
- ProjectSettings·Packages 변경을 작업 결과에 포함하지 않는다. 테스트가 만든 범위 밖 에셋/설정 변경은 시작 상태로 복구한다.

## 변경 파일

- WAV 및 새 `.meta`: `Audio/SFX/UI/{ui_slot_select,empty_slot_noise_loop,drone_priority_alert}.wav`, `Audio/SFX/World/{drone_bubble_urgent,drone_bubble_urgent_serious}.wav`.
- 런타임: 위 표의 MainMenuBinder/MainMenuView/DroneDialoguePanelView/DroneDialogueSocket, 새 EmptySlotNoiseAudio/DroneUrgentBubbleRule, Shared/Audio/SfxId.cs, App/Audio/SfxLibraryDefaults.cs.
- 에디터: 기존 PromptB156SfxLibraryBuilder, 새 PromptB183SfxLibraryBuilder. 데이터: Resources/SfxLibrary.asset.
- 테스트: 새 `Tests/EditMode/App/Audio/PromptB183SfxMenuDroneTests.cs`, `Tests/PlayMode/Integration/Audio/PromptB183SfxMenuDronePlayModeTests.cs` 및 새 `.meta`.
- 기존 테스트 기대값: PromptB156SfxTests / PromptB165SfxD1FixTests / PromptB168SfxD2Tests / PromptB178SfxTests / PromptB1791SfxFeedbackFixTests의 전체 칸 수 94→98(신규 4개), PromptB181SfxIngameFillTests의 급한 말 상수와 `Bubble_UrgentLinePlaysDedicatedBeepOnce`(13·1.35→104·1.0). 16번 음량은 새 칸 검사가 0.65를 검증한다.
- 문서: 이 보고서, `init/sound-effect-address.md`, `B-183-verification/`, 교훈 `init/rules/lessons/2026-11-04-issue158-typing-local-scope.md`와 `2026-11-04-issue158-editmode-before-playmode.md`.

## 검증

- Unity 6000.5.4f1의 열린 에디터에서 임포트·컴파일·테스트를 실행했다. CLI는 Pipeline 연결을 찾지 못해 기존 프로젝트의 Temp 플래그/Editor 실행기를 사용했다. 패키지/버전을 변경하지 않았다.
- 변경 전 전체 EditMode: **2,355 통과 / 23 실패 / 0 건너뜀**. `B-183-verification/editmode-baseline.txt`.
- 관련 EditMode: **85 통과 / 0 실패**. 신규 B-183와 B-181/B-156/B-179-1 관련 묶음. `B-183-verification/editmode-targeted.txt`.
- 최종 PlayMode: **4 선택 / 4 실행 / 4 통과 / 0 실패 / 0 건너뜀**. `B-183-verification/playmode-summary.txt`.
  - 실제 Update·AudioSource 재생, 클릭·NavigateSlot·카드 포인터 이벤트와 바인더 연결, 선택 만료·호버·모달·음소거·OnDisable·출발 중단.
  - 분석 창 열기/닫기 79/80·0.6배, 틱 최소 0.06초, 근거 3줄 27번 3회, 2초 이내 PRIORITY 재진입 생략, 닫는 중 무음, 건너뛰기 무음, 103 직후 말풍선 생략.
  - 격리된 런타임 화면을 절반 해상도로 캡처해 확인했다: `B-183-verification/terminal-priority.png`.
- 최종 전체 EditMode: **2,374 통과 / 24 실패 / 0 건너뜀**. B-183 신규 20개는 모두 통과했다. `B-183-verification/editmode-full.txt`.
- 기준 23개 실패는 그대로다. 추가 1개는 수정하지 않은 `PlayerLadderMiningTests.Damage_ResumesThirteen_KnockoutInterrupts`이다. 기대 프레임 12, 전체 실행에서 13, 단독 재실행에서 14로 실패했다(`editmode-timing-recheck.txt`). 이 검사는 `PlayerAnimationController.LateUpdate`를 직접 호출하며 실제 `Time.unscaledDeltaTime`으로 사다리 시퀀스를 진행한다. 기존 `2026-10-31-issue158-runtime-test-isolation.md`에 기록된 PlayMode 종료 후 시간값 의존 문제와 일치한다. 이번 오디오 경로를 사용하지 않으며 범위 밖 테스트/게임플레이 코드는 수정하지 않았다. **전체 테스트가 모두 통과했다고 보고하지 않는다.** 비교: `editmode-failure-comparison.txt`.
- 최종 PlayMode 결과 XML에 Warning/Exception/audio-listener 오류 출력은 없었다. 전체 EditMode에는 위 기존 실패/추가 시간 의존 실패가 남는다. 이번 구현 중 지역 변수 충돌로 발생했던 CS0136은 수정했으며 이후 컴파일과 테스트가 실행됐다.
- 음원 SHA-256 5개 일치, 라이브러리 diff는 16번 클립/음량 및 102~105 추가뿐, 임포터 Normalize=0·PCM 확인.

### 미검증

- **실제 청취:** 쇠 프레임 소리의 크기, 빈 슬롯 TV 노이즈 이음새/체감 크기, TV 켜짐과 타이핑/근거 소리의 조화, 낮은 삐빅의 구분감은 직접 듣지 못했다.
- **실제 플레이:** 사용자 세이브를 사용하는 Bootstrap → 새 게임/이어하기 → 탐사, 물리 마우스·키보드 입력, 실제 위험 발생·클라우드 대사·구조 안내의 청취는 미검증이다. 위 PlayMode는 격리된 테스트 UI와 이벤트/API 호출을 검증한다.
- Windows 빌드는 미검증이다. 아래 가이드로 실제 게임에서 확인한다.
- 추가 시간 의존 EditMode 실패는 미해결이다. 다음 전체 검증은 에디터 시작 후 **EditMode → PlayMode** 순서로 진행하고, 해당 사다리 애니메이션 검사는 별도 범위에서 시간 격리를 검토해야 한다.

## 사용자 테스트 가이드

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

## #158 대응 표 (보고서만, 이슈 체크는 수정하지 않음)

| 항목 | 상태 | 비고 |
|---|---|---|
| 슬롯 프레임 딸깍 | 구현 | 16·0.65, 0.12초 연타 제한 |
| 빈 슬롯 노이즈 | 구현 | 102, 선택/호버 때만, 모달·정지·출발 무음 |
| 드론 창 열림 | 구현 | 79·0.6배 |
| 드론 창 타이핑 | 구현 | 86·0.5배, 최소 0.06초 |
| 드론 창 근거 줄 | 구현 | 27, 줄별 피치 +0.03 |
| 드론 창 PRIORITY | 구현 | 103 UI, 2초 간격 |
| 드론 창 닫힘 | 구현 | 80·0.6배, 조용한 정리 제외 |
| 급한 말풍선 | 구현 | 단순 104 / 심각 105, PRIORITY 중복 억제, 일반 13 보존 |
