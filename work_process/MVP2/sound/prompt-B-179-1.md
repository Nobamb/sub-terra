# B-179-1 슬라이더 소리 "잡는 동안 반복 + 놓으면 끝소리" + 크기 키우기 + 키 조작 창 키 소리 + ESC로 함께 닫기 (#158 D-2 UI 공통·메뉴)

## 목표
- PM이 B-179(`MVP2-fix124`)를 직접 해 보고 고칠 점 세 가지를 반영한다.
  - A. 음량 슬라이더: 한 번만 "웅-" 하고 끝나니 느낌이 약하다 → **잡고 끄는 동안 가운데 "웅웅"이 계속 반복되다가, 손을 떼면 끝소리로 이어지며 멈춘다.** 그리고 다른 효과음보다 훨씬 작으니 키운다.
  - B. 키 조작 변경 창: 글자가 켜진 키에 마우스를 올리면 호버음(UiHover) 대신 **창이 열릴 때 나는 키 "톡" 소리(KeycapPop 27)**.
  - C. 키 조작 변경 창이 열린 채 ESC: 설정창이 닫히면서 키 창이 뚝 사라진다 → **키 창도 원래 닫힘 연출로 같이 사라진다.**
- 소리 id·음량·간격은 연결 지점마다 상수 하나로 둬서 PM이 숫자만 바꿔 맞출 수 있게 한다.
- 필요하면 다른 에이전트에게 나눠 맡겨도 된다. 결과는 한 브랜치로 모은다.

## 브랜치
1. `git fetch` 후 `MVP2-fix124`(`385b4a98`)에서 **`MVP2-fix125`**를 만든다. B-179 커밋(`feat: 슬라이더 드래그·출발 시계·키 조작 창 효과음을 연결 (프롬프트 B-179)`, `b1471534`)이 기준에 없으면 멈추고 보고한다.
2. `MVP2-fix125`가 이미 있으면(다른 프롬프트가 먼저 씀) 비어 있는 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다.

## 절대 규칙
1. 기존 SfxId 번호·버스 음량은 바꾸지 않는다. 바뀌는 칸은 91(파일·음량)만, 새 칸은 **92~94** 세 개.
2. **새 음원은 고치지 않는다.** 길이·이음새는 이미 다듬은 파일 그대로. 조정은 칸 값과 상수로만.
3. App 코드의 원샷은 `SfxPlayer.Play`. `SfxPlayer`에 멈춤·예약 API를 더하지 않는다. 반복 소리는 아래 A처럼 따로 AudioSource를 쓴다(기존 `WorldLoopAudio` 방식).
4. 씬·프리팹 YAML은 고치지 않는다. 설정 저장, 조작 방식 저장, 연출 시간표, B-164~B-179 다른 소리 연결은 그대로.
5. `UiScanTick`(8)·시설 이름표 틱, B-179의 시계 출발 꺼짐음은 건드리지 않는다.
6. 줄 번호는 fix124 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 음원 넣기
- 사용자가 `sub-terra-sfx-B179-1.zip`을 풀어 `work_process/MVP2/sound/sfx-B179-1/`에 넣어 둔다. 없으면 멈추고 보고한다.
- wav 4개 → `Assets/_Project/Audio/SFX/UI/`. `settings_slider_drag.wav`는 같은 이름 파일을 **덮어쓴다**(GUID·.meta 유지). 작업 폴더 쪽 wav는 지우고 mp3·README·`address-rows.md`는 둔다.
- 임포트: 기존 빌더(`PromptB156SfxLibraryBuilder`, 모노·로드 시 압축 해제)를 따르되 **92·93·94는 Normalize 끔 + PCM**(조각마다 피크를 맞추면 반복이 +10dB 커져 이음새가 튄다). 빌더·후처리기에 이 세 파일만 예외를 둔다. 안 되면 이유를 적는다. `LoopFileNames`(긴 반복용 압축)에는 넣지 않는다.
- `init/sound-effect-address.md`: `address-rows.md`대로 `settings_slider_drag` 줄은 고치고 3줄은 표 끝에 붙인다.

| 칸 | 이름 | 파일 | 버스 | 음량 | 길이 | 쓰는 때 |
|---|---|---|---|---|---|---|
| 91 | SettingsSliderDrag(이름 그대로) | settings_slider_drag | UI | 0.13 → **0.55** | 0.698초 | 방향키·스크롤 등 한 번(앞+끝) |
| 92 | SettingsSliderDragHead | settings_slider_drag_head | UI | 0.55 | 0.544초 | 끌기 시작 |
| 93 | SettingsSliderDragLoop | settings_slider_drag_loop | UI | 0.55 | 0.1696초(7481샘플) | 잡는 동안 반복 |
| 94 | SettingsSliderDragTail | settings_slider_drag_tail | UI | 0.55 | 0.164초 | 손 뗄 때 끝 |
- 음량 근거(README): UI 소리 11개의 가장 큰 0.1초 RMS 중앙값 -15.5dB, B-179 소리@0.13은 -29.4dB(14dB 작음). @0.55면 앞 조각 -16.9dB(예전 UiScanTick@0.5와 비슷), 반복은 그보다 조금 낮다. **네 칸은 같은 값이어야 이음새에서 크기가 안 튄다.**
- `Shared/Audio/SfxId.cs`, `App/Audio/SfxLibraryDefaults.cs`(파일 끝, 길이 주석), `Resources/SfxLibrary.asset` 세 곳에 같게. 87~90(B-178)·91 다음 번호다. B-180·B-181은 새 칸이 없어 겹치지 않는다.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/Scripts/` 기준)
- `work_process/MVP2/sound/B-179-sfx-feedback-result.md`, `work_process/MVP2/sound/sfx-B179-1/README.md`
- App/UI/MainMenu/SettingsMenuSkin.cs: `VolumeDragSfx`·`VolumeKeySfxGap`·`VolumeDragState` ~21-33, `OnEnable` ~85, `OnDisable` ~131, `PlayCloseAnimation` ~166, `AnimateClose` ~227(0.38초), `FinishClose` ~275, `LateUpdate` ~286, `OnVolumeChanged` ~462, `BeginVolumeDrag`·`EndVolumeDrag`·`FlushVolumeSound` ~489-516, `WireParticleTriggers` ~721
- App/Integration/WorldLoopAudio.cs(반복 AudioSource·음량 계산·음소거 즉시 멈춤·OnDisable 정리 방식. 단 설정창 열림을 정지로 보므로 그대로 쓰지 말고 방식만 따른다), App/Audio/SfxPlayer.cs ~434(음량 = 칸 × 버스 × 효과음 채널, 마스터는 AudioListener)
- App/UI/MainMenu/ControlSchemePanel.cs: `OnDisable` ~55, `Update` ~62, `Open` ~86, `Close` ~105(UiPopupClose + `PlayOutro`), `HideNow` ~129
- App/UI/ControlTerminal/ControlTerminalView.cs: `KeycapPitchStep` ~39, `KeyHoverSfx`·`MouseHoverSfx`·`KeyHoverGap` ~40-42, `OnKeyEntered` ~590, `PlayKeycapSfx` ~746(KeycapPop, 피치 1+줄×0.03), ControlTerminalTimeline.cs `OutroDuration` 0.36초
- ESC 경로: App/UI/MainMenu/MainMenuBinder.cs ~110, App/UI/SurfaceBase/SurfaceBaseBinder.cs ~207, App/Integration/UndergroundMenuController.cs `HandleEscape` ~88 → 모두 `SetSettingsVisible(false)` → `SettingsMenuSkin.PlayCloseAnimation`
- 검사 양식: Tests/EditMode/App/Audio/PromptB179SfxFeedbackTests.cs, Tests/PlayMode/Integration/Audio/PromptB179SfxFeedbackPlayModeTests.cs

## A. 음량 슬라이더: 잡는 동안 반복, 놓으면 끝소리
- 작은 App 클래스(예: `App/Audio/SliderDragLoopAudio`)를 설정 스킨에 붙인다. AudioSource 3개(앞·반복·끝), `spatialBlend 0`, 반복 쪽만 `loop = true`. 이음새는 `AudioSettings.dspTime` + `PlayScheduled`·`SetScheduledEndTime`으로 샘플 단위로 붙인다. 시간 계산은 순수 함수로 분리해 검사한다.
- 음량은 매 프레임 `칸 음량 × 버스 × EffectiveVolume(Sfx)`(WorldLoopAudio와 같음). 마스터는 AudioListener가 맡으므로 마스터 막대를 끄는 동안 소리가 같이 작아지는 건 정상.
- 흐름(B-179의 "끄는 중"·`Pending`·5% 칸·트랙 클릭 보류 규칙은 그대로 쓴다):
  1. 누르기만 하고 값이 안 바뀌면 무음.
  2. 끄는 중 5% 칸이 처음 바뀌면 시작: 앞(92)을 지금 틀고, 앞이 끝나는 시각에 반복(93)을 예약한다.
  3. 잡고 있는 동안은 반복을 계속한다(값이 잠시 멈춰도 손을 뗄 때까지. ★ 상수로 바꿀 수 있게 `KeepLoopWhileHeld = true`).
  4. 손을 떼면(PointerUp·EndDrag): 반복 중이면 **다음 반복 끝**(최대 0.17초 뒤)에 반복을 멈추고 그 시각에 끝(94)을 예약 → 원래 소리처럼 이어진다. 아직 앞을 트는 중이면 반복 예약을 취소하고 앞이 끝나는 시각에 끝을 예약한다(트랙을 한 번 누른 경우 = 앞+끝 한 번).
  5. 손을 뗀 뒤 다시 잡으면 새 끄기. 끝이 아직 나는 중이면 끝을 짧게(약 0.03초) 줄이고 새로 시작한다.
- 끄는 중이 아닐 때의 변화(방향키·게임패드·스크롤)는 91 한 번. `VolumeKeySfxGap`은 0.88 → **0.70**(새 91 길이). 끄는 중이면 91을 내지 않는다.
- 바로 멈춤(끝소리 없이 약 0.03초 줄임): 창 닫힘·`OnDisable`, 슬라이더 꺼짐, 씬 바뀜(오브젝트 파괴), 효과음·마스터 음소거, `UiPauseGate.IsHeld`, 앱이 포커스를 잃거나 멈춤(`OnApplicationFocus(false)`·`OnApplicationPause(true)`), 라이브러리·클립 없음. 이때 "끄는 중"도 풀어 다음 끄기가 정상으로 시작되게 한다.
- 그대로: 다시 열기·`SetValueWithoutNotify`·음소거 버튼·기본값은 무음, 입자·글로우, 칸 8.

## B. 키 조작 창: 켜진 키 호버 = 키 "톡"
- `KeyHoverSfx`를 `SfxId.KeycapPop`(27)으로, 피치는 등장 때처럼 `1 + cap.Def.Row × KeycapPitchStep`(줄이 없는 키는 1.0).
- 마우스 그림은 지금처럼 `MouseHoverSfx = UiClick`(1). 0.06초 간격(`KeyHoverGap`), Idle에서만, 흐린 키·등장·닫힘·숨김·자동 시연 무음, 같은 키에 머무는 동안 다시 안 남 — 모두 그대로.
- 창 버튼 4개(이전·다음·선택 후 돌아가기·X)의 호버는 B-179대로 UiHover.

## C. 키 조작 창이 열린 채 ESC → 함께 닫힘
- `SettingsMenuSkin.PlayCloseAnimation`이 처음 받아들여질 때(세 화면 ESC·취소가 모두 여기로 온다) 같은 오브젝트의 `ControlSchemePanel`이 열려 있으면 키 창도 닫힘 연출(`PlayOutro`, 0.36초)을 같이 시작한다.
  - 소리는 **설정창 닫힘음(UiPopupClose) 한 번만.** 키 창 쪽 닫힘음은 이 경로에서 내지 않는다(예: `Close(playSound: false)`). 조작 방식 저장은 지금처럼 즉시.
  - 설정창이 꺼지는 시각(0.38초)에 키 창 연출이 아직 남아 있으면 끝까지 보이게 한다(설정창 끄기를 잠깐 미루거나 연출 길이를 맞춤). 방법과 이유를 적는다. 뚝 사라지지 않는 것이 목적.
- X키로 키 창만 닫는 동작(`TryCloseControlSchemePanel`)은 그대로. 키 창이 닫혀 있으면 ESC 동작도 그대로.

## 손대지 말 것
- 칸 8·87~90 값과 음원, 시계 출발 꺼짐(B-179 B), 설정 값 저장, 키 조작 창 연출·시연·저장
- B-180·B-181 대상(이미 들어와 있으면 그대로), `AGENTS.md`·규칙 문서

## 테스트
- EditMode 전체에서 기준 브랜치의 기존 실패 말고 새 실패가 없어야 한다. 전체 묶음에서만 흔들리는 기존 검사(B-150 휴지통 호버, 사다리 다침 프레임)는 단독 재실행 결과도 같이 적는다. 기대값을 바꾼 기존 검사(칸 수 90→93, B-179 슬라이더·키 호버 검사)는 이름과 이유를 적는다.
- 새 EditMode `PromptB1791SfxFeedbackFixTests`, 필요하면 PlayMode `PromptB1791SfxFeedbackFixPlayModeTests`(소리 요청을 잡는 방식은 B-179 검사를 따름)
  - 칸 91~94: 이름·버스·음량 0.55·파일, 92~94 Normalize 끔·PCM, 칸 8 그대로
  - 시간 계산: 시작 → 앞 끝 시각에 반복, 반복 중 놓기 → 다음 반복 끝에 끝소리, 앞 중 놓기 → 앞 끝에 끝소리(반복 0번)
  - 끄기: 누르기만 0번, 한 번 끄기에 시작 1번·끝 1번, 1초 넘게 잡아도 시작 1번, 다시 잡으면 1번 더, 닫힘·OnDisable·음소거·포커스 잃음 → 반복 멈춤·끝소리 0번
  - 끄는 중 아님: 0.70초 안 연속 변화 91 1번, 다시 열기·`SetValueWithoutNotify` 0번
  - 키 창: 켜진 키 KeycapPop 1번(줄에 따른 피치), 마우스 UiClick 1번, 0.06초 안 두 키 1번, 등장·숨김 0번
  - ESC: 키 창이 열린 채 설정 닫기 → UiPopupClose 1번, 키 창 `PlayOutro` 시작, 설정창이 꺼질 때 키 창 연출이 끝나 있음
- PlayMode는 고친 검사만. 실행 전후 `git status`로 범위 밖 프리팹·폰트·씬·PNG·TimeManager 변경을 확인하고 되돌린다.

## 사용자 테스트 가이드 (보고서에 그대로 넣기)
1. 메인 화면 설정에서 음량 막대를 잡고 이리저리 계속 끈다 → "탁 웅-웅" 다음 "웅웅웅"이 끊김 없이 이어진다.
2. 손을 떼면 끝소리가 붙으면서 자연스럽게 멈춘다(뚝 끊기지 않음).
3. 막대를 누르기만 하고 안 움직이면 소리가 없다. 막대 빈 곳을 한 번 누르면 짧게 한 번("탁 웅-") 난다.
4. 잡은 채 손을 멈추고 있으면 "웅웅"이 계속된다. 이게 어색하면 PM에게 알려 준다.
5. 크기가 다른 메뉴 소리(클릭·창 열림)와 비슷한지 들어 본다. 크거나 작으면 말해 준다.
6. 막대를 선택한 채 방향키로 움직이면 짧은 소리가 한 번씩, 빠르게 눌러도 겹치지 않는다.
7. 끄는 도중 ESC로 창을 닫거나, 다른 창(Alt+Tab)으로 나가면 소리가 바로 멈추고 계속 울리지 않는다.
8. 지상 기지·광산 설정창에서도 1~3번이 똑같다.
9. "키 조작 변경" 창에서 글자가 켜진 키에 마우스를 올리면 창이 열릴 때 나는 키 "톡" 소리가 난다. 빠르게 훑어도 쌓이지 않고, 흐린 키는 무음.
10. 마우스 그림에 올리면 "딸깍" 소리는 그대로다.
11. 키 조작 창을 연 채 ESC → 키 창과 설정창이 같이 닫히는 연출이 보이고 닫힘 소리는 한 번만 난다. X키는 지금처럼 키 창만 닫는다.
12. 효과음 음량·음소거가 새 소리에 다 적용되고, Unity Console에 새 빨간 오류·노란 경고가 없다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 슬라이더 반복·끝소리와 키 조작 창 키 소리·ESC 함께 닫기를 연결 (프롬프트 B-179-1)`). 새 브랜치에 일반 push, 브랜치는 지우지 않는다. PR은 만들지 않는다.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue158-주제.md` 파일 하나로 기록. **`init/rules/lessons.md`에 줄을 추가하지 않는다.**
- 결과 보고서 `work_process/MVP2/sound/B-179-1-sfx-feedback-fix-result.md`
  - 기준 브랜치·커밋, 소리마다 붙인 곳(파일·줄)과 SfxId, ★ 상수 위치, 임포트 예외를 어떻게 했는지, C에서 설정창 끄기 시각을 어떻게 맞췄는지
  - 테스트 결과, 직접 들어 보지 못한 항목은 "미검증", 위 사용자 테스트 가이드
  - **#158 대응 표**: 슬라이더 반복·끝소리 / 슬라이더 크기 / 키 조작 창 키 소리 / ESC 함께 닫기 각각 구현·일부·못 함
  - 뒤 프롬프트 번호: 이 작업이 fix125를 썼으므로 B-180은 fix126, B-181은 fix127로 한 칸씩 밀린다고 적는다.
