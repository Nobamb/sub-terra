# B-212 결과 — 여자 광부 선택 시 여자 인트로 영상 (#394)

- 기준 main: `a5b70969` (Merge PR #399 MVP2-fix155). 문서 작성 때의 `a694acac`보다 B-211 병합만큼 앞선 최신 `origin/main`에서 시작했다.
- 브랜치: `MVP2-fix156` (원격에 같은 이름 없음)
- 커밋: `2d1da6be` (feat: 여자 광부 선택 시 여자 인트로 영상을 재생한다)

## 바꾼 파일
| 파일 | 내용 |
| --- | --- |
| `Scripts/App/UI/Intro/NewGameIntroPolicy.cs` | `ResolveVideoRelativePath(gender, male, female, fileExists, out fellBack)` 추가. 여자 파일이 없으면 남자로 폴백, 둘 다 없으면 null. 파일 확인 예외는 없음으로 본다. `ShouldPlayVideo` 시그니처는 그대로. |
| `Scripts/App/UI/Intro/NewGameIntroPlayer.cs` | `FemaleVideoRelativePath = "Video/sub-terra-intro-female.mp4"`, `VideoRelativePathFor(gender)`, `PathFor(relative)`, `ResolveVideoPath(gender)`(폴백 경고 로그) 추가. `RunNewGameLaunch`에 끝 인자 `string videoPath = null`을 더해 `SetUpVideo`가 그 파일을 연다(비우면 기존 남자 영상). 남자 상수 `VideoRelativePath`·`VideoAvailable()`은 그대로. |
| `Scripts/App/UI/MainMenu/MainMenuBinder.cs` | `OnMinerSelectLaunch`에서 `session.Flow.Selected`로 경로를 고르고 `RunNewGameLaunch(..., videoPath)`로 넘긴다. 모션 감소면 파일을 고르지 않는다. |
| `Tests/EditMode/App/UI/NewGameIntroTests.cs` | 8개 추가: 남자 → 남자, 여자 → 여자, 여자 파일 없음 → 남자 폴백, 둘 다 없음 → 영상 없음, 모션 감소·이어하기 → 영상 없음, 파일 확인 예외 → 없음 처리, 여자 상수·파일 존재, 메뉴가 고른 성별로 경로를 정하는지. 기존 남자 파일 검사 유지. |
| `Assets/StreamingAssets/Video/sub-terra-intro-female.mp4` (+ Unity가 만든 `.meta`) | 여자 인트로. H.264 High + AAC, 1920×1080, 30 fps, 49.2 s, 30.3 MB(남자 32.4 MB와 같은 사양). HyperFrames `sub-terra-intro-female` 렌더본. |

## 경로·폴백 로그
- 남자: `Video/sub-terra-intro.mp4` / 여자: `Video/sub-terra-intro-female.mp4` (`Application.streamingAssetsPath` 기준)
- 여자 파일이 없을 때: `[SubTerra] Intro video for Female not found at <여자 전체 경로>; falling back to Video/sub-terra-intro.mp4.`
- 둘 다 없을 때: `... ; no intro video available.` 뒤 영상 없이 켜짐 연출만.
- 성별 전달: `OnMinerSelectLaunch(session)` → `NewGameIntroPlayer.ResolveVideoPath(session.Flow.Selected)` → `ShouldPlayVideo(true, reduceMotion, videoPath != null)` → `RunNewGameLaunch(..., videoPath)` → `videoPlayer.url`. `TryStart`(성별 저장)·건너뛰기·검은 화면 유지·켜짐 연출·실패 복구는 그대로.

## 테스트
- Unity 컴파일 오류 0 (`SubTerra.App.dll`, `SubTerra.App.Tests.EditMode.dll`이 소스보다 새것인지 확인).
- EditMode(플래그로 묶음 지정): `NewGameIntroTests` + `SubTerra.App.Tests.UI.MainMenu`(B-209 포함) + `SubTerra.App.Tests.Save` + `PromptB208FemalePlayerTests` → **223 통과 / 0 실패**. `NewGameIntroTests` 단독 → 16/16 (새 8개 반영 확인).
- 테스트 실행이 다시 저장한 `MainMenuPanel.prefab`, `work_process/MVP2/103-main-menu/*.png` 2장은 `git restore`로 되돌림.

## 플레이 확인
- Bootstrap에서 플레이 → MainMenu. 메뉴 위에서 실제 코드 경로 `ResolveVideoPath(Female)` → `RunNewGameLaunch(play, startGame=false, …, femalePath)`를 실행(세이브를 덮어쓰지 않으려고 게임 시작만 실패로 돌림).
  - `VideoPlayer.url` = 여자 파일, prepared/playing true, 1920×1080, 길이 49.2 s.
  - 46.7 s 시점 Game 화면 캡처에서 **여자 광부**가 보임(엘리베이터·드론·여자 idle 광부).
  - 끝난 뒤 오버레이 제거, MainMenu 유지, `Time.timeScale` 1(정지 해제). Console 새 오류 0.
  - `ResolveVideoPath(Male)`은 남자 파일 경로를 돌려줌.

## 미검증 (직접 보지 못함)
- 광부 선택 화면에서 실제로 여자 카드를 클릭 → 「탐사 시작」 → 영상 → 새 게임 진입까지 한 번에 이어지는 흐름(세이브 덮어쓰기 때문에 하지 않음).
- 남자 선택 시 기존 남자 영상이 실제 클릭 흐름에서 나오는지.
- 화면 효과 감소 켜고 여자로 새 게임, 이어하기, 첫 광산 진입, `gender female|male` 디버그 명령, 아무 키로 건너뛰기.
- 소리(영상 AAC 트랙)는 측정만 하고 듣지 않음.

## 사용자 테스트 가이드
1. 새 게임 → 광부 선택에서 **여자** → 탐사 시작. 인트로의 광부가 여자인지 본다.
2. 새 게임을 다시 시작해 **남자**를 고른다. 기존 남자 인트로인지 본다.
3. 설정에서 화면 효과 감소를 켜고 여자로 새 게임. 영상 없이 켜짐 연출만 나오고 게임으로 가는지 본다.
4. 저장 → 메인 메뉴 → **이어하기**. 인트로가 나오지 않는지 본다.
5. 광산에 처음 들어간다. 영상이 다시 나오지 않고 생존자 브리핑은 그대로인지 본다.
6. 게임 중 디버그 `gender female`(또는 `male`). 캐릭터 그림만 바뀌고 인트로가 나오지 않는지 본다.
7. 아무 키로 인트로를 건너뛸 수 있는지, Console에 새 빨간 오류가 없는지 본다.

## 완료 체크리스트
- [x] `MVP2-fix156` push 확인
- [x] 기준 브랜치 기재
- [x] 커밋 해시 기재
- [x] Unity 컴파일 오류 없음
- [x] EditMode 새 실패 없음
- [x] 여자 선택 시 여자 영상(코드 경로·플레이 재생 확인, 클릭 흐름은 미검증)
- [ ] 남자 선택은 기존 영상 (경로만 확인, 플레이 미검증)
- [ ] 모션 감소 시 영상 생략 (테스트만, 플레이 미검증)
- [ ] 이어하기·첫 광산 진입 영상 없음 (코드 미변경, 플레이 미검증)
- [ ] `gender` 명령으로 인트로 안 뜸 (코드 미변경, 플레이 미검증)
- [x] 새 효과음 없음
- [x] 남자 영상·HyperFrames 남자 소스·부팅 인트로 미변경
- [x] YAML 손수정 없음
- [x] main 직접 커밋 없음
- [x] PR: 문서에는 "PR 안 만듦"이었지만 PM(사용자)이 직접 요청해 올림
- [x] 브랜치 삭제 안 함
- [x] #394·#266·#375 미수정
