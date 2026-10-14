# 데스크탑 시각 개선과 QA 재개 — 2026-10-14

기준 기록: work_process/notebook-visual-review-20261013.md 및 desktop-visual-progress-20261013.md.
브랜치: codex/demo-visual-polish. 기준 HEAD a2e700e8 + 이번 미커밋 수정. 원격 main 576c1be4 확인. PR #168/#178은 미병합이며 이번 작업에서 병합·커밋·푸시하지 않았다. 최신 main의 B-145~147 변경은 이 후보에 통합하지 않았다.

## 적용 결과

- V01: BasicHUD 430×248 → 380×208. 체력/전력 숫자는 22 유지. 미션 575×154 → 460×116, 목표 제목·진행도·설명 및 기존 상세창 접근 유지. 제목이 줄 높이 부족으로 숨는 것을 실제 캡처에서 발견해 높이 34로 보정. 시설창 너비 580.8 → 500, 상단 -426 → -360. 시설창·우측 메뉴는 처음에 닫혀 있고 기존 B/I/U/G/Esc 및 / 메뉴 조작은 유지.
- V03: 구조 위험 단계/붕괴·피해 판정 변경 없이 균열 alpha와 발광을 완화. 떨림 진폭 0.1/0.3 → 0.025/0.06, 보간 간격 0.02/0.01 → 0.12/0.08. 1.4초 주기 중 0.4초만 부드럽게 떨고 나머지는 정지. Reduce Motion에서는 흔들림 없음. 임박 표시의 느린 점멸과 단계 차이는 유지.
- V05: 고정 레일의 중심/윗끝/아랫끝·객실/문/탑승 로직 유지. 레일과 하이라이트 폭 축소, 재질/주황 클램프 명암을 낮추고 기존 가이드 중심의 짧은 금속 슬리브를 추가.
- V06: 실제 캡처에서 거대한 원이 새 스캔 코드와 무관하게 남아 있어 씬을 조사. 원인은 씬에 저장된 소유 View 없는 DroneScanPulseVisuals 생성 루트 6개였다. Editor API로 해당 이름·루트·MonoBehaviour 없음 조건의 계층만 제거. 그래서 씬 diff의 삭제량 약 16,000줄은 이 효과 계층이다. 새 런타임 스캔 루트는 DontSave. 실제 스캔 원은 512 텍스처의 얇고 부드러운 경계, alpha 0.22, 0.9초 페이드, sortingOrder 8. 스캔 판정 반경과 자원 마커 수명 유지. 전력/정화 범위는 별도 LineRenderer이며 폭 0.035, alpha 0.25로 완화하고 반경은 유지.
- 이전 PR #178의 보상 안내 수정과 낙하 잔재 회수도 이 후보에 포함돼 있다. 보상 안내는 두 해상도의 최종 화면에서 테두리 안쪽에 표시됨을 확인.

Editor 적용: DesktopVisualPolishBuilder.Build. 대상은 BasicHUD.prefab, BuildingMenu.prefab, StartElevator.prefab, Mine_Demo_Integration.unity만이다. 씬/프리팹 YAML 직접 편집 없음. 신규 런타임 Inspector 연결 없음. 기존 빌더로 되돌려 재생성한 뒤에는 이 후속 빌더를 다시 적용한다.

## 실제 검증

- Unity 6000.5.4f1 빌더/컴파일 성공. 최초 캡처 테스트 생성자 인자 오류는 수정 후 컴파일 성공.
- 관련 EditMode 45개 최초 실행: 44 PASS, 미션 레이아웃 1 FAIL(옛 좌표/여백을 강제한 검사). 실패를 숨기지 않고 실제 HUD·시설창의 비겹침·새 여백 기준으로 갱신. 최종 그 검사 1개 PASS. 나머지 통과 항목 반복 안 함.
- ScanRing_FadesAndExpiresWithoutChangingRadius: PASS. 이전 반경/자원/스캔 계약 검사도 통과.
- InGameMenu_ActionsHoverTransitionsAndHiddenShortcuts: PASS(닫힌 초기 상태, 버튼·단축키·접기·재활성화).
- DoorVisual_KeepsCabinRaisedAndClosedAfterTravel: PASS.
- HudBuildingAndReward_AtTwoResolutions: PASS. 1920×1080/2560×1440에서 HUD·시설창+우측 메뉴·보상 팝업의 PNG 6장 생성. 미션 제목의 실제 visible character도 검사. 선택 필터로 실행했으며 전체 테스트 스위트가 아니다.
- 최종 캡처에서 굵은 잔존 원이 사라지고 목표 제목과 안내가 보이는 것을 직접 확인. 새 스캔의 시간에 따른 연출, 위험 타일의 실제 플레이 가독성, 이동 중 레일의 미관은 사용자 최종 검수가 필요하다.
- 수정 후 QA Windows 빌드 한 번 성공, Editor 종료 코드 0.
- 이미 통과한 시설 이동 12개/기존 순수 관리형 검사는 반복하지 않았다.

검사 결과/로그: sub-terra/Builds/VisualPolish-20261014/{functional-tests,final-recheck,rendered-final}/.
최종 캡처: sub-terra/Builds/VisualPolish-20261014/screenshots/.
수정 전 잔존 원 캡처: screenshots-before-cleanup/에 별도 보존.

## QA 빌드와 저장 보존

- Build ID: 20261013-164846 (UTC 식별자; 한국 시각 2026-10-14 01:48:46).
- 채널 Qa / Windows x64 / gameVersion 1.0 / saveVersion 5.
- ZIP: sub-terra/Builds/Windows-x64/Qa/20261013-164846/sub-terra-windows-x64-Qa-20261013-164846.zip.
- SHA-256: cbd5e2382735ae8f303faf65cb869b194edc90a1728b445ba52886da9ea13a50.
- 후보는 기준 커밋 + 미커밋 소스다. 로컬 candidate-source.patch와 새 빌더/캡처 테스트도 함께 보존한다. 배포 커밋 또는 출시 완료 빌드로 표현하지 않는다.
- 이번 검사가 자동 갱신한 과거 기록 이미지 3개는 사용자 확인 후 원래 상태로 복원했다. 새 PNG 6장은 별도 보존. 빌드가 줄바꿈만 변경한 설정/폰트 5개도 복원. ProjectSettings·manifest·lock·버전·실사용 저장 슬롯의 최종 내용 변경 없음.
- 이전 QA Player 로그에 정상 종료 단계가 있고 ComputeBuffer 문자열은 검색되지 않았음. 이전 빌드의 이 결과를 이번 후보의 Demo 완주/이어하기/종료 검증으로 대체하지 않는다.

## 직접 조작 검수 재개

Run-QA.ps1/Run-QA.cmd는 새 후보를 이전의 격리된 QA 저장 폴더로 실행한다. 실행 전에 해당 QA 파일을 별도 폴더에 백업한다. 실제 사용자 저장 폴더는 사용하지 않는다. 실행 파일을 직접 열어 저장 경로 인자를 빠뜨리지 않는다.

1. 기존 QA 슬롯에서 이어하기 → 현재 미션부터 Demo 완주 → 귀환 → 판매/업그레이드 → 재탐사. 새 게임 경로가 필요하면 Run-QA.ps1 -Fresh 또는 빈 QA 슬롯을 사용한다.
2. 시설 이동 후 저장. 위치·점유·보관량·회복/충전 대기·미션 진행을 기록.
3. 게임 내 정상 종료로 프로세스를 완전히 닫은 뒤 Codex에 결과를 전달.
4. 같은 후보/QA 경로로 재실행해 이어하기 상태를 비교한 뒤 다시 정상 종료. Codex가 해당 Player 로그에서 예외·ComputeBuffer 종료 경고 확인.

현재 전체 상태: 시각 수정 및 자동 검증 완료, 일반 입력 Demo 완주·저장 복원·이번 Windows 후보 종료 검증 대기. Unity가 없는 다른 PC 검증도 미완료다. Windows 게임 화면 조작 도구가 연결되지 않아 일반 입력 완주는 사용자가 진행해야 한다.

## 변경 파일 목록

경로는 sub-terra/Assets/_Project/ 기준이다.
- Editor/DataValidation: DesktopVisualPolishBuilder.cs(+meta), PromptB107QuestUiBuilder.cs, PromptB110BuildingMenuBuilder.cs.
- Prefabs: UI/BasicHUD.prefab, UI/BuildingMenu.prefab, Gameplay/Traversal/StartElevator.prefab.
- Scenes/App/Mine_Demo_Integration.unity.
- Scripts/App/UI/HUD/GameplaySideMenuController.cs.
- Scripts/Gameplay: Drone/DroneScanPulseView.cs, Player/ElevatorDoorVisual.cs, Power/PowerSupplyRangeIndicator.cs, Structural/StructuralCrackOverlay.cs.
- Tests/EditMode: App/Tutorial/PromptB107QuestUiTests.cs, App/UI/PromptB105SideMenuTests.cs, Gameplay/Drone/DroneSensorUpgradeTests.cs.
- Tests/PlayMode/Integration/Bootstrap: PromptB105SideMenuPlayModeTests.cs, DesktopVisualPolishCaptureTests.cs(+meta).
- work_process/desktop-visual-polish-20261014.md(이번 기록).

QA Player 기동: PID 15624, Responding=True. Player-20261014-015125-958.log. 기동 로그에서 검색한 예외·ComputeBuffer 경고 없음. Direct3D 타임스탬프 CPU fallback 안내 관찰. 아직 종료 전이므로 이번 후보의 종료 검증 통과로 기록하지 않는다. 이전 QA 슬롯 파일은 qa-save-backup-20261014-015125-958에 백업 후 실행했다.
## 후속 검증 — Windows 화면 조작 연결

- 기존에 설치된 Computer Use 플러그인의 @oai/sky 연결 성공. 추가 소프트웨어 설치 없음. QA 빌드 창의 실제 화면 캡처, 이어하기 버튼 클릭, 플레이 영역 클릭 반응 확인. 위의 '화면 조작 도구가 연결되지 않음' 설명은 이 후속 상태로 대체한다.
- QA 슬롯은 실제 이어하기 화면에서 13/18, 심층 구역 해금 목표. 골드 515, 화물 28, 미정산 205, 깊이 1m. Demo 완료 아님.
- 자동 E/e/Esc 키 입력은 현재 게임에서 반응하지 않았다. 마우스 클릭으로 '채굴할 수 없습니다' 안내는 표시되어 클릭 전달은 확인됨. 키 입력 문제의 원인은 아직 확정하지 않았고 사용자에게 E키 한 번의 직접 입력으로 비교를 요청했다. 사용자 입력이 들어오면 기존 화면 좌표를 버리고 새 화면을 확인한다.
- 이번 Windows 후보의 별도 격리 저장 smoke: 새 게임 fixture(gold=321, depth=12) 생성 후 완전 별도 프로세스의 continue로 값 복원 확인. new/continue 모두 프로세스 종료 코드 0, 정상 Shutdown Sequence, ComputeBuffer 경고 0. Builds/VisualPolish-20261014/player-save-restart-check/summary.txt 및 로그 보존. 이 fixture 검사는 일반 입력 Demo 완주 후 상태 복원을 증명하지 않는다.
- 실제 QA 플레이의 Demo 완주·완주 후 이어하기·정상 종료는 여전히 미완료. 검증을 마친 다음 커밋·푸시하라는 사용자 승인에 따라 현재 시점에서는 커밋·푸시하지 않았다. 기존 사용자 저장 슬롯과 QA 백업 보존.

## 02:27~02:32 KST 후속 결과 — 실제 QA 슬롯 재시작

- 사용자 직접 E 입력으로 기지 귀환 성공. 자동 키 입력 전달 문제와 직접 입력의 정상 동작을 구분했다. 원인은 미확정. 재실행 후 자동 Right 입력도 이동 반응 없음.
- 기지 판매 화면에서 구리 4, 철 11 확인. 재료 판매/구매는 수행하지 않아 그대로 보존. 업그레이드 화면 드릴 속도 Lv.1 → Lv.2 비용 구리 10/철 6, 구리 6 부족 확인. 남은 Demo를 진행하려면 이동/채굴 후 필수 업그레이드가 필요하다.
- 게임 내 기지 종료 버튼으로 완전 종료. Player-20261014-015125-958.log에서 ShutdownInProgress → Shutdown, Polling Thread 종료, PlayerConnection Cleanup 확인. 전체 로그 ComputeBuffer 0, Exception: 0. 실행 프로세스 없음 확인. 이 정상 종료의 프로세스 exit code는 실행 당시 핸들을 유지하지 않아 수집하지 못했다.
- 현재 QA 저장 요약: saveVersion5 / SurfaceBase / gold515 / cargoWeight28 / unsettledValue205 / completedObjectives13 / demo.quest.unlock_deep_zone / isDemoComplete=false. 요약만 로컬 qa-before-continue-summary.json에 기록. 실제 사용자 세이브 원문 출력·변경 없음.
- 제한된 프로세스 실행에서는 Unity가 low integrity 초기화에서 중단되어 Player-20261014-023043-410.log에 초기화 일부만 남음. 정상 권한으로 재시도 후 PID12472, Player-20261014-023144-281.log로 정상 실행. QA 저장 파일은 각 재실행 전에 백업했다.
- 게임 UI의 슬롯1 이어하기 → SurfaceBase(골드515, 화물28) → 지하 탐사 시작 → Mine에서 13/18, 골드515, 화물28, 미정산205 확인. 지상 시설과 기존 채굴 구멍/사다리가 표시됨을 확인. 이번 후보에서 현재 진행 상태의 실제 재시작/이어하기는 통과했지만 전체 시설 내부 상태까지 대조한 것은 아니다.
- 현재 상태: 이번 후보 정상 종료 경고 검사 PASS, 13/18 상태 이어하기 PASS. 일반 입력 18/18 Demo 완주 및 완주 후 이어하기는 자동 키 입력 제한으로 BLOCKED. 게임은 격리된 QA 슬롯1의 Mine에서 실행 중이며 사용자 직접 입력으로 이어갈 수 있다. 완주가 남아 있어 요청한 '끝나면 커밋·푸시'는 아직 수행하지 않았다.

## 오늘 종료 및 팀 전달 요약

사용자가 남은 Demo 플레이를 다음 아침에 직접 재개하고, 오늘 수정은 지금 커밋·푸시하도록 명시적으로 승인했다. 위의 커밋 보류 기록은 당시 상태이며 이 요청으로 대체한다. QA 게임은 게임 내 종료 버튼으로 저장 후 정상 종료했다. Player-20261014-023144-281.log도 정상 Shutdown 단계, ComputeBuffer 0, Exception: 0을 확인했다.

팀 전달:
- 브랜치 codex/demo-visual-polish. PR #178의 보상 안내/낙하 잔재 수정(a2e700e8)에 이어 HUD·미션·시설창을 압축하고 우측 메뉴 기본 상태를 접힘으로 조정.
- 붕괴 예고의 균열/떨림, 엘리베이터 레일과 접점, 스캔/전력 범위 원의 시각 강도를 완화. 거대한 원의 원인이었던 씬에 저장된 소유 View 없는 스캔 생성 루트 6개를 Editor로 제거하고 런타임 루트에 DontSave 적용. 게임 판정 반경/붕괴 피해/탑승 로직은 유지.
- 관련 EditMode 45개 최종 통과(처음 실패한 레이아웃 1개 수정 후 재검사). 메뉴 기능, 레일 이동 후 상태, 스캔 페이드/반경, 두 해상도 화면 캡처 검사 통과. QA Windows 빌드 성공. 실제 QA 슬롯13/18의 완전 종료 → 이어하기 → 재탐사에서 미션과 자원 보존 확인, 종료 예외/ComputeBuffer 경고 0. 전체 Demo 완주는 미완료.
- Scene/Prefab은 Editor 빌더로 적용했고 새 Inspector 연결은 불필요. 기존 UI 빌더로 재생성하면 DesktopVisualPolishBuilder.Build를 후속 적용할 것. 큰 씬 삭제 diff는 잔존 스캔 효과 계층 제거이며 다른 월드 제거가 아님.
- PR #168/#178 미병합. 최신 main의 B145~147은 후보에 미통합이므로 팀 통합 때 해당 변경과 함께 재검수 필요. 출시 완료/전체 검증 완료로 취급하지 않을 것.

다음 아침 데스크탑 재개:
1. sub-terra/Builds/VisualPolish-20261014/Run-QA.cmd 실행 → QA 슬롯1 이어하기. 해당 실행기는 기존 QA 파일을 먼저 백업하고 별도 QA 저장 경로를 지정한다. exe를 인자 없이 직접 실행하지 않는다.
2. 현재 목표는 13/18 심층 구역 해금. 구리4/철11, 골드515, 화물28, 미정산205. 드릴 Lv.2에 구리10/철6이 필요하므로 우선 구리6 추가 확보. 드릴2/드론 스캔2/가스 저항1을 갖춰 해금.
3. 남은 리튬 채취 → 코어를 먼저 설치한 근처 리튬/가스 채굴 후 정화 → 코어 근처 정산 콘솔 판매 → 비상 탈출 귀환 목표를 진행해18/18 확인.
4. 완주 상태에서 정상 종료 → 같은 QA 경로로 이어하기 → 완료 상태/자원/시설 비교 → 정상 종료 로그 확인. 이 단계는 아직 통과하지 않았다.

다른 PC/노트북은 원격 브랜치를 받아 소스와 이 기록지를 확인할 수 있다. Builds 아래 QA 실행 파일·PNG·QA 세이브·로그는 로컬 산출물이며 Git 커밋에는 포함하지 않는다. 따라서 노트북에서 동일 QA 슬롯을 바로 재개하려면 별도 산출물 전달이 필요하고, 내일 데스크탑 재개는 현재 로컬 QA 저장을 사용한다. 실사용 저장 슬롯과 stash, 기존 작업 폴더 및 테스트 임시 씬은 보존했다.

## QA 자동 입력 보완 — 2026-10-14 재개

기준 커밋 40d3d65e. 기존 Qa 빌드에서 Shift+E 조합도 게임에 전달되지 않음을 재현했다. 도구의 키 주입과 게임 입력 간 구체적인 원인은 아직 확정하지 않았으므로 Windows.Gaming.Input 등을 확정 원인으로 기록하지 않는다.

사용자의 자동 이동 대응 요청에 따라 QaPointerInputOverlay를 추가했다. QA/Editor에서만 컴파일되고, -subterra-qa-pointer-input과 -subterra-save-root를 모두 전달한 실행에서만 생성한다. Release에는 포함되지 않는다. 마우스 버튼으로 별도 Keyboard 장치의 일반 Input System 상태를 전달한다. 기존 방향키 이동/WASD 채굴 조작 방식을 사용하며 이동0.45초/채굴2초/상호작용0.15초 후 자동 해제한다. 장면 전환, 포커스 상실, 설정창 열림, 종료 때도 해제한다. 자원 지급·위치 변경·미션 완료 등 게임 상태를 직접 조작하는 기능은 없다. 씬/프리팹/ProjectSettings/패키지 변경 없음.

검증: QaPointerInputTests 2개 PASS. 첫 batch 검사에서는 입력 포커스 환경 미설정으로2개 실패했고 기존 UiTestEnvironment의 합성 입력 포커스 설정을 적용한 재검사에서 모두 통과했다. 검사 원본/재검사 로그 모두 Builds/PointerInput-20261014에 보존. 의미: PlayerKeyboardControls 이동 전달/시간 만료/제거 후 해제, 기존 E InputAction started1회/시간 만료. 이 검사는 실제 Windows 화면 플레이를 대체하지 않는다.

새 Qa Windows 빌드 20261014-031757 성공. ZIP SHA256 30e18b85982cd4bfd6fbc991cb01f8294f8156958888f4f45d6884dce7ad56f8. 기존 QA 슬롯을 먼저 백업 후 실행. 로컬 재실행 helper: sub-terra/Builds/PointerInput-20261014/Run-QA.cmd. 실행로그 Player-20261014-121931-149.log.

현재 새 실행 파일의 Windows 네트워크 액세스 보안 창이 게임 앞에 떠 실제 화면 이동 검사가 대기 중이다. 이 로컬 검사에 네트워크 허용은 필요하지 않다. Computer Use의 보안/개인정보 권한 창 조작 금지 지침에 따라 사용자에게 취소를 직접 누르도록 요청했다. 입력 패널은 화면에 표시됨을 확인했으나 실제 이동·Demo 완주 성공으로 기록하지 않는다. 빌드가 자동 갱신한 설정/폰트5개는 내용 diff가 없음 확인 후 원래 상태로 복원했다.

## 검증 중단 및 인계 — 2026-10-14

사용자가 외출을 위해 "검증 여기까지 하자"고 요청했다. 물리 Escape로 Computer Use가 중단된 뒤 추가 화면 조작은 수행하지 않았다. Windows 보안 창은 사용자가 직접 허용을 눌렀다고 알려왔으며, 에이전트가 보안 설정을 변경하지 않았다.

- 실제 새 Windows QA 빌드에서 QA 버튼의 좌우 이동, 사다리 상하 이동, WASD 방향 채굴, C 근접 시설 설치가 반응함을 확인했다. 일반 입력 처리 경로를 사용하며 게임 상태를 직접 변경하지 않았다. 자동 OS 키 주입의 원인은 미확정이며, 이 보완은 QA 전용 우회 입력이다.
- 화면 관찰 기준 Demo 14/18, 드릴 속도 Lv.2 및 심층 해금 확인. 마지막 직접 관찰은 깊이31m, 체력25, 전력32, 골드595, 화물23.8, 미정산205. QA 플레이 중 붕괴 피해와 사망/자원 감소도 발생했다. 시작 전 QA 백업 보존.
- 중단 후 게임 프로세스 없음 확인. 마지막 QA 저장 파일은 Mine_Demo_Integration / completedObjectives15 / currentObjectiveId demo.quest.purify_gas_with_outpost / isDemoComplete=false / pendingQuestRewardId demo.quest.mine_lithium / questRewardSettledCount14. 이는 화면으로 관찰한14/18 이후의 저장 상태이며, 해당 후속 플레이를 에이전트가 직접 검증한 것으로 간주하지 않는다.
- 저장 inventory: 구리3, 철4, 리튬1, currentWeight13.3, unsettledValue130. player 요약: energy12, gold595, cargoWeight14.9, unsettledValue210. 두 요약의 화물/미정산 값이 일치하지 않으므로 재개 시 로드된 HUD/인벤토리와 비교해야 한다. 저장 원문 변경 없음. 드릴 속도2 및 zone.deep 저장 확인.
- Player-20261014-121931-149.log에서 ComputeBuffer 및 Exception: 문자열0건. 정상 Shutdown 단계/프로세스 exit code를 수집하지 못했으므로 이번 빌드의 정상 종료 검증은 미완료. "Releasing render texture that is set to be RenderTexture.active!" 경고1건은 별도 추적 필요.
- 화면에 굵은 회색 타일 격자가 지속 표시됐다. 원인은 미확정이며 후속 시각 검수 대상. 심층 해금 목표 안내의 드릴2/드론2/가스저항1 문구와 DeepZoneUnlock.Mvp의 실제 필수 조건(드릴2만)의 불일치도 발견했다. 이번 중단 시점에는 추가 수정하지 않았다.

재개 순서:
1. 로컬 Builds/PointerInput-20261014/Run-QA.cmd로 QA 파일 백업 후 같은 격리 슬롯1 이어하기. 최신 QA 빌드20261014-031757을 사용하며 예전 VisualPolish 실행기와 혼동하지 않는다.
2. 보류된 리튬 채굴 보상 정산 여부와 실제 HUD/인벤토리 자원 비교. 저장된15/18에서 가스 정화 → 코어 근처 정산 판매 → 비상 탈출 귀환 목표 진행. 에너지12이므로 보급 상태부터 확인.
3. 18/18 완료 확인 후 정상 종료 → 같은 QA 경로로 새 프로세스 이어하기 → 완료 상태/자원/시설 비교 → 정상 종료 및 ComputeBuffer/예외/렌더 텍스처 경고 재확인.
4. 회색 격자 및 심층 해금 안내 불일치는 원인 확인 후 좁은 수정/검증을 진행한다. 최신 main과의 통합 검수도 여전히 남아 있다.

이번 커밋 대상은 QA 입력 어댑터/테스트와 각 .meta, 이 기록지뿐이다. Inspector 추가 연결은 필요 없으며 두 실행 플래그로만 활성화된다. PlayMode2개 PASS 및 새 QA 빌드 성공 결과를 유지한다. Demo 완주/완주 후 이어하기/이번 빌드 정상 종료는 완료로 보고하지 않는다. 실제 사용자 저장 슬롯, 기존 stash/작업 폴더/테스트 임시 씬은 보존한다. Builds의 실행 파일/QA 세이브/로그/백업은 Git에 포함하지 않아 노트북에는 소스와 기록지만 전달된다.
