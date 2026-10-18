# Demo 안내·실패 문구 점검 — 2026-10-13

## 작업 기준

- 사용자 요청 1→2→3 순서: 18개 목표/가이드와 실제 동작 비교, 설치/이동 실패 이유와 해결 안내, 배포/데스크탑 인계 정리. 게임 규칙·UI 배치·신규 기능은 변경하지 않는다.
- 작업 시작 HEAD 9a57235e, codex/demo-failure-recovery, PR #176 후속 로컬 변경. main을 새로 받아오거나 PR을 병합하지 않았다.
- 근거: DemoObjectiveCatalog / DemoObjectiveDirector / ElevatorTravelBridge / DeepZoneUnlockRule.Mvp / ProgressionService / OutpostService / EmergencyEscapeService / GameGuideCatalog / BuildingMenuPresenter / GameplayBuildingPlacementBridge.
- Unity 실행 없이 정적으로 확인했다. 기존 사용자 저장과 데이터 에셋 3개는 보존한다.

## 18개 목표 비교

| 순서 | 목표 | 현재 완료 조건의 근거 | 안내 점검 |
|---|---|---|---|
| 1 | 블록 제거 | 현재 단계에서 TileMined 이벤트 | Enter/마우스 채굴 안내 유지 |
| 2 | 구리 채취 | 구리 TileMined, 실제 획득 quantity > 0 | 직접 채굴 안내 유지 |
| 3 | 드릴 속도 업그레이드 | 해당 구매 성공, 현재 레벨 증가 | 드릴 속도 1회 구매 안내 유지 |
| 4 | 지상으로 이동 | 엘리베이터 실제 귀환 성공 신호 | 탑승 후 E 안내 유지; 지연 실패는 #176 수정 |
| 5 | 광산 탐사 재개 | 도착 후 승강기 보호 구간 안을 관측하고 가로 밖으로 이탈 | 도착만으로 완료가 아님을 기존 안내가 설명 |
| 6 | 철 채취 | 철 TileMined, quantity > 0 | 직접 채굴 안내 유지 |
| 7 | 위험 지대 보강 | 버팀목 설치 결과의 reducedStructuralRisk가 true | 실제 위험 감소가 필요함을 문구에 반영 |
| 8 | 사다리 설치 | 사다리 BuildingPlaced | 수직 통로 설치 안내 유지; E로 등반하지 않음 |
| 9 | 심부 조명 설치 | 조명 BuildingPlaced 시 플레이어 Run.Depth >= 10 | 깊이 표시 10m 안내 유지 |
| 10 | 광물 보관 | 이 단계의 보관함 설치 기록 + Deposit 성공 quantity > 0 | 설치 후 1개 이상 맡기기 안내 유지 |
| 11 | 전진기지 코어 설치 | 코어 BuildingPlaced, 위치 기록 | 코어 설치 안내 유지 |
| 12 | 전진기지에서 충전 | 이 단계에서 코어 반경 10칸 내 충전기 설치 기록 + Charge 성공 | 설치 후 충전 안내 유지 |
| 13 | 전진기지에서 회복 | 이 단계에서 코어 반경 10칸 내 보건소 설치 기록 + Heal 성공 | 설치 후 회복 안내 유지 |
| 14 | 심층 구역 해금 | 완료 목표 >= 13, 드릴 속도 >= 2; 신규/기존 해금 신호 | 드론 2/가스 저항 1 필수라는 낡은 문구 제거 |
| 15 | 리튬 채취 | 리튬 TileMined, quantity > 0 | 심층에서 리튬 채굴 안내 유지. 완료 판정에 별도 깊이 검사는 없음 |
| 16 | 가스 정화 | 코어 추적 → 반경 5칸 내 대상 채굴 → 해당 가스 활성화 → 일치하는 GasPurified | 기존 순서 안내 유지; 코어만 설치해서 완료되지 않음 |
| 17 | 전진기지 자원 판매 | 코어 반경 10칸 내 콘솔 설치 기록 + 정산 성공 quantity/gold > 0 | 지상 판매와 구분하는 기존 안내 유지 |
| 18 | 긴급 탈출 귀환 | 포탈 성공 후 EmergencyEscapeSucceeded | E 목적지 선택/실행 안내 유지; R 구출과 별개 |

설치 기록은 각 목표 단계의 진행 상태를 사용한다. 일반 시설 사용 가능과 해당 목표 완료 조건을 같다고 가정하지 않았다. 저장 후 실제 재진입/이전 시설 활용 검증은 여기서 통과했다고 기록하지 않는다.

## 수정한 안내

1. DemoObjectiveCatalog: 버팀목 설명/힌트에 실제 위험도 감소를 명시. 심층 해금 설명/힌트를 현재 규칙인 목표 13개 + 드릴 속도 2레벨로 맞춤.
2. GameGuideCatalog: 리튬 카드에 현재 심층 해금 조건을 명시. 첫 탐사 5단계 '판매·업그레이드'의 클릭 연결을 지하 정산 콘솔 카드에서 지상 기지 카드로 수정. 지상 귀환 후 실제 화면의 판매/업그레이드와 맞춘다.
3. BuildingMenuPresenter: 겹침→빈 공간, 승강로 침범→승강로 밖, 지면 부족→바닥 위, 거리 초과→가까이 이동, 허용 구역 밖→구역 안 선택을 안내. reasonId/판정/비용은 유지.
4. GameplayBuildingPlacementBridge: 이동의 지면/승강로 실패에도 해결 위치를 안내. OutsideAllowedArea를 일반 실패 문구로 뭉개지 않고 별도로 안내. 기존 겹침/거리 안내는 유지.
5. BuildingMenuPresenterTests: 기존 occupied 검사에 남아 있던 '차지한 위치' 기대 문구를 현재 '빈 공간을 선택하세요'로 맞춤. 새로운 텍스트 전용 테스트는 만들지 않았다.

가이드 포탈 비용은 EmergencyEscapeService의 상수(골드/최대 전력 비율)를 이미 참조하고, 충전·회복 대기와 광산 초기화 비용도 기존 코드에서 읽는다. 이 경로는 유지했다. 현재 Ctrl+M 가이드는 토글을 설명하며 이번 코드에서도 토글 경로인 점을 확인했다. 과거 인계의 Hold 설명을 그대로 적용하지 않는다.

## 최소 검증과 제한

- App / App.GameplayIntegration / App.Tests.EditMode 현재 소스 경량 컴파일 성공. 생성 csproj/해당 PC 의존 DLL을 사용했으며 Unity 임포트 검증을 대신하지 않는다.
- 문구/연결만 변경하여 전체 자동 테스트·Unity·빌드·성능 측정은 실행하지 않았다. 테스트 소스는 컴파일만 확인한다.
- 기존 Builder/BuildingMenuView의 availability/status 표시 경로를 확인했다. UI 배치·프리팹/씬 YAML·글꼴 아틀라스는 변경하지 않았다. 실제 문구 줄바꿈/가독성/누락 글리프는 데스크탑에서 한 번 확인해야 한다.
- diff 검사, 기존 데이터 에셋 보존, 임시 컴파일 출력 정리. 변경은 아직 로컬 미커밋·미푸시다.

최신 데스크탑 시작 절차는 desktop-handoff-20261013.md를 먼저 읽는다.

공유 상태 갱신: 사용자 요청으로 이번 안내 수정과 최신 인계 문서를 codex/demo-failure-recovery에서 커밋·푸시한다. 위 로컬 미커밋 설명은 작성 당시 상태다. 실제 실행 미완료 항목은 그대로 남는다.
