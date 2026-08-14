# Phase R — 시설별 상호작용 패널 분리

## 0. 작업 전

- `init/prompt-B.md` 52번과 `init/rule.md`, 특히 UI 변경 범위 보호 규칙 2-5를 기준으로 한다.
- 기존 `OutpostService`, 인벤토리, 전력망 상태 DTO와 `OutpostPanel`을 재사용한다.
- Surface Base 판매 UI 자체와 Integration Scene은 수정하지 않는다.

## 1. 개요

코어에서 충전·보관·정산 기능이 한꺼번에 노출되던 `OutpostPanel`을 상호작용 대상 시설에 맞는 네 가지 모드로 분리한다.

## 2. 작업 목표

- 전진기지 코어에는 주변 활성 시설 목록과 세로 스크롤을 제공한다.
- 충전기는 전력망 연결 시 E 상호작용 한 번으로 현재 전력을 최대치까지 채우고 완료 결과를 표시한다.
- 정산 콘솔은 Surface Base 판매창과 같은 보유량·단가·예상 판매가 정보를 표시하고 선택 수량 또는 전량을 판매한다.
- 보관함은 플레이어 보유량과 보관량을 함께 표시하고 1·5·10·직접 입력 수량을 보관하거나 꺼낸다.

## 3. 구현 범위

- App Service: 선택 광물·수량 정산 트랜잭션
- App UI: 시설 모드 판정, 선택 수량, 표시 문자열과 버튼 라우팅
- UI Prefab: `OutpostPanel.prefab` 내부의 코어/충전기/정산/보관 모드
- Editor: `PhaseHOutpostUiPrefabBuilder`를 Prompt-B 52 전용 대상 빌더로 갱신
- Tests: 서비스 원자성, 시설별 모드, 스크롤·계층 정적 검증

## 4. 구현 방향

1. Gameplay가 전달하는 `interactionFacilityBuildingId`와 `interactionFacilityInstanceId`를 현재 시설 판정의 단일 원천으로 사용한다.
2. UI는 `OutpostPanelMode`만 전환하고 GameState·Inventory를 직접 수정하지 않는다.
3. 충전·보관·정산은 모두 기존 `OutpostService`의 시설 활성·거리·전력 검증 뒤 실행한다.
4. 선택 수량 정산은 인벤토리 차감과 골드 증가를 사전 검증한 뒤 한 트랜잭션으로 처리한다.
5. 코어와 정산 목록은 `ScrollRect`와 `RectMask2D`로 패널 밖 텍스트 노출을 차단한다.

## 5. 보안 및 안정성 기준

- 실패한 보관·꺼내기·정산은 인벤토리, 보관함과 골드를 부분 변경하지 않는다.
- 정산 성공 자동 저장 요청은 한 번만 발행한다.
- 기존 영구 ID, Shared DTO와 Save DTO를 변경하지 않는다.
- `OutpostPanel.prefab` 밖의 UI Prefab·Scene·Font 자동 변경은 포함하지 않는다.

## 6. 완료 기준

- 네 시설에서 E키를 누르면 해당 역할의 패널만 열린다.
- 충전 완료, 선택 판매, 전량 판매, 1·5·10·직접 입력 보관이 동작한다.
- 코어 시설 목록이 활성 시설만 표시하고 세로 스크롤된다.
- App Edit Mode 전체 테스트가 통과하고 Unity Console Error가 없다.

