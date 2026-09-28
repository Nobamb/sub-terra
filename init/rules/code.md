# C#·Unity 코드 규칙

3-2. C#과 Unity 직렬화 규칙을 따른다.
   - 신규 런타임 코드는 C#으로 작성한다.
   - Unity `UnityEngine.Object` 참조에는 C# Null 조건 연산자(`?.`) 대신 `if (obj != null)` 구문을 일관되게 적용한다.
   - 주요 바인딩 및 초기화 메서드(`WireContracts`, `Start` 등)는 try/catch로 방어하여 예외 발생 시 원인 로그 출력 및 부작용을 최소화한다.
   - 고정 정의 데이터는 ScriptableObject, 실행 중 상태는 일반 C# 객체, 영구 상태는 `[Serializable]` DTO로 분리한다.
   - 구체 구현이 아닌 Shared 인터페이스와 이벤트를 통해 Gameplay와 App 시스템을 연결한다.
   - 새 타입과 필드는 현재 작업에 필요한 범위만 정의한다.

   3-3. 파일 성격에 맞는 Unity 구조를 사용한다.
   - MonoBehaviour는 Scene 또는 Prefab 수명주기가 필요한 기능에 사용한다.
   - 순수 계산은 가능한 한 일반 C# 클래스로 분리해 Edit Mode 테스트가 가능하게 한다.
   - 정적 게임 수치를 코드에 하드코딩하지 않고 담당 ScriptableObject에서 관리한다.
   - 프로젝트가 이미 사용하는 네임스페이스, Assembly Definition과 직렬화 형식을 따른다.

   3-4. 참조와 의존성은 기존 스타일을 따른다.
   - 담당자 A는 담당자 B의 `InventoryService`, `GameState` 같은 구체 클래스를 직접 참조하지 않는다.
   - 담당자 B는 담당자 A의 Runtime Prefab 내부나 Gameplay 구현을 직접 수정하지 않는다.
   - Inspector 참조가 필요하면 필수 참조 목록과 연결 방법을 PR에 기록한다.
   - 같은 기능을 위한 중복 Service, Manager, State 또는 데이터 경로를 만들지 않는다.

   3-5. 주석은 필요한 곳에만 작성한다.
   - 구조 안정도, 전력망, 저장 복원 순서, 드론 판단 규칙처럼 이해 비용이 큰 코드에는 짧은 한국어 주석을 사용할 수 있다.
   - 모든 함수나 모든 줄에 의무적으로 주석을 달지 않는다.
   - 기존 주석은 해당 코드가 삭제되거나 의미가 틀린 경우에만 정리한다.

   3-6. 로그는 최소화한다.
   - 개발 확인용 로그는 필요한 경우에만 사용하고, 작업 완료 전 불필요한 로그는 제거한다.
   - API Key, Token, Secret, 로컬 세이브 원문과 외부 AI 응답 원문 전체를 로그로 출력하지 않는다.
   - 반복되는 Update 로그, Tilemap 전체 덤프와 매 프레임 상태 출력은 금지한다.
   - Release Profile에서는 디버그 메뉴와 불필요한 상세 로그를 제거한다.
