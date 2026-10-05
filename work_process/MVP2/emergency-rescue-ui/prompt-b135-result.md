# Prompt-B 135 — 전력 고갈 구출 팝업·머리 위 구출 버튼 개편

## 표시 흐름 (구출 규칙·비용·이동·저장은 그대로)
1. 전력이 0이 되면 팝업이 자동으로 나타난다(첫 고갈 한 번만).
2. 닫으면 강한 글리치 종료 연출이 끝난 **뒤에** 플레이어 머리 위 홀로그램 안내가 나타난다.
3. 안내 클릭 또는 R 키 → 같은 팝업이 다시 열린다(열 때마다 `EmergencyRescueService.GetCurrentCost()`로 값을 새로 읽는다).
4. 구출은 팝업의 **구출 요청** 버튼만 실행한다. 안내 버튼·R은 팝업을 여는 입력일 뿐이다.

- 전력이 계속 0이어도 매 프레임 확인하지 않는다. `OnEnergyChanged`가 에피소드를 시작할 때만 판단하고, 이미 보여 준 에피소드는 안내 버튼만 둔다. 전력이 회복되면 에피소드가 초기화되어 다음 고갈에서 다시 자동 표시된다.
- 팝업이 보이는 동안(등장·열림·닫는 중) 안내 버튼은 숨겨진다.

## 변경 파일
수정
- `Scripts/App/Integration/EmergencyRescueRuntimeController.cs` — 재열기·전환 상태 관리, 닫기 완료 뒤 안내 표시, 표시 비용 = 결제 비용 확인, 아이콘 조회, 이벤트 정리
- `Scripts/App/UI/EmergencyRescue/EmergencyRescuePanelView.cs` — 팝업/안내를 묶는 진입점으로 축소(`FormatCost` 제거)
- `Tests/EditMode/App/Run/EmergencyRescueServiceTests.cs`, `Tests/EditMode/App/UI/PromptB85EmergencyRescueChipTests.cs` — 비용 문장 → 표 행, 안내 생성 코드가 옮겨진 파일 경로 반영

신규 (`Scripts/App/UI/EmergencyRescue/`)
- `EmergencyRescueTimeline.cs` — 등장/종료/표시 중 글리치, 안내 등장·호버·키캡 시간표(순수 계산)
- `EmergencyRescuePopupView.cs` — 팝업 본체, 상태기계(Hidden/Opening/Open/Closing), 글리치 레이어
- `EmergencyRescueCostTable.cs` / `EmergencyRescueCostRows.cs` — 비용 표 / 계산 결과 → 행 변환·비교
- `EmergencyRescueChipView.cs` — 머리 위 홀로그램(문구·픽토그램·키캡 분리)
- `EmergencyRescueArt.cs` / `EmergencyRescueUi.cs` — 절차 생성 스프라이트(사람·화살표·코인·키캡·발광) / UI 생성 도우미
- 테스트: `Tests/EditMode/App/UI/PromptB135EmergencyRescueUiTests.cs`, `Tests/PlayMode/Integration/RunFailure/EmergencyRescuePlayModeTests.cs`, 실행기 `Editor/DataValidation/PromptB135EmergencyRescueTestRunner.cs`

프리팹·씬·폰트·ProjectSettings는 바꾸지 않았다. 팝업은 기존처럼 코드로 생성되고, 프레임·패널·버튼 그림은 기존 `MineResetTimedPopupSkin`(Resources)을 재사용한다.

## 팝업
- 880×675 카드, 기존 개선 팝업과 같은 남색 패널·청록 각진 프레임·금속 모서리·버튼 스킨. 붉은색은 제목, 제목 양옆 마름모, 제목 구분선, 프레임 윗부분, 위쪽 옅은 번짐에만 쓴다.
- 비용 표: `자원 아이콘·이름 | 차감량 | 현재 보유 → 구출 후 잔량`. 모든 행이 같은 열 좌표를 쓰고 숫자는 오른쪽 정렬, 천 단위 구분과 단위(G / 개). 줄 수가 많으면 행 높이가 줄어 영역 안에 들어가고, 줄 수가 적으면 가운데로 모인다. 무료면 표 대신 안내 문구.
- 광물 아이콘은 `GameDataCatalog`의 기존 `MineralData.Icon`, 골드는 코인 모양 절차 생성 스프라이트.
- 등장 0.5초: 프레임이 끊겨 나타남 + 가로 노이즈 막대 + 프레임 어긋남(청록/적 유령) + 빛, 청록·붉은·청록 3회 → 선명하게 정착. 재열기도 동일.
- 표시 중: 0.4초 칸마다 약 24% 확률로 0.08~0.16초 글리치. 창 가장자리·바깥 34px 이내 막대와 약한 유령 프레임뿐이며 본문·버튼·레이아웃은 움직이지 않는다.
- 닫기 0.3초: 장식 레이어와 패널 띠(최대 4개)가 가로로 어긋나고 분절, 본문 흔들림, 강한 노이즈, 마지막에 사라짐. 끝나면 루트가 꺼져 입력을 막는 투명 패널이 남지 않는다.
- 버튼은 등장이 끝난 열린 상태에서만 눌린다(등장·종료 도중 클릭으로 같은 입력에 확정되는 일을 막기 위함).

## 머리 위 홀로그램 안내
- 132×168. 위 `구출 요청` / 가운데 픽토그램 / 아래 R 키캡. 사람, 화살표, 발광, 키캡은 각각 별도 오브젝트이고 엘리베이터 레일·발판 위에 놓인다. 붉은색은 윗모서리 눈금 2개뿐.
- 등장 0.32초: 모서리 빛 → 세로로 펼침 → 내용 페이드.
- 호버(레벨 하나로 값 결정, 진입 0.5초/이탈 0.35초): 청록빛 확산 → 사람 10px 상승(Ease In-Out) → 화살표 12px 상승 후 한 번 작게 튐. 유지 중 반복 없음, 이탈하면 원위치. 문구·키캡은 고정. 빠른 진입·이탈을 반복해도 값이 누적되지 않는다.
- 키캡 반복 안내: 등장 1.8초 뒤 첫 안내, 이후 3~5초 간격, 1회 0.34초(3px 하강, 8% 축소, 청록 발광). 호버와 다른 오브젝트·값을 쓰며 입력이나 구출 요청을 만들지 않는다.
- 실제 입력(클릭/R): 키캡이 6px·16%로 더 분명히 눌리고 안내는 0.16초 페이드로 사라진다. 팝업 열기는 이를 기다리지 않는다.
- 모바일 대비: 문구(`TitleLabel`), 픽토그램, 키캡(`SetInputHint`)이 입력 처리(컨트롤러)와 분리되어 있다. 터치 UI는 만들지 않았다.

## 전환·입력 처리
- 열림/등장 중 추가 열기 입력은 무시(중복 생성 없음). 닫는 도중 재열기(R)는 닫기 완료 직후 한 번만 다시 열고 그 사이 안내 버튼은 나타나지 않는다.
- 구출 확정 전 표시 비용과 현재 계산을 비교해 다르면 결제하지 않고 최신 값을 다시 보여 준다(기존 `InventoryChanged` 안내 문구 사용). 결제 자체는 기존 `EmergencyRescueService.TryRescue` 한 번.
- 전력 회복·구출 완료: 팝업·안내·대기 중 닫기 콜백을 연출 없이 즉시 정리.
- 시간: 팝업·안내 모두 `unscaledDeltaTime`이라 `UiPauseGate`로 시간이 멈춰도 재생되고 클릭이 동작한다(PlayMode로 확인). R 키는 기존대로 `UiPauseGate` 중에는 처리하지 않는다.
- 정리: 비활성화·씬 전환 시 상태·효과를 초기화하고, `Unbind`에서 버튼 이벤트를 모두 해제한다.

## 검증
- EditMode: B-135 24개 + 기존 구출 관련 14개(B-81 서비스 9, B-85 안내 5) 모두 통과(시간표 구간·강도, 비용 행·열 정렬, 상태기계, 호버 비누적, 키캡 분리, 정리).
- PlayMode(실제 Integration Scene, 실제 마우스·키보드 이벤트) 10개 통과: 자동 표시 → 닫기 → 안내 표시 → 자동 재등장 없음 / 클릭·R 재열기와 비용 갱신 / 등장·종료 도중 입력과 연속 입력 / 확정 시 표시 비용 = 실제 차감, 연타해도 1회, 이동·정리 / 전력 회복(팝업 중·안내 중·닫는 중)과 다음 고갈 자동 표시 / 시간 정지 중 동작 / 실제 호버·키캡 / 1280×720·1920×1080·2560×1440 화면 안·글자 크기.
- 전체 실행: EditMode 1217/1219 통과, PlayMode 94 통과·1 실패·1 건너뜀.
  - 기존 실패: `IntegrationWiringTests.PlayableMineScene_HasRequestedCharacterTerrainAndMiningLayout`(Missing terrain, B-134 문서에도 기록된 실패), `GasExposurePlayModeTests.H_F02_F05_...`(B-134 문서에 기록).
  - `PromptB49FacilityVisualTests.PromptB49_BuildingMenuIcons_MatchEachFacilityArtwork`: 같은 이름의 Sprite 인스턴스가 달라 실패. 첫 전체 실행에서는 통과했고 건물 아이콘 코드는 건드리지 않았다(에디터 에셋 재임포트 뒤 상태 의존으로 보임).

## 캡처 (`work_process/MVP2/emergency-rescue-ui/`)
- `b135-popup-open-idle-close.gif` — 등장 → 표시 중 간헐 글리치 → 닫기 → 안내 등장
- `b135-chip-hover-keycap-press.gif` — 호버 → 이탈 → 키캡 반복 안내 → 실제 입력 눌림
- `b135-popup-1920x1080.png`, `b135-popup-1280x720.png`, `b135-popup-2560x1440.png`, `b135-chip-1920x1080.png`, `b135-chip-1280x720.png`, `b135-chip-2560x1440.png`
- 캡처 테스트는 연출을 `Tick`으로 30fps 고정 진행하고(`ManualTick`), 안내 연출이 잘 보이도록 캡처 동안에만 드론 대사창 캔버스를 꺼 둔다.

## 남은 사항·제한
- 머리 위 안내의 정렬(600)은 기존 그대로라, 드론 대사창(30000)이 같은 자리에 뜨면 안내를 가린다(전력 0 직후 드론 대사, 18초 뒤 재안내 대사). 안내를 올리면 인벤토리 등 모달(1000) 위로 올라오므로 이번 범위에서 바꾸지 않았다.
- 골드 아이콘은 런타임에서 쓸 수 있는 기존 스프라이트 참조가 없어 절차 생성 코인을 쓴다. 정식 아이콘이 생기면 `EmergencyRescueCostTable`의 아이콘 조회 한 곳만 바꾸면 된다.
- 닫는 연출(0.3초) 중 Esc는 팝업이 소비한다(`IsPanelOpen`이 닫는 중 포함).
- 전체 PlayMode/EditMode 실행은 에디터가 폰트·프리팹·ProjectSettings·다른 프롬프트의 증거 PNG를 다시 저장하게 만든다. 이번 작업 중 발생한 것은 모두 `git restore`로 되돌렸다. 커밋은 하지 않았다.
- Windows 빌드 실행은 확인하지 못했다.
