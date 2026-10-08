# Codex 작업 지시 — Prompt-B 137-1 광산 초기화 타이머 프레임 보완 + TV 전원 등장·퇴장

너는 구현자다. Planner/Reviewer(Claude)가 조사·설계를 끝냈다. 전체 재탐색 금지. 먼저 읽을 것(이 순서):
1. `work_process/MVP2/mine-reset-clock/prompt-b137-1-plan.md` — **구현 기준 문서**(경로·메서드·계층·시간표·범위·순서·검증).
2. `init/prompt-B.md`의 **137-1번** 항목 — 사용자 원문 요구(확정). 계획과 충돌하면 요구가 우선.
3. `CLAUDE.md`, `init/rules/ui-ugui.md`(2-5), `init/rules/code.md`, `init/rules/testing-qa.md`, `init/rules/reporting.md`.
4. 코드: `Scripts/App/UI/HUD/MineResetClockView.cs`, `MineResetClockOverlay.cs`(`BuildClock`, `RefreshFromState`), `MineResetClockStyle.cs`(읽기 전용), `MineResetPopupArt.cs`(패턴 참고), `MineResetClockIntro*.cs`(Update+unscaled 방식 참고, 수정 금지), 기존 테스트 `Tests/EditMode/App/UI/MineResetClockViewTests.cs`, `Tests/PlayMode/Integration/Bootstrap/PromptB137MineResetClockPlayModeTests.cs`, 러너 `Editor/DataValidation/PromptB137MineResetClockTestRunner.cs`.
(경로의 `Scripts/`·`Tests/`·`Editor/`는 `sub-terra/Assets/_Project/` 아래.)

## 해야 할 일
계획서 §5 구현 순서 1~7을 그대로 수행한다. 요약:
1. 순수 시간표 `MineResetClockPowerTimeline`(단일 스칼라 p, 등장 ≈0.40s / 퇴장 ≈0.28s 되감기) + 테스트.
2. 새 프레임 `MineResetClockFrameArt`(절차 생성 9-slice: 네 모서리 대칭 잘림, 금속 림, 안쪽 청록선, 코너 플레이트) 적용. `Window(RectMask2D)/Content(고정 280×78)` 계층 도입. **이 단계에서 프레임부터 스크린샷 확인.**
3. `MineResetClockView.SetPowered/Advance/ApplyPower` + 정리(`OnDisable/OnDestroy`) + 빔·빛점·가장자리 발광·섬광.
4. `MineResetClockOverlay.BuildClock`(라벨을 `Content`로 reparent) + `RefreshFromState`(등장/퇴장 분기, 멱등).
5~7. 테스트 갱신·추가 → 플래그 러너 실행 → PlayMode 검증·연속 캡처 → 범위 확인·결과 문서.

## 원칙 (위반 시 반려)
- **기존 구현을 유지하고 이번 변경만 적용**한다. 7세그먼트·HH:MM:SS·데이터 바인딩·구간색·마지막 1분 맥동·저장/불러오기·0초 종료 흐름·지상 숨김 규칙은 동작 변경 금지. `MineResetClockStyle`/`SevenSegmentGlyph` 로직 변경 금지.
- 실제 코드와 계획이 다르면 근거(파일·줄)를 확인해 계획을 보정하고, 달라진 점과 이유를 결과 문서에 적는다. 사용자 요구를 조용히 생략·변경하지 않는다.
- **표시 여부가 실제로 바뀔 때만** 등장/퇴장. 시간값 갱신, `RefreshFromState` 프레임당 2회 호출, 저장 불러오기(같은 씬 이미 표시 중), UI 재생성으로 등장 연출이 재생되면 안 된다. `SetPowered`는 멱등.
- 숫자·글자를 찌그러뜨리지 않는다: 마스크(`RectMask2D`의 sizeDelta)와 별도 빔/빛점/발광 Image로 표현하고, `Content`는 고정 크기·알파만 변경.
- 금속 바탕(철판·림·코너 플레이트)은 고정색. 구간색은 번짐·안쪽 청록선·세그먼트·콜론·DigitGlow에만.
- 크기·위치 불변: `ClockRoot` 280×78, 앵커/피벗 (0.5,1), 위치 (0,-12), Canvas order 900, 숫자 영역 불변. 모든 Graphic `raycastTarget=false`.
- 표시 연결은 `RefreshFromState` 한 곳. `SaveRuntimeController`, `MineResetService`, `MineResetCycleState`, Save DTO, `MineResetClockIntro*`, `MineResetPopupMotion`, `MineResetTimedPopupSkin*`, 만료 팝업 코드는 수정·참조 추가 금지. 계기판에서 `ExecuteTimedMineReset`/`ShowTimedResetPopup`/`StartCoroutine` 호출 금지.
- 코루틴·DOTween·전역 이벤트 구독 금지(`Update` + `Time.unscaledDeltaTime` 단일 상태, dt는 `Mathf.Min(dt, 1/20)` 제한). 숨은 동안 `Update` 비용 0.
- **원본 프레임 에셋 수정 금지**: `HUD-frame.png`, `mine-reset-frame*.png`(및 `.meta`/임포트 설정), `ResourceSellArt`, `CoreCctvArt`, `EmergencyRescueArt`. 필요하면 읽기·호출만. 새 이미지·폰트·패키지 파일 금지(절차 생성 스프라이트만).
- 범위 밖 Scene·Prefab·Font(`*_SDF.asset`)·TMP 설정·`ProjectSettings`·`Packages`·`init/prompt-B.md` 수정 금지. 특히 `Mine_Demo_Integration.unity`, `SurfaceBase.unity`, `MainMenu.unity`, `SurfaceBasePanel/MainMenuPanel/InventoryPanel/SaveSlotPanel.prefab`, `BasicHUD/HUDCanvas.prefab`. Play/테스트 후 `git status`로 자동 저장 에셋 확인, 있으면 `git restore -- <path>`.
- C# 규칙: `UnityEngine.Object`에 `?.`/`??` 금지, 프레임당 로그·할당 금지, 주석은 필요한 곳만 한국어, 초기화는 try/catch로 원인 로그, 순수 로직은 plain C#.
- 기존 테스트는 **경로만 갱신**하고 단언 의미를 약화하지 않는다(예: 토글 직후 `activeSelf==false` → 퇴장 완료까지 진행한 뒤 `false`).
- **커밋·푸시·브랜치 전환·파괴적 git 명령 금지.** 변경은 작업 트리에 남긴다.
- 실제 사용자 세이브를 변경하지 않는다. 자동화는 메모리 세이브만. 세이브 원문·로컬 경로를 출력하지 않는다. 검증하지 못한 항목을 성공으로 보고하지 않는다. 실패는 출력 그대로 보고.

## 실행 환경 메모
- Unity Editor가 프로젝트를 열고 있다(batchmode 불가). 컴파일·테스트는 플래그 파일: `sub-terra/Temp/subterra-run-editmode.flag` 생성 → `sub-terra/Temp/subterra-editmode-results.txt` 확인(PlayMode는 `...playmode...`). B-137 러너 메뉴/`Temp/subterra-run-prompt-b137.flag` 패턴 재사용. 결과 파일이 안 생기면 대기·재시도 후 원인을 보고(추측으로 성공 처리 금지). 컴파일 오류는 결과 파일·Editor 로그로 확인.
- 3시간을 기다리지 않는다: `SetFormattedClock`+`Advance(dt)` 직접 구동, PlayMode는 개발용 `timer` 디버그 명령/`SetRemaining` 패턴(기존 PlayMode 테스트 참고).
- 연속 캡처: `UiTestWait.Capture`를 프레임 간격(예: 0.04~0.05s)으로 반복해 `work_process/MVP2/mine-reset-clock/b137-1/` 아래 `power-on-NN.png`, `power-off-NN.png`(1920×1080, 청록 기준; 노랑·빨강·마지막 1분은 프레임 3~4장씩)로 저장. 가능하면 각 프레임의 `PowerProgress`·`Window.sizeDelta`를 함께 로그해 방향(중앙→가장자리/가장자리→중앙)을 수치로도 증명. 정지 프레임: 1280×720/1920×1080/2560×1440 × Normal/Warning/Final(밝음·어두움)/`00:00:00`.
- 테스트 시간 제어: EditMode는 `Advance(dt)`로 직접, PlayMode는 실제 프레임 진행.

## 완료 보고 (`work_process/MVP2/mine-reset-clock/prompt-b137-1-result.md` + 최종 답변)
`init/rules/reporting.md` 14번 형식 + 아래:
1. 변경 파일 목록(신규/수정) 및 `git diff --stat`, **범위 밖 파일 0개** 확인(`git status` 결과 요약).
2. 프레임 구현 방식(어떤 기존 에셋·구조를 확인했고 무엇을 재사용/신규로 만들었는지, 층 구조, 고정색 vs 구간색).
3. 등장·퇴장 연결 방법(`RefreshFromState` 분기, `SetPowered` 멱등 규칙, p 시간표 수치, 정리 지점).
4. 계획과 달라진 내용과 이유.
5. 검증 결과: 계획서 §6 각 항목 **통과/실패/미검증**과 근거(테스트 이름·캡처 파일명·로그 수치).
6. 미검증 사항과 남은 제한. 첨부 이미지가 없었다면 한 줄 명시.
