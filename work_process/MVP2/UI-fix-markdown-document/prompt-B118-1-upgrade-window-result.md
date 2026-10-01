# Prompt-B 118-1 업그레이드 창 통일·프레임·호버 연출 작업 결과

지상 기지 업그레이드 창을 기준으로 지하(광산) 업그레이드 창을 같은 모양으로 맞추고, 설정창 형태의 외부 프레임, 설정창과 같은 정사각 X 버튼, 노드 호버 빛·아이콘 연출, 스크롤 확대/축소를 넣었다. 업그레이드 효과·비용·해금 규칙·저장 데이터는 바꾸지 않았다.

## 반영 내용

| 요청 | 구현 |
| --- | --- |
| 지하 창을 지상 창 기준으로 통일 | `Mine_Demo_Integration`의 `UpgradePanel`을 화면 96% 스트레치 → 지상과 같은 1500×820 중앙 창으로 변경. 같은 프레임·닫기 버튼·구매 버튼(설정창 버튼 그림 + 호버 연출) 적용 |
| 설정창과 비슷한 외부 프레임, 기존 레이아웃·크기 비율 유지 | 설정창 그림(`setting-menu-asset.png`)에서 안쪽 섹션 박스·아이콘·장식선만 지운 `upgrade-window-frame.png`를 9-slice로 사용. 창 크기(1500×820)와 트리/상세 배치는 그대로 |
| 노드 호버 시 가운데 은은한 청록 빛 + 아이콘 연출 | `UpgradeNodeHoverFx`(노드별). 잠긴 노드는 블라인드 위 청록 빛만 표시 |
| 스크롤 위=확대, 아래=축소 | 창(지상은 어두운 배경 포함) 위 스크롤 → `UpgradeTreeView.HandleScroll`. 포인터 지점을 기준으로 0.7~2.2배, 트리 영역은 `RectMask2D`로 잘라 상세 패널을 가리지 않음. 창을 다시 열면 1배로 초기화 |
| X 버튼을 설정창 버튼 형태, 기존 세로 길이 기준 정사각형 | 기존 72×58 → 58×58. 설정창 `SettingsClose`와 같은 그림·호버 오버레이(청록 X) |
| "심층 구역 · 해금됨" 문구 제거 | 트리 하단 `DeepZoneText` 생성 제거 |

### 아이콘별 호버 연출

| 항목 | 연출 |
| --- | --- |
| 드릴 속도 | 아래 방향 화살표가 아래로 찍고 올라오는 동작 2회 |
| 드릴 전력 효율 | 플러그가 깜빡이며 점등(점등 그림 겹침) + 단자 주변 정전기 |
| 최대 화물 중량 | 닫힌 상자 뚜껑이 열리고 구리→철→리튬이 들어가며 투명해짐, 이후 닫힘 |
| 채굴 수확량 | 구리 주변으로 구리 파편이 두 번 튐 |
| 골드 획득 | 동전 여러 개가 튀며 투명해짐 |
| 드론 스캔 범위 | 드론 중심에서 청록 고리가 퍼져 나감(호버 중 반복) |
| 드론 구조 보존 | 화물 연출과 같은 방식으로 구리·철·리튬이 드론 안으로 들어감 |
| 가스 저항 | **아이콘 교체**: 반투명 녹색 가스 속 캐릭터. 호버 시 장갑 낀 손으로 입을 막음 |
| 최대 전력 | 번개 주변 청록 빛 + 둘레 정전기 |
| 최대 체력 | 하트가 한 바퀴 돌며 살짝 튀어오르고 붉은 회복 빛·(+) 표시 |
| 초당 체력 재생 | '새 광산 초기화'와 같은 회전 곡선으로 한 바퀴 + 붉은 회복 빛·(+) 표시 |

- 모든 시간은 `unscaledDeltaTime`(창이 게임을 멈춰도 재생). 연출 값은 경과 시간의 순수 함수(`UpgradeIconFx`)라 누적 변형이 없고, 포인터가 나가거나 창이 닫히면 원래 위치·크기·투명도로 복구된다.
- 연출 이미지는 전부 `raycastTarget=false`라 노드 클릭·구매 버튼을 가리지 않는다(노드당 입력 대상 1개 유지, 테스트로 고정).

## 아트

이미지 생성 AI는 쓰지 않았다. 기존 프로젝트 아트를 가공하거나 코드로 그렸고, 스크립트는 `b118-1-art/make_upgrade_art.py`에 있다.

- `Art/UI/Upgrade/upgrade-window-frame.png`: 설정창 프레임에서 안쪽 요소 제거(9-slice 경계 L200·B330·R200·T290, 표시 배율 1.34)
- `Icons/upgrade-icon-gas.png`, `upgrade-icon-gas-hand.png`: 플레이어 Idle 흉상 + 가스 구름, 피격 프레임의 주먹을 입 위치에 맞춤
- `Icons/upgrade-icon-box-body.png`, `upgrade-icon-box-lid.png`: 기존 화물상자를 몸통/뚜껑으로 분리하고 열린 입구를 그림
- `Icons/upgrade-icon-plug-lit.png`: 플러그 점등 상태
- `Fx/upgrade-fx-spark|ring|shard|coin|plus.png`: 정전기·스캔 고리·구리 파편·동전(기존 동전 아이콘에서 분리)·회복 표시

## 변경 파일

- 추가: `UpgradeNodeHoverFx.cs`, `UpgradeIconFx.cs`(연출 종류·시간 곡선), `UpgradeTreeZoom.cs`(확대 계산), `UpgradeTreeScrollForwarder.cs`, `PromptB1181UpgradeWindowBuilder.cs`, `PromptB1181UpgradeWindowTests.cs`, 위 아트 PNG
- 수정: `UpgradeTreeView.cs`(확대/축소), `ProgressionPanelView.cs`(`IScrollHandler`), `PromptB118UpgradeTreeBuilder.cs`(118-1 단계 호출, 심층 문구 제거, 아이콘 pivot 중앙화), `PromptB117SurfaceBaseBuilder.cs`(`StyleButton` 접근자만 internal), `SurfaceBasePanel.prefab`, `Mine_Demo_Integration.unity`(UpgradePanel 하위만)

### 빌더 메뉴

- `SubTerra/UI/Build Prompt-B 118-1 Upgrade Window (Surface Base)` / `(Mine Integration)` — B-118 트리 빌드에 118-1 단계가 포함되어 있어 B-118 메뉴로 빌드해도 결과가 같다.
- **주의**: `PromptB117SurfaceBaseBuilder`를 다시 실행하면 업그레이드 모달이 되돌아가므로 이후 118-1(Surface Base) 빌더를 다시 실행해야 한다.

## 검증

- 신규 `PromptB1181UpgradeWindowTests`(21케이스): 확대/축소 계산(방향·한계·포인터 고정·가운데 정렬), 아이콘 종류 매핑과 곡선, 지상 창 프레임·크기·정사각 X·심층 문구 없음·노드별 호버·입력 대상, 가스 아이콘·손, 화물상자 열림/복구, 드릴 화살표 이동/복구, 잠긴 노드는 빛만, 스크롤 전달(창·배경), 지하 창이 지상 창과 같은 구성인지.
- 관련 묶음(118-1, 118, 117, 56, 101, 업그레이드 패널 정적 테스트): 87/87 통과.
- EditMode 전체: Pass 942 / Fail 17. 실패 17건은 B-118 결과 문서에 적힌 기존 실패와 동일(이번 변경과 무관).
- `IntegrationRuntimeBinder.progressionPanelBinder` → `UpgradePanel` 유지, 나머지 참조도 HEAD와 동일.
- 캡처(편집기 격리 캔버스, `evidence/prompt-b118-1/`): 지하 창(`mine-window.png`), 확대 상태(`surface-window-zoom.png`), 11개 아이콘 연출을 0.1/0.25/0.45/0.7/1.0/1.3초로 찍은 필름스트립(`hover-filmstrip-1/2.png`).

## 남은 제한

- 실제 Play 모드에서 마우스를 올려 보는 확인은 하지 않았다(편집기에서 시간을 직접 진행시켜 프레임을 렌더링해 확인). Bootstrap → 지상 기지 업그레이드 창 / 광산 U 창에서 호버·스크롤·X 버튼을 한 번 확인해 주세요.
- 지하 창에는 지상 기지 모달의 어두운 전체 화면 배경을 넣지 않았다. 광산에서는 다른 창과 함께 열 수 있어 입력 차단 범위를 바꾸지 않기 위해서다. 스크롤 확대도 지하에서는 창 위에서만 동작한다.
