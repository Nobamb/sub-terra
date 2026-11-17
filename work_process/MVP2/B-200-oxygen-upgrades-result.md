# B-200 산소 업그레이드 2종 (#336) 결과

- 기준 브랜치·커밋: `origin/main` `f324038d1b75b80436e2f5dcf3d7f5db5904e743`
- 만든 브랜치: `MVP2-fix144` (원격에 없음을 `git fetch` 후 확인. `MVP2-fix143`까지 존재)
- 커밋 해시: **미커밋**. 커밋·push는 사용자 확인 대기 중이다(아래 "남은 일").

## 최종 ID·트리

| ID | 표시명 | 부모 | 좌표 | 탭 |
|---|---|---|---|---|
| `upgrade.oxygen.maximum` | 최대 산소 | `upgrade.energy.maximum` Lv.1 | (-450, 0) | 전력·체력·화물 |
| `upgrade.oxygen.efficiency` | 산소 소모 감소 | `upgrade.oxygen.maximum` Lv.1 | (-450, -205) | 전력·체력·화물 |

좌표는 기존 노드와 겹치지 않는다(x=-450 열은 체력 재생과 같고, 최대 전력 왼쪽/가스 저항 왼쪽 칸이 비어 있었다). 노드 13개·연결선 12개.

## 단계·비용·effectValue (초안 그대로. 바꾸지 않음)

| 업그레이드 | Lv.1 | Lv.2 | Lv.3 |
|---|---|---|---|
| 최대 산소 (+가산) | +40 / 구리 6 | +80 / 구리 6, 철 4 | +130 / 철 8, 리튬 5 |
| 산소 소모 감소 (비율) | -15% / 구리 6 | -30% / 구리 8, 철 5 | -45% / 철 10, 리튬 6 |

값은 `MvpDataAssetBuilder.BuildMaximumOxygenUpgrade/BuildOxygenEfficiencyUpgrade`에 있다. 검사기에서 에셋을 고치면 구매·표시 문구가 `UpgradeEffectFormatter`를 통해 따라간다(최대 +40, 소모 -15%).

## 소모 감소를 곱한 위치

`PlayerSurvivalController.AdvanceOxygen`: `workload(#334) × gasMultiplier × (1 - reduction)`를 `PlayerOxygenState.Advance`의 `drainMultiplier`로 넘긴다. 즉 #334 배율과 가스 2배가 곱해진 최종 소모율에 한 번 더 곱한다. 0~10M 회복 분기는 `drainMultiplier`를 쓰지 않아 변하지 않는다. `(baseRate + perMeter × 깊이)` 공식·기본 100/0.5/0.02·#334 상수는 건드리지 않았다.
감소율 상한: 제공자 0.9, 컨트롤러 0.95. 데이터를 잘못 넣어도 소모가 0이 되지 않는다.

## 바꾼 파일

- 상수·문구: `DataIds.cs`, `ItemDisplayNames.cs`, `UpgradeEffectFormatter.cs`, `UpgradeCategory.cs`(`upgrade.oxygen.` → Capacity)
- 계약: `IUpgradeEffectProvider`(+`GetMaximumOxygen`, `GetOxygenDrainReduction`), 신규 `PlayerOxygenContracts.cs`(`IPlayerOxygenUpgradeProvider`)
- 구현: `UpgradeEffectProvider`, `PlayerSurvivalState.cs`(`PlayerOxygenState.ApplyMaximum`), `PlayerSurvivalController`(`BindOxygenUpgradeEffects`), `RunFailureRuntimeController`(최초 바인딩 + 구매 즉시 반영)
- 아이콘 연출 종류: `UpgradeIconFx.cs`에 `MaximumOxygen`/`OxygenEfficiency` 종류 추가(기존 테스트 `IconFx_EveryUpgradeHasItsOwnKind` 요구). 전용 호버 모션은 아래 "필요 아트 / 조정 제안" 참고.
- 에디터: `MvpDataAssetBuilder`, 신규 `PromptB200OxygenUpgradeBuilder`(에셋 2개 생성·카탈로그 등록·해금 값. 기존 에셋은 건드리지 않음), `PromptB118UpgradeTreeBuilder`(노드 2개 + `oxygen` 아이콘)
- 데이터(빌더 산출): `Upgrade_Maximum_Oxygen.asset`, `Upgrade_Oxygen_Efficiency.asset`, `GameDataCatalog.asset`(+2줄)
- UI 산출(빌더 재실행, 손수정 없음): `SurfaceBasePanel.prefab`, `Mine_Demo_Integration.unity`(**씬은 별도 커밋 권장**, 트리 전체가 재생성돼 diff가 크다)
- 테스트: 신규 `PromptB200OxygenUpgradeTests`(14건), 수정 `ProgressionStaticStructureTests`(메서드 10→12), `PromptB118UpgradeTreeTests`(11/10→13/12), `PromptB1181UpgradeWindowTests`(11→13), 가짜 `IUpgradeEffectProvider` 6곳에 새 멤버 추가

기존 업그레이드 ID·효과·비용 변경 없음(`PromptB72` 통과). 새 SFX·`SfxId`·음량 없음. 프리팹 YAML 손수정 없음.

## 테스트 결과

- Unity 컴파일 오류 없음.
- 대상 픽스처(산소 신규·트리·업그레이드 창·B72·진행도·PhaseL 산소·HUD 산소·실패 서비스 등 14개 클래스): **213 통과 / 1 실패**.
- 전체 EditMode: 2781 통과 / 28 실패. 28건 모두 이 작업과 무관한 기존 실패다. 프리팹·씬을 `HEAD` 버전으로 되돌리고 해당 클래스를 다시 돌렸을 때 같은 27건이 그대로 실패했고, 나머지 `PromptB107QuestUiTests...`는 "통합 씬에 저장 안 된 변경이 있음" 예외로 씬 상태에 의존한다.
  - 대표: `PromptB101GoldGainTests.PurchasesAllLevels...`(골드 획득이 `cargo.maximum` Lv.1 해금을 요구하는데 테스트는 드릴 속도 3만 복원), `PromptB125*`(도움말 문구), HUD/메뉴/판매 모달 레이아웃 기대값 등.
- 전체 실행 전후로 씬·프리팹 바이트 비교 동일(오염 없음). 실행이 건드린 범위 밖 파일(폰트, `InventoryPanel/MainMenuPanel/ViewSocket.prefab`, `TimeManager.asset`, `EditorBuildSettings.asset`, 시안 PNG 2개 등)은 `git restore`했다.

## 직접 못 본 항목 — 미검증

- 실제 플레이: 초반 산소 부족 사망 횟수, 최대 산소 구매 시 HUD 게이지 증가, 기지 충전/복귀 충전, 소모 감소 체감, 가스 안·0~10M 회복, 만렙에서 가장 깊은 구간의 압박. (EditMode에서는 `AdvanceOxygen` 직접 호출로 공식·최대 증가 이벤트·회복 불변을 검증함.)
- 1920×1080에서 트리 노드 겹침·잘림, 업그레이드 창에서 잠금 표시, 호버 연출. 이 화면은 눈으로 확인하지 못했다.
- 새 구매가 이어하기 저장에 남는지: 구매 레벨은 기존 `UpgradeState` 저장 필드를 그대로 쓰므로 DTO 변경은 없다. 저장 후 로드 경로는 플레이에서 확인 필요.
- 저장된 산소 잔량이 기본 최대(100)를 넘는 경우(예: 최대 산소 구매 후 140 중 90 저장): 이어하기에서 `RestoreOxygen`이 업그레이드 바인딩(`RunFailureRuntimeController.Bind`)보다 먼저 실행되면 잔량이 100으로 잘릴 수 있다. 두 호출의 실행 순서는 코드로 확인하지 못했다. 플레이에서 확인하고, 순서가 반대면 복원 쪽에서 최대 갱신 뒤 값을 넣도록 고쳐야 한다.

## 사용자 테스트 가이드

`prompt-B-200.md`의 "테스트 (플레이 가이드)" 1~7번과 같다.

## 필요 아트 / 조정 제안

- 새 아트 없이 HUD의 `oxygen-icon.png`("O₂")를 두 노드에 재사용했다. 두 노드가 같은 아이콘이므로 구분이 약하다. 소모 감소 노드용으로 `O₂` 아래 아래 화살표(또는 시계/물방울)를 겹친 아이콘을 요청하면 좋다: 기존 업그레이드 아이콘과 같은 300×300 PNG, 투명 배경, 중앙 정렬 `Art/UI/Upgrade/Icons/upgrade-icon-oxygen-efficiency.png` 한 장.
- 두 노드 전용 호버 연출 추가(2026-11-17 후속 요청). 곡선은 `UpgradeIconFx.Breath`(들숨 40% → 멈춤 8% → 날숨 52%), 재생은 `UpgradeNodeHoverFx`.
  - 최대 산소: 아이콘 뒤에서 흰 광채가 피어나고 O2가 3.2초 주기로 부풀었다(가로 +22%, 세로 +18%) 줄어든다. 날숨이 시작될 때 작은 기포 4개가 떠오른다.
  - 산소 소모 감소: O2가 광부(`player_idle_01`, 새 아트 없음)로 바뀌고 4.6초 주기로 발을 붙인 채 가슴이 오르내린다. 날숨마다 입가에서 숨결 2개가 퍼진다. 호버를 떼면 O2로 돌아온다.
  - 빌더 재실행으로 `SurfaceBasePanel.prefab`·`Mine_Demo_Integration.unity`에 `Body/FxFront/Miner`가 추가됐다. 대상 픽스처 3개 71건 통과. 에디터에서 프레임을 그려 모양은 확인했지만 실제 플레이 호버는 미검증.
- 밸런스: 기본 산소 설정은 바꾸지 않았다. 초반이 너무 널널하면 설정값(`PlayerSurvivalSettings`)이 아니라 `#334` 배율 쪽 조정을 PM과 먼저 상의하는 편이 안전하다.

## 남은 일

- [ ] 사용자 확인 후 `MVP2-fix144`에 커밋(씬 파일은 별도 커밋)·일반 push. PR·브랜치 삭제 없음, #336 수정 없음.
- 위 "미검증" 항목의 플레이 확인.
