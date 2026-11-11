# B-192 인벤토리·건설창 검색·정렬 클릭음 적용 결과

- 기준 브랜치·커밋: `origin/main`, `09dd4c6bdb0bfc6418a129fa858420d13b8746db`.
- 기준 main에 #310 포함: **예**. 기준 커밋이 PR #310 병합 커밋이다.
- 작업 브랜치: `MVP2-fix137`. fetch 후 같은 이름의 로컬·원격 브랜치가 없음을 확인하고 생성했다.
- 구현 커밋: `29d879266abe29ced4f1ec484ccad60e1594949f`.
- 원격 반영: `git push -u origin MVP2-fix137` 성공. 보고서는 후속 문서 커밋으로 같은 브랜치에 반영한다.
- 요구사항: `work_process/MVP2/prompt-B-192.md`, [이슈 #307 최신 본문](https://github.com/Nobamb/sub-terra/issues/307).
- 작업일: 2026-11-11.

## 변경 파일·연결 위치

경로는 저장소 루트 기준이며, `.cs`의 줄 번호는 변경 후 기준이다.

| 파일 | 위치·변경 |
| --- | --- |
| `sub-terra/Assets/_Project/Scripts/App/Audio/UiSearchSortClickSfx.cs` | 신규 공통 컴포넌트. 16행 Attach, 53행 PointerDown, 71행 정렬 값 변경, 80행 검색 포커스 회차 초기화 |
| 위 파일의 `.meta` | Unity가 생성한 신규 GUID |
| `sub-terra/Assets/_Project/Scripts/App/UI/Inventory/InventoryPanelView.cs` | 306~307행, WireManifestControls에서 기존 검색·정렬 컨트롤에 Attach |
| `sub-terra/Assets/_Project/Scripts/App/UI/Building/BuildingMenuView.cs` | 675~676행, WireControls에서 큰 목록의 검색·정렬 컨트롤에 Attach |
| `sub-terra/Assets/_Project/Tests/EditMode/App/Audio/PromptB192SearchSortClickSfxTests.cs` 및 `.meta` | 92행부터 두 창에 각각 같은 규칙을 적용하는 신규 10건 |
| `sub-terra/Assets/_Project/Tests/PlayMode/Integration/Audio/PromptB192SearchSortClickSfxPlayModeTests.cs` 및 `.meta` | 37행부터 실제 마우스·TMP 포커스·드롭다운 항목 선택 검증 및 화면 캡처 |
| `init/rules/lessons/2026-11-11-issue307-tmp-pointer-focus-timing.md` | PointerClick 전에 TMP 포커스가 활성화될 수 있는 시점 문제 기록 |
| 이 보고서 | 결과·증거·사용자 확인 절차 |

## 구현 규칙

- 인벤토리와 건설창 모두 실제 정렬 컴포넌트는 `InventorySortDropdown`이다. 기존 빌더와 프리팹 참조를 확인했고, 프리팹·Scene을 수정하거나 재생성하지 않았다.
- 두 View의 기존 배선 메서드에서만 `UiSearchSortClickSfx.Attach`를 호출한다. 설정창의 `SettingsMenuSkin.WireDropdownTriggers`는 수정하지 않았다.
- 닫힌 드롭다운의 왼쪽 PointerDown에 `IsActive() && IsInteractable() && !IsExpanded` 조건으로 클릭음을 요청한다.
- 항목 변경은 기존 `onValueChanged`에서 펼쳐진 상태일 때만 클릭음을 요청한다. TMP가 같은 항목 선택에는 값 변경 이벤트를 보내지 않는 실제 PlayMode 경로도 확인했다.
- 검색창은 **왼쪽 PointerDown**에서 `!isFocused`일 때 요청한다. TMP의 다음 LateUpdate 전에 반복 입력이 들어와도 pending 상태로 한 번만 처리한다. 입력 종료·선택 해제·비활성화 때 pending을 푼다.
- 검색창 `onSelect`·`onValueChanged`에는 효과음을 추가하지 않았다. 코드 선택·자동 포커스·문자 입력·IME 조합 갱신·SetTextWithoutNotify는 효과음을 요청하지 않는다.
- 반복 Attach는 같은 컴포넌트를 재사용하고 리스너를 Remove/Add하므로 중복 연결되지 않는다. 원시 `SfxRequests.PlayRequested` 횟수와 실제 `StartedCount`를 함께 검사해 연타 제한에 숨은 중복을 방지했다.
- 효과음 요청은 기존 `SfxPlayer.Play(SfxId.UiClick)`만 사용한다. 새 음원·SfxId·SfxLibrary·음량 변경은 없다.
- 인벤토리 `OnSearchSelected`의 `UiPauseGate.AcquireInput` 경로는 그대로다. 건설창의 기존 정지 경로, 검색·정렬·레일 순서·TV 연출·`>>` 버튼도 변경하지 않았다.
- Inspector 추가 연결은 필요 없다. 기존 View가 실행될 때 자동 연결된다. 세이브 형식 변경도 없다.

## 검증 결과

- Unity 컴파일 오류 없음. 소스보다 새로 생성된 App·테스트 DLL을 확인한 뒤 실행했다.
- 최종 EditMode: **88 Passed / 0 Failed / 0 Skipped**.
  - 신규 B-192 10건, 기존 B-112·B-191·B-177·B-157 묶음 포함.
  - 두 창의 펼침·항목 변경 각 1회, 호버·오른쪽 클릭·비활성·열린 상태 누르기·코드 초기화 무음.
  - 검색 첫 누름 1회, 활성화 전 연속 누름·포커스 중 재클릭·입력·코드 포커스 무음.
  - 음소거 판정을 주입하면 요청이 있어도 StartedCount 증가 없음. 연속 재생 검사는 0.05초보다 시계를 더 진행했다.
- PlayMode: **3 Passed / 0 Failed**.
  - `PromptB192SearchSortClickSfxPlayModeTests.InventoryAndBuilding_RealPointerFocusAndDropdownSelection`.
  - `PromptB177SfxMenuPlayModeTests.Runtime_SettingsTransitionsSliderDropdownAndToggle`.
  - `PromptB112InventoryPanelPlayModeTests.InventoryPanel_LiveValuesStatesAndLayout`.
  - 기존 격리 Bootstrap 런타임·Integration 테스트 환경과 슬롯 0을 사용했다. 사용자 세이브는 사용하지 않았다.
  - 두 창에서 실제 합성 마우스를 누른 채 프레임을 진행한 뒤 놓아 검색 첫 포커스·재클릭을 확인했다. 실제 TMP 펼침·다른 항목·같은 항목 선택도 검증했다.
  - 기존 인벤토리 검사는 검색 중 오디오 루프의 재생 유지도 확인한다.
- 화면: 인벤토리와 건설 큰 목록의 검색·정렬 컨트롤 표시, 검색어 적용 상태 및 큰 레이아웃 이상 없음 확인. 절반 해상도 합본으로 검토했다.
- Console 오류·예외 없음. 기존 코드의 obsolete API 경고(CS0618 등)와 MCP 실행 파일 인증서 경고는 이 변경 범위 밖이다.
- `git diff --check` 통과. 테스트가 자동 변경한 TimeManager·EditorBuildSettings는 원복했으며 공용 설정·패키지·폰트·프리팹·Scene 변경은 커밋하지 않았다.
- 사용자 기존 변경 `init/prompt-B.md`, 미추적 `work_process/MVP2/prompt-B-192.md`는 보존하고 커밋에서 제외했다.

로컬 검증 산출물(무시되는 Builds 폴더):

- `sub-terra/Builds/B192/editmode-final.txt` — 최종 EditMode 결과.
- `sub-terra/Builds/B192/playmode.txt`, `playmode.xml` — PlayMode 결과.
- `sub-terra/Builds/B192/inventory-search.png`, `building-search.png`, `ui-contact.png` — 실제 Game View 캡처·절반 해상도 합본.

## 미검증·사용자 테스트 가이드

**미검증:** 귀로 듣는 최종 음색·음량·TV 소리와의 체감 중첩, 실제 설정창 음소거 토글 후 청음, 물리 마우스로 바깥 차단막을 눌러 닫는 경로, 창 재오픈 전체 구간의 청음. 자동 검증은 효과음 요청·재생 횟수와 화면에 대한 검증이며 청음을 대신하지 않는다.

1. I로 인벤토리를 열고 정렬을 펼친다. 클릭음 1회를 확인한다.
2. 다른 항목을 선택해 클릭음 1회와 정렬 변경을 확인한다.
3. 같은 항목 재선택·항목 호버·열린 드롭다운 재누름·바깥 누름은 무음인지 확인한다.
4. 검색창 첫 클릭에만 클릭음 1회가 나는지 확인한다. 문자·초성·한글 조합 입력은 무음이고 목록만 걸러져야 한다.
5. 입력 중 검색창을 다시 눌러도 소리가 추가되지 않는지 확인한다.
6. 닫고 다시 열어 검색·정렬 초기화 구간에 클릭음이 없는지 확인한다. 기존 TV 소리는 유지돼야 한다.
7. B → `>>` 큰 목록에서 1~6을 반복한다. `>>`의 기존 클릭음도 확인한다.
8. 설정창에서 드롭다운 펼침·다른 항목 선택은 기존처럼 각각 1회, 호버·닫기는 무음인지 확인한다.
9. 설정의 효과음 음소거를 켜고 두 창의 추가 클릭음이 사라지는지, 해제하면 돌아오는지 확인한다.
10. 인벤토리 검색 중 기존 효과음이 끊기지 않고 Console에 새 오류·경고가 없는지 확인한다.

## 완료 체크리스트

- [x] `MVP2-fix137` push 확인, 구현 커밋 해시 기재
- [x] Unity 컴파일 오류 없음
- [x] EditMode 새 실패 없음(B-112·B-191·B-177·B-157 포함)
- [x] 인벤토리·건설창 드롭다운 펼침·선택 각 1회(실제 TMP·원시 요청 검증)
- [x] 호버·열린 상태 누르기·같은 항목·SetValueWithoutNotify 초기화 무음
- [x] 검색 첫 클릭 1회·입력 무음·재클릭 겹침 없음
- [x] 설정창 드롭다운 소리 유지(코드 미변경·기존 PlayMode 검사 통과)
- [x] 효과음 음소거 준수(StartedCount 및 기존 설정 오디오 검사)
- [x] 새 SFX 없음, 검색·정렬·TV 연출 코드 유지
- [x] 인벤토리 검색 중 기존 효과음 유지(기존 PlayMode 검사)
- [x] main 직접 커밋 없음, PR 생성·브랜치 삭제·GitHub 이슈 변경 없음
- [ ] 재오픈 전체·바깥 누름·실제 음소거 토글·TV 소리 포함 최종 청음 — 미검증, 위 가이드로 확인
