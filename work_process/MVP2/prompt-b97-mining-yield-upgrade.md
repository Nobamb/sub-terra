# prompt-B 97 채굴 수확량 업그레이드 — 밸런스 검토 및 구현 계획

| 항목 | 내용 |
| --- | --- |
| **문서 제목** | Mining Yield Upgrade (prompt-B 97) |
| **날짜** | 2026-09-11 |
| **상태** | Draft — 수치 합의용. 구현 없음 |
| **원문** | `init/prompt-B.md` 97번 |
| **관련 코드** | `UpgradeData`, `UpgradeEffectProvider`, `IUpgradeEffectProvider`, `IntegrationRuntimeBinder.TryCommitMining`, `InventoryService`, `UpgradeCategoryRules`, `MvpDataAssetBuilder`, `ProgressionPanelView` |
| **범위** | 채굴 타일 1회당 광물 보너스 업그레이드 1종. 전력·체력·화물 탭에 추가 |
| **유지할 것** | 기존 구매·저장·탭 구조, 기존 업그레이드 ID, 타일 기본 수량 1, 화물 무게 규칙 |

숫자만 바꾸려면 **§4 확정표**와 **§10 조절 노브**를 고치면 된다. 원칙은 §2, 현재 경제 기준은 §3.

---

## 0. 한 줄 결론

원안 숫자는 **미래 비용 상승을 대비한 수확 속도 업그레이드**로 맞다. 지금 빌드에서는 “한 번에 더 많이 들고 온다”가 아니라 **같은 만재를 더 적은 타일·전력·체류시간으로 채운다**.

만재 총량은 화물 업그레이드가 담당한다. 이 업그레이드는 왕복 시간을 줄인다. 나중에 건물·업글 비용이 늘어나면 그 왕복 시간 절감이 실제 진행 완충이 된다.

판정 문장:

> “구리를 한 칸 캤더니 기본 1개가 아니라 보너스까지 들어왔다. 화물칸은 더 빨리 찼고, 만재량은 거의 그대로였다.”

이 문장을 못 만들면 효과가 잘못 적용된 것이다.

---

## 1. 원안

prompt-B 97 원문 그대로.

| 레벨 | 채굴 보너스 | 비용 |
| ---: | --- | --- |
| 1 | 구리 +1 | 구리 10 |
| 2 | 구리 +2, 철 +1 | 구리 20 + 철 10 |
| 3 | 구리 +3, 철 +2, 리튬 +1 | 구리 30 + 철 20 + 리튬 10 |

레벨 2 예시: 구리 칸 → 기본 1 + 보너스 2 = 3개. 철 칸 → 기본 1 + 보너스 1 = 2개. 리튬 칸 → 보너스 없음.

탭: **전력·체력·화물**.

의도: 이후 콘텐츠에서 비용이 더 늘어날 것을 상정해 둔 업그레이드.

---

## 2. 설계 원칙

구현·수치 조정 때 이 여섯 줄을 유지한다. 깨려면 이유를 §10에 적는다.

1. 보너스는 **채굴 타일 1칸당 고정 가산**이다. 퍼센트가 아니다.
2. 보너스로 얻은 광물도 **화물 무게를 그대로 차지**한다. 무게 없는 덤이 되면 경제가 붕괴한다.
3. 적용 범위는 **월드 채굴 커밋만**. 퀘스트 보상, 판매, 보관함 이동, 월드 드롭 줍기에는 적용하지 않는다.
4. 타일 기본분은 전량 수락이 실패하면 채굴 자체를 롤백한다. 보너스는 **들어가고 남은 자리만큼만** 넣는다.
5. 해금 광물은 레벨과 맞춘다. Lv.1 구리, Lv.2 철, Lv.3 리튬. 아직 못 캐는 광물에 보너스를 미리 주지 않는다.
6. 단일 `effectValue`에 구리·철·리튬을 우겨 넣지 않는다. 광물별 보너스 표를 데이터로 둔다.

---

## 3. 현재 빌드 기준값

보상·비용을 이 값으로 계산했다. 데이터가 바뀌면 이 절과 §4를 같이 고친다.

### 3-1. 채굴·화물

| 항목 | 값 | 출처 |
| --- | ---: | --- |
| 구리/철/리튬 타일 기본 수량 | 1 | `DemoWorldSetup` `MiningTileDto.quantity` |
| 기본 화물 | 50 | `InventoryState.DefaultMaxCapacity` |
| 구리 중량 / 단가 | 1.5 / 10G | `Mineral_Copper` |
| 철 중량 / 단가 | 2.0 / 15G | `Mineral_Iron` |
| 리튬 중량 / 단가 | 0.8 / 40G | `Mineral_Lithium` |
| 만재 구리 | 약 33개 | 50 / 1.5 |
| 만재 철 | 약 25개 | 50 / 2.0 |
| 만재 리튬 | 약 62개 | 50 / 0.8 |
| 광맥 크기 | 2~5칸 | `MineLayerDistribution` |
| 상층 구리 / 중층 철 / 심층 리튬 | 1~15 / 16~35 / 36~40m | 같은 에셋 |

### 3-2. 기존 업그레이드 비용 (현재 카탈로그)

| 계열 | Lv.1 | Lv.2 | Lv.3 |
| --- | --- | --- | --- |
| 드릴 속도 | 구리 8 | 구리 10 + 철 6 | 철 12 + 리튬 8 |
| 최대 전력 | 구리 8 | 구리 8 + 철 6 | 철 10 + 리튬 8 |
| 최대 화물 | 구리 6 | 구리 8 + 철 5 | 철 8 + 리튬 6 |
| 최대 체력 | 구리 6 | 구리 6 + 철 4 | 철 8 + 리튬 5 |
| 가스 저항 | 구리 6 | 구리 6 + 철 4 | 철 8 + 리튬 6 |

골드 환산(구리 10 / 철 15 / 리튬 40):

| 대상 | 골드 환산 |
| --- | ---: |
| 기존 가장 비싼 Lv.1 (드릴·전력) | 80G |
| 기존 가장 비싼 Lv.3 (드릴 속도) | 500G |
| 원안 Lv.1 | 100G |
| 원안 Lv.2 | 350G |
| 원안 Lv.3 | 1,000G |

원안 Lv.3은 현재 최고가 업글의 약 2배이고, **최대 레벨에서 구리를 계속 받는 유일한 항목**이 된다. 미래 구리 싱크로는 맞다.

---

## 4. 확정표 (수정 지점)

이 표가 구현 원본이다. 칸만 고치면 된다.

| 레벨 | 구리 보너스 | 철 보너스 | 리튬 보너스 | 구리 | 철 | 리튬 | 골드환산 |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | +1 | 0 | 0 | 10 | 0 | 0 | 100 |
| 2 | +2 | +1 | 0 | 20 | 10 | 0 | 350 |
| 3 | +3 | +2 | +1 | 30 | 20 | 10 | 1,000 |

권장 ID / 표시:

| 항목 | 값 |
| --- | --- |
| 영구 ID | `upgrade.cargo.yield` |
| 표시 이름 | 채굴 수확량 |
| 탭 | 전력·체력·화물 (`UpgradeCategory.Capacity`) |
| `effectValue` 폴백 | 그 레벨의 구리 보너스 (1 / 2 / 3). UI 전용. 실제 지급 계산에 쓰지 않음 |

ID를 `upgrade.cargo.*`로 두는 이유: `UpgradeCategoryRules.Resolve`가 `upgrade.energy.` / `upgrade.health.` / `upgrade.cargo.`만 용량 탭으로 보낸다. 새 prefix를 만들 필요가 없다. 수확량은 화물 무게와 직접 충돌하므로 cargo 계열이 맞다.

---

## 5. 밸런스 검토

### 5-1. 이 업글이 실제로 바꾸는 것

타일 기본 수량이 1이므로 보너스는 배율로 읽힌다.

| 레벨 | 구리 칸당 | 철 칸당 | 리튬 칸당 | 만재(화물 50)까지 구리 타일 | 만재 구리 개수 |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | 1 | 1 | 1 | 33칸 | 33 |
| 1 | 2 | 1 | 1 | 16칸 | 32 |
| 2 | 3 | 2 | 1 | 11칸 | 33 |
| 3 | 4 | 3 | 2 | 8칸 | 32 |

만재 개수는 거의 같다. 바뀌는 것은 칸 수다.

그래서 현재 빌드에서의 가치는 다음이다.

- 같은 만재를 **절반~1/4 타일**로 채운다.
- 채굴 전력·시간·붕괴 노출·가스 체류가 같이 줄어든다.
- 전력이 먼저 바닥나서 중간에 귀환하는 탐사는 **실제로 더 많이** 들고 온다.
- 퀘스트 “구리 채취” 같은 개수 조건은 더 빨리 끝난다.

반대로, 화물을 항상 꽉 채우고 귀환하는 플레이는 왕복 횟수가 줄지 않는다. 왕복 횟수를 줄이려면 화물 업그레이드가 따로 필요하다. 두 계열이 겹치지 않고 보완한다.

### 5-2. 현재 경제에서 강한가

**강하다. 다만 화물 무게가 안전핀이다.**

- Lv.1 비용 10구리는 기존 Lv.1 중 가장 비싸다. 첫 만재 33개로 사고 23개가 남는다.
- 본전은 구리 10칸. 광맥 2~5칸이면 광맥 2~5개면 회수된다.
- 드릴 속도 Lv.1은 채굴 시간 약 25% 단축. 이 업글 Lv.1은 구리 채집 칸 수를 50% 줄인다. **구리만 보면 드릴보다 체감이 크다.**
- 그래도 철 해금은 드릴 Lv.1, 리튬·심층은 드릴 Lv.2라서 진행 필수 구매를 대체하지는 않는다.

현재 비용이 아직 낮아서, 첫 귀환에 이 업글과 다른 Lv.1을 같이 살 수 있다. `upgrade-system-rework-proposal.md`의 “첫 귀환에 의미 있는 Lv.1 하나” 원칙과는 살짝 어긋난다. 지금은 미래 비용 상승을 전제로 원안을 유지한다. 비용이 안 오른 채로 오래 가면 §10에서 Lv.1만 구리 12~15로 올리면 된다.

### 5-3. 미래 비용이 늘면

이 업글의 존재 이유가 여기서 닫힌다.

| 미래 상황 | 이 업글이 하는 일 |
| --- | --- |
| 업글·건물 비용이 3~5배로 증가 | 같은 화물칸으로도 타일당 회수가 늘어 왕복 시간이 버팀 |
| 새 광물·심층 시설이 추가 | 광물별 보너스 표만 행을 늘리면 됨 |
| 화물 업글도 같이 커짐 | 만재 총량이 늘고, 수확량은 그 총량을 채우는 속도를 맞춤 |
| 수확 보너스에 무게를 안 붙임 | 만재 총량까지 곱해져 경제가 무너짐. 금지 |

미래 광물을 이 한 계열에 계속 넣으면 Lv.4 이후 표가 비대해진다. 그때는 광물별 수확 업글로 쪼개는 후속안을 본다. MVP2에서는 구리·철·리튬 3종만 다룬다.

### 5-4. 위험과 막을 것

| 위험 | 결과 | 차단 |
| --- | --- | --- |
| 보너스 무게 면제 | 만재 구리가 33 → 64(Lv.1) → 132(Lv.3) | 보너수도 `TryAddMineral` |
| 보너스까지 전량 수락 | 자리 1.5 남았는데 기본 1은 들어가는데 보너스 때문에 채굴 실패 | 기본분 exact, 보너스 partial |
| 퀘스트 보상에도 적용 | 클리어 보상 2~4배 | 채굴 커밋 한 곳만 |
| `effectValue` 하나에 3광물 인코딩 | 확장·표시·검증이 깨짐 | `miningYieldBonuses` 리스트 |
| 암석·가스·신호 타일 | 빈 ID에 보너스 지급 시도 | `mineralId` 없거나 기본 수량 0이면 0 |
| 보관함 이동·판매 | 옮길 때마다 복제 | 해당 경로에서 Provider를 읽지 않음 |

### 5-5. 판정

| 질문 | 답 |
| --- | --- |
| 원안 구조를 쓰는가 | 예. 레벨별 광물 해금 + 높은 후반 비용 |
| 원안 숫자를 쓰는가 | 예. 1차 가설. 플레이테스트 전 확정값 아님 |
| 지금 빌드를 깨는가 | 화물 무게와 부분 보너스를 지키면 아님 |
| 미래 확장 가정이 맞는가 | 맞다. 비용이 늘수록 이 업글의 가치가 커진다 |
| 지금 바로 비용을 더 올리라고 하는가 | 아니오. 비용 인상이 실제로 들어오기 전에는 §10 노브만 열어 둔다 |

---

## 6. 데이터 모델

현재 `UpgradeLevelDefinition`은 레벨당 `effectValue` 하나와 `costs`만 있다. 수확량은 광물별로 다른 정수라서 이 필드만으로는 구현할 수 없다.

### 6-1. `UpgradeLevelDefinition`에 보너스 표 추가

```csharp
[SerializeField] private List<MineralBonusEntry> miningYieldBonuses
    = new List<MineralBonusEntry>();

public IReadOnlyList<MineralBonusEntry> MiningYieldBonuses => miningYieldBonuses;
```

`MineralBonusEntry`는 `itemId + quantity`만 가진다. `ItemCostEntry`를 재사용하지 않는다. 비용과 보너스를 같은 타입으로 두면 검증·UI·세이브 설명에서 섞인다.

```csharp
[Serializable]
public sealed class MineralBonusEntry
{
    [SerializeField] private string mineralId;
    [SerializeField] private int quantity;
    public string MineralId => mineralId;
    public int Quantity => quantity;
}
```

`effectValue`는 기존 구매 검증(`> 0`)과 상세 카드 폴백을 위해 **그 레벨의 구리 보너스**를 넣는다. 지급 계산은 `miningYieldBonuses`만 본다.

### 6-2. 카탈로그 검증

`CatalogValidator.ValidateUpgrades`에 이 업글만의 규칙을 추가한다.

- `upgrade.cargo.yield`의 각 레벨 `miningYieldBonuses`는 비어 있으면 안 된다.
- `mineralId`는 등록된 광물이어야 한다.
- `quantity >= 1`.
- 같은 레벨에 같은 광물 중복 금지.
- 다음 레벨의 같은 광물 보너스는 이전 레벨 이상(단조 증가). 0 → 양수도 허용.
- 다른 업그레이드는 `miningYieldBonuses`가 비어 있어야 한다. 실수로 드릴 속도에 수확 표를 넣는 것을 막는다.

기존 `effectValue > 0` 규칙은 그대로 둔다.

### 6-3. 에셋·카탈로그 등록

`MvpDataAssetBuilder.BuildUpgrades()`에 한 줄 추가하고, `EnsureUpgrade`를 보너스 표를 받을 수 있게 확장하거나 수확량 전용 Ensure를 둔다.

Lv.3 비용은 구리+철+리튬이라 기존 `IronLithium()` 헬퍼로는 안 된다. `CopperIronLithium(30, 20, 10)`을 추가한다.

산출물:

- `Assets/_Project/Data/Upgrades/Upgrade_Cargo_Yield.asset`
- `GameDataCatalog.upgrades` 목록에 명시 등록
- `DataIds.Upgrades.CargoYield = "upgrade.cargo.yield"`

에디터 메뉴 `SubTerra/Data/Build MVP Data Assets`를 다시 돌려 에셋과 카탈로그를 맞춘다. YAML을 손으로 고치지 않는다.

---

## 7. 런타임 적용

### 7-1. 소유권

| 층 | 책임 |
| --- | --- |
| Gameplay `MiningSystem` | 타일 기본 `mineralId` + `quantity`만 커밋에 넘긴다. 보너스를 모른다. |
| Shared `IUpgradeEffectProvider` | `int GetMiningYieldBonus(string mineralId)` |
| App `UpgradeEffectProvider` | 현재 레벨의 `miningYieldBonuses`에서 해당 광물 수량을 읽는다. 없으면 0. |
| App `IntegrationRuntimeBinder.TryCommitMining` | 기본분 exact 추가 → 보너스 partial 추가 → 전력 차감 |

Gameplay가 `SubTerra.App.Progression`을 참조하면 `ProgressionStaticStructureTests.F_S03`가 실패한다. 보너스는 App 채굴 커밋에서만 더한다.

### 7-2. Shared 계약

`IUpgradeEffectProvider`에 메서드 1개:

```csharp
int GetMiningYieldBonus(string mineralId);
```

규칙:

- `mineralId`가 비었거나 현재 레벨 표에 없으면 0.
- 음수·NaN 데이터는 0.
- 레벨 0은 0.

`F_S03`는 현재 메서드 개수를 8로 고정한다. 9로 바꾸고, 스텁 구현을 모두 채운다.

스텁을 고쳐야 하는 파일:

- `Tests/PlayMode/Gameplay/Mining/MiningSystemPlayModeTests.cs`
- `Tests/EditMode/Gameplay/Drone/DroneSensorUpgradeTests.cs`
- `Tests/EditMode/App/Run/RunFailureServiceTests.cs`
- `Tests/EditMode/App/Integration/GasExposureEffectControllerTests.cs`

기본 구현은 `return 0;`이면 된다.

### 7-3. `TryCommitMining` 순서

현재는 기본 수량만 `TryAddMineralExact` 한 뒤 전력을 뺀다. 아래 순서로 바꾼다.

1. 의존성·전력 검사. 실패 시 월드·인벤·전력 그대로.
2. `mineralId`가 없거나 기본 수량 0이면 보상 없이 전력만 처리 (암석 등 기존 동작).
3. `TryAddMineralExact(mineralId, baseQuantity)`. 실패하면 `InventoryFull` / `InvalidReward`. 보너스는 시도하지 않는다.
4. `bonus = Effects.GetMiningYieldBonus(mineralId)`. `bonus > 0`이면 `TryAddMineral(mineralId, bonus)` — **partial 허용**.
5. 전력 차감.
6. 성공 반환.

자리 계산 예:

- 남은 무게 2.0, 구리 1.5, Lv.1 보너스 +1.
- 기본 1개(1.5)는 들어간다.
- 보너스 1개(추가로 1.5)는 0.5 부족 → 보너스 0개.
- 채굴은 성공, 구리는 1개, 타일은 제거.

남은 무게 4.0이면 기본 1 + 보너스 1 = 2개가 들어간다.

기본분까지 자리가 없으면 지금과 같이 채굴 실패. 타일은 남고 전력은 안 깎인다.

### 7-4. 적용하지 않는 경로

| 경로 | 이유 |
| --- | --- |
| `InventoryService.AddMineral` 직접 호출 | 퀘스트·치트·테스트 지급까지 배가됨 |
| `QuestRewardService` | 클리어 보상은 별도 표 |
| `OutpostService` 보관함 이동 | 옮기는 순간 복제됨 |
| `EconomyService.TrySellMineral` | 판매량 조작 |
| `MiningSystem` 월드 드롭 폴백 | 인벤 없는 테스트 경로. 드롭 오브젝트를 여러 개 만들지 않음 |

생산 런타임은 `miningTransactionBehaviour`가 바인더로 붙어 있으므로 드롭 폴백을 타지 않는다.

### 7-5. 저장

업그레이드 레벨만 저장된다. 새 Save 필드 없음. `saveVersion` 올리지 않는다. 구세이브는 레벨 0 → 보너스 0.

---

## 8. UI

### 8-1. 탭·목록

`ProgressionPanelView.EnsureUpgradeButtons`가 카탈로그 스냅샷으로 엔트리를 복제한다. 에셋만 등록하면 목록에 나타난다.

전력·체력·화물 탭은 지금 4개(전력, 최대 체력, 재생, 화물)다. 5행이 되면 `EntryListStartY = -120`, 행 높이 50 기준으로 아래가 잘릴 수 있다. 구현 때 확인해서 다음 중 하나만 한다.

- 행 높이를 44 전후로 줄인다.
- 엔트리 목록에 스크롤을 단다.
- 수확량을 화물 버튼 바로 아래에 두고 패널 높이를 늘린다.

Surface Base 하단 레벨 요약(`levelsOnlySummary`)에도 이름이 한 줄 추가된다. 잘리면 요약 폰트/행간만 조정한다.

레이아웃 빌더(`PromptB33LayoutBuilder`, `PromptB33_3`, `PromptB33_4`)는 카탈로그를 순회하므로 에셋 등록 후 해당 빌더를 다시 돌려 엔트리 버튼을 재생성한다. Prefab YAML을 직접 고치지 않는다.

### 8-2. 상세 카드

지금 카드는 `현재 1 → 다음 2 (+1)`처럼 숫자 하나다. 수확량은 광물 세 줄로 바꾼다. 드론 스캔이 `isDroneScan` 분기를 쓰듯, `upgrade.cargo.yield` 전용 분기를 `ProgressionPanelView.SetSelectedUpgrade`에 둔다.

구매 전 예시:

```text
채굴 수확량  Lv.1/3
구리 칸을 캘 때 추가로 얻는 광물 수입니다. 추가분도 화물 무게에 포함됩니다.

현재
구리 +0  철 +0  리튬 +0
→ 다음
구리 +1  철 +0  리튬 +0

같은 타일에서 더 많이 얻지만, 한 번 만재량은 화물 업그레이드가 담당합니다.

필요 재료: 구리 10
```

최대 레벨:

```text
채굴 수확량  Lv.3/3
현재 구리 +3  철 +2  리튬 +1
최대 레벨입니다.
```

`ItemDisplayNames.Upgrade` / `UpgradeDescription`에 한국어를 추가한다. ID 원문 `upgrade.cargo.yield`가 화면에 나오면 안 된다.

### 8-3. 채굴 중 피드백

보너스가 들어갔는지 숫자만 바뀌면 체감이 없다. 최소 하나는 넣는다.

- 채굴 완료 HUD/로그: `구리 +1 (+1 수확)`
- 또는 인벤토리 수량 옆에 이번 채굴 획득량

월드 플로팅 텍스트를 새로 만들지 않아도 된다. 기존 채굴 결과 표시가 있으면 그 문자열만 확장한다.

---

## 9. 구현 순서

한 커밋/한 작업 단위로 이 순서를 지킨다. 에셋만 넣고 커밋 경로를 안 바꾸면 “산 것처럼 보이는 가짜 업글”이 된다.

### 9-1. 데이터

1. `DataIds.Upgrades.CargoYield` 추가.
2. `MineralBonusEntry` + `UpgradeLevelDefinition.MiningYieldBonuses`.
3. `MvpDataAssetBuilder`에 에셋 생성·카탈로그 등록.
4. `CatalogValidator` 수확 표 검증.
5. `ItemDisplayNames` 이름·설명.
6. Editor 메뉴로 에셋 생성 후 카탈로그 Dirty 저장.

### 9-2. 효과 조회

1. `IUpgradeEffectProvider.GetMiningYieldBonus`.
2. `UpgradeEffectProvider` 구현.
3. 테스트 스텁 4곳 + `F_S03` 메서드 수 9.
4. `ProgressionServiceTests`에 레벨별 광물 보너스 표 테스트. 기존 `F_F04_AllNineEffects`는 10종으로 이름을 맞춘다.

### 9-3. 채굴 커밋

1. `IntegrationRuntimeBinder.TryCommitMining`에 기본 exact + 보너스 partial.
2. 바인더가 `runtime.Progression.Effects`를 읽게 한다. Progression이 아직 없으면 보너스 0.
3. 중간 실패 시 인벤·전력 롤백은 지금과 같다. 보너스 partial 실패는 롤백 사유가 아니다.

가능하면 지급 순서를 바인더에서 빼서 `MiningYieldCommit` 같은 순수 함수/작은 서비스로 옮긴다. 바인더 테스트가 무거워지는 것을 막기 위함이다.

### 9-4. UI

1. 상세 카드 3광물 표시.
2. 전력·체력·화물 탭 5행 레이아웃 확인.
3. 레이아웃 빌더 재실행.
4. 채굴 획득 문자열에 보너스 표기.

### 9-5. 검증 후 손대지 않을 것

- 드릴 속도·효율, 심층 해금 조건.
- 타일 기본 수량.
- 화물 기본 50.
- 퀘스트 보상 표.
- Save 버전.

---

## 10. 조절 노브

플레이테스트 후 숫자만 바꿀 때 여기만 고친다.

| 노브 | 기본 | 올리면 | 내리면 |
| --- | --- | --- | --- |
| Lv.1 구리 비용 | 10 | 첫 구매를 더 고민하게 함. 12~15 권장 상한 | 드릴과 동시에 사기 쉬워짐 |
| Lv.2 구리/철 | 20 / 10 | 중층 목표로 남김 | 철 해금 직후 바로 살 수 있음 |
| Lv.3 전체 | 30 / 20 / 10 | 심층 장기 목표 | 리튬 10칸이면 본전이라 너무 이름 |
| 레벨별 보너스 | +1 / +2+1 / +3+2+1 | 미래 비용이 실제 올랐을 때만 | 칸 수 절감이 약해져 드릴과 차별이 사라짐 |
| 보너스 무게 | 일반 광물과 동일 | 변경 금지 | 변경 금지 |

올리지 말아야 할 것:

- 보너스 무게 0.
- 리튬 보너스를 Lv.1~2에 미리 지급.
- 타일 기본 수량을 2로 올리면서 이 업글까지 유지. 둘 중 하나만.

---

## 11. 테스트

자동 테스트가 Runtime 경로를 실제로 타야 완료다. 에셋 존재만으로 완료하지 않는다.

### EditMode

| ID | 내용 |
| --- | --- |
| Y-D01 | 카탈로그 ID `upgrade.cargo.yield`, 3레벨, §4 비용·보너스 표 일치 |
| Y-D02 | 다른 업글에 `miningYieldBonuses`가 있으면 검증 실패 |
| Y-D03 | 레벨 0/1/2/3에서 구리·철·리튬 보너스가 §4와 같음. 빈 ID는 0 |
| Y-D04 | 구매는 기존처럼 정규화 → CanAfford → Spend → 레벨 커밋. 중간 실패 시 레벨 불변 |
| Y-D05 | `UpgradeCategoryRules.Resolve`가 Capacity 탭 |
| Y-D06 | `ItemDisplayNames`가 한국어. ID 원문 없음 |
| Y-D07 | `F_S03` 메서드 수 9, Gameplay가 App.Progression을 참조하지 않음 |

### 커밋 단위 (바인더 또는 순수 커밋 헬퍼)

| ID | 내용 |
| --- | --- |
| Y-C01 | Lv.0 구리 칸 → 인벤 +1, 전력만 차감 |
| Y-C02 | Lv.1 구리 칸 → 인벤 +2 |
| Y-C03 | Lv.2 구리 +3, 철 +2, 리튬 +1(보너스 없음) |
| Y-C04 | Lv.3 리튬 칸 → +2 |
| Y-C05 | 기본분 무게가 남은 용량보다 크면 실패. 타일·전력·인벤 불변 |
| Y-C06 | 기본분은 들어가고 보너스는 자리만큼만. 채굴은 성공 |
| Y-C07 | 퀘스트 지급 `TryAddManyExact`는 보너스를 더하지 않음 |
| Y-C08 | 암석(수량 0)은 보너스 0 |

### PlayMode / 레이아웃

| ID | 내용 |
| --- | --- |
| Y-P01 | Surface Base 전력·체력·화물 탭에 “채굴 수확량” 엔트리. 구매 후 레벨 표시 |
| Y-P02 | 상세 카드에 구리/철/리튬 보너스가 줄 단위로 보임 |
| Y-P03 | 통합 Scene에서 구리 타일 채굴 시 인벤이 기본+보너스만큼 증가 |
| Y-P04 | 5번째 엔트리가 패널 밖으로 안 잘림 |
| Y-P05 | 구세이브 로드 시 레벨 0, 보너스 0. 구매 후 저장·이어하기 시 유지 |

---

## 12. 작업 체크리스트

구현 에이전트는 이 순서로 닫는다.

1. [ ] `DataIds` + `MineralBonusEntry` + `UpgradeLevelDefinition` 확장
2. [ ] `MvpDataAssetBuilder`로 에셋 생성, 카탈로그 등록
3. [ ] `CatalogValidator` + `ItemDisplayNames`
4. [ ] `IUpgradeEffectProvider` + Provider 구현 + 스텁/F_S03
5. [ ] `TryCommitMining` 기본 exact / 보너스 partial
6. [ ] Progression 상세 카드 3광물 표시
7. [ ] 탭 5행 레이아웃 확인, 필요 시 빌더 재실행
8. [ ] EditMode Y-D*, Y-C* 통과
9. [ ] PlayMode Y-P* 또는 통합 Scene 수동 채굴 1회 기록
10. [ ] 이 문서 §4와 에셋 숫자가 같은지 최종 대조

---

## 13. 비목표

- 광물별 별도 업그레이드 3종으로 쪼개기.
- 드릴 탭으로 옮기기.
- 타일 기본 수량 변경.
- 화물 무게 공식 변경.
- 퍼센트 수확, 확률 추가 드롭, 자동 제련.
- 새 광물을 이 PR에서 추가.
- Save 마이그레이션.
- 채굴 획득 월드 플로팅 텍스트 신설. 기존 HUD 문자열 확장만.

---

## 14. 구현 후 기록 위치

작업이 끝나면 이 문서를 고치지 말고 `work_process/MVP2/prompt-b97-mining-yield-upgrade-result.md`를 새로 만든다. 결과 문서에는 실제 파일 목록, 테스트 결과, 레이아웃 빌더를 돌렸는지, §4와 다른 숫자가 있으면 그 이유만 적는다.
