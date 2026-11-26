# B-213 결과: 산소 부족 드론 경고 (#351)

- 기준 main: `a8de562b` (Merge PR #402 MVP2-fix156). 문서를 쓸 때의 `a694acac`보다 B-212 병합만큼 앞선 최신 `origin/main`에서 시작했다.
- 브랜치: `MVP2-fix157` (원격에 같은 이름 없음)
- 커밋: `19bab8ed` (feat: 산소가 위험할 때 드론이 빨간 경고를 한 번 띄운다), `322d079a` (docs: B-213 프롬프트 작성)

## 바꾼 파일
| 파일 | 내용 |
| --- | --- |
| `Scripts/App/Run/LowOxygenWarningPolicy.cs` (새) | 순수 발사 규칙. `Reset(critical)` 기준점, `Observe(critical)`은 무장 상태에서 위험으로 내려간 순간에만 true. 위험 위로 오면 다시 무장. 첫 관찰이 이미 위험이면 발사하지 않음. 전력은 입력이 아니다. |
| `Scripts/App/Integration/LowOxygenDroneWarningController.cs` (새) | `PlayerSurvivalController.OxygenRiskChanged` 구독. 발사하면 말풍선·터미널에 경고를 띄우고 `HudPanelChromeController.OpenDiggerBot()`. `[SerializeField] autoCloseSeconds = 6`. 경고로 연(또는 이미 열린 창을 경고로 바꾼) 터미널만 자동으로 닫고, TAB·X로 먼저 닫으면 자동 닫힘을 해제한다. 정지(`UiPauseGate.IsHeld`, 설정 창) 중이면 풀릴 때까지 미뤘다가 띄운다(디버그 콘솔에서 `O2 14`를 쳐도 콘솔을 닫은 뒤 소리·연출이 나온다). 그 사이 회복하면 띄우지 않는다. 저장 복원용 `BeginRestore`/`EndRestore`. |
| `Scripts/App/Integration/IntegrationRuntimeBinder.cs` | `EmergencyRescue` 다음 `TryStep("LowOxygenDroneWarning")`에서 컴포넌트를 런타임에 붙이고 `Bind`. 정리 때 `Unbind`. `TryRestorePlayerOxygen`(이어하기 산소 복원)을 `BeginRestore`/`EndRestore`로 감싸서, 복원으로 바뀐 위험 단계를 하강으로 보지 않는다. 구출 표식 로직은 그대로. 씬 YAML 변경 없음. |
| `Scripts/App/UI/Drone/DroneOxygenWarningFx.cs` (새) | 패널(말풍선·터미널) 자식으로 붙는 연출. 좌우 가장자리 빨간 글리치 조각 6개(`RunFailureGlitchArt.GetGlitchBlockSprite`). 0.07초마다 위치·폭이 바뀌고 처음 1.2초는 강하게, 그다음은 약하게. 조각 높이는 패널의 6~22%라 패널 전체를 덮지 않는다. 터미널에는 9-slice 후광(`GetHaloSprite`, `fillCenter=false`, 바깥 16px)을 0.55초 주기로 깜빡인다. 모두 `raycastTarget=false`. 시간 → 모양 계산은 결정적인 정적 함수. |
| `Scripts/App/UI/Drone/DroneDialogueSocket.cs` | `ShowOxygenWarning(dialogue)`: `urgentVisibleSeconds`(6초) 동안 빨간 테마(외곽선·제목 `DIGGER-BOT // O2 CRITICAL`·상태 `O2 LOW`·본문)와 가장자리 글리치를 켠다. 유지 중에는 분석 루프의 `SetDialogue`를 버려 경고 문구가 덮이지 않는다. 경고 말풍선만 간격·핑 겹침 생략 규칙을 건너뛰고 `DroneBubbleUrgentSerious`를 한 번 낸다(정지 중이면 내지 않음). 기존 모션 감소(펼침·타이핑 생략)는 그대로다. |
| `Scripts/App/UI/Drone/DroneDialoguePanelView.cs` | `ShowOxygenWarning(dialogue, holdSeconds)`: 본문을 빨간 경고로, 헤더를 `... // O2 CRITICAL`·짙은 빨간 바로 바꾸고 외곽선을 빨갛게 한다. 둘레 후광과 가장자리 글리치를 켠다. 이미 열린 창이면 경고 문구만 다시 찍는다. 유지 중 분석 대사는 미뤄 뒀다가 경고가 끝나면 조용히 돌려놓는다. 소리는 직접 내지 않는다. |
| `Scripts/App/UI/Drone/DroneUrgentBubbleRule.cs` | 심각한 긴급 목록에 `DroneOxygenWarning` 추가. |
| `Scripts/App/Core/Data/DataIds.cs` | `Dialogue.DroneOxygenWarning = "dialogue.drone.oxygen.warning"`. |
| `Data/Dialogue/Dialogue_Drone_Oxygen_Warning.asset` (새), `Data/Catalog/GameDataCatalog.asset` | 새 대사 템플릿(situation `oxygen_critical`, priority 650)과 카탈로그 dialogues에 한 줄 추가. Editor API로 만들었고 YAML은 손으로 고치지 않았다. 카탈로그 검증 오류 0. |
| `Editor/DataValidation/MvpDataAssetBuilder.cs` | 빌더를 다시 돌려도 같은 템플릿이 유지되게 `BuildDialogues`에 항목을 추가했다. |
| `Tests/EditMode/App/UI/PromptB213LowOxygenDroneWarningTests.cs` (새) | 12개(아래). |
| `init/rules/lessons/2026-11-26-issue351-timed-capture-short-ui.md` (새) | 짧게 뜨는 UI 캡처 교훈. |

## 규칙 정리
- **임계값 출처**: `PlayerSurvivalController.OxygenRisk == OxygenRiskLevel.Critical`. `PlayerSurvivalState.GetRisk`가 `settings.OxygenCriticalRatio`(기본 0.15, `<=`)로 정한다. 0.15를 하드코딩하지 않았다. 구출 표식(`EmergencyRescueService` 산소 조건)과 같은 값이다.
- **발사**: Safe/Caution → Critical로 내려가는 순간 한 번. Critical에 머무는 동안은 반복하지 않는다. Caution(35%)만으로는 띄우지 않는다.
- **재무장**: Critical 위(Caution/Safe)로 회복되면 다시 무장한다.
- **로드·이어하기**: 바인딩 시점이나 저장 산소 복원 직후 이미 Critical이면 무장하지 않는다(즉시 발사 없음). 회복한 뒤에만 다시 무장한다.
- **전력 0**: 컨트롤러가 전력을 보지 않는다. 구출 표식만 뜨고 이 경고는 없다.
- **자동 닫힘**: 6초(`LowOxygenDroneWarningController.autoCloseSeconds`, 검사기에서 조절). 말풍선 유지도 6초(`urgentVisibleSeconds`).
- **대사**: 「산소가 부족합니다. 위로 올라가거나 긴급 탈출(구출 요청)을 쓰세요.」. 카탈로그 템플릿을 쓰고, 없으면 코드 상수로 폴백한다. 분석 루프(`DroneAnalysisService`)에는 넣지 않았다.
- **SFX**: 새 음원·`SfxId`·`SfxLibrary` 변경 없음. 말풍선이 기존 `SfxId.DroneBubbleUrgentSerious`를 한 번 낸다. 터미널이 열릴 때의 기존 `WindowTvOpen`과, 분석 결과가 긴급일 때 원래 나던 `DronePriorityAlert`는 그대로 둔다. 경고 경로에서 소리를 직접 더 부르지 않는다.
- **건드리지 않은 것**: `DamageHitGlitchController`(B-207), 구출 가능 조건, 산소 소모, HUD 산소 게이지, `timeScale`, 입력·채굴, 카메라. 화면 전체 연출은 없다.

## 테스트
- Unity 컴파일 오류 0. 새 타입 4개가 각 어셈블리(`SubTerra.App`, `SubTerra.App.GameplayIntegration`, `SubTerra.App.Tests.EditMode`)에 들어간 것을 확인했다.
- 새 EditMode 12개(`PromptB213LowOxygenDroneWarningTests`), 모두 통과:
  - 정책: 첫 하강 한 번·유지 중 반복 없음 / 회복 후 재무장·재발사 / 로드 때 이미 Critical이면 발사 없음
  - 컨트롤러(실제 `PlayerSurvivalController` + `TrySetDebugOxygenPercent`): 50→14→8→3→100→16→15 순서로 발사가 1→1→1→1→재무장→1→2인지(15%는 Critical, 16%는 아님) / 산소가 임계값 위면 발사 없음 / Critical로 바인딩하면 발사 없음, 회복 후에만 발사 / 저장 복원으로 Critical이 되어도 발사 없음 / 자동 닫힘 기본 6초
  - 템플릿이 심각한 긴급으로 분류되는지 / 말풍선 경고가 심각한 긴급음 하나만 내고 분석 대사에 덮이지 않는지 / 후광 깜빡임 범위·조각이 좌우 가장자리에만 있고 결정적인지 / 후광 `fillCenter=false`·모든 연출 이미지 `raycastTarget=false`·`Hide`로 모두 꺼지는지
- 관련 묶음 EditMode(Drone, Audio, Data, Run, Progression, Debug, B85·B135 구출, B189 말풍선, HudPanelChrome, B207 + B213): **776 통과 / 7 실패**.
  - 실패 6개(`PromptB125AdvancedCommandTests.Help_…`, `PromptB125TerminalTests` 3개, `PromptB193DebugCommandTests.Falling_Cushion…`, `PromptB101GoldGainTests.Purchases…`)는 내 변경을 stash로 뺀 기준 main에서도 똑같이 실패한다(기존 실패).
  - `PromptB85EmergencyRescueChipTests.PromptB85_ChipFollowTick_PlacesChipAbovePlayerHead`는 큰 묶음에서만 실패했다. 내 변경을 넣은 채 B85+B213만 돌리면 **17/17 통과**라서, 실행 순서(화면 크기 상태)에 따라 흔들리는 기존 문제로 본다.
- 테스트 실행이 다시 저장한 `NotoSansKR-Regular_SDF.asset`, `InventoryPanel/SurfaceBasePanel/ViewSocket.prefab`, `Mine_Demo_Integration.unity`는 `git restore`로 되돌렸다.

## 플레이 확인 (Bootstrap → slot 1 `BeginContinue` → 광산 35m)
세이브 폴더를 백업하고, 끝난 뒤 slot 1 파일 3개를 바이트 단위로 되돌렸다(해시 일치 확인).
- 바인딩 직후 컨트롤러는 무장 상태(산소 Safe).
- `TrySetDebugOxygenPercent(14)`(`O2 14`와 같은 경로): 발사 1회. 말풍선과 터미널 모두 경고 상태, 터미널 자동 열림, `DroneBubbleUrgentSerious` 0.01초 전 재생, `timeScale` 1. 같은 프레임에 머리 위 「구출 요청」 표식도 떴다.
- 화면 캡처: 말풍선은 빨간 외곽선, `DIGGER-BOT // O2 CRITICAL`/`O2 LOW`, 빨간 본문, 좌우 가장자리 빨간 글리치 조각. 터미널은 빨간 헤더 `... // O2 CRITICAL_`, 빨간 테두리·후광, 좌우 가장자리 글리치, 경고 문장 전체. 화면 흔들림이나 화면 전체 글리치는 없다.
- 5%·3%로 더 낮춰도 재발사 없음. 100% → 재무장, 16% → 발사 없음, 15% → 재발사.
- 타이밍 로그: 1.7·2.0·4.0초 `open=True warn=True`, 6.6초 `open=False phase=Hidden warn=False`. 약 6초에 자동으로 닫힌다.
- TAB 경로(`ToggleDiggerBot`): 경고 중 닫힘 → 다시 열기 → 8.5초가 지나도 열린 채. 다시 연 창은 자동으로 닫지 않는다.
- 모션 감소 켜고 발사: 말풍선·터미널 경고, 가장자리 글리치, 후광이 모두 나온다(캡처 확인).
- 전력만 0(`SetCurrentEnergy(0)`, 산소 100%): 구출 가능 true, 경고 발사 0.
- 정지 중 하강(디버그 콘솔 정지 소유자 획득 상태): 발사는 대기(`pending`)로 남고 창은 열리지 않는다. 정지를 풀자 몇 프레임 안에 말풍선·터미널이 떴다.
- 경고 중 `UiPauseGate.IsHeld=False`, `IsInputBlocked=False`, `timeScale=1`, `PlayerMovement.enabled=True`.
- Console 새 빨간 오류 0. 기존 경고(스테레오 카메라, 오디오 리스너)만 있다.
- 참고: 캡처 중 체력이 230 → 102로 줄었다. 산소를 14%로 둔 채 1분쯤 지나 산소가 0이 되어 생긴 기존 산소 0 피해 틱이다. 다시 살펴볼 때 3초 동안 피해 이벤트는 0이었고 체력은 재생 중이었다.

## 미검증 (직접 보지 못함)
- **소리를 귀로 듣지 않았다**(재생 기록 `SecondsSinceLastPlayed`만 확인).
- 실제 키보드 TAB·디버그 콘솔 입력. 같은 메서드(`ToggleDiggerBot`, `TrySetDebugOxygenPercent`)를 스크립트로 불렀다.
- 경고 중 실제 걷기·점프·채굴 손맛. 입력 차단·정지 플래그가 꺼져 있는 것만 확인했다.
- 실제 이어하기로 Critical 저장을 불러오는 흐름(EditMode 복원 테스트로만 확인).
- 관찰: 드론이 플레이어 왼쪽에 있으면 말풍선 오른쪽 끝이 「구출 요청」 표식과 겹쳐 문장 끝이 일부 가려졌다. 둘 다 기존 위치 규칙(드론·플레이어 추적)을 따르고, 이번 범위(구출 표식 미변경)라 손대지 않았다. 터미널에는 전체 문장이 보인다.

## 사용자 테스트 가이드
1. 10m보다 깊은 광산에서 `O2 14`(또는 `O2 15`)를 입력하고 콘솔을 닫는다. 머리 위 「구출 요청」과 같은 타이밍에 빨간 말풍선과 빨간 터미널이 자동으로 한 번 뜨는지, 「산소가 부족합니다. 위로 올라가거나 긴급 탈출(구출 요청)을 쓰세요.」가 보이는지 본다.
2. 경고가 떠 있는 동안 걷기·점프·채굴이 되는지, 게임이 멈추지 않는지 본다.
3. 말풍선·터미널 가장자리만 빨갛게 찢기듯 흔들리고 화면 전체는 흔들리지 않는지, 터미널 둘레 빨간 후광이 깜빡이는지, 기존 심각한 긴급 경고음이 한 번 나는지 본다. 설정에서 화면 효과 감소를 켜고 같은 경고를 띄워 경고 자체는 나오는지 본다.
4. TAB으로 터미널을 닫을 수 있는지 본다. 다시 떨어뜨린 뒤(6번) 그대로 두면 약 6초 뒤 닫히는지 본다.
5. 산소를 낮춘 채로 더 기다려도 경고가 다시 뜨지 않는지 본다.
6. `O2 100` 뒤에 `O2 14`로 다시 떨어뜨려 한 번 더 뜨는지 본다. `O2 16`만으로는 안 뜨는지도 본다.
7. `O2 100` 상태에서 `energy 0`. 구출 표식만 뜨고 산소 경고는 없는지 본다.
8. Unity Console에 새 빨간 오류가 없는지 본다.
