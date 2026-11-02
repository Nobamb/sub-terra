# B-178 퀘스트 클리어·심층 해금 작은 글리치 창 소리 + 보상 지급음 + 드론 스캔 "핑" (#158 D-2·E)

## 목표
- 지금 무음인 세 곳에 PM이 고른 새 소리 4개를 붙인다.
  - A. 퀘스트 클리어 창과 심층 구역 해금 창(둘 다 작은 글리치 창): 열림·닫힘
  - B. 퀘스트 보상이 실제로 들어올 때: 보상 지급음
  - C. 드론 30초 스캔 링이 퍼지기 시작할 때: 소나 "핑"
- 새 SfxId는 87~90 네 칸. 음량은 칸 값과 연결 지점 상수로 둬서 PM이 숫자만 바꿔 맞출 수 있게 한다.
- 필요하면 다른 에이전트에게 나눠 맡겨도 된다. 결과는 한 브랜치로 모은다.

## 브랜치
1. `git fetch`로 원격 `MVP2-fixNNN` 중 가장 큰 번호를 확인한다. PM 확인 시점(2026-10-31 20:00 KST)에는 B-177이 아직 올라오지 않아 **`MVP2-fix121`**(`84c4e17c`)이 최신이다.
2. B-177 커밋(`… (프롬프트 B-177)`)이 들어간 `MVP2-fix122`가 있으면 그 위에서 **`MVP2-fix123`**을 만든다.
3. fix122가 없으면 `MVP2-fix121`(`84c4e17c`)에서 **`MVP2-fix122`**를 만들고, 보고서 맨 위에 "B-177 없이 fix121 기준"이라고 적는다. 이 경우 B-177이 바꾸는 파일(설정창·우측 메뉴·시계·버튼)은 건드리지 않아 나중에 합칠 때 충돌이 없게 한다.

## 절대 규칙
1. 기존 SfxId 번호·칸 값·버스 음량은 바꾸지 않는다. 새 칸은 86(MainMenuTypeTick) 뒤 87~90만.
2. **음원 파일은 고치지 않는다.** 길이·페이드는 이미 다듬은 파일 그대로. 조정은 칸 값(음량·피치·startTime·maxDuration·fadeOut)으로만.
3. App 코드는 `SfxPlayer.Play`, Gameplay 코드는 `SfxRequests.Play`(또는 이벤트). Gameplay에서 App을 참조하지 않는다. `SfxPlayer`에 멈춤 API를 추가하지 않는다(읽기 전용 조회는 가능).
4. 창 연출 시간·모양, 보상 지급 규칙·순서, 드론 스캔 주기·링 연출, 세이브 형식은 바꾸지 않는다.
5. 시작 브리핑 창의 소리(BriefingGlitch·BriefingGlitchShort·BriefingClose)는 그대로. 작은 창은 지금처럼 브리핑 소리를 내지 않는다.
6. 줄 번호는 fix121 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 음원 넣기
- 사용자가 `sub-terra-sfx-B178.zip`을 풀어 `work_process/MVP2/sound/sfx-B178/`에 넣어 둔다. 없으면 멈추고 보고한다.
- 기본 4개를 복사해 넣고 작업 폴더 쪽 같은 wav는 지운다. `_heavy` wav 2개(저음 원본 비교용)·mp3·README·`address-rows.md`는 그 폴더에 그대로 둔다.
  - `quest_reward.wav`, `glitch_popup_open.wav`, `glitch_popup_close.wav` → `Assets/_Project/Audio/SFX/UI/`
  - `drone_scan_ping.wav` → `Assets/_Project/Audio/SFX/World/`
- 임포트 설정은 기존 빌더(`PromptB156SfxLibraryBuilder`, 모노·로드 시 압축 해제)를 따른다.
- `init/sound-effect-address.md` 표 끝에 `address-rows.md`의 4줄을 붙인다(같은 이름 줄이 있으면 고친다).

| 칸 | 이름 | 파일 | 버스 | 시작 음량 | 길이 |
|---|---|---|---|---|---|
| 87 | QuestReward | quest_reward | UI | 0.5 | 1.10초 |
| 88 | GlitchPopupOpen | glitch_popup_open | UI | 0.6 | 0.90초 |
| 89 | GlitchPopupClose | glitch_popup_close | UI | 0.6 | 0.70초 |
| 90 | DroneScanPing | drone_scan_ping | World | 0.3 | 0.90초 |
- `Shared/Audio/SfxId.cs`, `App/Audio/SfxLibraryDefaults.cs`(파일 끝, 길이 주석 포함), `Resources/SfxLibrary.asset` 세 곳에 같게 넣는다.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/Scripts/` 기준)
- #158 D-2 "심층 구역 해금 팝업"·"드론 스캔", E "퀘스트 클리어"·"화물" 줄, `work_process/MVP2/sound/sfx-B178/README.md`
- App/UI/Tutorial/StartBriefingPopupMotion.cs: `playsBriefingSfx` ~89, `ConfigureQuestClear` ~96, `ConfigureCompact` ~111(~121에서 소리 끔), `RequestClose` ~265(~275 닫힘음), `BeginIntro` ~378(~385 열림음), `PlaySfx` ~394, `PlayShortGlitchSfx` ~403
- App/UI/Tutorial/QuestClearPopupMotion.cs(`Open` ~33, `Close` ~55, 열림 약 1.28초·닫힘 0.48초), App/UI/Tutorial/DemoObjectiveView.cs(`SetClaimVisible` ~785-800 닫히던 창 다시 열기, `OnClaimConfirmClicked` ~1183)
- App/UI/Progression/DeepZoneUnlockPopupMotion.cs(`Configure` ~105-115에서 `ConfigureCompact`, `Open` ~135, `RequestClose` ~165)
- App/UI/Tutorial/DemoObjectivePresenter.cs: 구독 ~59, `ConfirmClaim` ~200, `OnGrantResolved` ~330. App/Tutorial/QuestReward.cs(`QuestRewardGrantStatus` ~108, `IsEmpty` ~28), QuestRewardService.cs(`Settle` ~313, `RetryPending`)
- Gameplay/Drone/DroneSensor.cs: `Update` ~64(`Time.time`), `SetScanWorldReady` ~79, `SetUpgradeEffects` ~87, `TickScanPulse` ~212(`pulseView.Show` ~235). DroneScanPulseView.cs(링 0.585초 퍼짐, 0.9초에 사라짐)
- 겹침 대상: `SfxId.ElevatorArriveDing`(SaveRuntimeController ~1118, ElevatorDoorVisual ~281), `SfxId.UpgradeLevelUp`(UpgradeTreeView ~748). App/Audio/SfxPlayer.cs(`lastPlayedAt` ~37, `PlayNow` ~299)
- 설정창 열림: App/Integration/UndergroundMenuController.cs `IsSettingsOpen` ~32. 일시정지: `UiPauseGate.IsHeld`
- 검사 양식: Tests/EditMode/App/Audio/PromptB168SfxD2Tests.cs, PromptB169…Tests

## A. 작은 글리치 창 열림·닫힘 (퀘스트 클리어 + 심층 해금, 한 곳에서)
- `StartBriefingPopupMotion`에 "작은 창 소리"(열림 id·닫힘 id)를 둔다. `ConfigureCompact`를 거친 창은 브리핑 소리 대신 이걸 쓴다. 두 창이 같은 길을 쓰므로 한 번 고치면 둘 다 된다.
  - 열림: `BeginIntro`(첫 지직 0초)에서 GlitchPopupOpen 한 번.
  - 닫힘: `RequestClose`가 받아들여질 때 GlitchPopupClose 한 번. 닫는 중 재호출·X키 연타는 지금처럼 무시되므로 소리도 한 번.
  - 표시 중 짧은 지직(`PlayShortGlitchSfx`)은 작은 창에서 계속 무음.
- 무음: 씬을 열 때 꺼진 채 시작, `Configure`·`ResetVisuals`, 에디트 모드(`Application.isPlaying == false`).
- 모션 감소로 연출을 건너뛰는 경로가 있으면 그때도 열림·닫힘 소리는 한 번씩 낸다(소리 없는 창이 되지 않게). 경로가 없으면 "해당 없음"으로 적는다.
- 퀘스트 클리어가 연달아 뜰 때(`ConfirmClaim` → 닫히던 창을 `Open()`으로 다시 엶): 같은 프레임에 닫힘·보상·열림이 겹친다.
  - 닫힘음은 그대로, 다시 열림음은 상수 `ReopenOpenSfxDelay = 0.35f`(unscaled)만큼 늦춰 한 번 낸다. 그 사이 창이 닫히거나 꺼지면 내지 않는다.
- 심층 구역 해금 창이 업그레이드 썸네일 해금 소리(B-169 Rattle·Click·Glow)와 같은 순간에 뜨는지 확인해 보고서에 적는다(고치지 말 것).

## B. 보상 지급음 (`DemoObjectivePresenter.OnGrantResolved` ~330)
- `result.Status == QuestRewardGrantStatus.Granted && !result.Reward.IsEmpty`일 때 한 번 QuestReward. 이 이벤트는 실제로 지급이 끝날 때 한 번만 온다(`Settle`).
- `ConfirmClaim`의 반환값으로 판단하지 않는다(다음 퀘스트가 기다리면 그쪽 결과로 바뀜).
- 화물 공간 부족(`NeedsCapacity`)·포기(`Forfeited`)·`AlreadySettled`·`NothingPending`은 무음. 버리기로 공간을 만든 뒤 `RetryPending`으로 들어오면 그때 한 번 난다.
- 닫힘음과 같은 프레임에 나는 건 의도한 것(닫으면 보상). 금 보상으로 금화 팝업 소리(GoldLever 등)가 같이 나면 보고서에 적는다(고치지 말 것).
- B-177이 지상 퀘스트 버튼에 붙인 임시 UiClick은 퀘스트 목록 창 몫이라 이번엔 그대로 둔다.

## C. 드론 스캔 "핑" (`DroneSensor.TickScanPulse` ~235)
- 링이 시작되는 그 프레임(`pulseView.Show` 직후)에 한 번. Gameplay 코드이므로 `DroneSensor`는 "스캔 시작" 이벤트(또는 `SfxRequests`)만 내고, 판단은 App 쪽 작은 클래스(예: `DroneScanPingSfx`, 순수 판단 함수 분리)가 한다.
- 소리를 내지 않는 때(상수로):
  1. 첫 펄스: `SetScanWorldReady(true)`·`SetUpgradeEffects` 뒤 바로 오는 즉시 스캔(광산 도착 "딩"·레벨업 소리와 같은 순간). 링은 그대로, 소리만 생략.
  2. 겹침: ElevatorArriveDing 또는 UpgradeLevelUp이 `SkipIfRecentSeconds = 0.6f` 안에 났으면 생략. `SfxPlayer`에 "마지막으로 낸 시각" 읽기 전용 조회를 더해 쓴다.
  3. 광산 설정창이 열려 있거나(`UndergroundMenuController.IsSettingsOpen`, 일시정지가 아님) `UiPauseGate.IsHeld`일 때 생략.
  4. 스캔 업그레이드가 없어 펄스가 없으면 당연히 무음. 진짜 일시정지(timeScale 0)면 펄스 자체가 안 온다.
- 생략은 "그 회차만". 늦춰서 내지 않는다(링이 이미 퍼지는 중이라 어긋남). 다음 30초 회차는 정상.
- 피치는 1.0 고정. 음량은 칸 값 0.3으로 시작(30초마다 나므로 작게).

## 손대지 말 것
- 시작 브리핑 창 소리, 창 연출 시간표·글리치 모양, 보상 지급 규칙, 화물 부족·버리기 창 버튼(E 화물 몫), 드론 말풍선·Tab 분석 창·"찰칵"(E 드론 몫)
- 기존 칸 값·음원, B-164~B-177 소리 연결, `AGENTS.md`·규칙 문서

## 테스트
- EditMode 전체에서 기준 브랜치의 기존 실패 말고 새 실패가 없어야 한다. 기대값을 바꾼 기존 검사는 이름과 이유를 적는다.
- 새 EditMode `PromptB178SfxTests`(소리 요청을 잡는 방식은 B-168·B-169 검사를 따름)
  - 칸 87~90: 이름·버스·음량·파일 이름, 파일 4개가 `Audio/SFX/UI`·`World`에 있음
  - 작은 창: 열기 1번 GlitchPopupOpen, 닫기 1번 GlitchPopupClose, 닫는 중 다시 닫기 0번, 브리핑 소리 0번, 시작 브리핑 창은 기존 소리 그대로
  - 연달아 열기: 닫힘 1번 → 0.35초 뒤 열림 1번, 그 사이 꺼지면 열림 0번
  - 보상: Granted 1번, NeedsCapacity·Forfeited·빈 보상 0번, 공간 확보 뒤 RetryPending 지급 1번
  - 드론: 첫 펄스 0번, 다음 회차 1번, 0.6초 안 ElevatorArriveDing·UpgradeLevelUp 뒤 0번, 설정창 열림·UiPauseGate 중 0번
- PlayMode는 고친 검사만. 실행 전후 `git status`로 범위 밖 프리팹·폰트·씬·PNG·TimeManager 변경을 확인하고 되돌린다.

## 사용자 테스트 가이드 (보고서에 그대로 넣기)
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
12. 글리치 소리가 너무 얇게 들리면 `work_process/MVP2/sound/sfx-B178/`의 `_heavy` 파일(저음 원본)을 들어 보고, 바꿀지 PM에게 알려 준다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 퀘스트 클리어·심층 해금 창, 보상 지급, 드론 스캔 효과음을 연결 (프롬프트 B-178)`). 새 브랜치에 일반 push, 브랜치는 지우지 않는다. PR은 만들지 않는다.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue158-주제.md` 파일 하나로 기록. **`init/rules/lessons.md`에 줄을 추가하지 않는다.**
- 결과 보고서 `work_process/MVP2/sound/B-178-sfx-quest-drone-result.md`
  - 기준 브랜치·커밋(B-177 포함 여부), 소리마다 붙인 곳(파일·줄)과 SfxId, 음량·지연 상수 위치
  - 테스트 결과, 직접 들어 보지 못한 항목은 "미검증", 위 사용자 테스트 가이드
  - **#158 대응 표**: D-2 심층 구역 해금 팝업 / D-2 드론 스캔 / E 퀘스트 클리어 창 열림·닫힘 / E 보상 지급음 / E 화물 "공간 확보 뒤 한 번 지급" 각각 구현·일부·못 함
