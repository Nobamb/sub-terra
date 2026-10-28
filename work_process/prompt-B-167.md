# B-167 업그레이드 창 열기·닫기 연출 (#252)

## 목표
- 지금 지상 기지 업그레이드 창은 연출 없이 바로 켜지고 꺼진다. 게임 가이드(홀로그램 책)·광산 초기화(시계)처럼 이 창만의 모티브 **"큰 자물쇠가 금이 가며 부서지고, 그 자리에서 창이 펼쳐짐"**으로 열리고 닫히게 한다. 창 안 노드도 자물쇠로 풀리므로 같은 자물쇠 계열로 잇는다.
- 처음 열기 약 1.2초(나타남 → 흔들림 → 잠금 해제·금 감 → 금이 번지며 부서짐 → 창 펼침), 다시 열기 약 0.5초(흔들림 없이 바로 금 → 부서짐 → 펼침), 닫기 약 0.3초(정확한 역순).
- 노드 해금(작은 자물쇠: 흔들림 → 딸깍 → **아래로 떨어짐**)과 똑같아 보이면 안 된다. 창 열기는 **크고, 화면 가운데, 금이 가며 부서짐**으로 구분한다.
- **효과음은 이번에 넣지 않는다.** 소리를 달 자리(훅)만 만든다.
- 노드 자물쇠 해금 연출은 뒤 프롬프트(B-169)에서 넣는다. 이번에는 그 연출이 **창이 다 열린 뒤** 시작할 수 있게 "열기 끝남" 이벤트만 만든다.
- 기준: MVP2-fix107(8305ee2c) 이후 **가장 최신 MVP2 브랜치**에서 새 브랜치를 딴다. PR #248(창 TV 연출)이 없으면 main(3cb418a3, fix107 + #248 병합)을 먼저 합치고, 충돌하면 멈추고 보고한다. B-166과 #248 내용을 되돌리지 않는다.
- 필요하면 다른 에이전트(Codex 등)에게 나눠 맡겨도 된다. 결과는 한 PR로 모은다.

## 절대 규칙
1. 구매·해금 판정, 비용, `ProgressionService` 이벤트 순서, 세이브 형식은 바꾸지 않는다. 연출은 보여 주기만 한다.
2. 창 크기는 `sizeDelta`가 아니라 **scale·CanvasGroup 알파**로만 움직인다. 트리 맞춤(`UpgradeTreeView.FitContent`)이 연출 중 크기를 읽어 깨지지 않게 한다. 끝나면 원래 scale·알파·interactable로 정확히 되돌린다.
3. 시간은 `Time.unscaledDeltaTime`(일시정지 중에도 재생). 한 프레임 최대 진행은 가이드처럼 제한한다.
4. 자물쇠·금·조각은 **코드로 만든다**(가이드·광산 초기화 팝업과 같은 방식, 새 이미지 파일 없음). 프리팹·씬은 꼭 필요할 때만, Editor API로, `SurfaceBasePanel.prefab` 하나만 고친다(`init/rules/ui-ugui.md` 2-5). 범위 밖 dirty 에셋은 커밋하지 않는다.
5. 이모티콘 같은 귀여운 자물쇠 금지. 청록빛 금속 픽셀 자물쇠(point 필터), 차분한 SF 분위기.
6. 노드·심층 썸네일 자물쇠 코드(`UpgradeTreeNodeView`, `DeepZoneThumbnailStrip`)는 고치지 않는다. B-169가 그쪽을 바꾼다. 모양·값은 **읽어서 맞추기만** 한다.
7. 줄 번호는 main 3cb418a3 기준 근사(~)다. 못 찾으면 "못 찾음"으로 보고한다.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/` 기준)
- #252 본문, #249(U키, 아직 없음), #241·PR #248, `init/rules/lessons.md`(10-26 "닫기 요청과 시각적 종료 구분"), `init/rules/ui-ugui.md`, `testing-qa.md`
- Scripts/App/UI/SurfaceBase/SurfaceBaseView.cs: `upgradeRoot`(=`UpgradeModal`, 딤 Canvas) ~34, `IsUpgradeVisible` ~36, `SetUpgradeVisible` ~48-73(카드=`PopupWindowDrag` 달린 `ProgressionPanel`, 정렬), 설정 열 때 강제 숨김 ~528, 닫는 중 판정 예 `IsMineResetConfirmVisible` ~100
- Scripts/App/UI/SurfaceBase/SurfaceBaseBinder.cs: Bind 시 숨김 ~116, `OnDisable` 숨김 ~142, ESC ~194-199, X키 ~223-224·~233, `OnUpgradeClicked` ~258-265(열고 `Presenter.Refresh`), `OnUpgradeCloseClicked` ~267-270, 광산 초기화가 `IsUpgradeVisible`로 막는 곳 ~367·~388
- Scripts/App/UI/Guide/GameGuideTimeline.cs(순수 시간표: Full 1.22 / Quick 0.60 / Close 0.40, `Close(u, from)`이 시작값보다 안 커짐, 닫다가 다시 열 때 `OpenTimeForExpand`)
- Scripts/App/UI/Guide/GameGuidePopupView.cs(상태 Hidden/Opening/Open/Closing ~22, `Show` ~162-201, `BeginClose` ~206-232, `Tick` ~254-296, `Settle` ~337-346), GameGuideState.cs(`IntroPlayed` ~30, 새 플레이마다 초기화 ~37-42), GameGuideArt.cs(절차 생성 기호 `Shape` ~183, 캐시 방식)
- 자물쇠 모양·연출 값: DeepZoneThumbnailStrip.cs(자물쇠 4조각 `LockBody` 26×18·`ShackleLeft/Right` 5×16·`ShackleTop` 21×5, 틀 30×34 ~455-468, 색 `LockColor` ~30, `SampleReveal` 흔들림 `sin(p·8π)·12°·(0.5+0.5p)` ~266-290, `SetLockPose` ~305), Editor/DataValidation/PromptB118UpgradeTreeBuilder.cs(노드 자물쇠 같은 4조각·파편 ~508-530), UpgradeTreeTween.cs(`Cyan`·`CyanDim` ~14-15)
- Scripts/App/UI/HUD/MineResetClockIntroTimeline.cs(튀어나옴 0→1.2→1, `EaseOutCubic`·`EaseInOutSine`)
- Scripts/App/UI/TerminalWindowOpenMotion.cs(모션 감소 `AccessibilityPreferences.ReduceMotion` ~46-55·~114-121, 닫는 동안 `group.interactable=false` ~107, 청록 `LightColor` ~13)
- Scripts/App/UI/SurfaceBase/MineResetPopupMotion.cs(`IsClosing` ~63), Scripts/Shared/AccessibilityPreferences.cs
- Scripts/App/UI/Progression/ProgressionPanelView.cs(`OnPurchaseCompleted` ~135-140, 심층 썸네일 연출 막기 `DeepZoneRevealBlocked` ~566-571), UpgradeTreeView.cs(`OnEnable` ~80-98 확대·위치 초기화, `DeepZoneRevealBlocked` ~346), DeepZoneThumbnailStrip.cs(`TryPlayPending` ~121, `IsBlocked` ~146)

## 구현
### 1. 열기·닫기 한 경로
- `SurfaceBaseView`에 `RequestUpgradeOpen()` / `RequestUpgradeClose()`를 만들고, 업그레이드 버튼·ESC·X키·창 X 버튼이 모두 이 둘만 타게 한다. U키(#249)가 나중에 붙을 때도 `SurfaceBaseBinder`의 같은 열기 함수 하나만 부르면 되게 한다(보고서에 위치 적기).
- 기존 `SetUpgradeVisible(bool)`은 **즉시 켜고 끄는 함수로 남긴다**. 설정 창이 열 때 강제 숨김, Bind·OnDisable, 기존 테스트는 계속 즉시 처리(연출 없음, 진행 중 연출은 끝 상태로 정리).
- 닫는 중에는 `IsUpgradeVisible`이 false(광산 초기화 확인창과 같은 규칙). 닫는 중 다시 열면 지금 모양에서 이어서 열고, 여는 중 닫으면 지금 값에서 접는다(값이 튀지 않음). 루트 비활성화는 닫기 연출이 끝난 뒤.
- 구매 뒤 `Refresh`·노드 갱신·확대/이동으로는 연출이 다시 나오지 않는다. 연출은 열기·닫기 요청에서만 시작한다.

### 2. 순수 시간표 `UpgradeWindowMotionTimeline` (새 파일, EditMode 검사 가능)
- 상수: `FullOpenDuration` 1.2, `QuickOpenDuration` 0.5, `CloseDuration` 0.3. `Open(t, full)`·`Close(u, from)`이 한 프레임 값(구조체: 자물쇠 크기·각도·알파, 금 진행, 조각 퍼짐·알파, 창 펼침, 딤)을 돌려준다.
- 처음 열기(대략): 0~0.15 자물쇠가 가운데에 나타남(0→1.1→1) → 0.15~0.45 흔들림(심층 썸네일과 같은 식, 진폭은 크기에 맞게) → 0.45~0.55 잠금 해제(고리가 들림)와 함께 금이 가기 시작 → 0.55~0.75 금이 번짐 → 0.75~0.95 부서짐(조각이 바깥으로 흩어지며 회전·흐려짐) → 0.80~1.10 부서진 자리에서 창이 펼쳐짐(가운데 점에서 scale 0→1, 알파) → 1.10~1.2 창 내용·딤 완성.
- 다시 열기: 흔들림 없음. 0~0.08 나타남 → 0.08~0.2 바로 금 → 0.2~0.32 부서짐 → 0.22~0.45 펼침 → 0.5 끝.
- 닫기(정확한 역순): 0~0.12 창이 가운데 점으로 접힘 → 0.10~0.20 조각이 다시 모여 자물쇠가 됨 → 0.18~0.25 금이 사라짐 → 0.22~0.3 자물쇠가 흐려지며 사라짐. 흔들림 없음. 각 값은 시작값(from)보다 커지지 않는다(여는 도중 닫아도 튀지 않음).
- 훅 시각을 상수로 노출: `ShakeStartTime`(처음 열기만), `CrackStartTime`·`ShatterTime`(처음/다시 각각), 닫기의 `RegatherTime`.

### 3. 표시 컴포넌트 `UpgradeWindowMotion` (새 파일, `upgradeRoot`에 붙임)
- 상태 Hidden/Opening/Open/Closing, `Tick(dt)`를 공개해 테스트와 `Update`가 같은 경로를 탄다(가이드 방식).
- 자물쇠·금·조각은 처음 쓸 때 코드로 한 번 만들어 재사용한다. 딤 위·카드 앞(`PopupWindowSorting`), `raycastTarget=false`.
  - 자물쇠: 노드·썸네일과 **같은 4조각 비율**(몸통·고리 좌·우·위)을 약 4.5배(대략 135×153)로. 색은 `LockColor` 금속빛에 `UpgradeTreeTween.Cyan` 가장자리 빛을 더해 청록 금속 느낌. 픽셀감이 나도록 조각 이미지는 point 필터 작은 텍스처(`GameGuideArt.Shape` 방식, bilinear 대신 point).
  - 금: 몸통 가운데서 갈라지는 지그재그 픽셀 선 3~5개(고정 시드로 매번 같은 모양), 진행값만큼 길이가 자란다.
  - 조각: 6~10개, 바깥 방향·회전 값을 고정 시드로 정해 두고 `Close`에서 같은 길을 거꾸로 돌아온다.
- 창 펼침은 자물쇠가 부서진 화면 가운데를 기준으로 한다(카드 위치가 드래그로 바뀌어 있으면 카드 가운데를 기준으로 하고, 자물쇠도 그 자리에 둔다).
- "처음"은 가이드와 같은 규칙: 플레이 세션에서 처음 여는 한 번만 전체(`GameGuideState.IntroPlayed`와 같은 방식의 별도 정적 값, 새 플레이마다 초기화). 닫다가 다시 열면 다시 열기 규칙.
- 연출 중(여는 중·닫는 중)에는 카드의 CanvasGroup `interactable=false`로 노드·구매 버튼·창 드래그를 막는다. 딤은 raycast 유지. 끝나면 원래 값으로.
- 모션 감소(`AccessibilityPreferences.ReduceMotion`)면 자물쇠 없이 바로 열리고 닫힌다. 연출 중에 켜지면 바로 끝 상태로. 이때도 `OpenCompleted`·`CloseCompleted`는 한 번씩 나간다.
- `OnDisable`(씬 이동 등)에서는 끝 상태로 정리하고 효과 오브젝트를 끈다.

### 4. 훅과 "열기 끝남" 이벤트 (소리는 다음에)
- 소리 훅: `event Action ShakeStarted`, `CrackStarted`, `Shattered`, `CloseStarted`, `FragmentsRegathered`. 상태 이벤트: `OpenCompleted`, `CloseCompleted`.
- 시각을 "이번 프레임에 넘었는지"로 판정해 프레임이 튀어도 한 번만 낸다. 다시 열기에는 `ShakeStarted`가 없다. 닫다가 다시 열면 이미 지난 시점의 훅은 다시 내지 않는다.
- `OpenCompleted`는 창이 완전히 펼쳐지고 조작이 풀린 그 프레임에 낸다. 외부에서 `IsOpening` / `IsSettled`도 읽을 수 있게 한다.
- 심층 구역 썸네일 해금 연출(`DeepZoneRevealBlocked`)이 여는 중에는 미뤄지도록, 기존 조건(해금 창 보임)에 "업그레이드 창 여는 중"을 OR로 더한다.
- B-169 노드 해금 연출이 붙을 자리(`OpenCompleted` 구독 위치)를 주석과 보고서에 적는다.

## 손대지 말 것
- 구매 규칙·비용·이벤트 순서, 노드 해금·구매 피드백 연출(`PlayPurchaseFeedback`), 트리 확대·이동, 심층 구역 해금 팝업
- #248 창 연출 값(인벤토리·건설·퀘스트 상세), `TerminalWindowOpenMotion`, 다른 지상 기지 창(판매·설정·광산 초기화), 게임 가이드·시계 연출
- 노드·심층 썸네일 자물쇠 연출 코드(B-169 몫), 효과음 칸·음원·버스 음량(이번엔 소리 없음), 세이브 형식, 폰트 에셋

## 테스트
- EditMode 전체에서 기존 실패 말고 새 실패가 없어야 한다. `PromptB117SurfaceBaseTests`, `PromptB42UiLayerTests`, `ProgressionPlayModeTests`의 `SetUpgradeVisible` 즉시 동작이 그대로 통과해야 한다.
- 새 EditMode 테스트 `PromptB167UpgradeWindowMotionTests`
  - 시간표: 처음 1.2·다시 0.5·닫기 0.3초 끝에서 창 scale 1/0·자물쇠 알파 0, 다시 열기와 닫기의 흔들림 각도 항상 0
  - 순서: 금 시작 < 부서짐 < 펼침 시작, 닫기는 접힘 끝 ≤ 조각 모임 < 금 사라짐 < 자물쇠 사라짐
  - 닫기 값이 시작값보다 커지지 않음, 여는 중 닫기·닫는 중 열기에서 값이 튀지 않음, 조각 위치가 열기·닫기에서 같은 길(고정 시드)
  - 구분: 창 자물쇠 크기가 노드 자물쇠(30×34)의 3배 이상이고, 아래로 떨어지는 이동(y 감소)이 없음
  - 컴포넌트(`Tick`): 처음 열기 → 다시 열기 규칙, 연출 중 카드 interactable false·끝나면 원래 값, 끝나면 scale·알파가 원래 값과 같음(연타 20번 뒤에도)
  - 이벤트: 처음 열기에서 Shake/Crack/Shattered/OpenCompleted 각 1번(이 순서), 0.6초를 한 프레임에 건너뛰어도 각 1번, 다시 열기에 Shake 없음, 닫기에서 CloseStarted/FragmentsRegathered/CloseCompleted 각 1번, 모션 감소면 바로 끝나고 OpenCompleted 1번
  - `Refresh`·구매 뒤 연출 상태가 Open 그대로, ESC·X키·X 버튼·업그레이드 버튼이 같은 요청 함수를 탐
  - 여는 중에는 `DeepZoneRevealBlocked`가 true, 열기 끝나면 false
- PlayMode는 이번에 추가한 테스트만 돌린다(씬을 열면 TearDown에서 정리, lessons 10-25).

## 수동 확인 체크리스트 (보고서에 그대로 넣기)
1. [ ] 게임을 켜고 지상 기지에서 업그레이드 창을 처음 열면, 화면 가운데에 큰 청록 금속 자물쇠가 나타나 흔들리고 → 잠금이 풀리며 금이 가고 → 금이 번지며 조각나 흩어지고 → 그 자리에서 창이 펼쳐진다.
2. [ ] 창을 닫고 다시 열면 흔들림 없이 자물쇠가 바로 금 가고 부서지며 짧게 열린다.
3. [ ] 창 열기 자물쇠는 노드 해금 자물쇠(작고, 딸깍 뒤 아래로 떨어짐)와 확실히 다르게 보인다(크고, 가운데서 부서짐). 귀여운 느낌이 아니다.
4. [ ] 닫으면 창이 가운데로 빨려 들어가듯 접히고 → 흩어졌던 조각이 다시 모여 자물쇠가 되고 → 금이 사라진 뒤 자물쇠가 흐려져 사라진다.
5. [ ] 업그레이드 버튼, ESC, X키, 창의 X 버튼 어느 것으로 열고 닫아도 같은 연출이 나온다.
6. [ ] 열고 닫기를 빠르게 반복해도 창이 이상한 크기·위치로 남지 않고, 트리 확대 비율도 정상이다.
7. [ ] 창이 열리거나 닫히는 동안 노드·구매 버튼을 눌러도 아무 일도 없다. 다 열린 뒤에는 정상으로 눌린다.
8. [ ] 업그레이드를 사서 창 내용이 바뀌어도 열기 연출이 다시 나오지 않는다.
9. [ ] 심층 구역 썸네일 해금이 기다리고 있으면, 창이 다 열린 뒤에 해금 연출이 나온다.
10. [ ] 설정에서 모션 감소를 켜면 창이 바로 열리고 닫힌다. 일시정지 중에도 연출이 재생된다.
11. [ ] 설정 창을 열거나 광산으로 내려가면 업그레이드 창이 남지 않고 깨끗이 닫힌다. Console에 경고·오류가 없다.

## #252 확인
- #252 "확인할 것"과의 대응: 처음 열기=1, 다시 열기=2, 닫기=4, 자물쇠 구분=3, 연타=6, 모션 감소=10. "창이 다 펼쳐진 뒤 노드 해금 연출이 이어서 나옴"은 이번에 `OpenCompleted` 훅만 넣고, 실제 연출은 B-169에서 확인한다.
- #252는 이 작업에서 닫거나 본문을 고치지 않는다(PM이 고침).

## 완료 후
- PR 제목 예: `feat: 업그레이드 창 자물쇠가 부서지며 열리는 연출 (프롬프트 B-167)`. 커밋 메시지는 한국어 conventional 형식으로 쓰고, 브랜치는 지우지 않는다.
- 결과 보고서 `work_process/MVP2/B-167-upgrade-window-motion-result.md`에 적을 것
  - 기준 커밋, 브랜치, PR 링크, 테스트 결과(기존 실패와 새 실패 구분)
  - 열기·닫기 경로를 묶은 위치(파일·줄), U키(#249)가 붙을 함수
  - 소리 훅 5개(`ShakeStarted`·`CrackStarted`·`Shattered`·`CloseStarted`·`FragmentsRegathered`)와 `OpenCompleted`의 위치·시각, B-169 노드 해금 연출이 구독할 곳
  - 바꾼 프리팹·씬(없으면 "없음"), 직접 보지 못한 항목은 "미검증"으로 구분, 화면 녹화나 스크린샷
  - 위 수동 확인 체크리스트
