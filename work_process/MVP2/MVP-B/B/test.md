# Phase B Agent Test

## 1. 정적 검증 항목

| ID | 테스트 항목 | 검증 방법 | 예상 결과 |
| :-- | :-- | :-- | :-- |
| B-S01 | 분포 데이터화 | 생성 코드 상수/에셋 검사 | band별 데이터 존재 |
| B-S02 | 영구 ID | 생성 결과 타일 ID 검사 | 카탈로그 등록 ID만 사용 |
| B-S03 | 경계 정의 | 좌우/하단 타일 데이터 검사 | 채굴 불가 |
| B-S04 | 저장 계약 | World DTO 검사 | Seed와 generatorVersion 존재 |

## 2. 기능 테스트 항목

### B-F01: 같은 Seed 재현

- **준비:** 동일 Seed와 생성 버전
- **실행:** 월드를 두 번 생성해 좌표별 타일 ID를 비교한다.
- **예상 결과:** 완전히 동일하다.

### B-F02: 깊이별 분포

- **준비:** 고정 Seed 표본 100개 이상
- **실행:** band별 타일 비율과 vein 크기를 집계한다.
- **예상 결과:** 암석 85~90% 목표 허용 범위와 자원 종류 규칙을 만족한다.

### B-F03: 필수 경로

- **준비:** 생성된 40m 월드
- **실행:** 시작점에서 심층 신호까지 통과 가능 셀을 탐색한다.
- **예상 결과:** 최소 한 경로가 존재한다.

### B-F04: 경계 채굴 거부

- **준비:** 좌우/하단 경계 타일
- **실행:** 실제 MiningSystem으로 채굴을 시도한다.
- **예상 결과:** 타일과 보상이 변하지 않는다.

### B-F05: Integration 생성

- **준비:** 새 게임
- **실행:** Mine Scene에 진입한다.
- **예상 결과:** 40줄, band별 자원, 안전 시작 공간이 렌더링된다.

## 3. 테스트 절차

1. Edit Mode에서 Seed/분포/연결성 테스트를 실행한다.
2. Play Mode에서 실제 Tilemap 생성과 경계 채굴을 검증한다.
3. 생성 시간과 Tilemap Collider 갱신 시간을 기록한다.

## 4. 검증 결과 요약

- **상태:** 통과 (2026-07-31)
- **NUnit Edit Mode:** Phase B 생성기·Integration Scene·Snapshot 8/8 통과
- **고정 Seed 표본:** 128개 모두 암석 85~90%, band별 허용 종류, 2~5칸 광맥/가스 포켓, 필수 자원 최소 수량과 안전 경로 통과
- **결정론:** 같은 Seed/버전 해시 일치. Integration Seed `20260731`, version `1`, hash `11394529670739120800`
- **Integration 분포:** 3,240칸, 암석 90.00%, 구리 122, 철 115, 리튬 39, 가스 47, 잠긴 신호 1
- **Play Mode:** 실제 Tilemap 생성·경계 채굴 거부 1/1, 기존 Mining 회귀 4/4 통과
- **Integration Play:** 실제 Scene 진입 시 40m·Seed·버전·안전 경로·해시 일치 확인
- **DTO/Scene 회귀:** generatorVersion JSON 왕복과 Integration 구조/Missing Script 감사 5/5 통과
- **측정:** 128개 순수 지층 생성 35.74ms, Integration Tilemap 기록+Physics 동기화 6.11ms
- **Console:** Phase B 생성 오류 없음. 직접 Integration Scene만 연 검증에서는 기존 `DemoObjectiveDebugTools`의 구 Input API 오류가 별도로 재현됨
- **모든 항목 통과 시:** Phase B 완료
- **실패 항목 존재 시:** 실패 Seed와 분포 통계를 남기되 전체 Tilemap 덤프는 남기지 않는다.

