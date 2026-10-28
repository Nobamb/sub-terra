# B-160 효과음 B 단계 연결 (판매·보관함·가이드·타이머·미니맵·키캡·탈출 포탈·골드, #158 B)

## 목표
- #158 B 항목에 사용자가 고른 효과음을 B-156/B-158 효과음 시스템 위에 연결한다.
- 음원은 저장소 `init/work_process/MVP2/sound/sfx-B/`에 있다(Assets 밖). 이미 다듬은 파일이라 **음원은 고치지 않는다.**
- 기준: 최신 main(B-158이 합쳐진 4fcbc406 이후, 지금 98be39de)에서 새 브랜치를 따고 PR을 올린다.
- 필요하면 다른 에이전트에게 나눠 맡겨도 된다. 결과는 한 PR로 모은다.

## 절대 규칙 (먼저 읽기)
1. SfxId 번호는 바꾸지 않는다. 새 칸은 16(UiSlotSelect) 뒤 17부터 붙인다. 3(UiBack)은 비워 둔다.
2. 음원은 **복사만** 한다. `init/.../sfx-B/` 파일을 옮기거나 지우거나 다시 인코딩하지 않는다. 길이·음량 조정은 SfxLibrary 칸 값(volume, maxDuration, fadeOut)으로만 한다.
3. 소리는 연출 시간표(…Timeline)의 단계가 바뀌는 그 순간에 한 번 낸다. 연출 시간, 게임 로직, 입력 순서는 바꾸지 않는다.
4. 시간은 비스케일이다. 시간표 시점을 "이번 Tick에 넘었는지(prev < t ≤ now)"로 판정해 프레임이 튀어도 한 번만 난다.
5. 코드에서 실제 위치를 못 찾으면 지어내지 말고 보고서에 "못 찾음"으로 적는다.

## 먼저 읽을 코드 (sub-terra/Assets/_Project/ 기준, 줄 번호는 98be39de 기준)
- Shared/Audio/SfxId.cs, Audio/SfxLibraryDefaults.cs(폴더 = 버스: `FolderOf(bus)`), SfxPlayer.cs
  - PlayNow(:298): 같은 칸 0.05초 연타 제한(`MinRepeatInterval`), 풀 8개
  - **Alert 버스는 목소리가 하나라 새 Alert 소리가 진행 중인 위험도 경보 반복을 끊는다(:317~322).** 그래서 이번 소리는 Alert에 두지 않는다.
- Editor/DataValidation/PromptB156SfxLibraryBuilder.cs: 새 칸을 에셋에 붙이고 `SFX/{버스 폴더}/{파일 이름}.wav|.ogg|.mp3`로 클립을 채운다.
- 저장소 루트 `init/sound-effect-address.md`(출처 표), `init/work_process/MVP2/sound/sfx-B/README.md`(용도·Pixabay 주소)
  - README의 minimap_beep 줄은 옛 내용이다. 실제 파일은 **자르지 않은 원본 `minimap_beep.mp3`**(삑 0.12초 + 무음 1.6초)이고 사용자 선택이라 그대로 쓴다.

## 1. 파일 복사와 칸 (17칸 추가)
| 번호 | SfxId | 파일 (sfx-B/) | 버스·폴더 | 시작 음량 / 구간 |
|---|---|---|---|---|
| 17 | SellConfirm | sell_confirm.wav | UI | 0.6 |
| 18 | StorageBoxRise | storage_box_rise.wav | World | 0.5 |
| 19 | StorageLidOpen | storage_lid_open.wav | World | 0.7 |
| 20 | StorageTakeAll | storage_take_all.wav | World | 0.6 |
| 21 | GuidePageFlipX3 | guide_page_flip_x3.wav | UI | 0.5 |
| 22 | GuidePageFlip | guide_page_flip_loop.wav | UI | 0.5 |
| 23 | TimerTick | timer_tick.wav | UI | 0.45 / maxDuration 0.6, fadeOut 0.1 |
| 24 | TimerTickSingle | timer_tick_single.wav | UI | 0.4 |
| 25 | TimerEnd | timer_end.wav | UI | 0.6 |
| 26 | MinimapBeep | minimap_beep.mp3 | UI | 0.4 / maxDuration 0.3 (뒤 무음 때문에 목소리를 오래 잡지 않게) |
| 27 | KeycapPop | keycap_pop.wav | UI | 0.45 |
| 28 | PortalActivate | portal_activate.wav | World | 0.5 / maxDuration 1.3, fadeOut 0.3 |
| 29 | PortalSwitch | portal_switch.wav | UI | 0.5 |
| 30 | PortalWarp | portal_warp.wav | World | 0.6 |
| 31 | GoldLever | gold_lever.wav | World | 0.6 |
| 32 | GoldReelStop | gold_reel_stop.wav | World | 0.5 / maxDuration 0.6, fadeOut 0.2 |
| 33 | GoldCoinPop | gold_coin_pop.wav | World | 0.45 |
- 버스 판단: 창·버튼·HUD 소리는 UI, 화면 안 물체(보관 상자, 포탈 웜홀, 머리 위 골드 팝업) 소리는 World. 타이머 끝도 위 Alert 이유로 UI다.
- 표의 17개 파일만 `Assets/_Project/Audio/SFX/{UI|World}/`에 같은 이름으로 복사한다. `guide_page_flip.wav`, `guide_page_flip_intro.wav`는 이번에 안 쓰므로 복사하지 않는다(init에 남는다).
- **광물 튀어나옴(`storage_ore_pop`)은 지금 폴더에 없다.** 폴더에 `storage_ore_pop.*`가 있으면 34번 `StorageOrePop`(World, 0.5)을 추가해 3-② 위치에 연결하고, 없으면 칸을 만들지 말고 보고서에 "파일 없음"으로 적는다.
- SfxLibraryDefaults.Create에 표 순서로 추가하고 주석에 원래 길이를 적는다. 대체 표(Fallbacks)는 바꾸지 않는다.
- Unity 에디터로 빌더를 돌려 asset 칸과 .meta를 만든다. 에디터를 못 쓰면 기존 칸·.meta 형식대로 직접 쓰고 "미검증(에디터 미실행)"으로 적는다.
- `init/sound-effect-address.md` 표에 파일마다 한 줄: 파일 이름 / 쓰는 곳 / README의 Pixabay 주소 / 2026-10-23 / 비고(다듬음 여부).

## 2. 연타·겹침 규칙
- 콜 사이트에서 칸별 최소 간격을 따로 둔다(`SfxPlayback.IsThrottled` 재사용, 시계 주입 가능하게).
  - GoldCoinPop 0.25초, GoldReelStop 0.12초, KeycapPop 0.08초, PortalSwitch 0.08초
- 한 연출에서 한 번만 나는 소리(상자 올라옴, 뚜껑, 레버, 포탈 작동, 가이드 넘김)는 상태 플래그로 한 번만 낸다.
- 닫히는 역순 연출에는 이번에 소리를 넣지 않는다.

## 3. 연결 위치 (App 코드는 `SfxPlayer.Play`, Gameplay 코드는 `SfxRequests.Play`)
① **판매 확정**: UI/Sell/ResourceSellPopupView.cs RequestSell(:515) `case Sold`(:532)에서 StartSaleEffect 직전에 SellConfirm 한 번. Adjusted·Failed·Empty·Busy는 무음.
② **보관함**: UI/Outpost/StoragePopupView.cs
  - Show(:246)가 새로 열 때(이미 열린 상태면 return하는 경로 제외) StorageBoxRise
  - Tick(:313) Opening에서 clock이 `StorageBoxTimeline.LidOpenStart`(0.36)를 넘을 때 StorageLidOpen
  - (파일이 있으면) `PopStart`(0.44)를 넘을 때 StorageOrePop 한 번. 광물 사이 간격 0.04초는 연타 제한보다 짧아 광물마다 내지 않는다. popCount 0이면 무음
③ **전체 보관·전체 꺼내기**: UI/Outpost/OutpostPanelBinder.cs DepositAll(:171)·WithdrawAll(:173)에서 presenter 결과가 `IsSuccess`일 때만 StorageTakeAll. 한 개씩 보관·꺼내기는 무음.
④ **가이드 넘김**: UI/Guide/GameGuidePopupView.cs Show(:159), Tick(:238), 시간표 GameGuideTimeline.cs Full/Quick(:71, :81)
  - 첫 등장(fullIntro, 페이지 3장)은 Show에서 바로 GuidePageFlipX3. 파일 앞 0.6초 SF 소리가 끝날 때 첫 장(0.58초)이 넘어간다.
  - 다시 열기(Quick, 1장)는 clock이 Quick FlipStart(0.24)를 넘을 때 GuidePageFlip
  - 닫던 중 다시 연 경우(resume)는 무음. 시점 값은 GameGuideTimeline에 공개 함수로 꺼내 쓴다.
  - 가이드 안 키캡 그림(GuideKeyCapView)은 이번 대상이 아니다.
⑤ **광산 초기화 타이머**
  - 막판 째깍: UI/HUD/MineResetClockView.cs SetFormattedClock(:103), `style.Observe` 통과(:113) 뒤
    - 표시 초가 직전보다 정확히 1 줄었고 새 값이 1~60초(Final 구간)일 때만 TimerTickSingle 한 번
    - 첫 관측, 시간 점프, 0초는 무음. 시계가 꺼져 있으면(T) 무음
    - `timer_tick`(0.98초)은 째깍이 4번 들어 있어 1초 간격용으로 쓰지 않는다.
  - 3시간 만료 시계 연출: UI/HUD/MineResetClockIntro.cs Play(:31)·Update(:54), 시간표 MineResetClockIntroTimeline
    - 시침이 돌기 시작(HandStart 0.2)할 때 TimerTick, 흔들림 시작(HandEnd 0.78)할 때 TimerEnd
    - 연출 없이 바로 카드가 뜨는 경로(MineResetClockOverlay.ShowTimedResetPopup :235)는 TimerEnd만 한 번
⑥ **미니맵**: App/Integration/ExplorationMinimap.cs PressMapKey(:210), 반환 모드로 MinimapBeep 한 번
  - #158 "단계마다 다른 톤": 피치 Wide 1.0, Square 1.15, Closed 0.85
⑦ **키 조작 창 키캡**: UI/ControlTerminal/ControlTerminalView.cs PlayIntro(:468), Tick(:527) Intro 구간(ApplyIntroKeys :729)
  - 키캡 줄(0~4) 시작 시각(`KeyPopStart + row*KeyStagger`, ControlTerminalTimeline.cs :156, :159)을 넘을 때 KeycapPop, 0.08초 간격 제한이라 실제로는 2~3번. 피치는 줄마다 1.0~1.12로 조금씩 올린다.
  - ShowSettled(연출 없음)는 무음
⑧ **긴급 탈출 포탈**: UI/EmergencyEscape/EmergencyEscapePanelBinder.cs
  - Open(:144)의 `view.SetVisible(true)`(:166) 직후 PortalActivate. 웜홀 회전(0~0.6초)·창 튀어나옴과 같이 간다
  - 목적지 변경: StepSelection(:212)에서 Step이 true일 때, HandleDestinationSelected(:365)에서 index가 지금 선택과 다를 때 PortalSwitch. Open 안의 첫 PreviewDestinationAt(:168)은 무음
  - ConfirmSelectedDestination에서 `TryEscapeTo` 성공(:267) 시 Close 전에 PortalWarp. 실패·불가 항목은 이번에 무음
  - C단계 탈출 실행 연출(몸 발광·번개 착지) 소리는 대상이 아니다.
⑨ **골드 획득 팝업**: App/Integration/GoldPickupPopupState.cs(이벤트 :8, Advance :209), GoldPickupVfx.cs(PlayPopup :193, TickPopup :242)
  - 레버: `GoldPickupPopupEvents`에 `LeverPulled = 16`을 추가하고 Advance에서 Elapsed가 `GoldPickupPopupTimeline.LeverDownStart`(0.36)를 넘을 때 한 번 낸다. TickPopup이 받아 GoldLever
  - 숫자 착지: BaseLanded면 GoldReelStop(피치 1.0), BonusLanded면 GoldReelStop(피치 1.1). 같은 프레임이면 한 번
  - 금화 튀어나옴: 착지 이벤트와 합치기(PlayPopup :219 `OnMerged`)로 금화가 나올 때 GoldCoinPop, 0.25초 간격 제한. 연속 채굴로 합쳐져도 레버·착지음은 다시 나지 않는다
  - 퇴장, HUD로 날아가는 입자는 무음

## 손대지 말 것
- 음원 파일과 init 폴더, 기존 칸(1~16)의 값·클립, 버스 음량, B-157 음량 코드
- A 단계 소리(팝업 열림·닫힘, 호버, 위험도 경보), 설정창, 슬라이더 틱
- 판매·보관·가이드 창의 열림·닫힘·행 선택·실패음 (#158 B의 나머지는 이번 대상 아님)
- 마우스 커서 코드(B-151), 세이브 형식, 관련 없는 리팩터링

## 테스트
- EditMode 전체: 기존 실패 목록 외 새 실패 없음. 칸 개수를 세는 기존 테스트는 15 → 32(광물 파일이 있으면 33)로 고친다.
- 새 EditMode 테스트 (SfxRequests/SfxPlayer 기록으로 확인)
  - Defaults: 17~33 번호 중복 없음, 파일 이름·버스가 표와 같다, 주소 문서에 새 줄이 있다
  - 판매 Sold 한 번 SellConfirm, Failed는 무음
  - 보관함 Tick: 열기 한 번에 BoxRise·LidOpen 각 한 번, 큰 dt 한 번으로 건너뛰어도 한 번
  - 가이드: 첫 등장 X3 한 번, 다시 열기 Flip 한 번, resume 무음
  - 타이머: 61→60→59 째깍 2번, 첫 관측·10→7 점프·1→0은 무음
  - 골드: LeverPulled 한 번, Base·Bonus 착지 reel 2번, 합치기 3번을 0.1초 안에 하면 coin 1번
  - 포탈: Open 시 Activate 한 번·Switch 없음, Step 한 번 Switch 한 번, 성공 Warp 한 번
- PlayMode는 이번에 추가한 테스트만 실행한다.

## 수동 확인 체크리스트 (보고서에 그대로 넣기)
- [ ] 판매 창에서 판매하면 "철컹-딸랑" 금전등록기 소리가 한 번 난다
- [ ] 보관함을 열면 상자 올라올 때 "치익", 뚜껑 열릴 때 "탁"이 난다. 전체 보관·꺼내기를 누르면 묵직한 돌 굴러가는 소리가 난다
- [ ] 게임 가이드를 처음 열면 SF 소리 뒤 "촤라락" 세 장 넘김, 다시 열면 한 장 넘김이 난다
- [ ] 시계(T)를 켜 두고 1분 이하로 남으면 1초마다 "째깍"이 한 번씩 난다
- [ ] 3시간이 끝나면 시침이 도는 동안 째깍째깍, 시계가 흔들릴 때 벨이 울린다
- [ ] M을 누를 때마다 "삑"이 나고 가로형·정사각형·닫힘의 높이가 다르다
- [ ] 키 조작 창을 열면 키캡이 튀어나올 때 "톡톡"이 2~3번 난다
- [ ] 긴급 탈출 포탈 창이 열릴 때 충전음, 목적지를 바꿀 때 "삐릿", 이동하면 워프 소리가 난다
- [ ] 골드를 캐면 레버 "철컥" → 숫자 착지 → 동전 쏟아지는 소리. 연속으로 캐도 동전 소리가 쌓이지 않는다
- [ ] 위험도 경보 중 위 소리가 나도 경보가 끊기지 않는다. 설정의 효과음 음량·음소거가 새 소리에도 적용된다
- [ ] Console에 빈 칸 경고나 오류가 없다

## 완료 후
- PR 제목 예: `feat: 효과음 B 단계 연결 (프롬프트 B-160)`. 커밋 메시지는 한국어 conventional 형식. 브랜치는 지우지 않는다.
- 보고 내용
  - 기준 커밋, 브랜치, PR 링크, 테스트 결과
  - 칸 표(번호, SfxId, 파일, 폴더, 음량·구간)와 소리마다 연결한 클래스·메서드·줄
  - 못 찾은 위치, 직접 들어 보지 못한 항목은 "미검증"으로 구분
  - 길이·음량을 듣고 조정이 필요해 보이는 소리(예: 상자 1.8초, 포탈 작동, 타이머 벨이 카드 열림 소리와 겹침)
  - 위 수동 확인 체크리스트
