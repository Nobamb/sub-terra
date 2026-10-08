# Prompt-B 137-1 — 광산 초기화 타이머 프레임 보완 + TV 전원 등장·퇴장 (추가 구현 계획)

작성: Planner / Technical Architect (Claude). 구현·검증: Codex. 최종 리뷰: Claude.
요구사항 원문은 `init/prompt-B.md` 137-1번(작업 트리에 이미 추가돼 있으며 **수정 금지**). 이 문서는 조사 결과와 구현 판단만 담는다.
기준은 B-137(커밋 `49e37313`)의 현재 결과다. 타이머 전체를 다시 만들지 않고 아래 범위만 바꾼다.

## 0. 조사 기준
- 브랜치 `MVP2-fix63`, HEAD `49e37313`. 작업 트리 변경은 `init/prompt-B.md`뿐.
- 적용 규칙: `CLAUDE.md`, `init/rules/ui-ugui.md`(2-5 범위 보호), `code.md`, `testing-qa.md`, `reporting.md`, `git-commit.md`. (`init/rule.md`는 `init/rules/`로 분할된 원본이며 충돌 시 `init/rules` 우선.)
- Unity Editor가 프로젝트를 열고 있어 batchmode 불가 → 플래그 파일 러너(`Temp/subterra-run-*.flag`)로 테스트.

## 1. 현재 구현 (실제 경로, 모두 `Assets/_Project/Scripts/App/`)
| 파일 | 역할 |
| --- | --- |
| `UI/HUD/MineResetClockOverlay.cs` | `Create()`가 Canvas(order 900, 1920×1080 Scale, pixelPerfect) 생성. `BuildClock()`(≈341행)이 `ClockRoot`(앵커/피벗 (0.5,1), y=-12, 280×78) + `MineResetClockView` + `Label`(TMP) 생성. `RefreshFromState()`(≈141행)이 **`clockRoot.SetActive(clockOn)`** 으로 표시를 제어하고, 켜져 있을 때만 라벨·`SetFormattedClock(FormatClock(...))`. `RefreshFromState`는 `SaveRuntimeController.TickMineResetCycle`(1회)와 `Overlay.Update`(1회)에서 **프레임당 2회** 불린다. |
| `UI/HUD/MineResetClockView.cs` | 계기판. `Build()`가 루트 `Image`(철판 0.16/0.22/0.27) + `Bezel` + `Display` + `DigitGlow` + `AccentTop/Bottom/Left/Right`(1px 선, 구간색) + `Bracket0~3` + `Digits`(세그먼트 42 + 콜론 4)를 **ClockRoot 직속 자식**으로 만든다. `Update`는 색 전환/맥동 중일 때만 `Advance(unscaledDeltaTime)`. **`OnEnable`/`OnDisable`이 `ResetObservation()`**(→ 재활성화 시 색 스냅·맥동 재계산). |
| `UI/HUD/MineResetClockStyle.cs` | 순수 C#. 구간(1800/300/60), 색 전환 1.5s, 마지막 1분 맥동(`PulseFactor` 0.45~1.0). **변경 금지**. |
| `UI/HUD/MineResetPopupArt.cs` | 절차 생성 스프라이트 캐시(`GetOrCreate`/`Build`). `Segment`, `Bracket`, `Soft`, `ClockRing`, `ClockHand`. |
| `Save/SaveRuntimeController.cs` | 표시 규칙: `EnsureMineResetClockOverlay`(≈852)·`TickMineResetCycle`(≈691)·`OnSceneLoaded`(≈1050~1070)가 `SetSessionVisible(Integration 씬 && 슬롯>0)`. `IsMineResetClockVisible`(T 키 `ToggleMineResetClock`, 저장됨). **이 파일은 수정 금지** — 표시 전환은 이미 모두 `Overlay.RefreshFromState()`로 모인다. |

**표시 전환이 일어나는 실제 지점(전부 `RefreshFromState`의 `clockOn` 값 변화로 귀결)**: ① Integration 씬 진입(`OnSceneLoaded`→`EnsureMineResetClockOverlay`+Refresh) ② 지상/메뉴 복귀(`SetSessionVisible(false)`) ③ T 키 토글 ④ 새 슬롯·불러오기(`RestoreBState`→Refresh) ⑤ 만료 팝업 진행 중 `CloseImmediately`/`SetSessionVisible(false)`.
**따라서 연출의 단일 연결점은 `RefreshFromState`의 `clockOn` 이다.** 시간값 갱신은 `SetFormattedClock`만 부르므로 등장 연출과 무관하다.

### 기존 테스트가 의존하는 것 (깨질 수 있음 → 갱신 대상)
- `Tests/EditMode/App/UI/MineResetClockViewTests.cs`: `clock.Find("Label")`, `Find("Digits")`, `Find("AccentTop")`, `Find("DigitGlow")` 경로, `Digits` 하위 Image 46개, `sizeDelta == (280,78)`, 앵커/피벗/Canvas 설정, 모든 Graphic `raycastTarget == false`.
- `Tests/PlayMode/Integration/Bootstrap/PromptB137MineResetClockPlayModeTests.cs`: `view.transform.Find("Digits/Digit0/Segment0")`, 토글/씬 이동 직후 **`view.gameObject.activeSelf == false`**(퇴장 연출 도입 후에는 성립하지 않음 → "퇴장 완료까지 진행한 뒤 false"로 바꾸되 의미는 유지), `AssertLayout`의 `ClockRoot` 사각형 겹침 검사.
- `Editor/DataValidation/PromptB137MineResetClockTestRunner.cs`: 스크린샷 러너(3해상도). 그대로 재사용.
- 계층 경로가 바뀌면 테스트의 경로만 갱신하고 **단언의 의미는 약화하지 않는다**(Digits Image 46, raycast false, 크기·앵커 불변).

## 2. 외부 프레임 — 재사용 판단
실제로 확인한 기존 프레임:
- **좌측 HUD**: `Art/UI/Gameplay/HUD/HUD-frame.png`(1635×930 **그려진 PNG, 9-slice 경계 없음**). `PromptB106HudBuilder`가 사용. 네 모서리가 비스듬히 잘리고, 모서리마다 회색 금속 브래킷(리벳 점), 얇은 청록 발광 이중선, 어두운 청남색 바탕.
- **광산 만료 팝업**: `Art/UI/SurfaceBase/MineReset/mine-reset-frame.png`(+`-frame-glow.png`, 9-slice 없음), 스킨 `Resources/UI/MineResetTimedPopupSkin.asset`.
- **판매/코어 CCTV**: 절차 생성 9-slice — `UI/Sell/ResourceSellArt`(`ChamferFill/ChamferOutline/SoftRect`, 좌상·우하만 깎임), `UI/Outpost/CoreCctvArt`(`Border/Glow`). 둘 다 `internal`이며 `SubTerra.App` 어셈블리 하나라서 접근 가능.

**결정**: 그려진 PNG(HUD-frame/mine-reset-frame)를 280×78로 늘리면 모서리 디테일이 뭉개지고, 원본에 9-slice 경계를 넣으면 다른 화면이 바뀐다(금지). 따라서 **원본 에셋은 건드리지 않고**, 그 시각 문법(네 모서리 잘림 + 회색 금속 코너 플레이트 + 얇은 청록 이중선 + 어두운 청남 바탕)을 **작은 계기판용 절차 생성 9-slice 프레임**으로 새로 만든다.
- 새 파일 `UI/HUD/MineResetClockFrameArt.cs`(internal static, `MineResetPopupArt`와 같은 `GetOrCreate` 캐시 패턴, `HideAndDontSave`): 흰색 스프라이트(런타임에서 `Image.color`로 색을 입힘).
  - `PlateFill()` — **네 모서리가 모두 대칭으로 잘린** 9-slice 면(chamfer ≈5~6px, border ≈8). 바탕 철판색용.
  - `Rim()` — 같은 외곽의 1px 윤곽 9-slice(금속 림). `Inner()` — 안쪽 2~3px 안으로 들어간 1px 윤곽(청록 발광선).
  - `CornerPlate()` — 모서리 금속 플레이트(작은 사다리꼴/L자 + 리벳 점 1개, ≈14×10px). 기존 `Bracket()` 대체.
  - 발광 번짐은 기존 `CoreCctvArt.Glow()`/`ResourceSellArt.SoftRect()` 재사용 가능(수정 금지).
- 층 구조(바깥→안): ① 외곽 번짐(구간색, 알파 ≤0.18, 루트 밖 최대 6px) ② 금속 림(고정 회청색 `~(0.30,0.38,0.43)`) ③ 어두운 철판(고정 `~(0.035,0.055,0.075)`) ④ 안쪽 청록 발광선(**구간색, 맥동 대상**) ⑤ 표시창(기존 `Display` 남색) ⑥ 네 모서리 금속 플레이트(고정 회청색).
- **색 규칙**: 철판·림·코너 플레이트는 **고정색**(경고색으로 물들이지 않음). 구간색은 ①④, 세그먼트, 콜론, `DigitGlow`에만. 기존 `Apply()`의 색 적용 지점을 유지하고 `accents`를 ①④로 대체.
- **크기 규칙**: `ClockRoot` 280×78, 앵커/피벗/위치(0,-12) **불변**. 숫자 영역(Digits 220×36, Digit 24×36)·간격 불변. 프레임은 안쪽으로 그려 숫자 폭을 줄이지 않는다. 외곽 번짐만 루트 밖으로 최대 6px. 라벨 위치/크기 불변.

## 3. TV 전원 등장·퇴장 — 설계
### 3-1. 계층 (마스크는 별도 컨테이너, 내용은 고정 크기)
```
ClockRoot            (280×78 불변, MineResetClockView, Image 없음 ← 루트 철판 Image는 Window/Content로 이동)
├─ Window            (RectTransform 중앙 앵커, sizeDelta 애니메이션 = 마스크 크기, RectMask2D)
│  └─ Content        (280×78 고정, 중앙 앵커, CanvasGroup — 알파만 변경)
│     ├─ [프레임 ①~④⑥, Display, DigitGlow, Digits, Label]   ← 기존 이름 유지
├─ PowerBeam         (마스크 밖. 중앙의 밝은 가로선: Image, 폭/높이/알파 애니메이션)
├─ PowerSpark        (마스크 밖. 중앙 빛점: Soft 스프라이트)
├─ EdgeGlowTop/Bottom(마스크 밖. Window 위/아래 가장자리를 따라다니는 짧은 발광)
```
- 숫자·글자는 `Content`가 고정 크기라 **절대 찌그러지지 않는다**. 보이는 부분만 `Window`(RectMask2D)의 sizeDelta가 정한다. 라벨 TMP는 현재 오버레이가 `clockRoot` 직속으로 만든다 → `Content`로 reparent(오버레이 `BuildClock` 수정, `Label` 이름·위치·크기 유지).
- 모든 새 Image는 `raycastTarget=false`, 투명 메시 컬링 영향 없음. 입력 차단 요소 추가 금지.

### 3-2. 단일 스칼라 `p` (0=꺼짐, 1=완전 켜짐)와 순수 시간표
- 새 순수 C# `UI/HUD/MineResetClockPowerTimeline.cs`(UnityEngine 비의존 또는 `Color` 외 비의존, EditMode 테스트 대상):
  - `OnDuration ≈ 0.40s`(요구 0.3~0.5), `OffDuration ≈ 0.28s`(요구 0.2~0.35). 진행: `On`이면 `p += dt/OnDuration`, `Off`면 `p -= dt/OffDuration`, [0,1] 클램프.
  - `Evaluate(p)` → `Pose { SparkAlpha, BeamWidth01, BeamHeightPx, BeamAlpha, WindowWidth01, WindowHeight01, ContentAlpha, EdgeGlow, SettleFlash }`. 권장 구간(조정 가능, 단 아래 순서는 지킬 것):
    - p 0~0.15: 중앙 빛점 알파 0→1(작은 점).
    - p 0.10~0.40: 빛이 **좌우로** 펼쳐져 얇은 가로선(높이 2~3px, 폭 0→전체), Window 폭이 같이 0→전체(높이는 가로선 두께).
    - p 0.35~0.85: Window 높이가 **가로선→전체로 위아래 확장**(EaseOutCubic), `EdgeGlow`가 확장 가장자리를 따라 상승·감쇠.
    - p 0.55~0.90: `ContentAlpha` 0→1(빠른 페이드; 퇴장 방향에서는 p가 빨리 내려가므로 "내용이 빠르게 어두워짐"이 된다).
    - p 0.80~1.0: `SettleFlash`(프레임 짧은 발광) 피크 후 정상 밝기(1.0)로 감쇠.
    - p=1: 정확히 정상 상태(Window=전체, ContentAlpha=1, 번짐/섬광/빔/빛점 0). p=0: 모두 0(빛점도 0).
  - **퇴장 = 같은 `p`를 더 빠르게 되감기**: 내용 어두워짐 → 창이 위아래에서 가운데 선으로 수축 → 선이 좌우에서 점으로 줄어듦 → 점 소멸. 별도 상태 없이 중단·역전이 항상 연속이다(등장 중 퇴장, 퇴장 중 재등장 모두 현재 `p`에서 방향만 바뀜).
- 가로선(빔)과 빛점은 밝은 청록-흰색(구간색을 60% 흰색 쪽으로 섞음). 금속 바탕은 물들이지 않는다.

### 3-3. `MineResetClockView` 확장 (기존 API 유지)
- 신규: `SetPowered(bool on, bool immediate=false)`, `PowerProgress`(p), `IsPowerAnimating`, `IsPoweredOn`(목표). `Advance(dt)`가 **전원 진행 + 색 전환 + 맥동을 한 곳에서** 진행(기존 `IsTransitioning/IsPulsing` 조건에 `IsPowerAnimating` 추가해 `Update`가 돌게 함). 프레임당 호출은 `Update` 1곳뿐.
- 충돌 방지 규칙: 색(`MineResetClockStyle`)과 맥동은 **`p`와 독립 계산**하고, 합성은 `Apply()`에서만 한다 — 세그먼트 알파 = (켜짐/꺼짐 알파) × `ContentAlpha`가 아니라 `Content` CanvasGroup이 곱한다. 맥동 `PulseFactor`는 ①④에만, 섬광 `SettleFlash`는 별도 Image(`FrameFlash`)에만 적용. 따라서 p 진행 중에도 색 전환·맥동 값은 그대로 흐르고 서로의 상태를 바꾸지 않는다.
- 퇴장 완료(`p==0`) 시: 맥동·전환 정지(`Update` 조건 false), `gameObject.SetActive(false)`(뷰 자신이 루트를 끈다). `OnDisable`의 `ResetObservation()`은 기존대로 → 다음 표시에서 `OnEnable`이 현재 값으로 스냅. **숨은 동안 `Update` 비용 0**.
- 재표시(`SetActive(true)` 후 `SetPowered(true)`): `OnEnable` → 관측 초기화 → 오버레이가 같은 `RefreshFromState`에서 `SetFormattedClock(현재값)`을 불러 현재 시간·구간색·맥동 여부를 즉시 반영한 뒤 p=0에서 등장 시작.
- `immediate=true`(또는 `Application.isPlaying==false`/EditMode 테스트용 `SnapPower`)는 p를 목표로 즉시 이동하고 연출 없이 끝 상태 적용.
- 정리: `OnDisable`/`OnDestroy`에서 p·Window 크기·Content 알파·빔/빛점/섬광을 **정상 정지 상태**로 되돌리고(`ApplyPower(p)`로 끝값 재적용), 전역 이벤트·코루틴·Tween 없음(이 구현은 `Update`+`unscaledDeltaTime`만 사용, `Mathf.Min(dt, 1/20)`로 프레임 급등 제한 — `MineResetClockIntro`와 동일 방식). DOTween/코루틴 금지.

### 3-4. `MineResetClockOverlay.RefreshFromState()` 수정 (표시 제어의 유일한 변경점)
현재: `clockRoot.SetActive(clockOn); if (!clockOn) return; ...`
변경 의미:
```
if (clockOn) {
    if (!clockRoot.activeSelf) clockRoot.SetActive(true);      // OnEnable → 관측 초기화
    라벨/시간 갱신(기존 그대로)                                    // 값 갱신은 연출을 트리거하지 않는다
    clockView.SetPowered(true);                                  // 이미 켜져 있거나 켜지는 중이면 아무 일도 안 함
} else if (clockRoot.activeSelf) {
    clockView.SetPowered(false);                                 // 이미 꺼지는 중이면 아무 일도 안 함. 값/라벨은 갱신하지 않아 마지막 값이 유지된다
}
```
- `SetPowered`는 **목표가 실제로 바뀔 때만** 방향을 바꾼다(멱등) → 프레임당 2회 호출·시간 갱신으로 연출이 재시작되지 않는다.
- 최초 생성: `Build → RefreshFromState`는 `sessionVisible=false`라 숨김 상태로 시작 → Integration 진입 시 첫 표시가 등장 연출이 된다(원하는 동작). 저장 불러오기(`RestoreBState`)로 같은 씬에서 이미 표시 중이면 `clockOn`이 계속 true이므로 연출 재생 없음(시간/색은 스냅 갱신).
- 만료 팝업 `ShowTimedResetPopup`/`CloseImmediately`/`SetSessionVisible(false)` 경로: 시계는 `clockOn=false`로 퇴장 연출을 탄다. 팝업·시계 인트로(`MineResetClockIntro*`)·`ExecuteTimedMineReset` 코드는 **수정·호출 금지**.
- 퇴장 중 `UiPauseGate.IsHeld`/`sessionVisible=false`로 `Overlay.Update`가 조기 반환해도 퇴장은 뷰의 `Update`가 스스로 완료한다(오버레이 Update에 의존하지 않음).
- `DestroyOverlay`/씬 파괴: 별도 정리 코드 불필요하되, `MineResetClockView.OnDestroy`에서 상태 정리 + 테스트에서 확인.

## 4. 범위
**수정/신규**
- `Scripts/App/UI/HUD/MineResetClockView.cs`(수정: 계층, 프레임, 전원)
- `Scripts/App/UI/HUD/MineResetClockOverlay.cs`(수정: `BuildClock` 라벨 reparent, `RefreshFromState`)
- `Scripts/App/UI/HUD/MineResetClockFrameArt.cs`(신규), `MineResetClockPowerTimeline.cs`(신규) (+ 자동 생성 `.meta`)
- `Tests/EditMode/App/UI/MineResetClockViewTests.cs`(경로 갱신 + 전원 테스트 추가), `MineResetClockPowerTimelineTests.cs`(신규)
- `Tests/PlayMode/Integration/Bootstrap/PromptB137MineResetClockPlayModeTests.cs`(퇴장 단언 갱신 + 연속 캡처), `Editor/DataValidation/PromptB137MineResetClockTestRunner.cs`(필요 시 시퀀스 캡처만)
- `work_process/MVP2/mine-reset-clock/`: 결과 `prompt-b137-1-result.md`, 증거 PNG/연속 프레임

**수정 금지**: `MineResetClockStyle.cs`·`SevenSegmentGlyph.cs`(동작 변경 금지), `MineResetPopupArt.cs`(필요 없음; 부득이하면 신규 팩토리 추가만), `ResourceSellArt/CoreCctvArt/EmergencyRescueArt`, `HUD-frame.png`·`mine-reset-frame*.png` 및 `.meta`(임포트 설정 포함), `MineResetService`·`MineResetCycleState`·`SaveRuntimeController`·Save DTO, `MineResetClockIntro*`·`MineResetPopupMotion`·`MineResetTimedPopupSkin*`, 모든 Scene·Prefab(특히 `Mine_Demo_Integration.unity`, `SurfaceBase.unity`, `MainMenu.unity`, `*Panel.prefab`, `BasicHUD.prefab`, `HUDCanvas.prefab`), `Fonts/*_SDF.asset`·TMP 설정, `ProjectSettings/*`, `Packages/*`, `init/prompt-B.md`, `Localization` 문구. 새 이미지/폰트/패키지 파일 금지.

## 5. 구현 순서
1. `MineResetClockPowerTimeline`(순수) + 단위 테스트(경계 p=0/0.1/0.4/0.85/1, 단조성, 퇴장 속도 배율, 클램프).
2. `MineResetClockFrameArt` 스프라이트 4종 + 프레임 층 교체(`Build`): 계층 `Window/Content` 도입, 기존 이름·크기·색 규칙 유지. 이 단계에서 **전원 연출 없이** 스크린샷으로 프레임·가독성부터 확인(1920×1080, 1280×720).
3. `SetPowered/Advance/ApplyPower`와 `OnDisable/OnDestroy` 정리, `PowerBeam/PowerSpark/EdgeGlow/FrameFlash` 구성.
4. `Overlay.BuildClock` 라벨 reparent + `RefreshFromState` 분기.
5. EditMode 테스트 갱신·추가 → 플래그 러너로 실행, 컴파일 오류 0.
6. PlayMode: 실제 Bootstrap 흐름에서 등장/퇴장/왕복/중단/저장·불러오기/0초 종료 확인, 연속 캡처(§6).
7. `git status`로 범위 밖 변경 확인 → 복구, 결과 문서 작성.

## 6. 검증 항목 (각 항목 통과/실패/미검증 + 근거)
- 컴파일 오류 0, EditMode 전체 + 신규, PlayMode 스모크.
- **프레임**: 1280×720/1920×1080/2560×1440에서 좌측 HUD·우측 사이드 메뉴와 겹침 0, 숫자 가독성(기존 대비 폭·높이·간격 동일), 금속 바탕이 구간색으로 물들지 않음(청록/노랑/빨강 각각 캡처), 마지막 1분 맥동 시 프레임·번짐만 변화.
- **등장**: 지하 진입 직후 연속 캡처(≥8프레임, ~0.4s)에서 ① 중앙 빛점 → ② 좌우로 가로선 → ③ 위아래 확장 → ④ 가장자리 발광 → ⑤ 정상. 총 0.3~0.5s. 방향은 정지 이미지가 아니라 **프레임 번호가 붙은 연속 캡처**(또는 `p`와 `Window.sizeDelta` 로그 시계열)로 증명.
- **퇴장**: 지상 복귀 시 내용 어두워짐 → 위아래 수축 → 가로선 → 점 → 소멸, 0.2~0.35s, 종료 후 `ClockRoot` 비활성·잔존 Image 0.
- **중복/중단**: 값 갱신(초 변화, 저장 불러오기 같은 씬)으로 등장 재생 0회. 등장 중 지상 복귀, 퇴장 중 재진입이 현재 `p`에서 연속으로 방향 전환하고 최종 상태 정확(켜짐=Window 전체·ContentAlpha 1·빔/섬광 0, 꺼짐=비활성). T 키 연타·씬 왕복 ×N 후 `MineResetClockView` 1개, `Update` 구독/Coroutine/Tween 잔존 0(퇴장 완료 후 `isActiveAndEnabled==false`).
- **상태 보존**: 구간 전환(1800/300/60 경계)·맥동·`00:00:00`·저장/불러오기·0초 종료 알림(중복 실행 없음)이 기존 B-137 결과와 동일. `MineResetClockStyle` 테스트 전부 그대로 통과.
- **입력**: 연출 중/후 모든 Graphic `raycastTarget=false`, 시계 영역 클릭이 뒤 UI/게임으로 통과, 일시정지(`UiPauseGate`·timeScale 0)에서도 연출이 끝까지 진행(unscaled).
- 범위: `git status`에 §4 목록 외 파일 0개(Play 후 자동 저장된 Scene/Prefab 있으면 `git restore -- <path>`).
