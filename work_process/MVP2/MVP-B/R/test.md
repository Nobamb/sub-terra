# Phase R Agent Test

## 1. 정적 검증 항목

| ID | 테스트 항목 | 검증 방법 | 예상 결과 |
| :-- | :-- | :-- | :-- |
| R-S01 | 시설별 패널 계층 | `OutpostStaticStructureTests` | Core/Charger/Settlement/Storage/Transaction Root 존재 |
| R-S02 | 스크롤·클리핑 | `OutpostStaticStructureTests` | 코어·정산 목록이 세로 전용 `ScrollRect` 사용 |
| R-S03 | 필수 직렬화 참조 | `OutpostPanelView.HasRequiredReferences` | 모든 모드와 텍스트 참조 연결 |
| R-S04 | UI 범위 보호 | `git status`, `git diff --stat` | `OutpostPanel.prefab` 밖 기존 UI/Scene/Font 변경 0 |

## 2. 기능 테스트 항목

### R-F01: 시설별 E키 패널 분리

- **준비:** 코어, 충전기, 정산 콘솔, 보관함 각각의 Runtime Status
- **실행:** `OutpostPanelPresenter.ToggleInteractionPanel`
- **예상 결과:** 현재 상호작용 시설에 해당하는 `OutpostPanelMode` 하나만 표시

### R-F02: 충전기 즉시 충전

- **준비:** 활성 충전기와 최대치보다 낮은 플레이어 전력
- **실행:** 충전기에서 E 상호작용
- **예상 결과:** 전력이 최대치가 되고 충전 완료 결과 표시

### R-F03: 선택 수량 정산 원자성

- **준비:** 구리 12개, 단가 10G, 활성 정산 콘솔
- **실행:** 구리 5개 판매
- **예상 결과:** 구리 7개와 골드 50G, 실패 시 두 상태 모두 불변

### R-F04: 보관 수량 선택

- **준비:** 활성 보관함과 보유 광물
- **실행:** 1·5·10 버튼 또는 정수 직접 입력 후 보관/꺼내기
- **예상 결과:** 선택한 수량만 기존 `OutpostService` 트랜잭션으로 이동

## 3. 테스트 절차

1. `SubTerra/UI/Build Prompt-B 52 Facility Interaction Panel` 빌더를 실행한다.
2. App Edit Mode 전체 테스트를 실행한다.
3. App Play Mode 전체 테스트를 실행하고 결과 파일과 Console을 확인한다.
4. `git status`, `git diff --stat`으로 범위 밖 UI/Scene/Font 변경을 원복한다.

## 4. 검증 결과 요약

- **컴파일:** Unity 스크립트 재임포트 및 동적 명령 컴파일 성공
- **Edit Mode:** 518 통과, 0 실패, 0 스킵
- **Play Mode:** 전체 묶음과 Outpost 전용 필터를 각각 요청했으나 Unity MCP 경유 TestRunner가 Play Mode에서 종료 콜백/결과 파일을 남기지 않고 대기해 수동 중단함
- **Unity Console:** 중단 후 Error 0, Warning 0
- **범위 가드:** 자동 변경된 `NotoSansKR-Regular_SDF.asset` 원복, 기존 Scene·다른 UI Prefab 변경 0
- **남은 수동 확인:** Bootstrap → 새 게임 → 탐사에서 네 시설 E 상호작용과 보관함 직접 입력을 실제 플레이로 확인
