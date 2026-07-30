# Phase I — 시설 Runtime과 전력망 완결

## 1. 개요

전력 그래프와 조명·충전기·코어 Prefab은 있으나 BuildingData 다수가 공용 placeholder를 가리킨다. 보관함·정산 콘솔을 포함한 각 시설이 고유 Runtime 기능과 시각을 가져야 한다.

## 2. 작업 목표

- 버팀목, 조명, 충전기, 보관함, 정산 콘솔, 전진기지 코어를 각각 실제 Runtime Prefab으로 만든다.
- 코어와 케이블 연결에 따라 시설 활성 상태를 결정한다.
- 조명·충전·보관·정산이 실제 Player/App 상태를 변경한다.
- 연결 실패 원인과 공급/소비량을 Outpost UI에 표시한다.

## 3. 구현 범위

- 시설별 `BuildingPlacementDefinition`과 Runtime Prefab
- `PowerNode`, `PowerFacility`, 케이블 연결/해제
- Light 2D 또는 교체 가능한 조명 VisualRoot
- 충전기 상호작용, 보관함 입출고, 정산 콘솔
- 코어 공급량, 시설 우선순위, 네트워크 상태 DTO
- 설치/제거/복원 시 네트워크 재계산

## 4. 권장 구현 방향

1. BuildingData는 각 시설의 실제 Runtime Prefab을 가리키게 하고 공용 placeholder를 제거한다.
2. Gameplay는 전력 연결/활성/거리 판정, App는 Energy·Inventory·Gold 변이를 소유한다.
3. 시설 상호작용은 실제 `PowerFacility.IsInteractionAvailable`을 통과해야 한다.
4. 네트워크는 topology 또는 수요 변경 시에만 재계산한다.
5. 정산/보관 요청은 중복 request ID로 멱등 처리한다.

## 5. 보안 및 안정성 기준

- 비활성 시설은 상태를 변경하거나 자원을 차감하지 않는다.
- 전력 부족 시 우선순위와 instance ID로 결정론적으로 시설을 활성화한다.
- 시설 설치 실패 시 비용·전력망·Snapshot이 불변이다.
- 복원 후 `isPowered`를 저장값으로 신뢰하지 않고 네트워크에서 재계산한다.

## 6. 완료 기준

- 여섯 시설이 서로 구분되는 Prefab과 기능을 가진다.
- 코어/케이블 연결 전후 시설 활성 상태가 즉시 바뀐다.
- 조명, 충전, 보관, 정산을 실제 Integration Scene에서 사용할 수 있다.
- 전력 상태와 UI 표시가 동일하고 저장/복원 뒤 재계산된다.

