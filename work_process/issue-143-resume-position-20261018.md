# Issue #143 — 마지막 위치 이어하기 (2026-10-18)

- 담당 mink20001203-hub. 기준 main c4c89655. 브랜치 codex/issue-143-resume-position.
- 기존 저장에는 플레이어 좌표가 없어 Scene 기본 시작점에서 이어졌다.
- 저장 형식 v6에 선택적 resumePosition(hasPosition, x, y)을 추가한다. v1~v5는 좌표 없음으로 마이그레이션해 기존 시작점을 유지한다. JsonUtility의 누락/빈 객체를 좌표0,0으로 오해하지 않도록 존재 플래그를 사용한다. 잘못된 비유한 좌표는 나머지 정상 저장을 버리지 않고 좌표만 무시한다.
- 실제 ContinueRoutine에서 월드 복원·파생 재계산 → 좌표 배치 → UI/입력 게이트 순서를 보장한다. 복원 중 및 실패 뒤 기본 맵·시작 좌표로 정상 저장을 덮지 않는다. 정상 새 게임 또는 이어하기 성공 때 저장 잠금을 해제한다.
- IntegrationRuntimeBinder의 App 저장 계약으로 연결한다. App이 Gameplay를 참조하거나 Shared 계약을 바꾸지 않는다. 추가 Inspector 배선/Scene/Prefab 변경 없음.
- 몸 전체의 지형·실물 충돌과 발판/사다리 지지를 검사한다. 막힘/바닥 붕괴이면 주변3칸의 지지된 빈 공간, 없으면 Scene 시작점 순으로 검사한다. 둘 다 안전하지 않으면 실패 상태를 반환하고 입력은 열지 않는다. 지형·보상·자원을 수정하지 않는다. Rigidbody 속도/사다리 입력을 정리하고 카메라를 맞춘다.

## 실행 증거
Unity6000.5.4f1, 자동검사. 원래 사용자 저장은 사용하지 않음.
- EditMode45/45 PASS(exit0): SaveServiceTests, SaveRuntimeWiringTests, MineWorldCacheTests, PhaseMWorldSaveTests.
- 최종 PlayMode11/11 PASS(exit0): PlayerResumePositionTests, SaveContinuePlayModeTests.
- 실제 Bootstrap→MainMenu→새 게임→SurfaceBase→탐사→좌표 저장→씬 교체 이어하기 검사를 프로젝트 Temp의 별도 QA 루트에서 수행했다. 저장 파일의 좌표와 복원 좌표를 확인하고, 복원 도중 저장 요청은 CaptureFailed로 차단됨을 확인했다.
- 최초 검사에는 NUnit 미지원 Is.AnyOf로 컴파일 실패. 지원 구문으로 수정. 최초 PlayMode9/10 뒤, 최초 낙하/착지 이전의 불안정한 검사 조건을 명시적으로 안정화하고 저장 파일 좌표 확인을 추가해 해당1/1 통과. 이후 바닥 붕괴 검사 추가와 함께 전체11/11 통과. 최초 로그/XML은 보존하고 검사를 무시/비활성화하지 않았다.
- 로그/XML: Unity 프로젝트 Builds/Issue143-20261018/ (Git 제외). 검사 자동 변경 설정은 HEAD로 복원. 기존 무관한 작업 폴더/씬과 실제 저장 파일은 보존.

## 남은 검수
- Windows 플레이어의 임의 깊이 종료→새 프로세스 이어하기, 사다리/엘리베이터 근처 저장, 구버전 실제 슬롯 복원은 미검증. 사용자 요청대로 Windows 일괄 검증은 보류.
- 저장 v6은 이전 슬롯을 읽을 수 있지만 이전 빌드는 v6을 미래 버전으로 거부한다. 실제 사용자 슬롯을 이번 검사에서 업그레이드하지 않았다.
- 기존 씬의 기본 시작점 자체도 막힌 경우에는 복원 실패를 안내한다. 임의 지형 삭제나 강제 텔레포트는 하지 않는다.
- 같은 PR에 후속 커밋/검수 댓글을 추가하며 최종 병합은 팀원이 한다. #142는 별도 브랜치/PR로 진행한다.


## 2026-10-18 팀원 검수 준비
- 최신 main a3266026 통합. lessons.md 양쪽 기록을 보존해 충돌을 해소했고 저장 코드는 자동 병합됐다.
- 새 EditMode48/48, 최종 PlayMode11/11 PASS. 최초 PlayMode9/11은 실행 인자의 저장 루트를 Builds로 정해 검사 자체의 Temp 경로 안전 조건2개에 걸렸다. 프로젝트 Temp/PRReady198-20261018로 고쳐 전체11개 재통과했다. 제품 코드나 검사를 우회하지 않았다.
- 사용자 요청으로 Windows 실제 화면 검증은 완료 기준에서 제외하고 미실행 후속 항목으로 유지한다. Save v6의 이전 빌드 비호환 안내는 유지하며 코드 검수용 Ready로 전환한다. 새 증거: Builds/PRReady-20261018/pr198-*.

## 2026-10-19 최신 main 재통합
- main fa2a9db4의 메인 메뉴·드론 터미널 변경을 보존했다. #200과 공통 교훈 문서를 동일하게 맞춰 순차 병합 충돌을 방지했다.
- 저장/메뉴 EditMode 초기 62/63: 새 메뉴 검사에서 CurrentVersion=5를 고정해 #143의 v6과 충돌했다. v5/현재 버전 메타 읽기와 원본 파일 보존을 모두 확인하도록 보완한 뒤 64/64 PASS.
- 최신 main에서 PlayerResumePositionTests/SaveContinuePlayModeTests 11/11 PASS. 프로젝트 Temp/PRReady198-20261019 격리 저장. Windows 실제 화면 미실행.
- 증거: Builds/PRReady-20261018/pr198-latest-main-edit*, pr198-latest-main-play.*.
