# Phase C 구현 기록

## 구현

- `Assets/_Project/Scripts/App/State/GameState.cs`
  - HUD 10항목 원천: Energy/MaxEnergy, Depth, Gold, Cargo, UnsettledValue, StructuralRisk, GasExposure, BuildingSelection, InteractionPrompt
  - 의도 변경 API (`SetEnergy`, `AddGold`/`SetGold`, `SetInventory` 등) + 동일 값 재설정 시 이벤트 억제
  - 이벤트: `EnergyChanged`, `CreditsChanged`, `InventoryChanged`, `DepthChanged`, `StructuralRiskChanged`, `GasExposureChanged`, `BuildingSelectionChanged`, `InteractionPromptChanged`
- `Assets/_Project/Scripts/App/UI/HUD/`
  - `HudFormatter` — null/0/미선택 기본값 (`0`, `안전`, `선택 없음`)
  - `HudPresenter` — Bind 시 풀 렌더 1회 후 변경 항목만 갱신, Unbind 대칭
  - `IHudView` / `CompositeHudView` / `BasicHudView` / `StructuralHudView` / `GasWarningPanelView`
  - `HudBinder` — `OnEnable` 구독, `OnDisable` 해제 (State 쓰기 없음)
- Prefab (`Assets/_Project/Prefabs/UI/`)
  - `BasicHUD.prefab`, `StructuralHUD.prefab`, `GasWarningPanel.prefab`, `HUDCanvas.prefab`
  - Canvas Scaler: Scale With Screen Size, 1920×1080, match 0.5
  - Anchor: 좌상(기본), 우상(구조), 상단 중앙(가스)
- `MainMenu.unity`에 `HUDCanvas` 배치 및 참조 연결
- 테스트
  - Edit Mode: `HudFormatterTests`, `HudPresenterTests`(C-F01/C-F02), `GameStateHudEventsTests`, `HudStaticStructureTests`
  - Play Mode: `HudPlayModeTests` (재바인드·선택 갱신)

## 검증

- Edit Mode `SubTerra.App.Tests.EditMode`: **71 passed / 0 failed**
- Play Mode `SubTerra.App.Tests.PlayMode`: **3 passed / 0 failed**
- Prefab `HasRequiredReferences=True` (Basic/Structural/Gas/HUDCanvas)
- HUD 소스에 `void Update(` 폴링 및 UI→`AddGold` 경로 없음
- Unity Console error/warning 0 (구현 직후 조회)
- 소유권: Gameplay/Shared 미변경. 런타임 asmdef에 UnityEditor 참조 없음 (`Unity.TextMeshPro`, `UnityEngine.UI`만 추가)

## 설계 메모

- UI는 표시 문자열만 설정하고, 수치 변경은 `GameState` 의도 API만 사용한다.
- 구조·가스 등급은 A 계산 수신용 표시 필드이며 UI에서 재계산하지 않는다.
- 인벤토리 광물별 수량 UI·세이브 복원은 후속 단계(D+) 범위다.

## 남은 한계

- Game View 해상도별 스크린샷 육안 검증은 Canvas Scaler·Anchor 구조 증거로 대체했다.
- `PlayerState.AddGold` 직접 호출은 값을 바꾸지만 이벤트를 발행하지 않는다. HUD 연동 경로는 `GameState.AddGold`/`SetGold`를 사용한다.
- Integration Scene 전체 연결은 M 단계 범위다.
