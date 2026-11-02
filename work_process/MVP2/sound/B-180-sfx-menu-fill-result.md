# B-180 결과: 메뉴·창 버튼 소리 채우기 (기존 소리만)

## 기준
- 브랜치 **`MVP2-fix126`**, 기준 `MVP2-fix125`(`b7c159fa`, 원격과 같음). `git fetch` 시점 원격 최신은 fix125.
- 포함 확인: B-178(`4f1bb898`), B-179(`b1471534`), B-179-1(`2b474db7`) 모두 기준에 있음 → 계획 순서대로 fix126을 만들었다. 다음 B-181은 **fix127**.
- 새 음원·새 SfxId·칸 값·버스 음량·설정 저장·세이브 삭제 규칙·연출 시간표는 바꾸지 않았다. 씬·프리팹 YAML은 고치지 않았다(모두 런타임 연결).

## 공통 변경
- `App/Audio/UiSfx.cs`: `UiSfx.Attach(..., Func<bool> hoverGate = null)` 선택 인자 추가(~149). 조건이 false인 동안 **호버음만** 막는다(~137 `HoverAllowed`). 클릭·실패음은 기존 그대로. 확인용 `HasHoverGate`(~29). 멈춤 API는 추가하지 않았다.

## 붙인 곳 (줄은 이번 브랜치 기준, ★ = PM이 바꿔 끼우는 상수)
| # | 파일·줄 | 소리 |
|---|---|---|
| A 가이드 | `UI/Guide/GameGuidePopupView.Build.cs` ★`GuideClickSfx` ~37, 조건 `CanPlayButtonSfx` ~40(`OnCardClicked`와 같은 조건), `AttachGuideSfx` ~43. X ~148(호버만), 탭 ~161, 단계 타일 ~194, 작은 카드 ~233, 접기 ~247, 필터 ~300, 목록 카드 ~334 / `GameGuidePopupView.Detail.cs` 관련 링크 ~404 | 클릭 UiClick(1), 호버 UiHover(2) |
| B CCTV 목록 | `UI/Outpost/CoreCctvPopupView.cs` ★`CctvItemClickSfx` ~597, 조건 `CanPlayItemSfx` ~600, `EnsureItem` ~689(항목을 만들 때 한 번) | 클릭 UiClick(1) + 호버. `failWhenDisabled: false` |
| C 작은 창 닫기 | 퀘스트 완료: `UI/Tutorial/QuestClearPopupMotion.cs` `Awake` → `AttachCloseHoverSfx` ~57(조건 `CanPlayCloseHover` ~26). 심층 해금: `UI/Progression/DeepZoneUnlockPopupMotion.cs` `Configure` ~139(조건 ~43) | 호버만 UiHover, 클릭 None |
| D 퀘스트 이전/다음 | `UI/Tutorial/DemoObjectivePresenter.cs` ★`QuestPageTurnSfx` ~137, `ShowPreviousQuest` ~148 / `ShowNextQuest` ~160(index를 바꾼 뒤). 버튼 호버: `DemoObjectiveView.Awake` ~100-101 | KeysPageTurn(49), 버튼은 호버만 |
| E 슬롯 삭제 | `UI/MainMenu/MainMenuBinder.cs` ★~20-23. 휴지통 ~171(+확인 창이 실제로 뜨면 ~174), 확정 ~183(슬롯 > 0), 취소·X 버튼·X 키 ~195(확인 창이 떠 있었을 때만). 호버: 확인 창 `MainMenuView.AttachButtonSfx` ~177-179, 휴지통 `SaveSlotCardView.Awake` ~66 | 1 / 6 / 36 / 7 + 호버 |
| F 카드 접힘 | `UI/MainMenu/MainMenuIntroDirector.cs` ★`ExitFoldSfx` ~22, `TickExit` ~219-226(`SfxPlayback.Crossed`). 접힘 시작 `MainMenuExitTimeline.FoldStart` ~49(Sample ~63도 이 값을 씀, 값 동일) | WindowTvClose(80) |
| G 음소거 | `UI/MainMenu/SettingsMuteButton.cs` ★`UnmuteClickSfx` ~11, `ToggleMute` ~96-102: `SetMuted(…, false, …)` 다음에 `SfxPlayer.Play` | 켤 때 무음, 풀 때 UiClick(1) |
| H 노드 호버 | `UI/Progression/UpgradeTreeNodeView.cs` `OnEnable` ~117, 조건 `CanPlayButtonSfx` ~90(`UpgradeWindowMotion.IsTransitioningFor`) | 호버 UiHover 추가, 클릭 UiClick 그대로 |

### 항목별 메모
- **A**: 열림·닫힘 연출 중에는 그룹이 막혀 클릭이 일어나지 않고, 호버는 조건으로 막는다. 키보드 선택 변경은 버튼을 거치지 않아 무음(버튼은 `UiKeyboardSubmitGuard`로 Enter 재실행도 막혀 있음). 닫힘음은 기존 `BeginClose`. 같은 탭·카드를 다시 눌러도 클릭음 한 번.
- **B**: 목록을 다시 그려도 항목을 재사용하고, `Attach`는 기존 컴포넌트를 다시 쓴다(검사로 한 개 확인). 닫는 중(`Exiting`)·목록 등장 전 호버는 무음.
- **C**: 퀘스트 완료 창 닫기 버튼은 프리팹 빌더(B-95-1)의 `ClaimCloseButton`이다. `ConfigureQuestClear`가 본문을 `ClaimContent` 아래로 옮기므로 하위 전체에서 이름으로 찾는다(못 찾으면 경고 한 번). `QuestClearCloseHover.cs`는 그대로 둠(참고만).
- **D 이벤트 중복 여부: 없음.** `SurfaceBaseQuestPanel`(SurfaceBasePanel 프리팹)과 `TutorialDirectorBinder`(Mine_Demo_Integration 씬)는 **서로 다른** `DemoObjectiveView`를 구독한다. 각 프리팹·씬의 이전/다음 onClick 영구 연결도 1개씩. 한 번 눌러 두 칸 넘어가는 문제는 코드 기준 없음. 같은 프레임 중복 방지는 따로 넣지 않았다(중복 호출 경로가 없고, 같은 칸은 `SfxPlayer` 0.05초 제한도 있음).
- **E**: 새 게임·이어하기 중 휴지통은 지금처럼 무시하고 무음. 메인 화면의 ESC는 설정창 담당이라 삭제 확인 창을 닫지 않는다(기존 동작). 닫기 단축키는 **X 키**이며 `OnDeleteCancel`을 거쳐 같은 규칙(창이 떠 있을 때만 7).
- **F**: 누를 때 UiConfirm은 바인더 그대로. `CancelExit` 뒤에는 Tick이 멈춰 무음. **잘림 여부: 미검증.** 소리는 0.05초에 시작하고 씬 전환은 약 0.4초 뒤다. `SfxPlayer`는 Bootstrap 루트에 붙어 씬 전환에도 남으므로 코드상 잘리지 않을 것으로 보이나 직접 듣지 못했다.
- **G 막대 0 경우(고치지 않음)**: 효과음 막대가 0이면 클릭음은 재생되지만(재생 횟수는 셈) 음량 0이라 들리지 않는다. 마스터·효과음이 음소거 중일 때 배경음 음소거를 풀면 그 음소거 때문에 안 들린다(맞는 동작). 마스터 막대가 0이어도 `AudioListener.volume`이 0이라 안 들린다.
- **H**: 카테고리 탭(`ProgressionUpgradeEntryButton`)은 손대지 않음. 노드별로 다른 소리 없음.

## 기존 실패 검사 (NoAlertBus) — **고침**
- `git log`: `6c94d07a fix: 시작 브리핑 지직 효과음을 짧은 클립으로 교체 (프롬프트 B-162)`에서 `briefing_close.wav`를 새 원본으로 바꾸며 `SfxLibraryDefaults.cs`와 `SfxLibrary.asset`의 41번 음량을 **함께** 0.5 → 1.0으로 올렸다(README도 같은 커밋에서 갱신). 의도한 값으로 판단.
- `Tests/EditMode/App/Audio/PromptB162SfxCStageTests.cs` 표(~37)만 1.0으로 고침. 소리 값·음원은 그대로.

## 테스트
- 새 EditMode `Tests/EditMode/App/Audio/PromptB180SfxMenuFillTests.cs`(13개)
  - A: 가이드의 모든 `GameGuideButton`·카드에 소리 컴포넌트 1개, X만 클릭 None, 열림·닫힘 연출 중 호버 0번, 탭 재클릭 포함 클릭 횟수, X는 닫힘음만
  - B: 목록 재구성 뒤에도 컴포넌트 1개·클릭 1번, 닫는 중 호버 0번
  - C: 두 창 닫기 버튼 호버만, 등장 전·닫는 중 0번, 표시 중 1번, 클릭 무음
  - D: 닫혀 있으면 0번, 처음에서 이전 0번, 가운데 이전·다음 각 1번, 끝에서 다음 0번 / 버튼 호버만
  - E: 휴지통 1(+6), 빈 슬롯 휴지통 1만, 확정 36, 취소·X 7, 확인 창 없을 때 X 키 0번, 이어하기 중 무음, 이어하기 확인음 1번 / 휴지통·확인 창 버튼 호버만
  - F: 접힘 시작에 80 한 번, 남은 진행 0번, `CancelExit` 뒤 0번
  - G(마스터·배경음·효과음): 켤 때 0번, 풀 때 UiClick 1번, 재생 시점 음소거 확인값 false, `StartedCount` +1
  - H: 호버 추가·클릭 그대로, 창 Opening·Closing 중 호버 0번(클릭은 남)
- 기대값을 바꾼 기존 검사: `PromptB162SfxCStageTests.Defaults_CSlotsMatchTable_NumberedFrom35_NoAlertBus`(위 이유).
- 실행(에디터 플래그 실행기)
  - B-180 단독: 첫 실행 12 통과 / 1 실패 → CCTV 검사에서 항목 선택(Awake에서 붙는 리스너)을 단언한 검사 쪽 실수. 단언을 빼고 다시 실행.
  - **전체 EditMode: 2303 통과 / 23 실패** (`sub-terra/Temp/b180-full-editmode.txt`). 23개 모두 `work_process/MVP2/B-172-verification/editmode-full.txt` 기준 실패 목록에 있음 → **새 실패 0개**. 기준 목록 중 `PromptB162…NoAlertBus`(이번에 고침)와 `PromptB150MainMenuTests.DeleteButton_Hover_FadesInWhiteTrashIcon`(프레임 시간 의존, 이번엔 통과)은 통과.
- PlayMode: 고친 PlayMode 검사가 없어 실행하지 않음.
- 테스트 실행이 범위 밖 파일(`NotoSansKR-Regular_SDF.asset`, `InventoryPanel`·`MainMenuPanel`·`ViewSocket` 프리팹, `Mine_Demo_Integration.unity`, `ProjectSettings/TimeManager.asset`, `work_process/MVP2/103-main-menu/*.png` 2개)을 바꿔 `git restore`로 되돌렸다.
- 교훈: `init/rules/lessons/2026-11-02-issue158-runtime-attach-moved-children.md`.

## 미검증
- 실제 플레이에서 들어 보는 확인은 하지 못했다. 아래 사용자 테스트 가이드 1~12 전부 **미검증**. 특히 F의 잘림 여부, 가이드 열림 직후(Opening 끝 무렵) 호버음과 페이지 넘김 소리의 겹침 체감, 슬롯 삭제 "뚝"(RunFailBlackout)의 어울림.

## 사용자 테스트 가이드
1. 광산에서 게임 가이드를 열고 탭·필터·"첫 탐사" 접기를 눌러 본다 → 누를 때마다 클릭음, 올리면 호버음이 난다. 창이 열리는 동안에는 섞이지 않는다.
2. 가이드에서 카드를 눌러 설명을 바꾼다 → 클릭음 한 번. X로 닫으면 닫힘 소리만 난다.
3. 전진기지 코어 CCTV 창에서 시설 목록을 하나씩 눌러 본다 → 클릭음이 난다.
4. 퀘스트를 깨서 완료 창이 뜨면 닫기 버튼에 마우스를 올린다 → 호버음. 누르면 예전처럼 닫힘·보상 소리만 난다.
5. 심층 구역이 열려 해금 창이 뜨면 닫기 버튼에서 4번과 같다.
6. 지상 기지 퀘스트 상세창에서 이전/다음을 누른다 → 넘김 소리. 처음·마지막 퀘스트에서는 안 난다.
7. 메인 화면에서 슬롯 휴지통을 누른다 → 클릭음과 함께 확인 창이 열리는 소리.
8. 확인 창에서 취소·X·ESC → 닫힘 소리. 다시 열어 삭제 → "뚝" 소리 후 슬롯이 비워진다. 버튼에 올리면 호버음이 난다.
9. 이어하기나 새 게임을 누른다 → 확인음 뒤 화면이 접힐 때 TV 꺼지는 소리가 난다. 잘려서 안 들리면 알려 준다.
10. 설정에서 음소거 버튼을 눌러 음소거를 켠다 → 아무 소리 없음. 다시 눌러 풀면 → 바로 클릭음이 들린다(마스터·배경음·효과음 셋 다).
11. 업그레이드 창 노드에 마우스를 올린다 → 호버음. 처음 열 때 자물쇠가 깨지는 동안에는 섞이지 않는다.
12. 효과음 음량·음소거가 이번 소리에 다 적용되고, Unity Console에 새 빨간 오류·노란 경고가 없다. 바꿔 보고 싶은 소리(★)가 있으면 PM에게 알려 준다.

> 8번 참고: 메인 화면에서 ESC는 설정창을 여닫는 키라 삭제 확인 창은 **X 키**로 닫힌다(기존 동작, 이번에 바꾸지 않음).

## #158 대응 표
| 항목 | 상태 |
|---|---|
| A 가이드 | 구현 |
| B CCTV 목록 | 구현 |
| C 작은 창 닫기 호버 | 구현 |
| D 퀘스트 이전·다음 | 구현 (이벤트 중복 없음) |
| E 슬롯 삭제 | 구현 (ESC 대신 기존 X 키 경로) |
| F 카드 접힘 | 구현 (잘림 여부 미검증) |
| G 음소거 | 구현 |
| H 노드 호버 | 구현 |
