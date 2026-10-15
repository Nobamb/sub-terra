# #146 업그레이드 아이콘 일시 누락 수정 — 2026-10-15

## 이전 판단 정정 및 기준

기존에 OnEnable/OnDisable 복원 코드가 있다는 사실만으로 #146이 해결됐다고 판단할 수 없다. 이전의 '추가 수정 없이 복원 경로 존재' 판단은 실제 구매 후 해금 연출 경로를 빠뜨렸다.
앞서 제안한 단순 잠금→해금 검사도 revealPending/진행 중 코루틴/마지막 페이드에서 재가리는 동작을 다루지 못했다. 해당 검사는 사용자 요청으로 #184에서 제외됐으며 #184는 #146 수정 PR이 아니다.

- 조사 기준 main a92c2825. 게시 전 #184가 병합된 최신 main e4c352e2로 fast-forward했으며 수정 대상 코드/검사 원본은 동일하다. 브랜치 codex/issue-146-upgrade-icon-visibility에서 수정.
- main/MVP2-fix80/MVP2-fix82의 두 런타임 파일 마지막 변경은 동일하게 b5f22198(B-118-1). #146 수정이 이미 반영됐다는 근거 없음.
- 이번 작업을 커밋·푸시하고 #184 후속 PR로 게시한다. 아래 두 파일은 이번에 실제로 변경한 코드다. #184가 이미 main에 병합돼 같은 PR에 후속 커밋을 추가할 수 없는 상태를 확인했다.
- 기존 사용자 시설 데이터 에셋(Clinic/Light/Settlement) 변경은 보존하고 이번 범위에서 제외.

## 확인한 표시 경로

1. ProgressionService가 레벨 확정 후 PurchaseCompleted를 먼저 발행.
2. Presenter → UpgradeTreeView.PlayPurchaseFeedback이 새 해금 항목마다 HoldBlindForReveal 및 PlayUnlock 호출.
3. HoldBlindForReveal이 revealPending=true, PlayUnlock이 SetBlindVisible(true)로 전체 가림을 강제.
4. 이후 UpgradeChanged → 목록 Refresh → Apply가 해금 스냅샷을 받지만, 진행 중 unlockRoutine이면 블라인드 갱신 자체를 건너뜀. 대기 상태여도 revealPending 때문에 다시 가림.
5. UnlockRoutine이 순차 지연과 자물쇠/파편 단계를 끝내야 가림 해제. 마지막 페이드 단계는 blind.alpha를 다시 1에서 시작해 이미 열린 정보를 다시 덮을 수도 있음.

이 경로는 시간이 지나야 주변 아이콘이 다시 나타나는 현상을 설명한다. 실제 제보 화면의 재현 실행까지 완료한 것은 아니며, 모든 종류의 아이콘 누락 원인을 확정했다는 뜻은 아니다.

## 변경 내용

- UpgradeTreeView: 구매 피드백에서 HoldBlindForReveal 호출 제거.
- UpgradeTreeNodeView: HoldBlindForReveal 제거. Apply는 애니메이션 상태와 무관하게 확정 IsUnlocked에 따라 아이콘/정보와 블라인드 표시를 갱신.
- PlayUnlock은 새 블라인드를 올리지 않음. 마지막 점등에서 blind.alpha를 1로 되돌리던 코드 제거.
- 종료 시에도 최신 스냅샷의 잠금 상태를 유지. 실제 미해금 항목을 무조건 공개하지 않음.
- 순차 지연/파편/청록 점등 코드는 유지하지만 확정된 정보 위를 전체 블라인드로 덮지 않음. 자물쇠는 해금 상태 반영 시 숨겨짐.
- 구매/비용/레벨/해금 조건/연결선/세이브 형식/아이콘 에셋 변경 없음. #182의 인접 해금 변경과 별도.

## 검사

- UnlockedIcon_IsVisibleWhileUnlockFeedbackIsPending: 잠금 상태는 숨김. 연출 대기가 남아 있어도 해금 스냅샷 적용 즉시 아이콘 활성/블라인드 0. 반복 갱신에도 유지.
- UnlockFeedback_FinalTweenNeverRehidesUnlockedIcon: 연출의 각 중첩 대기를 수동으로 넘겨 마지막 tween 첫 프레임(t=0)을 직접 검사. 기존의 재가림을 검출하도록 구성.
- SubTerra.App 런타임 소스 경량 컴파일 성공.
- App.Tests.EditMode 중 PromptB118UpgradeTreeTests.cs 소스 경량 컴파일 성공. 전체 테스트 어셈블리/실제 NUnit 실행을 의미하지 않음.
- 두 검사는 Unity 오브젝트를 사용하는 회귀 검사 소스이며 실제 Test Runner에서는 미실행. 수정 전 실패/수정 후 성공을 실제 실행했다고 보고하지 않음.
- 현재 환경에서 Unity Editor는 실행 중이지 않으며 새 Editor 임포트/Windows 빌드/전체 Demo를 시작하지 않았다. 실제 구매·연출·닫기/다시 열기 화면은 미검증.
- diff 검사 및 HoldBlindForReveal 잔여 참조 없음 확인. 블라인드 alpha 쓰기는 상태 표시 메서드 한 곳으로 제한됨.
- 임시 컴파일 출력 정리. 씬/프리팹/.meta/이미지/ProjectSettings/Packages 변경 없음. Inspector 연결 추가 불필요.

## 수정 파일 (sub-terra/Assets/_Project/ 기준)

- Scripts/App/UI/Progression/UpgradeTreeView.cs
- Scripts/App/UI/Progression/UpgradeTreeNodeView.cs
- Tests/EditMode/App/Progression/PromptB118UpgradeTreeTests.cs

## 팀원 전달 시 구분

#184는 설정창/드론 추종이며 #146 수정이 없다. #182는 해금 규칙/블록 표시/드론 스캔이며 이 두 런타임 파일은 수정하지 않았다.
#146은 이번 브랜치의 두 런타임 파일 변경으로 처음 별도 처리한다. 이전에 고쳐졌다고 전달하거나 이슈를 자동 종료하지 않는다. 게시 상태/커밋/URL은 GitHub의 #184 후속 PR을 기준으로 확인하고 실제 실행 미검증을 유지한다.
