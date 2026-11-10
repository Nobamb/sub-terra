# 업그레이드 모션 감소에서도 해금 시간표와 결과를 공유한다
- 날짜: 2026-11-09
- 태그: ui, animation, accessibility, upgrade
- 실수: 업그레이드 창 열기와 닫기에 적용한 모션 감소가 노드 및 심층 썸네일의 해금에는 전달되지 않았다.
- 원인: 창, 노드, 썸네일이 별도의 연출 경로를 썼다.
- 다음부터 할 일: 동작과 광원만 줄이고 효과음 시점, 순차 해금, 최종 시각 상태는 공통 시간표를 유지한다. 연출 도중 설정 변경과 닫기 후 재열기를 확인한다.
- 관련: #282, UpgradeTreeNodeView, DeepZoneThumbnailStrip, UnlockRevealTimeline.
