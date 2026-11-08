# B-189 결과: 드론 말풍선 등장 펼침 + 대사 변경 타이핑 (#284)

## 기준
- 작업 브랜치: `MVP2-fix135` (원격에 이미 있었으나 PM이 B-189 문서 커밋 `d44f2d84`로 만든 브랜치라 그대로 사용. 새 번호 안 씀)
- 기준 커밋: `d44f2d84` (docs: prompt 189번 작성) → 부모 `8a7241aa`(origin/main)
- main 포함 확인: #300(`3d132945`) 포함, #276(`8a7241aa`) 포함
- 커밋 해시: `b78c2700` (feat: 코드·테스트). 이 보고서는 뒤따르는 docs 커밋.
- push: `MVP2-fix135` 일반 push
- PR: 원래 문서(11-9)는 PM이 PR을 연다고 했으나, PM이 대화에서 PR 생성을 직접 요청해 에이전트가 `main` 대상으로 열었다. 병합은 PM이 한다.

## 바꾼 파일
| 파일 | 내용 |
|---|---|
| `sub-terra/Assets/_Project/Scripts/App/UI/Drone/DroneBubbleMotion.cs` (새 파일) | 연출 시간표 순수 계산(★ 상수, 폭·상승·높이 보간·줄 단위 타이핑) |
| `sub-terra/Assets/_Project/Scripts/App/UI/Drone/DroneDialogueSocket.cs` | 연출 상태·적용(+302/−4) |
| `sub-terra/Assets/_Project/Tests/EditMode/App/UI/PromptB189DroneBubbleMotionTests.cs` (새 파일) | EditMode 14개(TV 켜짐 3개 추가) |
| `init/rules/lessons/2026-11-09-issue284-stale-dll-test-pass.md` (새 파일) | 컴파일 오류로 옛 DLL에서 테스트가 통과한 실수 기록 |

`DroneDialogueSocket.cs` 주요 위치(작업본 기준):
- ~60 B-189 필드(`TextInset*`, 패널 크기·등장·타이핑 상태)
- ~111 `LateUpdate`에서 `AdvancePresentation(Time.unscaledDeltaTime)` 호출(숨김 판정 뒤, `RefreshPosition` 앞)
- ~150–167 `SetDialogue`: 새 대사 높이 측정, 떠 있으면 `BeginRetype()`, 숨어 있으면 `SetCanvasVisible(true)`가 등장 시작
- ~174 `AdvancePresentation` / ~237 `BeginAppear` / ~261 `BeginRetype` / ~290 `BeginTyping`
- ~357 `MeasurePanelHeight` / ~369 `ConfigurePanelMotion` (`ApplyTerminalTheme` ~612에서 호출)
- ~764 `SetCanvasVisible`: 숨김→보임 전환일 때만 `BeginAppear`, 숨길 때 연출 정지
- `CreateTerminalFrame`의 `TerminalFooter`를 필드로 보관(펼치는 동안 숨기려고)
- `StyleDialogueText` 여백 숫자를 같은 값의 상수로 바꿈(16/23/36, 값 변경 없음)

## ★ 상수 (`DroneBubbleMotion.cs`)
| 상수 | 값 | 줄 |
|---|---|---|
| `AppearDuration` | 0.22f | 17 |
| `AppearRisePixels` | 8f | 18 |
| `AppearOvershoot` | 1.05f | 19 |
| `ResizeDuration` | 0.16f | 20 |
| `LineDuration` | 0.08f (TV 가로선이 뻗는 시간, 이후 0.22초까지 위아래로 열림) | 24 |
| `LineThickness` | 3f | 26 |
| `FlashPeak` / `FlashFade` | 0.85f / 0.12f | 28–29 |
| `FlickerAt` / `FlickerWidth` / `FlickerPeak` | 0.26f / 0.035f / 0.2f | 31–33 |
| `ScanDuration` / `ScanAlpha` / `ScanThickness` | 0.24f / 0.28f / 4f | 35–37 |
| `TvEffectEnd` | `AppearDuration + ScanDuration` (0.46초) | 40 |
| `TypingLineDuration` | `CoreCctvTimeline.TerminalLineTyping` (0.10f) | 119 |
| `TypingLineInterval` | `CoreCctvTimeline.TerminalLineInterval` (0.12f) | 121 |
| `MaxStep` | 0.05f (CCTV `CoreCctvPopupView.MaxStep`과 같은 값) | 124 |

(1차의 `AppearOvershootAt`은 TV 켜짐 추가로 `LineDuration`으로 바뀌어 지웠다.)

## 추가: TV 켜짐 연출 (PM 피드백 "켜지는 연출이 허전함" 반영)
- 순서는 전진기지 코어 CCTV 팝업 켜짐(`CoreCctvTimeline.Window`: 선 → 프레임 펼침 → 섬광)과 같고, 시간은 말풍선 범위(0.22초)에 맞춰 짧다.
  1. 0~0.08초: 두께 3의 밝은 강조색 가로선이 가운데에서 좌우로 뻗는다(폭 0 → 1.05).
  2. 0.08~0.22초: 그 선이 **위아래로 열려** 화면이 된다(세로 스케일, 다 열린 높이의 가운데 기준). 폭은 1.05 → 1. 동시에 8 아래에서 올라온다.
  3. 섬광(`PowerFlash`, 패널 전체 덮는 강조색)이 열리면서 0.85 → 0으로 빠져 어두운 화면이 드러난다.
  4. 0.26초 무렵 한 번 약하게 깜빡임(0.2).
  5. 0.22~0.46초: 주사선(`PowerScanline`, 두께 4, 최대 0.28)이 위에서 아래로 한 번 훑는다. 이 동안 타이핑은 이미 시작한다.
- 강조색은 현재 상태 색을 따른다(평소 청록, 우선 경고 시 주황).
- 폭은 크기(sizeDelta)로, 세로 열림만 스케일로 건다. 세로 스케일이라 선 단계에서는 머리줄·푸터·꼬리까지 화면 전체가 선으로 눌리고, 열리면서 제자리로 돌아온다. 꼬리 끝 위치는 다 열린 뒤 −72로 1차와 같다.
- `PowerFlash`/`PowerScanline`은 `CreateTerminalFrame`에서 런타임에 만드는 Image(레이캐스트 꺼짐, 맨 위 순서)이며, 연출이 끝나거나 숨기거나 모션 감소면 `enabled = false`로 꺼 둔다. 프리팹 YAML 수정 없음.
- 대사 변경(떠 있는 중)에는 TV 켜짐을 다시 하지 않는다. 효과음 추가 없음.

## 연출을 건 곳
- `visualRoot`(=`WorldDialogueCanvas`, 매 프레임 위치·화면 스케일을 덮어씀)는 **건드리지 않는다**.
- 안쪽 패널 `FindTerminalPanel()` = `WorldDialogueCanvas/VisualRoot`에만 건다. 런타임에 이 패널을 아래 변 기준(anchor (0,0)–(1,0), pivot (0.5,0))으로 바꾸고 `sizeDelta.x`(폭)·`sizeDelta.y`(높이)·`anchoredPosition.y`(상승)만 바꾼다. 프리팹 YAML은 수정 안 함.
- 폭은 스케일이 아니라 크기로 펼쳐서 외곽선·꼬리가 찌그러지지 않는다. 펼치는 동안 대사·제목·상태·푸터 글자는 `maxVisibleCharacters = 0`으로 숨기고, 폭이 100%가 된 뒤 보인다.
- 꼬리 `SignalPointer`는 패널 아래 변에 붙어 있어 펼치는 동안 가운데(드론 위)에서 시작해 원래 자리(x 0.2)로 이동한다. 높이가 커져도 아래 변이 고정이라 꼬리 위치는 그대로다.
- 패널 구조가 없는 경우(테스트 고정물처럼 `VisualRoot` 자식이 없어 패널 = `visualRoot`)는 크기 연출을 하지 않고 타이핑만 한다.

## 전진기지 타이핑 재사용
- 파일: `Scripts/App/UI/Outpost/CoreCctvTimeline.cs` (코어 CCTV 팝업이 켜질 때 터미널 줄 `> 코어 단말 접속` 등을 찍는 연출, 적용은 `CoreCctvPopupView.ApplyTerminal`).
- 방식: 줄마다 `TerminalLineInterval`(0.12초) 간격으로 시작해 `TerminalLineTyping`(0.10초) 동안 선형으로 `Mathf.RoundToInt(길이 × 진행)` 글자를 보인다. 말풍선은 TMP 줄바꿈 결과(`textInfo.lineInfo[i].characterCount`)를 줄로 보고 같은 식을 쓴다(`DroneBubbleMotion.VisibleCharacters`).
- 메뉴 인트로·건설 레일 홀로그램·Tab 분석 창 타이핑은 쓰지 않았다.

## 동작 정리
- A 첫 등장: 숨김→보임일 때만. 사라졌다 다시 뜨면(`SetVisible(false→true)` 포함) 다시 펼친다.
- B 대사 변경(떠 있는 중): 펼침 없음. 글자를 지우고 다시 찍고, 높이는 `ResizeDuration` 동안 smoothstep 보간. 타이핑 중 또 바뀌면 처음부터 새로 찍는다. 펼치는 중에 바뀌면 펼침을 이어 가고 끝난 뒤 새 대사를 찍는다. `sameText`면 아무것도 다시 하지 않는다.
- 높이: 새 대사의 선호 높이 + 위아래 여백(59)과 기존 높이(132) 중 큰 값. 짧은 대사는 지금과 같은 크기이고, 3줄 이상으로 넘칠 때만 커진다(폭은 고정 456).
- C 사라짐: 지금처럼 즉시 숨김.
- 표시 시간: 기본값(4초/6초)은 그대로. 등장 시작·타이핑 시작 때 `visibleUntil`을 "끝나는 시점 이상"으로만 늘린다.
- 모션 감소(`AccessibilityPreferences.ReduceMotion`): 펼침·타이핑·보간 없이 바로 전체 표시.
- 효과음: `TryPlayBubbleSfx` 호출 위치·조건 그대로(대사 수신 = 펼침 시작과 같은 프레임). 글자 틱 없음. SfxId·SfxLibrary 변경 없음.
- 클릭 통과(`interactable`/`blocksRaycasts` false), `OverlaySortingOrder`, Tab 분석 창(`DroneDialoguePanelView.cs`) 변경 없음.

## 테스트 결과
- **TV 켜짐 추가 후(2차)**
  - Unity 컴파일: 오류 0, B-189 파일에서 나오는 경고 0 (DLL이 원본보다 새것인지 확인).
  - B-189 + B-181 + B-183 + B-34 묶음: **62 통과 / 0 실패** (B-189 14개 포함).
  - Editor 수치(60fps): 0~0.067초 높이 3 고정·폭 0 → 476.6, 0.10~0.20초 높이 50.8 → 131.6(가운데 고정), 섬광 0.85 → 0(0.20초에 꺼짐), 0.267초 깜빡임 0.18, 주사선 0.233~0.433초 위→아래, 0.467초에 모두 꺼짐. 글자는 0.267초부터 찍힘. 대사 변경 시 세로 스케일 1·섬광 꺼짐 유지.
  - 전체 EditMode는 2차 변경 뒤 다시 돌리지 않았다(아래 1차 결과만 있음).
- **1차(펼침·타이핑)**
- Unity 컴파일: 오류 없음 (실행 중인 Editor에서 새로고침 후 타입 로드 확인).
- `PromptB189DroneBubbleMotionTests` 단독: **11 통과 / 0 실패**.
- 관련 묶음(B-189, B-181 SFX, B-183 SFX, B-34 Digger-Bot UI): **59 통과 / 0 실패**.
- 전체 EditMode: **2478 통과 / 24 실패**. 24개 모두 기존 기준 실패 목록(`work_process/MVP2/B-172-verification/editmode-full.txt`, `sound/B-183-verification/editmode-full.txt`, `sound/B-184-verification/editmode-failure-comparison.txt`)에 있다. **새 실패 0개.** 실패 스택에 드론 코드 없음.
- Editor 수치 확인(프리팹 인스턴스, 60fps 단계): 폭 0 → 최대 477.7(≈1.05배) → 456, 상승 −8 → 0, 펼침이 끝날 때까지 글자 0, 패널 아래 변 −66 고정, 3줄 대사에서 높이 132 → 145.9로 부드럽게 증가.
- 전체 EditMode 실행 뒤 범위 밖 파일이 dirty됨(테스트 빌더가 저장): `Fonts/NotoSansKR-Regular_SDF.asset`, `Prefabs/UI/InventoryPanel.prefab`, `MainMenuPanel.prefab`, `ViewSocket.prefab`, `Scenes/App/Mine_Demo_Integration.unity`, `ProjectSettings/TimeManager.asset`, `work_process/MVP2/103-main-menu/*.png`. **이번 변경이 아니므로 커밋에서 뺐다.** 작업 폴더에는 아직 남아 있어 `git restore`로 되돌려야 한다(에이전트 권한으로 막힘).
- PlayMode: 안 돌림.

## 미검증
- 실제 플레이(Bootstrap → 광산) 화면 확인 전부: 펼침 느낌, 글자 찌그러짐 없음, 꼬리, 비프 한 번, 드론 이동·확대 시 위치/크기, 클릭 통과, 모션 감소, Console 경고. 수치와 EditMode만 확인했다.
- 화면 맨 위 근처에서 긴 대사로 커진 말풍선이 화면 밖으로 나가는지(위로 커지므로 가능성 있음).
- `AppearRisePixels`는 말풍선 로컬 단위다. 실제 화면 픽셀은 확대 배율(`ApplyScreenScale`)에 따라 달라진다.

## 사용자 테스트 가이드 (직접 플레이)
1. 광산에서 드론 말풍선이 새로 뜨면, 밝은 가로선이 좌우로 뻗은 뒤 위아래로 열리며(옛 TV 켜짐) 아래에서 올라오는지 본다. 섬광이 빠진 뒤 한 번 깜빡이고 주사선이 한 번 훑는지, 너무 요란하지 않은지 본다.
2. 펼쳐지는 동안 글자가 찌그러져 보이지 않는지 본다. 폭이 다 펴진 뒤에 글자가 한 글자씩 찍히는지 본다.
3. 말풍선 꼬리가 드론을 가리키는지 본다.
4. 말풍선이 떠 있는 동안 대사가 바뀌면, 펼침이 다시 나오지 않고 기존 글자가 지워진 뒤 새 글자가 한 글자씩 찍히는지 본다. 길이가 달라지면 크기가 부드럽게 바뀌는지 본다.
5. 말풍선이 새로 뜰 때 기존처럼 높은 "삑"이 한 번 나는지 듣는다. 글자가 찍힐 때마다 소리가 나면 안 된다.
6. 말풍선이 사라진 뒤 다시 뜨면 펼침 연출이 다시 나오는지 본다.
7. 드론이 움직이거나 화면을 확대·축소해도 말풍선 위치와 크기가 이상한지 본다.
8. 말풍선 위를 클릭했을 때 클릭이 아래로 통과하는지 본다(말풍선이 채굴·설치를 막지 않아야 한다).
9. 설정에서 모션 감소를 켜면 펼침·타이핑 없이 바로 보이는지 본다.
10. Unity Console에 새 빨간 오류·노란 경고가 없는지 본다.

## 완료 체크리스트
- [x] `MVP2-fix135` push 확인
- [x] 커밋 해시 기재 (`b78c2700`)
- [x] Unity 컴파일 오류 없음 (빌드는 안 돌림)
- [x] EditMode 새 실패 없음
- [x] 첫 등장만 펼침 (EditMode)
- [x] 대사 변경은 타이핑 (EditMode)
- [x] 퍼지는 동안 글자 숨김 (EditMode·수치) — 화면 확인은 미검증
- [x] 꼬리가 아래 변에 고정, 펼침 시작은 드론 위 가운데 (수치) — 화면 확인은 미검증
- [x] 기존 말풍선 SFX 유지·새 SFX 없음 (B-181/B-183 테스트 통과)
- [x] Tab 창 미변경
- [x] 클릭 통과 유지
- [x] main 직접 커밋 없음
- [ ] PR 안 만듦 — PM 요청으로 PR 생성(위 기준 참고)
