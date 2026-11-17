# 트리 빌더의 전체 해금 적용이 기존 업그레이드 에셋 8개를 함께 바꿈

- 날짜: 2026-11-17
- 태그: upgrade, builder, data, scope
- 실수: 새 업그레이드 2종을 넣으려고 `PromptB118UpgradeTreeBuilder.ApplyUnlockRules()`를 돌렸더니 기존 `Upgrade_*.asset` 8개의 `unlockRequirementUpgradeId`/`unlockRequiredLevel`이 함께 바뀌었다.
- 원인: 이 메서드는 `Nodes` 표 전체를 모든 에셋에 다시 쓴다. 저장돼 있던 값은 표와 달랐고(트리 부모가 있으면 런타임이 부모 Lv.1로 읽으므로 동작은 같다), 요구사항은 "기존 업그레이드를 바꾸지 않는다"였다.
- 다음부터 할 일: 새 에셋의 해금 값은 새 에셋에만 쓰는 전용 빌더(`PromptB200OxygenUpgradeBuilder`)로 넣는다. 데이터 빌더를 돌린 뒤에는 `git status`로 새 에셋 외 `Data/Upgrades/*`가 바뀌었는지 보고, 바뀌었으면 `git restore`한다. 테스트 실행 뒤에는 폰트·프리팹·ProjectSettings가 같이 dirty가 되므로 범위 밖은 복구한다.
- 부수 메모: 플래그 러너(`subterra-run-editmode.flag`)는 파일 내용을 테스트 그룹 필터로 쓴다. 전체 실행은 빈 파일, 일부 실행은 클래스명을 줄바꿈으로 적는다("run" 같은 글자를 쓰면 1건만 돈다).
- 관련: #336, B-200
