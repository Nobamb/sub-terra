# #119 퀘스트 완료 보상 팝업 여백

## 범위와 기준

- origin/main fa2a9db4 기준. 브랜치: codex/issue-119-quest-reward-spacing.
- #119의 마지막 미완료 항목인 QuestClearRewardPanel 배치만 수정한다.
- #205 미션 카드 설명 여백과는 다른 창이다. #195 아이콘은 별도 PR #206, #191 드론 조준점은 보류다.
- 첫 번째 Unity 프로젝트: C:/Users/jeone/sub-terra/sub-terra.

## 변경

- Mine_Demo_Integration.unity의 DemoObjectiveRoot/QuestClearRewardPanel에서 제목·설명을 12px, 보상 행을 80px 위로 올렸다. 실행 코드의 기존 위치와 비교하면 보상 행은 48px 위로 이동한다.
- 보상 행 중심 y=-340, 하단 안내 중심 y=-418. 보상은 프레임 구분선 위 본문에, 안내는 하단 구획에 둔다.
- 보상 없음 대체 텍스트도 행과 같은 높이로 정렬했다. 하단 안내는 기존 실행 코드의 중앙 앵커·16px 글꼴 설정을 씬에도 적용했다. 기존 창 크기·글꼴 에셋·닫기 배선·보상 지급 규칙은 유지한다.
- DemoObjectiveView.ApplyClaimLayout을 빌더와 실행 시 함께 사용한다. SetClaimText가 이전 위치로 되돌리던 경로도 수정했다.
- 전용 메뉴 SubTerra/UI/Apply Quest Clear Reward Spacing은 명시된 통합 씬의 대상 위치만 저장한다. 전체 UI 생성이나 아트 재가져오기를 실행하지 않는다.

## 변경 파일

- Scripts/App/UI/Tutorial/DemoObjectiveView.cs
- Editor/DataValidation/PromptB951QuestClearRewardUiBuilder.cs
- Tests/EditMode/App/Tutorial/PromptB95QuestRewardTests.cs
- Scenes/App/Mine_Demo_Integration.unity (Editor 저장)

## 검증과 인계

- 최초 배치 검사에서 씬의 안내 글꼴 크기(12px)와 기존 실행 코드의 설정(16px)이 다른 점을 발견했다. 씬에도 실행 기준의 앵커·크기를 적용하도록 수정했다.
- Unity 6000.5.4f1 ApplySpacingAndVerify 종료 코드 0.
- ClaimLayout_KeepsRewardsAboveFooterAndSurvivesRuntimeRefresh 통과: 씬 배치, 구분선·안내 간격, 실행 시 이전 위치로 회귀하지 않음, 닫기 배선 유지 확인.
- Presenter_ShowsClaimPopupOnClear_AndGrantsWhenClosed 통과: 팝업을 닫은 후 보상 지급 규칙 유지 확인.
- 씬 저장으로 자동 추가된 다른 팝업·호버의 직렬화와 구형 필드 제거는 Git 부분 스테이징으로 제외했다. YAML을 직접 수정하지 않고 Editor가 생성한 대상 UI 변경만 남겼다.
- 팀원은 통합 씬에서 목표 하나를 완료해 보상 행과 하단 안내의 여백을 확인하면 된다. 닫기 후 기존 보상이 지급되어야 한다.
- Windows 빌드·전체 Demo 완주·실제 화면 검사는 이번 실행 범위에 포함하지 않는다.
