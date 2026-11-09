# B-190 인벤토리 UI: 화물칸 명세표 배치 결과 (#268 / #275)

- 기준 브랜치: `codex/issue-268-inventory-readability`
- 작업 시작 HEAD: `bdeac16e` (PM이 main `5db79706`을 합쳐 둔 커밋)
- main merge 여부: **추가 merge 없음.** `git fetch` 후 `origin/main`(`5db79706`)이 이미 이 브랜치의 조상이라 merge할 것이 없었다. 충돌 없음.
- 커밋 해시: `c11535c5` (기능), 이 보고서 해시 기재는 후속 docs 커밋
- 새 PR: 만들지 않음. 기존 #275 갱신 대상.

## 바꾼 파일·줄 (작업 후 기준, 근사)

| 파일 | 내용 |
| --- | --- |
| `Editor/DataValidation/PromptB112InventoryPanelBuilder.cs` | 상수(~22-80): 행 높이 60, 열 오른쪽 끝, 스탬프 크기. `BuildHeader`(~176) 가운데 제목·`HotkeyHint` 제거. `BuildCargoCard`(~226) 가로 적재 현황. `BuildValueCard`(~275) 미정산 스탬프. `BuildListHeader`(~322) 적재 내역·종류 수·표 머리·빈 안내. `BuildRow`(~457) 명세표 행. `RemoveFooter`(~579) 하단 안내 제거. `StyleWeightHelp`(~586) `?` 위치 이동 |
| `Prefabs/UI/InventoryPanel.prefab` | 위 빌더로만 재생성(손 YAML 수정 없음) |
| `Scripts/App/UI/Inventory/InventoryPanelDisplayFormatter.cs` | `StateLine`(~76) 순서 `남은 적재량 N · 상태`. `UnitWeight`(~111) 숫자만. `LineWeight`(~117) 수량×개당 무게 추가. `LoadedKinds`(~127) `N종`. `QuantityState`·`OwnedKinds` 제거 |
| `Scripts/App/UI/Inventory/InventoryStackRowView.cs` | 수량 0이면 행 숨김(~29). `lineWeightText`(~70). 보유/미보유 상태 문구·강조 막대 제거 |
| `Scripts/App/UI/Inventory/InventoryPanelView.cs` | `columnHeaderRoot`(~30). `SetStacks` 실린 종류 수·빈 안내·표 머리 토글(~134-156) |
| `Tests/EditMode/App/UI/PromptB112InventoryPanelTests.cs` | 새 배치 검사(스탬프 위치·크기, 표 머리 4열, 숫자 열 오른쪽 끝 정렬, 행 높이, 약 4행, 하단 안내 없음, 0행 숨김·빈 안내) |
| `Tests/EditMode/App/UI/PromptB68CargoWeightTests.cs` | `?` 겹침 기준을 숨김 `CargoSummaryText` → 보이는 `적재 현황` 제목으로 변경 |
| `Tests/PlayMode/.../PromptB112InventoryPanelPlayModeTests.cs` | `보유 없음` 행 검사 → 0행 숨김·이 줄 무게 검사 |
| `Tests/PlayMode/.../Issue268InventoryReadabilityTests.cs` | 빈 안내 문구, 수량 열 폭 84, 보이는 행만 이름 2줄 검사 |
| `init/rules/lessons/2026-11-09-issue268-cross-prompt-layout-tests.md` | 교훈 1건 |

## 배치

창 크기 560×532와 위치(오른쪽 오프셋 330)는 그대로 두었다. `SurfaceBasePanel.prefab`이 루트 크기 560×532를 덮어쓰고 있어서, 크기를 바꾸면 지상 기지 창만 달라진다.

- **헤더**: `인벤토리`를 가운데에 두고 아래에 작은 `CARGO INVENTORY`, 양옆에 가는 선을 그었다. `[I] 열기 / 닫기` 문구는 지웠다. 조작 안내는 가이드에만 둔다는 규칙 5에 따른 것이다.
- **A. 적재 현황(`CargoCard`)**: 가로 528 전체를 쓴다. 왼쪽 열(폭 360)은 위에서부터 이렇게 읽힌다.
  - `적재 현황` 제목과 `?`
  - `현재 / 최대`(32px)와 같은 줄 오른쪽의 `%`(24px)
  - 게이지(80% 기준선 유지)
  - `남은 적재량 N · 적재 여유 / 적재 한계 임박` 또는 `적재량 가득 참`
  - 색은 기존 판정을 따른다(여유 청록, 임박 주황, 가득 참 빨강).
- **미정산 스탬프(`ValueCard`)**: 적재 현황 안쪽 오른쪽 끝에 124×92로 둔다(앰버 테두리). 줄은 `미정산 가치` / 금액 / `정산 시 받을 금액` 순이다. 금액은 최대 24px이고 13px까지 자동으로 줄어서 `1,234,567`도 칸 안에 들어간다.
- **B. 적재 내역(`ListFrame`)**: 제목 `적재 내역` 옆에 `N종`을 둔다.
  - 표 머리(`ColumnHeader`)는 `자원 | 개당 무게 | 수량 | 이 줄 무게`이고 아래에 규칙선을 긋는다.
  - 행은 아이콘 40×40(x=12), 이름(x=62, 폭 142, 긴 이름만 2줄·자동 축소), 숫자 열 3개 순이다. 숫자 열은 오른쪽 정렬이고 오른쪽 끝이 284, 380, 496이며 표 머리와 일치한다.
  - 행 사이는 규칙선으로 나눈다.
- **행 높이와 보이는 행 수**: 행 높이 60, 간격 2, 목록 높이 252라서 한 화면에 약 4.1행이 보인다. 지금 카탈로그 4종은 스크롤 없이 모두 보인다.
- **빈 목록**: 수량 0 행은 숨긴다. 실린 자원이 없으면 표 머리도 숨기고 제목 아래에 `적재된 자원이 없습니다`만 보인다.
- **C. 하단**: `ControlsHint`와 `ControlsDivider`를 지웠다.
  - `?`는 `적재 현황` 제목 바로 오른쪽으로 옮겼다.
  - 도움말은 창 아래(-540)에 그대로 띄우므로 숫자를 덮지 않는다.
- **유지한 것**: 계산(서비스 스냅샷 값 그대로), TV 열림·닫힘 연출, SFX, I 키, X 버튼.
  - `이 줄 무게`는 표시 전용 곱셈이고 적재 판정에는 쓰지 않는다.

## 테스트 결과

- EditMode(에디터 TestRunnerApi로 필터 실행: `Inventory`, `PromptB68CargoWeightTests`, `HudFormatter`, `PromptB182`): **74 통과 / 0 실패**
  - 처음 실행에서는 `PromptB68CargoWeightTests.InventoryPanel_HasVisibleWeightHelpAndHoverDescription`이 실패했다. `?`를 옮긴 뒤에도 옛 요소를 기준으로 검사하고 있었기 때문이다. 기준을 고친 뒤 통과했다(교훈 기록).
- PlayMode `PromptB112InventoryPanelPlayModeTests.InventoryPanel_LiveValuesStatesAndLayout`: **통과**. 실제 Integration Scene에서 I 열기, 실시간 갱신, 이 줄 무게, 임박·가득 참, 닫고 다시 열기, X 닫기를 확인했다.
- PlayMode `InventoryPanel_StatesAndLayout_AtThreeResolutions`(Visual): **통과**. 1366×768, 1920×1080, 2560×1440에서 화면 안에 들어오고, 잘린 글자가 없고, HUD와 겹치지 않는다. 캡처는 `sub-terra/Temp/visual/prompt-b112/`에 있다.
- 프리팹 미리보기 렌더(보통 / 긴 이름·큰 값 / 빈 목록 / 가득 참): `sub-terra/Temp/visual/prompt-b190/`. 잘린 글자 0건.
- Unity 컴파일 오류 0건. 새 경고 없음(기존 CS0618 경고만 있음).
- 전체 EditMode·PlayMode 일괄 실행은 하지 않았다. 범위 밖 에셋이 dirty 되는 것을 피하려고 관련 묶음만 실행했다.

## 미검증

- `Issue268InventoryReadabilityTests`: `-subterra-save-root` 명령줄 인자가 필요한 전용 실행이라 에디터에서 돌리지 않았다. **미검증.**
- 직접 플레이 중 **채굴·버리기**로 열린 창이 바로 갱신되는지: 테스트에서 서비스 API로 바꾼 경우만 확인했다. **미검증.**
- **지상 기지** 창 직접 확인: 같은 프리팹이고 루트 크기 오버라이드(560×532)도 그대로라 같은 배치일 것으로 보지만, 실제 화면은 **미검증.**
- Windows 빌드: **미검증.**

## 참고(범위 밖, 손대지 않음)

- 컨셉 이미지의 `희귀도순` 정렬 드롭다운과 `자원 이름 검색` 칸은 프롬프트 명세에 없어 넣지 않았다(기능 추가 범위).
- 작업 전부터 dirty였던 파일은 커밋하지 않았다: `NotoSansKR-Regular_SDF.asset`(동적 아틀라스, 렌더 중 글리프가 더 추가됨), `LiberationSans SDF - Fallback.asset`, URP·GraphicsSettings, `init/prompt-B.md`, `Assets/Resources*`.

## 사용자 테스트 가이드 (직접 플레이)

1. I로 인벤토리를 열면 가운데 팝업이 뜨는지 본다. 맨 위에 적재량(현재/최대, 게이지, 남은 양)이 먼저 보이고, 미정산 금액이 그보다 작은 스탬프로 보이는지 본다.
2. 자원이 있을 때 표 머리가 `자원 | 개당 무게 | 수량 | 이 줄 무게`인지, 한 화면에 4개쯤 보이는지 본다.
3. 각 줄의 `이 줄 무게`가 (수량 × 개당 무게)와 같은지 본다.
4. 수량 0인 자원은 목록에 없는지 본다. 아무것도 없으면 `적재된 자원이 없습니다`만 보이는지 본다.
5. 채굴하거나 자원을 버리면 열린 창의 적재량, 목록, 이 줄 무게, 미정산 금액이 바로 바뀌는지 본다. 닫았다 다시 열어도 최신 값인지 본다.
6. 하단에 `I 닫기` 같은 조작 안내가 없는지 본다. I와 닫기 버튼으로 창이 닫히는지 본다.
7. `?`에 마우스를 올렸을 때 무게 도움말이 숫자를 가리지 않는지 본다.
8. 적재가 거의 가득 차면 `적재 한계 임박`, 가득 차면 `적재량 가득 참`이 게이지와 함께 보이는지 본다.
9. 지상 기지와 광산에서 같은 창인지 본다. 1366×768 / 1920×1080 / 2560×1440에서 창과 도움말이 화면 밖으로 나가지 않는지 본다.
10. Unity Console에 새 빨간 오류나 노란 경고가 없는지 본다.

## 완료 체크리스트

- [x] `codex/issue-268-inventory-readability` push 확인
- [x] 커밋 해시 기재
- [x] Unity 컴파일·빌드 오류 없음 (컴파일만 확인했고 빌드는 미검증)
- [x] 기존 인벤토리 테스트를 새 배치에 맞게 수정
- [x] 상단이 적재 현황 + 작은 미정산 스탬프
- [x] 표 열이 자원·개당 무게·수량·이 줄 무게
- [x] 한 화면에 약 4행(4.1)
- [x] 수량 0 행 숨김, 빈 안내 `적재된 자원이 없습니다`
- [x] 하단 조작 안내 없음
- [x] `?`가 숫자를 가리지 않음
- [x] 계산·TV 연출·SFX 미변경
- [x] 새 PR 안 만듦
- [x] main 직접 커밋 없음
