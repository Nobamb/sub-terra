# #253 퀘스트 상세창 닫기 통일

- 기준: `origin/main` `3cb418a3`, 브랜치 `codex/issue-253-quest-close`.
- 첫 Hub 프로젝트: `C:/Users/jeone/sub-terra/sub-terra`, 시작 씬 Bootstrap.
- 지상: `SurfaceBaseQuestPanel.Close()`가 presenter 해제로 상세창 닫힘을 시작한 직후 부모를 비활성화하는 경로 제거. 닫힘 완료 시 DemoObjectiveView가 상세창만 숨김.
- 광산: 최상위 팝업이 퀘스트 상세창일 때 기존 dismiss 이벤트로 전달. 다른 창이 위에 있으면 해당 창의 기존 처리를 유지. 닫는 동안 X 연타는 소비하되 dismiss는 반복하지 않음.
- 공통 TerminalWindowOpenMotion의 모션 감소·재열기 동작 재사용. 씬·프리팹·설정·패키지 변경 없음.
- 검사: 지상 실제 프리팹에서 닫는 동안 부모/상세창 유지 및 종료 후 숨김; 광산 X 경로 및 연타 중복 방지.
- EditMode 위 2건 통과(4.430초), 코드 컴파일 성공, diff 공백 검사 통과.
- 실제 Bootstrap 화면/Windows exe는 미검증. 팀원 확인: 지상 버튼/X·광산 X → 닫힘, 닫는 중 재열기, 모션 감소 시 즉시 닫힘.
