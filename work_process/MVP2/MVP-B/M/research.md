# Phase M — 전체 월드 저장·복원

## 1. 개요

기존 Save/WorldSnapshot 기반 위에 MVP2의 생성 Seed, 수직 구조물, 시설, 균열/붕괴, 가스, 체크포인트와 Run 상태를 완전하게 왕복해야 한다.

## 2. 작업 목표

- 기본 월드는 Seed와 generatorVersion으로 재생성한다.
- 채굴/변경 타일, 건물, 가스, 붕괴, 발견 구역만 변경점으로 저장한다.
- Player/Inventory/Upgrade/Outpost/Run/Drone cooldown을 함께 복원한다.
- 복원 순서 후 파생 상태와 UI를 활성화한다.

## 3. 구현 범위

- Save Version 증가와 Migration
- world seed/generator version, changed cells, buildings, gas/collapse DTO
- 사다리/발판/시설/케이블/체크포인트 Snapshot
- 복원 순서: State→Scene→기본 월드 생성→변경점→건물/가스→파생 재계산→UI
- tmp→검증→backup→정식 파일 원자적 저장

## 4. 권장 구현 방향

1. 기존 `SaveService`, `WorldSnapshotSystem`, `IntegrationActivationGate`를 확장한다.
2. 복원은 instance ID로 멱등 처리한다.
3. generatorVersion이 다르면 Migration 또는 명시적인 비호환 안내를 제공한다.
4. 전력/구조/가스 노출 등 파생값은 복원 뒤 다시 계산한다.
5. 테스트는 실제 사용자 경로가 아닌 임시 파일 시스템을 사용한다.

## 5. 보안 및 안정성 기준

- GameObject/Prefab/TileBase 참조를 JSON에 넣지 않는다.
- 손상된 정식 파일은 backup을 시도하며 둘 다 실패해도 사용자 선택 없이 덮어쓰지 않는다.
- 저장 중 실패하면 기존 정상 파일을 보존한다.
- 세이브 원문과 로컬 절대 경로를 로그에 출력하지 않는다.

## 6. 완료 기준

- 프로세스 재구성 뒤 모든 필수 상태가 동일하게 복원된다.
- 기본 월드+변경점 결과가 저장 직전 타일/시설 상태와 일치한다.
- 손상/중단/Migration 테스트가 통과한다.
- UI와 입력은 모든 복원·재계산이 끝난 뒤 활성화된다.

