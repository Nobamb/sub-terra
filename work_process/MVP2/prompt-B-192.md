# B-192 인벤토리·건설창 검색창·정렬 드롭다운 클릭음 (#307)

## 목표
인벤토리와 건설창(`>>` 큰 목록)의 검색창·정렬 드롭다운을 설정창 드롭다운과 같이, 이미 있는 `SfxId.UiClick`만으로 누를 때 클릭음이 나게 한다.
기준 이슈: https://github.com/Nobamb/sub-terra/issues/307 최신 본문. 선행: #268/PR #275(B-190), #304/PR #310(B-191~191-3).

## 브랜치
1. `git fetch` 후 최신 `origin/main`(`09dd4c6b`, #310/B-191 병합. 그 앞 `570aaaa`는 #307이 적을 당시 기준)에서 새 브랜치 **`MVP2-fix137`**을 만든다.
2. 원격에 `MVP2-fix137`이 이미 있으면 비어 있는 다음 번호를 쓰고 보고서 맨 위에 이유를 적는다.
3. 보고서 맨 위에 기준 커밋 해시와 main에 #310이 포함돼 있는지 적는다.

## 절대 규칙 (git-commit.md 11)
1. `main`에 직접 커밋·push 하지 않는다. 변경은 `MVP2-fix137`에서만.
2. 브랜치를 push만 한다. **PR은 만들지 않는다.** PM이 PR을 열고 확인 뒤 병합한다. 브랜치는 지우지 않는다.
3. 새 음원, 새 `SfxId`, `SfxLibrary`, 음량을 추가하거나 바꾸지 않는다. `SfxPlayer.Play(SfxId.UiClick)`만 쓴다.
4. 검색·정렬 동작, 창 TV 열림·닫힘 소리, 설정창 드롭다운 소리는 그대로 둔다. 공용 코드로 빼면 설정창 소리가 바뀌지 않았는지 확인한다.
5. 인벤토리 검색 포커스는 전역 일시정지를 쓰지 않는다(`UiPauseGate.AcquireInput`). 검색 중 기존 효과음이 끊기면 안 된다.
6. 프리팹 YAML을 손으로 고치지 않는다. 런타임 연결을 우선하고, 프리팹이 필요하면 빌더(`PromptB112InventoryPanelBuilder`, `PromptB188BuildingModeBuilder`)를 고쳐 다시 생성한다.
7. GitHub 이슈·PR의 체크박스는 고치지 않는다. 결과는 보고서에만 적는다.
7. 줄 번호는 `09dd4c6b` 기준 근사(~). 못 찾으면 "못 찾음"으로 보고.

## 먼저 읽을 것
- 이슈 #307 최신 본문(#307의 행 번호는 `570aaaa` 기준이라 아래 main 행을 따른다)
- 설정창 기준 구현: `SettingsMenuSkin.WireDropdownTriggers()` ~629
  - 닫힌 `TMP_Dropdown`을 왼쪽 클릭(`PointerDown`)해 펼칠 때만 `SfxPlayer.Play(SfxId.UiClick)` (`IsInteractable() && !IsExpanded`)
  - `onValueChanged`로 항목이 바뀌면 클릭음 1회
  - 이미 열린 드롭다운을 다시 눌러 닫기(`IsExpanded`), 호버, 코드로 값 바꾸기는 이 훅에서 소리가 안 나게 되어 있다
- 재생: `SfxPlayer.Play` → 음소거(`SettingsRuntimeApplier` 마스터·효과음)와 연타 제한(`MinRepeatInterval = 0.05f`)을 거친다. 버튼용 `UiSfx.Attach`는 드롭다운·검색창에 그대로 붙이지 않는다(닫기 클릭·호버·겹침 위험).
- 인벤토리: `InventoryPanelView.cs` `searchField` ~34, `sortDropdown` ~35(`InventorySortDropdown` : `TMP_Dropdown`), `WireManifestControls()` ~304, `OnSearchSelected()` ~341(지금은 입력 차단만, 클릭음 없음), `SetSort`/`ResetManifestControls`는 이미 `SetValueWithoutNotify`
- 건설창: `BuildingMenuView.cs` `searchField` ~63, `sortDropdown` ~79(`TMP_Dropdown`, 실제 컴포넌트 타입은 확인), `WireControls()` ~697(검색은 `onValueChanged`만, 정렬도 `onValueChanged`만), `SetSort` ~557 / `ResetMode()` ~609도 `SetValueWithoutNotify`. `>>` 버튼은 이미 `UiSfx.Attach`(건드리지 않음)
- 테스트 참고: `SfxPlayer.CreateForTests`, `StartedCount`, 기존 `PromptB177SfxMenuTests`·`PromptB157SettingsAudioTests`·`PromptB112InventoryPanelTests`·`PromptB191BuildingModeTests`

## A. 정렬 드롭다운 (인벤토리 · 건설창 `>>` 목록)
- 설정창과 같은 규칙:
  - 닫힌 드롭다운을 눌러 펼침 → 클릭음 1회
  - 항목을 골라 정렬이 바뀜 → 클릭음 1회
  - 항목 호버, 열린 드롭다운을 다시 눌러 닫기, 바깥을 눌러 닫기, 같은 항목을 다시 고르기 → 무음
  - 창을 다시 열며 기본 정렬로 돌아가는 코드 경로(`SetValueWithoutNotify`) → 무음
- 설정창 `WireDropdownTriggers`와 같은 조건을 재사용하거나, 공용 헬퍼로 빼서 인벤토리·건설·(선택)설정에 연결한다. 설정 동작을 바꾸면 안 된다.
- 펼침과 항목 선택을 한 번씩만 내게 한다. PointerDown과 `onValueChanged`가 같은 동작에서 둘 다 나면 겹친다. 연타 제한에 가려져 한 번처럼 들려도 훅이 두 번이면 버그로 본다.
- 검색어·목록 필터·레일 순서는 그대로.

## B. 검색창 (인벤토리 · 건설창 `>>` 목록)
- 마우스로 검색창을 눌러 입력을 시작할 때(포커스가 생길 때) 클릭음 1회.
- 글자 입력(`onValueChanged`, IME 조합 포함)은 무음. 초성·조합 중 검색은 그대로.
- 이미 입력 중인 검색창을 다시 눌러도 소리가 겹치지 않는다.
- 코드로 포커스를 주거나, 창이 열리며 포커스가 가거나, 검색어를 `SetTextWithoutNotify`로 비울 때는 무음. `onSelect`만 붙이면 이런 경우에도 날 수 있으니, 클릭으로 포커스가 생긴 경우만 낸다(확인 후 보고서에 적는다).
- 인벤토리 `OnSearchSelected`의 `UiPauseGate.AcquireInput` 경로는 유지한다. 전역 일시정지로 되돌리지 않는다.

## C. 범위
- 대상은 인벤토리 검색·정렬, 건설창 `>>` 큰 목록의 검색·정렬뿐이다. 레일·시설 행·설정 드롭다운·다른 창의 드롭다운은 이 작업에서 소리를 더하거나 빼지 않는다.

## 테스트 (EditMode 추가)
- 가능하면 공용 규칙 또는 `SfxPlayer.CreateForTests`+`StartedCount`로:
  - 닫힌 드롭다운 펼침 1회, 다른 항목 선택 1회
  - 호버·열린 채 닫기·같은 항목·`SetValueWithoutNotify` 초기화 0회
  - 검색창 첫 클릭 포커스 1회, 입력·이미 포커스인 재클릭 0회
  - 효과음 음소거면 0회. 연속 검사는 `MinRepeatInterval`(0.05초)보다 시계를 밀고 센다
- 기존 B-112·B-191·설정 드롭다운(B-177/B-157) 테스트 새 실패 없음

## 사용자 테스트 가이드 (직접 플레이)
1. I로 인벤토리를 열고 정렬 드롭다운을 눌러 펼친다. 클릭음이 한 번 나는지 본다.
2. 다른 정렬을 고른다. 클릭음이 한 번 나고 목록이 바뀌는지 본다.
3. 같은 정렬을 다시 고르거나, 항목에 마우스만 올리거나, 열린 드롭다운을 다시 눌러 닫아 본다. 소리가 나면 안 된다.
4. 검색창을 누른다. 클릭음이 한 번 나는지 본다. 글자를 칠 때는 소리가 없고 목록만 걸러지는지 본다.
5. 이미 입력 중인 검색창을 다시 눌러 본다. 소리가 겹치면 안 된다.
6. 창을 닫았다 다시 연다. 검색·정렬이 초기화되고 그때는 소리가 없는지 본다.
7. B로 건설 모드를 연 뒤 `>>` 큰 목록에서 1~6을 똑같이 확인한다. `>>`를 누를 때 나는 기존 클릭음은 그대로다.
8. 설정창 드롭다운을 펼치고 항목을 고른다. 예전과 같이 펼칠 때·고를 때 한 번씩 나고, 호버·다시 눌러 닫기는 무음인지 본다.
9. 설정에서 효과음을 음소거하면 인벤토리·건설창에 새로 붙인 소리도 안 나는지 본다. 음소거를 끄면 다시 나는지 본다.
10. 검색·정렬 동작과 창이 TV처럼 열리고 닫히는 소리가 전과 같은지, Unity Console에 새 빨간 오류·노란 경고가 없는지 본다.

## 완료 후
- 한국어 conventional 커밋(예: `feat: 인벤토리·건설창 검색·정렬에 클릭음 연결 (프롬프트 B-192)`). `MVP2-fix137`에 일반 push. PR·브랜치 삭제 안 함.
- 실수를 고쳤으면 `init/rules/lessons/YYYY-MM-DD-issue307-주제.md` 하나로 기록. `init/rules/lessons.md`에 줄 추가 안 함.
- **GitHub 이슈 #307은 고치지 않는다.** 체크는 PM이 한다.
- 결과 보고서 `work_process/MVP2/ui/B-192-search-sort-click-sfx-result.md`
  - 기준 브랜치·커밋, 만든 브랜치, 커밋 해시, 바꾼 파일·줄
  - 설정창과 같은 규칙을 어디에 붙였는지(공용 헬퍼 여부), 검색 클릭음을 어떤 이벤트로 냈는지
  - 테스트 결과, 직접 못 본 항목은 "미검증", 위 사용자 테스트 가이드
- **완료 체크리스트**: [ ] `MVP2-fix137` push 확인 [ ] 커밋 해시 기재 [ ] Unity 컴파일 오류 없음 [ ] EditMode 새 실패 없음(B-112·B-191·설정 드롭다운 테스트 포함) [ ] 인벤토리 드롭다운 펼침·선택 각 1회 [ ] 건설창 `>>` 드롭다운 동일 [ ] 호버·닫기·같은 항목·재오픈 초기화 무음 [ ] 검색창 클릭 1회·입력 무음·재클릭 겹침 없음 [ ] 설정창 드롭다운 소리 유지 [ ] 효과음 음소거 준수 [ ] 새 SFX 없음 [ ] 검색·정렬·TV 열림 동작 유지 [ ] 인벤토리 검색 중 기존 효과음 유지 [ ] main 직접 커밋 없음 [ ] PR 안 만듦
