# B-193 낙석 피해 배율·착지 가스 수정과 디버그 명령 (#289, PR #312)

## 목표
PR #312(`codex/issue-289-collapse-damage`, `5cda7979`)를 실제 광산 씬에서 플레이해 보니, 낙석 피해가 블록 종류와 상관없이 모두 25였고, 가스·리튬 블록이 땅에 떨어진 뒤에도 가스가 퍼지지 않았다. 테스트는 통과한 것으로 보이지만 실제 광산 경로에서는 거의 동작하지 않는다. 두 문제를 고치고, 확인용 디버그 터미널 명령을 넣는다.
기준: https://github.com/Nobamb/sub-terra/pull/312#issuecomment-6790375085 . Refs #289. 선행: PR #312(광물별 낙석 피해·착지 가스).

아래 원인 후보는 게임을 다시 실행한 결과가 아니라 코드를 읽고 찾은 것이다. 다른 원인이면 그 경로를 고치고, 실제 원인을 보고서에 적는다. 수정 후 확인은 테스트만이 아니라 실제 광산 씬 `Mine_Demo_Integration`에서 한다.

## 브랜치
1. `git fetch` 후 **기존 브랜치 `codex/issue-289-collapse-damage`**(PR #312, 커밋 `5cda7979`)을 체크아웃해 이어서 작업한다. 새 브랜치를 만들지 않는다.
2. `origin/main`이 그 뒤로 앞서 있으면 main을 병합해 충돌을 정리하고 보고서에 적는다. rebase·force push 하지 않는다. 이 프롬프트를 적을 당시 main은 `a3a2b8f8`이고, #314(혼자 남은 블록 떨어뜨리기)·#315/B-192·#267이 들어가 있다. 특히 #314가 같은 붕괴 파일(`StructuralIntegritySystem`, `StructuralCrackOverlay`)을 고쳤으니 그 동작을 되돌리지 않는다.
3. 보고서 맨 위에 시작 커밋 해시와, 병합한 main 해시를 적는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `codex/issue-289-collapse-damage`에서만.
2. 일반 push만 한다. **새 PR은 만들지 않는다.** PR #312는 PM이 확인 후 병합한다. 브랜치는 지우지 않는다.
3. 피격 무적 시간(`PlayerSurvivalSettings.asset`의 `invulnerabilitySeconds: 0.75`)은 유지한다.
4. 낙석 피해·착지 가스·낙사 피해는 기존 공식·이벤트를 그대로 탄다. 별도 계산으로 체력만 깎지 않는다. `falling`은 나중에 블록 지지 점수 감소와 연결될 수 있으니, 실제 착지 경로를 타게 한다.
5. #314의 혼자 남은 블록 떨어뜨리기 동작, 기존 붕괴 예고·낙하 연출, 채굴 가스 생성은 이번 버그 수정에 필요한 만큼만 고친다.
6. 새 효과음, 새 `SfxId`, `SfxLibrary`, 음량을 추가하거나 바꾸지 않는다.
7. GitHub 이슈 #289·PR #312의 체크박스는 고치지 않는다. 결과는 보고서에만 적는다.
8. 줄 번호는 `5cda7979` 기준 근사(~). 못 찾으면 "못 찾음"으로 보고. main 병합 뒤 줄이 바뀌면 새 줄을 보고서에 적는다.

## 먼저 읽을 것
- PR #312 본문과 위 댓글
- 피해 배율: `IntegrationRuntimeBinder.cs` `SetCollapseDamageReceiver(..., ResolveCollapseDamageMultiplier)` ~353, `ResolveCollapseDamageMultiplier` ~1026–1030, `GetCollapseDamageMultiplier` ~1033–1038
  - `miningSystem.TryResolveTile`에 실패하면 무조건 1을 반환한다. 그러면 `StructuralCrackOverlay.PlayCollapse` ~217의 `DamageMultiplier`가 1로 고정되고, `.gold` 접미사 ×1.3에도 도달하지 못한다
  - EditMode `Issue289_CollapseDamageMultiplierUsesMineralCatalogAndGold`는 손으로 만든 DTO만 검증하고, PlayMode는 가짜 람다를 넣는다. 테스트가 통과해도 실제 씬 경로는 실패할 수 있다
- 배율 콜백 연결: `StructuralIntegritySystem.BindCollapseDamageReceiver` ~66–74. 그 시점에 `crackOverlay`가 null이면 아무 것도 하지 않고, `DamageMultiplier`는 `?? 1f`로 남아 25만 들어간다. 씬에는 오버레이가 연결되어 있어도 바인딩 시점을 확인한다
- 카탈로그 캐스트(`bootstrap.AssignedCatalog as GameDataCatalog`) 실패나 `mineralId` 불일치는 광물 무게만 1로 만든다. 금 블록이면 그래도 1.3이 되어야 한다. 실제 플레이에서 금도 25였으므로 위 두 후보 쪽이 더 가깝다
- 낙석 피해 적용: `StructuralCrackOverlay` 접촉 시 `ApplyCollapseImpact(visual.DamageMultiplier)` ~695, `PlayerSurvivalController.ApplyCollapseImpact` ~143–153. 기본 피해는 `minorCollapseDamage: 25`
- 착지 가스 구독: `StructuralIntegritySystem.RockLanded` ~58–62는 `crackOverlay`가 null이면 구독을 버린다. `GameplayEventBridge.OnEnable` ~48에서 구독하는데, 이 시점에 오버레이가 없으면 이후 낙하가 보여도 `OnRockLanded` ~143–149는 한 번도 호출되지 않는다. 테스트는 필드를 넣은 뒤에 `OnEnable`을 호출해서 이 경로를 놓친다
- 착지 조건: `StructuralCrackOverlay` ~704–707. `GroundCell`이 없거나, 착지 시점에 그 바닥 타일이 없으면 가스를 만들지 않는다. `ResolveFallEnd` ~221은 낙하 시작 때 아래 타일을 바닥으로 저장하는데, 같은 붕괴 묶음에서 그 아래 칸도 같이 떨어지면 `HasTile`이 false가 되어 가스가 생략된다. `source.cellBounds.yMin` 때문에 실제 바닥을 못 찾으면 `GroundCell`이 null이 된다
- `GameplayEventBridge.OnRockLanded` ~143–149도 `TryResolveTile` 실패나 `containsGas == false`면 `GasHazardSystem.ActivateAtWorldPosition` ~48–50이 바로 null을 반환한다
- 디버그 터미널: `DeveloperDebugTerminal`(Ctrl+`), `DeveloperDebugCommandRegistry.CreateWithBuiltins` ~305–311, 기존 `DeveloperDebugSpawnCommands.Register`(이미 `spawn` 있음)
  - 기존 `spawn` 인자: `<normal|copper|iron|lithium|gas|engine_fuel> [gold]`. `normal`/일반 = `tile.rock.normal`, `engine_fuel`/엔진연료 = `tile.locked.signal`(금 불가)
  - `TryResolveFacingCell` ~85–108은 가로 한 칸만 보고 빈 칸에도 놓는다. Enter 자동 채굴 목표는 `MiningSystem.TryGetDirectionalCell` ~504–547(비공개)이 옆 칸을 먼저 보고, 없으면 위·아래를 보며 `HasTile`인 칸만 고른다(`IsDirectionalCandidate` ~549–551)
- 가스 도트: `GasExposureEffectController`, `GasExposureEffectModel` 기본값 `healthDamagePerTick = 10`, `tickInterval = 1`초. `PlayerSurvivalController.ApplyGasFailure` ~155
- 낙사: `PlayerSurvivalSettings.asset`의 `minimumFallDamageHeight: 10`, `fallDamageAtThreshold: 10`, `fallDamagePerAdditionalMeter: 1`. `PlayerSurvivalController.ApplyFall` ~174, `TrackFall` 착지 분기 ~528–540
- 기존 테스트: `IntegrationWiringTests`의 Issue289 항목, `StructuralCollapsePlayModeTests`, `PlayerSurvivalControllerPlayModeTests`, `PromptB125SpawnCommandTests`

기대 피해(`minorCollapseDamage: 25`, 카탈로그 `unitWeight` 구리 1.5 / 철 2 / 리튬 0.8, 금 ×1.3):
- 일반 암석 25, 구리 37.5, 철 50, 리튬 20, 금 암석 32.5, 금 철 65

## A. 광물·금 배율이 실제 낙석 피해에 반영되게
- 실제 광산 씬의 낙석이 `GetCollapseDamageMultiplier` 결과를 `PlayerSurvivalController.ApplyCollapseImpact`까지 그대로 전달하게 한다.
- `TryResolveTile`이 실제 떨어진 타일(일반 암석·구리·철·리튬·가스·엔진연료·금 접미사)을 읽게 한다. 실패해서 배율이 1로 남으면 안 된다.
- 배율 콜백과 피해 수신자는 오버레이가 나중에 준비되어도 붙어야 한다. `crackOverlay`가 바인딩 시점에 null이어서 콜백이 빠지면 안 된다.
- 금 블록(`.gold` 접미사)은 광물 무게에 ×1.3을 꼭 곱한다. 카탈로그를 못 읽더라도 금만으로 1.3은 나와야 한다.
- 위 기대 값대로 종류마다 피해가 달라야 한다. 무적 시간 동안 연타 피해는 겹치지 않는다.

## B. 가스·리튬 블록이 바닥에 착지하면 가스가 퍼지게
- 가스가 들어 있는 블록(가스·리튬)이 바닥 타일 위에 떨어져 멈추면, 채굴 때와 같은 종류·농도·범위·지속시간으로 착지 위치에 가스가 한 번 퍼지게 한다.
- `RockLanded` 구독이 오버레이 준비 시점과 상관없이 실제 착지에 연결되어야 한다. 낙하가 보여도 `OnRockLanded`가 안 불리면 안 된다.
- 착지 바닥은 낙하가 끝나는 시점의 실제 바닥을 기준으로 한다. 같은 붕괴 묶음에서 처음 본 아래 칸이 같이 떨어져도, 그 아래에 남는 바닥이 있으면 가스를 생략하지 않는다.
- 떨어진 타일의 `containsGas`가 실제 씬에서도 true로 읽혀야 한다. `TryResolveTile` 실패로 가스가 빠지면 안 된다.
- 아래가 끝까지 비어 바닥 없이 계속 떨어지는 경우에만 가스를 만들지 않는다. 월드 초기화로 취소된 낙하도 지금처럼 가스를 만들지 않는다.

## C. 디버그 터미널 명령
이미 있는 `DeveloperDebugTerminal`(Ctrl+`)과 `DeveloperDebugCommandRegistry.CreateWithBuiltins`에 명령을 등록한다. 기존 `spawn`은 아래 동작에 맞추고, `hit` / `hitgas` / `fall` / `falling`을 추가한다. 가능하면 `DeveloperDebugSpawnCommands` 옆에 두거나 같은 등록 방식을 쓴다. 명령은 광산 씬에서만 동작한다.

타입: `copper`(구리), `iron`(철), `gas`(가스), `lithium`(리튬, 요청에 있던 `litumn`은 `lithium`으로), `engine_fuel`(엔진연료). 일반 암석은 `rock`(`tile.rock.normal`). 기존 `spawn`의 `normal`/일반도 같이 받는다. 금 추가는 `golden` 접미사로 하고, 기존 `spawn`의 `gold`/골드도 별칭으로 받는다. `engine_fuel`에 금을 붙이지 않는 규칙은 유지한다.

- `hit <type> [golden]`: 그 지형이 이미 떨어진 뒤의 낙석 피해만 즉시 적용한다. `PlayerSurvivalController.ApplyCollapseImpact`에 `GetCollapseDamageMultiplier` 배율(금이면 ×1.3)을 넣어, 실제 낙석과 같은 계산·무적 시간을 타게 한다. 착지 가스 같은 낙하 이벤트는 없다.
- `fall <type> [golden]`: 캐릭터 머리 바로 위에 해당 지형을 실제로 떨어뜨린다. `StructuralCrackOverlay.PlayCollapse` 등 실제 낙하 경로를 타서, 피해와 (가스/리튬이면) 착지 가스 확산이 같이 일어나야 한다. `hit`과의 차이는 이 전체 낙하 이벤트다.
- `hitgas [n]`: 가스 누출 도트 피해다. 인자 없이 쓰면 1틱, `hitgas 3`이면 실제 가스 안에 있는 것처럼 틱 간격(1초)으로 3번 적용한다. 값은 `healthDamagePerTick = 10`이다. `ApplyGasFailure` / `GasExposureEffectController`의 실제 도트 경로를 사용한다.
- `spawn <type> [golden]`: 눈앞 자원을 다른 자원으로 바꾼다. 눈앞은 Enter 자동 채굴 목표와 같아야 한다. 옆 칸을 먼저 보고, 없으면 위·아래를 보며 `HasTile`인 칸만 대상이다. 지금 `TryResolveFacingCell`은 가로 한 칸만 보고 빈 칸에도 놓으므로, Enter와 같은 칸을 쓰도록 맞춘다. 바꿀 칸이 없으면 타일을 새로 놓지 않고 실패를 알려 준다.
- `falling [n]m`: 플레이어가 n미터 높이에서 떨어진 것과 같은 피해와 착지 효과를 준다. 인자가 없거나 10m 미만이면 최소 높이 10m부터 적용한다. `falling 30m` / `falling 40m`은 그 높이에서 실제로 떨어졌을 때와 같아야 한다(설정 기준 10 / 30 / 40). `ApplyFall`과 `TrackFall` 착지 분기와 같은 경로를 타고, 별도 공식으로 체력만 깎지 않는다.

## 테스트 (EditMode 추가, 가능하면 PlayMode도)
- 실제 타일 에셋(또는 씬에서 쓰는 리졸버)으로 일반 암석·구리·철·리튬·금의 `GetCollapseDamageMultiplier` / `ResolveCollapseDamageMultiplier`가 위 기대 값인지. 손으로 만든 DTO만 통과하고 실제 타일 경로를 비우지 않는다
- 오버레이가 늦게 준비되어도 배율 콜백과 `RockLanded` 구독이 붙는지
- 가스·리튬 타일이 바닥 위에 착지하면 채굴과 같은 설정으로 가스 1회, 바닥 없이 계속 떨어지면 0회
- `hit`은 피해만, `fall`은 피해+착지 가스(가스/리튬), `hitgas`·`hitgas 3`은 틱 횟수와 간격, `falling` / `falling 30m`은 10·30, 10m 미만도 10m와 같음
- `spawn`이 Enter와 같은 칸만 바꾸고, 옆이 비고 위·아래에만 블록이 있어도 그 칸을 쓰며, 빈 칸에는 놓지 않음. `rock`/`golden` 별칭
- 기존 B-125 스폰 테스트가 빈 칸 배치를 전제로 하면 새 규칙에 맞게 고친다
- 기존 Issue289·붕괴 PlayMode·생존 컨트롤러 테스트 새 실패 없음. 테스트만 통과한 것으로 완료하지 않는다

## 사용자 테스트 가이드 (직접 플레이)
1. 광산 씬(`Mine_Demo_Integration`)에 들어가 Ctrl+`로 디버그 터미널을 연다.
2. 체력을 채운 뒤 `hit rock`, `hit copper`, `hit iron`, `hit lithium`, `hit rock golden`, `hit iron golden`을 무적이 끝난 다음에 하나씩 입력한다. 체력이 25 / 37.5 / 50 / 20 / 32.5 / 65만큼 다르게 깎이는지 본다.
3. `hit copper`를 연달아 두 번 입력한다. 0.75초 무적 동안은 두 번째가 안 들어가고, 무적이 끝난 뒤에만 다시 깎이는지 본다.
4. 머리 위가 열려 있고 발밑에는 바닥이 있는 곳에서 `fall gas`와 `fall lithium`을 입력한다. 블록이 머리 위로 떨어져 땅에 닿은 자리에, 그 블록을 Enter로 캘 때와 같은 가스가 퍼지는지 본다. 맞으면 낙석 피해와 가스가 같이 나와야 한다.
5. 같은 자리에서 `hit gas`를 입력한다. 낙석 피해만 있고 가스는 안 퍼지는지 본다.
6. 아래가 끝까지 비어 있는 구멍 위에서 가스 블록이 바닥 없이 떨어지게 한다. 이때만 가스가 안 나와야 한다.
7. `hitgas`를 입력하면 체력이 10 깎이는지, `hitgas 3`은 1초 간격으로 10씩 세 번 깎이는지 본다.
8. Enter로 자동 채굴되는 눈앞 칸을 확인한 뒤, 같은 자리에서 `spawn iron`으로 그 칸만 바뀌는지 본다. 옆이 비고 위나 아래에만 블록이 있어도 Enter와 같은 칸을 바꾸는지, 빈 칸에는 새 블록을 놓지 않는지 본다.
9. `falling`, `falling 30m`, `falling 40m`을 입력해 10m / 30m / 40m에서 떨어진 것과 같은 피해(설정 기준 10 / 30 / 40)가 들어가는지 본다. `falling 5m`처럼 10m 미만도 최소 10m와 같아야 한다.
10. 가능하면 명령을 쓰지 않고, 실제로 광물·금·가스 블록이 떨어져 맞게 하고 땅에 닿게 해서 2·4와 같은지 본다.
11. Unity Console에 새 빨간 오류·노란 경고가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `fix: 낙석 피해 배율과 착지 가스를 실제 광산 경로에 연결하고 디버그 명령을 추가함 (프롬프트 B-193)`). `codex/issue-289-collapse-damage`에 일반 push. 새 PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue289-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #289·PR #312는 고치지 않는다.** 체크는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-193-collapse-damage-fix-result.md`
  - 시작 커밋, 병합한 main 해시, 만든 커밋 해시, 바꾼 파일·줄
  - 실제 씬에서 배율이 25로만 남던 원인, 착지 가스가 안 퍼지던 원인, 각각 어떻게 고쳤는지
  - 추가한 명령과 `spawn`을 Enter 목표에 맞춘 방식
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `codex/issue-289-collapse-damage` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음(Issue289·B-125 포함) [ ] `Mine_Demo_Integration`에서 암석/구리/철/리튬/금 낙석 피해가 기대 값대로 다름 [ ] 가스·리튬 착지 시 채굴과 같은 가스 퍼짐 [ ] 바닥 없이 계속 떨어질 때만 가스 없음 [ ] 무적 시간 동안 낙석 피해가 겹치지 않음 [ ] `hit`은 피해만, `fall`은 피해+착지 이벤트 [ ] `hitgas` / `hitgas 3` 도트 경로 [ ] `spawn`이 Enter와 같은 칸만 바꿈 [ ] `falling`이 실제 낙사 착지 경로를 탐 [ ] 테스트만이 아니라 실제 광산 씬에서 확인 [ ] 새 SFX 없음 [ ] #314 동작 유지 [ ] main 직접 커밋 없음 [ ] 새 PR 안 만듦
