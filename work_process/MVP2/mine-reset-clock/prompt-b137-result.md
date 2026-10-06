# Prompt-B 137 구현·검증 결과

- 구현자: Codex. 기준: `main`, `ce112b35`(B-136). Unity 6000.5.4f1의 연결된 Editor에서 실행.
- 결과: 7세그먼트 계기판 구현, EditMode **84 통과 / 0 실패 / 0 스킵**, PlayMode **2 통과 / 0 실패 / 0 스킵**.
- 커밋·스테이징·푸시·브랜치 전환 없음. 변경은 작업 트리에 남겼다.
- 참고 타이머·디지털 시계 이미지는 파일로 전달되지 않아 계획서의 수치와 구성으로 구현했다.

## 14-1. 수정/생성 파일 및 범위 확인

아래 코드 경로의 기준은 `sub-terra/Assets/_Project/`다.

| 구분 | 파일 | 역할 |
| --- | --- | --- |
| 수정 | `Scripts/App/UI/HUD/MineResetClockOverlay.cs` | 계기판 생성·기존 시간 바인딩·상태 교체 감지·제목 변경 |
| 수정 | `Scripts/App/UI/HUD/MineResetPopupArt.cs` | 가로/세로 세그먼트와 금속 브래킷의 절차 생성 스프라이트 |
| 수정 | `Scripts/Shared/Localization/LocalizationService.cs` | `광산 초기화까지` / `Mine Reset In` |
| 신규 | `Scripts/App/UI/HUD/SevenSegmentGlyph.cs` | 0–9 표준 7비트 마스크 |
| 신규 | `Scripts/App/UI/HUD/MineResetClockStyle.cs` | 표시 초에 따른 구간·전환·맥동 상태 |
| 신규 | `Scripts/App/UI/HUD/MineResetClockView.cs` | 세그먼트·콜론·프레임 생성 및 단일 Update 진행 |
| 신규 | `Tests/EditMode/App/UI/MineResetClockStyleTests.cs` | 경계·전환·재트리거·맥동·마스크 검증 |
| 신규 | `Tests/EditMode/App/UI/MineResetClockViewTests.cs` | 실제 오버레이 구조·숫자·고정 배치·lifecycle 검증 |
| 신규 | `Tests/PlayMode/Integration/Bootstrap/PromptB137MineResetClockPlayModeTests.cs` | Bootstrap·메모리 세이브·씬 전환·만료·해상도 검증 |
| 신규 | `Editor/DataValidation/PromptB137MineResetClockTestRunner.cs` | 메뉴 및 B-137 플래그 테스트 러너 |

신규 C# 7개에 대응하는 `.meta` 7개는 Unity가 생성했다. 기존 GUID를 변경하거나 에셋을 이동하지 않았다.

이 폴더에 추가한 결과물:

- `prompt-b137-result.md`
- [최종 EditMode 결과](prompt-b137-editmode-results.txt), [최종 PlayMode 결과](prompt-b137-playmode-results.txt)
- [초기 EditMode 실패 원문](prompt-b137-editmode-first-fail.txt), [초기 PlayMode 실패 원문](prompt-b137-playmode-first-fail.txt)
- 아래 15개 PNG. 게임용 이미지 에셋을 추가한 것이 아니라 검증 캡처다.

작업 전부터 존재한 `init/prompt-B.md`의 수정 및 `prompt-b137-plan.md`, `prompt-b137-codex-handoff.md`는 변경하지 않았다.

최종 `git diff --stat` 출력(추적 중인 파일만 포함):

```text
 init/prompt-B.md                                   | 100 +++++++++++++++++++++
 .../Scripts/App/UI/HUD/MineResetClockOverlay.cs    |  73 +++++----------
 .../Scripts/App/UI/HUD/MineResetPopupArt.cs        |  28 +++++-
 .../Shared/Localization/LocalizationService.cs     |   2 +-
 4 files changed, 151 insertions(+), 52 deletions(-)
```

`init/prompt-B.md`는 기존 사용자 변경이다. 이번 작업의 추적 파일만 대상으로 한 diff는 **3 files changed, 51 insertions(+), 52 deletions(-)**다. 신규 파일은 untracked 상태이므로 위 stat에 포함되지 않으며 위 목록에 별도로 기록했다.

최종 `git status --short`, diff, `git diff --check` 확인 완료. 이번 작업의 **범위 밖 변경 파일 0개**. Scene·Prefab·Font·TMP 설정·ProjectSettings·Packages·게임플레이·저장·종료 로직의 최종 diff는 없다.

PlayMode가 자동 변경한 `Fonts/NotoSansKR-Regular_SDF.asset`과 `ProjectSettings/EditorBuildSettings.asset`은 원복했다. 지정된 `git restore`는 `.git/index.lock` 쓰기 권한 제한으로 실패하여, 두 파일의 HEAD 내용을 읽어 기존 CRLF 형식으로 복원했다. 최종 해당 경로의 `git status` 출력 없음 및 `git diff --exit-code` 종료 코드 0을 확인했다. Git 인덱스·히스토리는 변경하지 않았다.

## 14-2. 주요 변경 내용과 설계 이유

- 숫자 6칸 × 세그먼트 7개 = Image 42개, 고정 콜론 점 Image 4개. 켜진 세그먼트 알파 1, 꺼진 세그먼트 알파 0.08. 모든 이미지의 raycast는 꺼져 있다.
- 숫자 원본은 기존 `MineResetService.FormatClock(runtime.MineResetRemainingSeconds)`다. HH:MM:SS를 할당 없이 파싱하고 같은 표시 초는 재적용하지 않는다. 잘못된 문자열은 이전 표시를 유지한다. 독립 countdown은 없다.
- 280×78, 상단 중앙 앵커·pivot `(0.5,1)`, 위치 `(0,-12)`, sortingOrder 900, pixelPerfect 및 기존 CanvasScaler 설정 유지. 숫자 높이 36px, 남색 표시창, 철제 프레임, 네 모서리 브래킷, 1px 구간색 accent, 최대 알파 0.12의 옅은 글로우를 사용한다.
- 표시 초가 1800 초과면 청록, 301–1800이면 노랑, 61–300이면 빨강, 0–60이면 빨강 + 테두리·글로우 맥동이다.
- 자연 감소 0–3초 내 구간 변경은 1.5초 SmoothStep 전환. 상태는 from/target/time 하나이며 재경계는 현재 표시색에서 새 목표로 전환한다. 최초·증가·3초 초과 점프는 즉시 적용한다.
- Final 맥동은 주기 1.75초, 알파 계수 0.45–1.0의 정현파다. 위상 0은 밝은 상태이며 숫자·콜론의 색과 알파는 맥동하지 않는다.
- 애니메이션 시간은 View.Update의 `Time.unscaledDeltaTime`에서만 진행한다. `RefreshFromState`를 반복 호출해도 전환 시간이 증가하지 않는다.
- OnEnable/OnDisable에서 관측·전환·맥동을 초기화한다. Save/Load로 Bootstrap State가 교체되면 오버레이가 View 관측을 초기화하여 같은 씬·작은 시간 차이에서도 즉시 적용한다. View는 이벤트 구독이나 코루틴을 만들지 않는다.
- 기존 팝업·시계 아이콘 코드, 시간 누적·정지·저장 스키마·만료 경로는 그대로다. 신규 View/Style/Glyph에는 종료 흐름 호출·참조가 없다.

계획 보정 및 이유:

1. 계획 §7의 전환 예시 `1800→1799`는 양쪽 모두 Warning이다. 실제 경계인 `1801→1800`을 전환 검증에 사용했다. `1800→1799`는 진행 중인 전환을 재시작하지 않는 것도 확인했다.
2. `SaveRuntimeController.ApplyRuntimeState()`는 State 교체 후 기존 오버레이에 바로 Refresh한다. 같은 씬에서 301→300 같은 작은 차이를 불러오면 시간 차이만으로 로드를 구분할 수 없어, 오버레이의 State 참조 감지와 `ResetObservation()`을 추가했다. `SaveRuntimeController`는 수정하지 않았다.
3. EditMode에서는 일반 MonoBehaviour의 활성화 콜백을 자동 실행하지 않으므로 reflection으로 OnDisable/OnEnable을 직접 호출한다. 실제 SetActive lifecycle은 PlayMode 반복 숨김·표시 및 씬 왕복으로 별도 검증했다.
4. 수동 터미널 입력 대신 `timer` 명령이 사용하는 기존 개발 API `SetMineResetElapsedSeconds`를 자동화에서 호출했다. 0초 정지 캡처는 메모리 State의 개발용 setter를 reflection으로 호출하고 정지 게이트 아래에서 표시만 관찰한다. 종료 검증은 게이트를 해제해 기존 Tick 경로로 실행했다.
5. `.git` 쓰기 제한으로 자동 변경 에셋 원복 방법만 위 설명대로 보정했다.

## 14-3. 실행한 테스트와 시각 검증 절차

1. 연결된 Unity Editor가 이 저장소의 `sub-terra/Assets`를 사용하며 Play/Compile이 false인지 확인하고 `AssetDatabase.Refresh()`로 컴파일했다.
2. `sub-terra/Temp/subterra-run-prompt-b137.flag`에 `edit`를 기록했다. Style/View 및 기존 MineResetService·TimedPopup·ClockIntro·PopupTimeline 테스트만 실행했다.
3. 같은 플래그에 `play`를 기록했다. 실제 Bootstrap 씬 → MainMenu → 메모리 슬롯 새 게임 → SurfaceBase → Integration 순서로 실행했다.
4. 슬롯 활성화 전에 SaveService/LoadService를 `MemoryFileSystem`으로 교체했다. 테스트 SaveService에는 물리 썸네일 저장 콜백을 연결하지 않았다. 실제 사용자 세이브를 쓰거나 삭제하지 않았다.
5. 경계·자리 전환·SaveCurrent/RestoreBState/BeginContinue·3회 숨김/표시 및 지상↔지하 왕복·Tick 만료를 검증했다. 해상도는 기존 `UiTestResolution`, 캡처는 `UiTestWait.Capture`를 사용했다.
6. PNG 헤더로 15장 크기를 검사하고 캡처를 열어 가독성·배치·HUD 관계를 확인했다. 사용한 임시 Game View 해상도와 입력 장치·시간 정지·Bootstrap 런타임은 테스트 teardown에서 정리했다.

스크린샷(각 행 5장):

| 해상도 | Normal | Warning | Final 밝음 | Final 어두움 | 00:00:00 |
| --- | --- | --- | --- | --- | --- |
| 1280×720 | [PNG](clock-1280x720-normal.png) | [PNG](clock-1280x720-warning.png) | [PNG](clock-1280x720-final-bright.png) | [PNG](clock-1280x720-final-dark.png) | [PNG](clock-1280x720-zero.png) |
| 1920×1080 | [PNG](clock-1920x1080-normal.png) | [PNG](clock-1920x1080-warning.png) | [PNG](clock-1920x1080-final-bright.png) | [PNG](clock-1920x1080-final-dark.png) | [PNG](clock-1920x1080-zero.png) |
| 2560×1440 | [PNG](clock-2560x1440-normal.png) | [PNG](clock-2560x1440-warning.png) | [PNG](clock-2560x1440-final-bright.png) | [PNG](clock-2560x1440-final-dark.png) | [PNG](clock-2560x1440-zero.png) |

Final 밝음·어두움은 실제 프레임을 캡처하므로 정현파 극값의 근처다. 숫자 알파 불변과 정확한 극값·주기는 순수/View 테스트로 검증했다.

## 14-4. 테스트 결과와 Unity Console 상태

최종 결과: EditMode 84/84 통과(2.5446765초), PlayMode 2/2 통과(14.8792352초). Console 오류 0개. 기존 무관한 EditMode 코드의 경고 6개(CS0618 5개, CS0184 1개)는 수정하지 않았다.

| 검증 항목 | 결과 | 근거 |
| --- | --- | --- |
| 1801 / 1800 / 1799 | 통과 | FirstObservation 경계 3건 + NaturalBoundary + PlayMode: 청록 / 노랑 / 노랑, 1801→1800 전환 |
| 301 / 300 / 299 | 통과 | 경계 3건 + NaturalBoundary + PlayMode: 노랑 / 빨강 / 빨강 |
| 61 / 60 / 59 | 통과 | 경계 3건 + FinalPulse + PlayMode: 빨강 / 빨강 맥동 / 빨강 맥동 |
| 1 / 0 / 초기화 10800 | 통과 | 경계 테스트, FinalPulse, View 초기화; PlayMode 0 표시 후 기존 Tick 만료 및 Elapsed=0 |
| 최초 표시 | 통과 | FirstObservation, Bootstrap 신규 메모리 슬롯의 Normal 상태·전환 없음 |
| Save/Load 즉시 적용 | 통과 | 메모리 SaveCurrent → Load → RestoreBState에서 301→300 snap, BeginContinue 성공·HUD 활성·전환 없음 |
| 지상 비표시 / 지하 표시 | 통과 | 기존 TimedPopup 테스트 + 실제 SceneManager 전환 3회 |
| 반복 숨김·재표시 | 통과 | EditMode lifecycle 및 PlayMode ToggleMineResetClock 3회, PulseFactor=1·전환 없음·View 1개 |
| 숫자 자리 전환 | 통과 | `00:10:00→00:09:59`, `00:01:00→00:00:59`의 FormatClock floor와 View/PlayMode 표시 초 |
| 0–9 형태·희미한 꺼짐·고정 칸·콜론 | 통과 | 표준 마스크 10건, 대표 문자열 4건의 켜짐 집합·알파·RectTransform 위치 검사 |
| 1.5초 전환·중간색·3초 감소 허용 | 통과 | NaturalBoundary 4건, 0.75초 중간색 및 1.5초 완료 |
| 전환 중 재경계·중첩 없음 | 통과 | NewBoundaryDuringTransition: 현재 중간색에서 새 빨강 목표, 완료 후 단일 상태 종료 |
| 증가·큰 점프·숨김 후 정리 | 통과 | ResetIncreaseAndJump, View lifecycle, PlayMode 재표시·씬 왕복 |
| 1.75초 맥동·숫자 불변 | 통과 | FinalPulse 및 Pulse_ChangesOnlyFrameAndGlow: 0.45/1.0, 숫자·콜론 색/알파 고정 |
| 해상도·HUD 겹침·가독성 | 통과 | 3개 해상도 × 5상태 캡처, 화면 경계·BasicHud·열린 SideMenu 비겹침 단언 및 시각 확인 |
| 종료 이벤트·저장·팝업 중복 | 통과 | PlayMode 기존 Tick 만료 후 상태 변경 1회·저장 1회·팝업 모션 1개, 기존 시계 연출 종료까지 재발 없음 |
| 파괴 시 잔존 | 통과 | View가 콜백·구독·코루틴을 만들지 않음; fixture teardown이 런타임을 파괴하고 다음 Bootstrap fixture도 단일 View로 실행 |
| 새 코드의 종료 호출·코루틴·Unity null 연산자·프레임 로그 | 통과 | View/Style/Glyph 정적 검색: 해당 참조 없음, 초기화 catch의 원인 로그만 존재 |
| 범위 밖 파일 | 통과 | 최종 status/diff: 기존 사용자 변경 외 지정된 코드·신규 파일·보고/캡처만 존재 |

초기 실패도 그대로 보존했다:

- EditMode: **81 통과 / 2 실패**. `Expected: 1.0f / But was: 0.450000018f`, `Expected: False / But was: True`. 비실행 EditMode lifecycle을 실제 호출로 보정하여 해결했다.
- PlayMode: **1 통과 / 1 실패**. 경계 다음 초에 `Expected: False / But was: True`. 1.5초 전환이 정상 진행 중인 1799/299/59에도 전환 상태를 기대하도록 테스트를 보정하여 해결했다.
- 초기 PlayMode 테스트 컴파일에서 internal setter 직접 접근, 팝업 모션 using, 잘못 가정한 SideMenu 타입 참조를 수정했다. 최종 컴파일·Console 오류 없음.
- 컴파일과 겹쳐 결과 파일이 남지 않은 요청은 성공으로 집계하지 않았다. 컴파일 종료 후 재요청하여 위 완료 결과를 확보했다.

## 14-5. Inspector 참조와 Integration 방법

Inspector 연결, Scene/Prefab 빌더 재실행, 폰트·패키지·설정 변경은 필요 없다. 기존 `MineResetClockOverlay.Create()`가 View를 생성하고 기존 Runtime이 표시 조건과 시간을 공급한다.

테스트 재실행: `SubTerra/Tests/Run Prompt-B 137 Mine Reset Clock Edit Mode Tests` 또는 `...Play Mode Tests`. 플래그는 `Temp/subterra-run-prompt-b137.flag`, 내용 `edit`/`play`. 결과는 `Temp/prompt-b137-editmode-results.txt` / `prompt-b137-playmode-results.txt`와 `.done` 파일이다. 스크립트 컴파일 완료 후 요청해야 한다.

## 14-6. 남은 TODO·세이브 호환성·검증 제한

- 세이브 스키마·DTO·시간 및 초기화 서비스 변경 없음. 세이브 호환성 영향 없음.
- **미검증:** Windows 플레이어 빌드·다른 PC, 전체 프로젝트 테스트, 3시간 실시간 대기, 실제 키보드 T/터미널 `timer` 입력 및 UI 버튼 클릭을 통한 저장·불러오기. 대응하는 런타임 메서드와 시간 데이터 경로는 위 자동화로 검증했다.
- **미검증:** 영문 제목의 실제 화면 캡처. 영문 문자열은 변경했지만 캡처는 한국어다.
- 실제 사용자 세이브에 대한 테스트는 수행하지 않았다. 테스트 슬롯은 전부 메모리이며 물리 슬롯 생성·삭제가 필요하지 않다.
- 첨부 참고 이미지와의 직접 비교 및 Claude의 최종 시각 리뷰는 남아 있다. 이 결과는 Codex 구현·검증 보고이며 Claude 리뷰 완료를 뜻하지 않는다.

## 14-7. 다른 담당자가 참고할 사항

리뷰 대상은 변경 코드, 위 경계표, 최종 테스트 로그와 15장 캡처다. `MineResetClockStyle`은 표시된 정수 초를 기준으로 하며 View가 실제 시간을 진행시키지 않는다. 오버레이의 State 교체 감지는 같은 씬 Save/Load snap 보장을 위해 필요하다.

기존 사용자 변경 `init/prompt-B.md`와 계획·handoff는 유지했다. 커밋하지 않았다.
