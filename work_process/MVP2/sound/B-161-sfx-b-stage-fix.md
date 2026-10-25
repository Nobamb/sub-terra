# B-161 효과음 B 단계 수정 (가이드 넘김·막판 째깍·3시간 만료 째깍, B-160 후속)

## 목표
- B-160 플레이 테스트에서 안 들린 소리 세 개를 고친다. 가이드 창 닫힘 소리를 추가한다.
  1. 가이드 첫 열기: SF 소리가 안 들리고, 세 장 넘김 대신 한 장 소리처럼 들림. 닫을 때는 무음.
  2. 시계(T)를 켜고 1분 이하로 남겨도 째깍이 한 번도 안 남.
  3. 3시간 만료 연출: 벨만 나고, 시침이 도는 동안 째깍째깍이 없음.
- **브랜치: B-160이 올라간 `MVP2-fix99`(eb5137d0)에서 이어서 작업한다.** 새 브랜치를 만들지 않는다.
- 필요하면 다른 에이전트에게 나눠 맡겨도 된다. 결과는 한 커밋으로 모은다.

## 절대 규칙
1. **추측으로 고치지 말고 먼저 재현·진단한다.** 아래 "의심 원인"은 힌트일 뿐이다. 로그·테스트로 확인한 원인을 보고서에 적는다.
2. SfxId 번호(1~34)와 다른 칸 값은 바꾸지 않는다. 바꾸는 칸은 21, 23, 24(필요하면 25)뿐이다.
3. 연출 시간, 게임 로직, 입력 순서는 바꾸지 않는다. 시간은 비스케일.
4. 진단용 Debug.Log는 커밋 전에 모두 지운다. 커밋은 사용자 확인 뒤에 하고, 브랜치는 지우지 않는다.

## 먼저 읽을 코드 (sub-terra/Assets/_Project/Scripts/App/ 기준, 줄은 eb5137d0)
- UI/Guide/GameGuidePopupView.cs: Show(:162), IntroPlayed(:171~172), X3 재생(:193~197), BeginClose(:203), Tick 한 장 넘김(:256~262)
- UI/Guide/GameGuideState.cs: IntroPlayed는 **플레이 세션마다 한 번**이다(정적 값, Play 시작 때만 초기화)
- UI/HUD/MineResetClockView.cs: SetFormattedClock(:105), Observe(:115), 째깍(:118), IsFinalTick(:125)
- UI/HUD/MineResetClockOverlay.cs RefreshFromState(:164): 매 프레임 SetFormattedClock → SetPowered(:202) 순서
- Save/SaveRuntimeController.cs(:764): **UiPauseGate가 잡혀 있으면(디버그 터미널 등) 시계가 흐르지 않는다**
- UI/HUD/MineResetClockIntro.cs: Tick(:68~), TimerTick(:80), TimerEnd(:85). 시간표 HandStart 0.2, HandEnd 0.78
- Audio/SfxPlayer.cs PlayNow(:298 근처): 음소거·0.05초 연타 제한·빈 칸 판정 후 StartVoice. Audio/SfxLibraryDefaults.cs, Resources/SfxLibrary.asset

## 의심 원인 (힌트, 확인할 것)
- eb5137d0 코드만 보면 세 경로 모두 Play 요청은 나가는 것으로 보인다. 칸·클립 GUID 연결도 맞다.
- **가장 유력한 원인은 소리 크기다.** 다듬은 파일을 측정한 값:
  - guide_page_flip_x3: 앞 0~0.65초 SF 부분이 넘김보다 15~20dB 작다(RMS -30~-39dB 대 -16dB). 볼륨 0.5에서는 SF가 묻힌다. 세 장 넘김은 0.65~1.05초에 몰려 "한 장"처럼 들릴 수 있다.
  - timer_tick·timer_tick_single: 째깍이 0.05초짜리 짧은 소리(RMS -21dB)다. 벨(-12dB, 지속)보다 훨씬 작다. 볼륨 0.4~0.45에 BGM까지 있으면 안 들릴 수 있다.
- 그 밖에 확인할 것
  - 가이드: 같은 Play 세션에서 이미 한 번 열었으면 이후는 전부 다시 열기(한 장)다.
  - 째깍: 디버그 터미널을 연 채로 기다리면 시계가 멈춰 째깍이 안 난다. "정확히 1초 감소" 조건(IsFinalTick)은 프레임이 튀어 2초가 줄면 그 초를 놓친다.
  - 만료 연출: 만료 직후 씬 전환·긴 프레임으로 Intro.Update가 늦게 돌거나, 연출 시작 전에 TimerTick이 다른 이유(음소거·연타 제한)로 막히는지 확인한다.

## 1. 진단 (먼저)
- 임시 로그를 넣는다.
  - SfxPlayer.PlayNow: id, 결과(음소거/연타 제한/빈 칸/재생), 클립 길이, 최종 볼륨
  - 가이드 Show: fullIntro, resume
  - 시계: previous, total, poweredOn, IsFinalTick 결과
  - Intro.Tick: previous, time
- 에디터에서 세 상황을 재현한다. 각 소리에 대해 "요청이 안 나감 / 막힘 / 재생됐지만 작음" 중 무엇인지 적는다.

## 2. 고칠 것
① **음원 교체(사용자가 넣어 둔 경우)**
- `work_process/MVP2/sound/sfx-B-fix/`에 같은 이름 파일 3개가 있으면 Assets/_Project/Audio/SFX/UI/의 같은 파일을 덮어쓴다. 기존 .meta(GUID)는 유지한다.
  - guide_page_flip_x3.wav: SF 부분 약 +9.5dB
  - timer_tick.wav, timer_tick_single.wav: +10dB에 리미터
- 길이·타이밍은 같다. 폴더가 없으면 건너뛰고 보고한다.

② **음량** (Defaults와 asset을 같이 고친다. 빌더나 직접 수정)
- 21 GuidePageFlipX3 0.5 → 0.8, 23 TimerTick 0.45 → 0.9, 24 TimerTickSingle 0.4 → 0.9
- 들어 보고도 작으면 값과 함께 "음원 더 키워야 함"으로 보고한다(파일을 직접 다시 인코딩하지 않는다).

③ **가이드**
- 첫 열기: SF 소리 + 세 장 넘김(X3)이 끝까지 들린다. 창 닫기나 다른 소리 때문에 끊기면 안 된다.
- 다시 열기: 한 장 넘김 한 번. 닫던 중 다시 열기: 무음(지금 그대로).
- **닫힘: BeginClose에서 실제로 Closing으로 바뀔 때 UiPopupClose를 한 번 낸다.**
  - X·G·ESC 모두 이 경로라 한 번만 난다. Hidden·이미 Closing·비활성(바로 FinishClose)·HideImmediate·OnDisable은 무음.
  - A 단계 규칙(X·ESC 닫힘은 UiPopupClose만, 회복 팝업·CCTV·전력 바닥과 같은 방식)과 같다.
  - 가이드 X 버튼에 클릭음이 있으면 겹치지 않게 뺀다(지금은 없음).

④ **막판 째깍** (판단은 순수 함수로 뺀다. 예: `MineResetClockTickGate`)
- 조건: 시계가 켜져 있음(poweredOn) + 표시 초가 직전보다 줄었음(1초든 2~3초든) + 새 값이 1~60초 + 마지막 째깍 뒤 실제 시간 0.9초 이상(비스케일, 시계 주입)
- 첫 관측(-1), 값 증가, 0초는 무음. 시계를 끄면 바로 멈춘다.
- 디버그로 시간을 건너뛴 뒤에도 다음 초부터 1초마다 난다. 건너뛴 그 순간은 한 번 나도 되고 안 나도 된다(보고에 적기).

⑤ **3시간 만료**
- 시침이 도는 동안(0.2~0.78초) 째깍째깍, 흔들림 시작(0.78초)에 벨 한 번
- 진단 결과 요청이 안 나갔다면 원인을 고친다. 작아서 안 들렸다면 ①②로 끝난다.
- 연출 없이 카드만 뜨는 경로는 벨만 한 번(지금 그대로).

## 테스트 (EditMode)
- B-160 테스트(PromptB160SfxBStageTests)는 기대값이 바뀐 곳만 고친다.
- 새 테스트
  - 칸 음량: 21=0.8, 23=0.9, 24=0.9 (Defaults와 asset 둘 다)
  - 가이드: 세션 첫 Show → X3 한 번·Flip 0번 / 두 번째 열기 → Flip 한 번 / Show 후 BeginClose → UiPopupClose 한 번, 두 번 불러도 한 번 / Hidden에서 BeginClose·HideImmediate → 무음
  - 째깍 게이트: 61→60→59(1초 간격) 2번 / 10→8(프레임 튐) 1번 / 같은 0.5초 안에 두 번 줄면 1번 / 첫 관측·0초·증가·poweredOn false 무음 / 1000→50 점프 뒤 49·48에서 각각 한 번
  - Intro: Tick(0.05)를 반복하면 TimerTick 한 번(0.2초), TimerEnd 한 번(0.78초) / 큰 dt 한 번에도 각각 한 번
- 관련 EditMode 묶음 실행. 기존 실패(PromptB138StoragePopupTests 오차 1건)는 main에서도 실패하는지 확인해서 적는다.

## 손대지 말 것
- 다른 B-160 소리(판매·보관함·미니맵·키캡·포탈·골드)와 칸 값, 포탈 목적지 변경음
- A 단계 소리, 위험도 경보, 설정창, BGM, 세이브 형식, 관련 없는 리팩터링
- work_process의 원본 음원 폴더(sfx-B, sfx-B-fix)는 복사만 한다

## 보고
- 기준 커밋, 브랜치, 테스트 결과
- 소리별 진단 결과(요청 안 나감 / 막힘 / 작음)와 실제 원인, 고친 클래스·메서드·줄
- 바꾼 칸 값, 음원 교체 여부
- 직접 들어 보지 못한 것은 "미검증"
- 수동 확인 체크리스트 (그대로 넣기, 효과음 음량 50 기준)
  - [ ] Play를 새로 시작해 G로 가이드를 처음 열면 "우웅" SF 소리 뒤 "촤라락" 넘김이 들린다
  - [ ] 닫고 다시 열면 "촥" 한 장만 난다. X·G·ESC로 닫을 때마다 닫힘 소리가 한 번 난다
  - [ ] 디버그 터미널로 남은 시간을 1분 10초 정도로 줄이고 **터미널을 닫은 뒤** 시계(T)를 켜 두면, 60초부터 1초마다 "째깍"이 또렷하게 난다
  - [ ] 째깍 중에 T로 시계를 끄면 바로 멈추고, 다시 켜면 다음 초부터 이어진다
  - [ ] 3시간이 끝나면 시침이 도는 동안 "째깍째깍", 시계가 흔들릴 때 벨이 난다
  - [ ] 째깍이 BGM에 묻히지 않고, 벨보다 지나치게 크지도 않다
  - [ ] Console에 경고·오류가 없고 진단 로그가 남아 있지 않다
- 커밋 메시지 예: `fix: 가이드 넘김·막판 째깍·만료 째깍 효과음 수정 (프롬프트 B-161)`. 사용자 확인 뒤 MVP2-fix99에 커밋한다.
