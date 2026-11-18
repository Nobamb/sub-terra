# B-202-1 마우스 과녁이 없을 때 키보드 목표로 되돌리기 (#306, PR #374)

## 목표
PR #374 과녁에서, 마우스가 사거리 밖(또는 캘 수 없는 칸)을 가리키면 과녁이 사라지지 않고, 다시 엔터로 캘 칸에 뜨게 한다. 우선순위 값(`MiningTargetSource`)은 바꾸지 않는다. 마우스를 다시 유효한 칸에 올리면 바로 마우스 쪽 과녁으로 돌아온다.
기준: https://github.com/Nobamb/sub-terra/pull/374#issuecomment-6943303588 (Nobamb, 최신·유일한 수정 요청). 리뷰 댓글·리뷰 본문은 없음.
이 범위 밖의 과녁 모양·조임·숨김 게이트·채굴 시작 규칙은 바꾸지 않는다.

## 브랜치
1. `git fetch` 후 기존 브랜치 **`MVP2-fix146`**(PR #374, head `509f05b269fbc89531f923286a35c3a8e6d1e1dd`)에서 이어서 작업한다. **새 번호 브랜치를 만들지 않는다.** 새 PR도 만들지 않는다. push하면 #374가 갱신된다.
2. head가 이미 더 앞서 있으면 그 위에 이어 작업하고, 보고서 맨 위에 실제 기준 해시를 적는다.
3. 브랜치를 지우지 않는다. PR #374를 병합·닫지 않는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix146`에서만.
2. 브랜치를 push만 한다. **새 PR은 만들지 않는다.** PM이 #374를 확인하고 병합한다. 브랜치는 지우지 않는다. #306 체크박스 수정·이슈 닫기, PR 댓글 작성은 하지 않는다.
3. 새 음원, 새 `SfxId`, `SfxLibrary`, 음량을 추가하거나 바꾸지 않는다. 과녁에 소리를 붙이지 않는다.
4. 큰 화면 전체 모션이 아니면 reduce-motion 분기를 새로 넣지 않는다. 기존 과녁의 `ReduceMotion` 정지 꺾쇠는 그대로 둔다.
5. 프리팹 YAML을 손으로 고치지 않는다. 목표 칸을 고르는 규칙과 실제 채굴 시작 동작은 바꾸지 않는다.
6. 이 댓글이 요청하지 않은 수정(과녁 모양, 우선권 기본값, 창·건설 모드 숨김 등)은 하지 않는다.

## 현재 상태 (`MVP2-fix146` `509f05b2`)
PR 댓글의 재현·원인과 같다.
- `PlayerMiningController.TrackTargetPriority()`(246~266줄): 마우스 `delta`가 있거나 왼쪽 버튼이 새로 눌리면 `targetPriority.NotePointerInput()`으로 `MiningTargetSource.Pointer`가 된다. 엔터 또는 채굴 방향키가 바뀌면 `NoteKeyboardInput()`.
- `TryResolveReticleCell()`(271~294줄): 채굴 중이면 `ActiveCell`에 붙는다. 우선순위가 `Pointer`면 `TryResolvePointerReticleCell`만 본다. 사거리 밖·캘 수 없는 칸이거나, `Mouse.current`/`Camera.main` 없음, `pointerClaimedByPlacement`/`pointerClaimedByDrone`, `IsPointerOverUi()`이면 `return false`라 과녁이 숨고, 키보드 목표로 돌아가는 경로가 없다.
- `TryResolveKeyboardReticleCell` / `TryResolvePointerReticleCell`는 각각 `MiningSystem.TryGetKeyboardTarget` / `TryGetPointerTarget`과 `IsTargetTerrainMineable`을 쓴다.
- `MiningTargetPriority`(`MiningTargetReticle.cs`): `NotePointerInput`/`NoteKeyboardInput`이 `Source`만 바꾼다. 기본은 `Keyboard`.
- 기존 `MiningTargetReticleTests.Controller_PriorityPicksPointerOrKeyboardCell_AndMiningPinsActiveCell`은 포인터 우선일 때 카메라가 없으면 숨는 전제를 주석에 두고, 키보드로 다시 전환한 뒤에야 칸을 확인한다. 이 테스트를 깨지 말 것(필요한 단언만 최소로 맞춘다).

## 할 일
PR 댓글의 수정 제안을 그대로 따른다.
1. `TryResolveReticleCell()`의 `Pointer` 분기에서, 마우스 목표가 없을 때(`TryResolvePointerReticleCell`이 실패하거나, 사거리 밖·캘 수 없는 칸·UI 위·포인터 점유·마우스/카메라 없음으로 지금 `false`가 되는 모든 경우) `return false` 대신 `return TryResolveKeyboardReticleCell(out cell);`로 넘어간다.
2. `targetPriority.Source`는 바꾸지 않는다. 우선순위가 `Pointer`로 남아 있어야, 마우스를 다시 유효한 칸에 올리면 바로 마우스 쪽 과녁으로 돌아온다.
3. 채굴 중 `ActiveCell`에 붙는 기존 분기는 그대로 둔다. 조회·채굴 시작 경로(`TryGetKeyboardTarget`/`TryGetPointerTarget`)는 재설계하지 않는다.
4. `MiningTargetReticleTests.cs`에 「우선순위가 마우스인데 마우스가 사거리 밖이면 키보드 목표 칸에 과녁이 뜬다」 테스트를 추가한다. `NotePointerInput()` 뒤에도 `Source`는 `Pointer`로 남고, `TryResolveReticleCell`은 키보드로 캘 칸을 돌려줘야 한다. EditMode에서 마우스/카메라가 없으면 포인터 목표가 없는 경우(카메라 없음 포함)가 같은 폴백이므로, 그 경로로 검증해도 된다. 기존 테스트가 새로 실패하면 이 폴백에 맞게 최소만 고친다.

## 테스트 (플레이 가이드)
1. 키보드 기준으로 채굴될 칸에 과녁(엔터 채굴 표식)이 떠 있는 상태에서, 마우스를 캘 수 있는 블록 위에 올려 과녁이 마우스 쪽으로 옮겨 가는지 본다.
2. 마우스를 사거리 밖, 또는 캘 수 없는 칸으로 옮긴다. 과녁이 사라지지 않고, 다시 엔터로 캘 칸에 떠 있는지 확인한다. 엔터나 방향키를 새로 누르지 않아도 보여야 한다.
3. 마우스를 다시 사거리 안의 캘 수 있는 칸에 올리면, 과녁이 바로 그 호버 칸으로 돌아오는지 본다.
4. 창이 열려 있거나 건설 모드일 때, 지상 기지에서는 과녁이 없는지, 채굴 시작 동작이 그대로인지를 본다.
5. Unity Console에 새 빨간 오류가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `fix: 마우스 과녁이 없을 때 키보드 채굴 칸으로 되돌림 (프롬프트 B-202-1)`). `MVP2-fix146`에 일반 push. 새 PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue306-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #306과 PR #374 본문·체크박스는 고치지 않는다.** 댓글·병합은 PM이 한다.
- 결과 보고서 `work_process/MVP2/B-202-1-reticle-pointer-fallback-result.md`
  - 기준 브랜치·커밋(작업 시작 head), 커밋 해시, 바꾼 파일
  - `TryResolveReticleCell` 폴백 위치, `Source`를 바꾸지 않은 것
  - 추가한 테스트 이름, 기존 테스트 결과
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix146` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음 [ ] 포인터 목표 없을 때 키보드 칸으로 폴백 [ ] `Source`는 Pointer로 유지, 유효 호버 시 마우스 과녁 복귀 [ ] 채굴 중 ActiveCell 유지 [ ] 요청 테스트 추가 [ ] 채굴 시작·목표 선정 규칙 미변경 [ ] 새 SFX 없음 [ ] 새 PR 안 만듦 [ ] 브랜치 삭제 안 함 [ ] #306·#374 미수정 [ ] main 직접 커밋 없음
