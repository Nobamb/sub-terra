# Phase K — 드론 World Space 대사와 상황 알림

## 1. 개요

규칙 기반 분석과 Canvas 패널은 존재하지만 드론 머리 위 팝업과 실제 위험 이벤트 연결이 부족하다. 가스·붕괴·인벤토리 가득 참·전력 부족에서 즉시 이해 가능한 안내가 필요하다.

## 2. 작업 목표

- 드론 `ViewSocket` 위에 World Space 대사 팝업을 표시한다.
- 화면 패널에는 추천 행동과 실제 근거 수치를 유지한다.
- 긴급 위험은 일반 탐사 대사보다 우선하고 쿨다운을 우회/갱신한다.
- 생성형 AI가 없어도 템플릿으로 전체 데모가 동작한다.

## 3. 구현 범위

- `DroneDialogueSocket`과 World Space Canvas/말풍선 Prefab
- 가스 진입, 붕괴 임박/발생, 인벤토리 가득 참, 전력 부족, 희귀 광물, 귀환 추천 trigger
- Context snapshot→analysis→template→world/overlay view 단일 흐름
- 우선순위, 쿨다운, 반복 억제와 가시 시간
- 화면 밖/벽 뒤/카메라 경계에서 말풍선 위치 보정

## 4. 권장 구현 방향

1. 기존 `DroneAnalysisService`, `TemplateDialogueGenerator`, `DroneUiBinder`를 재사용한다.
2. World 팝업과 상세 패널은 같은 `DroneDialogueResult`를 표시한다.
3. 긴급 이벤트가 와도 새 수치나 사실을 만들지 않고 최신 Context만 사용한다.
4. 클라우드 대사는 선택 사항이며 timeout/실패 시 즉시 템플릿을 표시한다.
5. 머리 위 시각은 Runtime Prefab 내부 `ViewSocket` 또는 App 소유 View attachment로 분리한다.

## 5. 보안 및 안정성 기준

- API 키나 외부 endpoint 비밀을 Unity 빌드에 포함하지 않는다.
- 같은 Context와 시간 상태에서 추천이 결정론적이다.
- 매 프레임 분석/네트워크 호출을 하지 않는다.
- 팝업이 HUD 입력을 가리거나 raycast를 가로채지 않는다.

## 6. 완료 기준

- 지정된 여섯 상황에서 올바른 템플릿 팝업이 표시된다.
- 긴급 위험이 일반 대사를 선점하고 반복 제한이 작동한다.
- 팝업·상세 패널·실제 GameState 수치가 일치한다.
- 오프라인과 클라우드 실패 조건에서도 데모 진행이 막히지 않는다.

