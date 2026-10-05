# Prompt-B 136 — 지상 자원 판매창·정산 콘솔 판매 UI 통일

## 공통화한 구조
두 화면이 같은 팝업(`ResourceSellPopupView`)과 같은 상태(`ResourceSellSession`)를 쓰고, 화면별 규칙은 어댑터(`IResourceSellBackend`)가 데이터로 넘긴다. 거래는 각 화면의 기존 서비스가 한다.

```
[지상] EconomyPanelBinder ── SurfaceSellBackend ──┐                ┌── EconomyPanelPresenter.RequestSellBatch → EconomyService.TrySellMinerals
                                                   ├─ ResourceSellSession ─ ResourceSellPopupView
[콘솔] OutpostPanelBinder ── SettlementSellBackend ┘                └── OutpostPanelPresenter.RequestSellBatch → OutpostService.TrySettlePlayerCargoBatch
```

- `ResourceSellSession`(순수 C#): 행별 수량(0 ≤ 수량 ≤ 판매 가능 수량), +1/+5/+10 누적, 최대, 최대 선택(일괄 대상만), 선택 초기화, 예상 결과, 확정 재검증, 중복 실행 방지.
- `ResourceSellPopupView`: 기존 개선 팝업(B-133·135)처럼 코드로 만든다. 프레임·패널·폰트는 `MineResetTimedPopupSkin`(기존 그림), 골드·화물 아이콘은 지상 기지 상단 아이콘을 가리키는 새 `Resources/UI/ResourceSellSkin`. 새 이미지 파일은 없다(버튼 각진 면·육각 무늬·금화·기호는 절차 생성).
- 프리팹·씬은 수정하지 않았다. 지상 판매 모달(`SurfaceBasePanel.prefab`)과 콘솔 정산 패널(`OutpostPanel.prefab`)의 예전 내용은 그대로 있고 런타임에 숨긴다. 열기 버튼·E 상호작용·닫기 경로는 기존 것을 그대로 쓴다.

## 화면별로 유지한 규칙
| | 지상 판매창 | 정산 콘솔 |
| --- | --- | --- |
| 열기/이용 조건 | Surface Base 판매 버튼, `SceneSellGate` 허용 시 | E 상호작용, 전력망 연결된 정산 콘솔 근처(기존 판정) |
| 닫기 | X 버튼, X 키(최상위 창) | X 버튼, X 키, 범위 이탈 시 자동 닫힘 |
| 희귀 품목(엔진 연료) | 판매 가능. 최대 선택에서 제외, 직접 수량 지정 | 판매 불가. 행은 보이되 조절 비활성("정산 불가 · 지상에서 판매") |
| 골드 보너스 | 광물별(행 단위) 반올림 — 기존 개별 판매와 같음 | 합계에 한 번 — 기존 화물 전체 정산과 같음 |
| 판매 후 | 창 유지, 경제 자동 저장 요청 1회 | 창 유지, 정산 자동 저장 요청 1회, 결과 형식(SettlePlayerCargo·수량·골드) 유지 → 퀘스트 16 판정 그대로 |

지상 판매창에는 전력·남은 시간을 넣지 않았다. 행의 '받을 골드'는 보너스 전 금액(단가×수량)이고, 보너스가 있으면 요약에 "기본 ○G + 보너스 ○G"로 따로 보이며 판매 버튼 금액은 실제 지급액과 같다.

## 거래 처리
- 판매 버튼 → `Confirm`: 서비스에서 다시 읽어 표시 중 보유량·단가가 바뀌었으면 거래하지 않고 수량을 맞춘 뒤 안내("보유량이 변경되어 판매 수량을 조정했습니다…"). 표시 중 변화는 인벤토리·골드 이벤트(콘솔은 스냅샷 이벤트)와 0.5초 안전 확인으로 즉시 반영된다.
- 여러 자원은 서비스 한 번으로 처리: 전 항목 사전 검증 → `InventoryService.TryReduceMany` 1회 → 골드 지급 1회. 하나라도 실패하면 인벤토리·골드 불변.
- 중복: 세션 `committing` 가드 + 기존 Presenter busy 가드 + 같은 프레임 재입력 무시 + 성공 즉시 수량 0 → 판매 버튼 비활성.
- 실패 시 선택 유지, 성공 연출·골드 증가 표시 없음.

## 연출
- 등장 1.15초(개정): 도는 이모티콘풍 금화 4개가 하나씩 떨어져 쌓임 → 양쪽 끝이 맞닿은 닫힌 창(프레임 모서리 장식 두 조각이 붙은 세로 막대, 내부는 보이지 않음)이 작고 어두운 상태에서 점점 빨라지며 다가옴 → 금화 더미와 부딪히는 순간 금빛이 번쩍이며 금화·빛줄기가 사방으로 터지고 더미는 사라짐 → 양쪽 끝이 좌우로 벌어지며 그 사이를 가운데 가는 선에서부터 세로·가로로 퍼지는 홀로그램(줄무늬·번쩍임, 점점 단단한 패널로 정착)이 채움 → 제목→목록(행 순서)→요약 순서로 표시. 골드 값은 바뀌지 않는다.
- 양쪽 끝(`CapLeft/CapRight`)은 같은 속도로 좌우로 밀려나고, 가운데(`Interior`)는 같은 자리에 고정된 패널을 마스크 폭·높이로 드러낸다. 글자·아이콘·버튼은 별도 레이어라 찌그러지지 않는다. 반동 없음(진행값 ≤ 1 단조 증가, 테스트로 고정). 정착하면 한 장짜리 패널(`Whole`)만 남는다.
- 판매 성공: 판매한 행이 짧게 청록으로 비치고 작은 빛줄기(최대 5개)가 받을 골드 요약으로 흐름 → 상단 골드가 새 값까지 올라감(0.45초). 데이터는 연출 전에 이미 반영. 연속 판매·닫기 시 즉시 정리하고 최신 값 표시.
- 종료 0.36초: 내용이 0.1초에 흐려짐 → 홀로그램이 가운데로 접히며 양쪽 끝이 다시 맞닿음 → 뒤로 물러나며 사라짐. 끝나면 루트가 꺼져 입력 차단·금화·빛이 남지 않는다. 등장·판매 연출 도중 닫기, 종료 도중 다시 열기 모두 처리.
- 버튼(`ResourceSellButton`): 기본 남색/흰 글자/얇은 청록 테두리, 호버·키보드 포커스 = 밝은 청록/짙은 남색 글자/발광 강화, 해제 0.12초 복귀, 누르면 즉시 0.95배로 눌렸다가 0.12초 복귀(기능은 기다리지 않음), 비활성은 어둡고 호버 무시. 값은 hover·press 수준에서 매번 새로 계산해 누적되지 않는다. 판매 버튼은 기본 상태에서도 테두리·발광이 더 강하다.
- 모든 시간은 `unscaledDeltaTime`.

## 변경 파일
수정
- `Scripts/App/Economy/EconomyService.cs` — `TrySellMinerals` 일괄 판매(원자적, 행 단위 보너스, 저장 요청 1회)
- `Scripts/App/Outpost/OutpostService.cs` — `TrySettlePlayerCargoBatch`(원자적, 희귀 거부, 합계 보너스, 정산 ID 중복 방지), 표시용 `PlayerGold`·`GoldGainBonusPercent`
- `Scripts/App/UI/Economy/EconomyPanelPresenter.cs` — `RequestSellBatch`, 표시 이름 공용 함수
- `Scripts/App/UI/Economy/EconomyPanelView.cs`, `EconomyPanelBinder.cs` — 공통 팝업 생성·세션 연결·표시/닫기 위임, 최상위 창 판정
- `Scripts/App/UI/Outpost/OutpostPanelPresenter.cs` — `RequestSellBatch`
- `Scripts/App/UI/Outpost/OutpostPanelView.cs`, `OutpostPanelBinder.cs` — 정산 모드를 공통 팝업으로, X 버튼을 기존 `ClosePanel`에 연결, 자원 아이콘 조회
- `Scripts/App/Integration/IntegrationRuntimeBinder.cs` — 콘솔 판매 창 자원 아이콘 조회 연결(카탈로그 기존 아이콘)
- `Tests/PlayMode/Integration/Bootstrap/UiPlayModeTestSupport.cs` — `LoadSurfaceBase` 테스트 도우미
- `docs/CHANGELOG.md`

신규
- `Scripts/App/UI/Sell/` — `ResourceSellModels`, `ResourceSellSelection`, `ResourceSellQuote`, `ResourceSellSession`, `ResourceSellTimeline`, `ResourceSellPopupView`, `ResourceSellRowView`, `ResourceSellButton`, `ResourceSellArt`, `ResourceSellUi`, `ResourceSellSkin`
- `Scripts/App/UI/Economy/SurfaceSellBackend.cs`, `Scripts/App/UI/Outpost/SettlementSellBackend.cs`
- `Editor/DataValidation/PromptB136ResourceSellSkinBuilder.cs`(메뉴 `SubTerra/UI/Build Prompt-B 136 Resource Sell Skin`) → `Resources/UI/ResourceSellSkin.asset`
- 테스트: `Tests/EditMode/App/UI/PromptB136ResourceSellTests.cs`(31), `Tests/PlayMode/Integration/Economy/PromptB136ResourceSellPlayModeTests.cs`(10)

세이브 DTO·ID·ProjectSettings·패키지·프리팹·씬·폰트는 바꾸지 않았다.

## 검증
- EditMode B-136 31개 통과: +5 누적(2→17), 13개 상한, 0/최대 버튼 비활성, 최대 선택 제외 규칙(지상·콘솔), 초기화, 화면별 보너스 규칙, 무게 기반 화물 예상, 확정 재검증(보유량 감소 시 거래 없이 조정), 실패 시 선택 유지, 거래 중 재진입 Busy·1회 실행, 서비스 일괄 거래 원자성·게이트·저장 1회, 콘솔 희귀 거부·정산 ID 중복, 시간표(0.9~1.3초/0.25~0.4초, 금화 하나씩 쌓임·도는 폭, 충돌 후 폭발, 충돌 뒤에만 벌어짐, 반동 없음, 빛 소멸, 종료 시 다시 맞닿음), 열 정렬, 빈 상태, 스크롤바 조건, 상태기계, 판매 연출 정리, 버튼 상태 비누적.
- PlayMode B-136 10개 통과(실제 SurfaceBase·Mine_Demo_Integration, 실제 마우스·키보드 이벤트, 실제 서비스 값 비교): 실제 클릭으로 수량 조절·여러 자원 판매·골드/인벤토리/화물 무게 일치·저장 요청 1회 / 최대 선택(연료 제외)·초기화·연료 개별 판매·빈 상태 / 표시 중 보유량 변화 즉시 반영·같은 프레임 연속 입력 1회 / 등장 중 X 키·판매 연출 중 닫기·반복 열기 후 입력 차단·효과 잔여 없음 / 1280×720·1920×1080·2560×1440 화면 안·열 정렬·버튼과 금액 비겹침·글자 크기 / 긴 목록에서 목록만 스크롤 / 콘솔: 같은 레이아웃, 희귀 품목 차단, 일괄 정산 결과 형식, 창 유지 / 콘솔 등장 중·열린 상태 범위 이탈, X 키, X 버튼, 범위 안에서 저절로 재열림 없음.
- 전체 실행: EditMode 1249/1250 통과 — 실패 1개 `IntegrationWiringTests.PlayableMineScene_HasRequestedCharacterTerrainAndMiningLayout`(Missing terrain, B-134·135 문서에 기록된 기존 실패). PlayMode 176 통과·2 실패·1 건너뜀 — `GasExposurePlayModeTests.H_F02_F05…`(기존 기록 실패), `BuildingUiPlayModeTests.PromptB71_CPlacesBuildingWhileEnterDoesNot`(이번 자동 실행에서 Unity 창이 OS 전면이 아니라 합성 C 키가 무시됨. 이 테스트 경로에는 변경 파일이 없고, 입력 설정을 포커스 무관으로 임시 교체해 단독 실행하면 통과 — 설정은 원복), `LadderBackBootstrap…`은 격리 세이브 경로 인자가 없어 원래 건너뜀.
- 전체 실행 중 에디터가 다른 프롬프트의 프리팹·Integration Scene·ProjectSettings·폰트·증거 PNG를 다시 저장했다. 이번 작업과 무관해 모두 `git restore`로 되돌렸다.

## 레이아웃 개정 (프레임 모서리 회피·간격 확대)
- 카드 1240x760 → 1400x840. 좌우 여백 72, 상단 여백 48. 프레임 모서리 장식(안쪽 대각선이 모서리에서 약 100)과 제목 막대·X 버튼·요약 막대가 겹치지 않는다.
- X 버튼은 오른쪽 위 모서리에서 (72, 56)만큼 안쪽에 놓고, 골드 표시는 X에서 28 떨어뜨림.
- 행 높이 72, 행 사이 8 → 12, 열 폭 확대(이름 230, 보유 100, 개당 가격 130, 수량 버튼 사이 12, 받을 골드 148).
- 하단 요약: 구역 사이 간격을 모두 32로 맞추고, 예상 골드는 판매 버튼(폭 280, 오른쪽 안쪽 여백 28)에서 40 이상 떨어뜨림.
- 새 레이아웃 검증: `Popup_Layout_CloseClearsFrameCorner_SpacingIsRoomy_AndProjectionIsApartFromSellButton`.
- 연출 프레임 확인: `b136-2-open-sheet*.png`, `b136-2-close-sheet.png`, `b136-2-final-1920x1080.png`. 이 파일들 이전 캡처(`b136-surface-*`, GIF)는 개정 전 레이아웃이다.

## 캡처 (`work_process/MVP2/resource-sell-ui/`)
- `b136-surface-open-adjust-sell-close.gif`, `b136-console-open-adjust-sell-close.gif` — 등장 → 실제 클릭 수량 조절 → 판매 성공 연출 → 종료(연출은 `ManualTick`으로 30fps 고정 진행, GIF는 15fps)
- `b136-surface-selected-1920x1080.png`, `b136-console-selected-1920x1080.png` — 같은 레이아웃, 화면별 규칙(연료 행)
- `b136-surface-1280x720.png`, `b136-surface-2560x1440.png`, `b136-surface-empty-1920x1080.png`, `b136-surface-long-list-scrolled.png`

## 남은 사항·제한
- 현재 판매 품목 데이터는 광물 3종 + 엔진 연료뿐이라 실제 데이터로 4행을 넘는 목록은 생기지 않는다. 긴 목록 스크롤은 같은 팝업에 시험용 데이터를 넣어 확인했다.
- 콘솔은 기존처럼 창 영역만 입력을 막고(월드 클릭은 통과), 지상은 기존 모달처럼 화면 전체를 막는다.
- 콘솔 E 상호작용은 기존 CCTV 테스트처럼 `ToggleInteractionPanel`로 열었다(같은 경로). 실제 E 키 입력 테스트는 하지 않았다.
- 예전 판매 모달·정산 패널의 프리팹 내용은 지우지 않고 숨겼다. 정리하려면 해당 빌더로 별도 작업이 필요하다.
- Windows 빌드 실행은 확인하지 못했다. 커밋은 하지 않았다.
