# B-210 남자 새 모션 4종 + 피격 경직(hit-stun)

## 목표
1. PM이 만든 **남자** 새 모션 4개를 남자 세트에 넣는다.
   - **가스 피해(damage_new):** 가스 때문에 체력이 깎일 때 입을 막고 기침하는 동작.
   - **사다리 오르기(ladder_new):** 지금 5장 뒷모습 등반을 버리고 새 등반 반복으로. 사다리에 가만히 있을 때도 이 세트 1번.
   - **사다리 피격(ladder_hit):** 사다리를 잡은 채 다칠 때(가스 포함)의 뒷모습 피격.
   - **사다리 채굴(laddermining):** 기존 사다리 채굴을 지우고 새 동작으로.
2. **피격 경직.** 가스 틱·낙하 등 일반 피해를 받으면 짧게(`HitStunSeconds`) 피격 동작을 보이며 그동안 이동·점프·채굴·사다리 오르내리기 입력을 받지 않는다. 사다리에서는 매달린 채 멈춘다.
**여자 새 모션은 이번 범위가 아니다.** 여자 그림은 따로 진행한다. 여자 그림 폴더·`PlayerAnimationSet_Female.asset`은 건드리지 않는다.
관련: #391(이 작업), #266(B-208 성별 세트), #264(모션 교체), #359(B-207 피해 글리치).

## 브랜치
1. `git fetch` 후 최신 `origin/main`에서 새 브랜치 **`MVP2-fix154`**를 만든다. B-208(#381)·B-209(#387)는 이미 병합됐다. 이 문서를 쓸 때의 main은 `bd440b7530cf85c9e0afad1ea3a3d719ec583b6b`이다.
2. 세트 필드는 **추가만** 한다. 나중에 여자 새 모션이 같은 필드를 채울 수 있게 이름을 성별 공통으로 둔다.
3. 원격에 같은 이름이 있으면 빈 다음 번호를 쓰고 이유를 적는다. 브랜치를 지우지 않는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix154`(또는 실제 쓴 번호)에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다.
3. GitHub 이슈 체크박스·댓글·닫기는 하지 않는다.
4. 새 음원, 새 `SfxId`, `SfxLibrary` 변경 없음. 사다리 발소리는 기존 `LadderSfx.OnClimbStep()`.
5. 프리팹 YAML을 손으로 고치지 않는다(빌더/메뉴로). Gameplay가 App을 참조하지 않게 한다. 걷기·땅 채굴·점프·기절 그림, B-207 붉은 글리치, 무적 시간은 바꾸지 않는다.

## 원본 (PM이 넣어 둔다)
`work_process/images/player-new-motions-20261124/source/new_motion_nukki/` — 누끼(배경 제거)와 잡티 정리가 끝난 **256×256 투명 RGBA PNG**. 구조 `<모션>/frames/0001.png`부터.
- `damage_new` 34장 (원본 17장을 2장씩 반복한 것이라 (1·2), (3·4)…가 같은 그림), `new_ladder` 32장, `ladder_hit` 25장, `laddermining` 25장.
- 크로마키·배경 제거·크기 보정은 하지 않는다(발 위치가 기존 256 그림과 맞음).

## 변환
`work_process/tools/prepare_new_motions_20261124.py`로 복사만 한다. 출력은 `Assets/_Project/Art/Characters/Player/Frames/` 아래
`GasDamage/gas_damage_NN.png`, `LadderClimb/ladder_climb_NN.png`, `LadderHit/ladder_hit_NN.png`, `LadderMiningV2/ladder_mining_v2_NN.png`. Import 설정은 기존 256 플레이어 그림과 같게(PPU 256, 중앙 피벗). 장수는 34 / 32 / 25 / 25 그대로. 미리보기 GIF를 `work_process/images/player-new-motions-20261124/`에 남긴다.

## 현재 상태 (main `bd440b7`)
- `PlayerAnimationSet`(남/여 `Art/Characters/Player/AnimationSets/`)과 `PlayerAnimationController.ApplyAnimationSet`. 짧은 세트는 실제 장수로 자른다(B-208).
- **피해.** 체력이 줄면 원인과 관계없이 `HandleHealthChanged` → `"Damage"` 0.4초(`DamageDuration`, `damageFrames` 8장 20fps). `ResolveState`에서 `Damage`가 가장 먼저라 그림만 덮고 **조작은 막지 않는다**(지금은 경직 없음). 사다리면 `IsShowingLadderDamageVisual`, 방향 `LadderDamageDirection`.
- **피해 원인.** `PlayerSurvivalController.ApplyDamage` → `State.TryApplyDamage`(무적 `InvulnerabilitySeconds` 기본 0.75초), `State.LastDamageCause`. 가스는 `GasEffect` 틱 **1초마다** `GasExposure`. 낙하 `Fall`(`SimulateFall` = 디버그 `falling`), 붕괴 `StructuralCollapse`, 산소 0 `OxygenDepleted`(약 1초마다).
- **사다리.** `PlayLadder`가 이동 거리로 `FiveFrameLadderSequence {0,1,2,1,0,3,4,3}`(`ladderDistancePerFrame` 0.25 → 한 바퀴 2유닛), 꼭대기 2·6에서 `OnClimbStep()`. 가만히 0번. 사다리 그림은 뒤집지 않는다.
- **사다리 채굴.** `PlayerLadderMiningAnimationSequence`, 설정 시작~12, 작업 13↔16 핑퐁, 마무리 17–24. 채굴 방향으로 뒤집힌다.
- **가이드.** 키 `player.ladder.1–5`, `GuideDemoLibrary`·`ControlTerminalDemoClips`가 8칸 순서 `{1,2,3,2,1,4,5,4}`, `GuideDemoLibrary` 721행 `Animate("player.ladder", 5, …)`. 성별 장수는 `GuidePlayerArt`. 빌더 `PromptB140GameGuideSkinBuilder`.

## 할 일
1. **세트 확장.** `PlayerAnimationSet`에 `gasDamageFrames`, `ladderHitFrames`, `ladderStepFrames`(발소리 그림 번호) 추가. **남자 에셋만** 빌더로 채우고 `ladderFrames`·`ladderMiningFrames`를 새 그림으로 바꾼다. 여자 에셋은 그대로 두고, 새 필드가 비어 있는 세트는 **지금 그림으로 대체**(가스 → `damageFrames`, 사다리 피격 → 지금 사다리 피해 그림)해서 오류 없이 돈다. `Validate Player Animation Runtime`은 남자 장수(34/32/25/25)를 검사하고 여자 빈 필드는 경고만.
2. **피격 경직.** 상수 **`HitStunSeconds = 0.4`**(검사기 조절, 보고서에 적기). 기존 `DamageDuration`도 이 값을 쓴다.
   - **켠다:** `GasExposure`, `Fall`, `StructuralCollapse`로 체력이 실제로 깎였을 때.
   - **켜지 않는다:** `OxygenDepleted`(산소 0 틱). 산소 전용 연출은 나중에 따로 한다. 산소 틱은 지금 그림·규칙 그대로 두고 경직만 없다.
   - 경직 동안 이동·점프·채굴·사다리 오르내리기·사다리에서 뛰어내리기 입력을 무시한다. 땅에서는 그 자리에 선다. 공중이면 좌우 조작만 없고 중력대로 떨어진다. **사다리에서는 매달린 채 멈추고 떨어지지 않는다.** 채굴 중이었으면 끊고, 경직이 끝난 뒤 버튼을 계속 누르고 있으면 다시 채굴(땅은 `ResumeAfterDamage`, 사다리는 사다리 채굴 이어서).
   - **영구 경직 금지.** 경직 중 새 피해가 와도 **늘리거나 다시 시작하지 않는다**(끝 시각은 처음 경직 그대로). 무적 0.75초 > 경직 0.4초라 보통 겹치지 않고, 가스 틱 1초 사이에 최소 0.6초는 움직일 수 있다. 검사기에서 `HitStunSeconds`가 무적 시간 이상이면 경고.
   - 경직 판단은 Gameplay(예: `PlayerSurvivalController.IsHitStunned`)에 두고 `PlayerMovement`·`PlayerMiningController`가 읽는다. 기절(`CanAct == false`)이 경직보다 우선.
3. **피격 그림(경직 시간과 같은 길이).**
   - 땅에서 `GasExposure` → 가스 피해 34장을 **`HitStunSeconds` 안에 처음부터 끝까지 시간 압축**해 보인다(장 번호 = 경과/0.4×34, 화면 프레임에 맞춰 건너뜀). 처음·끝이 선 자세라 앞뒤가 이어진다.
   - 땅에서 `Fall`·`StructuralCollapse` → 지금 `Damage` 8장 그대로(0.4초).
   - 사다리를 잡고 있을 때(오르기·가만히·사다리 채굴) → 원인과 관계없이(**가스 포함**) 사다리 피격 25장을 `HitStunSeconds`에 압축. 서 있는 가스 동작은 쓰지 않는다. 방향은 지금 `LadderDamageDirection` 규칙.
   - 산소 0 틱은 지금처럼 `Damage` 그림(경직 없음).
4. **사다리 오르기.** 세트 전체를 한 바퀴로: 올라가면 1→N, 내려가면 N→1, 한 장당 거리 = `ladderCycleDistance`(기본 2유닛) ÷ 장수. 가만히·멈춤 뒤에는 1번. 뒤집지 않는다. 발소리는 `ladderStepFrames`(남자 초안 **7·22**, 발이 가장 높은 장. 보고 고치면 적기). 여자 세트(옛 5장)는 지금 5장 순서·꼭대기 2·6을 그대로 쓴다(장수로 분기).
5. **사다리 채굴.** 남자 세트 구간: 시작 1–8, 작업 9↔18 핑퐁, 마무리 19–25(드릴을 뻗기 / 거의 정지한 드릴 / 등으로 돌아오기). fps는 그대로. 여자 세트(옛 24·23장)는 지금 구간(12 / 13↔16 / 17–24)을 유지해야 하니, 구간 설정을 **세트별**로 둔다(예: `PlayerAnimationSet`에 사다리 채굴 구간, 비어 있으면 지금 프리팹 값).
6. **옛 남자 그림 정리.** 남자 세트·가이드에서 옛 `Frames/LadderBack`·`Frames/LadderMining`을 뺀다. 옛 PNG는 GUID·경로 검색(프리팹, 세트, `GameGuideSkin`, Editor 빌더, 테스트, `.anim`)에서 참조가 하나도 없을 때만 지우고, 남으면 지우지 말고 어디서 쓰는지 적는다. **여자 `Female/Frames/*`는 지우지 않는다.**
7. **가이드.** 남자 사다리 장면을 새 등반으로: 키 `player.ladder.1–32`, `GuidePlayerArt`에 성별별 사다리 장수(여자는 지금 5장·8칸 순서 유지), 남자는 1→32 반복(속도는 지금 장면 길이에 맞춤). 721행도 같이. 남녀 그림이 섞이지 않게.
8. EditMode: 가스·낙하·붕괴 → 경직 0.4초 동안 이동·점프·채굴·사다리 입력 무시, 끝나면 정상. 산소 0 틱 → 경직 없음. 경직 중 재피해 → 끝 시각 그대로. 가스 1초 틱 반복 시 틱 사이 움직임 가능. 사다리 경직 중 위치·사다리 상태 유지(낙하 없음). 땅 가스 → 가스 그림, 땅 낙하 → `Damage`, 사다리 위 모든 피해 → 사다리 피격, 길이 = `HitStunSeconds`. 사다리 오르기 위/아래·1번 정지·발소리. 남자 사다리 채굴 새 구간, 여자 옛 구간 유지. 기존 테스트는 고친 이유를 적는다.

## 테스트 (플레이 가이드)
1. 광산에서 디버그 콘솔에 `gender male`을 친다.
2. 땅에 서서 `hitgas`를 친다. 기침하는 새 동작이 짧게(약 0.4초) 나오고, 그동안 좌우·점프·채굴이 안 되다가 바로 다시 되는지 본다.
3. 걸으면서 `hitgas`를 친다. 그 자리에 잠깐 멈췄다가 다시 걸을 수 있는지 본다. 채굴하면서도 해 본다(멈췄다가, 버튼을 계속 누르고 있으면 다시 채굴).
4. `hitgas 5`를 치고 계속 움직여 본다. 1초마다 짧게 멈추지만 그 사이엔 움직일 수 있고, 계속 굳어 있지 않은지 본다.
5. 땅에서 `falling 30m`을 친다. 기존 피격 동작이 나오고 짧게 조작이 안 되는지 본다.
6. `O2 0`으로 산소를 비운다. 체력이 틱으로 깎일 때 조작이 멈추지 **않는지** 본다.
7. 사다리를 오르내린다. 새 등반이 끊김 없이 반복되고, 내려갈 때 거꾸로 돌고, 멈추면 처음 자세로 서고, 뒤집히지 않고, 발소리가 손발과 맞는지 본다.
8. 사다리에 매달려 `falling 30m`을 친다. 뒷모습 사다리 피격이 짧게 나오고, 그동안 위아래가 안 되며 **사다리에서 떨어지지 않는지** 본다. 끝나면 다시 오를 수 있는지 본다.
9. 사다리에 매달려 `hitgas`를 친다. 서 있는 기침이 아니라 사다리 피격이 나오고 매달린 채 멈추는지 본다.
10. 사다리에서 좌우 블록을 길게 채굴하다 멈춘다. 드릴을 뻗고, 뚫는 동안 멈칫 없이 이어지고, 등으로 돌아오는지 본다. 채굴 중 `hitgas` → 사다리 피격 뒤 채굴이 이어지는지 본다.
11. 가이드 창 사다리 장면이 새 등반 그림인지 본다.
12. `gender female`로 바꿔 2·5·8을 한다. 여자는 지금 그림 그대로지만 경직은 똑같이 걸리고, 오류가 없는지 본다.
13. 새 효과음이 없는지, Unity Console에 새 빨간 오류가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 남자 새 모션 4종과 피격 경직 추가 (프롬프트 B-210)`). `MVP2-fix154`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-b210-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- GitHub 이슈는 고치지 않는다. 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-210-new-player-motions-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일
  - 모션별 장수, 최종 `HitStunSeconds`·`ladderCycleDistance`·발소리 장·사다리 채굴 구간
  - 경직 적용/제외 원인, 재피해 처리, 사다리 경직, 여자 세트 대체 동작
  - 옛 그림 참조 검색 결과와 지운/남긴 파일
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix154` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음 [ ] 남자 4모션 장수 34/32/25/25 [ ] 여자 그림·여자 세트 에셋 미변경 [ ] 가스·낙하·붕괴 경직 0.4초, 산소 0 제외 [ ] 경직 연장·영구 경직 없음 [ ] 사다리 경직 중 낙하 없음 [ ] 땅 가스 = 가스 그림(압축), 사다리 피해 = 사다리 피격 [ ] 새 등반 반복·역재생·1번 정지·뒤집지 않음 [ ] 발소리 장, 새 SFX 없음 [ ] 사다리 채굴 세트별 구간 [ ] 가이드 남자 사다리 새 그림 [ ] 옛 그림은 참조 없을 때만 삭제 [ ] Gameplay→App 참조 없음 [ ] 프리팹 YAML 손수정 없음 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] 브랜치 삭제 안 함
