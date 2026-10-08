# Prompt-B 140 — 게임 가이드 카드·상세 개편과 홀로그램 책 등장 연출

가이드의 UI·콘텐츠·연출만 바꿨다. 게임 조작·시스템·세이브·씬·프리팹·폰트·ProjectSettings·패키지는 바꾸지 않았다.

## 구조

| 역할 | 파일(`Scripts/App/UI/Guide/`) |
| --- | --- |
| 콘텐츠 정의(순수 데이터) | `GuideModels.cs`, `GameGuideCatalog.cs` — 카드 ID 기준. 탭·관련 안내·첫 탐사 단계가 모두 ID로 연결된다 |
| 카탈로그 값 읽기 | `GuideData.cs`(`IGuideData`/`GameDataGuideData`), `GuideDetailBuilder.cs` — 가격·무게·전력 요구·건설 재료를 카탈로그에서 읽어 상세 줄을 만든다 |
| 선택 상태(세션 한정) | `GameGuideState.cs` — 탭·카드·필터·첫 탐사 펼침, 영구 저장 없음, 플레이 시작마다 초기화 |
| 시간표(순수 계산) | `GameGuideTimeline.cs` — 등장(첫/재열기)·퇴장·끼어들기 복귀 |
| 시연 | `GuideDemoClip.cs`(키프레임 트랙), `GuideDemoLibrary.cs`(장면 38개 + 별칭 2개), `GameGuideStageView.cs`, `GameGuideSkin.cs`(스프라이트 참조) |
| 화면 | `GameGuidePopupView.cs`(+`.Build.cs`, `.Detail.cs`), `GameGuideCardView.cs`(공통 카드·단계 타일), `GameGuideButton.cs`, `GuideKeyCapView.cs`, `GameGuideBookFx.cs`, `GameGuideArt.cs` |
| 기존 호스트 | `HUD/GameGuidePanelView.cs`(플레이 중 새 창을 대신 연다, 기존 계약 유지), `HUD/HudPanelChromeController.cs`(X 버튼·최상위 팝업 판정 2곳) |

- 새 창은 B-136/B-138처럼 코드로 만든 팝업이다. 씬/프리팹의 `GameGuidePanel`은 그대로 두고, 플레이 중에는 예전 글 본문(`PanelRoot`)을 숨긴다. 프레임·패널·폰트는 `MineResetTimedPopupSkin`·`ResourceSell*` 도우미를 그대로 쓴다.
- 새 에셋: `Resources/UI/GameGuideSkin.asset`(기존 스프라이트 74개 참조, 메뉴 `SubTerra/UI/Build Prompt-B 140 Game Guide Skin`). 새 그림 파일은 없다.
- 기존 열기·닫기 경로 유지: G 키, 오른쪽 메뉴 가이드 버튼, X 버튼, X 키(최상위 팝업 닫기). 가이드는 이전처럼 게임을 멈추지 않고 창 영역만 입력을 막는다.

## 화면

- 제목 `SUB-TERRA 게임 가이드`, 오른쪽 위 X, 탭 3개(창 폭 균등 분할, 선택 = 어두운 청록 면 + 밝은 청록 테두리·하단선). 제목·X·탭·외부 프레임은 모든 탭·첫 탐사 접기/펼치기에서 위치·크기가 같다(테스트로 고정).
- 좌 60% 카드 목록(기본 조작·핵심 메커니즘 2열, 자원·시설 3열 아이콘 격자) / 우 40% 상세(큰 제목 + 키캡 → 시연 → 조작·설명 → 경고 → 팁 → 관련 안내). 목록과 상세는 서로 다른 `ScrollRect`이고 각자 중첩 Canvas라 한쪽이 다른 쪽을 다시 그리지 않는다.
- 첫 탐사 안내: 기본 조작 탭 상단의 접기/펼치기 카드(처음엔 펼침, 접으면 단계 아이콘·제목만). 단계를 누르면 관련 카드로 이동한다. 접기/펼치기는 0.2초 동안 목록·상세 영역만 다시 배치한다.
- 자원·시설 필터 전체/자원/시설. 관련 안내를 누르면 탭·카드 이동 + 목록 스크롤(필터에 가려진 카드면 필터를 '전체'로 푼다).
- 버튼: 평상시 진한 남색+흰 글자, 호버·포커스 밝은 청록+어두운 글자, 해제 0.12초 복귀(누적 없음).
- 카드를 바꿀 때는 상세만 0.14초 페이드하고 등장 연출은 다시 재생하지 않는다.

## 콘텐츠 검증 근거

| 항목 | 확인한 코드 | 가이드 문구 |
| --- | --- | --- |
| 이동 키·조작 방식 | `PlayerKeyboardControls`(WASD/방향키), `ControlPreferences`·`ControlSchemePanel`(기본: 둘 다 이동, 한쪽 선택 시 다른 쪽이 방향 채굴), `InputSystem_Actions`(Space=점프) | A D / ← →, W S / ↑ ↓, 조작 방식 설명 |
| 사다리 | `PlayerMovement`(닿으면 오르내림, 점프로 이탈), `PlayerFallDamageRules`(낙하 중 사다리 사용 시 피해 0), `Building_Ladder_Basic`(powerDraw 0) | **E 상호작용 없음**, 전력 불필요 |
| 엘리베이터 | `ElevatorController`(E, `ElevatorPromptResolver` '귀환'), `SaveRuntimeController.MineElevatorEnergyCost=0`; 지상→광산은 `SurfaceBaseBinder`→`TryStartExploration`('지하 탐사 시작' 버튼) | 광산에서는 E로 귀환, 지상에서는 버튼으로 진입 |
| 시설 상호작용 | `GameplayEventBridge.IsInteractionFacility`(충전기·보건소·보관함·정산 콘솔·코어), `EmergencyEscapePortal`, `OutpostService.TryValidateFacility`(근접 전력 시설 3종은 공급 범위 필요, 보관함은 불필요), `ItemDisplayNames.ShowsProximityName`, `FacilityUseCooldownSeconds` | 가까이에서 E, 재사용 대기(상수에서 읽음) |
| 채굴 | `PlayerMiningController`(마우스·Enter·방향 채굴, UI 위 클릭 제외), `MiningYieldCommit`(전력 소모, 화물이 못 받으면 `InventoryFull`), `MiningProgressHud` | 클릭/Enter/방향 채굴, 가득 차면 막힘 |
| 건설 | `GameplayBuildingPlacementBridge`(C=가까운 최적 칸 1회, 미리보기 칸 클릭도 설치), `HudPanelChromeController`(B 토글) | B → C |
| 미니맵 | `ExplorationMinimap.Update` — **Ctrl+M은 토글이 아니라 누르고 있는 동안 불투명**(`opaqueHold`), 기본 `PanelOpacity=0.5` | 문구 + 50%를 테스트가 상수와 대조 |
| 시계·초기화 | `MineResetClockOverlay`(T 토글), `SaveRuntimeController.IsMineResetSessionScene`(광산에서만 흐름), `MineResetService`(3시간·500G·2배·자동 초기화 시 500G 복귀), `LocalizationService`의 '초기화/유지' 문구 | 상수·게임 문구에서 읽음 |
| 가이드 열기 | `HudPanelChromeController`(G), `GameplaySideMenuController`(/), `UndergroundMenuController`(X) | G, 오른쪽 메뉴, X |
| 체력 | `PlayerSurvivalController`(낙석·낙하·가스), `OutpostService.TryHeal`(전체 회복·재사용 대기), `Upgrade_Health_Regeneration`, `RunFailureService`(화물 일부 손실) | 줄어드는 때·회복 방법 |
| 전력·구출 | `MiningYieldCommit`·`GasExposureEffectController`(소모), `OutpostService.TryCharge`, `SaveRuntimeController.OnSceneLoaded`(지상 도착 완충), `EmergencyRescueRuntimeController`(**R은 팝업만 열고 실행은 팝업 버튼**, 닫으면 머리 위 버튼), `EmergencyRescueService`(골드 최대 250·화물 80%, 보관함 제외) | 4단계 흐름 + 비용(상수에서 읽음) |
| 화물 | `InventoryService.TryAddMineralExact`, `CargoSpeedPolicy`·`CargoLoadEffectPolicy`, `OutpostService.TryDeposit/TryWithdraw` | 한도·적재 영향·비우는 방법 |
| 전력망·코어 | `GameplayEventBridge`(`IsWithinPowerSupplyRange`, 코어 `PowerSupplyRangeIndicator`), `OutpostService.IsProximityPoweredFacility`, `CoreCctvFacilityList`(활성 충전기·보건소·정산 콘솔만), `CoreCctvPopupView`(↑/↓) | 공급원·범위·CCTV 목록 |
| 구조 위험 | `StructuralIntegritySystem`(아래가 빈 천장, 위·아래 모두 비면 확정 붕괴, 점멸=예고, 버팀목은 위험 점수만 낮춤) | 점멸 칸은 피하고 미리 보강 |
| 가스 | `GasExposureEffectSettings`(이동 감속·시야·전력 소모·누적 한도 후 체력 피해/행동불능·회복), 코어 정화 지대(`shelteringOutpostIds`) | 정화 지대·가스 저항 |
| 자원 | `Mineral_*`·`RareItem_EngineFuel`(무게·가격·아이콘), `DataIds.RareItems.IsRare`, `OutpostService`(희귀 품목 콘솔 정산 거부), `SurfaceSellBackend`(최대 선택 제외), 광석·희귀 신호(`DroneAnalysisService`) | 값은 카탈로그에서 읽음 |
| 시설 | `Building_*`(전력 요구 `powerDraw`, `buildCosts`), `ItemDisplayNames` | 전력 필요/불필요·재료는 카탈로그에서 읽음 |

### 기존 문구에서 실제 동작에 맞게 고친 것
- 첫 단계 '엘리베이터로 광산에 진입' → 지상 기지의 **'지하 탐사 시작' 버튼**(광산에서 엘리베이터는 귀환용).
- 가스: '체력이 지속적으로 감소' → 이동 감속·시야 흐림·전력 감소, 누적되면 체력 피해/행동불능.
- 구조 위험: '버팀목으로 붕괴 취소' → 점멸 예고는 취소 경로가 없고, 버팀목은 점멸 전 위험도를 낮춘다.
- 사다리에는 E 키 상호작용이 없다(컨셉 이미지의 E 표기는 따르지 않음).
- 구출: R은 팝업을 다시 열 뿐이고 실행은 팝업 버튼이다.
- 전진기지 코어 CCTV 목록은 활성 충전기·보건소·정산 콘솔만이다(보관함·조명 아님).
- 탐사 실패 손실률 '30~50%'는 업그레이드로 달라지므로 '일부'로만 쓴다.
- '긴급 탈출 포탈': 요청 문서의 '포털' 대신 게임 코드(`ItemDisplayNames`·퀘스트 문구)의 '포탈'을 썼다.

## 미디어 구현 방식
- 새 영상 패키지·공용 설정 변경 없음. **실제 게임 스프라이트(플레이어 걷기·채굴·사다리 프레임, 타일, 시설, HUD 아이콘)를 키프레임으로 움직이는 무음 반복 장면**이다(`DemoClip` 38개 + 첫 탐사 별칭 2개). 키캡은 필요한 것만, 눌림은 색 변화로 표시.
- 목록 카드는 `ThumbTime`의 정지 장면만 그리고 재생하지 않는다. 재생은 상세의 단일 `GameGuideStageView`만 하며, 열림이 끝난 뒤 시작하고 카드·탭 변경·닫기 시작 때 즉시 멈춘다. 재생 버튼은 없다.
- 컨셉 이미지의 가상 장면은 쓰지 않았다. 장면은 모두 프로젝트 에셋으로 합성한다.
- **긴급 탈출 포탈**은 게임에 전용 아이콘이 없다(카탈로그 아이콘은 2x2 자리표시, 프리팹은 청록 외곽 사각형 + 남색 면). 같은 구성·색을 절차로 그려 썼다.
- 접근→입력→결과가 필요한 장면(채굴, 시설 사용, 건설 B→C, 구출 4단계 등)은 결과 순간을 `ThumbTime`으로 잡았다.

## 애니메이션 구현 방식
- 책 레이어(`GameGuideBookFx`)는 `ExpandRect` 하나를 중심·외곽으로 공유한다. 실제 금속 프레임 패널도 이 `ExpandRect`의 자식이라 책이 커지는 동안 같은 외곽으로 함께 커지고(9-slice), 책 페이지 면은 알파로 패널에 넘겨준다. 글자·썸네일은 별도 레이어에 최종 크기로만 나타나 늘어나거나 찌그러지지 않는다.
- 첫 등장 1.22초: 작은 책이 중앙 아래에서 떠오르며 중앙을 약간 지나쳤다가 되돌아와 정착 → 표지가 열림 → 페이지 3장 넘김(가장자리 청록 빛 이동) → 좌우 페이지가 넓어지며 프레임으로 변형 → 탭 → 목록 → 상세 순으로 표시.
- 재열기 0.60초: 페이지 1장, 짧은 상승. 닫기 0.40초: 내용 → 프레임이 펼친 책 크기로 축소 → 표지 닫힘·축소·소멸(페이지 넘김 반복 없음).
- 등장 중 닫기는 현재 값 이하로만 줄어드는 방식(`Close(u, from)`)이라 값이 튀지 않고, 닫는 중 열기는 현재 변형 진행에 맞는 시각부터 이어 간다. 창은 하나뿐이다.
- Tween 라이브러리·코루틴 없이 `Update`에서 `unscaledDeltaTime`(상한 0.05)으로 시간표를 진행한다. `timeScale`·카메라는 건드리지 않는다. 꺼질 때(`OnDisable`)와 파괴 시 재생·구독을 정리한다.

## 변경 파일
수정: `HUD/GameGuidePanelView.cs`, `HUD/HudPanelChromeController.cs`, `docs/CHANGELOG.md`, 테스트 3개(`GameGuidePanelTests`, `PromptB49FacilityVisualTests`, `PromptB94MinimapTests`: 글 본문 대신 카드 내용으로 같은 규칙을 대조).
신규: `Scripts/App/UI/Guide/*`(위 표), `Resources/UI/GameGuideSkin.asset`, `Editor/DataValidation/PromptB140GameGuideSkinBuilder.cs`·`PromptB140GameGuideTestRunner.cs`·`PromptB140GuideCaptureRig.cs`, 테스트 `PromptB140GameGuideTests`(EditMode 39) · `PromptB140GameGuidePlayModeTests`(PlayMode 9), `work_process/MVP2/guide-ui/*`.

## 검증
- 전용 EditMode 45개 통과(`PromptB140GameGuideTests` 39 + `GameGuidePanelTests` 6): 콘텐츠·키·구출 흐름·초기화·카탈로그 값 주입, 상태/필터/세션, 시간표(길이·바운스·페이지 수·순서·끼어들기), 시연 스프라이트 존재, 폰트 글리프, 창 구조(탭 3개·상단 고정·스크롤 분리·글자 맞춤·정리).
- 전용 PlayMode 9개 통과(실제 `Mine_Demo_Integration`, 실제 G·X 키와 마우스 클릭): 열기/닫기 경로, 첫/재열기 시간, 등장 중 닫기·12회 연타(창 1개, 재생 시연 1개, 닫은 뒤 입력 비차단), 정지 상태(`UiPauseGate`)에서 연출 진행·`timeScale` 불변, 탭/카드/필터/첫 탐사 접기/관련 링크 클릭, 목록·상세 스크롤 독립, 실제 카탈로그 값(가격 10G·무게 1.5·보관함 전력 불필요·충전기 전력 필요), 1280x720·1920x1080·2560x1440 화면 안 배치·글자 맞춤.
- 전체 EditMode 1372 중 1370 통과. 실패 2개는 이번 변경과 무관: `IntegrationWiringTests.PlayableMineScene_HasRequestedCharacterTerrainAndMiningLayout`(이전 문서에 기록된 기존 실패), `PromptB138StoragePopupTests.Open_Pops_GrowToPeak_ThenFlyToListIconShrinking`(부동소수 오차 9.5e-7, 보관함 시간표 테스트).
- 전체 PlayMode(`SubTerra.App.Tests.PlayMode`) 126 중 123 통과·1 건너뜀(`LadderBack…`, 격리 세이브 인자 없음)·2 실패 무관: `GasExposurePlayModeTests.H_F02_F05…`(기존 기록 실패), `PromptB138StoragePopupPlayModeTests.Storage_EmptyCargoAndStorage_KeepsBoxFlowWithoutPops`(보관함 팝업 텍스트 레이아웃 검사). 두 실패 모두 가이드 코드 경로를 쓰지 않으며 변경 전 트리와 직접 대조하지는 못했다.
- 전체 실행 중 에디터가 다른 프롬프트의 프리팹·씬·ProjectSettings·폰트·증거 PNG를 다시 저장했고, 모두 `git restore`로 되돌렸다. `Mine_Demo_Integration.unity`·프리팹·폰트·ProjectSettings 변경은 없다.

## 캡처 (`work_process/MVP2/guide-ui/`)
- `evidence/guide-1-controls-expanded.png`(펼친 첫 탐사 안내), `guide-2-controls-collapsed.png`, `guide-3-mechanics.png`, `guide-4-resources.png`, `guide-5-facilities.png`(필터 '시설')
- `evidence/guide-res-1280x720.png`, `guide-res-1920x1080.png`, `guide-res-2560x1440.png`
- `guide-open-close.gif` — 실제 게임 화면에서 등장(책 → 창)과 퇴장 30fps 고정 진행 캡처
- `guide-demo-clips.gif` — 상세 시연 4종(채굴, 사다리, 엘리베이터, 미니맵) 재생

## 미검증·제한
- 새 창은 예전 가이드 창의 **드래그 이동**을 제공하지 않는다(예전 `PopupWindowDrag`는 숨긴 `PanelRoot`에 붙어 있었다. B-136/B-138 팝업과 같은 방식).
- 실제 마우스 휠 입력은 테스트하지 않았다(`ScrollRect` 값을 직접 바꿔 목록·상세 독립만 확인). 키보드 포커스 강조는 `OnSelect` 경로만 단위 확인했고, 버튼은 키보드 내비게이션을 쓰지 않는다(Enter가 채굴 키이므로 기존 가이드 규칙과 같다).
- 시연의 플레이 장면은 게임의 실제 스프라이트 합성이지, 요청서가 말한 '2D 픽셀 그래픽'의 실제 플레이 녹화 클립이 아니다(게임 스프라이트는 카툰풍 고해상도). 실제 플레이 녹화로 바꾸려면 별도 캡처 에셋이 필요하다.
- 탐사 실패 손실률, Digger-Bot 판단 기준 등 업그레이드·데이터에 따라 달라지는 수치는 문구에서 뺐다. 미니맵 기본 투명도 50%만 코드 상수(`ExplorationMinimap.PanelOpacity`)와 테스트로 대조한다(App 어셈블리에서 Integration 어셈블리를 참조할 수 없어 문구 쪽은 상수 복사).
- Windows 빌드에서의 실행(동적 폰트 글리프 생성 포함)은 확인하지 못했다. 커밋은 하지 않았다.
