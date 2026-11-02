# B-174 사다리 위 채굴 동작

## 기준과 범위

- 브랜치: `MVP2-fix118`.
- 시작 HEAD: `f2be41d4`(교훈 파일 분리), 게임플레이 기준: fix117 `3d0bc4bf`.
- 사다리 채굴 원본 24장을 사용했다. 원본 그림의 수정·재생성 없이 전체 배율과 공통 이동만 적용했다.
- `MiningSystem`, 이동·점프·사다리 물리, 바닥 채굴 순서 및 설정, 효과음, 가이드·관제 터미널은 변경하지 않았다.
- `init/prompt-B.md`, `prompt-B-174.md`, 원본 이미지의 작업 전 변경은 보존했다. 커밋·push는 수행하지 않았다.

## 그림 측정과 연결

재현: `python work_process/tools/prepare_b174_frames.py` 후 Unity 메뉴 `SubTerra/Player/Build B-174 Ladder Mining`.

| 항목 | 값 |
| --- | --- |
| 원본 | 960×960 RGBA, 1~24.png, 24장 |
| 새 원본 헬멧 폭 | 346px |
| 사다리 원본 헬멧 폭 | 414px |
| 측정 기준 | 주황 헬멧 상부 외곽. 테두리 아래 챙·밴드, 램프, 드릴 제외 |
| 측정 영역 | 새 그림 y 0~149, x < 610 / 사다리 그림 y 62~249, x < 820 |
| 기존 사다리 배율 / PPU | 0.8248048569 / 1084 |
| 목표 헬멧 폭 | 414 × 0.8248048569 ÷ 1084 × 256 = 80.6422px |
| 계산 배율 | 0.2330698706 |
| 실제 래스터 배율 | 224 ÷ 960 = 0.2333333333 |
| 출력 헬멧 환산 폭 | 80.7333px, 사다리 기준 대비 +0.113% |
| 첫 프레임 가로 기준 | 헬멧 상부 중심 x=437px → 출력 x≈128px |
| 24장 공통 이동 | x +26, y +24px |
| 출력 | 256×256, PPU 256, 피벗 (0.5, 0.5), Bilinear, 밉맵 없음, 무압축 |
| 축소 | Pillow RGBa(미리 곱한 알파) → Lanczos → RGBA |
| 발 아래 여백 | 전체 16~18px, 첫 프레임 16px |

전체 프레임이 새 256 캔버스 안에 들어간다. 원본의 13~16·21~24번 드릴 끝 잘림과 1번 헬멧의 위쪽 경계 접촉은 그대로 보존했다. 새로운 캔버스 잘림은 없다. 게임 화면에서 드릴 끝은 짧게 보이지만 원본을 복원하거나 따로 이동하지 않았다. 자세에 따른 전체 키 차이도 유지했다.

- [측정 영역 이미지](../images/player-ladder-mining-24f-20261029/helmet-measurement.png)
- [사다리와 채굴 비교](../images/player-ladder-mining-24f-20261029/ladder-comparison.png)
- [24장 밝은·어두운 배경 비교](../images/player-ladder-mining-24f-20261029/comparison-contact-sheet.png)
- [전체 그림 미리보기 GIF](../images/player-ladder-mining-24f-20261029/runtime-preview.gif) — 24장 순서 확인용. 실제 구간별 타이밍은 아래 설정과 테스트 기준.
- [게임 화면 비교](../images/player-ladder-mining-24f-20261029/play-contact-sheet.png)
- [게임 화면 상세](../images/player-ladder-mining-24f-20261029/play-detail-sheet.png)
- [전체 프레임 좌표·해시](../images/player-ladder-mining-24f-20261029/measurements.json)

`Player.prefab`의 `ladderMiningFrames` 24개와 `ladderMiningAnimation` 설정을 Editor API로 연결했다. 기존 그림 배열과 설정은 유지했다. 새로운 에셋·스크립트의 `.meta`는 Unity에서 생성했다.

## 방향과 재생

- 등반 중이거나 사다리 접촉 상태에서 사다리 그림을 표시하는 동안에만 전용 동작을 시작한다. 접근 중 점프 자세는 제외한다.
- 방향키 채굴의 상하와 마우스의 다른 줄·자기 칸은 시작 전에 무시한다. Enter는 사다리에서 수평 인접 칸만 검사하며, 위·아래 대체 목표를 탐색하지 않는다.
- 반전 방향은 `ActiveCell.x - ForegroundTilemap.WorldToCell(movement.Position).x`의 부호다. 기존 바라보는 방향과 반대인 목표도 올바르게 표시한다.
- 채굴·회수·역재생 구간에서는 목표 방향을 유지한다. 사다리 자세로 돌아오면 `PlayerFacing`의 기존 비반전 처리를 사용한다.
- Unity 독립 `PlayerLadderMiningAnimationSequence`가 그림 번호를 계산한다. 그림 번호와 fps는 프리팹 설정이다.

| 상황 | 처리 |
| --- | --- |
| 시작 | 1→12, 24fps, 0.5초 |
| 채굴 유지 | 13→14→15→16→15→14→13, 12fps 왕복 |
| Completed | 같은 이벤트 안에서 즉시 17번, 17→24를 60fps, 약 0.133초 후 사다리 |
| 이동·상하 등반 입력, 점프, 사다리 이탈, 바닥↔사다리 문맥 전환 | `CancelMining` 및 두 재생 순서 즉시 중단 |
| 일반 Cancelled(설정 창·방향 변경 등) | 현재 그림→1, 최대 0.3초 역재생 |
| 버튼 해제 | 채굴과 재생 유지 |
| 회수 중 새 채굴 | 1번부터 시작 |
| 취소 후 같은 쪽으로 0.15초 이내 재시작 | 13번부터 시작 |
| 피격 | 기존 Damage 우선. 이후 채굴 중이면 13번부터 |
| 쓰러짐 | 기존 Knockout 우선, 채굴 재생 즉시 초기화 |

## 변경 파일

- Gameplay: `PlayerAnimationController.cs`, `PlayerFacing.cs`, `PlayerMiningController.cs`, 신규 `PlayerLadderMiningAnimationSequence.cs`.
- Editor: 신규 `LadderMiningAnimationBuilder.cs`.
- 에셋: `Player.prefab`, 신규 `Frames/LadderMining/ladder_mining_01~24.png`와 메타.
- 검사: 신규 `PlayerLadderMiningAnimationSequenceTests.cs`, `PlayerLadderMiningTests.cs`, 기존 `PlayerRedrawnActionPlayModeTests.cs`에 사다리 검사 추가.
- 도구·증거: `work_process/tools/prepare_b174_frames.py`, `work_process/images/player-ladder-mining-24f-20261029/` 출력.
- 교훈: `init/rules/lessons/2026-10-29-issue264-ladder-mining-context.md`.

## 검증

- Unity 6000.5.4f1, 연결된 Unity MCP와 기존 `TestValidationRunner` 사용. 프로젝트에는 CLI 연결용 Pipeline 패키지가 없어서 패키지를 추가하지 않았다.
- 수정 전 전체 EditMode: 2,202개 실행, 2,178 통과 / 기존 24 실패.
- 수정 후 전체 EditMode: 2,236개 실행, 2,212 통과 / 같은 24 실패. 이름으로 비교한 **새 실패 0**, 기존 실패 해소 0.
- [기존 실패 24개 목록](../images/player-ladder-mining-24f-20261029/baseline-failures.txt). 원본 결과: `sub-terra/Temp/b174-baseline/`, `sub-terra/Temp/b174-final-edit/`.
- 관련 EditMode 61/61 통과: 순수 순서·시간, 설정, 좌우 반전, 목표 제한, 즉시·일반 취소, 버튼 해제, 피격, 쓰러짐, 24개 이미지·발 정렬·프리팹 연결, 기존 바닥 채굴/B-172/fix117/사다리 정적 검사.
- PlayMode 2/2 통과: 신규 사다리 검사와 기존 바닥 채굴·가스 쓰러짐·구조 복귀·사다리 비반전 검사. `sub-terra/Temp/b174-final-play/`에 결과 XML·요약·단계별 기록이 있다.
- 최종 PlayMode Console: 오류 0, 기존 URP `Camera.stereoTargetEye` 경고 2회. 새 코드 관련 경고 없음.

화면 검사는 Bootstrap→새 게임→탐사로 진입해 메모리 저장소를 사용했다. 사다리와 블록을 런타임에 배치하고 물리·이동 컨트롤러는 고정한 상태에서 실제 채굴 이벤트와 LateUpdate 애니메이션을 검사했다. 프레임 카운터 증가 및 13~16번 왕복 표시를 확인했다. 이동·점프·이탈 조건은 이 검사에서는 상태를 주입한다.

미검증: 실제 키보드·마우스를 사용한 물리 등반 중 취소, 사다리 끝에서 바닥으로 넘어가는 경계, Windows 빌드. 아래 가이드의 5·6·8·9번을 실제 입력으로 확인한다. 가스 쓰러짐은 실제 생존 실패 이벤트를 주입해 검사하며 실제 가스 타일 폭발과의 연계는 가이드 11번으로 확인한다.

테스트가 변경한 범위 밖 폰트·UI 프리팹·씬·ProjectSettings·기존 PNG는 복원했다. 작업 전 사용자 변경은 보존했다. `git diff --check`를 통과했다.

## 사용자 테스트 가이드

디버그 터미널: 플레이 중 **Ctrl + `**(1 왼쪽 키). `spawn copper`(또는 iron·gas)는 바라보는 쪽 옆 칸에 블록을 놓는다. 사다리 위에서 왼쪽을 보고 쓰면 왼쪽에 놓인다.
1. 사다리를 설치하고 중간쯤 올라가 멈춘다. 오른쪽에 블록이 있으면 오른쪽을 판다 → 한 팔로 사다리를 잡고 다른 팔로 드릴을 꺼내 오른쪽을 판다.
2. 같은 자리에서 왼쪽 블록을 판다 → 같은 동작이 좌우만 뒤집혀 나온다.
3. 단단한 블록(`spawn iron`)을 판다 → 드릴을 대고 있는 동작이 끊김 없이 반복된다.
4. 블록이 깨지는 순간 → 바로 드릴을 거두고 사다리 자세로 돌아온다(멈칫하는 시간 없음).
5. 파는 중 위·아래 키를 누른다 → 동작과 채굴이 바로 멈추고 사다리를 오르내린다.
6. 파는 중 점프하거나 사다리 밖으로 나간다 → 동작과 채굴이 바로 취소된다.
7. 파는 중 마우스 버튼을 뗀다 → 블록이 깨질 때까지 계속 판다(바닥과 같음).
8. 사다리 위에서 위·아래 블록을 파려고 한다 → 시작되지 않는다. 바닥에서는 위·아래 채굴이 그대로 된다.
9. 사다리 위에서 블록 여러 개를 연달아 판다 → 매번 자연스럽게 시작하고 크기·위치가 튀지 않는다.
10. 사다리 그림 ↔ 사다리 채굴 그림이 바뀔 때 크기와 발 위치가 같다. 드릴 끝이 잘린 프레임(원본 그대로)이 얼마나 눈에 띄는지 적는다.
11. `hp 1` 뒤 사다리 위에서 가스 블록(`spawn gas`)을 판다 → 가스로 쓰러지면 쓰러지기 동작이 바로 나온다(가스가 터지지 않으면 건너뜀).
12. 바닥 채굴(시작 → 파기 → 깨지면 바로 회수)이 fix117과 똑같다.
13. Unity Console에 새 빨간 오류·노란 경고가 없다(기존 URP stereoTargetEye, obsolete, 라이선스 로그 제외).

