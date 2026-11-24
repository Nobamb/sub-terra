# #127 데모 다국어 누락 조사

2026-11-24 기준 main `bd440b75`에서 코드와 UI 생성 경로를 확인했습니다. 지원 언어는 한국어(`ko`)와 영어(`en`)입니다. 설정 로더와 적용기는 언어 값을 읽어 `LocalizationService.SetLanguageCode`에 전달합니다. 영어 키에 값이 없으면 한국어가 표시됩니다. 실제 화면, 저장 유지, 텍스트 잘림은 미검증입니다.

| 우선순위 | 화면 | 확인한 누락과 수정 범위 |
| --- | --- | --- |
| 1 | 메인 메뉴·설정 | [메인 메뉴 생성기](../sub-terra/Assets/_Project/Editor/DataValidation/PromptB150MainMenuBuilder.cs#L108)는 이어하기·새 게임·설정·종료에 한국어를 넣습니다. [MainMenuView](../sub-terra/Assets/_Project/Scripts/App/UI/MainMenu/MainMenuView.cs#L808)의 언어 선택 미리보기는 설정 라벨을 갱신합니다. 메인 메뉴의 버튼·슬롯·확인창도 번역 키로 연결하고, 언어 적용 후 이미 열린 메인 화면을 갱신합니다. 적용과 취소의 동작은 각각 확인합니다. |
| 2 | HUD·퀘스트 | [HudFormatter](../sub-terra/Assets/_Project/Scripts/App/UI/HUD/HudFormatter.cs#L29)는 전력·체력·화물의 한국어 문구를 만듭니다. [BasicHudView](../sub-terra/Assets/_Project/Scripts/App/UI/HUD/BasicHudView.cs#L139)는 전력·체력의 한국어 접두사를 제거해 숫자를 표시합니다. 게이지 숫자와 라벨을 분리해 두 언어에서 중복이나 누락을 막습니다. [DemoObjectiveCatalog](../sub-terra/Assets/_Project/Scripts/App/Tutorial/DemoObjectiveCatalog.cs#L9)와 [DemoObjectivePresenter](../sub-terra/Assets/_Project/Scripts/App/UI/Tutorial/DemoObjectivePresenter.cs#L439)는 한국어 목표 정의를 표시합니다. 브리핑, 목표 제목·조건, 보상 안내를 기존 목표 ID 기준으로 번역합니다. |
| 3 | 가이드·시설 | [GameGuideCatalog](../sub-terra/Assets/_Project/Scripts/App/UI/Guide/GameGuideCatalog.cs#L112)는 첫 조회에 카드 목록을 구성합니다. 한국어 고정 문구를 찾아 번역 키로 옮기고, 언어가 바뀌면 이미 열린 카드도 다시 표시합니다. [BuildingMenuView](../sub-terra/Assets/_Project/Scripts/App/UI/Building/BuildingMenuView.cs#L310)의 건설 안내, [FacilityServicePopupView](../sub-terra/Assets/_Project/Scripts/App/UI/Outpost/FacilityServicePopupView.cs#L26)의 충전·치료 문구와 [StoragePopupView](../sub-terra/Assets/_Project/Scripts/App/UI/Outpost/StoragePopupView.cs#L434)의 보유·보관 문구도 고정 한국어입니다. |

## 데스크탑 확인 순서

1. 메인 메뉴에서 한국어 → 영어를 적용하고 메뉴·슬롯·확인창을 확인합니다. 영어 → 한국어 선택 후 취소하면 적용된 영어가 유지되는지도 봅니다.
2. 영어로 새 게임과 이어하기를 각각 열고 HUD 숫자·퀘스트 브리핑·목표·보상 안내를 확인합니다. 재실행 후 영어 설정이 유지되는지 확인합니다.
3. 같은 실행에서 한국어로 되돌려 가이드와 건설·보관·충전·치료 창을 다시 엽니다. 선택한 언어, 입력값·수치, 긴 문구의 잘림을 확인합니다.

먼저 메인 메뉴와 설정을 한 PR에서 연결하고, HUD·퀘스트 및 가이드·시설을 대상 화면별로 이어갑니다. 목표 ID, 시설 ID, 저장 데이터 형식은 기존 것을 사용합니다.
