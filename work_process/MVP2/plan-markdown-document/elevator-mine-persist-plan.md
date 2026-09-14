# 엘리베이터 왕복 시 채굴 유지 (옵션 1) — 체크리스트 / PR 단위

작성 기준: 2026-08-08  
목표: 지상↔지하 엘리베이터 왕복 후에도 **채굴·시설·붕괴 등 월드 변경점이 유지**되어, 가까운 구리 무한 리스폰 파밍을 막는다.  
전제: 새 맵 생성 알고리즘/골드 경제 로직은 바꾸지 않는다. 기존 `WorldSnapshotSystem` Capture/Restore를 **엘리베이터 경로에도 연결**한다.

---

## 0. 범위

### In scope
- 지하 → 지상 귀환 직전 월드 스냅샷 확보
- 지상 저장 시 빈 world로 덮어쓰지 않기
- 지상 → 지하 재진입 시 스냅샷 복원 (이어하기와 동일 의미)
- 새 게임/슬롯 전환 시 캐시 초기화
- 단위·통합 테스트 및 수동 QA

### Out of scope (후속)
- 전력 완충 정책 변경 (지상 Max 회복 유지)
- 판매 단가/감쇠, 광맥 자연 재생
- 세이브 포맷 major 버전 bump (필드 추가는 가능하나 DTO 호환 유지)
- UI/튜토리얼 문구 대대적 변경

### 성공 기준 (플레이어 체감)
1. 구리 타일 채굴 → 엘리베이터 귀환 → 재하강 → **해당 셀이 비어 있음**
2. 설치한 버팀목 등이 재진입 후 유지
3. 인벤 광물·골드·업그레이드는 기존과 같이 유지
4. 이어하기(Continue)도 지상에서 저장한 뒤에도 채굴 상태 복원
5. 새 게임 슬롯은 이전 슬롯 월드 캐시를 물려받지 않음

---

## 1. 현재 끊긴 지점 (수정 근거)

| # | 위치 | 문제 |
|---|------|------|
| A | `SaveRuntimeController.TryElevatorTravel` | Scene 전환 **후** 저장만 함. 지하 Provider가 언로드된 뒤라 world 캡처 불가 |
| B | `SaveDataMapper.Capture` | Provider null이면 `new WorldSnapshotDto()` **빈 스냅샷** 저장 → miningChanges 소실 |
| C | `TryStartExploration` | Integration 로드 후 `RestoreSnapshot` 미호출. `MineLayerTilemapGenerator.Awake`만 풀 재생성 |
| D | Continue 경로 | 복원 로직은 이미 있음. 엘리베이터 재진입과 **공유되지 않음** |

재사용 자산:
- `IWorldSnapshotProvider.CaptureSnapshot` / `RestoreSnapshot`
- `WorldSnapshotSystem` (seed 재생성 + 변경점 적용)
- Continue의 복원 순서: State → Scene → Restore → Recalculate → UI Ready

---

## 2. PR 단위 쪼개기 (권장 스택)

의존 순서: **PR1 → PR2 → PR3 → PR4**.  
PR1만으로도 “지상 세이브가 월드를 지우지 않음”이 보장되고, PR2~3이 런타임 왕복을 완성한다.

```
PR1  World 캐시 + 빈 스냅샷 방지     (저장 안전망)
  └─ PR2  귀환 직전 Capture            (데이터 확보)
       └─ PR3  재진입 시 Restore         (플레이 루프 완성)
            └─ PR4  테스트·QA·문서       (회귀 방지)
```

Graphite/일반 스택 모두 동일 순서. 긴급 시 PR1+PR2를 한 PR로 합칠 수 있으나, **Restore(PR3)와 저장 안전망(PR1)은 분리 리뷰를 권장**.

---

### PR1 — 마지막 Mine World 캐시 + 지상 저장 시 빈 world 금지

**목적**  
Surface Base에서 저장해도 이전 채굴 기록이 파일에서 사라지지 않게 한다.

**변경 요약**
- `SaveRuntimeController`(또는 전용 작은 헬퍼)에  
  `WorldSnapshotDto cachedMineWorld` (런타임 캐시) 도입
- 캐시 수명:
  - 설정: Capture 성공 시 / 세이브 로드 시 world가 유효하면
  - 해제: 새 게임, 슬롯 전환, 명시적 Reset
- `CaptureContext` / `SaveDataMapper.Capture` 정책:
  - Provider 있음 → `CaptureSnapshot()` 결과를 저장 **그리고** 캐시 갱신
  - Provider 없음 → **캐시가 있으면 캐시 사용**, 없으면 기존처럼 빈 DTO(또는 seed만 있는 최소 DTO)
- 빈 스냅샷으로 “유효 캐시”를 덮지 않는 가드  
  (예: `miningChanges` 등 의미 있는 데이터가 있는 스냅샷만 캐시로 승격, 또는 “Provider 경로에서만 캐시 갱신”)

**주요 파일 (예상)**
- `Scripts/App/Save/SaveRuntimeController.cs`
- `Scripts/App/Save/SaveDataMapper.cs` 및/또는 `SaveCaptureContext`
- 필요 시 `Scripts/App/Save/MineWorldCache.cs` (단일 책임 분리용, 선택)

**완료 체크리스트**
- [ ] `cachedMineWorld` 필드/헬퍼 API (`Set` / `Peek` / `Clear`) 존재
- [ ] 새 게임·슬롯 로드 시 Clear 또는 Load world로 교체
- [ ] Provider null + 캐시 있음 → 저장 JSON의 `world.miningChanges`가 유지됨
- [ ] Provider null + 캐시 없음 → 저장 실패하지 않음 (기존 호환)
- [ ] Provider 있음 → 캡처 결과가 캐시와 파일 모두에 반영
- [ ] 세이브 버전 major bump 없음 (기존 슬롯 로드 가능)
- [ ] EditMode: “빈 Provider 저장이 이전 world를 지우지 않음” 테스트

**인수 기준**
- 유닛 테스트만으로 검증 가능 (PlayMode 왕복 불필요)
- 이 PR 단독 머지 시 동작은 보수적으로 개선(적어도 악화 없음)

**리스크**
- 캐시를 “항상 최신”으로 오인 → 아직 Capture 타이밍(PR2) 전이면 런타임 왕복은 미완
- 빈 DTO와 유효 DTO 구분 규칙이 모호하면 회귀 → 규칙을 주석+테스트로 고정

---

### PR2 — 엘리베이터 귀환 직전 World Capture

**목적**  
지하 Scene이 죽기 **전에** 채굴 상태를 캡처해 PR1 캐시에 넣는다.

**변경 요약**
- `TryReturnToSurface` / `TryElevatorTravel` 경로에서  
  **Scene 로드 전** `Resolve()` → `CaptureSnapshot()` → 캐시 저장
- 귀환 저장(`AutoSaveReason.SurfaceReturn`)은 도착 후 실행해도 됨  
  (world는 캐시에서 채워지므로)
- Capture 실패 시:
  - 권장: 귀환은 막지 않되 로그 + dirty 유지 (또는 직전 세이브 재시도)
  - 최소: 실패 reason을 남기고, 가능하면 이전 캐시 유지(덮어쓰지 않음)
- (선택 강화) 출발 직전 동기 저장 1회 — 크래시 대비. 범위 커지면 PR2-b로 분리

**주요 파일 (예상)**
- `Scripts/App/Save/SaveRuntimeController.cs`
- `Scripts/App/Integration/ElevatorTravelBridge.cs` (호출부 확인만, 로직은 Runtime에 집중)
- `Scripts/App/Save/ElevatorTravelSession.cs` (전력 차감 순서와 충돌 없게)

**완료 체크리스트**
- [ ] Integration → Surface 전환 **전** Capture 호출 순서 보장
- [ ] Capture 결과가 `cachedMineWorld`에 들어감
- [ ] Surface 도착 후 자동 저장 JSON에 해당 miningChanges 포함
- [ ] 하강(`TryStartExploration`) 경로에서는 불필요한 Capture로 캐시를 비우지 않음
- [ ] 엘리베이터 Busy/전력 부족 실패 시 캐시 오염 없음
- [ ] Scene 로드 실패 환불 경로와 Capture 순서 문서화

**인수 기준**
- “지상에서 세이브 파일을 열어보면 직전에 캔 타일이 miningChanges에 있다”  
  (에디터/테스트로 검증)

**리스크**
- `TryDepart`가 동기 Load라 Capture는 반드시 Load 앞
- 부분 채굴(`changedTiles`)·건물도 Capture에 포함되는지 스냅샷 구현 재확인 (이미 포함이면 추가 작업 없음)

---

### PR3 — 엘리베이터 재진입 시 World Restore

**목적**  
지상 → 지하 재진입 시 Continue와 같이 변경점을 적용해 **구리가 되살아나지 않게** 한다.

**변경 요약**
- `TryStartExploration` 성공 후 (Integration Scene 로드 직후):
  1. UI 게이트 필요 시 유지/재사용
  2. `Resolve()`로 `IWorldSnapshotProvider` 획득
  3. `RestoreSnapshot(cachedMineWorld ?? 세이브 world)`
  4. `Recalculate` / `NotifyIntegrationWorldRestored` / `NotifyDerivedRecalculated`
- Continue 코루틴의 복원 블록을 **공용 메서드로 추출** 권장  
  예: `TryRestoreMineWorld(WorldSnapshotDto snapshot, out string reason)`
- `MineLayerTilemapGenerator.Awake`의 base 재생성은 유지  
  → Restore가 그 위에 변경점 적용 (현재 Restore 구현과 동일)
- 캐시가 비어 있고 세이브 world도 비어 있으면: 신규 탐사와 동일(풀 맵) — 정상

**주요 파일 (예상)**
- `Scripts/App/Save/SaveRuntimeController.cs` (핵심)
- `Scripts/App/Save/ContinueService.cs` (공용 조건 `RequiresWorldRestore` 재사용)
- `Scripts/App/Integration/IntegrationRuntimeBinder.cs` (게이트 타이밍 확인)
- `docs/INTEGRATION_GUIDE.md` (엘리베이터 왕복 복원 순서 한 절 추가 — PR4로 미뤄도 됨)

**완료 체크리스트**
- [ ] Integration 재진입 시 Restore 1회 호출
- [ ] 채굴로 제거된 타일이 재진입 후 비어 있음
- [ ] 건물 복원 시 보상/비용 이중 적용 없음 (기존 restore 경로 준수)
- [ ] generatorVersion 불일치 시 Continue와 동일한 실패/로그 정책
- [ ] RunLifecycle `BeginRun`과 복원 순서: State 갱신 후 Scene, 그다음 World
- [ ] 전력 완충(지상)과 하강 비용 5는 기존 유지
- [ ] 인벤/골드가 Restore로 리셋되지 않음

**인수 기준**
- 수동: 구리 1칸 채굴 → 귀환 → 판매 → 재하강 → 그 칸 없음, 주변 미채굴 유지
- 자동: PlayMode 또는 최대한 EditMode 시나리오 1개 이상

**리스크**
- Awake 재생성 vs Restore 타이밍 레이스 → Continue와 같이 “Scene active 다음 프레임” 정렬
- Integration binder가 Restore 전에 UI를 켜면 깜빡임/잘못된 HUD → 게이트 재사용
- `RunLifecyclePhase.Completed` → 재탐사 시 캐시가 Clear되면 안 됨 (슬롯 유지 동안 캐시 유지)

---

### PR4 — 테스트, QA, 문서

**목적**  
회귀 방지와 인수 기준 고정.

**테스트 체크리스트**

| ID | 유형 | 시나리오 | 기대 |
|----|------|----------|------|
| T1 | EditMode | Provider null 저장 + 캐시 있음 | world가 비지 않음 |
| T2 | EditMode | 새 게임/Clear 후 저장 | 이전 슬롯 miningChanges 미혼입 |
| T3 | EditMode | Capture→캐시→Mapper | miningChanges 왕복 |
| T4 | PlayMode (가능 시) | 채굴 1셀 → 귀환 Capture → 재진입 Restore | 셀 파괴 유지 |
| T5 | PlayMode/수동 | 건물 1개 설치 후 왕복 | 건물 유지, 이중 차감 없음 |
| T6 | 수동 | 지상 저장 → 프로세스 종료 → 이어하기 → 지하 | 채굴 유지 |
| T7 | 수동 | 슬롯1 채굴 후 슬롯2 새 게임 | 슬롯2는 깨끗한 맵 |
| T8 | 수동 | 화물 가득 채굴 → 판매 → 재하강 | 같은 맥 재파밍 불가 |

**문서**
- [ ] `docs/INTEGRATION_GUIDE.md` — “엘리베이터 왕복 저장·복원 순서” 절 추가
- [ ] 본 문서 체크리스트 완료 표시 / CHANGELOG 한 줄 (프로젝트 관례 시)

**주요 파일 (예상)**
- `Tests/EditMode/App/Save/...`
- `Tests/PlayMode/Integration/Save/...` 또는 Elevator 관련
- `docs/INTEGRATION_GUIDE.md`
- `docs/CHANGELOG.md` (있는 경우)

---

## 3. 구현 순서 체크리스트 (개발자용 실행 리스트)

전체 머지 전 완료 조건:

### 데이터·저장
- [ ] 마지막 Mine world 캐시 도입
- [ ] 지상 저장이 빈 world로 덮지 않음
- [ ] 세이브 로드 시 캐시 시드
- [ ] 새 게임/슬롯 전환 시 캐시 Clear

### 런타임 왕복
- [ ] 귀환 전 Capture
- [ ] 재진입 후 Restore + Recalculate + Notify
- [ ] Continue 복원 로직 공용화(중복 최소화)

### 검증
- [ ] T1~T3 자동화
- [ ] T4 또는 T6 수동/PlayMode 중 최소 1
- [ ] T7 슬롯 격리
- [ ] 구리 무한 파밍 루프 재현 불가 확인

### 비기능
- [ ] 세이브 호환 (구 슬롯 로드)
- [ ] Console Error 없음 (왕복 10회 스모크)
- [ ] 소유권: App/Save·Integration 배선 위주, Gameplay Snapshot API 변경 최소화

---

## 4. 권장 구현 디테일 (합의 포인트)

PR 착수 전 아래로 고정하면 리뷰가 빨라진다.

| 항목 | 권장 결정 | 대안 |
|------|-----------|------|
| 캐시 위치 | `SaveRuntimeController` private + 테스트용 internal API | 별도 `MineWorldCache` 클래스 |
| 지상 저장 world | 캐시 우선, 없으면 빈 DTO | 캐시 없으면 직전 파일 world merge (복잡, 비권장) |
| Capture 실패 | 이전 캐시 유지 + 로그 | 귀환 hard-fail |
| Restore 소스 우선순위 | 1) 메모리 캐시 2) 방금 로드한 세이브 world | 항상 디스크 재로드 (느림) |
| 동기 저장 시점 | 도착 후 1프레임 (현행) + world는 캐시 | 출발 직전 추가 저장 |
| 전력 완충 | 유지 | 후속 밸런스 PR |

---

## 5. 플레이어 루프 전/후

**Before**
```
근처 구리 채굴 → 귀환(전력 Max) → 판매 → 재하강 → 구리 리스폰 → 무한 골드
```

**After (PR1~3 완료)**
```
근처 구리 채굴 → 귀환(전력 Max) → 판매 → 재하강 → 구멍 유지 → 더 깊게/옆으로만 확장
```

경제 구조(광물→골드)는 유지. 막는 것은 **공간 리셋 익스플로잇**뿐이다.

---

## 6. PR 설명 템플릿 (복붙용)

### PR1
```
Title: fix(save): keep last mine world when saving on Surface Base

Summary:
- Cache the last valid mine WorldSnapshotDto on SaveRuntimeController.
- When IWorldSnapshotProvider is missing (Surface Base), reuse the cache
  instead of writing an empty world snapshot.

Test plan:
- EditMode: null provider + cache preserves miningChanges.
- Load old save slots still succeeds.
```

### PR2
```
Title: fix(elevator): capture mine world before surface return

Summary:
- Capture IWorldSnapshotProvider snapshot before unloading Integration
  on elevator return, and store it in the mine world cache.

Test plan:
- Mine one tile, return to surface, inspect save/cache for that cell.
- Failed elevator call does not clear cache.
```

### PR3
```
Title: fix(elevator): restore mine world on re-entry

Summary:
- After Integration loads via TryStartExploration, restore cached/saved
  world snapshot (shared helper with Continue path).

Test plan:
- Mine → surface → re-enter: mined cell stays empty.
- Inventory/gold unchanged by restore.
- Support building round-trip without double charge.
```

### PR4
```
Title: test(docs): elevator mine persist coverage

Summary:
- Add EditMode/PlayMode coverage and document elevator save/restore order.

Test plan:
- Full checklist T1–T8.
```

---

## 7. 예상 공수 (참고)

| PR | 상대 공수 | 메모 |
|----|-----------|------|
| PR1 | S | 저장 경로·단위 테스트 중심 |
| PR2 | S | 호출 순서 한 곳 |
| PR3 | M | Continue와 타이밍 정렬, 게이트 |
| PR4 | S~M | PlayMode 환경에 따라 변동 |

총: 작은 스택 4개. 신규 시스템 없음.

---

## 8. 다음 액션

1. 이 문서의 **권장 결정 표(§4)** 확인/수정  
2. PR1 브랜치에서 캐시 + 빈 스냅샷 방지 구현  
3. PR2 → PR3 순 스택  
4. PR4로 테스트·문서 마감 후 수동 T6/T8 사인오프  

구현 시작 시: `PR1`부터 진행하면 된다.
