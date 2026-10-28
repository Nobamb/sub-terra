# B-168 효과음 D-2 결과 (위험·생존, 시설 설치·업그레이드, 실패음 공통 입구)

- 기준 커밋: `7de51747` (MVP2-fix109, D-2 음원·주소 추가). `f71535a8`(B-167) 조상 확인함(`git merge-base --is-ancestor` 통과)
- 브랜치: `MVP2-fix109`
- PR: 아직 없음(커밋·PR은 사용자 확인 후. 이 PC의 bash에는 `gh`가 없다)
- 음원 15개는 폴더에 모두 있었고 고치지 않았다. `init/sound-effect-address.md`에 15줄(+B-169 4줄)이 이미 있다.
- 작업 분담: 칸 15개 추가(SfxId·Defaults·기존 칸 수 검사 2곳)와 EditMode 검사 `PromptB168SfxD2Tests` 작성은 하위 에이전트(Haiku)에게 줄 단위 지시로 맡기고, 결과 diff를 검토했다. 판정·연결 코드와 PlayMode 검사는 직접 했다.

## 1. 새 칸 (이름은 표와 같다)

| 번호 | SfxId | 파일 | 버스 | 음량 | 연결 |
|---|---|---|---|---|---|
| 66 | GasPulseLoop | gas_pulse_loop | World | 0.45 | ① |
| 67 | PlayerHurt | player_hurt | World | 0.6 | ② |
| 68 | LowHealthWarning | low_health_warning | World | 0.5 | ③ |
| 69 | CeilingCollapse | ceiling_collapse | World | 0.7 | ④ |
| 70 | RescueHoloOpen | rescue_holo_open | World | 0.5 | ⑤ |
| 71 | BuildPlaceSmall | build_place_small | World | 0.65 | ⑥ |
| 72 | BuildPlaceHeavy | build_place_heavy | World | 0.6 | ⑥ |
| 73 | BuildFailResource | build_fail_resource | UI | 0.5 | 2, ⑦ |
| 74 | BuildFailBlocked | build_fail_blocked | UI | 0.55 | 2, ⑦ |
| 75 | UpgradeLevelUp | upgrade_levelup | UI | 0.55 | ⑦ |
| 76 | UnlockRattle | unlock_rattle | UI | 0.5 (maxDuration 0.3, fadeOut 0.1) | 칸만(B-169) |
| 77 | UnlockClick | unlock_click | UI | 0.7 | 칸만(B-169) |
| 78 | UnlockGlow | unlock_glow | UI | 0.45 | 칸만(B-169) |
| 79 | WindowTvOpen | window_tv_open | UI | 0.4 | 칸만(B-169) |
| 80 | WindowTvClose | window_tv_close | UI | 0.5 | 칸만(B-169) |

- `Scripts/Shared/Audio/SfxId.cs:75-89`, `Scripts/App/Audio/SfxLibraryDefaults.cs:185-220`(요약 주석 64→79칸). 3번은 비워 둠, 1~65 값 그대로.
- `Resources/SfxLibrary.asset`: 에디터에서 빌더(`PromptB156SfxLibraryBuilder.EnsureAsset`)로 채웠다. 15칸 모두 클립·버스·음량·재생 구간이 Defaults와 같다(검사로 확인). 추가 120줄, 삭제 없음.
- 가져오기: 15개 모두 모노(채널 1), gas_pulse_loop는 반복 파일이 아니라 DecompressOnLoad(맥박마다 한 번씩 틀므로 `LoopFileNames`에 넣지 않음).
- `upgrade_lock_*`·`upgrade_window_suck_in` 4개는 칸을 만들지 않았다(.meta만 에디터가 생성, 커밋에서 뺄 예정).
- 기존 칸 수 검사 고침: `PromptB156SfxTests.cs:286`, `PromptB165SfxD1FixTests.cs:91` (64→79).

## 2. 실패음 공통 입구 `FailureFeedback`

- 새 파일 `Scripts/App/Audio/FailureFeedback.cs`
  - `FailureFeedback.Play(SfxId, key)`: 같은 프레임에는 id와 상관없이 한 번, 같은 key는 0.3초 안이면 무시. 시계·프레임은 `ResetForTests(clock, frame)`로 주입.
  - `BuildingPlacementSfx.ResolvePlaced`(사다리·버팀목·조명 → Small, 그 밖 → Heavy), `ResolveFailure`(자원 부족·`cannot_afford`·`spend_failed` → Resource, 그 밖 → Blocked).
- **시설 설치 실패**: `App/UI/Building/BuildingMenuPresenter.cs:235-247` Failed 분기. 마우스 클릭(`TryPlaceAt` → `OnPlacementRejected` → Publish)과 C 단축키(`TryPlaceNearestByShortcut`, 후보 없음 Publish)가 모두 이 분기를 지난다. 자원 부족은 같은 자리의 `CanAfford` 판정 결과를 그대로 쓴다.
  - 노란 경고: **원래 보였다.** PlayMode 검사에서 실제 광산 씬의 `PlayerHeadMessageStack`(=`PlayerStatusWarnings.Current`)에 `building.placement-failed` 항목이 Warning 스타일, Level 1로 들어가는 것을 확인했다("자원이 부족합니다."). 고치지 않았다.
- **시설 이동 실패**: `App/Integration/GameplayBuildingPlacementBridge.cs:498-513` `OnPlacementRejected`의 이동 분기에서만 `building.move-failed` 노란 경고 + BuildFailBlocked(key `building.move`). 마우스를 올려 둘 때의 문구(~284 `MoveStatusChanged`)는 그대로 무음.
- **OutpostPanelPresenter**: `App/UI/Outpost/OutpostPanelPresenter.cs:78, 88`을 `FailureFeedback.Play(UiCancelFail, "outpost.facility")`로 옮겼다. 한 번 누를 때 한 번 재생·같은 문구라는 동작은 같다. 달라진 점은 0.3초 안 연타가 한 번으로 합쳐지는 것뿐이다.
- **MiningProgressHud**(~111-116): 옮기지 않았다. `MiningFailSfxGate`가 이미 같은 이유 반복을 막고, 이유가 바뀌면 바로 다시 내야 하는데, 공통 입구의 0.3초 key 규칙을 거치면 그 규칙이 달라질 수 있어서다.

## 3. 소리마다 연결한 곳

| 소리 | 클래스·메서드·줄 | 내용 |
|---|---|---|
| ① 가스 맥박 | 새 `App/Integration/GasPulseAudio.cs`(`GasPulseGate`, `GasPulseAudio`), 붙이기 `IntegrationRuntimeBinder.BindWorldSfx` 894-898 | `GasPoisonOverlayView`에 붙어 LateUpdate에서 타임라인 위상을 읽는다. 우세 단계 위상이 0.5를 넘는 프레임에 한 번, 따로 AudioSource 하나로 Stop→Play(쌓이지 않음). Caution 피치 1.0, Critical 1.08. 음량 칸×버스×효과음 설정. 멈춤: 일시정지(`UiPauseGate.IsHeld‖ControlPreferences.IsSettingsOpen`)·`RunFailureRuntimeController.IsHandling`·`CanAct==false`·`ElevatorController.IsRiding`·런 Returning/Completed·`Target==Safe`·음소거·OnDisable. 멈출 때 0.08초 페이드, 풀리면 기준 위상을 새로 잡아 다음 맥박부터. |
| ① 타임라인 | `App/UI/HUD/GasPoisonOverlayTimeline.cs:123-138` | 읽기 전용 `DominantLevel`·`DominantPhase`만 추가. 값 계산은 그대로. |
| ② 피격 | `Gameplay/Player/PlayerSurvivalController.cs:261-269`(`ApplyDamage`), 판정 `ResolveHurtSfx` 288-302 | `StartDamageFeedback`과 같은 프레임. 실제 피해 > 0이고 행동불능이 되지 않았을 때, 원인이 붕괴(`ApplyCollapse`·`ApplyCollapseImpact`)·가스(`ApplyGasFailure`)일 때만. 낙하(LandHeavy가 맡음)·마지막 한 방·전력 고갈은 무음. |
| ③ 체력 30% 경고 | 새 `App/Integration/LowHealthWarningAudio.cs`(`LowHealthSfxGate`, `LowHealthWarningAudio`), 붙이기 `IntegrationRuntimeBinder.BindWorldSfx` 900-909 | 생존 컨트롤러 오브젝트에 붙어 `HealthChanged`를 구독. `Current <= Maximum×0.3`을 줄어서 처음 넘을 때 0.25초 뒤 한 번. 그 사이 체력 0·실패 처리·행동불능이면 취소. 30% 위로 회복해야 다시 낸다. 첫 관측·최대 체력만 바뀐 경우 무음. HudBinder(App)는 실패 처리 상태(GameplayIntegration)를 볼 수 없어 바인더 쪽에 붙였다. |
| ④ 천장 붕괴 | `Gameplay/Structural/StructuralRiskFeedback.cs:59-65`(`OnCollapseTriggered`), 판정 `ShouldPlayCollapseSfx` | 돌이 닿는 순간(`CollapseFallSeconds` 0.18초 뒤) 이벤트당 한 번. 낙하 시작으로 옮기지 않았다(0.18초라 차이가 작음). 위험 경보와 별개, Alert 버스 안 씀. |
| ⑤ 구조 홀로그램 | `App/UI/EmergencyRescue/EmergencyRescueChipView.cs:140-141`(`Show`) | 이미 보이는 중이면 위에서 돌아가므로 펼칠 때 한 번. 첫 고갈(`OnPopupClosed` ~197)과 두 번째 고갈(~226) 모두 `SetChipVisible(true)` → `Show`를 지난다. 드론 안내 말풍선 경로에는 소리가 없어(코드 검색) 겹치지 않는다. |
| ⑥ 설치·이동 성공 | `GameplayBuildingPlacementBridge.cs:462-463`(`OnBuildingPlaced`), 477-478(`OnBuildingMoved`) | 사다리·버팀목·조명 Small, 그 밖 Heavy. 세이브 복원은 `BuildingRestored` 이벤트라 소리가 나지 않는다. |
| ⑦ 업그레이드 | `App/UI/Progression/UpgradeTreeView.cs:711-712`(성공), 747-748(실패), 판정 `ResolvePurchaseFailureSfx` | 성공은 UpgradeLevelUp 한 번(새로 풀린 노드 소리는 B-169). `InsufficientResources` → BuildFailResource, `Locked` → BuildFailBlocked, key `upgrade.purchase`, 노드 흔들림과 같은 프레임. UiCancelFail은 이 경로에 쓰지 않는다. |

## 테스트

- 새 EditMode `PromptB168SfxD2Tests` 8개 통과. 프롬프트가 적은 이름 `PromptB167SfxD2Tests`는 B-167(업그레이드 창 연출) 검사와 헷갈려 B-168로 붙였다.
  - 칸 표·asset 값·모노 가져오기, FailureFeedback(같은 프레임·0.2/0.35초·다른 key), 설치 실패 두 톤과 0.5초 간격 3번, 업그레이드 실패 톤, 피격(가스·붕괴 1번, 낙하·마지막 한 방 0번), 체력 경고(시나리오 5가지), 가스 맥박 판정(위상 넘김·프레임 튐·감싸기·막힘 뒤 다음 맥박·단계 변경), GasPulseAudio(일시정지·탑승·실패 처리·조작 불가에서 멈춤, Critical 피치 1.08, Safe에서 무음)
- EditMode 전체(15:01): 2093 통과 / 24 실패. 이 중 이번 변경으로 새로 실패한 3개를 고쳤다.
  - `PromptB158SfxFixTests.RiskFeedback_NoLongerPlaysAlarm`: StructuralRiskFeedback.cs에 `SfxRequests.Play(`가 없어야 한다는 정적 검사. 경보가 없어야 한다는 뜻은 그대로 두고 `SfxRequests.Play(SfxId.CeilingCollapse)`만 허용하도록 고쳤다.
  - `OutpostPanelPresenterTests.ToggleInteractionPanel_DisconnectedCharger_PlaysCancelFailOnce`, `PromptB99FacilityCooldownTests.PromptB158_CooldownToggle_PlaysCancelFailOnce`: EditMode 검사는 같은 프레임에 이어 돌아서 앞 검사의 실패음 기록에 막혔다. 누르기 직전에 `FailureFeedback.ResetForTests(null, null)`을 부르도록 고쳤다.
  - 고친 뒤 Audio 묶음 전체·Outpost·B99·BuildingMenuPresenter·B85 칩·StructuralFeedback 192개: 190 통과 / 2 실패. 2개(`PromptB162…Defaults_CSlotsMatchTable…` BriefingClose 0.5/1.0, `BuildingMenuPresenterTests.G_F03…` "겹쳐" 문구)는 B-163 보고서의 기존 실패다.
  - 나머지 21개는 B-167 보고서의 기존 실패 20개 + 불안정 검사 `PromptB150MainMenuTests.DeleteButton_Hover_FadesInWhiteTrashIcon`와 같은 묶음이다. 고친 뒤 전체를 다시 돌리지는 않았다(**미검증**: 전체 재실행).
- 새 PlayMode `PromptB168SfxD2PlayModeTests` 3개 통과(메뉴 `SubTerra/Tests/Run Prompt-B 168 Sfx D2 PlayMode Tests`, 플래그 `Temp/subterra-prompt-b168-playmode.flag`). Bootstrap → 메모리 세이브 새 게임 → 지상 "탐사 시작" 실제 클릭 → 광산.
  - 가스 맥박: 오버레이 옆에 붙음, Caution 2.6초에 1번(피치 1.0), 설정창 열면 0.2초 안에 멈추고 2.4초 무음, 닫으면 다시, Critical 2.6초에 3번(피치 1.08), Safe면 멈춤. (플레이어를 가스 칸에 넣지 않고 타임라인 단계만 올렸다.)
  - 피격·체력 경고: 체력 50에서 돌 피해 25 → 같은 프레임 PlayerHurt, 0.35초 뒤 LowHealthWarning 1번. 다시 25 → 체력 0: PlayerHurt·경고 없이 RunFailGlitch만.
  - 설치 실패: 사다리를 멀리 설치 → BuildFailResource 1번 + 노란 경고, 0.6초 뒤 다시 → 1번 더. UiCancelFail 없음. 붕괴 이벤트(핸들러 직접 호출) → CeilingCollapse 1번, `structuralSystem` 연결 확인.
  - 체력 경고 지연이 0.25초가 아니라 약 0.35초로 찍혔다. 검사 환경에서 그 사이 긴 프레임이 있었던 것으로 보인다(판정은 `now >= 피격 시각 + 0.25`인 첫 프레임). 실제 플레이에서 들어 보고 확인 필요(**미검증**).
- 전체 검사 뒤 범위 밖 파일이 dirty됐다(NotoSansKR SDF, InventoryPanel·MainMenuPanel·ViewSocket 프리팹, Mine_Demo_Integration.unity, ProjectSettings/TimeManager.asset, 103 메인 메뉴 PNG 2장). 모두 `git restore`로 되돌렸다.

## 음량 (클립 0.1초 최대 평균 × 칸 음량, dBFS)

| 소리 | 클립 0.1초 최대 | 칸 음량 적용 |
|---|---|---|
| gas_pulse_loop | -4.0 | -10.9 |
| low_health_warning | -5.5 | -11.5 |
| ceiling_collapse | -9.2 | -12.3 |
| rescue_holo_open | -8.4 | -14.4 |
| player_hurt | -10.8 | -15.2 |
| build_place_heavy | -10.8 | -15.2 |
| build_fail_blocked | -10.5 | -15.7 |
| build_fail_resource | -10.3 | -16.3 |
| upgrade_levelup | -12.1 | -17.3 |
| build_place_small | -15.0 | -18.7 |

- 음량 조정이 필요해 보이는 소리: gas_pulse_loop·low_health_warning은 다른 소리보다 4~5dB 크다(가스 안에서 2초마다 반복되므로 0.35 안팎까지 줄일 수 있음). build_place_small은 가장 작다(0.8 안팎까지 올릴 수 있음). 직접 들어 보지 않아 칸 값은 프롬프트 시작 음량 그대로 두었다.

## 미검증

- 모든 새 소리를 실제 스피커로 들어 본 것(음색·타이밍·크기 체감).
- 실제 가스 구역 안에서 걷는 맥박(검사는 타임라인 단계 주입), 실제 엘리베이터 탑승 중 맥박 멈춤(EditMode 판정만).
- 실제 천장 붕괴로 돌이 바닥에 닿는 순간의 소리(PlayMode는 핸들러 직접 호출), 위험 경보 "웽"과 함께 들리는지.
- 전력 0 → 팝업 닫기 → 구조 홀로그램 소리(코드 경로만 확인).
- 설치 성공(작게·묵직하게)·시설 이동 성공·이동 실패 경고, C 단축키 실패 소리.
- 업그레이드 구매 성공·자원 부족·잠김 소리.
- 효과음 음량·음소거가 새 소리에 적용되는 것(코드상 같은 경로), Console 빈 칸 경고 없음(검사 실행 중 오류 없음만 확인).

## 수동 확인 체크리스트

- [ ] 가스 구역에 들어가면 초록 가장자리가 밝아질 때마다 "두-둥"이 맞춰 난다. 위험(Critical)에서는 더 빠르고 조금 높다. 나오면 멈춘다.
- [ ] 가스 안에서 ESC·설정창을 열면 맥박이 바로 멈추고, 닫으면 다음 맥박부터 다시 난다.
- [ ] 무너진 돌·가스에 다치면 화면이 흔들리는 순간 "쿵+지지직"이 난다. 높은 곳에서 떨어져 다치면 "쿠웅" 하나만 난다.
- [ ] 체력이 0이 되는 순간에는 피격음 없이 실패 창 지직만 난다. 드릴 소리도 끊긴다.
- [ ] 체력이 처음 30% 아래로 내려가면 피격음 뒤에 낮은 "부-부-"가 한 번 난다. 더 다쳐도 다시 안 나고, 회복한 뒤 다시 내려가면 또 난다.
- [ ] 천장이 무너져 돌이 바닥에 닿는 순간 "쿠르릉"이 한 번 난다. 위험도 경보 "웽"이 끊기지 않는다.
- [ ] 전력이 0이 되어 팝업을 닫으면 머리 위 구조 홀로그램이 펼쳐지며 글리치 소리가 난다. 드론 말풍선과 소리가 겹치지 않는다.
- [ ] 사다리·버팀목·조명을 설치하면 짧은 "철컥", 충전기·보관함 등 큰 시설은 묵직한 "쿵-철컹"이 난다. 시설을 옮겨 놓아도 같다.
- [ ] 자원이 모자란데 설치하면 머리 위 노란 경고와 부드러운 "띵-딩", 겹치는 자리면 낮은 "부-"가 난다. 다시 누를 때마다 난다.
- [ ] 시설을 옮기는 중 놓을 수 없는 자리에 마우스를 올리기만 하면 무음, 클릭하면 노란 경고와 "부-"가 난다.
- [ ] 업그레이드를 사면 레벨업 소리, 자원이 모자라면 노드가 흔들리며 "띵-딩"이 난다.
- [ ] 효과음 음량·음소거가 새 소리에 모두 적용되고 Console에 빈 칸 경고나 오류가 없다.
