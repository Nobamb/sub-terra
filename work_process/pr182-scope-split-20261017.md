# PR182 범위 분리 — 2026-10-17

팀원 중복 정리 요청에 따라 기존 codex/demo-resource-upgrade-issues에서 작업했다. 새 PR/파생 브랜치 생성 및 최종 병합 없음.

- 최신 main dda39492를 일반 merge하고 중복 블록/드론 구현·검사6개를 main 기준으로 제거했다.
- main 대비 제품 코드 차이는 PromptB118UpgradeTreeBuilder.cs, UpgradeData.cs 및 PromptB118UpgradeTreeTests.cs 세 파일뿐이다. 연결 부모 Lv.1 해금(#150), 기존 구매 유지, 비용/효과/최대 레벨/저장 형식 보존.
- #160/#149의 구현 소유 PR은 #192이다. 이전 issues-160-147-149-150-20261014.md는 당시 작업 이력이며 현재 PR에 모두 포함된다는 의미가 아니다.
- 실제 Unity6000.5.4f1 EditMode: PromptB118UpgradeTreeTests/ProgressionServiceTests/PromptB72UpgradeBalanceTests 총51/51 PASS(exit0).
- Builds/PR182-192-20261017/pr182.log/xml에 로컬 증거 보존. 자동 갱신 ProjectSettings 복원, 씬/프리팹/버전/패키지 임의 변경 없음.
- #192와의 조합 및 Windows 화면 검증은 후속 결과에 별도 기록한다.
