# B-180 메뉴·창 버튼 소리 채우기, 기존 소리만 (#158 B·A·D-2·E의 B-180 줄)

## 목표
- 2026-11-02 점검에서 찾은 "누르거나 마우스를 올려도 조용한" 버튼과 창을 **이미 있는 소리로만** 채운다. 새 음원·새 SfxId는 없다.
- 소리 id는 연결 지점마다 상수 하나로 둬서 PM이 번호만 바꿔 끼울 수 있게 한다(아래 표의 ★).
- 필요하면 다른 에이전트에게 나눠 맡겨도 된다. 결과는 한 브랜치로 모은다.

## 브랜치
1. `git fetch`로 원격 `MVP2-fixNNN` 중 가장 큰 번호를 확인한다. 계획 순서는 B-179(`fix124`, `385b4a98`) → B-179-1(`fix125`) → B-180(`fix126`)이다.
2. B-179-1 커밋(`… (프롬프트 B-179-1)`)이 들어간 `MVP2-fix125`가 있으면 그 위에서 **`MVP2-fix126`**을 만든다.
3. 없으면 `fix125`·`fix124`(`385b4a98`) 중 있는 것의 가장 최신 위에서 **비어 있는 다음 번호**를 만들고, 보고서 맨 위에 기준 브랜치·커밋과 B-179-1 포함 여부를 적는다.
   - 계획 순서와 번호가 어긋났으면 뒤에 돌릴 프롬프트(B-181)가 어느 번호를 써야 하는지 보고서에 적는다.
   - B-179 커밋(`b1471534`)이 없으면 멈추고 보고한다.
4. 기준에 B-178 커밋(`feat: 퀘스트 클리어·심층 해금 창, 보상 지급, 드론 스캔 효과음을 연결 (프롬프트 B-178)`, `4f1bb898`)이 없으면 **멈추고 보고한다.**

## 절대 규칙
1. SfxId 번호·칸 값·버스 음량·음원 파일은 바꾸지 않는다. 새 칸도 만들지 않는다(91은 B-179 몫).
2. App 코드는 `SfxPlayer.Play`. `SfxPlayer`에 멈춤 API를 추가하지 않는다. 버튼은 `UiSfx.Attach`로, 코드가 이미 결과음을 내는 버튼은 `hover: true`, 클릭 `SfxId.None`(소리 두 번 금지).
3. 씬·프리팹 YAML은 고치지 않는다. 런타임에 버튼을 찾아 붙인다. 연출 시간·모양, 저장, 퀘스트·삭제 규칙, B-164~B-179 소리 연결은 바꾸지 않는다.
4. 줄 번호는 fix123 기준 근사(~). 못 찾으면 "못 찾음"으로 보고. 버튼이 생각과 다르게 생겼으면 가장 가까운 방법을 쓰고 이유를 적는다.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/Scripts/` 기준)
- #158의 `(B-180)` 줄 8개, `work_process/MVP2/sound/B-177-sfx-menu-result.md`, `B-178-sfx-quest-drone-result.md`
- App/Audio/UiSfx.cs(`UiSfxButton`, `Attach`), App/Audio/SfxPlayer.cs(재생할 때 음소거 확인 ~46·~279)
- 검사 양식: Tests/EditMode/App/Audio/PromptB177SfxMenuTests.cs, PromptB178SfxTests.cs

## 붙일 곳
| # | 플레이어가 하는 것 | 위치(fix123) | 소리 |
|---|---|---|---|
| A | 게임 가이드 창에서 탭·필터 칩·"첫 탐사" 접기·카드(목록·단계 타일·작은 카드·관련 링크)를 누름 | App/UI/Guide/GameGuidePopupView.Build.cs ~143 탭, ~213 작은 카드, ~226 첫 탐사, ~278 필터 / GameGuideCardView.cs ~146·~195 / GameGuidePopupView.Detail.cs ~403 | 클릭 `GuideClickSfx = UiClick(1)`★, 호버 `UiHover(2)` |
| B | 코어 CCTV 창에서 시설 목록 하나를 고름 | App/UI/Outpost/CoreCctvFacilityItem.cs ~45(`button`), CoreCctvPopupView.cs ~596 | 클릭 `CctvItemClickSfx = UiClick(1)`★ + 호버 |
| C | 퀘스트 완료 창·심층 해금 창의 닫기 버튼에 마우스를 올림 | 퀘스트 완료: DemoObjectiveView.cs `claimRoot` ~56 아래 닫기 버튼(프리팹 onClick → `OnClaimConfirmClicked` ~1186). 심층 해금: DeepZoneUnlockPopupMotion.cs `parts.CloseButton` ~97·`NewButton` ~253 | 호버만 `UiHover(2)`, 클릭 None(닫힘음·보상음은 B-178이 냄) |
| D | 퀘스트 상세창에서 이전/다음으로 넘김 | App/UI/Tutorial/DemoObjectivePresenter.cs `ShowPreviousQuest` ~136 / `ShowNextQuest` ~147(index가 실제로 바뀐 뒤) | `QuestPageTurnSfx = KeysPageTurn(49)`★. 버튼은 호버만 |
| E | 메인 화면 슬롯 삭제 | 아래 E 참고 | 1 / 6 / 36 / 7 + 호버 |
| F | 이어하기·새 게임 뒤 카드가 번쩍이고 화면이 접힘 | App/UI/MainMenu/MainMenuIntroDirector.cs `PlayExit` ~158, MainMenuExitTimeline.cs(접힘 시작 = `flashDuration*0.5` ~61) | `ExitFoldSfx = WindowTvClose(80)`★ |
| G | 설정창 음소거 버튼(마스터·배경음·효과음)을 누름 | App/UI/MainMenu/SettingsMuteButton.cs `ToggleMute` ~91-95 | 아래 G 규칙. `UnmuteClickSfx = UiClick(1)`★ |
| H | 업그레이드 창 노드에 마우스를 올림 | App/UI/Progression/UpgradeTreeNodeView.cs ~113(지금 클릭만) | `hover: true`(UiHover 2), 클릭은 그대로 |

### A. 게임 가이드
- 가이드 창이 `Open`일 때(또는 `Opening`이면서 상세가 눌릴 수 있을 때, `OnCardClicked` ~53과 같은 조건)만 소리. 처음 열 때 세 장 넘김·한 장 넘김·닫힘(B-156)과 겹치지 않게, 열림·닫힘 연출 중 호버음은 무음.
- 닫기 X(~131)는 `RequestClose`가 닫힘음을 내므로 호버만. 키보드로 선택이 바뀌는 것은 무음.
- 이미 고른 탭·카드를 다시 눌러도 버튼을 누른 것이니 클릭음 한 번.

### B. CCTV 시설 목록
- `failWhenDisabled: false`. 목록을 다시 그릴 때(항목 재사용) `Attach`가 겹쳐 붙어도 한 번만 나는지 확인(`DisallowMultipleComponent`).

### C. 작은 글리치 창 닫기 호버
- 닫는 중(`IsClosing`)·등장 연출 중에는 호버음 없음. `QuestClearCloseHover.cs`는 지금 아무 데도 안 붙어 있다(참고만).

### D. 퀘스트 이전/다음
- 소리는 Presenter에서 index가 바뀐 경우에만 낸다(끝에서 누르면 무음). `SurfaceBaseQuestPanel` ~86과 `TutorialDirectorBinder` ~213이 둘 다 같은 이벤트를 받는지 확인한다. 한 번 눌렀는데 두 번 불리면(두 칸씩 넘어감) 고치지 말고 보고서에 적고, 소리는 같은 프레임에 한 번만 낸다.
- 상세창 TV 켜짐·꺼짐(B-169)·퀘스트 버튼 클릭(B-177)은 그대로.

### E. 슬롯 삭제 (App/UI/MainMenu/)
- 휴지통 클릭: `MainMenuBinder.OnDeleteSlotClicked` ~161에서 `DeleteTrashSfx = UiClick(1)`★. `RequestDeleteSlot`이 true라 확인 창이 실제로 떴으면 `DeleteConfirmOpenSfx = UiPopupOpen(6)`★도 같이 낸다(같은 프레임이어도 됨).
- 삭제 확정: `OnDeleteConfirm` ~167에서 `ConfirmDeleteSlot`이 0보다 큰 슬롯을 돌려주면 `DeleteConfirmSfx = RunFailBlackout(36)`★("뚝").
- 취소·X·ESC(~130·~133): 확인 창이 떠 있었을 때만 `DeleteCancelSfx = UiPopupClose(7)`★.
- 호버: 카드의 휴지통(`SaveSlotCardView.deleteButton` ~22), 확인 창의 확정·취소·X(`MainMenuView` ~39-41). 클릭은 None(위 코드가 냄). 새 게임·이어하기 중(`startingNewGame || continuing`)에는 지금처럼 무시하고 무음.

### F. 카드 접힘
- 접힘이 시작되는 시점을 "이번 프레임에 넘었는지"(`SfxPlayback.Crossed`)로 한 번만 낸다. 누를 때 확인음(UiConfirm)은 그대로. `CancelExit`으로 되돌아갈 때는 무음.
- 씬이 바뀌며 소리가 잘리면 "잘림"으로 보고만 한다(대기 시간은 바꾸지 않음).

### G. 음소거 버튼 (PM 결정)
- 음소거를 **켜면 무음.** 음소거를 **풀면 풀린 직후 바로 클릭음(UiClick 1) 한 번.**
- 순서: `SetMuted(…, false, …)`를 먼저 부르고 **그다음** `SfxPlayer.Play`. 그래야 음소거 확인(`SfxPlayer` ~46 `isMuted`)에 클릭음이 먹히지 않는다. `UiSfx.Attach`로 클릭음을 붙이지 않는다(누르는 순간엔 아직 음소거라 먹힘).
- 배경음 음소거를 풀 때도 클릭음을 낸다. 마스터·효과음이 음소거 중이면 그 때문에 안 들리는 건 맞는 동작. 효과음 막대가 0이라 안 들리는 경우도 고치지 말고 보고서에 적는다.
- 아이콘 다시 그리기·저장 방식·B-179 슬라이더 소리는 그대로.

### H. 업그레이드 노드 호버
- 처음 열 때 자물쇠 연출·닫힘 빨려 들어감 중에는 호버음 없음. 아이콘별로 다르게 하지 않는다. 카테고리 탭(`ProgressionUpgradeEntryButton` ~175)은 이번에 손대지 않는다.

## 함께: 기존 실패 검사 하나
- `PromptB162SfxCStageTests.Defaults_CSlotsMatchTable_NumberedFrom35_NoAlertBus`는 41번 BriefingClose 음량을 0.5로 기대하지만, `SfxLibraryDefaults.cs` ~130과 `SfxLibrary.asset` 둘 다 1.0이다.
- `git log`로 1.0이 의도한 값인지 확인한다. 맞으면 **검사 표(~36)만 1.0으로 고친다.** 확실하지 않으면 고치지 말고 보고만 한다. **소리 값·음원은 바꾸지 않는다.**

## 손대지 말 것
- SfxId·SfxLibrary 칸, 음원, 설정 저장, 세이브 삭제 규칙, 가이드·업그레이드·메인 화면 연출 시간표, B-178 작은 창·보상 소리, B-179 대상(슬라이더·시계·키 조작 창), `AGENTS.md`·규칙 문서

## 테스트
- EditMode 전체에서 기준 브랜치의 기존 실패 말고 새 실패가 없어야 한다. 기대값을 바꾼 검사는 이름과 이유를 적는다.
- 새 EditMode `PromptB180SfxMenuFillTests`, PlayMode가 필요하면 `PromptB180SfxMenuFillPlayModeTests`(소리 요청을 잡는 방식은 B-177·B-178 검사를 따름)
  - A·B·H: 버튼마다 `UiSfxButton` 클릭·호버 값, 가이드 닫기 X는 호버만, 열림 연출 중 0번
  - C: 두 닫기 버튼 호버 1번·클릭 None, 닫는 중 0번
  - D: 가운데에서 이전·다음 각 1번(KeysPageTurn), 처음·끝에서 0번, 상세창이 닫혀 있으면 0번
  - E: 휴지통 1(+확인 창이 뜨면 6), 확정 36, 취소·X·ESC 7, 확인 창 없을 때 ESC 0번
  - F: 접힘 시작에 80 한 번, 확인음은 그대로 1번, `CancelExit` 뒤 0번
  - G: 켤 때 0번, 끌 때 UiClick 1번이고 재생 시점에 음소거가 이미 풀려 있음(검사용 `isMuted`로 확인)
- PlayMode는 고친 검사만. 실행 전후 `git status`로 범위 밖 프리팹·폰트·씬·PNG·TimeManager 변경을 확인하고 되돌린다.

## 사용자 테스트 가이드 (보고서에 그대로 넣기)
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

## 완료 후
- 한국어 conventional 커밋(예: `feat: 가이드·CCTV·슬롯 삭제·음소거 버튼 등 메뉴 소리를 채움 (프롬프트 B-180)`). 새 브랜치에 일반 push, 브랜치는 지우지 않는다. PR은 만들지 않는다.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue158-주제.md` 파일 하나로 기록. **`init/rules/lessons.md`에 줄을 추가하지 않는다.**
- 결과 보고서 `work_process/MVP2/sound/B-180-sfx-menu-fill-result.md`
  - 기준 브랜치·커밋(B-179 포함 여부, fix125/126 중 무엇을 만들었는지와 이유), 소리마다 붙인 곳(파일·줄)과 SfxId, ★ 상수 위치
  - D의 이벤트 중복 여부, G의 막대 0 경우, F 잘림 여부, NoAlertBus 검사를 고쳤는지/보고만 했는지
  - 테스트 결과, 직접 들어 보지 못한 항목은 "미검증", 위 사용자 테스트 가이드
  - **#158 대응 표**: A 가이드 / B CCTV 목록 / C 작은 창 닫기 호버 / D 퀘스트 이전·다음 / E 슬롯 삭제 / F 카드 접힘 / G 음소거 / H 노드 호버 각각 구현·일부·못 함
