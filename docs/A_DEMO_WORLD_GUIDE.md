# A 데모 월드 좌표 가이드

`Gameplay_DemoWorld_Test`은 A 기능을 연결해 보는 전용 테스트 씬이다. 최종 Integration Scene은 수정하지 않는다.

| 구역 | 좌표 | 목적 |
| --- | --- | --- |
| 시작/안전 구역 | x -10 | 플레이어, 드론, 전진기지 시작 위치 |
| 구리 학습 | x -3 | 첫 채굴과 보상 흐름 |
| 철 보상 경로 | x 1 | 버팀목 제작 자원 예시 |
| 구조 균열 | x 5, 천장 y 3 | 채굴 후 구조 위험·부분 붕괴 확인 |
| 버팀목 설치 공간 | x 4~7 | 건설 배치와 구조 안정화 공간 |
| 가스 주머니 | x 8 | 가스 구역 활성·드론 Context 확인 |
| 잠긴 희귀 신호 | x 13 | MVP 이후 콘텐츠 예고, 채굴 불가 |

## B 통합 시 필요한 참조

- `GameplayRoot/DemoSystems`: Mining, Structural, Gas, Building, Power, Snapshot 시스템
- `GameplayRoot/Player`: 이동·채굴 입력 대상
- `GameplayRoot/DiggerBot_Runtime`: `DroneSensor`의 실제 월드 Context 제공자
- `GameplayRoot/OutpostCore_Demo`: 드론의 가장 가까운 기지 거리 측정 기준

## 확인 흐름

1. 시작 구역에서 구리를 채굴한다.
2. 철 보상 경로를 지난다.
3. 리튬과 구조 균열 구역에서 위험 상태를 확인한다.
4. 버팀목을 설치할 공간을 확인한다.
5. 가스 주머니와 잠긴 희귀 신호 구역으로 진행한다.
