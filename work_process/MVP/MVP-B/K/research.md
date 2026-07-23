# Phase K — 로컬 세이브 시스템

## 1. 개요

`process-B`의 B-10을 구현한다. B의 플레이어·진행·드론 State와 A의 월드 스냅샷을 버전 있는 JSON으로 원자적으로 저장하고, 정상 파일 손상 시 백업으로 복구한다.

## 2. 작업 목표

- `GameSaveData`, 하위 Save DTO와 `saveVersion`을 정의한다.
- tmp 기록 → JSON 검증 → 기존 정상 파일 backup → tmp를 정상 파일로 교체하는 순서를 지킨다.
- 정상 파일 실패 시 backup을 시도하고, 둘 다 실패하면 사용자에게 복구 선택지를 제공한다.
- 자동 저장, 슬롯, 이어하기와 이전 버전 마이그레이션을 지원한다.

## 3. 구현 범위

- `Scripts/App/Save/`: DTO, 매퍼, Save/LoadService, migration, 슬롯 메타데이터
- `Editor/SaveTools/`: 개발용 슬롯 검사/삭제 도구
- `Prefabs/UI/SaveSlotPanel.prefab`
- `Tests/EditMode/App/Save/`, `Tests/PlayMode/Integration/Save/`
- A의 `IWorldSnapshotProvider`/`WorldSaveData`는 사용만 하며 임의 변경하지 않는다.

## 4. 권장 구현 방향

1. 저장 요구사항 표를 만든다: 골드, 전력/플레이어 상태, 인벤토리/보관함, 업그레이드/진행도, 드론 쿨다운에 필요한 상태, 체크포인트, A 월드 변경점.
2. DTO는 `[Serializable]` 일반 데이터로 만들고 GameObject, Sprite, TileBase, Prefab, MonoBehaviour, Collider를 넣지 않는다.
3. 런타임 State↔DTO 변환을 별도 매퍼로 둔다. `JsonUtility`가 지원하지 않는 Dictionary는 키/값 entry 리스트로 명시 변환하고 로드 시 중복 키를 검증한다.
4. 슬롯 파일명을 allowlist/정수 슬롯 ID로 생성해 `Application.persistentDataPath` 밖의 경로를 만들 수 없게 한다.
5. 저장 시 모든 State와 `CaptureWorldSnapshot()`을 먼저 수집해 완전한 DTO를 만든다. JSON을 tmp에 쓰고 다시 읽어 버전/필수 필드를 검증한다.
6. 검증 성공 후 기존 정상 파일을 backup으로 교체하고 tmp를 정상 파일로 이동한다. 각 파일 작업 실패를 결과로 반환하며 성공으로 가장하지 않는다.
7. 로드 시 정상 파일 읽기/파싱/검증/마이그레이션을 수행한다. 실패하면 backup으로 같은 과정을 반복한다.
8. 복원 순서는 B State 복원 → 목표 Scene 로드 → A `RestoreWorldSnapshot()` → 파생 구조/전력 재계산 완료 신호 → UI 활성화로 둔다.
9. Migration은 버전별 한 단계 함수로 구성하고 원본 파일을 먼저 보존한다. 알 수 없는 미래 버전은 덮어쓰지 말고 로드를 거부한다.
10. 자동 저장은 지상 귀환, 정산, 업그레이드, 전진기지, 새 구역, 구조 실패, 종료, dirty 상태의 주기 저장 이벤트를 한 큐에서 직렬 처리한다.
11. 저장 중 추가 요청은 dirty 플래그로 합치고 동시 파일 쓰기를 막는다. 종료 시 무한 대기하지 않도록 제한된 완료 정책을 둔다.
12. 실제 사용자 세이브가 아닌 임시 테스트 디렉터리에 파일 시스템 테스트를 수행한다.

## 5. 보안 및 안정성 기준

- 슬롯 입력으로 경로 순회가 불가능해야 한다.
- 세이브 JSON 원문과 전체 로컬 경로를 로그로 출력하지 않는다.
- 손상 파일을 정상 파일 위에 덮어쓰지 않고 backup 복구 전 원본을 보존한다.
- 누락 필드는 안전한 기본값, 중복 ID·음수 수량·범위 밖 레벨은 검증/정규화 정책으로 처리한다.
- 테스트가 실제 사용자 persistentDataPath 세이브를 삭제하거나 바꾸지 않게 한다.

## 6. 완료 기준

- 모든 B State와 A 월드 스냅샷이 저장·재실행 후 복원된다.
- 정상 파일 손상 시 backup으로 복구된다.
- `saveVersion`과 마이그레이션 테스트가 존재한다.
- 자동 저장 요청이 직렬화되고 tmp/backup 찌꺼기와 실패 결과를 안전하게 처리한다.
