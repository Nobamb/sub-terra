# Issue #147 — 업그레이드 구매 알림 순서 보완 (2026-10-17)

## 소유권과 작업 흐름
- 담당: mink20001203-hub. 이슈에 assignee 추가 및 진행 중 댓글 작성. 기존 Nobamb 지정은 보존.
- 기준: main dda394923a14d244bf2e4f4eafe16ce65f578735.
- 전용 브랜치: codex/issue-147-immediate-upgrade. 사용자가 새 작업별 브랜치/PR 생성을 명시적으로 허용함.
- 기존 #182/#192 후속 작업과 분리. 최종 병합은 팀원 검수 영역.

## 조사 결과와 수정
- main에는 07bb9597에서 도입된 RunFailureRuntimeController의 MaximumHealth/HealthRegeneration 즉시 재바인딩이 이미 존재한다. 이 경로를 새로 구현하거나 원래 제보의 모든 증상을 재현했다고 주장하지 않는다.
- 남은 관찰 가능한 문제: ProgressionService.TryPurchase의 성공 경로가 PurchaseCompleted를 UpgradeChanged보다 먼저 알린다. 따라서 성공 피드백 소비자는 레벨이 커밋된 뒤에도 이전 선택 레벨/파생 상태를 읽을 수 있다.
- 신규 EditMode 재현 2개가 수정 전 실패: 구매 완료 알림 시 최대 전력 100(기대120), 최대 화물50(기대70).
- 수정: 레벨 커밋 → UpgradeChanged → PurchaseCompleted → AutoSaveRequested. 실패 경로, 비용 차감, 해금 기준, 효과 계산, 저장 DTO는 유지.
- 추가 Inspector 배선 없음. Scene/Prefab/meta/Gameplay 구현/Shared 계약/연출 변경 없음.

## 실행 증거
Unity 6000.5.4f1 Test Framework, 동일 로컬 체크아웃.
- 수정 전 EditMode 재현: 0/2 PASS, exit2 (예상한 실패).
- 수정 후 EditMode: 53/53 PASS, exit0. ProgressionServiceTests, ProgressionPanelBinderTests, PromptB118UpgradeTreeTests, PromptB72UpgradeBalanceTests.
- 최종 PlayMode: 3/3 PASS, exit0. ProgressionPlayModeTests.
  - 구매 프레임에 Presenter 읽기 모델/드릴 Provider 갱신.
  - 구리 비용 수확량 구매 시 성공 피드백 이전 선택 레벨 갱신, 수확량 보너스 및 비용 확인.
  - 실제 RunFailureRuntimeController와 PlayerSurvivalController 연결에서 성공 알림 시 최대체력130 확인 및 구매 즉시 재생 적용. 씬 재로드/저장 파일 사용 없음.
- 로그/XML: Unity 프로젝트 Builds/Issue147-20261017/ (Git 미포함).
- Unity 자동 생성 설정의 SENTIS_ANALYTICS_ENABLED 제거 및 EditorBuildSettings 줄바꿈 변화는 검사 후 HEAD로 복원. 기존 무관한 미추적 작업 폴더/씬은 보존.
- 기존 obsolete API/라이선스 갱신 진단 로그는 남아 있으며 검사 실행과 종료는 성공. 테스트를 비활성화하지 않음.

## 미완료 / PR 후속 작업
- Windows 실제 화면에서 원래 제보의 구리 업그레이드 라벨, 최대 체력 및 HUD가 창 재열기/엘리베이터 이동 없이 즉시 반영되는지 확인해야 한다. 자동 검사는 실제 화면 확인을 대체하지 않는다.
- 이번 작업에서 Windows 빌드/Demo 완주/종료/이어하기는 실행하지 않았다. 사용자 요청대로 일괄 실행 검증은 보류 상태.
- 위 항목은 같은 PR의 후속 댓글과 커밋으로 관리하며 #147을 아직 닫지 않는다.
- #143 마지막 위치 복원, #142 초기 사다리 제거는 다음 별도 작업이며 이번 PR 범위에 포함하지 않는다.
