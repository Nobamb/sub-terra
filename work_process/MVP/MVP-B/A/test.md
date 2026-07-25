# Phase A Agent Test

## 1. 정적 검증 항목

| ID | 테스트 항목 | 검증 방법 | 예상 결과 |
| :-- | :-- | :-- | :-- |
| A-S01 | 소유 영역 준수 | 변경 파일 목록 확인 | App, B 소유 Scene·Tests 외 변경 없음 |
| A-S02 | 런타임/Editor 분리 | asmdef와 참조 확인 | 런타임 어셈블리에 `UnityEditor` 참조 없음 |
| A-S03 | 초기화 순서 | `GameBootstrapper` 코드 검토 | 카탈로그 검증과 세이브 확인 후 State/Scene 처리 |
| A-S04 | 직렬화 안전성 | State 필드 형식 확인 | Unity Object가 State에 저장되지 않음 |
| A-S05 | Scene 등록 | Build Profile/EditorBuildSettings 확인 | Bootstrap이 첫 진입 Scene |

## 2. 기능 테스트 항목

### A-F01: 기본 State 생성

- **준비:** 세이브 없음과 성공하는 카탈로그 대역을 사용한다.
- **실행:** bootstrapper 초기화를 실행한다.
- **예상 결과:** 네 State가 안전한 기본값으로 한 번 생성되고 Main Menu 전환이 요청된다.

### A-F02: 중복 Bootstrap 방지

- **준비:** Bootstrap Root가 존재하는 상태에서 Bootstrap Scene을 다시 연다.
- **실행:** 한 프레임 이상 진행한다.
- **예상 결과:** 전역 Root와 서비스가 각각 하나만 남고 기존 State가 유지된다.

### A-F03: 초기화 실패 차단

- **준비:** 중복 데이터 ID 오류를 반환하는 카탈로그 대역을 연결한다.
- **실행:** 초기화를 실행한다.
- **예상 결과:** Main Menu 전환이 발생하지 않고 오류 원인이 한 번 기록된다.

### A-F04: Scene 왕복 상태 유지

- **준비:** PlayerState 값을 Service 경로로 변경한다.
- **실행:** MainMenu → 임시 B 소유 Scene → MainMenu로 왕복한다.
- **예상 결과:** 값과 GameState 인스턴스가 유지되고 UI용 참조를 다시 연결할 수 있다.

## 3. 테스트 절차

1. Edit Mode에서 State 기본값, 초기화 순서, 실패 결과 테스트를 실행한다.
2. Play Mode에서 Bootstrap 단독 시작, 중복 로드, Scene 왕복을 실행한다.
3. Enter Play Mode 옵션의 Domain Reload 켬/끔에서 각각 한 번 확인한다.
4. Console의 Error, Exception, 예상하지 않은 Warning을 확인한다.
5. 결과와 미실행 항목을 아래에 기록한다.

## 4. 검증 결과 요약

- **모든 항목 통과 시:** Phase A 완료. Phase B가 카탈로그 구현체를, Phase K가 세이브 구현체를 같은 초기화 경계에 연결할 수 있다.
- **실패 항목 존재 시:** 중복 생성, 호출 순서, Scene 등록, 참조 누락 중 어느 층의 문제인지 구분해 수정하고 전체 항목을 재실행한다.
