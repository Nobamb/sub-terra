# Phase A 구현 기록

## 구현

- `Assets/_Project/Scripts/App/State/GameState.cs`
  - Unity Object를 보유하지 않는 `GameState`, `PlayerState`, `ProgressState`, `RunState`
  - `GameState.CreateNew()`에서 새 게임 기본값을 한 곳에서 생성
- `Assets/_Project/Scripts/App/Core/BootstrapContracts.cs`
  - 카탈로그/세이브/씬 전환 포트와 논리 씬 이름
- `Assets/_Project/Scripts/App/Core/GameBootstrapper.cs`
  - SubsystemRegistration 정적 초기화
  - 단일 영속 Root 및 중복 Bootstrap 제거
  - 카탈로그 검증 → 세이브 확인 → State 생성/복원 → MainMenu 전환 순서
  - 실패 시 원인을 포함한 단일 오류 로그와 Scene 전환 차단

## 검증

- Unity MCP `GetConsoleLogs`가 정상 응답했고 Console 오류/경고는 0개였다.
- Unity Editor 명령으로 `Assets/_Project/Scenes/Bootstrap/Bootstrap.unity`와 `Assets/_Project/Scenes/App/MainMenu.unity`를 생성했다.
- `EditorBuildSettings.asset`에서 Bootstrap이 첫 Scene이고 MainMenu가 등록된 것을 확인했다.
- RunCommand 컴파일/실행 결과가 성공했다. Play Mode 왕복 테스트와 실제 중복 로드 검증은 아직 미실행이다.
- Edit Mode 테스트 어셈블리와 `GameStateTests`를 추가했고, 이후 Console 재조회에서 오류/경고가 0개임을 확인했다.
- Bootstrap 씬을 열고 Play Mode를 요청했으며, 직후 Console에 오류/경고가 없었다. MCP 재탐지 지연으로 활성 씬/State 객체를 별도 명령으로 읽는 단계는 실행되지 않았다.
- `Initialize(catalog, save, scenes)` 주입 경로와 카탈로그 실패 차단/새 상태 생성 Edit Mode 테스트를 추가했다. Unity MCP RunCommand의 컴파일 체크가 성공했고 컴파일 로그는 비어 있었다.
- Unity Test Runner API로 `SubTerra.App.Tests.EditMode` 실행을 요청했고 요청 자체는 컴파일/실행 성공했다. 비동기 콜백 결과는 MCP Console 수집에 나타나지 않아 통과 여부는 미확정으로 기록한다.
- `AssetDatabase.Refresh()` 기반 재컴파일/자산 검증 명령이 성공했고, 직후 Console 오류/경고는 0개였다.
- State의 직접 쓰기를 막도록 읽기 전용 프로퍼티와 `AddGold` 같은 의도 기반 메서드로 변경했다. 변경 직후 Unity MCP가 재탐지되지 않아 이 마지막 변경의 Editor 컴파일 결과는 아직 미확정이다.
- 재연결 후 최종 State API 컴파일이 성공했고 컴파일 로그가 비어 있었다. Bootstrap Play Mode 스모크 테스트도 시작되었으며 직후 Console 오류/경고는 0개였다.
- 명세의 Play Mode 테스트 경로에 `BootstrapPlayModeTests`와 전용 테스트 asmdef를 추가했다. 추가 직후 Unity MCP 재탐지 지연으로 새 테스트 자산의 컴파일 결과는 미확정이다.
- Unity 6의 deprecated API 경고를 확인해 `FindObjectsByType(..., FindObjectsSortMode.None)`로 수정했다. 수정 후 재컴파일은 MCP 재탐지 지연으로 미확정이다.
- Unity 6.3의 추가 deprecated 안내에 맞춰 정렬 인자 없는 `FindObjectsByType<T>()`로 최종 수정했다. 수정 직후 재컴파일은 MCP 재탐지 지연으로 미확정이다.
- 재연결 후 `AssetDatabase.Refresh()` 검증이 컴파일 성공/빈 컴파일 로그로 완료됐고, Unity Console 오류/경고도 0개였다.
- `UnitySceneLoader`에 잘못된 씬 이름/로드 예외를 `false`와 원인 로그로 반환하는 경계를 추가했다. Unity MCP 재탐지 지연으로 이 마지막 변경의 컴파일은 미확정이다.
- 재시도 후 SceneLoader 변경 컴파일이 성공했고 컴파일 로그가 비어 있었다. 직후 Unity Console 오류/경고도 0개였다.
- MainMenu 씬 전환 실패를 주입해 초기화를 중단하는 Edit Mode 테스트를 추가했다. 추가 직후 MCP 재탐지 지연으로 재컴파일 결과는 미확정이다.
- 재연결 후 failure-path 테스트 포함 자산 새로고침/컴파일이 성공했고 컴파일 로그 및 Unity Console 오류/경고가 0개였다.
- Unity reflection 점검에서 `SubTerra.App.Tests.EditMode` 어셈블리 로드 및 테스트 타입 2개, `SubTerra.App.Tests.PlayMode` 로드 및 테스트 타입 1개를 확인했다.
- 실제 Play Mode에서 Bootstrap 진입 후 `ActiveScene=MainMenu`, Root/State 존재, `Failed=False`를 확인했다. Bootstrap 재로드 직후에는 씬 교체 프레임 중 임시로 RootCount=2가 관찰됐지만, 다음 프레임을 기다리는 Play Mode 테스트는 단일 Root를 검증하도록 작성되어 있다. 동일 Root 참조와 State 유지도 확인했다.
- State 클래스에 직렬화/복원 경계용 기본 생성자를 추가했다. 추가 직후 Unity MCP 재탐지 지연으로 재컴파일 결과는 미확정이다.
- 재연결 후 State 생성자 변경 컴파일이 성공했고 컴파일 로그 및 Unity Console 오류/경고가 0개였다.

## Phase A 보정 (요구사항/기능/보안)

### 수정한 결함
- `GameState` 하위 State를 public 필드에서 읽기 전용 프로퍼티로 변경하고 `FromParts`/`IsComplete`로 복원·완전성 경계를 명시.
- 부트스트랩 실패 폐쇄 강화: null 포트, 세이브 null/불완전 상태, 포트 예외, 씬 로드 실패 시 MainMenu 전환 차단 + 단일 오류 로그.
- `UnitySceneLoader`가 `Application.CanStreamedLevelBeLoaded`로 미등록 씬을 성공 처리하지 않도록 수정.
- 성공 초기화 후 `Initialize` 재호출이 기존 State를 덮어쓰지 않도록 `IsInitialized` 가드 추가.
- 자동 초기화를 `Start`로 옮겨 Edit Mode에서 포트 주입 후 `Initialize` 직접 호출이 가능.
- 예외 로그는 메시지 본문 대신 예외 타입명만 기록(경로/세이브 본문 유출 방지).
- 고비용 경계(싱글톤/DDOL, 초기화 순서, Fail, 포트)에 한국어 주석 보강.

### 검증
- Edit Mode `SubTerra.App.Tests.EditMode` 21개 전부 통과 (성공 경로, 카탈로그 실패, null/불완전 세이브, 씬 실패, 재초기화 보호, 중복 Root no-op, SceneLoader 실패).
- 런타임 asmdef에 `UnityEditor` 참조 없음. Bootstrap이 Build Settings 첫 씬.
- Play Mode 스모크: `ActiveScene=MainMenu`, Root 1, `IsInitialized=True`, `Failed=False`, State 완전.
- Play Mode Bootstrap 재로드: RootCount=1, 기존 Gold/State 유지(`DuplicateDiscardSafe=True`).

### A-F02 보정
- 중복 Root: `IsDuplicateDiscarded` + `enabled=false` + Play Mode `Destroy` 예약.
- `Start`/`Initialize` 모두 폐기 대상·비-Instance Root에서 no-op.
- `Initialize`가 `Instance==null`이면 Root 채택(Edit Mode Awake static 미설정 대비).
- 활성 Root의 `OnDestroy`만 static Instance를 비움.
