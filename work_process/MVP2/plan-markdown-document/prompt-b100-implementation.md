# prompt-B 100 골드 블록 적용 기록

## 구현

- 암석 1% / 20G, 가스 10% / 50G, 구리·철·리튬 5% / 카탈로그 단가 × 5.
- `GoldDropSettings.asset`에서 확률·고정 보상·배수를 편집한다. 카탈로그는 Shared `IMineralPriceProvider`로 조회한다.
- 기존 광맥 RNG와 분리된 시드·좌표 해시를 사용한다. generatorVersion과 saveVersion은 변경하지 않았다.
- 기반 타일 DTO를 복사하여 드릴·전력·가스·구조·광물 수량을 유지한다. 보호/경계/신호 블록과 튜토리얼 고정 칸은 제외한다.
- 채굴 커밋 성공 시 골드 즉시 지급, 화물 무게 증가 없음. 광물 기본 수량을 담지 못하거나 전력이 부족하면 골드도 지급하지 않는다. 정수 최대값에서 지급량을 제한한다.
- 골드 칸은 노란 드론 스캔, 일반 광석은 흰색, 활성 가스는 빨간색이다. 한 칸에서는 골드가 우선한다.
- 기존 채굴 HUD에 `골드 +20G` 등의 피드백을 추가했다. 가이드와 가스 정화 퀘스트도 연결했다.

## 에셋 및 연결

프로젝트 기준 경로는 `sub-terra/` 아래다.

| 기반 | 생성 PNG | 타일 |
| --- | --- | --- |
| 암석 | `Assets/_Project/Art/Tiles/Ground/ground_normal_gold_01.png` | `RockGold.asset` |
| 가스 | `Assets/_Project/Art/Tiles/Ground/ground_gas_gold_01.png` | `GasPocketGold.asset` |
| 구리 | `Assets/_Project/Art/Tiles/Ore/ore_copper_gold_01.png` | `CopperGold.asset` |
| 철 | `Assets/_Project/Art/Tiles/Ore/ore_iron_gold_01.png` | `IronGold.asset` |
| 리튬 | `Assets/_Project/Art/Tiles/Ore/ore_lithium_gold_01.png` | `LithiumGold.asset` |

- 타일 경로: `Assets/_Project/Tilemaps/DemoWorld/`.
- PNG는 생성 후 256×256으로 리사이즈했다. Sprite Single, 256 PPU, 월드 크기 1×1이다.
- `Mine_Demo_Integration.unity`의 생성기에 설정과 골드 TileBase 5개를 연결하고 Resolver에 영구 ID 5개를 등록했다.
- 재적용 메뉴: `SubTerra/World/Apply Prompt B100 Gold Tiles`.
- Phase B 지층 재설정에도 같은 에셋 연결을 포함한다. 추가 Inspector 작업은 필요 없다.
- 통합 씬 외 UI 프리팹·폰트는 변경하지 않는다.

## 이미지 생성

내장 image_gen 도구를 사용했다. 각 기존 `*_01.png`를 개별 편집 참조로 입력했다. 원본은 보존했다.

공통 최종 프롬프트:

```text
Use case: precise-object-edit. Asset type: square 2D mining game terrain tile, gold-bearing normal rock variant. Edit the reference tile: preserve exactly the original rock shapes, dark grey/brown stone palette, cracks, composition, edge-to-edge square coverage and painted game texture. Add only 5-7 clearly visible metallic native gold nuggets and short thin gold veins embedded in existing cracks and surface hollows. Gold highlights #FFD700, shadows #B8860B. Readable at 256x256, localized gold occupies about 10% of tile. Do not recolor whole rocks. No coins, lettering, UI, cut gemstones, border, background padding or perspective change. Output one square tile.
```

각 이미지의 최종 프롬프트는 공통 프롬프트에 다음 치환/추가를 적용했다.

- 암석: 공통 프롬프트 그대로.
- 가스: `gold-bearing normal rock variant` → `gold-bearing gas rock variant`; `dark grey/brown stone palette` → `green stone palette and luminous toxic green cracks`. 추가: `Preserve all green gas fissures. Gold nuggets must be distinct orange-gold metallic deposits, not green glow.`
- 구리: `normal rock variant` → `copper rock variant`; `dark grey/brown stone palette` → `original rusty reddish copper-brown stone palette`.
- 철: `normal rock variant` → `iron rock variant`; `dark grey/brown stone palette` → `original charcoal and silver grey iron stone palette`.
- 리튬: `normal rock variant` → `lithium crystal rock variant`; `dark grey/brown stone palette` → `original dark blue stone and bright turquoise cyan crystal palette`. 추가: `Preserve every turquoise crystal shape and its color. Embed gold only in dark stone gaps and crystal roots, never replace or recolor the cyan crystals.`

## 검증

- Unity 6000.5.4f1에서 컴파일 및 에셋 연결 완료. 최종 Console Error 0.
- Edit Mode **40/40 통과**: `PromptB100GoldTileTests`, `PromptB100GoldCommitTests`, `DroneSensorUpgradeTests`, `PromptB97MiningYieldTests`, `MineLayerGeneratorTests`.
  - 실제 통합 씬의 생성기와 Resolver로 재생성, 골드 칸의 스냅샷 제거/복원, 보호 타일·튜토리얼 칸 제외 및 1×1 Sprite 크기 확인.
  - 카탈로그 배수, 가스 플래그, 인벤 부족/전력 부족 시 상태 불변, 정수 상한 및 HUD 문구 확인.
  - 암석·광석 골드가 실제 Light2D 노란색 2개로 표시되고 활성 가스보다 우선하는 것을 확인.
- 신규 골드 채굴 Play Mode **2/2 통과**: 성공 시 골드 20·전력 차감·타일 제거 1회, InventoryFull 실패 시 골드/전력/타일 불변.
- 원본 5종과 골드 5종을 Unity 임시 비교 씬에서 시각 확인 후 임시 씬을 닫았다.
- 현재 통합 씬에 골드 변형 45칸 적용: 암석 20, 가스 4, 구리 10, 철 9, 리튬 2. 이후 새 광산에서는 해당 시드와 확률로 다시 결정한다.
- 결과 원본(프로젝트 임시 경로): `Temp/prompt-b100-final-editmode.xml`, `Temp/prompt-b100-gold-playmode.xml`.

### 추가 회귀 검사 및 제한

- 기존 Mining/DemoWorld Play Mode 어셈블리 검사에서 아래 **서로 다른 2건**이 실패했다. 골드 전용 검사는 통과했지만 전체 회귀 통과로 간주하지 않는다.
  - `RuntimeGenerator_RendersFortyMetersAndMiningRejectsBoundary`: `DeepZoneLocked` 기대, `InvalidTarget` 반환. 이 테스트의 임시 Resolver에는 일반 지형 정의가 등록되어 있지 않다.
  - `E_F03_KeyboardAndMouse_UseTheSameTimedCompletionPath`: Enter 입력 후 채굴 시작 기대 실패.
  - 최초 Bootstrap 오진입 실행 뒤 남은 콜백 때문에 통합 텍스트 로그에는 동일 실패가 두 번 기록되었다. 별도 골드 전용 XML 결과는 2/2 통과다.
- Play Mode 검증 중 Bootstrap 시작 씬 고정을 일시 해제하고 끝난 뒤 복원했다. 테스트로 변경된 EditorBuildSettings는 원복했다.
- Windows 실행 파일 빌드, 전체 데모 완주, 실제 사용자 세이브 파일을 이용한 이어하기는 실행하지 않았다. 저장 복원은 테스트용 DTO로 검증했다.
- 원본 요청 문서와 밸런스 계획서는 수정하지 않았다. 자동 커밋/배포는 하지 않았다.
