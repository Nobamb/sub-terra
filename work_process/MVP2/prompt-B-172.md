# B-172 채굴·쓰러지기·사다리 새 그림 교체 + 사다리 뒤집힘 금지 (이슈 #264)

## 목표
- 플레이어 채굴(24장)·쓰러지기(48장)를 사용자가 새로 그린 그림으로 바꾸고, 채굴은 "시작 → 파는 중 → 블록 깨짐 → 완료 → 일어서기" 흐름으로 재생한다.
- 사다리 등 그림 5장을 사용자가 새로 그린 그림(헤드램프를 작게 그림)으로 바꾸고, 사다리에서 좌우 키를 눌러도 그림이 뒤집히지 않게 한다. 가이드·관제 터미널 데모의 사다리·채굴 그림을 게임 그림과 같게 맞춘다.
- 베이스: 최신 `main`(PM 확인 5cccca03, 2026-10-28 17:59 KST, #242 걷기 40장 병합 후). 새 브랜치 **`MVP2-fix116`**.
- 다른 에이전트(Codex 등)에게 나눠 맡겨도 된다. 결과는 이 브랜치 하나로 모은다.
- 이번에 하지 않음(그림 준비 후 B-173 등 후속): 점프 4묶음(도약/상승/낙하 반복/착지), 맞을 때 8프레임(약 0.4초). 이번 코드가 이것들을 막지 않게만 한다.

## 절대 규칙
1. 걷기·대기·점프·맞을 때 그림과 속도는 건드리지 않는다. 사다리는 그림 5장 교체·뒤집힘·가이드·데모만 바꾸고 오르내리는 규칙(거리 기준, 순서, 소리)은 그대로 둔다.
2. 새 그림을 다시 그리거나 부위별로 고치지 않는다. 동작마다 **한 배율·한 위치 이동**으로만 맞춘다(프레임마다 따로 옮기지 않음).
3. 채굴 판정·시간·에너지, 생존·실패 처리, 효과음 코드(드릴·피격·실패 창)는 바꾸지 않는다. 애니메이션은 이벤트를 **듣기만** 한다.
4. 줄 번호는 5cccca03 기준 근사(~). 못 찾으면 "못 찾음"으로 보고한다.

## 먼저 읽을 것 (`sub-terra/Assets/_Project/` 기준)
- Scripts/Gameplay/Player/PlayerAnimationController.cs: `SetMining`(~154), `ResolveStateName`(~159, 우선순위 기절 > 피격 > 채굴 > … ), `HandleFailureRequested`(~222, 인자를 버림), `ResolveFrames`(~300: Mining 10fps 반복, Knockout 8fps 1회), `ShouldHoldLadderVisual`(~414)
- Scripts/Gameplay/Player/PlayerFacing.cs(~18–28: `movement.FacingDirection`이 바뀌면 `visualRoot.localScale.x` 부호를 바꿈. 사다리에서도 바뀜)
- Scripts/Gameplay/Mining/PlayerMiningController.cs(~69–147: `SetMining(true/false)` 호출, 이동 입력·설정 창·방향 변경 시 `CancelMining`), MiningSystem.cs(`TileMined` 이벤트 ~128, `ProgressChanged`의 `MiningPhase.Completed/Cancelled`, `IsMining`)
- Scripts/Gameplay/Player/PlayerSurvivalController.cs(`FailureRequested`의 `RunFailureInputDto.cause`, ~243 `ApplyDamage`), PlayerSurvivalState.cs(`LastDamageCause`), Scripts/Shared/DTO/RunFailureDtos.cs(`RunFailureCause`: GasExposure·StructuralCollapse·Fall·PowerDepleted)
- Prefabs/Gameplay/Player/Player.prefab(`ladderFrames` ~408, `miningFrames` ~414, `knockoutFrames` ~428, 그림 오브젝트 크기 0.95)
- Editor/RedrawnWalkCycleBuilder.cs(B-171 걷기 정렬·가져오기 방식 참고), Editor/DataValidation/PromptB140GameGuideSkinBuilder.cs(~30 `player.mine.1~8`, ~31 `player.ladder.1~5` = `ladder_back_01~05`, ~34 `player.knockout.1`), Resources/UI/GameGuideSkin.asset
- Scripts/App/UI/Guide/GuideDemoLibrary.cs(~225·~709 사다리 5장 8fps, ~283 채굴 8장 11fps), Scripts/App/UI/ControlTerminal/ControlTerminalDemoClips.cs(~150 사다리, ~216 채굴)
- 테스트: PhaseEMiningStaticTests, PhaseCTraversalStaticTests, LadderBackBootstrapPlayModeTests, PlayerWalkCycleTests(~209 반전 검사), PlayerMovementPlayModeTests, PromptB85EmergencyRescueChipTests
- `work_process/images/player-walk-40f-20261028/`(걷기 원본·정렬 기록)

## 1. 새 그림 넣기
- 사용자가 둘 위치(걷기와 같은 방식, 파일 이름은 숫자만):
  - 쓰러지기: `work_process/images/player-knockout-48f-20261029/source/1.png ~ 48.png`
  - 채굴: `work_process/images/player-mining-24f-20261029/source/1.png ~ 24.png`
  - 사다리: `work_process/images/player-ladder-5f-20261029/source/1.png ~ 5.png`
  - 채굴 원본의 `13(minig-process).png` → `13.png`, `14(mining-complete).png` → `14.png`로 이름을 바꾼다(그림은 그대로). 원래 이름으로 들어와 있으면 이 작업에서 바꾸고 보고서에 적는다. 파일이 없으면 멈추고 보고한다.
- PM이 잰 원본 정보(모두 960×960 RGBA, 배경 투명, 오른쪽을 봄, 빠진 번호 없음):
  - 쓰러지기: 1~18 서서 휘청, 19~36 오른쪽 앞으로 쓰러짐, 37~48 엎드려 숨쉬기(이웃 차이 매우 작음). 서 있을 때 키 813~841px, 발 y 910~915, 가로 중심 469 → 엎드리면 545, 폭 최대 813px(x 75~888). 48→37 차이는 이웃 프레임의 약 3배.
  - 채굴: 1~4 드릴 들고 숙임, 5~13 파는 중, 14 완료, 15~24 일어나 드릴을 위로 듦. 발 y 898~907, 가로 중심 약 497, 폭 x 135~826. 15~24는 드릴 끝이 y 46~57까지 올라감(발부터 최대 853px).
  - 사다리: 5장, 1229×1280 RGBA 투명, 뒷모습. 1 = 두 발 디딤·두 손 위(멈춤 그림), 2·3 = 오른손·오른발 올림(3이 최대), 4·5 = 왼손·왼발 올림(5가 최대). 지금 게임 세트(neutral_v2, style_02~05)와 같은 구성이라 순서 {0,1,2,1,0,3,4,3}·멈춤 0번을 그대로 쓴다.
    - 키 1161~1178px(기존 게임 사다리 955~961px의 약 1.22배), 머리 위 y 50~62, 디딘 발 아래 y 1218~1231(5번만 1184: 몸을 웅크린 자세라 약 41px 올라감), 가로 중심 616~640.
    - 헤드램프: 빛나는 렌즈 없이 헬멧 오른쪽에 작은 어두운 몸통만 보임(뒤집지 않으니 항상 오른쪽).
    - **5.png 왼쪽 위(x≈18, y≈21)에 점 하나(약 18px)가 있다. 정렬 때 지운다.**
 발 y≈846, 키 ≈740px → 게임 256 그림에서 배율 약 0.307, 발은 아래에서 약 13~16px, 가로 중심 128.
  - 새 그림은 걷기보다 약 8~12% 크게 그려져 있다(PM 추정).
- 정렬 스크립트를 `work_process/tools/`에 두고 각 폴더 `runtime-frames/`에 출력한다.
  - **배율**: 키가 아니라 **머리·헬멧 크기**를 걷기에 맞춘다. 걷기 서 있는 프레임과 쓰러지기 1번·채굴 1번의 헬멧 폭(주황 헬멧, 램프 제외)을 재서 배율 = 0.307 × (걷기 헬멧 / 새 헬멧). 두 동작 값이 비슷하면 하나로 통일한다. 잰 값과 배율을 보고서에 적는다.
  - **발 기준선**: 동작마다 이동량 하나로, 쓰러지기 1번·채굴 1번의 발이 걷기와 같은 높이에 오게 한다. 가로는 같은 두 프레임의 몸 중심을 128에. 쓰러지며 오른쪽으로 가는 움직임은 그대로 둔다.
  - **캔버스**: 지금 256×256에서 드릴 끝(채굴 15~24)과 엎드린 몸(쓰러지기 37~48)이 가장자리에서 2px 이상 떨어지면 256 그대로. 모자라면 캔버스를 키우고(예: 320×320) 피벗을 조정해 **월드에서 발 높이가 걷기와 같게** 한다. 어느 쪽을 골랐는지와 잘림 검사 결과를 적는다.
  - 걷기처럼 알파를 미리 곱해서 줄인다(테두리 어두워짐 방지).
- 사다리 정렬: 기존 게임 사다리와 같은 1254×1254 캔버스·같은 가져오기 설정(PPU 1084 등)을 유지한다. 배율 하나로 헬멧 폭(또는 키)을 기존 `ladder_back_neutral_v2`(키 약 958px)와 맞추고(약 0.82 예상), 1번의 발 아래 끝·가로 중심을 neutral_v2(발 y≈1104, 중심 ≈635)에 맞춘 이동 하나를 5장에 똑같이 적용한다. 결과를 **기존 파일 `ladder_back_neutral_v2.png`, `ladder_back_style_02~05.png`에 덮어써서 GUID를 유지**한다(프리팹·가이드 참조 그대로). 기존 `Editor/LadderMinerStyleBuilder.cs`는 이 파일들을 다시 만들어 덮어쓰므로 새 원본을 쓰게 고치거나 실행되지 않게 막고, 그 내용을 보고서에 적는다.
- 게임 파일: `Art/Characters/Player/Frames/Mining/mining_01~24.png`, `Frames/Knockout/knockout_01~48.png`. **기존 01~08의 .meta(GUID)는 유지**하고 그림만 바꾼다. 가져오기 설정은 걷기와 같게(Sprite Single, PPU는 걷기와 같은 픽셀 크기가 되도록, Bilinear, 밉맵 없음, 압축 없음). 빌더는 RedrawnWalkCycleBuilder 방식으로 만든다.

## 2. 채굴 재생 (그림 번호는 1부터, 코드 배열은 0부터)
1. 채굴 시작: 1→4 한 번(시작 fps 기본 20, 0.2초).
2. 파는 중: 5→13 정재생(작업 fps 기본 12).
3. 13에 닿았는데 블록이 안 깨졌으면 13→`miningReturnFrame`까지 빠르게 역재생(역재생 fps 기본 30) 후 다시 정재생. 반복.
   - `miningReturnFrame` 기본 5. 프리팹에서 바꿀 수 있는 설정값(범위 5~12로 제한)으로 둔다.
4. 블록이 깨지는 순간(`MiningSystem.TileMined` 또는 `ProgressChanged`의 Completed를 듣는다. 애니메이션에 알리는 메서드 하나 추가 예: `NotifyMiningCompleted()`):
   - 정재생 중이면 그대로, 역재생 중이면 정재생으로 바꿔 13까지 **빠르게**(30fps) 간다. 시작 구간(1~4) 중이면 그대로 이어서 13까지 빠르게.
   - 14(완료)를 잠깐(기본 0.12초) 보여 주고 15→24(마무리 fps 기본 20, 0.5초)로 끝낸 뒤 대기로.
   - 블록이 깨지면 `IsMining`이 바로 false가 되므로, 마무리는 `SetMining(false)`가 와도 끝까지 재생한다.
5. 중간에 멈출 때(PM 제안. 다르게 하는 게 낫다면 이유를 적고 고른다):
   - 이동·점프·사다리 입력: 즉시 끊고 해당 동작으로(조작감 우선). 마무리 재생 중이어도 같다.
   - 버튼만 떼서 취소(이동 없음): 현재 프레임에서 1까지 빠르게 역재생(최대 약 0.3초) 후 대기. 그 사이 입력이 오면 바로 끊는다.
   - 피격: 지금처럼 피격 그림이 먼저. 피격이 끝났을 때 아직 채굴 중이면 시작 구간 없이 5부터.
   - 기절: 언제든 바로 쓰러지기로.
6. 연속 채굴:
   - 마무리(14~24) 중에 다음 채굴이 시작되면 마무리를 끊고 1부터 시작 구간을 다시 재생.
   - 방향 바꾸기 등으로 취소 직후(기본 0.15초 안) 바로 다시 시작되면 시작 구간 없이 현재 파는 동작을 이어 간다(같은 동작이 매번 처음부터 시작되는 깜빡임 방지).
- 프레임 고르는 규칙은 Unity와 분리된 작은 클래스(예: `PlayerMiningAnimationSequence`)로 만들어 시간·이벤트만 넣어 테스트할 수 있게 한다. fps·유지 시간은 모두 설정값.
- 드릴 소리·채굴 시간은 바꾸지 않는다. 블록 하나 채굴 시간(기본 약 1초 안팎)이 짧아도 위 흐름이 깨지지 않는지(예: 시작 구간 중에 깨짐) 확인한다.

## 3. 쓰러지기 재생
- 1→48 한 번, **24fps(2.0초)**.
- 끝난 뒤:
  - 원인이 가스(`RunFailureCause.GasExposure`)면 37→48 숨쉬기 구간을 계속 반복(숨쉬기 fps 설정값, 기본 12 = 한 번 1.0초. 24면 너무 빠르게 보일 수 있음). 48→37 이음이 튀어 보이면 보고서에 적는다(왕복 재생은 PM 결정 사항).
  - 그 외(무너진 돌 = `StructuralCollapse`, 추락 = `Fall`, 기타)는 48에서 멈춤.
- 원인 구분: `HandleFailureRequested(RunFailureInputDto dto)`에서 `dto.cause`를 저장한다. 없으면 `survival.State.LastDamageCause`를 쓴다. 가스 구조 필요(`RescueRequired`) 경우도 GasExposure로 들어온다. 전력 0은 기절이 아니다(B-81, `ApplyPowerDepletion`은 아무것도 안 함).
- 구조·복귀(`RestoreAfterRescue`로 `CanAct`가 true) 뒤에는 원인과 재생 상태를 초기화하고 바로 대기로.

## 4. 사다리 뒤집힘 금지
- 사다리 등 그림(Ladder·LadderDown·LadderIdle, 사다리 그림 유지 유예 포함)이 보이는 동안에는 `visualRoot.localScale.x`를 항상 원래 방향(양수)으로 둔다. 좌우 키는 그림을 뒤집지 않는다.
- `FacingDirection` 값 자체는 지금처럼 바뀐다(채굴 방향 등에 쓰임). 사다리를 벗어나는 순간 현재 `FacingDirection`대로 다시 뒤집는다.
- 가장 단순한 방법: 애니메이션이 "지금 사다리 그림을 보여 주는 중"을 알려 주고 `PlayerFacing`이 그동안 반전을 멈춘다. 사다리에서 채굴할 때는 채굴 그림이라 지금처럼 방향을 따른다.

## 5. 가이드·관제 터미널 데모
- 사다리: `player.ladder.1~5`를 새 게임 사다리 그림(덮어쓴 `ladder_back_neutral_v2`, `ladder_back_style_02~05`, 프리팹 `ladderFrames`와 같은 그림·같은 순서)으로 바꾼다. 게임의 재생 순서와 비슷하게 보이게 하고, 크기가 이전 데모와 비슷한지 확인한다(사다리 그림은 1254 캔버스, PPU가 서로 다름). 예전 `ladder_back_01~05` 파일은 지우지 않는다(테스트·빌더가 대체 경로로 참조).
- 채굴: `player.mine.1~24`로 늘리고, 데모 한 번에 시작 → 파기 → 완료 → 일어서기가 보이게 한다(약 2초). 데모 재생 기능이 연속 반복만 되면 가장 단순한 방법을 고르고 보고한다.
- `player.knockout.1`: 쓰이는 곳을 찾아 새 그림(엎드린 48번)이 맞는지 확인한다.
- `GameGuideSkin.asset`은 빌더로 다시 만든다.

## 손대지 말 것
- 걷기·대기·점프·맞을 때 그림과 속도, 이동·점프·사다리 이동 규칙, 효과음, 채굴·생존 규칙, 다른 PR, `AGENTS.md`·규칙 문서, `work_process`의 예전 기록

## 테스트
- EditMode 전체에서 main의 기존 실패 말고 새 실패가 없어야 한다.
- 새/수정 테스트:
  - 채굴 순서 클래스: 시작 1~4 한 번 → 5~13 → 13에서 되돌림 → 돌아오는 지점 설정 변경 반영(예: 7) → 정재생/역재생/시작 구간 중 깨짐 → 13 → 14 → 15~24 → 대기, 버튼 떼기 취소, 마무리 중 재시작, 짧은 취소 후 재시작
  - 쓰러지기: 24fps, 48장 2.0초. 가스면 37~48 반복, 무너짐·추락이면 48 유지, 구조 후 대기
  - 그림: 프리팹 `miningFrames` 24·`knockoutFrames` 48 모두 다른 그림, 01~08 GUID 유지, 발 기준선·헬멧 크기가 걷기와 비슷, 가장자리 잘림 없음
  - 사다리 그림: 5장 GUID 유지, 키·발·중심이 이전 세트와 비슷(키 ±5%), 5번의 점 제거 확인, 가장자리 잘림 없음
  - 반전: 사다리 그림 중 좌우 입력에도 `localScale.x` > 0, 사다리를 벗어나면 다시 반전(기존 `PlayerWalkCycleTests` 반전 검사 유지)
  - 가이드: `player.mine.1~24`, `player.ladder.1~5`가 프리팹 `ladderFrames`와 같은 그림
- 위 "먼저 읽을 것"의 기존 테스트가 장수·fps를 고정 검사하면 새 값으로 고친다.
- PlayMode는 이번에 고친 테스트만 돌린다. 실행 전후 `git status`로 범위 밖 프리팹·폰트·씬·PNG 변경을 확인하고 되돌린다.

## 수동 확인 체크리스트 (보고서에 그대로 넣기)
1. [ ] 블록을 캐면 드릴을 내리고(시작) → 파는 동작이 자연스럽게 반복되고 → 블록이 깨질 때 완료 자세 → 드릴을 들고 일어선다.
2. [ ] 단단한 블록(오래 걸림)에서 13→5 되돌아가는 반복이 끊기거나 튀어 보이지 않는다.
3. [ ] 무른 블록(빨리 깨짐)에서도 마무리까지 어색하지 않다.
4. [ ] 채굴 중 이동·점프하면 바로 움직인다. 버튼만 떼면 짧게 일어서며 대기로 간다.
5. [ ] 블록을 연달아 캘 때 매번 처음부터 크게 튀지 않는다.
6. [ ] 채굴·쓰러지기 그림의 크기와 발 높이가 걷기·대기와 비슷해서 바뀔 때 커지거나 떠 보이지 않는다. 드릴 끝이 잘리지 않는다.
7. [ ] 가스로 쓰러지면 약 2초 동안 쓰러진 뒤 엎드려 숨 쉬는 동작이 계속된다.
8. [ ] 무너진 돌·추락으로 쓰러지면 엎드린 자세에서 멈춘다.
9. [ ] 구조·복귀 후 바로 서 있는 그림으로 돌아온다.
10. [ ] 왼쪽을 보고 채굴·쓰러지기를 해도 좌우만 뒤집혀 같게 보인다.
11. [ ] 사다리에서 좌우 키를 눌러도 등 그림·헤드램프가 좌우로 뒤집히지 않는다. 사다리에서 내려와 걸으면 방향이 맞다.
12. [ ] 새 사다리 그림이 오르내릴 때 손발이 번갈아 자연스럽게 움직이고, 램프가 작게 보여 튀지 않는다. 사다리에 붙거나 내릴 때 크기·위치가 튀지 않는다(5번 그림에서 몸이 살짝 들려 보이는지 적기).
13. [ ] 게임 가이드·관제 터미널 데모의 사다리·채굴이 게임과 같은 그림으로 나온다.
14. [ ] 밝은 지상·어두운 광산에서 가장자리 검은 테두리나 깨진 픽셀이 없다.
15. [ ] Console에 새 경고·오류가 없다.

## 완료 후
- 커밋은 한국어 conventional 형식(그림 넣기 / 채굴 재생 / 쓰러지기 / 사다리·데모로 나눔). `MVP2-fix116`에 일반 push, 브랜치는 지우지 않는다.
- main으로 PR을 열고 본문에 `Refs #264`(후속이 남아 있어 닫지 않음).
- 실수로 고친 것이 있으면 `init/rules/lessons/`에 교훈 파일 하나(#261 방식).
- 결과 보고서 `work_process/MVP2/B-172-mining-knockout-ladder-result.md`
  - 기준 main 커밋, 이름 바꾼 파일, 헬멧 측정값·배율·이동량·캔버스 선택, 잘림 검사
  - 채굴·쓰러지기 설정값(fps·되돌림 지점·유지 시간)과 고른 이유, 멈춤·연속 채굴 처리
  - 쓰러진 원인 구분 방법, 테스트 결과, 채굴·쓰러지기 비교 GIF 또는 프레임 이미지
  - 직접 보지 못한 항목은 "미검증", 위 수동 확인 체크리스트
