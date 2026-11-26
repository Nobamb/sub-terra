# #127 건설창 한·영 문안 및 HUD·Digger-Bot 누락 조사

PR #390의 `fbe9e4a6` 기준 문안입니다. 표시 문구와 연결 위치를 정리했으며, 런타임에는 아직 연결하지 않았습니다. Unity 화면과 줄바꿈은 미검증입니다.

## 건설창 공통 문구

| 위치 | 한국어 | English |
| --- | --- | --- |
| 메인 제목 | 시설 건설 | Build Facilities |
| 목록 제목 | 시설 목록 · {count} | Facilities · {count} |
| 상세 제목 | 시설 상세 | Facility Details |
| 상세에서 목록으로 | < 시설 목록 | < Facilities |
| 설명 캡션 | 기능 | Function |
| 운영 조건 캡션 | 설치 조건 | Placement Condition |
| 충전기·보건소·정산기 조건 | 전진기지 범위 내 | Within Outpost Range |
| 검색창 | 시설명 검색 | Search facilities |
| 검색 결과 없음 | 맞는 시설이 없습니다. | No facilities match. |
| 정렬 옵션 | 기본 순 / 기본 역순 / 가나다순 / 가나다 역순 | Default / Reverse Default / A–Z / Z–A |
| 자원 표 제목 | 건설 비용 · 보유 / 필요 | Build Cost · Owned / Required |
| 구형 UI 자원 표 | 필요 자원 | Required Resources |
| 단독 비용 라벨 | 비용 | Cost |
| 미선택 안내 | 시설을 선택하세요. | Choose a facility. |
| 미선택 상세 안내 | 왼쪽 목록에서 설치할 시설을 선택하세요. 용도, 필요 자원과 설치 조건이 여기에 표시됩니다. | Choose a facility on the left to see its purpose, costs and placement conditions. |
| 구형 UI 열기·닫기 | [B] 열기 / 닫기 | [B] Open / Close |
| 구형 UI 설치 조작 | 좌클릭 설치 / C 근접 설치 / B 닫기 | Left Click: Place / C: Place Nearby / B: Close |
| 구형 UI 취소 | 건설 취소 | Cancel Build |

고정 라벨은 [PromptB188BuildingModeBuilder](../sub-terra/Assets/_Project/Editor/DataValidation/PromptB188BuildingModeBuilder.cs)와 구형 [PromptB110BuildingMenuBuilder](../sub-terra/Assets/_Project/Editor/DataValidation/PromptB110BuildingMenuBuilder.cs)에 있습니다. 런타임 갱신은 [BuildingMenuView](../sub-terra/Assets/_Project/Scripts/App/UI/Building/BuildingMenuView.cs)에서 처리하고, [BuildingCatalogSort](../sub-terra/Assets/_Project/Scripts/App/UI/Building/BuildingCatalogSort.cs)의 정렬 이름을 표시 순서와 맞춥니다. 언어 전환 중 정렬 모드, 검색 입력, 선택된 시설 ID는 보존합니다. A–Z와 Z–A는 영어로 표시된 시설명 기준입니다.

## 시설 이름과 설명

영어 이름은 [ItemDisplayNames.LocalizedBuilding](../sub-terra/Assets/_Project/Scripts/App/Core/Data/ItemDisplayNames.cs#L26)과 같습니다. 짧은 설명은 목록 카드와 선택 요약에 쓰고, 긴 설명은 상세 화면에 씁니다. 실제 재료와 수량은 실행 시 카탈로그에서 읽습니다.

| 시설 | English | 짧은 설명 KO / EN | 상세 설명 KO / EN |
| --- | --- | --- | --- |
| 완충판 | Cushion Pad | 착지 피해 완화 / Reduces fall damage | 착지 피해를 일반 피해의 약 10%로 줄입니다. 위에 서서 위·아래 방향키로 높이를 조절합니다. / Reduces fall damage to about 10% of normal. Stand on the pad and press Up or Down to adjust its height. |
| 사다리 | Ladder | 수직 갱도 오르내리기 / Climb vertical shafts | 수직 갱도에서 위아래로 이동합니다. 세로 5칸을 설치합니다. / Climb up and down vertical shafts. Places a five-tile section. |
| 조명 | Light | 지하 구역을 밝힘 / Lights underground areas | 전력이 연결된 지하 구역을 밝힙니다. / Lights an underground area when powered. |
| 충전기 | Charger | 전력 충전 / Restores power | 전력망에 연결되면 플레이어 장비를 충전합니다. / Recharges your equipment when connected to the power grid. |
| 보건소 | Clinic | 체력 회복 / Restores health | 전력망에 연결되면 플레이어 체력을 최대치까지 회복합니다. / Restores you to full health when connected to the power grid. |
| 보관함 | Storage Chest | 광물 보관 / Stores minerals | 탐사 중 수집한 광물을 맡기거나 꺼냅니다. / Deposit and withdraw minerals collected during expeditions. |
| 자원 정산기 | Settlement Terminal | 광물 판매 / Sells minerals | 들고 있는 광물을 판매해 골드를 얻습니다. / Sell carried minerals for gold. |
| 전진기지 | Outpost | 전력 공급과 가스 정화 / Supplies power and purifies gas | 연결된 시설에 전력을 공급하고 가스 정화 구역을 형성하며 탐사 체크포인트가 됩니다. / Powers connected facilities, creates a gas purification zone and serves as a checkpoint. |
| 긴급 탈출 포탈 | Emergency Escape Portal | 긴급 이동 / Emergency transport | E로 목적지 선택 창을 엽니다. {gold}G와 최대 전력의 {powerPercent}%를 내고 엘리베이터나 설치한 전진기지로 이동합니다. / Press E to choose a destination. Spend {gold}G and {powerPercent}% of maximum power to travel to the elevator or a built outpost. |

원문은 [시설 카탈로그](../sub-terra/Assets/_Project/Data/Buildings)에 있습니다. 정산기의 에셋에는 ‘보관한 광물’이 남아 있어 문안은 [정산 창의 실제 화물 경로](../sub-terra/Assets/_Project/Scripts/App/UI/Outpost/SettlementSellBackend.cs)에 맞췄습니다. 포탈의 {gold}와 {powerPercent}는 [EmergencyEscapeService](../sub-terra/Assets/_Project/Scripts/App/Run/EmergencyEscapeService.cs#L29)의 값을 씁니다. ‘전진기지 범위 내’는 현재 건설창 조건에 대한 번역이며, 전력망 연결이나 게임 규칙 조정이 필요한지는 별도로 확인합니다.

## 비용과 설치 불가 문구

| 상태 또는 표시 위치 | 한국어 | English |
| --- | --- | --- |
| 비용 없음 | 비용 없음 | No Cost |
| 목록 비용 요약 | 구리 {required} | Copper {required} |
| 보유·필요 수량 | {owned} / {required} | {owned} / {required} |
| 충분 | 충분 | Enough |
| 부족 표시 | 부족 / {missing} 부족 | Short / {missing} short |
| 부족 상세 단일 예 | 구리 1개가 더 필요합니다. | Need 1 more Copper. |
| 부족 상세 복수 예 | 구리 1개, 철 2개가 더 필요합니다. | Need 1 more Copper and 2 more Iron. |
| 설치 가능 | 설치 가능 | Ready to Place |
| 자원 부족 | 자원 부족 / 자원이 부족합니다. | Not Enough Resources / You need more resources. |
| 위치 확인 | 설치 위치 확인 / 설치 가능한 위치로 커서를 옮기세요. | Check Placement / Move the cursor to a valid tile. |
| 시설 미선택 | 시설 미선택 / 시설을 고르면 설치 상태를 확인합니다. | No Facility Selected / Choose a facility to check placement. |
| 선택 또는 미리보기 실패 | 선택할 수 없는 시설입니다. / 건설 미리보기를 시작할 수 없습니다. | This facility cannot be selected. / Could not start the placement preview. |
| 지형·시설과 겹침 | 다른 시설이나 지형과 겹칩니다. 빈 공간을 선택하세요. | Overlaps terrain or another facility. Choose an empty space. |
| 승강로 | 엘리베이터 이동 공간입니다. 승강로 밖을 선택하세요. | This is the elevator shaft. Choose a spot outside it. |
| 지면 없음 | 시설을 지지할 지면이 없습니다. 바닥 위를 선택하세요. | This facility needs ground beneath it. Choose a spot above solid ground. |
| 거리 초과 | 플레이어에게서 너무 먼 위치입니다. 가까이 이동하세요. | Too far away. Move closer. |
| 건설 제한 구역 | 건설이 허용되지 않은 구역입니다. 구역 안을 선택하세요. | Building is restricted here. Choose a spot in the allowed area. |
| 시설 데이터 오류 | 시설 데이터가 올바르지 않습니다. | Facility data is invalid. |
| 시설 생성 실패 | 시설 생성에 실패했습니다. | Could not create the facility. |
| 자원 차감 실패 | 비용 차감에 실패했습니다. | Could not deduct the build cost. |
| 자원 상태 미확인 | 건설 자원 상태를 확인할 수 없습니다. | Could not check available resources. |
| 미선택 또는 기타 실패 | 선택된 시설이 없습니다. / 시설 설치에 실패했습니다. | Select a facility first. / Could not place the facility. |

비용 및 헤더는 [BuildingMenuDisplayFormatter](../sub-terra/Assets/_Project/Scripts/App/UI/Building/BuildingMenuDisplayFormatter.cs#L63), 실패 사유와 경고는 [BuildingMenuPresenter](../sub-terra/Assets/_Project/Scripts/App/UI/Building/BuildingMenuPresenter.cs#L296)에서 표시합니다. 부족량은 `required - owned`로 계산하고, 부족한 자원만 경고색으로 표시합니다. 설치 가능·불가와 비용 차감은 기존 판정 및 사유 ID를 사용합니다. 설치한 시설을 옮길 때 나오는 안내는 [GameplayBuildingPlacementBridge](../sub-terra/Assets/_Project/Scripts/App/Integration/GameplayBuildingPlacementBridge.cs) 경로에 있으며 별도 연결 범위입니다.

## 산소 HUD·Digger-Bot 표시 경로

| 화면 | 확인한 경로 | 문안 및 연결 범위 |
| --- | --- | --- |
| 산소 게이지 | [HUD 생성기](../sub-terra/Assets/_Project/Editor/DataValidation/PromptB198HudOxygenGaugeBuilder.cs#L78) → [BasicHudView](../sub-terra/Assets/_Project/Scripts/App/UI/HUD/BasicHudView.cs) → [HudFormatter](../sub-terra/Assets/_Project/Scripts/App/UI/HUD/HudFormatter.cs#L59) | `산소 / Oxygen`. 언어 전환 시 라벨을 갱신하고, 현재량·최대량 표기 `72 / 100`을 유지합니다. |
| 산소 위험도 | [IntegrationRuntimeBinder.RenderOxygenRisk](../sub-terra/Assets/_Project/Scripts/App/Integration/IntegrationRuntimeBinder.cs#L1238) → [StructuralHudView.SetOxygenRisk](../sub-terra/Assets/_Project/Scripts/App/UI/HUD/StructuralHudView.cs#L40) | `산소 위험도 안전 / Oxygen: Safe`, `산소 위험도 주의 / Oxygen: Caution`, `산소 위험도 위험 / Oxygen: Critical`. 현재 위험 단계가 바뀌지 않아도 언어 변경 직후 다시 표시합니다. |
| 드론 터미널 | [DroneAnalysisService](../sub-terra/Assets/_Project/Scripts/App/Drone/DroneAnalysisService.cs) → [DroneReasonDisplayFormatter](../sub-terra/Assets/_Project/Scripts/App/UI/Drone/DroneReasonDisplayFormatter.cs) → [DroneDialoguePanelView](../sub-terra/Assets/_Project/Scripts/App/UI/Drone/DroneDialoguePanelView.cs#L203) | 추천 행동과 이유를 함께 번역합니다. `가스 구역 이탈 / Leave the Gas Zone`, `기지로 귀환 / Return to Base`, `충전 / Recharge`, `전진기지 설치 / Build an Outpost`. |
| 드론 말풍선 | [DroneAnalysisService](../sub-terra/Assets/_Project/Scripts/App/Drone/DroneAnalysisService.cs) → [TemplateDialogueGenerator](../sub-terra/Assets/_Project/Scripts/App/Drone/Dialogue/TemplateDialogueGenerator.cs)·[대사 에셋](../sub-terra/Assets/_Project/Data/Dialogue) → [DroneDialogueSocket](../sub-terra/Assets/_Project/Scripts/App/UI/Drone/DroneDialogueSocket.cs) | 템플릿과 `{action}`, `{reason}`, `{safetyGuidance}` 등 치환 문구를 같은 언어로 만듭니다. `가스 위험 {risk}. 가스 구역에서 벗어나세요. / Gas risk: {risk}. Leave the gas zone.`, `현재 전력 {currentEnergy}, 귀환 예상 {returnEnergyEstimate} / Power: {currentEnergy}. Estimated return cost: {returnEnergyEstimate}.`, `현재 귀환 경로를 사용할 수 없습니다. / The return route is currently unavailable.` |

터미널과 말풍선은 서로 다른 표시 경로이므로 한쪽만 바꾸면 혼용이 남습니다. 숫자·템플릿 ID·추천 순위·경고 발동 여부는 기존 값을 사용합니다. 산소 위험도는 기존 HUD 표시를 번역하는 작업이며, 드론이 자동으로 경고창을 여는 [#351](https://github.com/Nobamb/sub-terra/issues/351)은 별도 기능입니다.

## 코드 연결 후 확인할 화면

1. 건설창을 연 상태에서 영어·한국어를 전환해 목록, 상세, 검색과 정렬 표시가 갱신되고 선택한 시설이 유지되는지 확인합니다.
2. 구리만 부족한 경우, 구리와 철이 모두 부족한 경우, 자원은 충분하지만 설치 위치가 막힌 경우를 비교합니다. 영문 설명과 포탈 비용의 줄바꿈을 확인합니다.
3. 산소 위험 단계가 유지된 상태에서 언어를 바꾸고, 같은 가스·전력·귀환 상황의 터미널과 말풍선을 비교합니다.
