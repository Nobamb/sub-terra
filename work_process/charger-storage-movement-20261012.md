# 충전기·보관함 이동 확대

## 작업 범위

- 첫 번째 프로젝트, `codex/facility-move-runtime-followup`, 기준 152c990f.
- B → 시설 이동에서 기존 조명·정산 콘솔에 충전기·보관함을 추가한다.
- 충전기는 현재 규격 2×2, 보관함은 기존 규격 1×1을 사용한다. 미리보기·점유·바닥·겹침 검사와 안내는 선택한 시설 정의의 실제 크기를 따른다.
- 기존 시설 오브젝트와 인스턴스 ID를 보존한다. 재생성·철거·비용 차감·건설 퀘스트 진행·보상 재지급을 하지 않는다. 취소와 거부 시 원래 상태를 유지한다.
- 기존 이동 이벤트로 지반 보호·전력망·미니맵·저장 좌표와 점유 크기를 갱신한다. 보호되는 엘리베이터 공간도 기존 배치 검사에 따른다.

## 상태 보존 근거

- `OutpostState.Storage`는 공용 광물 보관 상태이며 건물 그림이나 좌표에 저장하지 않는다. 이동 코드에서 이 상태와 보관 수량을 수정하지 않는다.
- 충전 재사용 대기는 `FacilityCooldownState.InstanceId`를 키로 보존한다. 같은 BuildingInstance와 PowerNode, EntityId를 유지하고 OutpostService의 충전/초기화 메서드를 호출하지 않는다. 시간이 정상적으로 흐르는 동안의 대기시간 감소는 유지한다.
- 근접 시설 판정은 PowerNode의 현재 transform.position을 사용한다. 이전 좌표를 새 영구 상태로 복제하지 않는다.
- 시설 선택·실패 안내의 1×2 고정 문구는 선택 시설의 실제 폭×높이로 바꿨다.

## 변경 파일

- BuildingPlacementSystem.cs: 이동 허용 종류 2개 추가. 기존 검증/확정 경로 재사용.
- GameplayBuildingPlacementBridge.cs: 대상 안내 및 실제 설치 크기 안내.
- BuildingPlacementTests.cs: 충전기 네 칸과 보관함 한 칸의 겹침 거부·취소·기존 ID/위치/비용 보존 검사 5개 추가.
- WorldSnapshotSystemTests.cs: 실제 충전기·보관함 프리팹의 같은 오브젝트/PowerNode 보존 및 이동→JSON 저장→복원 검사 2개 추가. 레벨·체력·회전과 점유도 확인하도록 기존 검사 도우미 재사용.

## 최소 검증 결과와 제한

- 시작 시 실제 사용 가능한 RAM 약 0.45GB, 디스크 약 11.61GB. Unity와 전체 씬·Windows 빌드를 실행하지 않았다.
- Unity 번들 .NET SDK 8.0.318의 C# 컴파일러로 기존 생성 csproj의 소스·정의·참조 및 캐시된 의존 DLL을 사용해 검증했다.
- SubTerra.Gameplay.Building / SubTerra.App.GameplayIntegration / SubTerra.Gameplay.Building.EditModeTests / SubTerra.Gameplay.Snapshot.EditModeTests: 4개 어셈블리 모두 컴파일 종료 코드 0, 오류 없음.
- 이는 경량 C# 컴파일 검증이며 Unity Editor 전체 임포트/소스 생성/런타임 검증과 같다고 주장하지 않는다. 새 테스트 7개는 컴파일됐지만 실제 실행하지 않았다.
- 필터: `MoveChargerOrStorage_BlockedCellAndCancelPreserveOriginal;MovedChargerAndStorage_PreserveIdentityAndSaveFootprint`. 메모리 여유가 있는 데스크탑에서 이 검사만 실행하면 된다.
- diff 검사 통과. ProjectSettings/Packages/Scene/Prefab/사용자 저장/폰트 stash 변경 없음. 기존 데이터 에셋 3개의 의미 없는 줄바꿈 상태는 보존했다.
- 임시 컴파일 DLL/응답 파일은 검사 후 제거했다. 사용자 승인으로 이 변경을 기존 PR #168에 추가 게시한다. ComputeBuffer 패키지 적용과 채굴 성능 측정은 이번 범위에 포함하지 않는다.

## 다음 작업 순서

1. 메모리 여유가 있는 데스크탑에서 위 필터의 새 검사 7개만 실행한다. 이전 전체 검사·Windows 빌드는 반복하지 않는다.
2. 시설 이동을 더 확장한다면 보건소부터 추가한다. 같은 오브젝트/ID와 회복 재사용 대기를 유지하며, 코어·엘리베이터·사다리·버팀목은 이번 이동 목록에 넣지 않는다.
3. ComputeBuffer는 패키지 등록 정보 변경 허용 여부가 확정된 뒤 준비된 소유 코드 패치를 적용하고 최소 종료 재현으로 검증한다.
4. 채굴 끊김은 데스크탑에서 게임 처리와 측정 도구 비용을 짧게 분리 측정하고, 확인된 병목만 수정한다. 현재 노트북에서 무거운 측정을 재시도하지 않는다.
