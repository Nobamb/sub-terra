# Phase N Implementation Summary

## 구현 내용

### 데모 13단계 목표
- `Scripts/App/Tutorial/DemoObjectiveIds.cs` — 영구 ID 13개
- `DemoObjectiveCatalog.cs` — 시작/완료 신호·문구·다음 목표 표
- `DemoObjectiveTransitionEngine.cs` — 순수 전이 로직(허용/금지 행렬)
- `DemoObjectiveDirector.cs` — Inventory/Gameplay/Outpost/Progression 성공 이벤트만 구독
- unknown ID → 완료 개수 기준 안전 폴백

### 진행 State·Save
- `ProgressState.CurrentObjectiveId`, `IsDemoComplete` 추가
- `ProgressSaveData` 직렬화·mapper·Normalize 폴백
- 세이브 버전 유지(필드 기본값으로 구세이브 호환)

### UI
- `UI/Tutorial/DemoObjectivePresenter` — 읽기 전용 목표, dismiss 안내, 위험 시 양보
- `DemoObjectiveView` + Integration Scene 연결
- `TutorialDirectorBinder` — IntegrationRuntimeBinder fan-out 구독
- `UiLayerPriority` / `HazardHudView` sort — 긴급 UI > 튜토리얼
- `DemoObjectiveDebugTools` — `#if DEVELOPMENT_BUILD || UNITY_EDITOR` only

### 소유권
- App/B 전용. Gameplay Runtime Prefab·Shared 계약 미변경.
- 튜토리얼은 자원/위험 State를 직접 조작하지 않음.

## 심층 잠금 정합 (skeptic fix)
- `ProgressionPanelPresenter` 구매 성공 후 `TryUnlockDeepZone(completedObjectives)` 호출
- `SurfaceBasePresenter` 새로고침 시 `GetDeepZoneAccess` → `TryUnlockDeepZone`
- `TutorialDirectorBinder.EvaluateDeepZoneProgress` — 조건 충족 알림 + Service 커밋
- 업그레이드 목표 문구/조건을 `DeepZoneUnlockRule.Mvp`(드론 스캔 2·가스 저항 1)에 맞춤
- 테스트 `DemoDeepZoneUnlockPathTests` — 가짜 `ZoneAccessResult` 없이 Service 실경로

## 검증
- Editor 게이트 `SubTerra/Tests/Verify Phase N Gates` — pass=40 fail=0 (`real-service-unlock-path` 포함)
- 증거: `Temp/phase-n-editmode-results.txt`, `Temp/phase-n-playmode-results.txt`
- Edit Mode 단위 테스트: `Tests/EditMode/App/Tutorial/*`
- Play Mode: `Tests/PlayMode/Integration/DemoFlow/*`
- 오디오: 미배선(라이선스 확인 에셋 없음) — N-S05 스킵 허용
- 첫 사용자 관찰 플레이테스트: 에이전트 환경 미실행

## Unity 메뉴
- `SubTerra/UI/Build Phase N Tutorial UI`
- `SubTerra/Tests/Verify Phase N Gates`
- `SubTerra/Tests/Run Phase N Edit Mode` / `Play Mode`
