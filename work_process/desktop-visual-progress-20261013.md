# 데스크탑 시각 개선 착수 — 2026-10-13

기준: work_process/notebook-visual-review-20261013.md / HEAD 42068539.
원격 main은 576c1be4(B-145~147 후속)까지 확인했으며, PR #168은 미병합이다. 사용자 변경 보호를 위해 이번에 main 통합·PR 병합은 하지 않았다.

## 첫 수정

- V02: DemoObjectiveView.SetClaimText에서 보상 행을 -388, 안내를 상단 중앙 기준 -428에 배치. 글자 16, 높이 28, 대비 강화. 기존 씬에도 런타임으로 적용. PromptB951QuestClearRewardUiBuilder도 같은 배치를 생성한다. 보상 지급 로직 변경 없음. 실제 팝업 화면 검수는 남아 있다.
- V04: 낙하 종료 후 바위를 납작한 고정 잔재로 남기지 않고 회수. 기존 0.3초 착지 먼지는 유지하며 Reduce Motion에서는 생략한다. 붕괴 판정·피해·타일/저장 로직 변경 없음.

## 검증

Unity 6000.5.4f1 / 기존 Run-UnityValidation.ps1 / quick PlayMode:
- FallingRockAndImpactDust_AreRemovedAfterLanding: PASS
- PromptB55_1_EachFallingRockConsumesOnePlayerContact: PASS
Selected 2 / Executed 2 / Pass 2 / Fail 0 / Skip 0 / ExitCode 0.
결과: sub-terra/Builds/VisualReview-20261013/collapse-tests/.
새 테스트는 잔재 및 먼지의 회수를 검증한다. 기존 검사로 피해 횟수 회귀를 확인했다. 이미 통과한 시설 이동 12개는 반복하지 않았다.

## 아직 남은 작업

- V01: HUD/미션/시설창/우측 메뉴의 실제 공간·겹침을 점검하고 압축. 전체 글자/Canvas를 일괄 축소하지 않는다.
- V03: 위험 단계 인지는 유지하며 균열·떨림을 완화. 실제 직렬화 값과 Reduce Motion 확인.
- V05: 레일 접합점·폭·재질 수정은 기존 ElevatorVisualPrefabSetup으로 적용.
- V06: DroneScanPulseView와 PowerSupplyRangeIndicator를 각각 재현하여 스크린샷의 원 소유자를 확정한 후 조정.
- 보상 안내 전후 화면, 1920x1080/2560x1440 UI 검수.
- 전체 Demo 완주·종료 후 이어하기·Windows 종료 ComputeBuffer 확인은 미완료.

수정 파일: PromptB951QuestClearRewardUiBuilder.cs, DemoObjectiveView.cs, StructuralCrackOverlay.cs, StructuralCollapsePlayModeTests.cs.
씬·프리팹 YAML·ProjectSettings·패키지·버전·사용자 저장 슬롯·stash 변경 없음. 신규 Inspector 연결 없음. 기존 QA 실행 파일에는 이번 소스 수정이 들어 있지 않으므로 그 실행 화면을 수정 결과로 판정하지 않는다.
공유 브랜치: codex/demo-visual-first-fixes. PR 기준 브랜치는 codex/facility-move-runtime-followup이며 PR #168 이후 변경 5개 파일만 검토한다. PR #168이 main에 병합된 뒤 이 PR의 기준 브랜치를 main으로 변경한다.
최종 Builder 좌표 보정 후 Unity batchmode 컴파일도 종료 코드 0으로 완료했다. git diff --check 통과. 자동 생성 InitTestScene과 기존 작업 폴더는 커밋 대상에서 제외했다.
