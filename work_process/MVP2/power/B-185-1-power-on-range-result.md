# B-185-1 전력 켜짐 연출: 실제 게임 범위 판정 기준으로 수정 결과

- 기준 브랜치·커밋: `MVP2-fix132` `b58c00b1` (B-185 `beda67fb` 포함)
- 커밋: 아직 없음(사용자 동의 전). push·PR 안 함.

## 원인
- B-185 연출은 `PowerFacility`가 `PowerNode.PowerStateChanged`(케이블 연결 + 공급/수요 용량)의 꺼짐→켜짐에서만 시작했다.
- 실제 게임의 켜짐 판정은 `GameplayEventBridge.BuildFacilityStatuses`의 범위 판정이다(코어 `PowerSupplyRangeIndicator` 반경 10, 또는 엘리베이터 반경 10). HUD·CCTV 창·`OutpostService`가 이 값을 쓴다.
- 실제 게임에는 플레이어가 케이블을 만드는 코드가 없고(케이블은 `WorldSnapshotSystem` 복원에서만 생성), CCTV 창의 "연결되었습니다"도 범위 판정 결과(`connectedFacilities`)를 보여 줄 뿐이다. 그래서 `PowerNode.IsPowered`는 실제 플레이에서 켜지지 않았고 연출도 나오지 않았다.
- B-185 화면 확인은 미리보기 씬에서 `Tick`을 직접 돌린 것이라 이 경로를 거치지 않았다.

## 조명·보관함은 언제 켜지나
- 조명: 가스 시야 구멍(`GasClearance`의 `GasVisionClearanceSource`)이 `PoweredVisualRoot` 밖에 있어 **전력과 무관하게 항상 동작**한다.
- 보관함: `OutpostService.TryValidateFacility`가 상호작용 범위 안이면 전력 확인 없이 허용한다(**항상 사용 가능**).
- 두 시설은 기능상 전력 조건이 없으므로 기능은 그대로 두고, **연출만 다른 시설과 같은 범위 기준**(코어·엘리베이터 반경 안으로 들어오는 순간)을 쓴다.

## 바꾼 파일
| 파일 | 내용 |
| --- | --- |
| `Scripts/Gameplay/Integration/GameplayEventBridge.cs` | `RefreshFacilityPowerRange(bool allowEffect)` ~332: 전력망 노드 중 연출 대상 5종(`IsPowerOnEffectFacility` ~478: 충전기·보건소·정산 콘솔·조명·보관함)에 `IsWithinPowerSupplyRange` 결과를 넘기고, 끝에서 `PowerOnEffect.FlushPendingStarts()`. 호출: `OnPowerNetworkRebuilt` ~199(연출 허용), `OnEnable` ~55·`SetElevatorPowerOrigin` ~96(연출 없이 상태만) |
| `Scripts/Gameplay/Power/PowerFacility.cs` | `IsInSupplyRange` ~23, `SetSupplyRangePowered(bool inRange, bool allowEffect)` ~48: 범위상 꺼짐→켜짐일 때만 `PowerOnEffect.Play`. 켜짐→꺼짐은 연출 즉시 정리. `PowerNode` 경로는 켜짐 그림·상호작용만 담당(연출 호출 제거) |
| `Scripts/Gameplay/Power/PowerOnEffect.cs` | 전력망 `NetworkRebuilt` 구독 제거, `FlushPendingStarts()` ~178 공개(판정 한 번 = `BatchStarted` 한 번). `Bind(PowerFacility)`로 단순화. 켜짐 그림 페이드는 보이는(활성) 그림만 |
| `Tests/EditMode/Gameplay/Power/PromptB185PowerOnEffectTests.cs` | 범위 기준으로 수정, 14개 |
| `Tests/EditMode/App/Integration/PromptB185RangePowerOnEffectTests.cs` (새) | 실제 `GameplayEventBridge` 판정으로 10개 |
| `Tests/PlayMode/Integration/Outpost/PromptB185PowerOnEffectPlayModeTests.cs` (새) | Bootstrap → 새 게임 → 광산 실제 흐름 1개 |
| `init/rules/lessons/2026-11-05-issue292-effect-trigger-real-judgement.md` (새) | 교훈 |

전력 규칙(`PowerNetworkSystem.Rebuild`)·`IsInteractionAvailable`·`AvailabilityChanged`·범위 판정 자체·효과음(SfxId)·씬/프리팹 YAML은 바꾸지 않았다. ★ 상수 값·이벤트 시그니처(`Started(PowerFacility, bool)`, `BatchStarted(int, Vector3)`)·연출 시간표는 B-185 그대로다.

## 연출이 나오는 순간 (모두 같은 `Rebuild` → 범위 판정 경로)
- 코어 범위 안에 시설 설치 / 범위 밖 시설이 이동해 범위 안으로 들어옴
- 시설이 먼저 있고 코어를 나중에 설치해 범위가 생김
- 엘리베이터 반경 안에 시설 설치
- 이어하기·탐사 진입(`RestoreSnapshot` 중 `PowerOnEffect.IsSuppressed`), 브리지 켜짐, 엘리베이터 위치 연결 때는 상태만 맞추고 연출 없음
- 켜짐→켜짐(다시 판정)·켜짐→꺼짐은 연출 없음. 한 번에 여러 개가 켜지면 시설마다 연출 + `Started` 시설마다 + `BatchStarted` 한 번

## 테스트 결과
- 전체 EditMode
  - 기준 `b58c00b1`(변경을 잠시 되돌리고 batchmode): 2,445 실행 / **24 실패**
  - 변경 후(에디터 안 플래그 실행기): 2,457 실행 / 2,433 통과 / **24 실패**. 실패 목록이 기준과 이름까지 같다(새 실패 0). 기준 실패가 남아 있어 **전체 테스트가 모두 통과한 상태는 아니다.**
  - 실행 수 증가 12개(새 테스트 수 기준으로는 13개). 실행기 차이로 보이지만 원인은 확인하지 않았다.
- B-185 관련 묶음: `PromptB185PowerOnEffectTests` 14 + `PromptB185RangePowerOnEffectTests` 10 → 3회 연속 24/24 통과(정전기 개수 무작위 확인). 기존 `PromptB51PowerInteractionTests` 5개 포함 실행 29/29 통과.
- PlayMode `PromptB185PowerOnEffectPlayModeTests.Bootstrap_Mine_PlaceChargerInCoreRange_BothOrders_PlaysPowerOnEffect`: **통과**(Test Runner에서 사용자가 실행, 6.7초). 확인 항목:
  - 탐사 진입 중 `Started` 없음
  - 코어 먼저 → 범위 안 충전기 `TryPlaceAt` → 충전기 연출 시작, `Started` 1·`BatchStarted(1)` 1
  - 엘리베이터 범위 밖 충전기 먼저(연출 없음) → 코어 `TryPlaceAt` → 충전기 연출 시작, `Started` 1·`BatchStarted(1)` 1
  - 실제 `Update`로 진행(0.15초 뒤 빛 알파 > 0), 0.7초 뒤 연출 정리
  - 같은 상태 다시 판정 → 연출 없음, `RestoreSnapshot` → 복원된 충전기 범위 켜짐, 연출 없음
  - 이 검사는 설치 거리·구역 제한과 비용을 풀고, 설치 자리 타일을 코드로 비웠다. 키보드 입력·건설 메뉴 경로는 거치지 않는다.
- 전체 PlayMode(플래그 실행기)는 10:54에 "player was stopped"로 중단되어 결과 없음.

## 미검증
- **실제 화면에서 연출 모양 확인**: PlayMode 테스트가 남긴 스크린샷은 카메라가 순간 이동한 플레이어를 따라가지 않아 시설·연출이 찍히지 않았다. 화면 증거가 아니어서 커밋하지 않았다.
- 건설 메뉴·키 입력으로 직접 설치하는 실제 조작, 보건소·정산 콘솔·조명·보관함 실제 화면, 모션 감소 실제 화면, 저장 후 이어하기 실제 화면
- 전체 PlayMode 회귀

## 참고 (이번 범위 밖, PM 결정 필요)
- 켜짐 그림(`PoweredVisualRoot`, 예: 조명 `PowerGlow`)은 여전히 케이블 기준이라 실제 플레이에서 거의 켜지지 않는다. 연출이 나와도 켜짐 그림은 꺼진 채이고, 이때 켜짐 그림 페이드는 건너뛴다. 범위 기준으로 켤지는 별도 결정.
- `Bootstrap.unity`에 `LightFacility(Clone)`이 남아 있다(브리지가 없어 이번 동작과 무관).

## 범위 밖 변경 정리
- 테스트 실행으로 바뀐 폰트·UI 프리팹 3개·`Mine_Demo_Integration.unity`·`ProjectSettings`·`QualitySettings`·`TimeManager`·main-menu-103 PNG 2장은 `git restore`로 되돌렸다.
- 남은 테스트 부산물(손대지 않음): `work_process/images/issue260/*.png` 2장 변경, 새 폴더 `work_process/images/b169-unlock-evidence/`·`work_process/images/issue249/`·`work_process/tools/issue225-pixel-comparison/`.

## 사용자 테스트 가이드
1. Bootstrap에서 시작 → 광산 진입 → 엘리베이터에서 10칸 넘게 떨어진 곳에 전진기지 코어를 설치한다.
2. 코어 범위(파란 원) 안에 충전기를 설치하는 순간 충전기 주변에 청록 정전기가 몇 번 튀고 청록빛이 은은하게 퍼졌다 사라지는지 본다(1초 안).
3. 반대 순서: 범위 밖에 충전기를 먼저 설치(연출 없음) → 그 근처에 코어를 설치하는 순간 충전기에서 연출이 나오는지 본다.
4. 엘리베이터 근처(10칸 안)에 시설을 설치해도 연출이 나오는지 본다.
5. 보건소·정산 콘솔·조명·보관함도 범위 안에 들어오는 순간 같은 연출이 나오는지 본다.
6. 범위 밖 시설 여러 개를 두고 코어를 설치하면 시설마다 연출이 나오는지 본다.
7. 범위 안 시설을 범위 밖으로 옮기면 연출이 없는지, 다시 범위 안으로 옮기면 연출이 나오는지 본다.
8. 저장 후 이어하기·지상에서 다시 탐사 진입 시 이미 켜진 시설에 연출이 없는지 본다.
9. 설정에서 모션 감소를 켜고 2를 반복 → 정전기 없이 아주 약한 빛만 나오는지 본다.
10. 소리는 없는 게 정상이다. Unity Console에 새 빨간 오류·노란 경고가 없는지 본다.
