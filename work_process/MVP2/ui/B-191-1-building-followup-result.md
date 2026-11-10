시작 커밋: `23e2711aa52ce69f66c6077c0bbb02b7d9046fc3`

# B-191-1 건설창 후속 수정 결과

- 작업일: 2026-11-10
- 브랜치: `MVP2-fix136` / 기존 PR [#310](https://github.com/Nobamb/sub-terra/pull/310)
- 구현 커밋: `c4557264bed4ea6af539a68b6d945d83a4e885a7`
- 일반 push: `origin/MVP2-fix136`에서 위 구현 커밋 일치 확인.
- `git fetch origin` 후 `origin/main = 570aaaa0d4a444445252dc6ffdaf6868a39584ce`가 시작 HEAD의 조상이어서 추가 병합 불필요. 새 브랜치·PR, rebase·force push·main 직접 커밋 없음.
- 사용자 기존 변경 `init/prompt-B.md`, 미추적 요청서 `work_process/MVP2/prompt-B-191-1.md`는 커밋에서 제외.

## 변경 내용과 경로

### A. 레일 재클릭 해제

실제 클릭 경로는 `Button.onClick → BuildingMenuEntryButton.Select()`이다. 이전에는 레일·큰 목록 모두 `BuildingMenuBinder.SelectBuilding → BuildingMenuPresenter.SelectBuilding()`만 호출했다.

이제 `BuildingMenuEntryVisual`이 붙은 레일 버튼이고 Presenter의 현재 ID와 같은 경우에만 `binder.CancelSelection()`을 호출한다. 이어서 기존 `BuildingMenuPresenter.CancelSelection()`의 Preview 취소·이동 취소·상태 초기화를 그대로 탄다. Preview, 선택 강조, 작은 카드가 사라지고 빈 상태 메시지와 기본 안내로 돌아간다. 다른 시설은 기존 선택 경로로 즉시 전환한다. 큰 목록 행은 계속 선택 후 상세를 연다.

- 이동 중에는 기존 `CancelMove(true)` 경로 유지.
- 별도 취소음 호출이나 새 SFX 추가 없음. 기존 `UiSfx` 클릭 배선도 변경하지 않음.
- 해제한 버튼 위의 호버 이름표는 유지되며, 떠나면 기존 `HintReshowDelay = 0.3f` 규칙으로 안내가 돌아온다.

### B. 자음 표시 원인과 복구

원인은 검색 필터가 문자열을 지우는 문제가 아니라 **동적 TMP 폰트 조회표와 저장된 아틀라스의 불일치**였다.

- 변경 전 `NotoSansKR-Regular_SDF.asset`의 런타임 조회는 `ㄱ·ㅅ·ㄷ`을 보유한 것으로 응답했지만 각 글리프 영역에서 알파 0.2 초과 픽셀은 **0개**였다. `ㅏ`는 조회에서도 누락됐다.
- 디스크에 저장된 글자 표에는 U+3131~U+318E 호환 자모가 없었다. 완성형 `사`의 픽셀은 정상이라 완성 글자부터 보이는 증상과 일치했다.
- `LateUpdate → OnSearchChanged → ApplySearchFilter`는 검색 표시 문자열을 다시 쓰지 않는다. 해당 경로와 IME 미확정 문자열 소비 동작은 유지했다.
- 빌더에서 기존 `KoreanFontAssetUtility.EnsureCharactersSaved`를 사용해 조회표를 동기화하고 호환 자모 **94개**를 저장했다.
- 기존 글리프 **596개 제거 0·수정 0**, FontFeatureTable·KerningTable 동일을 비교했다. 폰트 GUID와 기존 렌더링 리소스 참조는 유지했다.
- 복구 후 `ㄱ·ㅅ·ㅏ`의 실제 픽셀은 각각 **197·276·238개**. PlayMode Domain Reload 후에도 표시·검색 검증 통과.
- 인벤토리 검색창도 같은 폰트를 참조하며 동일 자모 픽셀을 갖는다. 인벤토리 코드·프리팹은 변경하지 않았다.
- 추가 실행한 인벤토리 PlayMode 검사는 모든 초성·조합 검색 단계를 통과했다. 다만 마지막 닫기 직후 입력 차단 해제 검사가 실패해 아래 제한 사항에 기록했다.

### C. 작은 카드 디자인

작은 카드에 큰 목록·상세창과 같은 프레임을 재사용했다.

- 프레임: `Assets/_Project/Art/UI/Inventory/inventory-window-frame.png`
- `Image.Type.Sliced`, 작은 카드 폭 300에 맞춘 픽셀 비율, 배경·청록 테두리·상단 탭 장식 재사용.
- 기존 `CardHeader`에 제목 띠와 얇은 구분선을 추가했다.
- 기존 카드 크기·위치·12px 내부 여백, 이름·썸네일·설명·비용·전력/설치 상태 배치 유지.
- 프레임은 `LayoutElement.ignoreLayout = true`이며 모든 장식의 raycast를 껐다. 카드 자체만 기존 클릭 영역을 유지한다.
- 새 이미지·카드 등장 연출 없음. 실제 캡처에서 글자가 장식에 가리지 않음을 확인했다.

### D. 호버 0.3초 전환

- ★ `BuildingMenuEntryVisual.cs:15` — `HoverTransitionDuration = 0.3f`.
- 인스펙터 값: `BuildingMenuEntryVisual.cs:17`, `BuildingRailHologram.cs:26`의 `[SerializeField] hoverTransitionDuration`.
- 시설 버튼의 즉시 `SpriteSwap`을 기존 off 프레임 위 on 프레임의 알파 전환으로 변경했다. `>>` 버튼의 기존 스타일은 유지했다.
- `Time.unscaledDeltaTime`으로 진행 위치를 이동시키고 SmoothStep을 적용한다. 들어오기·나가기·도중 역전 모두 현재 밝기를 이어간다.
- 시설별 이름표를 빌더가 배선하므로 빠르게 옮길 때 이전 이름표는 현재 위치에서 사라지고 새 이름표는 새 위치에서 켜진다. 이전 버튼의 늦은 Exit가 새 이름표를 끄지 않는다.
- 선택 강조는 별도 `SelectedHighlight`로 유지한다. 모션 감소 설정에서도 작은 호버 전환은 계속 작동한다.
- `>>`·B 닫기·부모 비활성화는 `HideRailHoverNow()`로 모든 이름표와 호버 프레임을 즉시 정리한다.
- 기존 안내 지연 ★ `BuildingMenuView.cs:22`의 `HintReshowDelay = 0.3f` 유지.
- 대체된 즉시 깜빡임·타이핑 계산 코드는 제거하고 기존 주사선 계산은 유지했다.

## 변경 파일·주요 줄

경로 기준은 Unity 프로젝트 `sub-terra/`이며, 교훈은 Git 루트 기준이다. 줄 번호는 구현 커밋 기준이다.

| 파일 | 주요 위치·내용 |
| --- | --- |
| `Assets/_Project/Scripts/App/UI/Building/BuildingMenuEntryButton.cs` | 47: 레일만 같은 시설 재클릭 취소 |
| `Assets/_Project/Scripts/App/UI/Building/BuildingMenuEntryVisual.cs` | 15: ★ 기본 시간, 47: 알파 진행 |
| `Assets/_Project/Scripts/App/UI/Building/BuildingMenuView.cs` | 80: 이름표 배열, 412: 시설별 호버, 464: 즉시 정리 |
| `Assets/_Project/Scripts/App/UI/Building/BuildingRailHologram.cs` | 26: 인스펙터 시간, 133: 역전 가능한 unscaled 진행 |
| `Assets/_Project/Scripts/App/UI/Building/BuildingRailHologramMotion.cs` | 주사선 계산 유지, 대체된 계산 제거 |
| `Assets/_Project/Editor/DataValidation/PromptB188BuildingModeBuilder.cs` | 183: 시설별 이름표, 452: 호버 프레임, 639: 작은 카드, 1123: 자모 저장 |
| `Assets/_Project/Fonts/NotoSansKR-Regular_SDF.asset` | 호환 자모·아틀라스 저장, 기존 글리프/자간 보존 |
| `Assets/_Project/Prefabs/UI/BuildingMenu.prefab` | 위 공식 빌더로만 재생성. 새 프레임·호버·이름표 참조 배선 |
| `Assets/_Project/Tests/EditMode/App/UI/PromptB191FollowupTests.cs`와 `.meta` | 45: 재클릭, 66: 이동 취소, 95: 픽셀, 134: 시간·역전, 176: 빠른 전환 |
| `Assets/_Project/Tests/EditMode/App/UI/PromptB188RailHologramTests.cs` | 시설별 이름표 참조와 새 전환 사양에 맞춤 |
| `Assets/_Project/Tests/EditMode/App/UI/PromptB191BuildingModeTests.cs` | 시설 호버만 SpriteSwap 대신 알파 전환임을 검사 |
| `Assets/_Project/Tests/PlayMode/Integration/Bootstrap/PromptB110BuildingMenuPlayModeTests.cs` | 54: 실제 클릭·미확정 IME·일시정지 호버·화면 캡처 |
| `init/rules/lessons/2026-11-10-issue304-search-jamo-atlas.md` | 자모 조회표·아틀라스 불일치 교훈 |

`BuildingMenu.prefab` 직렬화 변경량에는 이름표 8개·호버 프레임 9개 추가와 Unity 저장 시 블록 재정렬이 포함된다. 기존 UI를 파괴 후 다시 만든 것이 아니며 Scene YAML은 변경하지 않았다. Inspector 연결은 빌더가 수행하므로 별도 수동 배선은 없다.

## 검증

- Unity 6000.5.4f1에서 새 코드와 테스트 컴파일 확인. 테스트 전에 새 DLL의 갱신 시각 확인.
- EditMode **85 passed / 0 failed**: 새 후속 테스트 12개, B-188·B-191·B-110·B-181 관련 검사.
- PlayMode **10 passed / 0 failed**: B-188 레일, #213·#241 창 연출 6개, 기존 건설 동작 3개, 새 후속 검사 1개.
- 실제 Integration 화면에서 카드 표시·재클릭 해제, 미확정 `ㅅ·ㅅㄷ·사ㄷ` 표시와 필터 결과를 확인했다. `Time.timeScale = 0` 및 ReduceMotion 상태에서도 Update가 호버 열기·닫기를 끝까지 진행했다.
- 기존 PlayMode 검사에서 창 밖 지형 채굴·빈 칸 설치, 건설 비용·플레이어 전력, 정렬·검색·상세·뒤로가기·B 닫기·표시 순서를 확인했다.
- 최신 Console: 오류/예외 **0**, 기존 `CoreCctvCameraRig.EnsureCreated`의 SRP `stereoTargetEye` 경고 **4**. 이번 수정과 무관한 파일은 변경하지 않았다.
- 추가로 실행한 `BuildingMenuPresenterTests`는 **9 passed / 1 failed**. 기존 `G_F03`이 `겹쳐`를 기대하지만 변경하지 않은 Presenter는 `겹칩니다`를 반환하는 문구 불일치다. 건설 후속 검사 85개에는 이 기존 실패가 포함되지 않는다.
- 같은 폰트의 인벤토리 회귀 검사 `InventoryPanel_LiveValuesStatesAndLayout` **1 failed**: 초성·IME 조합·기존 검색 입력 검사는 모두 지난 뒤, 마지막 닫기 단계 `PromptB112InventoryPanelPlayModeTests.cs:296`에서 `UiPauseGate.IsInputBlocked`가 즉시 false가 되지 않았다. 인벤토리 변경은 없으며 이 닫기/입력 차단 해제 문제는 이번 범위 밖으로 남겼다. 원인 분리·수정은 미검증이다.
- 코드·테스트·문서 `git diff --check` 통과. 전체 검사에는 Unity가 저장한 프리팹/폰트의 빈 문자열 필드 후행 공백 234건이 있다. YAML 수동 정리는 하지 않았다.
- 테스트가 자동 저장한 `ProjectSettings/EditorBuildSettings.asset`, `TimeManager.asset`는 시작 상태로 복구했다. Packages·버전·SFX 파일 변경 없음.
- 구현 커밋의 범위 밖 UI 프리팹·Scene·기존 사용자 변경은 0개.

보존된 로컬 결과(모두 Git 제외):

- `sub-terra/Builds/B191-1/editmode-final.txt`
- `sub-terra/Builds/B191-1/playmode-font-final/PlayMode-summary.txt` 및 `PlayMode-results.xml`
- `sub-terra/Builds/B191-1/shared-font-inventory/PlayMode-results.xml` — 추가 인벤토리 검사 실패 기록
- `sub-terra/Builds/B191-1/evidence/small-card.png`, `reclick-cleared.png`, `composition-ㅅ.png`, `composition-ㅅㄷ.png`, `composition-사ㄷ.png`, `ui-contact.png`

## 미검증·사용자 플레이 가이드

Windows 실물 한글 키보드/IME 입력과 스피커로 듣는 취소음, 빠른 포인터 이동의 체감, 3개 해상도 Visual 전체 검사는 **미검증**이다. 자동 검사에서 IME 문자열은 TMP가 실제로 소비하는 BaseInput에 주입했고, 오디오 경로를 변경하지 않았으나 청취 검증과는 구분한다.

1. B로 건설 모드를 열고 시설 하나를 고른다. 같은 시설을 다시 눌러 카드와 Preview가 사라지고 기본 안내로 돌아가는지, 기존 취소음이 나는지 듣는다.
2. 시설을 고른 상태에서 다른 시설을 눌러 바로 전환되는지 확인한다.
3. 시설 하나를 설치한 뒤 재클릭 해제 상태와 화면을 비교한다.
4. `>>` 목록의 검색창에 실물 키보드로 `ㅅ`을 쳐 표시와 필터를 확인한다.
5. `ㅅㄷ`, `사ㄷ`를 차례로 입력해 보이는 문자열대로 사다리가 남는지 확인한다.
6. 작은 카드가 상세창과 같은 프레임인지, 이름·비용·전력/상태가 가리지 않는지 확인한다.
7. 레일 버튼에 천천히 들어갔다 나가며 양방향 약 0.3초 전환을 본다.
8. 레일을 빠르게 위아래로 훑어 이전/다음 전환과 안내 깜빡임을 확인한다.
9. 고른 버튼에서 마우스를 떼도 선택 표시가 남는지 확인한다.
10. 정렬 4종·초성 검색·큰 창/상세 TV 켜짐·말풍선 위 순서를 확인한다.
11. 카드 밖 지형을 캐고 빈 칸 클릭·C로 시설을 설치해 본다.
12. 새 효과음이 없는지 듣고, 기존 CCTV 경고 외 새 빨간 오류·노란 경고가 없는지 확인한다.

## 완료 체크리스트

- [x] `MVP2-fix136` 일반 push 확인·구현 커밋 해시 기재
- [x] Unity 컴파일 오류 없음·요청 EditMode 검사 새 실패 없음
- [x] 같은 시설 재클릭 해제·카드/Preview 제거·다른 시설 전환·큰 목록 상세 유지
- [x] 기존 CancelSelection/CancelMove 취소 경로 유지·새 SFX 없음
- [ ] 취소음 실물 청취 — 미검증
- [x] TMP 미확정 자음/조합 표시·즉시 검색·원인 보고
- [ ] Windows 실물 IME 타이핑 — 미검증
- [x] 작은 카드 상세창 프레임 계열·크기/정보 유지·화면 가림 없음
- [x] 호버 0.3초 기본·인스펙터 조절·unscaled·역전/빠른 전환·선택 표시 유지
- [x] 안내 재표시 지연·B-191 정렬/검색/TV/Canvas 기능 유지
- [x] main 직접 커밋 없음·새 PR 없음·GitHub 이슈/PR 체크박스 수정 없음
