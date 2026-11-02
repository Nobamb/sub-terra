# B-177 메뉴·설정창·시계 효과음 결과

- 기준: `origin/MVP2-fix121`, `84c4e17c` (`feat: 다침 동작 중간 프레임을 추가 (프롬프트 B-176)`). fetch 후 최신 번호 확인. 작업 브랜치 `MVP2-fix122`.
- [#158](https://github.com/Nobamb/sub-terra/issues/158)의 D-2 UI 공통·메뉴와 구현 메모 확인.
- 기존 SfxId와 음원만 사용. SfxLibraryDefaults·Resources/SfxLibrary.asset·음원·버스 음량·연출 시간·설정 저장 정책 미변경. 씬·프리팹 YAML 미편집.
- 기존 사용자 변경 `init/prompt-B.md`, ProjectSettings의 EditorBuildSettings·QualitySettings 및 작업 중 추가된 B-178 문서·음원은 보존하고 이번 커밋에서 제외한다.

## 연결 위치

아래 경로는 `sub-terra/Assets/_Project/Scripts/` 기준이다.

| 순간 | SfxId | 파일·위치 |
|---|---|---|
| 설정 열기 | UiPopupOpen(6) | App/UI/MainMenu/SettingsMenuSkin.cs:113, OnEnable의 재생 중 분기 |
| 설정 닫기 | UiPopupClose(7) | SettingsMenuSkin.cs:174, PlayCloseAnimation의 최초 진입만 |
| 적용 | UiConfirm(4) | SettingsMenuSkin.cs:182 PlayApplySound. 다음 닫힘 한 번만 생략. MainMenuBinder.OnSettingsApply / SurfaceBaseBinder.OnSettingsApply / UndergroundMenuController.ApplySettings → 각 View.PlaySettingsApplySound |
| 기본값 / 토글 / 드롭다운 | UiClick(1) | SettingsMenuSkin.cs:108 / :191 / :555·561. 토글은 onValueChanged, 드롭다운은 펼침 트리거·onValueChanged |
| 마스터·배경음·효과음 슬라이더 | ★UiScanTick(8) | SettingsMenuSkin.cs:448. 5% 칸 변경, SfxThrottle 0.07초, 피치 0.9~1.1. 두 View.SetSettingsDraft는 무음 재표시 후 칸 기준도 갱신 |
| 우측 메뉴 펼침·접힘 | ★UiPopupOpen(6) / ★UiPopupClose(7) | App/UI/HUD/GameplaySideMenuController.cs:89. 전환 시작 한 번, 전환 중 재입력·ResetClosed 무음 |
| T키 시계 켜기·끄기 | ★UiCctvOn(9) / ★WindowTvClose(80) | App/UI/HUD/MineResetClockOverlay.cs:383 ToggleClockFromInput. 토글 후 실제 clockView 전원과 활성 상태 비교. SfxPlayer 사용 |
| 이어하기 실패 | UiCancelFail(5) | App/UI/MainMenu/MainMenuBinder.cs:266·297. 런타임 없음 / 실패 문구 표시 |
| 새 게임 실패 | UiCancelFail(5) | MainMenuBinder.cs:308·320. 런타임 없음 / StartNewGame 실패 |

★ 교체할 상수: SettingsMenuSkin.VolumeTickSfx(:21), GameplaySideMenuController.ExpandSfx·CollapseSfx(:16·17), MineResetClockOverlay.PowerOnSfx·PowerOffSfx(:380·381), SurfaceBaseQuestPanel.QuestClickSfx(:14), UpgradeTreeNodeView.SelectionSfx(:30), ProgressionUpgradeEntryButton.SelectionSfx(:22).

## 버튼 점검

| 버튼 | 처리 | 파일·위치 (`Scripts/` 기준) |
|---|---|---|
| 메인 설정 | 호버만 | App/UI/MainMenu/MainMenuView.AttachButtonSfx, UiClick→None |
| 설정 취소·X | 호버만, 닫힘 결과음 | SettingsMenuSkin.cs:105·106 |
| 설정 기본값 | UiClick+호버 | SettingsMenuSkin.cs:108 |
| 키 조작 변경(기존 버튼·런타임 대체 버튼) | 호버만, KeysHoloOpen 유지 | App/UI/MainMenu/ControlSchemePanel.cs:149·207 |
| 지상 업그레이드·설정·인벤토리·탐사 | 호버만 | App/UI/SurfaceBase/SurfaceBaseView.OnEnable |
| 판매 열기 | 호버만 | App/UI/Economy/EconomyPanelView.WireButtons |
| 지상 퀘스트 | ★UiClick+호버 | App/UI/SurfaceBase/SurfaceBaseQuestPanel.cs:25 |
| 우측 Open0~3·가이드·MenuToggleButton×2 | 호버만 | App/UI/HUD/SideMenuButtonView.cs:42 |
| 옛 OpenGameGuideButton | 호버만 | App/UI/HUD/HudPanelChromeController.WireButtons의 gameGuideOpenButton 참조. SideMenuButtonView가 없는 숨김 레거시 버튼도 연결 |
| 광산 SettingsShortcut | 호버만 | App/Integration/UndergroundMenuController.OnEnable |
| HUD QuestSummaryButton | 호버만 | App/UI/Tutorial/DemoObjectiveView.ApplyCompactSummary |
| Select_building.* | UiClick | App/UI/Building/BuildingMenuEntryButton.OnEnable |
| CategoryTab_0~4 | UiClick | App/UI/Progression/ProgressionPanelView.cs:1176 |
| Node_* / UpgradeEntry_* | ★UiClick | UpgradeTreeNodeView.cs:113 / ProgressionUpgradeEntryButton.cs:175 |
| 퀘스트 상세 이전·다음, 슬롯 삭제, 화물 부족·버리기, 드론 분석 닫기, 확성기 | 남음(E 범위) | 이번에는 수정하지 않음 |
| 구매·탐사·종료·초기화 실행 결과음 | 기존 연결 유지 | B-164~170 범위 |

설정 패널은 MainMenuPanel·SurfaceBasePanel 프리팹 및 광산 씬의 SettingsPanel 세 곳 모두 m_IsActive=0을 확인했다. EditMode·비활성 시작·이미 숨은 창 닫기·SetValueWithoutNotify는 무음이다. 시계 SetPowered·BeginDeparture에는 효과음을 연결하지 않았다.

옛 SaveSlotPanel은 MainMenu.unity에서 m_IsActive=0인 비활성 인스턴스다. 현재 화면은 MainMenuPanel의 슬롯 카드를 사용한다. EconomyPanelView는 SurfaceBasePanel에 실제로 남아 판매 열기 연결에 사용되므로 해당 필드에만 호버를 붙였다. 다만 예전 판매 본문·수량 버튼은 플레이 중 ResourceSellPopupView로 대체되고 HideLegacyModal이 기존 CanvasGroup의 알파·상호작용·레이캐스트를 끈다. 추가 무음 버튼은 보고만 하며 범위를 넓히지 않았다.

## 검증

- 새 EditMode `PromptB177SfxMenuTests` 8개: 버튼 연결·중복 배선·비활성 설정·무음 재표시·이어하기 성공/실패·런타임 없는 시작 실패·숨겨진 레거시 가이드 버튼 검사.
- 새 PlayMode `PromptB177SfxMenuPlayModeTests` 2개: 실제 설정 코루틴·슬라이더·드롭다운·토글·사이드 메뉴·시계·잘못된 슬롯 시작 실패 검사. EditMode 안의 EnterPlayMode는 다음 검사의 unscaledDeltaTime에 영향을 주어 분리했다. 확인 항목은 유지하며 PlayMode 전체 대신 새 검사 2개만 실행했다.
- 기존 `PromptB158SfxFixTests.MainMenuView_AttachesHoverAndClickPerButton_SlotsUntouched`의 설정 버튼 기대값만 UiClick→None 변경. 열림음과 클릭음 중첩 방지 요구에 맞춘 변경이다.
- 변경 전 전체 EditMode: **2238 통과 / 26 실패** (`Temp/b177-baseline-editmode.txt`). 문서의 과거 21개 기준보다 현재 직접 측정한 기준을 사용한다.
- 최종 관련 EditMode: **75 통과 / 0 실패** (`Temp/b177-targeted-final.txt`, B-177·B-158·B-150·플레이어 두 묶음). 최종 PlayMode만 실행: **2 통과 / 0 실패**, ExitCode 0 (`Temp/b177-playmode-final-code/PlayMode-summary.txt`).
- 최종 전체 EditMode: **2247 통과 / 25 실패** (`Temp/b177-final-normal-frame-editmode.txt`). **24개는 변경 전 실패와 동일**하다. 변경 전 실패 중 SurfaceQuest_ReusesDetailsWithoutChangingProgressOrRewards와 Issue253_XClosesQuestDetailsAndConsumesRepeatedClose는 최종 실행에서 통과했다.
- 기준에 없던 실패 1개: `PromptB150MainMenuTests.DeleteButton_Hover_FadesInWhiteTrashIcon`. 수정하지 않은 SlotDeleteButtonHover는 Time.unscaledDeltaTime으로 진행하고, 기존 검사는 EditMode에서 Update를 수동 반복한다. 에디터 프레임 시간에 따라 전체 묶음에서는 실패했지만 최종 관련 75개 재실행에서는 통과했다. **기준 외 실패가 항상 0이라는 조건은 미충족**이며, E 범위인 슬롯 삭제 코드·검사를 변경해 감추지 않았다.
- Bootstrap → MainMenu 실제 재생: 설정 초기 비활성 확인, 버튼으로 열기·슬라이더·취소, 재열기·적용 닫기를 확인했다. 기록은 `Temp/b177-smoke-sfx.txt`: UiPopupOpen → UiScanTick(pitch 1.02) → UiPopupClose → UiPopupOpen → UiConfirm. 슬라이더 조작의 SfxPlayer.StartedCount 증가 1 확인. 적용 닫힘음 없음.
- 메인 설정 화면 확인: `Temp/b177-main-settings.png`. UI 배치 변경 없음.
- 최종 에디터는 Bootstrap 비재생 상태로 복구했고 scriptCompilationFailed=false를 확인했다. 전체 검사 Console에는 기존 실패·예외 재현 로그가 포함되어 있다. 실제 플레이 전 구간의 새 오류·경고 유무는 수동 확인 항목으로 남긴다.
- 범위 밖 자동 변경(씬·프리팹·폰트·PNG·TimeManager)은 검사 전후 status로 확인하고 이번 검사에서 생긴 것만 복구한다. 기존 EditorBuildSettings·QualitySettings 변경은 그대로 둔다.
- Inspector 추가 연결 없음. 기존 직렬 참조와 컴포넌트 수명주기에서 연결한다.
- 교훈: `init/rules/lessons/2026-10-31-issue158-runtime-test-isolation.md`.

변경 파일은 위 연결·버튼 표의 런타임 C# 18개, 기존 B-158 검사 1개, 신규 EditMode/PlayMode 검사 2개와 해당 .meta 2개, 이 보고서와 교훈 파일이다. 프리팹·씬·음원·폰트·ProjectSettings는 이번 변경에 포함하지 않는다.

## #158 대응

| 항목 | 상태 |
|---|---|
| 설정창 | 구현 |
| 지상 기지 버튼 | 구현 |
| 모든 버튼 점검 | 일부: B-177 대상 연결, E 항목은 남음 |
| 우측 메뉴 | 구현 |
| T키 시계 | 구현 |
| 이어하기·새 게임 실패 | 구현 |
| D-2 시설 설치의 건설창 클릭음 | 구현 |

## 미검증

- 스피커·헤드폰으로 직접 들은 음색·체감 음량, 세 화면의 전체 수동 입력 경로, 엘리베이터 탑승 중 ESC와 종료 확인 창의 입력 차단, 실제 성공 세이브를 만드는 새 게임→탐사 완주는 미검증이다. 아래 가이드로 확인한다.
- B-178의 퀘스트 클리어·보상·심층 해금·드론 스캔, E 항목·음소거 버튼은 이번 범위에서 변경하지 않았다.

## 사용자 테스트 가이드 (프롬프트 원문)

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
