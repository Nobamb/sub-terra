# B-193 낙석 피해 배율·착지 가스 수정과 디버그 명령 결과 (#289, PR #312)

## 기준 커밋
- 시작 커밋: `5cda7979`(PR #312 기존 브랜치 `codex/issue-289-collapse-damage`)
- 병합한 main: `a3a2b8f8`(충돌 없음, 병합 커밋 `0b78f3aa`)
- 작업 중 같은 브랜치에 팀원 커밋 `6500917e`(같은 #289 수정)이 병합되어, 작업 폴더에서 그 위에 이 구현을 얹었다(현재 로컬 브랜치 `289-issue-fix`, HEAD `b7ed3d34`). 겹치는 파일은 이 구현 기준으로 정리했고 `MiningSystem`·`PlayerMiningController`는 팀원 버전을 그대로 썼다.
- 만든 커밋 해시: **아직 커밋하지 않음**(사용자 승인 대기).

## 원인 (실제 광산 씬 기준)
- **배율이 25로만 남던 원인은 재현하지 못했다.** `Bootstrap → 새 게임 → 광산`과 `저장 후 이어하기` 두 경로에서 실제 씬 배선 그대로 `PlayCollapse`/`CollapseUnsupportedCeiling`을 돌려 보니 일반 25 / 구리 37.5 / 철 50 / 리튬 20 / 금 암석 32.5 / 금 철 65가 정상으로 들어갔다(씬 YAML 연결·`MiningTileResolver` 타일 등록·카탈로그 `unitWeight`도 정상). 보고서 원인 후보 1·2·3은 현재 코드와 씬에서 확인되지 않았다.
- **착지 가스**도 실제 광산 씬에서 가스/리튬 낙석은 1회 퍼졌다. 다만 코드상 실제 결함이 둘 있어 고쳤다.
  1. 같은 붕괴 묶음에서 위 칸이 먼저 떨어지기 시작해 아래 칸을 바닥으로 잡으면, 아래 칸이 같이 떨어진 뒤 `HasTile`이 false가 되어 가스가 생략됨 → 착지 시점에 바닥을 다시 찾도록 변경.
  2. `StructuralIntegritySystem.RockLanded`·`BindCollapseDamageReceiver`가 `crackOverlay`가 null이면 구독·콜백을 버림 → 시스템이 보관했다가 오버레이가 준비되는 순간(`EnsureOverlayHooked`: OnEnable/Update/Bind 때) 연결.
- 따라서 "테스트는 통과하고 실제는 안 된다"는 보고의 원인은 현재 코드에서 확정하지 못했고, 위 취약 지점만 보강했다. 사용자 직접 플레이 확인이 필요하다.

## 변경
- `StructuralCrackOverlay.cs`: 착지 바닥을 낙하 끝에 `SourceCell` 아래로 다시 탐색(`TryFindLanding`). 낙석별 배율은 생성 시 보관하고, 콜백이 늦게 붙으면(NaN) 접촉 시점에 읽음. 배율 ≤0·콜백 없음은 ×1. `PlayFallingTile`(타일맵에 없는 타일도 낙하) 추가.
- `StructuralIntegritySystem.cs`: 늦은 오버레이 연결, `TryDropTile`(터미널로 게임이 멈춘 동안은 닫힌 뒤 시작). 팀원 쪽 `TryPlayDebugCollapse`는 중복이라 제거.
- `IntegrationRuntimeBinder.cs` ~1026: `bootstrap` null 안전. 금 `.gold` ×1.3은 카탈로그를 못 읽어도 적용(기존 로직).
- `PlayerSurvivalController.cs`: 착지 처리를 `ResolveLanding`으로 분리해 `TrackFall`과 `SimulateFall`(디버그)이 공유. 별도 공식 없음, 새 효과음 없음.
- `GasExposureEffectController.cs`: 도트 입력 생성(`RaiseDamageTick`)을 공통화하고, 디버그 `DebugRaiseDamageTicks`(첫 틱 즉시, 이후 틱 간격 1초) 추가.
- 디버그 명령: `DeveloperDebugCollapseCommands.cs`(신규) + `DeveloperDebugSpawnCommands.cs`, `DeveloperDebugCommandRegistry.cs`(등록).
  - `hit <type> [golden]`: `ApplyCollapseImpact(배율)`만. 무적 시간 적용, 낙석·가스 없음.
  - `fall <type> [golden]`: 머리 위 3칸에서 실제 낙석 경로로 떨어뜨림(피해 + 가스/리튬 착지 가스).
  - `hitgas [n]`: `FailureInputRaised`(실제 도트 경로) n번, 틱당 10, 간격 1초.
  - `falling [n]m`: `SimulateFall` → `ApplyFall`+착지음. 인자 없음·10m 미만(0·음수 포함)은 10m. 10/30/40m → 피해 10/30/40.
  - `spawn <type> [golden]`: Enter 자동 채굴과 같은 칸(`MiningSystem.TryGetDirectionalCell`, 사거리는 `PlayerMiningController.Reach`)만 바꾸고, 블록이 없으면 `눈앞에 바꿀 블록이 없습니다`.
  - 타입: `rock`(별칭 `normal`/`일반`/`암석`), `copper`, `iron`, `lithium`, `gas`, `engine_fuel`. 금은 `golden`(별칭 `gold`/`골드`), `engine_fuel`은 금 불가.
- 테스트 러너: `PromptB193CollapseDamageTestRunner.cs`(메뉴 `SubTerra/Tests/Run Prompt-B 193 ...`, Temp 플래그).
- 팀원 테스트 `Issue289CollapseIntegrationTests.cs`: 메시지 기대값만 이 구현에 맞게 수정(`falling`·`hitgas`·`fall` 반환 문구, `hitgas` 첫 틱 즉시).

## 테스트 결과 (Unity 6000.5.4f1 에디터)
- 신규 EditMode(`PromptB193DebugCommandTests`, `Issue289LandingGasTests`)와 수정한 `PromptB125SpawnCommandTests`, `IntegrationWiringTests` Issue289 항목: **통과 73건**.
- 새 PlayMode `Issue289CollapseDamagePlayModeTests` 3건: **통과**(실제 Bootstrap→광산 씬, 이어하기 경로, 터미널 명령 hit/fall/hitgas/falling/spawn 전부).
- 기존 붕괴·생존·채굴 PlayMode 회귀(Structural/Player/Mining): 86건 통과, 1건 실패 `LadderAnimation_ActiveTriggerContact_PrioritizesLadderFrames`(`TargetParameterCountException`, 내 변경 파일과 무관).
- 실패(내 변경과 무관해 보임, 기준선 재실행은 안 함): EditMode `M_F02`·`M_F04`(`BuildingPlacementSystem.cs:179` NRE), `PlayableMineScene_HasRequestedCharacterTerrainAndMiningLayout`(씬 지형 `(-13, -40)`).
- **미검증**: 팀원 `Issue289CollapseIntegrationTests`는 `-subterra-save-root` 인자가 필요해 에디터 실행으로는 못 돌렸다(기대값만 수정). 실제 게임 화면에서 사람이 직접 하는 플레이 확인(아래 가이드 1~11), Console 새 오류·경고 확인, 배치모드 실행.

## 사용자 테스트 가이드 (직접 플레이)
1. 광산 씬(`Bootstrap`에서 시작)에서 Ctrl+`로 디버그 터미널을 연다.
2. 체력을 채운 뒤 `hit rock`, `hit copper`, `hit iron`, `hit lithium`, `hit rock golden`, `hit iron golden`을 무적이 끝난 뒤 하나씩 입력해 25 / 37.5 / 50 / 20 / 32.5 / 65가 깎이는지 본다.
3. `hit copper`를 연달아 두 번: 0.75초 무적 동안 두 번째는 `피해 없음`.
4. 머리 위가 열려 있고 발밑에 바닥이 있는 곳에서 `fall gas`, `fall lithium`: 터미널을 닫으면 머리 위에서 떨어져 낙석 피해와 착지 가스가 같이 나온다.
5. 같은 자리에서 `hit gas`: 피해만, 가스 없음.
6. 아래가 끝까지 빈 구멍 위에서 가스 블록이 떨어지게 하면 가스가 안 나온다.
7. `hitgas`는 체력 10, `hitgas 3`은 1초 간격으로 10씩 세 번.
8. Enter로 채굴될 눈앞 칸을 확인하고 `spawn iron`: 그 칸만 바뀐다. 옆이 비고 위·아래에만 블록이 있어도 Enter와 같은 칸을 쓰고, 빈 칸에는 놓지 않는다.
9. `falling`, `falling 30m`, `falling 40m`, `falling 5m`: 10 / 30 / 40 / 10.
10. 가능하면 명령 없이 실제 광물·금·가스 블록이 떨어져 맞게 하고 2·4와 같은지 본다.
11. Unity Console에 새 오류·경고가 없는지 본다.

## 완료 체크리스트
- [ ] 브랜치 push 확인 — 미수행(승인 대기)
- [ ] 커밋 해시 기재 — 미수행
- [x] Unity 컴파일 오류 없음
- [x] EditMode 새 실패 없음(Issue289·B-125 포함, 기존 무관 실패 3건 별도)
- [x] 실제 광산 씬 PlayMode에서 암석/구리/철/리튬/금 낙석 피해 기대 값 확인(자동 테스트)
- [x] 가스·리튬 착지 가스 1회, 바닥 없을 때만 0회(자동 테스트)
- [x] 무적 시간 중 낙석 피해 겹치지 않음
- [x] `hit`은 피해만, `fall`은 피해+착지 이벤트
- [x] `hitgas`/`hitgas 3` 도트 경로
- [x] `spawn`이 Enter와 같은 칸만 바꿈
- [x] `falling`이 `ApplyFall`+착지 경로를 탐
- [ ] 사람이 직접 한 실제 플레이 확인 — **미검증**
- [x] 새 SFX 없음
- [x] #314 동작 유지(붕괴 로직 순서 변경 없음, 기존 Structural PlayMode 통과)
- [x] main 직접 커밋 없음 / 새 PR 안 만듦
