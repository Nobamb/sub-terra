# B-175 점프 동작 새 그림 교체 (이슈 #264)

## 기준과 범위

- 기준 브랜치: `MVP2-fix119` `89d3eac7`(B-173 교훈 분리 + B-174 사다리 채굴 포함). 작업 브랜치: `MVP2-fix120`.
- 점프 물리(점프힘 11, 중력 ×3, 질량 1), 코요테 0.1초, 버퍼 0.1초, 공중 이동, 효과음 시점, 다른 동작 그림·속도는 바꾸지 않았다.
- `PlayerMovement`에는 그림 계산용 읽기 값 3개(`VerticalSpeed`, `JumpCount`, `LastJumpLaunchSpeed`)만 추가했다. `ApplyJumpImpulse`의 힘·소리 코드는 그대로다.
- 원본 그림은 다시 그리거나 부위별로 고치지 않았다. 한 배율·한 이동량만 적용했다.

## 1. 그림 정렬과 실측

재현: `python work_process/tools/prepare_b175_frames.py` 후 Unity 메뉴 `SubTerra/Player/Build B-175 Jump`.

| 항목 | 값 |
| --- | --- |
| 원본 | ready 8, processing 24, fall 24. 모두 960×960 RGBA, 번호 빠짐 없음 |
| 배율 | 0.267 → 실제 래스터 256/960 = 0.26667 |
| 공통 이동량(56장 모두) | x +1, y +1px |
| 출력 | 256×256, PPU 256, 피벗 (0.5, 0.5), Bilinear, 밉맵 없음, 무압축, 미리 곱한 알파(RGBa) 축소 |
| PNG 쓰기 | 정렬 스크립트·빌더 모두 임시 파일 후 원자적 교체, 같은 바이트는 건너뜀 |
| ready 1 발끝(위에서) | y241 — 대기 그림 y241과 같음 |
| ready 1 키 | 224px (대기 226px) |
| ready 1 헬멧 폭 | 77px — 대기 77px과 같음(0%) |
| ready 1 무게중심 x | 125.77 (대기 125.55), 외곽 중심 128.5 |
| 대기 대비 실루엣 차이 | 4.6% |
| 전체 범위 | 위 12, 아래 251, 가로 45~222 |
| 묶음별 최소 여백 | ready 13px, rise 11px, fall 5px(fall 15 발끝). 잘린 프레임 없음 |

**지시와 다르게 한 점 — 가로 기준:** 지시대로 무게중심(원본 x≈469)을 128에 맞추면 이동량이 x +3이 되어, 무게중심이 125.55인 대기 그림보다 몸이 2px 오른쪽에 놓인다. 대기↔점프 전환 때 옆으로 튀어 보이지 않게(사용자 테스트 10번) 무게중심을 대기 그림에 맞췄다(x +1). 외곽 중심은 128.5라 "128 근처"는 유지된다. x +3이 맞다면 스크립트의 `dx` 한 줄만 바꾸면 된다.

**예상 범위와 차이:** 아래 끝이 예상(≤250)보다 1px 큰 251(fall 15). 캔버스 아래 여백은 5px 남아 잘림은 없다.

- [56장 밝은·어두운 배경 비교](../images/player-jump-20261029/comparison-contact-sheet.png)
- [대기·단계별 비교(빨간 선 = y241)](../images/player-jump-20261029/idle-comparison.png)
- [재생 순서 미리보기 GIF](../images/player-jump-20261029/runtime-preview.gif) — 상승 1~24, fall 1~15, fall 18~24 두 번, ready 8→1 순서 확인용(42ms 일정 간격, 실제 단계별 속도는 아래 표)
- [전체 프레임 좌표·해시·출렁임 표](../images/player-jump-20261029/measurements.json)

게임 파일: `Art/Characters/Player/Frames/JumpReady/jump_ready_01~08.png`, `JumpRise/jump_rise_01~24.png`, `JumpFall/jump_fall_01~24.png`. `Player.prefab`에 `jumpReadyFrames`(8)·`jumpRiseFrames`(24)·`jumpFallFrames`(24)를 Editor API로 연결했다.

**기존 `Frames/Jump/` 10장:** 파일은 지우지 않았고 프리팹 `jumpFrames` 참조는 비웠다. 다른 사용처: `Art/Characters/Player/Animations/PlayerJump.anim`(비활성 Animator 미리보기 클립)과 `PlayerAnimationAssetBuilder`의 `Jump` 정의가 아직 이 10장을 가리킨다. 런타임 표시에는 쓰이지 않으며 범위 밖이라 손대지 않았다. `jumpFrames`는 새 3묶음이 비어 있을 때만 쓰는 예비 경로로 남겼다(`ConfigureFrames`를 쓰는 기존 검사 호환).

## 2. 단계 상태 구조

- `PlayerJumpAnimationSequence`(Unity와 분리된 순서 클래스): 단계 `None / TakeOff / Rise / Apex / FallLoop / Land`, 출력은 `Clip`(Ready/Rise/Fall)과 `FrameIndex`.
- 입력: `StartJump(시작 위 속도)`, `Tick(시간, 공중 여부, 세로 속도, 이동 입력)`, `Reset()`.
- 알림: `PhaseChanged(이전, 다음)` 하나. `PlayerAnimationController.JumpAnimation`으로 접근해 #265 효과(착지 먼지 등)가 구독할 수 있다.
- 점프 시작은 `PlayerMovement.JumpCount` 변화로 본다. 착지와 버퍼 점프가 같은 물리 틱에 처리되어 공중 상태가 끊기지 않아도 새 점프를 구분한다.
- 설정 `PlayerJumpAnimationSettings`(프리팹 `jumpAnimation`):

| 필드 | 기본값 | 뜻 |
| --- | --- | --- |
| takeOffLastFrame / takeOffFps | 5 / 40 | processing 1→5, 0.125초 |
| riseLastFrame | 24 | 상승 진행도로 6→24 |
| apexLastFrame / apexFps | 15 / 40 | fall 1→15 한 번, 0.375초 |
| fallLoopFirstFrame / fallLoopLastFrame / fallLoopFps | 18 / 24 / 12 | fall 18→24 반복 |
| landFirstFrame / landFps | 8 / 30 | ready 8→1, 약 0.267초 |
| fallBobCompensation | 0(끔) | 0~1, fall 1~15 출렁임 보정 비율 |
| fallBobOffsets | 0, 19, 32, 38, 39, 40, 39, 37, 34, 30, 24, 17, 8, -1, -8 | fall 1~15 발 위치 차이(게임 px, 아래로 +). 정렬 스크립트가 측정 |

## 3. 재생 규칙

| 상황 | 처리 |
| --- | --- |
| 점프 키 | 물리는 그대로 즉시. 같은 화면 프레임에 processing 1 표시, 1→5 40fps. ready는 점프 전에 틀지 않음 |
| 상승 | 진행도 = 1 − 현재 위 속도 ÷ 시작 위 속도(0~1, 줄어들지 않음) |
| 도약 1~5가 끝난 뒤 | 그 시점의 진행도부터 정점까지를 6~24에 나눠 **6번부터 이어 감** |
| 정점(위 속도 ≤ 0) | fall 1→15 40fps 한 번 → fall 18→24 12fps 반복 |
| 걸어서 떨어짐(코요테 포함) | processing 없이 fall 1부터. 코요테 점프면 processing 1로 바꿈 |
| 착지 | ready 8→1 30fps 후 대기. 이동 입력이면 바로 걷기, 점프 입력이면 바로 processing 1 |
| 착지 틱 버퍼 점프 | 착지 그림 없이 바로 processing 1 |
| 다침 | 지금 우선순위대로 덮음. 공중 단계는 뒤에서 계속 진행 |
| 쓰러짐·채굴·사다리 채굴·사다리 | 덮고 점프 단계를 초기화 |
| 사다리 위에서 내려오기 직전 | 이전 점프 그림 대신 fall 18→24 12fps 반복 |
| 걷기 접지 유예(0.06초) | 그대로. 유예 안의 짧은 공중은 fall로 바꾸지 않음 |
| 좌우 반전 | `PlayerFacing` 그대로(모든 점프 그림 반전) |

**지시를 해석한 점 두 가지:**
1. 지시의 진행도 식을 그대로 6~24에 대입하면 도약이 끝나는 0.125초에 진행도가 이미 약 0.33이라 5번에서 12번으로 건너뛴다. "1~5가 끝난 뒤 이어 간다"를 6번부터 이어 가는 뜻으로 보고, 남은 진행도를 6~24에 나눴다. 정점에서 24, 되돌아가지 않음, 낮은 점프도 24는 그대로 지킨다.
2. 60fps 화면에서 24번 구간이 한 프레임보다 짧아 건너뛸 수 있어서, 정점에 닿았을 때 24를 아직 못 보였으면 한 프레임 보인 뒤 fall 1로 넘어간다.

## 4. 가이드·관제 터미널

`player.jump.4` 정지 그림을 `JumpRise/jump_rise_12.png`(다리를 접고 오르는 장면)로 바꿨다(`PromptB140GameGuideSkinBuilder` 경로 한 줄 → `GameGuideSkin.asset` 재생성). 새 그림도 같은 256 캔버스·발 기준이라 데모의 크기·위치 값은 그대로 맞는다. 다른 데모는 손대지 않았다.

## 효과음 시점

- `PlayerSurvivalController.TrackFall`(착지음)과 `PlayerMoveSfxGate`는 바꾸지 않았다. `ApplyJumpImpulse`의 `SfxRequests.Play(moveSfx.ResolveJump())` 줄도 그대로다.
- 그림 단계는 소리를 내거나 소리 시점을 옮기지 않는다.
- EditMode의 효과음 검사(`PromptB164SfxD1Tests` 등)는 전체 실행에서 통과했다. 실제 플레이에서 귀로 확인한 것은 아니다(미검증).

## 테스트 결과

| 실행 | 결과 |
| --- | --- |
| EditMode 전체(Editor 열린 상태, 플래그 실행) | 2227 통과 / 24 실패. 실패 24개는 B-174 기준 실패 목록(`player-ladder-mining-24f-20261029/baseline-failures.txt`)과 이름까지 같음. 새 실패 없음 |
| 플레이어 동작 묶음 EditMode | 75/75 통과 |
| 새 `PlayerJumpAnimationSequenceTests` | 10/10 통과 — 전체 순서(1~5 40fps → 6~24 진행도·되돌아가지 않음 → fall 1~15 → 18~24 반복 → ready 8→1 → 끝, 단계 알림 순서), 낮은 점프 11/8.25/5.5 및 60·120fps에서 24 도달, 도약 중 정점, 걸어서 떨어짐 fall 1·코요테 점프, 착지 중 이동·점프 끊김, 버퍼 점프 착지 생략, 출렁임 보정 기본 끔 |
| 새 `PlayerJumpArtTests` | 5/5 통과 — 프리팹 배열 8·24·24, 원본과 바이트 일치, 모두 256×256·서로 다름·여백 ≥2px, 가져오기 설정, ready 1 발끝 241±2, 헬멧 폭 대기 ±5%, 이전 10장 파일 유지·참조 끊김, 가이드 `player.jump.4` |
| 고친 `PlayerWalkCycleTests` 접지 누락 검사 | 통과 — 걸어서 떨어지면 fall 1, 점프면 processing 1, 착지 ready 8, 착지 중 이동이면 Walk |
| PlayMode `PlayerMovementPlayModeTests` + `PlayerRedrawnActionPlayModeTests` + `LadderBackBootstrapPlayModeTests` (`SubTerra/Tests/Run Prompt-B 175 Jump PlayMode Tests`) | 37 통과 / 1 실패 / 1 건너뜀. 점프 물리·코요테·버퍼·화물 75%·사다리 점프 검사 모두 통과 |
| 새 PlayMode `PromptB175_RealJump_PlaysTakeOffRiseApexFallThenLanding` | 통과 — 실제 물리 점프에서 키 직후 processing 1, 상승 그림 단조 증가 후 24, fall 1, 착지 ready 8, 대기 복귀 |
| PlayMode 실패 1건 | `LadderAnimation_ActiveTriggerContact_PrioritizesLadderFrames` — B-164부터 `ExitLadderMode(bool silent = false)`인데 검사가 인자 없이 리플렉션 호출해 `TargetParameterCountException`. 이번 변경과 무관한 기존 실패라 고치지 않음 |
| 건너뜀 1건 | `Bootstrap_NewGame_ClimbUsesImportedBackFramesOnly` — `-subterra-save-root` 격리 경로 필요 |

검사 실행 전후 `git status`로 확인한 범위 밖 변경(폰트 SDF, `InventoryPanel`·`MainMenuPanel`·`ViewSocket` 프리팹, `Mine_Demo_Integration.unity`, ProjectSettings 3개, 메인 메뉴 스크린샷 2장)은 전체 검사 실행의 부작용이라 모두 `git restore`로 되돌렸다.

## 미검증

- 실제 게임(Bootstrap → 광산)에서의 플레이 확인 전체: 아래 사용자 테스트 가이드 1~13번은 직접 보지 못했다.
- 특히 화물로 낮아진 점프의 실제 모습, 사다리 꼭대기 내려오기·사다리 점프 이탈의 그림 이음새, 관제 터미널 데모 화면, Console 경고 유무.
- fall 1~15 출렁임(그림 안에서 몸이 최대 40px 위로 갔다 내려옴)이 실제 화면에서 어색한지. 어색하면 `fallBobCompensation`을 0.5~1로 올려 보면 된다.

## 사용자 테스트 가이드

디버그 터미널: 플레이 중 **Ctrl + `**(1 왼쪽 키).
1. 평지에서 점프 → 키를 누르는 순간 바로 뜬다(웅크렸다 늦게 뜨는 느낌 없음). 펴며 뛰고, 다리를 접고 오르고, 정점에서 떨어지는 자세로 바뀐다.
2. 착지 → 웅크렸다 일어서고 대기 자세로 자연스럽게 이어진다. 착지 소리는 전과 같은 때 난다.
3. 높은 데서 걸어서 떨어진다 → 처음부터 떨어지는 자세로 나오고, 오래 떨어지면 같은 자세가 끊김 없이 반복된다.
4. 발판 끝에서 떨어지자마자(아주 짧은 순간) 점프 → 정상 점프 동작이 나온다.
5. 착지 직전에 점프 키를 미리 눌러 둔다 → 착지 동작 없이 바로 다시 뛴다.
6. 착지 동작 중 좌우 키 → 바로 걷기. 점프 키 → 바로 다시 뛴다.
7. 좌우로 방향을 바꾸며 점프 → 모든 단계에서 몸이 진행 방향을 본다.
8. 화물을 많이 실어 점프가 낮아진 상태로 점프 → 정점에서 떨어지는 자세로 정확히 바뀐다(화물 명령이 있으면 사용).
9. 사다리를 타다 점프로 떼어 낸다 / 사다리 꼭대기에서 내려온다 → 그림이 튀거나 크기가 바뀌지 않는다.
10. 대기 ↔ 점프 ↔ 걷기를 오갈 때 캐릭터 크기·발 위치가 순간 커지거나 떠 보이지 않는다.
11. 공중에서 다치기(`hp` 명령 후 낙하 피해 등) → 다침 동작이 바로 덮는다. 사다리 채굴·바닥 채굴은 전과 같다.
12. 관제 터미널 점프 키 데모 그림이 새 그림이다.
13. Unity Console에 새 빨간 오류·노란 경고가 없다(기존 URP stereoTargetEye, obsolete, 라이선스 로그 제외).

## 변경 파일

- 새: `Scripts/Gameplay/Player/PlayerJumpAnimationSequence.cs`, `Editor/JumpAnimationBuilder.cs`, `Editor/DataValidation/PromptB175JumpTestRunner.cs`, `Tests/EditMode/App/Readiness/PlayerJumpAnimationSequenceTests.cs`, `PlayerJumpArtTests.cs`, 그림 56장(`JumpReady`·`JumpRise`·`JumpFall`), `work_process/tools/prepare_b175_frames.py`, `work_process/images/player-jump-20261029/*`
- 수정: `PlayerAnimationController.cs`, `PlayerMovement.cs`(읽기 값만), `Player.prefab`(점프 배열·설정), `PromptB140GameGuideSkinBuilder.cs`·`GameGuideSkin.asset`(`player.jump.4`), `PlayerWalkCycleTests.cs`, `PlayerMovementPlayModeTests.cs`(새 검사 1개)
- 교훈: `init/rules/lessons/2026-10-29-issue264-jump-landing-spawn.md`
