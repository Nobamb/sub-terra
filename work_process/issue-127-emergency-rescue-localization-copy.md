# #127 긴급 구출 UI 한·영 문안

`main`의 `a5b70969`와 다국어 PR #390의 `5ff1b7f4`에서 긴급 구출의 표시 경로를 대조했습니다. 영어 제목 `Emergency Rescue`와 행동 `Request Rescue`는 #390의 가이드 용어에 맞췄습니다.

## 표식·확인창·드론 안내

| 표시 위치와 조건 | 한국어 | English |
| --- | --- | --- |
| 캐릭터 머리 위 표식 | 구출 요청 | Request Rescue |
| 확인창 제목 | 긴급 구출 요청 | Emergency Rescue |
| 확인창 본문 | 구출을 요청하면 엘리베이터로 이동하며 아래 자원이 차감됩니다. | Requesting rescue returns you to the elevator and deducts the resources listed below. |
| 확인 버튼 | 구출 요청 | Request Rescue |
| 닫기 버튼 | 닫기 | Close |
| 드론의 재안내 | 구출이 필요하면 머리 위 버튼을 누르거나 R 키를 누르세요. | Need rescue? Click the marker above your character or press R. |
| 엘리베이터 위치 조회 실패 | 엘리베이터 위치를 찾지 못했습니다. 잠시 후 다시 시도해 주세요. | Couldn’t locate the elevator. Please try again shortly. |
| 표시한 비용과 현재 비용이 달라 갱신 | 화물 상태가 변경되었습니다. 최신 비용을 다시 확인해 주세요. | Your cargo has changed. Review the updated rescue cost. |
| 구출 요청 불가 | 현재는 긴급 구출을 요청할 수 없습니다. | Emergency rescue is currently unavailable. |

`EmergencyRescueChipView`에서 표식, `EmergencyRescuePopupView`에서 확인창, `EmergencyRescueRuntimeController`에서 드론 재안내와 실패 안내를 표시합니다. 드론 재안내는 같은 문구를 말풍선과 터미널에 전달합니다. `R`은 확인창을 열며 실제 구출은 확인 버튼으로 요청합니다. 키캡은 연결된 키 글자를 사용합니다.

## 비용 표와 안내

| 표시 위치와 조건 | 한국어 | English |
| --- | --- | --- |
| 열 제목: 자원 | 자원 | Resource |
| 열 제목: 차감량 | 차감량 | Cost |
| 열 제목: 구출 전 보유량 | 현재 보유 | Before |
| 열 제목: 구출 후 잔량 | 구출 후 잔량 | After |
| 골드 행 이름 | 골드 | Gold |
| 광물 행 이름 예시 | 구리 / 철 / 리튬 | Copper / Iron / Lithium |
| 골드 보유량 / 차감량 | `{amount}G` / `-{amount}G` | `{amount}G` / `-{amount}G` |
| 광물 보유량 / 차감량 | `{amount}개` / `-{amount}개` | `{amount}` / `-{amount}` |
| 무료 구출 | 보유 골드와 미정산 화물이 없어 무료로 구출됩니다. | Rescue is free because you have no gold or unsold cargo. |
| 비용 정보 없음 | 비용 정보를 불러올 수 없습니다. | Couldn’t load the rescue cost. |
| 유료 구출의 광물 차감 안내 | 미정산 광물은 종류별 {percent}%가 차감됩니다. | Rescue deducts {percent}% of each unsold mineral type. |

`EmergencyRescueCostTable`의 열 제목은 좁은 폭에 맞춰 짧게 잡았습니다. `EmergencyRescueCostRows`의 광물 이름은 자원 ID를 기준으로 #390의 공통 자원 번역에 연결하고, 번역이 없는 ID는 기존 표시명으로 대체합니다. `{amount}`에는 기존 비용 계산 결과를 넣고 천 단위 구분과 `G` 단위를 유지합니다. `{percent}`에는 `EmergencyRescueService.MineralLossPercent`를 사용합니다. 실제 표시 예시는 `1,240G → 740G`, `12개 → 2개` / `12 → 2`입니다.

## 적용 후 화면 확인

아래는 실제 화면에서 미검증입니다.

- 한·영을 각각 적용한 뒤 구출 표식과 `R`로 확인창을 열어 버튼, 제목, 드론 재안내의 언어와 줄바꿈을 확인합니다.
- 골드와 여러 광물이 있는 상태에서 비용 표의 열과 자릿수가 겹치지 않는지, 무료 상태에서는 무료 안내가 표시되는지 확인합니다.
- 비용이 바뀌어 표가 갱신되는 경우에 안내와 비용이 같은 언어로 표시되는지 확인합니다.
