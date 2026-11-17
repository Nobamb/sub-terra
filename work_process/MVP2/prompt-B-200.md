# B-200 산소 업그레이드 2종 (#336)

## 목표
기존 업그레이드 체계에 산소 업그레이드 2종을 넣는다. (1) 최대 산소 증가, (2) 산소 소모 감소. 초반은 업그레이드 없이 산소 부족으로 몇 번 죽을 수 있을 만큼 빡빡하고, 만렙이어도 깊이 소모 증가를 상쇄하고 남지 않아 가장 깊은 곳에서 압박이 남게 한다. 단계·비용·효과 값은 미정이며 검사기에서 조절한다. 소모 감소는 #334 깊이·행동·무게 배율(및 가스 배율)이 곱해진 최종 소모율에 추가로 적용한다.
기준: https://github.com/Nobamb/sub-terra/issues/336
이 이슈는 Nobamb 담당이다(디자인 포함).

## 브랜치
1. `git fetch` 후 최신 `origin/main`(`f324038d1b75b80436e2f5dcf3d7f5db5904e743`, #360 사다리·점프 산소 비교 병합)에서 새 브랜치 **`MVP2-fix144`**를 만든다. (`MVP2-fix143`까지 이미 있음. 이 번호는 쓰지 않는다.)
2. 원격에 `MVP2-fix144`가 이미 있으면 비어 있는 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다.
3. 보고서 맨 위에 기준 커밋 해시를 적는다. 브랜치를 지우지 않는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix144`(또는 실제 쓴 번호)에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다. #336에 댓글·체크박스 수정·닫기를 하지 않는다.
3. 새 음원, 새 `SfxId`, `SfxLibrary`, 음량을 추가하거나 바꾸지 않는다. 기존 SFX만 쓴다.
4. 큰 화면 전체 모션이 아니면 reduce-motion 분기를 새로 넣지 않는다. 업그레이드 창 기존 모션을 재설계하지 않는다.
5. 프리팹 YAML을 손으로 고치지 않는다. 트리 노드·카탈로그는 기존 에디터 빌더로 수정한다.
6. 기존 업그레이드 ID·효과·비용을 바꾸지 않는다. 산소 기본 소모/회복/가스·#334 배율 공식 자체를 재설계하지 않는다. 수치는 미정 — 아래 초안은 검토용이며 잠정치가 아니다.

## 현재 상태 (main `f324038d`)
- **데이터.** `UpgradeData`/`UpgradeLevelDefinition`(`Scripts/App/Core/Data/UpgradeData.cs`). ID 상수 `DataIds.Upgrades`에 산소 ID는 아직 없다. 표시명·설명: `ItemDisplayNames.Upgrade`/`UpgradeDescription`. 에셋: `Assets/_Project/Data/Upgrades/`(예: `Upgrade_Maximum_Energy.asset`, id `upgrade.energy.maximum`). 카탈로그 목록은 `GameDataCatalog.asset`이며 `MvpDataAssetBuilder.BuildUpgrades`가 생성·등록한다.
- **효과.** `UpgradeEffectProvider`가 `GetCurrentEffect(id)`로 현재 레벨 `effectValue`를 읽는다. 최대 전력/체력은 기본값+보너스, 가스 저항·드릴 효율은 감소 비율. 공유 경계: `IUpgradeEffectProvider`, 체력은 `IPlayerHealthUpgradeProvider`. `RunFailureRuntimeController`가 `progression.Effects`를 `PlayerSurvivalController.BindUpgradeEffects`에 넘긴다. 체력 최대가 오르면 `PlayerSurvivalState.ApplyUpgradeEffects`가 증가분만큼 현재 체력을 올리고 전부 채우지는 않는다.
- **트리/탭.** `UpgradeCategoryRules`: `upgrade.energy.`/`health.`/`cargo.` → 용량 탭「전력·체력·화물」. `upgrade.oxygen.`는 지금 드릴 탭으로 떨어지니 용량 탭에 넣어야 한다. 노드 좌표·부모는 `PromptB118UpgradeTreeBuilder.Nodes`가 정하고, 같은 파일이 `EditorSetUnlock(..., parent Lv.1, treeParentId)`를 쓴다. `PromptB118UpgradeTreeTests`가 노드 11개·연결 10개, `Nodes.Length == catalog.Upgrades.Count`를 검사하므로 테스트도 갱신한다.
- **산소 런타임.** `PlayerSurvivalSettings.MaximumOxygen` 기본 100, `OxygenPerSecond` 0.5, `OxygenPerSecondPerMeter` 0.02. `PlayerOxygenState.Maximum`은 생성 시 고정. `Advance` 소모: `(baseRate + perMeter * Round(depth)) * drainMultiplier`, 깊이 ≤10M는 회복만. `PlayerSurvivalController.AdvanceOxygen`의 drainMultiplier는 #334 `DrainMultiplier`(이동 1.5·사다리 1.5·만재 1.5·사다리 화물 상한 1.2) × 가스 2배. HUD는 이미 `OxygenChanged(current, max)`를 쓴다. 소모 감소 훅은 아직 없다.

## 할 일
1. 상수·에셋(기본 3단계, 검토용 초안 — 잠정치 아님):
   - `DataIds.Upgrades.MaximumOxygen = "upgrade.oxygen.maximum"` — 표시명 「최대 산소」. `effectValue`는 최대량 가산. 초안 +40/+80/+130, 비용은 `Upgrade_Maximum_Health`와 같은 구리 6 / 구리 6+철 4 / 철 8+리튬 5.
   - `DataIds.Upgrades.OxygenEfficiency = "upgrade.oxygen.efficiency"` — 표시명 「산소 소모 감소」. `effectValue`는 감소 비율(가스 저항과 같이 0.15 → 「-15%」). 초안 0.15/0.30/0.45, 비용은 `Upgrade_Drill_Efficiency`와 같은 구리 6 / 구리 8+철 5 / 철 10+리튬 6. 만렙 45%여도 깊이항(`0.02×깊이`)이 더 크므로 심층 압박이 남는다. 1.0에 가깝게 올리지 않는다.
   - 에셋 `Upgrade_Maximum_Oxygen.asset`, `Upgrade_Oxygen_Efficiency.asset`을 `MvpDataAssetBuilder`로 만들고 `GameDataCatalog`에 등록한다.
2. 트리: 용량 탭. 제안 부모/좌표(겹치면 조정): 최대 산소 부모 `upgrade.energy.maximum`, 좌표 예 `(-450, 0)`; 소모 감소 부모 `upgrade.oxygen.maximum`, 예 `(-450, -205)`. 아이콘은 기존 노드 스프라이트를 재사용하고, 새 아트가 필요하면 보고서에 크기·영역을 적는다.
3. `ItemDisplayNames`·`UpgradeEffectFormatter`(최대는 「+40」, 소모 감소는 가스처럼 「-15%」)와 짧은 설명문을 추가한다. `UpgradeCategoryRules`에 `upgrade.oxygen.` → Capacity.
4. `IUpgradeEffectProvider`에 `GetMaximumOxygen(float baseMaximum)`, `GetOxygenDrainReduction()`(0~상한, 1 미만)를 추가하고 `UpgradeEffectProvider`에서 구현한다. Gameplay용 `IPlayerOxygenUpgradeProvider`를 Shared 계약에 두고 `UpgradeEffectProvider`가 구현한다. `RunFailureRuntimeController`에서 체력과 같이 바인딩한다.
5. `PlayerOxygenState` 최대량을 체력과 같이 갱신: 증가분만큼 현재량을 올리고 전부 채우지 않으며, 감소 시 클램프. `OxygenChanged`로 HUD 최대가 바로 바뀌게 한다. `AdvanceOxygen`에서는 #334·가스가 곱해진 뒤 `(1 - reduction)`을 한 번 더 곱한다. 0~10M 회복, 기본 소모 공식, 가스 2배는 그대로.
6. 기본 산소 설정(100 / 0.5 / 0.02 등)은 유지한다. 플레이에서 초반이 너무 널널하면 보고서의 조정 제안에만 적고, 기존 배율 상수를 임의로 바꾸지 않는다.
7. EditMode: 제공자 보너스, 소모 감소가 #334·가스 이후에 적용되고 얕은 회복은 불변, 만렙이어도 더 깊은 곳이 더 빨리 닳는지, 최대 증가 시 전체 충전 없음. `PromptB118UpgradeTreeTests`의 11/10 하드코딩을 새 노드 수에 맞게 수정. 기존 `PromptB72` 수치를 깨지 말 것. 프리팹 YAML 손수정 금지.

## 테스트 (플레이 가이드)
1. 업그레이드를 사지 않은 채 초반 광산을 판다. 깊이 10M를 넘기면 산소가 줄고, 오래 버티지 못해 산소 고갈로 몇 번 죽을 수 있는지 확인한다. 0~10M에서는 회복되는지 본다.
2. 기지에서 업그레이드 창을 연다. 「전력·체력·화물」 탭과 트리에 「최대 산소」「산소 소모 감소」가 보인다. 최대 전력 Lv.1 전이면 최대 산소가 잠겨 있고, 최대 산소 Lv.1 전이면 소모 감소가 잠겨 있는지 확인한다. 다른 업그레이드 위치·비용은 그대로인지 본다.
3. 「최대 산소」를 1단계 산다. HUD 산소 최대가 기본 100에서 늘어나고, 현재량은 증가분만큼만 늘며 전부 채워지지 않는지 확인한다. 기지 충전/복귀로 최대까지 채워지는지 본다.
4. 같은 깊이·같은 행동(정지/걷기/점프/사다리)·같은 화물에서, 소모 감소 업그레이드 전과 후를 비교해 닳는 속도가 줄어드는지 확인한다. 가스 안에서 기존처럼 더 빨리 닳는지, 0~10M 회복 속도는 변하지 않는지 본다.
5. 두 업그레이드를 만렙으로 올리고 가장 깊은 구간으로 내려간다. 만렙이어도 얕은 곳보다 빨리 닳고, 소모가 0이 되지 않아 압박이 남는지 확인한다.
6. 검사기에서 단계 효과·비용을 바꿨을 때 다음 구매와 표시 문구(예: 최대 산소 +40, 소모 감소 -15%)가 따라가는지 본다.
7. 1920×1080에서 트리 노드가 겹치거나 잘리지 않는지, Unity Console에 새 빨간 오류가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 산소 최대량·소모 감소 업그레이드 추가 (프롬프트 B-200)`). `MVP2-fix144`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue336-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #336은 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-200-oxygen-upgrades-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일
  - 최종 ID·부모·좌표, 단계·비용·effectValue(초안에서 바꿨으면 이유), 소모 감소를 곱한 위치
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드, 필요 아트
- **완료 체크리스트**: [ ] `MVP2-fix144` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음 [ ] 2종 UpgradeData·카탈로그·트리·용량 탭 [ ] 최대 증가가 HUD에 반영, 전부 충전 아님 [ ] 소모 감소가 #334·가스 이후 적용, 얕은 회복 불변 [ ] 만렙이어도 심층 압박 잔류, 소모 0 아님 [ ] 수치 검사기 조절, 최종값 보고 [ ] 기존 업그레이드·#334 공식 미변경 [ ] 새 SFX 없음 [ ] 프리팹 YAML 손수정 없음 [ ] 아트 요청을 보고서에 명시 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] 브랜치 삭제 안 함 [ ] #336 미수정
