# Demo 완주 검증 — 실행 전 점검 및 실제 결과


공유 상태 갱신: 최신 main f23f00d6은 통합 커밋 a30f7470에 포함되며, 귀환 안내 수정·이 문서·관리 코드 검사 스크립트는 이번 후속 커밋에 포함해 기능 브랜치와 PR #168로 전달한다. 아래의 미커밋/미푸시 표현은 당시 점검 시점의 기록이며, 데스크탑에서는 실제 원격 HEAD를 확인한다. 실제 Demo 완주·이어하기·Windows 종료 검증은 여전히 미완료다.

## 검증한 버전과 환경

- 작업 브랜치: codex/facility-move-runtime-followup, HEAD c1d745b7.
- PR #168은 조회 시 open / 미병합. 팀원의 PR 병합은 수행하지 않았다.
- origin/main을 fetch한 결과 f23f00d6. 현재 브랜치에는 main의 후속 키 조작 단말기(B-143) 및 가이드 포탈 이미지(B-144) 변경이 아직 통합되지 않았다.
- 사용자 데이터 에셋 3개(Clinic/Light/Settlement)의 기존 변경은 보존했다.
- 노트북 RAM 여유 0.38GB / 총 7.40GB. 사용자 결정에 따라 실제 Unity/Windows 실행 검증은 여유 있는 데스크탑으로 넘긴다.
- Unity와 빌드를 새로 실행하지 않았으며, 기존 Windows-x64 출력 폴더에 사용할 빌드가 없었다. 빌드 해시와 실제 완주 결과는 아직 없다.

## 이번에 실행한 최소 검사

work_process/tools/RunDemoManagedChecks.ps1로 기존 순수 관리 코드 검사 두 클래스의 현재 테스트 소스를 컴파일하고 실행했다. 제품 참조는 현재 PC의 Library/ScriptAssemblies에 있는 캐시 DLL이다. Unity 객체/네이티브 API/씬/JSON 파일 저장 테스트는 호출하지 않았다.

| 검사 | 결과 |
| --- | --- |
| ElevatorTravelSessionTests.CallAndDepart_TransitionsThroughExplicitStatesAndChargesOnce | PASS |
| ElevatorTravelSessionTests.Call_ZeroDepartureCost_DoesNotSpendEnergy | PASS |
| ElevatorTravelSessionTests.Call_BlockedExitOrInsufficientEnergy_DoesNotCharge | PASS |
| ElevatorTravelSessionTests.Depart_WhenSceneLoadFails_RefundsEnergyAndBlocks | PASS |
| RunLifecycleServiceTests.BeginExploration_ResetsRunAndTracksMaximumDepth | PASS |
| RunLifecycleServiceTests.NormalReturn_UsesOnlyActiveInRangeOutpostCheckpoint | PASS |
| RunLifecycleServiceTests.InvalidCheckpoint_FallsBackAndFailedTransitionPreservesActiveRun | PASS |

7개 통과 / 실패 0개 / 실행 종료 코드 0. 이미 통과한 위 검사는 관련 코드가 바뀌지 않으면 반복하지 않는다. 캐시 DLL 검사 결과를 최신 main 통합 후 전체 컴파일이나 실제 귀환 애니메이션 통과로 표현하지 않는다.

## 코드로 확인한 저장 경로 — 실행 통과와 구분

- BuildingPlacementSystem.TryMoveAt은 기존 BuildingInstance와 ID를 유지하고 이동 성공 후에만 BuildingMoved를 발행한다. 새 건설 보상이나 자원 차감 경로를 호출하지 않는다.
- WorldSnapshotSystem.OnBuildingMoved는 동일 저장 항목의 좌표·설치 영역만 갱신한다. 실제 파일 저장 시점과 재실행 복원까지 확인한 것은 아니다.
- SaveDataMapper.CaptureOutpost/RestoreOutpost에는 보관 수량과 시설 ID별 남은 대기시간 저장·복원 경로가 있다. 이는 정적 확인이며 실제 사용자 슬롯의 JSON 왕복 통과를 뜻하지 않는다.
- SaveRuntimeController의 -subterra-save-root는 Editor/Development/QA에서만 지원한다. Release에서 이 인자가 사용자 저장소를 격리한다고 가정하지 않는다.

## 데스크탑에서 이어서 할 최소 작업

1. 인계 문서 desktop-handoff-20261012.md 기준으로 Git/Unity 경로·버전을 맞춘다. PR #168 병합 여부를 다시 확인하고, 팀원이 확정한 main과 후속 코드가 함께 포함된 후보 커밋을 정한다. 임의 병합이나 사용자 변경 삭제는 하지 않는다.
2. 이 PC에서 정상 Unity 임포트·컴파일 후 QA Windows 빌드를 한 번 만든다. 메뉴는 docs/MVP2_WINDOWS_QA.md의 SubTerra > Phase P > Build Windows. 빌드 커밋·BUILD_MANIFEST·SHA-256을 기록한다.
3. QA 빌드를 새 QA 전용 저장 폴더(-subterra-save-root) 및 별도 로그 경로로 실행한다. 기존 사용자 슬롯 경로를 쓰지 않는다. Bootstrap부터 일반 조작으로 새 게임 → 채굴·시설 설치 → Demo 경로 → 귀환 → 판매·업그레이드 → 재탐사를 진행한다.
4. 시설 이동은 정상 확정 1회와 막힌 위치 거부·취소를 확인한다. 보관량과 충전/회복 대기를 기록한 뒤 저장하고 프로세스를 완전히 종료한다. 같은 QA 저장 폴더로 재실행해 이어하기 후 위치·점유·보관량·대기를 비교한다.
5. 전력 고갈/화물 가득 참/귀환 실패 복구는 별도 QA 슬롯에서 확인한다. 진행 불가·저장 손실이 보이면 그 원인부터 좁히고 관련 코드만 고친다.
6. 같은 빌드의 실제 종료 로그에서 ComputeBuffer 경고를 확인한다. 채굴 끊김이 반복될 때에만 CPU Timeline과 진단 마커를 수집한다. 이번에 최적화 또는 경고 제거를 실증한 것은 아니다.
7. Unity가 설치되지 않은 다른 Windows PC에서 같은 빌드의 완주와 이어하기를 확인한 뒤 Demo 완료로 판정한다.

단순히 새 검증 항목을 추가했다는 이유로 테스트·빌드를 반복하지 않는다. 위 실제 실행 결과가 나오기 전에는 Demo 완료/출시 준비 완료로 보고하지 않는다.

## 최신 main 통합·안내 점검 결과

이 절은 위의 main 미통합 상태보다 최신이다.
- origin/main f23f00d6을 현재 기능 브랜치에 충돌 없이 통합했다. 로컬 통합 커밋은 a30f7470이며 아직 원격에 푸시하지 않았다. GitHub PR은 병합하지 않았다. 기존 사용자 데이터 에셋 3개와 미커밋 인계 기록을 보존했다.
- B-143 키 조작 단말기와 B-144 가이드 포탈 이미지 변경이 현재 체크아웃에 포함됐다. merge-base --is-ancestor origin/main HEAD 종료 코드 0.
- 실제 조작과 다른 지상 이동 퀘스트 안내를 수정했다: '엘리베이터에서 지상 기지 선택' → '엘리베이터 안에서 상호작용 키(E)를 눌러 귀환'. ElevatorController의 Interact.started → RequestTravel 및 ElevatorPromptResolver의 E 귀환 표시와 일치시켰다. E는 기본 키이며 키 변경 기능의 실제 화면 반영은 런타임 검증 범위다.
- 첫 안내는 기존 브리핑과 첫 채굴 목표에 연결되어 있고, 이후에는 현재 목표가 표시된다. 새 브리핑/튜토리얼 구조는 추가하지 않았다.
- DemoObjectiveCatalog는 18개 목표를 정의하며 마지막 긴급 탈출 귀환을 terminal로 둔다. 완료 ReadModel은 완료 제목/설명을 사용하고 다음 행동 문구를 비운다. DemoObjectiveView는 완료 상태에 체크 아이콘을 표시한다. 기존 완료 카드 흐름을 유지하고 별도 완료 팝업은 추가하지 않았다.
- SaveDataMapper는 완료 여부와 현재 목표 ID를 저장·복원한다. 이번에는 코드 경로만 확인했으며 실제 JSON 저장/이어하기 통과가 아니다.
- 바꾼 DemoObjectiveCatalog 한 파일을 기존 의존 DLL로 경량 컴파일: 종료 코드 0. git diff --check 통과. 최신 main의 전체 신규 UI·테스트 어셈블리 컴파일은 아직 미검증이다. 생성 csproj가 main의 새 파일 목록을 반영하지 않을 수 있으므로 데스크탑 Unity에서 정상 재생성/컴파일한다.
- Unity/Windows 빌드·플레이·새 성능 측정을 실행하지 않았다. 기존 상태 검사 7개는 관련 로직이 바뀌지 않아 반복하지 않았다. 임시 컴파일 출력은 삭제했다.
- 문구 수정과 이 추가 기록/순수 상태 검사 도구는 아직 미커밋이다. 원격 전달 전 커밋·푸시 여부를 확인한다.

## 데스크탑 검증이 끝난 뒤의 작업 순서

이 절은 다음 작업 계획이며, 검증 완료나 신규 콘텐츠 구현 승인을 뜻하지 않는다. 실제 결과를 아래 기준에 따라 분류하고 이어간다.

### 1. 결과를 남기고 필요한 문제만 수정

- Git 커밋, 빌드 SHA-256, PC/해상도, QA 저장 폴더, 완주·이어하기·종료 결과를 기록한다. 실패는 재현 순서·기대 결과·실제 결과·관련 로그/사진을 함께 남긴다. 사용자 저장 내용이나 민감한 로컬 경로를 공개 PR에 그대로 올리지 않는다.
- 진행 불가·저장 손실·시설 상태 초기화가 있으면 먼저 원인을 확인하고 수정한다. 변경한 흐름과 바로 연결된 저장/복원만 재검증한다.
- 채굴 끊김이 반복되면 같은 장면에서 짧게 CPU/GPU·프레임 지연을 측정한다. 측정 도구·OS 메모리 압박을 구분하고 확인된 병목만 한 가지씩 수정한다. 목표 PC와 성능 기준을 먼저 합의하며, 평균 FPS만으로 개선을 판단하지 않는다.
- ComputeBuffer 경고가 남으면 패키지가 Embedded 15.1.0인지와 해제 이벤트 적용을 확인한 뒤 빈 장면/실제 Demo 종료 로그를 비교한다. 정상 종료 여부와 경고 제거 여부를 각각 기록한다.
- 안내 때문에 진행을 못 한 경우에만 실제 입력·현재 목표·완료 문구를 보완한다. 사다리 접점·호버·시설 이미지의 작은 차이는 조작이나 정보 전달을 방해하지 않으면 후속 목록으로 옮긴다.

### 2. 필수 검증이 통과하면 Demo 후보 빌드 마감

- 새 게임 완주·귀환·판매/업그레이드·완전 종료 후 이어하기 및 다른 Windows PC 실행 결과를 확인한다. 실패를 숨긴 채 Demo 완료로 표시하지 않는다.
- 최종 변경을 커밋·푸시하고 PR 설명에 실제 통과 결과와 남은 제한을 적는다. PR 병합은 팀원이 수행한다. 팀원이 확정한 배포 커밋으로 빌드하며, 테스트한 빌드와 배포 빌드가 다르면 변경 범위만큼 다시 확인한다.
- 전달할 Windows 패키지 하나와 실행 안내, 기본 조작, 알려진 문제, 빌드 해시를 정리한다. 디버그/진단 전용 파일과 QA 슬롯을 배포 패키지에 넣지 않는다. Release에서는 QA 저장 경로 인자가 적용되지 않는 점을 확인한다.
- 팀원이 최종 확인한 뒤 Demo 마일스톤을 확정하고 그 버전의 기록·빌드 식별자를 보존한다. 통과한 전체 검사나 동일 빌드를 이유 없이 반복하지 않는다.

### 3. Demo 이후 첫 개발은 작은 몬스터 구간부터

사용자의 확장 방향은 몬스터 → 던전 → 다른 행성과 상위 자원·아이템이다. 아래는 이 방향을 작게 나눠 검토할 제안이며, 구현 시작 전 범위와 게임 규칙을 합의한다.

1. **몬스터 1종 + 작은 시험 구간:** 발견/추적/공격/피격/사망, 플레이어 대응 수단과 안전한 귀환을 먼저 연결한다. 채굴·건설과 함께 있어도 진행을 막거나 저장을 깨지 않는지 확인한다. 전투 수치·보상은 데이터로 관리하고 기존 탐사 경제와 함께 검토한다.
2. **던전 1개:** 진입 조건·입구/출구·완료 보상·실패 복귀를 한 구간으로 연결한다. 적 처치·보상·재방문 상태의 저장 범위를 정하고 보상 중복을 막는다. 무한 생성이나 몬스터 대량 추가는 먼저 하지 않는다.
3. **행성 이동 1회 + 상위 자원·제작 경로 1개:** 출발/도착 조건, 기존 행성 복귀, 행성별 월드 변경점과 시설 저장을 설계한다. 자원·레시피·장비의 영구 ID와 세이브 호환성을 정한 뒤 수집 → 제작 → 활용의 한 흐름을 만든다.

각 단계는 앞 단계의 작은 플레이 구간이 성립한 뒤 확대한다. 몬스터·던전·행성의 전체 시스템을 한 PR로 구현하지 않는다.

### 다음 Codex에 전달할 요청

이 문서의 데스크탑 실행 결과부터 확인하세요. 진행 불가/저장 문제가 남으면 해당 원인만 수정하고 관련 검사를 최소 실행하세요. 필수 흐름이 통과하면 Demo 후보 빌드와 팀원 전달 기록을 마감하세요. 이후에는 몬스터 1종과 작은 시험 구간의 규칙을 먼저 정리하며, 던전·행성 확장을 동시에 시작하지 마세요. 기록에 없는 검증을 통과했다고 추측하거나 팀원의 PR을 임의 병합하지 마세요.
