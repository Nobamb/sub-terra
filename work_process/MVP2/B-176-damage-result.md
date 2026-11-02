# B-176 다침 동작 4장 → 8장 결과 (이슈 #264)

## 기준
- 기준 브랜치 `MVP2-fix120` `1e5c6c3a`(B-175 점프 커밋 포함 확인) → 작업 브랜치 `MVP2-fix121`.
- 재현: `python work_process/tools/prepare_b176_frames.py` 후 Unity 메뉴 `SubTerra/Player/Build B-176 Damage`.

## 1. 새 그림 4장
- 원본 4장 1254×1254 RGBA 확인. 알파 8 미만 제거 → 미리 곱한 알파 LANCZOS 축소 → 축소 뒤 남은 알파 1~7 다시 제거 → 무게중심 128 + 이동, 발끝 y242.
- 게임 파일 `Frames/Damage/damage_1_2.png`, `damage_2_3.png`, `damage_3_4.png`, `damage_4_idle.png`: 256×256, PPU 256, 피벗 가운데, Bilinear, 밉맵·압축 없음(기존 damage 메타와 같은 값). 임시 파일 후 원자적 교체.
- 기존 `damage_01~04.png`·메타는 바뀌지 않음(git diff 없음, GUID 검사 통과).

| 새 그림 | 배율 | 가로 이동 | 실제 이동(px) | 키 | 발끝 | 무게중심 x | 여백 좌/위/우/아래 | 겹침 앞/뒤 (평균) |
|---|---|---|---|---|---|---|---|---|
| 1-2 | 0.209 | −8 | (−18, 2) | 220 | 242 | 120.40 | 55/23/58/13 | 0.642 / 0.869 (0.76) |
| 2-3 | 0.211 | −9 | (−15, 5) | 209 | 242 | 119.30 | 47/34/44/13 | 0.793 / 0.875 (0.83) |
| 3-4 | 0.213 | −7 | (−11, 4) | 211 | 242 | 121.21 | 51/32/44/13 | 0.795 / 0.555 (0.68) |
| 4-idle | 0.229 | 0 | (−18, −10) | 221 | 242 | 128.34 | 56/22/50/13 | 0.850 / 0.804 (0.83) |

- 앞뒤 기존 그림: 01 키 228 / 02 220 / 03 206 / 04 219, 발끝 모두 242. 잘림 없음(여백 ≥ 13px), 알파 1~7 픽셀 0개, 4장 서로 다름.
- 겹침 앞/뒤 평균이 PM 측정(0.75 / 0.83 / 0.67 / 0.83)과 맞음. 4-idle의 "뒤"는 대기 그림.
- 장 사이 실루엣 차이: 01→1-2 0.446, 1-2→02 0.144, 02→2-3 0.230, 2-3→03 0.140, 03→3-4 0.229, **3-4→04 0.613**, 04→4-idle 0.170, 4-idle→대기 0.213. 지시대로 1→1-2, 3-4→4 큰 구간은 그대로 둠.
- 비교 이미지: [8장 비교(빨간 선 y242, 파란 선 x128)](../images/player-damage-8f-20261029/damage-8f-comparison.png), [밝은/어두운 배경 시트](../images/player-damage-8f-20261029/comparison-contact-sheet.png), [재생 GIF(50ms, 끝에 대기)](../images/player-damage-8f-20261029/runtime-preview.gif), 수치 `measurements.json`.

## 2. 재생
- `Player.prefab` `damageFrames` 8장: 01, 1_2, 02, 2_3, 03, 3_4, 04, 4_idle. 새 직렬화 값 `damageFps = 20`(Editor API로 설정).
- `PlayerAnimationController.DamageDuration` 0.35 → **0.4**. 20fps 한 번, 반복 없음. 4-idle은 0.35~0.4초 구간(0.05초) 표시.
- 다침 중 입력·물리·점프 단계 진행은 그대로(막지 않음). 다침이 끝나면 그 순간 상태로 바로 전환.
- 다침 중 다시 맞으면: 기존과 같이 `damageUntil`만 늘고 `stateStartedAt`은 그대로라 그림을 처음부터 다시 틀지 않음(검사로 확인). 이때 남은 시간은 마지막 4-idle 장 유지.
- 효과음·붉은 깜빡임·화면 흔들림: `PlayerSurvivalController`·`PlayerSurvivalSettings`를 수정하지 않았음. 시점·길이 변화 없음(코드 기준).

## 3. 사다리 위 다침 좌우 반전
- 재현: 수정 전 EditMode 검사 "사다리 + 왼쪽 입력 + 다침"에서 VisualRoot `localScale.x = -0.95` → **재현됨**. 원인은 추정대로 다침 그림이 사다리 그림을 덮어 `IsShowingLadderVisual`이 false가 되면서 `PlayerFacing`이 이동 방향을 쓴 것.
- 고침:
  - `PlayerAnimationController.IsShowingLadderDamageVisual`(다침 중 + 사다리 잡음), `LadderDamageDirection` 추가. 맞는 순간 사다리 채굴 동작이 진행 중이면 그 방향, 아니면 오른쪽(1)을 기록.
  - `PlayerFacing.LateUpdate`: 사다리 채굴 → 사다리 다침 → 사다리 그림 → 이동 방향 순서로 방향 결정.
  - "사다리 잡음" 조건은 기존 `ResolveStateName`·`UpdateJumpSequence`의 식을 `IsHoldingLadder` 하나로 묶어 같이 씀(동작 동일).
- 땅·공중 다침은 기존대로 이동 방향. 다침 끝나 사다리 그림/사다리 채굴로 돌아올 때 방향이 같아 튀지 않음(검사로 확인).

## 4. 함께 확인
- `PlayerAnimationAssetBuilder`: `Damage` 정의 4장 10fps → 8장 20fps, 프레임 이름 표 `DamageFrameNames` 추가. `PlayerDamage.anim` 미리보기 클립을 8장 20fps(0.05 간격)로 다시 만듦. 런타임 영향 없음.
- 가이드 `player.damage.1`(`GameGuideSkin`)은 손대지 않음(변경 없음).
- 다침 0.35에 기대는 코드·검사: `PlayerLadderMiningTests.Damage_ResumesThirteen_KnockoutInterrupts`는 `damageUntil`을 직접 넣어 시간과 무관하게 통과. 채굴 재개 코드도 `damageUntil` 경과만 봄. 그 밖에 0.35를 쓰는 다침 코드 없음.

## 바뀐 파일
- `Scripts/Gameplay/Player/PlayerAnimationController.cs`, `PlayerFacing.cs`
- `Editor/DamageAnimationBuilder.cs`(새), `Editor/PlayerAnimationAssetBuilder.cs`
- `Prefabs/Gameplay/Player/Player.prefab`(damageFrames 8, damageFps), `Art/Characters/Player/Animations/PlayerDamage.anim`
- `Art/Characters/Player/Frames/Damage/damage_1_2·2_3·3_4·4_idle.png`(+meta)
- `Tests/EditMode/App/Readiness/PlayerDamageAnimationTests.cs`(새)
- `work_process/tools/prepare_b176_frames.py`, `work_process/images/player-damage-8f-20261029/*`

## 테스트
| 항목 | 결과 |
|---|---|
| 새 `PlayerDamageAnimationTests` | 13/13 통과 — 프리팹 8장·순서·기존 GUID·새 4장 256×256/서로 다름/발끝 242±2/알파 1~7 0개/원본 바이트 일치/가져오기 설정, 0.05초 간격 8장·0.4초 뒤 끝, 다시 맞아도 처음부터 안 틂, 끝난 뒤 대기·걷기·점프·사다리·채굴 이어 가기, 사다리 다침 왼쪽 입력 반전 없음·사다리 그림 복귀, 사다리 채굴(왼쪽) 반전 유지·채굴 재개, 땅 다침 이동 방향(±1), 쓰러짐이 다침 덮음 |
| 플레이어 관련 기존 묶음(PlayerLadderMining·Redrawn·WalkCycle·Jump·MiningSequence 등) | 모두 통과(묶음 88개 중 새 검사 3개 첫 실행 실패 → 아래 교훈, 수정 후 통과) |
| EditMode 전체(에디터 열린 상태, 플래그 실행) | 2240 통과 / 24 실패. 실패 24개는 B-174 기준 목록(`player-ladder-mining-24f-20261029/baseline-failures.txt`)과 이름까지 같음. **새 실패 없음** |
| PlayMode | 고친 PlayMode 검사가 없어 실행하지 않음 |
| 범위 확인 | 전체 실행 뒤 범위 밖 변경(InventoryPanel·MainMenuPanel·ViewSocket 프리팹, Mine_Demo_Integration.unity, TimeManager.asset, 메인 메뉴 PNG 2장)을 `git restore`로 되돌림. 작업 전부터 있던 `init/prompt-B.md`·폰트·ProjectSettings 변경은 손대지 않음 |

## 미검증
- 실제 플레이에서의 모양·부드러움(아래 가이드 1~10), 효과음·깜빡임 시점의 눈 확인, 실제 사다리 물리 중 다침 방향, Console 새 경고(가이드 11). 에디터 검사만 확인했다.

## 사용자 테스트 가이드
디버그 터미널: 플레이 중 **Ctrl + `**(1 왼쪽 키). `hp 50`처럼 체력을 낮추면 다침 그림이 나온다(효과음·깜빡임은 실제 피해 때만 날 수 있음). 실제 피해는 `spawn gas`로 옆에 가스 블록을 놓고 캐거나, 높은 곳에서 떨어지면 된다.
1. 가만히 서서 맞는다 → 움찔했다가 부드럽게 서 있는 자세로 돌아온다(약 0.4초, 뚝 끊기지 않음).
2. 맞자마자 좌우로 걷는다 → 다침 동작이 끝나면 바로 걷기로 이어진다.
3. 맞자마자 점프한다 → 바로 뛰고, 다침 동작이 끝나면 점프·낙하 그림으로 이어진다.
4. 높은 곳에서 떨어져 착지 피해를 입는다 → 다침 동작 후 대기(또는 걷기)로 돌아온다.
5. 사다리에 매달린 채 맞고, 맞는 동안 좌우 키를 누른다 → 캐릭터가 좌우로 뒤집히지 않고, 끝나면 사다리 자세로 돌아온다.
6. 사다리 위에서 왼쪽 블록을 캐다가 맞는다 → 왼쪽을 본 채 다침, 끝나면 사다리 채굴을 이어 간다.
7. 바닥에서 블록을 캐다가 맞는다 → 다침 동작 후 채굴을 이어 간다.
8. 가스 블록을 연달아 캐서 여러 번 맞는다 → 매번 다침 동작이 나오고 깜빡임·효과음이 전과 같다.
9. `hp 1` 후 가스 블록을 캐서 쓰러진다 → 다침 대신 쓰러지기가 바로 나온다.
10. 대기 ↔ 다침 ↔ 걷기에서 캐릭터 크기·발 위치가 튀지 않는다. 1번 다음, 7번(3-4→4) 근처가 끊겨 보이는지 적는다.
11. Unity Console에 새 빨간 오류·노란 경고가 없다(기존 URP stereoTargetEye, obsolete, 라이선스 로그 제외).
