# Phase A — 현재 상태 기준선과 통합 완료 게이트

## 1. 개요

기존 MVP는 서비스·Prefab·테스트가 많이 존재하지만 실제 플레이 연결 여부가 서로 다르다. 후속 단계가 잘못된 완료 가정 위에 쌓이지 않도록 현재 상태를 재현 가능한 기준선으로 고정한다.

## 2. 작업 목표

- PRD 필수 기능을 Definition/Runtime/Restore/Play 네 수준으로 추적한다.
- Integration Scene의 Missing Script, 누락 참조, 중복 시스템과 임시 placeholder를 자동 탐지한다.
- 실제 입력 기반 최종 완주 테스트의 뼈대를 만든다.
- 대역 테스트와 실제 Runtime 테스트 결과를 구분한다.

## 3. 구현 범위

- `Mvp2ReadinessReport` 또는 동등한 Editor 검증 도구
- Scene/Prefab/카탈로그/Build Settings 참조 감사
- 핵심 Shared 계약의 Producer/Consumer 연결 표
- 단계별 실패가 차단하는 후속 단계 표시
- Play Mode 최종 시나리오 fixture와 테스트 데이터 격리

## 4. 권장 구현 방향

1. 기존 검증기를 재사용하고 중복 Validator를 만들지 않는다.
2. `Mine_Demo_Integration`을 열어 실제 컴포넌트 참조와 활성 상태를 검사한다.
3. 데이터 에셋의 Runtime Prefab이 공용 placeholder인지 실제 기능 Prefab인지 구분한다.
4. 테스트는 사용자 세이브가 아닌 임시 경로와 독립 State를 사용한다.
5. 보고서는 기능 상태를 `완료/부분/미구현/미검증`으로만 분류하고 추측으로 완료 처리하지 않는다.

## 5. 보안 및 안정성 기준

- 사용자 세이브 원문이나 `persistentDataPath` 실제 파일을 읽지 않는다.
- ProjectSettings와 패키지 버전을 자동 수정하지 않는다.
- Scene 감사 중 에셋을 재생성하거나 저장하지 않는 읽기 전용 모드를 제공한다.
- 기존 사용자 Git 변경을 기준선 생성 과정에서 덮어쓰지 않는다.

## 6. 완료 기준

- PRD 필수 항목마다 현재 증거와 담당 MVP2 단계가 연결된다.
- Integration Scene 필수 참조 누락 목록이 자동 생성된다.
- 대역만 통과한 기능이 실제 통합 완료로 표시되지 않는다.
- B~P가 공통으로 사용할 테스트 진입점과 결과 기록 형식이 확정된다.

