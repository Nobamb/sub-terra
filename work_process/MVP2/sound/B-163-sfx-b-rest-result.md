# B-163 결과: 효과음 B 단계 마무리

- 기준 커밋: `09e0ad8b` (MVP2-fix102, `6c94d07a` 이후 문서·음원 커밋), 브랜치: `MVP2-fix102`
- 커밋·PR: 아직 없음(사용자 확인 대기)

## 칸 표 (`SfxLibraryDefaults.Create`, `Resources/SfxLibrary.asset`)
| 번호 | SfxId | 파일 | 버스 | 음량 | 클립 길이 | 0.1초 최대 × 칸 음량 |
|---|---|---|---|---|---|---|
| 45 | StorageClose | UI/storage_close.wav | UI | 0.7 | 1.00s | -20.5 dB |
| 46 | StoragePartialNotice | UI/storage_partial_notice.wav | UI | 0.65 | 0.80s | -11.2 dB |
| 47 | Timer5MinAlert | UI/timer_5min_alert.wav | UI | 0.6 | 0.80s | -18.3 dB |
| 48 | KeysHoloOpen | UI/keys_holo_open.wav | UI | 0.45 | 0.90s | -20.0 dB |
| 49 | KeysPageTurn | UI/keys_page_turn.wav | UI | 0.65 | 0.62s | -11.9 dB |
| 50 | PortalClose | UI/portal_close.wav | UI | 0.55 | 0.71s | -17.4 dB |

- 음원은 `sfx-B-rest`에서 `Audio/SFX/UI/`로 복사만 했다(cmp로 같은 바이트 확인). 빌더(`SubTerra/Audio/Create Or Refresh Sfx Library`)를 에디터에서 돌려 칸 6개를 채웠다. 모노·Decompress On Load로 import됨.
- 기존 칸 1~44의 값은 그대로다. 저장할 때 41번 `volume: 1.0`이 `1`로 다시 쓰였을 뿐 값은 같다. 대체 표도 그대로다.
- 같은 방식으로 잰 KeycapPop은 -25.6 dB다. 모두 기존 범위(-9.7 ~ -23.5 dB) 안이다.

## 연결 위치 (`Scripts/App/...`)
| 소리 | 클래스·메서드(줄) | 조건 |
|---|---|---|
| UiClick | `ResourceSellRowView` 생성(171-175): − / + / +5 / +10 / 최대 | 막힌 버튼은 무음(failWhenDisabled:false) |
| UiClick | `ResourceSellPopupView.Build…`(1312-1313): 전체 선택·초기화 | 같음 |
| UiCancelFail | `ResourceSellPopupView.RequestSell`(561/565/569) | Adjusted·Failed·Empty일 때 1번. Busy는 무음, Sold는 SellConfirm 그대로 |
| UiCancelFail | 판매 버튼 `UiSfx.Attach(sellButton, None)`(1482) | 고른 게 없어 막힌 버튼을 누를 때만 |
| StorageClose | `StoragePopupView.BeginClose`(292) | 역순 연출을 실제로 시작할 때만. 비활성이라 바로 FinishClose로 가면 무음 |
| UiClick | `StorageCargoRowView`(90), `OutpostMineralPickerView`(242), `StoragePopupView` 수량 −(1188)·빠른 수량(1205)·모두(1212) | failWhenDisabled:false |
| UiCancelFail | `StoragePopupView` 보관·꺼내기·전체 보관·전체 꺼내기(1244-1247) `Attach(…, None)` | 막힌 버튼을 누를 때만 |
| UiConfirm / UiCancelFail | `OutpostPanelBinder.DepositQuantity`/`WithdrawQuantity` → `PlayQuantitySfx`(208) | 성공이면 확인음, 실패면 실패음. 요청 전에 `OutpostPanelPresenter.IsBusy`였으면("처리 중입니다.") 무음 |
| UiCancelFail / StoragePartialNotice / StorageTakeAll | `OutpostPanelBinder.PlayTakeAllSfx`(224/228) | 실패면 실패음. 성공이면서 `IsPartial`이면 안내음만, 아니면 기존 묵직한 소리 |
| (판정) | `OutpostOperationResult.IsPartial`(기본 false), `OutpostService.TryTransferAll`(483-487) | 전체 꺼내기에서 `total < requestedTotal`일 때만 true |
| Timer5MinAlert | `MineResetClockOverlay.Update` → `ObserveFiveMinuteAlert`(424), 판정은 순수 클래스 `UI/HUD/MineResetFiveMinuteGate` | sessionVisible·!departing·ActiveSlot>0. T 시계를 꺼 두어도 판정한다 |
| KeysHoloOpen | `ControlSchemePanel.Open`(98) | `PlayIntro` 경로에서만, PlayIntro 직전(t=0). ShowSettled 경로는 무음 |
| KeysPageTurn | `ControlSchemePanel.Cycle`(81) | 창이 열려 있고 선택이 실제로 바뀌었을 때. 화살표 버튼 클릭음 없음 |
| UiPopupClose | `ControlSchemePanel.Close`(114) | `PlayOutro` 경로에서만. OnDisable의 Close는 isActiveAndEnabled가 false라 HideNow 경로라서 무음 |
| PortalClose | `EmergencyEscapePanelBinder.Close`(197) | busy를 풀기 전에 `!busy && view.IsOpen`일 때만 |
| UiCancelFail | `EmergencyEscapePanelBinder.ConfirmSelectedDestination`(281/288/302/324) | 경로 준비 안 됨 / 목적지 없음 / 흐린 목적지 / 이동 실패 |
| UiCancelFail | 이동 버튼 `UiSfx.Attach(confirmButton, None)`(`WireButtons` 367) | 흐린 목적지라 막힌 버튼을 마우스로 누를 때 |

- 5분 게이트: 직전 값이 300초보다 크고 이번 값이 61~300초이면서 10초 이하로 줄었을 때만 낸다. 첫 관측, 시간 증가, 10초 넘는 건너뛰기는 무음이다. `GameBootstrapper.State`가 바뀌거나 세션이 끝나거나(departing·슬롯 없음 포함) 하면 다시 첫 관측부터 잰다. 초는 `FormatClock`과 같은 방식으로 내림한다.
- 일시정지 중의 5분 알림: `UiPauseGate`가 잡힌 동안 Overlay.Update가 멈추지만, `SaveRuntimeController`(763줄)도 같은 조건에서 광산 시간을 쌓지 않는다. 그래서 정지 중에 5:00을 지나는 일이 없고, 풀린 뒤 이어서 판정하므로 놓치지 않는다.
- 포탈 이동 성공: Bridge가 처리 중(busy)에 Close를 부르고, 그 뒤 Binder가 다시 부를 때는 이미 Closing 상태라 PortalClose가 나지 않는다(테스트로 확인).
- 키 조작 창은 EditMode에서 `Application.isPlaying`이 false라 연출 경로를 탈 수 없다. 그래서 확인용 `ControlSchemePanel.ForceMotionForTests`를 추가했다. 실제 동작은 바꾸지 않는다.
- 못 찾은 위치: 없음.

## 테스트
- 새 EditMode `PromptB163SfxBRestTests`: 12개 모두 통과.
- 기존 테스트 수정
  - `PromptB156SfxTests` 칸 수 43 → 49
  - `OutpostServiceTests`에 `IsPartial` 확인 추가(일부 꺼냄이면 true, 전부 꺼냄이면 false)
- 대상 묶음(B-156/158/160/161/162/163, OutpostServiceTests): 주소 문서를 고친 뒤 B-162 한 개만 실패. 이 실패는 아래 기존 실패와 같다.
- EditMode 전체: 2037 통과 / 20 실패.
  - 19개는 B-162 보고서에 적힌 기존 실패와 같은 묶음이다(건설 배치, 패널 폭 580/500, HUD 크기, B-85 칩, B-89 인벤토리 닫기, B-107 씬, B-138·B-150 미세 수치 등).
  - 나머지 1개 `PromptB162SfxCStageTests.Defaults_CSlotsMatchTable…`(BriefingClose 기대 0.5, 실제 1.0)는 `6c94d07a`에서 기본값을 1.0으로 바꾸면서 테스트를 고치지 않아 기준 커밋에서 이미 실패한다. 이번 범위(기존 칸 값) 밖이라 고치지 않았다.
  - 이번 변경 파일을 쓰는 테스트 중 새로 실패한 것은 없다. 기준 커밋에서 전체를 다시 돌려 비교하지는 않았다(**미검증**).
- PlayMode: 이번에 추가한 PlayMode 테스트는 없다.

## 미검증
- 플레이에서 직접 들어 보지 못했다: 위 소리 전부(판매 창 지상·정산 콘솔, 보관함, 5분 알림, 키 조작 창, 포탈 창).
- 보관함 목록 행·드롭다운 항목의 클릭음은 컴포넌트가 붙는 코드만 확인했다. 데이터가 있는 상태로 눌러 보는 테스트는 없다.
- 키 조작 창을 OnDisable로 닫을 때 무음인 것은 코드 경로로만 확인했다(EditMode에서는 OnDisable이 불리지 않음).

## 음량 조정이 필요해 보이는 소리
- KeysHoloOpen(-20.0 dB)이 같은 방식으로 잰 KeycapPop(-25.6 dB)보다 약 5.6 dB 크다. 0.9초 길이라 0.42초부터 나는 키캡과 겹친다. 키캡이 묻히면 0.3 정도로 낮출 것을 권한다.
- StoragePartialNotice(-11.2)와 KeysPageTurn(-11.9)은 새 소리 중 가장 크다. 둘 다 UiCancelFail보다 부드러운 소리지만, 플레이에서 튀면 0.5 정도로 낮춘다.

## 수동 확인 체크리스트
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

## #158 대응
46, 47, 50, 51, 52, 53, 57, 61-63, 66-67 줄을 이번 작업에서 연결했다. 수동 확인을 마치면 부모 줄 44, 48, 55, 59, 64도 체크할 수 있다. #158 본문은 고치지 않았다.
