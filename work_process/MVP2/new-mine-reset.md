# Surface Base 새 광산 초기화 (이용료 500G)

| 항목 | 내용 |
| --- | --- |
| **문서 제목** | New Mine Reset (Surface Base, 500G fee) |
| **날짜** | 2026-08-19 |
| **상태** | Draft |
| **관련 코드** | `SaveRuntimeController`, `MineWorldCache`, `WorldSnapshotDto`, `WorldSnapshotSystem`, `EconomyService`/`GameState` 골드, `SurfaceBaseBinder`/`SurfaceBasePresenter`, `LocalizationService` |
| **범위 씬** | `SurfaceBase.unity` + `Prefabs/UI/SurfaceBasePanel.prefab` only |
| **전제** | 엘리베이터 왕복 시 채굴 유지(`elevator-mine-persist-plan.md`)는 **그대로 둔다**. 맵 생성 알고리즘·광맥 분포·판매/업그레이드 비용은 바꾸지 않는다. |

---

## 0. 한 줄 결론

Surface Base에서 **500G를 내고** 지하 월드를 새 시드로 다시 배치한다.  
업그레이드·심층 해금·인벤 광물·남은 골드는 유지한다. 골드 잔액을 0으로 만들지 않는다.

플레이어가 리셋 전후에 말할 수 있어야 하는 문장:

> “광산을 다 캐서 500G 내고 새 구역을 열었다. 드릴은 그대로고, 땅만 새로 생겼다.”

이 문장을 못 만들면 기능이 아직 완결되지 않은 것이다.

---

## 1. 문제

지하는 Tilemap 기반이고, 캔 타일은 `WorldSnapshotDto` 변경점으로 **영구 유지**된다.  
지상↔지하 엘리베이터 왕복 때도 이 변경점을 복원한다. 가까운 구리 무한 리스폰을 막기 위한 의도다.

그 결과:

| 플레이어가 겪는 것 | 원인 |
| --- | --- |
| 맵을 오래 파면 캘 타일이 사라진다 | 채굴 유지 + 유한 데모 광산(깊이 약 40m) |
| 귀환 후 재하강해도 빈 칸은 그대로다 | `MineWorldCache` + Restore |
| 새 게임 외에는 땅을 되돌릴 수단이 없다 | persist 설계의 출구가 없음 |

PRD는 “완전한 절차적 무한 월드”를 MVP에서 제외한다.  
따라서 무한 확장이 아니라, **유료 새 구역 재배치**로 고갈을 푼다.

---

## 2. 제품 결정 (확정)

### 2-1. 무엇을 하는가

- 위치: **Surface Base만**. 지하 HUD/인벤/엘리베이터에는 버튼을 두지 않는다.
- 동작: 이용료 **500G** 차감 → **새 `worldSeed`** 부여 → 지하 변경점 전부 폐기.
- 다음 탐사: 빈 광산이 새 레이아웃으로 생성된다.
- 확인: 확인 팝업 없이 실행 금지.

### 2-2. 남기는 것 / 지우는 것

| 구분 | 대상 | 저장 위치 | 리셋 시 |
| --- | --- | --- | --- |
| 유지 | 업그레이드 레벨 | `UpgradeState` / `GameSaveData.upgrades` | 유지 |
| 유지 | 심층 해금 (`zone.deep`) | `UpgradeState` unlocked zones | 유지 |
| 유지 | 인벤 광물·화물 중량·미정산 가치 | `InventoryState` | 유지 |
| 유지 | 골드 (이용료 500G만 차감) | `PlayerState.Gold` | 유지 (잔액 −500) |
| 유지 | 목표·튜토리얼·데모 완료 플래그 | `ProgressState` | 유지 |
| 유지 | 지상 전진기지/정산 상태 | `OutpostState` | 유지 |
| 유지 | 경력 최대 심도 | `RunState.MaximumDepth` | 유지 |
| 삭제 | 캔 타일 | `world.miningChanges` | 비움 |
| 삭제 | 변경 타일 | `world.changedTiles` | 비움 |
| 삭제 | 붕괴 | `world.collapseChanges` | 비움 |
| 삭제 | 지하 시설(버팀목·조명·충전기·사다리 등) | `world.buildings` | 비움 |
| 삭제 | 가스 상태 | `world.gasChanges` | 비움 |
| 삭제 | 발견 구역 | `world.discoveredChunkIds` | 비움 |
| 삭제 | 지하 전력 케이블 | `world.powerState` | 비움 |
| 교체 | 월드 시드 | `world.worldSeed` | **새 값. 기존과 달라야 함** |

인벤 광물을 지우지 않는 이유: Surface Base에 이미 판매가 있다.  
리셋 전에 팔지 말지는 플레이어 선택이다. “팔지 않아서 손해”를 시스템에 넣지 않는다.

골드를 0으로 만들지 않는 이유: 현재 업글·시설 비용은 광물이다. 골드는 판매 결과 + 긴급 탈출 정도다.  
잔액 몰수는 진행도 리셋이 아니라 **안 쓰던 숫자 삭제**가 된다. 대신 **입장료 500G**로 싱크를 만든다.

### 2-3. 하지 않는 것 (Non-Goals)

- 골드/인벤/업그레이드 전액 초기화 (새 게임과 동일해짐)
- 같은 시드로 타일만 되돌리기 (업글된 드릴로 동일 구리 자리 재파밍)
- 캔 타일 비율 조건, 횟수 제한, 이용료 누증 (후속)
- 광맥 자연 재생, 구역 여러 개를 동시에 보관
- 지하에서 리셋, 엘리베이터 왕복 시 자동 리셋
- 세이브 major 버전 bump (`SaveVersions.Current = 2` 유지. world 필드만 교체)
- 판매 단가·업글 비용·지층 생성 알고리즘 변경
- Main Menu / 설정 / 인벤 / 업글 / 판매 패널 레이아웃 변경 (`init/rule.md` 2-5)

### 2-4. persist와의 관계

| 행동 | 월드 |
| --- | --- |
| 엘리베이터 귀환 → 재하강 | **유지** (기존 persist) |
| 이어하기 | **유지** |
| Surface Base **새 광산 초기화** (본 기능) | **새 시드 + 변경점 삭제** |
| 새 게임 | 슬롯 전체 초기화 (기존) |

본 기능은 persist를 뒤집지 않는다. **명시적 유료 출구**만 추가한다.

---

## 3. 현재 구현 (코드 기준)

재사용 자산:

- `MineWorldCache` — 지상에서 Provider가 없어도 world를 빈 DTO로 덮지 않음. `Clear()`는 새 게임·슬롯 전환용.
- `SaveDataMapper.CaptureWorld` — Provider 없으면 캐시 폴백.
- `TryStartExploration` — Integration 로드 1프레임 뒤 `TryRestoreMineWorld`. 캐시에 의미 있는 내용이 없으면 신규 탐사.
- `WorldSnapshotSystem.RestoreSnapshot` — `Regenerate(worldSeed)` 후 변경점 적용.
- `GameState.SetGold` / `AddGold` — 골드 변경. 업글은 광물 `ItemCostEntry`.
- `EmergencyEscapeService.GoldCost = 100` — 현재 유일한 골드 싱크. 본 기능이 두 번째 싱크가 된다.
- Surface Base 액션 행: `ExploreButton` / `SettingsButton` / `QuitButton` (`ActionY=120`), 그 아래 `OpenSellButton` (`SellButtonY=56`), `MessageText` (`MessageY=0`).
- 확인 UX 참고: Main Menu `OverwriteConfirm` (확인/취소). Surface Base에는 동일 패턴의 리셋 확인이 없다.
- 자동 저장: `AutoSaveReason` (`Manual`, `SurfaceReturn`, `UpgradePurchased` 등). 리셋 전용 값은 아직 없음.

주의 — **캐시를 `Clear()`만 하고 끝내면 안 된다.**  
캐시가 비면 다음 탐사가 `MineLayerTilemapGenerator` 기본 시드(`20260731`)로 같은 맵을 다시 만들 수 있다.  
리셋은 Clear가 아니라 **새 시드만 있는 스냅샷으로 캐시를 교체**해야 한다.

`MineWorldCache.HasMeaningfulContent`는 `worldSeed != 0`이면 true다. 시드만 있는 스냅샷도 복원 대상으로 승격된다.

---

## 4. 제안 설계

### 4-1. 이용료

```
public const int FeeGold = 500;
```

단일 상수. UI 문구·검증·차감이 이 값을 공유한다. 하드코딩 500을 Presenter/Prefab 텍스트에 흩뿌리지 않는다.

- 잔액 ≥ 500: 진행 가능. 차감 후 0원 허용.
- 잔액 < 500: 상태 미변경. 메시지: 골드 부족 (필요 500, 보유 N).
- 차감은 월드 교체와 **한 트랜잭션**. 골드만 깎이고 월드가 남거나, 월드만 바뀌고 골드가 남는 상태를 만들지 않는다.

### 4-2. 시드

- 새 `worldSeed`는 현재 캐시/세이브 시드와 **달라야** 한다.
- 0 금지 (`HasMeaningfulContent`·기본값과 충돌).
- 주입 가능한 `IMineResetSeedSource` (테스트에서 고정값). 런타임 기본은 UTC ticks 기반 long. 충돌 시 +1 재시도.
- `generatorVersion`은 현재 값 유지 (기본 1). 생성기 버전을 올리지 않는다.

리셋 직후 캐시에 넣을 스냅샷:

```
worldSeed          = <new>
generatorVersion   = 기존과 동일 (없으면 1)
version            = WorldSnapshotDto 기본 ("1.2")
miningChanges      = []
changedTiles       = []
collapseChanges    = []
buildings          = []
gasChanges         = []
discoveredChunkIds = []
powerState.cableConnections = []
```

### 4-3. 런타임 API

`MineResetService` (신규, `SubTerra.App`)가 순수 규칙만 담당한다.  
Scene 로드·UI·골드 표시는 하지 않는다.

```
TryReset(GameState state, MineWorldCache cache, IMineResetSeedSource seeds, out MineResetResult result)
```

성공 시:

1. `state.SetGold(gold - 500)` (`CreditsChanged` 발생)
2. 위 빈 스냅샷으로 `cache.ReplaceFromProvider(newSnapshot)`
3. `result`에 이전 시드, 새 시드, 차감 후 골드

실패 시 상태·캐시 불변:

| 실패 | 조건 |
| --- | --- |
| `InvalidState` | GameState 불완전, cache null |
| `InsufficientGold` | Gold < 500 |
| `SeedFailed` | 유효한 새 시드를 못 만듦 (테스트/가드) |

`SaveRuntimeController.TryResetMine(out string reason)`가 오케스트레이션한다.

1. 현재 씬이 Surface Base가 아니면 거부
2. 탐사/엘리베이터 busy면 거부 (`explorationGuard` / `elevatorTravel`)
3. `MineResetService.TryReset`
4. 성공 시 `SaveCurrent(AutoSaveReason.MineReset)` — enum에 `MineReset = 10` 추가
5. reason은 로컬라이즈 키가 아니라 Presenter가 `LocalizationService`로 풀어 보여 줄 메시지/키

Presenter는 골드를 직접 빼지 않는다. 판매와 동일: View는 표시, Presenter는 런타임 호출만.

### 4-4. 다음 탐사

기존 `TryStartExploration` → `RestoreMineWorldAfterExplorationEntry`를 그대로 탄다.

- 캐시에 새 시드 스냅샷이 있으므로 Restore가 호출된다.
- `WorldSnapshotSystem.RestoreSnapshot`이 `Regenerate(newSeed)` 후 빈 변경점을 적용한다.
- 결과: 새 레이아웃, 시설 없음, 붕괴/가스 초기 상태.

이어하기(Continue)도 세이브 `world`가 새 시드 스냅샷이므로 동일하다.

### 4-5. UI (Surface Base only)

`init/rule.md` 2-5: **SurfaceBase 패널·씬만** 수정. 판매 패널 내부 컨트롤·설정창·메인메뉴·인벤·업글 프리팹은 열거나 저장하지 않는다.  
레이아웃 빌더는 대상 경로 상수만 저장한다.

#### 버튼

- 이름: `ResetMineButton`
- 라벨 키: `mine_reset.button` → `새 광산 초기화 (500G)`
- 배치: `OpenSellButton` 아래, `MessageText` 위. 기존 액션 행을 가로로 밀어 겹치지 않게 한다.

권장 좌표 (Sell 빌더 기준, 구현 시 실측 후 문서 좌표를 빌더 상수로 고정):

| 요소 | 기존 | 리셋 추가 후 |
| --- | --- | --- |
| Explore / Settings / Quit | `ActionY = 120` | 유지 |
| OpenSellButton | `SellButtonY = 56` | 유지 |
| **ResetMineButton** | 없음 | `y = 8`, 크기 320×48, x=0 중앙 |
| MessageText | `MessageY = 0` | **`y = -48`** 로 내려 버튼과 겹치지 않게 |

Progression 하단 텍스트·판매 모달·설정 모달보다 **뒤**에 둔다.  
설정/판매가 열려 있으면 리셋 버튼은 가려지거나 비활성. 모달 위에 리셋이 뜨면 안 된다.

골드 부족이어도 버튼은 보인다. 숨기지 않는다. 눌러서 부족 메시지를 보여 준다.

#### 확인 팝업

Main Menu `OverwriteConfirm`과 같은 확인/취소 2버튼.

- 루트: `ResetMineConfirm` (기본 비활성)
- 캔버스 정렬: Surface Base 본문보다 위, Settings 모달과 충돌 시 Settings가 더 위 (설정 연 채로 리셋 확인이 가리면 안 되고, 반대로 리셋 확인이 설정 위에 떠도 안 된다). 구현: 설정이 열려 있으면 리셋 클릭을 막고 `mine_reset.fail.busy` 또는 설정 닫기 유도.
- 제목: `mine_reset.confirm.title` → `새 광산 구역`
- 본문: `mine_reset.confirm.body`  
  → `이용료 500G를 내고 지하를 새로 배치합니다.`  
  `캔 타일, 지하 시설, 붕괴와 가스 상태가 사라집니다.`  
  `업그레이드, 심층 해금, 보유 광물, 남은 골드는 유지됩니다.`  
  `현재 골드 {0} → {1}`
- 확인: `mine_reset.confirm.yes` → `확인`
- 취소: `mine_reset.confirm.no` → `취소`

확인 연타 가드: busy 동안 확인/취소/리셋 버튼 비활성. 실패 시 해제.

취소는 골드·월드 불변, 팝업만 닫기.

#### 결과 메시지 (`MessageText`)

| 키 | 한국어 | 영어 |
| --- | --- | --- |
| `mine_reset.button` | 새 광산 초기화 (500G) | New Mine (500G) |
| `mine_reset.success` | 새 광산이 배치되었습니다. 이용료 500G. | New mine laid out. Fee 500G. |
| `mine_reset.fail.gold` | 골드가 부족합니다. 500G 필요 (보유 {0}G). | Not enough gold. Need 500G (have {0}G). |
| `mine_reset.fail.busy` | 지금은 새 광산을 열 수 없습니다. | Cannot open a new mine right now. |
| `mine_reset.fail.surface` | 지상 기지에서만 새 광산을 열 수 있습니다. | New mines can only be opened at Surface Base. |

`LocalizationService`에 KO/EN 동시 등록.  
한글 글리프가 깨지면 `KoreanFontAssetUtility` 문자 집합에 부족한 글자를 추가한다.

### 4-6. 세이브

- `SaveVersions` major bump **없음**. 기존 슬롯 로드 가능.
- 저장 내용은 기존 `world` 교체 + `player.gold` 차감뿐.
- 리셋 횟수 필드는 넣지 않는다 (후속 이용료 누증용으로만 검토).

실패 정책은 판매와 동일: 런타임 반영 후 AutoSave. 저장 실패 시 런타임은 유지하고 dirty 재시도.

---

## 5. 흐름

```
Surface Base
  ResetMineButton
    ├─ busy / 설정·판매 모달 열림 → fail.busy
    ├─ Gold < 500 → fail.gold, 팝업 없음
    └─ Gold ≥ 500 → ResetMineConfirm
         ├─ 취소 → 닫기
         └─ 확인
              MineResetService.TryReset
                ├─ 실패 → 상태 불변, 메시지
                └─ 성공
                     Gold -= 500
                     cache = 새 시드 빈 스냅샷
                     AutoSave(MineReset)
                     팝업 닫기
                     success 메시지
                     다음 Explore → Regenerate(newSeed) + 빈 변경점
```

---

## 6. PR 단위

의존: **PR1 → PR2 → PR3**. UI를 규칙 없이 먼저 넣지 않는다.

```
PR1  MineResetService + 캐시 교체 + AutoSave
  └─ PR2  Surface Base 버튼·확인 팝업·로컬라이즈
       └─ PR3  테스트·QA·문서 좌표 고정
```

### PR1 — 규칙·세이브

- `MineResetService`, `IMineResetSeedSource`, `FeeGold = 500`
- `SaveRuntimeController.TryResetMine`
- `AutoSaveReason.MineReset = 10`
- 캐시 Clear 금지, 시드-only 스냅샷 `ReplaceFromProvider`
- EditMode: 차감, 부족 시 no-op, 업글/인벤 유지, miningChanges 빈 배열, 새 시드 ≠ 구 시드, persist 경로 회귀 없음

### PR2 — Surface Base UI

- `ResetMineButton` + `ResetMineConfirm`
- Presenter/Binder 배선만. 경제 연산 없음
- 로컬라이즈 키
- 전용 레이아웃 빌더. `SurfaceBasePanel.prefab` (+ 필요 시 `SurfaceBase.unity`)만 저장
- 판매/설정 모달과 겹침 없음

### PR3 — 검증

- 정적 구조 테스트: 버튼·확인 루트 존재, Presenter에 `SetGold` 없음
- PlayMode 또는 수동 QA 체크리스트 (아래 8장)
- `docs/INTEGRATION_GUIDE.md`에 한 절 추가 (구현 시점)

---

## 7. 테스트 요구

### EditMode

1. Gold 800 → 성공 → Gold 300, 캐시 시드 변경, miningChanges Count 0
2. Gold 499 → 실패 → Gold 499, 캐시 동일
3. Gold 500 → 성공 → Gold 0
4. 업글 레벨·`zone.deep`·인벤 copper 수량 불변
5. 리셋 전 캐시에 miningChanges/buildings가 있어도 리셋 후 비어 있음
6. 새 시드 ≠ 구 시드, 시드 ≠ 0
7. 리셋하지 않은 persist: Capture → 캐시 → Restore 시 miningChanges 유지 (회귀)
8. `TryResetMine`을 Surface가 아닌 컨텍스트에서 호출하면 실패, 골드 불변

### 플레이어 체감 (성공 기준)

1. 구리 채굴 → 귀환 → 재하강 → 해당 칸 **빈 칸** (persist 회귀 아님)
2. 지상에서 500G 내고 확인 → 재하강 → 해당 칸 **다시 채워짐**, 레이아웃이 이전과 **다름**
3. 드릴 레벨·심층 해금·인벤 광물 유지, 골드만 −500
4. 499G에서 확인이 안 열리거나 실행이 거부되고 월드 불변
5. 취소 시 골드·월드 불변
6. 리셋 후 이어하기 → 새 광산 유지 (옛 채굴 복원 없음)
7. 리셋 직후 저장·프로세스 종료·이어하기 → 골드·시드 유지

---

## 8. 수동 QA

1. 새 게임 → Surface Base. 골드 0. 리셋 클릭 → 부족 메시지. 월드 이후 탐사 시 기본 새 게임 광산.
2. 탐사 → 광물 채굴 → 귀환 → 판매로 500G 이상.
3. 리셋 클릭 → 본문에 유지/삭제·골드 전후가 보임 → 취소 → 골드 그대로, 재하강 시 캔 칸 유지.
4. 다시 리셋 → 확인 → 골드 −500, 성공 메시지.
5. 재하강 → 맵이 새로 생성, 이전 굴·버팀목 없음.
6. 다시 귀환 → 재하강 → 4에서 캔 칸이 비어 있음 (새 광산에 대한 persist).
7. 설정창·판매창을 연 뒤 리셋이 그 위를 뚫고 나오지 않는지.
8. 확인 연타 시 골드가 1000 이상 깎이지 않는지 (1회 500만).

---

## 9. 리스크

| 리스크 | 대응 |
| --- | --- |
| `Clear()`만 해서 기본 시드 맵이 재생성 | 시드-only 스냅샷으로 Replace. Clear는 새 게임·슬롯 전환만 |
| 골드 차감과 월드 교체가 분리돼 한쪽만 적용 | Service 한 메서드에서 둘 다 성공한 뒤에만 커밋 |
| Surface Base 레이아웃 회귀 (판매/레벨 텍스트 겹침) | 전용 빌더, 2-5, 판매 빌더와 좌표 표를 공유해 MessageY만 내림 |
| 같은 시드 재사용 | 구 시드와 다를 때까지 재시도, 테스트로 고정 |
| persist 회귀 | PR1에 “리셋 안 한 왕복은 빈 칸 유지” 테스트 필수 |
| 확인 없이 실행 | 팝업 없는 경로를 Binder에 두지 않음 |

---

## 10. 후속 (본 문서 범위 밖)

- 캔 타일 비율이 낮을 때 버튼 비활성
- 리셋 횟수마다 이용료 증가
- 여러 광산 슬롯을 동시에 보관
- 골드 전액 몰수형 prestige (업글이 골드 기반이 된 뒤에만 재검토)
