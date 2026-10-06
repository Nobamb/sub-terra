# Codex 작업 지시 — Prompt-B 137 인게임 광산 초기화 타이머 7세그먼트 계기판

너는 구현자다. Planner/Reviewer(Claude)가 조사·설계를 끝냈다. 먼저 읽을 것(이 순서, 그 외 전체 재탐색 금지):
1. `work_process/MVP2/mine-reset-clock/prompt-b137-plan.md` — 조사 결과·파일별 변경·구현 순서·검증. **구현 기준 문서.**
2. `init/prompt-B.md`의 137번 항목 — 사용자 원문 요구사항(확정). 계획과 충돌하면 요구사항이 우선.
3. `CLAUDE.md`, `init/rules/ui-ugui.md`, `init/rules/code.md`, `init/rules/testing-qa.md`, `init/rules/reporting.md`.
4. 코드: `Scripts/App/UI/HUD/MineResetClockOverlay.cs`, `MineResetPopupArt.cs`, `MineResetClockIntro*.cs`(애니메이션 방식 참고), `Scripts/App/Save/MineResetService.cs`(읽기 전용), `Save/SaveRuntimeController.cs:691-870`(읽기 전용), 기존 테스트 `Tests/EditMode/App/UI/MineResetTimedPopupTests.cs`·`MineResetClockIntroTests.cs`, 러너 패턴 `Editor/DataValidation/PromptB135EmergencyRescueTestRunner.cs`.
(경로의 `Scripts/`·`Tests/`·`Editor/`는 `sub-terra/Assets/_Project/` 아래.)

참고 이미지: 사용자가 대화에 첨부한 타이머·디지털 시계 이미지는 파일로 전달되지 않았다. 계획서·요구사항의 수치·구성이 기준이다. 이미지가 없다는 점을 완료 보고에 한 줄 적어라.

## 해야 할 일
계획서 §6 구현 순서 1~7을 그대로 수행한다. 요약: 순수 로직(`SevenSegmentGlyph`, `MineResetClockStyle`) → 절차 생성 스프라이트 → `MineResetClockView` → `MineResetClockOverlay` 교체(라벨 "광산 초기화까지") → EditMode 테스트 → 검증 러너·PlayMode 스모크·스크린샷 → 범위 확인·결과 문서.

## 원칙 (위반 시 반려)
- 계획서에 지정된 파일부터 확인하고 불필요한 전체 재탐색을 하지 않는다.
- 실제 코드와 계획이 다르면 근거(파일·줄)를 확인해 계획을 보정하고, 달라진 점과 이유를 보고서에 적는다.
- 사용자 요구사항을 조용히 생략하거나 바꾸지 않는다. 바꿔야 하면 이유와 함께 보고한다.
- gameplay timer, 종료·저장 로직(`MineResetService`, `MineResetCycleState`, `SaveRuntimeController`, Save DTO)을 변경하지 않는다. 계기판에서 `ExecuteTimedMineReset`/`ShowTimedResetPopup`/`StartCoroutine`을 호출·참조하지 않는다.
- 범위 밖 Scene·Prefab·Font(`*_SDF.asset`)·TMP 설정·`ProjectSettings`·`Packages`·`init/prompt-B.md`를 수정하지 않는다. Play/테스트 후 `git status`로 자동 저장된 에셋이 없는지 확인하고, 있으면 `git restore -- <path>`.
- 기존 구조(코드 생성 UI, `Update`+`unscaledDeltaTime` 단일 상태, 순수 계산 클래스 분리, `MineResetPopupArt` 절차 스프라이트)를 따른다. 새 이미지 파일·폰트·패키지 추가 금지.
- C# 규칙: `UnityEngine.Object`에 `?.`/`??` 금지, 프레임당 로그 금지, 주석은 필요한 곳만 한국어, 주요 초기화는 try/catch로 원인 로그.
- **커밋·푸시·브랜치 전환·파괴적 git 명령 금지.** 변경은 작업 트리에 남긴다.
- 실제 사용자 세이브를 변경하지 않는다. 자동화 테스트는 메모리 세이브만. 수동 `timer` 확인은 새 임시 슬롯에서만 하고 끝나면 삭제하며, 세이브 원문·로컬 경로를 출력하지 않는다. 안전하게 못 하면 미검증으로 보고.
- 검증하지 못한 항목을 성공으로 보고하지 않는다. 실패 테스트는 출력 그대로 보고.

## 실행 환경 메모
- Unity Editor가 이 프로젝트를 열고 있다(batchmode 불가). 컴파일·테스트는 플래그 파일로: `sub-terra/Temp/subterra-run-editmode.flag` 생성 → `sub-terra/Temp/subterra-editmode-results.txt` 확인(PlayMode도 동일, `...playmode...`). 새 러너를 만들면 `Temp/subterra-run-prompt-b137.flag` 패턴. 컴파일 오류는 Editor 로그 `Library/`·결과 파일로 확인. 결과 파일이 안 생기면 대기·재시도 후 원인을 보고(추측으로 성공 처리 금지).
- 3시간을 기다리지 않는다: 순수 로직은 `displaySeconds` 주입, 오버레이는 `SetFormattedClock`+`Advance(dt)` 직접 구동, PlayMode는 개발용 `timer` 디버그 명령(`SetMineResetElapsedSeconds`, Editor/Development 빌드 전용)을 쓴다.
- 스크린샷은 `work_process/MVP2/mine-reset-clock/`에 PNG로(1280×720, 1920×1080, 2560×1440 각각 Normal/Warning/Final(맥동 밝은·어두운 시점)과 `00:00:00`). Claude가 직접 열어 시각 리뷰한다.

## 완료 보고 (`work_process/MVP2/mine-reset-clock/prompt-b137-result.md` + 최종 답변)
`init/rules/reporting.md` 14번 형식 + 아래를 포함:
1. 변경 파일 목록(신규/수정) 및 `git diff --stat`, 범위 밖 파일 0개 확인.
2. 구현 방식 요약(세그먼트 구성, 구간·전환·맥동 규칙, 정리 지점).
3. 계획과 달라진 내용과 이유.
4. 검증 결과: 경계표(1801/1800/1799, 301/300/299, 61/60/59, 1/0/초기화, 최초·Save/Load·지상↔지하 반복, 자리 전환, 해상도·HUD 겹침, 애니메이션 중첩·잔존, 종료 이벤트 중복) 각 항목 **통과/실패/미검증**과 근거.
5. 미검증 사항과 남은 제한.
