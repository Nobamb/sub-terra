# B-185 결과: 시설 전력 켜짐 청록 정전기·빛 연출 (#292, 소리 없음)

- 기준: `origin/main` `ed5ac6a0` (B-184 커밋 `7f8471a0` 포함 확인, PR #290 병합 `0be77b1a`·`e08695e0` 포함)
- 만든 브랜치: `MVP2-fix132` (원격에 같은 이름 없음)
- 기능 커밋: `beda67fb` feat: 시설 전력 켜짐 청록 정전기·빛 연출 추가 (프롬프트 B-185)
- PR 만들지 않음. main 직접 커밋 없음. 이슈 #292·#158 수정 안 함.

## 바꾼 파일
경로는 `sub-terra/Assets/_Project/` 기준.

| 파일 | 내용 |
|---|---|
| `Scripts/Gameplay/Power/PowerOnEffect.cs` (새 파일) | 연출 본체. ★ 상수 16~36줄, 이벤트 47·49줄, 생략 구간 `IsSuppressed`/`BeginSuppression`/`EndSuppression` 74~86줄 |
| `Scripts/Gameplay/Power/PowerFacility.cs` | `ApplyPowerState(bool, bool allowEffect)`. 꺼짐→켜짐이고 생략 구간이 아닐 때만 `PlayPowerOnEffect()`. 꺼지면 `Stop()`. `OnEnable` 첫 적용은 `allowEffect=false`. `OnDisable`에서도 연출 정리 |
| `Scripts/Gameplay/Snapshot/WorldSnapshotSystem.cs` | 읽기 전용 `IsRestoring`(~46줄). `RestoreSnapshot`(~92줄)이 try/finally로 `IsRestoring`과 `PowerOnEffect` 생략 구간을 감싸고, 기존 본문은 `RestoreSnapshotCore`로 옮김(로직은 같음) |
| `Scripts/Gameplay/Power/SubTerra.Gameplay.Power.asmdef` | `SubTerra.Shared` 참조 추가(`AccessibilityPreferences.ReduceMotion` 읽기용. Shared는 의존이 없어 순환 없음) |
| `Tests/EditMode/Gameplay/Power/PromptB185PowerOnEffectTests.cs` (새 파일) | 테스트 11개 |
| `Tests/EditMode/Gameplay/Power/SubTerra.Gameplay.Power.EditModeTests.asmdef` | 테스트용 `SubTerra.Gameplay.Snapshot`·`SubTerra.Shared` 참조 추가 |
| `init/rules/lessons/2026-11-04-issue292-random-pool-slot-state.md` | 교훈 1건(아래 "고친 실수") |

전력 규칙(`PowerNetworkSystem.Rebuild`, 공급·수요·우선순위), `IsInteractionAvailable`·`AvailabilityChanged` 시점, SfxId·SfxLibrary·음원·AudioSource·SfxPlayer, 씬·프리팹 YAML은 바꾸지 않았다. 연출 오브젝트는 처음 켜질 때 코드로 만든다(시설 루트 아래 `PowerOnEffect` 자식 하나, Glow 1 + Spark 4칸).

## ★ 상수 (`PowerOnEffect.cs`)
| 상수 | 값 | 뜻 |
|---|---|---|
| `PowerOnDuration` | 0.7 | 전체 길이(초) |
| `GlowStartScale` / `GlowEndScale` | 0.8 / 1.3 | 빛 크기(시설 그림 크기 대비) |
| `GlowMaxAlpha` | 0.35 | 빛 최대 알파 |
| `GlowRiseSeconds` | 0.15 | 빛이 가장 밝은 시점 |
| `GlowFadeEndSeconds` | 0.6 | 빛이 다 사라지는 시점 |
| `GlowSizeFactor` | 1.1 | 시설 그림 대비 빛 기준 지름 |
| `SparkCountMin` / `SparkCountMax` | 2 / 4 | 정전기 개수 |
| `SparkWindowSeconds` | 0.35 | 정전기가 튀는 구간 |
| `SparkLifeMin` / `SparkLifeMax` | 0.05 / 0.08 | 한 줄기 표시 시간 |
| `SparkLength` | 0.36 | 정전기 길이(월드 단위) |
| `SparkEdgeInset` | 0.12 | 시설 가장자리에서 안쪽 거리 |
| `PoweredFadeIn` | 0.25 | 켜짐 그림 페이드 시간(0이면 바로 켬) |
| `ReducedMotionShowsGlow` | true | 모션 감소 때 약한 빛 표시(false면 연출·이벤트 없음) |
| `ReducedMotionGlowAlpha` | 0.12 | 모션 감소 빛 최대 알파 |
| `ReducedMotionRiseSeconds` / `ReducedMotionDuration` | 0.1 / 0.35 | 모션 감소 빛 최대 시점·전체 길이 |
| `GlowSortingOffset` / `SparkSortingOffset` | 1 / 2 | 시설 최대 sortingOrder + 1 / + 2 |

청록은 `MinimapPalette.Cyan`과 같은 `(0.26, 0.9, 1)`이다(`PowerOnEffect.Cyan`). 정전기 가운데 구간만 `(0.78, 1, 1)`로 조금 밝다.

## 불러오기 구분 방법
권장 1번 방식을 썼다. `WorldSnapshotSystem.IsRestoring`(Restore 시작부터 끝까지 true, 실패·예외에도 finally에서 false)을 두었다.
Power 어셈블리는 Snapshot을 참조할 수 없다(Snapshot이 Power를 참조하므로 순환). 그래서 연출 쪽이 이 값을 직접 읽지 않는다. 대신 같은 try/finally에서 `PowerOnEffect.BeginSuppression()`/`EndSuppression()`을 부르고, `PowerFacility`가 `PowerOnEffect.IsSuppressed`를 확인한다. 시간 기반 2번 방식(`PowerOnQuietSeconds`)은 쓰지 않았다.
- 이어하기(`ContinueService`)와 탐사 진입 복원(`SaveRuntimeController.TryRestoreMineWorld`)은 모두 `RestoreSnapshot`을 거친다. 그 안에서 `RestoreBuildings → RestorePowerConnections → RequestRebuild`로 켜지는 시설은 조용히 바로 켜진다.
- 씬 시작: 통합 씬에는 미리 놓인 시설이 없다(시설 프리팹 5종은 설치·복원으로만 생성). `OnEnable` 첫 적용도 연출이 없다.

## 연출 시간표 (다음 소리 프롬프트용)
| 시점(초) | 일 |
|---|---|
| 0.00 | 전력 켜짐(같은 프레임, `Rebuild` 끝의 `NetworkRebuilt`에서 `Started`/`BatchStarted` 발생). 빛 알파 0, 크기 0.8배. 켜짐 그림 알파 0 |
| 0.00~0.35 | 정전기 2~4줄기가 흩어져 튐. 구간을 개수로 나눈 칸마다 한 줄기씩 0.05~0.08초 보임. 예: 3개면 대략 0.00~0.09 / 0.12~0.20 / 0.23~0.31 |
| 0.15 | **빛이 가장 밝음**(알파 0.35) |
| 0.25 | 켜짐 그림 페이드 끝(원래 알파) |
| 0.60 | 빛 사라짐(크기 1.3배까지 퍼진 상태) |
| 0.70 | 연출 끝, 연출 오브젝트 비활성 |

모션 감소: 정전기 없음, 크기 변화 없음, 0.10초에 알파 0.12, 0.35초에 끝. 켜짐 그림 페이드 없음.
시간은 `Time.deltaTime`을 쓰므로 일시정지(timeScale 0)에는 멈춘다.

## 이벤트 (이번엔 테스트만 구독)
```csharp
// 연출이 시작된 시설마다 한 번. isBatch = 같은 Rebuild에서 둘 이상 켜졌는지(첫 시설 포함 정확한 값).
public static event Action<PowerFacility, bool> PowerOnEffect.Started;
// 한 Rebuild에서 연출이 하나 이상 시작되면 한 번만. count = 켜진 시설 수, center = 연출 중심 평균.
public static event Action<int, Vector3> PowerOnEffect.BatchStarted;
```
- 발생 시점: 시설별 시작 요청을 모았다가 `PowerNetworkSystem.NetworkRebuilt`(같은 프레임, `Rebuild` 직후)에서 한꺼번에 낸다. `Started`를 모두 낸 뒤 `BatchStarted`를 한 번 낸다. 안전망으로 다음 `LateUpdate`에서도 남은 것을 비운다.
- 생략(불러오기·첫 적용·`ReducedMotionShowsGlow=false`)이면 이벤트가 없다. 모션 감소라도 빛이 있으면 이벤트는 있다.
- 다음 프롬프트에서 `BatchStarted`에 소리 하나를 붙이면 "여러 개가 켜져도 소리는 한 번"이 된다.

## 정렬
시설의 sortingLayer를 그대로 쓴다. sortingOrder는 시설 SpriteRenderer 최대값 + 1(빛) / + 2(정전기)다. 거의 투명한 덧그림(알파 0.1 미만, 예: 조명의 `GasClearance` 22)은 기준에서 뺐다. 실제 프리팹 기준으로 빛은 7, 정전기는 8이다.
참고: 플레이어 sortingOrder는 5이므로 시설 앞에 선 플레이어 위에도 0.7초 동안 옅은 청록빛(최대 알파 0.35)이 겹친다. 가리지는 않지만 신경 쓰이면 `GlowSortingOffset`을 음수로 바꿔 빛을 시설 뒤로 보낼 수 있다. 이 경우 테두리 후광처럼 보인다.

## 테스트
- 새 `PromptB185PowerOnEffectTests` 11개: 단독 3회 연속 **11/11 통과**
  - 꺼짐→켜짐: 연출 시작, `Started` 1번 / 켜짐→켜짐·켜짐→꺼짐: 없음
  - `OnEnable` 첫 적용 조용함, `RestoreSnapshot` 중 켜짐: 연출·이벤트 없음, 끝나면 `IsRestoring`·생략 구간 false
  - 한 `Rebuild`에서 3개: 연출 3개(시설마다 따로), `Started` 3번(모두 isBatch=true), `BatchStarted` 1번(count 3)
  - 모션 감소: 정전기 0개, 빛 최대 알파 ≤ 0.12, 이벤트 유지, 켜짐 그림 페이드 없음
  - 일반: 정전기 2~4개가 모두 0.35초 안에만 보임, 빛 최대 알파 ≤ 0.35이고 0.15초 근처가 최대
  - 연출 중 꺼짐: 즉시 정리, 켜짐 그림 꺼짐, 알파 원복 / 0.7초 뒤 연출 오브젝트 비활성, 다시 켜지면 같은 오브젝트 재사용
  - 정렬: 연출 sortingOrder가 시설 최대(6) + 1~2
  - SfxId 개수 103, 값 합 5274, 최대 105(기준과 같음)
- Power·Snapshot EditMode 묶음 30개 통과.
- 전체 EditMode(같은 에디터 세션, 같은 절차):
  - 기준 `ed5ac6a0`(변경을 잠시 stash하고 실행): 2,433 실행 / 2,410 통과 / **23 실패**
  - 변경 후: 2,444 실행 / 2,420 통과 / **24 실패**
  - 추가 실패 1개 `PromptB150MainMenuTests.DeleteButton_Hover_FadesInWhiteTrashIcon`은 같은 코드로 다시 돌렸을 때 1회 통과, 1회 실패했다. 메인 메뉴 호버의 시간 의존 검사로, B-184 보고서에도 기준 실패로 적혀 있다. App 어셈블리는 Gameplay를 참조하지 않으므로 이번 변경과 무관하다. 나머지 23개는 기준과 같다. **전체 테스트가 모두 통과한 상태는 아니다.**
- Unity 컴파일: 오류·경고 0(콘솔 확인).
- 테스트 실행 중 바뀐 범위 밖 파일(NotoSansKR SDF, InventoryPanel·MainMenuPanel·ViewSocket 프리팹, Mine_Demo_Integration.unity, EditorBuildSettings·QualitySettings·TimeManager, 103 메인메뉴 PNG 2개)은 모두 `git restore`로 되돌렸다.
- 화면 확인: 에디터 미리보기 씬(저장 안 함)에서 실제 `ChargerFacility`·`LightFacility` 프리팹으로 0.03/0.15/0.3/0.5초를 렌더했다. 시설 둘레에 옅은 청록 후광이 퍼지고, 가장자리에 작은 픽셀 지그재그 정전기가 보였다. 화면을 가리는 번쩍임은 없었다.

## 고친 실수
- 처음엔 이번에 쓰는 정전기 칸에만 정렬값을 넣어서, 쓰지 않는 칸이 sortingOrder 0으로 남았다. 칸은 꺼져 있어 화면 문제는 없었다. 그런데 무작위 개수가 4일 때만 정렬 검사가 통과했다. 모든 칸에 정렬값을 먼저 넣도록 고치고, 단독 3회 반복으로 확인했다(`init/rules/lessons/2026-11-04-issue292-random-pool-slot-state.md`).
- `GetInstanceID()`가 Unity 6000.5에서 컴파일 오류(obsolete)라서 `GetEntityId().GetHashCode()`로 바꿨다. 연출 난수는 게임 결정성에 영향이 없도록 시설별 `System.Random`을 쓴다.

## 미검증
- **실제 플레이(Bootstrap → 광산)에서 설치·케이블 연결·CCTV 창 연결·이어하기·모션 감소 설정으로 연출을 직접 보지 않았다.** 위 렌더는 미리보기 씬에서 Tick을 직접 진행한 결과다. `Update`의 `Time.deltaTime` 진행, 일시정지 중 멈춤, 실제 카메라 줌에서의 크기·밝기 체감도 미검증이다.
- 보건소·정산 콘솔·보관함 프리팹은 렌더하지 않았다(같은 코드 경로이고 `VisualRoot/Artwork` 구조는 같다).
- Windows 빌드(`Phase P > Build Windows`)는 실행하지 않았다.
- PlayMode 테스트는 추가·실행하지 않았다.

## 사용자 테스트 가이드
1. 전진기지 코어 범위에 충전기를 설치한다.
2. 전력이 들어오는 순간 충전기 주변에 청록 정전기가 몇 번 짧게 튀고, 청록빛이 은은하게 퍼졌다 사라지는지 본다(1초 안).
3. 너무 밝거나 번쩍이지 않는지, 플레이어나 화면을 가리지 않는지 본다.
4. 코어 CCTV 창에서 케이블을 연결해도 같은 연출이 나오는지 본다(창을 닫고 시설을 보면 됨).
5. 보건소·조명·정산 콘솔·보관함도 켜질 때 같은 연출이 나오는지 본다.
6. 시설 여러 개가 한 번에 켜지면 각 시설마다 연출이 나오는지 본다.
7. 전력을 끊으면 지금처럼 바로 꺼지고 연출이 없는지 본다.
8. 저장 후 이어하기로 들어오면 켜진 시설에 연출이 없는지 본다.
9. 설정에서 모션 감소를 켜고 다시 켜 본다 → 정전기 없이 아주 약한 빛만(또는 없이) 나오는지 본다.
10. 이번엔 소리가 없는 게 정상이다. 정전기가 튀는 순간·빛이 가장 밝은 순간을 기억해 PM에게 알려 준다(소리 맞출 때 씀). 바꿔 보고 싶은 길이·밝기(★)도 알려 준다.
11. Unity Console에 새 빨간 오류·노란 경고가 없는지 본다.

## 완료 체크리스트
- [x] `MVP2-fix132` push 확인
- [x] 커밋 해시 기재 (`beda67fb`, 보고서 커밋은 그 다음)
- [x] Unity 컴파일 오류 없음 / [ ] 빌드는 미실행
- [x] EditMode 새 실패 없음(추가 1개는 기존 시간 의존 검사, 재실행 시 통과)
- [x] SfxId·음원·SfxLibrary 변경 없음
- [x] main 직접 커밋 없음
- [x] PR 안 만듦
