# B-163 효과음 B 단계 마무리 (판매·보관함·타이머 5분 알림·키 조작 창·탈출 포탈, #158 B)

## 목표
- #158 B에 남은 소리를 B-156~B-162 효과음 시스템 위에 붙여서 B 단계를 끝낸다.
- 새 소리 6개는 사용자가 `Assets/_Project/Audio/SFX/UI/`에 이미 넣어 두었다. 설명과 출처는 `work_process/MVP2/sound/sfx-B-rest/README.md`에 있다. **음원은 고치지 않는다.**
- 나머지 칸은 새 파일 없이 기존 공통음(UiClick, UiConfirm, UiCancelFail, UiPopupClose)을 붙인다.
- 기준: 최신 `MVP2-fix101`(6c94d07a 이후)에서 새 브랜치를 따고 PR을 올린다.
- 필요하면 다른 에이전트(Codex 등)에게 나눠 맡겨도 된다. 결과는 한 PR로 모은다.

## 절대 규칙
1. SfxId 번호는 바꾸지 않는다. 새 칸은 44(PortalIdleLoop) 뒤 45부터 붙이고, 3번은 계속 비워 둔다.
2. wav 6개가 위 폴더에 없으면 지어내지 말고 멈춘 뒤 보고한다. 길이·음량은 SfxLibrary 칸 값으로만 조정한다.
3. 새 소리는 모두 UI 버스에 둔다. 빌더가 `FolderOf(bus)`로 폴더를 찾으니 UI 폴더 파일은 UI 버스여야 한다. Alert 버스는 목소리가 하나라 위험도 경보가 끊기므로 쓰지 않는다.
4. SfxPlayer 규칙(같은 소리 0.05초에 한 번, 동시 8개, ignoreListenerPause)은 그대로 둔다. 연타가 잦은 곳만 부르는 쪽에서 `SfxThrottle`을 쓴다.
5. 코드가 이미 결과음을 내는 버튼에는 클릭음을 겹치지 않는다. 연출 시간, 게임 로직, 입력 순서는 바꾸지 않는다.
6. 실패음은 창 안에서 `SfxPlayer.Play(SfxId.UiCancelFail)`로 직접 낸다. A 단계 창(SurfaceBaseBinder, CoreCctvPopupView)과 같은 방식이다. #158 메모의 `FailureFeedback`은 아직 코드에 없고 D 단계(머리 위 노란 경고) 몫이니 이번에 만들지 않는다.
7. 위치를 못 찾으면 지어내지 말고 보고서에 "못 찾음"으로 적는다.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/` 기준)
- Scripts/Shared/Audio/SfxId.cs, Scripts/App/Audio/SfxLibraryDefaults.cs, SfxPlayer.cs, UiSfx.cs(`UiSfx.Attach(button, click, hover, failWhenDisabled)`: 비활성 버튼을 누르면 UiCancelFail)
- Editor/DataValidation/PromptB156SfxLibraryBuilder.cs (없는 칸만 붙이고 클립을 채운다)
- `work_process/MVP2/sound/sfx-B-rest/README.md`, 저장소 루트 `init/sound-effect-address.md`

## 1. 새 칸 (모두 UI 버스)
| 번호 | SfxId | 파일 | 시작 음량 |
|---|---|---|---|
| 45 | StorageClose | storage_close.wav | 0.7 (원래 작은 소리) |
| 46 | StoragePartialNotice | storage_partial_notice.wav | 0.65 |
| 47 | Timer5MinAlert | timer_5min_alert.wav | 0.6 |
| 48 | KeysHoloOpen | keys_holo_open.wav | 0.45 (키캡 소리를 덮지 않게) |
| 49 | KeysPageTurn | keys_page_turn.wav | 0.65 |
| 50 | PortalClose | portal_close.wav | 0.55 |
- `SfxLibraryDefaults.Create()`에 표 순서로 추가하고, 주석에 길이를 적는다(1.00 / 0.80 / 0.80 / 0.90 / 0.62 / 0.71초). 요약 주석의 칸 수도 고친다. Fallbacks는 바꾸지 않는다.
- Unity 에디터에서 빌더(SubTerra/Audio/Create Or Refresh Sfx Library)를 돌려 `Resources/SfxLibrary.asset` 칸을 채운다. 에디터를 못 쓰면 기존 형식대로 직접 쓰고 "미검증(에디터 미실행)"이라고 적는다.
- `init/sound-effect-address.md`에 파일마다 한 줄씩 남긴다: 파일 이름 / 쓰는 곳 / README의 Pixabay 주소 / 2026-10-24 / 비고.

## 2. 연결 위치 (`Scripts/App/...`, 줄 번호는 6c94d07a 기준 근사)
① **판매 창** (UI/Sell/ResourceSellPopupView.cs, ResourceSellRowView.cs: 지상과 정산 콘솔이 같은 창)
  - 행의 − / + / +5 / +10 / 최대(RowView ~157-161)와 전체 선택·초기화(PopupView ~1307-1308)에 `UiSfx.Attach(…, UiClick, hover:false, failWhenDisabled:false)`를 붙인다. 0개나 최대일 때 비활성 버튼을 눌러도 무음이다.
  - `RequestSell`(~536)에서 Failed, Adjusted, Empty일 때 UiCancelFail을 한 번 낸다. Busy는 무음이고 Sold의 SellConfirm은 그대로 둔다.
  - 판매 버튼에는 `UiSfx.Attach(sellButton, SfxId.None, failWhenDisabled:true)`를 붙여서, 고른 게 없어 막힌 버튼을 누르면 실패음이 나게 한다. 닫기 X는 BeginClose(~359)가 이미 UiPopupClose를 내니 건드리지 않는다.
② **보관함** (UI/Outpost/StoragePopupView.cs, OutpostPanelBinder.cs, App/Outpost/OutpostService.cs)
  - 닫기: `StoragePopupView.BeginClose`(~269)에서 실제로 역순 연출을 시작할 때만 StorageClose를 한 번 낸다. 비활성이라 바로 `FinishClose`로 가는 경로는 무음이다. X·ESC와 시설에서 멀어질 때(OutpostPanelView ~223) 모두 같은 경로다.
  - 클릭음 UiClick(failWhenDisabled:false)을 붙일 곳:
    - 목록 행(StorageCargoRowView ~87)
    - 드롭다운 항목(OutpostMineralPickerView ~239)
    - 수량 −(~1186), 빠른 수량(~1202), 모두(~1208)
  - 선택 수량 보관·꺼내기(~1227-1228)와 전체 보관·꺼내기(~1237-1238) 버튼에는 `UiSfx.Attach(…, SfxId.None, failWhenDisabled:true)`를 붙인다. 옮길 게 없어 막힌 버튼을 누르면 실패음이 난다.
  - 결과음은 Binder에서 낸다:
    - `DepositQuantity`/`WithdrawQuantity`(~163-170): 성공이면 UiConfirm, 실패(한도 초과, 수량 부족 등)면 UiCancelFail
    - `PlayTakeAllSfx`(~178): 실패면 UiCancelFail
    - busy로 막힌 요청("처리 중입니다.")은 무음
  - 일부만 꺼내짐(#212): `OutpostService.TryTransferAll`(~430-486)에서 `total < requestedTotal`일 때뿐이다(한 개씩 꺼내기는 넘치면 실패).
    - 메시지 문자열을 비교하지 말고 `OutpostOperationResult`에 `IsPartial`(기본 false)을 추가한다.
    - 일부만 꺼내졌으면 StorageTakeAll 대신 StoragePartialNotice만 한 번 낸다. 두 소리가 겹치지 않게 하려는 것이다.
③ **타이머 5분 알림** (UI/HUD/MineResetClockOverlay.cs, MineResetClockStyle.cs)
  - **결정: T 시계를 꺼 두어도 들린다.** 1분 째깍은 시계 화면(`MineResetClockView.SetFormattedClock`)에서 나서 시계가 켜져 있을 때만 들리지만, 5분 알림은 놓치면 안 되는 안내라서 시계와 관계없이 낸다.
  - 시계를 끄면 `RefreshFromState`가 `SetFormattedClock`을 부르지 않고 `Update`(~377)도 시계가 켜져 있을 때만 갱신한다. 그래서 알림 판정은 Overlay.Update에서 따로 한다.
  - 판정 조건: sessionVisible, !departing, ActiveSlot>0일 때만 `runtime.MineResetRemainingSeconds`를 정수 초로 읽는다.
  - 판정은 순수 클래스 `MineResetFiveMinuteGate`로 만든다(`MineResetClockTickGate`처럼 테스트할 수 있게).
    - 낼 때: 직전 관측이 300초보다 크고 이번 값이 61~300초(`GetBand`가 Critical로 바뀌는 순간)
    - 무음: 첫 관측(불러오기, 씬 진입), 시간 증가(초기화), 10초 넘는 큰 건너뛰기
    - 같은 주기에는 한 번만 낸다. 남은 시간이 다시 300초를 넘으면 다시 낼 수 있다. GameState가 바뀌면 처음부터 다시 잰다.
  - 30분 구간(1800초)은 무음이다. 1분 째깍(60초 이하)과 끝 벨과는 시점이 겹치지 않는다. 일시정지 중의 동작(UiPauseGate로 Update가 멈춤)은 보고서에 적는다.
④ **키 조작 창** (UI/MainMenu/ControlSchemePanel.cs, UI/ControlTerminal/ControlTerminalView.cs)
  - `ControlSchemePanel.Open`(~79)이 `terminal.PlayIntro()`를 부르는 플레이 중 경로에서 KeysHoloOpen을 한 번 낸다. t=0 프레임 조립 때 나므로 KeycapPop(0.42초~)보다 먼저 난다. `ShowSettled` 경로는 무음이다.
  - `Cycle`(~71): 창이 열려 있고 선택이 실제로 바뀌었을 때 KeysPageTurn을 한 번 낸다. 즉시 저장되므로 저장음은 없다. 화살표 버튼에는 클릭음을 붙이지 않는다.
  - 닫기: **결정: 기존 UiPopupClose.** 다른 창 닫힘과 같은 소리로 맞추고 새 칸을 늘리지 않는다(예비 홀로그램 닫힘 614160은 쓰지 않음).
    - `Close`(~93)가 `terminal.PlayOutro()`를 부르는 경로에서만 낸다. 돌아가기 버튼, X, ESC(MainMenuBinder ~119, UndergroundMenuController ~91, SurfaceBaseBinder ~208)가 모두 이 경로다.
    - OnDisable에서 부르는 Close는 무음이다.
⑤ **긴급 탈출 포탈** (UI/EmergencyEscape/EmergencyEscapePanelBinder.cs)
  - `Close`(~192): busy를 풀기 전에 확인해서, `view.IsOpen`이고 busy가 아닐 때만 PortalClose를 낸다. X, 닫기 버튼, ESC(LateUpdate ~148, UndergroundMenuController ~118/135)가 모두 이 경로다.
  - 이동 성공 때는 Bridge(EmergencyEscapePortalRuntimeBridge ~232)가 busy 중에 Close를 부르고, 그 뒤 Binder가 다시 부를 때는 이미 Closing 상태다. 그래서 워프 때 PortalClose가 나지 않는다. 테스트로 확인한다.
  - `ConfirmSelectedDestination`(~265)의 실패 경로마다 UiCancelFail을 한 번 낸다:
    - 경로 준비 안 됨(~274)
    - 목적지 없음(~280)
    - 흐린 목적지: 골드·전력 부족(~291-295)
    - 이동 실패(~306, !success)
  - 이동 버튼에 `UiSfx.Attach(confirmButton, SfxId.None, failWhenDisabled:true)`를 붙인다. 흐린 목적지에서 막힌 버튼을 마우스로 눌러도 같은 실패음이 난다. Enter 키는 ~293 경로로 간다.
⑥ 지금 연결된 다른 소리(A~C)는 건드리지 않는다.

## 손대지 말 것
- 음원 파일, 기존 칸(1~44)의 값과 클립, 버스 음량, 설정창
- 마우스 커서 코드(B-151 보류), 세이브 형식, 관련 없는 리팩터링
- D·E 항목: FailureFeedback 입구, 머리 위 노란 경고, 드릴 루프, 버튼 전체 점검

## 테스트
- EditMode 전체에서 기존 실패 목록 말고 새 실패가 없어야 한다. 칸 개수를 세는 기존 테스트는 새 개수로 고친다.
- 새 EditMode 테스트 `PromptB163SfxBRestTests` (SfxPlayer 기록으로 확인)
  - Defaults: 45~50 번호 중복 없음, 파일 이름이 표와 같고 모두 UI 버스
  - 판매: Failed, Adjusted, Empty면 UiCancelFail 1번, Sold면 0번
  - 보관: 한 개 보관 성공이면 UiConfirm, 한도 초과면 UiCancelFail, 일부 꺼냄이면 PartialNotice 1번에 TakeAll 0번, BeginClose 1번이면 StorageClose 1번
  - 5분 게이트: 301→300이면 1번, 1801→1800이면 0번, 첫 관측 200이면 0번, 300→299면 0번, 400→250(큰 건너뛰기)이면 0번
  - 키 조작: Open이면 HoloOpen 1번, Cycle(+1)이면 PageTurn 1번, Close면 UiPopupClose 1번
  - 포탈: 열린 창 Close면 PortalClose 1번, 이동 성공이면 PortalClose 0번, 실패 4경로마다 UiCancelFail 1번
- PlayMode는 이번에 추가한 테스트만 돌린다.

## 수동 확인 체크리스트 (보고서에 그대로 넣기)
- [ ] 판매 창에서 −/+/최대, 전체 선택, 초기화를 누르면 "딸깍"이 난다. 아무것도 안 고르고 판매 버튼을 누르면 실패음이 난다.
- [ ] 판매 창을 연 채로 다른 곳에서 자원이 줄어 판매하면 수량이 다시 맞춰지면서 실패음이 난다.
- [ ] 보관함에서 광물 줄이나 −/+/모두를 누르면 "딸깍"이 난다. 한 개 보관이나 꺼내기에 성공하면 짧은 확인음이 난다.
- [ ] 화물이 꽉 찬 상태에서 꺼내기를 누르면 실패음이 난다.
- [ ] 화물이 거의 찬 상태에서 "전체 꺼내기"를 누르면 묵직한 소리 대신 부드러운 "주의" 소리가 나고 일부만 꺼내진다.
- [ ] 보관함을 X나 ESC로 닫으면 상자가 내려가면서 "탁, 딸깍" 소리가 난다.
- [ ] 남은 시간이 5:00이 되는 순간 "삐삐삐삐"가 한 번 난다. T로 시계를 꺼 둬도 들린다. 30:00에는 소리가 없다.
- [ ] 키 조작 창을 열면 홀로그램이 펼쳐지는 소리가 먼저 나고 키캡 "톡톡"이 이어진다. 이전/다음을 누르면 "스윽" 소리가 나고, 돌아가기나 ESC를 누르면 닫힘 소리가 난다.
- [ ] 포탈 창을 이동 없이 닫으면 빨려 들어가는 소리가 난다. 이동하면 워프음만 난다.
- [ ] 포탈 창에서 흐린 목적지를 골라 이동을 누르면(클릭이나 Enter) 실패음이 난다.
- [ ] 효과음 음량과 음소거가 새 소리에도 적용되고, 위험도 경보가 끊기지 않는다. Console에 빈 칸 경고나 오류가 없다.

## #158 대응 (완료 표시용)
| #158 줄 | 내용 | 이 프롬프트 |
|---|---|---|
| 46 | 판매 수량·전체 선택·초기화 클릭 | ① 첫째 |
| 47 | 판매 실패·자동 조정 실패음 | ① 둘째·셋째 |
| 50 | 보관함 닫힘 | ② 닫기 |
| 51 | 보관함 광물·수량 클릭 | ② 클릭음 |
| 52 | 한 개 보관·꺼내기 확인음 | ② 결과음 |
| 53 | 보관함 실패음, 일부 꺼냄 안내음 | ② 결과음·일부 |
| 57 | 타이머 5분 알림 | ③ |
| 61-63 | 키 조작 창 열림·넘김·닫힘 | ④ |
| 66-67 | 포탈 이동 없이 닫힘, 실패음 통일 | ⑤ |
- 다 되면 44, 48, 55, 59, 64의 부모 줄도 체크할 수 있다. #158 본문은 이 작업에서 고치지 않는다(PM이 고침).

## 완료 후
- PR 제목 예: `feat: 효과음 B 단계 마무리 (프롬프트 B-163)`. 커밋 메시지는 한국어 conventional 형식으로 쓰고, 브랜치는 지우지 않는다.
- 보고할 것
  - 기준 커밋, 브랜치, PR 링크, 테스트 결과
  - 칸 표, 그리고 소리마다 연결한 클래스·메서드·줄
  - 못 찾은 위치와 직접 들어 보지 못한 항목은 "미검증"으로 구분
  - 5분 알림의 일시정지 중 동작, 음량 조정이 필요해 보이는 소리
  - 위 수동 확인 체크리스트
