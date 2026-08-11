# Phase Q Agent Test

## 1. 정적 검증 항목

| ID | 테스트 항목 | 검증 방법 | 예상 결과 |
| :-- | :-- | :-- | :-- |
| Q-S01 | 영구 ID/카탈로그 | `PromptB46EmergencyEscapePortalTests` | 포탈 ID가 중복 없이 조회됨 |
| Q-S02 | 건설 비용/전력 | BuildingData·PlacementDefinition·PowerNode 검사 | 철 3, 리튬 3, 전력 수요 30 |
| Q-S03 | 통과 가능한 시설 | Runtime Prefab Collider 검사 | 모든 Collider가 Trigger |
| Q-S04 | 건설창 항목 | BuildingMenu Entry ID 검사 | 포탈 항목 정확히 1개 |
| Q-S05 | Integration 배선 | Scene SerializedObject 검사 | Bridge·Player·Elevator·복원 정의 연결 |
| Q-S06 | UI 범위 보호 | `git status`, `git diff --stat` | 요청 밖 UI/Scene/Font/ProjectSettings 변경 0 |

## 2. 기능 테스트 항목

### Q-F01: 사용 비용 원자성

- **준비:** 골드/현재 전력/최대 전력이 서로 다른 GameState
- **실행:** `EmergencyEscapeService.TrySpend`
- **예상 결과:** 성공 시 100G와 최대 전력 10%(올림) 차감, 부족 시 둘 다 불변

### Q-F02: 전력·탑승 E 상호작용 게이트

- **준비:** 수요 30 포탈과 공급 50 전력망, 포탈 내부 플레이어
- **실행:** `EmergencyEscapePortal.RequestEscape`
- **예상 결과:** 전력 연결 시 1회 요청, 수요를 공급보다 높이면 요청 거부

### Q-F03: 목적지 우선순위

- **준비:** 엘리베이터 중앙과 설치 전진기지가 있는/없는 상태
- **실행:** Runtime Bridge로 긴급 탈출 요청
- **예상 결과:** 전진기지가 있으면 최신 코어, 없으면 엘리베이터로 이동

### Q-F04: 시작·부활 위치

- **준비:** Integration Scene의 Player, RunFailureSurfaceFallback, Elevator BoardingAnchor
- **실행:** Scene 정적 검사와 체크포인트 없는 전력 고갈 실패 Play Mode 테스트
- **예상 결과:** 시작과 부활 모두 BoardingAnchor 위치, Run은 Active로 복구되고 엘리베이터 귀환용 최소 전력 5 확보

## 3. 테스트 절차

1. 전용 빌더 `SubTerra/UI/Build Prompt-B 46 Emergency Escape Portal`을 실행한다.
2. App/Gameplay Edit Mode 묶음을 실행한다.
3. App Integration/Player Play Mode 묶음을 실행한다.
4. Unity Console Error와 `git status` 변경 범위를 확인한다.
5. 테스트가 자동 변경한 공용 Font/ViewSocket/ProjectSettings는 원복한다.

## 4. 검증 결과 요약

- **Edit Mode:** 433 통과, 2 실패, 0 스킵
  - 새 Phase Q 테스트는 모두 통과했다.
  - 실패 2개는 수정 범위 밖의 기존 `SettingsPanel` 행 Y 좌표(기대 350, 실제 340), `SurfaceBasePanel` modal sibling 순서(기대 11, 실제 10)다.
- **Play Mode:** 57 통과, 0 실패, 0 스킵
- **Unity Console:** 빌더/컴파일 시 Error 0. Edit Mode의 의도된 실패 경로 테스트가 남긴 Error 로그 외 비예상 Error 0.
- **범위 가드:** `BuildingMenu.prefab`, `Mine_Demo_Integration.unity` 외 기존 UI/Scene 변경 0. Font/ViewSocket/ProjectSettings 자동 변경 원복 완료.
- **모든 Phase Q 항목 통과 시:** 긴급 탈출 포탈 기능 완료
- **실패 항목 존재 시:** Phase Q 직접 실패와 기존 범위 밖 실패를 분리 기록하고, 범위 밖 UI는 별도 요청에서 수정
