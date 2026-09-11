# prompt-B 97 작업 결과

## 변경 내용

채굴 타일 1칸당 광물 보너스를 주는 **채굴 수확량** 업그레이드를 추가했다.

- ID `upgrade.cargo.yield`, 탭은 전력·체력·화물, 3레벨.
- 확정표: Lv.1 구리 +1 / 구리 10. Lv.2 구리 +2·철 +1 / 구리 20+철 10. Lv.3 구리 +3·철 +2·리튬 +1 / 구리 30+철 20+리튬 10.
- 보너스는 월드 채굴 커밋에만 적용한다. 기본분은 전량 수락, 보너스는 남은 화물 자리만큼만 넣는다. 보너스도 화물 무게를 차지한다.
- 퀘스트 지급·판매·보관함 이동·월드 드롭에는 적용하지 않는다.
- 상세 카드는 구리/철/리튬 보너스를 줄 단위로 표시한다. 채굴 완료 HUD는 `구리 +1 (+1 수확)` 형태로 잠시 보여 준다.
- Save 필드는 추가하지 않았다. 구세이브는 레벨 0 → 보너스 0.

§4 숫자와 에셋 숫자는 같다.

## 파일

`sub-terra/Assets/_Project/` 기준:

- `Data/Upgrades/Upgrade_Cargo_Yield.asset`: 신규 에셋.
- `Data/Catalog/GameDataCatalog.asset`: 수확량만 목록에 추가. 사다리 등 기존 등록은 유지.
- `Data/Upgrades/Upgrade_*.asset`: 새 `miningYieldBonuses` 필드 직렬화(기존 업글은 빈 목록).
- `Scripts/App/Core/Data/DataIds.cs`, `UpgradeData.cs`, `CatalogValidator.cs`, `ItemDisplayNames.cs`
- `Scripts/Shared/Contracts/IUpgradeEffectProvider.cs`: `GetMiningYieldBonus`
- `Scripts/App/Progression/UpgradeEffectProvider.cs`, `UpgradeSnapshot.cs`, `ProgressionService.cs`
- `Scripts/App/Inventory/MiningYieldCommit.cs`: 기본 exact / 보너스 partial
- `Scripts/App/Integration/IntegrationRuntimeBinder.cs`, `MiningProgressHud.cs`
- `Scripts/App/UI/Progression/ProgressionPanelView.cs`: 3광물 상세 카드, 용량 탭 행 높이 44
- `Editor/DataValidation/MvpDataAssetBuilder.cs`, `PromptB97YieldUpgradeLayoutBuilder.cs`
- `Editor/DataValidation/KoreanFontAssetUtility.cs`: 시드에 `수확량` 추가. 아틀라스는 재생성하지 않음.
- `Scenes/App/Mine_Demo_Integration.unity`: UpgradePanel에 카탈로그에 없던 엔트리 3개 복제(최대 체력, 체력 재생, 채굴 수확량)
- 테스트: `PromptB97MiningYieldTests.cs` 및 기존 Provider 스텁 4곳, `F_S03` 메서드 수 9, `F_F04_AllTenEffects`

## 검증 및 연결

- Unity 컴파일 성공. 카탈로그 `upgrades=10`, `valid=True`, 사다리 등록 유지.
- EditMode 56개 통과 (`PromptB97MiningYieldTests` + Progression + Catalog + PromptB72). `pass=56 fail=0`.
- `SubTerra/Data/Build MVP Data Assets`는 건물·광물 표시 이름까지 덮어써서, 그 부작용은 restore 했다. 수확량 에셋은 빌더로 만든 뒤 카탈로그만 수동으로 한 줄 추가했다.
- 레이아웃 빌더는 Integration UpgradePanel만 저장했다. Surface Base Prefab·설정 창·폰트 아틀라스는 변경하지 않았다.
- PlayMode 통합 씬에서 구리 타일을 실제로 캐는 Y-P03는 이번 세션에서 돌리지 않았다. 커밋 경로는 EditMode Y-C01~C08이 담당한다.
- 게임 세이브 버전은 올리지 않았다. 커밋·배포는 수행하지 않았다.
