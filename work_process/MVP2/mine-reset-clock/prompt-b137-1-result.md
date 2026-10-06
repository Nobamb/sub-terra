# Prompt-B 137-1 결과

기록: 2026-10-06 KST. 구현과 Correction Delta 1의 A~F 보완 완료. 관련 EditMode 103개와 PlayMode 4개 통과. 전체 EditMode는 1,315개 통과 / 범위 밖 지형 검사 1개 실패다. 커밋·푸시·브랜치 전환 없음.

## 1. 수정/생성 파일

아래 코드 경로는 `sub-terra/Assets/_Project/` 기준이다.

| 구분 | 파일 | 내용 |
| --- | --- | --- |
| 수정 | `Scripts/App/UI/HUD/MineResetClockView.cs` | 프레임, 마스크, 전원 시간표 적용, 수명 정리 |
| 수정 | `Scripts/App/UI/HUD/MineResetClockOverlay.cs` | `BuildClock` 라벨 부모와 `RefreshFromState` 표시 연결만 변경 |
| 신규 | `Scripts/App/UI/HUD/MineResetClockFrameArt.cs` + `.meta` | 절차 생성 프레임 스프라이트 4종 |
| 신규 | `Scripts/App/UI/HUD/MineResetClockPowerTimeline.cs` + `.meta` | UnityEngine 비의존 전원 시간표 |
| 수정 | `Tests/EditMode/App/UI/MineResetClockViewTests.cs` | 기존 단언 유지, 경로 갱신, 전원·파괴·패딩 회귀 검사 |
| 신규 | `Tests/EditMode/App/UI/MineResetClockPowerTimelineTests.cs` + `.meta` | 경계·단조성·속도·역전·두 알파 검사 |
| 수정 | `Tests/PlayMode/Integration/Bootstrap/PromptB137MineResetClockPlayModeTests.cs` | 퇴장 완료 대기, 실제 프레임 시계열/캡처, 역전·입력 검사 |
| 수정 | `Editor/DataValidation/PromptB137MineResetClockTestRunner.cs` | 시간표 fixture 추가, 정지 캡처만 실행하는 `frame` 모드 |
| 신규/갱신 | 이 문서 및 `b137-1/` | 결과 로그, PNG, CSV, 검토용 연락판 |

최종 `git diff --stat`(미추적 신규 파일 제외):

```text
 init/prompt-B.md                                   | 107 ++++++++++
 .../PromptB137MineResetClockTestRunner.cs          |   7 +-
 .../Scripts/App/UI/HUD/MineResetClockOverlay.cs    |   6 +-
 .../Scripts/App/UI/HUD/MineResetClockView.cs       | 157 ++++++++++++---
 .../EditMode/App/UI/MineResetClockViewTests.cs     | 212 +++++++++++++++++++-
 .../PromptB137MineResetClockPlayModeTests.cs       | 217 ++++++++++++++++++++-
 6 files changed, 658 insertions(+), 48 deletions(-)
```

`init/prompt-B.md`의 107줄 변경 및 입력 문서(계획/핸드오프/Correction)는 시작 전 변경이며 보존했다. 이번 작업이 남긴 **범위 밖 변경 파일은 0개**다. 전체 테스트가 자동 변경한 씬·프리팹·폰트·ProjectSettings·기존 다른 기능 증거 PNG 14개는 인덱스의 원래 내용으로 복원했다. `.git` 쓰기 권한 제한으로 `git restore` 대신 읽기 전용 `git show :<path>`의 바이트를 작업 파일에 적용했으며, 텍스트는 저장소 CRLF 규칙을 적용했다. `git diff --check` 통과.

## 2. 프레임과 계층

기존 HUD/초기화 프레임 이미지와 `MineResetPopupArt`, `ResourceSellArt`, `CoreCctvArt` 구조를 확인했다. 원본 이미지·임포트 설정·공용 Art 코드는 변경하지 않았다. 새 프레임은 3×3 서브픽셀 샘플링으로 생성한 흰색/무채색 스프라이트를 캐시한다. Delta 2에서 철판·림·안쪽 윤곽의 잘림을 32px 텍스처 기준 6→10px로 강화하고, 9-slice border를 8→12px로 늘려 대각선이 늘어지지 않도록 했다. 안쪽 윤곽과 FrameFlash도 같은 대각선 문법을 따른다. 기존 `ResourceSellArt.SoftRect()`와 `MineResetPopupArt.Soft/Segment()`를 재사용한다.

코너 플레이트는 20×14px이며 중심 좌표를 (±130, ±32)로 배치하고 기존 네 방향 스케일 반전을 유지한다. 프레임 잘림과 평행한 외곽 변, 1px 윗면 하이라이트, 1px 어두운 베벨, 리벳 점을 표현한다. 고정 Image.color는 (0.54, 0.64, 0.70)이며 내부 명암은 불투명 무채색으로 만들어 리벳 홈에 아래의 구간색이 비치지 않는다. 외곽 윤곽에만 알파 샘플링을 적용한다. Delta 2 코드 편집은 FrameArt와 View.Build의 Bracket 크기·좌표·고정색 두 줄뿐이며, 전원 로직·테스트 코드·계층 구조는 변경하지 않았다.

```text
ClockRoot (280×78, anchor/pivot 0.5,1, position 0,-12, Canvas order 900)
├─ Window (RectMask2D, 중앙 고정)
│  └─ Content (280×78 고정, scale 1)
│     ├─ Frame (CanvasGroup)
│     │  └─ FrameGlow / Plate / Bezel / Display / AccentTop / Bracket0~3
│     └─ Readout (CanvasGroup)
│        └─ DigitGlow / Digits / Label
├─ PowerBeam / PowerSpark
├─ EdgeGlowTop / EdgeGlowBottom
└─ FrameFlash
```

철판·금속 림·코너 플레이트·Display는 고정색이다. 구간색은 세그먼트·콜론·DigitGlow·FrameGlow·안쪽 윤곽과 전원 발광에 적용한다. 숫자 영역 220×36, 각 숫자 24×36, 간격과 라벨 크기/위치는 유지했다. 모든 Graphic은 `raycastTarget=false`, 두 CanvasGroup은 `interactable=false`, `blocksRaycasts=false`다. 마스크만 크기가 변하므로 글자·숫자는 찌그러지지 않는다.

## 3. 전원 연결과 Correction 반영

- 단일 `p`를 등장 `dt/0.40`, 퇴장 `-dt/0.28`로 진행한다. 목표 반전 시 현재 p를 유지한다. `Update`의 unscaled delta를 최대 0.05초로 제한하며 코루틴·Tween·이벤트 구독은 추가하지 않았다.
- 시간표: 점 0~0.15, 가로 폭 0.10~0.40, 세로 확장 0.35~0.85(EaseOutCubic), 프레임 알파 0.30~0.45, 판독부 알파 0.55~0.90, 가장자리 발광 0.35~0.85, 정착 섬광 0.80~1.00. p=0/1에서 잔존 효과 알파는 정확히 0이다.
- `RefreshFromState` 한 곳에서 활성화 → 기존 라벨/시간 갱신 → `SetPowered(true)` 순으로 연결한다. 숨김은 `SetPowered(false)` 요청 후 뷰가 퇴장 완료 시 루트를 끈다. 동일 목표·시간값 변경·두 번의 Refresh는 p를 바꾸거나 연출을 재시작하지 않는다. 같은 씬 불러오기도 현재 p를 유지한다.
- A: `FrameAlpha`와 `ReadoutAlpha`를 분리했다. 퇴장 시 숫자/라벨이 먼저 꺼져도 철판·림은 수축 중에 보인다.
- B: 이미 꺼짐 목표여도 p=0이면 활성 루트를 비활성화한다. 최초 생성 숨김과 반복 off 모두 검사했다. 숨은 뷰의 `isActiveAndEnabled=false`로 Update가 실행되지 않는다.
- C: `OnDestroy`는 상태만 정리한다. `Apply/ApplyPower`는 Unity null 검사로 파괴된 참조 접근을 막는다. `OnDisable`은 유효한 참조만 초기화하며 관측/맥동도 정리한다. 자식 Window 선행 파괴와 overlay DestroyImmediate 검사에서 예상 밖 로그 0. 비 RectTransform에 Build를 호출하는 실패 테스트는 원인 `InvalidCastException` 초기화 로그 1개만 의도적으로 기대하고 이후 활성화/비활성화/파괴에서 추가 예외가 없음을 확인했다. 과거 `Apply ← ResetObservation ← OnEnable` NRE는 현재 트리에서 재현되지 않아 특정 자식을 원인으로 단정하지 않는다.
- D: 고정 −6px 패딩의 실제 연속 캡처를 먼저 열었다. p≈0.375에서 마스크 높이 약 12px에 패딩 12px가 더해져 철판 띠가 넓게 보였다. `qa-power-on-fixed-padding.png`, `qa-power-off-fixed-padding.png`가 보정 전 근거다. 보정 후 p≤0.40은 padding=0, 0.40~0.85는 0→−6, 이후 −6이다. `qa-power-on.png`의 02 및 퇴장 05에서 초기 띠가 줄고 정상 프레임의 번짐은 유지된다. 패딩의 등장/되감기 경계도 EditMode로 검사했다.
- E: FrameArt의 리벳 설명은 정상 UTF-8 한국어다.
- F: EditMode의 20단계 값 갱신/반복 요청과 PlayMode의 실제 20프레임 × 두 번 Refresh 검사 모두 통과했다. `power-refresh.csv`의 after와 expected가 전 행에서 일치한다. PlayMode에서는 수동 Advance 없이 실제 Update의 unscaled delta를 검사했다.

## 4. 검증 결과

모든 증거 경로는 `b137-1/` 기준이다. Delta 2 최종 코드를 2026-10-06에 재검증했으며, 낡은 플래그/결과와 구별하여 시작 시각·fixture 수를 확인했다. 기존 테스트의 구조·단언은 변경하지 않았다. 정지 캡처 18장, 전원 시퀀스와 검토용 연락판을 새로 생성했다.

| 항목 | 결과 | 근거 |
| --- | --- | --- |
| 컴파일 | 통과 | 최신 코드 import 후 MCP 명령 컴파일/실행 성공, `isCompiling=false` |
| 관련 EditMode | 통과 | `editmode-focused.txt`: 16:36:17 시작, 103 Pass / 0 Fail / 0 Skip, 2.783초 |
| 전체 EditMode | 실패(이전 실행 유지) | `editmode-all.txt`: Delta 1의 16:23:50 실행, 1,315 Pass / 1 Fail / 0 Skip. Delta 2 범위에서는 전체 suite를 재실행하지 않음 |
| 실제 Bootstrap PlayMode | 통과 | `playmode-focused.txt`: 16:36:38 시작, 아래 fixture 4개 모두 Pass, 42.094초 |
| 프레임/배치/가독성 | 통과 | 3해상도 × normal/warning/critical/final-bright/final-dark/zero = 18장 `clock-<해상도>-<상태>.png` 재생성; `AssertLayout` HUD/사이드 메뉴 겹침 0. 1280×720 원본을 직접 열어 코너 조각과 숫자·라벨의 시각적 겹침 0 확인 |
| 모서리 확대 비교 | 통과 | `qa-frame-zoom.png`: 구 B-137 원본(상위 폴더 `clock-1920x1080-normal.png`)과 이번 프레임의 (800,0)-(1120,100) 영역을 최근접 보간으로 정확히 4배 확대해 나란히 배치. 대각선 잘림·밝은 금속 조각·리벳/베벨이 구 사각 브래킷과 구분되어 보임 |
| 금속 고정색·맥동 | 통과 | 갱신한 `qa-frame-colors.png`와 ViewTests. 1920×1080 normal/warning/critical의 리벳 픽셀 (833,15)은 모두 RGB (41,50,55), 베벨 (831,16)은 모두 (45,54,61). 세그먼트 밝기와 마지막 1분 번짐/윤곽 맥동 유지 |
| 등장 방향 | 통과 | `power-on-00~18.png` 19장과 `power-on.csv`: p 0.125→1, width 23.333→280, height 0.208→78 |
| 퇴장 방향·정리 | 통과 | `power-off-00~11.png` 12장과 `power-off.csv`: p 0.87394→0, height 78→0, width 280→0. FrameAlpha가 ReadoutAlpha보다 늦게 꺼짐, 종료 루트 비활성 |
| 실제 소요 시간 | 통과 | 캡처 없는 warmed PlayMode / timeScale 0: 등장 약 0.403787초, 퇴장 0.281589초. 갱신한 `power-duration.txt`, `power-refresh.csv`, `playmode-reversal-stages.tsv` |
| 노랑·빨강·마지막 1분 연출 | 통과 | on/off-1800 각 24/21장, on/off-120 각 24/11장, on/off-60 각 25/12장. 해당 CSV와 `qa-power-*.png` 갱신 |
| 중복 갱신·중단 역전·반복 토글 | 통과 | ViewTests 및 `Power_ReverseOnSceneVisibility_AndRepeatedRefresh_PreserveCurrentProgress`; off→on/on→off에서 p 연속. 토글·씬 왕복 3회 후 View 1개 |
| 저장/불러오기·구간 경계·0초 | 통과 | `Bootstrap_MemorySaveLoad_Reentry_Boundaries_AndSingleExpiry`: 같은 씬 load p 유지, 1800/300/60 경계, 메모리 Continue, 00:00:00, 만료 변경/저장 각각 1회, 기존 팝업/인트로 유지 |
| 입력 통과 | 통과 | 모든 Graphic false 및 매 캡처 EventSystem raycast 검사. `VerifyClickThroughClock`에서 order 899의 시계 뒤 투명 버튼에 실제 입력을 보내 등장 중/완료 후 클릭 1회씩 도달 |
| 일시정지 | 통과 | UiPauseGate 획득 및 timeScale=0에서 경고/위험/마지막 1분 on/off와 양방향 역전 완료 |
| 파괴/초기화 실패 | 통과 | `PartialChildDestruction_AndOverlayDestruction_DoNotAccessDestroyedReferences`, `BuildFailure_ThenEnableDisableAndDestroy_HasOnlyTheInitializationError` |
| 변경 범위 | 통과 | 최종 git status: 시작 전 변경을 제외한 계획 범위 밖 파일 0, 금지된 런타임/원본 에셋/세이브 DTO 변경 0 |
| Delta 2 Console/편집기 | 통과 | PlayMode 종료 직후 Warning/Error/Exception 0. Bootstrap 씬, Play=false, Compiling=false. 자동 변경 EditorBuildSettings 원복, 이번 실행의 미추적 임시 InitTestScene 잔존 0 |
| 지형 실패의 기존 HEAD 재현 | 미확인 | 깨끗한 HEAD에서 재현하지 않았다. 최종 변경 파일 목록에 지형·씬·프리팹·설정이 없다는 읽기 전용 diff 근거만 있으며 시계 변경과의 무관성을 확정하지 않음. stash/브랜치 전환/커밋 없음 |

PlayMode fixture:

1. `Bootstrap_MemorySaveLoad_Reentry_Boundaries_AndSingleExpiry`
2. `Clock_ThreeResolutions_FiveStates_WithHud` (기존 이름 유지, critical 추가로 실제 6상태)
3. `Power_EntryExitSequences_WarningCriticalAndFinal_WithPauseAndInput`
4. `Power_ReverseOnSceneVisibility_AndRepeatedRefresh_PreserveCurrentProgress`

연속 PNG에서 시계 부분을 원본 픽셀 그대로 잘라 배열한 `qa-power-on.png`와 `qa-power-off.png`를 갱신해 직접 열었다. 청록 등장의 00→01은 점→좌우 선, 02→06은 중앙 선에서 위/아래로 확장, 07→18은 판독부 페이드와 발광 감쇠다. 13~14의 FrameFlash가 새 잘림선과 일치하고 코너 조각을 찌그러뜨리지 않는다. 청록 퇴장의 00→02는 숫자/라벨 소멸, 02→04는 프레임이 위/아래에서 가운데로 수축, 05→10은 선 폭 감소→점, 11은 소멸이다. 마스크 수축에서 코너가 잔존하거나 밖으로 튀어나오지 않았다. 전원 시간표·패딩 로직을 변경하지 않고 확인한 결과다. PNG와 CSV는 WaitForEndOfFrame의 같은 상태를 기록하며 PNG 압축은 연출 뒤에 수행한다.

등장/퇴장 CSV의 기록 구간은 각각 0.36917/0.25843초다. 장면 진입/복귀 감지 뒤 시작하므로 첫 p가 0/1이 아닐 수 있다. 이 구간 길이를 전체 연출 시간으로 보고하지 않았다. 전체 시간은 별도 캡처 없는 실제 프레임 측정(등장: 20프레임 delta 합 + 완료 대기, 퇴장: 요청부터 완료 대기)으로 확인했다. 0.05초 dt 상한으로 심한 프레임 정체에서는 벽시계 시간이 길어질 수 있다.

## 5. 전체 EditMode 실패와 Console

범위 밖 실패는 아래 출력이다. 시계 변경으로 지형을 수정하지 않았다.

```text
SubTerra.App.Tests.Integration.IntegrationWiringTests.PlayableMineScene_HasRequestedCharacterTerrainAndMiningLayout
Missing terrain at (-13, -40).
Expected: True
But was: False
IntegrationWiringTests.cs:78
```

전체 suite가 모두 통과했다고 보고하지 않는다. PlayMode 종료 직후 MCP Console 조회는 Warning/Error/Exception 0이었다. 전체 EditMode에는 `SceneLoaderTests`의 빈/없는 씬 오류와 Build 실패 회귀 테스트의 초기화 오류 등 `LogAssert.Expect`로 검사하는 의도적 로그가 있다. 해당 로그를 런타임 오류 0이라는 주장에 섞지 않았다. 새 컴파일 오류·시계 MissingReference/NRE는 관련 검증에서 발생하지 않았다.

## 6. 계획 보정·통합·제한

- Correction에 따라 Content 단일 알파 대신 Frame/Readout 두 CanvasGroup을 사용했고 OnDestroy는 UI를 건드리지 않는다. 원래 Find 이름의 의미는 유지하면서 테스트 경로만 갱신했다.
- 테스트 Boot는 지상에서 overlay가 아직 없는 실제 생성 시점을 반영하여 광산 진입 후 참조를 얻는다. 시퀀스 Boot는 생성/켜짐 목표를 감지하며 즉시 캡처한다.
- 런타임은 광산에서 매 프레임 SetSessionVisible을 다시 설정한다. visibility 중단 테스트의 해당 구간만 테스트 소유 runtime.enabled=false로 자동 갱신을 멈춰 오버레이 경로를 검증하고 마지막에 복구한다. 실제 씬 이동은 별도 스모크/진입·복귀 캡처로 검사한다. 프로덕션 runtime 코드는 수정하지 않았다.
- 추가 Inspector 연결/씬 빌더 실행은 필요 없다. 기존 overlay의 BuildClock/RefreshFromState에 연결되어 자동 생성된다.
- 실제 사용자 세이브는 수정하지 않았다. 모든 save/load 검증은 MemoryFileSystem이며 세이브 포맷·호환성 변경 0이다.
- 이번 테스트가 만든 미추적 InitTestScene 및 .meta는 제거했다. 저장소가 원래 추적하는 `InitTestScenee44af6e8-beef-46bb-b617-faa6da2bfe9f.unity` 쌍은 범위 밖 기존 파일이므로 변경하지 않았다. 최종 Unity는 Bootstrap, Play=false이며 요청 플래그 잔존 0이다.
- 전체 PlayMode 모든 기능 suite와 플레이어 빌드는 미검증이다. 이번 요청에 해당하는 PlayMode 4개는 모두 실행했다. 전체 EditMode 지형 실패 1개는 남는다.
- 사용자 첨부 이미지는 없었으며 저장소의 기존 프레임 이미지를 참고했다. Computer Use는 사용하지 않았다.
