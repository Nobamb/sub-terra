# Phase U 보건소 시설 및 체력 회복

## 0. 작업 전제

- `init/prompt-B.md` 86번과 `init/rule.md`를 기준으로 작업한다.
- 보건소는 기존 충전소 시설 흐름을 그대로 따른다: `BuildingData` → Catalog → Placement → Runtime Prefab → BuildingMenu → Integration.
- App은 Gameplay 구현 타입을 직접 참조하지 않고 Shared의 체력 회복 명령 계약만 사용한다.
- 수정 가능한 UI/Scene 범위는 `BuildingMenu.prefab`과 `Mine_Demo_Integration.unity`로 제한한다.

## 1. 구현 범위

| 항목 | 결정 |
| :-- | :-- |
| 영구 ID | `building.clinic.basic` |
| 표시명 | `보건소` |
| 건설 비용 | 구리 3 |
| 전력 수요 | 3 |
| 크기 | 1×1 |
| 외형 | 흰색 정사각형 본체와 빨간 십자 |
| 메뉴 위치 | 충전소 바로 아래, 저장고 바로 위 |
| 사용 조건 | 엘리베이터 또는 전진기지 코어 반경 10칸의 연결 전력망 |
| 사용 비용/쿨다운 | 없음 |

## 2. 체력 회복 경계

- Shared에 `IPlayerHealthCommand.RestoreFull()`을 추가한다.
- Gameplay의 `PlayerSurvivalController`가 계약을 구현한다.
- App의 `OutpostService.TryHeal`은 Shared 계약으로만 회복을 요청한다.
- 체력이 감소한 상태에서는 최대 체력으로 복구하고 `체력 회복이 완료되었습니다.`를 반환한다.
- 이미 최대 체력이어도 성공으로 처리하고 `이미 체력이 최대입니다.`를 반환한다.
- 전력망이 끊긴 경우 회복 명령을 호출하지 않고 지정된 3초 안내 문구를 반환한다.

## 3. 퀘스트 변경

- 기존 충전소 퀘스트와 심층 구역 해금 퀘스트 사이에 `demo.quest.heal_near_outpost`를 삽입한다.
- 보건소가 코어 10칸 이내에 설치되어 있고 회복 사용이 성공해야 다음 퀘스트로 이동한다.
- 최대 체력에서의 사용 성공도 퀘스트 성공으로 인정한다.
- 데모 퀘스트 수는 18개, 심층 구역 해금 요구 완료 수는 13개다.

## 4. 저장 마이그레이션

Save 버전을 3으로 올리고 v2 저장을 다음처럼 변환한다.

| 기존 저장 상태 | 변환 결과 |
| :-- | :-- |
| 충전소 이전 또는 충전소 진행 중 | 현재 목표와 완료 수 유지 |
| 충전소 완료 또는 심층 해금 이후 | 보건소 완료로 간주해 완료 수 +1, 현재 목표 유지 |
| 기존 데모 완료(17개) | 새 데모 완료(18개) |

마이그레이션 후 현재 버전으로 다시 저장하고 로드해도 완료 수가 다시 증가하지 않아야 한다.

## 5. 에디터 자산 연결

- 전용 Editor builder가 보건소 Runtime Prefab, BuildingData, PlacementDefinition을 생성한다.
- Catalog에는 충전소 바로 다음 순서로 등록한다.
- BuildingMenu에는 보건소 버튼만 추가하고 기존 시설 순서를 유지한다.
- Integration scene의 배치 정의와 Gameplay 배치 이벤트 브리지에 보건소를 등록한다.
- 에디터가 자동 변경한 Font와 ProjectSettings 파일은 작업 범위에서 제외하고 복원한다.

## 6. 완료 기준

- 실제 Catalog/Prefab/Menu/Integration 자산이 보건소 ID로 연결되어 있다.
- 전력 연결, 회복 성공, 최대 체력 성공, 퀘스트 진행, 저장 마이그레이션 테스트가 통과한다.
- Unity 컴파일 오류가 없다.
- 허용 범위 밖 UI/Scene/Font/ProjectSettings 변경이 남지 않는다.
