# B-162 효과음 C 단계 연결 (탐사 실패·긴급 탈출 도착·시작 브리핑·판매 창 열림·포탈 대기음, #158 C)

## 목표
- 사용자가 고른 C 단계 효과음을 B-156~B-161 효과음 시스템 위에 연결한다.
- 음원은 저장소 `work_process/MVP2/sound/` 안의 C 폴더에 있다(Assets 밖, README 제목이 "Sub-Terra C단계 효과음"). 이미 다듬은 파일이라 **음원은 고치지 않는다.**
- 기준: 최신 `MVP2-fix100`(58fa484c 이후)에서 새 브랜치를 따고 PR을 올린다.
- 필요하면 다른 에이전트에게 나눠 맡겨도 된다. 결과는 한 PR로 모은다.

## 절대 규칙 (먼저 읽기)
1. SfxId 번호는 바꾸지 않는다. 새 칸은 34(StorageOrePop) 뒤 35부터 붙인다.
2. 음원은 **복사만** 한다. 원본 폴더 파일을 옮기거나 지우거나 다시 인코딩하지 않는다. 길이·음량은 SfxLibrary 칸 값(volume, maxDuration, fadeOut)으로만 조정한다.
3. 소리는 연출 시간표(…Timeline)의 단계가 바뀌는 순간에 한 번 낸다. 연출 시간, 게임 로직, 입력 순서는 바꾸지 않는다.
4. 시간은 비스케일이다. 시점은 "이번 Tick에 넘었는지(prev < t ≤ now)"로 판정해 프레임이 튀어도 한 번만 난다.
5. 새 소리는 Alert 버스에 두지 않는다(위험도 경보 반복이 끊김).
6. 코드에서 실제 위치나 연출을 못 찾으면 지어내지 말고 보고서에 "못 찾음/연출 없음"으로 적는다.

## 먼저 읽을 것 (sub-terra/Assets/_Project/ 기준)
- Shared/Audio/SfxId.cs, Audio/SfxLibraryDefaults.cs, SfxPlayer.cs(피치 인자, 0.05초 연타 제한, 풀 8개), SfxPlayback.IsThrottled
- Editor/DataValidation/PromptB156SfxLibraryBuilder.cs (새 칸을 에셋에 붙이고 클립 채움)
- C 폴더 README.md (용도·재생 시각·Pixabay 주소), 저장소 루트 `init/sound-effect-address.md`
- B-160/B-161에서 연결한 방식(판매 SellConfirm, 보관함 Tick 판정)을 그대로 따라 한다.

## 1. 파일 복사와 칸
| 번호 | SfxId | 파일 | 버스·폴더 | 시작 음량 / 구간 |
|---|---|---|---|---|
| 35 | RunFailGlitch | run_fail_collapse_glitch.wav | UI | 0.6 |
| 36 | RunFailBlackout | run_fail_blackout.wav | UI | 0.6 |
| 37 | RunFailRowTick | run_fail_row_tick.wav | UI | 0.5 |
| 38 | EscapeArrive | escape_arrive.wav | World | 0.6 |
| 39 | BriefingGlitch | briefing_glitch_tick.wav | UI | 0.5 |
| 40 | BriefingGlitchShort | briefing_glitch_tick_short.wav | UI | 0.45 |
| 41 | BriefingClose | briefing_close.wav | UI | 0.5 / maxDuration 1.0, fadeOut 0.3 |
| 42 | SellCoinStack | sell_open_coin_stack.wav | UI | 0.5 |
| 43 | SellCoinBurst | sell_open_coin_burst.wav | UI | 0.55 (자르지 않고 끝까지) |
| 44 | PortalIdleLoop | portal_idle_loop.wav | World | 0.35 (반복 재생 전용, 5번 참고) |
- 표의 10개 파일만 `Assets/_Project/Audio/SFX/{UI|World}/`에 같은 이름으로 복사한다.
- SfxLibraryDefaults.Create에 표 순서로 추가하고 주석에 원래 길이를 적는다. 대체 표(Fallbacks)는 바꾸지 않는다.
- Unity 에디터로 빌더를 돌려 asset 칸과 .meta를 만든다. portal_idle_loop.wav는 Import에서 Load Type을 길게 재생하기 좋은 값(Compressed In Memory 등)으로 둔다. 에디터를 못 쓰면 기존 형식대로 직접 쓰고 "미검증(에디터 미실행)"으로 적는다.
- `init/sound-effect-address.md`에 파일마다 한 줄: 파일 이름 / 쓰는 곳 / Pixabay 주소 / 2026-10-23 / 비고.

## 2. 연결 위치 (App 코드는 `SfxPlayer.Play`, Gameplay 코드는 `SfxRequests.Play`)
① **탐사 실패 창** (UI/RunFailure/RunFailureRevealTimeline.cs, RunFailurePanelView.cs)
  - 창이 글리치와 함께 처음 나타날 때(창 글리치 시작) RunFailGlitch 한 번
  - 잃은 자원 줄이 하나씩 보이기 시작할 때마다(각 줄 StartTime을 넘을 때) RunFailRowTick 한 번. "잃은 자원 없음"이면 무음
  - 스킵(건너뛰기)으로 한꺼번에 최종 상태가 되면 남은 줄 틱은 내지 않는다
② **실패 후 지상 기지 이동** (RunFailureTransitionTimeline.cs)
  - Darken 단계 시작에 RunFailBlackout 한 번
  - HeavyGlitch 단계는 무음(암전 소리 꼬리가 이어짐)
  - TvTurnOn 단계 시작에 기존 `UiPopupOpen` 한 번 (새 파일 없음)
③ **광산 타이머 만료**: 지금은 TryTimedReset 뒤 만료 팝업만 뜨고 암전·글리치 이동 연출이 없다. 연출이 없으면 소리를 넣지 말고 보고서에 "연출 없음"으로 적는다. 같은 RunFailureTransition을 쓰는 경로가 있으면 ②와 같게 연결한다.
④ **긴급 탈출 도착** (UI/EmergencyEscape/EmergencyEscapePanelBinder.cs, App/Integration/EmergencyEscapePortalRuntimeBridge.cs)
  - 기존 PortalWarp는 그대로 둔다.
  - 이동이 끝나고 카메라가 엘리베이터·전진기지 도착 지점으로 바뀐 **직후 첫 프레임**에 EscapeArrive 한 번
  - 이동과 카메라 전환이 PortalWarp와 같은 프레임이면, 두 소리가 겹치지 않게 비스케일 0.35초 뒤에 낸다. 실제로 어떻게 했는지 보고서에 적는다.
  - 몸 발광·번개 착지 연출은 게임에 없다. 새로 만들지 않는다.
⑤ **시작 브리핑 창** (UI/Tutorial/StartBriefingPopupMotion.cs, StartBriefingTimeline)
  - 창 등장 글리치가 시작될 때 BriefingGlitch 한 번
  - 표시 중 불규칙 글리치(ActiveBurst가 새 값으로 바뀔 때, TriggerGlitch EdgeBurst)마다 BriefingGlitchShort. 같은 칸 0.15초 간격 제한
  - 창이 닫히는 글리치가 시작될 때 BriefingClose 한 번 (닫기 버튼·X·ESC 모두 같음)
  - **심층 구역 해금 창(DeepZoneUnlockPopupMotion)도 이 Motion을 재사용한다. 해금 창에서는 이번 소리를 내지 않는다**(설정 플래그로 시작 브리핑만 켬). 해금 창 소리는 D-2에서 따로 한다.
⑥ **판매 창 열림** (UI/Sell/ResourceSellTimeline.cs, ResourceSellPopupView.cs — 지상 판매와 정산 콘솔이 같은 창)
  - 금화 4개가 쌓일 때(착지 0.15 / 0.23 / 0.31 / 0.39초를 넘을 때마다) SellCoinStack. 피치 1.00 → 1.04 → 1.08 → 1.12. 앞 소리를 끊지 말고 겹치게 둔다
  - 창과 금화가 부딪히는 0.56초를 넘을 때 SellCoinBurst 한 번
  - 시점 값은 하드코딩하지 말고 ResourceSellTimeline 상수/공개 함수에서 꺼낸다
  - 닫힘 연출 시작에 기존 `UiPopupClose` 한 번. 이미 열린 창을 다시 여는 경로는 무음
  - 판매 확정 SellConfirm은 그대로 둔다
⑦ 지금 연결된 다른 소리는 건드리지 않는다.

## 3. 포탈 대기음 (반복 재생, 새 컴포넌트)
- 긴급 탈출 포탈(Gameplay/Building/EmergencyEscapePortal.cs) 근처에서만 나는 반복음. 원샷 풀(SfxPlayer)을 쓰지 않고 **AudioSource 하나**를 따로 둔다(loop 켬).
- 플레이어와 가장 가까운 포탈 하나만 기준으로 한다. 여러 개가 있어도 소리는 하나만 나고 겹치지 않는다.
- 거리에 따라 음량: 가까울수록 크고 또렷하게, 멀어질수록 작게. 시작값은 2칸 이내 최대, 10칸 이상 0(부드러운 곡선). 거리·곡선은 인스펙터에서 바꿀 수 있게 한다.
- 가장 가까운 포탈이 바뀌면 소리 위치만 옮기고 처음부터 다시 틀지 않는다.
- 설정의 효과음 음량·음소거를 그대로 따른다. 게임 일시정지(UiPauseGate) 중에는 멈추거나 작게 한다(어느 쪽인지 보고).
- 범위 안에 포탈이 없으면 재생을 멈춘다. 탐사 실패 이동·화면 암전 중에도 멈춘다.

## 손대지 말 것
- 음원 파일과 원본 폴더, 기존 칸(1~34)의 값·클립, 버스 음량
- A·B 단계 소리, 설정창, 마우스 커서 코드(B-151), 세이브 형식, 관련 없는 리팩터링
- 심층 구역 해금 창 소리, 시설 쿨다운 TV 켜짐 등 D·E 항목

## 테스트
- EditMode 전체: 기존 실패 목록 외 새 실패 없음. 칸 개수를 세는 기존 테스트는 새 개수로 고친다.
- 새 EditMode 테스트 (SfxRequests/SfxPlayer 기록으로 확인)
  - Defaults: 35~44 번호 중복 없음, 파일 이름·버스가 표와 같다
  - 실패 창: 줄 3개면 RowTick 3번, 잃은 자원 없음이면 0번, 스킵하면 남은 틱 없음
  - 이동: Darken 시작 Blackout 한 번, TvTurnOn 시작 UiPopupOpen 한 번, 큰 dt로 건너뛰어도 한 번씩
  - 판매 창: 열기 한 번에 CoinStack 4번(피치 4종)·CoinBurst 1번, 닫기 UiPopupClose 1번
  - 브리핑: 등장 1번, 닫기 1번, 해금 창에서는 0번
  - 포탈 대기음: 포탈 2개 중 가까운 쪽 기준, 범위 밖이면 음량 0/정지
- PlayMode는 이번에 추가한 테스트만 실행한다.

## 수동 확인 체크리스트 (보고서에 그대로 넣기)
- [ ] 쓰러져서 실패 창이 뜰 때 "지직" 소리가 나고, 잃은 자원이 한 줄 올라올 때마다 "톡"이 난다
- [ ] 확인을 누르면 화면이 꺼질 때 "뚝", 지상 기지 화면이 켜질 때 팝업 켜짐 소리가 난다
- [ ] 긴급 탈출로 이동하면 워프 소리 뒤, 도착 화면에서 "뿌웅" 소리가 한 번 난다(두 소리가 뭉개지지 않는다)
- [ ] 새 게임 시작 브리핑 창이 뜰 때 지직, 떠 있는 동안 가끔 짧은 지직, 닫을 때 긴 지직이 난다. 심층 구역 해금 창에서는 이 소리가 안 난다
- [ ] 판매 창(지상·정산 콘솔)을 열면 동전 4개가 쌓이며 "칭칭칭칭" 높아지고, 터질 때 "짤랑~드르르"가 난다. 닫으면 팝업 닫힘 소리가 난다
- [ ] 긴급 탈출 포탈에 다가갈수록 "웅~" 소리가 커지고 멀어지면 작아진다. 포탈 두 개 사이를 오가도 소리가 겹치지 않는다
- [ ] 효과음 음량·음소거가 새 소리와 포탈 대기음에도 적용된다. 위험도 경보가 끊기지 않는다
- [ ] Console에 빈 칸 경고나 오류가 없다

## 완료 후
- PR 제목 예: `feat: 효과음 C 단계 연결 (프롬프트 B-162)`. 커밋 메시지는 한국어 conventional 형식. 브랜치는 지우지 않는다.
- 보고 내용
  - 기준 커밋, 브랜치, PR 링크, 테스트 결과
  - 칸 표와 소리마다 연결한 클래스·메서드·줄
  - 못 찾은 위치, 연출이 없는 항목, 직접 들어 보지 못한 항목은 "미검증"으로 구분
  - 길이·음량 조정이 필요해 보이는 소리(브리핑 닫힘 1.8초, 동전 터짐 1.4초 꼬리, 동전 4번 겹침 음량)
  - 위 수동 확인 체크리스트
