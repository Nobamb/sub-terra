# prompt-B 101 골드 획득 업그레이드 구현 결과

## 적용 수치

참조 문서 `prompt-b101-gold-gain-upgrade.md`의 확정표를 적용했다. `init/prompt-B.md` 원문의 30/60/100과 차이가 있음을 안내하고 확정표 기준으로 진행했다.

| 레벨 | 추가 골드 | 비용 |
| --- | --- | --- |
| 1 | +50% | 500G + 구리 10개 |
| 2 | +75% | 1000G + 철 10개 |
| 3 | +100% | 3000G + 리튬 10개 |

## 주요 변경

- `upgrade.cargo.gold`를 전력·체력·화물 탭에 추가했다.
- 판매, 전진기지 단일 광물·화물·보관함 정산, 골드 블록 채굴에 같은 보너스 계산을 사용한다. 거래 기본 골드 합계에 한 번 적용하고 0.5는 올림한다.
- 골드와 광물을 사전 전량 검증하고 광물 일괄 차감 성공 후 골드를 차감한다. 부족·미등록 ID·비용 합산 오버플로에서는 지급 상태를 바꾸지 않는다.
- 판매·정산 한도 초과는 거래 실패로 처리한다. 채굴은 기존 정책대로 잔액 한도까지 지급하며 표시 보너스도 실제 받은 금액만 사용한다.
- 상세 카드, 판매 미리보기·결과, 채굴 HUD·금화 연출에 보너스를 분리 표시한다.
- 퀘스트·직접 AddGold, 광물 단가, 타일 기본 골드와 확률은 변경하지 않았다.
- 세이브 버전·DTO 변경 없이 기존 업그레이드 ID/레벨 목록을 사용한다. 구세이브의 누락 레벨은 0이다.

## 수정 파일

경로는 `sub-terra/Assets/_Project/` 기준이다.

- 데이터: `Data/Upgrades/Upgrade_Cargo_Gold.asset` 및 `.meta`, `Data/Catalog/GameDataCatalog.asset`
- 에디터: `Editor/DataValidation/MvpDataAssetBuilder.cs`, `PromptB101GoldGainBuilder.cs` 및 `.meta`
- 씬: `Scenes/App/Mine_Demo_Integration.unity` (골드 엔트리 및 참조 추가)
- 정의·검증: `Scripts/App/Core/Data/DataIds.cs`, `ItemDisplayNames.cs`, `CatalogValidator.cs`
- 경제: `Scripts/App/Economy/EconomyPricing.cs`, `EconomyService.cs`, `Scripts/App/Outpost/OutpostService.cs`
- 효과: `Scripts/Shared/Contracts/IUpgradeEffectProvider.cs`, `Scripts/App/Progression/UpgradeEffectProvider.cs`
- 채굴·연출: `Scripts/App/Inventory/MiningYieldCommit.cs`, `Scripts/App/Integration/GoldPickupPresentation.cs`, `GoldPickupVfx.cs`, `IntegrationRuntimeBinder.cs`
- 저장 배선: `Scripts/App/Save/SaveRuntimeController.cs`
- UI: `Scripts/App/UI/Economy/EconomyPanelPresenter.cs`, `Scripts/App/UI/Progression/ProgressionPanelView.cs`
- 테스트: `Tests/EditMode/App/Progression/PromptB101GoldGainTests.cs` 및 `.meta`, `ProgressionServiceTests.cs`, `ProgressionStaticStructureTests.cs`
- Shared 테스트 대역: `Tests/EditMode/App/Integration/GasExposureEffectControllerTests.cs`, `Tests/EditMode/App/Run/RunFailureServiceTests.cs`, `Tests/EditMode/Gameplay/Drone/DroneSensorUpgradeTests.cs`, `Tests/PlayMode/Gameplay/Mining/MiningSystemPlayModeTests.cs`

## Unity 배선 및 검증

- Unity MCP로 전용 빌더 실행 완료. 재생성 메뉴: `SubTerra/UI/Build Prompt-B 101 Gold Gain Upgrade`.
- 별도 Inspector 수동 연결은 필요 없다. Economy는 저장 런타임에서 현재 UpgradeState를 읽고, Outpost는 IntegrationRuntimeBinder에서 Effects를 주입한다.
- 카탈로그 검증: `No issues.`
- 관련 EditMode 회귀 테스트 209개 통과, 실패 0. 경제·진행·전진기지·저장·업그레이드 UI·골드 연출 계산 포함.
- 비용 합산·화물 가득 경계를 추가한 뒤 최종 골드 기능 테스트 21개 재검증 통과, 실패 0. 앞선 회귀 실행의 골드 테스트 19개와 겹친다.
- Unity 에디터에서 용량 탭 버튼 6개 확인: Y=0/-44/-88/-132/-176/-220, 높이 44. 목록 높이 420 안에 모두 들어간다.
- 실제 상세 카드 확인: `현재 +0% → 다음 +50%`, `필요 재료: 500G, 구리 x10`. 기존 패널 크기는 유지했다.
- 부정 입력 테스트가 의도적으로 기록하는 경고·오류와 별개로 스크립트 컴파일은 완료했다.
- 테스트 중 자동 변경된 공용 폰트·빌드 설정은 복원했다. 사용자 기존 `init/prompt-B.md` 수정은 유지했다.

## 검증 제한

Bootstrap부터 시작하는 실제 구매→채굴 플레이와 Windows 빌드 검증은 실행하지 않았다. 실제 저장 파일을 변경하지 않고 메모리 상태·JSON 왕복과 에디터 UI로 검증했다.
