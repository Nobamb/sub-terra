# 2026-10-01 데스크탑 작업 인수인계

## 다음 Codex가 먼저 알아야 할 상태

- 저장소: https://github.com/Nobamb/sub-terra
- 작업 브랜치: `codex/elevator-outpost-ride-followup`
- 검토 중 PR: https://github.com/Nobamb/sub-terra/pull/138
- 기능 변경의 마지막 커밋: `003b4f6c`.
- 노트북 작업 위치는 `.worktrees/elevator-outpost-ride-followup`이었다. 원래 체크아웃을 열면 다른 브랜치의 이전 상태가 보일 수 있다.
- 데스크탑에서는 실제 체크아웃 경로와 브랜치를 먼저 확인한다. 노트북의 절대 경로나 `.worktrees` 폴더를 그대로 복사할 필요는 없다.
- Unity 프로젝트는 Git 저장소 루트가 아니라 **선택한 체크아웃 아래 `sub-terra/`**이다. 버전은 6000.5.4f1.
- 시작 씬은 `sub-terra/Assets/_Project/Scenes/Bootstrap/Bootstrap.unity`. 여기서 Play → 새 게임으로 확인한다.
- 저장소의 AGENTS.md, `init/rule.md`, `init/PRD.md`, `docs/INTEGRATION_GUIDE.md`와 관련 작업 문서를 따른다.

## 오늘 끝낸 작업

1. `a436b880`: 객실과 캐릭터가 레일 끝/출발 화면 상단을 넘어간 뒤 화면 전환. 출발 카메라는 고정하고, 실패 때만 복귀한다.
2. `cc133611`: 고정 레일을 엘리베이터 아래 보호 블록 윗면까지 연장하고 금속 받침·좌우 고정부 추가. 객실·문·탑승자만 상승하며 중앙 승강로는 비워 둔다. HUD 시설 선택 안내를 숨기고, 보호 블록 3칸과 맵 좌우·하단 경계를 동일한 짙은 암석으로 맞췄다.
3. `003b4f6c`: 두 방향 모두 공기에 노출된 바깥 모서리만 완화. 채굴 접촉 돌가루와 제거 성공 시 질감 파편을 추가하고 최대 64개를 재사용한다. 원본 타일 식별자, 충돌, 저장 및 보상 규칙은 유지한다.

위 작업은 커밋·푸시했고 PR #138에 설명을 반영했다. PR 게시에는 임시 GitHub CLI를 사용했다. GitHub Desktop과 CLI는 별개이며, CLI 사용 한도 소진 때문에 막혔던 것이 아니다. `gh pr edit`의 불필요한 조직 권한 조회는 `gh api --method PATCH repos/Nobamb/sub-terra/pulls/138`로 피했다. 인증 정보를 출력하거나 문서에 저장하지 않는다.

## 검증과 남은 확인

- 사용자는 고정 승강로 수정이 제대로 보인다고 확인했다.
- 모서리/채굴 작업의 관련 C# 컴파일 및 Unity 셰이더 패스 컴파일 통과.
- 연결된 내부 모서리 유지, 노출된 외곽만 처리, 채굴 후 판정 및 타일 식별자 유지 확인.
- 실제 MiningSystem 시작·취소·완료 이벤트에서 접촉 2개, 성공 시 추가 파편 14개, 반복 채굴 시 최대 64개 제한 확인.
- 위 최소 검증 후 Unity Console 오류 0건 및 git diff --check 통과.
- 전체 Test Runner, Windows QA 빌드, 데모 완주는 추가로 실행하지 않았다.
- **사용자는 모서리 효과가 충분히 보이지 않는다고 피드백했다.** 프레임 저하 때문이라고 확정하지 말 것. 후속 시각 확인 대상이다.
- 노트북에는 `Building_OutpostCore_Basic.asset`, `ProjectSettings/ProjectSettings.asset`의 기존 로컬 변경이 남아 있었다. 이번 커밋에서 제외했으며, 데스크탑으로 옮겨야 하는 변경으로 간주하지 않는다.

## 데스크탑에서 작업 준비

1. 저장소 루트에서 `git status`로 로컬 변경을 확인하고 보존한다. 강제 초기화 금지.
2. `git fetch origin` 후 PR #138의 병합 여부와 최신 main을 확인한다.
3. PR이 아직 열려 있으면 작업 브랜치를 받아 이어간다. 새 체크아웃이라면:

   ```powershell
   git switch --track origin/codex/elevator-outpost-ride-followup
   ```

   이미 로컬 브랜치가 있으면 `git switch codex/elevator-outpost-ride-followup` 후 `git pull --ff-only`를 사용한다. 변경이 있는 상태에서는 임의로 브랜치를 바꾸지 않는다.
4. PR이 병합됐다면 최신 main에서 사다리 개선용 새 `codex/` 브랜치를 준비한다. PR이 열려 있는 경우 최신 main 반영이 필요하면 변경을 보존한 상태에서 병합·충돌을 확인한다.
5. Unity Hub에는 **선택한 체크아웃의 `sub-terra/`**를 등록하고 해당 프로젝트를 연다. 에디터 임포트·컴파일이 끝나면 Console부터 확인한다.

## 다음 우선 작업: 사다리 광부의 입체감 개선

새 기능이나 3D 모델을 만드는 작업이 아니라, 기존 광부와 맞는 등반 스프라이트 3장을 제작·연결하는 작업이다. 아직 새 등반 이미지는 제작하지 않았다.

### 기준 파일

- 외형 기준: `sub-terra/Assets/_Project/Art/Characters/Player/Frames/Idle/player_idle_01.png`
- 기존 등반: 같은 Player 폴더 아래 `Frames/LadderBack/ladder_back_01.png`, `ladder_back_02.png`, `ladder_back_03.png`
- 사다리: `sub-terra/Assets/_Project/Art/Facilities/MVP/ladder_segment_mine.png`
- 런타임: `sub-terra/Assets/_Project/Scripts/Gameplay/Player/PlayerAnimationController.cs`
- 빌더: `sub-terra/Assets/_Project/Editor/PlayerAnimationAssetBuilder.cs`
- 프리팹: `sub-terra/Assets/_Project/Prefabs/Gameplay/Player/Player.prefab`

### 작업 순서

1. 위 이미지를 실제로 열어 헬멧·작업복·배낭의 색과 비율, 윤곽선, 명암 방향 및 현재 임포트 설정을 확인한다.
2. 먼저 **기본 매달림 1장**만 만든다. 약간 돌아선 뒷모습, 헬멧과 배낭의 볼륨, 기존 광부와 같은 명암이 목표다. 투명 PNG를 사용하며 사다리 자체를 캐릭터 이미지에 포함하지 않는다.
3. 인게임에서 기존 광부와 비교한다. 키·체형·화면상 선명도와 손/발의 사다리 접점을 맞춘다. 사용자/팀원의 확인을 받을 때까지 스타일이 확정됐다고 가정하지 않는다.
4. 기준이 맞으면 **왼손·오른발 상승**, **오른손·왼발 상승** 두 장을 추가한다. 캔버스 크기, 중심점, 발 위치 기준, PPU와 임포트 설정을 통일한다.
5. 현재 이동 거리 기반 재생을 유지한다. 기존 순서는 `0 → 1 → 0 → 2`, 하강 시 역방향이며 정지 때 0번 자세다. 단순 시간 재생으로 바꾸거나 이동·사다리 판정을 수정하지 않는다.
6. 기존 에셋을 교체할 경우 `.meta` GUID를 보존한다. 시안은 별도 파일로 보관하고, 씬/프리팹은 수동 YAML 대신 Editor 또는 좁은 빌더 흐름으로 수정한다. 전체 플레이어 에셋 빌더는 여러 애니메이션을 건드리므로 필요 범위부터 확인한다.

### 최소 확인

- 서 있는 광부와 등반 광부의 크기·색·질감이 일관적인가.
- 상승/정지/하강에서 손·발 위치와 프레임 전환이 자연스러운가.
- 사다리 진입·이탈·점프에서 이미지가 튀지 않는가.
- 기존 이동·충돌·발 위치가 유지되는가.
- Console 오류와 변경 diff를 확인한다. 큰 QA 빌드나 전체 테스트는 사용자가 요청하지 않으면 자동으로 반복하지 않는다.

## 이후 보완: 모서리 효과의 식별성

- `sub-terra/Assets/_Project/Resources/MineRoundedCorners.shader`의 `_CornerRadius` 기본값은 셀 크기의 0.065.
- `MineTileCornerVisual.cs`는 전경 타일의 양쪽 이웃이 모두 비어 있는 꼭짓점만 처리한다. 긴 직선 경계나 내부 모서리가 그대로인 것은 의도한 동작이다.
- 새 게임에서 컴포넌트·재질 연결, Game 창 배율과 실제 노출 꼭짓점을 먼저 확인한다. 적용 오류와 곡률이 작은 문제를 구분한 뒤 아주 조금씩 조정한다. 연결된 내부까지 둥글게 만들어 틈을 내지 않는다.

## 다음 Codex에 전달할 요청

> `work_process/desktop_handoff_20261001.md`를 먼저 읽고 현재 체크아웃과 PR #138의 병합 여부, 최신 main 반영 상태를 확인해줘. 다음 작업은 기존 광부와 같은 질감의 사다리 등반 기본 자세 1장을 만들고 인게임 크기·접점을 확인하는 일이야. 확인 후 교차 동작 2장을 추가해 기존 3장 재생 구조에 연결해줘. 테스트와 빌드는 최소한으로 하고, 무관한 설정이나 기존 로컬 변경은 건드리지 말아줘.
