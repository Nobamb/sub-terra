# Issue #142 — 초기 배치 사다리 제거 (2026-10-18)

- 담당 mink20001203-hub. 기준 main c4c89655. 전용 브랜치 codex/issue-142-remove-starter-ladder.
- 초기 사다리는 PhaseCElevatorLadderBuilder가 Integration Scene의 PhaseCTraversal 아래에 배치한 LadderTrainingShaft_6m이다. 건설 사다리 및 저장 스냅샷과 별개인 authored prefab instance다.
- 기존 빌더에서 초기 사다리 생성만 제거했다. 건설용1x5 Ladder_Buildable, 비용 철1/구리3, 카탈로그 및 restoreDefinitions, 원래 단위 Ladder.prefab/GUID는 보존했다.
- 대상 Scene 한 개의 정확한 경로/이름/원본 프리팹을 검증하는 전용 Editor 메서드 RemoveStarterLadderFromIntegrationScene으로1개를 제거했다. 플레이어 BuildingInstance는 제거 대상에서 제외한다. 사다리가 없으면 저장하지 않는 멱등 작업이다. 전체 UI/프리팹 빌더는 실행하지 않았다.
- Scene diff는 Editor가 수행한 사다리 instance 및 부모 child 참조 제거79줄뿐이다. 무관한 UI 기본값 직렬화는 Git 역패치로 원래 상태로 복원했다. Scene YAML을 직접 작성하지 않았다.
- 기존 저장 파일과 저장된 설치 사다리 DTO는 변경하지 않는다. 씬 고정 사다리는 저장 목록에 없는 오브젝트이므로 기존 슬롯의 씬 재진입에서도 사라진다. 저장된 플레이어 설치 사다리는 그대로 복원된다.

## 실행 증거
Unity6000.5.4f1.
- Editor 대상 변경 메서드 성공(exit0), 제거1개.
- EditMode8/8 PASS(exit0): PhaseCTraversalStaticTests. 엘리베이터/입력/전진기지/건설사다리 비용/규격/같은좌표 복원 및 초기사다리 부재 확인.
- PlayMode1/1 PASS(exit0): StarterLadderPlayModeTests. 실제 Bootstrap→MainMenu→새 게임→탐사에서 LadderZone0개 확인. 기존 포맷의 플레이어 소유 사다리 스냅샷을 복원해 같은 instanceId/좌표로 저장하고, 씬 교체 이어하기 후에도1개로 유지됨을 확인했다.
- QA 저장은 프로젝트 Temp/Issue142-QA-20261018 별도 루트만 사용. 사용자 슬롯/기존 작업 폴더 미변경.
- 증거: Unity 프로젝트 Builds/Issue142-20261018/ (Git 제외). git diff --check 통과. 검사 자동 설정 변경은 HEAD로 복원.

## 남은 검수
Windows 새 게임의 실제 화면, 사다리 직접 건설/오르내리기, 기존 실제 슬롯의 복사본 검증은 미실행이다. 사용자의 Windows 일괄 검증 보류를 유지한다. 추가 Inspector 배선 없음. 최종 검수·병합은 팀원 영역이며 같은 PR에서 후속 기록을 이어간다.

## #143과 조합 검사
- #142 5dc324cb + #143 1fe93abe를 commit 없는 임시 merge로 조합했다. 충돌 없음. 트리 ce5989d3a56b3a853e29fcaa6a1c06bc9d837089.
- PlayerResumePositionTests + SaveContinuePlayModeTests + StarterLadderPlayModeTests PlayMode12/12 PASS(exit0).
- 실제 Bootstrap 새 게임과 이어하기에서 기본 사다리0개, 저장된 설치 사다리1개 및 종료 좌표 복원을 함께 확인했다. 조합은 검사 후 merge --abort로 해제했으며 팀원의 최종 병합은 하지 않았다.
- 증거 combined-playmode.xml/log, combined-tree.txt는 Builds/Issue142-20261018/에 보존.


## 2026-10-18 최신 main 충돌 해소 및 검토 준비
- 최신 main a3266026의 Integration 씬을 기준으로 전용 Editor 제거 메서드를 실행했다. 잘못 지정한 메서드 namespace의 첫 실행은 종료1로 보존했고 정확한 SubTerra.App.Editor 경로 재실행은 종료0/사다리1개 제거다.
- Editor 자동 직렬화 부수 변경은 제외하고 main 대비 초기 사다리 제거79줄만 보존했다. HUD/레일/현재 씬 연결은 최신 main을 유지한다.
- 새 EditMode8/8, PlayMode1/1 PASS. 실제 Bootstrap 새 게임에서 초기 LadderZone0, 별도 QA 저장의 플레이어 설치 사다리 ID/좌표 복원 확인. 증거: Builds/PRReady-20261018/pr199-*.
- Windows 실제 화면 검증은 사용자 요청으로 완료 기준에서 제외한 미실행 후속 항목이다. 구현·관련 자동 검사 완료로 팀원 검수용 Ready로 전환한다.
