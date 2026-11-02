# 착지 그림 추가 후 생성 직후 대기 가정 확인

- 날짜: 2026-10-29
- 태그: animation, jump, player, testing, playmode
- 실수: B-175 새 PlayMode 검사가 생성 직후 바로 대기 그림이라고 가정해 실패함
- 원인: 생성 첫 틱에는 바닥 접점이 없어 공중으로 보고 fall 1→착지(ready 8→1)가 먼저 재생됨
- 다음부터 할 일: 착지·전환 단계를 추가하면 생성·복원 직후 첫 틱의 접지 상태를 확인하고, 검사는 대기 그림이 될 때까지 기다린 뒤 시작한다
- 관련: #264, B-175, PlayerJumpAnimationSequence
