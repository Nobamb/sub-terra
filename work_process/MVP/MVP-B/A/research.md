# Phase A — Bootstrap과 기본 프로젝트 상태

## 1. 개요

`process-B`의 B-0을 구현한다. 게임 진입점, 장면 전환, 전역 State의 수명과 초기화 순서를 먼저 고정한다. 데이터 카탈로그와 실제 세이브 구현은 뒤 단계에서 주입할 수 있게 경계를 두되, 이 단계에서는 새 게임 상태로 Main Menu까지 안전하게 진입해야 한다.

## 2. 작업 목표

- `Bootstrap.unity` 한 곳에서만 전역 객체를 생성한다.
- `GameState`, `PlayerState`, `ProgressState`, `RunState`의 기본값과 소유 관계를 정의한다.
- 재진입이나 Scene 왕복에도 전역 서비스가 중복 생성되지 않게 한다.
- 데이터 검증 실패와 초기화 실패를 원인 포함 로그로 남기고 잘못된 상태로 다음 Scene에 진입하지 않는다.

## 3. 구현 범위

- `Assets/_Project/Scripts/App/Core/`: `GameBootstrapper`, `SceneLoader`, 시작 순서
- `Assets/_Project/Scripts/App/State/`: 네 State와 초기 상태 생성 코드
- `Assets/_Project/Scenes/Bootstrap/Bootstrap.unity`
- `Assets/_Project/Tests/EditMode/App/Core/`
- `Assets/_Project/Tests/PlayMode/Integration/Bootstrap/`

`Gameplay/`, `Shared/`, A의 Prefab은 수정하지 않는다.

## 4. 권장 구현 방향

1. Unity Editor에서 B 소유 폴더와 App용 asmdef, Edit/Play Mode 테스트 asmdef를 만든다. 런타임 asmdef가 `UnityEditor`를 참조하지 않게 한다.
2. 네 State를 일반 C# 객체로 작성한다. 생성자 또는 팩터리 한 곳에서 골드, 전력, 적재량, 진행도, 현재 Run의 안전한 기본값을 정한다.
3. `GameState`가 하위 State를 소유하게 하고, UI가 필드를 직접 쓰지 못하도록 읽기 전용 노출과 의도가 드러나는 변경 메서드를 사용한다.
4. `SceneLoader`에 Bootstrap, MainMenu, SurfaceBase, Integration Scene의 논리 이름을 모은다. 문자열을 여러 UI에 흩뿌리지 않는다.
5. `GameBootstrapper`의 순서를 설정 로드 → 카탈로그 로드/검증 → 세이브 슬롯 확인 → State 신규 생성 또는 복원 → MainMenu 로 고정한다. 아직 없는 카탈로그/세이브 구현은 작은 포트와 테스트 대역으로 경계를 검증한다.
6. 전역 Root 하나에만 `DontDestroyOnLoad`를 적용하고, 동일 Root가 다시 만들어지면 새 인스턴스를 즉시 제거한다. 자식 서비스마다 따로 영속화하지 않는다.
7. 실패 결과를 성공/실패와 원인으로 표현한다. 데이터 ID 오류 등 복구 불가능한 초기화 실패 시 Scene 전환을 중단하고 오류를 한 번만 기록한다.
8. Unity Editor에서 `Bootstrap.unity`에 Root와 bootstrapper를 배치하고 직렬화 참조를 연결한다. Build Settings/Profile의 첫 Scene으로 Bootstrap을 등록한다.
9. Domain Reload 설정 차이에서도 static singleton이 오염되지 않도록 SubsystemRegistration 또는 명시적 초기화 지점을 둔다.

초기화 계약은 후속 단계 B와 K가 실제 구현체를 연결해도 호출 순서가 바뀌지 않는 형태여야 한다.

## 5. 보안 및 안정성 기준

- 로그에 세이브 JSON 원문, 로컬 사용자 경로, 비밀값을 출력하지 않는다.
- Scene 이름, 서비스 참조 누락은 조용히 무시하지 않고 명확한 오류로 처리한다.
- Unity Object를 State에 저장하지 않는다.
- 프로젝트 엔진/패키지 버전을 변경하지 않는다.

## 6. 완료 기준

- 새 게임 기본 State를 만들고 Main Menu로 이동한다.
- Bootstrap을 중복 로드해도 전역 Root가 하나다.
- Scene 전환 후 동일 State 인스턴스와 값이 유지된다.
- 데이터 검증 실패를 주입하면 전환이 중단되고 원인이 기록된다.
- Edit Mode와 Play Mode 테스트가 통과하고 Console에 예외가 없다.
