# B-167 업그레이드 창 열기·닫기 연출 결과 (#252)

## 기준
- 기준 커밋: main 3cb418a3 (MVP2-fix107 8305ee2c + PR #248 포함, 별도 병합 필요 없었음)
- 브랜치: MVP2-fix108
- PR: 미생성(커밋·PR은 사용자 승인 후 진행)

## 바뀐 파일
- 새 파일: UpgradeWindowMotionTimeline.cs(순수 시간표), UpgradeWindowMotion.cs(표시 컴포넌트, upgradeRoot에 런타임 부착), UpgradeWindowLockArt.cs(point 필터 절차 생성 자물쇠·금·조각), EditMode 테스트 PromptB167UpgradeWindowMotionTests.cs, PlayMode 테스트 PromptB167UpgradeWindowMotionPlayModeTests.cs
- 수정: SurfaceBaseView.cs(열기·닫기 요청 함수, 닫는 중 IsUpgradeVisible=false), SurfaceBaseBinder.cs(버튼·ESC·X키·X버튼이 요청 함수 사용), ProgressionPanelView.cs(DeepZoneRevealBlocked에 "업그레이드 창 여는 중" OR 추가), PanelToggleController.cs(광산 업그레이드 창도 같은 연출)
- 바꾼 프리팹·씬: 없음 (UpgradeWindowMotion과 카드 CanvasGroup은 런타임에 AddComponent, Mine_Demo_Integration.unity도 미변경)
- 새 이미지 파일: 없음 (자물쇠·금·조각 모두 코드 생성)

## 열기·닫기 경로
- `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/SurfaceBaseView.cs:56` `public void RequestUpgradeOpen()`
- `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/SurfaceBaseView.cs:66` `public void RequestUpgradeClose()`
- `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/SurfaceBaseView.cs:74` `public void SetUpgradeVisible(bool visible)`
- `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/SurfaceBaseBinder.cs:258` `private void OnUpgradeClicked()`
- `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/SurfaceBaseBinder.cs:269` `private void OnUpgradeCloseClicked()`
- ESC: `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/SurfaceBaseBinder.cs:198` `else if (view.IsUpgradeVisible) OnUpgradeCloseClicked();`
- X키: `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/SurfaceBaseBinder.cs:223` `if (view.IsTopUpgradeWindow(topWindow))`
- X키(위에 열린 창이 없을 때): `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/SurfaceBaseBinder.cs:233` `if (view.IsUpgradeVisible) { OnUpgradeCloseClicked(); return; }`
- 창 X 버튼: `CloseUpgradeButton` → `SurfaceBaseView.UpgradeCloseClicked` → `SurfaceBaseBinder.OnUpgradeCloseClicked`(:269)
- U키(#249)는 `SurfaceBaseBinder.OnUpgradeClicked`(`sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/SurfaceBaseBinder.cs:258`) 하나만 부르면 된다.
- `SetUpgradeVisible(bool)`(`SurfaceBaseView.cs:74`)은 즉시 함수로 유지(설정 창 강제 숨김·Bind·OnDisable·기존 테스트). 진행 중 연출은 `CompleteImmediately`(`sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/UpgradeWindowMotion.cs:217`)로 끝 상태 정리.
- 닫는 중 다시 열기: 닫던 길(Close 시간표)을 되감아 지금 모양에서 이어서 연 뒤 원래 열기 시간표로 이어짐. 처음 열기의 흔들림 전에 닫혔다면 다시 열기 규칙(흔들림 없음)으로 이어짐.

## 광산 업그레이드 창 (추가 요청)
- 광산 창(`HUDCanvas/PanelLayout/UpgradePanel`)은 U키·사이드 메뉴 버튼·창 X 버튼·X키(UndergroundMenuController) 모두 `PanelToggleController.SetVisible(Upgrade, …)`를 지나므로 그 한 곳에서 같은 `UpgradeWindowMotion`을 탄다(`PanelToggleController.SetUpgradeVisible`).
- 광산 창은 카드 자체가 토글 루트라 연출 컴포넌트가 카드에 붙는다. 이때 카드 배경 Image를 딤으로 쓰지 않고(광산 창은 원래 딤이 없음), 자물쇠는 카드 scale에 같이 줄지 않도록 카드 옆(같은 부모)에 그린다.
- 닫기 요청 즉시 `IsVisible(Upgrade)`는 false, 창은 연출이 끝난 뒤 꺼진다. 닫는 중인 창은 X키의 맨 위 창으로 잡히지 않게 정렬 목록에서 먼저 뺀다(그리는 순서는 유지).
- 시작 숨김(Awake)은 연출 없이 즉시 처리.
- "처음 열기"는 플레이 세션당 한 번(지상·광산 공유). 지상에서 먼저 열었다면 광산에서는 다시 열기 규칙.
- 광산 PlayMode 캡처: work_process/MVP2/B-167-upgrade-window-motion-mine.png

## 시간표와 훅

| 훅 | 위치 | 시각 |
|---|---|---|
| ShakeStarted | `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/UpgradeWindowMotion.cs:89` | 처음 열기 0.15초(다시 열기에는 없음) |
| CrackStarted | `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/UpgradeWindowMotion.cs:90` | 처음 0.45초 / 다시 0.08초 |
| Shattered | `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/UpgradeWindowMotion.cs:91` | 처음 0.75초 / 다시 0.2초 |
| CloseStarted | `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/UpgradeWindowMotion.cs:92` | 닫기 요청 순간(0초) |
| FragmentsRegathered | `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/UpgradeWindowMotion.cs:93` | 닫기 0.2초(조각이 흩어진 상태에서 닫을 때만) |
| OpenCompleted | `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/UpgradeWindowMotion.cs:96` | 처음 1.2초 / 다시 0.5초, 카드 조작이 풀린 그 프레임(`Settle`: `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/UpgradeWindowMotion.cs:437`) |
| CloseCompleted | `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/UpgradeWindowMotion.cs:97` | 닫기 0.3초, 루트 비활성화 직후 |

- 훅은 "이번 프레임에 넘었는지(SfxPlayback.Crossed)"로 판정하므로 프레임이 튀어도 한 번만 난다.
- 모션 감소면 소리 훅 없이 OpenCompleted·CloseCompleted만 한 번씩 난다.
- B-169 노드 해금 연출 구독 위치: `SurfaceBaseView.UpgradeMotion.OpenCompleted` (`sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/UpgradeWindowMotion.cs:447` B-169 주석).
- 여는 중 판정은 `UpgradeWindowMotion.IsOpeningFor(transform)` (`sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/UpgradeWindowMotion.cs:114`).
- 심층 썸네일 해금 미루기: `sub-terra/Assets/_Project/Scripts/App/UI/Progression/ProgressionPanelView.cs:85` `private bool IsDeepZoneRevealBlocked()`, 적용 줄 `sub-terra/Assets/_Project/Scripts/App/UI/Progression/ProgressionPanelView.cs:576` `treeView.DeepZoneRevealBlocked = IsDeepZoneRevealBlocked;`.
- 시간표 상수: `sub-terra/Assets/_Project/Scripts/App/UI/SurfaceBase/UpgradeWindowMotionTimeline.cs:40-57` (FullOpenDuration 1.2f, QuickOpenDuration 0.5f, CloseDuration 0.3f, ShakeStartTime 0.15f, FullCrackStartTime 0.45f, FullShatterTime 0.75f, QuickCrackStartTime 0.08f, QuickShatterTime 0.2f, RegatherTime 0.2f).

## 테스트 결과
- 새 EditMode PromptB167UpgradeWindowMotionTests 17개 통과. 이웃 묶음(PromptB117SurfaceBaseTests, PromptB42UiLayerTests, PromptB1181UpgradeWindowTests, ProgressionServiceTests 포함) 74개 모두 통과.
- EditMode 전체: B-167 적용 2087 통과 / 21 실패, 적용 전(같은 작업트리에서 B-167 변경만 stash) 2071 통과 / 20 실패. 실패 20개는 동일(기존 실패). 차이 1개 PromptB150MainMenuTests.DeleteButton_Hover_FadesInWhiteTrashIcon은 같은 코드로 다시 돌리면 통과·실패가 번갈아 나오는 불안정 검사(메인 메뉴, 이번 변경과 무관).
- 기존 실패 중 업그레이드 화면 관련: PromptBSellPanelLayoutTests.SurfaceBasePrefab_PreservesSellModalWithPrompt117Actions (프리팹 미변경, 적용 전에도 실패).
- 광산 추가 후 EditMode: PromptB167(광산 검사 포함 18개)·PanelVisibilityStateTests·PromptB89UndergroundMenuTests·PromptB117·PromptB42·PromptB1181 75개 모두 통과.
- PlayMode: 이번에 추가한 PromptB167UpgradeWindowMotionPlayModeTests 2개(지상·광산)와 광산 업그레이드 토글을 쓰는 이웃 PromptB105SideMenuPlayModeTests 1개 실행, 3개 모두 통과 (슬롯 0 격리 런타임, 플레이어 세이브 미사용, TearDown에서 빈 씬으로 정리).
- ProgressionPlayModeTests는 이번에 실행하지 않음(미검증).

## 화면 확인
- 실제 지상 기지 Scene PlayMode 캡처: work_process/MVP2/B-167-upgrade-window-motion-play.png (12컷: 나타남→흔들림→금→번짐→완성 / 접힘→조각 모임→자물쇠 흐려짐 / 다시 열기). 캡처 저장이 프레임을 늦춰 컷 사이 시각은 실제 연출 시각과 다르다.
- 딤 확인: 화면 왼쪽 평균 밝기 열기 전 49.6 → 자물쇠 단계 26.3 → 완성 19.2.
- 관찰: 자물쇠가 카드 가운데(=지하 탐사 시작 버튼 위치)에 나타나 고리 빈 칸 사이로 뒤 버튼 글자가 비친다(딤으로 어둡게 깔림).

## 직접 보지 못한 항목 (미검증)
- 실제 마우스·키보드로 ESC·X키를 눌러 본 플레이 확인(EditMode에서 가상 키보드로 확인함), 창 드래그 후 열기 위치, 연타 시 체감, 모션 감소 설정 토글의 실제 플레이, 광산으로 내려갈 때 정리, 심층 썸네일 해금 대기 중 실제 순서, 일시정지 중 체감(PlayMode에서 timeScale 0 재열기는 확인함).

## 수동 확인 체크리스트
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

## #252 확인 대응
- 처음 열기=1, 다시 열기=2, 닫기=4, 자물쇠 구분=3, 연타=6, 모션 감소=10. "창이 다 펼쳐진 뒤 노드 해금 연출" 은 OpenCompleted 훅만 넣었고 실제 연출은 B-169에서 확인한다. #252는 닫거나 고치지 않음.
