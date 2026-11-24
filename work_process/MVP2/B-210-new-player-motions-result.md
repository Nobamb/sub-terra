# B-210 새 모션 4종(남·여) + 피격 경직 결과 (#391)

## 기준
- 기준 브랜치·커밋: `origin/main` `bd440b7530cf85c9e0afad1ea3a3d719ec583b6b` (문서와 같음)
- 작업 브랜치: `MVP2-fix154` (원격에 같은 이름 없음)
- 커밋 해시: `a84b44ce` (기능), 보고서 해시 기재 커밋은 그 뒤
- PR은 만들지 않았다. 이슈는 건드리지 않았다. 브랜치는 지우지 않았다.

## 문서와 다르게 한 것 (요청에 따름)
- **여자 새 모션도 적용했다.** 문서에는 "여자 새 모션은 이번 범위가 아니다"라고 되어 있지만, 요청이 "추가 스프라이트(남자: new_motion_nukki / 여자: female_new_nukki) 참고해서 게임 내에 적용"이었다. 그래서 여자 세트 에셋(`PlayerAnimationSet_Female.asset`)과 여자 그림 폴더에 새 4모션을 넣었다. 옛 여자 그림(`Female/Frames/*`)은 하나도 지우거나 바꾸지 않았다.
- 여자 원본 `new_ladder`는 **25장**이다(남자 32장). 여자 사다리 채굴 새 그림도 남자와 같은 구성이라 같은 구간을 쓴다(아래).
- **채굴 중 경직:** 문서는 "채굴 중이었으면 끊고"라고 했다. 진행도를 0으로 지우면 가스 안에서 1초마다 진행도가 날아가 단단한 블록을 못 깨게 된다. 그래서 경직 동안에는 **진행을 멈추기만** 한다. 경직이 끝날 때 버튼을 누르고 있으면 그대로 이어 채굴하고(그림은 `ResumeAfterDamage`), 버튼을 뗐으면 그 블록 채굴을 끝낸다(취소). 채굴하지 않던 중에 버튼만 누르고 있었으면 경직이 끝날 때 목표를 다시 잡는다.

## 바꾼 파일
- Gameplay(Player)
  - `PlayerAnimationSet.cs`: 필드 추가만 — `gasDamageFrames`, `ladderHitFrames`, `ladderStepFrames`, `ladderMiningRanges`
  - `PlayerLadderMiningAnimationSequence.cs`: `PlayerLadderMiningFrameRanges`(세트별 구간), `SetFrameRanges`. 비어 있으면 설정값(12 / 13↔16 / 17–24)
  - `PlayerAnimationController.cs`: 새 그림 필드, `ladderCycleDistance`(2), 경직 그림 선택·압축 재생(`PlayerDamageVisual`), 6장 이상 사다리 한 바퀴 재생·발소리 장, `DamageDuration` 상수 제거 → `HitStunSeconds`
  - `PlayerSurvivalSettings.cs`: `hitStunSeconds`(기본 **0.4**, 검사기 조절), 무적 이상이면 `OnValidate` 경고
  - `PlayerSurvivalState.cs`: `CausesHitStun`, `TryStartHitStun`(늘리지 않음), `IsHitStunned`, `HitStunEndsAt`, `ClearHitStun`
  - `PlayerSurvivalController.cs`: 실제 피해 직후 경직 시작(알림보다 먼저), `IsHitStunned`, `BindMovement`/`Awake`에서 이동에 연결
  - `PlayerMovement.cs`: `IsHitStunned`, 경직 중 이동·점프·사다리 오르내리기·뛰어내리기·완충판 조절 무시, 땅에서 즉시 정지
- Gameplay(Mining): `PlayerMiningController.cs` — `HandleHitStun`(위 "채굴 중 경직")
- App: `GuidePlayerArt.cs`(사다리 장수 남 32·여 25, `LadderFps`), `GuideDemoLibrary.cs`(사다리 장면 2곳·721행), `ControlTerminalDemoClips.cs`(오르기/내리기)
- Editor
  - 추가 `Editor/DataValidation/PromptB210PlayerMotionsBuilder.cs` — 메뉴 `SubTerra/MVP2/Build Prompt-B 210 Player New Motions`(import 설정, Player.prefab 연결, B-208 빌더로 남/여 세트 갱신, 검사)
  - `PromptB208PlayerAnimationSetBuilder.cs`: 여자 새 4모션·장수 표, 남자 세트가 새 필드도 프리팹에서 복사
  - `PromptB140GameGuideSkinBuilder.cs`: `player.ladder.1–32`, `player.female.ladder.1–25`
  - `PlayerAnimationAssetBuilder.cs`: `Validate Player Animation Runtime`이 B-210 검사도 부름
- 에셋(빌더로 생성·연결, YAML 손수정 없음)
  - 추가 `Art/Characters/Player/Frames/{GasDamage,LadderClimb,LadderHit,LadderMiningV2}/*.png`(116장) + meta
  - 추가 `Art/Characters/Player/Female/Frames/{GasDamage,LadderClimb,LadderHit,LadderMiningV2}/*.png`(109장) + meta
  - 수정 `Prefabs/Gameplay/Player/Player.prefab`(남자 기본 사다리·사다리 채굴 배열 교체, 새 필드), `AnimationSets/PlayerAnimationSet_{Male,Female}.asset`, `Resources/UI/GameGuideSkin.asset`
- 테스트: 추가 `Tests/EditMode/App/Readiness/PromptB210PlayerMotionTests.cs`, 수정(아래 "기존 테스트 수정")
- 도구·자료: `work_process/tools/prepare_new_motions_20261124.py`, `work_process/images/player-new-motions-20261124/`(시트·남녀 비교 GIF·`checks.json`). zip·푼 원본(`source/`)은 커밋하지 않았다.
- 교훈: `init/rules/lessons/2026-11-24-issue391-play-qa-oxygen-ticks-and-autosave.md`
- 씬(`Mine_Demo_Integration` 포함), SFX·`SfxLibrary`, ProjectSettings, 옛 PNG는 바꾸지 않았다. `init/prompt-B.md`, `prompt-B-210.md`는 작업 전부터 있던 PM 변경이라 커밋하지 않았다.

## 모션별 장수 (복사만, 256×256 RGBA, PPU 256·중앙 피벗·FullRect·Bilinear·무압축)
| 모션 | 출력 | 남자 | 여자 |
|---|---|---|---|
| 가스 피해 `damage_new` | `GasDamage/gas_damage_NN` | 34 | 34 |
| 사다리 오르기 `new_ladder` | `LadderClimb/ladder_climb_NN` | 32 | 25 |
| 사다리 피격 `ladder_hit` | `LadderHit/ladder_hit_NN` | 25 | 25 |
| 사다리 채굴 `laddermining` | `LadderMiningV2/ladder_mining_v2_NN` | 25 | 25 |

- 옛 남자 사다리(1254 캔버스, PPU 1084, 몸 높이 약 0.88유닛)와 새 사다리(256, PPU 256, 약 0.93유닛)는 크기가 거의 같다. 크기 보정은 하지 않았다.

## 최종 값
- `HitStunSeconds` = **0.4** (`PlayerSurvivalSettings.hitStunSeconds`, 무적 0.75). 기존 다침 표시 길이도 이 값.
- `ladderCycleDistance` = **2유닛**(한 장 = 2 ÷ 장수: 남 0.0625, 여 0.08).
- 발소리 장: 남자 **9·25**, 여자 **8·20**. 문서 초안(7·22)은 다리 그림을 줄로 놓고 보니 발이 가장 높이 올라간 장이 9·25여서 바꿨다(반 바퀴 간격 16). 여자는 25장의 반 바퀴 간격에 맞춰 8·20.
- 사다리 채굴 구간(남·여 같음): 시작 1–8, 작업 9↔18 핑퐁, 마무리 19–25. fps 그대로(24 / 12 / 60). 세트에 구간이 비어 있으면 프리팹 설정값(12 / 13↔16 / 17–24)을 쓴다.

## 경직 규칙
- 켠다: `GasExposure`, `Fall`, `StructuralCollapse`로 체력이 실제로 깎였을 때(기절로 이어진 마지막 한 방은 제외 — 기절이 우선).
- 켜지 않는다: `OxygenDepleted`. 그림도 지금처럼 `Damage` 8장.
- 재피해: 경직 중 새 경직은 시작하지 않고 끝 시각은 처음 그대로. 경직 그림 중 다시 다쳐도 그림을 다시 시작하지 않는다. 무적 0.75 > 경직 0.4라 가스 1초 틱 사이에 0.6초 움직일 수 있다(테스트).
- 땅: 그 자리에 바로 선다. 공중: 좌우 조작만 없고 중력대로 떨어진다. 사다리: 중력 0 그대로 매달린 채 멈추고 오르내리기·뛰어내리기를 무시한다. 입력 값은 보관해서 경직이 끝나면 누르고 있던 입력이 바로 이어진다.
- 판단 위치: `PlayerSurvivalController.IsHitStunned`(Gameplay). `PlayerMovement`·`PlayerMiningController`가 읽는다. Gameplay→App 참조 없음.
- 피격 그림: 땅 가스 → 가스 피해 34장을 경직 시간에 압축(장 = 경과 ÷ 길이 × 34). 땅 낙하·붕괴 → `Damage` 8장. 사다리를 잡고 있으면 원인과 관계없이(가스 포함) 사다리 피격 25장을 압축. 방향은 `LadderDamageDirection`.
- 새 필드가 빈 세트: 가스 → `damageFrames`, 사다리 피격 → `damageFrames`(지금 사다리 피해 그림), 발소리 → 1/4·3/4 지점, 5장 이하 사다리 → 옛 8칸 순서(테스트).

## 옛 그림 참조 검색
- 남자 `Frames/LadderBack/*`(10장), `Frames/LadderMining/*`(24장): 프리팹·세트·`GameGuideSkin`·씬·`.anim`·`.controller`의 GUID 참조는 **0개**가 됐다.
- 하지만 Editor 빌더가 경로로 아직 가리킨다: `Editor/PlayerAnimationAssetBuilder.cs`(`Apply Approved Ladder Five Frames`, `Build Player Animation Assets`, LadderBack PPU 설정), `Editor/LadderMinerStyleBuilder.cs`, `Editor/LadderMiningAnimationBuilder.cs`, `Editor/DataValidation/PromptB175JumpTestRunner.cs`. 그래서 **지우지 않았다**. 이 옛 메뉴를 다시 돌리면 프리팹 사다리가 옛 그림으로 돌아가니 쓰지 말 것(B-210 빌더를 쓴다).
- 여자 `Female/Frames/LadderBack`·`LadderMining`은 지우지 않았다(세트에서는 빠짐).

## 테스트 결과
- Unity 컴파일 오류 없음(DLL 시각이 소스보다 새것인지 확인).
- EditMode 묶음(`PromptB210PlayerMotionTests`, `PlayerDamageAnimationTests`, `PlayerLadderMiningTests`, `PromptB208FemalePlayerTests`, `PhaseCTraversalStaticTests`, `PlayerRedrawnActionTests`, `PlayerJumpArtTests`, `PlayerWalkCycleTests`, `MiningTargetReticleTests`, `PromptB207DamageHitGlitchTests`, Audio 전체, `PromptB193DebugCommandTests`): **529 통과 / 1 실패**.
  - 실패 1개 `PromptB193DebugCommandTests.Falling_CushionUsesConfiguredRatioAfterCargo(1.7, 0.25)`는 B-207·B-208 보고서의 **기존 실패**와 같은 항목이다(완충판 피해 반올림, 이번 변경과 무관).
- 새 테스트 `PromptB210PlayerMotionTests`: 원인별 경직(산소 제외), 재피해 시 끝 시각 유지, 가스 1초 틱 사이 0.6초 이상 이동 가능, 기절 우선, 경직 중 이동·점프·방향·완충판 무시와 끝난 뒤 복귀, 공중 낙하 유지, 사다리 매달림(속도 0·사다리 유지·뛰어내리기 무시), 땅 가스 = 가스 그림 압축(0.4초에 34장), 땅 낙하/붕괴 = `Damage`, 사다리 위 가스/낙하/붕괴 = 사다리 피격 압축, 산소 = 경직 없는 `Damage`, 경직 그림 재시작 없음, 사다리 1→32/32→1·정지 시 1번·뒤집지 않음, 발소리 9·25, 새 사다리 채굴 구간·빈 구간 대체, 남녀 세트 장수·폴더, 여자 세트 적용(25장 역재생·구간), 빈 필드 대체, 채굴 중 경직(진행 유지·떼면 취소), 가이드 남 32장 32fps·여 25장 25fps·키 안 섞임.
- 테스트 실행 뒤 범위 밖 에셋 변경 없음(`git status` 비교).

### 기존 테스트 수정과 이유
- `PhaseCTraversalStaticTests.PlayerPrefab_UsesFiveSharedLadderFramesInFrontOfLadder`: 5장 `LadderBack` → 32장 `LadderClimb`, `ladderDistancePerFrame 0.25` → `ladderCycleDistance 2`(문서 4번).
- `PlayerLadderMiningTests`: 24장 옛 그림 바이트 비교 → 새 25장 경로·import·구간 8/9/18/19/25 검사로 교체. 작업 재개 13번(인덱스 12) → 9번(8), 마무리 시작 17번(16) → 19번(18). 이름의 "Thirteen"도 바꿨다.
- `PromptB208FemalePlayerTests`: 여자 장수 표(사다리 25, 사다리 채굴 25, 가스 34, 사다리 피격 25), 여자 폴더 PNG 수 = 표 합계 + 남겨 둔 옛 사다리 28장, 남자 세트 = 프리팹 비교에 새 필드 추가, 가이드 여자 사다리 키 25장.
- PlayMode `LadderBackBootstrapPlayModeTests`(실행 인자 없으면 Ignore), `PlayerRedrawnActionPlayModeTests`: 같은 이유로 새 장수·구간에 맞췄다. PlayMode는 실행하지 않았다(**미검증**).

## 플레이 확인 (Bootstrap → 메인 메뉴 → `BeginContinue(1)` → 광산, MCP 스크립트)
세이브 폴더를 백업하고, 끝난 뒤 자동 저장으로 바뀐 `save_slot_1*` 3개를 바이트 비교로 원래대로 되돌렸다.
- 여자, 땅 가스: `gas_damage_13→34`가 0.4초 안에 나오고(첫 0.15초는 기록기 지연), 그동안 `IsMovementRequested=false`·가로 속도 0, 0.400초에 경직이 풀림.
- 여자, 사다리(바위 없는 칸에 사다리 6칸): 위 1→25 반복, 아래 25→1, 멈추면 1번, 뒤집힘 없음. 발소리는 8·20 부근(장이 건너뛸 때는 그 사이). 낙하·가스 → `ladder_hit_01→25` 0.4초, y 고정·사다리 유지, 아래 입력 무시, 끝나면 바로 내려감.
- `PlayerGenderPresentation.Set(Male)` 뒤 남자: 사다리 1→32/역재생/1번 정지, 가스 → 사다리 피격(서 있는 기침 아님), 매달림 유지. 땅 가스 → `gas_damage_01→34` 0.4초, 땅 낙하 → `damage_01…4_idle` + 경직, `O2 0` → 1초마다 `Damage` 그림, 경직 **없음**.
- Console 새 빨간 오류 없음.
- 처음엔 slot 1의 산소가 0이라 산소 틱·무적이 섞여 확인이 꼬였다 → 교훈 파일로 남김.

### 미검증 (직접 눈으로 못 본 것)
- 실제 키보드 입력으로 걷기·채굴하며 `hitgas`, `hitgas 5` 연속 틱 체감(EditMode·스크립트 입력으로만 확인).
- 채굴 중 경직 뒤 버튼을 누른 채 이어 채굴하기: 플레이에서 시험한 블록이 경직 전에 깨져 확인 못 함(EditMode로 진행 유지·떼면 취소만 확인).
- 사다리 채굴 새 그림(드릴 뻗기·작업 핑퐁·복귀)의 화면 확인과 사다리 채굴 중 `hitgas` 뒤 이어 채굴.
- 가이드 창·조작 단말 사다리 장면 화면(EditMode로 키·장수·fps만 확인).
- 발소리가 손발과 맞는지의 귀 확인, 화면 캡처로 새 사다리 그림 크기 비교.
- PlayMode 테스트 실행.

## 사용자 테스트 가이드
1. 광산에서 디버그 콘솔에 `gender male`을 친다.
2. 땅에 서서 `hitgas`를 친다. 기침하는 새 동작이 약 0.4초 나오고, 그동안 좌우·점프·채굴이 안 되다가 바로 다시 되는지 본다.
3. 걸으면서 `hitgas` → 그 자리에 잠깐 멈췄다가 다시 걷는지. 채굴하면서도 해 본다(버튼을 계속 누르고 있으면 이어 채굴, 떼면 그 블록은 멈춤).
4. `hitgas 5`를 치고 계속 움직인다. 1초마다 짧게 멈추지만 그 사이엔 움직일 수 있는지.
5. 땅에서 `falling 30m` → 기존 피격 동작 + 짧은 조작 불가.
6. `O2 0` → 체력이 틱으로 깎여도 조작이 멈추지 **않는지**.
7. 사다리를 오르내린다. 새 등반이 끊김 없이 반복, 내려갈 때 거꾸로, 멈추면 처음 자세, 뒤집히지 않음, 발소리가 발과 맞는지.
8. 사다리에 매달려 `falling 30m` → 뒷모습 사다리 피격, 위아래 안 됨, **떨어지지 않음**, 끝나면 다시 오를 수 있음.
9. 사다리에 매달려 `hitgas` → 서 있는 기침이 아니라 사다리 피격.
10. 사다리에서 좌우 블록을 길게 채굴 → 드릴을 뻗고, 뚫는 동안 멈칫 없이 이어지고, 등으로 돌아오는지. 채굴 중 `hitgas` → 사다리 피격 뒤 버튼을 누르고 있으면 이어 채굴.
11. 가이드 창 사다리 장면이 새 등반 그림인지(남자 32장, 여자 25장).
12. `gender female`로 바꿔 2·5·7·8·9·10을 한다. 여자도 새 기침·새 사다리 오르기(25장)·사다리 피격·새 사다리 채굴이 나오고 경직이 똑같이 걸리는지, 오류가 없는지.
13. 새 효과음이 없는지, Console에 새 빨간 오류가 없는지.

## 완료 체크리스트
- [x] `MVP2-fix154` push 확인
- [x] 커밋 해시 기재
- [x] Unity 컴파일 오류 없음
- [x] EditMode 새 실패 없음(기존 실패 1개 그대로)
- [x] 남자 4모션 장수 34/32/25/25
- [x] 여자 그림·여자 세트: 요청에 따라 **새 4모션 추가**(옛 여자 그림 미변경·미삭제) — 문서 원안과 다름
- [x] 가스·낙하·붕괴 경직 0.4초, 산소 0 제외
- [x] 경직 연장·영구 경직 없음
- [x] 사다리 경직 중 낙하 없음
- [x] 땅 가스 = 가스 그림(압축), 사다리 피해 = 사다리 피격
- [x] 새 등반 반복·역재생·1번 정지·뒤집지 않음
- [x] 발소리 장(남 9·25, 여 8·20), 새 SFX 없음
- [x] 사다리 채굴 세트별 구간
- [x] 가이드 남자 사다리 새 그림(여자도 새 그림)
- [x] 옛 그림은 참조 없을 때만 삭제 → Editor 빌더 참조가 남아 삭제 안 함
- [x] Gameplay→App 참조 없음
- [x] 프리팹 YAML 손수정 없음
- [x] main 직접 커밋 없음
- [x] PR 안 만듦
- [x] 브랜치 삭제 안 함
