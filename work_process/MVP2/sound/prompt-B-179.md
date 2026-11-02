# B-179 설정 슬라이더 드래그 소리 다시 + 떠날 때 시계 꺼짐 소리 + 키 조작 창 호버음 (#158 D-2 UI 공통·메뉴)

## 목표
- PM이 B-177(`MVP2-fix122` `ce7a9175`)을 직접 해 보고 고칠 점 세 가지를 반영한다.
  - A. 음량 슬라이더를 끌 때 틱(UiScanTick 8)이 계속 겹쳐 "뽁뽁" 거슬림 → **한 번 끄는 동안 소리 한 번**. 가운데를 늘린 새 소리(칸 91)를 쓰고, 소리가 끝난 뒤 계속 끌어도 다시 나지 않는다.
  - B. 엘리베이터로 광산을 떠날 때 시계가 강제로 꺼지는 연출이 무음 → T키로 끌 때와 같은 꺼짐 소리(WindowTvClose 80).
  - C. 키 조작 변경 창: 창 안 버튼 호버음, 키보드 그림의 키 호버음, 마우스 그림 호버는 클릭음.
- 소리 id는 연결 지점마다 상수 하나로 둬서 PM이 번호만 바꿔 끼울 수 있게 한다.
- 필요하면 다른 에이전트에게 나눠 맡겨도 된다. 결과는 한 브랜치로 모은다.

## 브랜치
1. `git fetch`로 원격 `MVP2-fixNNN` 중 가장 큰 번호를 확인한다. PM 확인 시점(2026-10-31 22:20 KST)에는 **`MVP2-fix122`**(`ce7a9175`, B-177)이 최신이고 B-178은 아직이다.
2. B-178 커밋(`… (프롬프트 B-178)`)이 들어간 `MVP2-fix123`이 있으면 그 위에서 **`MVP2-fix124`**를 만든다.
3. 없으면 `MVP2-fix122`(`ce7a9175`)에서 **`MVP2-fix123`**을 만들고, 보고서 맨 위에 "B-178 없이 fix122 기준"이라고 적는다. 이 경우에도 새 칸은 **91**로 하고 87~90은 B-178 몫으로 비워 둔다(번호 사이가 비어도 된다. 3번도 비어 있음).
4. 기준에 B-177 커밋(`feat: 설정창·우측 메뉴·시계 효과음과 버튼 소리를 연결 (프롬프트 B-177)`)이 없으면 멈추고 보고한다.

## 절대 규칙
1. 기존 SfxId 번호·칸 값·버스 음량·음원 파일은 바꾸지 않는다. 새 칸은 91 `SettingsSliderDrag` 하나.
2. **새 음원은 고치지 않는다.** 길이·페이드는 이미 다듬은 파일 그대로. 조정은 칸 값(음량·피치·maxDuration·fadeOut)으로만.
3. App 코드는 `SfxPlayer.Play`. `SfxPlayer`에 멈춤 API를 추가하지 않는다. 버튼은 `UiSfx.Attach`로, 코드가 이미 결과음을 내는 버튼은 `hover: true`, 클릭 `SfxId.None`.
4. 씬·프리팹 YAML은 고치지 않는다. 연출 시간·모양, 설정 저장, 조작 방식 저장, B-164~B-178 소리 연결은 바꾸지 않는다(아래 B의 출발 무음만 예외).
5. `UiScanTick`(8)은 시설 이름표 스캔선(`FacilityNameTagVisual` ~411)도 쓰므로 그대로 둔다. 설정 슬라이더만 91로 바꾼다.
6. 줄 번호는 fix122 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 음원 넣기
- 사용자가 `sub-terra-sfx-B179.zip`을 풀어 `work_process/MVP2/sound/sfx-B179/`에 넣어 둔다. 없으면 멈추고 보고한다.
- `settings_slider_drag.wav` → `Assets/_Project/Audio/SFX/UI/`로 옮기고 작업 폴더 쪽 wav는 지운다. mp3·README·`address-rows.md`는 그 폴더에 둔다.
- 임포트 설정은 기존 빌더(`PromptB156SfxLibraryBuilder`, 모노·로드 시 압축 해제)를 따른다.
- `init/sound-effect-address.md` 표 끝에 `address-rows.md`의 1줄을 붙인다(같은 이름 줄이 있으면 고친다).

| 칸 | 이름 | 파일 | 버스 | 시작 음량 | 길이 |
|---|---|---|---|---|---|
| 91 | SettingsSliderDrag | settings_slider_drag | UI | 0.13 | 0.88초 |
- 소리 모양: 기존 `ui_scan_tick`에서 앞 무음을 빼고 가운데 "웅웅" 구간만 음 높이 그대로 약 1.8배로 늘림(시작 "탁"·끝 꼬리는 그대로). 피크 -1dBFS라 0.13이면 지금 틱(0.5)과 비슷한 크기.
- `Shared/Audio/SfxId.cs`, `App/Audio/SfxLibraryDefaults.cs`(파일 끝, 길이 주석 포함), `Resources/SfxLibrary.asset` 세 곳에 같게 넣는다.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/Scripts/` 기준)
- #158 D-2 "UI 공통 · 메뉴"의 B-179 줄 3개, `work_process/MVP2/sound/B-177-sfx-menu-result.md`, `work_process/MVP2/sound/sfx-B179/README.md`
- App/UI/MainMenu/SettingsMenuSkin.cs: `VolumeTickSfx`·`volumeTickThrottle` ~21-23, `OnEnable` ~76(~79 스로틀 리셋), `OnDisable` ~122, `HookVolumeSliders` ~418, `OnVolumeChanged` ~442-452, `WireParticleTriggers`·`WireBurst` ~663-680(슬라이더에 PointerDown·Drag `EventTrigger`), `RefreshVolumeSoundSteps` ~454
- App/UI/MainMenu/SettingsAudioRows.cs(값 다시 그리기는 `SetValueWithoutNotify`), SettingsMuteButton.cs ~91(이번엔 손대지 않음)
- App/UI/HUD/MineResetClockOverlay.cs: `BeginDeparture` ~143, `EndDeparture` ~150, `PowerOnSfx`·`PowerOffSfx` ~380, `ToggleClockFromInput` ~383-396(`!departing` 조건)
- App/Save/SaveRuntimeController.cs: `ExecuteTimedMineReset` ~805(~819 `BeginDeparture`, 3시간 만료 경로), `WaitForClockPowerOff` ~1193(~1201 `BeginDeparture`, 엘리베이터 출발 ~858·~1183 경로)
- App/UI/MainMenu/ControlSchemePanel.cs: `Cycle` ~76(KeysPageTurn), `Open` ~86, `Close` ~105(UiPopupClose), `Build` ~141(이전/다음·"선택 후 돌아가기"·X 버튼 ~171-176), `Button` ~219(`ControlTerminalButton`, 소리 없음)
- App/UI/ControlTerminal/ControlTerminalKeyCap.cs(`OnPointerEnter` ~247, `IsFunctionKey` ~60, `Def.IsMouse` ~134, 기능 없는 키는 `raycastTarget` 꺼짐 ~191), ControlTerminalView.cs(`OnKeyEntered` ~585, `IsIntroPlaying`·`IsOutroPlaying`·`IsHidden` ~130), ControlTerminalLayout.cs ~100(마우스 키 `ControlTerminalBindings.MouseKey`)
- 검사 양식: Tests/EditMode/App/Audio/PromptB177SfxMenuTests.cs, Tests/PlayMode/Integration/Audio/PromptB177SfxMenuPlayModeTests.cs(~42-80 슬라이더, ~157 출발 무음)

## A. 음량 슬라이더: 끄는 동안 한 번 (`SettingsMenuSkin`)
- 상수 `VolumeDragSfx = SfxId.SettingsSliderDrag`. 피치는 1.0 고정(값에 따라 높이를 바꾸면 길이도 바뀌므로 B-177의 0.9~1.1 피치는 뺀다).
- "한 번 끄기" = 슬라이더 위에서 포인터를 누른 때부터 뗄 때까지. `WireBurst` 옆에 PointerDown·PointerUp(+EndDrag) 트리거를 더해 슬라이더별로 "끄는 중" 상태를 둔다(기존 입자 트리거는 그대로).
  1. 끄는 중 5% 칸이 처음 바뀔 때 한 번 낸다. 트랙을 눌러 값이 바로 바뀐 것도 이 한 번이다.
  2. 같은 끄기 동안에는 소리가 끝난 뒤(0.88초 넘게 계속 끌어도) **다시 내지 않는다.** 되풀이·이어 붙이기 없음.
  3. 누르기만 하고 값이 안 바뀌면 무음. 손을 뗀 뒤 다시 누르면 새 끄기로 본다.
- 끄는 중이 아닐 때 값이 바뀌면(방향키·게임패드, 스크롤) 한 번 내되, 마지막 소리에서 상수 `VolumeKeySfxGap = 0.88f`(소리 길이) 안이면 무음. 겹쳐 들리지 않게 하는 것이 목적.
- 그대로: 다시 열기·`SetValueWithoutNotify`·음소거 버튼·기본값으로 값이 바뀔 때 무음(B-177 `volumeSteps` 방식 유지), 5% 칸 계산, 입자·글로우.
- 창이 닫히거나(`OnDisable`) 슬라이더가 꺼지면 "끄는 중"을 풀어, PointerUp을 놓쳐도 다음 끄기에 소리가 난다.
- `volumeTickThrottle`(0.07초)는 필요 없으면 지우고, 남기면 이유를 적는다.

## B. 엘리베이터로 떠날 때 시계 꺼짐 소리 (`MineResetClockOverlay.BeginDeparture`)
- B-177이 일부러 무음으로 둔 것을 되돌린다. `BeginDeparture`가 불릴 때, 그 직전에 시계가 화면에 켜져 있었고(`IsClockOnScreen` && 전원 켜짐) 실제로 꺼짐 연출로 바뀌면 `PowerOffSfx`(WindowTvClose 80) 한 번.
- 이미 꺼져 있거나 시계를 안 켰으면 무음. `departing` 중 재호출(`BeginDeparture` 두 번)은 무음. 이동이 취소돼 `EndDeparture`로 다시 켜질 때 켜짐음을 낼지는 지금처럼 무음으로 두고 보고서에 적는다.
- 3시간 만료 경로(`ExecuteTimedMineReset` ~819)도 같은 꺼짐인지 확인한다. 그 순간 `TimerEnd` 벨·초기화 팝업 소리가 같이 나면 거기서는 무음으로 두고(인자나 별도 메서드로 구분), 어떻게 했는지 적는다.
- T키 켜고 끄기(B-177)는 그대로. 씬이 바뀌어 소리가 잘리면 "잘림"으로 보고만 한다(이동 대기 시간은 바꾸지 않음).

## C. 키 조작 변경 창 호버음 (`ControlSchemePanel`, `ControlTerminalKeyCap`)
| 대상 | 호버 | 클릭 |
|---|---|---|
| 이전·다음 화살표 | UiHover(2) | 지금처럼 KeysPageTurn(코드가 냄) → `UiSfx.Attach(button, SfxId.None, hover: true)` |
| "선택 후 돌아가기"·X | UiHover(2) | 지금처럼 UiPopupClose(코드가 냄) → 호버만 |
| 키보드 그림의 기능 키(글자가 켜진 키) | 상수 `KeyHoverSfx = SfxId.UiHover`(★) | 없음 |
| 마우스 그림(`Def.IsMouse`) | 상수 `MouseHoverSfx = SfxId.UiClick`(★, 마우스 "딸깍"으로 씀. 따로 마우스 클릭 음원은 없음) | 없음 |
- 키 호버음은 `ControlTerminalView.OnKeyEntered` 쪽(창 상태를 아는 곳)에서 낸다. 등장 연출 중(`IsIntroPlaying`, 키캡 "톡톡"과 겹침)·닫힘 연출·숨김 때는 무음.
- 기능 없는 키(흐린 키)는 지금처럼 포인터를 받지 않으므로 무음. 조작 방식을 바꿔 기능이 생긴 키는 그때부터 난다.
- 키 사이를 빠르게 훑을 때 쌓이지 않게 `SfxThrottle` 상수 `KeyHoverGap = 0.06f`. 같은 키에 머무는 동안 다시 나지 않는다(`OnPointerEnter`가 다시 올 때만).
- 자동 시연(`ControlTerminalAutoDemo`, `SetDemoDown`)으로 키가 눌리는 것은 무음.
- 설정창 "키 조작 변경" 버튼은 B-177대로 호버만(열림음 KeysHoloOpen).

## 손대지 말 것
- `UiScanTick` 칸·음원·시설 이름표 틱, 설정 값 저장, 시계 연출 시간표·이동 대기 시간, 키 조작 창 연출·시연·저장
- B-178의 칸 87~90과 그 연결(이미 들어와 있으면 그대로), `AGENTS.md`·규칙 문서

## 테스트
- EditMode 전체에서 기준 브랜치의 기존 실패 말고 새 실패가 없어야 한다. 기대값을 바꾼 기존 검사(B-177 슬라이더·출발 무음 검사)는 이름과 이유를 적는다.
- 새 EditMode `PromptB179SfxFeedbackTests`, PlayMode가 필요하면 `PromptB179SfxFeedbackPlayModeTests`(소리 요청을 잡는 방식은 B-177 검사를 따름)
  - 칸 91: 이름·버스·음량·파일 이름, 파일이 `Audio/SFX/UI`에 있음, 칸 8은 그대로
  - 슬라이더: 한 끄기에서 0→100% 여러 번 이동 1번, 1초 넘게 계속 끌어도 1번, 떼고 다시 끌면 1번 더, 누르기만 하면 0번, `SetValueWithoutNotify`·다시 열기 0번, 끄는 중 아닌 연속 변화는 0.88초 안 1번
  - 시계: 켜진 채 `BeginDeparture` 1번 WindowTvClose, 꺼진 채 0번, 두 번 호출 1번, T키 켜고 끄기 각 1번 그대로
  - 키 조작 창: 버튼 4개 호버만(클릭 None), 기능 키 들어감 UiHover 1번, 마우스 키 UiClick 1번, 등장 연출 중·숨김 0번, 0.06초 안 두 키 1번
- PlayMode는 고친 검사만. 실행 전후 `git status`로 범위 밖 프리팹·폰트·씬·PNG·TimeManager 변경을 확인하고 되돌린다.

## 사용자 테스트 가이드 (보고서에 그대로 넣기)
1. 메인 화면 설정에서 음량 막대를 잡고 길게 이리저리 끈다 → 처음에 "웅-" 소리가 한 번만 나고, 계속 끌어도 겹치거나 다시 나지 않는다.
2. 손을 뗐다가 다시 잡고 끈다 → 다시 한 번 난다. 막대 아무 곳이나 한 번 눌러도 한 번 난다.
3. 설정창을 닫았다 다시 열기만 하거나, 음소거·기본값을 누르면 이 소리는 나지 않는다.
4. 소리 크기가 예전 틱과 비슷한지, 너무 길거나 늘어진 느낌인지 들어 본다.
5. 지상 기지·광산 설정창에서도 1~3번이 똑같다. 시설 이름표가 뜰 때 스캔선 틱 소리는 예전 그대로다.
6. 광산에서 T키로 시계를 켜 두고 엘리베이터를 타고 올라간다 → 시계가 꺼질 때 T키로 끌 때와 같은 꺼짐 소리가 한 번 난다.
7. 시계를 꺼 둔 채 엘리베이터를 타면 꺼짐 소리가 나지 않는다. T키로 켜고 끄는 소리는 예전 그대로다.
8. 설정 → "키 조작 변경" 창을 연다 → 열리는 동안 키에 마우스가 있어도 호버음이 섞이지 않는다.
9. 이전·다음 화살표, "선택 후 돌아가기", X에 마우스를 올리면 호버음이 난다. 누르면 넘김·닫힘 소리만 난다(두 번 안 남).
10. 키보드 그림에서 글자가 켜진 키에 마우스를 올리면 호버음이 나고, 키를 빠르게 훑어도 소리가 쌓이지 않는다. 흐린 키는 무음.
11. 마우스 그림에 올리면 "딸깍" 클릭음이 난다.
12. 효과음 음량·음소거가 새 소리에 다 적용되고, Unity Console에 새 빨간 오류·노란 경고가 없다. 바꿔 보고 싶은 소리(★)가 있으면 PM에게 알려 준다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 슬라이더 드래그 소리를 한 번으로 바꾸고 떠날 때 시계 꺼짐·키 조작 창 호버음을 연결 (프롬프트 B-179)`). 새 브랜치에 일반 push, 브랜치는 지우지 않는다. PR은 만들지 않는다.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue158-주제.md` 파일 하나로 기록. **`init/rules/lessons.md`에 줄을 추가하지 않는다.**
- 결과 보고서 `work_process/MVP2/sound/B-179-sfx-feedback-result.md`
  - 기준 브랜치·커밋(B-178 포함 여부), 소리마다 붙인 곳(파일·줄)과 SfxId, ★ 상수 위치, 3시간 만료 경로를 어떻게 했는지
  - 테스트 결과, 직접 들어 보지 못한 항목은 "미검증", 위 사용자 테스트 가이드
  - **#158 대응 표**: 슬라이더 드래그 소리 / 떠날 때 시계 꺼짐 / 키 조작 창 호버 각각 구현·일부·못 함
