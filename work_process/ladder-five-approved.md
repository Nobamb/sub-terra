# 승인된 사다리 5프레임 적용

- 기준 main: `2364b01e` (2026-10-04 fetch).
- 작업 브랜치: `codex/ladder-five-frame-approved`.
- 팀원이 승인한 `output/ladder-review-five-v1`의 5장을 그대로 적용. 새 아트 생성/추가 프레임 제작은 하지 않음.
- 파일 순서: 01 중립, 02 오른손/오른발 중간, 03 오른손/오른발 상승, 04 왼손/왼발 중간, 05 왼손/왼발 상승.
- 상승 순서: `0,1,2,1,0,3,4,3`. 하강은 동일 배열을 역방향으로 진행.
- 실제 Y 거리 기반으로 진행, 정지 시 중립, 이탈 시 일반 표시 복원. 기존 시각 유예와 이동/입력/중력/재탑승 판정은 변경하지 않음.
- 5장 연결의 단계 거리는 0.25m. 이전 4단계×0.5m와 새 8단계×0.25m의 전체 주기 이동 거리(2m)는 같음.
- 기존 3장 사용하는 프리팹/테스트는 기존 4단계 순서로 호환.
- 기존 01~03 meta GUID 유지. 04/05 meta는 Unity 임포트로 생성.
- 전용 Editor 메뉴 `SubTerra/Apply Approved Ladder Five Frames`는 사다리 이미지 임포트와 Player Prefab의 사다리 배열/단계 거리만 갱신. 일반 애니메이션, Animator Controller, 씬은 재생성하지 않음.

## 실행할 Unity 프로젝트

`C:\Users\전민권\sub-terra\codexwt-ladder-five\sub-terra`

기존 `C:\Users\전민권\sub-terra\sub-terra` 및 이전 작업 폴더는 변경하지 않았다. 자동으로 이번 변경이 반영되는 것은 아니다. 이 프로젝트에서 Bootstrap부터 실행한다.

## 검증 계획 및 현재 상태

- git diff --check 통과.
- Unity 6000.5.4f1 일반 에디터에서 컴파일 및 집중 검증 완료.
- PlayerMovement PlayMode: 32개 통과, 실패/스킵 0. 기존 사다리 점프·재탑승·상단 이탈 및 새 5프레임 정/역방향 순서·정지 회귀 포함.
- PhaseCTraversalStaticTests EditMode: 6개 통과, 실패/스킵 0. 정확한 5개 PNG 경로/참조, 렌더 순서, 파츠 리그 비활성 연결, 보존 파츠, 사다리 복원 검사 포함.
- Bootstrap 통합 PlayMode: 1개 통과, 실패/스킵 0 (7.19초). Bootstrap → 새 게임 → SurfaceBase → 탐사 후 실제 시설 Prefab에서 상승/하강 모두 5프레임 표시, Walk 혼입 없음, 정지 중립, 이탈 일반 표시 복원, VisualRoot 바운스 없음 확인.
- 초기 Bootstrap 실행은 시작 브리핑이 물리를 일시 정지한 상태에서 FixedUpdate를 기다려 시간 초과. 테스트만 실제 TryCloseGuidance 경로로 브리핑을 닫도록 보완한 재실행은 통과. 게임의 일시정지/이동 로직은 수정하지 않음.
- 첫 제한 환경 실행은 Package Manager IPC 실패. 배치 재시도는 headless 라이선스 부재로 종료. 일반 에디터 모드는 정상 실행. 기존 CS0618/CS0184 경고와 라이선스 토큰 갱신 경고는 남아 있으나 집중 테스트 실패/새 컴파일 오류는 없음.
- 증거: 저장소 루트 output/ladder-review-five-v1의 five-player-results.xml, five-bootstrap-results-v2.xml, five-editmode-results.xml 및 대응 로그.
- 검증 자동 생성 폰트·공용 설정·임시 테스트 씬은 generated-validation-backup에 보존하고 변경 범위에서 제외. 사용자 원래 작업과 Recovery는 변경하지 않음.
- Unity Prefab 저장 과정에서 기존 스크립트 기본값(ladderSpeed 4 등)이 명시적으로 직렬화됨. 값은 기존 기본값과 같으며 이동 동작 변경 없음.
- 자동 검증은 Sprite 선택/연결과 물리 동작을 입증하지만 최종 게임 크기에서의 미적 자연스러움은 사용자 확인 필요. 화면 캡처는 하지 않음.
- 실제 저장 대신 Temp의 새 테스트 세이브 경로 사용.
- 전체 테스트 및 Windows 빌드는 이번 집중 검증 범위 밖.
- 2026-10-04 사용자 요청으로 한글 커밋·브랜치 푸시·main 대상 PR 게시를 진행. main 직접 푸시나 PR 병합은 하지 않음.
