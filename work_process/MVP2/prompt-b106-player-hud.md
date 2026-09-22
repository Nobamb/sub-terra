# Prompt-B 106 — 플레이어 HUD

## 적용 내용

- 제공된 HUD 프레임과 체력/전력의 Empty → Fill → Frame 레이어로 좌측 상단 HUD 구성.
- 체력·전력 수치는 TextMeshPro로 표시. 깊이·골드·화물·미정산 가치·전력망은 기존 데이터와 바인딩을 유지.
- 게이지는 Horizontal Filled Image, Left origin으로 오른쪽부터 잘라낸다. 감소는 unscaled time 기준 0.5초 선형 보간, 회복/최초 표시 즉시 반영.
- 감소 도중 새로운 값이 오면 현재 표시 비율부터 다시 보간. 비활성화 후 재활성화 시 현재 수치로 초기화.
- 투명 배경의 체력·전력·깊이·골드·화물·미정산·전력망 아이콘 7종을 한 아틀라스로 생성하고 RawImage UV로 참조.
- HUD 아래 건설 선택 안내를 유지하고, 상호작용 안내는 HUD 오른쪽에 배치하여 목표 패널과 겹치지 않게 함.

## 수정/생성 파일

Unity 경로의 기준은 `sub-terra/Assets/_Project/`.

- `Prefabs/UI/BasicHUD.prefab`
- `Scenes/App/Mine_Demo_Integration.unity` — HUD와 기존 전력망 문구의 레이아웃 override만 적용.
- `Scripts/App/UI/HUD/BasicHudView.cs`, `HudBinder.cs`, `HudPresenter.cs`, 신규 `HudGaugeView.cs`
- `Art/UI/Gameplay/HUD/` — 원본 제공 PNG, 참조 이미지 사본, 신규 `hud-icons.png`, 각 .meta
- `Editor/DataValidation/PromptB106HudBuilder.cs`, `PromptB106HudTestRunner.cs`, 각 .meta
- `Tests/EditMode/App/UI/PromptB106HudTests.cs`, 기존 `HudStaticStructureTests.cs`
- `Tests/PlayMode/Integration/Bootstrap/PromptB106HudPlayModeTests.cs`, 신규 테스트 .meta

## Inspector 및 재적용

- 이미 Editor API로 프리팹과 통합 씬에 적용됨. 수동 배선 불필요.
- `BasicHudView.energyGauge/healthGauge` → 각 HudGaugeView.
- `HudGaugeView.fill` → Empty와 Frame 사이 Fill Image.
- 기존 HudBinder 및 HazardHudView.powerText 참조 보존.
- 재적용 메뉴: `SubTerra/UI/Build Prompt-B 106 Player HUD`.
- EditMode 테스트 메뉴: `SubTerra/Tests/Run Prompt-B 106 Player HUD Tests`.
- PlayMode 테스트: Test Runner에서 `PromptB106HudPlayModeTests` 실행.

## 검증 결과

- EditMode 20/20 통과: 기존 Presenter/Formatter, 바인딩 구조, 신규 레이어·참조·수치 이벤트 테스트.
- PlayMode 1/1 통과: 초기값, 즉시 잘리지 않는 감소, 0.5초 완료, 감소 중 재목표 설정, 회복, 비활성/재활성, 0 최대값.
- 이전 체력 행 간격 테스트를 45px 게이지 행 간격으로 변경.
- 기존 폴더 전체 Update 금지 검사는 상태 바인딩/텍스트 소스 대상으로 한정. 입력, 시계 및 게이지 애니메이션을 상태 폴링으로 오인하지 않게 수정.
- Bootstrap 재생 후 런타임을 유지한 통합 씬 HUD 검증: 활성 상태, 필수 참조 true, 계약 연결 true, 전력 49/100에 fillAmount 0.49 확인.
- IntegrationRuntimeBinder: hudCanvasGroup/hudBinder 참조 존재, deferredInputBehaviours 2개. HUD 게이지 2개, Missing Script 0개.
- 최종 C# 컴파일 성공. 스모크에서 게임 코드 예외 없음.
- `git diff --check`의 경고는 Unity가 직렬화한 빈 YAML 값 뒤 공백이며 C# 공백 오류 없음.
- 변경 범위 밖 메뉴/설정/인벤토리 프리팹, 폰트, ProjectSettings의 최종 변경 없음.

## 제한

- 기존 슬롯 3개가 모두 사용 중이므로 새 게임으로 덮어쓰지 않았다. activeSlot=0 상태에서 HUD만 검증하여 세이브를 쓰지 않음.
- 새 게임 → Surface Base → 탐사 전체 흐름, Windows 실행 파일 빌드는 이번 UI 변경에서 재검증하지 않음.
- Save DTO, Shared 계약, Gameplay 코드 변경 없음.
- Play Mode 종료 후 Bootstrap 편집 상태로 복귀.

## 아이콘 생성 기록

Built-in image_gen 사용. 최종 프로젝트 파일: `sub-terra/Assets/_Project/Art/UI/Gameplay/HUD/hud-icons.png`.

사용 프롬프트:

> Create a production game UI ICON SPRITE SHEET PNG with genuinely transparent alpha background, no backdrop, no labels/text. Exactly 7 icons in one horizontal row with equal square cells, clean space separating cells, all same visual scale, centered in each cell. From LEFT to RIGHT: glossy RED heart health icon, luminous CYAN lightning bolt energy icon, CYAN downward arrow depth icon, GOLD stacked coins icon, CYAN sci-fi cargo crate icon, CYAN square ledger/calculator unsettled value icon, CYAN electric plug/network connection icon. Industrial sci-fi mining HUD style, crisp beveled small game icons with restrained cyan edge light, readable at 24px. 7 equal cells in a wide 7:1 image, transparent margins in every cell. This is one texture atlas intended for Unity sprite slicing. No frames, no extra icons, no checkerboard painted background.
