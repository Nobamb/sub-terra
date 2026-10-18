# 시설 실패·전력 고갈·귀환 복구 점검 — 2026-10-13

## 범위와 기준

- 사용자 승인 범위: 시설 이동/설치 거부 시 원본·보관량·대기·자원 보존, 전력 고갈 및 귀환 실패 후 정상 진행. 문제가 확인된 부분만 수정.
- 브랜치 codex/demo-visual-issues-170-174 / 시작 HEAD fa62076b. 첫 Unity 프로젝트인 저장소 내부 sub-terra에서 작업했다. 새 main 동기화·병합·커밋·푸시·PR 게시는 수행하지 않았다.
- 앞선 저장 검사 소스/문서 및 기존 데이터 에셋 3개는 보존했다. 이번에 실제 사용자 저장 슬롯을 읽거나 쓰지 않았다.
- 최근 Unity 실행 시 RAM 부족으로 결과 없이 중단한 기록은 facility-save-validation-20261013.md를 따른다. 이번에는 Unity·빌드·전체 테스트를 실행하지 않았다.

## 1. 시설 이동·설치 실패 — 추가 제품 수정 없음

- BuildingPlacementSystem.TryMoveAt은 CanPlaceAt 실패 시 위치/ID/설치 점유/지원 지면 변경 전에 반환한다. 기존 객체를 계속 사용하며 이동 경로에는 wallet 차감이나 BuildingPlaced 이벤트가 없다. 따라서 거부 자체로 기존 객체의 보관량/회복·충전 대기를 교체하지 않는다. 이는 코드 근거이며 실제 저장·이어하기 통과를 뜻하지 않는다.
- GameplayBuildingPlacementBridge.OnPlacementRejected는 이동 중 실패 이유를 표시하고 이동 선택을 유지한다. 우클릭 취소 및 비활성화는 ClearSelection/미리보기 정리로 끝난다. 일반 설치 실패는 선택과 미리보기를 정리한다.
- 일반 설치는 CanPlaceAt 후 인스턴스를 만들고, wallet 실패 시 생성물만 제거하며 점유 등록을 하지 않는다. 설치 영역/지면/승강로 거부는 차감 전 반환한다.
- 기존 검사: ClinicMovement_BlockedFootprintKeepsOriginal(4칸), MoveChargerOrStorage_BlockedCellAndCancelPreserveOriginal, MoveFacility_RejectOrCancelLeavesOriginalUntouched, Charger_BlockedFootprintCellRejectsWithoutSpending, Elevator_ShaftSpaceRejectsWithoutSpending. Unity 객체/타일맵 검사는 이번에 재실행하지 않았다. 기존 데스크탑 12개 결과를 이번 검사 통과로 대체하지 않는다.

## 2. 전력 고갈·귀환 실패 — 지연 씬 로딩 복구 보완

### 확인한 코드 결함

ClockOffSceneLoader.Load는 시계 OFF 코루틴을 예약한 직후 true를 반환했다. 실제 UnitySceneLoader.Load는 나중에 실행되는데, 실패해도 시계 표시만 복구했다. ElevatorTravelSession은 이미 Arrived, 정상 Run은 Completed로 바뀌고 객실/캐릭터는 상승 후 잠긴 채 남을 수 있었다. RunFailureRuntimeController도 같은 지연 로더의 false를 받지 못해 기존 입력 복구 경로로 돌아가지 못했다. 코드상 누락을 확인했으며 실제 Windows 로딩 실패를 재현한 것은 아니다.

### 수정

- ElevatorTravelSession.TryDepart에 지연 도착 옵션을 추가했다. 접수 후 Moving을 유지하며 CompleteArrival에서만 성공/실패를 확정한다. 실패 시 전력 환불은 한 번만 하고 Blocked로 전환하여 재시도를 허용한다. 로더가 null인 실패도 환불 후 재시도 가능하게 했다. 미확정 Moving 취소 시에도 예약 비용을 복원한다.
- SaveRuntimeController의 지연 로더는 실제 로딩 결과를 완료 콜백으로 전달한다. 항상 최소 한 프레임 뒤에 완료하여 예약 호출의 재진입을 피한다. 정상 귀환은 실제 성공 뒤 Completed로 확정하며 실패하면 Active로 복구하고 귀환 대상을 지운다. 시계 OFF는 유지하고 실패 시 EndDeparture로 표시를 돌린다.
- ElevatorController는 접수된 지연 이동이 끝날 때까지 기다린다. Blocked이면 기존 객실 하강 → 물리·이동·카메라 복구 경로를 사용한다. 성공 시 기존 객실을 아래로 되돌리지 않는다.
- ElevatorTravelBridge는 지연 접수를 퀘스트 성공으로 기록하지 않는다. 지연 성공 신호는 씬 전환에도 남는 SaveRuntimeController가 보낸다. 목적지 씬이 활성화되지 않은 경우 도착 자동 저장도 수행하지 않는다.
- RunFailureRuntimeController는 즉시/지연 로딩 실패를 같은 로컬 복구로 연결한다. 기존 구조 실패의 화물 손실·부활 규칙은 변경하지 않는다.
- 전력 0은 기존 EmergencyRescueRuntimeController의 선택형 구출 경로가 담당한다. 이 경로는 이동·점프·사다리를 잠그지 않는다. 목적지 유무와 최신 비용을 결제 전에 검사하며 실패하면 버튼/안내를 다시 사용할 수 있다. 이 경로는 새 결함을 확인하지 않아 변경하지 않았다.

## 실제 실행한 최소 검증

- CompileUnityChanges.ps1: 현재 소스로 Player / App / App.GameplayIntegration / Player.PlayModeTests / App.Tests.EditMode 총 5개 경량 컴파일, 모두 종료 코드 0. Unity 임포트·씬·물리 검증을 대신하지 않는다.
- RunFailureRecoveryManagedChecks.ps1: 위 현재 소스로 생성한 DLL 사용, 관리형 검사 10개 PASS / 종료 코드 0. 기존 캐시 App DLL만으로 수정 통과를 판정하지 않았다.
  - 엘리베이터 7개: 단일 차감·중복 방지, 비용 0, 막힘/전력 부족, 즉시 로드 실패 환불, 지연 실패 환불/재시도, 지연 성공 확정, 로더 없음 복구.
  - RunLifecycle 실패 후 Active/깊이 보존 1개.
  - 자원 0 구출, 전력 회복 후 구출 거부/무차감 2개.
- 새 PlayMode 회귀 검사 DeferredTravelFailure_RestoresRiderControlAndPhysics는 컴파일했으나 실행하지 않았다. 실제 객실 하강/조작/카메라/팝업 복구는 데스크탑에서 확인해야 한다.
- diff 검사 및 변경 범위 확인. ProjectSettings/Packages/씬/프리팹/meta는 이번 작업에서 변경하지 않았다. 임시 컴파일 출력은 정리한다.

## 데스크탑에서 다음으로 할 최소 확인

1. 후속 소스 정상 Unity 컴파일 후 새 PlayMode 검사 1개를 실행한다. 시계 OFF가 표시된 귀환의 성공 시 퀘스트 진행과 지상 저장도 확인한다.
2. 검사 전용 환경에서 지연 목적지 로딩을 실패시키면 객실이 복귀하고 조작/물리/카메라/시계가 복원되는지 확인한다. 제품 Build 설정을 임의 변경하거나 사용자 저장을 훼손하지 않는다. 정상 경로가 실패했다면 로그를 보존한다.
3. 막힌 시설 이동 → 취소 → 정상 공간 재이동에서 원본/자원/보관량/대기가 유지되는지 확인한다. 전력 0 팝업 닫기 → R 재열기 → 구출을 한 번 확인한다.
4. facility-save-validation-20261013.md의 실제 저장·완전 종료·이어하기 절차로 이어간다. 위 코드 점검만으로 Demo 완주/저장 완료를 기록하지 않는다.

사용자의 커밋·푸시·PR 게시 요청에 따라 후속 브랜치 codex/demo-failure-recovery로 공유한다. PR #175를 기준으로 이번 저장 검사·실패 복구 diff만 검토한다. 부모 PR 병합 후 main으로 대상을 변경해 남은 diff를 확인한다. Inspector 추가 연결, 저장 버전 변경, 패키지 변경은 없다. 실제 실행이 남은 항목은 사용량 회복 후 데스크탑에서 이어간다.
