# Prompt-B 43 업그레이드 적용 검증 결과

검증일: 2026-08-10

## 결론

업그레이드 데이터와 효과 계산 자체는 정상이나, 일부 효과의 실제 런타임 반영과 심층 구역 연동은 완료되지 않았다.

실제 프로젝트 카탈로그를 사용해 최고 레벨 효과를 계산한 결과는 다음과 같았다.

- 드릴 속도: `1.0 → 1.3배`
- 드릴 전력 효율: `1.0 → 1.15배`
- 최대 전력: `100 → 160`
- 최대 화물: `50 → 80`
- 드론 스캔: 기준 반경 `4 → 8`
- 드론 구조 보존: `0 → 0.3`
- 가스 저항: `0 → 0.3`

드릴 속도·전력 효율, 드론 스캔, 드론 구조 보존, 가스 저항은 통합 Scene에서 각 소비자에 효과 Provider가 연결되어 있었다. 아래에는 수정이 필요한 항목만 기록한다.

## 1. 최대 전력 업그레이드가 실제 GameState에 반영되지 않음

### 현상

`upgrade.energy.maximum` 레벨 3의 계산 결과는 160이지만, 게임의 `PlayerState.MaxEnergy`는 기본값 100에서 갱신되지 않을 수 있다. 사용자가 확인한 `Lv.0 → Lv.3` 후에도 최대 전력이 100인 현상과 일치한다.

### 원인

`ProgressionDerivedStateSynchronizer`에는 구매 직후 최대 전력을 갱신하는 구현이 있고 단위 테스트도 통과한다. 그러나 실제 런타임 코드에서는 이 클래스를 생성하거나 `ProgressionService`에 바인딩하는 경로가 없다. 즉, 계산 코드는 존재하지만 생산 조립에서 사용되지 않는다.

### 수정 방향

- `SaveRuntimeController`가 게임 서비스 재구성 시 `ProgressionDerivedStateSynchronizer`를 생성하고 `ProgressionService`에 바인딩한다.
- 서비스 재구성·새 게임·이어하기 시 기존 Synchronizer를 먼저 해제한 뒤 새 `GameState`, `InventoryService`, `ProgressionService`에 다시 연결한다.
- 복원 직후와 업그레이드 구매 직후 모두 `PlayerState.MaxEnergy`가 Provider 결과로 갱신되는 통합 테스트를 추가한다.
- 실제 카탈로그 기준 `Lv.0=100`, `Lv.1=120`, `Lv.2=140`, `Lv.3=160`을 검증한다.

## 2. 최대 화물 업그레이드도 동일한 런타임 배선 누락

### 현상

`upgrade.cargo.maximum`의 계산 결과는 정상이나, 실제 `InventoryService.MaxCapacity` 갱신도 최대 전력과 같은 Synchronizer에만 구현되어 있다.

### 원인

최대 전력과 동일하게 `ProgressionDerivedStateSynchronizer`가 생산 런타임에서 생성·바인딩되지 않는다.

### 수정 방향

- 최대 전력 수정과 같은 조립 지점에서 화물 용량도 함께 갱신한다.
- 실제 카탈로그 기준 `Lv.0=50`, `Lv.1=60`, `Lv.2=70`, `Lv.3=80`과 구매 직후 `InventoryChanged` 발생을 통합 테스트로 검증한다.
- 현재 적재량이 새 최대치보다 큰 세이브를 복원하는 정책은 별도 명시한다. 기존 화물을 조용히 삭제해서는 안 된다.

## 3. 심층 구역은 해금 상태만 구현되어 있고 성공 팝업이 없음

### 현재 구현된 범위

- 완료 목표 1개 이상
- 드론 스캔 2레벨 이상
- 가스 저항 1레벨 이상
- 조건 충족 시 `zone.deep`을 `UpgradeState`에 기록
- 최초 해금 시 `DeepZoneAccessChanged` 이벤트와 `DidUnlockNow=true` 반환
- 업그레이드 창의 심층 탭 텍스트와 데모 목표 전진

위 상태·이벤트 경로는 테스트에서 정상 동작했다.

### 누락된 범위

- `DidUnlockNow=true`를 받아 표시하는 전용 팝업 또는 공용 알림 UI가 없다.
- 현재 UI는 심층 탭 상세 문구를 `심층 구역: 잠금 해제`로 바꾸는 수준이다.
- `zone.deep`을 조회해 실제 심층 진입, 경계, 잠긴 신호 상호작용을 허용/차단하는 Gameplay 소비자가 없다. 따라서 현재 해금은 진행 상태와 튜토리얼 묘사에 가깝고, 월드 접근 권한으로는 완결되지 않았다.

### 수정 방향

- UI Binder가 `DeepZoneAccessChanged`를 구독하고 `IsUnlocked && DidUnlockNow`일 때만 `심층 구역 잠금이 해제되었습니다` 팝업을 한 번 표시한다.
- 이미 해금된 세이브 로드나 단순 UI 재바인딩에서는 팝업을 다시 띄우지 않는다.
- Gameplay 쪽에 Shared 읽기 전용 접근 계약을 두고, 심층 경계 또는 잠긴 신호 상호작용이 `zone.deep` 상태를 실제로 소비하게 한다.
- 미충족 상태 차단, 최초 충족 팝업 1회, 저장 후 재접속 시 무팝업, 해금 후 실제 접근 허용을 PlayMode 테스트로 검증한다.

## 테스트 결과

- EditMode: 415 통과 / 1 실패 / 0 스킵
  - 유일한 실패는 업그레이드와 무관한 `PromptB42UiLayerTests.MainMenuSettingsAndSelectedSlot_UseTheRequiredVisualPriority`였다.
  - 업그레이드 구매·7종 효과 계산·최대 전력/화물 Synchronizer 단위 테스트·심층 해금 상태 테스트는 통과했다.
- PlayMode: 54 통과 / 0 실패 / 0 스킵
- 통합 Scene 참조 검사: `MiningSystem`, `DroneSensor`, `GasExposureEffectController`, `RunFailureRuntimeController`, `ProgressionPanelBinder` 모두 연결됨.

기존 테스트는 `ProgressionDerivedStateSynchronizer`를 직접 생성해 검증하므로, 생산 런타임에서 해당 객체가 생성되지 않는 조립 누락을 탐지하지 못한다. 이 때문에 단위 테스트 통과만으로 최대 전력·화물의 실제 적용을 보장할 수 없다.
