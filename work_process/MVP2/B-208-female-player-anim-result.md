# B-208 여자 캐릭터 모션 세트 + 성별 저장 + 디버그 `gender` 결과 (#266 일부)

## 기준
- 기준 브랜치·커밋: `origin/main` `64f84fcc2d86236afd705dea9c6fa31ce4eb49a1` (문서 작성 시점 `0c51ec15` 이후 B-207 병합 커밋)
- 작업 브랜치: `MVP2-fix152` (원격에 같은 이름 없음)
- 커밋 해시: `b52f727c` (기능), 보고서 갱신 커밋은 그 뒤. 사용자 요청으로 PR을 열었다(B-208 문서는 PR 미생성이 원칙). #266은 건드리지 않았다.

## 바꾼 파일
- Shared: 추가 `Scripts/Shared/PlayerGender.cs` (`Male = 0`, `Female = 1`)
- Gameplay(Player)
  - 추가 `PlayerAnimationSet.cs` (ScriptableObject, 성별별 그림 배열 10개)
  - 수정 `PlayerAnimationController.cs`: `maleAnimationSet`/`femaleAnimationSet` 필드, `SetGender`, `ApplyAnimationSet`, `Gender`, 시퀀스 장수 동기화, 기절 숨쉬기 루프 일반화
  - 수정 `PlayerMiningAnimationSequence.cs`, `PlayerLadderMiningAnimationSequence.cs`, `PlayerJumpAnimationSequence.cs`: `SetFrameCount(s)`와 "설정값·실제 장수 중 작은 값"
- App
  - 수정 `Save/GameSaveData.cs` (`PlayerSaveData.gender`), `Save/SaveDataMapper.cs`, `Save/SaveRuntimeController.cs`, `State/GameState.cs`
  - 추가 `State/PlayerGenderPresentation.cs` (현재 게임 성별 알림)
  - 추가 `UI/Guide/GuidePlayerArt.cs`, 수정 `UI/Guide/GuideDemoClip.cs`, `UI/Guide/GameGuideStageView.cs`
  - 수정 `UI/MainMenu/ControlSchemePanel.cs`, `UI/MainMenu/MainMenuView.cs` (메인 메뉴 조작 안내 남자 고정)
- Integration
  - 추가 `PlayerGenderVisualBridge.cs`, `Debug/DeveloperDebugGenderCommands.cs`
  - 수정 `IntegrationRuntimeBinder.cs` (`PlayerGenderVisual` 단계), `Debug/DeveloperDebugCommandRegistry.cs` (등록 1줄)
- Editor
  - 추가 `Editor/DataValidation/PromptB208PlayerAnimationSetBuilder.cs` (메뉴 `SubTerra/MVP2/Build Prompt-B 208 Player Animation Sets`)
  - 수정 `Editor/PlayerAnimationAssetBuilder.cs` (`Validate Player Animation Runtime`이 여자 세트 검사 호출), `Editor/DataValidation/PromptB140GameGuideSkinBuilder.cs` (여자 키)
- 에셋 (빌더로 생성·연결)
  - 추가 `Art/Characters/Player/Female/Frames/<모션>/*.png` 199장 + meta
  - 추가 `Art/Characters/Player/AnimationSets/PlayerAnimationSet_Male.asset`, `PlayerAnimationSet_Female.asset`
  - 수정 `Prefabs/Gameplay/Player/Player.prefab`: **2줄만**(`maleAnimationSet`, `femaleAnimationSet` 참조). 기존 프레임 배열·설정은 그대로.
  - 수정 `Resources/UI/GameGuideSkin.asset`: 여자 키 71개 추가(기존 항목 변경 없음)
- 테스트: 추가 `Tests/EditMode/App/Readiness/PromptB208FemalePlayerTests.cs`
- 도구·자료: 추가 `work_process/tools/prepare_female_frames.py`, `work_process/images/player-female-20261121/`
- 교훈: `init/rules/lessons/2026-11-22-issue266-play-smoke-begincontinue.md`
- 씬(`Mine_Demo_Integration` 포함)·남자 원본 PNG/meta·SFX·ProjectSettings는 바꾸지 않았다. `.webm`/`.mov`와 압축·풀린 원본 폴더는 커밋하지 않았다.

## 모션별 사용 번호·장수
`prepare_female_frames.py`가 512 → 256 LANCZOS(premultiplied)로 줄이고, 크기·위치 보정은 하지 않는다. 결과는 남자와 같은 파일 이름이며 빠진 번호는 만들지 않았다(복제 없음).

| 모션 | 원본 연번 | 사용 번호 | 남자 | 여자 |
|---|---|---|---|---|
| Idle | `player_idle_01.png` | 그대로(256) | 1 | 1 |
| Walk | `00051` | 0000, 0002 … 0076 | 40 | 39 |
| Mining | `00047` | 0000 … 0044 짝수 | 24 | 23 |
| LadderMining | `00043` | 0000 … 0044 짝수 | 24 | 23 |
| JumpReady | `00015` | 0000 … 0012 짝수 | 8 | 7 |
| JumpRise | `00019` | 0000 … 0044 짝수 | 24 | 23 |
| JumpFall | `00021` | 0000 … 0044 짝수 | 24 | 23 |
| Knockout | `00023` | 0000 … 0092 짝수 | 48 | 47 |
| LadderBack | `00033` | neutral_v2=0008, style_02=0000, 03=0002, 04=0004, 05=0006 | 5 | 5 |
| Damage | `00013` | 0000, 0006, 0011, 0015, 0022, 0026, 0030, 0035 | 8 | 8 |

- **Damage 번호는 문서 그대로**다. 남자 8칸과 나란히 놓고 보니 서 있음 → 젖혀지기 → 최대 → 돌아옴 → 선 자세 순서가 칸마다 맞아 바꾸지 않았다(`damage-sheet.png`, `damage-male-vs-female.gif`).
- **잡티:** `LadderBack/…__0006.png`(style_05)에만 초록 점이 있었다. 몸과 떨어진 알파 덩어리 중 초록 우세(G − max(R,B) > 40) 픽셀이 30% 이상인 것만 투명하게 지웠다. 지운 픽셀 1,149개(512 기준, 길쭉한 점 4개 + 1px 1개), 남은 초록 픽셀 0. 다른 장은 건드리지 않았고 같은 연번에서만 가져왔다.
- **Import:** Sprite 한 장, PPU 256, 중앙 피벗, FullRect, Bilinear, 밉맵 끔, 무압축, Clamp — 남자 같은 모션과 같다.
- **사다리 PPU (문서와 다름, 근거 있음):** 문서는 "남자 1254/PPU 1254"라고 했지만 실제 남자 사다리 meta는 **PPU 1084**다(월드 높이 약 1.157 캔버스). 여자 사다리 캔버스는 남자 캔버스와 비율이 같아(내용 위치 0.116~0.880 vs 0.117~0.887) PPU 256이면 남자보다 13% 작아진다. 그래서 같은 월드 크기가 되도록 **PPU = 256 × 1084 / 1254 ≈ 221.3**을 썼다. 플레이 중 사다리 위 남/여 `SpriteRenderer.bounds` 높이가 둘 다 **1.098985**로 같음을 확인했다.
- **관찰(보정 안 함):** 사다리 채굴 여자 발 위치가 남자보다 약 11px 높다(남자 16~18, 여자 28~29, 여자 사다리 뒷모습 27~36과는 비슷). 문서 지시대로 위치 보정은 하지 않았다. 어색하면 PM 판단 필요.
- 미리보기(왼쪽 남자, 오른쪽 여자, 각자 장수로 반복): `work_process/images/player-female-20261121/<모션>-male-vs-female.gif`, 시트 `<모션>-sheet.png`, 수치 `checks.json`. 용량 때문에 GIF는 절반 크기다.

## 짧은 세트 처리
컨트롤러가 그림 배열 장수를 시퀀스에 넘기고(`SetFrameCount(s)`, 0이면 설정값 그대로), 시퀀스는 **설정값과 실제 장수 중 작은 값**을 쓴다. 남자는 장수 = 설정값이라 같은 값이다.
- 채굴: 마무리 끝 `PlayerMiningAnimationSequence.FinishLastFrame`(24)과 장수 중 작은 값 → 여자 23. 작업 핑퐁 5↔13 그대로.
- 사다리 채굴: `finishLastFrame`(24) vs 장수 → 여자 23. 작업 13~16 그대로.
- 점프: 상승 끝·정점 강제 표시 = `riseLastFrame`(24) vs 장수 → 여자 23. 낙하 루프 18~`fallLoopLastFrame`(24) vs 장수 → 여자 18~23. 착지 시작 = `landFirstFrame`(8) vs Ready 장수 → 여자 7→1.
- 기절: 조건을 `>= 48`에서 "37장보다 많을 때"로 바꾸고, 끝까지 재생(장수/24fps) 뒤 37~마지막 장을 반복한다(남자 37~48, 여자 37~47 = 11장).
- 걷기: 원래 `frames.Length`로 반복한다(여자 39장, 40fps). 발소리 프레임 `{1, 21}` 그대로.
- 사다리 내려가기 직전(LadderDrop): 이미 낙하 장수로 자르고 있어 그대로.
- 가이드: 아래 「가이드 성별 처리」.
- **남자 결과 불변:** 남자 장수를 넣은 시퀀스와 장수를 넣지 않은(옛 동작) 시퀀스의 채굴·사다리 채굴·점프 프레임 기록이 같고, 끝값(채굴 24, 사다리 채굴 24, 상승 24, 낙하 18~24, 착지 8)도 그대로임을 테스트로 확인했다. 기존 남자 테스트(`PlayerMiningAnimationSequenceTests`, `PlayerJumpAnimationSequenceTests`, `PlayerRedrawnActionTests`의 기절 2초/숨쉬기 등)도 통과. 기본 남자 상태에서는 프리팹 배열을 그대로 쓰고(브리지가 같은 성별이면 세트를 다시 넣지 않음) 남자 세트 에셋은 프리팹 배열을 그대로 복사한 것이다(테스트로 동일 참조 확인).

## 세트 교체
- `PlayerAnimationController.SetGender(gender)` → 해당 세트가 있으면 `ApplyAnimationSet`. 그림 배열과 시퀀스 장수만 바꾸고 상태(`currentState`), 채굴/점프 시퀀스 객체·진행 번호·시간은 그대로다. 다음 그리기부터 새 그림이고, 진행 번호가 새 장수를 넘으면 그리기 단계에서 마지막 장으로 자른다(테스트: 채굴 23번 진행 중 여자로 바꾸면 여자 23번째 장, 다시 남자로 바꾸면 남자 24번째 장).
- 연결: `IntegrationRuntimeBinder.WireContracts`의 `PlayerGenderVisual` 단계가 플레이어 `VisualRoot`에 `PlayerGenderVisualBridge`를 붙인다(씬 YAML 변경 없음, 런타임 AddComponent). 브리지는 `PlayerGenderPresentation.Changed`를 구독해 바로 적용한다.
- `Validate Player Animation Runtime` 메뉴가 끝에 `PromptB208PlayerAnimationSetBuilder.Validate()`를 불러 여자 장수 ≠ 표, 여자 폴더 밖 그림(남자 그림 섞임), 프리팹 미연결이면 예외를 낸다.

## 저장·성별 값
- `PlayerSaveData.gender`(int). JsonUtility는 없는 항목을 0으로 두므로 **옛 저장 = 남자**. 0/1 외의 값도 남자로 복원한다. 정수 기본값으로 충분해 `SaveVersions.Current`(6)는 올리지 않았다(마이그레이션 불필요).
- 런타임 값은 `GameState.Player.Gender`. `GameState.SetPlayerGender`는 값이 바뀔 때만 `PlayerGenderChanged`를 낸다.
- `SaveRuntimeController`가 이 이벤트를 받아 `dirty = true`(다음 저장에 남김) + `PlayerGenderPresentation.Set`. 상태를 붙일 때(`ApplyRuntimeState`: 새 게임·이어하기·부트) 저장된 성별을 알린다.

### 새 게임 성별 진입점 (B-209용)
```csharp
SaveRuntimeController.Instance.StartNewGame(slotId, confirmOverwrite: true, gender: PlayerGender.Female);
```
- 세 번째 인자 `gender`(기본 `PlayerGender.Male`)만 넘기면 된다. 내부에서 `GameState.CreateNew()` 직후 `SetPlayerGender(gender)`를 하고, 첫 저장(Surface Base 진입 시)에 남는다.
- 지금 호출부(`MainMenuBinder.OnStartNewGame`, 스모크 명령)는 인자를 넘기지 않아 늘 남자다. B-209 선택 화면은 `MainMenuBinder.OnStartNewGame`에서 고른 값을 이 인자로 넘기면 된다.
- 이어하기는 저장된 `gender`를 쓴다.

## `gender` 명령
- `gender male|female`, 별칭 `성별 남|여`. 인자는 `male/m/남/남자`, `female/f/여/여자`(대소문자 무시).
- 즉시 `GameState.SetPlayerGender` + `PlayerGenderPresentation.Set` → 플레이어 그림·열린 가이드/조작 안내가 바로 따라오고, 다음 저장에 남는다.
- 인자 없음·틀림·인자 2개 이상: `현재 성별: 남자(male)` + `사용법: gender male|female (성별 남|여)`. 씬 제한 없음(지상에서도 가이드 확인용으로 쓸 수 있음).

## 가이드 성별 처리
- `GameGuideSkin`에 `player.female.*` 키 추가: `idle`, `walk.1–39`, `mine.1–23`, `ladder.1–5`, `jump.4`(jump_rise_12), `damage.1`, `knockout.1`(**knockout_47**).
- 시연 장면은 남자 키로 만들어 두고, `GuidePlayerArt.ForGender(clip, Female)`가 플레이어 층만 복사해 키를 `player.female.*`로 바꾼다. 걷기는 39장으로, 채굴은 23장(작업 1~13, 마무리 15~23 60fps)으로 다시 만든다. 여자 장면은 여자 키만 가리켜 남자 그림이 섞일 수 없다(테스트: 가이드·조작 단말 모든 장면 검사). 남자는 원래 장면 인스턴스를 그대로 쓴다.
- `GameGuideStageView`가 `Bind` 때 현재 성별로 장면을 고르고, `Update`·`OnEnable`에서 성별이 바뀌면 같은 장면·시각·재생 상태로 다시 그린다 → 열려 있는 가이드·조작 안내도 바로 바뀐다.
- 메인 메뉴: `MainMenuView`가 조작 안내 패널을 붙일 때 `LockGuideGender(Male)`로 남자 고정(부트 시 저장된 상태가 여자여도 메인 메뉴는 남자).
- `GuideSprites` 자체는 키 그대로 찾는다. 성별 판단을 장면 단계 한 곳에서만 해서, 장면은 남자(40장)인데 그림만 여자로 바뀌어 없는 키를 찾는 일이 없게 했다.

## 테스트 결과
- EditMode `PromptB208FemalePlayerTests` **27/27 통과**: 옛 저장(항목 없음·모르는 값) → 남자, 저장 왕복 여자 유지, 새 게임 남자·`StartNewGame` 성별 기본값, 이벤트 1회, 여자 장수 = 표·중복 장 없음·여자 폴더 PNG 199장·webm/mov 없음, import 설정, 남자 세트 = 프리팹 배열·발소리 1·21, 남자 순서 불변, 여자 범위·낙하 루프 18~23·착지 7부터, 기절 숨쉬기(남 37~48, 여 37~47), 교체 시 상태 유지·마지막 장 자르기, 여자 걷기 39장 반복, 브리지 즉시 적용, `gender` 파싱·명령, 가이드 여자 키·장수, 여자 장면에 남자 키 없음, 남자 채굴 장면 트랙 불변, 조작 단말 무대 성별 따라가기·`OnEnable` 갱신·메인 메뉴 고정.
- 관련 묶음 재실행(B-208, Readiness `Player*`, `MiningTargetReticleTests`, `PhaseCTraversalStaticTests`, Debug 전체, Save 전체, `DemoObjectiveSaveRoundTripTests`, `ControlTerminalTests`, `PromptB140GameGuideTests`, MainMenu 전체, `PromptB1791ControlPanelTests`): **507 통과 / 6 실패**.
  - 5개는 기존 실패(B-207 보고서에서 변경 전 기준선으로 확인된 것과 같은 항목): `PromptB125AdvancedCommandTests.Help_ListsAdvancedCommands_AndSourcesStayGated`, `PromptB125TerminalTests.Enter_ConfirmsCandidateBeforeExecuting`, `Terminal_ShiftArrowsSelectAndWrapCandidates_ForTabOrEnter`×2, `PromptB193DebugCommandTests.Falling_CushionUsesConfiguredRatioAfterCargo(1.7, 0.25)`.
  - `PlayerLadderMiningTests.Damage_ResumesThirteen_KnockoutInterrupts`: 첫 실행(같은 컨트롤러 코드)에서는 통과, Play 종료·재컴파일 뒤부터 실패. 테스트가 13번 재개 직후 같은 `LateUpdate`에서 `Time.unscaledDeltaTime`만큼 진행하는데, 그때 편집기 값이 **0.356초**(12fps 4장 이상)였다. 같은 값으로 시퀀스만 돌리면 장수 제한 유무와 상관없이 같은 번호(14)가 나와 B-208 변경과 무관한 **환경 의존 실패**로 판단한다. 변경 전 기준선 재실행은 하지 않았다(**미검증**).
- 테스트 실행 뒤 범위 밖으로 바뀐 `MainMenuPanel.prefab`, `work_process/MVP2/103-main-menu/*.png` 2장은 `git restore`로 되돌렸다. `Fonts/NotoSansKR-Regular_SDF.asset`, `ProjectSettings/TimeManager.asset`, `init/prompt-B.md`는 작업 시작 전부터 바뀌어 있던 파일이라 커밋하지 않았다.
- Unity 컴파일 오류 없음. 플레이 중 Console 새 빨간 오류 없음(MCP 카메라 캡처 도구 자체 오류 1건은 도구 문제).

## 플레이 확인 (Bootstrap → 메인 메뉴 → 이어하기 → 광산, MCP 스크립트)
세이브 폴더를 백업하고 끝난 뒤 바이트 비교로 원래대로 되돌렸다.
- 옛 저장(slot 1, `gender` 항목 없음) `BeginContinue` → `IsUiReady = true`, 광산에서 **남자**(사다리 위 `Frames/LadderBack/ladder_back_neutral_v2`).
- `gender`, `gender abc` → 현재 성별 + 사용법. `gender female` → 같은 프레임에 `Gender = Female`, 다음 그리기에서 `Female/Frames/LadderBack/ladder_back_neutral_v2`(엘리베이터 안 낙하 루프 상태에서는 `Female/Frames/JumpFall/jump_fall_18`).
- 저장 → slot 파일에 `"gender": 1`. `성별 남` → 남자 그림으로 돌아옴. 사다리 남/여 월드 높이 같음(1.098985).
- 화면 캡처로 여자 캐릭터가 엘리베이터 바닥에 남자와 같은 크기·발 위치로 서 있음을 확인했다.
- 조작 단말 무대: 화면이 숨겨진 동안(`Opener` 비활성) `Update`가 돌지 않아 성별 변경이 늦게 반영될 수 있음을 플레이에서 발견해 `OnEnable` 갱신을 추가했다(EditMode 테스트로 확인).
- 처음엔 동기 `Continue()`로 들어가 `IsUiReady never became true` 경고가 났다. 하네스 실수(실제 메뉴는 `BeginContinue`)였고 교훈 파일로 남겼다.

### 미검증 (직접 눈으로 못 본 것)
- 실제 키 입력으로 오래 걷기·채굴/사다리 채굴 마무리·높은 점프 착지·사다리 오르내리기·`falling 30m` 다침·`hitgas` 숨쉬기 반복을 **화면으로** 본 확인(프레임 번호·범위는 EditMode로만 확인).
- 채굴 중 `gender male` 입력 시 채굴이 끊기지 않는지의 실제 플레이(EditMode에서 상태·진행 유지 확인).
- 열린 가이드 창(광산/지상)에서 성별을 바꿨을 때의 화면(조작 단말 무대는 EditMode로 확인).
- 여자로 저장 → 메인 메뉴 → 이어하기 왕복의 실제 메뉴 클릭, 메인 메뉴 조작 안내가 남자인지의 화면.
- 새 게임 시작 실제 클릭(코드상 기본 남자, 테스트로 확인).
- 발소리 타이밍 귀로 확인(프레임 `{1, 21}` 그대로).

### 사용자 테스트 가이드
1. 옛 저장으로 이어하기한다. 캐릭터가 지금과 똑같은 남자 그림·움직임인지 본다.
2. 새 게임을 시작한다. 남자로 시작하는지 본다.
3. 디버그 콘솔에 `gender female`을 친다. 그 자리에서 바로 여자 캐릭터로 바뀌는지 본다.
4. 여자 상태로 오래 걷는다. 걸음이 끊기거나 멈칫하는 장면 없이 이어지는지 본다.
5. 땅을 길게 채굴하다 멈춘다. 드릴 반복과 마무리 자세가 멈칫 없이 끝나는지 본다. 사다리에서도 같은 것을 해 본다(여자 사다리 채굴 발 위치가 남자보다 약간 높음 — 어색한지 판단).
6. 높이 뛰어 오래 떨어지다가 착지한다. 떨어지는 동안 반복이 자연스럽고 착지가 어색하지 않은지 본다.
7. 사다리를 오르내린다. 뒷모습 반복이 매끄럽고 초록 점이 없는지 본다.
8. `falling 30m`으로 다친다. 맞는 모습이 남자와 비슷한 박자로 나오는지 본다.
9. `hitgas`로 가스에 쓰러진다. 쓰러진 뒤 숨쉬기 반복이 계속 움직이는지 본다(멈춰 있으면 실패).
10. 채굴 중에 `gender male`을 친다. 채굴이 끊기지 않고 그림만 남자로 바뀌는지 본다.
11. `gender female` 상태로 가이드 창과 조작 안내를 연다. 걷기·채굴·사다리 그림이 모두 여자인지 본다. 연 채로 `gender male`을 치면 바로 남자로 바뀌는지 본다.
12. 여자로 바꾼 뒤 저장하고 메인 메뉴로 나갔다가 이어하기한다. 여자로 나오는지 본다. 메인 메뉴 조작 안내는 남자인지 본다.
13. `gender`만 치거나 `gender abc`를 친다. 오류 없이 사용법이 나오는지 본다.
14. 새 효과음이 없고 발소리 타이밍이 같은지, Unity Console에 새 빨간 오류가 없는지 본다.

## Inspector·통합
- `Player.prefab > VisualRoot > PlayerAnimationController`: `Male Animation Set`, `Female Animation Set` (빌더가 연결). 다시 만들려면 `SubTerra/MVP2/Build Prompt-B 208 Player Animation Sets`.
- 가이드 스킨은 `SubTerra/UI/Build Prompt-B 140 Game Guide Skin`으로 다시 만들면 여자 키가 포함된다.
- 씬 재배선 없음(`IntegrationRuntimeBinder` 참조 변경 없음, 브리지는 런타임에 붙음).

## 완료 체크리스트
- [x] `MVP2-fix152` push 확인
- [x] 커밋 해시 기재
- [x] Unity 컴파일 오류 없음
- [x] EditMode 새 실패 없음(실행 묶음 기준. 기존 5개 + 환경 의존 1개, 위 설명)
- [x] 여자 장수 = 표(빠진 장 복제 없음)
- [x] webm·mov 미커밋
- [x] 사다리 초록 점 제거, 한 연번에서만
- [x] 남자 재생 결과 불변(테스트 기준)
- [x] 짧은 세트에서 끊김·범위 초과 없음(테스트 기준, 화면 확인은 미검증)
- [x] 낙하 루프·숨쉬기 루프 동작(테스트 기준)
- [x] 옛 저장·새 게임 = 남자
- [x] 성별 저장·이어하기 유지(저장 파일 `"gender": 1` 확인, 메뉴 왕복 화면은 미검증)
- [x] `gender male/female` 즉시 적용(플레이어: 플레이 확인, 가이드: 테스트)
- [x] 가이드에 남녀 그림 안 섞임
- [x] 성별 선택 화면 안 만듦(B-209)
- [x] 새 SFX 없음
- [x] 남자 원본 미수정
- [x] Gameplay→App 참조 없음
- [x] 프리팹 YAML 손수정 없음
- [x] main 직접 커밋 없음
- [x] PR: 문서는 미생성이었으나 사용자 요청으로 생성
- [x] 브랜치 삭제 안 함
- [x] #266 미수정

## 커밋
- 기능: `b52f727c` feat: 여자 캐릭터 모션 세트와 성별 저장·디버그 명령 추가 (프롬프트 B-208)
