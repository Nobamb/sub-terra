# Prompt-B 118 업그레이드 트리 UI 작업 결과

드릴 속도를 중심에 둔 트리 UI로 업그레이드 창을 개편하고, 드릴 속도 레벨에 따른 단계별 해금을 **데이터 + 구매 로직(`ProgressionService.TryPurchase`)**에 반영했다. 기존 업그레이드의 효과·비용·최대 레벨, 저장 ID, 열기/닫기·단축키는 바꾸지 않았다.

> 참고 이미지: 작업 중 전달된 참고 이미지는 내용을 확인할 수 없어(파일 미첨부) 연결 구조는 실제 항목 수에 맞춰 직접 설계했다.

## 최종 해금 조건표

해금 조건은 모두 `upgrade.drill.speed`(드릴 속도) 레벨이다. 최대 레벨(3)은 늘리지 않았다.
`UpgradeData`의 `unlockRequirementUpgradeId / unlockRequiredLevel / treeParentId` 필드에 저장된다.

| 항목 | 해금 조건 | 연결 대상 |
| --- | --- | --- |
| 드릴 속도 | 기본 공개 | (중심) |
| 드릴 전력 효율 | 드릴 속도 Lv.1 | 드릴 속도 |
| 최대 전력 | 드릴 속도 Lv.1 | 드릴 속도 |
| 최대 화물 중량 | 드릴 속도 Lv.1 | 드릴 속도 |
| 최대 체력 | 드릴 속도 Lv.2 | 최대 전력 |
| 가스 저항 | 드릴 속도 Lv.2 | 최대 전력 |
| 채굴 수확량 | 드릴 속도 Lv.2 | 최대 화물 중량 |
| 드론 스캔 범위 | 드릴 속도 Lv.2 | 드릴 속도 |
| 초당 체력 재생 | 드릴 속도 Lv.3 | 최대 체력 |
| 골드 획득 | 드릴 속도 Lv.3 | 최대 화물 중량 |
| 드론 구조 보존 | 드릴 속도 Lv.3 | 드론 스캔 범위 |

- 단계별 공개 수: Lv.0 → 1개, Lv.1 → 4개, Lv.2 → 8개, Lv.3 → 11개(전부).
- 부모 노드의 해금 단계는 항상 자식보다 같거나 빠르다(테스트로 고정).

### 진행 막힘이 없다는 근거 (실제 코드 확인)

- 드릴 속도 비용: Lv.1 구리 8 / Lv.2 구리 10·철 6 / Lv.3 철 12·리튬 8 — 광물만 쓰고 골드·다른 업그레이드가 들어가지 않는다.
- 채굴 조건(`requiredDrillLevel`): 구리 0, 철 1, 리튬 2. 구리(깊이 1~15) → Lv.1 → 철(16~35) → Lv.2 → 리튬(36~40, 심층)로 드릴 속도만으로 순서대로 마련된다.
- 심층 구역 해금 규칙(`DeepZoneUnlockRule.Mvp`)은 완료 목표 13개 + 드릴 속도 Lv.2뿐이며, 18개 퀘스트 중 다른 업그레이드 구매를 요구하는 항목은 없다.
- 따라서 잠긴 업그레이드 없이 드릴 Lv.3까지 도달 가능하고, 그 시점에 11개 전부 해금된다. 순환 조건은 `CatalogValidator`(→ `UpgradeUnlockRules.Validate`)가 오류로 막는다.

## 구현 요약

| 영역 | 내용 |
| --- | --- |
| 데이터 | `UpgradeData`에 해금 필드 3개 추가(기본값=조건 없음). 11개 에셋에 값만 추가, 효과·비용·ID 불변 |
| 해금 판정 | `UpgradeUnlockRules`(순수 C#): 구매 이력(레벨≥1)이 있으면 새 조건 때문에 다시 잠기지 않음. 상태는 레벨에서 매번 계산 → **세이브 필드·버전 변경 없음** |
| 구매 검증 | `TryPurchase`에서 비용 검증·차감보다 먼저 해금 검사 → 미해금이면 `Locked` 실패, 차감 없음 |
| UI 표시 | `UpgradeSnapshot`에 `IsUnlocked / LockedReason / NextCostShortages` 등 추가. UI와 서비스가 같은 규칙 사용 |
| 신규 해금 정보 | 구매 성공 결과 `NewlyUnlockedUpgradeIds`(연출 전용, 저장 안 함) |
| 부족량 | App 내부 `IResourceBalanceProvider`(EconomyService 구현). Shared 계약은 변경하지 않음 |
| 효과 표기 | `UpgradeEffectFormatter`: 채굴 속도 +25%, 전력 소모 -20%, 최대 화물 +30kg, 스캔 반경 3칸 등 실제 명칭·단위 |
| UI | `UpgradeTreeView / UpgradeTreeNodeView / UpgradeTreeConnector / UpgradeTreeTween`, `IProgressionPurchaseFeedbackView`. 기존 `ProgressionPanelView`는 `treeView`가 연결되면 트리 모드로 위임 |
| Builder | `PromptB118UpgradeTreeBuilder` (메뉴 3개) |

### 빌더 메뉴

- `SubTerra/Data/Apply Prompt-B 118 Upgrade Unlock Rules` — 해금 조건표를 UpgradeData 에셋에 기록
- `SubTerra/UI/Build Prompt-B 118 Upgrade Tree (Surface Base)` — `SurfaceBasePanel.prefab`의 `UpgradeModal/ProgressionPanel`만 수정
- `SubTerra/UI/Build Prompt-B 118 Upgrade Tree (Mine Integration)` — `Mine_Demo_Integration.unity`의 `UpgradePanel`만 수정

기존 탭·목록·상세 텍스트 오브젝트는 삭제하지 않고 비활성으로 남겼다(기존 테스트·참조 보존). **주의: `PromptB117SurfaceBaseBuilder` 를 다시 실행하면 업그레이드 모달이 탭 UI로 되돌아가므로, 실행 후 위 Surface Base 트리 빌더를 다시 실행해야 한다.**

## UI 구성

- 트리(왼쪽 주 영역) + 상세 패널(오른쪽 430px). 노드는 업그레이드당 1개이며 아이콘·이름·Lv.n/max를 표시한다. 패널 크기에 맞춰 균일 비율로 자동 맞춤(`FitContent`).
- 레이아웃: 중앙 드릴 속도, 위(드릴 전력 효율) / 오른쪽(화물 계열) / 왼쪽(전력·체력·가스) / 아래(드론). 연결선은 겹치지 않는 직각선이며 미해금 노드에도 위치·관계는 표시된다.
- 미해금: 어두운 실루엣 + `?` + 자물쇠. 이름·아이콘·레벨 텍스트는 비워 두고, 상세 패널은 `???` + `드릴 속도 Lv.N 필요`만 표시한다(툴팁은 만들지 않음).
- 상태 구분(색 + 문구/모양): 구매 가능(청록 테두리 + 우상단 다이아몬드, "구매 가능"), 재료 부족(톤 다운, "재료 부족" + 자원별 부족량), 최대(금색 테두리 + `MAX`, 버튼 대신 "최대 레벨 달성"), 미해금.
- 효과는 `현재 ▶ 다음` 두 열로 배치한다. 비용 줄은 부족한 자원만 경고색 + `(N 부족)`.
- 아이콘: 기존 `hud-icons.png`에서 아이콘만 잘라 `Art/UI/Upgrade/Icons/`에 개별 PNG로 저장(원본 미수정), 나머지는 기존 `icon_copper / icon-reset / ground_gas / digger_bot` 재사용. 판 모양(`upgrade-node-plate.png`, 모서리 절단 9-slice)과 발광(`upgrade-glow.png`)은 빌더가 코드로 생성했다. 이미지 생성 AI는 사용하지 않았다.

### 상태별 애니메이션 (모두 `unscaledDeltaTime`, 연출은 서비스 결과에만 연결)

| 상태 | 구현 | 시간 |
| --- | --- | --- |
| 비용 부족 | 노드 좌우 흔들림(감쇠) + 테두리 경고색 복귀, 비용 줄·상태 문구 경고 강조 후 복귀 | 0.3s / 0.35s |
| 레벨업 성공 | 청록빛이 테두리 4변을 한 바퀴, 발광 펄스, 1.08배 확대 후 복귀, 레벨 표기 팝 | 0.65s |
| 신규 해금 | 연결선 청록빛 흐름(0.22s) → 자물쇠 진동 → 파손(파편 6개) → 블라인드 걷힘 → 청록 점등. 동시 해금은 0.08s 시차 | ≈0.9s |

- 재시도·연타 시 코루틴을 교체하며 위치·크기·색을 매 프레임 절대값으로 계산하므로 변형이 누적되지 않는다. 창 닫기(`OnDisable`)에서 즉시 원상 복구하고, 재개방·로드 시에는 현재 데이터로만 그려 해금 연출을 반복하지 않는다.
- 장식 요소(글로우·블라인드·자물쇠·파편·테두리 막대)는 전부 `raycastTarget=false`이며 노드 루트만 입력을 받는다(테스트로 고정).

## 변경 파일

수정: `UpgradeData.cs`, `CatalogValidator.cs`, `EconomyService.cs`, `ProgressionService.cs`, `ProgressionPurchaseResult.cs`, `UpgradeSnapshot.cs`, `ProgressionPanelPresenter.cs`, `ProgressionPanelView.cs`, `Data/Upgrades/Upgrade_*.asset` 11개, `SurfaceBasePanel.prefab`, `Mine_Demo_Integration.unity`(UpgradePanel 하위만), `PromptB101GoldGainTests.cs`

추가: `UpgradeUnlockRules.cs`, `UpgradeEffectFormatter.cs`, `IResourceBalanceProvider.cs`, `IProgressionPurchaseFeedbackView.cs`, `UpgradeTreeView/NodeView/Connector/Tween.cs`, `PromptB118UpgradeTreeBuilder.cs`, `PromptB118UpgradeTreeTests.cs`, `Art/UI/Upgrade/*`(PNG 8개 + 메타)

`Mine_Demo_Integration.unity`에는 노드 약 1,400개 직렬화 문서가 추가되어 diff가 크다. **규칙에 따라 씬 변경은 기능 커밋과 분리해 커밋**해야 한다.

## 검증 결과

- EditMode 전체: Pass 921 / Fail 17. 실패 17건은 이번 변경과 무관한 기존 실패다(`GameDataCatalogTests` 10건: 테스트 카탈로그가 `upgrade.cargo.gold` 누락, `EconomyPanelPresenterSellTests`("Copper" vs "구리"), `IntegrationWiringTests`·`HudPanelChromeLayoutTests`(BasicHUD 수치), `PromptB104SettingsMenuTests`, `PromptB42UiLayerTests` 2건, `SellPanelPrefabStructureTests`(HEAD에서도 동일 문자열 없음)).
- 이번 작업으로 영향받은 기존 테스트 1건(`PromptB101GoldGainTests`)은 "골드 획득은 드릴 속도 Lv.3 해금" 전제를 반영해 수정했다.
- 신규 `PromptB118UpgradeTreeTests`(29케이스): 새 게임 해금 상태, Lv.0~3 단계별 개수, 전 항목 도달 가능, 부모≤자식 단계, 데이터-빌더 표 일치, 기존 세이브(구매 이력) 유지, 미해금 구매 차단(무차감), 신규 해금 목록, 부족량, 순환/미지 조건 검증 거부, 효과 단위 표기, 프리팹의 노드 11개·연결선 10개, 미해금 노드 정보 비노출, 레이캐스트 대상 1개.
- Game View 확인(Play 모드, 4K 캡처): 새 게임 상태(중심 노드 + 나머지 블라인드 + 상세 패널), 드릴 Lv.1 구매 직후 연출 프레임(발광·블라인드 걷힘·연결선 점등)과 3개 해금, 재료 부족 상태(경고색·부족량), 전부 해금 + 선택 노드, 미해금 노드 선택(`???` + 조건만), 최대 레벨(MAX + "최대 레벨 달성"), `Time.timeScale=0`에서 연출 재생. 지상 기지 모달은 프리팹을, 광산 창은 씬의 UpgradePanel 사본을 격리된 캔버스에서 확인했다.
- `IntegrationRuntimeBinder.progressionPanelBinder` 참조 유지 확인(재빌드 후 `UpgradePanel`, 트리 모드 노드 11·연결선 10).

## 남은 제한

- Bootstrap → 새 게임 → 실제 구매까지의 전체 플레이 스모크는 사용자의 실제 세이브 슬롯 보호를 위해 수행하지 않았다. 병합 전 "Bootstrap → 새 게임/이어하기 → 업그레이드(U/지상 기지 버튼) → 드릴 속도 구매 → 저장 후 재실행"을 한 번 확인해 주세요.
- PlayMode 테스트 일괄 실행은 이 환경에서 결과 파일이 생성되지 않아 확인하지 못했다.
- 참고 이미지를 볼 수 없어 연결 구조는 자체 설계다. 노드 좌표는 `PromptB118UpgradeTreeBuilder.Nodes`에서 한 곳으로 조정한다.
- 퀘스트 `UnlockDeepZone`의 설명 문구는 "드론 스캔 2레벨, 가스 저항 1레벨"을 언급하지만 실제 규칙(`DeepZoneUnlockRule.Mvp`)은 드릴 속도 Lv.2뿐이다(기존 불일치, 이번 범위 밖이라 수정하지 않음).
- 가스 저항 아이콘은 가스 지형 타일 이미지를 재사용했다. 전용 아이콘이 생기면 `Art/UI/Upgrade/Icons`와 빌더의 `LoadIcon`만 바꾸면 된다.
