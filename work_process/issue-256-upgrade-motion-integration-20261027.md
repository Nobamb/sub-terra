# PR #256 / #252 업그레이드 연출 연결

## 작업 상태
- 기존 PR #256 브랜치 `codex/issue-249-surface-inventory-main`에서 작업.
- 최신 main 확인: `3cb418a3`. 팀원 #252 구현은 아직 main 미병합.
- 팀원 `origin/MVP2-fix108`의 `f71535a8`를 `git merge --no-commit --no-ff`로 로컬 통합. 새 연출 제작 없음.
- SurfaceBaseBinder/View의 충돌 2개 해결. 5건 검사 통과 후 사용자 승인으로 기존 #256에 통합 커밋을 추가해 게시한다. 새 PR은 만들지 않으며 팀원 커밋은 원래 이력/작성자를 유지한다.

## 이번 연결 변경
- 업그레이드 버튼·U키 → 공통 OnUpgradeClicked → RequestUpgradeOpen.
- ESC·X키·U키 토글·창 X 버튼 → 공통 OnUpgradeCloseClicked → RequestUpgradeClose.
- 업그레이드의 중복 TerminalWindowOpenMotion 호출과 별도 upgradeVisible 플래그 제거. 전용 UpgradeWindowMotion 상태 사용.
- 닫는 중 다른 인벤토리/설정 창은 막되 같은 업그레이드는 이어서 재열기 허용.
- Bind/OnDisable 같은 강제 초기화는 팀원 SetUpgradeVisible 즉시 정리 경로 유지.
- 인벤토리는 기존 TV 연출·수량 갱신 유지.
- 실제 연결 수정 파일: SurfaceBaseBinder.cs, SurfaceBaseView.cs, Issue249SurfaceInventoryTests.cs. lessons 및 이 기록 추가.
- 그 외 staging 파일은 팀원 f71535a8의 원본 코드/검사/기록이며 재구현한 코드가 아님. 씬·프리팹·ProjectSettings·Packages 내용 변경 없음.

## 최소 검증
- EditMode 3/3 통과(2.470초): 지상 닫힘 종료까지 루트 유지, 모션 감소 즉시 처리, 버튼·ESC·X·창 X의 공통 요청.
- 실제 지상 씬 PlayMode 2/2 통과(18.130초): U키 최초 자물쇠 연출, 버튼 재열기 단축 연출, ESC/X/U 닫기, 닫는 중 재열기, 닫는 중 I 열기 차단, 기존 인벤토리 실시간 수량 및 재열기.
- 결과 `work_process/tools/issue-256-upgrade-motion`.
- 코드 컴파일 성공. 기존 obsolete API 경고는 이번 범위 밖. 새 컴파일 오류 없음.
- 첫 Hub 프로젝트 `C:/Users/jeone/sub-terra/sub-terra`에서 확인 가능.

## 미검증 / 수동 확인
1. Bootstrap → 실제 슬롯 → 지상: 버튼과 U키의 자물쇠 전 구간이 같은지 육안 확인.
2. 열다 닫기·닫다 재열기, 구매 버튼이 연출 종료 전 눌리지 않는지 확인.
3. 광산 U키와 비교하고 Windows exe에서도 동일하게 닫히는지 확인.
4. 효과음은 팀원 후속 브랜치 작업이므로 이 통합에 추가하지 않음.
