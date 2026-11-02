# B-178 결과: 퀘스트 클리어·심층 해금 작은 글리치 창 + 보상 지급음 + 드론 스캔 "핑"

## 기준
- 브랜치 `MVP2-fix123`, 기준 `MVP2-fix122`(`ce7a9175`, **B-177 포함**). `git fetch` 시점 원격 최신은 fix122.
- 음원 묶음은 `work_process/MVP2/sound/sub-terra-sfx-B178/sfx-B178/`에 있었다(프롬프트의 `sfx-B178/` 경로와 한 단계 다름). 그 폴더를 그대로 썼다.
- 기존 SfxId 번호·칸 값·버스 음량, 음원 파일, 창 연출 시간표, 보상 규칙, 드론 스캔 주기·링 연출, 세이브 형식은 바꾸지 않았다.

## 음원·칸
| 칸 | 이름 | 파일 | 버스 | 음량 | 길이(임포트) |
|---|---|---|---|---|---|
| 87 | QuestReward | `Audio/SFX/UI/quest_reward.wav` | UI | 0.5 | 1.10초 |
| 88 | GlitchPopupOpen | `Audio/SFX/UI/glitch_popup_open.wav` | UI | 0.6 | 0.90초 |
| 89 | GlitchPopupClose | `Audio/SFX/UI/glitch_popup_close.wav` | UI | 0.6 | 0.70초 |
| 90 | DroneScanPing | `Audio/SFX/World/drone_scan_ping.wav` | World | 0.3 | 0.90초 |

- `Shared/Audio/SfxId.cs`(87~90), `App/Audio/SfxLibraryDefaults.cs`(파일 끝, 길이 주석), `Resources/SfxLibrary.asset`(87~90, `SubTerra/Audio/Create Or Refresh Sfx Library` 빌더로 추가) 세 곳이 같다.
- 임포트: 기존 `PromptB156SfxPostprocessor` 그대로(모노, DecompressOnLoad, Vorbis 70%).
- `init/sound-effect-address.md` 표 끝에 `address-rows.md` 4줄 추가.
- **못 함: 작업 폴더 쪽 기본 wav 4개 삭제.** 삭제 명령이 자동 권한 판정에서 거부되어 그대로 남아 있다(게임 폴더 파일과 같은 바이트). 이 폴더는 git 추적 밖이므로 커밋 내용에는 영향 없음. 필요하면 직접 지우면 된다: `quest_reward.wav`, `glitch_popup_open.wav`, `glitch_popup_close.wav`, `drone_scan_ping.wav`. `_heavy` wav·mp3·README·`address-rows.md`는 그대로 둔다.

## 붙인 곳 (줄은 이번 브랜치 기준)
### A. 작은 글리치 창(퀘스트 클리어 + 심층 해금, 한 곳)
- `App/UI/Tutorial/StartBriefingPopupMotion.cs`
  - 상수 `CompactOpenSfx`·`CompactCloseSfx` ~21-22, `ReopenOpenSfxDelay = 0.35f` ~24
  - `ConfigureCompact`에서 `playsCompactSfx = true`(브리핑 소리는 지금처럼 끔)
  - 열림: `BeginIntro` ~405 → `BeginCompactOpenSfx` ~428 (첫 지직 0초에 GlitchPopupOpen 한 번)
  - 닫힘: `RequestClose` ~280, 받아들여질 때 ~293 GlitchPopupClose 한 번. 닫는 중 재호출·X 연타는 `TryBeginClose`에서 막혀 소리도 한 번.
  - 늦춘 열림음: `DelayNextOpenSfx` ~423, `TickPendingOpenSfx` ~443(Tick ~317에서 확인). 기다리는 동안 닫히면(`RequestClose`) 취소, 꺼지면(`OnDisable`) 버림.
  - 무음: 씬을 열 때 꺼진 채 시작·`Configure`·`ResetVisuals`·`OnEnable`에서는 소리를 내지 않는다. 에디트 모드는 `PlayCompactSfx` ~458에서 `Application.isPlaying`으로 막는다(검사 전용 스위치 `CompactSfxInEditModeForTests`만 예외).
  - 표시 중 짧은 지직(`PlayShortGlitchSfx`)은 작은 창에서 계속 무음.
- `App/UI/Tutorial/QuestClearPopupMotion.cs` `Open` ~49: 닫히던 창을 다시 열면 `DelayNextOpenSfx(ReopenOpenSfxDelay)`.
- 심층 해금 창(`DeepZoneUnlockPopupMotion.Configure` → `ConfigureCompact`)은 코드 변경 없이 같은 길을 쓴다.
- 모션 감소: 두 창과 `StartBriefingPopupMotion`에는 모션 감소로 연출을 건너뛰는 경로가 없음 → **해당 없음**(설정과 무관하게 항상 같은 연출·소리).
- 등장 전(Waiting)에 바로 닫히는 경우(`RequestCompactClose`가 Intro를 건너뜀)는 열림음 없이 닫힘음만 난다(기존 연출 흐름 그대로).

### B. 보상 지급음
- `App/UI/Tutorial/DemoObjectivePresenter.cs` `OnGrantResolved` ~340: `ShouldPlayRewardSfx(result)`(~358, `Granted && !Reward.IsEmpty`)일 때 QuestReward 한 번.
- `ConfirmClaim` 반환값은 쓰지 않는다. `NeedsCapacity`는 위에서 바로 return, `Forfeited`·`AlreadySettled`·`NothingPending`·빈 보상은 규칙에서 무음. 버리기로 공간을 만든 뒤 `RetryPending` → `Settle` → `GrantResolved`에서 한 번.
- 금 보상과 금화 팝업 소리: 금화 팝업(`GoldPickupVfx`/`GoldPickupSfx`의 GoldLever 등)은 `MiningSystem.TileMined`에만 묶여 있어 퀘스트 금 보상(`GameState.AddGold`)으로는 나지 않는다(코드 기준 확인, 고치지 않음).
- B-177이 지상 퀘스트 버튼에 붙인 임시 UiClick은 그대로.

### C. 드론 스캔 "핑"
- Gameplay `Gameplay/Drone/DroneSensor.cs`: 이벤트 `ScanPulseStarted(bool immediate)` ~70. `TickScanPulse`에서 `pulseView.Show` 직후 ~254-258에 한 번 낸다. `immediate`는 `SetScanWorldReady`·`SetUpgradeEffects`가 타이머를 0으로 되돌린 뒤 바로 오는 즉시 스캔(필드 `pulseTimerReset` ~45, ~92, ~100). Gameplay는 App을 참조하지 않는다.
- App(Integration) `App/Integration/DroneScanPingSfx.cs`(새 파일)
  - 순수 판단 `DroneScanPingRule.ShouldPlay`, 상수 `SkipIfRecentSeconds = 0.6f` ~15, `Pitch = 1f`
  - 생략: 즉시 스캔(첫 펄스) / ElevatorArriveDing·UpgradeLevelUp이 0.6초 안 / 광산 설정창 열림(`UndergroundMenuController.IsSettingsOpen`) / `UiPauseGate.IsHeld`. 생략은 그 회차만이고 늦춰 내지 않는다.
  - `DroneScanPingSfx.HandlePulseStarted` → `SfxPlayer.Play(SfxId.DroneScanPing, 1f)` ~89
- 연결: `App/Integration/IntegrationRuntimeBinder.cs` `BindWorldSfx` ~920-929, 정리 `OnDestroy` ~1007.
- `App/Audio/SfxPlayer.cs` `SecondsSinceLastPlayed(SfxId)` ~79: 읽기 전용 조회 추가(멈춤 API는 추가하지 않음).
- 참고: 스캔 업그레이드가 없다가 광산에서 처음 사면 다음 프레임에 펄스가 바로 오는데, 이 회차는 "즉시 스캔"이 아니라 레벨업 0.6초 규칙으로 생략된다.

## 심층 해금 창과 B-169 해금 소리 겹침 확인(고치지 않음)
- 심층 해금 창은 `ProgressionPanelBinder.OnDeepZoneAccessChanged`(구매 성공 같은 프레임)에서 열리고, 열림음은 다음 프레임 끝(`CaptureThenStart`)에 난다. 같은 순간 구매의 `UpgradeLevelUp`(UpgradeTreeView ~748)이 난다.
- 심층 썸네일 해금 연출(`DeepZoneThumbnailStrip`의 Rattle·Click·Glow)은 B-167 `IsDeepZoneRevealBlocked`로 창이 보이는 동안 미뤄지므로 창 열림음과 겹치지 않는다.
- 같은 구매로 풀리는 **다른 노드 썸네일**(`UpgradeTreeNodeView`의 해금 연출)은 미루지 않아 Rattle(0.2초쯤)이 창 열림음과 겹칠 수 있다(코드 기준, **미검증**).

## 테스트
- 새 EditMode `Tests/EditMode/App/Audio/PromptB178SfxTests.cs`
  - 칸 87~90 이름·번호·버스·음량·파일, wav 4개가 `Audio/SFX/UI`·`World`에 모노·DecompressOnLoad로 있음
  - 작은 창: 열기 1번 GlitchPopupOpen, 닫기 1번 GlitchPopupClose, 닫는 중 다시 닫기 0번, 브리핑 소리 0번, 에디트 모드 무음 / 시작 브리핑 창은 기존 소리 그대로·새 소리 0번
  - 연달아 열기: 닫힘 1번 → 0.30초 0번 → 0.40초 1번, 그 사이 꺼지거나 닫히면 0번, `QuestClearPopupMotion.Open`이 닫는 중에만 지연을 건다
  - 보상: Granted 1번(다시 눌러도 1번), NeedsCapacity·Forfeited 0번, 빈 보상·그 밖 상태 0번, 버리기 뒤 RetryPending 지급 1번
  - 드론: 규칙(첫 펄스·0.6초 안 딩/레벨업·설정창·UiPauseGate), `DroneScanPingSfx` + 검사용 `SfxPlayer`로 첫 펄스 0번 → 다음 회차 1번 → 생략 → 다음 회차 다시 1번
- Gameplay EditMode `DroneSensorUpgradeTests.B178_ScanPulseStarted_MarksImmediatePulseAfterReset`: 즉시 스캔 true → 30초 회차 false, 월드 준비 뒤 다시 true.
- 기대값을 바꾼 기존 검사(칸 수 85 → 89, B-178 4칸 추가 때문):
  - `PromptB156SfxTests`(칸 수 검사), `PromptB165SfxD1FixTests`(칸 수 메시지), `PromptB168SfxD2Tests.Defaults_D2SlotsNumberedFrom66_MatchTableAndAsset`
- 실행 결과(에디터의 `SubTerra` 플래그 실행기 사용)
  - 관련 EditMode(B-178·B-156·B-162·B-165·B-168·드론 센서·화물 B-152/B-153·시작 브리핑 B-120): **136 통과 / 1 실패** (`sub-terra/Temp/b178-targeted-final.txt`). 실패 1개는 아래 기존 실패 `PromptB162SfxCStageTests.Defaults_CSlotsMatchTable_NumberedFrom35_NoAlertBus`.
  - 최종 전체 EditMode: **2261 통과 / 25 실패** (`sub-terra/Temp/b178-full-final.txt`). 25개 모두 `work_process/MVP2/B-172-verification/editmode-full.txt`(2026-10-29 전체 실행)의 실패 목록에 있는 기존 실패이며 **기준 밖 새 실패 0개**.
    - 그중 `PromptB150MainMenuTests.DeleteButton_Hover_FadesInWhiteTrashIcon`은 B-177 보고서에 적힌 프레임 시간 의존 불안정 검사로, 이번 첫 전체 실행에서는 통과했다.
    - `PromptB162…NoAlertBus`: 기준 브랜치 `SfxLibrary.asset`의 41번(BriefingClose) 음량이 1.0인데 검사 표는 0.5. 이번에 건드리지 않은 기존 불일치.
  - 첫 전체 실행에서 `PromptB178SfxTests.Reopen_DisabledOrClosedDuringDelay_NoOpen`이 실패했다. 검사가 먼저 연 창의 열림음 1회를 지우지 않고 센 검사 쪽 실수였고, 고친 뒤 통과했다(`init/rules/lessons/2026-11-01-issue158-test-sound-count-reset.md`).
  - 테스트 실행이 범위 밖 파일(`InventoryPanel`·`MainMenuPanel`·`ViewSocket` 프리팹, `Mine_Demo_Integration.unity`, `ProjectSettings/TimeManager.asset`, `work_process/MVP2/103-main-menu/*.png` 2개)을 바꿔 놓아 `git restore`로 되돌렸다. 작업 전부터 있던 `init/prompt-B.md`·`NotoSansKR-Regular_SDF.asset`·`EditorBuildSettings.asset`·`QualitySettings.asset` 변경은 사용자 것이라 손대지 않고 커밋에도 넣지 않는다.
- PlayMode: 고친 PlayMode 검사가 없어 실행하지 않음.

## 미검증
- 실제 플레이에서 소리를 들어 보는 확인은 하지 못했다(아래 사용자 테스트 가이드 1~12 전부 **미검증**). 특히 음량 밸런스(드론 0.3, 글리치 0.6), 연달아 클리어 때 0.35초 간격의 체감, 노트북 스피커에서 글리치가 얇게 들리는지.

## 사용자 테스트 가이드
디버그 터미널은 플레이 중 **Ctrl + `**(1 왼쪽 키).
1. 광산에서 퀘스트를 하나 완료한다 → "퀘스트 완료" 창이 지지직 뜰 때 짧은 글리치 소리가 한 번 난다. 시작 브리핑 창보다 가볍다.
2. 완료 창을 닫기 버튼이나 X키로 닫는다 → 닫힘 글리치 소리와 함께 보상 "띠-링" 소리가 한 번 난다. X키를 연타해도 쌓이지 않는다.
3. 퀘스트 두 개를 연달아 완료한 상태에서 닫는다 → 보상 소리 뒤 조금 있다가 다음 창의 열림 소리가 난다(한꺼번에 뭉개지지 않음).
4. 화물칸이 꽉 찬 채로 보상을 받으면 "화물 공간 부족" 창이 뜨고 보상 소리는 나지 않는다. 버려서 공간을 만든 뒤 받으면 그때 보상 소리가 한 번 난다. 포기하면 소리가 없다.
5. 업그레이드로 심층 구역이 열리면 "심층 해금" 창이 뜰 때·닫을 때 1·2번과 같은 글리치 소리가 난다(보상 소리는 없음).
6. 드론 스캔 업그레이드가 있는 상태로 광산에 들어간다 → 도착 "딩"과 스캔 소리가 겹치지 않는다(처음 한 번은 링만 보임).
7. 광산에서 30초쯤 기다린다 → 링이 퍼지기 시작할 때 작은 "핑"이 한 번 난다. 계속 들어도 거슬리지 않는 크기인지 본다.
8. 광산에서 ESC로 설정창을 연 채 30초 넘게 기다린다 → 링은 돌아도 "핑"은 나지 않는다. 닫고 나면 다음 회차부터 다시 난다.
9. 광산에서 업그레이드를 사서 레벨업 소리가 날 때 스캔 소리가 바로 겹쳐 나지 않는다.
10. 설정의 모션 감소를 켜도 1·2·5번 창 소리가 한 번씩 난다.
11. 효과음 음량·음소거가 새 소리 4개에 다 적용되고, Unity Console에 새 빨간 오류·노란 경고가 없다.
12. 글리치 소리가 너무 얇게 들리면 `work_process/MVP2/sound/sfx-B178/`의 `_heavy` 파일(저음 원본)을 들어 보고, 바꿀지 PM에게 알려 준다. (이번 묶음의 실제 위치: `work_process/MVP2/sound/sub-terra-sfx-B178/sfx-B178/`)

## #158 대응 표
| 항목 | 상태 | 비고 |
|---|---|---|
| D-2 심층 구역 해금 팝업 | 구현 | 작은 창 공통 경로로 열림·닫힘 |
| D-2 드론 스캔 | 구현 | 링 시작 "핑", 첫 펄스·겹침·설정창·UiPauseGate 생략 |
| E 퀘스트 클리어 창 열림·닫힘 | 구현 | 연달아 열기 0.35초 지연 |
| E 보상 지급음 | 구현 | GrantResolved(Granted·비지 않은 보상) |
| E 화물 "공간 확보 뒤 한 번 지급" | 구현 | RetryPending 지급 때 한 번, 부족·포기 무음 |
