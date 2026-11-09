# 전체 EditMode 비교는 PlayMode보다 먼저 실행한다
- 날짜: 2026-11-04
- 태그: testing, editmode, playmode, timing, issue158
- 실수: 변경 전 전체 EditMode를 확보한 뒤 PlayMode를 실행하고 최종 전체 EditMode를 돌려, 사다리 피해 복귀 검사의 시간 의존 실패가 하나 추가됐다.
- 원인: PlayerAnimationController.LateUpdate가 실제 Time.unscaledDeltaTime으로 진행한다. 기대 프레임 12가 전체 실행에서 13, 단독 재실행에서 14로 바뀌었다. 기존 시간 격리 교훈이 별도 PlayMode 어셈블리뿐 아니라 실행 순서에도 적용됨을 놓쳤다.
- 다음부터 할 일: 동일 에디터 세션의 전체 EditMode 기준 비교를 먼저 끝내고 PlayMode를 실행한다. 추가 실패는 기준 목록·호출 경로·단독 결과를 함께 확인하고, 범위 밖 코드를 고쳐 통과시켰다고 보고하지 않는다.
- 관련: #158, B-183, 2026-10-31-issue158-runtime-test-isolation.md, PlayerLadderMiningTests.Damage_ResumesThirteen_KnockoutInterrupts.
