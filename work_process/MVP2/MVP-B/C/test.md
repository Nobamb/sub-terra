# Phase C Agent Test

## 1. 정적 검증 결과

| ID | 검증 항목 | 결과 | 근거 |
| :-- | :-- | :-- | :-- |
| C-S01 | 상호작용·수직 입력 | 통과 | `Player/Interact`의 E binding과 `Player/Move`의 up/down binding 확인 |
| C-S02 | 엘리베이터 상태 머신 | 통과 | `Idle/Calling/Moving/Arrived/Blocked` 및 단일 호출·출발 경로 테스트 |
| C-S03 | Runtime Prefab | 통과 | Elevator/Ladder Prefab의 Collider2D와 필수 컴포넌트 확인 |
| C-S04 | 저장 경계 | 통과 | 체크포인트 DTO에 Unity Object 참조가 없고 사다리 스냅샷 좌표 복원 확인 |

## 2. 기능 검증 결과

| ID | 시나리오 | 결과 | 자동 검증 |
| :-- | :-- | :-- | :-- |
| C-F01 | Surface Base → Mine → Surface Base 왕복 | 통과 | 실제 Build Settings Scene 왕복 및 Mine 정거장·브리지 재탐색 |
| C-F02 | 6칸 이상 사다리 이동 | 통과 | 7m 훈련용 사다리 배치, 중력 0 수직 이동, 이탈 시 중력 복원 |
| C-F03 | 이동 중 중복 입력 | 통과 | 두 번째 요청 거부, 전력·Scene 요청 1회, Rigidbody 잠금·복원 |
| C-F04 | 막힌 출구 | 통과 | 출구 OverlapBox가 막히면 `Blocked`, 플레이어 이동·물리 불변 |
| C-F05 | 수직 구조물 저장·복원 | 통과 | `building.ladder.basic`을 동일 셀에 복원하고 `LadderZone` 유지 |

## 3. 실행 결과

- Unity Edit Mode 전체: **254 통과 / 0 실패 / 0 건너뜀**
- Phase C Play Mode: **12 통과 / 0 실패 / 0 건너뜀**
- Unity 컴파일: **오류 0**
- Inspector 직렬화 감사: Ladder, Elevator, Input, Scene, 비용 참조 모두 통과
- 최종 Unity Console: **Error 0**

전체 App Play Mode 회귀는 **24 통과 / 1 실패**다. 실패 항목은 기존
`SaveContinuePlayModeTests.K_F07_BootstrapLoadsInteractiveSaveMenu`의
`SaveSlotPanelBinder` 탐색 실패이며, Phase C 변경 파일과 직접 관련 없는 기존 메뉴 Scene 항목으로 분리 기록한다.

## 4. 정책과 제한

- 지상 → Mine 호출 비용은 전력 5이며 호출 예약 시 한 번만 차감한다.
- Scene 로드 실패 시 차감 전력을 환불한다.
- Mine → 지상 비상 귀환은 전력 0으로 허용한다.
- 목적지 Scene의 `Start()`가 끝난 다음 프레임에 저장해 seed·위치 초기화와 저장이 겹치지 않게 한다.
- 지상 정거장은 기존 Surface Base 메뉴 버튼을 사용하고, Mine 정거장은 월드 탑승 구역과 E 상호작용을 사용한다.
- 엘리베이터는 현재 상태 표시와 플레이어 잠금 후 Scene 전환을 수행하며, 별도 객실 이동 애니메이션은 MVP 범위에 포함하지 않았다.
