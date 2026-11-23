# B-208 여자 캐릭터 모션 세트 넣기 + 성별 저장 + 디버그 `gender` (#266 일부)

## 목표
남자 캐릭터와 같은 모션·같은 발 위치로 만든 여자 캐릭터 모션을 게임에 넣는다. 플레이어 그림은 성별별 모션 세트(남자 = 지금 그림, 여자 = 새 그림)를 바꿔 끼운다. 재생 속도·채굴/점프 규칙은 남자와 같다. 여자 세트는 몇 모션이 **1장 짧다**(아래 표). 짧은 세트도 안전하게 돌게 한다. 성별은 새 게임/저장 데이터에 들어가고 기본값은 남자(옛 저장 = 남자). 디버그 명령 `gender male` / `gender female`로 바로 바꿔 볼 수 있다.
기준: https://github.com/Nobamb/sub-terra/issues/266 (그림 교체·저장·가이드 맞춤 항목). **성별 선택 화면은 이번 범위가 아니다(B-209).**
#266은 Nobamb 담당이다(mink 작업 금지 댓글 있음). 이번 작업은 PM 요청으로 진행한다.

## 브랜치
1. `git fetch` 후 최신 `origin/main`에서 새 브랜치 **`MVP2-fix152`**를 만든다. 이 문서를 쓸 때의 main은 `0c51ec15c858428362aa4f4cf55b0e03b4492cc8`이다.
2. 원격에 같은 이름이 있으면 빈 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다. 브랜치를 지우지 않는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix152`(또는 실제 쓴 번호)에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다.
3. GitHub 이슈 체크박스·댓글·닫기는 하지 않는다(#266 포함).
4. 새 음원, 새 `SfxId`, `SfxLibrary` 변경 없음. 발소리 프레임(`footstepFrames` 1·21)도 그대로.
5. 프리팹 YAML을 손으로 고치지 않는다(에디터 빌더/메뉴로 연결). Gameplay가 App을 참조하지 않게 한다. 남자 원본 PNG·meta는 수정하지 않는다. **남자 재생 결과(프레임 순서·시간)는 한 장도 달라지면 안 된다.**

## 원본 (PM이 넣어 둔다)
`work_process/MVP2/female_sprite`에 받은 압축(`female_sprite`)을 그대로 푼다.
- 배경은 **이미 투명**이다(RGBA PNG). 배경 제거·크로마키는 하지 않는다.
- `Idle/player_idle_01.png` — 256×256, 바로 사용.
- 나머지 모션은 `<모션>/video/ComfyUI_000NN__frames/ComfyUI_000NN__0000.png`부터 이어지는 512×512 PNG 연번. **이 PNG만 쓴다.** 같은 폴더의 `.webm`·`.mov`는 쓰지 않고 커밋하지 않는다(`git add` 하지 말 것).
- 연번은 남자 프레임을 2배로 늘린 것이다: **파일 번호 `__{2k−2}` ≈ 남자 k번째 그림.** 홀수 번호는 중간 그림이라 쓰지 않는다.

## 변환 (스크립트로, 손작업 금지)
`work_process/tools/prepare_female_frames.py`를 만든다(기존 `prepare_walk_40f.py`·`prepare_b17x_frames.py` 방식 참고). 결과는 `Assets/_Project/Art/Characters/Player/Female/Frames/<모션>/`에 **남자와 같은 파일 이름**으로(빠진 번호는 만들지 않는다).
1. 512 → 256 축소(LANCZOS). 발 위치·키가 남자와 맞으니 크기·위치 보정 없이 같은 캔버스 그대로. 사다리도 256 캔버스·PPU 256(남자 1254/PPU 1254와 같은 1유닛 크기).
2. Import 설정은 남자 같은 모션 PNG와 같게(Sprite, PPU, 중앙 피벗, filterMode, 압축).
3. `LadderBack/…__0006.png`(style_05가 될 그림)에 초록 점 잡티가 약 650px 있다. 초록이 강한 작은 점만 투명하게 지운다. 다른 사다리 그림과 같은 연번에서만 가져오고, 따로 그린 선명한 PNG를 섞지 않는다(이 압축에는 없다).
4. 마지막 1장이 없으면 **복제하지 않고 그냥 뺀다.**

| 모션 | 원본 연번 (번호) | 남자 | 여자 |
|---|---|---|---|
| Idle | `player_idle_01.png` | 1 | 1 |
| Walk | `00051` 0000,0002…0076 | 40 | **39** |
| Mining | `00047` 0000…0044 짝수 | 24 | **23** |
| LadderMining | `00043` 0000…0044 짝수 | 24 | **23** |
| JumpReady | `00015` 0000…0012 짝수 | 8 | **7** |
| JumpRise | `00019` 0000…0044 짝수 | 24 | **23** |
| JumpFall | `00021` 0000…0044 짝수 | 24 | **23** |
| Knockout | `00023` 0000…0092 짝수 | 48 | **47** |
| LadderBack | `00033` neutral_v2=0008, style_02=0000, 03=0002, 04=0004, 05=0006 | 5 | 5 |
| Damage | `00013` 0000,0006,0011,0015,0022,0026,0030,0035 → damage_01, 1_2, 02, 2_3, 03, 3_4, 04, 4_idle | 8 | 8 |

- Damage는 여자 그림만 있는 37장이다(남자 비교 영상 없음). 37장을 처음부터 보면 서 있음(0000) → 맞고 젖혀지기 시작(0006·0011) → 가장 크게 젖혀짐(0015·0022·0026) → 돌아옴(0030) → 거의 선 자세(0035) 순서다. 남자 8칸(20fps, 0.4초)의 자세 순서와 맞춰 위 8장을 고른다. 어색하면 바로 옆 번호로 바꾸고 보고서에 적는다.
- 옛 `Jump/` 10장·`LadderRig/`·`.anim` 클립은 여자 버전을 만들지 않는다(런타임 미사용).
- 미리보기 GIF를 `work_process/images/player-female-20261121/`에 남긴다.

## 현재 상태 (main `0c51ec15`)
- `PlayerAnimationController`(Gameplay/Player)가 `SpriteRenderer`를 직접 바꾼다. 프레임은 `Player.prefab` 직렬화 배열(`idleFrames`, `walkFrames`, `jumpReady/Rise/FallFrames`, `ladderFrames`, `miningFrames`, `ladderMiningFrames`, `damageFrames`, `knockoutFrames`). `jumpFrames`는 비어 있음. 빌더 `Editor/PlayerAnimationAssetBuilder.cs`(`Build Player Animation Assets`, `Validate Player Animation Runtime`).
- **24·48장 전제로 박힌 곳(짧은 세트에서 확인할 것):**
  - 채굴 `PlayerMiningAnimationSequence`: 작업 핑퐁 5↔13(인덱스 4~12), 마무리 15~24(`if (FrameIndex < 23)` 하드코딩).
  - 사다리 채굴: `finishLastFrame` 24.
  - 점프 `PlayerJumpAnimationSequence`: `riseLastFrame` 24(정점에서 24를 한 번 보여 주는 코드 포함), 낙하 루프 18~24, 착지 `landFirstFrame` 8(Ready 8→1 역재생).
  - 기절: `frames.Length >= 48`일 때만 가스 숨쉬기 루프 37~48(`48f`, `% 12`). **47장이면 지금 코드로는 루프가 아예 안 돈다.**
  - 컨트롤러는 인덱스를 `frames.Length - 1`로 자르기만 해서, 짧은 세트면 마지막 장이 두 번 보이는 끊김이 생긴다.
- 가이드/조작 안내 그림: `GameGuideSkin`(Resources/UI) 키 `player.idle`, `player.walk.1–40`, `player.mine.1–24`, `player.ladder.1–5`, `player.jump.4`(jump_rise_12), `player.damage.1`, `player.knockout.1`(knockout_48). 빌더 `PromptB140GameGuideSkinBuilder`. 장수는 `GuideDemoLibrary.PlayerWalkFrameCount`(40)·`Animate("player.mine", 24, …)`로 고정. 쓰는 곳: `GuideDemoLibrary`, `GuideDemoClip`, `ControlTerminalDemoClips`, 메인 메뉴 `ControlSchemePanel`(`GuideSprites.Load(null)`).
- 저장: `GameSaveData`(`SaveVersions.Current` = 6). 디버그: `DeveloperDebugAdvancedCommands`(`timer`, `O2`), `DeveloperDebugCollapseCommands`(`hitgas`).

## 할 일
1. **성별 값.** Shared에 `PlayerGender { Male = 0, Female = 1 }`. `GameSaveData`에 성별 필드(없으면 0 = 남자). 버전을 올려야 하는 구조면 기존 마이그레이션 방식대로 남자로 채운다.
2. **새 게임 진입점.** 새 게임 시작 시 쓸 성별을 한 곳(예: `SaveRuntimeController.StartNewGame(slot, confirmOverwrite, gender = Male)` 또는 새 게임 설정 값)에서 받는다. 이번에는 늘 남자. **B-209 선택 화면이 이 값만 넘기면 되게** 만들고 보고서에 사용법을 적는다. 이어하기는 저장된 성별.
3. **모션 세트.** Gameplay에 성별별 프레임 묶음(예: ScriptableObject `PlayerAnimationSet` 남/여). `PlayerAnimationController`에 세트 교체 메서드를 두고, 바꿔도 지금 상태·채굴/점프 진행을 리셋하지 않고 다음 그리기부터 그림만 바뀐다(인덱스가 새 세트 장수를 넘으면 마지막 장으로).
4. **짧은 세트 안전 처리.** 위 「24·48장 전제」 자리를 모두 **"설정값과 실제 장수 중 작은 값"**으로 바꾼다. 남자는 값이 그대로라 결과가 같아야 한다.
   - 채굴 마무리 끝 = 마지막 장(여자 23). 핑퐁 5↔13은 그대로(23장 안에 있음).
   - 사다리 채굴 마무리 끝 = 마지막 장(여자 23). 작업 13~16 그대로.
   - 점프 상승 끝·정점 강제 표시 = 마지막 장(여자 23), 낙하 루프 18~마지막(여자 18~23), 착지 시작 = Ready 마지막 장(여자 7→1).
   - 기절: 끝까지 재생 뒤 가스 숨쉬기 루프 = 37~마지막 장(여자 37~47, 11장). 조건을 `>= 48` 대신 "37장보다 많을 때"로.
   - 걷기: 장수만큼 루프(여자 39장, 같은 40fps). 발소리 1·21 그대로.
5. **연결.** App/통합 쪽에서 현재 저장의 성별을 읽어 플레이어에 세트를 넣는다. 프리팹 연결은 빌더 메뉴로. `Validate Player Animation Runtime`이 여자 세트 장수를 위 표와 대조해 틀리면 오류를 낸다.
6. **가이드 그림.** `GameGuideSkin`에 여자 키를 추가하고 `GuideSprites`가 현재 성별이면 여자 그림을 준다. 걷기·채굴 애니 장수와 기절 대표 그림은 **성별별 실제 장수**를 쓴다(여자 걷기 39, 채굴 23, 기절 대표 = knockout_47). 여자 애니에 남자 그림이 한 장이라도 섞이면 안 된다. 열려 있는 가이드·조작 안내도 바뀐 성별을 바로 따른다. 메인 메뉴(저장 없음)는 남자.
7. **디버그 `gender`.** 기존 방식(`DeveloperDebugCommandSpec`)으로 `gender male` / `gender female`(별칭 `성별 남`/`성별 여` 정도). **즉시** 플레이어와 가이드 그림에 적용하고 현재 게임 성별 값도 바꿔 다음 저장에 남긴다. 인자 없거나 틀리면 지금 성별과 사용법을 보여 준다.
8. EditMode: 옛 저장 JSON → 남자. 저장/불러오기 왕복. 여자 장수 = 표. **남자 채굴·사다리 채굴·점프·기절 프레임 순서가 바꾸기 전과 같음.** 여자(23·7·47장)에서 인덱스가 장수를 넘지 않고, 낙하 루프 18~23, 숨쉬기 37~47을 돎. 세트 교체 시 상태 유지. `gender` 파싱. 가이드 성별 키·장수.

## 테스트 (플레이 가이드)
1. 옛 저장으로 이어하기한다. 캐릭터가 지금과 똑같은 남자 그림·움직임인지 본다.
2. 새 게임을 시작한다. 남자로 시작하는지 본다.
3. 디버그 콘솔에 `gender female`을 친다. 그 자리에서 바로 여자 캐릭터로 바뀌는지 본다.
4. 여자 상태로 오래 걷는다. 걸음이 끊기거나 멈칫하는 장면 없이 이어지는지 본다.
5. 땅을 길게 채굴하다 멈춘다. 드릴 반복과 마무리 자세가 멈칫 없이 끝나는지 본다. 사다리에서도 같은 것을 해 본다.
6. 높이 뛰어 오래 떨어지다가 착지한다. 떨어지는 동안 반복이 자연스럽고 착지가 어색하지 않은지 본다.
7. 사다리를 오르내린다. 뒷모습 반복이 매끄럽고 초록 점이 없는지 본다.
8. `falling 30m`으로 다친다. 맞는 모습이 남자와 비슷한 박자로 나오는지 본다.
9. `hitgas`로 가스에 쓰러진다. 쓰러진 뒤 숨쉬기 반복이 계속 움직이는지 본다(멈춰 있으면 실패).
10. 채굴 중에 `gender male`을 친다. 채굴이 끊기지 않고 그림만 남자로 바뀌는지 본다.
11. `gender female` 상태로 가이드 창과 조작 안내를 연다. 걷기·채굴·사다리 그림이 모두 여자인지 본다. 연 채로 `gender male`을 치면 바로 남자로 바뀌는지 본다.
12. 여자로 바꾼 뒤 저장하고 메인 메뉴로 나갔다가 이어하기한다. 여자로 나오는지 본다. 메인 메뉴 조작 안내는 남자인지 본다.
13. `gender`만 치거나 `gender abc`를 친다. 오류 없이 사용법이 나오는지 본다.
14. 새 효과음이 없고 발소리 타이밍이 같은지, Unity Console에 새 빨간 오류가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 여자 캐릭터 모션 세트와 성별 저장·디버그 명령 추가 (프롬프트 B-208)`). `MVP2-fix152`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue266-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #266은 고치지 않는다.** 체크·댓글·닫기는 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-208-female-player-anim-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일
  - 모션별 사용 번호·장수, 바꾼 번호와 이유, 잡티 처리, 미리보기 GIF 경로
  - 짧은 세트 처리 위치(채굴·사다리 채굴·점프·기절·걷기·가이드), 남자 결과 불변 확인
  - 저장 필드·옛 저장 처리, **새 게임 성별 진입점 사용법(B-209용)**, `gender` 명령, 가이드 성별 처리
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix152` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음 [ ] 여자 장수 = 표(빠진 장 복제 없음) [ ] webm·mov 미커밋 [ ] 사다리 초록 점 제거, 한 연번에서만 [ ] 남자 재생 결과 불변 [ ] 짧은 세트에서 끊김·범위 초과 없음 [ ] 낙하 루프·숨쉬기 루프 동작 [ ] 옛 저장·새 게임 = 남자 [ ] 성별 저장·이어하기 유지 [ ] `gender male/female` 즉시 적용(플레이어·가이드) [ ] 가이드에 남녀 그림 안 섞임 [ ] 성별 선택 화면 안 만듦(B-209) [ ] 새 SFX 없음 [ ] 남자 원본 미수정 [ ] Gameplay→App 참조 없음 [ ] 프리팹 YAML 손수정 없음 [ ] main 직접 커밋 없음 [ ] PR 안 만듦 [ ] 브랜치 삭제 안 함 [ ] #266 미수정
