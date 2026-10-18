# 시설 이동 후 저장·이어하기 검증 — 2026-10-13

## 범위와 현재 상태

- 사용자 요청: 다음 작업 1번(이동 위치·보관량·회복/충전 대기 보존)부터 확인한다.
- 기준 코드: codex/demo-visual-issues-170-174 / fa62076b / PR #175. 실제 검사 시 Git HEAD와 이후 main 통합 여부를 다시 기록한다.
- 기존 데스크탑 시설 이동/점유/스냅샷 검사 12개 통과는 notebook-visual-review-20261013.md의 결과이며 이번에는 반복하지 않았다.
- 현재 단계는 **진행 중**이다. 관리 코드 상태 변환 검사는 통과했고 실제 Unity JSON 파일 저장·재실행·이어하기는 남아 있다. 새 오류를 재현하지 않았으므로 제품 코드를 추측으로 수정하지 않았다.
- 최초 읽기 확인: Unity 프로세스 없음, RAM 여유 약 1.15GB / 총 7.40GB, 디스크 여유 약 13.14GB. 이후 사용자 요청으로 아래 Unity 최소 검사를 한 번 시도했다. 사용자 슬롯은 읽거나 변경하지 않았다.

## 이번에 실행한 검사 1개

`SaveServiceTests.MovedFacilities_MapperPreservesStorageAndCooldownIdentity`를 현재 테스트 소스와 기존 캐시 App DLL로 컴파일하여 관리형 실행했다. PASS / 종료 코드 0.

검사 데이터:
- 보관함 storage.moved: (3,-5), 1×1, 레벨 2 / 체력 0.75.
- 충전기 charger.moved: (7,-5), 2×2, 레벨 3 / 체력 0.8, 대기 120초.
- 보건소 clinic.moved: (11,-5), 2×2, 레벨 4 / 체력 0.9, 대기 240초.
- 공용 보관량: 구리 7 / 철 4. 골드 350.

실제 SaveDataMapper.Capture/TryRestore를 통과한 후 ID·유형·좌표·설치 크기·레벨·체력·보관량·두 대기를 비교했다. 복원 OutpostState가 원본과 별도 객체이며, 복원 상태의 플레이 시간 5초 경과 후 충전 대기는 115초가 되고 원본은 120초로 유지되는지도 확인했다.

좌표 fixture는 이미 이동된 스냅샷을 표현한다. 이번 관리형 검사는 실제 시설 드래그/배치, Unity 객체 복원/점유 또는 플레이어의 Continue 클릭을 실행하지 않았다. Unity JsonUtility를 .NET의 다른 직렬화기로 대체하여 통과했다고 보고하지 않는다.

반복 가능한 도구: `work_process/tools/RunManagedFacilitySaveCheck.ps1`. 이 PC의 생성 csproj/캐시 DLL과 Unity 번들 SDK가 필요하다. 다른 PC의 Library를 복사하지 않는다. 출력은 tools/facility-save-check-output이며 이번 실행 후 삭제했다. 관련 코드가 바뀌지 않으면 이 통과 검사를 반복하지 않는다.

## 추가한 실제 파일 검사 — 실행 시도했으나 결과 없음

기존 SaveServiceTests에 `MovedFacilities_FileSaveAndFreshLoadPreserveState`를 추가했다. TestCase 2개:

1. Mine_Demo_Integration: 실제 WorldProvider의 캡처 경로로 저장.
2. SurfaceBase: provider 없이 마지막 광산 snapshot을 MineWorldFallback으로 저장.

두 경우 모두 제품 SaveService/SaveJsonCodec으로 별도 임시 폴더의 슬롯 2를 저장하고, 새 PhysicalSaveFileSystem/SaveDataMapper/SaveJsonCodec/LoadService로 실제 JSON을 다시 읽어 동일 상태를 비교한다. 임시 파일이 남지 않는지와 목표 씬도 확인한다. NUnit SetUp이 QA 전용 임시 폴더를 만들고 TearDown이 자기 폴더만 정리한다. 사용자 실제 슬롯을 사용하지 않는다.

새 파일 검사 소스도 경량 컴파일됐으나 실제 실행은 하지 않았다. Unity 없이 실행 가능한 관리형 도구는 이 두 메서드를 호출하지 않는다.

### 사용자 요청 후 Unity 실행 시도

- Unity 6000.5.4f1을 batchmode / nographics로 실행하고 위 파일 검사 2개만 필터링했다. Windows 빌드나 전체 테스트는 실행하지 않았다.
- 실행 직전 여유 RAM은 0.53GB / 7.40GB, 디스크는 13.27GB였다. 기존 Unity 프로세스는 없었다.
- 약 182초 후 제가 시작한 Editor의 private memory가 3.00GB, 여유 RAM이 0.26GB가 되어 설정한 실행 상한에 따라 해당 프로세스만 중단했다. 중단 후 여유 RAM은 약 1.12GB였다.
- 로그에는 스크립트 컴파일과 어셈블리 재로드가 기록됐으나 결과 XML은 생성되지 않았다. 파일 검사 2개는 통과/실패를 판단할 수 없다. 실제 게임의 완전 종료·이어하기 역시 미완료다. 이 시도에서 저장 버그를 재현했다고 해석하지 않는다.
- 실행 전 ProjectSettings 및 DefaultVolumeProfile 바이트를 보존하고 복원했다. 기존에 Git으로 관리되던 InitTestScene 두 파일은 그대로 보존했다. 패키지 파일의 변경은 없다.
- 진단 로그: `work_process/facility-save-native-20261013/editor.log`. 이 PC에서 동일한 무거운 실행은 반복하지 않고 아래 데스크탑 절차로 이어간다.

## 데스크탑에서 이어서 할 최소 검사

1. 최신 후속 커밋을 받은 뒤 정확한 Git/Unity 프로젝트 경로에서 정상 컴파일한다. Editor Test Runner → EditMode에서 `MovedFacilities_FileSaveAndFreshLoadPreserveState`만 선택해 2개 TestCase를 실행한다. 결과/XML·코드 커밋을 기록한다. 전체 이동 검사 12개나 전체 테스트를 기본으로 반복하지 않는다.
2. 같은 QA Windows 빌드를 **새 QA 전용 저장 경로**로 실행한다. 저장 경로 분리 인자는 QA/Development에서만 지원하며 Release에 적용된다고 가정하지 않는다. 기존 사용자 저장을 덮어쓰지 않는다.
3. 구리/철을 보관하고 충전/회복을 한 번 사용한다. 보관함·충전기·보건소를 이동한 뒤 위치·보관량·남은 대기와 퀘스트 진행을 기록하고 광산에서 저장한다. 게임 프로세스를 완전히 종료하고 같은 경로로 이어하기를 실행한다.
4. 이동 위치와 설치 점유가 맞는지, 보관량과 기존 대기가 초기화되지 않았는지 확인한다. 대기는 실제 플레이 경과만큼 줄 수 있으므로 사용자 화면에서 초 단위 값이 무조건 같아야 한다고 요구하지 않는다. 초기화/중복 시설/새로 차감된 자원 여부를 구분한다.
5. 지상 귀환 후 저장 → 완전 종료 → 이어하기 → 광산 재진입을 한 번 확인한다. 지상에서 저장했다는 이유로 이동한 광산 시설/보관량이 사라지지 않아야 한다. 변경이 없으면 같은 경로를 반복하지 않는다.
6. 두 자동 파일 검사와 실제 QA 저장·재실행이 끝난 뒤에만 작업 1번 완료로 기록한다. 그 전에는 실패 복구/전체 Demo 완주 완료로 확대하지 않는다. 실패가 있으면 저장 파일을 보존하고 ID·좌표·대기의 최초 불일치 지점부터 조사한다.

이번 추가 테스트·도구·기록은 아직 로컬 미커밋·미푸시다. GitHub 이슈/PR이나 제품 저장 형식·버전·패키지는 변경하지 않았다.
