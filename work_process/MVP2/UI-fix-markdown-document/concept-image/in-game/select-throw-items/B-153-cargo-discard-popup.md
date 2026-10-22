# B-153 화물 버리기 창 개편: B-152와 같은 단말의 다음 페이지 (#119)

## 목표
'버릴 자원 선택'으로 뜨는 "화물 버리기" 창(QuestRewardDumpPanel)을 B-152 팝업과 같은 모양으로 바꾸고, 두 창이 **한 단말 안에서 페이지가 넘어가듯** 느껴지게 한다.
- 첨부: `B-153-current.png`(지금 창: 반투명 단색, 갈색·빨강 버튼, 리튬 x0도 그대로 보임), `B-153-ref-b152.png`(B-152 결과: 이 프레임·배경·헤더·게이지를 그대로 쓴다)

## 기준 브랜치
- B-152는 로컬 브랜치 **MVP2-fix92**(635122f3, 035f1cef)에만 있다 (main·PR 없음). 이 위에서 작업하고, 없으면 멈추고 보고한다.

## 절대 규칙 (먼저 읽기)
1. 버리기는 0 아래로 내려가지 않는다. 모든 버튼은 **min(요청 수, 보유 수)**만 버린다. '전부'는 가진 수량 그대로 버린다 (QuantityOf → TryReduceMineral).
   - TryReduceMineral은 보유보다 많으면 실패하므로, 5개·10개·필요는 Presenter(DumpMineral)에서 보유 수로 잘라서 넘긴다.
2. 버린 자원은 골드를 주지 않는다. 판매 경로를 부르지 않는다.
3. 보상은 **정확히 한 번** 지급된다.
   - 연타·연출 중 클릭·마지막 1개 뒤 추가 클릭에도 중복 없음. 연출 중과 처리 중엔 버튼을 잠근다 (CargoFullChoiceGate 방식).
4. 흐름은 바꾸지 않는다. 지금 흐름:
   - 1개 → Dump(id, 1). 전부 → int.MaxValue → 보유 수량. 5개·10개·필요도 같은 DumpRequested(id, 수량) 경로로 보낸다.
   - 버린 뒤 CanFitPending이면 RetryPending → TryGrant가 **즉시 지급**하고 CloseOverflow로 창을 닫는다. 다음 보상이 있으면 ClaimOffered로 클리어 창이 뜬다.
   - 돌아가기와 X 키(TryCloseTopPopup → OnDumpClosedClicked)는 CloseDumpPanel → RetryPending을 거쳐, 아직 부족하면 갱신된 값으로 화물 부족 팝업을 다시 띄운다.
5. 창을 닫은 뒤 이동·채굴·메뉴 입력이 막히지 않는다 (IsInputLocked == false).
6. 계속 깜빡이는 연출은 넣지 않는다.

## 먼저 읽을 코드 (sub-terra/Assets/_Project/ 기준)
- 흐름
  - Scripts/App/UI/Tutorial/DemoObjectivePresenter.cs: ChooseDumpInventory, DumpMineral, CloseDumpPanel, RenderDump, QuantityOf, CloseOverflow
  - Scripts/App/Tutorial/QuestRewardService.cs: Dump, GetDumpRows(QuestDumpRow: MineralId, DisplayName, Quantity, UnitWeight), CanFitPending, RetryPending, FormatDumpSummary, DescribeCargoShortfall(B-152)
  - Scripts/App/Inventory/InventoryService.cs: TryReduceMineral (보유보다 많이 빼면 Insufficient로 실패)
- View: Scripts/App/UI/Tutorial/DemoObjectiveView.cs
  - dumpRoot, dumpSummaryText, dump{Copper,Iron,Lithium}{Text,OneButton,AllButton}
  - SetDumpRows/SetDumpMineral: 0개면 interactable만 끄고, 겉모양은 그대로다
  - OnDump*Clicked, OnDumpClosedClicked
  - B-152의 capacityPopup / OnCapacityPopupClosed / dumpAfterCapacityClose
- 현재 UI: Editor/DataValidation/PromptB95QuestRewardUiBuilder.cs EnsureDumpPanel(720×480)
  - 씬 오브젝트: Scenes/App/Mine_Demo_Integration.unity의 QuestRewardDumpPanel, DumpCopper/DumpIron/DumpLithium, DumpAll, DumpCloseButton
- B-152 재사용 대상 (Scripts/App/UI/Tutorial/): CargoFullPopupView, CargoFullPopupArt, CargoFullChoiceButton, CargoFullChoiceGate, CargoFullPopupTimeline, CargoFullPopupText, CargoFullPopupSkin
  - Scripts/App/Tutorial/CargoShortfallModel.cs, Resources/UI/CargoFullPopupSkin.asset, Editor/DataValidation/PromptB152CargoFullPopupBuilder.cs. 프레임은 ResourceSellPopupView.BuildPanelLayers + MineResetTimedPopupSkin(읽기만)
- 무게: Data/Minerals/*.asset unitWeight (구리 1.5, 철 2, 리튬 0.8). 화면에는 QuestDumpRow.UnitWeight를 쓴다.
- 테스트: Tests/EditMode/App/Tutorial/PromptB95QuestRewardTests.cs("DumpAll" 이름 검사), PromptB152CargoFullPopupTests.cs, PromptB107QuestUiTests.cs

## 재사용 원칙
- B-152 코드를 복사하지 않는다.
  - 화물 게이지(아이콘 + 현재/최대 + 12칸 + 초과 칸 + 부족 문구)는 공용 컴포넌트(예: CargoGaugeView)로 빼서 두 창이 같이 쓴다.
  - 프레임·헤더·버튼(CargoFullChoiceButton)·선택 문(CargoFullChoiceGate)도 그대로 쓴다.
- 공용으로 뺀 뒤에도 B-152 화면과 테스트는 그대로 통과해야 한다.
- 씬 오브젝트 이름(QuestRewardDumpPanel, DumpAll, DumpCloseButton 등)과 onClick 연결은 유지한다. 모양과 위치만 바꾼다.

## 1. 레이아웃 (B-152와 같은 1000×660, 같은 위치)
1) **헤더**: B-152와 같은 아이콘 자리 + "화물 버리기". 오른쪽에 작게 받을 보상 표시 (아이콘 + 수량, 예: 구리 ×3 · 철 ×3 · 골드 ×200)
2) **게이지**: B-152와 같은 공용 게이지
   - 현재/최대와 "부족한 공간 N"이 버릴 때마다 실시간으로 줄어든다. 지금의 "현재 화물 169 / 170 · 보상 중량 10.5" 한 줄은 게이지로 대신한다.
3) **자원 슬롯 목록**: 한 줄에 자원 하나
   - 왼쪽: 도트 아이콘 (Icons/icon_copper.png, icon_iron.png, icon_lithium.png) + 이름 + "×80"
   - 그 아래 작은 글씨: "개당 2 · 합계 160"
   - 오른쪽 버튼 다섯 개: **1개 · 5개 · 10개 · 필요 · 전부** (아래 '버튼 규칙')
   - 버튼 아래에 작게 비워지는 양을 표시한다 (예: 필요 "−10 (5개)", 전부 "−160")
   - **0개인 줄은 흐리게 하고 버튼을 비활성**으로 한다 (줄 순서는 유지해 레이아웃이 튀지 않게). 비활성은 겉모양으로도 분명히 보여야 한다.
- **버튼 규칙**
  - 1개·5개·10개: min(N, 보유)를 버린다. 보유가 N보다 적어도 누를 수 있고, 글자는 그대로 둔다. 보유 0이면 비활성.
  - 필요: 부족을 0으로 만드는 **가장 적은 정수 개수** = ceil(부족 / 개당 무게), 보유 수로 자른다.
    - 가진 것을 다 버려도 모자라면 보유 전부를 버리고, 아래 글씨로 "전부 버려도 N 부족"을 알린다.
    - 부족이 0이거나 보유가 0이면 비활성.
    - 계산은 CargoShortfallModel과 같은 반올림(소수 둘째 자리)을 쓴다. 나눗셈 직전에 작은 epsilon(예: 1e-4)을 빼서 9.5/2 같은 값이 한 개 더 나오지 않게 한다. 버린 뒤 CanFitPending이 true이고, 하나 적게 버리면 false여야 한다.
  - 모양: 1개·5개·10개는 청록 계열 중립, 필요는 조금 더 밝은 청록으로 강조, 전부는 어두운 면 + 절제한 붉은 테두리 (B-152 형식).
  - 다섯 버튼이 1280×720과 1920×1080 모두에서 글자 넘침 없이 한 줄에 들어가게 한다. 필요하면 이름·무게 칸을 줄이고 버튼은 같은 폭으로 둔다.
4) **아래**: 중립 카드 버튼 "돌아가기" (B-152 선택 카드와 같은 형식, 작은 글씨 "화물 부족 창으로")
5) **맨 아래 한 줄**: "버린 자원은 판매 금액을 받지 않습니다." (B-152 꼬리말과 같은 컴포넌트)

- 배경은 B-152처럼 불투명한 금속·육각 무늬 (뒤 화면이 비치지 않게).
- 목록은 지금처럼 구리·철·리튬 세 줄만 다룬다. ×999 같은 큰 수량에서도 글자가 넘치지 않아야 한다.

## 2. 연출
- **페이지 전환** (화물 부족 → 화물 버리기)
  - 프레임은 그대로 둔다. 안쪽 내용만 짧게 바뀐다 (약 0.25초, 가로 스캔선이 한 번 지나가며 이전 내용이 빠지고 새 내용이 들어옴).
  - 창이 가운데 선으로 접혔다 다시 펴지지 않는다. 돌아가기도 같은 방식으로 반대로 넘어간다.
  - 구현은 두 패널을 같은 크기·위치에 두고, 이 경로에서만 접기 연출을 생략하고 내용 전환만 하는 방식을 권한다. B-152의 dumpAfterCapacityClose 순서는 유지한다.
- **버릴 때**
  - 게이지는 부드럽게 줄어든다 (약 0.2초).
  - 해당 줄의 수량 숫자는 한 번 짧게 바뀌는 표시만 준다.
  - 부족 문구 숫자도 같이 줄어든다.
- **공간 확보 완료**
  - 부족이 0이 되면 게이지 끝과 문구가 청록 "공간 확보 완료"로 한 번 바뀐다.
  - 로직은 그 순간 이미 지급을 마쳤다. 표시는 약 0.4초 보여 준 뒤 접혀 닫히고, 그동안 모든 버튼은 잠근다.
  - 다음 클리어 창은 닫힘이 끝난 뒤에 뜬다 (겹치지 않게).
- **호버·키보드 포커스**: CargoFullChoiceButton 규칙을 그대로 쓴다.
- 시간 값은 인스펙터에서 조정하고, 계산은 순수 클래스(예: CargoDiscardTimeline)로 둔다.

## 3. 글꼴 누락 수정 (B-152에도 해당)
- B-152 꼬리말이 "버  자원은 판매 금  을 받지 않습니다."로 나온다. '린'과 '액' 글자가 빠진 것이다.
  - Fonts/NotoSansKR-Regular_SDF.asset은 Dynamic 모드인데, main 기준 글자 표에 '린'(U+B9B0)과 '액'(U+C561)이 없다.
  - 팝업 글자가 실제로 어떤 폰트 에셋을 쓰는지 먼저 확인한다 (CargoFullPopupView는 titleText.font를 따른다).
  - 런타임에 동적 추가가 안 되는 원인도 확인한다. 예: 원본 TTF 참조, 아틀라스 상태, 테스트 중 git restore로 글자 표와 텍스처가 어긋남.
- 고치는 방법
  - B-152와 B-153 팝업의 모든 문구 글자를 폰트 에셋에 미리 넣는다. Editor/DataValidation/KoreanFontAssetUtility.cs의 SeedCharacters 확장이나 TryAddCharacters를 쓴다.
  - 또는 동적 fallback 폰트를 연결한다. 다른 화면 글자 모양이 바뀌면 안 된다.
- 테스트: CargoFullPopupText와 새 버리기 창 문구의 모든 글자에 대해 사용하는 폰트(+fallback)의 HasCharacters가 true인지 EditMode로 검사한다.

## 4. 구현 방향
- 버리기 창 View는 CargoDiscardPopupView처럼 따로 만들고, DemoObjectiveView.SetDumpRows/SetDumpSummary에서 값을 넘긴다.
  - Presenter에 표시값이 더 필요하면 읽기 전용 메서드만 추가한다 (예: SetDumpPanel(CargoShortfallModel, QuestReward, rows)). 새 버튼은 기존 이름 규칙(예: DumpIronFive, DumpIronTen, DumpIronNeeded)을 따른다.
- 지급·버리기 판정은 기존 메서드를 그대로 쓴다. 필요 개수, 줄별 합계, 비워지는 양은 순수 함수(예: CargoDiscardMath)로 둔다.
- Builder는 새로 만들거나(PromptB153CargoDiscardPopupBuilder) B-152 Builder를 확장한다. 씬 변경은 QuestRewardDumpPanel 아래에서만 한다.

## 손대지 말 것
- **마우스 커서 코드** (B-151: GameplayCursor*, PromptB151GameplayCursorBuilder). 보류 중이다.
- 세이브 형식 (SaveVersion, 보류 보상 저장 방식)
- QuestRewardService의 지급·포기·재시도 규칙, InventoryService 계산, 판매 로직
- 퀘스트 클리어·상세 창, 판매창, 보관함 (#212, #213, #217)
- SurfaceBasePanel.prefab의 비어 있는 capacityRoot/dumpRoot (B-152 보고 사항. 이번에도 고치지 않는다)
- MineResetTimedPopupSkin.asset과 다른 창 그림 원본

## 테스트
- EditMode 전체 실행: B-152 보고의 기존 실패 17개 외에 새 실패가 없고, B-95·B-107·B-152 묶음은 전부 통과해야 한다.
- 새 EditMode 테스트
  - 1개·5개·10개·전부: min(N, 보유)만 버림(보유 3에서 5개 → 3), 0 미만 없음, 0개 줄 비활성, 골드 변화 없음
  - 필요: 부족 9.5에서 철(2) → 5개, 구리(1.5) → 7개, 리튬(0.8) → 12개 / 보유가 모자라면 보유 전부 + 안내 / 부족 0이면 비활성 / epsilon 경계(9.5/2, 3.0/1.5 = 정확히 2개)
  - 부족이 0이 되는 순간 지급이 한 번만 일어남. 연타·추가 클릭에도 중복 없음
  - 돌아가기 → 화물 부족 팝업이 갱신된 값으로 다시 뜸
  - 페이지 전환 중 접기 연출이 없고, 두 창이 같이 보이지 않음
  - 줄별 "개당·합계·비워지는 양" 문구, 필요 버튼 아래 "−10 (5개)" 형식
  - 폰트 글자 검사 (3장)
  - 닫은 뒤 IsInputLocked == false
- 옛 문구("1개 버리기", "전부 버리기")나 요약 줄을 검사하는 테스트가 있으면 새 문구로 고친다. 오브젝트 이름 검사는 그대로 통과해야 한다.

## 수동 확인 체크리스트
실행 경로: Bootstrap → 새 게임 → 탐사 → 화물을 채운 뒤 광물 보상 퀘스트 클리어 → 버릴 자원 선택 (DemoObjectiveDebugTools 사용 가능)
- [ ] 화물 부족 → 화물 버리기가 같은 창 안에서 페이지가 넘어가듯 바뀐다 (접혔다 펴지지 않음)
- [ ] 프레임, 배경, 헤더, 게이지가 B-152와 같고, 뒤 화면이 비치지 않는다
- [ ] 헤더 옆 보상 표시가 실제 보상과 맞다
- [ ] 각 줄에 아이콘·이름·수량·개당·합계 무게가 맞고, 0개 자원 줄은 흐리고 눌리지 않는다
- [ ] 1개·5개·10개를 누를 때마다 min(N, 보유)만 줄고 게이지·부족한 공간이 맞게 바뀐다. 전부는 그 자원을 정확히 0으로 만든다
- [ ] 필요를 누르면 딱 필요한 만큼만 버려 공간 확보 완료가 되고, 모자라면 보유 전부를 버리고 안내가 뜬다
- [ ] 다섯 버튼이 1280×720·1920×1080에서 넘치지 않고, 필요가 강조·전부가 붉은 테두리로 구분된다
- [ ] 버려도 골드가 늘지 않는다
- [ ] 부족이 0이 되면 "공간 확보 완료"가 잠깐 보이고 창이 닫히며, 보상이 한 번 들어온다
- [ ] 다음 클리어 창은 닫힘 뒤에 뜬다
- [ ] 돌아가기와 X 키 → 화물 부족 팝업이 갱신된 값으로 다시 뜬다
- [ ] 빠르게 연타해도 두 번 처리되지 않는다
- [ ] 계속 깜빡이는 것이 없다
- [ ] B-152와 B-153 모든 문구에 빠진 글자가 없다 (특히 "버린", "금액")
- [ ] 닫은 뒤 이동·채굴·메뉴 입력과 커서 동작이 정상이다
- [ ] Console 오류가 없다

## 완료 후
변경 파일, 테스트 결과, 공용으로 뺀 컴포넌트, 폰트 누락 원인과 수정 방법, 남은 문제를 보고한다.
직접 확인 못 한 항목은 "미검증"으로 적는다. 커밋은 사용자 확인 후에만 한다.
