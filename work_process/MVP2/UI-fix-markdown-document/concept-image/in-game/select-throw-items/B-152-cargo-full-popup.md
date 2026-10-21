# B-152 화물 공간 부족 팝업 개편: 단말 경고창 + 도트 철제 스타일 (#119)

## 목표

퀘스트 보상(예: 철 ×2)이 화물에 안 들어갈 때 뜨는 팝업을 바꾼다.
플레이어가 **받을 보상 → 비워야 할 양 → 버릴 것** 순서로 바로 읽을 수 있어야 한다.

- 레이아웃은 첨부 `B-152-ref-layout.png` 그대로. 단 네온·둥근 박스·이모지 아이콘은 따르지 않고 판매창·보관함·새 광산 구역 창 그림체에 맞춘다. `B-152-current.png`는 지금 팝업.

## 절대 규칙 (먼저 읽기)

1. 보상은 **정확히 한 번만** 지급되거나 버려진다.
   - 연타·동시 클릭·연출 중 클릭에도 중복 없음. 열기 연출 중엔 버튼을 잠그고, 하나를 누르면 두 버튼 모두 잠근다.
2. 창을 닫은 뒤 이동·채굴·메뉴 입력이 막히지 않는다 (SetInputLocked가 남지 않게).
3. 동작 흐름은 바꾸지 않는다.
   - 보상 버리기는 지금처럼 확인 없이 바로 포기된다. 확인 단계를 넣지도 빼지도 않는다(필요해 보이면 보고만).
   - 버릴 자원 선택은 지금처럼 선택 창(QuestRewardDumpPanel)을 연다. 돌아가기와 공간 확보 후 자동 지급(RetryPending)은 그대로 둔다.
   - UiLayerPriority 양보, 정렬 순서, TryCloseTopPopup도 그대로 둔다.
4. 화물은 **무게 단위**다. 구리 1.5, 철 2, 리튬 0.8 (Data/Minerals/\*.asset의 unitWeight). 철 ×2는 4를 차지한다.
   - **부족한 공간 = CurrentWeight + 보상 무게 − MaxCapacity**, 0 아래로는 내리지 않는다.
   - 보상 무게 = PendingReward.ComputeMineralWeight(catalog). 골드는 빼고 계산한다. 시안의 20/20·부족 2는 예시 값.
   - 표시는 HudFormatter.FormatCargoAmount와 같은 형식 (정수면 소수점 없음).
5. 계속 깜빡이는 연출은 넣지 않는다.

## 먼저 읽을 코드 (sub-terra/Assets/\_Project/ 기준)

- **흐름**: Scripts/App/UI/Tutorial/DemoObjectivePresenter.cs
  - ShowCapacity(지금 문구), ChooseDumpInventory(선택 창 열기), ChooseForfeitReward(바로 ForfeitPending), DumpMineral, CloseDumpPanel, CloseOverflow
- **View**: DemoObjectiveView.cs, IDemoObjectiveView.cs (capacityRoot/TitleText/BodyText, SetCapacityChoiceText/Visible, OnCapacityDumpClicked, OnCapacityForfeitClicked)
- **계산**
  - Scripts/App/Tutorial/QuestRewardService.cs: CanFitPending, RetryPending, ForfeitPending, Dump, GetDumpRows, FormatDumpSummary
  - Scripts/App/Tutorial/QuestReward.cs, Scripts/App/Inventory/InventoryService.cs(CanFitExact), Scripts/App/UI/HUD/HudFormatter.cs
- **현재 UI**: Editor/DataValidation/PromptB95QuestRewardUiBuilder.cs
  - EnsureCapacityPanel이 720×360 패널과 단색 버튼 두 개를 만든다.
  - 씬은 Scenes/App/Mine_Demo_Integration.unity의 DemoObjectiveRoot/QuestRewardCapacityPanel, CapacityDumpButton, CapacityForfeitButton이다.
- **스타일**
  - Scripts/App/UI/HUD/MineResetTimedPopupSkin.cs와 Resources/UI/MineResetTimedPopupSkin.asset (판매창도 이 스킨을 쓴다)
  - Editor/DataValidation/MineResetSurfaceBaseLayoutBuilder.cs (그림 경로, FontPath)
  - Scripts/App/UI/Sell/ResourceSellArt.cs, ResourceSellButton.cs, Scripts/App/UI/Outpost/StoragePopupView.cs
  - Scripts/App/UI/SurfaceBase/SurfaceBaseButtonFeedback.cs, Scripts/App/UI/HUD/MineResetPopupArt.cs (Segment, Bracket)
- **연출**: Scripts/App/UI/SurfaceBase/MineResetPopupTimeline.cs (가로 빛 → 펼침 → 테두리 섬광), Scripts/App/UI/Drone/DroneTerminalTimeline.cs (B-149). #213 TV 켜짐과 같은 계열이니 공용 계산이 있으면 쓴다.
- **테스트**: Tests/EditMode/App/Tutorial/PromptB95QuestRewardTests.cs (지금 버튼 문구 검사), PromptB107QuestUiTests.cs, QuestDetailsPopupSortTests.cs
- **확인만**: Prefabs/UI/SurfaceBasePanel.prefab의 DemoObjectiveView는 capacityRoot가 비어 있다. 지상에서도 떠야 하는지 확인하고 보고한다. 고치지 않는다.

## 1. 레이아웃 (위 → 아래)

1. **제목**: 경고 아이콘(작게) + "화물 공간 부족", 아래에 구분선
2. **안내**: "보상을 받으려면 화물 공간을 확보하세요."
3. **보상 카드**: 자원 아이콘 + 작은 글씨 "퀘스트 보상" + 큰 글씨 "철 ×2"
   - 값은 실제 QuestReward. 여러 종류면 가로로 나열 (철 ×2 · 구리 ×3).
4. **화물 게이지**
   - 화물함 아이콘 + "화물 적재량" + 오른쪽에 "현재 / 최대"
   - 칸으로 나뉜 게이지 끝에 초과 칸을 붙인다
   - 아래에 호박색 "부족한 공간 N"
5. **선택 두 칸** (나란히, 같은 크기)
   - 왼쪽 청록: 목록 아이콘 + "버릴 자원 선택" / "기존 자원을 버리고 보상받기"
   - 오른쪽 붉은 테두리: 휴지통 + "보상 버리기" / "철 ×2 수령 포기" (실제 보상으로)
6. **맨 아래**: 양옆 얇은 선 + "버린 자원은 판매 금액을 받지 않습니다." (지금 본문의 "판매가 아니라…" 문구를 대신한다)

- "버릴 자원 선택"은 선택 창을 여는 동작이라 정확한 문구다. 바로 버리는 경로가 보이면 문구를 바꾸지 말고 보고한다.
- 창은 지금보다 조금 커져도 된다. 1920×1080에서 가운데에 놓고, 1280×720에서도 잘리지 않아야 한다.
- 게이지: 고정 칸(예: 12칸), 초과 칸 = ceil(부족 / 칸당 무게)로 1~4칸. 칸 수·상한은 인스펙터 값.
- 선택 창에서 돌아오면(CloseDumpPanel → ShowCapacity) 새 값으로 다시 그린다.

## 2. 스타일과 그림 (Art/ 기준, 재사용 우선)

- **프레임·패널**: UI/SurfaceBase/MineReset/mine-reset-frame.png, -frame-glow, -panel을 9-slice로 쓴다.
  - 구분선 mine-reset-title-divider.png / 칸 받침 mine-reset-info-plate.png, mine-reset-cost-plate.png, UI/Gameplay/Quest/quest-reward-plate.png / 철제 모서리 UI/MainMenu/B150/slot-frame-corner.png, slot-frame-bar.png
  - 청록은 테두리 안쪽 얇은 선 정도로만 쓴다. 큰 외곽 발광은 없다.
- **버튼**: 둥근 네온 박스는 쓰지 않는다.
  - 청록: mine-reset-button-confirm(-hover).png, 또는 UI/MainMenu/Settings/button-active-off/on.png
  - 붉은: UI/MainMenu/B150/button-danger-off/on.png. 어두운 바탕에 절제한 붉은 테두리로, 붉은 면을 넓게 칠하지 않는다.
- **아이콘**: 모두 도트 그림. 이모지처럼 보이는 ⚠·둥근 휴지통·매끈한 목록 기호는 쓰지 않는다.
  - 자원: Icons/icon_iron.png, icon_copper.png, icon_lithium.png, UI/Gameplay/Quest/quest-icon-gold.png
  - 화물함: UI/Upgrade/Icons/upgrade-icon-box.png (또는 UI/SurfaceBase/Icons/icon-cargo.png)
  - 경고: UI/Gameplay/HUD/structural-caution.png / 휴지통: UI/MainMenu/B150/icon-trash.png, icon-trash-white.png
  - 목록 아이콘·줄무늬 초과 칸은 새로 만든다. 기존 외곽선·팔레트·픽셀 크기에 맞추고, ResourceSellArt처럼 코드로 그려도 된다. 파일이면 Art/UI/Gameplay/Quest/CargoFull/에 둔다.
  - 크기: 보상 아이콘 약 56px, 버튼 아이콘 약 32px, 경고 아이콘은 제목 글자 높이.
- **색**: 기본은 어두운 남색 금속 + 청록 포인트. 호박색은 **부족한 부분에만**(경고 아이콘, 초과 칸, 부족 문구). 붉은색은 보상 버리기 테두리·아이콘에만.
- **게이지**: 딱 끊기는 도트 블록 (FilterMode.Point, 번짐 최소). 채운 칸 청록, 빈 칸 어둡게, 초과 칸은 호박색 사선 줄무늬 + 얇은 호박 테두리.
- **글씨**: Fonts/NotoSansKR-Regular_SDF.asset. 제목은 판매창·보관함 제목과 같은 크기 (시안처럼 크고 굵게 하지 않음). titleGlowMaterial은 다른 창 수준으로만.

## 3. 연출

- **열기** (0.6초 이내): 가운데 가로 빛이 좌우로 펼쳐짐 → 창이 위아래로 열리며 테두리가 한 번 켜짐 → 내용 페이드 인 → 게이지가 약 0.25초 동안 참 → 초과 칸이 **한 번만** 밝아짐.
- **닫기**: 약 0.2초 역재생. 다음 창(선택 창, 클리어 창)이 바로 이어져도 겹쳐 보이지 않게 한다.
- **호버·키보드 포커스**: 밝기가 오르고 얇은 테두리가 반응한다 (ResourceSellButton 또는 SurfaceBaseButtonFeedback 규칙).
- 계산은 순수 클래스(예: CargoFullPopupTimeline)로 분리한다. 시간·색 값은 인스펙터에서 조정한다.

## 4. 구현 방향

- 표시값은 순수 함수로 뺀다 (예: CargoShortfallModel.From(current, max, rewardWeight)). 지급 판정은 기존 CanFitExact를 그대로 쓴다.
- Presenter 상태(capacityOpen/dumpOpen/claimOpen) 흐름은 유지하고 표시 데이터만 늘린다 (예: IDemoObjectiveView.SetCapacityChoice(CargoShortfallModel, QuestReward)).
- UI는 새 Builder(Editor/DataValidation/PromptB152CargoFullPopupBuilder.cs)로 만들거나 B95 EnsureCapacityPanel을 고친다.
  - 패널·버튼 이름과 onClick 연결은 유지하고, 씬 변경은 이 패널 아래에만 한다.
- 스킨이 필요하면 새 ScriptableObject(Resources/UI/CargoFullPopupSkin)를 만든다. MineResetTimedPopupSkin은 읽기만 한다.

## 손대지 말 것

- **마우스 커서 코드** (B-151: GameplayCursor\*, PromptB151GameplayCursorBuilder). 다른 작업자 때문에 보류 중이다.
- 세이브 형식 (SaveVersion, 보류 보상 저장 방식)
- QuestRewardService의 지급·포기·재시도 규칙, InventoryService 계산
- QuestRewardDumpPanel의 구조와 동작 (모양이 어긋나 보이면 보고만)
- 퀘스트 클리어·상세 창, 판매창, 보관함 (#212, #213, #217 진행 중)
- MineResetTimedPopupSkin.asset과 다른 창이 쓰는 그림 원본

## 테스트

- EditMode 전체 실행, 전부 통과
- 새 EditMode 테스트
  - 부족 계산: 20/20 + 철×2 → 4, 19/20 + 구리×1 → 0.5, 0 미만 없음, 골드 제외, 칸 수 상·하한
  - "N 수령 포기" 문구: 보상 한 종류 / 여러 종류
  - 연타·동시 클릭 시 ForfeitPending / ChooseDumpInventory가 한 번만 실행
  - 타임라인: 0.6초 이내, 초과 칸 섬광 1회, 끝난 뒤 값 고정
  - 닫은 뒤 IsInputLocked == false
- PromptB95QuestRewardTests의 버튼 문구 검사는 새 문구로 고친다. 패널 이름 검사는 그대로 통과해야 한다.
- PlayMode는 이번에 추가하거나 고친 테스트만 실행한다.

## 수동 확인 체크리스트

실행 경로: Bootstrap → 새 게임 → 탐사 → 화물을 채운 뒤 광물 보상 퀘스트 클리어 (DemoObjectiveDebugTools 사용 가능)

- [ ] 가로 빛과 함께 창이 켜지고, 게이지가 찬 뒤 초과 칸이 한 번만 밝아진다. 이후 깜빡임 없음
- [ ] 제목·보상·게이지·버튼·맨 아래 문구가 시안 순서다
- [ ] 판매창과 같은 철제 프레임이고, 네온이 과하지 않으며, 아이콘이 전부 도트다
- [ ] 제목 크기가 다른 팝업과 비슷하다
- [ ] 철·구리·리튬·여러 종류 보상에서 보상 카드와 "N 수령 포기"가 맞다
- [ ] 18/20, 20/20, 소수점 무게에서도 현재/최대와 부족한 공간이 실제와 맞다
- [ ] 호박색은 부족한 부분에만 쓰였다
- [ ] 버릴 자원 선택 → 선택 창 → 공간이 생기면 보상이 한 번 지급된다
- [ ] 선택 창에서 돌아가기를 누르면 갱신된 값으로 다시 뜬다
- [ ] 보상 버리기 → 지급 없이 닫히고, 다음 보상이 있으면 정상으로 이어진다
- [ ] 빠르게 연타해도 두 번 처리되지 않는다
- [ ] 닫은 뒤 이동·채굴·메뉴 입력이 정상이다
- [ ] 긴 문구나 큰 숫자에서도 글자가 넘치지 않는다 (1920×1080, 1280×720)
- [ ] 버튼 호버와 키보드 포커스에 반응한다
- [ ] 커서 동작이 이전과 같다
- [ ] Console 오류가 없다

## 완료 후

변경 파일, 테스트 결과, 재사용·신규 그림, 부족 계산식과 칸 규칙, SurfaceBasePanel capacityRoot 확인 결과, 남은 문제를 보고한다.
직접 확인 못 한 항목은 "미검증"으로 적는다. 커밋은 사용자 확인 후에만 한다.
