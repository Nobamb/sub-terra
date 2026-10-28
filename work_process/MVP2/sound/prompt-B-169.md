# B-169 업그레이드 노드 자물쇠 해금 연출 + 창 TV 켜짐·꺼짐 소리 + 업그레이드 창 자물쇠 소리 (#158 D-2, #241·#252 후속)

## 목표
- A. 일반 업그레이드 노드(최대 전력·최대 체력 등)가 풀릴 때 자물쇠가 **보이지 않는** 문제를 고치고, 심층 구역 썸네일과 같은 "흔들림 → 딸깍 → 떨어지며 회전 → 빛" 연출로 바꾼다. 소리 3개(UnlockRattle·UnlockClick·UnlockGlow)를 노드와 심층 썸네일 둘 다에 붙인다.
- B. #241 연출은 다른 작업자 PR #248(`codex/issue-241-tv-window-lifecycle`, 62316959)이 인벤토리·건설창·퀘스트 상세창에 이미 넣었다. 이번에는 그 세 창에 WindowTvOpen/WindowTvClose 소리만 연출 시점에 맞춰 붙인다. 연출은 새로 만들거나 바꾸지 않는다.
- 업그레이드 창 열기·닫기 연출은 B-167(#252)에서 이미 들어왔다. 연출은 바꾸지 않는다. 노드 해금 연출은 B-167이 남긴 "열기 연출 끝" 신호 뒤에 시작한다(A-1).
- C. 업그레이드 창 자물쇠 연출(B-167, #252)의 소리 훅 5개에 PM이 고른 소리를 붙인다. 지상 기지·광산 업그레이드 창 둘 다. 새 음원 4개는 묶음 zip으로 이미 `Audio/SFX/UI/`에 들어와 있다.
- 음원과 SfxId 칸(76~80)은 B-168에서 이미 들어왔다. 이번에 칸 81~84만 새로 붙인다. **음원 파일은 고치지 않는다.**
- 기준: MVP2-fix108(f71535a8, B-167 업그레이드 창 연출)과 B-168(효과음 연결)이 들어간 **가장 최신 MVP2 브랜치**에서 새 브랜치를 딴다. f71535a8이 조상이 아니거나 76~80 칸이 없으면 멈추고 보고한다. B-166·B-168 고침을 되돌리지 않는다.
- PR #248이 기준 브랜치에 들어와 있어야 한다(`TerminalWindowOpenMotion.Close`, `CloseDuration`, `IsClosingWindow`가 있는지 확인). 없으면 #248 커밋 62316959를 먼저 합친 뒤 시작하고, 충돌이 나면 멈추고 보고한다. #248 PR 자체는 고치지 않는다.
- 필요하면 다른 에이전트(Codex 등)에게 나눠 맡겨도 된다. 결과는 한 PR로 모은다.

## 절대 규칙
1. 기존 SfxId 번호·칸 값, 버스 음량은 바꾸지 않는다. 새 칸은 C의 81~84만, 80(WindowTvClose) 뒤 끝에 붙인다.
2. `ProgressionService`의 이벤트 순서(UpgradeChanged → PurchaseCompleted, ~196-201)는 바꾸지 않는다. 다른 구독자(파생 능력치, 저장)가 이 순서에 기댄다.
3. 구매·해금 판정, 비용, 세이브 형식은 바꾸지 않는다. 연출은 확정된 결과를 보여 주기만 한다.
4. 연출 소리는 **비주얼 시간표 단계를 넘는 그 프레임에** 낸다("이번 프레임에 그 시점을 넘었는지", 프레임이 튀어도 한 번, #158 구현 메모).
5. `SfxPlayer`에 멈춤 API를 추가하지 않는다(B-166 규칙). 끊고 다시 내야 하는 창 소리는 따로 AudioSource를 둔다.
6. 줄 번호는 d662c376 기준 근사(~)이고, B-167 파일은 f71535a8 기준이다. 못 찾으면 "못 찾음"으로 보고한다.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/` 기준)
- `work_process/MVP2/sound/B-168-sfx-d2-result.md`, `sfx-D2-build/README.md`(해금 소리 시점), #241, PR #248과 `work_process/issue-241-tv-window-motion-20261026.md`, #158 D-2 업그레이드 줄, #188, #223
- Scripts/App/Progression/ProgressionService.cs(~185-201), Scripts/App/UI/Progression/ProgressionPanelPresenter.cs(`OnPurchaseCompleted` ~270, `OnUpgradeChanged` ~289), ProgressionPanelView.cs(~131-139)
- Scripts/App/UI/Progression/UpgradeTreeView.cs(`UnlockFlowDuration`·`UnlockStagger` ~22-23, `Render` ~229-259, 노드 `Apply` ~253, `PlayPurchaseFeedback` ~699-748)
- Scripts/App/UI/Progression/UpgradeTreeNodeView.cs(`OnEnable`·`OnDisable` ~84-101, `Apply` ~122-167, `SetBlindVisible` ~221-238, `PlayUnlock` ~417, `UnlockRoutine` ~439-525)
- Editor/DataValidation/PromptB118UpgradeTreeBuilder.cs(~490-546: 자물쇠 4조각과 파편이 블라인드 자식)
- Scripts/App/UI/Progression/DeepZoneThumbnailStrip.cs(`TryPlayPending` ~121, `RevealRoutine` ~194-205, `SampleReveal` ~250-302, `SetLockPose` ~305-316, 자물쇠 조각 ~455-468), DeepZoneRevealTracker.cs, Scripts/App/Progression/DeepZoneThumbnailStyle.cs(~32-39)
- Scripts/App/UI/TerminalWindowOpenMotion.cs(#248 이후: 열기 0.18초, `CloseDuration` 0.16, `Close(root, completed)`, `IsClosingWindow`, `IsClosing`)
- Scripts/App/UI/HUD/HudPanelChromeController.cs(`ApplyBuildingMenuVisible` ~457-497, `ApplyInventoryPanelVisible` ~719-745, 늦게 닫는 예 `CompleteDiggerBotClose` ~583)
- Scripts/App/UI/Inventory/InventoryPanelView.cs(`SetVisible` ~181-208), App/UI/Building/BuildingMenuView.cs(`OnEnable` ~70-73, `SetVisible` ~228-246), App/UI/Tutorial/DemoObjectiveView.cs(`SetDetailsVisible` ~306-331)
- B-167(f71535a8): `work_process/MVP2/B-167-upgrade-window-motion-result.md`, Scripts/App/UI/SurfaceBase/UpgradeWindowMotion.cs(훅 이벤트 ~91-97, `BeginOpen` ~127-144 모션 감소면 훅 없이 `Settle`, `BeginClose` ~173-215, `Tick` 훅 ~283-314, `OpenCompleted` ~447), UpgradeWindowMotionTimeline.cs(시각 상수 ~40-57), SurfaceBaseView.cs(`RequestUpgradeOpen`·`RequestUpgradeClose` ~56-72, `EnsureUpgradeMotion` ~89), PanelToggleController.cs(광산 창 `SetUpgradeVisible` ~125-130)
- `work_process/MVP2/sound/sfx-D2-lock/README.md`(훅별 소리·시작 음량·미리듣기)

## A. 업그레이드 노드 해금 연출
**원인(코드로 확인):** 구매 성공 → `UpgradeChanged`가 먼저 와서 `Render` → 새로 풀린 노드의 `Apply`가 `SetBlindVisible(false)`로 블라인드(알파 0)와 자물쇠를 바로 숨긴다. 빌더에서 자물쇠·파편이 블라인드의 자식이라 둘 다 안 보인다. 그 뒤 `PurchaseCompleted` → `PlayUnlock`이 보이지 않는 자물쇠를 흔들고 파편을 날린다. 화면에는 청록 발광만 남는다. 창이 닫힌 동안 풀린 노드는 아무 연출도 없다(`OnDisable`이 정리).
1. **해금 대기 추적** (새 순수 클래스 `UpgradeUnlockRevealTracker`, `DeepZoneRevealTracker`와 같은 방식, 런타임 메모리만)
   - 노드마다 "풀린 모습으로 그렸는지"를 기억한다. 처음 그릴 때, 불러오기 직후는 연출 없이 그대로 그린다.
   - 그린 뒤 잠김 → 풀림이 된 노드는 "해금 대기"다. 대기 노드는 `Apply`가 정보를 갱신해도 **블라인드와 자물쇠를 그대로 보인 채** 둔다.
   - 창이 열려 있으면 바로, 닫혀 있었으면 다음에 업그레이드 창이 열릴 때 **B-167 열기 연출이 끝난 뒤** 재생한다. 열기 연출 중 창을 닫으면 대기를 유지한다. 여러 개면 0.12초 간격으로 이어서 재생한다. 커넥터 흐름(`PlayFlow`, ~0.22초)은 지금처럼 먼저.
   - `PlayPurchaseFeedback`의 `PlayUnlock` 직접 호출은 이 추적으로 옮겨 두 번 재생되지 않게 한다.
2. **연출 시간표** (새 순수 클래스, 1.0초. 심층 썸네일 `SampleReveal`과 같은 값)
   - 0~0.2 흔들림(각도 `sin(p·8π)·12°·(0.5+0.5p)`) → 0.2~0.3 딸깍(크기 1+0.1 펄스, 고리 `ShackleTop`·`ShackleRight`가 6px 들림) → 0.3~0.65 떨어짐(아래로, -25° 기울며, 뒤쪽에서 흐려짐) → 0.65~1.0 블라인드가 걷히고 아이콘·이름이 드러나며 청록 발광.
   - 노드 자물쇠는 썸네일 것의 1/1.3 크기이므로 떨어지는 거리는 비율로 줄인다. 파편은 쓰지 않는다(필드·오브젝트는 지우지 말고 꺼 둔다).
   - 가능하면 자물쇠 자세 계산을 공용 순수 함수로 빼서 `DeepZoneThumbnailStrip`과 같이 쓴다. 썸네일의 그림 결과는 지금과 같아야 한다.
   - 창을 닫거나 노드가 꺼지면 끝 상태(풀림)로 맞춘다. 대기 중 아직 시작 안 한 노드는 다음 열 때 재생한다.
3. **소리** (같은 프레임 단계 넘김): 0.0 UnlockRattle, 0.2 UnlockClick, 0.65 UnlockGlow
   - UnlockRattle은 칸 값으로 0.3초에서 끊긴다(B-168).
   - 여러 노드가 한꺼번에 풀리면 같은 소리는 0.1초 안에 한 번만, 소리를 내는 노드는 처음 3개까지(나머지는 연출만). 레벨업 소리(B-168)와 겹치면 레벨업이 먼저 난다.
4. **심층 구역 썸네일**(`RevealRoutine`)에도 같은 세 소리를 u=0, 0.2, 0.65에서 낸다. 한 번에 여러 칸이 풀려도 한 세트만 낸다. `FinishReveal`로 바로 끝 상태가 될 때는 무음이다.
   - 심층 구역 해금 **팝업**(`DeepZoneUnlockPopupMotion`)의 소리는 이번에 넣지 않는다.

## B. 세 창 TV 소리
**지금(#248 이후):** 인벤토리·건설창·퀘스트 상세창은 가운데 빛 점 → 가로선 → 펼침으로 열리고, 닫을 때는 거꾸로 접힌 뒤 숨는다(열기 0.18초, 닫기 최대 0.16초). 소리는 없다.
- 새 `App/Audio/WindowTvSfx.cs`: UI 버스 AudioSource 하나, 칸 음량 × 버스 × 효과음 설정, 음소거 반영.
- 열기 시작(점이 보이는 프레임)에 WindowTvOpen, 닫기 시작에 WindowTvClose. `TerminalWindowOpenMotion`의 열기·닫기 시작 한 곳에서 내서 창마다 따로 걸지 않는다. 이 컴포넌트를 쓰는 다른 창이 있으면 보고서에 적고, 그 창은 PM 확인 전까지 소리를 끈다.
- 같은 AudioSource라 새 소리가 이전 소리를 **끊고** 처음부터 낸다(연달아 열면 끊고 다시 재생). 모션 감소일 때도 소리는 낸다.
- 창 내용만 바뀔 때(퀘스트 페이지 넘김, 건설 항목 선택, 인벤토리 갱신)는 소리를 내지 않는다.
- 세 창의 열기·닫기 버튼에 클릭음이 붙어 있으면 None으로 바꿔 겹치지 않게 한다.
- 업그레이드 창은 TV 연출이 아니라 C의 자물쇠 소리를 쓴다. 다른 팝업(CCTV, 보관함, 판매 등)의 소리와 연출은 건드리지 않는다.

## C. 업그레이드 창 자물쇠 소리 (#252)
**지금(fix108):** `UpgradeWindowMotion`이 훅을 내지만 듣는 곳이 없어 열고 닫을 때 아무 소리도 없다(업그레이드 버튼 클릭음도 없음).
1. 새 칸(UI 버스, `SfxLibraryDefaults.cs`와 `Resources/SfxLibrary.asset` 두 곳): UpgradeLockCrack(81) `upgrade_lock_crack` 0.25, UpgradeLockShatter(82) `upgrade_lock_shatter` 0.55, UpgradeWindowSuckIn(83) `upgrade_window_suck_in` 0.35, UpgradeLockRegather(84) `upgrade_lock_regather` 0.5. 길이는 파일 그대로(자르지 않음).
2. 연결(새 `UpgradeWindowMotionSfx`, 모션 하나에 한 번만 구독. `SurfaceBaseView.EnsureUpgradeMotion`과 `PanelToggleController`가 모션을 만드는 두 곳에서 붙여 지상·광산이 같게):
   - ShakeStarted(처음 열기 0.15초) → UnlockRattle(76)
   - CrackStarted(처음 0.45초 / 다시 0.08초) → UnlockClick(77). 처음 열기일 때만 UpgradeLockCrack(81)을 같은 프레임에 겹친다(다시 열기는 금 가는 시간이 0.12초뿐이라 뺀다).
   - Shattered(처음 0.75초 / 다시 0.2초) → UpgradeLockShatter(82). 창이 펼쳐지는 데 따로 소리를 넣지 않는다(WindowTvOpen 안 씀).
   - CloseStarted → UpgradeWindowSuckIn(83). FragmentsRegathered(닫기 0.2초, 조각이 흩어진 뒤 닫을 때만) → UpgradeLockRegather(84)
3. 닫다가 다시 열기(되감기)·열다가 닫기에서 이미 지난 훅은 다시 나지 않으므로 소리도 다시 안 난다. 연타해도 같은 소리는 0.1초 안에 한 번만.
4. 모션 감소: 자물쇠가 안 보이므로 자물쇠 소리 5개는 내지 않는다. 대신 창이 바로 열리고 닫힐 때 다른 창과 같은 WindowTvOpen(79)·WindowTvClose(80)을 한 번씩 낸다(`OpenCompleted`·`CloseCompleted` 중 모션 감소 경로에서만, 소리 없는 창이 되지 않게).
5. `SetUpgradeVisible`·`CompleteImmediately`(설정 창 강제 숨김, Bind, 씬 이동)는 무음.
6. 시점·음량은 미리듣기(`sfx-D2-lock/preview_*.mp3`)와 같게 시작한다. 조각 모임 "착"이 늦게 들리면 보고서에 적는다(바꾸지 말고 PM 확인).

## 손대지 말 것
- 업그레이드 창 열기·닫기 연출·시각 값과 훅 위치(B-167), #248 연출 값, B-168 소리 연결, 기존 칸 값, `ProgressionService` 이벤트 순서, 구매 규칙, 노드 호버 연출, 레벨업·흔들림 연출
- 심층 구역 해금 팝업 연출·소리, 드론 분석 창(Tab) 연출, 충전기·보건소 대기 창, 다른 팝업 연출, 세이브 형식

## 테스트
- EditMode 전체에서 기존 실패(B-168 보고서 기준) 말고 새 실패가 없어야 한다.
- 새 EditMode 테스트 `PromptB169UnlockAndTvTests`
  - 해금 추적: 첫 그리기·불러오기는 대기 0, 잠김→풀림이면 대기 1, 창이 닫힌 동안 풀리면 다음 열 때 1번 재생, 재생 뒤 다시 열면 0
  - 노드: 대기 중 `Apply(풀림)` 뒤에도 블라인드 알파 1·자물쇠 활성, 연출 1.0초 뒤 블라인드 0·자물쇠 꺼짐
  - 소리: 0.0/0.2/0.65에 Rattle/Click/Glow 각 1번, 0.5초 한 프레임에 건너뛰어도 각 1번, 노드 5개면 소리 내는 노드 3개
  - 심층 썸네일: 공용 자세 함수로 바꾼 뒤 `SampleReveal` 결과가 이전과 같음, 세 소리 한 세트
  - 창 소리: 열기 두 번 연달아면 두 번째가 첫 번째를 끊음, 모션 감소면 연출 없이 소리만
  - 업그레이드 창(지상·광산 각각 `Tick`): 처음 열기 Rattle→Click+Crack→Shatter 각 1번, 다시 열기 Click→Shatter(Rattle·Crack 없음), 닫기 SuckIn→Regather 각 1번, 0.6초를 한 프레임에 건너뛰어도 각 1번, 모션 감소면 자물쇠 소리 0·TV 켜짐/꺼짐 각 1번, `SetUpgradeVisible`은 무음, 칸 81~84 값과 파일 이름
- PlayMode는 이번에 추가한 테스트만 돌린다.

## 수동 확인 체크리스트 (보고서에 그대로 넣기)
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

## #158 대응 (완료 표시용)
| #158 항목 | 이 프롬프트 |
|---|---|
| D-2 업그레이드 창: 다음 노드 해금(흔들림→딸깍→떨어짐→빛, 여러 개 이어서) | A |
| E 창 등장 공통: TV 켜짐·꺼짐 공통음(건설창·인벤토리·퀘스트 상세), 연달아 열면 끊고 다시 | B |
| E 업그레이드 창 심층 구역 썸네일 해금 소리 | A-4 |
| D-2 업그레이드 창 열고 닫을 때(#252): 흔들림·금·부서짐·빨려 들어감·조각 모임 | C |
- #158 본문과 #241·#252는 이 작업에서 고치거나 닫지 않는다(PM이 고침).

## 완료 후
- PR 제목 예: `feat: 업그레이드 노드 해금 연출과 창 TV·자물쇠 소리 (프롬프트 B-169)`. 커밋 메시지는 한국어 conventional 형식으로 쓰고, 브랜치는 지우지 않는다.
- 결과 보고서 `work_process/MVP2/sound/B-169-unlock-tv-result.md`에 적을 것
  - 기준 커밋, 브랜치, PR 링크, 테스트 결과
  - 자물쇠가 안 보이던 원인을 실제로 확인한 방법(로그·스크린샷)과 고친 위치(파일·줄)
  - 창 소리·업그레이드 창 자물쇠 소리를 건 위치(지상·광산), 직접 보고 듣지 못한 항목은 "미검증"으로 구분
  - 위 수동 확인 체크리스트
