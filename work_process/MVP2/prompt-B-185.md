# B-185 시설 전력 켜짐 연출: 청록 정전기 + 은은한 청록빛 (#292 연출만, 소리 없음)

## 목표
- 충전기·보건소·조명·정산 콘솔·보관함이 **꺼짐 → 켜짐**으로 바뀔 때, 시설 주변에 청록 정전기가 짧게 몇 번 튀고 청록빛이 은은하게 퍼졌다 사라지게 한다.
- 전체 약 0.5~0.8초. 번쩍이거나 화면을 가리지 않게, 작고 조용하게(#172 요란했던 연출 참고).
- **이번에는 소리를 넣지 않는다.** PM이 연출을 보고 소리 시점을 정한 뒤 다음 프롬프트에서 붙인다. 대신 소리를 붙일 **이벤트 하나**를 열어 둔다(아래 C).
- 숫자(길이·밝기·크기·개수)는 상수 하나씩(★) 두어 PM이 숫자만 바꿔 볼 수 있게 한다.

## 브랜치
1. `git fetch` 후 최신 `origin/main`(B-184 PR #290 병합 `0be77b1a`, 그 뒤 `e08695e0`·`ed5ac6a0`까지 있음)에서 새 브랜치 **`MVP2-fix132`**를 만든다.
2. 원격에 `MVP2-fix132`가 이미 있으면 비어 있는 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다.
3. 보고서 맨 위에 기준 커밋 해시와 B-184 커밋(`7f8471a0`) 포함 여부를 적는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다(11-2). 모든 변경은 `MVP2-fix132`에서만(11-3).
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다(11-9). 브랜치는 지우지 않는다.
3. **SfxId·SfxLibraryDefaults·SfxLibrary.asset·음원·AudioSource·`SfxPlayer` 호출을 추가하거나 바꾸지 않는다.**
4. 전력 규칙(공급·수요·우선순위·`PowerNetworkSystem.Rebuild`)과 상호작용 가능 여부(`IsInteractionAvailable`·`AvailabilityChanged`)는 바꾸지 않는다. 켜짐 → 꺼짐은 지금처럼 바로 꺼진다.
5. 씬·프리팹 YAML은 가능한 한 고치지 않는다. 연출 오브젝트는 코드에서 만든다(필요하면 이유를 보고).
6. 줄 번호는 `ed5ac6a0` 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/Scripts/` 기준)
- 이슈 #292 본문, #282(모션 감소), #172
- Gameplay/Power/PowerFacility.cs (전체 45줄): `OnEnable` ~21-25에서 구독하며 현재 상태를 바로 적용, `ApplyPowerState(bool)` ~34-43이 `poweredVisuals`(`PoweredVisualRoot`) `SetActive`만 함
- Gameplay/Power/PowerNode.cs `PowerStateChanged` ~28, `SetPowered` ~78-83(같은 값이면 이벤트 없음)
- Gameplay/Power/PowerNetworkSystem.cs `Rebuild` ~43-66(한 번의 Rebuild에서 여러 시설이 차례로 `SetPowered(true)`, 끝에 `NetworkRebuilt`)
- Gameplay/Snapshot/WorldSnapshotSystem.cs `RestoreSnapshot` ~88(~131 `RestoreBuildings` → ~135 `RestorePowerConnections` → ~136 `RequestRebuild`)
- Gameplay/Building/BuildingPlacementSystem.cs ~420, Gameplay/Integration/GameplayEventBridge.cs ~98 (설치·연결 뒤 `RequestRebuild`)
- Gameplay/Building/FacilityGroundedVisual.cs ~154(`PoweredVisualRoot` 찾기), Shared/AccessibilityPreferences.cs `ReduceMotion` ~10
- 청록 참고: App/Integration/MinimapPalette.cs `Cyan (0.26, 0.9, 1)` ~8. Gameplay는 App을 참조하지 않으므로 같은 값을 Gameplay 쪽 상수로 둔다.

## A. 언제 연출하나 (불러오기 구분)
- 연출은 `PowerStateChanged`로 **false → true**가 된 경우에만. `OnEnable`의 첫 적용은 연출 없음(지금처럼 바로 켜짐).
- **불러오기·이어하기는 연출 없음.** `RestoreSnapshot` 중 `RequestRebuild`로 켜지는 시설은 조용히 바로 켜져야 한다. 방법(권장 순):
  1. `WorldSnapshotSystem`에 읽기 전용 `IsRestoring`(Restore 시작~끝 true, 실패해도 finally로 false)을 두고, 연출 쪽이 이 값을 보고 생략.
  2. 1이 어려우면 씬 시작·Restore 직후 `PowerOnQuietSeconds = 0.5f`★ 동안 생략. 고른 방법과 이유를 보고.
- 코어 CCTV 창 안에서 연결하든 창 밖에서 연결되든, 새로 설치한 시설이 처음 전력을 받든 똑같이 연출.
- 연출 중 다시 꺼지면 연출을 바로 멈추고 켜짐 그림도 바로 끈다(지금처럼).

## B. 연출 모양 (Gameplay, 예: `Gameplay/Power/PowerOnEffect.cs`, `PowerFacility`가 하나 붙여 씀)
- 청록빛: 시설 둘레의 부드러운 원형/타원 빛(코드로 만든 원형 그라디언트 스프라이트). 크기 `0.8 → 1.3`★배로 퍼지며 알파 `0 → 0.35`★(~0.15초) → 0(~0.6초). 최대 알파는 낮게.
- 정전기: 2~4개★의 짧은 지그재그 줄기(픽셀 느낌, `LineRenderer` 또는 작은 스프라이트), 시설 가장자리 둘레 랜덤 위치. 각각 0.05~0.08초 보였다 사라짐, 처음 0.35초 안에 흩어져 튐. 색은 청록, 가운데만 조금 밝게.
- 켜짐 그림(`poweredVisuals`): 빛이 퍼지는 동안 `PoweredFadeIn = 0.25f`★초로 서서히 나타나도 좋다(SpriteRenderer 알파). 구조가 복잡해 어렵다면 지금처럼 바로 켜고 보고.
- 전체 길이 `PowerOnDuration = 0.7f`★(0.5~0.8 범위). 시간은 `Time.deltaTime`(정지 중엔 멈춤).
- 정렬: 시설 스프라이트 바로 앞(시설 `sortingLayer` 그대로, `sortingOrder`는 시설 최대값 +1~2). 플레이어·UI를 가리지 않게.
- **모션 감소**(`AccessibilityPreferences.ReduceMotion`): 정전기 없음, 빛은 최대 알파 `0.12f`★로 크기 변화 없이 짧게(또는 아예 없음, 상수로 선택). 켜짐 그림 페이드는 생략하고 바로 켠다.
- 여러 시설이 한 번에 켜지면 연출은 **각 시설마다**. 오브젝트는 시설당 하나를 재사용(매번 새로 만들지 않음).

## C. 소리용 이벤트 (다음 프롬프트에서 씀)
- 정적 이벤트 하나: `PowerOnEffect.Started(PowerFacility facility, bool isBatch)` (이름은 맞춰도 됨). 연출이 시작되는 프레임에 시설마다 한 번.
- `isBatch`: 같은 `Rebuild` 안에서 둘 이상이 켜졌는지. 첫 시설은 false일 수 있으니, **배치용 이벤트를 따로** 둔다: `PowerOnEffect.BatchStarted(int count, Vector3 center)` — 한 `Rebuild`에서 켜진 시설이 1개 이상이면 **한 번만**(가장 가까운 시점: `NetworkRebuilt` 직후 또는 다음 `LateUpdate`). 다음 프롬프트는 이 이벤트 하나에 소리를 붙이면 "여러 개 켜져도 소리는 한 번"이 된다.
- 불러오기·첫 적용·모션 감소로 연출을 생략한 경우: 생략이면 이벤트 없음, 모션 감소는 빛이 있으면 이벤트 있음(소리는 남김).
- 이번에는 이 이벤트를 **아무도 구독하지 않는다**(테스트만 구독).

## 손대지 말 것
- 효과음 전부(B-164~B-184 연결 포함), 전력 규칙, CCTV 창 연결 성공음, 시설 상호작용, `AGENTS.md`·규칙 문서

## 테스트
- EditMode 전체에서 기준의 기존 실패 말고 새 실패 없음. 새 `PromptB185PowerOnEffectTests`(+필요하면 PlayMode):
  - 꺼짐→켜짐: 연출 시작, `Started` 1번 / 켜짐→꺼짐·켜짐→켜짐: 없음
  - `OnEnable` 첫 적용·`RestoreSnapshot` 중 켜짐: 연출·이벤트 없음
  - 한 `Rebuild`에서 3개 켜짐: 연출 3개, `Started` 3번, `BatchStarted` 1번(count 3)
  - `ReduceMotion` true: 정전기 0개, 빛 최대 알파 ≤ 0.12
  - 연출 중 꺼짐: 연출 즉시 정리, 켜짐 그림 꺼짐 / 0.7초 뒤 연출 오브젝트 비활성
  - SfxId 개수·값이 기준과 같음
- 실행 전후 `git status`로 범위 밖 프리팹·폰트·씬·PNG·TimeManager 변경을 되돌린다.

## 사용자 테스트 가이드 (보고서에 그대로 넣기)
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

## 완료 후
- 한국어 conventional 커밋(예: `feat: 시설 전력 켜짐 청록 정전기·빛 연출 추가 (프롬프트 B-185)`). `MVP2-fix132`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue292-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #292·#158은 고치지 않는다.** 체크는 PM이 한다.
- 결과 보고서 `work_process/MVP2/power/B-185-power-on-effect-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일·줄, ★ 상수 위치, 불러오기 구분 방법
  - 연출 시간표(정전기 튀는 시점, 빛 최대 시점) — 다음 소리 프롬프트용
  - 이벤트 이름·시그니처, 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix132` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일·빌드 오류 없음 [ ] EditMode 새 실패 없음 [ ] SfxId·음원·SfxLibrary 변경 없음 [ ] main 직접 커밋 없음 [ ] PR 안 만듦
