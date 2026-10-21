# B-154 심층 구역 해금 창: 시작 브리핑 글리치 창의 작은 버전 (#119 마지막 항목)

## 목표
업그레이드 창 위에 뜨는 "심층 구역 잠금이 해제되었습니다" 창을, 게임 시작 때 글리치로 나타나는 **시작 브리핑 창(B-120)과 같은 느낌**으로 바꾼다.
- 프레임, 글리치 등장·퇴장, 폰트, 색, 버튼은 시작 브리핑 것을 **재사용**한다 (복사 금지).
- 크기는 지금 창 정도로 작게 한다 (시작 브리핑 1700×950보다 훨씬 작게).
- 첨부 `B-154-current.png`: 지금 창 (반투명 단색 520×220, 한 줄 문구 + 단색 '닫기').

## 기준 브랜치
- 최신은 **origin/MVP2-fix93**(B-152 + B-153, 10-20 17:50)이다. main에는 아직 합쳐지지 않았고 PR도 없다.
- 이 브랜치 위에서 작업한다. 더 새 브랜치가 있으면 그 위에서 하고 보고한다.

## 절대 규칙 (먼저 읽기)
1. 해금 조건·비용·저장은 바꾸지 않는다.
   - 대상: DeepZoneUnlockRule.Mvp (드릴 속도 Lv.2, 드론 스캔 Lv.2, 가스 저항 Lv.1), ZoneAccessResult, 세이브 형식
2. 창은 **해금 한 번에 한 번만** 뜬다. 지금처럼 ProgressionPanelBinder.OnDeepZoneAccessChanged에서 IsUnlocked && DidUnlockNow일 때만 뜬다.
   - 창을 다시 열거나 세이브를 불러와도 다시 뜨지 않는다.
3. 닫기
   - 닫기 버튼, X 키(UndergroundMenuController.HandleCloseTopPopup), ESC(DeepZoneUnlockPopupEscClose), 지상 기지(SurfaceBaseBinder)가 모두 **같은 닫기 경로**(TryHideDeepZoneUnlockPopup)를 탄다.
   - 닫기 요청 순간부터 IsDeepZoneUnlockPopupOpen == false이고, 두 번째 닫기는 무시한다 (TryHide → false).
   - 닫기 연출이 시작되면 창은 클릭을 받지 않는다 (blocksRaycasts = false). 그래서 뒤의 업그레이드 창이 바로 클릭된다.
   - 연출이 끝나면 root를 끄고 PopupWindowSorting.Remove와 UiKeyboardSubmitGuard.ClearSelection을 호출한다.
4. 글리치는 열 때와 닫을 때 재생한다. 빠르게 열고 닫거나, 닫는 중에 다시 열어도 잔상·노이즈 막대·반투명 프레임이 남지 않는다.
   - 닫는 중 다시 열기: 닫기를 끊고 처음부터 연다.
   - 비활성화될 때는 StartBriefingPopupMotion.OnDisable처럼 모두 정리한다.
5. **게임을 멈추지 않는다** (pauseOnOpen = false, UiPauseGate 사용 안 함). 업그레이드 창 입력과 정지 상태는 그대로 둔다.
6. 계속 깜빡이는 강한 글리치는 넣지 않는다. 표시 중에는 시작 브리핑처럼 약한 가장자리 간섭만 둔다.

## 먼저 읽을 코드 (sub-terra/Assets/_Project/ 기준)
- 지금 창: Scripts/App/UI/Progression/ProgressionPanelView.cs
  - ShowDeepZoneUnlockPopup (const 문구), Hide/TryHideDeepZoneUnlockPopup, IsDeepZoneUnlockPopupOpen
  - CreateDeepZoneUnlockPopupIfNeeded: 런타임에 "DeepZoneUnlockPopup_Runtime", 520×220, sortingOrder ModalPanel+10
  - EnsureDeepZoneUnlockCloseButton, CreateDeepZoneUnlockCloseButton
  - 씬·프리팹의 deepZoneUnlockPopup* 참조는 모두 비어 있다. 창은 항상 런타임에 만들어진다.
- 열기·닫기 호출
  - Scripts/App/UI/Progression/ProgressionPanelBinder.cs
  - DeepZoneUnlockPopupEscClose.cs
  - Scripts/App/Integration/UndergroundMenuController.cs
  - Scripts/App/UI/SurfaceBase/SurfaceBaseBinder.cs
- 해금 규칙: Scripts/App/Progression/DeepZoneUnlock.cs
- **재사용할 시작 브리핑**
  - Scripts/App/UI/Tutorial/StartBriefingPopupMotion.cs: 글리치 → 신호 집결·확산 → 프레임 개방 → 본문 페이드, 닫을 때 프레임 축소 + 글리치
  - **ConfigureQuestClear**는 이미 같은 연출을 퀘스트 클리어 창(900×450)에 런타임으로 붙인다. 프레임, 잔상 프레임, 가장자리 막대, 신호선, 점, 창 안 노이즈 막대를 만들고, pauseOnOpen = false, tearSlices 없음이다.
    - RequestQuestClose는 어느 단계에서든 닫기로 넘어간다.
  - Scripts/App/UI/Tutorial/StartBriefingTimeline.cs (BriefingLifecycle, 시간표), QuestClearPopupMotion.cs (사용 예)
  - Editor/DataValidation/PromptB120StartBriefingBuilder.cs
    - 프레임: Art/UI/Gameplay/Quest/Clear/quest-clear-popup-frame.png
    - 버튼: Art/UI/MainMenu/Settings/button-active-off.png / -on.png
    - 폰트: Fonts/StartBriefingNotoSansKR_SDF.asset
    - 청록 (0.20, 1, 0.96), 제목 36 Bold / 본문 30 / 버튼 30 Bold (1700 폭 기준)
- 깊이 데이터: Data/World/MineLayerDistribution.asset의 심층 띠는 minDepth 36, maxDepth 40이다 (signalDepth 38이 속한 띠).
  - MineLayerTilemapGenerator.ConfigureDeepZoneMiningBoundary가 같은 값을 쓴다.
  - 이 타입은 SubTerra.Gameplay.DemoWorld에 있어서 SubTerra.App(ProgressionPanelView)에서 직접 참조할 수 없다.
- 폰트 도구: Editor/DataValidation/KoreanFontAssetUtility.cs EnsureCharactersSaved (B-153에서 추가)
- 테스트: Tests/EditMode/App/UI/PromptB90DeepZoneUnlockPopupTests.cs
  - "닫기" 문구, 닫기 직후 IsOpen == false, X 경로, Hide/TryHide 같은 상태를 검사한다

## 1. 구현 방향 (재사용)
- ConfigureQuestClear를 크기·프레임 비율을 받는 공용 메서드로 일반화한다 (예: ConfigureCompact(Vector2 size, Sprite frame, RectTransform glint)).
  - 퀘스트 클리어 창은 그 메서드를 불러 지금과 똑같이 동작해야 한다 (QuestClearPopupMotion 테스트 그대로 통과).
- 심층 해금 창은 CreateDeepZoneUnlockPopupIfNeeded에서 그 메서드로 같은 연출을 붙인다.
  - 기존 이름 "DeepZoneUnlockPopup_Runtime", "CloseButton"과 sortingOrder는 유지한다.
- 런타임에 쓸 그림·폰트·깊이는 새 스킨 ScriptableObject에 둔다. 예: Resources/UI/DeepZoneUnlockPopupSkin.asset
  - 들어갈 것: 프레임, 버튼 off/on, 폰트, minDepth, maxDepth
  - Editor Builder(PromptB154DeepZoneUnlockPopupBuilder)가 MineLayerDistribution.asset의 심층 띠 값을 읽어 채운다.
  - 스킨이 없으면 지금 단색 창으로 떨어지고 경고 로그만 남긴다 (크래시 없음).
- 표시 문구는 순수 클래스로 둔다 (예: DeepZoneUnlockPopupText.Range(36, 40) → "36~40M 진입 가능").
  - 레벨 자리를 미리 만들어 둔다: Header(level) — 레벨이 없으면 "심층 구역 해금", 나중에 #223에서 "심층 구역 Lv.1 해금"
  - Range(min, max, level) — 레벨이 없으면 "36~40M 진입 가능", 나중에 "심층 구역 Lv.1 · 36~40M"
  - **이번에는 레벨을 넘기지 않는다.**

## 2. 레이아웃 (기준 1920×1080)
- 창 약 560×280 (프레임 그림 비율 2:1). frameHalfExtent는 ConfigureQuestClear와 같은 비율식으로 환산한다.
- 위→아래
  1) 제목 "심층 구역 해금": 청록, Bold 약 30
  2) 한 줄 "36~40M 진입 가능": 흰색 약 22 (깊이는 스킨 값)
  3) 버튼 "닫기": 시작 브리핑 버튼 그림, 약 200×52, 글자 Bold 약 22
- 글자 크기와 여백은 시작 브리핑 비율을 창 폭에 맞춰 줄인 값이다. 인스펙터에서 조정할 수 있게 한다.
- 1280×720과 1920×1080에서 넘침·잘림이 없어야 한다. 긴 문구("심층 구역 Lv.3 · 76~100M")도 한 줄에 들어가는지 미리 확인한다 (autoSize 하한 사용 가능).

## 3. 연출
- **열기**: 시작 브리핑 시간표를 창 크기에 맞게 쓴다.
  - 짧은 글리치 → 신호선 집결·확산 → 프레임 개방 → 내용 페이드.
  - 작은 창이라 전체 길이는 약 0.8초 이내로 줄여도 된다 (비율 유지).
- **표시 중**: 약한 가장자리 간섭만 둔다.
- **닫기**: 시작 브리핑 닫기 (내용 먼저 사라짐 → 프레임 축소 → 신호 수축 → 짧은 글리치, 약 0.48초).
- 화면 전체를 찢는 tearSlices와 화면 캡처는 쓰지 않는다. 노이즈 막대는 창 안에서만 그린다.
- 시간은 비스케일이다 (업그레이드 창이 시간을 멈춰도 재생).

## 4. 글꼴
- StartBriefingNotoSansKR_SDF는 정적 아틀라스라 글자가 69개뿐이다. 새 문구 글자를 넣는다.
  - 글자 목록: 심층 구역 해금 진입 가능 닫기 숫자 0~9 ~ M L v . ·
  - KoreanFontAssetUtility.EnsureCharactersSaved로 저장해서 디스크 값과 아틀라스가 어긋나지 않게 한다.
- 시작 브리핑 글자 모양은 바뀌면 안 된다. 이미 있는 글자는 건드리지 않는다.
- 테스트: DeepZoneUnlockPopupText의 모든 출력(레벨 문구 예시 포함)이 이 폰트의 HasCharacters로 true인지 검사한다.

## 손대지 말 것
- **마우스 커서 코드** (B-151: GameplayCursor*, PromptB151GameplayCursorBuilder). 보류 중이다.
- 세이브 형식, 해금 규칙·비용, ProgressionService의 해금 판정
- 업그레이드 창 레이아웃과 심층 구역 탭. 레벨제·썸네일은 #223에서 한다.
- 시작 브리핑과 퀘스트 클리어 창의 겉모양·시간·동작 (공용화만 하고 결과는 그대로)
- MineLayerDistribution.asset 값과 채굴 경계 로직
- B-152/B-153 화물 팝업

## 테스트
- EditMode 전체 실행: 기존 실패 목록(B-152·B-153 보고) 외에 새 실패가 없어야 한다.
- PromptB90DeepZoneUnlockPopupTests는 새 동작에 맞게 고치되 뜻은 유지한다.
  - "닫기" 버튼 존재, 닫기 요청 직후 IsOpen == false, X·ESC·버튼이 같은 경로, 두 번째 TryHide == false
- 새 EditMode 테스트
  - 문구: Range(36, 40) → "36~40M 진입 가능", 레벨을 넣으면 "심층 구역 Lv.1 · 36~40M"
  - 스킨 깊이 값 == MineLayerDistribution.asset 심층 띠 값 (코드에 36·40을 박지 않았는지 확인)
  - DidUnlockNow == false나 이미 해금된 상태에서는 창이 뜨지 않음
  - 닫기 연출 중 blocksRaycasts == false, 끝나면 root 비활성 + 정렬 제거
  - 열기 → 즉시 닫기 → 즉시 다시 열기를 반복해도 잔상 오브젝트가 남지 않고 상태가 하나
  - ConfigureCompact로 바꾼 뒤 퀘스트 클리어 창 크기·프레임 값이 이전과 같음
  - 폰트 글자 검사 (4장)
- PlayMode는 이번에 추가하거나 고친 테스트만 실행한다 (QuestClearPopupMotionPlayModeTests 포함).

## 수동 확인 체크리스트
실행 경로: Bootstrap → 새 게임 → 탐사 → 업그레이드 창에서 드릴 속도 Lv.2, 드론 스캔 Lv.2, 가스 저항 Lv.1 구매 (개발자 터미널 사용 가능). 지상 기지 업그레이드 창에서도 한 번 확인한다.
- [ ] 마지막 조건을 사는 순간 창이 시작 브리핑처럼 글리치와 함께 열린다
- [ ] 창 크기가 지금 창 정도로 작고, 프레임·폰트·색·버튼이 시작 브리핑과 같다
- [ ] "심층 구역 해금"과 "36~40M 진입 가능"이 보이고, 빠진 글자가 없다
- [ ] 닫기 버튼, X 키, ESC 모두 글리치와 함께 닫히고, 닫히는 동안 뒤 업그레이드 창이 바로 클릭된다
- [ ] 빠르게 열고 닫아도 노이즈 막대, 잔상 프레임, 어두운 막이 남지 않는다
- [ ] 창을 닫고 업그레이드 창을 다시 열거나 세이브를 다시 불러도 창이 또 뜨지 않는다
- [ ] 게임이 멈추지 않고, 업그레이드 창 입력이 정상이다
- [ ] 시작 브리핑과 퀘스트 클리어 창 연출이 이전과 같다
- [ ] 1280×720과 1920×1080에서 글자가 넘치지 않는다
- [ ] 36~40M 채굴이 해금 후에만 된다 (기존 동작 유지)
- [ ] 커서 동작이 이전과 같다
- [ ] Console 오류가 없다

## 완료 후
변경 파일, 테스트 결과, 공용화한 메서드, 스킨과 깊이 값 출처, 추가한 글자, 남은 문제를 보고한다.
직접 확인 못 한 항목은 "미검증"으로 적는다. 커밋은 사용자 확인 후에만 한다.
