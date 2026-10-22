# B-155 업그레이드 창 심층 구역: 글자 → 레벨별 광산 썸네일 + 자물쇠 해금 연출 (#223 남은 부분)

## 목표
업그레이드 창 아래 심층 구역 글자 칸("심층 구역 Lv.0/3 · 잠금 / Lv.1 36~40M ...")을 **레벨마다 가로로 긴 광산 썸네일 1장씩, 총 3장**으로 바꾼다.
- 잠김: 흑백 + 광물만 회색빛으로 은은하게 반짝임 + 가운데 자물쇠
- 해금 순간 (한 번만): 자물쇠 흔들림 → 딸깍 → 떨어짐, 자물쇠 자리부터 색이 퍼지며 광물이 원래 색으로 빛남
- 이미 해금된 레벨: 연출 없이 컬러로 바로 보임
- 썸네일 그림과 연출 값은 사용자(PM)가 직접 만들고 조정한다. 그림이 없어도 동작해야 하고, 값은 인스펙터나 설정에서 바꿀 수 있어야 한다.

## 기준 브랜치
- PR #235(`codex/issue-223-deep-zone-display`)가 글자 부분을 이미 했다. 이 위에서 작업한다.
- #235가 main에 합쳐졌으면 최신 main, 아니면 `origin/codex/issue-223-deep-zone-display`에서 새 브랜치를 딴다. 어느 쪽인지 보고한다.

## 절대 규칙 (먼저 읽기)
1. 실제 해금·탐사 깊이·비용·저장은 바꾸지 않는다. 이번 작업은 **표시만** 한다.
   - 대상: DeepZoneUnlockRule.Mvp, ProgressionService.TryUnlockDeepZone/GetDeepZoneAccess, ZoneAccessResult, 세이브 형식
2. 레벨 수·깊이·조건 문구는 **DeepZoneDisplaySettings**에서만 읽는다. 36, 40, 41, 75, 3 같은 숫자를 코드에 박지 않는다.
3. 해금 연출은 **해금 한 번에 한 번만** 재생한다.
   - 창을 다시 열거나 세이브를 불러오면 연출 없이 컬러로 바로 보인다 (기존 세이브는 Lv.1 해금으로 보임).
   - 해금 상태를 새로 저장하지 않는다. 런타임 메모리로만 판단한다.
4. 반짝임은 은은하게 한다. 주기는 2초 이상이고 빠른 깜빡임은 넣지 않는다. 세 장이 동시에 번쩍이지 않게 위상을 어긋낸다.
5. 시간은 비스케일이다 (UpgradeTreeTween.Run 사용). 업그레이드 창이 게임을 멈춰도 재생된다.
6. 그림·셰이더가 없거나 못 읽어도 크래시 없이 대체 화면을 보여 주고, 경고 로그는 한 번만 남긴다.
7. 썸네일은 클릭을 받지 않는다 (raycastTarget = false). 트리 스크롤과 줌을 막지 않는다.

## 먼저 읽을 코드 (sub-terra/Assets/_Project/ 기준)
- 설정: Scripts/App/Progression/DeepZoneDisplaySettings.cs, Resources/DeepZoneDisplaySettings.asset
  - DeepZoneDisplayLevel: minimumDepth, maximumDepth, availableInDemo, plannedRequirement
  - BuildText: 지금은 access.IsUnlocked면 Lv.1, 아니면 Lv.0이다.
  - 빌더: Editor/DataValidation/Issue223DisplaySettingsBuilder.cs (에셋이 있으면 아무것도 안 함)
- 아래 칸: Scripts/App/UI/Progression/UpgradeTreeView.cs
  - SetDeepZoneStatus(access) → EnsureDeepZoneText()
  - 런타임에 "DeepZoneStatus"(높이 84, viewport.parent 아래)를 만들고 viewport.offsetMin을 그만큼 올린다.
  - **프리팹·씬에는 이 칸이 없다.** 업그레이드 창은 Prefabs/UI/SurfaceBasePanel.prefab(지상 기지)과 Scenes/App/Mine_Demo_Integration.unity(UpgradePanel)에 있다. 둘 다 런타임 생성으로 처리하고, 프리팹·씬은 고치지 않는다.
- 호출 흐름: ProgressionPanelPresenter.RefreshDeepZoneAccess / OnDeepZoneAccessChanged → ProgressionPanelView.SetDeepZoneAccess → treeView.SetDeepZoneStatus
- 해금 창: ProgressionPanelView.ShowDeepZoneUnlockPopup, IsDeepZoneUnlockPopupOpen, ProgressionPanelBinder.OnDeepZoneAccessChanged (IsUnlocked && DidUnlockNow)
- **#188 자물쇠 톤**: Scripts/App/UI/Progression/UpgradeTreeNodeView.cs UnlockRoutine
  - 진동 0.2초 (sin 8π, 12°) → 자물쇠 숨김과 파편 → 청록 점등
  - 자물쇠 모양은 Editor/DataValidation/PromptB118UpgradeTreeBuilder.cs LockPart에 있다: LockBody, ShackleLeft, ShackleRight, ShackleTop, 색 (0.5, 0.62, 0.68)
- 트윈과 색: Scripts/App/UI/Progression/UpgradeTreeTween.cs (Run, WaitUnscaled, EaseOut, Pulse, Cyan, TextMain, TextDim)
- UI 셰이더 예시: Resources/DepthDarknessOverlayUI.shader (CGPROGRAM)
  - 로드는 DepthDarknessOverlayController처럼 Resources.Load<Shader>를 먼저 하고, 없으면 Shader.Find를 쓴다.
  - 렌더 파이프라인: URP 17.6, uGUI 2.5
- 테스트: Tests/EditMode/App/UI/PromptB56UpgradePanelTests.cs, Tests/EditMode/App/Progression/PromptB1181UpgradeWindowTests.cs ("심층 구역 Lv.0/3" 검색, 새로고침 중복 없음)

## 1. 설정 확장 (DeepZoneDisplaySettings)
- DeepZoneDisplayLevel에 필드를 추가한다. 기존 필드와 직렬화 이름은 유지한다.
  - `Sprite thumbnail`: 광산 그림
  - `Sprite mineralMask`: 같은 크기이고, 광물 자리만 불투명한 투명 PNG. 반짝임에 쓴다.
- 해금된 레벨 수를 계산하는 순수 메서드를 만든다: `UnlockedLevelCount(ZoneAccessResult access)`. 지금은 IsUnlocked면 1, 아니면 0 (BuildText와 같은 규칙).
  - 나중에 #50에서 Lv.2·3 해금이 생기면 이 메서드만 바꾼다.
- 빌더는 에셋이 이미 있어도 **비어 있는 sprite 칸만** 아래 경로의 파일로 채운다. 깊이와 문구는 덮어쓰지 않는다.
- 행성별 레벨(#224)은 하지 않는다. 다만 설정 한 장이 한 행성 분량이 되도록 지금 구조를 유지한다.

## 2. 그림 자리 (사용자가 채움)
- 경로: `Art/UI/DeepZone/deep-zone-lv1.png`, `deep-zone-lv1-minerals.png` (lv2, lv3도 같은 방식). 기존 Art/UI 폴더처럼 소문자와 하이픈을 쓴다.
- 권장 원본은 **480×160px (3:1)** 도트다. Import 설정: Sprite (Single), Filter Point, 압축 없음, Mesh Type Full Rect.
  - 실제 칸 크기를 재고, 비율이 3:1과 많이 다르면 권장 크기를 보고에 적는다.
- 이번 작업에서 **임시 그림 3장 + 광물 마스크 3장**을 만들어 넣는다. 레벨이 오를수록 더 어둡게 하고, 광물 점을 몇 개 찍는 정도면 된다. 사용자가 같은 이름으로 덮어쓰면 바로 바뀌어야 한다.
- sprite가 비어 있으면 레벨별 단색 대체 칸(어두운 남색, 레벨마다 조금 더 어둡게)을 보여 준다. 그래도 자물쇠·글자·연출은 그대로 동작한다.

## 3. 레이아웃 (기준 1920×1080)
- EnsureDeepZoneText의 "DeepZoneStatus"를 썸네일 칸으로 바꾼다. 같은 자리(트리 영역 아래, 트리 폭)를 쓴다.
  - 높이 약 140 (윗줄 약 22 + 썸네일 약 110). viewport는 그만큼 올리고, 트리가 FitContent로 잘 줄어드는지 확인한다.
- 윗줄: 한 줄 요약 "심층 구역 Lv.1/3" (TMP, 기존 deepZoneText 재사용). 모두 해금이면 끝에 " · 모든 구역 해금"을 붙인다.
- 아래: 썸네일 3장을 가로로 나란히 둔다 (HorizontalLayoutGroup, 간격 8). 각 칸 구성은 이렇다.
  1) Thumbnail (Image)
  2) MineralGlint (Image, mineralMask)
  3) Lock: LockPart와 같은 4조각, 크기 약 1.3배
  4) 아래 띠: 반투명 검정 + 글자 2줄 (1줄 "Lv.2 · 41~75M" Bold 약 16, 2줄 조건 약 13 TextDim)
- 2줄 조건 문구: 해금됨 → "해금됨", Lv.1 잠김 → DeepZoneUnlockRule.Mvp.DescribeUpgradeRequirements(), 그 밖 → plannedRequirement (Lv.3은 "준비 중"이 들어 있으니 그대로)
  - availableInDemo == false면 1줄 끝에 " (예정)"을 붙인다.
- 칸 수는 settings.Levels.Count를 따른다. 새로고침해도 칸이 늘지 않는다 (한 번 만들고 재사용).
- 폰트는 detailName.font를 쓴다. 새 문구 글자(모든, 예정, 준비 중, ·, ~)가 빠지지 않는지 HasCharacters로 확인한다.
- 1280×720과 1920×1080에서 글자가 잘리지 않아야 한다 (autoSize 하한 11).

## 4. 흑백과 색 퍼짐 (셰이더)
- 새 UI 셰이더 `SubTerra/DeepZoneThumbnailUI`를 Resources/DeepZoneThumbnailUI.shader로 만든다. UI-Default 기반으로 하고, RectMask2D(UNITY_UI_CLIP_RECT)와 stencil을 유지한다.
  - 속성: `_Saturation` (0 흑백 ~ 1 컬러), `_RevealCenter` (UV, 자물쇠 위치), `_RevealRadius` (0 → 약 1.2), `_RevealFeather`
  - 퍼짐 안쪽은 컬러, 바깥은 흑백. 잠김은 radius 0이고, 흑백 밝기는 0.6배쯤으로 살짝 어둡게 한다.
- 칸마다 머티리얼 인스턴스를 쓰고 OnDestroy에서 지운다. 공유 머티리얼은 건드리지 않는다.
- 셰이더를 못 읽으면 Image.color를 회색 (0.45, 0.45, 0.45)으로 대체하고, 해금 연출은 색 페이드로만 한다.

## 5. 광물 반짝임
- MineralGlint는 썸네일 위에 겹친다. 색은 잠김 회색 (0.8, 0.85, 0.88), 해금 흰색(그림 원래 색)이다.
- 알파는 느린 사인으로 움직인다: 잠김 0.15~0.45, 해금 0.6~0.9, 주기 약 2.6초, 칸마다 위상을 다르게 한다.
- 진폭, 주기, 색은 인스펙터에서 조정할 수 있게 [SerializeField]로 둔다.
- 마스크가 없으면 반짝임 Image를 끈다.

## 6. 해금 연출 (약 1.0초, #188 톤)
- 0.00~0.20: 자물쇠 진동. UnlockRoutine과 같은 식 (sin 8π, 12°)
- 0.20~0.30: 딸깍. ShackleTop, ShackleRight가 위로 약 6px 튀고, 자물쇠 scale이 1 → 1.1 → 1로 바뀐다.
- 0.30~0.65: 떨어짐. 자물쇠가 아래로 약 40px 떨어지며 20~30° 돌고 사라진다 (EaseIn).
- 0.30~1.00: 색 퍼짐. _RevealRadius가 0 → 1.2 (EaseOut), 광물 반짝임은 회색 → 컬러로 바뀐다.
- 0.85~1.00: 광물이 한 번 밝게 빛난다 (Pulse). 끝나면 일반 해금 상태로 넘어간다.
- 끝 상태는 "연출 없이 해금으로 그린 상태"와 같아야 한다. 연출 도중 창이 꺼지면 바로 끝 상태로 맞춘다.

## 7. 언제 재생할지
- SetDeepZoneStatus에서 UnlockedLevelCount를 계산한다.
  - 처음 그릴 때와 DidUnlockNow == false일 때는 연출 없이 바로 그린다.
  - 개수가 늘었고 DidUnlockNow == true면, 새로 열린 레벨을 "연출 대기"로 표시한다.
- 대기한 연출은 업그레이드 창이 보이고(isActiveAndEnabled) 해금 창이 닫혀 있을 때(IsDeepZoneUnlockPopupOpen == false) 한 번만 재생하고 대기를 지운다.
- 그래서 해금 창(B-154)에 가리지 않고, 해금 창을 닫은 뒤에 재생된다. 창이 닫혀 있었으면 다음에 열 때 재생된다.
  - UpgradeTreeView가 해금 창 상태를 알 수 있게 ProgressionPanelView에서 Func<bool>이나 메서드 호출로 넘긴다. 순환 참조는 만들지 않는다.
- 대기 상태는 저장하지 않는다. 게임을 껐다 켜면 연출 없이 컬러로 보인다.

## 손대지 말 것
- **마우스 커서 코드** (B-151: GameplayCursor*, PromptB151GameplayCursorBuilder)
- 세이브 형식, 해금 규칙·비용, ProgressionService 판정, MineLayerDistribution.asset, 채굴 경계 로직
- 업그레이드 트리 노드·연결선 연출 (UpgradeTreeNodeView, UpgradeTreeConnector). 읽고 톤만 맞춘다.
- 해금 창(B-154) 겉모양과 동작, B-152/B-153 화물 창
- 행성별 레벨(#224), 실제 Lv.2·3 해금(#50)
- SurfaceBasePanel.prefab와 Mine_Demo_Integration.unity의 직렬화 내용 (런타임 생성으로 해결. 꼭 필요하면 이유를 보고)

## 테스트
- EditMode 전체 실행: 기존 실패 목록 외에 새 실패가 없어야 한다.
- 기존 테스트는 뜻을 유지한다: "심층 구역 Lv.0/3" 요약, 새로고침 중복 없음, 제목 '업그레이드'.
- 새 EditMode 테스트
  - UnlockedLevelCount: 잠김 0, 해금 1. 레벨 수가 설정을 따른다 (2단계 설정 → 칸 2개).
  - sprite·마스크가 비어도 예외 없이 대체 칸이 나온다.
  - 해금 + DidUnlockNow == false → 연출 없음. 자물쇠 꺼짐, _Saturation 1 (또는 대체 색), 대기 없음.
  - DidUnlockNow == true인데 해금 창이 열려 있음 → 대기만 하고, 닫힌 뒤 한 번 재생, 다시 SetDeepZoneStatus해도 재생 안 함
  - 연출 끝 상태 == 바로 그린 해금 상태
  - 라벨 문구: "Lv.1 · 36~40M", Lv.3 "준비 중" 포함. 깊이 값이 설정에서 온다.
  - 빌더가 기존 깊이·문구를 덮어쓰지 않고 빈 sprite만 채운다.
  - 셰이더가 Resources에서 로드된다.
- PlayMode는 이번에 추가한 테스트만 실행한다.

## 수동 확인 체크리스트
실행 경로: Bootstrap → 새 게임 → 탐사 → U 업그레이드 창. 지상 기지 업그레이드 창에서도 한 번 확인한다. 해금 조건은 개발자 터미널로 채워도 된다.
- [ ] 창 아래에 썸네일 3장이 보이고, 모두 흑백 + 자물쇠 + 은은한 회색 반짝임이다
- [ ] 각 칸에 "Lv.N · 깊이"와 조건이 잘리지 않고 보이며, Lv.3은 "준비 중"이다
- [ ] 드릴 속도 Lv.2, 드론 스캔 Lv.2, 가스 저항 Lv.1을 사면 해금 창이 뜨고, 닫으면 Lv.1 칸에서 자물쇠가 흔들리고, 딸깍하고, 떨어지며 색이 퍼진다 (약 1초)
- [ ] 창을 닫았다 열거나 세이브를 다시 불러오면 Lv.1이 연출 없이 컬러다
- [ ] 반짝임이 거슬리게 깜빡이지 않는다
- [ ] 트리 스크롤·줌·노드 클릭이 이전과 같고, 트리가 아래 칸에 가리지 않는다
- [ ] 임시 그림을 다른 PNG로 덮어쓰면 바로 반영된다
- [ ] 1280×720과 1920×1080에서 넘침이 없다
- [ ] 36~40M 채굴은 해금 후에만 된다 (기존 동작)
- [ ] 커서 동작이 이전과 같다
- [ ] Console 오류가 없다

## 완료 후
기준 브랜치, 변경 파일, 테스트 결과, 실제 썸네일 칸 크기(px)와 권장 원본 크기, 그림 넣는 법(경로·Import 설정·마스크 만드는 법 한 줄), 조정 가능한 값과 위치, 남은 문제를 보고한다.
직접 확인 못 한 항목은 "미검증"으로 적는다. 커밋은 사용자 확인 후에만 한다.
