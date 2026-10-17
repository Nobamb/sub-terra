# PR #180 충돌 해결 및 최신 main 통합 — 2026-10-17

## 기준 및 범위
- 로컬 시작138c4254, 원격 PR head d2e845b9로 fast-forward. 기존 미추적 작업 폴더/임시 테스트 씬/stash 보존.
- 최신 origin/main dda39492 및 PR 대상 origin/codex/facility-move-runtime-followup 66457f2e를 fetch했다.
- 팀원 댓글 https://github.com/Nobamb/sub-terra/pull/180#issuecomment-6208307548 확인. PR base는 이미 #168 브랜치로 변경되어 있었다.
- #180 브랜치에 최신 #168을 일반 merge(fe2615d4)하고 최신 main도 일반 merge했다. #180을 #168에 승인 병합하거나 #168을 main에 병합한 작업은 아니다. 강제 푸시 없음.

## 충돌 해결
- 최신 #168 통합은 로컬 Git에서 자동 통합되었다. 미션 요약의 간결한 다음 행동 안내, 우클릭 시설 이동, HUD 체력 잔상 및 사다리 상단 탈출을 보존했다.
- main 통합의 유일한 수동 충돌 파일: Assets/_Project/Scripts/App/Integration/RunFailureRuntimeController.cs.
- #176의 CreateSurfaceSceneLoader 완료 콜백/즉시·지연 실패 복구를 유지하고, 최신 main의 확인 대기/글리치 전환/자원 손실 및 저장1회/플레이어 이동1회 처리를 함께 유지했다.
- StructuralCrackOverlay와 DroneScanPulseView의 #180 시각 완화 보존. PlayerMovement는 최신 #168과 동일하며 main의 점프 관련 개선도 유지되어 있다.
- 씬/프리팹 YAML 수동 수정 및 빌더 재실행 없음. 설정/패키지/Unity 버전을 임의 변경하지 않았다. main/부모에서 상속한 기존 설정은 그대로 통합.

## 검사 보정 및 추가
- 미션 요약 검사: 전체 설명 대신 현재 의도인 NextActionHint 검증. 상세 설명 검사는 유지.
- 시설 검사: 우클릭 안내 문구와 단위 셀 바닥(-0.5) 기대값을 기존 구현에 맞춤. 호버의 무변경/이동 취소/동일 인스턴스 검사는 유지.
- 체력 잔상: EditMode에서 일반 MonoBehaviour 비활성 콜백이 자동 실행되지 않으므로 OnDisable을 reflection으로 명시 호출. 잔상 수치/마스크/색상 검증 유지.
- 사다리 재탑승: 점프 후에도 유효한 사다리 범위 안에 있도록 fixture 높이5 적용 및 상단보다 아래임을 추가 확인. 실제 PlayerMovement 수정 없음.
- 지연 지상 로드 실패 콜백 후 Active 상태/조작 복구 회귀 검사 추가. 저장 경로 및 시스템 상태를 직접 조작하는 QA 치트는 추가하지 않았다.

## 검증 결과
Unity6000.5.4f1 실제 컴파일 및 Test Framework 실행.
- 첫 EditMode: 73개 중69 PASS/4 FAIL. 위 구형 기대값/콜백 환경 문제 기록 보존.
- nographics 재검사: 퀘스트 화면 캡처 Camera.Render의 URP attachment 오류로 Editor native crash. 결과 없음. 실패 로그 보존 후 그래픽 장치 활성 실행으로 전환.
- 그래픽 장치 활성 EditMode: 80개 중79 PASS/1 FAIL(체력 잔상 SendMessage의 ShouldRunBehaviour assert). reflection 호출로 수정 후 해당 클래스6/6 PASS. 따라서 대상 EditMode80개 모두 최종 통과 근거 확보. 전체80개를 한 번에 통과한 결과라고 표현하지 않는다.
- PlayMode 최초: 36개 중35 PASS/1 FAIL(사다리 재탑승 fixture가 상단을 벗어남).
- PlayMode 최종: 36/36 PASS, exit0. RunFailureRuntimePlayModeTests3, PlayerMovementPlayModeTests31, QaPointerInputTests2. 확인 전 이동 금지/전환 중 이동1회/복구/중복 자원 손실 방지 및 지연 콜백 복구 포함.
- EditMode 대상: PromptB105SideMenuTests, PromptB106HudTests, PromptB107QuestUiTests, PromptB110BuildingMenuTests, PromptB49FacilityVisualTests, StructuralCollapse, DroneSensorUpgradeTests, DemoDeepZoneUnlockPathTests, ElevatorTravelSessionTests.
- 로컬 산출물: sub-terra/Builds/PR180-Sync-20261017/의 editmode*.log/xml, hud-final.log/xml, playmode*.log/xml 및 퀘스트 PNG3장. 최종 HUD 검사 exit0. PlayMode는 test-saves 격리 경로 사용.
- 테스트가 갱신한 폰트/통합 씬/ProjectSettings/기존 증거 PNG3장은 통합 index로 복원. 실제 사용자 세이브와 기존 QA 진행 슬롯 수정 없음. 새 캡처는 Builds에 보존.
- 충돌 표식 및 수동 C# diff 공백 검사 통과. main에서 가져온 Unity YAML/meta 기존 trailing whitespace는 범위 밖 정리 없이 보존.

## 상태 및 다음 작업
충돌 해결·관련 자동 검사 완료. Windows 새 빌드/Demo18완주/완주 상태 이어하기/실행 종료 증거는 이번에 수행하지 않았다. 10월14일 빌드는 최신 통합 결과의 증거가 아니다.
1. 팀 검토 후 #180 → #168 → main 순서로 병합. 최종 대상의 변경 상태를 다시 확인한다.
2. 최종 통합 head에서 QA Windows 빌드를 새로 만들고 Bootstrap → 새 게임 → 탐사 스모크 및 HUD/우클릭 시설 이동/사다리/게임오버 확인과 지연 귀환 실패 복구를 확인한다.
3. 기존 QA 슬롯은 백업 후15/18부터 리튬 보상/전력/화물 요약을 확인하고 가스 정화 → 정산 판매 → 비상 탈출 귀환 →18/18까지 진행한다.
4. 완전 종료 후 같은 격리 경로 이어하기에서 완료/자원/시설 비교, ComputeBuffer/예외/렌더 텍스처 경고 확인. 굵은 회색 타일 격자와 player/inventory 요약 불일치는 별도 원인 확인.
