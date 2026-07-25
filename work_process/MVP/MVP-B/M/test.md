# Phase M Agent Test

## 1. 정적 검증 항목

| ID | 테스트 항목 | 검증 방법 | 예상 결과 |
| :-- | :-- | :-- | :-- |
| M-S01 | Scene 계층 | Unity Hierarchy 확인 | 기준 Root와 4 Tilemap, HUD, EventSystem 존재 |
| M-S02 | 계약 연결 | Inspector/문서 매핑 확인 | 5개 Shared 경계 Producer/Consumer 연결 |
| M-S03 | 소유권 | Git diff 확인 | A Runtime Prefab/Gameplay 변경 없음 |
| M-S04 | 중복 객체 | Scene/런타임 계층 확인 | 전역 서비스와 EventSystem 각각 의도한 수 |
| M-S05 | 참조 건전성 | Missing Script/Reference 검사 | 누락 없음 |
| M-S06 | 통합 문서 | 가이드 확인 | 설치·참조·이벤트·복원·검증 절차 최신 |

## 2. 기능 테스트 항목

### M-F01: 채굴→인벤토리→HUD

- **준비:** 실제 A Player/Mining Runtime과 B HUD를 연결한다.
- **실행:** 구리 타일 하나를 완전히 채굴한다.
- **예상 결과:** 타일 제거와 보상은 한 번이고 인벤토리·중량·가치·HUD가 일치한다.

### M-F02: 건설 성공/실패

- **준비:** 충분한 자원과 유효/무효 위치를 준비한다.
- **실행:** 무효 위치 실패 후 유효 위치에 버팀목을 설치한다.
- **예상 결과:** 실패에는 차감이 없고 성공에만 Runtime 생성과 비용 차감이 한 번 발생한다.

### M-F03: 위험과 드론

- **준비:** 구조 위험과 가스가 발생하는 실제 데모 구간을 사용한다.
- **실행:** 각 구간에 진입한다.
- **예상 결과:** A 실제 수치와 B 경고·드론 추천/근거가 일치한다.

### M-F04: 저장과 월드 복원

- **준비:** 광물을 채굴하고 시설을 설치한 뒤 저장한다.
- **실행:** 앱 상태를 새로 만들고 이어하기로 진입한다.
- **예상 결과:** State와 채굴 타일·시설·파생 구조/전력이 복원되고 중복 보상이 없다.

### M-F05: Scene 재진입

- **준비:** Integration에서 SurfaceBase로 나갈 수 있게 한다.
- **실행:** Integration→SurfaceBase→Integration을 반복한다.
- **예상 결과:** 서비스, EventSystem, 구독과 Runtime 오브젝트가 중복되지 않는다.

## 3. 테스트 절차

1. A의 각 Test Scene과 인수인계 체크를 먼저 통과시킨다.
2. Shared 경계를 하나씩 연결할 때마다 해당 최소 Play Mode 테스트를 실행한다.
3. 전체 Integration Play Mode 테스트와 수동 핵심 루프를 실행한다.
4. 새 게임/이어하기/Scene 왕복에서 Hierarchy와 Console을 비교한다.
5. Git diff로 Scene 외 A 소유 파일 변경과 `.meta` 누락을 확인한다.

## 4. 검증 결과 요약

- **모든 항목 통과 시:** Phase M 완료. 실제 A Runtime 기반 결과와 Unity Console 상태를 기록한다.
- **실패 항목 존재 시:** A 단독 Scene에서도 재현되면 A 요청 형식으로 Issue를 작성하고, 통합에서만 재현되면 bridge/Inspector 연결을 수정한다.
