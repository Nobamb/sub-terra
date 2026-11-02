# B-177 메뉴·설정창·우측 메뉴·시계 효과음 + 버튼 소리 점검 (#158 D-2 UI 공통·메뉴)

## 목표
- 지금 무음인 메뉴 소리를 **기존 효과음만 다시 써서** 채운다: 설정창 열기·닫기·슬라이더 틱·버튼, 지상 기지 버튼 호버, 소리 빠진 버튼 점검, 우측 메뉴 펼침·접힘, T키 시계 켜고 끔, 이어하기·새 게임 실패.
- 새 음원·새 SfxId는 만들지 않는다. 퀘스트 클리어 창·보상 지급음·심층 해금 팝업·드론 스캔은 다음 B-178에서 한다(이번엔 손대지 않음).
- 재사용 소리 중 "판단으로 고른 것"은 표에 ★로 표시했다. 나중에 PM이 칸 번호만 바꿔 끼울 수 있게, 소리 id는 각 연결 지점의 상수 하나로 둔다.
- 필요하면 다른 에이전트에게 나눠 맡겨도 된다. 결과는 한 브랜치로 모은다.

## 브랜치
1. `git fetch`로 원격 `MVP2-fixNNN` 중 가장 큰 번호를 확인한다. PM 확인 시점(2026-10-30 22:20 KST)에는 **`MVP2-fix121`**(`84c4e17c`)이 최신이다.
2. 그 최신 브랜치에서 다음 번호(지금이면 **`MVP2-fix122`**)를 만든다. B-176 커밋(`feat: 다침 동작 중간 프레임을 추가 (프롬프트 B-176)`)이 기준에 없으면 멈추고 보고한다.

## 절대 규칙
1. SfxId·`SfxLibraryDefaults`·`Resources/SfxLibrary.asset`·음원 파일·버스 음량은 바꾸지 않는다. 새 칸 없음.
2. 한 번 누르면 소리는 한 번. 코드가 이미 결과음(창 열림음, 확인음, 자물쇠 소리 등)을 내는 버튼에는 `UiSfx.Attach(button, SfxId.None, hover: true)`로 **호버만** 붙인다.
3. 씬·프리팹 YAML은 고치지 않는다. 버튼 소리는 지금처럼 코드에서 `UiSfx.Attach`로 붙인다(프리팹·씬에 `UiSfxButton`이 저장된 곳은 0개).
4. App 코드는 `SfxPlayer.Play`, Gameplay 코드는 `SfxRequests.Play`. 연타가 잦은 소리는 `SfxThrottle`(App/Audio/SfxPlayback.cs ~91)로 간격을 둔다. `SfxPlayer`에 멈춤 API를 추가하지 않는다.
5. 연출 시간·모양, 설정 값 저장 방식, 일시정지 구조, B-164~B-170 소리 연결은 바꾸지 않는다. 줄 번호는 fix121 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/Scripts/` 기준)
- #158 D-2 "UI 공통 · 메뉴"와 구현 메모, `work_process/MVP2/sound/B-169-unlock-tv-result.md`, `work_process/MVP2/B-170-boot-quit-result.md`
- App/Audio/UiSfx.cs(`UiSfxButton`, `Attach` ~126), SfxPlayer.cs(`Play(id, pitch)` ~105, 같은 소리 0.05초 제한), SfxPlayback.cs(`SfxThrottle` ~91)
- App/UI/MainMenu/SettingsMenuSkin.cs: `OnEnable` ~70(열기 코루틴 ~96), `Cancel` ~123, `PlayCloseAnimation` ~137, `AnimateOpen` ~160(0.35초), `AnimateClose` ~184(0.38초), `OnVolumeChanged` ~406, `WireDropdownTriggers` ~487, `OnToggleParticleEvent` ~645
- App/UI/MainMenu/SettingsMuteButton.cs `ToggleMute` ~91(이번엔 손대지 않음), SettingsAudioRows.cs(값은 `SetValueWithoutNotify`)
- 설정창 여닫기 경로: App/UI/SurfaceBase/SurfaceBaseView.cs(`OnEnable` ~237-262 버튼 연결, `SetSettingsVisible` ~620), SurfaceBaseBinder.cs(`OnSettingsClicked` ~362, `OnSettingsApply` ~375, `OnSettingsCancel` ~389, `OnSettingsDefaults` ~401), App/UI/MainMenu/MainMenuView.cs(`AttachButtonSfx` ~166-176, `SetSettingsVisible` ~544), MainMenuPresenter.cs(`OpenSettings` ~236), MainMenuBinder.cs(~197-238), App/Integration/UndergroundMenuController.cs(`OnEnable` ~34-58, `OpenSettings` ~158, `CloseSettings` ~172, `ApplySettings` ~208, `ResetDefaults` ~217)
- App/UI/HUD/GameplaySideMenuController.cs(`/`키 ~26, `Toggle` ~76, `Transition` ~82-97: 나감 0.5초 → 들어옴 0.3초), SideMenuButtonView.cs
- App/UI/HUD/MineResetClockOverlay.cs(T키 ~388-394, `RefreshFromState` ~167, `BeginDeparture` ~143), MineResetClockView.cs(`SetPowered` ~126), MineResetClockPowerTimeline.cs(켜짐 0.40초, 꺼짐 0.28초), App/Save/SaveRuntimeController.cs(`ToggleMineResetClock` ~365)
- App/UI/MainMenu/MainMenuBinder.cs: `OnContinue` ~253-270, `OnContinueCompleted` ~272-296("이어하기 실패" ~294), `OnStartNewGame` ~298-315(실패 ~309-313)
- 버튼: App/UI/Building/BuildingMenuEntryButton.cs ~36, App/UI/Progression/ProgressionPanelView.cs(`SelectCategoryTab` ~863, 탭 연결 ~1173), UpgradeTreeNodeView.cs, ProgressionUpgradeEntryButton.cs, App/UI/SurfaceBase/SurfaceBaseQuestPanel.cs ~22
- 교훈 양식: `init/rules/lessons/2026-10-30-issue264-test-fixture-state-reuse.md`

## 1. 설정창 (메인 화면·지상·광산이 같은 `SettingsMenuSkin`)
| 순간 | 소리 | 붙일 곳 |
|---|---|---|
| 열림(카드가 떨어지기 시작) | UiPopupOpen(6) | `SettingsMenuSkin.OnEnable`, 플레이 중 열기 코루틴 시작 직전(~94-96) |
| 닫힘(카드가 올라가기 시작) | UiPopupClose(7) | `PlayCloseAnimation`에서 닫기 코루틴 시작할 때(~152). 이미 닫히는 중 재호출은 무음 |
| 적용 | UiConfirm(4), 이때 닫힘음은 생략 | 세 바인더의 적용 경로. 스킨에 "다음 닫힘 한 번 무음" 표시를 두는 식으로 한 번만 |
| 취소·X·ESC | 닫힘음만(클릭음 없음) | 위 닫힘 |
| 기본값 | UiClick(1) | 버튼에 `UiSfx.Attach(…, UiClick)` 또는 세 `ResetDefaults` 경로 중 한 곳 |
| 해상도·언어·프레임 드롭다운 펼침, 항목 고름 | UiClick(1) | 드롭다운 트리거(~487) / `onValueChanged`. 값 다시 그리기(`SetValueWithoutNotify`)에는 안 나게 |
| 애니메이션 감소 토글 | UiClick(1) | 토글 값이 바뀔 때 한 번(`OnToggleParticleEvent` ~645 근처) |
| 슬라이더(마스터·배경음·효과음) 움직임 | ★UiScanTick(8) | `OnVolumeChanged` ~406. 5% 칸이 바뀔 때만, 0.07초 간격(`SfxThrottle`), 피치 0.9~1.1을 값에 따라 |
| "키 조작 변경" | 호버만(열림음 KeysHoloOpen이 이미 남) | |
- 메인 화면 설정 버튼은 `UiClick` → `SfxId.None, hover: true`로 바꾼다(MainMenuView ~168, 열림음과 겹침 방지). 기존 B-158 검사 기대값도 같이 고친다.
- 지상 `settingsButton`, 광산 우측 메뉴 `SettingsShortcut`도 호버만.
- 설정창이 처음부터 꺼져 있는지(프리팹 3곳 `SettingsPanel` 비활성) 확인하고, 씬을 열 때·`SetSettingsVisible(false)`로 숨길 때 소리가 나지 않게 한다. 에디트 모드·`Application.isPlaying == false`는 무음.
- 엘리베이터 탑승 중 ESC(B-166에서 막힘)·종료 확인 창이 떠 있을 때(B-170)는 열리지 않으므로 소리도 없어야 한다.
- 확성기(음소거) 버튼은 #158 E에서 따로 정하므로 이번엔 그대로 둔다.

## 2. 지상 기지 버튼 (`SurfaceBaseView.OnEnable` ~237-262)
- 업그레이드 `upgradeButton`, 설정 `settingsButton`, 인벤토리 `inventoryButton`, 탐사 시작 `exploreButton`, 판매 열기 버튼: **호버만**(눌렀을 때 소리는 각자 창·확인음이 냄).
- 지상 퀘스트 버튼(`SurfaceBaseQuestPanel` ~22): 지금 열 때 무음이라 ★UiClick(1)+호버를 임시로 붙인다(#158 E #193에서 전용 소리로 바뀔 수 있음).
- 종료·광산 초기화 버튼은 이미 끝(B-170, #158 A 단계). 탐사 시작 실패음은 이번 범위 아님.

## 3. 소리 빠진 버튼 점검
- 아래를 코드에서 붙인다. 붙인 곳(파일·줄)을 보고서 표에 적는다. 씬 버튼은 이름으로 찾지 말고, 가능하면 그 버튼을 이미 다루는 컴포넌트(예: `SideMenuButtonView`, `BuildingMenuEntryButton`)의 Awake/OnEnable에서 붙인다.

| 버튼 | 붙일 소리 |
|---|---|
| 우측 메뉴 건설·인벤토리·업그레이드·가이드(`Open0~3`, `OpenGameGuideButton`), `SettingsShortcut`, HUD `QuestSummaryButton` | 호버만 |
| 우측 메뉴 펼침/접힘 `MenuToggleButton`×2 | None(4번 소리가 맡음), 호버만 |
| 건설창 시설 고르기 `Select_building.*`(`BuildingMenuEntryButton`) | UiClick(1) (#158 D-2 시설 설치의 남은 줄) |
| 업그레이드 창 카테고리 탭 `CategoryTab_0~4` | UiClick(1) |
| 업그레이드 노드 고르기(`Node_*`, `UpgradeEntry_*`) | ★UiClick(1). 구매 버튼은 결과음이 있으니 손대지 않음 |
- 이번에 **건드리지 않는 버튼**(E 항목 몫): 퀘스트 상세 이전/다음, 메인 화면 슬롯 삭제(휴지통·확인·취소), 화물 공간 부족·버리기 창, 드론 말풍선·분석 창 닫기, 퀘스트 클리어 닫기, 심층 해금 팝업, 확성기 버튼.
- 점검 결과로 소리가 없는 버튼을 하나 더 찾으면 보고서 표에 "남음"으로만 적는다(고치지 말 것). 옛 `SaveSlotPanel.prefab`·`EconomyPanelView` 버튼이 지금 화면에서 쓰이는지도 확인해 적는다.

## 4. 우측 사이드 메뉴 (`GameplaySideMenuController.Transition` ~82)
- 전환 시작 프레임(~84)에 한 번: 펼칠 때(`IsOpen == false`) ★UiPopupOpen(6), 접을 때 ★UiPopupClose(7). `/`키와 버튼이 같은 길이라 둘 다 된다.
- 들어오는 쪽 미끄러짐(0.5초 뒤)에는 소리 없음. 전환 중 재입력은 지금처럼 무시되므로 소리도 한 번.
- `ResetClosed`(OnEnable/OnDisable)는 무음.

## 5. T키 광산 초기화 시계 (`MineResetClockOverlay.Update` ~388-394)
- T키로 `ToggleMineResetClock`를 부른 **직후**, 화면의 시계가 실제로 켜짐 ↔ 꺼짐으로 바뀌었을 때만 소리를 낸다(`sessionVisible`, `departing`, 슬롯 조건을 거친 결과로 판단).
  - 켜짐 ★UiCctvOn(9), 꺼짐 ★WindowTvClose(80). `WindowTvSfx`가 아니라 `SfxPlayer.Play`로 낸다(창 TV 소리를 끊지 않게).
- `SetPowered`(엘리베이터 출발 `BeginDeparture`, 씬 진입)에는 걸지 않는다. 남은 1분 째깍·5분 알림·만료 벨은 그대로.

## 6. 이어하기·새 게임 실패 (`MainMenuBinder`)
- 이어하기 실패: `OnContinueCompleted`에서 "이어하기 실패" 글자를 띄우는 줄(~294)과 같은 곳에 UiCancelFail(5). 런타임이 없어 되돌아가는 경로(~261-266)도 같게.
- 새 게임 시작 실패: `OnStartNewGame` 실패 분기(~309-313)에 UiCancelFail(5).
- 머리 위 노란 경고가 아니라 메인 화면 글자이므로 `FailureFeedback` 대신 `SfxPlayer.Play`를 바로 쓴다(B-170 메인 화면 소리와 같은 방식). 누를 때 확인음(~150, ~157)은 그대로.

## 손대지 말 것
- 퀘스트 클리어 창·보상 지급, 심층 구역 해금 팝업, 드론 스캔(B-178), 확성기 버튼, E 항목 버튼들
- 연출 시간표·모양, SfxId·칸 값·음원, 세이브·설정 저장, `AGENTS.md`·규칙 문서

## 테스트
- EditMode 전체에서 기준 브랜치의 기존 실패(B-170 보고서 21개 묶음 기준) 말고 새 실패가 없어야 한다. 기대값을 바꾼 기존 검사는 이름과 이유를 적는다.
- 새 EditMode `PromptB177SfxMenuTests`(소리 요청을 잡는 방식은 B-168·B-169 검사를 따름)
  - 설정창: 열기 1번 UiPopupOpen, 닫기 1번 UiPopupClose, 닫는 중 다시 닫기 0번, 적용은 UiConfirm만, 숨긴 채 시작·`SetValueWithoutNotify`는 무음
  - 슬라이더: 한 프레임에 0→100% 이동 1번, 0.07초 안 연속 변화 1번, 5% 칸 안 변화 0번
  - 메인 화면 설정 버튼 클릭음 None, 지상 버튼 호버만, 건설·탭 버튼 UiClick
  - 우측 메뉴: 펼침 UiPopupOpen 1번, 접힘 UiPopupClose 1번, 전환 중 재입력 0번
  - 시계: 켜짐/꺼짐 각 1번, 출발 중(departing)·슬롯 없음이면 0번
  - 이어하기·새 게임 실패 UiCancelFail 1번, 성공 0번
- PlayMode는 고친 검사만. 실행 전후 `git status`로 범위 밖 프리팹·폰트·씬·PNG·TimeManager 변경을 확인하고 되돌린다.

## 사용자 테스트 가이드 (보고서에 그대로 넣기)
디버그 터미널은 플레이 중 **Ctrl + `**(1 왼쪽 키).
1. 메인 화면에서 설정을 열고 닫는다 → 열 때 "뾰롱", 닫을 때 닫힘 소리가 한 번씩 난다. 설정 버튼 클릭음이 따로 겹치지 않는다.
2. 설정창에서 음량 막대를 끌어 본다 → 작은 "틱"이 너무 빽빽하지 않게 나고, 크게 할수록 조금 높아진다.
3. 적용을 누르면 확인음만, 취소·X·ESC로 닫으면 닫힘 소리만 난다. 기본값·드롭다운·애니메이션 감소 토글은 클릭음이 난다.
4. 지상 기지와 광산(ESC, 우측 메뉴 "설정(esc)")에서도 1~3번이 똑같다. 엘리베이터 타는 중 ESC는 소리 없이 무시된다.
5. 지상 기지 업그레이드·설정·인벤토리·탐사 시작 버튼에 마우스를 올리면 호버음이 나고, 누르면 각 창 소리만 난다(두 번 안 남).
6. 광산에서 `/`키나 메뉴 버튼으로 우측 메뉴를 펼치고 접는다 → 펼칠 때·접을 때 다른 소리가 한 번씩 난다. 연타해도 쌓이지 않는다.
7. 우측 메뉴 버튼들에 마우스를 올리면 호버음, 누르면 창 소리만 난다.
8. 건설창에서 시설을 고르면, 업그레이드 창에서 위쪽 탭·노드를 고르면 클릭음이 난다.
9. 광산에서 T키로 시계를 켜고 끈다 → 켤 때 화면 켜지는 소리, 끌 때 꺼지는 소리가 난다. 엘리베이터를 타고 떠날 때 시계가 꺼지는 건 무음.
10. 이어하기에 실패하면(깨진 세이브 등, 확인하기 어려우면 건너뜀) "이어하기 실패" 글자와 함께 실패음이 난다.
11. 효과음 음량·음소거가 새 소리에 다 적용되고, Unity Console에 새 빨간 오류·노란 경고가 없다.
12. 바꿔 보고 싶은 소리(★ 표시)가 있으면 번호와 함께 PM에게 알려 준다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 설정창·우측 메뉴·시계 효과음과 버튼 소리를 연결 (프롬프트 B-177)`). 새 브랜치에 일반 push, 브랜치는 지우지 않는다. PR은 만들지 않는다.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue158-주제.md` 파일 하나로 기록. **`init/rules/lessons.md`에 줄을 추가하지 않는다.**
- 결과 보고서 `work_process/MVP2/sound/B-177-sfx-menu-result.md`
  - 기준 브랜치·커밋, 소리마다 붙인 곳(파일·줄)과 SfxId, ★ 재사용 소리 목록(바꿀 상수 위치)
  - 버튼 점검 표(붙인 버튼 / 호버만 / 남음), 테스트 결과, 직접 들어 보지 못한 항목은 "미검증", 위 사용자 테스트 가이드
  - **#158 대응 표**: 설정창 / 지상 기지 버튼 / 모든 버튼 점검 / 우측 메뉴 / T키 시계 / 이어하기 실패(+새 게임 실패) / D-2 시설 설치 "건설창 클릭음" 각각 구현·일부·못 함
