# Phase D — 카메라 추적과 맵 경계

## 1. 개요

`PlayerCameraFollow`는 부드러운 추적만 제공하고 40m 맵 경계를 제한하지 않는다. 수직 이동 시 빈 공간이 과도하게 노출되지 않도록 화면 크기를 고려한 Confiner가 필요하다.

## 2. 작업 목표

- 수평·수직 이동을 부드럽게 추적한다.
- 카메라 viewport 전체가 월드 경계 밖으로 나가지 않게 한다.
- Surface Base와 Mine의 서로 다른 경계를 지원한다.
- 엘리베이터 이동/구조 실패 시 순간이동에 적절히 대응한다.

## 3. 구현 범위

- 기존 `PlayerCameraFollow` 확장 또는 별도 `CameraBounds2D`
- Orthographic size와 aspect를 반영한 clamp
- Scene별 bounds provider
- 순간이동 시 velocity reset/snap API
- 해상도 변경과 창 모드 대응

## 4. 권장 구현 방향

1. 새 패키지가 필요하지 않으면 기존 Follow를 단순 확장한다.
2. bounds는 Tilemap cell bounds 또는 명시적 Collider2D에서 읽는다.
3. 목표 위치를 먼저 계산한 뒤 viewport half-size를 빼서 clamp한다.
4. 맵이 viewport보다 작은 축은 중앙에 고정한다.
5. 카메라 경계는 렌더링 문제만 해결하며 Player 이동 경계는 B의 충돌 타일이 담당한다.

## 5. 보안 및 안정성 기준

- `Camera.main` 반복 검색과 매 프레임 전체 Tilemap bounds 재계산을 피한다.
- aspect/orthographicSize 변경 시에만 viewport clamp 값을 갱신한다.
- target 소실 시 마지막 안전 위치를 유지하고 예외를 내지 않는다.

## 6. 완료 기준

- 16:9, 16:10, 4:3에서 경계 밖 빈 공간 노출이 허용치 이하다.
- 엘리베이터와 사다리 이동 중 Player가 화면에서 이탈하지 않는다.
- 순간이동/로드 후 카메라가 긴 시간 뒤따라오지 않고 즉시 안정된다.
- 카메라 이동에 눈에 띄는 떨림이 없다.
