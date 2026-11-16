# B-198 HUD 전력 게이지 아래 산소 게이지 결과 (#342)

- 기준: `origin/main` `e9c9ee8be8f04fd074165111928e1f12d02acd51` (#338 산소 기본 병합)
- 브랜치: `MVP2-fix142` (원격에 없음을 확인하고 새로 만듦)
- 커밋: 기능 커밋 + 통합 씬 별도 커밋 2개 (해시는 PR 커밋 목록 참조). 사용자 요청으로 PR까지 올림.

## 바꾼 파일
| 파일 | 내용 |
| --- | --- |
| `Scripts/Gameplay/Player/PlayerSurvivalController.cs` | 읽기 전용 `OxygenRemaining`/`OxygenMaximum`, 이벤트 `OxygenChanged(현재, 최대)` 추가. 값이 바뀔 때(소모, 구조 후 `Refill`)만 발행 |
| `Scripts/App/Integration/IntegrationRuntimeBinder.cs` | `BindOxygen`에서 `OxygenChanged` 구독 → `BasicHud.SetOxygenLevel`. `OnDestroy`에서 해제. 상태 줄 「산소 위험도」 경로는 그대로 |
| `Scripts/App/UI/HUD/BasicHudView.cs` | `oxygenGauge`/`oxygenText` 필드, `SetOxygenLevel`, `ApplyCompactLayout`에 산소 줄과 아래 요소 이동 반영 |
| `Scripts/App/UI/HUD/HudFormatter.cs` | `FormatOxygen` (현재/최대 문자열, 범위 보정) |
| `Editor/DataValidation/PromptB198HudOxygenGaugeBuilder.cs` | `SubTerra/UI/Build Prompt-B 198 HUD Oxygen Gauge`. `BasicHUD.prefab`만 대상. 전력 줄을 복제해 산소 줄 생성(여러 번 실행해도 중복 안 생김) |
| `Prefabs/UI/BasicHUD.prefab` | 빌더 결과 (`OxygenGauge`, `OxygenText`, `OxygenLabel`). 손으로 YAML 수정 안 함 |
| `Art/UI/Gameplay/HUD/oxygen-bar.png(.meta)` | `energy-bar.png`를 흑백화하고 밝기를 올린 채움용 스프라이트 (알파·모양 동일) |
| `Tests/EditMode/App/UI/PromptB198HudOxygenGaugeTests.cs` | 새 EditMode 테스트 8개 |

## 수정: 플레이 시작 시 화면 전체가 하얗게 나오던 문제
- 증상: 이어하기로 광산에 들어가면 월드 전체가 흰색/밝은 회색으로 덮이고 HUD만 보였다(탐사 기록 01 썸네일도 흰색으로 저장됨).
- 원인: 씬 좌표 수정 뒤 EditMode 전체를 다시 돌렸는데, 퀘스트 빌더 테스트가 통합 씬을 다시 저장하면서 런타임 UI(시계·모트·`Stage`·`FxFront` 등)가 씬에 박혔다. 씬 diff가 +110,281/−35,401줄이었다. 코드(산소 게이지·아이콘·레이아웃)가 원인이 아니다.
- 조치: 씬을 `git restore`한 뒤 에디터 API로 위치 3개(퀘스트 -268, NextAction -418, 건설창 -396)만 다시 적용했다. 씬 diff는 +32/−6으로 돌아왔다(Unity 직렬화 기본값 정리뿐).
- 확인: 같은 슬롯으로 이어하기 → 월드·플레이어·HUD(O2 게이지 포함) 정상 렌더, 퀘스트 카드·HUD 겹침 없음. 플레이 전에 세이브를 백업했고, 플레이 뒤 자동 저장된 슬롯 1을 원본으로 되돌렸다(그래서 슬롯 1 썸네일은 다음 저장 때까지 흰색으로 보일 수 있다).
- 교훈 파일: `init/rules/lessons/2026-11-16-issue342-editmode-run-saves-integration-scene.md`

## 후속: 산소 게이지 왼쪽 「O2」 아이콘
- `Art/UI/Gameplay/HUD/oxygen-icon.png(.meta)` 신규 (192×128, 투명 배경). Arial Black 「O2」(2는 작게 아래), 어두운 외곽선 + 은은한 글로우, 위쪽이 밝고 아래가 살짝 회색인 탁한 흰색. 기존 `hud-icons.png` 아틀라스는 건드리지 않았다.
- 프리팹 `OxygenIcon` (`UnityEngine.UI.Image`, `preserveAspect`, Raycast Target 끔). 색 `RGBA(0.86, 0.88, 0.90, 0.90)`로 산소 게이지와 같고, Image Color에서 조절한다. `HudIcon1`(전력) 바로 뒤 형제로 들어가 프레임 위에 그려진다.
- 좌표(`ApplyCompactLayout`): x=14, y=90, 36×24. 중심 x=32로 체력·전력 아이콘(24px, x=20)과 같은 열이고, 게이지보다 2px 아래인 것도 같다. 라벨(x=52)과 겹치지 않는다.
- 빌더 `PromptB198HudOxygenGaugeBuilder`가 아이콘도 만든다(여러 번 실행해도 중복 안 생김). 테스트 `OxygenIcon_SitsLeftOfOxygenGaugeAlignedWithOtherIcons` 추가.
- 렌더 확인(미리보기 씬, 3840×2160): 체력·전력 아이콘과 같은 열에 놓이고 라벨·게이지와 겹치지 않음. 실제 플레이 확인은 **미검증**.
- 참고: 산소 게이지 위 수치(흰색 글자)가 밝은 회색 채움 위라 대비가 체력·전력보다 약하다. 이번 요청 범위가 아니라 바꾸지 않았다.

## 후속: 좌측 퀘스트·건설창을 HUD 늘어난 만큼 아래로 (+36px)
기준 상수 `BasicHudView.BelowHudY = 12 + FrameHeight(244) + 12 = 268`.

| 요소 | 이전 | 이후 | 위치 |
| --- | --- | --- | --- |
| `QuestSummaryButton` | y=-232 | **-268** | `DemoObjectiveView.ApplyCompactSummary`(런타임 덮어씀), `PromptB107QuestUiBuilder.SummaryCardY`, 씬 |
| `NextAction` (비활성 레거시) | y=-382 | **-418** | 씬 |
| `BuildingPanel` | y=-360 | **-396** (`BelowHudY + 128`) | `DesktopVisualPolishBuilder`, 씬 |

- 값을 HUD 높이에서 파생시켜 이후 HUD 높이가 바뀌어도 같이 움직인다.
- 건설 패널 높이 560 → 하단 956px로 1080 안에 들어간다.
- 씬은 에디터 API로 수정·저장했다(YAML 직접 수정 없음). **저장 시 Unity가 무관한 직렬화 필드도 다시 써서(약 32줄) 씬 diff에 섞여 있다.** 규칙대로 `Mine_Demo_Integration.unity`는 기능 커밋과 분리해 커밋해야 한다.
- `PromptB110BuildingMenuTests`의 메시지 문구만 (16,-396)으로 맞췄다. 단언 값은 그대로.
- 퀘스트 위치 관계 단언(카드가 HUD 아래, 카드 하단이 건설 패널 위)은 새 값에서도 성립한다. 해당 테스트(`Scene_UsesQuestFramesWithoutCoveringTheFacilityPanel`)는 씬이 dirty일 때 빌더가 예외를 던져 기존부터 실패 중이라 실제 실행은 못 했다 → 미검증.
- EditMode 재실행 2회: 총 실패 35. 실행마다 새로 실패하는 테스트가 달라져(플레이어 애니메이션 프레임, 폰트 글자, 호버) 불안정한 테스트로 보인다.

## 설계 결정
- **산소 값 접근**: `PlayerSurvivalController.OxygenRemaining/OxygenMaximum` + `OxygenChanged`. Safe 구간에서도 소모를 따라가도록 위험도 이벤트가 아니라 값 변경 이벤트를 쓴다. 소모 중엔 프레임마다 발행되지만 구독자는 게이지 값 갱신뿐이다. #339 API는 쓰지 않았다.
- **채움**: 전력과 같은 `HudGaugeView.SetValue`(감소 0.5초 보간, 증가 즉시). `EnableDamageTrail`은 호출하지 않는다.
- **색 (최종값)**: `Fill` 색 `RGBA(0.86, 0.88, 0.90, 0.90)`. `OxygenGauge/Fill`의 Image Color에서 조절한다. 전력 그림은 청록이 구워져 있어 색을 곱하면 회색이 안 되므로, 흑백 스프라이트를 따로 만들어 틴트만 곱하게 했다. 유리 광택은 유지된다.
- **산소 아이콘 없음**: `hud-icons.png`에 산소 아이콘이 없어서 라벨 「산소」 + 게이지 + 수치만 만들었다. 왼쪽 아이콘 자리(x=20)는 비어 있다.

## `ApplyCompactLayout` 새 좌표 (HUD 로컬 px, 위가 0)
| 요소 | 이전 | 이후 |
| --- | --- | --- |
| 루트 / `HudFrame` | 380×208 | **380×244** (`BasicHudView.FrameHeight`) |
| `HealthGauge` / `EnergyGauge` | y=16 / 52 | 그대로 |
| `OxygenGauge` (258×32, x=106) | - | **y=88** (줄 간격 36 유지) |
| `OxygenText` / `OxygenLabel` | - | y=88 / y=89 |
| `TopSeparator` | y=89 | y=125 |
| 깊이·화물 / 골드·미정산 (텍스트·아이콘) | y=96/126 (아이콘 97/127) | y=132/162 (아이콘 133/163) |
| `ColumnSeparator` | y=96 | y=132 |
| `BottomSeparator` | y=158 | y=194 |
| `StructuralStatusRow` | y=164 | y=200 |

세로 확장량은 `BasicHudView.OxygenRowOffset = 36`.

## 프레임 아트 (사용자 작업 요청)
- 9-slice 안 함. `HUD-frame.png`는 Image Type **Simple**이라 세로로 늘어난다. 임시로 늘어져 보인다.
- 새 그림이 필요한 크기: 현재 1635×930 (380×208 비율) → 새 비율 380×244 기준 **1635×1050**.
- 게이지가 들어가는 윗 영역(위 380×244 기준 → 1635×1050 환산, 배율 4.30): 체력/전력/산소 3줄은 x 106~364 → **약 456~1566px**, y 16~120 → **약 69~516px**. 그 아래 구분선은 y=125(약 538px).
- 교체할 때는 같은 이름 `HUD-frame.png`로 덮어쓰면 된다. 코드·좌표 변경 없음.

## 테스트
- EditMode 전체: Pass 2713 / Fail 33 (플래그 러너, 67초). **새 `PromptB198HudOxygenGaugeTests` 8개 전부 통과.**
- 컴파일 오류 0건(기존 obsolete 경고만).
- 실패 33건 중 HUD와 닿는 3건은 이번 변경과 무관해 보인다(베이스라인 `main`에서 실행해 확인하지는 않았다 → **미검증**):
  - `HudStaticStructureTests.PromptB106_HealthAndEnergyRowsMatchGaugeAlignmentAndSpacing`: 체력↔전력 글자 간격을 45로 기대하지만 main의 `ApplyCompactLayout`은 이미 36.
  - `StructuralStatusHudTests.BasicHud_LegacyPowerIconRemoved_AndRowSitsInsideFrameBelowSeparator`: HUD 크기 430×248을 기대하지만 main은 이미 380×208. 이번에는 380×244.
  - `StructuralStatusHudTests.StructuralRow_LongestLabel_FitsWithoutClipping`: 라벨 높이 28.97 > 28, 산소와 무관.
  - 나머지(오디오, 건물 메뉴, 터미널, 업그레이드 창 등)는 산소·HUD 게이지와 무관.
  - 두 번째 테스트(430×248)는 이미 낡은 기대값이라, 이 PR에서 고칠지 PM과 정하는 게 좋다.
- PlayMode 테스트: **미실행**.
- 시각 확인: 미리보기 씬에서 프리팹을 렌더링해 산소 게이지가 전력 바로 아래, 회색 반투명 톤, 겹침 없음을 확인함. **실제 Bootstrap 플레이에서 확인한 것은 아님 → 미검증.**

## 미검증
- 실제 플레이에서 산소 감소 속도·보간·구조 후 회복 표시
- 1920×1080, 1280×720, 16:10 해상도별 프레임 잘림 (CanvasScaler는 기존 그대로)
- 가스 경고·체력 잔상·화물 표시의 플레이 중 회귀
- Console 새 오류 (컴파일 단계에서는 없음)

## 사용자 테스트 가이드
1. Bootstrap에서 Play → 광산 진입. 전력 게이지 바로 아래에 「산소」 게이지가 있고 겹치지 않는다. 상태 줄 「산소 위험도」도 그대로 있다.
2. 깊이 내려가거나 오래 머문다. 지금 main은 0M부터 산소가 줄어든다. 게이지·수치(예: `74 / 100`)가 줄어든다.
3. 거의 빌 때까지 가서 게이지가 비고 위험도가 주의→위험으로 바뀌는지 본다. `O2` 디버그는 #345 이후.
4. 1920×1080, 1280×720, 16:10에서 프레임이 잘리거나 게이지가 밖으로 나가지 않는지 본다. 프레임이 늘어져 보이는 것은 아트 교체 전이면 정상.
5. 체력/전력 게이지, 화물·골드, 가스 경고가 그대로이고 Console에 새 빨간 오류가 없는지 본다.
