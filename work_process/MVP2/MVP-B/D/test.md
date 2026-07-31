# Phase D Agent Test

## 1. 정적 검증 항목

| ID | 테스트 항목 | 검증 방법 | 예상 결과 |
| :-- | :-- | :-- | :-- |
| D-S01 | bounds 참조 | Scene/Prefab 필드 검사 | Mine/Surface 경계 연결 |
| D-S02 | viewport 계산 | 순수 계산 테스트 | aspect/size 반영 |
| D-S03 | 전체 Tilemap 폴링 | 소스 검사 | 매 프레임 탐색 없음 |
| D-S04 | 순간이동 API | Follow API 검사 | velocity reset 경로 존재 |

## 2. 기능 테스트 항목

### D-F01: 네 방향 경계

- **준비:** Player를 맵 좌/우/상/하 끝에 배치
- **실행:** 각 방향으로 이동한다.
- **예상 결과:** viewport가 허용된 월드 영역을 벗어나지 않는다.

### D-F02: 40m 수직 추적

- **준비:** 엘리베이터와 사다리 경로
- **실행:** 상단에서 하단까지 이동한다.
- **예상 결과:** 부드럽게 추적하고 Player가 화면에 유지된다.

### D-F03: 해상도 변경

- **준비:** 16:9, 16:10, 4:3 Game View
- **실행:** 동일 경계 위치를 캡처한다.
- **예상 결과:** 모든 비율에서 검은 빈 공간이 과도하게 보이지 않는다.

### D-F04: 로드 순간이동

- **준비:** 심층 체크포인트 Save
- **실행:** 로드해 Player를 복원한다.
- **예상 결과:** 카메라가 복원 위치로 즉시 정렬되고 잔류 velocity가 없다.

## 3. 테스트 절차

1. Edit Mode에서 clamp 수학을 검증한다.
2. Play Mode에서 실제 Camera pixel viewport를 월드 좌표로 비교한다.
3. 저프레임 조건에서도 떨림과 overshoot를 확인한다.

## 4. 검증 결과 요약

- **상태:** 통과 (2026-07-31)
- **Edit Mode:** `PhaseDCameraTests` 5/5 통과
  - Mine/Surface Scene별 경계와 Follow 참조 확인
  - 16:9, 16:10, 4:3 viewport clamp 및 작은 월드 중앙 고정 확인
- **Play Mode:** `PlayerCameraFollowPlayModeTests` 4/4 통과
  - 네 방향 경계, 40m 연속 하강, 세 종횡비 viewport 모서리, 순간이동 스냅 확인
- **Scene 연결:** Mine `81 x 47`, Surface Base `36 x 20` 경계를 Unity Editor 빌더로 저장
- **컴파일:** Player Runtime, App Editor, EditMode/PlayMode 테스트 어셈블리 오류 0

