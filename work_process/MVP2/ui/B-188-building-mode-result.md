# B-188 결과 보고: 시설 건설 아이콘 레일 모드 + 목록·검색·상세 (#269)

## 기준
- 기준 브랜치: `origin/main` `16d7e053` (#250 병합). `38af13f8`(#297) 포함 확인: `git merge-base --is-ancestor 38af13f88 origin/main` → 포함.
- 만든 브랜치: `MVP2-fix134` (원격에 같은 이름 없음 확인). `codex/issue-269-building-readability`(PR #276)는 이어 고치지 않았다.
- 커밋 해시: **미정 — 커밋·push는 사용자 승인 대기** (에이전트 권한 정책상 커밋이 차단되어 작업 트리에만 반영됨).
- `prompt-B-188.md`는 `codex/issue-269-building-readability` 브랜치(`1c4ed580`)에만 있어 이 브랜치에는 없다.

## 바꾼 파일
| 파일 | 내용 |
|---|---|
| `Editor/DataValidation/PromptB188BuildingModeBuilder.cs` (신규) | `SubTerra/UI/Build Prompt-B 188 Building Mode Rail`. 상수 경로 `BuildingMenu.prefab` 하나만 열고 저장. 기존 Select_*·View 참조를 옮겨 재사용, B-110 머리말·DETAILS·조작 안내 장식 삭제 |
| `Prefabs/UI/BuildingMenu.prefab` | 위 빌더로 재생성 |
| `Scripts/App/UI/Building/BuildingMenuView.cs` | 레일·카드·`>>` 큰 창(목록/검색/상세)·뒤로가기·검색 중 입력 정지 처리. `IBuildingMenuView` 계약은 그대로 |
| `Scripts/App/UI/Building/BuildingModeNavigation.cs` (신규) | 레일/목록/상세 전환·검색어 유지·닫을 때 초기화(순수 C#) |
| `Scripts/App/UI/Building/BuildingModeSlideMotion.cs` (신규) | 왼쪽 슬라이드인/아웃. 기존 `WindowTvSfx` 켜짐·꺼짐만 재생(새 SFX 없음) |
| `Scripts/App/UI/Building/BuildingMenuCatalogRowView.cs` (신규) | 목록 한 칸(아이콘·이름·짧은 용도·보유/필요 비용). 선택은 같은 오브젝트의 `BuildingMenuEntryButton` |
| `Scripts/App/UI/Building/BuildingMenuDisplayFormatter.cs` | ~88-102: 설치 가능 옆 조작 안내 삭제, 위치 확인 문구. ~107-190: `전진기지 범위 내`, `ShortPurpose`, `MatchesSearch`. ~278-316: 비용 `보유 / 필요`, `충분`/`N 부족`, `CostLines` |
| `Scripts/App/Integration/GameplayBuildingPlacementBridge.cs` | ~72-77 `OwnsPointerInput`, ~329-369 좌클릭 우선순위(지형 블록 → 채굴, 그 밖 → 기존 설치 판정) |
| 테스트 | 신규 `PromptB188BuildingModeTests`(EditMode), `PromptB110BuildingMenuPlayModeTests` 갱신(+클릭 우선순위 검사), `Issue213WindowOpenMotionTests`(+레일 슬라이드 검사), 예전 2단 배치 기대값을 레일 기준으로 갱신: `PromptB110BuildingMenuTests`, `BuildingMenuPresenterTests.G_S05`, `PromptB35/B46/B48` |
| `init/rules/lessons/2026-11-07-issue269-slide-open-click-timing.md` (신규) | 교훈 1건 |

Integration Scene(`Mine_Demo_Integration.unity`)·ProjectSettings·다른 UI 프리팹·폰트는 커밋 대상에 없다. 테스트 실행이 dirty로 만든 범위 밖 파일은 모두 `git restore`로 되돌렸다.

## 화면 구성 (Scene 루트 16,-360 / 500×560은 그대로 두고 내용만 재배치)
- **레일** `PanelRoot/Rail` (64×550): 시설 아이콘 9개(48×48, 기존 순서) + 하단 `>>`/`<<`. 이름·비용 글자는 숨김.
- **카드** `PanelRoot/SidePane` (x=72, 폭 300, 세로 레이아웃): 미선택이면 `HintChip`(“시설을 고르세요”)만, 선택하면 `Card` = 아이콘·시설명 → 짧은 용도(첫 문장, 40자 넘으면 `…`) → `전진기지 범위 내`(충전기·보건소·정산기만) → 건설 비용(`자원 | 보유 / 필요 | 충분·N 부족`, 부족만 경고색) → 설치 상태(`시설 미선택`/`자원 부족 + 철 2개가 더 필요합니다.`/`설치 위치 확인 + 실제 사유 또는 설치 가능한 위치로 커서를 옮기세요.`/`설치 가능`). 설치 결과 메시지는 카드 밑 `StatusChip`에 따로.
- **큰 창** `PanelRoot/CatalogWindow` (x=72, 520×550): `ListPage` = 제목·`시설명 검색` 입력·스크롤 목록 9칸·“맞는 시설이 없습니다.” / `DetailPage` = `< 시설 목록`·썸네일·이름·`설치 조건 · 전진기지 범위 내`·기능(전체 설명)·건설 비용·설치 상태. 펼친 동안 작은 카드는 접힌다.
- 빈 `PanelRoot`는 배경 Image 비활성·`PopupWindowDrag` 제거 → 레일·카드·큰 창(및 그 버튼·검색창·스크롤)만 raycast를 받는다. X 버튼은 참조 호환용으로 남기고 숨김(B·메뉴 버튼으로 닫음).
- 검색창에 글자를 치는 동안만 기존 `UiPauseGate`(디버그 터미널과 같은 방식)를 잡아 B·C·이동 키가 월드로 가지 않게 했다. 포커스가 빠지면 즉시 풀린다.

## 클릭 우선순위
1. UI 위: EventSystem이 레일·카드·큰 창 버튼으로 보낸다(기존).
2. 건설 대상이 있어도 **지형 블록 위에서 시작한 좌클릭**은 `GameplayBuildingPlacementBridge.OwnsPointerInput`이 false → `PlayerMiningController`가 채굴. 그 칸은 원래 `Occupied`로 설치 실패하던 칸이라 설치 판정 결과는 바뀌지 않는다.
3. 그 밖의 칸: 기존 `TryPlaceAt` 설치 판정 그대로. C 근접 설치·Preview·우클릭 이동/취소는 변경 없음.

## 테스트 결과 (에디터 6000.5.4f1, 플래그·검증 실행기)
- **EditMode 전체**: 변경 후 2460 통과 / 28 실패. 같은 세션에서 변경을 stash 한 `main` 기준 전체 실행은 2459 / 23 실패. 변경 후에만 있던 5건 중 4건(`G_S05`, `B35 LeftRightContentGap`, `B46 portal entry`, `B48 ladder`)은 예전 2단 배치 기대값이라 레일 기준으로 고친 뒤 통과, 1건(`PromptB150 DeleteButton_Hover`)은 단독 재실행 통과(전체 실행 순서에서 흔들리는 메인 메뉴 검사, 이번 변경과 무관).
- 남은 실패는 모두 `main`에도 있는 기존 실패: `G_F03`(“겹쳐” 문구), `PromptB32/B35` 건설창 폭 580.8 기대, `B89 X_Closes...`, `B107 Scene_UsesQuestFrames...`, `IntegrationWiringTests` 3건, `PhaseF F_S04`, Surface/Upgrade/MainMenu/HUD 계열 등.
- **새 실패 없음**: 수정 후 관련 묶음 재실행 80 통과 / 3 실패(3건 모두 기존 실패).
- **PlayMode(quick)**: `PromptB110BuildingMenuPlayModeTests`, `Issue213WindowOpenMotionTests`, `PromptB169UnlockAndTvPlayModeTests`(창 TV 소리), `PromptB105SideMenu`, `PromptB112InventoryPanel`, `BuildingUiPlayModeTests`, `PromptB79MiningFailureHud` → **20 / 20 통과**. 새 검사 `BuildingMode_ClickOnTerrainMinesAndEmptyGroundPlaces`(가상 마우스로 지형 블록 → 채굴 시도·설치 실패 없음, 빈 땅 → 설치 1회), `Issue188_BuildingRailSlidesInFromLeftAndRestoresPosition` 포함.
- **PlayMode(visual)**: `BuildingMenu_StatesAndLayout_AtThreeResolutions` 통과. 1366×768·1920×1080·2560×1440에서 레일·카드·큰 창이 화면 안이고 `BasicHUD`·`QuestSummaryButton`·우측 메뉴와 겹치지 않음을 검사. 캡처: `sub-terra/Temp/visual/prompt-b188/*.png`(Temp라 커밋 안 됨).
- Unity Console: 컴파일 오류 0. 새 경고 없음(기존 CS0618 경고만).

## 미검증
- 사람이 직접 Bootstrap → 광산까지 플레이하며 아래 가이드를 끝까지 확인하는 것은 **미검증**(자동 PlayMode·캡처로만 확인).
- 한글 IME로 검색창에 입력(조합 중 문자 포함) — **미검증**.
- Windows 빌드(`PhasePWindowsBuildGate`) — **미검증**.
- PM 컨셉 이미지(`sub-terra-ui-B188/`)는 작업 환경에서 찾지 못해 이슈 #269 본문만 기준으로 배치했다.

## 판단·남은 점 (PM 확인 요청)
- 레일 열기·닫기에 **기존** 창 켜짐·꺼짐 소리(`WindowTvOpen/Close`)를 그대로 썼다. 새 SFX는 없지만, 레일에 이 소리가 어색하면 빼는 것도 한 줄 변경이다.
- 설치가 끝나면 선택이 풀리는 기존 Presenter 동작은 바꾸지 않았다. 상세를 보던 중 설치하면 상세 상태가 `시설 미선택`으로 바뀐다.
- 예전 `PromptB110BuildingMenuBuilder`를 다시 실행하면 레일 구조가 깨진다(빌더는 B-110 배치 전용). 다시 만들 때는 B-188 빌더를 쓴다.
- `HudPanelChromeLayoutTests`의 `SelectionText` x=240 검사는 경로가 바뀌어 건너뛰어진다(`if (selection != null)`). 필요하면 별도 정리.

## 사용자 테스트 가이드 (직접 플레이)
1. B를 누르면 큰 팝업 대신 왼쪽 아이콘 레일이 나오는지 본다.
2. 사다리를 고르면 작은 카드에 이름·용도·비용·설치 상태가 보이는지 본다. `좌클릭 설치 · C 근접 설치 · B 닫기` 문구가 없는지 본다.
3. 레일·카드 밖 블록을 클릭해 캘 수 있는지, 빈 땅에 설치할 수 있는지 본다.
4. `>>`를 누르면 여러 시설이 한눈에 보이는 목록과 검색창이 나오는지 본다.
5. 검색창에 시설명 일부를 넣으면 그 시설만 남고, 지우면 다시 전체가 보이는지 본다.
6. 목록에서 충전기를 고르면 썸네일, 기능, 필요 자원, `전진기지 범위 내`, 설치 상태가 보이는지 본다.
7. 상세에서 뒤로가기(`시설 목록`)를 누르면 목록으로 돌아가고 건설 모드는 그대로인지 본다.
8. 상세가 열린 채로 B를 누르면 레일·카드·큰 창이 모두 닫히는지 본다.
9. `>>`를 다시 누르면(`<<`) 레일+작은 카드로 접히는지 본다.
10. 자원을 쓰거나 주운 뒤 보유/필요·충분/부족·설치 상태가 바로 맞는지 본다.
11. 1366×768, 1920×1080, 2560×1440 Game View에서 레일과 카드가 체력·미션 HUD를 가리지 않는지 본다.
12. Unity Console에 새 빨간 오류·노란 경고가 없는지 본다.

## 완료 체크리스트
- [ ] `MVP2-fix134` push 확인 — 승인 대기
- [ ] 커밋 해시 기재 — 승인 대기
- [x] Unity 컴파일 오류 없음 (빌드는 미검증)
- [x] EditMode 새 실패 없음 (main 기준 비교)
- [x] 기본이 레일+카드
- [x] `>>` 목록·검색
- [x] 상세·뒤로가기
- [x] B는 전체 닫기
- [x] 조작 안내 문구 없음
- [x] 새 SFX 없음
- [x] #276 브랜치를 이어 고치지 않음
- [x] main 직접 커밋 없음
- [x] PR 안 만듦

---

## 추가: 레일 호버 홀로그램 이름표 (2026-11-07)

### 요청
기본 레일(막대형)에서 시설 아이콘에 마우스를 올리면 오른쪽에 홀로그램 형태로 시설 이름만 간단히 보여 준다.

### 바꾼 파일
| 파일 | 내용 |
|---|---|
| `Scripts/App/UI/Building/BuildingRailHologramMotion.cs` (신규) | 순수 계산: 가로 펼침(0.15→1, 0.14초) · 펼치는 중 한 번 깜빡임 · 이름 글자 순서대로 찍기(0.2초) · 주사선 위치(1.4초 주기) · 꺼짐 0.08초 |
| `Scripts/App/UI/Building/BuildingRailHologram.cs` (신규) | 이름표 표시/숨김. 호버한 아이콘 오른쪽 가운데에 붙고 이름 길이에 맞춰 폭을 잡는다. `Time.unscaledDeltaTime`, ReduceMotion이면 연출 없이 바로 표시 |
| `Scripts/App/UI/Building/BuildingRailHoverRelay.cs` (신규) | 레일 아이콘의 PointerEnter/Exit를 View에 전달. 선택·클릭은 기존 `BuildingMenuEntryButton` 그대로 |
| `Scripts/App/UI/Building/BuildingMenuView.cs` | `railHologram` 필드, `ShowRailHover`/`HideRailHover`. 기본 레일 페이지에서만 띄우고 `>>` 펼침·닫기·초기화 때 즉시 지움. 호버 중엔 "시설을 고르세요" 힌트를 잠시 숨김 |
| `Editor/DataValidation/PromptB188BuildingModeBuilder.cs` | `PanelRoot/RailHoverHologram`(연결선·점·빛·불투명 판·청록 테두리·왼쪽 띠·줄무늬 2줄·주사선·이름) 생성, 레일 아이콘 9개에 Relay 부착. 모든 그래픽 raycast 끔 + CanvasGroup `blocksRaycasts=false` |
| `Prefabs/UI/BuildingMenu.prefab` | 위 빌더 재실행으로 갱신(다른 프리팹·Scene·폰트 변경 없음) |
| 테스트 | 신규 `PromptB188RailHologramTests`(EditMode 3건), `PromptB110BuildingMenuPlayModeTests`에 실제 마우스 호버 단계·캡처 2장 추가 |

### 테스트
- EditMode(건설창 관련 묶음): 42 통과 / 3 실패 — 3건(`G_F03`, `B35` 폭 580.8 ×2)은 main에도 있는 기존 실패. 새 실패 없음.
- PlayMode: `PromptB110BuildingMenuPlayModeTests`(Visual 포함), `Issue213WindowOpenMotionTests`, `BuildingUiPlayModeTests` 13 / 13 통과. 가상 마우스로 버팀목 위에 올려 이름표가 아이콘 오른쪽·같은 높이·화면 안에 뜨고 글자가 잘리지 않음, 이어서 같은 아이콘 클릭이 이름표에 막히지 않음을 확인.
- 캡처: `sub-terra/Temp/visual/prompt-b188/building-rail-hover-1920x1080.png`, `building-rail-hover-card-1920x1080.png`.
- Console: 컴파일 오류 0, 새 경고 없음(기존 `CoreCctvCameraRig` stereoTargetEye 경고만).

### 미검증
- 사람이 직접 Bootstrap → 광산까지 들어가 마우스로 호버하는 플레이 — **미검증**(자동 PlayMode 가상 마우스·캡처로만 확인).
- 1366×768·2560×1440에서의 호버 모습 — **미검증**(1920×1080만 캡처).

### 판단·남은 점
- 카드가 열린 상태에서 다른 아이콘을 호버하면 이름표가 카드 일부(설명·비용 머리말) 위에 겹친다. 일반 툴팁처럼 위에 그리는 것으로 두었다. 거슬리면 호버 중 카드를 살짝 흐리게 하는 방식으로 바꿀 수 있다.
- 새 소리는 넣지 않았다.
