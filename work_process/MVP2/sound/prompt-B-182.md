# B-182 화물 공간 부족·버리기 창 소리 채우기, 기존 소리만 (#158 E "화물" 6줄)

## 목표
- 퀘스트 보상을 받으려는데 화물이 꽉 찼을 때 뜨는 두 창(화물 공간 부족 B-152, 화물 버리기 B-153)이 지금 무음이다. **이미 있는 소리로만** 채운다. 새 음원·새 SfxId는 없다.
- 소리 id·음량 배율·간격은 연결 지점마다 상수 하나로 둬서 PM이 숫자만 바꿔 끼울 수 있게 한다(아래 ★).
- 필요하면 다른 에이전트에게 나눠 맡겨도 된다. 결과는 한 브랜치로 모은다.

## 브랜치
1. `git fetch`로 원격 `MVP2-fixNNN` 중 **가장 큰 번호**를 찾고, 그 브랜치에 어떤 프롬프트 커밋(`… (프롬프트 B-NNN)`)이 들어 있는지 `git log`로 확인한다.
   - 계획 순서: B-179(`fix124`, `b1471534`+문서 `385b4a98`) → B-179-1(`fix125`) → B-180(`fix126`) → B-180-1(`fix127`) → B-181(`fix128`) → **B-182(`fix129`)**. PM 확인 시점(2026-11-02 15:45 KST)에는 `MVP2-fix124`가 최신이다.
2. 가장 최신 브랜치 위에서 **비어 있는 다음 번호** `MVP2-fixNNN`을 만든다. 계획대로면 `fix128`(B-181) 위의 `MVP2-fix129`.
3. 보고서 맨 위에 기준 브랜치·커밋과 B-179-1·B-180·B-180-1·B-181 포함 여부를 적는다. 계획과 번호가 어긋났으면 뒤에 돌릴 프롬프트가 어느 번호를 써야 하는지도 적는다.
4. 기준에 B-179 커밋(`b1471534`, `feat: 슬라이더 드래그·출발 시계·키 조작 창 효과음을 연결 (프롬프트 B-179)`)이 없으면 **멈추고 보고한다.**

## 절대 규칙
1. SfxId 번호·칸 값·버스 음량·음원 파일은 바꾸지 않는다. 새 칸도 만들지 않는다. 경고(Alert) 버스는 쓰지 않는다.
2. App 코드는 `SfxPlayer.Play`, 창 TV 소리(79·80)는 `WindowTvSfx`. 멈춤 API는 추가하지 않는다.
3. "작게"는 B-181이 더한 재생 배율 `SfxPlayer.Play(id, pitch, volumeScale)`·`WindowTvSfx.Play(id, volumeScale)`를 쓴다. **기준에 이 겹이 없으면(B-181 미포함)** 79·80은 다른 창도 쓰므로 칸 음량은 건드리지 말고 **배율 없이 칸 음량 그대로** 낸다. 상수는 그대로 두고 "배율 미적용"이라고 보고서에 적는다.
4. **불러오기·이어하기, 초기화(Bind·씬 시작·`EnsureBuilt`·`EnsureReady`), 에디트 모드·`ManualTick` 미리보기, 조용히 치우는 경로(`HideImmediate`·`OnDisable`·씬 전환)에서는 소리를 내지 않는다.**
5. 씬·프리팹 YAML은 고치지 않는다. 연출 시간표, 보상·버리기·포기 규칙, 버튼 문(gate), B-164~B-181 소리 연결(특히 B-178 보상음·글리치 창)은 바꾸지 않는다.
6. 줄 번호는 fix124 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/Scripts/App/UI/Tutorial/` 기준)
- #158 E "화물 (B-152·B-153)" 6줄, `work_process/MVP2/sound/B-178-sfx-quest-drone-result.md`(보상음 B절), B-181 보고서(있으면)
- CargoFullPopupView.cs: `Show(bool page)` ~159, `Close(bool page)` ~215, `FoldInsteadOfPage` ~244, `HideImmediate` ~256, `TryAcceptChoice` ~267, `Tick` ~289, `OnDisable` ~324, `SettleOpen` ~344, `ApplyOpen` ~396(넘침 반짝 `pose.OverflowFlash`)
- CargoFullPopupTimeline.cs: `gaugeStart 0.3`·`gaugeDuration 0.24` → `GaugeEnd` ~36(넘침 칸이 밝아지는 순간), `closeDuration 0.2`
- CargoDiscardPopupView.cs: `Show(bool page)` ~228, `Close(CloseMode)` ~273(`Complete` = 공간 확보 완료 표시 → `completeHold 0.4` 뒤 접힘 ~418-428), `FoldInsteadOfPage` ~327, `TryAcceptDump` ~351, `TryAcceptBack` ~374, 버튼 생성 ~899(`BackName`)
- DemoObjectiveView.cs: `OnCapacityDumpClicked` ~1078, `OnCapacityForfeitClicked` ~1102, `OnDumpClosedClicked` ~1120(돌아가기·X키), `RequestDump` ~1161(`dumpRequestInFlight`), `SetDumpPanelVisible` ~630, `TryCloseTopPopup` ~856
- DemoObjectivePresenter.cs: `ChooseDumpInventory` ~158, `ChooseForfeitReward` ~173, `DumpMineral` ~224(`CanFitPending` → `RetryPending` → `CloseOverflow`), `CloseDumpPanel` ~257, `ShowCapacity` ~394, `OnCapacityBlocked` ~312, B-178 보상음 `OnGrantResolved` ~332
- CargoFullChoiceButton.cs(버튼 호버 연출만, 소리 없음), App/Audio/UiSfx.cs, App/Audio/WindowTvSfx.cs
- 검사 양식: Tests/EditMode/App/Audio/PromptB178SfxTests.cs, B-181 검사(있으면)

## 붙일 곳
| # | 플레이어가 보는 것 | 위치 | 소리 |
|---|---|---|---|
| 1 | 화물 공간 부족 창이 켜짐(접힌 상태에서 새로 열림, `page`=false) | CargoFullPopupView `Show` ~159, 실제로 Opening이 시작될 때 | `CargoOpenSfx = WindowTvOpen(79)`★, `CargoTvVolume = 0.6f`★ |
| 2 | 게이지가 찬 뒤 넘치는 칸이 한 번 밝아짐 | 같은 창 열기 연출 `GaugeEnd`를 "이번 프레임에 넘었는지"(`SfxPlayback.Crossed`)로. `SettleOpen`으로 건너뛰어도 한 번 | `CargoOverflowSfx = CargoHeavyWarning(65)`★ |
| 3 | "버릴 자원 선택"을 눌러 버리기 창으로 페이지가 넘어감 | DemoObjectiveView `OnCapacityDumpClicked` ~1078, `AcceptCapacityChoice`가 통과한 뒤 | `CargoPageSfx = KeysPageTurn(49)`★ |
| 4 | "보상 버리기"로 보상을 포기 | `OnCapacityForfeitClicked` ~1102, 통과한 뒤 | `CargoForfeitSfx = UiCancelFail(5)`★ |
| 5 | 1개·5개·10개·필요만큼 버리기 | `RequestDump` ~1161, `TryAcceptDump`가 통과하고 실제로 버렸을 때 | `DiscardTickSfx = UiScanTick(8)`★ |
| 6 | "전부" 버리기 | 같은 곳, `CargoDiscardKind.All` | `DiscardAllSfx = StorageTakeAll(20)`★ (보관함 "전체 보관"의 묵직한 소리. `BlockBreak(62)`는 채굴 파편이라 덜 어울림. 둘 다 들어 보고 고르게 보고서에 적기) |
| 7 | 공간이 다 비워져 완료 표시 뒤 창이 접힘 | CargoDiscardPopupView `Tick` ~426(`Complete` → `Fold`로 바뀌는 순간) | `CargoCloseSfx = WindowTvClose(80)`★, 같은 배율 |
| 8 | 버리기 창에서 "돌아가기"·X키로 부족 창으로 돌아감 | `OnDumpClosedClicked` ~1120, `TryAcceptBack`이 통과한 뒤 | `DiscardBackSfx = UiClick(1)`★ (페이지만 바뀌고 창은 안 닫히므로 닫힘음 대신 클릭) |

### 겹침·생략 규칙
- 1번 창 열림은 `page`=true(버리기 창에서 돌아올 때)면 무음. 그때는 8번 클릭이 맡는다. 버리기 창이 페이지 없이 새로 열리는 경로(`ShowDumpPanel(false)`)가 있으면 그 경우에만 같은 79를 작게 낸다.
- 2번은 페이지로 돌아올 때(`gauge.Apply(1f, 0f)`, 반짝 없음) 무음. 한 번 열기에 한 번만.
- 3번 페이지 넘김 뒤 버리기 창이 열리지 않고 접히면(`FoldInsteadOfPage`) 그때 80을 작게 한 번.
- 4번 포기 뒤 부족 창이 접힐 때는 80을 내지 않는다(실패음과 같은 프레임이라 뭉개짐). 포기 뒤 바로 다음 보상의 부족 창이 다시 뜨면(`ShowCapacity` 재호출) 1번 규칙대로.
- 5·6번: 같은 이유로 연타해도 쌓이지 않게 상수 `DiscardTickGap = 0.08f`★(`SfxThrottle`). 문이 닫혀 무시된 클릭, 버릴 게 없는 칸(수량 0)은 무음.
- **공간을 다 비운 마지막 클릭**(`RequestDump` 처리 뒤 창이 `Closing` + `ActiveCloseMode == Complete`)에서는 5·6번을 내지 않는다. 같은 프레임에 B-178의 보상음(QuestReward 87, 1.1초)이 이미 난다. 상수 `SkipDiscardSfxOnComplete = true`★.
- 7번 접힘음은 완료 표시 0.4초 뒤라 보상음 꼬리와 작게 겹친다. 이어서 다음 퀘스트 완료 창이 뜨면 B-178 글리치 열림음(88)과 겹치는지 확인만 하고, 너무 뭉개지면 고치지 말고 보고한다.
- 버리기 창이 `Fold`로 닫히는 다른 경로(돌아갔는데 부족 창이 이어지지 않음 등)는 80을 작게 한 번. `Page`로 닫힐 때는 무음.
- 불러오기·이어하기 직후 보류 보상 때문에 부족 창이 다시 뜨는 경로(`OnCapacityBlocked`·Bind)가 있는지 확인한다. 있으면 그 표시는 1·2번 무음(상수 `CargoStartupQuiet = 1.0f`★ 또는 경로 구분). 어떻게 했는지 적는다.

### 호버
- 부족 창 "버릴 자원 선택"·"보상 버리기", 버리기 창의 수량 버튼 15개(구리·철·리튬 × 1·5·10·필요·전부)와 "돌아가기"에 `UiSfx.Attach(button, SfxId.None, hover: true, failWhenDisabled: false)`. 클릭 소리는 위 코드가 낸다(두 번 금지). 열기 연출 중 잠긴 버튼은 지금처럼 `IsInteractable`이 false라 호버도 무음.
- 버튼이 실행 중에 만들어지므로(`EnsureBuilt`·`HookMissingDumpListeners` ~701) 붙이는 곳은 버튼이 생긴 직후 한 번.

## 손대지 말 것
- SfxId·SfxLibrary 칸, 음원, 창 연출 시간표, 보상·버리기·포기 규칙, 버튼 문(gate), B-178 보상음·글리치 창·연달아 열기 지연, B-179~B-181 대상, `AGENTS.md`·규칙 문서

## 테스트
- EditMode 전체에서 기준 브랜치의 기존 실패 말고 새 실패가 없어야 한다. 기대값을 바꾼 검사는 이름과 이유를 적는다.
- 새 EditMode `PromptB182SfxCargoTests`, PlayMode가 필요하면 `PromptB182SfxCargoPlayModeTests`(소리 요청을 잡는 방식은 B-178 검사를 따름, `ManualTick`으로 시간 진행)
  - 1·2: 새로 열기 79 한 번(배율 0.6, 겹이 없으면 1), `GaugeEnd` 넘을 때 65 한 번, 큰 dt로 건너뛰어도 1번, `page`=true 열기 0번, 에디트 모드·`HideImmediate` 0번
  - 3·4: 선택 통과 뒤 49 한 번 / 5 한 번, 두 번째 클릭(문 잠김) 0번, 포기 뒤 80 0번
  - 5·6: 1·5·10·필요 각 8 한 번, 전부 20 한 번, 0.08초 안 두 번째 0번, 수량 0 0번, 마지막 클릭(Complete) 8·20 0번 + 87 한 번
  - 7·8: Complete → Fold 넘어갈 때 80 한 번, 돌아가기·X 1 한 번, `Page` 닫힘 0번
  - 호버: 버튼마다 `UiSfxButton`(클릭 None·호버 true·failWhenDisabled false)
- PlayMode는 고친 검사만. 실행 전후 `git status`로 범위 밖 프리팹·폰트·씬·PNG·TimeManager 변경을 확인하고 되돌린다.

## 사용자 테스트 가이드 (보고서에 그대로 넣기)
화물을 꽉 채운 채로 퀘스트를 깨면 된다. 디버그 터미널(Ctrl + `)로 화물을 채울 수 있으면 그 방법도 보고서에 적어 준다.
1. 화물이 꽉 찬 채로 퀘스트 완료 창을 닫는다 → "화물 공간 부족" 창이 작은 TV 켜짐 소리와 함께 뜨고, 게이지가 넘치는 칸이 밝아질 때 경고음이 한 번 난다.
2. 버튼에 마우스를 올린다 → 호버음. 창이 열리는 동안에는 나지 않는다.
3. "버릴 자원 선택"을 누른다 → 페이지 넘기는 소리와 함께 버리기 창으로 바뀐다.
4. 1개·5개·10개를 누른다 → 누를 때마다 "틱". 빠르게 연타해도 소리가 쌓이지 않는다.
5. "전부"를 누른다 → 더 묵직한 소리가 난다. 버릴 게 없는 칸을 누르면 무음.
6. "돌아가기"나 X키 → 클릭음과 함께 부족 창으로 돌아온다. 이때 켜짐음·경고음은 다시 나지 않는다.
7. 다시 들어가 공간이 딱 맞게 버린다 → "틱" 대신 보상 "띠-링"이 나고, "공간 확보 완료"가 잠깐 보인 뒤 작은 꺼짐 소리와 함께 창이 접힌다.
8. 다음 퀘스트 완료 창이 이어서 뜨면 소리가 한꺼번에 뭉개지지 않는지 들어 본다.
9. 다른 보상으로 1번을 다시 만든 뒤 "보상 버리기"를 누른다 → 실패음만 나고 창이 접힌다(보상 소리 없음).
10. 보상 대기 중에 저장하고 이어하기로 들어온다 → 창이 다시 떠도 켜짐·경고음은 나지 않는다(이 상황이 안 생기면 건너뛰어도 됨).
11. 소리 크기가 다른 창보다 너무 크거나 작지 않은지 본다.
12. 효과음 음량·음소거가 이번 소리에 다 적용되고, Unity Console에 새 빨간 오류·노란 경고가 없다. 바꿔 보고 싶은 소리(★, 특히 "전부" 소리)가 있으면 PM에게 알려 준다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 화물 공간 부족·버리기 창 효과음을 연결 (프롬프트 B-182)`). 새 브랜치에 일반 push, 브랜치는 지우지 않는다. PR은 만들지 않는다.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue158-주제.md` 파일 하나로 기록. **`init/rules/lessons.md`에 줄을 추가하지 않는다.**
- **GitHub 이슈 #158은 고치지 않는다.** 체크는 PM이 확인 뒤 한다. 보고서의 대응 표에만 적는다.
- 결과 보고서 `work_process/MVP2/sound/B-182-sfx-cargo-result.md`
  - 기준 브랜치·커밋(B-179-1·B-180·B-180-1·B-181 포함 여부, 만든 번호와 이유), 배율 적용 여부, 소리마다 붙인 곳(파일·줄)과 SfxId·배율, ★ 상수 위치
  - 마지막 클릭·보상음·접힘음·다음 완료 창 순서를 실제로 어떻게 확인했는지, 불러오기 경로 처리, "전부" 소리 선택 이유
  - 테스트 결과, 직접 들어 보지 못한 항목은 "미검증", 위 사용자 테스트 가이드
  - **#158 대응 표**(보고서 안에만): 부족 창 켜짐·넘침 경고 / 페이지 넘김 / 보상 포기 / 버리기 틱·전부 / 공간 확보 뒤 닫힘·보상 / 돌아가기·X 각각 구현·일부·못 함
