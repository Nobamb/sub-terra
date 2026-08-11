# Phase Q — 긴급 탈출 포탈

## 0. 작업 전

- `init/prompt-B.md` 46번과 `init/rule.md`, 특히 UI 변경 범위 보호 규칙 2-5를 기준으로 한다.
- 기존 건설·전력망·경제·전진기지·엘리베이터·실패 복구 흐름을 재사용한다.

## 1. 개요

시설 건설창에 통과 가능한 긴급 탈출 포탈을 추가한다. 포탈은 연결 전력 30을 요구하고 철 3개·리튬 3개로 건설한다. 플레이어가 포탈 안에서 E키를 누르면 100G와 최대 전력의 10%를 지불하고, 최근 설치된 전진기지 코어 또는 엘리베이터 중앙으로 이동한다.

## 2. 작업 목표

- 영구 ID `building.escape_portal.emergency`와 데이터/배치 정의/Runtime Prefab을 등록한다.
- 통과 가능한 Trigger 시설과 E 상호작용, 전력 30 활성 조건을 구현한다.
- 사용 전 목적지와 비용을 전량 검증하고 실패 시 골드·전력을 변경하지 않는다.
- 최근 전진기지 코어를 우선하고, 없으면 엘리베이터 중앙으로 결정론적으로 이동한다.
- 새 게임 시작 위치와 체크포인트가 없는 부활 위치를 엘리베이터 중앙으로 통일한다.

## 3. 구현 범위

- Shared: `IEmergencyEscapePortalPort`, 목적지 enum
- Gameplay: `EmergencyEscapePortal`, Trigger Collider, PowerNode 수요 30
- App: `EmergencyEscapeService`, `EmergencyEscapePortalRuntimeBridge`
- Data: BuildingData, BuildingPlacementDefinition, GameDataCatalog 등록
- UI: `BuildingMenu.prefab`에 긴급 탈출 포탈 선택 항목 1개 추가
- Integration: 포탈 브리지 배선, Player/부활 fallback을 `BoardingAnchor`에 정렬
- Save: 기존 WorldSnapshot 건설물 복원 경로와 수동 저장 요청 재사용

## 4. 구현 방향

1. 건설 비용은 기존 구조대로 광물 비용과 시설 전력 수요를 분리한다.
2. Gameplay 포탈은 App 상태를 직접 참조하지 않고 Shared 포트만 호출한다.
3. App Service는 100G와 `ceil(MaxEnergy × 0.1)`을 사전 검증한 후 함께 반영한다.
4. Runtime Bridge는 설치·복원된 전진기지 `BuildingInstance` 중 가장 최신 ID를 우선한다.
5. 전진기지가 없으면 Scene에 명시 연결된 엘리베이터 `BoardingAnchor`를 사용한다.
6. 체크포인트가 없는 실패 복구는 Surface Scene 전환 대신 Integration Scene의 엘리베이터 중앙에서 입력을 복원하고, 전력이 5 미만이면 지상 귀환에 필요한 최소 전력 5를 복구한다.

## 5. 보안 및 안정성 기준

- 설치 생성 성공 전에 광물 비용을 차감하지 않는다.
- 전력 미연결, 목적지 없음, 골드/전력 부족 시 사용 비용을 일부 차감하지 않는다.
- 동일 프레임의 InputAction 콜백과 폴링이 중복 결제를 만들지 않게 요청을 1회로 제한한다.
- `BuildingMenu.prefab`과 `Mine_Demo_Integration.unity` 외 기존 UI Prefab/Scene은 수정하지 않는다.
- 폰트 아틀라스와 ProjectSettings 자동 변경은 범위 밖 변경으로 원복한다.

## 6. 완료 기준

- 시설 건설창에서 긴급 탈출 포탈을 선택·설치할 수 있다.
- 철 3·리튬 3 비용과 전력 수요 30이 카탈로그/배치/Runtime에서 일치한다.
- E 사용 성공 시 100G와 최대 전력 10%가 정확히 한 번 차감된다.
- 최근 전진기지 우선/엘리베이터 폴백과 시작·부활 위치가 검증된다.
- Play Mode 회귀 테스트가 통과하고 Unity Console의 비예상 Error가 0이다.
