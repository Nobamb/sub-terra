# PlayMode 실행 시간 최적화 결과

2026-10-04, `MVP2-fix53`. 실행 방법과 **분리 전후 검증 항목 대응표**는 [TEST_EXECUTION.md](TEST_EXECUTION.md)에 있다. 테스트별 실제 이름·성공/실패/스킵·시간·선택 목록·단계 계측과 비교에서 제외한 시도는 [측정 원자료](evidence/playmode-optimization/results.json)에 보존했다. 상세 NUnit XML/진행 기록은 로컬 `sub-terra/Temp/test-timing/<실행 이름>/`에 있다.

## 측정 조건과 비교 기준

같은 Windows PC의 같은 열린 Unity 6000.5.4f1 Editor, UTF 1.7.0, Direct3D12, 일반 렌더링 모드, domain reload 비활성 조건에서 비교했다. 네 UI fixture를 동일한 이름 필터로 선택했고, Unity TestRunnerApi의 NUnit suite/test duration을 사용했다. 이 시간에는 테스트와 fixture 준비·정리가 포함되지만 Editor 시작, 코드 컴파일, 테스트 발견과 PowerShell 프로세스 시작 시간은 포함되지 않는다. 별도 PC/Player/batch 결과로 일반화하지 않는다.

수정 전 원본 두 실행은 준비 브리핑/입력 타이밍 때문에 6개 중 3개, 4개가 실패했다(`before-1`, `before-2`). 조기 실패한 시간은 절감 비교에 사용하지 않았다. 초기 계측 `before-ready-1/2`는 기존 혼합 테스트에 **브리핑이 실제 표시된 뒤 정상 닫기, 입력 프레임 전달 안정화, 잘못된 해상도 정리 인덱스 수정만** 적용했다. 기존 고정 대기, 캡처, 해상도 반복, 기능/레이아웃 assert를 유지한 채 6/6 통과했다. 두 번째 초기 계측은 같은 대기에 계측을 추가했다.

최종 비교는 `before-matched-1/2`와 `after-isolated-daily/full-1/2`다. 전체 프로젝트의 기존 테스트가 자동 저장한 에셋·설정을 복원한 뒤 측정했다. 양쪽에 같은 진행 중 세션 데이터, 기준 1920×1080, 테스트별 InputSettings 복사본을 적용해 관련 없는 첫 브리핑/보상 팝업과 운영체제 포커스 조건의 차이를 제거했다. 기존 혼합 쪽의 고정 대기·해상도 반복·스크린샷·assert는 유지했다. 최종 쪽은 독립 기능/Visual로 분리하고 준비 상태를 기다린다. 대상 hover·설정 팝업·메뉴 전환은 실제 애니메이션 완료까지 기다린다.

`before-controlled-1..4`와 `after-clean-*`는 입력 실패로 비교에서 제외했다. 원인은 Game View 내부 포커스가 있어도 Editor가 OS 포커스를 잃으면 입력 이벤트가 폐기되는 것이었다. 양쪽의 테스트 전용 설정 복사본에서 합성 입력을 전달하고 종료 시 원본 설정을 복원해 해결했다. 프로젝트 InputSettings/ProjectSettings 파일은 수정하지 않았다. 아래 수치는 **분리와 조건 대기·공통 준비 변경을 합친 효과**이며, 각 변경 또는 렌더링/디스크의 개별 절약 시간을 뜻하지 않는다.

## 실제 반복 측정

| 실행 경로 | 1회 | 2회 | 평균 | 매회 실제 결과 |
| --- | ---: | ---: | ---: | --- |
| 기존 혼합, 준비 조건 통일 | 36.708초 | 36.168초 | 36.438초 | 6 실행, 6 성공, 실패/스킵 0 |
| 최적화 일상 기능 | 15.790초 | 15.949초 | 15.869초 | 5 실행, 5 성공, 실패/스킵 0 |
| 최적화 기능+Visual | 33.773초 | 33.012초 | 33.393초 | 10 실행, 10 성공, 실패/스킵 0 |

일상 반복은 평균 20.569초(56.4%) 감소했다. 모든 기존 검증을 포함하고 기능 일부를 Visual에서도 재검증하는 기능+Visual 경로도 평균 3.045초(8.4%) 감소했다. 일상 테스트 수의 감소만으로 성공을 판정하지 않았다. 기존 5:4 빈 슬롯 테스트는 Visual에 있으며, 일상의 세이브/취소/덮어쓰기/중복 시작 검증은 별도로 남아 있다.

fixture 내 실제 테스트 시간 합계 평균은 다음과 같다. suite 준비 차이 때문에 위 suite duration과 합계가 정확히 같지는 않다.

| Fixture | 기존 혼합 | 일상 기능 | 기능+Visual |
| --- | ---: | ---: | ---: |
| B105 사이드 메뉴 | 18.484초 | 9.333초 | 18.262초 |
| B110 건설창 | 4.378초 | 0.825초 | 2.447초 |
| B112 인벤토리 | 4.639초 | 0.773초 | 2.307초 |
| Issue119 전체 슬롯 흐름 | 8.881초 | 4.873초 | 10.321초 |

Issue119 전체 경로는 기준 해상도 기능 테스트와 두 비율 Visual에서 저장/입력 흐름을 각각 검증하므로 시간이 늘었다. 이 비용을 숨기거나 중요한 검증을 제외하지 않았다.

## 확인한 병목과 변경

다음 상세 단계 숫자는 최초 `before-ready-2`와 `after-daily-final-2/after-visual-final-1` 계측으로 병목을 선정한 근거다. 최종 동일 조건 성능 비교는 위 반복 측정 표를 사용한다.

- B105 기존 고정 준비/설정/해상도 대기는 계측 2.911초, 캡처 전용 대기 9회는 1.668초였다. 해상도 반복의 6회 실제 전환은 유지하되 완료 조건을 기다린다. 일상의 실제 hover/전환 계측은 6.862초이며, 0.25초 중간 상태 검사 2회도 0.510초로 유지했다. 실제 연출 시간은 줄이지 않았다.
- B110/B112 각각 씬 준비 1초 및 해상도 준비 0.3초 4회의 계측은 2.210/2.208초였다. 기존 캡처 7/8회의 대기 포함 시간은 1.442/1.656초였다. 최종 Visual의 렌더 완료·PNG 저장 대기는 각각 0.571/0.591초였다. 일상에서는 캡처를 호출하지 않는다.
- Issue119의 0.2초 대기는 캡처가 아니라 해상도 변경 대기였다(4회 0.804초). 원자료의 초기 `capture-fixed` 라벨은 이 숫자 기반 분류이므로 의미 해석 시 주의한다. 0.5초 대기도 설정 애니메이션과 SurfaceBase 준비가 섞여 있다. 이를 실제 설정 coroutine 종료/비활성화와 SurfaceBase 로드·서비스 준비 조건으로 바꿨다. Visual에서 두 비율의 모든 원래 입력/보존 assert를 유지했다.
- 최종 일상의 Integration 로드 계측은 B105 0.927초, B110 0.476초, B112 0.495초였다. 각각 준비 전체는 1.079/0.584/0.612초였다. 준비 전체에 하위 씬/해상도/바인딩 시간이 포함되므로 중복 합산하지 않는다. 순수 씬 로드 비용을 무조건 1초 기다리는 것으로 대체하지 않는다.
- 최종 Visual은 26개 PNG를 생성했다(B105 9, B110 7, B112 8, Issue119 2). 렌더/파일 대기는 합계 2.060초였다. CPU 렌더, PNG 인코딩, 디스크 쓰기를 개별 profiler sample로 분리하지는 못했다. PNG 존재·헤더 해상도를 확인하며 기존 화면 경계/겹침/텍스트 잘림 assert도 유지한다.
- 씬 준비, 테스트 데이터, 입력 장치, 버튼 raycast와 실제 클릭, 해상도 선택·복원·삭제를 테스트 전용 헬퍼로 모았다. 각 테스트는 독립 환경을 만들고 실패 후에도 TearDown/Dispose로 정리한다. 합성 입력용 InputSettings는 메모리 복사본만 변경하고 원본을 복원한다. 고유 라벨로 자신이 만든 해상도만 제거하며, 기존 해상도가 있으면 재사용한다.
- 조건 대기는 Stopwatch 실시간 제한과 대상 이름을 사용한다. 시간 정지에도 timeout이 작동한다. 강제 애니메이션 완료나 게임 시간 가속을 추가하지 않았다.
- 화물 정책의 순수 계산 9개는 파라미터/assert를 그대로 EditMode로 이동했다. 실제 이동·점프·등반·낙하 테스트는 PlayMode에 유지했다. 별도 quick EditMode에서 9/9 통과했지만 이 이동 자체의 속도 개선을 따로 주장하지 않는다.

## 러너와 실행 경로 검증

UTF 1.7.0의 설치된 Filter/TestRunnerApi/CallbacksHolder 구현을 확인했다. categoryNames 제외 문자열에 의존하지 않고 발견된 프로젝트 테스트의 Visual category를 부모까지 확인한 뒤 정확한 testNames를 선택한다. 클래스 전체 Visual/Explicit 처리는 하지 않았고 Explicit 속성을 새로 도입하지 않았다.

| 실제 실행 | 선택 | 성공 | 실패 | 스킵 | NUnit 시간 | 프로세스 종료 코드 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| quick EditMode 화물 계산 | 9 | 9 | 0 | 0 | 0.034초 | 0 |
| quick PlayMode B112 | 1 | 1 | 0 | 0 | 2.003초 | 0 |
| Visual 네 UI, 최종 코드 | 5 | 5 | 0 | 0 | 18.151초 | 0 |
| 프로젝트 전체 EditMode, 최종 코드 | 1084 | 1065 | 19 | 0 | 39.908초 | 1 |
| 프로젝트 전체 PlayMode, Visual 포함, 최종 코드 | 125 | 116 | 8 | 1 | 59.945초 | 1 |
| 프로젝트 일상 EditMode | 1084 | 1065 | 19 | 0 | 17.693초 | 1 |
| 프로젝트 일상 PlayMode | 120 | 111 | 8 | 1 | 41.573초 | 1 |
| 존재하지 않는 quick 필터 | 0 | 0 | 0 | 0 | 실행 전 실패 | 1 |

전체/일상 프로젝트 실행은 Both 요청으로 EditMode **이후** PlayMode가 각각 실제 실행됐다. 모든 결과의 Missing/Unexpected는 비어 있다. 일상 120 + Visual 5의 선택 목록은 전체 125와 일치한다. 이동한 화물 9개는 전체 EditMode 결과에도 모두 존재한다. 전체 실행의 변경 UI 10개는 모두 통과했다. 전체와 일상의 시간은 별도 목적의 실행이고 EditMode 캐시 상태 영향도 있으므로 프로젝트 전체 성능 비교로 사용하지 않는다.

프로젝트 전체를 실행한 뒤 OS 포커스 문제를 해결했고, 최종 코드로 `project-full-isolated-final` Both를 다시 실행해 위 전체 결과를 확인했다. 프로젝트 일상 Both/quick 행은 이전 실행 기록이며, 최종 일상 UI 5개는 반복 측정 표에서 별도로 확인했다. 최종 전체의 기능 부분은 일상 선택 목록과 동일한 120개(111 성공, 8 실패, 1 스킵)다.

기존 HeadlessTestRunner는 실패 여부와 관계없이 Exit(0)을 호출했다. 이제 공통 러너의 집계 실패 코드로 종료한다. 실패/스킵/실행 시간/선택·실행 목록/누락을 기록하며, 0개 선택 또는 0개 실행을 성공으로 처리하지 않는다. Both에서 한 모드의 실패를 다른 모드 성공으로 덮지 않는다. 시작 씬을 테스트 실행 중 비우고 종료 후 복원하며, 기존 Bootstrap 기본 시작 hook이 테스트 초기 씬을 다시 덮는 충돌도 방지한다. 도메인 재로드 후 콜백 복구와 종료/취소 시 콜백 정리를 추가했다.

## 실패와 미검증 항목

전체 검증은 녹색이 아니다. 이번 변경 밖의 테스트에서 다음 실패를 확인했다. 실패 요약은 원자료에 보존했으며 테스트를 제외하지 않았다. 1MB 이상 에셋 전체를 출력한 assertion은 JSON에 앞부분과 원문 SHA-256만 보존했고, 전체 메시지는 로컬 NUnit XML에 있다.

- EditMode 19: 카탈로그에 `upgrade.cargo.gold` 누락 관련 10개, Copper/구리 기대값 1개, 지형 좌표 1개, HUD/버튼/Prefab 기존 구조 기대값 3개, 저장된 활성 씬 이름 변경 2개, popup Canvas/raycast 구성 2개.
- PlayMode 8: Copper/구리 표시 1개, Surface fallback 위치 1개, DeepZoneLocked/InvalidTarget 1개, 마우스 채굴 시작 1개, 엘리베이터 이동/door 2개, hologram prompt 2개. 수정 대상 UI 네 fixture의 실패는 없다. 이 테스트들의 최적화 전 전체 실행 기준을 확보하지 않았으므로 모든 실패가 본 작업 이전부터 재현됐다고 단정하지 않는다. 런타임/에셋 기대값 수정은 이번 범위를 벗어나 보고만 한다.
- 기존 `LadderBackBootstrapPlayModeTests` 1개는 현재 Editor 시작 인자에 격리 저장 경로가 없어 Ignore됐다. 필터 누락은 아니며 완료로 간주하지 않는다. [실행 문서](TEST_EXECUTION.md)에 필요한 새 Editor 시작 조건을 적었다.
- Visual은 실제 렌더링 Editor에서 실행했다. batch Visual은 WaitForEndOfFrame 호환성 때문에 명시적으로 거부한다. 새 캡처 헬퍼는 해당 yield를 사용하지 않는다. 이 세션에서 Editor를 종료하지 않았으므로 별도 headless Unity 프로세스의 성공/실패 Exit, batch 전체 기능, domain reload 활성 환경은 실행 검증하지 못했다. 실제 PowerShell live 경로의 성공 0/실패 1/0개 선택 1은 확인했다.
- Windows Development/QA/Release 빌드, 데모 완주, 다른 PC의 새 게임/이어하기·세이브 생성/재실행은 이번 세션에서 실행하지 않았다. 기존 [필수 QA 규칙](../init/rules/testing-qa.md)과 [Windows QA](MVP2_WINDOWS_QA.md)를 그대로 유지했다. Visual 야간 자동 실행은 제공하지 않았으며 실행 문서의 명령/메뉴가 제공된 경로다.

게임 런타임, 애니메이션 길이, 게임 시간 배율 정책, Prefab/Scene/Font/ProjectSettings/Packages 변경은 최종 diff에 남기지 않는다. 테스트 실행이 자동으로 변경한 폰트·빌드 설정·과거 캡처 증거는 복원하며, 사용자가 기존에 수정한 `init/prompt-B.md`는 보존한다.

## 변경 파일과 마지막 확인

- `Tests/PlayMode/Integration/Bootstrap/`의 `PromptB105SideMenuPlayModeTests.cs`, `PromptB110BuildingMenuPlayModeTests.cs`, `PromptB112InventoryPanelPlayModeTests.cs`, `Issue119OverwritePopupPlayModeTests.cs`: 기능/Visual 분리, 조건 대기·캡처 헬퍼 사용.
- 같은 폴더의 새 `UiPlayModeTestSupport.cs`와 meta: 독립 씬/데이터/입력/해상도 준비와 복원, 실시간 대기·계측, 실제 클릭과 캡처 확인.
- `Tests/PlayMode/Gameplay/PlayerMovementPlayModeTests.cs`, 새 `Tests/EditMode/App/Integration/CargoPolicyCalculationTests.cs`와 meta: 순수 계산 9개 이동.
- `Assets/Editor/HeadlessTestRunner.cs`, 새 `_Project/Editor/DataValidation/TestValidationRunner.cs`와 meta, 기존 `EditModeTestRunnerCommand.cs`, `BootstrapPlayModeStartScene.cs`: 실행 범위/결과/종료 코드와 테스트 시작 씬 충돌 해결.
- `tools/Run-UnityValidation.ps1`, 이 보고서, `docs/TEST_EXECUTION.md`, `docs/evidence/playmode-optimization/results.json`: 실제 실행 경로, 운영 기준, 대응표와 원자료.

위 상대 경로의 `Tests/`는 `sub-terra/Assets/_Project/Tests/`다. 마지막 컴파일 성공, `git diff --check` 통과, 선택 목록 집합 비교(120+5=125), 최종 전체의 대상 UI 10개와 화물 계산 9개 성공을 확인했다. 최종 git status에 런타임/Prefab/Scene/Font/ProjectSettings/Packages/기존 증거 PNG 변경은 없다. 커밋/푸시는 하지 않았다.
