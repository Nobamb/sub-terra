# Prompt-B 137 — 인게임 광산 초기화 타이머 7세그먼트 계기판 (구현 계획)

작성: Planner / Technical Architect (Claude). 구현·검증: Codex. 최종 리뷰: Claude.
요구사항 원문은 `init/prompt-B.md` 137번. 이 문서는 실제 코드 조사 결과와 구현 판단만 담는다.

## 0. 조사 기준
- 브랜치 `main`, 기준 커밋 `ce112b35`(B-136). 작업 트리 변경은 `init/prompt-B.md`(137번 요구 추가)뿐이며 **건드리지 않는다**.
- 인게임 타이머의 마지막 변경 커밋은 `1eaa0b29`(B-126, 만료 알림 시계 연출). 타이머 계기판 자체는 그 이전부터 코드 생성 UI다.
- 사용자가 첨부했다는 "현재 타이머·디지털 시계 이미지"는 저장소에 파일로 없다. 아래 요구 수치(색·구성)가 기준이다. 시각 판단은 최종 리뷰에서 스크린샷으로 한다.
- Unity Editor(6000.5.4f1)가 이 프로젝트를 열고 있다 → batchmode 불가. 테스트는 플래그 파일 러너(`Temp/subterra-run-*.flag`)와 메뉴 러너로 돌린다.

## 1. 현재 데이터 흐름 (실제 경로)
```
GameState.MineResetCycle.ElapsedSeconds (MineResetCycleState)
  ├─ 누적: SaveRuntimeController.TickMineResetCycle()  [Save/SaveRuntimeController.cs:691]
  │        Integration 씬에서만 AddMineResetElapsed(Time.unscaledDeltaTime). UiPauseGate.IsHeld 동안은 누적 안 함.
  ├─ 남은 시간: SaveRuntimeController.MineResetRemainingSeconds → MineResetService.GetRemainingSeconds  [:103]
  ├─ 표시 문자열: MineResetService.FormatClock(remaining)  "HH:MM:SS", floor(+1e-7)  [MineResetService.cs:154]
  └─ UI: MineResetClockOverlay.RefreshFromState()  [UI/HUD/MineResetClockOverlay.cs:142]
         · Tick에서 1회 + Overlay.Update에서 1회 = 프레임당 2회 호출됨(애니메이션을 여기서 진행하면 2배속이 된다)
         · clockRoot.SetActive(sessionVisible && ActiveSlot>0 && IsMineResetClockVisible)
         · clockText.text = FormatClock(...)  ← 이것을 7세그먼트로 교체
```
- 표시 조건: `SaveRuntimeController.EnsureMineResetClockOverlay/TickMineResetCycle/OnSceneLoaded`가 `SetSessionVisible`을 호출. Integration 씬만 true, 지상(SurfaceBase)·메뉴는 false. T 키는 `ToggleMineResetClock`(저장되는 `ClockVisible`). **이 규칙은 그대로.**
- 오버레이는 Bootstrap 수명의 `SaveRuntimeController` 자식(`MineResetClockOverlay.Create`)이며 **프리팹·씬·빌더가 없다**. `ClockRoot`/`Label`/`Digits`를 `BuildClock()`이 코드로 만든다(앵커 상단 중앙, pivot (0.5,1), y=-12, 280×78, Canvas sortingOrder 900, 1920×1080 ScaleWithScreenSize 0.5). → **씬·프리팹 수정 불필요.**
- 저장/불러오기: `ElapsedSeconds`만 저장(v2). 불러오면 같은 `RefreshFromState`가 현재 값을 읽는다. 저장 스키마 변경 없음.
- 0초 종료: `TickMineResetCycle`이 `IsCycleExpired`면 `ExecuteTimedMineReset()` 코루틴(중복 가드 `pendingTimedMineReset`) → `TryTimedReset`(Elapsed=0) → Integration이면 SurfaceBase 이동 → 저장 → `ShowTimedResetPopup(wasInMine)`(`MineResetClockIntro` 시계 아이콘 → 안내 카드). **UI 계기판은 이 흐름의 소비자가 아니라 표시만 한다.** 새 이벤트·콜백·`StartCoroutine` 호출을 계기판에 넣지 않는다.
- 애니메이션 방식(재사용): `MineResetClockIntro`·`MineResetPopupMotion`은 MonoBehaviour `Update`에서 `Time.unscaledDeltaTime`(프레임 제한)으로 진행하는 단일 상태 + 순수 계산 클래스(`MineResetClockIntroTimeline`, EditMode 테스트 가능). 코루틴·DOTween 없음. 같은 방식으로 한다.
- 절차 생성 스프라이트: `MineResetPopupArt`(`GetOrCreate`/`Build`, `HideAndDontSave` 캐시). 새 이미지 파일 없이 같은 방식으로 세그먼트 스프라이트를 만든다.

## 2. 수정 대상 / 범위 밖
| 파일 | 변경 |
| --- | --- |
| `Scripts/App/UI/HUD/MineResetClockOverlay.cs` | `BuildClock()`을 계기판 구성으로 교체(`clockText` 제거, `MineResetClockView` 추가). `RefreshFromState()`는 라벨 설정 + `clockView.SetFormattedClock(MineResetService.FormatClock(...))`만. `ClockRoot` 이름·앵커·위치(0,-12)·pivot·Canvas 설정 유지. 팝업·시계 연출 코드는 손대지 않는다. |
| `Scripts/App/UI/HUD/MineResetClockView.cs` (신규) | 계기판 MonoBehaviour. 세그먼트 Image 보유, `SetFormattedClock(string)`, `Advance(float dt)`(Update에서 `Time.unscaledDeltaTime` 1회), `OnEnable`에서 기록 초기화, `OnDisable`에서 전환·맥동 정리. |
| `Scripts/App/UI/HUD/MineResetClockStyle.cs` (신규, 순수 C#) | 구간 판정·색 전환·맥동 계산. UnityEngine.Color 외 의존 없음. |
| `Scripts/App/UI/HUD/SevenSegmentGlyph.cs` (신규, 순수 C#) | 숫자 0–9 → 7비트 마스크. |
| `Scripts/App/UI/HUD/MineResetPopupArt.cs` | 세그먼트(가로/세로 육각 끝)·모서리 브래킷 스프라이트 팩토리 추가. 요약 주석의 "만료 알림 전용" 문구를 사실에 맞게 한 줄 수정. |
| `Scripts/Shared/Localization/LocalizationService.cs:161` | `mine_reset.clock.label` 값 "광산 초기화" → "광산 초기화까지"(영문 "Mine Reset In"). 이 키의 소비자는 오버레이뿐(검증됨). 오버레이의 기본값 인자도 동일하게. |
| `Tests/EditMode/App/UI/MineResetClockStyleTests.cs`, `MineResetClockViewTests.cs` (신규) | §6 참고. |
| `Editor/DataValidation/PromptB137MineResetClockTestRunner.cs` (신규, 선택) | `PromptB135EmergencyRescueTestRunner` 패턴(메뉴 + `Temp/subterra-run-prompt-b137.flag`). 플래그 파일로 EditMode/PlayMode 실행 가능하게. |
| `Tests/PlayMode/...PromptB137...` (신규, 선택) | 실제 Bootstrap 흐름 스모크 + 스크린샷. `UiPlayModeTestSupport`(`Tests/PlayMode/Integration/Bootstrap`) 재사용. |
| `work_process/MVP2/mine-reset-clock/` | 결과 보고 `prompt-b137-result.md`, 증거 스크린샷. |

**수정 금지(범위 밖)**: `MineResetService.cs`, `MineResetCycleState.cs`, `SaveRuntimeController.cs`, `GameState`/Save DTO/`SaveDataMapper`, `MineResetClockIntro*.cs`, `MineResetPopupMotion.cs`, `MineResetTimedPopupSkin*`, 모든 Scene·Prefab(특히 `Mine_Demo_Integration.unity`, `SurfaceBase.unity`, `MainMenu.unity`, `*Panel.prefab`), `Fonts/*_SDF.asset`·TMP 설정, `ProjectSettings/*`, `Packages/*`, `Art/UI/SurfaceBase/MineReset/*`(지상 기지 타이머 판), `init/prompt-B.md`. 폰트 아틀라스 재생성 금지.
적용 규칙: `CLAUDE.md`, `init/rules/ui-ugui.md`(2-5 범위 보호), `code.md`(UnityEngine.Object에 `?.` 금지, 로그 최소화, 순수 로직 분리), `testing-qa.md`, `reporting.md`(14 보고 형식), `git-commit.md`(사용자 동의 없이 커밋 금지).

## 3. 7세그먼트 구현 방식 (하나만 선택)
**세그먼트 Image 방식**: 숫자 6칸 × 7세그먼트 = Image 42개 + 콜론 점 4개를 `MineResetClockView`가 보유하고, 흰색 절차 생성 스프라이트에 `Image.color`로 색을 입힌다.
- 켜짐 = 구간 색 알파 1, 꺼짐 = 같은 색 알파 ≈0.08. 칸 위치·폭은 고정이라 숫자가 바뀌어도 레이아웃이 움직이지 않는다(콜론은 항상 표시).
- 선택 이유: TMP 폰트를 추가하면 `Fonts/*`·TMP 설정이 dirty 되어 범위 규칙 위반이다. 완성 시간 이미지·스프라이트 시트는 금지 요구에 걸린다. 세그먼트 Image는 기존 `MineResetPopupArt` 절차 생성 패턴과 같고, 색 전환이 `Image.color` 한 곳이며, 숫자 판정이 순수 마스크라 EditMode 테스트가 쉽다. 숫자 자체는 공급원이 같은 `FormatClock` 문자열.
- 숫자 소스: `FormatClock` 결과 "HH:MM:SS"를 파싱(`SetFormattedClock`). `MineResetService`를 수정하지 않고 반올림 규칙(floor)을 기존과 동일하게 유지하려는 선택이다. 파싱 실패 시 이전 표시 유지 + 로그 없음.
- 프레임당 갱신: 직전 `displaySeconds`와 같으면 세그먼트·문자열 작업을 건너뛴다(프레임당 할당 0).
- 계기판 구성(시작값, 구현 중 가독성에 따라 조정 가능): 어두운 남색 표시창 `(0.015,0.03,0.06,0.96)`, 얇은 철제 프레임(바깥 어두운 강철 + 안쪽 1~2px 구간색 발광선 `accent`), 네 모서리 금속 브래킷, 제목 TMP 소형(기존 라벨 위치), 숫자 높이 ≈36px, 숫자 뒤 아주 옅은 소프트 글로우(구간색, 알파 ≤0.15, 숫자 선명도를 해치지 않음). 크기는 ≤ 300×92 안에서(현 280×78), 앵커·위치 유지. 1280×720 / 1920×1080 / 2560×1440에서 다른 HUD와 겹침 없음을 스크린샷으로 확인.
- 정수 px 크기로 세그먼트를 배치(Canvas `pixelPerfect = true`)해 가장자리 번짐을 피한다.

## 4. 색상 상태 · 전환 · 맥동 (`MineResetClockStyle`)
기준은 **표시 중인 정수 초** `displaySeconds`(= 화면에 보이는 값, 반올림 일관성 보장).
| displaySeconds | 구간 | 색(시작값) | 맥동 |
| --- | --- | --- | --- |
| > 1800 | Normal | 청록 `(0.45,0.96,1)` | 없음 |
| 301 ~ 1800 | Warning | 노랑 `(1,0.78,0.2)` | 없음 |
| 61 ~ 300 | Critical | 빨강 `(1,0.24,0.2)` | 없음 |
| 0 ~ 60 | Final | 빨강 | 프레임 accent·바깥 글로우만 |
`00:30:00`은 노랑, `00:30:01`은 청록. `00:05:00` 빨강, `00:01:00` 맥동 시작, `00:00:00`도 Final.
- **전환(1.5초, `unscaledDeltaTime`, SmoothStep)**: 직전 관측값이 있고 `0 ≤ prev - current ≤ 3`(프레임 끊김 허용)이며 구간이 다를 때만 시작. 진행 중 다시 구간이 바뀌면 현재 표시색에서 새 목표로 재시작(누적·중첩 없음, 코루틴 없이 `from/to/t` 하나).
- **즉시 적용(snap)**: 최초 관측, `OnEnable` 이후 첫 값(재표시·씬 복귀·불러오기), 값이 **증가**(초기화·디버그 타이머), 3초 초과 점프(디버그 `timer`, 불러오기).
- **맥동**: Final 구간에서만, 주기 1.75초 sine, 프레임 accent·글로우 알파 0.45↔1.0. 숫자 알파·색은 맥동과 무관하게 고정(테스트로 고정). Final 진입 시 위상 0부터. Final을 벗어나거나 snap 시 맥동 계수 = 1로 즉시 복귀.
- **정리**: `OnDisable`(숨김·씬 전환·파괴·T 키)에서 전환·맥동·직전 관측값을 모두 초기화 → 재표시는 항상 현재 값으로 snap. 오버레이 파괴 시 MonoBehaviour와 함께 사라지므로 잔존 불가. `RefreshFromState`는 애니메이션 시간을 진행하지 않는다(진행은 `Update` 1곳).
- 일시정지(`UiPauseGate`) 중 시간은 정지하지만 전환·맥동은 장식이라 계속 진행해도 무방.

## 5. 0초 종료에서 UI의 책임
- 1초→0초: 계기판은 `00:00:00` 빨강(맥동 중)만 표시. 그 뒤 기존 `ExecuteTimedMineReset`이 Elapsed를 0으로 만들고 씬을 지상으로 이동 → 계기판은 값 증가로 snap(또는 이미 비표시) → 이전 전환·맥동 정리.
- 시계 아이콘·초기화 안내 연출은 `ShowTimedResetPopup`/`MineResetClockIntro`가 담당 — **재사용만, 호출 추가 금지**. 계기판 코드에 `ExecuteTimedMineReset`/`ShowTimedResetPopup`/`StartCoroutine` 참조가 생기면 리뷰 반려.

## 6. 구현 순서
1. 순수 로직: `SevenSegmentGlyph`(마스크 0–9), `MineResetClockStyle`(구간·전환·맥동) + EditMode 테스트(경계 표 아래).
2. `MineResetPopupArt`에 세그먼트·브래킷 스프라이트 추가.
3. `MineResetClockView` 작성(세그먼트·콜론·프레임 빌드, `SetFormattedClock`, `Advance`, `OnEnable/OnDisable`).
4. `MineResetClockOverlay.BuildClock/RefreshFromState` 교체, 라벨 문구 변경. `ClockRoot` 이름·위치 유지.
5. EditMode: 오버레이를 `MineResetClockOverlay.Create(host)`로 만들어 View를 직접 구동(`SetFormattedClock`+`Advance`)하는 테스트. 기존 `MineResetTimedPopupTests`·`MineResetClockIntroTests`가 그대로 통과해야 한다.
6. 검증 러너(선택) + PlayMode 스모크 + 스크린샷.
7. `git status`/`git diff --stat`로 범위 확인, 결과 문서 작성. **커밋하지 않는다.**

## 7. 완료 조건과 검증
**EditMode(순수/View, 시간 재현은 `displaySeconds` 주입 — 3시간 대기 없음)**
- 구간 경계: 1801/1800/1799, 301/300/299, 61/60/59, 1/0, 값 증가(초기화) → 색·맥동 플래그·snap/전환 여부.
- 전환: 1800→1799에서 1.5초에 걸쳐 변화(0.75초 중간색), 도중 재트리거 시 중첩 없음, 증가·점프·`OnDisable→OnEnable` 후 즉시 snap.
- 맥동: Final에서 주기 1.75초(±), 비-Final에서 계수 1, 숫자 알파 불변.
- 마스크: 0–9 세그먼트 수·형태, `00:00:00`/`03:00:00`/`02:59:59`/`00:09:09` 켜짐 집합, 꺼진 세그먼트 알파 ≈0.08, 칸 `RectTransform` 위치가 모든 값에서 동일.
- 지상 비표시·지하 표시: 기존 `MineResetTimedPopupTests`의 `ClockRoot.activeSelf` 단언 유지 + 재표시 반복 시 잔존 상태 없음.
- 0초: 기존 `TickMineResetCycle` 경로로 만료 1회에 팝업 1회·Elapsed 0(기존 테스트 재사용), 계기판은 표시만.
**PlayMode/수동(Bootstrap 시작)**: 디버그 터미널 `timer`(`DeveloperDebugAdvancedCommands.HandleTimer`: 양수=경과 증가, `end`=만료)로 경계 근처 재현, 최초 표시·Save/Load·지상↔지하 반복 전환·숫자 자리 전환(`00:10:00→00:09:59`, `00:01:00→00:00:59`)·1280×720/1920×1080/2560×1440 스크린샷(`work_process/MVP2/mine-reset-clock/`). 실제 세이브 보호: 자동화 테스트는 메모리 세이브(`MemoryFileSystem`+임시 경로, `PromptB125AdvancedCommandTests` 참고)만 사용하고, 수동 `timer` 확인은 기존 사용자 슬롯이 아닌 새로 만든 임시 슬롯에서만 하며 끝나면 삭제(`SubTerra/Save/*`). 안전한 임시 슬롯을 만들 수 없으면 해당 항목을 미검증으로 보고.
**정적 확인**: 새 코드에 프레임당 로그·`?.`(UnityEngine.Object)·`StartCoroutine`·종료 흐름 참조 없음, 범위 밖 파일 diff 0, Console 에러 0.
