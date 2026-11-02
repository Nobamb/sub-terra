# 사다리 채굴은 목표 칸과 이동 상태를 함께 확인
- 날짜: 2026-10-29
- 태그: animation, mining, ladder, player, testing
- 실수: 사다리에서 채굴할 때도 바닥 채굴 그림과 방향 처리가 적용됨
- 원인: 채굴 상태가 사다리 표시보다 우선하며 사다리 전용 재생·목표 조건이 없었음
- 다음부터 할 일: 시작 시 사다리 상태와 ActiveCell을 함께 기록하고, 이동·점프·이탈 취소와 일반 취소·완료의 재생을 분리한다. 검사 준비 시에는 기본 프리팹과 통합 씬의 추가 컴포넌트 차이를 확인한다.
- 관련: #264, B-174, PlayerLadderMiningAnimationSequence, PlayerMiningController
