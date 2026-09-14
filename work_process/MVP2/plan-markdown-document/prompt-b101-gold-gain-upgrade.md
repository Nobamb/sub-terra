# prompt-B 101 골드 획득 업그레이드 — 밸런스 확정 및 구현 계획

| 항목 | 내용 |
| --- | --- |
| **문서 제목** | Gold Gain Upgrade (prompt-B 101) |
| **날짜** | 2026-09-13 |
| **상태** | Draft — 수치 합의 완료. 구현 없음 |
| **원문** | `init/prompt-B.md` 101번 |
| **관련 코드** | `UpgradeData`, `UpgradeEffectProvider`, `IUpgradeEffectProvider`, `EconomyService`, `EconomyPricing`, `MiningYieldCommit`, `OutpostService`, `GoldPickupPresentation`, `ProgressionPanelView`, `CatalogValidator`, `MvpDataAssetBuilder`, `IResourceWallet` |
| **범위** | 판매·정산·골드 블록 채굴 골드에 퍼센트 보너스를 주는 업그레이드 1종. 전력·체력·화물 탭. 골드+광물 혼합 비용 |
| **유지할 것** | 광물 단가, 골드 블록 확률·기본 지급량, 화물 무게, 퀘스트 골드, 채굴 수확량(97), `saveVersion` |

숫자만 바꾸려면 **§4 확정표**와 **§11 조절 노브**를 고치면 된다. 원칙은 §2, 경제 기준은 §3.

---

## 0. 한 줄 결론

확정 곡선은 **Lv.1 +50% / Lv.2 +75% / Lv.3 +100%(2배)** 이다.

원문 30/60/100은 첫 구매 체감이 약하고, 50/100/150은 중층에서 이미 2배가 되며, 50/75/110은 2배 헤드라인을 버린다. 50/75/100은 첫 만재 직후 다음 정산에서 보이고, 더블은 심층 사치로 남긴다.

이 업글은 깊이·경로·위험을 바꾸지 않는다. 같은 행동을 하고 정산 숫자만 커진다. 그래서 **보너스를 판매 미리보기·골드칸 연출·HUD에 따로 보여 주지 않으면 가짜 업글**이 된다.

판정 문장:

> “만재 구리를 팔았더니 330G가 아니라 330G + 165G 보너스로 들어왔다. 암석 골드칸을 캐자 20G + 10G 보너스가 머리 위에 떴다.”

이 문장을 못 만들면 기능이 아직 완결되지 않은 것이다.

---

## 1. 원안과 확정 차이

prompt-B 101 원문:

- 판매 또는 골드 블록 채굴 시 추가 골드
- Lv.1 30% / 500G + 구리 10
- Lv.2 60% / 1000G + 철 10
- Lv.3 100% / 3000G + 리튬 10
- 탭: 전력·체력·화물

검토 후 버린 곡선:

| 곡선 | 버린 이유 |
| --- | --- |
| 30 / 60 / 100 | Lv.1이 330→429라 500G 대비 약함. 암석 골드 20→26은 연출 대비 안 보임 |
| 50 / 100 / 150 | 첫맛은 좋으나 Lv.2에서 이미 2배. 초기화 500G·탈출 100G 긴장이 중층에서 죽음 |
| 50 / 75 / 110 | 75는 좋으나 110은 2배로 안 읽히고, 660→693(+33G)은 체감이 없음 |

확정:

| 항목 | 원문 | 확정 |
| --- | --- | --- |
| 퍼센트 | 30 / 60 / 100 | **50 / 75 / 100** |
| 골드 비용 | 500 / 1000 / 3000 | 유지 |
| 광물 비용 | 구리 10 / 철 10 / 리튬 10 | 유지 |
| 적용 | 판매, 골드 블록 | 판매 + 전진기지 정산 + 골드 블록. 퀘스트 골드 제외 |

비용은 원문 그대로 둔다. 퍼센트만 첫 구매 체감에 맞게 올린다. Lv.3 2배는 헤드라인으로 남긴다.

---

## 2. 설계 원칙

구현·수치 조정 때 이 아홉 줄을 유지한다. 깨려면 이유를 §11에 적는다.

1. 보너스는 **기본 골드에 대한 정수 퍼센트 가산**이다. 단가를 바꾸지 않는다.
2. 적용 범위는 **광물→골드 변환**과 **골드 블록 채굴**뿐이다. 퀘스트·치트·`AddGold` 직접 호출에는 적용하지 않는다.
3. 퍼센트는 칸·개수가 아니라 **그 거래의 기본 골드 합**에 한 번 곱한다. 철 1개씩 두 번 반올림하면 15×50%가 7.5+7.5가 된다.
4. 반올림은 `MidpointRounding.AwayFromZero` (0.5는 올림). 기존 최대 전력·화물 손실과 같다.
5. 보너스는 `GameState.AddGold` 안에 넣지 않는다. 넣으면 퀘스트·치트·차감까지 배가된다.
6. 월드 타일의 `goldDrop`은 **기본량**이다. 생성기가 업글 레벨을 읽지 않는다.
7. 골드 비용과 광물 비용은 **한 트랜잭션**이다. 골드만 깎이고 레벨이 안 오르거나, 광물만 깎이는 상태를 만들지 않는다.
8. `currency.gold`는 광물이 아니다. 인벤·화물 무게·광물 카탈로그에 넣지 않는다.
9. 더블(100%)은 **Lv.3만**. 중층에서 2배를 주지 않는다.

---

## 3. 현재 빌드 기준값

보상·비용을 이 값으로 계산했다. 데이터가 바뀌면 이 절과 §4를 같이 고친다.

### 3-1. 경제

| 항목 | 값 |
| --- | ---: |
| 시작 골드 | 0 |
| 구리 / 철 / 리튬 단가 | 10 / 15 / 40G |
| 기본 화물 | 50 |
| 만재 구리 판매 | 약 330G |
| 긴급 탈출 | 100G + 최대 전력 10% |
| 새 광산 (유료) | 500G, 결제마다 2배 |
| 3시간 자동 초기화 후 유료 비용 | 다시 500G |
| 기존 업글 최고가 (골드 환산) | 드릴 속도 Lv.3 ≈ 500G, 수확량 Lv.3 ≈ 1,000G |
| 골드 본수입 | 광물 판매 |
| 골드 블록 풀클리어 기대 | 약 1,960G (`prompt-b100-gold-drop-tiles.md`) |
| 왕복 1회 골드 블록 기대 | 약 20~90G + 리튬 잭팟 0 또는 200G |

지금 업글·건물은 광물 비용이다. 골드 싱크는 탈출 100G / 새 광산 500G / (이 업글) 세 곳이다. 골드를 늘려도 드릴이 세지거나 심층이 열리지는 않는다. 깨질 수 있는 것은 탈출·초기화 긴장뿐이다.

### 3-2. 골드 지급 경로 (현재)

| 경로 | 기본 골드 | 이 업글 |
| --- | --- | --- |
| `EconomyService.TrySellMineral` | 단가 × 수량 | **적용** |
| `OutpostService` 화물/보관함 정산 | 단가 × 수량 | **적용** (판매와 같은 변환) |
| `MiningYieldCommit` `goldGrant` | 타일 `goldDrop` | **적용** |
| `QuestRewardService` | 퀘스트 표 | 제외 |
| `GameState.AddGold` 직접 | 호출자 값 | 제외. 보너스를 여기 넣지 않음 |
| 긴급 탈출 / 광산 초기화 | 차감 | 제외 (수입이 아님) |

### 3-3. 지갑 공백

`IResourceWallet.TrySpend` / `CanAfford`는 광물 카탈로그 + 인벤만 본다. 골드 비용 ID를 넣으면 지금 `알 수 없는 비용 항목`으로 거절된다. 이 업글이 **골드+광물 혼합 비용의 첫 사용처**다. 지갑을 같이 열지 않으면 에셋만 있고 살 수 없는 가짜 업글이 된다.

---

## 4. 확정표 (수정 지점)

이 표가 구현 원본이다. 칸만 고치면 된다.

| 레벨 | 추가 골드 | 배율 | 골드 | 구리 | 철 | 리튬 | 만재 구리 판매 |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | +50% | 1.5배 | 500 | 10 | 0 | 0 | 330 → 495 |
| 2 | +75% | 1.75배 | 1000 | 0 | 10 | 0 | 330 → 578 |
| 3 | +100% | 2.0배 | 3000 | 0 | 0 | 10 | 330 → 660 |

골드 블록 예시 (기본량 × 보너스):

| 기본 | Lv.1 | Lv.2 | Lv.3 |
| ---: | ---: | ---: | ---: |
| 암석 20G | 20+10=30 | 20+15=35 | 20+20=40 |
| 가스 50G | 50+25=75 | 50+38=88 | 50+50=100 |
| 구리 50G | 50+25=75 | 50+38=88 | 50+50=100 |
| 철 75G | 75+38=113 | 75+56=131 | 75+75=150 |
| 리튬 200G | 200+100=300 | 200+150=350 | 200+200=400 |

반올림 검산 (거래 합 기준, 0.5 올림):

| 기본 | 50% | 75% | 100% |
| ---: | ---: | ---: | ---: |
| 15 (철 1개) | 8 | 11 | 15 |
| 30 (철 2개) | 15 | 23 | 30 |
| 75 (철 골드칸) | 38 | 56 | 75 |

철 1개를 두 번 팔면 8+8=16이고, 2개를 한 번에 팔면 15다. 이는 거래 단위 반올림이라 허용한다. 단가에 퍼센트를 미리 곱하지 않는다.

권장 ID / 표시:

| 항목 | 값 |
| --- | --- |
| 영구 ID | `upgrade.cargo.gold` |
| 표시 이름 | 골드 획득 |
| 탭 | 전력·체력·화물 (`UpgradeCategory.Capacity`) |
| `effectValue` | 50 / 75 / 100 (퍼센트 정수). 지급 계산도 이 값 |
| 골드 비용 ID | `currency.gold` (`DataIds.Currency.Gold`) |

ID를 `upgrade.cargo.*`로 두는 이유: `UpgradeCategoryRules.Resolve`가 이 prefix만 용량 탭으로 보낸다. 새 prefix를 만들지 않는다.

`effectValue`를 0.5/0.75/1.0으로 두지 않는다. UI와 계산이 50/75/100을 그대로 쓴다.

---

## 5. 밸런스 검토

### 5-1. 이 업글이 실제로 바꾸는 것

바뀌는 것은 **왕복당 현금**이다. 만재 개수, 채굴 칸 수, 화물 무게는 97 수확량·화물 업글 몫이다.

| 레벨 | 만재 구리 | 왕복 기대(판매+골드칸) | Lv.1 대비 |
| ---: | ---: | ---: | ---: |
| 0 | 330G | ~350–370G | — |
| 1 | 495G | ~525–555G | 다음 판매에서 바로 보임 |
| 2 | 578G | ~610–650G | 거의 두 배 직전 |
| 3 | 660G | ~700–740G | 2배. 심층 사치 |

97 수확량은 같은 만재를 더 적은 타일로 채운다(시간). 101은 그 만재의 골드만 키운다(금액). 두 계열은 곱해지지 않고 보완한다. 수확량이 골드칸 기본량을 늘리지도 않는다.

### 5-2. 본전

만재 구리 330G, 추가분만.

| 구매 | 추가분/왕복 | 본전 |
| --- | ---: | ---: |
| Lv.1 500G | 165G | **약 3왕복** |
| Lv.2 1000G (Lv.1 대비 +25%p) | 83G | 약 12왕복 |
| Lv.3 3000G (Lv.2 대비 +25%p) | 83G | 약 36왕복 |

Lv.1은 첫 만재 2회 분량(구리 10을 남기고 500G를 모음) 뒤에 누르고, 바로 다음 정산에서 확인한다. 이게 도파민 구간이다.

Lv.2·Lv.3는 ROI가 아니다. 중층·심층 싱크이자 2배 헤드라인이다. 본전이 길다고 비용을 내리지 않는다. 내리면 초기화가 공짜가 된다.

Lv.1 500G는 새 광산 초기화와 같은 가격이다. 의도된 선택이다. +50%면 “새 맵 vs 다음 판매 +165G”가 고민되고, +30%면 새 맵이 이긴다.

### 5-3. 지금 빌드를 깨는가

**판매 경제는 안 깨진다. 골드 싱크 긴장은 후반에 약해진다.**

- 업글·건물은 광물값이라 골드 배율이 전투력을 안 산다.
- 풀클리어 광물 ~4,000G + 골드칸 ~1,960G. Lv.3면 약 1.2만G. 초기화 500→1000→2000을 여러 번 살 수 있다. 그건 후반 “돈으로 새 땅”이지 진행 스킵이 아니다.
- 중층 Lv.2는 1.75배라 아직 2배가 아니다. 탈출 100G는 줄어 보이지만 공짜는 아니다.
- 리튬 골드칸 Lv.3 = 400G. 한 방이 초기화 1회에 못 미친다. 150%안(500G)보다 안전하다.

### 5-4. 위험과 막을 것

| 위험 | 결과 | 차단 |
| --- | --- | --- |
| `AddGold`에 퍼센트 삽입 | 퀘스트·치트·음수 차감까지 왜곡 | 판매/정산/채굴 커밋 세 곳만 |
| 단가에 퍼센트 적용 | 미정산 가치 HUD·미리보기가 업글에 묶임 | 단가 불변. 지급 순간에만 가산 |
| 타일 `goldDrop`에 미리 곱함 | 시드 월드가 레벨에 종속 | 생성기는 기본량만 |
| 골드 비용과 광물 비용 분리 커밋 | 골드만 깎이거나 레벨만 오름 | 지갑 한 메서드 |
| `currency.gold`를 광물 등록 | 인벤에 골드가 쌓임 | 카탈로그 광물 금지. 지갑만 특수 처리 |
| 건물 레시피에 골드 비용 허용 | 의도치 않은 골드 싱크 | 카탈로그 검증: 이 업글만 `currency.gold` 허용 |
| 보너스 미표시 | 산 보람이 없음 | 판매 미리보기 + 결과 + 골드칸 텍스트 |
| Lv.2를 100%로 “개선” | 중층에서 경제 종료 | §4 금지. 더블은 Lv.3 |

### 5-5. 판정

| 질문 | 답 |
| --- | --- |
| 원안 구조를 쓰는가 | 예. 3레벨, 용량 탭, 골드+깊이 광물 비용 |
| 원안 숫자를 쓰는가 | 비용은 예. 퍼센트는 50/75/100 |
| 지금 빌드를 깨는가 | 지갑 원자성과 퀘스트 제외를 지키면 아님 |
| 도파민이 사는가 | Lv.1 +50%와 보너스 분리 표시가 있으면 예 |
| 지금 비용을 더 올리라고 하는가 | 아니오. Lv.3 3000G는 싱크 목적 |

---

## 6. 데이터 모델

### 6-1. ID

```csharp
public static class DataIds
{
    public static class Currency
    {
        public const string Gold = "currency.gold";
    }

    public static class Upgrades
    {
        public const string CargoGold = "upgrade.cargo.gold";
        // 기존 CargoYield 등 유지
    }
}
```

`currency.gold`는 `DataIdRules` (`a-z` + 점) 을 통과한다. 광물 prefix `mineral.`이 아니다.

### 6-2. 레벨 정의

새 리스트 필드는 필요 없다. `effectValue` = 50/75/100, `costs`에 골드+광물.

| 레벨 | effectValue | costs |
| ---: | ---: | --- |
| 1 | 50 | `currency.gold` 500, `mineral.copper` 10 |
| 2 | 75 | `currency.gold` 1000, `mineral.iron` 10 |
| 3 | 100 | `currency.gold` 3000, `mineral.lithium` 10 |

`miningYieldBonuses`는 비어 있어야 한다. 97 검증이 수확량 전용으로 막는다.

### 6-3. 카탈로그 검증

`CatalogValidator.ValidateCosts` / `ValidateUpgrades`에 규칙을 추가한다.

- `currency.gold`는 **`upgrade.cargo.gold`의 각 레벨에 1회**, 수량 > 0.
- 다른 업글·건물·레시피 비용에 `currency.gold`가 있으면 실패.
- 이 업글 레벨에 골드 비용이 없으면 실패.
- `effectValue`는 50/75/100과 일치. 다른 값이면 실패 (퍼센트 오타 방지).
- 레벨이 오를 때 퍼센트는 단조 증가.

건물 검증이 광물 ID만 허용하던 관행은 유지한다. 골드 특수 처리는 이 업글 비용에만 연다.

### 6-4. 에셋·카탈로그 등록

`MvpDataAssetBuilder.BuildUpgrades()`에 한 줄. 산출물:

- `Assets/_Project/Data/Upgrades/Upgrade_Cargo_Gold.asset`
- `GameDataCatalog.upgrades` 명시 등록
- `DataIds.Upgrades.CargoGold`

헬퍼 예: `GoldCopper(500, 10)`, `GoldIron(1000, 10)`, `GoldLithium(3000, 10)`.

주의: `SubTerra/Data/Build MVP Data Assets`는 건물·광물 표시 이름까지 덮어쓴 전례가 있다 (`prompt-b97-mining-yield-upgrade-result.md`). 전체 빌더를 돌리면 표시 이름을 restore 하거나, 수확량 때처럼 **에셋만 만들고 카탈로그 한 줄은 수동 등록**한다. YAML을 손으로 고치지 않는다.

---

## 7. 런타임 적용

### 7-1. 소유권

| 층 | 책임 |
| --- | --- |
| Gameplay `MiningSystem` | 타일 `goldDrop` 기본량만 커밋에 넘긴다. 퍼센트를 모른다 |
| Shared `IUpgradeEffectProvider` | `int GetGoldGainBonusPercent()` |
| App `UpgradeEffectProvider` | 현재 레벨 `effectValue`를 0/50/75/100으로 반환 |
| App `EconomyPricing` (또는 동일 순수 헬퍼) | 기본 골드 × 퍼센트 → 보너스 정수 |
| App `EconomyService` / `OutpostService` / `MiningYieldCommit` | 기본량 계산 후 헬퍼로 보너스 가산 |
| App `IResourceWallet` | `currency.gold`는 `Player.Gold`, 나머지는 인벤 |

Gameplay가 `SubTerra.App.Progression`을 참조하면 `F_S03`가 실패한다. 보너스는 App 커밋에서만 더한다.

### 7-2. Shared 계약

`IUpgradeEffectProvider`에 메서드 1개:

```csharp
int GetGoldGainBonusPercent();
```

규칙:

- 레벨 0, 데이터 없음, NaN, 음수 → 0
- 유효 값은 50 / 75 / 100만. 그 외는 0 (잘못된 세이브가 999%를 못 만듦)
- 인자는 없다. 광물별로 다르지 않다

`ProgressionStaticStructureTests.F_S03` 메서드 수는 **9 → 10**. 스텁을 모두 채운다.

현재 스텁 4곳 (97 기준, 구현 때 grep으로 재확인):

- `Tests/PlayMode/Gameplay/Mining/MiningSystemPlayModeTests.cs`
- `Tests/EditMode/Gameplay/Drone/DroneSensorUpgradeTests.cs`
- `Tests/EditMode/App/Run/RunFailureServiceTests.cs`
- `Tests/EditMode/App/Integration/GasExposureEffectControllerTests.cs`

기본 구현은 `return 0;`.

`ProgressionServiceTests.F_F04_AllTenEffects`는 **11종**으로 이름과 목록을 맞춘다.

### 7-3. 보너스 계산 (한곳)

`EconomyPricing`에 둔다. 판매 미리보기와 커밋이 같은 함수를 쓴다.

```csharp
public static int ComputeGoldBonus(int baseGold, int percent)
{
    if (baseGold <= 0 || percent <= 0)
    {
        return 0;
    }

    return (int)Math.Round(
        baseGold * (percent / 100.0),
        MidpointRounding.AwayFromZero);
}

public static bool TryAddBonus(
    int baseGold,
    int percent,
    int currentBalance,
    out int bonus,
    out int total,
    out string diagnostic)
```

`TryAddBonus`는 `base + bonus`가 `int.MaxValue - currentBalance`를 넘으면 false. 판매·정산은 실패(상태 불변). 채굴 골드칸은 기존처럼 **남는 칸만큼만** 지급해도 된다. 문서화: 판매는 거부, 채굴은 클램프. 이유는 판매는 광물을 돌려줘야 하고, 채굴 타일은 이미 기본량 클램프가 있다.

`baseGold == 0`이면 보너스 0. 공짜 판매가 골드를 찍어내지 않는다.

### 7-4. 판매 `TrySellMineral`

현재: 단가×수량 검사 → 인벤 차감 → `AddGold(goldGain)`.

변경:

1. 기존대로 `baseGold` 계산·인벤 보유 검사.
2. `percent = effects.GetGoldGainBonusPercent()` (EconomyService가 Provider를 모르면 GameState/Progression에서 읽거나, 생성자에 `IUpgradeEffectProvider`를 optional로 받는다. null이면 0).
3. `TryAddBonus(baseGold, percent, currentGold, ...)`. 실패 시 `GoldOverflow`, 인벤 불변.
4. 인벤 차감 성공 후에만 `AddGold(total)`.
5. `GoldDelta` = **total** (HUD 잔액과 일치).
6. 결과 메시지에 보너스가 있으면 기본/보너스를 분리한다. 예: `판매 완료  330G + 165G 보너스`.

Presenter가 단가×수량만 보여 주면 산 보람이 없다. 미리보기도 `ComputeGoldBonus`를 쓴다.

`EconomyService` 생성자에 Provider를 추가할 때 **기존 테스트 `new EconomyService(inv, catalog, state)`가 컴파일되도록 optional null**을 유지한다. null = 보너스 0.

### 7-5. 전진기지 정산

`OutpostService`의 단가×수량 골드 지급 두 경로(단일 광물 정산, 화물/보관함 `TrySettle`)에 같은 헬퍼를 적용한다. 메시지 `+N G`는 total. 보너스 있으면 `+330G (+165G 보너스)`.

정산 콘솔을 빼 먹으면 Surface에서만 보너스가 나와 플레이어가 정산을 기피한다. 원문 “판매”는 광물→골드 변환이다.

### 7-6. 채굴 골드칸

`MiningYieldCommit.TryCommit`은 지금 `goldGrant`를 그대로 `AddGold`한다.

변경:

1. `baseGold = max(0, goldGrant)` (타일 기본량).
2. `percent = effects?.GetGoldGainBonusPercent() ?? 0`.
3. `bonus = ComputeGoldBonus(baseGold, percent)`.
4. `total = min(base+bonus, int.MaxValue - currentGold)` (기존 클램프).
5. `AddGold(total)`.
6. HUD / 픽업 연출에 base와 bonus를 넘긴다.

인벤 가득으로 광석 채굴이 실패하면 지금도 골드 0이다. 보너스도 0. 암석·가스(광물 없음)는 기존처럼 골드만 주고, 그 골드에 보너스를 붙인다.

### 7-7. 골드+광물 지갑

`EconomyService.TryValidateSpend` / `TrySpend`:

1. `CostAggregator.TryNormalize` (변경 없음. 골드 ID도 합산).
2. 항목을 `currency.gold`와 그 외로 나눈다.
3. 골드: `gameState.Player.Gold >= need`. 카탈로그 광물 조회를 하지 않는다.
4. 나머지: 지금처럼 광물 카탈로그 + 인벤 수량.
5. 커밋 순서: 광물 `TryReduceMany` → 성공 시에만 골드 차감.
6. 골드 차감은 `AddGold(-need)`로 하되, 사전 검사 때문에 0 미만이 되면 안 된다. 검사와 차감 사이에 다른 시스템이 골드를 쓰면 실패로 돌리고 광물을 롤백해야 한다. 광물 롤백이 없으면 **한 메서드에서 골드 잔액 검사를 차감 직전에 한 번 더** 하고, 부족하면 `TryReduceMany` 이전으로 못 돌아가니 순서를 **골드 잔액 재검사 → 광물 차감 → 골드 차감**으로 둔다. 골드 차감은 음수 클램프가 아니라 `Gold -= need` (이미 검사함).

`gameState` null이면 골드 비용이 있는 요청은 `DependencyMissing`. 지금 지갑은 광물만 써서 `gameState` 없이 CanAfford가 통과할 수 있다. 골드 비용이 생기면 GameState가 필수다.

부분 차감 금지. 실패 시 골드·인벤·업글 레벨 불변. `ProgressionService` 구매 순서는 그대로 정규화 → CanAfford → TrySpend → 레벨 커밋.

### 7-8. 적용하지 않는 경로

| 경로 | 이유 |
| --- | --- |
| `QuestRewardService` | 클리어 표가 이미 보상. 배면 퀘스트 골드가 초기화를 연다 |
| `GameState.AddGold` / `PlayerState.AddGold` | 모든 수입·치트가 배가됨 |
| 월드 드롭 폴백 | 인벤 없는 테스트 경로. 생산은 바인더 커밋 |
| 긴급 탈출·광산 초기화 | 싱크. 보너스를 붙이면 비용이 줄어 듦 |
| 미정산 가치 HUD | 단가×중량. 업글 전 가치와 판매 후 골드가 달라 보이는 것은 허용 (미리보기에 보너스를 보여 줌) |

### 7-9. 저장

업그레이드 레벨만 저장된다. 새 Save 필드 없음. `saveVersion` 올리지 않는다. 구세이브는 레벨 0 → 보너스 0.

---

## 8. UI

### 8-1. 탭·목록

에셋만 등록하면 `EnsureUpgradeButtons`가 용량 탭에 한 줄 추가한다.

지금 전력·체력·화물 탭은 5행(전력, 최대 체력, 재생, 화물, 수확량)이다. 6행이 되면 잘릴 수 있다. 구현 때 확인해서 다음 중 하나만 한다.

- 행 높이를 더 줄인다 (지금 44).
- 엔트리 목록에 스크롤을 단다.
- 패널 높이를 늘린다.

Surface Base 하단 레벨 요약에 이름이 한 줄 는다. 잘리면 요약 폰트/행간만 조정한다.

레이아웃 빌더는 **UpgradePanel / Integration Scene만**. Surface Base·설정·인벤 프리팹을 저장하지 않는다 (`rule.md` 2-5). 97 때 `PromptB97YieldUpgradeLayoutBuilder`와 같은 전용 빌더를 새로 만든다.

### 8-2. 상세 카드

퍼센트 업글이라 기존 `현재 1 → 다음 2` 숫자 카드로도 읽을 수 있다. 다만 50/75/100을 **+50%** 로 보여야 한다. `0.##`만 찍으면 `50`으로 보여 퍼센트인지 불명이다.

`upgrade.cargo.gold` 전용 분기:

구매 전 예시:

```text
골드 획득  Lv.1/3
광물 판매·정산과 골드 블록 채굴로 받는 골드가 늘어납니다. 퀘스트 보상에는 적용되지 않습니다.

현재 +0%
→ 다음 +50%

만재 구리 330G 기준 495G가 됩니다.

필요 재료: 500G, 구리 x10
```

최대 레벨:

```text
골드 획득  Lv.3/3
현재 +100% (2배)
최대 레벨입니다.
```

비용 표시: `ItemDisplayNames.Mineral`만 쓰면 `currency.gold x500`이 화면에 나온다. 비용 줄은 `ItemDisplayNames.Cost(itemId, qty)`로 통일한다.

- `currency.gold` → `500G`
- 광물 → `구리 x10`

부족 문구 `자원 부족 (인벤토리 보유량 기준)`은 골드 비용이 있으면 `자원 부족 (골드·인벤토리 보유량 기준)`으로 바꾼다. 이 업글만이 아니라, 지갑이 골드를 보게 된 뒤의 공통 문구다.

`ItemDisplayNames.Upgrade` / `UpgradeDescription`에 한국어를 추가한다. ID 원문이 화면에 나오면 안 된다.

### 8-3. 판매 미리보기·결과

미리보기 행이 `단가 × 수량`만 보여 주면 Lv.1을 사고도 330G로 보인다. 보너스 줄을 넣는다.

- 미리보기: `330G + 165G 보너스`
- 성공 토스트/상태: 같은 형식
- `GoldDelta`는 total이라 좌상단 골드는 495로 오른다

보너스 0이면 지금 문자열 유지.

### 8-4. 골드 블록 연출

`GoldPickupPresentation.FormatPickupText`를 확장한다. 기존 `(int acceptedGold)` 단독 호출은 total만 넘긴 것으로 취급해 `20G 골드 획득!`을 유지한다.

보너스 > 0:

```text
20G + 10G 보너스!
```

색·폰트·타임라인은 100-1 유지 (`#FFFB19FF` / `#FFFD96FF` / `SeoulAlrimTTF-Heavy`). 문구만 기본+보너스로 나눈다.

채굴 HUD (`MiningYieldCommit.FormatHudFeedback`):

- 보너스 0: `골드 +20G` (현재)
- 보너스 > 0: `골드 +30G (+10G 보너스)`

금화 개수는 바꾸지 않는다. 숫자가 연출이다.

---

## 9. 구현 순서

한 작업 단위로 이 순서를 지킨다. 에셋만 넣고 지갑·커밋을 안 바꾸면 살 수도, 효과도 없는 가짜 업글이 된다.

### 9-1. 순수 계산 + 지갑

1. `DataIds.Currency.Gold`, `DataIds.Upgrades.CargoGold`.
2. `EconomyPricing.ComputeGoldBonus` / `TryAddBonus` + EditMode 반올림 표.
3. `EconomyService`가 `currency.gold`를 Player.Gold로 검사·차감. 광물과 한 트랜잭션.
4. 기존 광물-only `TrySpend` 테스트가 통과하는지 확인 (골드 항목 없으면 동작 동일).

### 9-2. 데이터

1. `MvpDataAssetBuilder` 에셋 + 카탈로그 등록 (표시 이름 덮어쓰기 주의).
2. `CatalogValidator` 골드 비용 허용 범위.
3. `ItemDisplayNames` 이름·설명·Cost 포맷.

### 9-3. 효과 조회

1. `IUpgradeEffectProvider.GetGoldGainBonusPercent`.
2. `UpgradeEffectProvider` 구현.
3. 테스트 스텁 + `F_S03` 메서드 수 10.
4. `F_F04` 11종.

### 9-4. 지급 경로

1. `TrySellMineral` 보너스 + 오버플로 거부.
2. `OutpostService` 정산 동일.
3. `MiningYieldCommit` 골드칸 보너스 + HUD 문자열.
4. 퀘스트 지급 회귀: 보너스 0.

### 9-5. UI

1. 상세 카드 퍼센트·2배 문구, 비용 `500G, 구리 x10`.
2. 판매 미리보기 보너스 줄.
3. 골드칸 `20G + 10G 보너스!`.
4. 용량 탭 6행 레이아웃. 전용 빌더, UpgradePanel만 저장.

### 9-6. 검증 후 손대지 않을 것

- 광물 단가 10/15/40.
- 골드 블록 확률 1/5/10%와 기본 20/50/단가×5.
- 채굴 수확량 보너스 표.
- 퀘스트 보상 표.
- 탈출 100G, 초기화 500G×2배.
- Save 버전.
- Surface Base / 설정 창 프리팹 (요청에 없음).

---

## 10. 조절 노브

플레이테스트 후 숫자만 바꿀 때 여기만 고친다. 카탈로그 `effectValue`와 비용, 검증 허용 퍼센트 집합을 같이 고친다.

| 노브 | 기본 | 올리면 | 내리면 |
| --- | --- | --- | --- |
| Lv.1 퍼센트 | 50 | 첫 판매가 더 큼. 60 이상은 초기화와 경쟁에서 업글이 이김 | 30은 체감 부족으로 이미 버림 |
| Lv.2 퍼센트 | 75 | 80은 허용. **100은 금지** (더블이 중층) | 60은 50과 간격이 없음 |
| Lv.3 퍼센트 | 100 | 125는 “더블 위 한 단계”. 150은 초기화 남발 | 110은 체감 없음으로 이미 버림 |
| Lv.1 골드 | 500 | 새 광산과 더 강하게 경쟁 | 250이면 첫 만재 직후. 도파민↑, 초기화 선택↓ |
| Lv.2 골드 | 1000 | 중층 목표로 남김 | 너무 싸면 75%가 기본값이 됨 |
| Lv.3 골드 | 3000 | 2배를 엔딩 싱크로 유지 | 내리면 ROI가 짧아져 초기화가 공짜 |
| 광물 게이트 | 구리10 / 철10 / 리튬10 | 깊이 게이트 강화 | 0이면 골드만의 저축 미션 |

올리지 말아야 할 것:

- Lv.2를 100%로 만드는 것.
- 퀘스트 골드에 배율을 거는 것.
- `AddGold` 전역 배율.
- 골드칸 기본량(20/50/200)을 이 업글과 동시에 올리는 것. 둘 중 하나만.

---

## 11. 테스트

자동 테스트가 Runtime 경로를 실제로 타야 완료다. 에셋 존재만으로 완료하지 않는다.

### EditMode — 데이터

| ID | 내용 |
| --- | --- |
| G-D01 | 카탈로그 ID `upgrade.cargo.gold`, 3레벨, §4 비용·퍼센트 일치 |
| G-D02 | 다른 업글·건물에 `currency.gold`가 있으면 검증 실패 |
| G-D03 | 이 업글 레벨에 골드 비용 누락·퍼센트 오타면 검증 실패 |
| G-D04 | `UpgradeCategoryRules.Resolve`가 Capacity 탭 |
| G-D05 | `ItemDisplayNames`가 한국어. `upgrade.cargo.gold` / `currency.gold` 원문 없음. 비용은 `500G` |
| G-D06 | `F_S03` 메서드 수 10, Gameplay가 App.Progression을 참조하지 않음 |

### EditMode — 계산

| ID | 내용 |
| --- | --- |
| G-M01 | 레벨 0/1/2/3 → 퍼센트 0/50/75/100. 잘못된 레벨 → 0 |
| G-M02 | 기본 15, 50% → 보너스 8. 30, 50% → 15. 75, 75% → 56 |
| G-M03 | 기본 0 또는 퍼센트 0 → 보너스 0 |
| G-M04 | 기본+보너스가 잔액 한도를 넘으면 `TryAddBonus` false |

### EditMode — 지갑

| ID | 내용 |
| --- | --- |
| G-W01 | 골드 500+구리 10, 둘 다 있으면 CanAfford true |
| G-W02 | 골드만 부족 / 구리만 부족 → false, 잔액·인벤 불변 |
| G-W03 | TrySpend 성공 시 골드 −500, 구리 −10, 한 호출 |
| G-W04 | 광물-only 비용은 기존과 동일 (골드 0 변화) |
| G-W05 | 구매 정규화 → CanAfford → Spend → 레벨 커밋. 중간 실패 시 레벨 불변 |

### EditMode — 지급

| ID | 내용 |
| --- | --- |
| G-C01 | Lv.0 구리 2개 판매 → +20G. 인벤 −2 |
| G-C02 | Lv.1 만재 330 기본 → +495G (330+165) |
| G-C03 | Lv.3 구리 1개 → +20G (10+10) |
| G-C04 | 판매 오버플로(보너스 포함) → 인벤·골드 불변 |
| G-C05 | 골드칸 `goldGrant` 20, Lv.1 → 잔액 +30. HUD에 보너스 표기 |
| G-C06 | 인벤 가득 광석 실패 → 골드 0 (보너스도 0) |
| G-C07 | 퀘스트 `AddGold(100)` 경로 → +100, 보너스 없음 |
| G-C08 | 정산 경로도 C02와 같은 퍼센트 |
| G-C09 | Provider null인 EconomyService는 보너스 0 (기존 테스트 호환) |

### PlayMode / 레이아웃

| ID | 내용 |
| --- | --- |
| G-P01 | Surface Base 용량 탭에 “골드 획득”. 구매 후 레벨 표시 |
| G-P02 | 상세 카드에 +50% / 필요 재료 500G, 구리 x10 |
| G-P03 | 판매 미리보기에 보너스 줄. 판매 후 잔액이 total |
| G-P04 | 통합 Scene 골드칸 채굴 시 `20G + 10G 보너스!` (Lv.1) |
| G-P05 | 6번째 엔트리가 패널 밖으로 안 잘림 |
| G-P06 | 구세이브 로드 시 레벨 0, 보너스 0. 구매 후 저장·이어하기 시 유지 |

---

## 12. 작업 체크리스트

구현 에이전트는 이 순서로 닫는다.

1. [ ] `DataIds` + `EconomyPricing` 보너스 헬퍼
2. [ ] `EconomyService` 골드+광물 원자 차감
3. [ ] `MvpDataAssetBuilder`로 에셋 생성, 카탈로그 등록 (표시 이름 부작용 확인)
4. [ ] `CatalogValidator` + `ItemDisplayNames`
5. [ ] `IUpgradeEffectProvider` + Provider + 스텁/`F_S03`/`F_F04`
6. [ ] 판매·정산·채굴 커밋에 보너스 적용. 퀘스트 제외
7. [ ] 상세 카드, 판매 미리보기, 골드칸 문구
8. [ ] 용량 탭 6행 레이아웃. UpgradePanel만 저장
9. [ ] EditMode G-D* / G-M* / G-W* / G-C* 통과
10. [ ] PlayMode G-P* 또는 통합 Scene 판매 1회 + 골드칸 1회 기록

---

## 13. 관련 문서

- `init/prompt-B.md` 101번 (원문), 100 / 100-1 (골드 블록·연출)
- `work_process/MVP2/prompt-b97-mining-yield-upgrade.md` (용량 탭 추가 패턴)
- `work_process/MVP2/prompt-b100-gold-drop-tiles.md` (기본 골드칸 경제)
- `work_process/MVP2/new-mine-reset.md` (500G 싱크. Lv.1과 가격 충돌은 의도)
- `work_process/MVP2/upgrade-system-rework-proposal.md` (업글은 눈에 보이는 변화를 줄 것 → 이 업글은 숫자 분리 표시로 충족)
- `init/rule.md` 2-5 (UI Prefab·Scene 범위)
