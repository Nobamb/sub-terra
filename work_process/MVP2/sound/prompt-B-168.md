# B-168 효과음 D-2 (위험·생존 소리, 시설 설치·업그레이드 소리, 실패음 공통 입구, #158 D-2)

## 목표
- #158 D-2의 "위험·생존"과 "시설 설치·업그레이드 레벨업" 소리를 붙이고, 실패음을 `FailureFeedback` 한 입구로 모은다.
- 새 소리 15개는 사용자가 이미 넣어 둔다(묶음 `sub-terra-sfx-D2.zip`). 위험 5개와 설치음 2개는 `Assets/_Project/Audio/SFX/World/`, 나머지 8개는 `.../SFX/UI/`. 설명과 출처는 `work_process/MVP2/sound/sfx-D2-hazard/README.md`, `sfx-D2-build/README.md`, 출처 줄은 `init/sound-effect-address.md`(15줄 추가됨). 같은 UI 폴더의 `upgrade_lock_*`·`upgrade_window_suck_in` 4개는 B-169 몫이라 이번에 칸을 만들거나 연결하지 않는다. **음원은 고치지 않는다.**
- 창 열기·닫기 소리(WindowTvOpen/Close)와 자물쇠 해금 소리(UnlockRattle/Click/Glow)는 **칸만 만들고 연결은 B-169**에서 한다.
- 기준: **MVP2-fix108(f71535a8, B-167 업그레이드 창 연출, #252)이 들어간 가장 최신 MVP2 브랜치**에서 새 브랜치를 따고 PR을 올린다. f71535a8이 조상이 아니면(`git merge-base --is-ancestor f71535a8 HEAD`) 멈추고 보고한다. B-167 연출과 B-166의 고침(`UiSfxButton` 같은 프레임 실패음 생략, 엘리베이터 탑승 중 설정창 막기)을 되돌리지 않는다. 업그레이드 창 연출에는 소리를 달지 않는다(B-169).
- 필요하면 다른 에이전트(Codex 등)에게 나눠 맡겨도 된다. 결과는 한 PR로 모은다.

## 절대 규칙
1. SfxId 번호는 바꾸지 않는다. 새 칸은 65(CargoHeavyWarning) 뒤 66부터 붙이고, 3번은 계속 비워 둔다.
2. 음량은 `SfxLibraryDefaults.cs`와 `Resources/SfxLibrary.asset` **두 곳 모두** 같은 값으로 넣는다. 새 파일 가져오기 설정은 기존처럼 모노+normalize(빌더 `PromptB156SfxPostprocessor` ~117-138)다.
3. wav가 폴더에 없으면 지어내지 말고 멈춘 뒤 보고한다. 파일 이름은 아래 표와 같아야 한다.
4. Gameplay 코드는 `SfxRequests.Play`, App 코드는 `SfxPlayer.Play`. Gameplay는 App을 참조하지 못한다. SfxPlayer 규칙(같은 소리 0.05초, 동시 8개)은 그대로 둔다.
5. Alert 버스는 쓰지 않는다(목소리가 하나라 구조 위험 경보 "웽"이 끊긴다).
6. 게임 로직, 피해량, 연출 시간, 입력은 바꾸지 않는다. 줄 번호는 d662c376 기준 근사(~)이며 B-166 뒤 조금 밀렸을 수 있다. 못 찾으면 "못 찾음"으로 보고한다.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/Scripts/` 기준)
- `work_process/MVP2/sound/B-164-sfx-d1-result.md`, `B-165-…`, `B-166-sfx-d1-fix2-result.md`, #158 "구현 메모"
- Shared/Audio/SfxId.cs, App/Audio/SfxLibraryDefaults.cs(`Create` ~44-185, `LoopFileNames` ~31), SfxPlayer.cs
- App/Integration/WorldLoopAudio.cs(`WorldLoopGate` ~23, `isPaused` ~82), IntegrationRuntimeBinder.cs(~871-890 `WorldLoopAudio.Attach`), App/Integration/MiningSfxGates.cs
- App/UI/HUD/GasPoisonOverlayTimeline.cs(Caution 2.0초·Critical 1.2초 맥박 ~47-48, `Advance` ~158), GasPoisonOverlayView.cs(~86-125), App/Integration/GasExposureEffectController.cs
- Gameplay/Player/PlayerSurvivalController.cs(`ApplyDamage` ~243-279, `StartDamageFeedback` ~293-322, `TrackFall` 착지 ~504-517), Gameplay/Player/PlayerMoveSfxGate.cs(`ResolveLanding` ~34)
- App/Integration/ExplorationBgmController.cs(`lowHealthRatio` ~13, `ShouldPlayDanger` ~43-47), App/UI/HUD/HudBinder.cs(~96)
- Gameplay/Structural/StructuralIntegritySystem.cs(돌 낙하 시작 ~703, `AdvancePendingCollapses` ~745-755), StructuralRiskFeedback.cs(`OnCollapseTriggered` ~58)
- App/UI/EmergencyRescue/EmergencyRescueChipView.cs(`Show` ~129-151, 펼침 ~311-315), App/Integration/EmergencyRescueRuntimeController.cs(~176-199, ~217-228, 드론 안내 ~55-67·~326)
- App/UI/Building/BuildingMenuPresenter.cs(`OnPlacementChanged` ~211-245), App/Integration/GameplayBuildingPlacementBridge.cs(`OnBuildingPlaced` ~457, `OnBuildingMoved` ~470, `OnPlacementRejected` ~491-506, 이동 호버 문구 ~284), Gameplay/Building/BuildingPlacementResult.cs(실패 이유 enum)
- App/UI/PlayerStatus/PlayerHeadMessageStack.cs, PlayerStatusWarnings.cs, App/UI/Outpost/OutpostPanelPresenter.cs(~71-95), App/Integration/MiningProgressHud.cs(~111-116)
- App/UI/Progression/UpgradeTreeView.cs(`PlayPurchaseFeedback` ~699-748), ProgressionPanelView.cs(~131-139)

## 1. 새 칸
| 번호 | SfxId | 파일 | 버스(폴더) | 길이 | 시작 음량 |
|---|---|---|---|---|---|
| 66 | GasPulseLoop | gas_pulse_loop.wav | World | 1.23 | 0.45 |
| 67 | PlayerHurt | player_hurt.wav | World | 0.68 | 0.6 |
| 68 | LowHealthWarning | low_health_warning.wav | World | 0.97 | 0.5 |
| 69 | CeilingCollapse | ceiling_collapse.wav | World | 1.40 | 0.7 |
| 70 | RescueHoloOpen | rescue_holo_open.wav | World | 1.63 | 0.5 |
| 71 | BuildPlaceSmall | build_place_small.wav | World | 0.30 | 0.65 |
| 72 | BuildPlaceHeavy | build_place_heavy.wav | World | 1.37 | 0.6 |
| 73 | BuildFailResource | build_fail_resource.wav | UI | 0.64 | 0.5 |
| 74 | BuildFailBlocked | build_fail_blocked.wav | UI | 0.42 | 0.55 |
| 75 | UpgradeLevelUp | upgrade_levelup.wav | UI | 0.56 | 0.55 |
| 76 | UnlockRattle | unlock_rattle.wav | UI | 0.67 | 0.5 (maxDuration 0.3, fadeOut 0.1) |
| 77 | UnlockClick | unlock_click.wav | UI | 0.44 | 0.7 |
| 78 | UnlockGlow | unlock_glow.wav | UI | 1.00 | 0.45 |
| 79 | WindowTvOpen | window_tv_open.wav | UI | 0.73 | 0.4 |
| 80 | WindowTvClose | window_tv_close.wav | UI | 0.56 | 0.5 |
- `Create()`에 표 순서로 넣고 주석에 길이·원본을 적는다. 요약 주석의 칸 수(64 → 79)와 칸 수를 세는 기존 테스트도 고친다. 이름은 코드 관례에 맞게 바꿔도 되지만 바꾸면 표로 보고한다.
- UnlockRattle은 흔들림(0.2초) 뒤 딸깍과 겹치지 않게 칸 값으로 0.3초에서 끊는다(SfxPlayer에 멈춤 API를 만들지 않는다).
- 빌더(SubTerra/Audio/Create Or Refresh Sfx Library)로 asset을 채운다. 에디터를 못 쓰면 기존 형식대로 쓰고 "미검증(에디터 미실행)"으로 적는다. `init/sound-effect-address.md`에 15줄이 있는지만 확인한다(없으면 묶음의 줄을 붙인다).

## 2. 실패음 공통 입구 `FailureFeedback` (새 `App/Audio/FailureFeedback.cs`)
- `FailureFeedback.Play(SfxId id, string key)`: 같은 프레임에는 한 번만(어떤 id든), 같은 key는 0.3초 안에 다시 오면 무시. 시계·프레임은 테스트용으로 주입 가능하게.
- **시설 설치 실패** (`BuildingMenuPresenter` Failed 분기 ~232-240): 마우스 클릭(`TryPlaceAt`)과 C 단축키(`TryPlaceNearestByShortcut` ~362) 실패가 모두 여기로 온다.
  - 확인한 사실: 이 분기는 이미 `PlayerStatusWarnings.Current?.ShowWarning("building.placement-failed", …)`(~239)로 머리 위 노란 경고를 띄운다. 경고가 실제로 보이는지 확인하고, 안 보이면 고친 뒤 이유를 적는다.
  - 자원 부족(같은 자리의 `CanAfford` 판정 ~234-236, 또는 이유 `CannotAfford`·`SpendFailed`) → BuildFailResource. 그 밖(겹침·바닥 없음·범위 밖·허용 구역 밖·엘리베이터 자리 등) → BuildFailBlocked. 다시 시도할 때마다 난다(0.3초 연타만 거름).
- **시설 이동 실패**: `OnPlacementRejected`의 이동 분기(~493-497)에서만 BuildFailBlocked와 같은 문구의 노란 경고를 낸다. 마우스를 올려 둘 때의 문구(~284)는 무음이다.
- `OutpostPanelPresenter` ~76·~86의 직접 `UiCancelFail`은 동작(한 번 재생, 문구)이 같을 때만 `FailureFeedback.Play(UiCancelFail, "outpost.facility")`로 옮긴다. 다르면 그대로 두고 이유를 적는다.
- `MiningFailSfxGate`(같은 이유 반복 없음)는 그대로 둔다. `MiningProgressHud` ~111-116이 `FailureFeedback`을 거쳐도 되지만 규칙은 그대로 유지한다.

## 3. 연결 위치
① **가스 맥박** (새 `App/Integration/GasPulseAudio.cs`, `GasPoisonOverlayView` 옆에 붙임)
  - 따로 AudioSource 하나. 음량·음소거는 `WorldLoopAudio`와 같은 방식(칸 × 버스 × 효과음 설정)이다. loop는 끄고 **맥박 한 번에 한 번** 처음부터 재생한다(Stop → Play, 쌓이지 않게).
  - 초록 비네팅이 가장 밝아지는 순간(타임라인 위상 0.5)에 "두-둥"의 첫 박이 오도록, 위상이 그 지점을 넘는 프레임에 재생한다. 타임라인에 위상·현재 우세 단계를 읽는 속성만 추가한다(값 계산은 바꾸지 않는다).
  - Caution은 2.0초마다 피치 1.0, Critical은 1.2초마다 피치 1.08(클립 1.23초가 주기 안에 끝나게).
  - 멈춤: `WorldLoopGate` 규칙(일시정지 `UiPauseGate.IsHeld || ControlPreferences.IsSettingsOpen`, `RunFailureRuntimeController.IsHandling`, `CanAct == false`)에 더해 엘리베이터 탑승(`ElevatorController.IsRiding`), 런 Returning·Completed, 가스 밖(Safe), OnDisable·씬 전환. 멈출 때 페이드아웃 0.08초, 다시 조건이 되면 다음 맥박부터.
② **피격** (`PlayerSurvivalController.ApplyDamage` ~243-279, 흔들림·깜빡임 `StartDamageFeedback`과 같은 프레임)
  - 실제 피해(`appliedDamage > 0`)이고 살아 있을 때만 PlayerHurt. 원인: 무너진 돌(`ApplyCollapse`·`ApplyCollapseImpact`), 가스(`ApplyGasFailure`)
  - 낙하(`RunFailureCause.Fall`)는 무음. `TrackFall`이 이미 LandHeavy를 내므로 하나만 남긴다(#158 D-1).
  - 체력이 0이 되는 마지막 한 방(`becameIncapacitated`)과 전력 고갈은 무음(실패 창 지직에 양보). 드릴 루프는 기존대로 `CanAct == false`에서 끊긴다.
③ **체력 30% 경고** (새 순수 클래스 `LowHealthSfxGate`, App 쪽에서 `HealthChanged` 구독. 예: `HudBinder` ~96 근처)
  - 기준은 `ExplorationBgmController`와 같은 `Current <= Maximum * 0.3`. 체력이 **줄어서** 처음 넘을 때 한 번, 피격음 뒤 0.25초에 LowHealthWarning(그 사이 사망·실패 처리면 취소).
  - 30% 위로 회복해야 다시 낼 수 있다. 씬 진입·불러오기 첫 관측, 최대 체력만 바뀐 경우는 무음. 반복 재생 없음, World 버스.
④ **천장 붕괴** (`StructuralRiskFeedback.OnCollapseTriggered` ~58)
  - 돌은 ~703에서 떨어지기 시작하고, `CollapseTriggered`는 `CollapseFallSeconds` 뒤(돌이 닿는 순간, ~753)에 온다. 이 순간 CeilingCollapse를 이벤트당 한 번 낸다. 들어 보고 늦으면 낙하 시작으로 옮겨도 되며 그 경우 이유를 적는다. 위험 배경음·경보와 별개다.
⑤ **구조 홀로그램** (`EmergencyRescueChipView.Show` ~129-151, 이미 보이는 중이면 무음)
  - 전력 0 → 팝업(UiPopupOpen) → 닫힘 지직(UiPopupClose) → `OnPopupClosed`(~176-199)에서 홀로그램이 펼쳐질 때 RescueHoloOpen. 두 번째 고갈(~217-228)도 같다.
  - 드론 안내 말풍선(~55-67, ~326)은 18초 뒤에 뜨고 소리가 없다. 같은 프레임에 뜨는 경로를 찾으면 홀로그램 소리 하나만 남긴다(#218).
⑥ **설치 성공** (`GameplayBuildingPlacementBridge.OnBuildingPlaced` ~457)
  - 사다리·버팀목·조명(`DataIds.Buildings.LadderBasic/SupportBasic/LightBasic`)은 BuildPlaceSmall, 그 밖 시설은 BuildPlaceHeavy. **결정: 시설 이동 성공(`OnBuildingMoved` ~470)도 같은 규칙으로 낸다.** 집기·취소 소리는 E 단계다.
⑦ **업그레이드 구매** (`UpgradeTreeView.PlayPurchaseFeedback` ~699-748)
  - 성공(노드 레벨 상승)이면 UpgradeLevelUp 한 번. 새로 풀린 노드 소리는 B-169.
  - 실패: `InsufficientResources` → BuildFailResource(시설 설치와 같은 "자원 부족" 톤), `Locked` → BuildFailBlocked("할 수 없음" 톤). 둘 다 `FailureFeedback.Play(…, "upgrade.purchase")`로, 노드 흔들림과 같은 프레임. 기존 `UiCancelFail`은 이 경로에 쓰지 않는다.

## 손대지 말 것
- 음원 파일, 기존 칸(1~65) 값과 클립, 버스 음량, 설정창, 세이브 형식, B-166 고침, 관련 없는 리팩터링
- 창 TV 연출과 그 소리, 업그레이드 노드·심층 썸네일 해금 연출과 소리(B-169), 업그레이드 창 열기·닫기 소리(B-169)
- 구조 비용 지불 뒤 워프·도착음, 설정창·퀘스트·메뉴 등 UI 공통 소리, 드론 스캔음, 건설창 항목 클릭음(#158 다른 줄)

## 테스트
- EditMode 전체에서 기존 실패(B-166 기준 19개, B-167 보고서에 다르면 그 기준) 말고 새 실패가 없어야 한다.
- 새 EditMode 테스트 `PromptB167SfxD2Tests`
  - Defaults: 66~80 번호 중복 없음, 파일 이름·버스가 표와 같음, asset 음량이 Defaults와 같음, UnlockRattle maxDuration 0.3
  - FailureFeedback: 같은 프레임 두 번이면 1번, 같은 key 0.2초 뒤면 0번, 0.35초 뒤면 1번, 다른 key는 각각 1번
  - 설치 실패: 자원 부족이면 BuildFailResource, 겹침·바닥 없음·엘리베이터 자리면 BuildFailBlocked, 0.5초 간격 3번 시도면 3번
  - 피격: 가스·붕괴 피해면 1번, 낙하면 0번(LandHeavy만), 마지막 한 방이면 0번
  - 체력 경고: 1.0→0.5→0.25면 1번, 0.25→0.2면 0번, 0.5로 회복 뒤 0.28이면 다시 1번, 첫 관측 0.2면 0번, 0.25초 안에 사망이면 0번
  - 가스 맥박: 위상이 0.5를 넘는 프레임에만 1번(프레임이 튀어도 1번), Critical 피치 1.08, 일시정지·설정창·탑승·실패 처리면 멈춤
- PlayMode는 이번에 추가한 테스트만 돌린다. 테스트 끝에 `ControlPreferences.IsSettingsOpen`, `UiPauseGate`를 원래대로 돌려놓는다.

## 수동 확인 체크리스트 (보고서에 그대로 넣기)
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

## #158 대응 (완료 표시용)
| #158 항목 | 이 프롬프트 |
|---|---|
| 시설 설치: 설치 성공 작게/묵직하게, 실패 두 톤·재시도마다 | ⑥, 2 |
| 업그레이드: 레벨업, 흔들릴 때 실패음 | ⑦ |
| 위험·생존: 가스 맥박, 피격음, 체력 30% 경고, 천장 붕괴, 구조 홀로그램 펼침 | ①~⑤ |
| 구현 메모: 실패음 입구 `FailureFeedback`, 시설 이동 실패는 ~493에서만 | 2 |
- #158 본문은 이 작업에서 고치지 않는다(PM이 고침).

## 완료 후
- PR 제목 예: `feat: 효과음 D-2 위험·생존·시설 설치·업그레이드 (프롬프트 B-168)`. 커밋 메시지는 한국어 conventional 형식으로 쓰고, 브랜치는 지우지 않는다.
- 결과 보고서 `work_process/MVP2/sound/B-168-sfx-d2-result.md`에 적을 것
  - 기준 커밋, 브랜치, PR 링크, 테스트 결과
  - 칸 표(이름을 바꿨으면 바꾼 이름), 소리마다 연결한 클래스·메서드·줄
  - 설치 실패 노란 경고가 원래 보였는지, OutpostPanelPresenter를 옮겼는지와 이유
  - 못 찾은 위치와 직접 들어 보지 못한 항목은 "미검증"으로 구분, 음량 조정이 필요해 보이는 소리
  - 위 수동 확인 체크리스트
