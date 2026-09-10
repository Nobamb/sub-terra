# prompt-B 95-1 작업 결과

## 변경 내용

- 퀘스트를 클리어하면 즉시 지급하지 않고, 화면 중앙에 **퀘스트 클리어 팝업**을 연다.
- 팝업에는 클리어한 퀘스트 이름과 받을 보상(구리/철/리튬/골드)을 표시한다.
- **닫기(X) 또는 확인**을 누르면 그때 `QuestRewardService.ClaimPending`으로 보상을 지급한다. UI는 인벤/골드를 직접 바꾸지 않는다.
- 광물 보상이 화물 한도를 넘으면 기존 자원을 버릴지, 퀘스트 보상을 버릴지 선택 팝업이 이어진다. 버리기는 판매가 아니다.
- 세이브에는 아직 받지 않은 보상 ID가 유지되므로, 팝업을 닫기 전에 저장해도 이어하기 때 다시 표시된다.

## 파일

`sub-terra/Assets/_Project/` 기준:

- `Scripts/App/Tutorial/QuestReward.cs`, `QuestRewardService.cs`: 지급을 팝업 확인 뒤로 미룸.
- `Scripts/App/UI/Tutorial/IDemoObjectiveView.cs`, `DemoObjectiveView.cs`, `DemoObjectivePresenter.cs`, `TutorialDirectorBinder.cs`: 클리어 팝업 표시/닫기.
- `Editor/DataValidation/PromptB951QuestClearRewardUiBuilder.cs`: Integration Scene에 클리어 팝업만 추가.
- `Tests/EditMode/App/Tutorial/PromptB95QuestRewardTests.cs`, `DemoObjectiveTransitionTests.cs`: 지연 지급·팝업·화물 부족 선택.

## 검증 및 연결

- Unity MCP로 `PromptB951QuestClearRewardUiBuilder`를 실행해 `Mine_Demo_Integration.unity`만 저장했다.
- Surface Base, Main Menu, 인벤토리, 세이브 슬롯 Prefab은 열지 않았다.
- EditMode 36개 통과 (`Pass: 36 Fail: 0`). 결과: `sub-terra/Temp/prompt-b95-editmode-results.txt`.
- Bootstrap부터 실제 플레이 완주 및 Windows 빌드 검증은 수행하지 않았다.
- 커밋·배포는 수행하지 않았다.
