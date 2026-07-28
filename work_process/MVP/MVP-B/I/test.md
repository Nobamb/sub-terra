# Phase I Agent Test

## 1. 정적 검증 항목

| ID | 테스트 항목 | 검증 방법 | 예상 결과 |
| :-- | :-- | :-- | :-- |
| I-S01 | 후보 완전성 | 행동 enum/목록 확인 | process-B의 7개 후보 존재 |
| I-S02 | 결정론 | 코드 검토 | 난수·현재 시간·불안정 열거 순서가 추천에 영향 없음 |
| I-S03 | 책임 분리 | UI/Generator 확인 | 점수 재계산 없이 분석 결과 소비 |
| I-S04 | 우선순위 | 규칙/테스트 확인 | 생존→붕괴→가스→전력→귀환→희귀→일반 |
| I-S05 | 오프라인 독립 | 의존성 확인 | 네트워크 없이 템플릿 생성 가능 |

## 2. 기능 테스트 항목

### I-F01: 가스 위험 우선

- **준비:** 가스 위험, 낮은 전력, 인근 리튬이 동시에 있는 Context를 만든다.
- **실행:** 분석과 대사 생성을 실행한다.
- **예상 결과:** `LeaveGasZone`과 실제 가스 수치 기반 근거/대사가 최우선이다.

### I-F02: 귀환 점수 합산

- **준비:** 낮은 전력과 높은 미정산 가치를 함께 설정한다.
- **실행:** 후보 점수를 조회한다.
- **예상 결과:** 귀환 후보에 두 규칙 점수가 합산되고 근거가 둘 다 존재한다.

### I-F03: 동점 결정론

- **준비:** 두 행동이 같은 점수가 되도록 Context를 만든다.
- **실행:** 새 Service 인스턴스로 분석을 여러 번 반복한다.
- **예상 결과:** 고정 우선순위에 따라 매번 같은 행동이 선택된다.

### I-F04: 대사 쿨다운

- **준비:** 제어 가능한 Clock과 같은 Context를 사용한다.
- **실행:** 쿨다운 전/중/후에 대사를 요청한다.
- **예상 결과:** 중간 반복은 억제되고 만료 후 다시 표시된다.

### I-F05: 불완전 Context

- **준비:** 선택 필드가 unknown인 Context를 만든다.
- **실행:** 분석한다.
- **예상 결과:** 예외나 허위 수치 없이 안전한 추천/근거가 나온다.

## 3. 테스트 절차

1. Edit Mode에서 모든 점수 규칙, 경계값, 동점, 우선순위와 템플릿 토큰을 테이블 테스트로 실행한다.
2. 같은 Context를 100회 분석해 결과 동등성을 확인한다.
3. Play Mode에서 A Context Provider 대역과 두 드론 UI를 연결한다.
4. 실제 A Provider가 준비되면 표시 수치와 Context 원본을 비교한다.
5. 인터넷을 끈 상태에서도 전체 드론 UI 흐름을 확인한다.

## 4. 검증 결과 요약

- **모든 항목 통과 시:** Phase I 완료. Phase J는 이 결과의 문장 표현만 선택적으로 바꿀 수 있다.
- **실패 항목 존재 시:** Context 생산, 규칙 계산, 템플릿, UI 표현을 분리해 수정하고 추천 자체를 UI에서 보정하지 않는다.

### 2026-07-28 실행 결과

- **환경:** Unity `6000.5.4f1`, Unity MCP 연결 Editor
- **Edit Mode:** 통과 — `167 passed / 0 failed / 0 skipped`
- **Play Mode:** 통과 — `12 passed / 0 failed / 0 skipped`; Shared Provider 대역과 실제 `DroneDialoguePanelView`·`DroneReasonPanelView` 표시 흐름 포함
- **I-S01~I-S05:** 통과 — 7개 후보, 고정 동점 순서, UI 책임 분리, 명시적 위험 대사 우선순위, 네트워크 비의존성을 코드와 테스트로 확인
- **I-F01:** 통과 — 가스 `0.7`, 낮은 전력, 리튬 동시 Context에서 `LeaveGasZone`과 실제 수치 근거 확인
- **I-F02:** 통과 — 낮은 전력 `+40`과 고가 화물 `+20`이 귀환 점수 `60` 및 두 근거로 합산
- **I-F03:** 통과 — 새 Service에서 같은 Context를 100회 분석해 동일 행동·점수 확인
- **I-F04:** 통과 — 일반 대사 10초, 긴급 대사 3초 재표시 정책을 제어 Clock으로 검증
- **I-F05:** 통과 — null/unknown Context에서 허위 수치 없이 안전 보류 fallback 확인
- **A Runtime 연동:** `Gameplay_Drone_Test.unity`의 실제 `DroneSensor` → B `DroneContextProviderAdapter` → Shared DTO → `DroneAnalysisService` 연결 확인
- **에셋:** 카탈로그 유효, 대사 템플릿 8개 등록, `DroneDialoguePanel`, `DroneReasonPanel`, `DroneAnalysisUI` Prefab 필수 참조 유효
- **Console:** 컴파일 및 테스트 오류 0. 기존 HUD의 `LiberationSans SDF` 한글 fallback 부재 경고는 Phase I 변경 외 기존 프로젝트 공통 폰트 설정 이슈로 확인
