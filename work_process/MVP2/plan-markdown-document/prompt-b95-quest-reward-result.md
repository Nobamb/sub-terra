# prompt-B 95 작업 결과

## 변경 내용

- `quest-reward-proposal.md` §4 기본선대로 데모 퀘스트 18개의 클리어 보상(구리/철/리튬/골드)을 `DemoObjectiveCatalog`에 넣었다.
- 퀘스트가 완료되면 `QuestRewardService`가 `InventoryService`와 `GameState.AddGold`로만 지급한다. UI는 인벤/골드를 직접 바꾸지 않는다.
- 좌측 퀘스트 HUD를 클릭하면 중앙 상세 팝업이 열린다. 제목·설명·보상, 하단 `3/18`, 좌우 `<`/`>`, 우측 상단 닫기를 표시한다. `<`는 클리어한 퀘스트, `>`는 아직 클리어하지 않은 퀘스트다.
- 광물 보상이 화물 한도를 넘으면 인벤토리는 그대로 두고, 화물을 버리거나 보상을 포기하는 선택 팝업을 연다. 버리기는 판매가 아니다. 버리기 창을 닫았는데도 공간이 부족하면 같은 선택 팝업이 다시 나온다.
- 세이브 버전을 4로 올렸다. 구세이브는 이미 끝낸 퀘스트 보상을 소급 지급하지 않는다. 화물 부족으로 보류 중인 보상 ID는 저장된다.

## 파일

`sub-terra/Assets/_Project/` 기준:

- `Scripts/App/Tutorial/QuestReward.cs`, `QuestRewardService.cs`: 보상 값과 지급/포기/버리기.
- `Scripts/App/Tutorial/DemoObjectiveCatalog.cs`, `DemoObjectiveDefinition.cs`: 18퀘스트 보상 수치.
- `Scripts/App/Inventory/InventoryService.cs`: 여러 광물 전량 추가 `TryAddManyExact`.
- `Scripts/App/State/GameState.cs`, `Scripts/App/Save/*`: 보류/정산 카운트와 세이브 v4.
- `Scripts/App/UI/Tutorial/DemoObjectiveView.cs`, `DemoObjectivePresenter.cs`, `TutorialDirectorBinder.cs`: 상세 로그와 부족 팝업.
- `Editor/DataValidation/PromptB95QuestRewardUiBuilder.cs`: Integration Scene UI만 생성.
- `Tests/EditMode/App/Tutorial/PromptB95QuestRewardTests.cs`: 보상 표, 지급, 버리기/포기, 로그 탐색, 세이브.

## 검증 및 연결

- Unity EditMode 34개 통과. 결과: `sub-terra/Temp/prompt-b95-editmode-results.txt`.
- Integration Scene `Mine_Demo_Integration.unity`만 저장했다. Surface Base, Main Menu, 인벤토리, 세이브 슬롯 Prefab은 열지 않았다.
- 시작 시 이미 수정돼 있던 `Fonts/NotoSansKR-Regular_SDF.asset`, `init/prompt-B.md`, `ProjectSettings/EditorBuildSettings.asset`은 이번 작업에서 건드리지 않았다.
- Bootstrap부터 실제 플레이 완주 및 Windows 빌드 검증은 수행하지 않았다.
- 커밋·배포는 수행하지 않았다.
