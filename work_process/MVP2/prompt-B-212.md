# B-212 여자 광부 선택 시 여자 인트로 영상 (#394)

## 목표
새 게임 전 광부 선택 화면에서 **여자**를 고르고 「탐사 시작」을 누르면, 글리치 전환 뒤에 나오는 인트로 영상에 **여자 광부**가 나와야 한다. 남자를 고르면 **지금 남자 영상 그대로**다. 여자 영상은 PM이 기존 남자 HyperFrames 인트로에서 광부만 바꿔 만든 MP4를 아래 경로에 넣는다. 구현자는 영상을 만들거나 HyperFrames를 돌리지 않는다.
기준: https://github.com/Nobamb/sub-terra/issues/394. 관련: #266(B-209 광부 선택, 병합됨), #375(B-205 남자 인트로, 닫힘).
#394는 Nobamb 담당이다(mink 작업 금지 댓글 있음). 이번 작업은 PM 요청으로 진행한다.

## 브랜치
1. `git fetch` 후 최신 `origin/main`에서 새 브랜치 **`MVP2-fix156`**을 만든다. B-211은 `MVP2-fix155`를 쓰므로 이 번호는 건너뛴다. 이 문서를 쓸 때의 main은 `a694acacb343053e04cf8f5321e8831d07b76b99`이다.
2. 원격에 같은 이름이 있으면 빈 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다. 브랜치를 지우지 않는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix156`(또는 실제 쓴 번호)에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다.
3. GitHub 이슈 체크박스·댓글·닫기는 하지 않는다(#394, #266, #375 포함).
4. 새 음원, 새 `SfxId`, `SfxLibrary` 변경 없음. 인트로 효과음은 기존 `UiCctvOn`(켜짐 연출)만.
5. 프리팹·Scene YAML을 손으로 고치지 않는다. Gameplay가 App을 참조하지 않게 한다. 남자 영상·HyperFrames 소스(`hyperframes/sub-terra-intro` 등)·부팅 인트로(`MainMenuIntroDirector`)는 건드리지 않는다.

## PM이 넣는 여자 영상
경로(남자 영상 옆, 같은 StreamingAssets 폴더):

`sub-terra/Assets/StreamingAssets/Video/sub-terra-intro-female.mp4`

코드 상대 경로: `Video/sub-terra-intro-female.mp4` (`Application.streamingAssetsPath` 기준, 남자는 지금 `Video/sub-terra-intro.mp4`).
형식은 남자와 같게: **MP4, H.264 + AAC, 1920×1080, 30fps**. 남자 영상은 약 49.2초·약 31MB다. 길이는 남자와 맞춰 두는 것이 좋지만, 코드가 길이를 강제할 필요는 없다.
파일이 작업 시작 전에 아직 없으면 **가짜 MP4를 만들지 않는다.** 연결·폴백만 넣고, 보고서의 플레이 확인은 "파일 없음·미검증"으로 적는다. 파일이 오면 그대로 쓰고 Unity가 만든 `.meta`만 함께 커밋한다.

## 현재 상태 (main `a694acac`)
- **재생 위치.** B-209 이후 인트로는 광부 선택 확정(글리치/암전) 직후 `MainMenuBinder.OnMinerSelectLaunch` → `NewGameIntroPlayer.RunNewGameLaunch`다. 영상 → `session.TryStart()`(성별을 `SaveRuntimeController.StartNewGame`에 넘김) → 씬 로드 동안 검은 화면 → 켜짐 연출. `IntegrationRuntimeBinder.BeginNewGameIntroIfNeeded`는 **이미 없다.** 첫 광산 진입·이어하기(`OnContinue` → `BeginContinue`)에서는 영상을 틀지 않는다.
- **정책.** `NewGameIntroPolicy.ShouldPlayVideo(isNewGameStart, reduceMotion, videoAvailable)`. 모션 감소면 영상 생략, 켜짐 연출만. `PlaysOnFirstMineEntry = false`. 파일이 없거나 준비 실패면 경고 로그 후 바로 게임으로.
- **플레이어.** `NewGameIntroPlayer.VideoRelativePath = "Video/sub-terra-intro.mp4"`, `VideoPath` / `VideoAvailable()`은 이 한 파일만 본다. `SetUpVideo`가 `videoPlayer.url = VideoPath`로 연다. 건너뛰기 유예 `SkipGraceSeconds` 0.6. 오버레이는 코드로 만들며 씬 YAML을 안 건드린다.
- **성별.** `PlayerGender`(`Male=0`, `Female=1`). 선택 화면 `MinerSelectSession.Flow.Selected`, 저장 `GameSaveData`의 `gender`. 디버그 `gender male|female`(`DeveloperDebugGenderCommands`)은 **이미 시작된 게임**의 그림만 바꾸고, 인트로를 부르지 않는다. 이 명령을 인트로에 연결하지 말 것.
- **테스트.** `NewGameIntroTests`(정책·켜짐 연출·남자 파일 존재), `PromptB209MinerSelectTests`. 기존 정책을 깨지 말고, 경로 선택·폴백을 추가한다.

## 할 일
1. **경로 고르기.** `PlayerGender`로 재생할 상대 경로를 정하는 API를 둔다(이름 예: `VideoRelativePathFor`, `ResolveVideoPath`). 남자 → `Video/sub-terra-intro.mp4`. 여자 → `Video/sub-terra-intro-female.mp4`가 있으면 그것, **없으면 남자 경로로 폴백**하고 `Debug.LogWarning`에 기대한 경로와 폴백 사실을 남긴다(기존 `[SubTerra] Intro video...` 형식). 둘 다 없으면 지금처럼 영상 없이 진행한다. `ShouldPlayVideo`의 `videoAvailable`은 **실제로 재생할 파일**(폴백 포함)이 있는지로 본다. 모션 감소면 파일을 고르지 않고 지금처럼 생략한다.
2. **연결.** `OnMinerSelectLaunch`에서 `session.Flow.Selected`로 경로를 고른 뒤 `RunNewGameLaunch` / `SetUpVideo`가 그 파일을 열게 한다. `NewGameIntroPlayer`를 새로 만들지 않는다. 건너뛰기·검은 화면 유지·켜짐 연출·실패 시 메뉴 복구는 그대로다.
3. **건드리지 말 것.** 이어하기, 첫 광산 진입(시작 브리핑 유지), 부팅 인트로, `gender` 디버그 명령, 남자 영상 파일, 광부 선택 화면 연출.
4. EditMode: 남자는 남자 경로, 여자는 여자 파일이 있으면 여자 경로, 없으면 남자 경로 폴백. 모션 감소·이어하기(새 게임 시작이 아님)·파일 둘 다 없음이면 영상 안 함. 남자 파일 존재 검사는 유지. 기존 메뉴·저장·선택 화면 테스트를 깨지 말 것.

## 구현 참고
- 남자 상수 `VideoRelativePath` 이름과 값(`Video/sub-terra-intro.mp4`)은 유지한다. `NewGameIntroTests.IntroVideo_ShippedInStreamingAssets`가 이 값을 본다. 여자 경로는 상수를 하나 더 둔다.
- `SetUpVideo`의 `videoPlayer.url`이 고른 파일을 가리키게 한다. RenderTexture 1920×1080, 건너뛰기, DontDestroyOnLoad, 로드 대기, 실패 시 `OnMinerSelectLaunchFailed`는 그대로 둔다.
- `ShouldPlayVideo` 시그니처를 굳이 바꿀 필요는 없다. 호출 전에 성별별로 파일을 고르고, 그 결과가 있으면 `videoAvailable: true`를 넘긴다.
- 인트로 재생을 `IntegrationRuntimeBinder`로 되돌리지 않는다.

## 테스트 (플레이 가이드)
1. 새 게임 → 광부 선택에서 **여자**를 고르고 탐사 시작. 인트로가 나오면 광부가 여자인지 본다. (여자 파일이 아직 없으면 남자 영상이 나오고 Console에 폴백 경고가 있는지 본 뒤, 보고서에는 미검증으로 적는다.)
2. 새 게임을 다시 시작해 **남자**를 고른다. 지금과 같은 남자 인트로인지 본다.
3. 설정에서 화면 효과 감소를 켜고 여자로 새 게임을 시작한다. 영상이 생략되고 지금처럼 켜짐 연출만 나온 뒤 게임으로 가는지 본다.
4. 저장한 뒤 메인 메뉴로 나가 **이어하기**한다. 인트로가 나오지 않는지 본다.
5. 광산에 처음 들어간다. 영상이 다시 나오지 않고 생존자 브리핑은 그대로인지 본다.
6. 게임이 시작된 뒤 디버그 `gender female`(또는 `gender male`)을 입력한다. 캐릭터 그림만 바뀌고 인트로가 다시 나오지 않는지 본다.
7. 아무 키로 인트로를 건너뛸 수 있는지, Unity Console에 새 빨간 오류가 없는지를 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 여자 광부 선택 시 여자 인트로 영상을 재생한다 (프롬프트 B-212)`). `MVP2-fix156`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue394-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #394·#266·#375는 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-212-female-intro-result.md`
  - 기준 main 커밋, 만든 브랜치, 커밋 해시, 바꾼 파일
  - 경로 상수·폴백 로그, `OnMinerSelectLaunch`에서 성별을 넘긴 방법
  - 여자 영상 파일이 저장소에 있었는지, 플레이로 여자 광부를 직접 봤는지
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix156` push 확인 [ ] 기준 브랜치 기재 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음 [ ] 여자 선택 시 여자 영상(또는 파일 없으면 남자 폴백+경고) [ ] 남자 선택은 기존 영상 [ ] 모션 감소 시 영상 생략 [ ] 이어하기·첫 광산 진입 영상 없음 [ ] `gender` 명령으로 인트로 안 뜸 [ ] 새 효과음 없음 [ ] 남자 영상·HyperFrames·부팅 인트로 미변경 [ ] YAML 손수정 없음 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] 브랜치 삭제 안 함 [ ] #394·#266·#375 미수정
