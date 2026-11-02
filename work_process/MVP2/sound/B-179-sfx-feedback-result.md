# B-179 슬라이더·출발 시계·키 조작 창 효과음 결과

- 기준: `origin/MVP2-fix123`, `48ed55cf`. `git fetch`로 최신 번호를 확인했고 B-177 `ce7a9175`와 B-178 `4f1bb898`이 포함된 기준에서 `MVP2-fix124`를 만들었다.
- 입력 묶음은 사용자가 지정한 `work_process/MVP2/sound/sub-terra-sfx-B179/sfx-B179/`를 사용했다. WAV만 게임 폴더로 이동했고 MP3·README·address-rows는 원래 폴더에 보존했다.
- [#158](https://github.com/Nobamb/sub-terra/issues/158)의 D-2 UI 공통·메뉴를 확인했다. 공개 페이지에서 B-179 줄은 찾지 못해 로컬 `prompt-B-179.md`를 세부 기준으로 삼았다.
- 새 Inspector 연결은 없다. 기존 코드의 수명주기·버튼 빌더에서 연결한다. 씬·프리팹 YAML은 편집하지 않았다.

## 연결 위치

아래 경로는 `sub-terra/Assets/_Project/Scripts/` 기준이다.

| 순간 | SfxId | 파일·위치 |
|---|---|---|
| 음량 막대를 잡고 처음 5% 칸을 넘을 때 | ★SettingsSliderDrag(91) | `App/UI/MainMenu/SettingsMenuSkin.cs:21` 상수, `:462` 값 변경, `:489` 누름, `:506` 재생, `:721` 포인터 연결 |
| 방향키·게임패드 등 포인터 드래그 이외의 변경 | SettingsSliderDrag(91) | 같은 파일 `:22` VolumeKeySfxGap=0.88초, `:285` LateUpdate에서 대기 요청 처리 |
| 켜진 시계를 엘리베이터 출발로 끌 때 | ★WindowTvClose(80) | `App/UI/HUD/MineResetClockOverlay.cs:143` BeginDeparture, `:385` PowerOffSfx |
| 조작 창 이전·다음·선택 후 돌아가기·X 호버 | UiHover(2) | `App/UI/MainMenu/ControlSchemePanel.cs:227`, UiSfx.Attach(click=None, hover=true) |
| 기능 키 호버 | ★UiHover(2) | `App/UI/ControlTerminal/ControlTerminalView.cs:40` KeyHoverSfx, `:590` OnKeyEntered |
| 마우스 그림 호버 | ★UiClick(1) | 같은 파일 `:41` MouseHoverSfx, Def.IsMouse로 구분 |

★ 상수의 SfxId만 바꿔 교체할 수 있다. 키·마우스 호버 간격은 `ControlTerminalView.cs:42` KeyHoverGap=0.06초다.

## 동작

- 슬라이더는 각 막대별로 누름부터 PointerUp/EndDrag까지 재생 여부를 보관한다. 0→100%를 반복하거나 0.88초 뒤에도 계속 끌어도 한 번만 요청한다. 다음 누름은 새 드래그다. 누르기만 하고 5% 칸이 바뀌지 않으면 무음이다.
- 실제 Unity Slider는 트랙을 누를 때 EventTrigger보다 먼저 값을 바꿀 수 있다. 아직 누름 상태가 없던 변경은 PointerDown 또는 같은 프레임 LateUpdate까지 보류해 트랙 클릭도 같은 드래그에 포함했다. 드래그 도중 피치는 1.0 고정이다.
- 포인터 외 입력은 마지막 재생부터 0.88초 간격을 둔다. 기존 0.07초 volumeTickThrottle과 값에 따른 피치 변조는 제거했다. 창 OnDisable에서 상태를 지우며, 개별 슬라이더 비활성도 LateUpdate에서 해제한다.
- 기존 volumeSteps·SetValueWithoutNotify를 유지했다. 무음 재표시 때 대기 요청도 지우므로 재열기·음소거·기본값으로 드래그음이 생기지 않는다. 기존 입자·글로우는 그대로다.
- BeginDeparture는 직전 화면 표시·전원 ON을 확인하고 실제 OFF로 바뀔 때만 꺼짐음을 요청한다. 이미 OFF·숨김·출발 중 재호출은 무음이다. EndDeparture의 복구 켜짐음은 기존처럼 무음이다. T키 소리는 그대로다.
- 3시간 만료는 `SaveRuntimeController.cs:820`에서 `BeginDeparture(playSound: false)`로 구분했다. 만료 벨·초기화 알림 연출의 기존 소리만 유지한다. 뒤의 공통 대기 `:1202`에서 다시 BeginDeparture를 호출해도 departing 가드로 추가 소리가 나지 않는다. 이동 대기 시간은 바꾸지 않았다.
- 기능 키·마우스 호버음은 Idle에서만 요청한다. Intro의 키캡 톡 소리와 겹치지 않고 Outro·Hidden·기능 없는 키·자동 시연은 무음이다. 기존 포인터 진입 이벤트만 사용하므로 머무르는 동안 반복하지 않는다.

## 음원·칸 보존

- `Audio/SFX/UI/settings_slider_drag.wav`: 44.1kHz, 16bit, 모노, 실제 길이 0.877초. Unity의 기존 B-156 Postprocessor·Builder로 forceToMono, DecompressOnLoad, Vorbis 0.7 설정과 라이브러리 연결을 생성했다.
- `Shared/Audio/SfxId.cs`, `App/Audio/SfxLibraryDefaults.cs`, `Resources/SfxLibrary.asset`에 91을 추가했다. UI 버스, 음량 0.13, 피치 1, startTime/maxDuration/fadeOut=0이다.
- 이동 전후 SHA-256: `5f1047c34b951f5c35fdfffa51f90e738bf84cb8c563ce55c046afe245bda17d`. WAV 내용은 바꾸지 않았다.
- `Temp/verify-b179.py`로 기존 라이브러리 89칸의 전체 값·클립 참조와 모든 버스 음량이 기준과 동일하고 91만 추가된 것을 확인했다. 칸 8 UiScanTick과 B-178의 87~90은 그대로다.
- `init/sound-effect-address.md`에 제공된 출처 1줄을 추가했다.

## 검증

- 새 EditMode `PromptB179SfxFeedbackTests` **5/5 통과**: 칸·임포터·기존 틱 보존, 버튼 4개 호버 전용, 키·마우스 및 0.06초 제한, 등장·닫힘·숨김·기능 없는 키·시연 무음, 만료 경로 배선.
- 관련 오디오 EditMode: **167 통과 / 1 실패**, `Temp/b179-targeted-editmode.xml`. 실패는 기준에도 있던 `PromptB162SfxCStageTests.Defaults_CSlotsMatchTable_NumberedFrom35_NoAlertBus`의 BriefingClose 음량 기대값(0.5)과 기존 값(1.0)의 불일치다. 범위 밖이므로 수정하지 않았다.
- 변경한 B-177 2개 + 새 B-179 4개 PlayMode: **6/6 통과**, `Temp/b179-final-playmode.xml`, CLI exit 0. 실제 ExecuteEvents 컴포넌트 순서의 트랙 클릭, 1초 이상 끌기, 재드래그·비활성 복구·무음 재표시·비포인터 간격·시계/T키를 검사했다. 첫 실행의 트랙 클릭 실패는 테스트 Slider의 fillRect 누락이었고 실제 필 영역을 갖추어 재검증했다.
- 변경 전 전체 EditMode: **2263 통과 / 24 실패**, `Temp/b179-baseline-editmode.xml`.
- 첫 최종 전체 EditMode: **2267 통과 / 25 실패**, `Temp/b179-final-editmode.xml`. 기준의 24개와 동일한 실패에 더해, 수정하지 않은 `PlayerLadderMiningTests.Damage_ResumesThirteen_KnockoutInterrupts`에서 FrameIndex 12 기대/13 실제의 추가 실패가 있었다. 이 묶음의 단독 재실행은 **24/24 통과**, `Temp/b179-ladder-editmode.txt`. 프레임 시간에 영향을 받는 것으로 추정하며 Gameplay 코드·기대값은 변경하지 않았다.
- 실행 중인 Editor에서 전체 재확인: **2266 통과 / 25 실패**, `Temp/b179-final-editor-editmode.txt` (기존 러너의 프로젝트 어셈블리 탐색 범위). 기준의 24개 실패는 동일했고 사다리 검사는 통과했다. 대신 `PromptB150MainMenuTests.DeleteButton_Hover_FadesInWhiteTrashIcon`가 추가 실패했다. 이 검사는 B-177 결과 보고서에도 전체 묶음에서의 프레임 시간 의존 실패로 기록되어 있다. **기준 외 실패 0개 조건은 미충족**이다. 두 추가 실패는 모두 수정하지 않은 기존 검사이며, 소리 변경으로 기대값을 완화하지 않았다.
- 마지막 관련 EditMode **111/111 통과**, `Temp/b179-targeted-final.txt`: B-179·B-177, ControlTerminalTests, PlayerLadderMiningTests, PromptB150MainMenuTests. 전체 묶음에서 추가 실패했던 두 기존 검사도 이 실행에서는 통과했다.
- Bootstrap → MainMenu 실제 재생: 설정 버튼으로 열기, 슬라이더 첫 드래그·1.1초 유지·재드래그, 조작 창 열기와 Idle 키·마우스 호버 확인. `Temp/b179-smoke.txt`에 프레임 1747→2008, 첫 드래그 실제 SfxPlayer.StartedCount +1, 유지 요청 1회, 두 번째 잡기까지 누적 2회, 피치 1을 기록했다.
- 화면은 `Temp/b179-settings-half.png`, `Temp/b179-controls-half.png`를 절반 해상도로 확인했다. 설정의 오디오 3줄·기존 레이아웃과 조작 창 기능 키·마우스·버튼이 표시되는 것을 확인했다. UI 모양은 변경하지 않았다.
- 실제 재생 확인 뒤 compilationFailed=False를 확인했다. 기존 컴파일 경고·라이선스/에디터 종료 진단 로그는 있으나 위 재생 확인 구간에서 새 App 예외·효과음 누락 경고는 없었다. 직접 청음과 전 구간 Console 수동 확인은 아래 미검증에 남긴다.
- 검증용 B179Smoke Editor 코드와 .meta는 제거하고 AssetDatabase.Refresh를 실행했다. 테스트가 저장한 범위 밖 프리팹·씬·폰트·PNG·ProjectSettings는 원복했다. 원래 사용자 변경 `init/prompt-B.md`와 제공된 프롬프트·음원 참고 묶음은 보존하고 커밋에서 제외한다.

## 기존 검사 기대값 변경

- `PromptB177SfxMenuPlayModeTests.Runtime_SettingsTransitionsSliderDropdownAndToggle`: UiScanTick/0.07초/가변 피치 → SettingsSliderDrag/0.88초/피치 1. 비포인터 대기 요청을 LateUpdate에서 확인한다.
- `Runtime_SideMenuAndClock_OnlyInputChangesMakeSound` → `Runtime_SideMenuAndClock_InputAndVisibleDepartureMakeSound`: 출발 무음 기대를 켜진 시계 꺼짐음 1회로 변경했다.
- `PromptB156SfxTests.Defaults_HaveAllSlotsWithUniqueIds`, `PromptB165SfxD1FixTests.Volumes_DefaultsAndAssetMatch`, `PromptB168SfxD2Tests.Defaults_D2SlotsNumberedFrom66_MatchTableAndAsset`, `PromptB178SfxTests.Slots87To90_MatchTableAssetAndFiles`: 전체 칸 수만 89→90으로 갱신했다. 기존 칸별 값 기대는 그대로다.

## 변경 파일

- 런타임 C# 7개: 위 연결 표의 SettingsMenuSkin, MineResetClockOverlay, SaveRuntimeController, ControlSchemePanel, ControlTerminalView와 SfxId, SfxLibraryDefaults.
- Resources/SfxLibrary.asset, 새 WAV와 Unity가 생성한 .meta.
- 기존 검사 C# 5개, 새 EditMode/PlayMode 검사 2개와 각각의 .meta.
- 출처 문서, 이 보고서, `init/rules/lessons/2026-11-02-issue158-slider-pointer-order.md`.
- 저장 형식·설정 저장·조작 저장·패키지·Unity 버전·씬·프리팹의 최종 변경은 없다. 별도 PR은 만들지 않는다.

## #158 대응 표

| 항목 | 상태 |
|---|---|
| 슬라이더 드래그 소리 | 구현 |
| 떠날 때 시계 꺼짐 | 구현 |
| 키 조작 창 호버 | 구현 |

## 미검증

스피커·헤드폰 직접 청음, 새 음원의 체감 크기·길이, 지상/광산 설정창의 수동 입력, 실제 엘리베이터 왕복 중 씬 전환에서의 소리 잘림, 실제 3시간 만료 연출 완주, Windows 빌드는 미검증이다. 시계는 독립 PlayMode 컴포넌트 경로를 검증했으며 실제 광산 탑승으로 확인한 것은 아니다. 아래 순서로 수동 확인한다.

## 사용자 테스트 가이드 (프롬프트 원문)

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
