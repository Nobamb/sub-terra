# 인벤토리 검색 효과음 유지와 한글 즉시 검색 — 2026-11-09

- 기준: `codex/issue-268-inventory-readability`, `7fd80b1f`. 커밋·push 없음.
- 검색 포커스의 전역 일시정지를 입력 전용 차단으로 바꿨다. 월드 입력과 I·B·U 등의 단축키는 차단하고 게임 시간·기존 효과음은 유지한다.
- TMP의 확정 문자열 외에 화면에 표시되는 IME 조합 문자열을 반영한다. 한 글자·초성·혼합 입력(`ㄱ`, `ㄱㄹ`, `구ㄹ`)과 마지막 받침 조합 전 후보(`처` → 철)를 검색한다.
- 기존 정렬·수량 0 제외·전체 적재량 표시·검색 초기화 동작은 유지한다. Inspector 연결 변경 불필요. 세이브 형식 변경 없음.

## 변경 파일

Unity 프로젝트의 `Assets/_Project/` 기준:

- `Scripts/App/UI/Inventory/InventoryPanelView.cs`: 포커스 입력 소유권, 조합 중 문자열 반영, 해제 시 확정 문자열 동기화.
- `Scripts/App/UI/Inventory/InventoryManifestQuery.cs`: 한글 초성·혼합 입력 검색.
- `Scripts/App/UI/UiPauseGate.cs`: 일시정지와 입력 전용 소유권 분리.
- `Scripts/App/Integration/PauseInputSuspender.cs`: 두 종류의 차단을 함께 확인하고 월드 입력을 복원.
- `Scripts/App/UI/HUD/HudPanelChromeController.cs`, `Scripts/App/UI/PanelToggleController.cs`: 검색 중 창 단축키 차단 유지.
- `Scripts/App/Integration/GameplayBuildingPlacementBridge.cs`, `Scripts/App/Integration/UndergroundMenuController.cs`, `Scripts/App/Integration/EmergencyRescueRuntimeController.cs`: 검색 중 월드 건설·설정·구출 단축키 차단 유지.
- `Tests/EditMode/App/UI/PromptB112InventoryPanelTests.cs`: 초성·혼합·받침 조합·빈 자원·다른 정지 소유권 회귀 검사.
- `Tests/PlayMode/Integration/Bootstrap/PromptB112InventoryPanelPlayModeTests.cs`: 실제 인벤토리의 조합 문자열·반복음 재생 위치·단축키·포커스 해제·입력 중 닫기 검사.

교훈: `init/rules/lessons/2026-11-09-inventory-search-input-and-ime.md`.

## 검증

- 수정 전 초성·조합 검색 8건 실패 재현. 수정 후 관련 Edit Mode 66/66 통과.
- Play Mode 2/2 통과. 실제 Integration 인벤토리에서 TMP가 읽는 IME 조합을 테스트 입력으로 제공했다. 사용자 세이브를 사용하지 않는 기존 격리 런타임을 사용했다.
- 반복 효과음은 실제 기본 정지 판정으로 검색 포커스 전후 `AudioSource.isPlaying`과 `timeSamples` 진행을 확인했다. 사용자 음소거 설정은 변경하지 않고 검사 소스의 음소거 판정만 대체했다.
- 1366×768, 1920×1080, 2560×1440 UI 검사 통과. 반해상도 화면 확인에서 `ㄱ` 입력·구리 및 엔진 연료(희귀) 표시·철 숨김·검색 결과 없음 안내와 겹침/잘림 없음을 확인했다. `(희귀)`의 `귀`도 표시 이름 검색 대상이다.
- 결과: `sub-terra/Builds/InventorySearch-20261109/editmode.xml`, `playmode-results.txt`, `search-states-half.jpg` (로컬 무시 파일).
- Unity Console 오류 0. 검사 중 기존 `CoreCctvCameraRig`의 URP/stereoTargetEye 경고 2건과 잘못된 인벤토리 입력 검사에서 의도적으로 발생시킨 경고 4건을 확인했다.
- 초기 전체 B112 검사에 포함된 기존 건설창 폭 검사 1건은 기대값 580.8 / 실제 500으로 실패했다. 인벤토리 수정 범위 밖이라 변경하지 않았다.
- `git diff --check` 통과. Scene·Prefab·폰트·ProjectSettings·Packages 최종 변경 없음. 테스트가 자동 변경한 폰트와 TimeManager는 원복했다. 시작 시 이미 수정된 `init/prompt-B.md`는 건드리지 않았다.

## 미검증 수동 체크리스트

- [ ] 실제 Windows 한글 IME 키보드로 `ㄱ → 구 → 구ㄹ → 구리`, 삭제·재입력·Enter·Esc를 확인한다.
- [ ] 지상 및 광산에서 검색창 선택 전후 주변음·경보·시설 효과음을 실제 스피커로 듣고 끊김이 없는지 확인한다.
- [ ] Windows 빌드에서도 같은 입력·효과음 동작을 확인한다.
