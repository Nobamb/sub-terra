# Phase P — 성능·Windows QA·배포 게이트

## 1. 개요

MVP 완료는 Editor 테스트 통과가 아니라 Windows x64 빌드에서 새 게임, 완주, 저장, 프로세스 재실행, 이어하기가 가능한 상태다.

## 2. 작업 목표

- Development, QA, Release Build Profile을 분리한다.
- 40m 월드와 위험/시설 시스템의 성능 예산을 확인한다.
- Windows 패키지와 README/CHANGELOG/버전 정보를 만든다.
- 개발 PC와 다른 PC에서 새 게임·이어하기를 검증한다.

## 3. 구현 범위

- Build Profile과 Scene 목록/define 설정
- Development debug 도구와 Release 제거 검증
- Profiler 기준: 월드 생성, Tilemap collider, 구조 재계산, UI, 저장
- Version/Build/Save Version 표시
- Windows x64 패키지 구조와 수동 QA 문서
- 치명적 Console/Player 로그 검사

## 4. 권장 구현 방향

1. O의 동일 commit/state로 자동 테스트와 빌드를 수행한다.
2. Release는 Development Build와 debug UI를 끄고 불필요한 상세 로그를 제거한다.
3. 전체 Tilemap/구조/드론/저장 작업의 spike를 Profiler로 측정한 뒤 필요한 부분만 최적화한다.
4. 배포 ZIP에는 exe만이 아니라 Data, UnityPlayer.dll, README, CHANGELOG를 포함한다.
5. 외부 업로드는 사용자 승인 없이 수행하지 않는다.

## 5. 보안 및 안정성 기준

- API 키, 로컬 경로, 사용자 세이브, debug 치트가 Release에 포함되지 않는다.
- QA는 임시 세이브 경로로 Migration/손상 복구를 검증한다.
- 빌드 실패를 숨기거나 Editor 성공으로 대체하지 않는다.
- 다른 PC 검증 전 패키지 해시와 버전을 기록한다.

## 6. 완료 기준

- 세 Build Profile의 차이가 문서와 실제 define/옵션에 일치한다.
- Windows x64 빌드가 생성되고 필수 파일이 완전하다.
- 개발 PC와 다른 PC에서 최종 완주/저장/재실행/이어하기가 성공한다.
- 치명적 오류 0, 성능 예산과 알려진 제한이 기록된다.

