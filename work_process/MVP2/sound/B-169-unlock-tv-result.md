# B-169 결과 (업그레이드 노드 해금 연출 + 창 TV 소리 + 업그레이드 창 자물쇠 소리)

- 기준 커밋: `507ec192` (MVP2-fix113, B-168-2 뒤 자물쇠·흡입 meta 추가). `f71535a8`(B-167)·`62316959`(#248) 모두 조상 확인(`git merge-base --is-ancestor`). 76~80 칸 있음. `TerminalWindowOpenMotion.Close`·`CloseDuration`·`IsClosingWindow` 있음.
- 브랜치: `MVP2-fix114` (MVP2-fix113에서 땀)
- PR: 아직 없음(커밋·PR은 사용자 확인 후)
- 음원 파일은 고치지 않았다. 새 칸은 81~84만 80 뒤에 붙였다. 기존 칸 값·버스 음량·`ProgressionService` 이벤트 순서·구매 규칙·세이브 형식은 그대로다.
- 작업 분담(하위 에이전트 Haiku, 지시는 줄 단위로 주고 diff를 검토함)
  - 칸 81~84 추가(`SfxId.cs`, `SfxLibraryDefaults.cs`)와 칸 수 검사 3곳 79→83
  - 세 창 열기·닫기 버튼 클릭음(`UiSfxButton`) 조사(읽기 전용): 프리팹·씬·코드·빌더 어디에도 붙어 있지 않음. PlayMode에서 실제 광산 씬도 확인(아래)
  - C 업그레이드 창 소리 EditMode 검사(`PromptB169UnlockAndTvTests.UpgradeWindow.cs`, partial)
  - 판정·연결 코드, A·B 검사, PlayMode 검사, 실행기, 보고서는 직접 했다.

## A. 업그레이드 노드 해금 연출

### 원인 확인
- 코드 경로: 구매 성공 → `UpgradeChanged`가 먼저 `Render` → 새로 풀린 노드 `Apply`가 `SetBlindVisible(!unlocked)`(기준 커밋 `UpgradeTreeNodeView.cs:167`)로 블라인드 알파 0 + 자물쇠 비활성. 빌더(`PromptB118UpgradeTreeBuilder.cs:506-515`)에서 자물쇠·파편이 블라인드 자식이라 함께 안 보임. 그 뒤 `PurchaseCompleted` → `PlayUnlock`이 보이지 않는 자물쇠를 흔듦.
- 같은 동작이 기존 검사 `PromptB118UpgradeTreeTests.UnlockedIcon_IsVisibleWhileUnlockFeedbackIsPending`에 "해금 확정 정보는 순차 연출을 기다리지 않음 → blind.alpha 0"으로 고정돼 있었다(이번에 B-169 의도대로 고침, 아래).
- 고친 뒤 실제 지상 기지 PlayMode: 구매 직후 새로 풀린 4개 노드 모두 `IsRevealPending`·자물쇠 활성·블라인드 알파 1을 검사로 확인. 화면: `work_process/MVP2/sound/B-169-node-unlock-play.png`(0.00 / 0.30 / 0.41 / 0.55 / 0.75 / 1.00 / 1.39 / 1.89초, 왼쪽 위부터). 연결선 → 흔들림 → 고리 들림 → 기울며 떨어짐 → 아이콘 공개가 0.12초 간격으로 이어진다.
- **미검증**: 고치기 전 코드로 같은 장면을 찍은 비교 화면은 남기지 않았다(원인은 코드와 기존 검사로 확인).

### 고친 곳
- 새 `Scripts/App/UI/Progression/UpgradeUnlockRevealTracker.cs`: 노드마다 "풀린 모습으로 그렸는지" 기억. 처음 그리기는 연출 없음, 그린 뒤 잠김→풀림만 대기. 대기는 연출을 실제 시작할 때(`MarkStarted`) 지움. 런타임 메모리만.
- 새 `Scripts/App/UI/Progression/UnlockRevealTimeline.cs`: 노드·썸네일 공용 자물쇠 자세 `SampleLock`(0~0.2 흔들림 `sin(p·8π)·12°·(0.5+0.5p)` → 0.2~0.3 크기 1+0.1 펄스·고리 들림 → 0.3~0.65 떨어짐·-25°·흐려짐)과 소리 시점(0 Rattle / 0.2 Click / 0.65 Glow, `SfxPlayback.Crossed`로 프레임이 튀어도 한 번).
- `UpgradeTreeView.cs`
  - `:290` `Render`에서 `node.SetRevealPending(tracker.Observe(...))` 뒤 `Apply`.
  - `:788` `TickUnlockReveal`(LateUpdate): 창이 보이고 `UpgradeWindowMotion.IsTransitioningFor`(열기·닫기 중)가 아닐 때만 시작. 대기 노드마다 커넥터 `PlayFlow`(0.22초) 먼저, 노드는 0.22 + 0.12×순번에 시작. 처음 3개만 소리. 창이 닫히거나 열기·닫기 연출이 시작되면 아직 시작 안 한 노드는 대기로 남는다.
  - `:859` 같은 소리 0.1초 제한은 **예정된 시간표 시각**으로 판정(아래 "고친 실수").
  - `:754` `PlayPurchaseFeedback`의 `PlayUnlock` 직접 호출 제거(두 번 재생 방지). 레벨업 소리는 그대로 먼저 난다.
  - `:27` `UnlockStagger` 0.08 → 0.12(프롬프트 값).
  - `:865` `ResetUnlockReveal`: `ProgressionPanelPresenter.Bind`(`ProgressionPanelPresenter.cs:59`)에서 불러 불러오기·다시 묶기 직후 첫 그리기는 연출 없음.
- `UpgradeTreeNodeView.cs`
  - `:205` `Apply`는 정보를 채우되 대기 중이면 블라인드·자물쇠 유지(`SetBlindVisible(!unlocked || revealPending)`), 테두리·준비 표시도 잠김 색.
  - `:494` `BeginUnlock` / `:529` `StepUnlock` / `:540` `SampleUnlock`: 코루틴 대신 수동 진행(EditMode 검사 가능). 1.0초, 0.65~1.0 블라인드가 걷히며 청록 발광. 노드 자물쇠는 썸네일의 1/1.3이라 떨어지는 거리 40/1.3, 옆 밀림 4/1.3. 고리 들림 6px. 파편은 쓰지 않고 꺼 둔다(필드·오브젝트 그대로).
  - `:362` 꺼지면 시작한 연출은 끝 상태(풀림), 시작 안 한 대기는 유지(OnEnable·OnDisable에서 대기를 지우지 않게 바꿈).
- `DeepZoneThumbnailStrip.cs`: `:315` 자물쇠 자세를 공용 `SampleLock`으로 바꿈(검사로 이전 식과 비트 단위 같음 확인). 코루틴을 `Update` + `TickReveal`(`:221`)로 바꾸고, 연출 한 번에 소리 한 세트(`:233`). `FinishReveal`로 바로 끝날 때는 무음. 해금 팝업(`DeepZoneUnlockPopupMotion`) 소리는 넣지 않았다.
  - 참고: `DeepZoneDisplaySettings.UnlockedLevelCount`가 최대 1이라 지금 게임에서는 한 번에 한 칸만 풀린다. 여러 칸이어도 연출이 한 묶음이라 한 세트만 난다.

## B. 세 창 TV 소리
- 새 `Scripts/App/Audio/WindowTvSfx.cs`: UI 버스 AudioSource 하나(`ignoreListenerPause`), 음량 = 칸 × 버스 × 효과음 설정, 음소거면 무음. 새 소리가 앞 소리를 `Stop` 후 처음부터 낸다. `SfxPlayer`에 멈춤 API를 추가하지 않았다.
- `Scripts/App/UI/TerminalWindowOpenMotion.cs` 한 곳에서만 낸다: 열기 시작 `:99, :106`(`PlayOpenSound`), 닫기 시작 `:132`, 모션 감소 닫기 `:63`. "보이는 창" 플래그(`IsShown`)로 이미 열린 창 다시 열기(내용 갱신)·숨은 창 다시 닫기(HUD Awake/Start의 숨김 호출)·비활성화는 무음. 닫는 중 다시 열면 꺼짐을 끊고 켜짐.
- 이 컴포넌트를 쓰는 창은 인벤토리(`InventoryPanelView`), 건설창(`BuildingMenuView`), 퀘스트 상세(`DemoObjectiveView.detailsRoot`) 세 개뿐이다(코드 검색). 다른 창은 없음.
- 세 창 열기·닫기 버튼 클릭음: 붙은 것 없음 → 바꿀 것 없음. 실제 광산 씬 PlayMode에서 이름에 Inventory·Building·Quest·Objective가 들어간 `UiSfxButton` 중 클릭음이 None이 아닌 것 0개 확인.
- 업그레이드 창·다른 팝업(CCTV, 보관함, 판매 등)은 건드리지 않았다.

## C. 업그레이드 창 자물쇠 소리 (#252)
- 새 칸(UI 버스, `SfxLibraryDefaults.cs` + `Resources/SfxLibrary.asset` 빌더로 채움, 파일 길이 그대로)

| 번호 | SfxId | 파일 | 음량 |
|---|---|---|---|
| 81 | UpgradeLockCrack | upgrade_lock_crack (0.60초) | 0.25 |
| 82 | UpgradeLockShatter | upgrade_lock_shatter (0.78초) | 0.55 |
| 83 | UpgradeWindowSuckIn | upgrade_window_suck_in (0.38초) | 0.35 |
| 84 | UpgradeLockRegather | upgrade_lock_regather (0.25초) | 0.5 |

- 새 `Scripts/App/UI/SurfaceBase/UpgradeWindowMotionSfx.cs`: 모션 하나에 한 번만 구독. ShakeStarted→UnlockRattle, CrackStarted→UnlockClick(+처음 열기만 UpgradeLockCrack), Shattered→UpgradeLockShatter, CloseStarted→UpgradeWindowSuckIn, FragmentsRegathered→UpgradeLockRegather. 같은 소리 0.1초 안 한 번.
- 붙인 곳: 지상 `SurfaceBaseView.cs:95`(`EnsureUpgradeMotion`), 광산 `PanelToggleController.cs:134`(`SetUpgradeVisible`). 두 곳 모두 `Attach`가 이미 있으면 다시 구독하지 않는다(PlayMode에서 컴포넌트 1개 확인).
- 모션 감소: 자물쇠 훅이 나지 않으므로 자물쇠 소리 0. `UpgradeWindowMotion`에 읽기 전용 `IsReducedMotionTransition`(`:106`, `:158`, `:231`)을 더해 `BeginOpen`·`BeginClose`의 모션 감소 경로에서만 `OpenCompleted`·`CloseCompleted`에 WindowTvOpen·WindowTvClose를 낸다. `CompleteImmediately`(설정 창 강제 숨김·Bind·씬 이동)·`SetUpgradeVisible`은 무음. 훅 위치·시간표 값은 바꾸지 않았다(`IsTransitioningFor` `:131`도 읽기 전용).
- 조각 모임 "착"은 README대로 닫기 연출이 끝난 뒤 약 0.15초에 들린다. 시점은 바꾸지 않았다. 늦게 들리는지는 **미검증**(PM 청감 확인 필요).

## 음량 (클립 0.1초 최대 RMS, 칸 음량·버스 적용 뒤, dBFS)

| 소리 | 길이 | 피크 | 0.1초 최대 | 최종 |
|---|---|---|---|---|
| window_tv_close | 0.56 | -1.0 | -4.8 | **-10.8** |
| upgrade_lock_shatter | 0.78 | 0.0 | -10.4 | -15.6 |
| window_tv_open | 0.73 | -1.2 | -7.7 | -15.7 |
| upgrade_lock_regather | 0.25 | 0.0 | -10.5 | -16.5 |
| unlock_glow | 1.00 | -1.1 | -10.4 | -17.3 |
| (비교) upgrade_levelup | 0.56 | -1.0 | -12.1 | -17.3 |
| unlock_click | 0.44 | -1.2 | -16.8 | -19.9 |
| unlock_rattle (0.3초까지) | 0.67 | -3.7 | -16.2 | -22.3 |
| upgrade_window_suck_in | 0.38 | -1.2 | -13.5 | -22.6 |
| upgrade_lock_crack | 0.60 | -1.1 | -14.2 | -26.2 |

- 프롬프트 시작 음량 그대로 두었다. window_tv_close가 다른 소리보다 5dB 안팎 크다(창을 닫을 때마다 나므로 0.3 안팎까지 줄일 여지). unlock_rattle은 0.3초에서 끊겨 가장 작은 편(노드 해금 "덜그럭"이 묻히면 0.7 안팎까지 올릴 여지). crack은 딸깍 밑에 까는 용도라 작게 둔 값. 직접 들어 보지 않아 바꾸지 않았다(**미검증**).

## 테스트
- 새 EditMode `PromptB169UnlockAndTvTests` 20개 통과(메뉴 `SubTerra/Tests/Run Prompt-B 169 Unlock And TV EditMode Tests`, 플래그 `Temp/subterra-prompt-b169.flag` 첫 줄 `edit`)
  - 해금 추적(첫 그리기·불러오기 0, 잠김→풀림 1, 닫힌 동안 풀리면 다음 열 때 1번, 재생 뒤 다시 열면 0), 열기 연출 중 미룸·열다 닫으면 대기 유지
  - 노드: 대기 중 `Apply(풀림)` 뒤 블라인드 1·자물쇠 활성 → 흔들림·펄스·떨어짐·기울기 → 1.0초 뒤 블라인드 0·자물쇠 꺼짐, 파편 꺼짐, 중간에 꺼지면 풀림으로 끝
  - 소리: 0/0.2/0.65 각 1번, 0.5초·1초 프레임 건너뛰기에도 각 1번, 노드 5개면 3개만(들쭉날쭉한 프레임), 0.12초 간격, 구매 피드백은 레벨업만 내고 해금은 한 번만 재생
  - 심층 썸네일: 공용 자세가 이전 식과 같음(0~0.65를 0.0025 간격, RectTransform 값까지), 한 세트, FinishReveal 무음
  - 창 TV: 연달아 열면 두 번째가 첫 번째를 끊음(AudioSource 하나), 이미 열린 창·닫힌 창은 무음, 닫는 중 다시 열기, 모션 감소면 연출 없이 소리만, 음소거 무음
  - 업그레이드 창(지상·광산 `Tick`): 처음 열기 Rattle→Click+Crack→Shatter, 다시 열기 Click→Shatter, 닫기 SuckIn→Regather, 0.6초 한 프레임 건너뛰기 각 1번, 연타, 모션 감소면 자물쇠 0·TV 켜짐/꺼짐 각 1, `SetUpgradeVisible` 무음, 칸 81~84(+76~80 그대로) 값·파일·asset
- 기존 검사 고침(이번 의도 변경에 맞춤)
  - `PromptB118UpgradeTreeTests`: `UnlockedIcon_IsVisibleWhileUnlockFeedbackIsPending` → `UnlockedIcon_IsFilledButCoveredWhileUnlockRevealIsPending`(정보는 채우고 블라인드는 유지), `UnlockFeedback_FinalTweenNeverRehidesUnlockedIcon`(없어진 코루틴을 리플렉션으로 부름) → `UnlockFeedback_FinalFramesNeverRehideUnlockedIcon`(StepUnlock).
  - `PromptB168SfxD2Tests`: "upgrade_lock 칸이 없어야 한다(B-169에서)" → "81~84번으로만 들어온다". 칸 수 79→83(B156·B165·B168).
- EditMode 전체: B-169 적용 2121 통과 / 20 실패. 같은 작업트리에서 B-169 변경을 stash한 기준선 2101 통과 / 20 실패. **실패 20개 목록이 정확히 같다**(새 실패 없음). 단독으로 돌리면 통과하는 `PromptB89UndergroundMenuTests.X_ClosesOnePanelAtATimeWithoutOpeningSettings`도 기준선에서 똑같이 실패한다(전체 실행 순서 영향).
- 이웃 묶음(B-169, B-167, B-118, B-155, B-168) 119개 통과. Progression·Audio·Inventory·BuildingMenu·DemoObjective·SurfaceBase 묶음에서도 기존 실패 외 새 실패 없음.
- 새 PlayMode `PromptB169UnlockAndTvPlayModeTests` 2개 통과(메뉴 `... PlayMode Tests`, 플래그 첫 줄 `play`). 슬롯 0 격리 런타임, 플레이어 세이브 미사용, TearDown에서 빈 씬으로 정리.
  - 지상: 씬 여는 동안 TV·자물쇠 소리 0 → 업그레이드 버튼 처음 열기 소리 순서 → 드릴 속도 Lv.1 구매 시 새로 풀린 4개 노드 자물쇠 보임 → 연출 끝 풀림, 레벨업 먼저·해금 소리 3세트 → 닫기 SuckIn·Regather → 다시 열기 Click·Shatter, 해금 재생 없음.
  - 광산: 씬 여는 동안 TV 소리 0, 인벤토리·건설창·퀘스트 상세 열기/닫기 각 1번, 닫힌 창 다시 닫기 무음, 퀘스트 이전/다음 무음, 연타에 끊고 다시·창 크기/위치 원래대로, TV 소리 음량 = 칸×버스×설정, U키 경로(`PanelToggleController`) 자물쇠 소리 지상과 같음, 모션 감소면 인벤토리·업그레이드 창 모두 TV 소리만.
- Console: 검사 실행 중 오류 없음. 경고는 기존 CCTV 카메라(`Camera.stereoTargetEye`) 1건뿐.
- 검사 실행 뒤 범위 밖 파일이 dirty됐다(NotoSansKR SDF, InventoryPanel·MainMenuPanel·ViewSocket 프리팹, Mine_Demo_Integration.unity, ProjectSettings/TimeManager.asset, 103 메인 메뉴 PNG 2장). 모두 `git restore`로 되돌렸다.

## 고친 실수
- 지상 PlayMode 첫 실행에서 새로 풀린 노드 4개 중 소리 낸 노드가 2개뿐이었다. 0.12초 간격이 프레임 경계 때문에 실제 시각으로 0.1초 아래로 붙어 같은 소리 제한에 걸렸다. 같은 소리 제한을 예정된 시간표 시각(노드 시작 예정 시각 + 소리 시점)으로 판정하게 고치고, EditMode 검사에 들쭉날쭉한 프레임을 넣어 고정했다. `init/rules/lessons.md`에 한 줄 추가.

## 다른 담당자 참고
- `UpgradeTreeNodeView.PlayUnlock(delay)`는 없어졌다. 해금 연출은 `UpgradeTreeView`가 순서를 잡아 `BeginUnlock`을 부른다.
- `TerminalWindowOpenMotion`을 새 창에 쓰면 TV 소리가 자동으로 난다. 소리가 맞지 않는 창이면 PM 확인 뒤 끄는 옵션이 필요하다.
- 에디트 모드에서는 검사가 `WindowTvSfx.ResetForTests`로 주입했을 때만 소리를 내 다른 EditMode 검사가 숨은 오브젝트를 남기지 않는다.

## 미검증
- 모든 새 소리를 실제 스피커로 들어 본 것(음색·타이밍·크기 체감). 특히 조각 모임 "착"의 늦음, window_tv_close 크기, 해금 "덜그럭"이 묻히는지.
- 광산에서 실제 조건을 채워 창이 닫힌 동안 노드가 풀린 뒤 다시 열 때의 연출(EditMode 트리 검사로만 확인).
- 실제 마우스·키보드(I·B·U키, X 버튼, ESC)로 연 경로(PlayMode는 HUD 메서드·`PanelToggleController` 직접 호출).
- 설정 창에서 모션 감소를 실제로 켜고 끄는 조작(검사는 값만 바꿈), 일시정지 중 창 소리 체감, 효과음 음량 슬라이더·음소거 조작.
- 고치기 전 코드의 자물쇠가 안 보이는 장면 캡처.

## 수동 확인 체크리스트
- [ ] 지상 기지에서 업그레이드를 사서 다음 노드가 풀리면, 그 노드 위 자물쇠가 흔들리고("덜그럭") → "딸깍" → 아래로 떨어지며 기울어 사라지고 → 아이콘이 빛나며("샤랑") 나타난다.
- [ ] 한 번에 여러 노드가 풀리면 짧게 이어서 풀리고, 소리가 한꺼번에 시끄럽게 쌓이지 않는다.
- [ ] 광산에서 조건을 채워 노드가 풀린 뒤(창이 닫혀 있을 때) 업그레이드 창을 열면 그때 해금 연출이 나온다. 다시 열면 또 나오지 않는다.
- [ ] 심층 구역 썸네일이 풀릴 때도 같은 세 소리가 난다.
- [ ] 인벤토리·건설창·퀘스트 상세창을 열면 켜짐 소리가, 닫으면 꺼짐 소리가 연출 시작과 맞춰 난다.
- [ ] 창을 빠르게 열고 닫기를 반복해도 창 크기·위치가 이상하게 남지 않고, 소리가 겹쳐 쌓이지 않는다. 닫히는 중에는 창 버튼이 눌리지 않는다.
- [ ] 퀘스트 상세창에서 이전/다음을 눌러도 켜짐 연출과 소리가 다시 나지 않는다.
- [ ] 설정의 모션 감소를 켜면 창이 바로 열리고 닫힌다. 일시정지 중에도 연출이 재생된다.
- [ ] 지상 기지에서 업그레이드 창을 처음 열면 자물쇠가 흔들릴 때 "덜그럭", 잠금이 풀리며 금 갈 때 "딸깍"과 작은 "치직", 부서질 때 "쨍그랑"이 연출과 맞게 난다.
- [ ] 다시 열면 "딸깍" 뒤 바로 "쨍그랑"만 짧게 난다.
- [ ] 닫으면 창이 빨려 들어가는 "슈욱" 뒤 조각이 모이는 "착"이 난다. 둘이 뭉개지지 않고 "착"이 너무 늦지 않다.
- [ ] 광산에서 U키·메뉴 버튼으로 연 업그레이드 창도 같은 소리가 난다.
- [ ] 빠르게 열고 닫기를 반복해도 소리가 쌓여 시끄러워지지 않는다.
- [ ] 모션 감소를 켜면 업그레이드 창은 다른 창처럼 켜짐·꺼짐 소리만 난다.
- [ ] 효과음 음량·음소거가 새 소리에 적용되고 Console에 경고나 오류가 없다.

## #158 대응
| #158 항목 | 이 작업 |
|---|---|
| D-2 업그레이드 창: 다음 노드 해금(흔들림→딸깍→떨어짐→빛, 여러 개 이어서) | A (검사·PlayMode 화면 확인, 청감 미검증) |
| E 창 등장 공통: TV 켜짐·꺼짐 공통음(건설창·인벤토리·퀘스트 상세), 연달아 열면 끊고 다시 | B |
| E 업그레이드 창 심층 구역 썸네일 해금 소리 | A-4 |
| D-2 업그레이드 창 열고 닫을 때(#252): 흔들림·금·부서짐·빨려 들어감·조각 모임 | C |
- #158 본문과 #241·#252는 고치거나 닫지 않았다.
