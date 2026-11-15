# B-197 광산 타이머 만료 시 글리치 암전 후 지상 기지로 (#151)

## 목표
광산 초기화 타이머가 다 되면, 비상 탈출(전력 고갈 구출)과 같은 글리치 워프로 화면이 검어졌다가 바로 밝아지며 지상 기지 화면이 보이게 한다. 엘리베이터 카메라 이동은 넣지 않는다.
기준: https://github.com/Nobamb/sub-terra/issues/151 체크 항목 **「광산이 타이머에 의해 시간이 다 되면 글리치가 발생하면서 화면이 검어지고 바로 다시 밝아지면서 지상기지화면으로 옴」**. 만료 게임플레이 결과만 그대로 두고, 전환 연출만 바꾼다.

## 브랜치
1. `git fetch` 후 최신 `origin/main`(`094b156378418372e47fa395cdb65737f369bba3`, #328/B-196·B-196-1 병합. 그 안에 #327/B-195-1 포함)에서 새 브랜치 **`MVP2-fix141`**을 만든다. (`MVP2-fix139`·`MVP2-fix140`은 이미 쓰였고 병합됨. 이 번호는 쓰지 않는다.)
2. 원격에 `MVP2-fix141`이 이미 있으면 비어 있는 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다.
3. 보고서 맨 위에 기준 커밋 해시를 적는다. 브랜치를 지우지 않는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix141`(또는 실제 쓴 번호)에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다. #151에 댓글·체크박스 수정·닫기를 하지 않는다.
3. 새 음원, 새 `SfxId`, `SfxLibrary`, 음량을 추가하거나 바꾸지 않는다. 기존 만료 벨·팝업 소리만 유지하고, 워프 효과음이 필요하면 구출과 같은 `SfxId.PortalWarp`만 재사용한다.
4. 만료 로직은 그대로다. `MineResetService.TryTimedReset`(무료, `PaidResetCount` 0, 요금 500G 복구, 새 시드, 타이머 재시작), 시설 쿨다운 비우기, `AutoSaveReason.MineReset`, `ShowTimedResetPopup`, 지상 유료 초기화(`TryReset`)를 바꾸지 않는다. 체력 0 `RunFailure` 전환, 긴급 탈출 포탈 웜홀 워프, 구출 결제·목적지(엘리베이터)도 건드리지 않는다.
5. 프리팹 YAML을 손으로 고치지 않는다. 줄 번호는 `094b1563` 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 현재 상태 (main `094b1563` — B-195-1은 #327로 이미 병합됨)
- **만료 경로.** `SaveRuntimeController.TickMineResetCycle`이 Integration(광산)에서만 unscaled 시간을 쌓는다. 3시간(`MineResetService.CycleDurationSeconds`)이 되면 `ExecuteTimedMineReset`을 시작한다. 디버그 `timer end` / `SetMineResetElapsedSeconds`도 같은 코루틴을 탄다.
- **지금 전환.** 시계 `BeginDeparture(playSound: false)` → 엘리베이터·저장 대기 → `TryTimedReset`으로 월드를 바꾼 뒤, `WaitForClockPowerOff`가 끝나면 **암전 없이** `UnitySceneLoader().Load(SceneNames.SurfaceBase)`로 지상 씬을 바로 연다. 저장 후 `MineResetClockOverlay.ShowTimedResetPopup(wasInMine)`(제목 「탐사 시간 종료」). 시계 인트로가 있으면 초침 끝에 `SfxId.TimerEnd`, 이어서 `SfxId.UiPopupOpen`. 글리치·전체 화면 암전은 없다.
- **재사용할 구출 글리치 (이미 main).** `EscapeWarpRunner` + `EscapeWarpStyle.Glitch`. `EmergencyRescueRuntimeController.BeginRescueWarp`가 이 조합을 쓴다. 순서: 암전 0.25초와 짧은 청록 글리치 0.25초가 겹침 → 완전 암전 유지 0.15초(이때 한 번 이동, 카메라 `SnapToTarget`) → 화면 켜짐 0.30초. 카메라 이동은 없다. 기본값은 `EscapeWarpSettings`. 모션 감소면 글리치 없이 페이드 아웃 → 이동 → 페이드 인. 그림은 `EscapeWarpOverlay`(정렬 32,650)가 `RunFailureGlitchArt`만 재사용. 기존 검사: `PromptB195EscapeWarpTests`.
- **주의.** 구출은 **같은 Integration 씬 안**에서 엘리베이터로만 옮긴다. 만료는 씬을 `SurfaceBase`로 바꾸므로, 지금 `EscapeWarpOverlay` 루트는 `DontDestroyOnLoad`가 아니다. 암전 중에 씬을 열면 오버레이가 같이 사라질 수 있다. 코루틴은 Bootstrap 수명의 `SaveRuntimeController`에서 돌려야 한다.

## 할 일
- `ExecuteTimedMineReset`의 **지상 이동 구간**에 구출과 같은 `EscapeWarpRunner`(`EscapeWarpStyle.Glitch`, 같은 `EscapeWarpSettings` 기본값, 검사기에서 조절)를 연결한다. 새 워프 타임라인을 만들지 말고, 구출 결제·엘리베이터 이동 코드로 우회하지도 않는다.
- 권장 순서: 만료 확정 → 글리치 암전 시작(기존 시계 `BeginDeparture(playSound: false)`는 유지) → 화면이 완전히 검은 `ShouldTeleport`/`arrive`에서 기존처럼 월드 교체·지상 씬 로드·카메라 스냅(필요하면 저장) → 화면이 켜질 때는 이미 지상 기지 → 워프가 끝난 뒤에만 `ShowTimedResetPopup`. 팝업이 암전 도중 뜨거나, 지상 씬이 글리치보다 먼저 보이면 안 된다.
- 오버레이·러닝 코루틴이 씬 교체 뒤에도 남아 켜짐 연출이 이어지게 한다(`DontDestroyOnLoad` 또는 Bootstrap 부모). 끝나면 지금처럼 정리한다.
- 엘리베이터 카메라 이동·웜홀 스타일은 넣지 않는다. 모션 감소는 기존 `EscapeWarpTimeline` 페이드만 쓴다.
- SFX: 새 음원 없음. 만료 벨·팝업음 유지. 출발 꺼짐음은 지금처럼 만료 경로에서 겹치지 않게.

## 테스트 (EditMode 추가, 필요하면 PlayMode)
- `TryTimedReset` 결과(무료, 요금 500G, 시드 교체, 경과 0)와 유료 `TryReset`, 구출 글리치·포탈 웜홀·사망 전환 검사가 이전과 같다. `PromptB195EscapeWarpTests` 통과.
- 만료 전환: 완전 암전일 때만 지상 로드가 일어나고, 켜질 때는 이미 지상이다. 모션 감소면 글리치 강도 0. 오버레이가 씬 로드 후에도 켜짐이 끝날 때까지 남는다. `timer end`가 이 경로를 탄다.

## 사용자 테스트 가이드 (직접 플레이)
1. 광산에 들어가 시계가 보이는지 확인한 뒤, 개발 콘솔에서 `timer end`(또는 경과를 3시간으로)로 만료시킨다. 글리치와 함께 화면이 검어지고, 곧 밝아지며 **지상 기지**가 보여야 한다. 광산이 그대로 보이다가 뚝 끊기면 안 된다.
2. 켜진 뒤에는 「탐사 시간 종료」 팝업이 뜨고, 시계 인트로·벨·확인 버튼은 예전과 같아야 한다. 월드가 바뀌고 다음 유료 초기화 요금이 500G이며 타이머가 다시 3시간인지도 본다.
3. 설정에서 모션 감소를 켜고 1을 다시 한다. 글리치 없이 페이드로 어두워졌다가, 지상 기지가 밝아져야 한다.
4. 광산에서 전력 고갈 구출을 한 번 한다. 예전처럼 암전+글리치 후 엘리베이터에 도착해야 하고, 만료 연출 때문에 구출 비용·목적지가 바뀌면 안 된다.
5. 지상에서 유료 광산 초기화를 한 번 한다. 요금·성공 흐름이 그대로여야 한다. 만료 글리치가 이 경로에 끼어들면 안 된다.
6. 인벤토리·골드·체력이 연출 때문에 더 깎이지 않는지, Unity Console에 새 빨간 오류·노란 경고가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 광산 타이머 만료 시 글리치 암전 후 지상으로 (프롬프트 B-197)`). `MVP2-fix141`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue151-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #151은 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-197-mine-timeout-glitch-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일·줄
  - 워프를 `ExecuteTimedMineReset`에 어떻게 끼웠는지, 오버레이가 씬 전환을 어떻게 견디는지, 팝업을 워프 끝으로 옮긴 방식, 모션 감소, 재사용한 설정·SFX, 만료 로직을 유지한 방식
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix141` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음(만료 결과·구출/포탈/사망 검사 포함) [ ] 만료 시 글리치+암전 후 지상 기지가 켜짐 [ ] 지상 씬이 암전 중에만 로드됨 [ ] 오버레이가 씬 전환 후에도 켜짐까지 유지 [ ] 엘리베이터 카메라 이동 없음 [ ] 완료 팝업은 워프 뒤에 표시 [ ] 무료 초기화·500G 복구·타이머 재시작·자동 저장 유지 [ ] 구출 글리치·포탈 웜홀·사망 전환 불변 [ ] 유료 초기화 불변 [ ] 모션 감소 시 페이드만 [ ] 새 SFX 없음 [ ] 프리팹 YAML 손수정 없음 [ ] 수량 불변 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] 브랜치 삭제 안 함 [ ] #151 미수정
