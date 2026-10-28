# B-171 광부 걷기 40프레임 교체 (PR #242 이어서)

## 목표
- 플레이어 걷기 그림을 사용자가 준 새 40장(오른쪽으로 걷기 기준)으로 바꾼다. 1 → 40 → 다시 1로 끊김 없이 무한 반복. 왼쪽으로 걸을 때는 지금처럼 좌우 반전만 한다.
- 새 브랜치를 만들지 않는다. **PR #242의 브랜치 `codex/player-walk-two-leg-cycle`에서 이어서** 작업하고 같은 브랜치에 푸시해 #242를 갱신한다.
- 지금 #242 상태(04dd38f0): 팀원 GIF 47장(256×256, 10fps, 4.7초 반복)이 걷기·게임 가이드 데모에 쓰인다. 이번에 이 47장을 새 40장으로 바꾼다.
- 필요하면 다른 에이전트(Codex 등)에게 나눠 맡겨도 된다. 결과는 #242 하나로 모은다.

## 0. 먼저: main 충돌 풀기
#242는 지금 `init/rules/lessons.md` 하나 때문에 main과 충돌한다(PM이 fd6cb2c4와 시험 병합: 충돌 파일은 이것 하나, `PlayerAnimationController.cs`는 자동 병합됨).
1. `git fetch origin` → `codex/player-walk-two-leg-cycle` 체크아웃 → `git merge origin/main`. **리베이스·force push 금지**(이미 푸시된 브랜치).
2. `init/rules/lessons.md`는 **main 버전을 그대로** 쓴다.
3. #242가 그 표에 넣은 한 줄(2026-10-26 "걷기에서 바지 밑단과 허벅지가 잘라 붙인 것처럼 보임 | 고정 몸통 마스크가 … | 공통 접합선과 회전 보간을 사용하고 모든 자세의 무릎을 확대 비교한다")은 표에 다시 넣지 말고, main의 새 방식대로 `init/rules/lessons/` 폴더에 파일 하나로 옮긴다.
   - 예: `init/rules/lessons/2026-10-26-pr242-walk-knee-seam.md`. 양식은 같은 폴더의 `2026-10-28-issue260-surface-settings-overlay.md`를 따른다(제목, 날짜, 태그, 실수, 원인, 다음부터 할 일, 관련: #242). 같은 내용이 이미 있으면 만들지 않는다.
   - 참고: 개별 파일 방식은 #261 제안이고, `AGENTS.md`·`init/rules/rule.md`는 아직 "lessons.md에 한 줄 추가"라고 적혀 있다. 이번 작업에서 그 문서들은 고치지 않는다.
4. 예상과 다른 충돌이 더 나오면 각 파일을 어떻게 풀었는지 보고서에 적는다. 판단이 어려우면 멈추고 보고한다.
5. 병합 커밋 뒤 Unity 컴파일 오류가 없는지 확인하고 나서 아래 걷기 작업을 시작한다.

## 절대 규칙
1. 걷기 말고 다른 애니메이션(대기, 점프, 사다리, 채굴, 피격, 기절)과 그 그림·속도·전환 규칙은 건드리지 않는다.
2. 그림을 새로 그리거나 부위별로 고치지 않는다. 40장 전체를 **같은 배율·같은 위치 이동**으로만 맞춘다(프레임마다 따로 늘리거나 옮기지 않음).
3. 이동 속도, 점프, 사다리, 좌우 반전 코드(`PlayerFacing`)는 바꾸지 않는다.
4. 줄 번호는 #242(04dd38f0)에 main(fd6cb2c4)을 병합한 뒤 기준 근사(~). 발소리 코드는 main(B-164)에서 들어온다. 못 찾으면 "못 찾음"으로 보고한다.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/` 기준)
- Scripts/Gameplay/Player/PlayerAnimationController.cs: `WalkFrameCount` 47·`WalkFrameRate` 10(맨 위), `footstepFrames` 기본값 {2, 7}(~35), `ResolveFrames`의 Walk(~250), `Play`(프레임 = 경과시간 × fps, 반복은 나머지 연산), `TryPlayFootstep`
- Scripts/Gameplay/Player/PlayerFacing.cs(~26: `visualRoot.localScale.x` 부호로 좌우 반전), PlayerMovement.cs(`moveSpeed` 6, `CurrentSpeedMultiplier` ~86)
- Prefabs/Gameplay/Player/Player.prefab(`walkFrames` ~356, 그림 오브젝트 크기 0.95)
- Editor/RedrawnWalkCycleBuilder.cs(`Approved` 입력 폴더 → `Art/Characters/Player/Frames/Walk/walk_NN.png`), Editor/PlayerAnimationAssetBuilder.cs, Art/Characters/Player/Animations/PlayerWalk.anim
- 가이드 데모: Scripts/App/UI/Guide/GuideDemoLibrary.cs(`PlayerWalkFrameCount`·`PlayerWalkFrameRate`), Resources/UI/GameGuideSkin.asset(`player.walk.1~47`), Editor/DataValidation/PromptB140GameGuideSkinBuilder.cs, Scripts/App/UI/ControlTerminal/ControlTerminalDemoClips.cs
- Tests/EditMode/App/Readiness/PlayerWalkCycleTests.cs(47장·10fps·발 기준선 14~18px·키 218~236px 검사)
- `work_process/images/teammate-walk-gif-20261027/README.md`(지난번 정렬 방식), `work_process/player-walk-teammate-gif-20261027.md`

## 1. 새 그림 넣기
- 사용자가 받은 압축을 풀어 `work_process/images/player-walk-40f-20261028/source/1.png ~ 40.png`에 둔다(파일 이름 그대로). 없으면 멈추고 보고한다.
- PM이 잰 원본 정보: 40장 모두 960×960 RGBA, 배경 투명(몸 안쪽 알파 254), 빠진 번호 없음, 모두 오른쪽을 향함.
  - 캐릭터 키 723~750px, 발바닥 아래 끝 y=841~849(위에서), 가로 중심 491~502. 프레임 사이 차이(발 8px, 중심 10px)는 걸음의 자연스러운 흔들림이라 고치지 않는다.
- 게임용 256×256 PNG를 만드는 스크립트를 `work_process/tools/`에 두고 `runtime-frames/walk-01.png ~ walk-40.png`로 출력한다.
  - 배율은 40장 공통 하나(약 0.307). 40장 평균 키가 지금 걷기(약 227px)와 같고, 발 기준선은 아래에서 약 16px, 가로 중심은 128이 되게 한다.
  - 줄일 때 가장자리에 어두운 테두리가 생기지 않게 알파를 미리 곱해서 줄인다(premultiplied). 몸 안쪽 알파 254는 255로 올려도 된다.
  - 결과가 `PlayerWalkCycleTests`의 범위(발 14~18, 키 218~236, 좌우 잘림 없음)에 들어가는지 스크립트에서 먼저 확인한다.
- `RedrawnWalkCycleBuilder`의 입력 폴더를 새 폴더로 바꿔 `walk_01~40.png`를 쓴다.
  - **walk_01~40의 .meta(GUID)는 그대로 두고** 그림만 바꾼다. 그래야 프리팹·가이드 참조가 유지된다.
  - walk_41~47.png와 .meta는 지운다. 남은 참조가 없는지 확인한다.
- 가져오기 설정은 지금 걷기 그림과 같게 한다: Sprite(Single), PPU 256, 피벗 가운데(0.5, 0.5), Bilinear(지금 값. Point로 바꾸지 않음), 밉맵 없음, 기본 압축 없음(지금 값 그대로).

## 2. 재생 설정
- `WalkFrameCount` 40, `GuideDemoLibrary.PlayerWalkFrameCount` 40. `walkFrames` 배열 40개, `GameGuideSkin`의 `player.walk.1~40`(41~47 삭제), `PlayerWalk.anim` 40키(반복 켬).
- 속도: `WalkFrameRate` **40fps**(한 바퀴 1.0초, 두 걸음). 가이드 데모도 같은 값.
  - 근거: 미끄러지지 않게 하려면 한 바퀴에 몸이 실제 걸음 폭(약 0.8~0.95칸)만큼 가야 하는데, 이동 속도가 초당 6칸이라 초당 약 260~300장이 필요하다. 그래서 발 미끄러짐을 완전히 없애는 것은 불가능하다.
  - 지금 main의 걷기(10장/10fps)와 #242 앞선 시안이 쓰던 "1초에 한 바퀴"를 유지해 느낌을 맞춘다. 60fps(0.67초)는 PM이 고를 수 있는 다른 값이다. 결정값은 코드 상수 하나로 둔다.
- 반복: 40번 다음 1번으로 바로 이어진다(같은 그림 두 번 없음). 걷기를 멈췄다 다시 걸으면 1번부터 시작한다(지금 규칙 그대로).
- 왼쪽: `PlayerFacing`의 기존 반전을 그대로 쓴다. 반전 때 발 위치가 옆으로 튀지 않는지 확인한다(가로 중심 128 정렬).

## 3. 발소리
- 병합 뒤 `footstepFrames`는 {2, 7}(main B-164, 10장 기준)이다. 47장 그대로면 4.7초에 두 번만 난다.
- 새 40장에서 발이 땅에 닿는 프레임으로 바꾼다. PM이 잰 값으로는 두 발이 가장 벌어지는 2~3번과 21~22번(1부터 셈)이 착지다. 그래서 시작값은 **{1, 21}(0부터 셈)**.
  - 앞발이 처음 땅에 닿는 그림을 직접 보고 ±1 안에서 맞춘다.
- 코드 기본값과 프리팹에 같은 값을 넣는다. 발소리 음원·음량·번갈아 피치는 바꾸지 않는다.

## 손대지 말 것
- 다른 상태 그림과 속도, 이동·점프·사다리 규칙, 좌우 반전 방식, 발소리 음원, 다른 PR, `AGENTS.md`·규칙 문서, `work_process`의 예전 기록(teammate-walk 폴더 포함)

## 테스트
- EditMode 전체에서 main의 기존 실패 말고 새 실패가 없어야 한다.
- `PlayerWalkCycleTests`를 40장 기준으로 고친다.
  - 40장 순서대로 재생, 40fps, 한 바퀴 1.0초, 40번 다음 1번
  - 프리팹 `walkFrames` 40개 모두 다른 그림, `player.walk.41`은 없음
  - 가이드와 플레이어의 장수·fps가 같음
  - 발 기준선·키·잘림 범위
  - 발소리가 한 바퀴에 2번 정해진 프레임에서 남, 방향을 바꿔도 같은 프레임에서 남
- PlayMode는 이번에 고친 테스트만 돌린다. 전체 실행 전후 `git status`로 범위 밖 프리팹·폰트·씬·PNG 변경을 확인하고 되돌린다.

## 수동 확인 체크리스트 (보고서에 그대로 넣기)
- [ ] 오른쪽으로 걸으면 새 40장 걷기가 부드럽게 반복되고, 40번에서 1번으로 넘어갈 때 튀지 않는다.
- [ ] 왼쪽으로 걸으면 같은 동작이 좌우만 뒤집혀 나오고, 방향을 바꿀 때 몸이 옆으로 튀지 않는다.
- [ ] 캐릭터 크기와 발 높이가 대기·점프·사다리 그림과 비슷해서 상태가 바뀔 때 커지거나 떠 보이지 않는다.
- [ ] 걷다가 멈추면 바로 대기 그림이 되고, 다시 걸으면 처음부터 걷는다.
- [ ] 걷다가 점프, 사다리 타기, 채굴로 바뀌어도 지금처럼 바뀐다.
- [ ] 발소리가 발이 땅에 닿는 순간에 한 걸음에 한 번 난다.
- [ ] 화물을 많이 싣거나 가스 등으로 느려질 때 걸음이 너무 빨라 보이는지 적는다(PM 판단용).
- [ ] 게임 가이드·관제 터미널 데모의 걷기도 새 그림으로 나온다.
- [ ] 캐릭터 가장자리에 검은 테두리나 깨진 픽셀이 없다(밝은 지상, 어두운 광산 둘 다).
- [ ] Console에 새 경고·오류가 없다.

## 완료 후
- 커밋은 한국어 conventional 형식. 병합 커밋과 걷기 커밋을 나눈다. 같은 브랜치에 일반 push(force 금지)하고, 브랜치는 지우지 않는다.
- PR #242 본문은 고치지 않고, 결과 요약 코멘트를 하나 남긴다(PM이 본문을 고침).
- 결과 보고서 `work_process/MVP2/B-171-walk-40f-result.md`에 적을 것
  - 병합 기준 main 커밋, 충돌 파일과 해결 방법, 옮긴 교훈 파일 경로
  - 정렬 배율·위치 값, 40장 측정표(키·발·중심)
  - fps와 발소리 프레임을 고른 근거, 테스트 결과
  - 걷는 화면 GIF 또는 프레임 비교 이미지
  - 직접 보지 못한 항목은 "미검증"
  - 위 수동 확인 체크리스트
